using System;
using System.Collections.Generic;

namespace Consola
{
    namespace Tests
    {
        abstract public class TestSuite
        {
            private uint step = 0;
            private int count = -1;
            private int failures = -1;
            protected bool verbose = false;
            private List<string> errors;
            private string current;
            private int skiperror;
            private int skipfails;
            private int failsgone;
            public event Action TestRun;


            // internals
            public int setPassed(string description)
            {
                if (failures < 0) return failures;
                if (count > 0)
                {
                    StdStream.Out.WriteLine("PASS [{0}.{1}]: {2}", step, count, description);
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
                if (verbose)
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
                return true;
            }

            public int CloseCase(bool successive)
            {
                int total = count;
                count = 0;

                int gones = failures - failsgone;
                failsgone = failures;
                int returnval = gones;
                if (successive)
                {
                    returnval = setPassed(string.Format("{0}: {1}", current, total));
                }
                else
                {
                    if (gones == 0)
                    {
                        setSkipped(string.Format("{0}: {1}", current, total));
                    }
                    else if (verbose)
                    {
                        returnval = setFailed(string.Format("{0}: {1}", current, gones));
                    }
                } // GC.Collect();

                return returnval;
            }

            public int CloseCase(string casename)
            {
                if (casename == current)
                {
                    bool successive = (failures - failsgone) == 0;
                    return CloseCase(successive);
                }
                else
                {
                    string actual = current;
                    current = casename;
                    failsgone = 0;
                    failsgone = CloseCase(hasPassed());
                    current = actual;
                    return failsgone;
                }
            }

            public int SkipCase(string casename)
            {
                string previous = CurrentCase;
                NextCase(casename);
                SkipStep("depending on {0}", previous);
                return CloseCase(false);
            }

            public TestSuite(bool logall)
            {
                TestRun += Test;
                errors = new List<string>();
                verbose = logall;
                failures = 0;
                count = 0;
                skiperror = 0;
                skipfails = -1;
                current = "";
            }

            private void Header()
            {
                StdStream.Out.WriteLine("\n#####################################################################");
                StdStream.Out.WriteLine("# TEST: {0}", this.GetType().Name);
                StdStream.Out.WriteLine("#####################################################################");
            }

            private void Footer()
            {
                StdStream.Out.WriteLine("\n=====================================================================");
                if (wasErrors())
                {
                    string[] errs = getErrors();
                    StdStream.Out.Log.WriteLine("\n...FATAL {0} Error happend:", errs.Length);
                    StdStream.Err.WriteLine("\n...FATAL {0} Error happend:", errs.Length);
                    for (int i = 0; i < errs.Length; ++i)
                        StdStream.Err.WriteLine("ERROR[{0}]: {1}", i, errs[i]);
                }
                else if (hasPassed())
                {
                    StdStream.Out.WriteLine("\n...All PASSED");
                }
                else if (hasFailed())
                {
                    StdStream.Err.WriteLine("\n...{0} FAILS", failures);
                    StdStream.Out.Log.WriteLine("\n...{0} FAILS", failures);
                }
                else
                {
                    StdStream.Out.WriteLine("\n...was SKIPPED");
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
                        if (SkipOnError)
                        {
                            ++skiperror;
                            continue;
                        }
                        else break;
                    }

                return failures;
            }

            public TestSuite Run()
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
                        if (SkipOnError)
                        {
                            ++skiperror;
                            continue;
                        }
                        else break;
                    }

                Footer();
                
                return this;
            }

            virtual protected void Test() {}
        }
    }
}