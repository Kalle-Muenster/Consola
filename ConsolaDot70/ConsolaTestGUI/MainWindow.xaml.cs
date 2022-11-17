using Consola;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ConsolaTestGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            uint ver = Utility.VersionNumber;
            lbl_Version.Foreground = new SolidColorBrush( Colors.Green );
            lbl_Version.Content = "Consola v." + Utility.VersionString;
        }
        public MainWindow( object pinoekel ) : this()
        {
            std = pinoekel as StdStreams;
        }

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
            do {
                StdStream.Out.WriteLine("This is task '{0}' writing to stdout: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", taskname);
                Thread.Sleep(cycletime);
            } while( ( running & 1u ) == 1u );
        }
        private void ErrorTask( object ident )
        {
            string taskname = (ident as object[])[0] as string;
            int cycletime = (int)(ident as object[])[1];
            do {
                StdStream.Err.WriteLine("This is task '{0}' writing to stderr: 111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111", taskname);
                Thread.Sleep(cycletime);
            } while( ( running & 1u ) == 1u );
        }

        private void OutputStream( object ident )
        {
            string taskname = (ident as object[])[0] as string;
            int cycletime = (int)(ident as object[])[1];
            do {
                StdStream.Out.Stream.Put("This is task '").Put(taskname).Put("' writing to stdout: ").Put("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\n").End();
                Thread.Sleep(cycletime);
            } while( ( running & 2u ) == 2u );
        }
        private void ErrorStream( object ident )
        {
            string taskname = (ident as object[])[0] as string;
            int cycletime = (int)(ident as object[])[1];
            do {
                StdStream.Err.Stream.Put("This is task '").Put(taskname).Put("' writing to stderr ").Put("222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222\n").End();
                Thread.Sleep(cycletime);
            } while( ( running & 2u ) == 2u );
        }

        private void InputTask( object taskObj )
        {
            do {
                if( getcha.Length == 0 ) {
                    char x = Consola.StdStream.Inp.GetChar();
                    getcha += x;
                    if( x == '\n' )
                        getching = false;
                    UpdateLayout();
                } else {
                    Thread.Sleep(2);
                }
            } while( getching );
        }




        private void btn_Test_Click( object sender, RoutedEventArgs e )
        {
            StdStream.Init( CreationFlags.TryConsole );
            StdStream.Out.WriteLine("This is a test!");
        }

        private void btn_Error_Click( object sender, RoutedEventArgs e )
        {
            if( std == null )
                std = new StdStreams( CreationFlags.UseConsole );
            std.Err.WriteLine("This is an error!");
        }

        private void btn_WriteThreads_Click( object sender, RoutedEventArgs e )
        {
            if( ( running & 1u ) == 0 ) {
                running |= 1u;
                if( std == null )
                    std = new StdStreams(CreationFlags.TryConsole);
                if( outputs == null ) {
                    outputs = new Task[2];
                    errors = new Task[2];
                }
                outputs[0] = new Task(OutputTask, new object[] { "Writer AAA", 7 });
                errors[0] = new Task(ErrorTask, new object[] { "Errors 111", 203 });
                outputs[0].Start();
                errors[0].Start();
            } else {
                running &= ~1u;
                while( ( !outputs[0].Status.HasFlag(TaskStatus.RanToCompletion) )
                    && ( !errors[0].Status.HasFlag(TaskStatus.RanToCompletion) ) ) {
                    Thread.Sleep(5);
                }
            }
        }

        private void btn_StreamThreads_Click( object sender, RoutedEventArgs e )
        {
            if( ( running & 2u ) == 0 ) {
                running |= 2u;
                if( std == null )
                    std = new Consola.StdStreams(Consola.CreationFlags.TryConsole);
                if( outputs == null ) {
                    outputs = new Task[2];
                    errors = new Task[2];
                }
                outputs[1] = new Task(OutputStream, new object[] { "Stream BBB", 23 });
                errors[1] = new Task(ErrorStream, new object[] { "Inferte 222", 179 });
                outputs[1].Start();
                errors[1].Start();
            } else {
                running &= ~2u;
                while( ( !outputs[1].Status.HasFlag(TaskStatus.RanToCompletion) )
                    && ( !errors[1].Status.HasFlag(TaskStatus.RanToCompletion) ) ) {
                    Thread.Sleep(5);
                }
            }
        }

        private void radioButtons_Click( object sender, RoutedEventArgs e )
        {
            if( std == null ) return;
            RadioButton rdbtn = (sender as RadioButton);
            switch( rdbtn.Content.ToString() ) {
                case "Separate":
                if( rdbtn.IsChecked.Value ) {
                    if( std.Out.existsLog() ) {
                        if( std.Out.Log == std.Err.Log ) {
                            std.closeLog();
                        }
                    }
                    std.Out.Log = std.Out;
                    std.Err.Log = std.Err;
                }
                break;
                case "TwoInOne":
                if( rdbtn.IsChecked.Value ) {
                    if( std.Out.existsLog() ) {
                        if( std.Out.Log != std.Err.Log ) {
                            std.closeLog();
                        }
                    }
                    std.Log = std.Out;
                }
                break;
                case "Disabled":
                if( rdbtn.IsChecked.Value ) {
                    std.Log = null;
                }
                break;
                case "Remove":
                if( rdbtn.IsChecked.Value ) {
                    std.removeLog();
                }
                break;
            }
        }

        private void GetCharOnStdIn( object sender, RoutedEventArgs e )
        {
            if( !getching ) {
                inputs = new Task(InputTask, inputs);
                btn_Getch.Content = "Stop Getching";
                getching = true;
                inputs.Start();
            } else {
                getching = false;
                btn_Getch.Content = "Start Getching";
            }
        }

        private void StartReadingStdIn( object sender, RoutedEventArgs e )
        {
            StdStream.Init(CreationFlags.TryConsole);
            string inp = StdStream.Inp.ReadLine();
            txt_Input.Text += inp;
        }

        private void button7_Click( object sender, RoutedEventArgs e )
        {
            txt_Input.Text = string.Format("processid: {0}", Utility.CommandLine( txt_Input.Text, getReturnCode ));
            LayoutUpdated += Form1_Paint;
        }
        
        
        private void Form1_Paint( object sender, EventArgs e )
        {
            if( labeltext.Length > 0 ) {
                txt_Input.Text += ( " \n" + labeltext );
                labeltext = "";
            }
        }

        private void getReturnCode( int returncode )
        {
            labeltext = string.Format("returned: {0}", returncode);
            UpdateLayout();
        }
    }
}
