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

        public class CaseContainer
        {
            private static Dictionary<string,CaseContainer> caselist;
            private Test   test;
            private Action call;
            private string name;
            private bool   skip;

            static CaseContainer()
            {
                caselist = new Dictionary<string,CaseContainer>();
            }

            public CaseContainer( Test testrun, Action casefunc, string testcase, bool skiptest )
            {
                test = testrun;
                call = casefunc;
                name = testcase;
                skip = skiptest;
                caselist.Add(name,this);
            }

            public void RunTestCase()
            {
                if ( test.NextCase( name ) ) {
                    call(); int failed =
                    test.CloseCase( name );
                    if( skip ) 
                        test.SkipNextTest = failed > 0;
                } caselist.Remove( name );
            }
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
            private bool skipnext;
            private Action StepsList;
            public event Action TestRun;

            private const string DashLine = "---------------------------------------------------------------------";
            private string NextCaseHeader( string nextcase )
            {
                return "\n------------------ Next Case: " 
                     + nextcase + " ------------------\n";
            }

            private void WriteXmlStepResult( uint casenum, int stepnum, TestResults result, string description )
            {
                StdStream.Aux.Xml.WriteElement( "teststep", 
                    new string[] { $"result={result}", $"number={step}.{count}" }
                );
                if( description.Length > 0 ) {
                    StdStream.Aux.Xml.WriteElement("description")
                                     .WriteContent( description )
                                     .CloseScope( "teststep" );
                } else
                StdStream.Aux.Xml.CloseScope();
            }

            private void WriteXmlStepInfo( uint casenum, int stepnum, string description )
            {
                StdStream.Aux.Xml.WriteElement( "description", new string[] {
                    "level=INFO", $"related={StdStream.Aux.Xml.Element}-{casenum}.{stepnum}" }
                ).WriteContent( description ).CloseScope();
            }

            private void WriteXmlCaseResult( uint casenum, string casename, int failed, int total, int skips )
            {
                TestResults result = failed == 0 ?
                     total > skips ? TestResults.PASS 
                                   : TestResults.SKIP
                                   : TestResults.FAIL;

                StdStream.Aux.Xml.WriteElement( "caseresult", new string[] {
                    $"result={result}", $"number={casenum}", $"name={casename}", $"failed={failed}",
                    $"passed={total-(skips+failed)}", $"skipped={skips}" }
                ).CloseScope( "testcase" );
            }

            //private string CheckedStringFormat( string fmt, params object[] args )
            //{
            //    if( args == null ) return fmt;
            //    char[] fmtarg = new char[3] { '{','_','}' };
            //    for( int i= 0; i < args.Length; ++i ) {
            //        fmtarg[i] = (char)(i+48);
            //        if( !fmt.Contains( new string(fmtarg) )) {
            //            fmt += (" " + fmtarg);
            //        }
            //    } return string.Format( fmt, args );
            //}

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
            public int setPassed( string description )
            {
                if (failures < 0) return failures;
                if (count > 0) {
                    if( flags.HasFlag( TestResults.XmlOutput ) ) 
                        WriteXmlStepResult( step, count, TestResults.PASS, description );
                    StdStream.Out.WriteLine( "PASS [{0}.{1}]: {2}", step, count, description );
                    return (int) ( count - (failures - failsgone) );
                } else {
                    StdStream.Out.WriteLine( DashLine );
                    StdStream.Out.WriteLine( "CASE [{0}, {1} Tests PASSED", step, description );
                    return failures - failsgone;
                }
            }

            public int setFailed(string description)
            {
                if (failures < 0) return failures;
                if (count > 0) {
                    if( flags.HasFlag(TestResults.XmlOutput) )
                        WriteXmlStepResult( step, count, TestResults.FAIL, description );
                    StdStream.Err.WriteLine( "FAIL [{0}.{1}]: {2}", step, count, description );
                    StdStream.Out.Log.WriteLine( "FAIL [{0}.{1}]: {2}", step, count, description );
                    return ++failures - failsgone;
                } else {
                    StdStream.Err.WriteLine( DashLine );
                    StdStream.Err.WriteLine( "CASE [{0}, {1} Tests FAILED", step, description );
                    StdStream.Out.Log.WriteLine( DashLine );
                    StdStream.Out.Log.WriteLine( "CASE [{0}, {1} Tests FAILED", step, description );
                    return failures - failsgone;
                }
            }

            public void setSkipped(string description)
            {
                if (failures < 0) return;
                if (count > 0) {
                    if( flags.HasFlag(TestResults.XmlOutput) )
                        WriteXmlStepResult( step, count, TestResults.SKIP, description );
                    StdStream.Out.WriteLine( "SKIP [{0}.{1}]: {2}", step, count, description );
                } else {
                    StdStream.Out.WriteLine( "CASE [{0}, {1} test steps SKIPPED", step, description );
                }
            }

            public int setFatal( string description, bool continueAnyway )
            {
                if( failures > 0 ) {
                    failures = 0;
                }

                if( count > 0 ) {
                    if( flags.HasFlag(TestResults.XmlOutput) )
                        StdStream.Aux.Xml.WriteElement( "erroreport", new string[] { $"number={step}.{count}" } )
                                         .WriteContent( description )
                                           .CloseScope( "erroreport" );
                    description = string.Format( "FATAL [{0}.{1}]: {2}", step, count, description );
                } else {
                    description = string.Format( "CASE [{0}, {1} test crashed", step, description );
                }

                StdStream.Err.WriteLine( description );
                StdStream.Out.Log.WriteLine( description );
                errors.Add( description );

                if (!continueAnyway) {
                    throw new Exception("FATAL");
                }
                return --failures;
            }

            public void setChecked( bool check, string description )
            {
                if (failures < 0) return;
                ++count;
                if (check) {
                    setPassed( description );
                } else {
                    setFailed( description );
                }
            }




            // result generators:
            // InfoStep(info) - doesn't generates result, but can be used logging meta info
            // SkipStep(info) - doesn't generates result, but loggs entry 'step' was skipped
            // PassStep(info) - generates and logs a PASS result entry plus regarding info
            // FailStep(info) - generates and logs a FAIL result entry plus regarding info
            // CheckStep(bool,info) logs either PASS or FAIL depending on expression true or false 
            // MatchStep(proof,probe,info) compares 'probe' against 'proof'. logs PASS if probe equals proof 

            public void InfoStep( string fmt, params object[] args )
            {
                if( flags > TestResults.TextOutput ) {
                    string description = string.Format( fmt, args );
                    StdStream.Out.WriteLine( "INFO [{0}.{1}]: {2}", step, count, description );
                    if( flags.HasFlag( TestResults.XmlOutput ) )
                        WriteXmlStepInfo( step, count, description );
                }
            }

            public void SkipStep( string fmt, params object[] args )
            {
                ++count;
                setSkipped( string.Format( fmt, args ) );
            }

            public void FailStep( string fmt, params object[] args )
            {
                setChecked( false, string.Format( fmt, args ) );
            }

            public void PassStep( string fmt, params object[] args )
            {
                if (Verbose) {
                    setChecked( true, string.Format( fmt, args ) );
                } else {
                    ++count;
                }
            }

            public bool CheckStep( bool check, string description, params object[] args )
            {
                if ( check ) {
                    PassStep( description, args );
                } else {
                    FailStep( description, args );
                } return check;
            }

            public bool MatchStep( object probe, object proof, params string[] text )
            {
                if (text.Length == 0) text = new string[] { "values" };
                if (text.Length == 1) text = new string[] { text[0], "" };
                return CheckStep( probe.Equals( proof ),
                    "comparing {0}...\n expect: {1} {3} \n result: {2} {3} ",
                    text[0], proof.ToString(), probe.ToString(), text[1]
                );
            }


            // flow control
            public bool SkipOnFails
            {
                get { return skipfails >= 0; }
                set { if (value != SkipOnFails) {
                        skipfails = value ? 0 : -1;
                        skipnext = false;
                    }
                }
            }

            public bool SkipOnError
            {
                get { return skiperror >= 0; }
                set { if (value != SkipOnError) {
                        skiperror = value ? 0 : -1;
                    }
                }
            }

            public bool SkipNextTest {
                get { return SkipOnFails && skipnext; }
                set { if( value ) SkipOnFails = true;
                      skipnext = value; }
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



            // Case handling:
            // NextCase(name) - begins a new test case section named 'name'
            // CloseCase(name) - closes a test cases and generates summary
            // SkipCase(name) - doesn't execute but loggs test case was skiped 
            public bool NextCase( string casename )
            {
                if( SkipNextTest ) {
                    SkipNextTest = false;
                    SkipCase( casename );
                    return false;
                } else {
                    if (failures < 0) return false;
                    Consola.StdStream.Out.Write( NextCaseHeader( casename ) );
                    current = casename;
                    count = 0;
                    ++step;
                    if ( flags.HasFlag( TestResults.XmlOutput ) ) {
                        StdStream.Aux.Xml.WriteElement( "testcase",
                            new string[]{ $"number={step}", $"name={casename}" }
                        );
                    }
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
                    returnval = setPassed( string.Format( "{0}]: {1}", current, total ) );
                } else {
                    if (gones == 0) {
                        if (flags.HasFlag(TestResults.XmlOutput))
                            WriteXmlCaseResult( step, current, gones, total, total);
                        setSkipped( string.Format( "{0}]: {1}", current, total ) );
                    } else {
                        if (flags.HasFlag(TestResults.XmlOutput))
                            WriteXmlCaseResult( step, current, gones, total, 0 );
                        if (Verbose) {
                            returnval = setFailed( string.Format( "{0}]: {1}", current, gones ) );
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

            // configuration
            public Test() : this( true ) { }
            public Test( bool logall ) : this( logall, false ) { }
            public Test( bool logall, bool logxml )
            {
                skipnext = false;
                errors = new List<string>();
                Verbose = logall;
                if ( logxml ) flags |= TestResults.XmlOutput;
                failures = 0;
                count = 0;
                skiperror = 0;
                skipfails = -1;
                current = string.Empty;
                TestRun += TestSuite;
            }

            public void AddTestStep( Action stepfunction )
            {
                if( StepsList == null ) StepsList = stepfunction;
                else StepsList += stepfunction;
            }

            public void AddSection( string name, Action section )
            {
                TestRun += () => { InfoStep( "Test Section: {0}", name ); section(); };
            }

            public void AddTestCase( string name, Action function )
            {
                AddTestCase( name, function, SkipOnFails );
            }
            public void AddTestCase( string name, Action function, bool skiponfailues )
            {
                TestRun += new CaseContainer( 
                    this, function, name, skiponfailues
                ).RunTestCase;
            }

            private Action MergeStepsToTestCase( Action steps, string name )
            {
                Delegate[] steplist = steps.GetInvocationList();
                return () => { NextCase( name );
                    int pastfails = failures;
                    CloseCase( ( Runner( steplist ) - pastfails ) == 0 );
                };
            }

            public void MergeQueue( string asNewTestCase )
            {
                if( StepsList == null ) throw new Exception("step list empty");
                TestRun += MergeStepsToTestCase( StepsList, asNewTestCase );
                StepsList = null;
            }

            private void Header()
            {
                if( StepsList != null ) TestRun += StepsList;
                string testsuite = this.GetType().Name;
                StdStream.Out.WriteLine("\n#####################################################################");
                StdStream.Out.WriteLine("# TEST: {0}", testsuite );
                StdStream.Out.WriteLine("#####################################################################");
                if( flags.HasFlag( TestResults.XmlOutput ) ) {
                    if( StdStream.Aux.Xml.existsLog() )
                        StdStream.Aux.Xml.removeLog();
                    StdStream.Aux.Xml.createLog();
                    StdStream.Aux.Xml.NewScope( AuxXml.State.Document );
                    StdStream.Aux.Xml.WriteElement( "testsuite", new string[] { $"name={testsuite}",$"time={DateTime.Now}"});
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
                        StdStream.Err.WriteLine("ERROR [{0}]: {1}", i, errs[i]);
                        if (flags.HasFlag(TestResults.XmlOutput))
                            StdStream.Aux.Xml.WriteElement("errorreport", new string[] { $"number={i}" }).WriteContent( errs[i] ).CloseScope();
                    } if (flags.HasFlag(TestResults.XmlOutput))
                        StdStream.Aux.Xml.CloseScope("suiteresult");

                }
                else if (hasPassed())
                {
                    StdStream.Out.WriteLine("\n   All Tests PASSED\n");
                    if (flags.HasFlag(TestResults.XmlOutput))
                        StdStream.Aux.Xml.WriteElement("suiteresult", new string[] { "result=PASS", $"failures={failures}" })
                                           .CloseScope("suiteresult");
                }
                else if (hasFailed())
                {
                    StdStream.Err.WriteLine("\nTestrun FAILS total: {0}\n", failures);
                    StdStream.Out.Log.WriteLine("\nTestrun total FAILS total: {0}\n", failures);
                    if (flags.HasFlag(TestResults.XmlOutput))
                        StdStream.Aux.Xml.WriteElement("suiteresult", new string[] { "result=FAIL", $"failures={failures}" })
                                           .CloseScope("suiteresult");
                }
                else
                {
                    StdStream.Out.WriteLine("\nWhole testrun was SKIPPED\n");
                    if (flags.HasFlag(TestResults.XmlOutput))
                        StdStream.Aux.Xml.WriteElement("suiteresult", new string[] { "result=SKIP", $"failures={failures}" })
                                           .CloseScope("suiteresult");
                }

                if ( flags.HasFlag( TestResults.XmlOutput ) ) {
                    StdStream.Aux.Xml.closeLog();
                }
            }

            private int Runner( Delegate[] steplist )
            {
                foreach( Action testrun in steplist ) try {
                    testrun();
                } catch ( Exception exception ) {
                    setFatal( string.Format("{0}: {1} {2}", current, errors.Count, exception.Message), SkipOnError );
                    if ( flags.HasFlag(TestResults.XmlOutput) ) {
                        StdStream.Aux.Xml.WriteElement( "errorreport", new string[] { $"testcase={current}", $"number={errors.Count}" } )
                            .WriteContent( exception.Message ).CloseScope();
                    } if ( SkipOnError ) {
                        ++skiperror;
                        continue;
                    } else break;
                } return failures;
            }

            // call this for starting the test run (returns results)
            public Test Run()
            {
                Header();

                Runner( TestRun.GetInvocationList() );
                
                Footer();

                return this;
            }

            virtual protected void TestSuite() {}
        }
    }
}