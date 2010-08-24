
SOURCES=m2net/ByteArrayExtensions.cs  m2net/Connection.cs m2net/Request.cs m2net/Properties/AssemblyInfo.cs 
LIBS=-r:lib/Jayrock.Json.dll -r:lib/clrzmq.dll


bin/m2net.dll: $(SOURCES)
	mkdir -p bin
	gmcs -t:library $(LIBS) -out:$@ $(SOURCES)

clean:
	rm bin/m2net.dll
