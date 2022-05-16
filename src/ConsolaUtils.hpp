/*///////////////////////////////////////////////////////////*\
||                                                           ||
||     File:      ConsolaUtils.hpp                           ||
||     Author:    Kalle                                      ||
||     Generated: 25.03.2022                                 ||
||                                                           ||
\*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/
#ifndef _ConsolaUtils_hpp_
#define _ConsolaUtils_hpp_

using namespace System;
using namespace System::Collections::Generic;

namespace Consola
{
    

    public ref class Utility
    {
    public:

        [FlagsAttribute()]
        enum class Flags : unsigned {
            Simple = 0, Asynch = 1, Detached = 2, Hidden = 4, Shell = 8
        };

        delegate void ProcessFinishedDelegate(
            int result, String^ stdOut, String^ stdErr
        );

        static Utility() {
            exits = gcnew Dictionary<int,ProcessFinishedDelegate^>(0);
            axits = gcnew Dictionary<int,Action<int>^>(0);
        }

        static property unsigned VersionNumber{ unsigned get(void); };
        static property String^  VersionString{ String^ get(void); };

        static Int32    ProgramProc();
        static String^  ProgramName();
        static String^  ProgramPath();
        static String^  MachineName();
        static String^  MachineArch();

        static int      CommandLine( String^ command );
        static int      CommandLine( String^ command, Action<int>^ onexit );
        static int      CommandLine( String^ command, Flags flags, ...array<Object^>^ );

    internal:
        static const char*  merge( const char* a, const char* b );

    private:
        static void         ended( Object^ sender, EventArgs^ e );
        static Dictionary<int,ProcessFinishedDelegate^>^    exits;
        static Dictionary<int,Action<int>^>^                axits;
    };


} //end of Consola
#endif
