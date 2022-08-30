/*///////////////////////////////////////////////////////////*\
||                                                           ||
||     File:      ConsolaAuxilary.hpp                        ||
||     Author:    Autogenerated                              ||
||     Generated: 23.02.2022                                 ||
||                                                           ||
\*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/
#ifndef _ConsolaAuxilary_hpp_
#define _ConsolaAuxilary_hpp_

#using <System.Xml.dll>
using namespace System::Xml;


namespace Consola
{
    ref class AuxXml;
    ref class StdStream;

    public ref class AuxilaryStream
        : public StdStream
    {
    private:
        static int extendRaum( unsigned des, Direction how );
    
    protected:
        const unsigned typ;

    internal:
        static volatile uint lockvar = EMPTY;
       
        static array<unsigned>^        auxeen;
        static array<AuxilaryStream^>^ auxtrm;

        AuxilaryStream( unsigned name );
        virtual ~AuxilaryStream();

        virtual bool lockup( uint key ) override {
            if (lockvar == EMPTY) {
                lockvar = key;
            } return lockvar == key;
        }
        virtual bool unlock( uint key ) override {
            if (lockvar == key) {
                lockvar = EMPTY;
            } return lockvar == EMPTY;
        }
        virtual bool locked(void) override {
            return lockvar != EMPTY;
        }

    public:
        property LogWriter^ Log {
            virtual LogWriter^ get( void ) override;
            virtual void set( LogWriter^ value ) override;
        }

        virtual String^ ToString( void ) override {
            return String::Format( "{0}_{1}.log", this->nam, typ );
        }

        property AuxXml^ Xml {
            AuxXml^ get(void);
        }
    };

    public ref class AuxXml
        : public AuxilaryStream
    {
    internal:
        AuxXml( void );

    public:
        enum class State { 
            NoScope = 0, Document = 1, Element = 2,
            Attribute = 4, Content = 8, CData = 16,
            Comment = 32
        };

        AuxXml^ CloseScope( void );
        AuxXml^ CloseScope( String^ element );
        AuxXml^ WriteNode( XmlNode^ node );
        AuxXml^ WriteCData( String^ data );
        AuxXml^ WriteContent( String^ format, ...array<Object^>^ optjects );
        AuxXml^ WriteElement( String^ tagname, ...array<String^>^ opttributes );
        AuxXml^ WriteElement( String^ tagname, String^ contents, ...array<String^>^ opttributes );
        AuxXml^ WriteAttribute( String^ name, Object^ value );
        AuxXml^ WriteComment( String^ format, ...array<Object^>^ optjects );

        void    NewScope( State newScope );
        void    NewScope( State newScope, bool closeActualScope );

        void closeLog(void) override {
            while( statesCount > 0 && scope > State::NoScope ) {
                CloseScope();
            }
        }

        virtual property LogWriter^ Log {
            LogWriter^ get(void) override;
            void set(LogWriter^ logger) override;
        }

        property bool IsInput {
            bool get(void) { return false; }
        }

        property State Scope {
            State get(void) { return scope; }
        }

        property String^ Element {
            String^ get(void) {
                return state == nullptr 
                     ? String::Empty : state;
            }
        }

        property int Depth { 
            int get(void) { return statesCount-1; }
        }

    private:
        bool statesContains( String^ element );
        void pushState( String^ element );
        bool notabs;
        bool content;
        State scope;
        array<String^>^ states;
        int statesCount;
        String^ state;
    };

} //end of Consola

#endif
