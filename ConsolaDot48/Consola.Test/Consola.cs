using ConsolaUtility = Consola.Utility;
using CreationFlags = Consola.CreationFlags;

namespace Consola
{
    static public class Consola 
    {
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

        public static void Init( CreationFlags creationflags )
        {
            std = new StdStreams( creationflags );
        }

        public static void Init( CreationFlags creationflags, string logfilename )
        {
            std = new StdStreams( creationflags, logfilename );
        }

        public static void Init( string logfilename )
        {
            std = new StdStreams( logfilename );
        }

        public static void Init()
        {
            std = new StdStreams();
        }
    }
}
