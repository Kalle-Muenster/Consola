//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading.Tasks;
//using CreationFlags = Consola.CreationFlags;

//[assembly: InternalsVisibleTo("Consola, PublicKey="+
//"00240000048000009400000006020000" +
//"00240000525341310004000001000100" +
//"217B62BDF3FC97C915361097C8DDE798" +
//"3FD6CE101155A05F23D704045ADE7AC6" +
//"5652CCA5ACC242A475B2A574D397B60D" +
//"575F31E15720D7FD2E76EA33654C43FA" +
//"BC60687FA251B2B22CF69BD527ACD834" +
//"E21A5D22F30814DD6C4768EC99E5EF46" +
//"7599A6CCC17A15A1FCD3414BA4EE067E" +
//"5E22955E811F56BC666E0A4F3B5BE2C5")]


namespace Consola
{
    static public class Consola 
    {
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
