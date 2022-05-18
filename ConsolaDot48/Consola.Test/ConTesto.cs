using System;
using System.Collections.Generic;

namespace Consola
{
    namespace Test
    {
        [Flags]
        public enum TestResults : uint
        {
            Verbose = 0x00000001u, XmlOutput= 0x00000002u, TextOutput = 0,
            PASS = 1397965136u, FAIL = 1279869254u, SKIP = 1346980691u
        }

        abstract public class Test
        {
            private uint step = 0;
            private int count = -1;
            private int failures = -1;
            protected TestResults flags = 0;
            private List<string> errors;
            private string current;
            private int skiperror;
            private int skipfails;
            private int failsgone;
            public event Action TestRun;

            private void WriteXmlStepResult( uint casenum, int stepnum, TestResults result, string description )
            {
                StdStream.Aux.Xml.WriteElement("teststep", new string[] { $"result={result}", $"number={step}.{count}" })
                    .WriteElement("description").WriteContent( description )
                .CloseScope("teststep");
            }

            private void WriteXmlDescription( uint casenum, int stepnum, string description )
            {
                StdStream.Aux.Xml.WriteElement( "description", new string[] {"level=INFO",$"related={StdStream.Aux.Xml.Element}-{casenum}.{stepnum}" } )
                                 .WriteContent( description ).CloseScope();
            }

            private void WriteXmlCaseResult( uint casenum, string casename, int failed, int total, int skips )
            {
                TestResults result = failed == 0 ? total > skips ? TestResults.PASS : TestResults.SKIP : TestResults.FAIL;
                StdStream.Aux.Xml.WriteElement("caseresult", new string[] {
                    $"result={result}",$"number={casenum}",$"name={casename}",$"failed={failed}",
                    $"passed={total-(skips+failed)}",$"skipped={skips}" }
                ).CloseScope("testcase");
            }

            public bool Verbose
            {
                get { return (flags & TestResults.Verbose) > 0; }
                set { if (value) flags |= TestResults.Verbose; else flags &= ~TestResults.Verbose; }
            }

            public TestResults Results
            {
                get { return flags; }
                set { flags = value; }
            }

            // internals
            public int setPassed(string description)
            {
                if (failures < 0) return failures;
                if (count > 0)
                {
                    if( flags.HasFlag( TestResults.XmlOutput ) ) 
                        WriteXmlStepResult( step, count, TestResults.PASS, description );
                    StdStream.Out.WriteLine( "PASS [{0}.{1}]: {2}", step, count, description );
                    return (int) (count - (failures - failsgone));
                }
                else
                {
                    StdStream.Out.WriteLine("---------------------------------------------------------------------");
                    StdStream.Out.WriteLine("CASE[{0}]-{1} Tests PASSED", step, description);
                    return failures - failsgone;
                }
            }

            public int setFailed(string description)
            {
                if (failures < 0) return failures;
                if (count > 0)
                {
                    if( flags.HasFlag(TestResults.XmlOutput) )
                        WriteXmlStepResult( step, count, TestResults.FAIL, description);
                    StdStream.Err.WriteLine("FAIL [{0}.{1}]: {2}", step, count, description);
                    StdStream.Out.Log.WriteLine("FAIL [{0}.{1}]: {2}", step, count, description);
                    return ++failures - failsgone;
                }
                else
                {
                    StdStream.Err.WriteLine("---------------------------------------------------------------------");
                    StdStream.Err.WriteLine("CASE[{0}]-{1} Tests FAILED", step, description);
                    StdStream.Out.Log.WriteLine(
                        "---------------------------------------------------------------------");
                    StdStream.Out.Log.WriteLine("CASE[{0}]-{1} Tests FAILED", step, description);
                    return failures - failsgone;
                }
            }

            public void setSkipped(string description)
            {
                if (failures < 0) return;
                if (count > 0)
                {
                    if( flags.HasFlag(TestResults.XmlOutput) )
                        WriteXmlStepResult( step, count, TestResults.SKIP, description );

                    StdStream.Out.WriteLine("SKIP [{0}.{1}]: {2}", step, count, description);
                }
                else
                {
                    StdStream.Out.WriteLine("CASE[{0}]-{1} test steps SKIPPED", step, description);
                }
            }

            public int setFatal(string description, bool continueAnyway)
            {
                if (failures > 0)
                {
                    failures = 0;
                }

                if (count > 0)
                {
                    if( flags.HasFlag(TestResults.XmlOutput) )
                        StdStream.Aux.Xml.WriteElement( "erroreport", new string[] { $"number={step}.{count}" } )
                                         .WriteContent( description ).CloseScope( "erroreport" );

                    description = string.Format("FATAL [{0}.{1}]: {2}", step, count, description);
                }
                else
                {
                    description = string.Format("CASE[{0}]-{1} test crashed", step, description);
                }

                StdStream.Err.WriteLine(description);
                StdStream.Out.Log.WriteLine(description);
                errors.Add(description);
                if (!continueAnyway)
                {
                    throw new Exception("FATAL");
                }

                return --failures;
            }

            public void setChecked(bool check, string description)
            {
                if (failures < 0) return;
                ++count;
                if (check)
                {
                    setPassed(description);
                }
                else
                {
                    setFailed(description);
                }
            }


            // Step actions
            public void InfoStep( string fmt, params object[] args )
            {
                if( flags > TestResults.TextOutput ) {
                    string description = string.Format( fmt, args );
                    StdStream.Out.WriteLine( "INFO [{0}.{1}]: {2}", step, count, description );
                    if( flags.HasFlag( TestResults.XmlOutput ) )
                        WriteXmlDescription( step, count, description );
                }
            }

            public void SkipStep(string fmt, params object[] args)
            {
                ++count;
                setSkipped(string.Format(fmt, args));
            }

            public void FailStep(string fmt, params object[] args)
            {
                setChecked(false, string.Format(fmt, args));
            }

            public void PassStep(string fmt, params object[] args)
            {
                if (Verbose)
                {
                    setChecked(true, string.Format(fmt, args));
                }
                else
                {
                    ++count;
                }
            }

            public bool CheckStep(bool check, string description, params object[] args)
            {
                if (check)
                {
                    PassStep(description, args);
                }
                else
                {
                    FailStep(description, args);
                }

                return check;
            }

            public bool MatchStep(object probe, object proof, params string[] text)
            {
                if (text.Length == 0) text = new string[] { "values" };
                if (text.Length == 1) text = new string[] { text[0], "" };
                return CheckStep( probe.Equals( proof ),
                    "comparing {0}...\n expect: {1} {3} \n result: {2} {3} ",
                    text[0], proof.ToString(), probe.ToString(), text[1]
                );
            }

            public void AddStep(Action steption)
            {
                TestRun += steption;
            }

            private void MergeStepsToCase(Action section)
            {
                Delegate[] steps = section.GetInvocationList();
                int pastfails = failures;
                TestRun += () => { CloseCase( (Runner(steps)-pastfails) == 0 ); };
            }



            // flow control
            public bool SkipOnFails
            {
                get { return skipfails >= 0; }
                set
                {
                    if (value != SkipOnFails)
                    {
                        skipfails = value ? 0 : -1;
                    }
                }
            }

            public bool SkipOnError
            {
                get { return skiperror >= 0; }
                set
                {
                    if (value != SkipOnError)
                    {
                        skiperror = value ? 0 : -1;
                    }
                }
            }

            public string CurrentCase
            {
                get { return current; }
            }


            // result details
            public bool hasFailed()
            {
                return failures != 0;
            }

            public bool hasPassed()
            {
                return !(hasFailed() || wasErrors());
            }

            public bool wasErrors()
            {
                return errors.Count > 0;
            }

            public int getFailures()
            {
                return failures;
            }

            public string[] getErrors()
            {
                return errors.ToArray();
            }


            


            // Case handling (each 'Case' consists from maybe several 'Steps')
            public void AddCase( Action firststep )
            {
                MergeStepsToCase( TestRun );
                TestRun += firststep;
            }

            public bool NextCase(string casename)
            {
                if (failures < 0)
                    return false;
                StdStream.Out.Stream.Put("\n------------------ Next Case: ")
                    .Put(casename)
                    .Put(" ------------------\n").End();
                current = casename;
                count = 0;
                ++step;
                if (flags.HasFlag(TestResults.XmlOutput)) {
                    StdStream.Aux.Xml.WriteElement("testcase", new string[]{ $"number={step}", $"name={casename}" });
                } return true;
            }

            public int CloseCase( bool successive )
            {
                int total = count;
                count = 0;

                int gones = failures - failsgone;
                failsgone = failures;
                int returnval = gones;
                if (successive) {
                    if (flags.HasFlag(TestResults.XmlOutput)) 
                        WriteXmlCaseResult( step, current, 0, total, gones );
                    returnval = setPassed( string.Format( "{0}: {1}", current, total ) );
                } else {
                    if (gones == 0) {
                        if (flags.HasFlag(TestResults.XmlOutput))
                            WriteXmlCaseResult( step, current, gones, total, total);
                        setSkipped( string.Format( "{0}: {1}", current, total ) );
                    } else {
                        if (flags.HasFlag(TestResults.XmlOutput))
                            WriteXmlCaseResult( step, current, gones, total, 0 );
                        if (Verbose) {
                            returnval = setFailed( string.Format( "{0}: {1}", current, gones ) );
                        }
                    }
                } 

                return returnval;
            }

            public int CloseCase( string casename )
            {
                if (casename == current) {
                    bool successive = (failures - failsgone) == 0;
                    return CloseCase( successive );
                } else {
                    string actual = current;
                    current = casename;
                    failsgone = 0;
                    failsgone = CloseCase( hasPassed() );
                    current = actual;
                    return failsgone;
                }
            }

            public int SkipCase( string casename )
            {
                string previous = CurrentCase;
                NextCase( casename );
                SkipStep( "depending on {0}", previous );
                return CloseCase( false );
            }

            public Test() : this( true ) { }
            public Test( bool logall ) : this( logall, false ) { }
            public Test( bool logall, bool logxml )
            {
                errors = new List<string>();
                Verbose = logall;
                if ( logxml ) flags |= TestResults.XmlOutput;
                failures = 0;
                count = 0;
                skiperror = 0;
                skipfails = -1;
                current = "";
                TestRun += TestSuite;
            }

            private void Header()
            {
                string testsuite = this.GetType().Name;
                StdStream.Out.WriteLine("\n#####################################################################");
                StdStream.Out.WriteLine("# TEST: {0}", testsuite );
                StdStream.Out.WriteLine("#####################################################################");
                if (flags.HasFlag(TestResults.XmlOutput)) {
                    if( Consola.StdStream.Aux.Xml.existsLog() )
                        Consola.StdStream.Aux.Xml.removeLog();
                    Consola.StdStream.Aux.Xml.createLog();
                    Consola.StdStream.Aux.Xml.WriteElement("testsuite", new string[] { $"name={testsuite}",$"time={DateTime.Now}"});
                }
            }

            private void Footer()
            {
                StdStream.Out.WriteLine("\n=====================================================================");
                if (wasErrors())
                {
                    string[] errs = getErrors();
                    StdStream.Out.Log.WriteLine("\n...FATAL {0} Error happend:", errs.Length);
                    StdStream.Err.WriteLine("\n...FATAL {0} Error happend:", errs.Length);
                    if (flags.HasFlag(TestResults.XmlOutput))
                        StdStream.Aux.Xml.WriteElement("suiteresult", new string[] { "result=NORESULT", $"errors={errs.Length}" });
                    for (int i = 0; i < errs.Length; ++i) { 
                        StdStream.Err.WriteLine("ERROR[{0}]: {1}", i, errs[i]);
                        if (flags.HasFlag(TestResults.XmlOutput))
                            StdStream.Aux.Xml.WriteElement("errorreport", new string[] { $"number={i}" }).WriteContent( errs[i] ).CloseScope();
                    } if (flags.HasFlag(TestResults.XmlOutput))
                        StdStream.Aux.Xml.CloseScope("suiteresult");

                }
                else if (hasPassed())
                {
                    StdStream.Out.WriteLine("\n...All PASSED");
                    if (flags.HasFlag(TestResults.XmlOutput))
                        StdStream.Aux.Xml.WriteElement("suiteresult", new string[] { "result=PASS", $"failures={failures}" })
                                           .CloseScope("suiteresult");
                }
                else if (hasFailed())
                {
                    StdStream.Err.WriteLine("\n...{0} FAILS", failures);
                    StdStream.Out.Log.WriteLine("\n...{0} FAILS", failures);
                    if (flags.HasFlag(TestResults.XmlOutput))
                        StdStream.Aux.Xml.WriteElement("suiteresult", new string[] { "result=FAIL", $"failures={failures}" })
                                           .CloseScope("suiteresult");
                }
                else
                {
                    StdStream.Out.WriteLine("\n...was SKIPPED");
                    if (flags.HasFlag(TestResults.XmlOutput))
                        StdStream.Aux.Xml.WriteElement("suiteresult", new string[] { "result=SKIP", $"failures={failures}" })
                                           .CloseScope("suiteresult");
                }
            }

            private int Runner( Delegate[] steplist )
            {
                foreach (Action testrun in steplist)
                    try
                    {
                        testrun();
                    }
                    catch (Exception exception)
                    {
                        setFatal(string.Format("{0}: {1} {2}", current, errors.Count, exception.Message), SkipOnError);
                        if (flags.HasFlag(TestResults.XmlOutput)) {
                            StdStream.Aux.Xml.WriteElement("errorreport", new string[] { $"testcase={current}", $"number={errors.Count}" })
                                             .WriteContent( exception.Message ).CloseScope();
                        }

                        if (SkipOnError)
                        {
                            ++skiperror;
                            continue;
                        }
                        else break;
                    }
                if (flags.HasFlag(TestResults.XmlOutput)) 
                    StdStream.Aux.Xml.closeLog();
                return failures;
            }

            public Test Run()
            {
                Header();

                foreach (Action testrun in TestRun.GetInvocationList())
                    try
                    {
                        testrun();
                    }
                    catch (Exception exception)
                    {
                        setFatal(string.Format("{0}: {1} {2}", current, errors.Count, exception.Message), true);
                        if (flags.HasFlag(TestResults.XmlOutput)) {
                            StdStream.Aux.Xml.WriteElement("errorreport", new string[] { $"testcase={current}", $"number={errors.Count}" })
                                .WriteContent( exception.Message ).CloseScope( "errorreport" );
                        }
                        if (SkipOnError)
                        {
                            ++skiperror;
                            continue;
                        }
                        else break;
                    }

                Footer();
                if (flags.HasFlag(TestResults.XmlOutput))
                    StdStream.Aux.Xml.closeLog();
                return this;
            }

            virtual protected void TestSuite() {}
        }
    }
}