GMCS_FLAGS=-define:MONO -keyfile:key.snk
LIBS=-r:lib/Jayrock.Json.dll -r:lib/clrzmq.dll

SOURCES=m2net/ByteArrayExtensions.cs  m2net/Connection.cs m2net/Request.cs m2net/Properties/AssemblyInfo.cs 
ASP_SOURCES=m2net.Asp/ByteParser.cs m2net.Asp/Connection.cs \
            m2net.Asp/Server.cs m2net.Asp/ByteString.cs m2net.Asp/Host.cs \
            m2net.Asp/Messages.cs m2net.Asp/Request.cs  m2net.Asp/Properties/AssemblyInfo.cs
ASP_HANDLER_SOURCES=m2net\ AspNetHandler/Program.cs m2net\ AspNetHandler/Properties/AssemblyInfo.cs

ALL=bin/m2net.dll bin/m2net.Asp.dll bin/m2net.AspNetHandler.exe bin/m2net.HandlerTest.exe

all: $(ALL)

bin/:
	mkdir -p bin

bin/m2net.dll: bin/ $(SOURCES)
	gmcs $(GMCS_FLAGS) -t:library $(LIBS) -out:$@ $(SOURCES)

bin/m2net.Asp.dll: bin/m2net.dll $(ASP_SOURCES)
	gmcs $(GMCS_FLAGS) -t:library $(LIBS) -r:bin/m2net.dll -r:System.Web \
		-out:$@ $(ASP_SOURCES)

bin/m2net.AspNetHandler.exe: bin/m2net.dll bin/m2net.Asp.dll $(ASP_HANDLER_SOURCES)
	gmcs $(GMCS_FLAGS) -t:exe $(LIBS) -r:bin/m2net.dll -r:bin/m2net.Asp.dll -r:System.Web \
		-out:$@ $(ASP_HANDLER_SOURCES)

bin/m2net.HandlerTest.exe: bin/m2net.dll m2net\ HandlerTest/Program.cs
	gmcs $(GMCS_FLAGS) -t:exe $(LIBS) -r:bin/m2net.dll -out:$@ m2net\ HandlerTest/Program.cs

clean:
	rm $(ALL)