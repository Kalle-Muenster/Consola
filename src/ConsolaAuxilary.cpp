/*///////////////////////////////////////////////////////////*\
||                                                           ||
||     File:      ConsolaAuxilary.cpp                        ||
||     Author:    Autogenerated                              ||
||     Generated: 23.02.2022                                 ||
||                                                           ||
\*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/
#include <settings.h>
#include <.byteOrder.h>
 
using namespace   System;
using namespace   System::IO;
using namespace   System::Threading::Tasks;
using namespace   System::Threading;


#include "ConsolaLogger.hpp"
#include "ConsolaStream.hpp"
#include "ConsolaAuxilary.hpp"
#include <enumoperators.h>


#define TABS if( notabs ) notabs = false; else for( int i = 0; i < Depth; ++i ) log->Write( "    " )


Consola::AuxilaryStream::AuxilaryStream( fourCC name )
    : StdStream( Direction::Aux )
    , typ( name )
{
    if( auxeen == nullptr ) {
        auxeen = gcnew array<fourCC>(1) { name };
        auxtrm = gcnew array<AuxilaryStream^>(1) { this };
        aux = this;
    } else
        auxtrm[extendRaum( typ, Direction::Inp )] = this;
}

Consola::AuxilaryStream::~AuxilaryStream()
{
    extendRaum( typ, Direction::Out );
}

Consola::AuxXml::AuxXml(void)
    : AuxilaryStream( byteOrder_stringTOfourCC("Xml") )
{
    scope = State::NoScope;
    state = nullptr;
    states = gcnew System::Collections::Generic::List<String^>(4);
    nocontent = notabs = false;
}

Consola::AuxXml^
Consola::AuxilaryStream::Xml::get(void)
{
    for( int idx = 0; idx < auxeen->Length; ++idx )
        if( auxeen[idx] == byteOrder_stringTOfourCC("Xml") )
            return (AuxXml^)auxtrm[idx];
    return nullptr;
}

int
Consola::AuxilaryStream::extendRaum( unsigned des, Direction how) {
    int size = 0;
    array<AuxilaryStream^>^ raumExtender = nullptr;
    array<unsigned>^ nameExtender = nullptr;
    switch (how) {
    case Direction::Out: {
        size = auxeen->Length;
        raumExtender = gcnew array<AuxilaryStream^>( auxtrm->Length + 1 );
        nameExtender = gcnew array<unsigned>( auxeen->Length + 1 );
        auxtrm->CopyTo( raumExtender, 0 );
        auxeen->CopyTo( nameExtender, 0 );
        auxeen[size] = des;
    } break;
    case Direction::Inp: {
        if (auxtrm->Length > 1) {
            size = auxeen->Length - 1;
            raumExtender = gcnew array<AuxilaryStream^>( auxtrm->Length - 1 );
            nameExtender = gcnew array<unsigned>( auxeen->Length - 1 );
            for( int i = size, n = size - 1; i >= 0; --i, --n ) {
                if( auxeen[i] != des ) {
                    raumExtender[n] = auxtrm[i];
                    nameExtender[n] = auxeen[i];
                } else size = n++;
            }
        }
        else {
            size = 0;
        }
    } break;
    default: how = Direction::Err; size = 0; break;
    }
    auxtrm = raumExtender;
    auxeen = nameExtender;
    return size;
}

Consola::AuxXml^
Consola::AuxXml::WriteContent( System::String^ format, ...array<Object^>^ content )
{
    if( scope != State::Content ) {
        NewScope(State::Content);
    }
    Log->Write( format, content );
    Log->Flush();
    notabs = true;
    return this;
}

Consola::AuxXml^
Consola::AuxXml::WriteElement( String^ tagname, ...array<String^>^ attribute )
{
    NewScope( State::Element, false );
    states->Add( state = tagname );
    TABS; log->Write( String::Format( "<{0}", tagname ) );
    scope = State::Attribute;
    if( attribute->Length == 0 ) log->Flush();
    for( int i = 0; i < attribute->Length; ++i ) {
        String^ a = attribute[i]->ToString();
        if( a->Contains("=") ) {
            array<String^>^ kv = a->Split( '=' );
            WriteAttribute( kv[0], kv->Length > 1
                          ? kv[1]: String::Empty );
        } else {
            WriteAttribute( a, nullptr );
        }
    } return this;
}

Consola::AuxXml^
Consola::AuxXml::WriteAttribute( String^ name, Object^ value )
{
    if (scope == State::Element) scope = State::Attribute;
    if (scope == State::Attribute) {
        log->Write( " " + name );
        if ( value ) log->Write( "=\"" + value->ToString() + "\"" );
        log->Flush();
    } return this;
}

void
Consola::AuxXml::WriteNode( System::Xml::XmlNode^ node )
{
    if ( enum_utils::anyFlag( State::Content|State::Element|State::Attribute, scope ) ) { // == State::Content || scope == State::Element || scope == State::Attribute ) {
        NewScope( State::Content );
    } TABS;
    log->WriteLine( node->OuterXml );
    log->Flush();
}

Consola::AuxXml^
Consola::AuxXml::CloseScope( void )
{
    //if (scope == State::Content) TABS;
    switch( scope ) {
    case State::Content: TABS;
    case State::Attribute:
    case State::Element: {
        NewScope( Depth >= 0 ? State::Content : State::Document );
    } break;
    case State::Document: {
        scope = State::NoScope;
        state = nullptr;
        states->Clear();
        log->Flush();
        log->Close();
    } break;
    } return this;
}

Consola::AuxXml^
Consola::AuxXml::CloseScope( String^ element )
{
    if( states->Contains( element ) ) {
        while (Element != element) CloseScope();
        if (Element == element) CloseScope();
    } return this;
}

void
Consola::AuxXml::NewScope( State newScope, bool closeActual )
{
    if( scope == State::NoScope ) {
        scope = State::Document;
        log->Write( "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" );
    } bool closeCurrentScope = false;

    switch( scope ) {
    case State::Attribute:
        if( !enum_utils::anyFlag( State::Content|State::Comment, newScope ) ) { // !( (newScope & (State::Content|State::Comment)) != State::NoScope ) ) {
            log->Write( "/" ); 
        } log->Write( ">" );
        if ( !enum_utils::hasFlag( newScope, State::Content) ) log->Write("\n");
        log->Flush();
        break;
    case State::Element:
        if( state != nullptr && newScope == State::Content ) {
            log->Write( String::Format( "</{0}>\n", state ) );
        } else log->Write( "/>\n" );
        log->Flush();
        closeCurrentScope = closeActual;
        break;
    case State::Comment:
        log->Write( " -->\n" );
        log->Flush();
        closeCurrentScope = closeActual;
        break;
    case State::Content:
        if( closeActual ) {
            log->Write( "</{0}>\n", state );
            log->Flush();
            closeCurrentScope = closeActual;
        } break;
    case State::CData:
        log->Write( "\"]>\n" );
        log->Flush();
        closeCurrentScope = closeActual;
        break;
    } scope = newScope;

    if( closeCurrentScope ) {
        states->RemoveAt( Depth );
        if( states->Count > 0 ) {
            state = states->ToArray()[Depth];
        } else state = nullptr;
    } 
}

void
Consola::AuxXml::NewScope( State newScope )
{
    NewScope( newScope, true );
}

Consola::LogWriter^
Consola::AuxilaryStream::Log::get( void )
{
    return log == nullptr
         ? createLog() : log;
}
void
Consola::AuxilaryStream::Log::set( LogWriter^ value )
{
    if( ( (log != nullptr) && (log != value) ) || (value == nullptr) ) {
        closeLog();
    } log = value;
}


Consola::LogWriter^
Consola::AuxXml::Log::get( void )
{
    return log == nullptr
         ? createLog()
         : log;
}
void
Consola::AuxXml::Log::set( LogWriter^ logger )
{
    if (logger == nullptr) {
        closeLog();
    } else {
        log = logger;
    }
}
