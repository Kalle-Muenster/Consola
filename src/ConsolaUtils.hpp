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
        enum class Flags : unsigned {
            Simple, Asynch, Detach, Hidden = 4
        };

        delegate void ProcessFinishedDelegate(int result, String^ stdOut, String^ stdErr);

        static Utility() {
            exits = gcnew Dictionary<int,ProcessFinishedDelegate^>(0);
            axits = gcnew Dictionary<int, Action<int>^>(0);
        }
        static unsigned VersionNumber();
        static String^  VersionString();
        static Int32    ProgramProc();
        static String^  ProgramName();
        static String^  MachineName();
        static String^  MachineArch();

        static int      CommandExec( String^ command );
        static int      CommandExec( String^ command, Action<int>^ onexit );
        static int      CommandExec( String^ command, Flags flags, ...array<Object^>^ );

    private:
        static const char*  merge( const char* a, const char* b );
        static void         ended( Object^ sender, EventArgs^ e );
        static Dictionary<int,ProcessFinishedDelegate^>^ exits;
        static Dictionary<int,Action<int>^>^             axits;
    };


} //end of Consola
#endif
