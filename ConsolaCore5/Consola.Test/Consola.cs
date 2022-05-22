using ConsolaUtility = Consola.Utility;
using Flags = Consola.CreationFlags;

namespace Consola
{
    public static class Extensions
    {
        public static Flags flags( this Consola.CreationFlags flags )
        {
            return (Flags)flags;
        } 
    }

    static public class Consola 
    {
        [System.Flags]
        public enum CreationFlags : uint
        {
            NewConsole = Flags.NewConsole,
            TryConsole = Flags.TryConsole,
            UseConsole = Flags.UseConsole,
            CreateLog = Flags.CreateLog,
            OutputLog = Flags.OutputLog,
            AppendLog = Flags.AppendLog,
            NoInputLog = Flags.NoInputLog,
            SharedLogs = Flags.SharedLogs,
            LoggingFlags = Flags.LoggingFlagsMask
        }

        public class Utility : ConsolaUtility {}
        private static StdStreams std;

        public static StdStreams StdStream
        {
            get { return std ?? (std = new StdStreams()); }
        }

        public static StdOut Out
        {
            get { return std.Out; }
        }

        public static StdErr Err
        {
            get { return std.Err; }
        }

        public static StdInp Inp
        {
            get { return std.Inp; }
        }

        public static AuxilaryStream Aux
        {
            get { return std.Aux; }
        }

        public static void Init( CreationFlags creation )
        {
            std = new StdStreams( creation.flags() );
        }

        public static void Init( CreationFlags creation, string logfile )
        {
            std = new StdStreams( creation.flags(), logfile );
        }

        public static void Init( string logfile )
        {
            std = new StdStreams( logfile );
        }

        public static void Init()
        {
            std = new StdStreams();
        }
    }
}
