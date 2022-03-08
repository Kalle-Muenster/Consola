using Consola;
using System.IO;
using System;


namespace StdStreamsTest
{
    class Program
    {
        private static StdStreams std = new StdStreams(CreationFlags.TryConsole);
        static Random generator = new Random((int)DateTime.Now.Ticks);

        static void Main(string[] args)
        {
            if (args.Length > 0) {
                if (args[0]== "--inputtest") {
                    string check = std.Inp.ReadLine();
                    std.Out.WriteLine( "have read input:" );
                    std.Out.WriteLine( check );
                    std.Out.WriteLine( "    ...{0} byte from stdin", check.Length );
                    std.Out.WriteLine( "\n\nwill save to file: fromStdInp.txt" );
                    FileStream f = new FileInfo("fromStdInp.txt").OpenWrite();
                    TextWriter writer = new System.IO.StreamWriter(f);
                    writer.Write(check);
                    writer.Write('\n');
                    writer.Flush();
                    writer.Close();
                    f.Close();
                    return;
                }
            }

            uint ver = StdStream.VersionNumber();

            std.Out.WriteLine("Consola Stream IO and Log Writer v{0}.{1}.{2}.{3}\n",(ver&0xff000000)>>24,
                                 (ver&0x00ff0000)>>16,(ver&0x0000ff00)>>8,ver&0x000000ff);
            std.Out.WriteLine("test: writing to stdout (this line) and stderr (next line)...");
            std.Err.WriteLine("    Hallo World!");
            std.Out.WriteLine("...next test: reading line from stdin... -> please enter some text:\n");
            string input = std.Inp.ReadLine();
            std.Out.WriteLine( "Input was:\n    {0}", input );
            std.Out.WriteLine("...next test: writing one long string per one single write operation");
            int charcount = (int)(5 * 1024);
            char[] buffer = new char[charcount];
            for (int i = 0; i < charcount; i++)
                buffer[i] = (char)generator.Next(48,122);
            string ratherlongstring = new string(buffer);
            std.Err.Write( ratherlongstring );
            std.Out.WriteLine("");
            std.Out.WriteLine( "...done  ... wrote {0} chars into the stderr stream...", charcount );

            return;
        }
    }
}
