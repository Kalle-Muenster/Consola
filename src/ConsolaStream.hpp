/*///////////////////////////////////////////////////////////*\
||                                                           ||
||     File:      ConsolaStream.hpp                          ||
||     Author:    Kalle                                      ||
||     Generated: 07.12.2018                                 ||
||                                                           ||
\*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/
#ifndef _ConsolaStream_hpp_
#define _ConsolaStream_hpp_

#include "ConsolaLogger.hpp"

using namespace System::Runtime;



namespace Consola
{
    ref class StdInp;
    ref class StdOut;
    ref class StdErr;
    ref class AuxXml;
    ref class OutStream;
    ref class AuxilaryStream;

    generic<class O> where O : OutStream 
    ref class Locked;

    [FlagsAttribute()]
    public enum class CreationFlags : uint { None = 0,
        UseConsole = 0x00000010, NewConsole = 0x00000020,
        TryConsole = 0x00000030, AppendLog = 0x00000001,
        NoInputLog = 0x00000004, CreateLog = 0x00000002,
    };

    public ref class StdStream
    {
    public:
        enum class Direction : uint {
            Inp,Out,Err,Non,Aux
        };

        static void Init( void );
        static void Init( String^ logfile );
        static void Init( CreationFlags flags );
        static void Init( CreationFlags flags, String^ logfile );
        static property String^ Cwd {
            String^ get(void);
            void set(String^ cd);
        }
        static DirectoryInfo^ CwdInfo() {
            return gcnew DirectoryInfo(Cwd);
        }
        static void    SetLogName( String^ logFile );
        static uint    VersionNumber();
        static String^ VersionName();
        static Int32   ProgramProc();
        static String^ ProgramName();
        static String^ MachineName();
        static String^ MachineArch();
        static bool    HasConsole( void );
        virtual       ~StdStream( void );

        property Direction StreamDirection {
            Direction get( void ) {
                return Direction( dir );
            }
        }
        property LogWriter^ Log {
            virtual LogWriter^ get( void ) {
                return log == nullptr
                     ? createLog() : log;
            }
            virtual void set( LogWriter^ value ) {
                if (((log != nullptr) && (log != value)) || (value == nullptr)) {
                    closeLog();
                } log = value;
            }
        }

        static property StdInp^ Inp {
            StdInp^ get(void) { return (StdInp^)inp; }
        }
        static property StdOut^ Out {
            StdOut^ get(void) { return (StdOut^)oup; }
        }
        static property StdErr^ Err {
            StdErr^ get(void) { return (StdErr^)err; }
        }

        static property AuxilaryStream^ Aux {
            AuxilaryStream^ get(void);
        }

        static operator LogWriter^( StdStream^ cast ) {
            return cast->Log;
        }

        void            closeLog();
        void            removeLog();
        LogWriter^      createLog();
        bool            existsLog();

    protected:
        static String^   asynchronStreamRead( Object^ );
        generic<class T> where T : ValueType
        static array<T>^ asynchronArrayRead( Object^ );
        generic<class T> where T : ValueType
        static uint      asynchronDataRead( Object^ );
        static uint      asynchronRawDataRead( Object^ );
        static void      asynchronStreamWrite( Object^ );
        static void      asynchronDataWrite( Object^ );
        static void      asynchronRawDataWrite( Object^ );
        static void      asynchronStreamGetLocked( Object^ );
        generic<class T> where T : ValueType
        static void      asynchronArrayWrite( Object^ );

        
        static StdStream^  inp;
        static StdStream^  oup;
        static StdStream^  err;
        static StdStream^  aux = nullptr;
        static String^     nam = nullptr;
        const  uint        dir;

        String^          systemStringFromStdIn( uint );
        generic<class T> where T : ValueType
        array<T>^        systemArrayFromStdIn( uint );
        generic<class T> where T : ValueType
        uint             readDataFromStdIn( array<T>^, uint, uint );
        uint             rawDataFromStdIn( IntPtr dst, int offset, int length );
        void             systemStringToStdOut( String^ );
        void             dataArrayToStdOut( array<byte>^ );
        generic<class T> where T : ValueType
        void             systemArrayToStdOut( array<T>^, int, int );
        void             rawDataToStdOut( IntPtr dst, int offset, int length );

    internal:
        static Random^ keygenerator;
        static void    CreateConsole(void);
        static void    RedirectStreams(void);
        LogWriter^     log;
                       StdStream( Direction );
        virtual bool   lockup(uint) abstract;
        virtual bool   unlock(uint) abstract;
        virtual bool   locked(void) abstract;
        static  bool   strmlockup(StdStream^ strm, uint);
        static  bool   strmunlock(StdStream^ strm, uint);

    private:

        static CreationFlags   consolestate = CreationFlags(-1);
        static CreationFlags   loggingstate = CreationFlags(-1);
        static unsigned        instanzencounter = 0;
    };


    public ref class StdInp : public StdStream
    {
    internal:
        static volatile uint lockvar = EMPTY;

        StdInp( void ) : StdStream( Direction::Inp ) {
            if( inp == nullptr ) inp = this;
        }
        virtual bool lockup(uint key) override {
            if (lockvar == EMPTY) {
                lockvar = key;
            } return lockvar == key;
        }
        virtual bool unlock(uint key) override {
            if (lockvar == key) {
                lockvar = EMPTY;
            } return lockvar == EMPTY;
        }
        virtual bool locked(void) override {
            return lockvar != EMPTY;
        }

    public:
        virtual ~StdInp( void ) { inp = nullptr; }
        property bool IsInput { bool get( void ); }
        int     CanRead( void );
        wchar_t GetChar( void );
        String^ ReadAll( void );
        String^ ReadLine( void );

        String^   Read( uint charCount );
        uint      Read( IntPtr dst, uint cbOffset, uint cbSize );
        generic<class T> where T : ValueType
        array<T>^ Read( uint countOnTs );
        generic<class T> where T : ValueType
             uint Read( array<T>^ destination );
        generic<class T> where T : ValueType
             uint Read( array<T>^ dst,
                        uint offsetTs,
                        uint countOnTs );

        virtual String^ ToString( void ) override {
            return this->ReadLine();
        }
        virtual property LogWriter^ Log {
        public: LogWriter^ get(void) override {
                return log == nullptr
                     ? createLog() : log;
            }
        public: void set( LogWriter^ logger ) override {
                if ( logger == nullptr ) {
                    closeLog();
                } else { log = logger; }
            }
        }
    };



    private value struct StreamLocker {
    private: 
        static System::Func<StdStream^,UInt32,bool>^   lockFunco;
        static System::Func<StdStream^,UInt32,bool>^   freeFunco;
        StdStream^                                     hocker;
        uint                                           locker;

    public:
        uint direct;
        static StreamLocker(void) {
            lockFunco = gcnew System::Func<StdStream^,UInt32,bool>(StdStream::strmlockup);
            freeFunco = gcnew System::Func<StdStream^,UInt32,bool>(StdStream::strmunlock);
        }
        StreamLocker( StdStream^ stream, uint keyid )
            : direct( (uint)((StdStream^)stream)->StreamDirection )
            , hocker( stream ) {
            locker = keyid;
        }
        bool up(void) {
            return lockFunco( (StdStream^)hocker, locker );
        }
        bool un(void) {
            return freeFunco( (StdStream^)hocker, locker );
        }
    };

    public interface class ILocked {
    public:
        ILocked^ Put( Object^ content );
        void End();
    };



    public ref class OutStream abstract : public StdStream
    {
    protected:
        static bool ErrAndOutSharedLock = true;
       
    internal:
        uint streamlocked = false;
        static volatile uint lockvar = EMPTY;
        virtual ILocked^ l() abstract;
        OutStream( Direction streamdirection )
            : StdStream( streamdirection ) {
        }
        virtual bool lockup( uint key ) override {
      const uint hocker = lockvar;
            if ( hocker ) lockvar = 0;
            else return false;
            if ( hocker == EMPTY )
                 lockvar = key;
            else lockvar = hocker;
            return lockvar == key;
        }
        virtual bool unlock( uint key ) override {
            uint hocker = lockvar;
            if (!hocker) return false;
            if ( hocker == key ) lockvar = EMPTY;
            return lockvar == EMPTY;
        }
        virtual bool locked( void ) override {
            const uint hocker = lockvar;
            return hocker != EMPTY;
        }
        void LockedStreamWrite( String^ data );

    protected:
        void okokpiereStreamLock( uint key ) {
            streamlocked = key;
        }

    public:
        void WriteLine( String^ line );
        void WriteLine(String^ format, ...array<Object^>^ parameter);
        void Write( String^ text );
        void Write(String^ format, ...array<Object^>^ parameter);
        generic<class T> where T : ValueType
        void Write(array<T>^ data);
        generic<class T> where T : ValueType
        void Write( array<T>^ data, int offsetTs, int countOnTs );
        void Write( IntPtr data, int cbOffset, int cbSize );
        property ILocked^ Stream {
            ILocked^ get(void);
        };
    };

    generic<class O>
        where O : OutStream
    public ref class Locked
        : public ILocked, IDisposable
    {
    internal:
        O okopierer;
        Locked( O kopierer ) {
            okopierer = kopierer;
        }
        operator O() { return this->okopierer; }
        virtual ILocked^ l() override { return this; }

    public:
        virtual void End() {
            switch ( okopierer->StreamDirection ) {
            case StdStream::Direction::Out: { if ( okopierer->unlock( okopierer->streamlocked ) )
                    okopierer->streamlocked = EMPTY;
                else throw gcnew System::Exception( "thread lock invalid" );
            } break;
            case StdStream::Direction::Err: { if ( okopierer->unlock( okopierer->streamlocked ) )
                    okopierer->streamlocked = EMPTY;
                else throw gcnew System::Exception( "thread lock invalid" );
            } break;
            }
        }
        Locked^ operator << ( Object^ object ) {
            okopierer->LockedStreamWrite( object->ToString() );
            return this;
        }
        virtual ILocked^ Put( Object^ data ) {
            return this << data->ToString();
        }
        property StdStream::Direction StreamDirection {
            StdStream::Direction get(void) {
                return okopierer->StreamDirection;
            }
        }
        virtual ~Locked(void) {
            End();
        }
    };


    public ref class StdOut sealed : public OutStream
    {
    internal:
        static volatile uint lockvar = EMPTY; 

        StdOut(void) : OutStream( Direction::Out ) {
            if (oup == nullptr) oup = this;
        }
        virtual bool     lockup( uint key ) override {
            if (ErrAndOutSharedLock) {
                return OutStream::lockup( key );
            } else if ( lockvar == EMPTY ) {
                lockvar = key;
            } return lockvar == key;
        }
        virtual bool     unlock( uint key ) override {
            if (ErrAndOutSharedLock) {
                return OutStream::unlock( key );
            } else if ( lockvar == key ) {
                lockvar = EMPTY;
            } return lockvar == EMPTY;
        }
        virtual bool     locked( void ) override {
            if (ErrAndOutSharedLock) {
                return OutStream::locked();
            } else return lockvar != EMPTY;
        }
        virtual ILocked^ l() override {
            return gcnew Locked<StdOut^>(this);
        }
        static operator ILocked^ (StdOut^ cast) {
            return  cast->l();
        }
    };



    public ref class StdErr sealed : public OutStream
    {
    internal:
        static volatile uint lockvar = EMPTY;
        StdErr(void) : OutStream(Direction::Err) {
            if (err == nullptr) err = this;
        }
        virtual bool     lockup(uint key) override {
            if (ErrAndOutSharedLock) {
                return OutStream::lockup(key);
            }
            else if (lockvar == EMPTY) {
                lockvar = key;
            } return lockvar == key;
        }
        virtual bool     unlock(uint key) override {
            if (ErrAndOutSharedLock) {
                return OutStream::unlock(key);
            }
            else if (lockvar == key) {
                lockvar = EMPTY;
            } return lockvar == EMPTY;
        }
        virtual bool     locked(void) override {
            if (ErrAndOutSharedLock) {
                return OutStream::locked();
            }
            else return lockvar != EMPTY;
        }
        virtual ILocked^ l() override {
            return gcnew Locked<StdErr^>(this);
        }
        static operator ILocked^ (StdErr^ cast) {
            return cast->l();
        }
    };

    public ref class StdStreams sealed : public StdStream
    {
    public:
        StdStreams()
            : StdStream( Direction::Non ) {
            nam = ProgramName() + "_{0}.log";
            StdStream::Init();
        }
        StdStreams( String^ logfile )
            : StdStream( Direction::Non ) {
            StdStream::Init( logfile );
        }
        StdStreams( CreationFlags createConsole )
            : StdStream( Direction::Non ) {
            nam = ProgramName() + "_{0}.log";
            StdStream::Init( createConsole );
        }
        StdStreams( CreationFlags flags, String^ logfile )
            : StdStream( Direction::Non ) {
            StdStream::Init( flags, logfile );
        }

        void closeLog(void) new;
        void removeLog(void) new;

        property StdInp^ Inp {
            StdInp^ get( void ) { return StdStream::Inp; }
        }
        property StdOut^ Out {
            StdOut^ get( void ) { return StdStream::Out; }
        }
        property StdErr^ Err {
            StdErr^ get( void ) { return StdStream::Err; }
        }

        property AuxilaryStream^ Aux {
            AuxilaryStream^ get(void) { return StdStream::Aux; }
        }

        property LogWriter^ Log {
            virtual LogWriter^ get( void ) override {
                if ( log == nullptr )
                     log = StdStream::Out->Log;
                return log;
            }
            virtual void set( LogWriter^ value ) override {
                if( value == nullptr ) {
                    this->closeLog();
                } else {
                    StdStream::Out->Log = value;
                    StdStream::Err->Log = value;
                }
            }
        }

    internal:
        virtual bool lockup(uint key) override {return false;};
        virtual bool unlock(uint key) override {return true;}
        virtual bool locked(void) override {return false;}

    };

}//end of Consola

//#include <.byteOrder.h>

#endif
