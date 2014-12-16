m2net, a .NET library to develop [Mongrel2] handlers
-----------------------------------------------------------------

[m2net] helps you develop Mongrel2 handlers. It also comes with a rough port of the
[Cassini] ASP.NET web server to use m2net instead of sockets.

nuget
-----

The libary is available on [Nuget].


Dependencies
------------

 - .NET Framework 4.0
 - [clrzmq](http://packages.nuget.org/packages/clrzmq-x64/)
 - [Jayrock.JSON](http://packages.nuget.org/packages/jayrock-json/)


License
-------

m2net is licensed under the 3-clause [BSD License].  m2net.asp is licensed under
the [Microsoft Public License], since it is derived from Cassini.


Currently limitations
---------------------

* Only works with x64 .NET apps on Windows. This should be improved in the next release.


Areas for improvement
---------------------

 - Inject m2net.Asp.dll into ASP.NET on Mono so that it does not have to be in the GAC.
 - A better Linux build experiance, perhaps such that the Linux build can be based on the VS solution and project files.
 - Document how to use m2net and the ASP.NET handler.
 - See if the recieve socket also does not like multiple threads using it and added a recieve queue if needed.
 - Complile a 64-bit version of the ZMQ native library and use it with clrzmq to allow for 64-bit handlers on Windows.
 - Make the Cassini port more correctly implement the overrides of SimpleWorkerRequest.


  [m2net]: http://github.com/AustinWise/m2net/
  [Cassini]:http://blogs.msdn.com/b/dmitryr/archive/2008/10/03/cassini-for-framework-3-5.aspx
  [Mongrel2]:http://mongrel2.org/
  [Microsoft Public License]:http://www.opensource.org/licenses/ms-pl.html
  [BSD License]:http://en.wikipedia.org/wiki/BSD_licenses#3-clause_license_.28.22Revised_BSD_License.22.2C_.22New_BSD_License.22.2C_or_.22Modified_BSD_License.22.29
  [Nuget]:https://www.nuget.org/packages/m2net/