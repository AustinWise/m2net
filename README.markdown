m2net, a .NET library to develop [Mongrel2] handlers
-----------------------------------------------------------------

[m2net] is a pretty rough port of the python handler libraries included in the
Mongrel2 examples.  It also comes with an even more rough port of the
[Cassini] web server to use m2net instead of sockets.

Dependencies
------------

 - .NET Framework 3.5 (though it probably could be easily changed to use 2.0)
 - [libzmq.dll](http://www.zeromq.org/) (included)
 - [clrzmq](http://github.com/zeromq/clrzmq/) (included)
 - [Jayrock.JSON](http://jayrock.berlios.de/) (included)


License
-------

m2net is licensed under the [LGPL] because clrzmq and Jayrock are.  m2net.asp is licensed under the [Microsoft Public License].


Currently limitations
---------------------

* Only works with x86 (x64 not supported).


Areas for improvement
---------------------

 - See if the recieve socket also does not like multiple threads using it and added a recieve queue if needed.
 - Complile a 64-bit version of the ZMQ native library and use it with clrzmq to allow for 64-bit handlers.
 - Make the Cassini port more correctly implement the overrides of SimpleWorkerRequest.



  [m2net]: http://github.com/AustinWise/m2net/
  [Cassini]:http://blogs.msdn.com/b/dmitryr/archive/2008/10/03/cassini-for-framework-3-5.aspx
  [Mongrel2]:http://mongrel2.org/
  [LGPL]:http://www.gnu.org/licenses/lgpl.html
  [Microsoft Public License]:http://www.opensource.org/licenses/ms-pl.html