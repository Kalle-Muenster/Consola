using System;


namespace Consola.Test
{
    internal class ConsolaTest : Test
    {
        public ConsolaTest( bool logall ) : base(logall)
        {
            Consola.Init( CreationFlags.CreateLog );
        }

        protected override void TestSuite()
        {
            Random generator = new Random((int)DateTime.Now.Ticks);
            uint version = Utility.VersionNumber;
            uint minor = (version & 0x0000ff00) >> 8;
            uint build = version & 0x000000ff;
            version = (version & 0xffff0000) >> 16;
            Consola.StdStream.Out.Stream.Put("version: ").Put(version).Put(".").Put(minor).Put(".").Put(build).Put("\n").End();

            NextCase("StdOut + StdErr");
            Consola.StdStream.Out.WriteLine("test: writing to stdout (this line) and stderr (next line)...");
            Consola.StdStream.Err.WriteLine("    Hallo World!");
            CloseCase( CurrentCase );

            NextCase( "StdInp" );
            Consola.StdStream.Out.WriteLine( "\n...next test: reading line from stdin... -> please enter some text:\n" );
            string input = Consola.StdStream.Inp.ReadLine();
            Consola.StdStream.Out.WriteLine( "Input text was:\n    {0}", input );
            Consola.StdStream.Out.WriteLine( "\n...next test: writing one long string by one single write call" );
            int charcount = (int)(5 * 1024);
            char[] buffer = new char[charcount];
            for (int i = 0; i < charcount; i++)
                buffer[i] = (char)generator.Next(48, 122);
            string ratherlongstring = new string(buffer);
            Consola.StdStream.Err.Write( ratherlongstring );
            Consola.StdStream.Out.WriteLine( "" );
            Consola.StdStream.Out.WriteLine( "...done  ... wrote {0} chars into the stderr stream...", charcount );
            CloseCase( CurrentCase );

            NextCase( "XmlOut" );
            Consola.StdStream.Aux.Xml.createLog();
            string logfile = Consola.StdStream.Aux.Xml.Log.Info.FullName;
            CheckStep( logfile.Length > 0, "Creating xml-log file:", logfile );
            Consola.StdStream.Aux.Xml.NewScope( AuxXml.State.Document );
            Consola.StdStream.Aux.Xml.WriteElement("aussenRom");
            Consola.StdStream.Aux.Xml.WriteElement("dingensDong",new string[]{"wurst=schinken","durst=trinken"});
            Consola.StdStream.Aux.Xml.WriteElement("inneframe");
            Consola.StdStream.Aux.Xml.WriteContent("innerPong");
            Consola.StdStream.Aux.Xml.closeLog();
            CloseCase( CurrentCase );
        }
    }
}
