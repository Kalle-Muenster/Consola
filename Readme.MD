Consola
=======


Dll for Threadsafe Std Stream Access and Log File Generation to be used from within DotNet Projects


## Simple short description:

To getting it used, simply clone and build the solution: 
 
 - *StdStreamsDll/StdStreams.sln* builds .Net(Framework) 4.8 dll 
 
 - *ConsolaCore5/ConsolaCore5.sln* builds a .Net(Core) 5.0 dll
 
In a Project to make usage of *Consola*, add a reference to the *Consola.dll* file that was built and add *Consola.StdStream.Init();* as first line inside the programs Main() function. (makes it initializing by default parameters). So any code in the Project should be able now using the *Consola.StdStream* api in quite a similar way as like also the  *System.Console* api would be used:

## Writing Output:

### writing a strng to stdout
```
Consola.StdStream.Out.WriteLine("Hello {0}!",worldObj);
```

### writing a string to stderr
```
Consola.StdStream.Err.WriteLine("Bad Error");
```

### reading a line from stdin
```
string userinp = Consola.StdStream.Inp.ReadLine();
```

## Redirecting Streams: 

For redirecting streams, these can be simply assigned to each other:
 
redirect *stderr* to *stdout*
```
Consola.StdStream.Out = Consola.StdStream.Err;
```

## Starting Log Sessions: 

Streams can be not only redirected to other streams, but also to attached file loggers

this enables *stderr* stream logging via a file logger, which gets attached as *Err.Log*:
```
Consola.StdStream.Err.Log = Consola.StdStream.Err
```

## Redirecting Log files: 

this enables *stdout* stream logging via a file logger, which gets attached as *Out.Log*:
```
Consola.StdStream.Out.Log = Consola.StdStream.Out;
```

this enables *stderr* stream logging via the *Out.Log* logger which gets attached as *Err.Log*:
```
Consola.StdStream.Err.Log = Consola.StdStream.Out.Log
```

(* e.g.: in the above example, stderr and stdout are not redirected so. these still will stay a separate stream each. but both will produce log within one single log file so) 

## Using Stream Objects:

Get a locked output stream object: 
```
Locked output = Consola.StdStream.Out.Stream;
```

Use it in the current scope: 
```
output.Put("Hallo").Put(" ");
WorldClass worldObj = GetWorldInfo();
output.Put(" ...").Put(worldObj).Put(" how are you?");
```

When done with writing, to reelease lock, *End()* must be called. So other threads can write to StdStream.Out again
```
output.Put(" ...by by").End();
```

