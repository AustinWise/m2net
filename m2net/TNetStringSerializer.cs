using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;

namespace m2net
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class TNetStringPropertyAttribute : Attribute
    {
        // This is a positional argument
        public TNetStringPropertyAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }

        public string PropertyName { get; private set; }

        public Encoding Encoding { get; set; }
    }

    abstract class TypeMapping
    {
        abstract public bool CanDeser(TnetString tns, Type type);
        abstract public object Deser(TnetString tns, Type type);
    }

    class LambedaTypeMapping : TypeMapping
    {
        Func<TnetString, Type, bool> mCanMap;
        Func<TnetString, Type, object> mMap;

        public LambedaTypeMapping(Func<TnetString, Type, bool> canMap, Func<TnetString, Type, object> map)
        {
            this.mCanMap = canMap;
            this.mMap = map;
        }

        public override bool CanDeser(TnetString tns, Type type)
        {
            return mCanMap(tns, type);
        }

        public override object Deser(TnetString tns, Type type)
        {
            return mMap(tns, type);
        }
    }

    class DictTypeMapper : TypeMapping
    {
        private TNetStringSerializer mSer;

        public DictTypeMapper(TNetStringSerializer ser)
        {
            this.mSer = ser;
        }

        public override bool CanDeser(TnetString tns, Type type)
        {
            return tns.Type == TnetStringType.Dict && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public override object Deser(TnetString tns, Type type)
        {
            if (type.GetGenericArguments()[0] != typeof(string))
                throw new NotSupportedException("Dictionary keys must be strings");

            var subType = type.GetGenericArguments()[1];

            var dict = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), subType));
            var add = dict.GetType().GetMethod("Add");
            foreach (var kvp in tns.DictValue)
            {
                add.Invoke(dict, new object[] { kvp.Key, mSer.Deserialize(kvp.Value, subType, null) });
            }
            return dict;
        }
    }

    class DictClassTypeMapper : TypeMapping
    {
        private TNetStringSerializer mSer;

        public DictClassTypeMapper(TNetStringSerializer ser)
        {
            this.mSer = ser;
        }

        public override bool CanDeser(TnetString tns, Type type)
        {
            return tns.Type == TnetStringType.Dict;
        }

        public override object Deser(TnetString tns, Type type)
        {
            object obj = Activator.CreateInstance(type);
            var dict = tns.DictValue;
            foreach (var p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (p.GetSetMethod() == null || !p.GetSetMethod().IsPublic)
                    continue;

                string name = p.Name;
                Encoding enc = null;
                foreach (TNetStringPropertyAttribute atr in p.GetCustomAttributes(typeof(TNetStringPropertyAttribute), false))
                {
                    name = atr.PropertyName;
                    if (atr.Encoding != null)
                        enc = atr.Encoding;
                }

                if (!dict.ContainsKey(name))
                    continue;

                p.SetValue(obj, mSer.Deserialize(dict[name], p.PropertyType, enc), null);
            }

            return obj;
        }
    }

    class TupleTypeMapping : TypeMapping
    {
        private List<TypeMapping> mMappings;
        private ConcurrentDictionary<Type, Func<TnetString, object>> mDeserCache;

        MethodInfo[] tupleCreates = typeof(Tuple).GetMethods().Where(m => m.Name == "Create").Where(m => m.GetParameters().Length < 8).OrderBy(m => m.GetParameters().Length).ToArray();
        MethodInfo whereMeth = typeof(Enumerable).GetMethods().Where(o => o.Name == "Where" && o.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2).Single().MakeGenericMethod(typeof(TypeMapping));
        MethodInfo firstOrDefaultMeth = typeof(Enumerable).GetMethods().Where(o => o.Name == "FirstOrDefault" && o.GetParameters().Length == 1).Single().MakeGenericMethod(typeof(TypeMapping));

        public TupleTypeMapping(List<TypeMapping> mappings)
        {
            this.mMappings = mappings;
            this.mDeserCache = new ConcurrentDictionary<Type, Func<TnetString, object>>();
        }

        public override bool CanDeser(TnetString tns, Type type)
        {
            return type.GetInterface("System.ITuple") != null;
        }

        public override object Deser(TnetString tns, Type type)
        {
            Func<TnetString, object> fun;

            if (!mDeserCache.TryGetValue(type, out fun))
            {
                var tupTypes = new List<Type>(type.GetGenericArguments());

                if (tupTypes.Count < 1 || tupTypes.Count >= 8)
                    throw new Exception("Tuple size not supported.");

                var tnsParam = Expression.Parameter(typeof(TnetString), "tns");

                var block = new List<Expression>();
                var foundElement = Expression.Variable(typeof(bool), "found");
                var tupElVars = new List<ParameterExpression>();
                block.Add(Expression.Assign(foundElement, Expression.Constant(false, typeof(bool))));

                foreach (var t in tupTypes)
                {
                    var v = Expression.Variable(t);
                    block.Add(Expression.Assign(v, Expression.Default(t)));
                    tupElVars.Add(v);
                }

                for (int i = 0; i < tupTypes.Count; i++)
                {
                    var t = tupTypes[i];
                    var mapperVar = Expression.Variable(typeof(TypeMapping), "m");
                    var whereLambdaVar = Expression.Parameter(typeof(TypeMapping), "mm");
                    var getMapper = Expression.Assign(mapperVar, Expression.Call(firstOrDefaultMeth, Expression.Call(whereMeth, Expression.Constant(mMappings), Expression.Lambda(Expression.Call(whereLambdaVar, typeof(TypeMapping).GetMethod("CanDeser"), tnsParam, Expression.Constant(t)), whereLambdaVar))));
                    var innerIf = Expression.IfThen(Expression.NotEqual(mapperVar, Expression.Constant(null)), Expression.Block(Expression.Assign(foundElement, Expression.Constant(true)), Expression.Assign(tupElVars[i], Expression.Convert(Expression.Call(mapperVar, typeof(TypeMapping).GetMethod("Deser"), tnsParam, Expression.Constant(t)), t))));
                    block.Add(Expression.IfThen(Expression.Equal(foundElement, Expression.Constant(false)), Expression.Block(new[] { mapperVar }, getMapper, innerIf)));
                }

                block.Add(Expression.IfThen(Expression.Equal(foundElement, Expression.Constant(false)), Expression.Throw(Expression.New(typeof(Exception).GetConstructor(new[] { typeof(string) }), Expression.Constant("No conversion found.")))));
                block.Add(Expression.Call(tupleCreates[tupElVars.Count - 1].MakeGenericMethod(type.GetGenericArguments()), tupElVars));

                var f = Expression.Lambda<Func<TnetString, object>>(Expression.Block(tupElVars.Concat(new[] { foundElement }), block), tnsParam);
                fun = f.Compile();
                mDeserCache.TryAdd(type, fun);
            }

            return fun(tns);
        }
    }

    class ListTypeMapping : TypeMapping
    {
        private TNetStringSerializer mSer;
        private ConcurrentDictionary<Type, Func<IEnumerable<object>, object>> mDeserCache;

        public ListTypeMapping(TNetStringSerializer ser)
        {
            this.mSer = ser;
            mDeserCache = new ConcurrentDictionary<Type, Func<IEnumerable<object>, object>>();
        }

        public override bool CanDeser(TnetString tns, Type type)
        {
            return tns.Type == TnetStringType.List && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        public override object Deser(TnetString tns, Type type)
        {
            var subType = type.GetGenericArguments()[0];
            var list = tns.ListValue.Select(subTns => mSer.Deserialize(subTns, subType, null));

            Func<IEnumerable<object>, object> fun;

            if (!mDeserCache.TryGetValue(subType, out fun))
            {
                var listParam = Expression.Parameter(typeof(IEnumerable<object>));
                var casted = Expression.Call(typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(subType), listParam);
                var listed = Expression.Call(typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(subType), casted);
                fun = Expression.Lambda<Func<IEnumerable<object>, object>>(listed, listParam).Compile();
                mDeserCache.TryAdd(subType, fun);
            }

            return fun(list);
        }
    }

    public class TNetStringSerializer
    {
        private List<TypeMapping> mMappings = new List<TypeMapping>();
        private Encoding Enc = Encoding.ASCII;

        public TNetStringSerializer()
        {
            var tupleCreates = typeof(Tuple).GetMethods().Where(m => m.Name == "Create").Where(m => m.GetParameters().Length < 8).OrderBy(m => m.GetParameters().Length).ToArray();

            mMappings.Add(new LambedaTypeMapping((tns, _) => tns.Type == TnetStringType.Null, (_, type) =>
            {
                if (type.IsArray)
                    return Array.CreateInstance(type.GetElementType(), 0);
                else if (type.IsValueType)
                    return Activator.CreateInstance(type);
                else
                    return null;
            }));
            mMappings.Add(new LambedaTypeMapping((tns, type) => tns.Type == TnetStringType.String && type == typeof(byte[]), (tns, _) => tns.StringValue.ToArray()));
            mMappings.Add(new LambedaTypeMapping((tns, type) => tns.Type == TnetStringType.String && type == typeof(ArraySegment<byte>), (tns, _) => tns.StringValue));
            mMappings.Add(new LambedaTypeMapping((tns, type) => tns.Type == TnetStringType.String && type == typeof(string), (tns, _) => tns.StringValue.ToString(Enc)));
            mMappings.Add(new LambedaTypeMapping((tns, type) => tns.Type == TnetStringType.Int && type == typeof(int), (tns, _) => tns.IntValue));
            mMappings.Add(new LambedaTypeMapping((tns, type) => tns.Type == TnetStringType.Bool && type == typeof(bool), (tns, _) => tns.BoolValue));
            mMappings.Add(new TupleTypeMapping(mMappings));
            mMappings.Add(new ListTypeMapping(this));
            mMappings.Add(new DictTypeMapper(this));
            mMappings.Add(new DictClassTypeMapper(this));
        }

        public object Deserialize(TnetString tns, Type type, Encoding stringEncoding)
        {
            foreach (var m in mMappings)
            {
                if (m.CanDeser(tns, type))
                    return m.Deser(tns, type);
            }

            throw new NotSupportedException();
        }

        public T Deserialize<T>(TnetString tns)
        {
            return (T)Deserialize(tns, typeof(T), null);
        }

        public T Deserialize<T>(ArraySegment<byte> netstring)
        {
            var parsed = netstring.TParse();
            return (T)Deserialize(parsed.Data, typeof(T), null);
        }
    }
}
