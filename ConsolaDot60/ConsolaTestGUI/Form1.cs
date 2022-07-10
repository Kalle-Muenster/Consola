using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
 

namespace Consola.TestApp
{
    public partial class Form1 : Form
    {
        private volatile uint running = 0;
        private volatile bool getching = false;
        private volatile string getcha = "";
        private Task inputs;
        private Task[] outputs;
        private Task[] errors;
        private volatile string labeltext = "undefined~undefined";
        private StdStreams std = null;

        private void OutputTask( object ident )
        {
            string taskname = (ident as object[])[0] as string;
            int cycletime = (int)(ident as object[])[1];
            do
            {
                StdStream.Out.WriteLine( "This is task '{0}' writing to stdout: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", taskname );
                Thread.Sleep( cycletime );
            } while ( ( running & 1u ) == 1u );
        }
        private void ErrorTask( object ident )
        {
            string taskname = (ident as object[])[0] as string;
            int cycletime = (int)(ident as object[])[1];
            do
            {
                StdStream.Err.WriteLine( "This is task '{0}' writing to stderr: 111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111", taskname );
                Thread.Sleep( cycletime );
            } while ( ( running & 1u ) == 1u );
        }

        private void OutputStream( object ident )
        {
            string taskname = (ident as object[])[0] as string;
            int cycletime = (int)(ident as object[])[1];
            do
            {
                StdStream.Out.Stream.Put( "This is task '" ).Put( taskname ).Put( "' writing to stdout: " ).Put( "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\n" ).End();
                Thread.Sleep( cycletime );
            } while ( ( running & 2u ) == 2u );
        }
        private void ErrorStream( object ident )
        {
            string taskname = (ident as object[])[0] as string;
            int cycletime = (int)(ident as object[])[1];
            do
            {
                StdStream.Err.Stream.Put( "This is task '" ).Put( taskname ).Put( "' writing to stderr " ).Put( "222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222\n" ).End();
                Thread.Sleep( cycletime );
            } while ( ( running & 2u ) == 2u );
        }

        private void InputTask( object taskObj )
        {
            do
            {
                if ( getcha.Length == 0 )
                {
                    char x = Consola.StdStream.Inp.GetChar();
                    getcha += x;
                    if ( x == '\n' )
                        getching = false;
                    this.Invalidate( true );
                }
                else
                {
                    Thread.Sleep( 2 );
                }
            } while ( getching );
        }

        public Form1(object pinoekel) : this()
        {
            std = pinoekel as StdStreams;
        }
        public Form1()
        {
            InitializeComponent();
            uint ver = Utility.VersionNumber;
            lbl_version.ForeColor = Color.Green;
            lbl_version.Text = "Consola v." + Utility.VersionString;            
        }

        private void button1_Click( object sender, EventArgs e )
        {
            
            StdStream.Init( CreationFlags.TryConsole );
            StdStream.Out.WriteLine( "This is a test!" );
        }

        private void button2_Click( object sender, EventArgs e )
        {
            if ( std == null )
                std = new StdStreams( CreationFlags.UseConsole );
            std.Err.WriteLine( "This is an error!" );
        }

        private void StartConcurrentWriteThreads( object sender, EventArgs e )
        {
            if ( ( running & 1u ) == 0 )
            {
                running |= 1u;
                if ( std == null )
                    std = new StdStreams( CreationFlags.TryConsole );
                if ( outputs == null )
                {
                    outputs = new Task[2];
                    errors = new Task[2];
                }
                outputs[0] = new Task( OutputTask, new object[] { "Writer AAA", 7 } );
                errors[0] = new Task( ErrorTask, new object[] { "Errors 111", 203 } );
                outputs[0].Start();
                errors[0].Start();
            }
            else
            {
                running &= ~1u;
                while ( ( !outputs[0].Status.HasFlag( TaskStatus.RanToCompletion ) )
                    && ( !errors[0].Status.HasFlag( TaskStatus.RanToCompletion ) ) )
                {
                    Thread.Sleep( 5 );
                }
            }
        }

        private void StartConcurrentStreamThreads( object sender, EventArgs e )
        {
            if ( ( running & 2u ) == 0 )
            {
                running |= 2u;
                if ( std == null )
                    std = new Consola.StdStreams( Consola.CreationFlags.TryConsole );
                if ( outputs == null )
                {
                    outputs = new Task[2];
                    errors = new Task[2];
                }
                outputs[1] = new Task( OutputStream, new object[] { "Stream BBB", 23 } );
                errors[1] = new Task( ErrorStream, new object[] { "Inferte 222", 179 } );
                outputs[1].Start();
                errors[1].Start();
            }
            else
            {
                running &= ~2u;
                while ( ( !outputs[1].Status.HasFlag( TaskStatus.RanToCompletion ) )
                    && ( !errors[1].Status.HasFlag( TaskStatus.RanToCompletion ) ) )
                {
                    Thread.Sleep( 5 );
                }
            }
        }

        private void radioButtons_Click( object sender, EventArgs e )
        {
            if ( std == null ) return;
            RadioButton rdbtn = (sender as RadioButton);
            switch ( rdbtn.Text )
            {
                case "Separate":
                    if ( rdbtn.Checked )
                    {
                        if ( std.Out.existsLog() )
                        {
                            if ( std.Out.Log == std.Err.Log )
                            {
                                std.closeLog();
                            }
                        }
                        std.Out.Log = std.Out;
                        std.Err.Log = std.Err;
                    }
                    break;
                case "TwoInOne":
                    if ( rdbtn.Checked )
                    {
                        if ( std.Out.existsLog() )
                        {
                            if ( std.Out.Log != std.Err.Log )
                            {
                                std.closeLog();
                            }
                        }
                        std.Log = std.Out;
                    }
                    break;
                case "Disabled":
                    if ( rdbtn.Checked )
                    {
                        std.Log = null;
                    }
                    break;
                case "Remove":
                    if ( rdbtn.Checked )
                    {
                        std.removeLog();
                    }
                    break;
            }
        }

        private void GetCharOnStdIn( object sender, EventArgs e )
        {
            if ( !getching )
            {
                inputs = new Task( InputTask, inputs );
                button5.Text = "Stop Getching";
                getching = true;
                inputs.Start();
            }
            else
            {
                getching = false;
                button5.Text = "Start Getching";
            }
        }

        private void StartReadingStdIn( object sender, EventArgs e )
        {
            StdStream.Init( CreationFlags.TryConsole );
            string inp = StdStream.Inp.ReadLine();
            InputField.Text += inp;
        }

        private void button7_Click( object sender, EventArgs e )
        {
            InputField.Text = string.Format( "processid: {0}", Utility.CommandLine( InputField.Text, getReturnCode ) );
            Paint += Form1_Paint;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (labeltext.Length > 0) {
                InputField.Text += (" \n"+labeltext);
                labeltext = "";
            } 
        }

        private void getReturnCode(int returncode)
        {
            labeltext = string.Format( "returned: {0}", returncode );
            Invalidate();
        }
    }
}
