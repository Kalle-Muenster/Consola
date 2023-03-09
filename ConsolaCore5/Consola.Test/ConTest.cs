using System;
using System.Collections.Generic;
using System.Reflection;

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
            NONE = 1162760014u, INFO = 1330007625u, TimeStamps = 0x80000000u,
            FATAL = NONE
        }


        abstract public class Test
        {
            internal class CaseContainer
            {
                private static Dictionary<string,CaseContainer> list;
                private Test   test;
                private Action call;
                private string name;
                private bool   skip;

                static CaseContainer()
                {
                    list = new Dictionary<string, CaseContainer>();
                }

                public CaseContainer( Test testrun, Action casefunc, string testcase, bool skiptest )
                {
                    test = testrun;
                    call = casefunc;
                    name = testcase;
                    skip = skiptest;
                    list.Add(name, this);
                }

                public void RunTestCase()
                {
                    if( test.NextCase(name) ) {
                        call(); int failed =
                            test.closeCase( name );
                        if( skip )
                            test.SkipNextTest =
                            failed > 0;
                    }
                    list.Remove(name);
                }
            }

            public class CaseName
                : Tuple<string, uint>
            {
                public string Name {
                    get { return Item1; }
                }
                public uint Number {
                    get { return Item2; }
                }
                public CaseName( string item1, uint item2 )
                    : base(item1, item2)
                { }
            }

            private uint step = 0;
            private int count = -1;
            private int failures = -1;
            private TestResults conf = 0;
            private string current;
            private int skiperror;
            private int skipfails;
            private int failsgone;
            private bool skipnext;
            private int runId = -1;
            private List<string> errors = new List<string>();
            private Action StepsList;
            private readonly string testsuite;
            private int repeats = 0;

            private const uint   Completed = 0x7fffff00u;
            private const string SingleLine = "---------------------------------------------------------------------";
            private const string DoubleLine = "=====================================================================";
            private const string HashLine   = "#####################################################################";
            private const string StepForm = "{0} [{1}.{2}]{4}: {3}";
            private const string STEPINFO = "stepinfo";
            private const string TIMESTAMP = "timestamp";
            private const string TESTCASE   = "testcase";
            private const string CASERESULT = "caseresult";
            private const string TESTSTEP   = "teststep";
            private const string STEPRESULT = "stepresult";
            private const string TESTSUITE  = "testsuite";
            private const string SUITERESULT = "suiteresult";

            protected TestResults flags = 0;
            public event Action   TestRun;

            private static string NextCaseHeader( string nextcase )
            {
                return "\n------------------ Next Case: "
                     + nextcase + " ------------------\n";
            }

            private static void WriteXmlStepEntry( TestResults result, uint casenum, int stepnum, string description )
            {
                string timestomp;
                if( result.HasFlag(TestResults.TimeStamps) ) {
                    DateTime time = DateTime.Now;
                    timestomp = $"{time.Hour,2:D2}:{time.Minute,2:D2}:{time.Second,2:D2}.{time.Millisecond,3:D3}";
                    if( result != TestResults.TimeStamps )
                        result &= ~TestResults.TimeStamps;
                } else {
                    timestomp = string.Empty;
                }

                switch( result ) {
                    case TestResults.INFO: {
                            StdStream.Aux.Xml.WriteElement(STEPINFO,
                                new string[] { $"{TESTSTEP}={casenum}.{stepnum}" }
                            ).WriteContent(description).CloseScope();
                        }
                        break;
                    case TestResults.TimeStamps: {
                            StdStream.Aux.Xml.WriteElement(TIMESTAMP,
                                new string[] { $"{TESTSTEP}={casenum}.{stepnum}" }
                            ).WriteContent(timestomp).CloseScope();
                        }
                        break;
                    case TestResults.FATAL: {
                            string[] attr = timestomp.Length > 0
                                      ? new string[]{ $"number={casenum}.{stepnum}", $"time={timestomp}" }
                                      : new string[]{ $"number={casenum}.{stepnum}" };
                            StdStream.Aux.Xml.WriteElement("error", attr).WriteContent(description)
                                               .CloseScope("error");
                        }
                        break;
                    default: {
                            string[] attr = timestomp.Length > 0
                                      ? new string[]{ $"number={casenum}.{stepnum}", $"time={timestomp}" }
                                      : new string[]{ $"number={casenum}.{stepnum}" };
                            StdStream.Aux.Xml.WriteElement(TESTSTEP, attr);
                            if( description.Length > 0 )
                                StdStream.Aux.Xml.WriteElement(STEPINFO)
                                                 .WriteContent(description);
                            StdStream.Aux.Xml.WriteElement(STEPRESULT)
                                             .WriteContent(result.ToString())
                        .CloseScope(TESTSTEP);
                        }
                        break;
                }
            }

            private static void WriteXmlCaseResult( int failed, int total, int skips )
            {
                TestResults result = failed == 0 ?
                     total > skips ? TestResults.PASS
                                   : TestResults.SKIP
                                   : TestResults.FAIL;

                StdStream.Aux.Xml.WriteElement(CASERESULT, new string[] {
                    $"skipped={skips}", $"passed={total-(skips+failed)}", $"failed={failed}" }
                ).WriteContent(result.ToString()).CloseScope(TESTCASE);
            }

            private static void WriteLogEntry( TestResults result, TestResults flags, uint casenum, int stepnum, string msgtext )
            {
                if( msgtext == null ) msgtext = string.Empty;
                result |= ( flags & TestResults.TimeStamps );

                if( flags.HasFlag(TestResults.XmlOutput) ) {
                    WriteXmlStepEntry(result, casenum, stepnum, msgtext);
                }

                string timestomp = string.Empty;
                if( result.HasFlag(TestResults.TimeStamps) ) {
                    DateTime time = DateTime.Now;
                    timestomp = $"[{time.Hour,2:D2}:{time.Minute,2:D2}:{time.Second,2:D2}.{time.Millisecond,3:D3}]";
                    if( result != TestResults.TimeStamps ) {
                        result &= ~TestResults.TimeStamps;
                    }
                }

                if( result.HasFlag(TestResults.FAIL) || result.HasFlag(TestResults.FATAL) ) {
                    StdStream.Err.WriteLine(StepForm, result, casenum, stepnum, msgtext, timestomp);
                    StdStream.Out.Log.WriteLine(StepForm, result, casenum, stepnum, msgtext, timestomp);
                } else {
                    StdStream.Out.WriteLine(StepForm, result, casenum, stepnum, msgtext, timestomp);
                }
            }

            // internals
            private int setPassed( string description )
            {
                if( failures < 0 ) return failures;
                if( count > 0 ) {
                    WriteLogEntry(TestResults.PASS, flags, step, count, description);
                    return (int)( count - ( failures - failsgone ) );
                } else {
                    StdStream.Out.WriteLine(SingleLine);
                    StdStream.Out.WriteLine("CASE [{0}, {1} Tests PASSED", step, description);
                    return failures - failsgone;
                }
            }

            private int setFailed( string description )
            {
                if( failures < 0 ) return failures;
                if( count > 0 ) {
                    WriteLogEntry(TestResults.FAIL, flags, step, count, description);
                    return ++failures - failsgone;
                } else {
                    description = String.Format("CASE [{0}, {1} Tests FAILED", step, description);
                    StdStream.Err.WriteLine(SingleLine);
                    StdStream.Err.WriteLine(description);
                    StdStream.Out.Log.WriteLine(SingleLine);
                    StdStream.Out.Log.WriteLine(description);
                    return failures - failsgone;
                }
            }

            private void setSkipped( string description )
            {
                if( failures < 0 ) return;
                if( count > 0 ) {
                    WriteLogEntry(TestResults.SKIP, flags, step, count, description);
                } else {
                    StdStream.Out.WriteLine("CASE [{0}, {1} test steps SKIPPED", step, description);
                }
            }

            private int setFatal( string description, bool continueAnyway )
            {
                if( failures > 0 ) {
                    failures = 0;
                }

                if( count > 0 ) {
                    WriteLogEntry(TestResults.FATAL, flags, step, count, description);
                    errors.Add(string.Format("FATAL [{0}.{1}]: {2}", step, count, description));
                } else {
                    description = string.Format("CASE [{0}, {1} test crashed", step, description);
                    StdStream.Err.WriteLine(description);
                    StdStream.Out.Log.WriteLine(description);
                    errors.Add(description);
                }

                if( !continueAnyway ) {
                    throw new Exception("FATAL");
                }
                return --failures;
            }

            private void setChecked( bool check, string description )
            {
                if( failures < 0 ) return;
                ++count;
                if( check ) {
                    setPassed(description);
                } else {
                    setFailed(description);
                }
            }


            /// <summary> result generator: InfoStep( string infomessage )
            /// it doesn't generates any result, but logs an info message </summary>
            /// <param name="message"> message to be logged (resolves contained '{}' format tags) </param>
            /// <param name="fmtargs"> optionally replacement arguments to be resolved in message text </param>
            public void StepInfo( string fmt, params object[] args )
            {
                if( flags.HasFlag(TestResults.Verbose) ) {
                    WriteLogEntry(TestResults.INFO, flags, step, count + 1, string.Format(fmt, args));
                }
            }

            public void StepTime()
            {
                if( flags.HasFlag(TestResults.Verbose) ) {
                    WriteLogEntry(TestResults.TimeStamps, flags, step, count + 1, string.Empty);
                }
            }

            /// <summary> result generator: SkipStep( string reasontext )
            /// doesn't generates a result, but loggs a message which tells about a teststep has been skipped.
            /// </summary><param name="reason"> message text which describes reason why teststep is skipping </param>
            /// <param name="fmtargs"> optional replacement arguments to be resolved within message text </param>
            public void SkipStep( string fmt, params object[] args )
            {
                ++count;
                setSkipped(string.Format(fmt, args));
            }

            /// <summary> result generator: FailStep( string expected )
            /// Generates and logs a FAIL result entry with also a message describing the failed expectation </summary>
            /// <param name="expected"> message text describing the expectation the testrun not accomplished </param>
            /// <param name="fmtargs"> optional replacement arguments to be resolved within the message text </param>
            public void FailStep( string fmt, params object[] args )
            {
                setChecked(false, string.Format(fmt, args));
            }

            /// <summary> result generator: PassStep( string expected )
            /// Generates and logs a PASS result entry with also a message describing a fullfilled expectation </summary>
            /// <param name="expected"> message text describing an expectation which a testrun has accomplished </param>
            /// <param name="fmtargs"> optional replacement arguments to be resolved within the message text </param>
            public void PassStep( string fmt, params object[] args )
            {
                if( Verbose ) {
                    setChecked(true, string.Format(fmt, args));
                } else {
                    ++count;
                }
            }

            /// <summary> result generator: CheckStep( bool checkedfacts, string expactations )
            /// Logs either a PASS or a FAIL result, depending on if given expression evaluates to true or false </summary>
            /// <param name="checkedfacts"> boolean expression which decides about if logged will be PASS or FAIL result </param>
            /// <param name="expectations"> message text describing expected result and how a testrun would accomplish it </param>
            /// <param name="fmtargs"> optional replacement arguments to be resolved within the message text </param>
            /// <returns> boolean value forwarded from evaluating the passed fact - true on PASS or false on FAIL </returns>
            public bool CheckStep( bool check, string description, params object[] args )
            {
                if( check ) {
                    PassStep(description, args);
                } else {
                    FailStep(description, args);
                }
                return check;
            }

            /// <summary> result generator: MatchStep( object proof, object probe, string info)
            /// Compares a given object 'probe' against a predefined expected 'proof' object. 
            /// If the object 'probe' equals the the expected 'proof' object then a PASS result
            /// will be logged. otherwise, if probed object is different than the proof object
            /// then a FAIL result will be logged </summary>
            /// <param name="probe"> object taken for 'probing' AUT generated output data </param>
            /// <param name="proof"> object of expected type whichs state equals expected output </param>
            /// <param name="text"> one or two facts describing Type and expected state of the probe </param>
            /// <returns> true - if logged a PASS result, or false - when it logged a FAIL result </returns>
            public bool MatchStep( object probe, object proof, params string[] text )
            {
                if( text.Length == 0 ) text = new string[] { "values" };
                if( text.Length == 1 ) text = new string[] { text[0], "" };
                return CheckStep(probe.Equals(proof),
                    "comparing {0}...\n expect: {1} {3} \n result: {2} {3} ",
                    text[0], proof.ToString(), probe.ToString(), text[1]
                );
            }


            // flow control
            public bool SkipOnFails {
                get { return skipfails >= 0; }
                set {
                    if( value != SkipOnFails ) {
                        skipfails = value ? 0 : -1;
                        skipnext = false;
                    }
                }
            }

            public bool SkipOnError {
                get { return skiperror >= 0; }
                set {
                    if( value != SkipOnError ) {
                        skiperror = value ? 0 : -1;
                    }
                }
            }

            public bool SkipNextTest {
                get { return SkipOnFails && skipnext; }
                set {
                    if( value ) SkipOnFails = true;
                    skipnext = value;
                }
            }

            public int MaxRepeatCount {
                get { return repeats; }
                set { repeats = value; }
            }

            public CaseName CurrentCase {
                get { return new CaseName(current, step); }
            }

            public int CurrentStep {
                get { return count + 1; }
            }

            public bool Running {
                get { return ( (uint)flags & Completed ) == 0; }
            }


            // result details
            public int RunsExecuted {
                get { return runId; }
            }

            public bool Verbose {
                get { return ( flags & TestResults.Verbose ) > 0; }
                set {
                    if( value ) flags |= TestResults.Verbose;
                    else flags &= ~TestResults.Verbose;
                }
            }

            public bool TimesStamps {
                get { return ( flags & TestResults.TimeStamps ) != 0; }
                set {
                    if( value ) flags |= TestResults.TimeStamps;
                    else flags &= ~TestResults.TimeStamps;
                }
            }
            public TestResults Results {
                get { return flags; }
                set { flags = value; }
            }

            public bool hasFailed()
            {
                return failures != 0;
            }

            public bool hasPassed()
            {
                return !( hasFailed() || hasCrashed() );
            }

            public bool hasCrashed()
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
            // NextCase(name) - begins a new test case section named 'casename'
            // CloseCase(name) - close a test case section and generate a summary
            // SkipCase(name) - doesn't execute but log a case was skiped instead 
            public bool NextCase( string casename )
            {
                if( SkipNextTest ) {
                    SkipNextTest = false;
                    SkipCase(casename);
                    return false;
                } else {
                    if( failures < 0 ) return false;
                    Consola.StdStream.Out.Write(NextCaseHeader(casename));
                    current = casename;
                    count = 0;
                    ++step;
                    if( flags.HasFlag(TestResults.XmlOutput) ) {
                        StdStream.Aux.Xml.WriteElement(TESTCASE,
                            new string[] { $"number={step}", $"name={casename}" }
                        );
                    }
                }
                return true;
            }

            private int closeCase( bool successive )
            {
                bool xml = flags.HasFlag( TestResults.XmlOutput );
                int total = count;
                count = 0;
                int gones = failures - failsgone;
                failsgone = failures;
                int returnval = gones;
                if( successive ) {
                    if( xml ) WriteXmlCaseResult(0, total, gones);
                    returnval = setPassed(string.Format("{0}]: {1}", current, total));
                } else {
                    if( gones == 0 ) {
                        if( xml ) WriteXmlCaseResult(gones, total, total);
                        setSkipped(string.Format("{0}]: {1}", current, total));
                    } else {
                        if( xml ) WriteXmlCaseResult(gones, total, 0);
                        if( Verbose ) {
                            returnval = setFailed(string.Format("{0}]: {1}", current, gones));
                        }
                    }
                }
                return returnval;
            }

            private int closeCase( string casename )
            {
                if( casename == current ) {
                    bool successive = (failures - failsgone) == 0;
                    return closeCase(successive);
                } else {
                    string actual = current;
                    current = casename;
                    failsgone = 0;
                    failsgone = closeCase(hasPassed());
                    current = actual;
                    return failsgone;
                }
            }

            public int CloseCase()
            {
                return closeCase(current);
            }

            protected int SkipCase( string casename )
            {
                string previous = current;
                NextCase(casename);
                SkipStep("depending on {0}", previous);
                return closeCase(false);
            }


            // construction
            public Test() : this(TestResults.Verbose)
            {
            }
            public Test( TestResults flags )
                : this(flags.HasFlag(TestResults.Verbose),
                        flags.HasFlag(TestResults.XmlOutput),
                        flags.HasFlag(TestResults.TimeStamps))
            {
            }
            public Test( bool logall, bool logxml, bool stamps )
            {
                testsuite = this.GetType().Name;
                if( StdStream.CreationFlags == CreationFlags.None ) {
                    StdStream.Init(CreationFlags.TryConsole
                                  | CreationFlags.NoInputLog
                                  | CreationFlags.CreateLog);
                }
                TestResults   config  = TestResults.TextOutput;
                if( logall ) config |= TestResults.Verbose;
                if( logxml ) config |= TestResults.XmlOutput;
                if( stamps ) config |= TestResults.TimeStamps;

                TestRun += OnStartUp;
                Initialize(config);
            }
            public Test( bool logall, bool logxml )
                 : this(logall, logxml, false) { }

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
                    SkipOnError = conf.HasFlag(TestResults.SkipOnError);
                    SkipOnFails = conf.HasFlag(TestResults.SkipOnFails);
                }
            }

            private void NewRun()
            {
                if( ++runId <= repeats ) {
                    if( runId == 0 ) {
                        if( SkipOnError ) conf |= TestResults.SkipOnError;
                        if( SkipOnFails ) conf |= TestResults.SkipOnFails;
                    } else {
                        Initialize(conf);
                    }
                    if( StepsList != null ) {
                        TestRun += StepsList;
                        StepsList = null;
                    }
                    flags |= TestResults.IsRunning;
                }
            }

            public void AddTestStep( Action stepfunction )
            {
                if( StepsList == null ) StepsList = stepfunction;
                else StepsList += stepfunction;
            }

            public void AddSection( string name, Action section )
            {
                TestRun += () => { StepInfo("Test Section: {0}", name); section(); };
            }

            public void AddTestCase( string name, Action function )
            {
                AddTestCase(name, function, SkipOnFails);
            }

            public void AddTestCase( string name, Action function, bool skiponfailues )
            {
                TestRun += new CaseContainer(
                    this, function, name, skiponfailues
                ).RunTestCase;
            }

            private void Header()
            {
                if( !Running ) return;

                StdStream.Out.Write("\n");
                string[] attributes = Array.Empty<string>();

                if( runId == 0 ) {
                    StdStream.Out.WriteLine(HashLine);
                    StdStream.Out.WriteLine("# TESTRUN: {0}", testsuite);
                    StdStream.Out.WriteLine(HashLine);
                    if( flags.HasFlag(TestResults.XmlOutput) ) {
                        if( runId == 0 ) {
                            if( StdStream.Aux.Xml.existsLog() )
                                StdStream.Aux.Xml.removeLog();
                            StdStream.Aux.Xml.createLog();
                        }
                        attributes = new string[]{ $"name={testsuite}",
                            $"time={DateTime.Now}" };
                    }
                } else {
                    StdStream.Out.WriteLine(DoubleLine);
                    StdStream.Out.WriteLine($"= REPEAT-{runId}: {testsuite}");
                    StdStream.Out.WriteLine(DoubleLine);
                    attributes = new string[]{ $"name={testsuite}",
                            $"repeat={runId}", $"time={DateTime.Now}" };
                }
                if( flags.HasFlag(TestResults.XmlOutput) ) {
                    StdStream.Aux.Xml.NewScope(AuxXml.State.Document);
                    StdStream.Aux.Xml.WriteElement(TESTSUITE, attributes);
                }
            }

            private void Footer()
            {
                if( !Running ) return;
                bool xml = flags.HasFlag( TestResults.XmlOutput );
                StdStream.Out.WriteLine("\n{0}", DoubleLine);
                if( hasCrashed() ) {
                    string[] errs = getErrors();
                    StdStream.Out.Log.WriteLine("\n...FATAL {0} Error happend:", errs.Length);
                    StdStream.Err.WriteLine("\n...FATAL {0} Error happend:", errs.Length);
                    if( xml )
                        StdStream.Aux.Xml.WriteElement(SUITERESULT, new string[] { $"errors={errs.Length}", "result=NONE" });
                    for( int i = 0; i < errs.Length; ++i ) {
                        StdStream.Err.WriteLine("ERROR [{0}]: {1}", i, errs[i]);
                        if( xml )
                            StdStream.Aux.Xml.WriteElement("error", new string[] { $"number={i}" }).WriteContent(errs[i]);
                    }
                    if( xml )
                        StdStream.Aux.Xml.CloseScope(SUITERESULT);
                    flags = TestResults.NONE;
                } else if( hasPassed() ) {
                    StdStream.Out.WriteLine("\n   All Tests PASSED\n");
                    if( xml )
                        StdStream.Aux.Xml.WriteElement(SUITERESULT, new string[] { $"errors={errors.Count}", $"failed={failures}" })
                                         .WriteContent("PASS")
                                           .CloseScope(SUITERESULT);
                    flags = TestResults.PASS;
                } else if( hasFailed() ) {
                    StdStream.Err.WriteLine("\nTestrun FAILS total: {0}\n", failures);
                    StdStream.Out.Log.WriteLine("\nTestrun FAILS total: {0}\n", failures);
                    if( xml )
                        StdStream.Aux.Xml.WriteElement(SUITERESULT, new string[] { $"errors={errors.Count}", $"failed={failures}" })
                                         .WriteContent("FAIL")
                                           .CloseScope(SUITERESULT);
                    flags = TestResults.FAIL;
                } else {
                    StdStream.Out.WriteLine("\nWhole testrun was SKIPPED\n");
                    if( xml )
                        StdStream.Aux.Xml.WriteElement(SUITERESULT, new string[] { $"errors={errors.Count}", $"failed={failures}" })
                                         .WriteContent("SKIP")
                                           .CloseScope(SUITERESULT);
                    flags = TestResults.SKIP;
                }

                if( xml ) {
                    StdStream.Aux.Xml.CloseScope(TESTSUITE);
                }
            }

            private void Runner( Delegate[] steplist )
            {
                if( !Running ) return;
                foreach( Action testrun in steplist ) try {
                        testrun();
                    } catch( Exception exception ) {
                        setFatal(string.Format("{0}: {1} {2}", current, errors.Count, exception.Message), SkipOnError);
                        if( flags.HasFlag(TestResults.XmlOutput) ) {
                            StdStream.Aux.Xml.WriteElement("error", new string[] { $"number={errors.Count}", $"testcase={current}" })
                                .WriteContent(exception.Message).CloseScope();
                        }
                        if( SkipOnError ) {
                            ++skiperror;
                            continue;
                        } else break;
                    }
            }

            private void Cleaner()
            {
                if( repeats == runId ) {
                    OnCleanUp();
                }
            }

            /// <summary> Test.Run()
            /// call this for starting a test run (it will return results)
            /// ...call it again for running same test once again </summary>
            /// <returns> Test instance with gained results </returns>
            public Test Run()
            {
                NewRun();

                Header();

                Runner(TestRun.GetInvocationList());

                Footer();

                Cleaner();

                return this;
            }

            /// <summary>
            /// override this for implementing testdata initialization or similar 
            /// </summary>
            virtual protected void OnStartUp() { }

            /// <summary>
            /// override this for implementing any cleanup logic maybe needed
            /// </summary>
            virtual protected void OnCleanUp() { }
        }
    }
}