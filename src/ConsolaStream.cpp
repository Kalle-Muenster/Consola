/*///////////////////////////////////////////////////////////*\
||                                                           ||
||     File:      ConsolaStream.cpp                          ||
||     Author:    Kalle                                      ||
||     Generated: 07.12.2018                                 ||
||                                                           ||
\*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/

#include <settings.h>

#include <consoleapi.h>
#include <consoleapi3.h>
#include <stdio.h>
#include <string.h>
#include <limits.h>

#include <.stringPool.h>


using namespace   System;
using namespace   System::IO;
using namespace   System::Threading::Tasks;
using namespace   System::Threading;
const char*       EmptyString = "\0";


#include "ConsolaLogger.hpp"
#include "ConsolaStream.hpp"
#include "ConsolaAuxilary.hpp"
#include "ConsolaUtils.hpp"
#include "ConsolaParser.hpp"


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// non-threadsafe helper function:

int Consola::sizeOfPoolInstance(void) {
    return CYCLE_SIZE;
}

/* condition:
   NULL:  means read till linebrake (or at maximum poolsize on byte)
   VALUE: means read VALUE count on bytes (or at maximum poolsize on byte)
   EMPTY: means read all available bytes (or at maximum poolsize on byte) */
uint _readFromStdIn( uint condition, char* workBuffer, int& plan ) { pool_scope

    uint i = 0;
    bool done = false;
    uint size = 0;
    int slices = 0;
    plan = *pool_ensure( MAX_NAM_LEN );
    const char* end = workBuffer + (MAX_NAM_LEN - 1);
    do { int need = MAX_NAM_LEN;
        for (char* buffer = workBuffer; buffer < end; ++buffer) { ++i;
            done = fread( buffer, 1, 1, stdin ) == EOF;
            if ( condition ? (!--condition) : (*buffer == '\n') ) {
                done = true;
                *buffer = '\0';
                need -= (int)(end - buffer);
                break; }
        } if ( ( plan = pool_sizePlan( need ) ) > 0 ) {
            pool_setb( workBuffer, i );
            size += need;
            slices += 1;
            i = 0;
        } else {
            pool_merge( slices );
        return size; }
    } while ( !done );
    pool_merge( slices );
    return size;
}

String^
_readStringFromStdIn( uint condition = NULL ) { pool_scope

    char Buffer[MAX_NAM_LEN] = { ' ' };
    Buffer[MAX_NAM_LEN - 1] = '\0';
    int plan = MAX_NAM_LEN, size = 0;
    Text::StringBuilder^ builder = gcnew Text::StringBuilder(MAX_NAM_LEN);
    do { size += _readFromStdIn( condition, &Buffer[0], plan );
        builder->Append( gcnew String( pool_get() ) );
    } while ( plan < 0 );
    return builder->ToString();
}

uint
_rawDataFromStdIn( IntPtr destination, uint offset, uint length ) { pool_scope

    char Buffer[MAX_NAM_LEN] = { ' ' };
    Buffer[MAX_NAM_LEN - 1] = '\0';
    int plan = MAX_NAM_LEN;
    uint bytesCopied = 0;
    byte* ptDst = (byte*)destination.ToPointer();
    do { uint bytesRead = _readFromStdIn( length, &Buffer[0], plan );
        const uint chunk = min( MAX_NAM_LEN - 1, bytesRead );
        byte* pool = (byte*)pool_get();
        byte* dst = (byte*)ptDst + bytesCopied;
        byte* end = (byte*)dst + chunk;
        for (; dst < end; ++dst) {
            *dst = *(pool++);
        } bytesCopied += chunk;
    } while (plan < 0);
    return bytesCopied;
}

generic<class T> where T : ValueType
uint _readDataFromStdIn( array<T>^ destination, uint offset, uint length ) { pool_scope

    const uint sizeOfT = sizeof(T);
    char Buffer[MAX_NAM_LEN] = { ' ' };
    Buffer[MAX_NAM_LEN-1] = '\0';
    int plan = MAX_NAM_LEN;
    uint bytesCopied = 0;
    pin_ptr<T> ptDst = &destination[offset];
    do { uint bytesRead = _readFromStdIn( length * sizeOfT, &Buffer[0], plan );
        const uint chunk = min(MAX_NAM_LEN - 1, bytesRead);
        byte* pool = (byte*)pool_get();
        byte* dst = (byte*)ptDst + bytesCopied;
        byte* end = (byte*)dst + chunk;
        for (; dst < end; ++dst) {
              *dst = *(pool++);
        } bytesCopied += chunk;
    } while ( plan < 0 );
    return bytesCopied / sizeOfT;
}

generic<class T> where T : ValueType
array<T>^ _readArrayFromStdIn( uint Tcount ) { pool_scope

    const uint sizeOfT = sizeof(T);
    char Buffer[MAX_NAM_LEN] = { ' ' };
    Buffer[MAX_NAM_LEN - 1] = '\0';
    uint bytesCopied = 0;
    int plan = MAX_NAM_LEN; array<T>^ destination = gcnew array<T>(Tcount);
    pin_ptr<T> ptDst = &destination[0];
    do { uint bytesRead = _readFromStdIn( Tcount * sizeOfT, &Buffer[0], plan );
        const uint chunk = min( MAX_NAM_LEN - 1, bytesRead );
        byte* buffer = (byte*)pool_get();
        byte* dst = (byte*)ptDst + bytesCopied;
        byte* end = (byte*)dst + chunk;
        for (; dst < end; ++dst ) {
              *dst = *buffer++;
        } bytesCopied += chunk;
    } while (plan < 0);
    return destination;
}

/*----------------------------------------------------------------------------*/

void _writeSystemStringToStdtStream( uint direction, String^ string )
{
    if ( string == nullptr ) string = String::Empty;
    char buffer[128];
    array<wchar_t>^ str = string->ToCharArray();
    const int end = str->Length;
    const int lst = end - 1;
    for(int i = 0; i < end; ++i) {
        int I = i % 128;
        buffer[I] = (char)str[i];
        if( (I == 127) || (i == lst) ) {
            fwrite( &buffer[0], 1, I + 1, __acrt_iob_func(direction) );
        }
    }
}

uint _writeByteArrayToStdtStream( uint direction, array<byte>^ data, int oset, int size )
{
    if ( data == nullptr ) { data = Array::Empty<byte>(); size = oset = 0; }
    byte buffer[128];
    const int end = Math::Min( data->Length - oset, size );
    const int lst = end - 1;
    for (int i = 0; i < end; ++i) {
        int I = i % 128;
        buffer[I] = (byte)data[i + oset];
        if ((I == 127) || (i == lst)) {
            fwrite(&buffer[0], 1, I + 1, __acrt_iob_func(direction));
        }
    } return end;
}

uint _writeByteDataToStdtStream( uint direction, IntPtr data, int oset, int size )
{
    char nostring[1] = {'\0'};
    if (data == IntPtr::Zero) { data = IntPtr(&nostring[0]); size = 0; }
    byte buffer[128];
    const int end = size;
    const int lst = end - 1;
    byte* src = (byte*)data.ToPointer() + oset;
    for (int i = 0; i < end; ++i) {
        int I = i % 128;
        buffer[I] = src[i];
        if ((I == 127) || (i == lst)) {
            fwrite( &buffer[0], 1, I + 1, __acrt_iob_func(direction) );
        }
    } return end;
}

generic<class T> where T : ValueType
uint _writeSystemArrayToStdtStream( uint direction, array<T>^ data, int oset, ulong size )
{
    if (data == nullptr) { data = Array::Empty<T>(); size = oset = 0; }
    byte buffer[128];
    const int siz = sizeof(T);
    const int end = Math::Min( data->Length, (int)(oset + size) );
    const int chk = 128 - (128 % siz);
    const int brk = chk - siz;
    const int lst = end - 1;
    for (int i = 0; i < end; ++i) {
        T elm = data[i+oset];
        int I = (i * siz) % chk;
        _memccpy( &buffer[I], &elm, siz, 1 );
        if ((I == brk) || (i == lst)) {
            fwrite(&buffer[0], 1, I + siz, __acrt_iob_func(direction));
        }
    } return ((end - oset) * siz);
}


const char*
Consola::Utility::merge( const char* a, const char* b )
{ pool_scope

    pool_set(a);
    pool_set(b);

return pool_merge(2); }


////////////////////////////////////////////////////////////////////////////
// loose functions to be asynchronuosly executed

void
Consola::StdStream::asynchronStreamWrite( Object^ taskData )
{
    array<Object^>^ dataArray = safe_cast<array<Object^>^>( taskData );
    StreamLocker lock = safe_cast<StreamLocker>( dataArray[0] );
    String^ data = safe_cast<String^>( dataArray[1] );
    LogWriter^ logger = safe_cast<LogWriter^>( dataArray[2] );
    while (true) if ( lock.up() ) {
        _writeSystemStringToStdtStream( lock.direct, data );
        if( logger != nullptr ) {
            logger->Write( data );
            logger->Flush();
        } lock.un();
        return; 
    } else {
        Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    }
}

void
Consola::StdStream::asynchronDataWrite( Object^ taskData )
{
    array<Object^>^ dataArray = safe_cast<array<Object^>^>(taskData);
    StreamLocker lock = safe_cast<StreamLocker>(dataArray[0]);
    array<byte>^ byteData = safe_cast<array<byte>^>(dataArray[1]);
    while (true) if ( lock.up() ) {
        _writeByteArrayToStdtStream( lock.direct, byteData, 0, byteData->Length );
        lock.un();
        return;
    } else {
        Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    }
}

void
Consola::StdStream::asynchronRawDataWrite( Object^ taskData )
{
    ulong size; int oset;
    array<Object^>^ taskDataObjects = safe_cast<array<Object^>^>(taskData);
    StreamLocker lock = safe_cast<StreamLocker>(taskDataObjects[0]);
    IntPtr rawData = safe_cast<IntPtr>(taskDataObjects[1]);
    size = safe_cast<ulong>(taskDataObjects[2]);
    oset = (int)(size & 0x00000000ffffffff);
    size = (size & 0xffffffff00000000) >> 32;
    while (true) if (lock.up()) {
        _writeByteDataToStdtStream( lock.direct, rawData, oset, (int)size );
        lock.un();
        return;
    }
    else {
        Thread::Sleep(THREAD_WAITSTATE_CYCLE_TIME);
    }
}


generic<class T> where T : ValueType void
Consola::StdStream::asynchronArrayWrite( Object^ taskData )
{
    int oset;
    ulong size;
    array<Object^>^ taskDataObjects = safe_cast<array<Object^>^>(taskData);
    StreamLocker lock = safe_cast<StreamLocker>(taskDataObjects[0]);
    array<T>^ dataArray = safe_cast<array<T>^>(taskDataObjects[1]);
    if (taskDataObjects->Length > 2) {
        size = safe_cast<ulong>(taskDataObjects[2]);
        oset = (int)(size & 0x00000000ffffffff);
        size = (size & 0xffffffff00000000) >> 32;
    } else { oset = 0; size = dataArray->Length; }
    while (true) if ( lock.up() ) {
        _writeSystemArrayToStdtStream<T>( lock.direct, dataArray, oset, size );
        lock.un();
        return;
    } else {
        Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    }
}

void
Consola::StdStream::asynchronStreamGetLocked( Object^ taskData )
{
    array<Object^>^ dataArray = safe_cast<array<Object^>^>( taskData );
    StreamLocker lock = safe_cast<StreamLocker>( dataArray[0] );
    String^ data = safe_cast<String^>( dataArray[1] );
    LogWriter^ logger = safe_cast<LogWriter^>( dataArray[2] );
    while (true) if ( lock.up() ) {
        _writeSystemStringToStdtStream( lock.direct, data );
        if( logger != nullptr ) {
            logger->Write( data );
            logger->Flush();
        } return;
    } else {
        Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    }
}


String^ Consola::StdStream::asynchronStreamRead( Object^ taskData )
{
    StreamLocker lock = safe_cast<StreamLocker>( safe_cast<array<Object^>^>(taskData)[0] );
    uint condition = safe_cast<uint>( safe_cast<array<Object^>^>( taskData )[1] );
    while( true ) if ( lock.up() ) {
        return _readStringFromStdIn( condition );
    } else {
        Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    }
}

generic<class T> where T : ValueType
array<T>^ Consola::StdStream::asynchronArrayRead( Object^ taskData )
{
    StreamLocker lock = safe_cast<StreamLocker>( safe_cast<array<Object^>^>(taskData)[0] );
    uint condition = safe_cast<uint>( safe_cast<array<Object^>^>(taskData)[1] );
    while( true ) if ( lock.up() ) {
        return _readArrayFromStdIn<T>( condition );
    } else {
        Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    }
}

generic<class T> where T : ValueType
uint Consola::StdStream::asynchronDataRead( Object^ taskData )
{
    array<Object^>^ dataArray = safe_cast<array<Object^>^>( taskData );
    StreamLocker lock = safe_cast<StreamLocker>( dataArray[0] );
    array<T>^ dst = safe_cast<array<T>^>( dataArray[1] );
    uint offset = safe_cast<uint>( dataArray[2] );
    uint Tcount = safe_cast<uint>( dataArray[3] );
    while( true ) if ( lock.up() ) {
        return _readDataFromStdIn<T>( dst, offset, Tcount );
    } else {
        Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    }
}

uint Consola::StdStream::asynchronRawDataRead( Object^ taskData )
{
    array<Object^>^ dataArray = safe_cast<array<Object^>^>(taskData);
    StreamLocker lock = safe_cast<StreamLocker>(dataArray[0]);
    IntPtr dst = safe_cast<IntPtr>(dataArray[1]);
    uint offset = safe_cast<uint>(dataArray[2]);
    uint Tcount = safe_cast<uint>(dataArray[3]);
    while (true) if (lock.up()) {
        return _rawDataFromStdIn( dst, offset, Tcount );
    }
    else {
        Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    }
}

// --- class member definitions: ---------------------

Consola::StdStreams::StdStreams( void )
    : StdStream( Direction::Non )
{
    nam = Utility::NameOfTheCommander() + "_{0}.log";
    StdStream::Init();
}

Consola::StdStreams::StdStreams( Consola::CreationFlags createConsole )
    : StdStream( Direction::Non )
{
    nam = Utility::NameOfTheCommander() + "_{0}.log";
    StdStream::Init( createConsole );
}

Consola::StdStream::StdStream( Direction dir )
    : dir((uint)dir)
    , log(nullptr)
    , enc(Encoding::Default) {
    if( dir == Direction::Inp || dir == Direction::Non ) { pool_scope
        if( keygenerator == nullptr ) keygenerator
            = gcnew Random( (int)DateTime::Now.Ticks );
        pool_InitializeCycle();
    } else { instanzencounter++; }
}

Consola::StdStream::~StdStream( void ) { pool_scope
    
    closeLog();
    if ( instanzencounter )
       --instanzencounter;
}

void
Consola::StdStream::CreateConsole( void )
{
    AllocConsole();
    RedirectStreams();
    consolestate = Consola::CreationFlags::NewConsole;
}

void
Consola::StdStream::RedirectStreams( void )
{
    freopen("conin$", "r", stdin);
    freopen("conout$", "w", stdout);
    freopen("conout$", "w", stderr);
    consolestate = Consola::CreationFlags::UseConsole;
}



void
Consola::StdStream::Init( void )
{
    nam = Utility::NameOfTheCommander()
        + "_{0}.log";
    Init( Consola::CreationFlags::None );
}

void
Consola::StdStream::Init( String^ logfile )
{
    Init( Consola::CreationFlags::TryConsole, logfile );
}

void
Consola::StdStream::SetLogName( String^ logfilename )
{
    int ext = logfilename->LastIndexOf(".");
    if( ext < 0 ) {
        logfilename += "_{0}.log";
    } else {
        logfilename = logfilename->Substring( 0, ext ) + "_{0}"
                    + logfilename->Substring( ext );
    } nam = logfilename;
}

void
Consola::StdStream::Init( Consola::CreationFlags flags, String^ logfile )
{
    SetLogName( logfile );
    Init( flags | Consola::CreationFlags::AppendLog );
}

void
Consola::StdStream::Init( Consola::CreationFlags creationflags )
{
    loggingstate = creationflags & Consola::CreationFlags::LoggerMask;
    if( nam == nullptr )
        nam = Utility::NameOfTheCommander()
            + "_{0}.log";

    creationflags = creationflags & ~Consola::CreationFlags::LoggerMask;

    if( creationflags == Consola::CreationFlags::None ) {
        creationflags = Consola::CreationFlags::TryConsole;
    }
        if ( consolestate != creationflags )
    switch ( consolestate = creationflags )
    {
    case Consola::CreationFlags::NewConsole:
        CreateConsole();
        break;
    case Consola::CreationFlags::UseConsole:
        RedirectStreams();
        break;
    case Consola::CreationFlags::TryConsole:
        if ( !GetConsoleWindow() )
            CreateConsole();
        else
            RedirectStreams();
        break;
    }

    if( instanzencounter == 0 ) {
        gcnew StdInp();
        gcnew StdOut();
        gcnew StdErr();
    }
    if( loggingstate != Consola::CreationFlags::None ) {
        oup->createLog();
        if( !loggingstate.HasFlag( Consola::CreationFlags::NoInputLog ) )
            inp->createLog();
        if( !loggingstate.HasFlag( Consola::CreationFlags::SharedLogs ) )
             err->createLog();
        else err->Log = oup->log;
    }
}

Consola::CreationFlags
Consola::StdStream::CreationFlags::get(void)
{
    return loggingstate == Consola::CreationFlags::None
         ? consolestate == Consola::CreationFlags::None
                         ? Consola::CreationFlags::None
                         : Consola::CreationFlags::ByDefault
         : loggingstate;
}

bool
Consola::StdStream::strmlockup( StdStream^ strm, uint key )
{
    return strm->lockup( key );
}
bool
Consola::StdStream::strmunlock(StdStream^ strm, uint key)
{
    return strm->unlock( key );
}

String^
Consola::StdStream::Cwd::get()
{
    return Directory::GetCurrentDirectory();
}

void
Consola::StdStream::Cwd::set( String^ cd )
{
    Directory::SetCurrentDirectory( cd );
}


Consola::AuxilaryStream^
Consola::StdStream::Aux::get(void)
{
    if (aux == nullptr) {
        gcnew AuxXml();
    } return (AuxilaryStream^)aux;
}

Consola::LogWriter^
Consola::StdStream::createLog()
{
    if( log == nullptr ) {
        String^ logfileName = String::Format(nam, (Direction)dir);
        if ( !loggingstate.HasFlag( Consola::CreationFlags::CreateLog ) ) {
            log = LogWriter::AddLog( logfileName, enc );
        } else {
            log = LogWriter::NewLog( logfileName, enc );
        } log->AutoFlush = false;
    } return log;
}

void
Consola::StdStream::closeLog()
{
    if( log != nullptr ) {
        LogWriter::Notlog(
            log->Info->FullName
                            );
        log = nullptr;
    }
}

bool    
Consola::StdStream::existsLog()
{
    return( log != nullptr );
}

void           
Consola::StdStream::removeLog()
{
    if( log != nullptr ) {
        LogWriter::DelLog(
            log->Info->FullName
                            );
        log = nullptr;
    }
}

void
Consola::StdStreams::closeLog( void )
{
    StdStream::Inp->closeLog();
    if ( StdStream::Out->Log == StdStream::Err->Log )
         StdStream::Out->log = nullptr;
    else StdStream::Out->closeLog();
    StdStream::Err->closeLog();
    this->log = nullptr;
}

void
Consola::StdStreams::removeLog(void)
{
    StdStream::Inp->removeLog();
    if ( StdStream::Out->Log == StdStream::Err->Log)
         StdStream::Out->log = nullptr;
    else StdStream::Out->removeLog();
    StdStream::Err->removeLog();
    this->log = nullptr;
}

void
Consola::StdStream::systemStringToStdOut( String^ string )
{
    uint key = keygenerator->Next( INT_MAX );
    if ( lockup( key ) ) {
        _writeSystemStringToStdtStream( dir, string );
        if (log != nullptr) {
            log->Write( string );
            log->Flush(); }
        unlock( key );
    } else {
        Action<Object^> ^fu = gcnew Action<Object^>(this->asynchronStreamWrite);
        ( gcnew Task( fu, gcnew array<Object^>{ StreamLocker( this, key ), string, log })
         )->Start();
    } 
}

void
Consola::StdStream::dataArrayToStdOut( array<byte>^ data )
{
    uint key = keygenerator->Next( INT_MAX );
    if ( lockup( key ) ) {
        _writeByteArrayToStdtStream( dir, data, 0, data->Length );
        unlock( key );
    } else {
        Task^ task;
        Action<Object^>^ fu = gcnew Action<Object^>( this->asynchronDataWrite );
        task = gcnew Task( fu, gcnew array<Object^>{ StreamLocker(this,key), data } );
        task->Start();
    }
}

generic<class T> where T : ValueType void
Consola::StdStream::systemArrayToStdOut( array<T>^ data, int oset, int size )
{
    uint key = keygenerator->Next( INT_MAX );
    if ( lockup( key ) ) {
        _writeSystemArrayToStdtStream<T>( dir, data, oset, size );
        unlock( key );
    } else { Task^ task;
        ulong info = size;
        info = (oset | (info << 32));
        Action<Object^>^ fu = gcnew Action<Object^>( this->asynchronArrayWrite<T> );
        task = gcnew Task( fu, gcnew array<Object^>{ StreamLocker(this,key), data, info } );
        task->Start();
    }
}

void
Consola::StdStream::rawDataToStdOut( IntPtr data, int oset, int size )
{
    uint key = keygenerator->Next(INT_MAX);
    if (lockup(key)) {
        _writeByteDataToStdtStream( dir, data, oset, size);
        unlock(key);
    } else {
        Task^ task;
        ulong info = size;
        info = (oset | (info << 32));
        Action<Object^>^ fu = gcnew Action<Object^>( this->asynchronRawDataWrite );
        task = gcnew Task(fu, gcnew array<Object^>{ StreamLocker(this, key), data, info });
        task->Start();
    }
}

void
Consola::OutStream::LockedStreamWrite( String^ data )
{
    while (lockvar != streamlocked) {
        if (lockvar == EMPTY) {
            lockvar = streamlocked;
            break;
        } else {
            System::Threading::Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
        }
    }
    _writeSystemStringToStdtStream( dir, data );
    if( log != nullptr ) {
        log->Write( data );
        log->Flush();
    }
}

System::Diagnostics::DataReceivedEventHandler^ 
Consola::OutStream::GetDelegate()
{
    return gcnew System::Diagnostics::DataReceivedEventHandler( this, &OutStream::WriteLineCallback );
}

void
Consola::OutStream::WriteLineCallback( Object^ sender, System::Diagnostics::DataReceivedEventArgs^ e )
{
    WriteLine( e->Data == nullptr ? L"\n" : e->Data );
}

Consola::ILocked^
Consola::OutStream::Stream::get( void )
{
    uint hocker = keygenerator->Next( INT_MAX );
    while( !lockup( hocker ) ) {
        System::Threading::Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    } streamlocked = hocker;
    switch( Direction(dir) ) {
    case Direction::Out: return gcnew Locked<StdOut^>( (StdOut^)this );
    case Direction::Err: return gcnew Locked<StdErr^>( (StdErr^)this );
    default: return nullptr;
    }
}

String^
Consola::StdStream::systemStringFromStdIn( uint condition )
{
    String^ returnvalue = String::Empty;
    UInt32 key = keygenerator->Next( INT_MAX );
    if ( lockup( key ) )
        returnvalue = _readStringFromStdIn( condition );
    else { Task<String^>^ task;
        Func<Object^,String^> ^fu = gcnew Func<Object^,String^>( this->asynchronStreamRead );
        task = Task<String^>::Factory->StartNew( fu, gcnew array<Object^>{ StreamLocker( this, key ), condition } );
        returnvalue = task->GetAwaiter().GetResult();
    } unlock( key );
    if( log != nullptr ) {
        log->Write( String::Format( "{0}\n", returnvalue ) );
        log->Flush();
    } return returnvalue;
}

uint
Consola::StdStream::rawDataFromStdIn( IntPtr dst, int offset, int length )
{
    uint returnvalue = 0;
    UInt32 key = keygenerator->Next(INT_MAX);
    if (lockup(key))
        returnvalue = _rawDataFromStdIn( dst,offset, length );
    else {
        Task<uint>^ task;
        ulong info = length;
        info = (offset | (info << 32));
        Func<Object^,uint>^ fu = gcnew Func<Object^,uint>( this->asynchronRawDataRead );
        task = Task<uint>::Factory->StartNew(fu, gcnew array<Object^>{ StreamLocker(this, key), info });
        returnvalue = task->GetAwaiter().GetResult();
    } unlock(key);
    return returnvalue;
}

generic<class T> where T : ValueType array<T>^
Consola::StdStream::systemArrayFromStdIn( uint condition )
{
    array<T>^ returnvalue = nullptr;
    UInt32 key = keygenerator->Next(INT_MAX);
    if ( lockup( key ) )
        returnvalue = _readArrayFromStdIn<T>(condition);
    else { Task<array<T>^>^ task;
        Func<Object^,array<T>^>^ fu = gcnew Func<Object^,array<T>^>( this->asynchronArrayRead<T> );
        task = Task<array<T>^>::Factory->StartNew( fu, gcnew array<Object^>{ StreamLocker( this, key ), condition } );
        returnvalue = (array<T>^)task->GetAwaiter().GetResult();
    } unlock( key );
    if (log != nullptr) {
        for (int i = 0; i < returnvalue->Length; ++i)
        log->Write( String::Format( "{0}\n", returnvalue[i]->ToString() ) );
        log->Flush();
    } return returnvalue;
}

generic<class T> where T : ValueType uint
Consola::StdStream::readDataFromStdIn( array<T>^ dst,uint offset,uint length )
{
    uint returnvalue = 0;
    uint key = keygenerator->Next(INT_MAX);
    if ( lockup( key ) )
        returnvalue = _readDataFromStdIn<T>( dst, offset, length );
    else { Task<uint>^ task;
        Func<Object^,uint>^ fu = gcnew Func<Object^,uint>( this->asynchronDataRead<T> );
        array<Object^>^ taskdata = gcnew array<Object^>{ StreamLocker( this, key ), dst, offset, length };
        task = Task<uint>::Factory->StartNew( fu, taskdata );
        returnvalue = task->GetAwaiter().GetResult();
    } unlock( key );
    if (log != nullptr) {
        for  ( int i = 0; i < dst->Length; ++i )
        log->Write( String::Format( "{0}\n", dst[i]->ToString() ) );
        log->Flush();
    } return returnvalue;
}

String^
Consola::StdInp::ReadLine(void)
{
    return systemStringFromStdIn( NULL );
}

String^
Consola::StdInp::ReadAll( void )
{
    return systemStringFromStdIn( EMPTY );
}

byte* 
_getPointerToPrimitive( Object^ primitiv, int* size )
{
    if (size) *size = System::Runtime::InteropServices::Marshal::SizeOf( primitiv );
    return (byte*)System::Runtime::InteropServices::Marshal::GetIUnknownForObject( primitiv ).ToPointer();
}

String^
Consola::StdInp::ReadTill( Object^ termi )
{
    Type^ typie = termi->GetType();
    array<byte>^ sequence = Array::Empty<byte>();
    if ( typie == String::typeid ) {
        sequence = System::Text::Encoding::Default->GetBytes( termi->ToString() );
    } else if ( typie->IsPrimitive ) {
        if( Type::GetTypeCode(typie) >= System::TypeCode::Single )
            sequence = System::Text::Encoding::Default->GetBytes( termi->ToString() );
        else {
            int size = 0;
            byte* data = _getPointerToPrimitive( termi, &size );
            sequence = gcnew array<byte>( size );
            for( int i = 0; i < size; ++i ) sequence[i] = *data++;
        }
    } else if( typie->IsEnum ) {
        return ReadTill( Convert::ChangeType( termi, typie->GetEnumUnderlyingType() ) );
    } else if( typie->IsArray ) {
        if( typie->GetElementType()->IsPrimitive ) {
            Array^ data = (Array^)termi;
            int leng = data->GetLength(0);
            int size = 0;
            byte* pntr = _getPointerToPrimitive( data->GetValue(0), &size );
            sequence = gcnew array<byte>( size * leng );
            int pos = 0;
            do { for( int i = 0; i < size; ++i ) sequence[(pos*size)+i] = *pntr++;
                if( ++pos < leng ) pntr = _getPointerToPrimitive( data->GetValue( ++pos ), NULL );
            } while( pos < leng );
        } else {
            // if some array of class objects ... call ToString() on each element and concatanate to one long string
            return ReadTill( termi->ToString() );
        } 
    } else {
        return ReadTill( termi->ToString() );
       // if some array of class objects ... either call ToString() on each element and concatanate all to one long string
       // or maybe serialize each element to bynary data structure representing its member field and property values for concatanating a byte[]
    }

    System::Text::StringBuilder^ buildi = gcnew System::Text::StringBuilder(0);
    StreamParser^ parsi = gcnew StreamParser( sequence );
    do { buildi->Append( parsi->Check( (byte)GetChar() ) );
    } while( !parsi->Found );
     
    return buildi->ToString();
}

String^
Consola::StdInp::Read( uint charCount )
{
    return systemStringFromStdIn( charCount );
}

generic<class T> where T : ValueType array<T>^
Consola::StdInp::Read( uint Tcount )
{
    return systemArrayFromStdIn<T>(Tcount);
}

generic<class T> where T : ValueType uint
Consola::StdInp::Read( array<T>^ destination )
{
    return readDataFromStdIn<T>( destination, 0, destination->Length );
}

generic<class T> where T : ValueType uint
Consola::StdInp::Read( array<T>^ dst, uint cToffset, uint cTlength )
{
    return readDataFromStdIn<T>( dst, cToffset, cTlength );
}

uint
Consola::StdInp::Read( IntPtr dst, uint cbOffset, uint cbSize )
{
    return rawDataFromStdIn( dst, cbOffset, cbSize );
}

int
Consola::StdInp::CanRead( void )
{
    int val = 0;
    uint key = keygenerator->Next( INT_MAX );
    while ( !lockup(key) ) Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    val = ftell( stdin );
    unlock( key );
    return val;
}

bool
Consola::StdInp::IsInput::get(void)
{
    return CanRead() > 0;
}

wchar_t
Consola::StdInp::GetChar( void )
{
    uint key = keygenerator->Next( INT_MAX );
    while ( !lockup( key ) ) Thread::Sleep( THREAD_WAITSTATE_CYCLE_TIME );
    wchar_t c = getchar();
    if( log != nullptr ) {
        log->Write(c);
        log->Flush();
    } unlock( key );
    return getchar();
}

void
Consola::OutStream::WriteLine( String^ line )
{
    systemStringToStdOut( String::Format( "{0}\r\n", line ) );
}
void
Consola::OutStream::WriteLine( String^ format, ...array<Object^>^ parameter )
{
    systemStringToStdOut( String::Format( format + "\r\n", parameter ) );
}

void
Consola::OutStream::Write( String^ text )
{
    systemStringToStdOut( text );
}
void
Consola::OutStream::Write( String^ format, ...array<Object^>^ parameter )
{
    systemStringToStdOut( String::Format( format, parameter ) );
}

bool
Consola::StdStream::HasConsole( void )
{
    return GetConsoleWindow() > HWND(0);
};

generic<class T> where T : ValueType void
Consola::OutStream::Write( array<T>^ data )
{
    if ( Type::GetTypeCode( T::typeid ) == TypeCode::Byte ) {
        Object^ blah = data;
        dataArrayToStdOut( safe_cast<array<byte>^>(blah) );
    } else {
        systemArrayToStdOut<T>( data, 0, data->Length );
    }
}

generic<class T> where T : ValueType void
Consola::OutStream::Write( array<T>^ data, int offsetTs, int contOnTs )
{
    systemArrayToStdOut<T>( data, offsetTs, contOnTs );
}

void
Consola::OutStream::Write( IntPtr data, int cbOffset, int cbSize )
{
    rawDataToStdOut( data, cbOffset, cbSize );
}
