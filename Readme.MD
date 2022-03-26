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


## Locking a Stream:

Streams can be scope locked, so no simultanuous write operations of other threads are able to interferr a planed output operation:
```
Consola.Locked stream = Consola.StdStream.Out.Stream;
```

with getting *Out.Stream* and assigning it to a (here *stream*) variable, the StdStream.Out stream gets thread locked. No other thread will be able writing to *StdStream.Out* anymore and will be forced to wait until *stream* later releases the lock via calling *.End();*
```
stream.Put("Hello ").Put(worldObj).Put(",... how are you?\n");
foreach(var obj in ObjectsList)
    stream.Put("Hello").Put(obj).Put("How are you?\n");
stream.Put("ByBy").End();
```

*this example ensures that all elements of list *ObjectList* are progressed and will be written to StdStream.Out as one consectuitive text block - with no interruption other threads could cause when writing output at same time.

any other thread will be able writing StdStream.Out again as soon either *stream.End()* is called, or at least with *stream* leaving the actual scope. (it then, on disposal, calls *.End()* automatically as soon catched by the collector) 


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


