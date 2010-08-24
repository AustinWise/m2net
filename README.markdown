m2net, a .NET library to develop [Mongrel2] handlers
-----------------------------------------------------------------

[m2net] helps you develop Mongrel2 handlers. It also comes with a rough port of the
[Cassini] ASP.NET web server to use m2net instead of sockets.

Dependencies
------------

 - .NET Framework 3.5 (Can be compiled against the 2.0 BCL if you add add your own ExtensionAttribute)
 - [libzmq.dll](http://www.zeromq.org/) (included)
 - [clrzmq](http://github.com/zeromq/clrzmq/) (included)
 - [Jayrock.JSON](http://jayrock.berlios.de/) (included)


License
-------

m2net is licensed under the BSD license.  m2net.asp is licensed under the [Microsoft Public License], since it is derived from Cassini.


Currently limitations
---------------------

* Only works with x86 .NET apps on Windows.  That is, it will run just fine
  under x64 Windows just as long as you compile it as X86 and not AnyCPU or x64.


Areas for improvement
---------------------

 - A better Linux build experiance, perhaps such that the Linux build can be based on the VS solution and project files.
 - See if the recieve socket also does not like multiple threads using it and added a recieve queue if needed.
 - Complile a 64-bit version of the ZMQ native library and use it with clrzmq to allow for 64-bit handlers on Windows.
 - Make the Cassini port more correctly implement the overrides of SimpleWorkerRequest.


  [m2net]: http://github.com/AustinWise/m2net/
  [Cassini]:http://blogs.msdn.com/b/dmitryr/archive/2008/10/03/cassini-for-framework-3-5.aspx
  [Mongrel2]:http://mongrel2.org/
  [Microsoft Public License]:http://www.opensource.org/licenses/ms-pl.html