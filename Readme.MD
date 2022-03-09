Consola
=======


Dll for Threadsafe Std Stream Access and Log File Generation to be used from within DotNet Projects


## Simple short description:

To getting it used, simply clone and build the solution: 
 
 - *StdStreamsDll/StdStreams.sln* builds .Net(Framework) 4.8 dll 
 
 - *ConsolaCore5/ConsolaCore5.sln* builds a .Net(Core) 5.0 dll
 
In a Project to make usage of *Consola*, add a reference to the Console.dll file that was built and add *Consola.StdStream.Init();* as first line inside the programs Main() function. (makes it initializing by default parameters). So any code in the Project should be able now using the *Consola.StdStream* api in quite a similar way as like also the  *System.Console* api would be used:

## Writing Output:
```
// writing a strng to stdout

Consola.StdStream.Out.WriteLine("Hello {0}!",worldObj);

// writing a string to stderr

Consola.StdStream.Err.WriteLine("Bad Error");

// reading a line from stdin

string userinp = Consola.StdStream.Inp.ReadLine();
```

## Redirecting Streams: 

Streams can be simply assigned to each other:

```  
// example: this would redirect stderr to stdout

Consola.StdStream.Out = Consola.StdStream.Err;

```

## Starting Log Sessions: 

Streams can simply be redirected to log files:

```
// anthing that goes through stderr shall get logget:

Consola.StdStream.Err.Log = Consola.StdStream.Err

```

Log files can be redirectd individually:

```
// this enables logging for stdout

Consola.StdStream.Out.Log = Consola.StdStream.Out;


// this redirects Error-Log to the Output-Log. 

Consola.StdStream.Err.Log = Consola.StdStream.Out.Log

```
(* e.g.: in the above example, stderr and stdout are not redirected so. these still will stay a separate stream each. but both will produce log within a single log file so) 

## Using Stream Objects:

```
// gets a locked output stream object: 

Locked output = Consola.StdStream.Out.Stream();


// which can be used from within current scope: 

output.Put("Hallo").Put(" ");
WorldClass worldObj = GetWorldInfo();
output.Put(" ...").Put(worldObj).Put(" how are you?");

// to enable other threads being able again writing to
// StdOut, the locked stream when done with writing,
// must release that lock it holds via calling End()

output.Put(" ...by by").End();

```



