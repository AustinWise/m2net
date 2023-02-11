# m2net, a .NET library to develop Mongrel2 handlers

[m2net] helps you develop [Mongrel2] handlers. It also comes with a rough port of the
Cassini ASP.NET web server to use m2net instead of sockets.

## Why this archived and unlikely to be updated in the future

Since this library and Mongrel2 were released in 2010, a lot has changed in both
the webserver world and the .NET world. On the webserver side, it does not
appear that Mongrel 2 has become popular. Nginx and HA Proxy instead continue
to be popular for the role of proxying HTTP requests. Cloud-based load balancers
and Kubernetes ingress controllers have also become popular solutions.
And gRPC and HTTP/2 changed how web APIs are defined.
So even if this library were rewritten to modern .NET standards
(see below why it would be a re-write), I don't believe there will be an
audience to use it.

On the .NET side, the landscape has radically changed. `async`/`await` changes
the way APIs are designed. .NET Core made .NET available cross platform.
`Span<T>` and `System.Text.Json` changed the way parsers are written.
And ASP.NET Core changed how a web servers hosts ASP.NET apps.
So a total write of this library on completely new dependencies would be
required to be relevant for 2023's .NET.

As an aside, one part of this library does live on: the
[NetString implementation](m2net/NetString.cs) formed the basis of an RPC
library that is used in several
[wafer](https://www.brooks.com/solutions/automation-solutions/factory-automation-solutions/spartan-sorters/)
[handling](https://www.brooks.com/solutions/automation-solutions/wafer-handling-systems/)
[robots](https://www.brooks.com/solutions/automation-solutions/factory-automation-solutions/vision-leap-loadports/)
. It has probably touched the chips running in your server.

## Nuget

The library is available on [Nuget].

## Dependencies

 - .NET Framework 4.0 x64
 - [clrzmq](http://packages.nuget.org/packages/clrzmq-x64/)
 - [Jayrock.JSON](http://packages.nuget.org/packages/jayrock-json/)

## License

m2net is licensed under the 3-clause [BSD License].  m2net.asp is licensed under
the [Microsoft Public License], since it is derived from Cassini.

## Currently limitations

* Only works with x64 .NET apps on Windows.

  [m2net]: https://github.com/AustinWise/m2net/
  [Mongrel2]:https://mongrel2.org/
  [Microsoft Public License]:https://opensource.org/licenses/ms-pl.html
  [BSD License]:LICENSE
  [Nuget]:https://www.nuget.org/packages/m2net/
