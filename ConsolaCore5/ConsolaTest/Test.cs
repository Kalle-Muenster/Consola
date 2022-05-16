// Test.cs
/////////////////////////////////

using Consola;
using Consola.Test;
using System.Collections.Generic;


List<string> Args = new( args );

/////////////////////////////////

StdStream.Init(
    CreationFlags.TryConsole |
    CreationFlags.NoInputLog |
    CreationFlags.CreateLog
);

Test test = new ConsolaTest(
    Args.Contains("-v") ||
    Args.Contains("--verbose")
).Run();

return test.wasErrors() ?
  -1 : test.hasPassed() ?
   0 : test.getFailures();
