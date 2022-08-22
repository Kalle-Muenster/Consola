using System;
using System.Collections.Generic;

namespace Consola
{
    namespace Test
    {
        [Flags]
        public enum TestResults : uint
        {
            TextOutput = 0, Verbose = 0x00000001u, XmlOutput= 0x00000002u,
            SkipOnError = 4u, SkipOnFails = 8u, IsRunning = 0x00000010u,
            PASS = 1397965136u, FAIL = 1279869254u, SKIP = 1346980691u,
            NONE = 1162760014u
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
                caselist.Add( name, this );
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
            private const uint Completed = 0xffffff00u;
            private uint step = 0;
            private int count = -1;
            private int failures = -1;
            protected TestResults flags = 0;
            private TestResults conf = 0; 
            private string current;
            private int skiperror;
            private int skipfails;
            private int failsgone;
            private bool skipnext;
            private int runId = -1;
            private List<string> errors = new List<string>();
            private Action StepsList;
            public event Action TestRun;
            private readonly string testsuite;
            private int repeats = 0;

            private const string SingleLine = "---------------------------------------------------------------------";
            private const string DoubleLine = "=====================================================================";
            private const string HashLine   = "#####################################################################";
            private const string TESTCASE   = "testcase";
            private const string CASERESULT = "caseresult";
            private const string TESTSTEP   = "teststep";
            private const string DESCRIPTION = "description";
            private const string TESTSUITE  = "testsuite";
            private const string SUITERESULT = "suiteresult";

            private static string NextCaseHeader( string nextcase )
            {
                return "\n------------------ Next Case: " 
                     + nextcase + " ------------------\n";
            }

            private static void WriteXmlStepResult( uint casenum, int stepnum, TestResults result, string description )
            {
                StdStream.Aux.Xml.WriteElement( TESTSTEP, 
                    new string[] { $"number={casenum}.{stepnum}", $"result={result}" }
                );
                if( description.Length > 0 ) {
                    StdStream.Aux.Xml.WriteElement( DESCRIPTION )
                                     .WriteContent( description )
                                     .CloseScope( TESTSTEP );
                } else
                StdStream.Aux.Xml.CloseScope();
            }

            private static void WriteXmlStepInfo( uint casenum, int stepnum, string description )
            {
                StdStream.Aux.Xml.WriteElement( DESCRIPTION, new string[] {
                    "level=INFO", $"relate={StdStream.Aux.Xml.Element}-{casenum}.{stepnum}" }
                ).WriteContent( description ).CloseScope();
            }

            private static void WriteXmlCaseResult( uint casenum, string casename, int failed, int total, int skips )
            {
                TestResults result = failed == 0 ?
                     total > skips ? TestResults.PASS 
                                   : TestResults.SKIP
                                   : TestResults.FAIL;

                StdStream.Aux.Xml.WriteElement( CASERESULT, new string[] {
                    $"number={casenum}", $"name={casename}", $"result={result}",
                    $"failed={failed}", $"passed={total-(skips+failed)}",
                    $"skipped={skips}" }
                ).CloseScope( TESTCASE );
            }

            public bool Verbose
            {
                get { return (flags & TestResults.Verbose) > 0; }
                set { if (value) flags |= TestResults.Verbose; 
                            else flags &= ~TestResults.Verbose; }
            }

            public TestResults Results
            {
                get { return flags; }
                set { flags = value; }
            }

            public bool Running
            {
                get { return ( (uint)flags & Completed ) == 0; }
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
                    StdStream.Out.WriteLine( SingleLine );
                    StdStream.Out.WriteLine( "CASE [{0}, {1} Tests PASSED", step, description );
                    return failures - failsgone;
                }
            }

            public int setFailed(string description)
            {
                if (failures < 0) return failures;
                if (count > 0) {
                    if( flags.HasFlag( TestResults.XmlOutput ) )
                        WriteXmlStepResult( step, count, TestResults.FAIL, description );
                    StdStream.Err.WriteLine( "FAIL [{0}.{1}]: {2}", step, count, description );
                    StdStream.Out.Log.WriteLine( "FAIL [{0}.{1}]: {2}", step, count, description );
                    return ++failures - failsgone;
                } else {
                    StdStream.Err.WriteLine( SingleLine );
                    StdStream.Err.WriteLine( "CASE [{0}, {1} Tests FAILED", step, description );
                    StdStream.Out.Log.WriteLine( SingleLine );
                    StdStream.Out.Log.WriteLine( "CASE [{0}, {1} Tests FAILED", step, description );
                    return failures - failsgone;
                }
            }

            public void setSkipped(string description)
            {
                if (failures < 0) return;
                if (count > 0) {
                    if( flags.HasFlag( TestResults.XmlOutput ) )
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
                    if( flags.HasFlag( TestResults.XmlOutput ) )
                        StdStream.Aux.Xml.WriteElement( "error", new string[] { $"number={step}.{count}" } )
                                         .WriteContent( description )
                                           .CloseScope( "error" );
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
                    StdStream.Out.WriteLine( "INFO [{0}.{1}]: {2}", step, count+1, description );
                    if( flags.HasFlag( TestResults.XmlOutput ) )
                        WriteXmlStepInfo( step, count+1, description );
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

            public int MaxRepeatCount {
                get { return repeats; }
                set { repeats = value; }
            }

            public string CurrentCase
            {
                get { return current; }
            }

            public int RunsExecuted
            {
                get { return runId; }
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
            // CloseCase(name) - closes a test cases and generate a summary
            // SkipCase(name) - doesn't execute but log a case was skiped 
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
                        StdStream.Aux.Xml.WriteElement( TESTCASE,
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
                    if (flags.HasFlag( TestResults.XmlOutput )) 
                        WriteXmlCaseResult( step, current, 0, total, gones );
                    returnval = setPassed( string.Format( "{0}]: {1}", current, total ) );
                } else {
                    if (gones == 0) {
                        if (flags.HasFlag( TestResults.XmlOutput ))
                            WriteXmlCaseResult( step, current, gones, total, total);
                        setSkipped( string.Format( "{0}]: {1}", current, total ) );
                    } else {
                        if (flags.HasFlag( TestResults.XmlOutput ))
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
            public Test() : this( TestResults.Verbose ) {
            }
            public Test( TestResults flags ) 
                : this( flags.HasFlag( TestResults.Verbose ),
                        flags.HasFlag( TestResults.XmlOutput ) ) {
            }
            public Test( bool logall, bool logxml )
            {
                testsuite = this.GetType().Name;
                if( StdStream.CreationFlags == CreationFlags.None ) {
                    StdStream.Init( CreationFlags.TryConsole
                                  | CreationFlags.NoInputLog
                                  | CreationFlags.CreateLog );
                }
                TestResults   config  = TestResults.TextOutput;
                if ( logall ) config |= TestResults.Verbose;
                if ( logxml ) config |= TestResults.XmlOutput;

                TestRun += TestSuite;
                Initialize( config );
            }

            private void Initialize( TestResults config )
            {
                skipnext = false;
                errors.Clear();
                conf = config;
                flags = conf & ~( TestResults.SkipOnError
                                | TestResults.SkipOnFails );
                failures = 0;
                count = 0;
                skiperror = 0;
                skipfails = -1;
                current = string.Empty;
                if( runId > 0 ) {
                    SkipOnError = conf.HasFlag( TestResults.SkipOnError );
                    SkipOnFails = conf.HasFlag( TestResults.SkipOnFails );
                }
            } 

            private void NewRun()
            {
                if( ++runId == 0 ) {
                    if( SkipOnError ) conf |= TestResults.SkipOnError;
                    if( SkipOnFails ) conf |= TestResults.SkipOnFails;
                } else
                    Initialize( conf ); 
                if( StepsList != null ) {
                    TestRun += StepsList;
                    StepsList = null;
                } flags |= TestResults.IsRunning;
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

            private void Header()
            {
                StdStream.Out.Write( "\n" );
                StdStream.Out.WriteLine( HashLine );
                StdStream.Out.WriteLine( "# TEST: {0}", testsuite );
                StdStream.Out.WriteLine( HashLine );
                if( flags.HasFlag( TestResults.XmlOutput ) ) {
                    if( runId == 0 ) {
                        if( StdStream.Aux.Xml.existsLog() )
                            StdStream.Aux.Xml.removeLog();
                        StdStream.Aux.Xml.createLog();
                    }
                    StdStream.Aux.Xml.NewScope( AuxXml.State.Document );
                    StdStream.Aux.Xml.WriteElement( TESTSUITE, new string[] { $"name={testsuite}",$"time={DateTime.Now}"});
                }
            }

            private void Footer()
            {
                bool xml = flags.HasFlag( TestResults.XmlOutput );
                StdStream.Out.WriteLine( "\n{0}", DoubleLine );
                if (wasErrors())
                {
                    string[] errs = getErrors();
                    StdStream.Out.Log.WriteLine("\n...FATAL {0} Error happend:", errs.Length);
                    StdStream.Err.WriteLine("\n...FATAL {0} Error happend:", errs.Length);
                    if (xml)
                        StdStream.Aux.Xml.WriteElement( SUITERESULT, new string[] { $"errors={errs.Length}", "result=NONE", $"failed=-1" });
                    for (int i = 0; i < errs.Length; ++i) { 
                        StdStream.Err.WriteLine("ERROR [{0}]: {1}", i, errs[i]);
                        if (xml)
                            StdStream.Aux.Xml.WriteElement( "error", new string[] { $"number={i}" } ).WriteContent( errs[i] ).CloseScope();
                    } if (xml)
                        StdStream.Aux.Xml.CloseScope( SUITERESULT );
                    flags = TestResults.NONE;
                }
                else if (hasPassed())
                {
                    StdStream.Out.WriteLine("\n   All Tests PASSED\n");
                    if (xml)
                        StdStream.Aux.Xml.WriteElement( SUITERESULT, new string[] { $"errors={errors.Count}", "result=PASS", $"failures={failures}" })
                                         .WriteContent("PASS")
                                           .CloseScope( SUITERESULT );
                    flags = TestResults.PASS;
                }
                else if (hasFailed())
                {
                    StdStream.Err.WriteLine("\nTestrun FAILS total: {0}\n", failures);
                    StdStream.Out.Log.WriteLine("\nTestrun total FAILS total: {0}\n", failures);
                    if (xml)
                        StdStream.Aux.Xml.WriteElement( SUITERESULT, new string[] { $"errors={errors.Count}", "result=FAIL", $"failures={failures}" })
                                         .WriteContent("FAIL") 
                                           .CloseScope( SUITERESULT );
                    flags = TestResults.FAIL;
                }
                else
                {
                    StdStream.Out.WriteLine("\nWhole testrun was SKIPPED\n");
                    if (xml)
                        StdStream.Aux.Xml.WriteElement( SUITERESULT, new string[] { $"errors={errors.Count}", "result=NONE", $"failures={failures}" })
                                         .WriteContent("SKIP")
                                           .CloseScope( SUITERESULT );
                    flags = TestResults.SKIP;
                }

                if ( xml ) {
                    StdStream.Aux.Xml.CloseScope( TESTSUITE );
                }
            }

            private int Runner( Delegate[] steplist )
            {
                foreach( Action testrun in steplist ) try {
                    testrun();
                } catch ( Exception exception ) {
                    setFatal( string.Format("{0}: {1} {2}", current, errors.Count, exception.Message), SkipOnError );
                    if ( flags.HasFlag( TestResults.XmlOutput ) ) {
                        StdStream.Aux.Xml.WriteElement( "error", new string[] { $"number={errors.Count}", $"testcase={current}" } )
                            .WriteContent( exception.Message ).CloseScope();
                    } if ( SkipOnError ) {
                        ++skiperror;
                        continue;
                    } else break;
                } return failures;
            }

            private void Cleaner()
            {
                if (repeats == runId ) {
                    OnCleanUp();
                }
            }

            // call this for starting a test run (returns results)
            public Test Run()
            {
                NewRun();

                Header();

                Runner( TestRun.GetInvocationList() );
                
                Footer();

                Cleaner();

                return this;
            }

            // overide this for implementing simple testrun function 
            virtual protected void TestSuite() {}

            // overide this for implementing any cleanup logic maybe needed
            virtual protected void OnCleanUp() {}
        }
    }
}