/*///////////////////////////////////////////////////////////*\
||                                                           ||
||     File:      ConsolaUtils.cpp                           ||
||     Author:    Autogenerated                              ||
||     Generated: 25.03.2022                                 ||
||                                                           ||
\*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/
#include <settings.h>
#include <process.h>

#include <.byteOrder.h>

using namespace   System;
using namespace   System::IO;
using namespace   System::Text;
using namespace   System::Threading;
using namespace   System::Threading::Tasks;
using namespace   System::Collections::Generic;

#include "ConsolaLogger.hpp"
#include "ConsolaStream.hpp"
#include "ConsolaAuxilary.hpp"
#include "ConsolaUtils.hpp"

#include <enumoperators.h>


typedef Dictionary<String^,String^> StringDict;
typedef Tuple<StringDict^,StringBuilder^,Delegate^,Consola::Utility::Flags> ParameterTuple;



/*------------------------- Consola::Utility HelerFunctions -----------------------------*/

int 
_performSystemCall( String^ command )
{
    array<byte>^ Cmd = Encoding::Default->GetBytes(command);
    pin_ptr<byte> cmdptr(&Cmd[0]);
    const char* cmd = (const char*)cmdptr;
    while (*++cmd);
    while (*--cmd) if (*cmd == '\\') break;
    if (!*cmd) {
        array<byte>^ Wrk = Encoding::Default->GetBytes( Directory::GetCurrentDirectory() );
        pin_ptr<byte> wrkptr(&Wrk[0]);
        const char* wrk = (const char*)wrkptr;
        uint val = Consola::StdStream::keygenerator->Next(_CRT_INT_MAX);
        while ( !Consola::StdStream::Inp->lockup(val) ) {
            Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME * 5 );
        } cmd = Consola::Utility::merge( wrk, cmd );
        Consola::StdStream::Inp->unlock( val );
    } else {
        cmd = (const char*)cmdptr;
    } return system( cmd );
}

ParameterTuple^
_evaluateParameters( array<Object^>^ args, Consola::Utility::Flags flags )
{
     StringBuilder^ parameters = nullptr;
     StringDict^ environment = nullptr;
     Delegate^ onexit = nullptr;

     for (int x = 0; x < args->Length; ++x ) {
         if (args[x]->GetType() == array<String^>::typeid) {
             array<String^>^ strarg = safe_cast<array<String^>^>( args[x] );
             parameters = gcnew System::Text::StringBuilder( strarg[0] );
             for (int i = 1; i < strarg->Length; ++i)
                 parameters->Append( String::Format(" {0}", args[i]) );
         }
         else if (args[x]->GetType() == String::typeid) {
             parameters = gcnew System::Text::StringBuilder( args[0]->ToString() );
         }
         else if (args[x]->GetType() == Dictionary<String^,String^>::typeid) {
             environment = safe_cast<Dictionary<String^, String^>^>( args[x] );
         }
         else if (args[x]->GetType() == Consola::Utility::ProcessFinishedDelegate::typeid) {
             onexit = safe_cast<Consola::Utility::ProcessFinishedDelegate^>( args[x] );
             enum_utils::addFlag( flags, Consola::Utility::Flags::Hidden );
         }
         else if (args[x]->GetType() == Action<int>::typeid) {
             onexit = safe_cast<Action<int>^>( args[x] );
         }
     } return gcnew ParameterTuple( environment, parameters, onexit, flags );
}

System::Diagnostics::Process^ 
_instanciateNewProcess( String^ cmd, Consola::Utility::Flags flg, StringDict^ env, StringBuilder^ arg )
{
    System::Diagnostics::ProcessStartInfo^ info = arg
        ? gcnew System::Diagnostics::ProcessStartInfo( cmd, arg->ToString() )
        : gcnew System::Diagnostics::ProcessStartInfo( cmd );

    info->RedirectStandardOutput = !enum_utils::hasFlag(flg, Consola::Utility::Flags::Detached);
    info->RedirectStandardError = !enum_utils::hasFlag(flg, Consola::Utility::Flags::Detached);
    info->CreateNoWindow = enum_utils::hasFlag(flg, Consola::Utility::Flags::Hidden);
    info->UseShellExecute = enum_utils::hasFlag(flg, Consola::Utility::Flags::Shell);
    info->WorkingDirectory = Consola::StdStream::Cwd;

    if( env ) {
        IEnumerator<KeyValuePair<String^,String^>>^ it = env->GetEnumerator();
        while( it->MoveNext() ) {
#ifdef Dotnet6Build
            info->EnvironmentVariables->Add(it->Current.Key, it->Current.Value); 
#else
            info->Environment->Add(it->Current.Key, it->Current.Value);
#endif
        } it->~IEnumerator<KeyValuePair<String^,String^>>();
    } 

    System::Diagnostics::Process^ proc = gcnew System::Diagnostics::Process();
    proc->StartInfo = info;

    if (!enum_utils::hasFlag(flg, Consola::Utility::Flags::Detached)) {
        proc->EnableRaisingEvents = true;
        if (!enum_utils::hasFlag(flg, Consola::Utility::Flags::Hidden)) {
            proc->ErrorDataReceived += Consola::StdStream::Err->GetDelegate();
            proc->OutputDataReceived += Consola::StdStream::Out->GetDelegate();
        } 
    } return proc;
}


/*------------------------- Consola::Utility Implementation -----------------------------*/

unsigned
Consola::Utility::VersionNumber::get(void)
{
    return CONSOLA_VERSION_NUMBER;
}

String^
Consola::Utility::VersionString::get(void)
{
    const char vers[] = CONSOLA_VERSION_STRING;
    return gcnew String( vers );
}

System::String^
Consola::Utility::NameOfTheCommander()
{
    return System::Diagnostics::Process::GetCurrentProcess()->ProcessName;
}

System::Int32
Consola::Utility::ProcessOfIdentity()
{
    return System::Diagnostics::Process::GetCurrentProcess()->Id;
}

System::String^
Consola::Utility::PathOfTheCommander()
{
    String^ path = System::Diagnostics::Process::GetCurrentProcess()->MainModule->FileName;
    return path->Substring( 0, path->LastIndexOf('\\') );
}

System::String^
Consola::Utility::NameOfTheMachinery()
{
    return System::Diagnostics::Process::GetCurrentProcess()->MachineName;
}

System::String^
Consola::Utility::ArchitectureOfChip()
{
#ifdef _WIN64
    String^ bits = "64bit ";
#elif _WIN32
    String^ bits = "32bit ";
#endif
    return bits + IS_BIG_ENDIAN()
        ? String::Format("x86 {0} (BigEndian)", bits)
        : String::Format("CPU {0} (LittleEndian)", bits);
}

int
Consola::Utility::CommandLine( String^ command, Flags flags, ...array<Object^>^ args )
{
    if ( args == nullptr ) args = System::Array::Empty<Object^>();
    if ( enum_utils::anyFlag( flags, Flags::Asynch | Flags::Detached ) ) {
        ParameterTuple^ proc_args = _evaluateParameters( args, flags );
        System::Diagnostics::Process^ proc = _instanciateNewProcess( 
            command, flags = proc_args->Item4, proc_args->Item1, proc_args->Item2
        );

        if ( !enum_utils::hasFlag( flags, Flags::Detached ) ) {
            proc->Exited += gcnew EventHandler(ended);
        } 

        int result = -1;
        if( proc->Start() ) {
            result = proc->Id;
            if( proc_args->Item3 != nullptr ) {
                if (proc_args->Item3->GetType() == ProcessFinishedDelegate::typeid)
                    exits->Add( result, dynamic_cast<ProcessFinishedDelegate^>(proc_args->Item3) );
                else {
                    proc->BeginErrorReadLine();
                    proc->BeginOutputReadLine();
                    axits->Add( result, dynamic_cast<Action<int>^>(proc_args->Item3) );
                }
            }
        } return result;
    } else return _performSystemCall( command );
}

int
Consola::Utility::CommandLine( String^ command, Action<int>^ onexit )
{
    String^ parameters = String::Empty;
    if ( command->StartsWith( "\"" ) ) {
        int split = command->IndexOf('\"', 1) + 1;
        parameters = command->Substring(split);
        command = command->Substring(0,split);
    } else if ( command->Contains(" ") ) {
        int split = command->IndexOf(" ") + 1;
        parameters = command->Substring(split);
        command = command->Substring(0, split);
    }
    return CommandLine( command, Flags::Asynch, parameters, onexit );
}

int
Consola::Utility::CommandLine( String^ command )
{
    return CommandLine( command, Flags::Simple );
}


void
Consola::Utility::ended( Object^ sender, EventArgs^ e )
{
    System::Diagnostics::Process^ proc = dynamic_cast<System::Diagnostics::Process^>( sender );
    if (exits->ContainsKey( proc->Id )) {
        Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME * 100 );
        Consola::Utility::ProcessFinishedDelegate^ onFinished;
        if( exits->TryGetValue( proc->Id, onFinished ) )
            onFinished( proc->ExitCode, proc->StandardOutput->ReadToEnd(), proc->StandardError->ReadToEnd() );
        exits->Remove( proc->Id );
    } else if( axits->ContainsKey( proc->Id ) ) {
        Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME * 100 );
        proc->ErrorDataReceived -= StdStream::Err->GetDelegate();
        proc->OutputDataReceived -= StdStream::Out->GetDelegate();
        Action<int>^ onFinished;
        if( axits->TryGetValue( proc->Id, onFinished ) )
            onFinished( proc->ExitCode );
        axits->Remove( proc->Id );
    } proc->Exited -= gcnew EventHandler( ended );
    proc->~Process();
}

