Consola
=======


Dll for Threadsafe Std Stream Access and Log File Generation to be used from within DotNet Projects


## Simple short description:

To getting it used, simply clone and build the solution: 
 
 - *StdStreamsDll/StdStreams.sln* builds .Net(Framework) 4.8 dll 
 
 - *ConsolaCore5/ConsolaCore5.sln* builds a .Net(Core) 5.0 dll
 
In a Project, to make usage of *Consola*, add a reference to the *Consola.dll* file that was built and add *Consola.StdStream.Init();* as first line inside the programs Main() function. (makes it initializing by default parameters). So any code in the Project should be able now using the *Consola.StdStream* api in quite a similar way as like also the  *System.Console* api would be used:

## Writing Output:

writing a strng to stdout
```
Consola.StdStream.Out.WriteLine("Hello {0}!",worldObj);
```

writing a string to stderr
```
Consola.StdStream.Err.WriteLine("Bad Error");
```

reading a line from stdin
```
string userinp = Consola.StdStream.Inp.ReadLine();
```

## Redirecting Streams: 

redirect *stderr* to *stdout*
```
Consola.StdStream.Out = Consola.StdStream.Err;
```

---
## Locked Streams:

Streams can be scope locked, so no simultanuous write operations of other threads are able to interferr a planed output operation:

```
Consola.Locked stream = Consola.StdStream.Out.Stream;
```

with getting *Out.Stream* and assigning it to a variable (here *stream*) the StdStream.Out stream gets thread locked so no other threads can write to *StdStream.Out* and will be forced into wait state until *stream* releases the the lock:

```
stream.Put("Hello ").Put(worldObj).Put(",... how are you?\n");
foreach(var obj in ObjectsList)
    stream.Put("Hello").Put(obj).Put("How are you?\n");
stream.Put("ByBy").End();
```
(the above example code ensures that all elements of list *ObjectList* will be progressed and written to StdStream.Out without a risk that other threads could interrupt within output until the lock gets released again via *stream.End()* call 
---

## Starting Log Sessions: 

Streams can be not only redirected to other streams, but also to attached file loggers

this enables *stderr* stream logging via a file logger, which gets attached as *Err.Log*:
```
Consola.StdStream.Err.Log = Consola.StdStream.Err;
```

## Redirecting Log files: 

this enables *stdout* stream logging via a file logger, which gets attached as *Out.Log*:
```
Consola.StdStream.Out.Log = Consola.StdStream.Out;
```

this enables *stderr* stream logging via the *Out.Log* logger which gets attached as *Err.Log*:
```
Consola.StdStream.Err.Log = Consola.StdStream.Out.Log;
```

(* e.g.: in the above example, stderr and stdout are not redirected so. these still will stay a separate stream each. but both will produce log within one single log file so) 


