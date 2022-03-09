Consola
=======


Dll for Threadsafe Std Stream Access and Log File Generation for DotNet Projects


1. Simple short description:

- For getting it used, simply clone the repo, and build the solution: 
-- StdStreamsDll/StdStreams.sln builds for .Net(Framework) 4.8
-- ConsolaCore5/ConsolaCore5.sln builds for .Net(Core) 5.0
- In Projects where you want to use it add reference to the built Consola.dll
- for getting it anabled within the programm with programm start, add Consola.StdStream.Init(); as first line inside the programs Main() function.
- From within any Code in the Project it then can be accessed then almost same like reguler Console output like this:


// writing a strng to stdout
Consola.StdStream.Out.WriteLine("Hello {0}!",worldObj);

// writing a string to stderr
Consola.StdStream.Err.WriteLine("Bad Error");

// reading a line from stdin
string userinp = Consola.StdStream.Inp.ReadLine();


- Redirecting Streams: 

// these can be simply assigned to each other like this:   

// example: this would redirect stderr to stdout
Consola.StdStream.Out = Consola.StdStream.Err;


- Starting log sessions: 
-- Streams can simply be redirected to also a log files

// anthing that goes through stderr shall get logget:
Consola.StdStream.Err.Log = Consola.StdStream.Err

-- stream loggers them self can be redirectd individually also 

// this enables logging for stdout
Consola.StdStream.Out.Log = Consola.StdStream.Out;
// this redirects Error-Log to the Output-Log. 
Consola.StdStream.Err.Log = Consola.Out.Log
// (* e.g.: stderr and stdout are not redirected so. these stay a separate stream each. they just will produce log within one single log file so) 



