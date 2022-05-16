/*///////////////////////////////////////////////////////////*\
||                                                           ||
||     File:      versionmacro.h                             ||
||     Author:    autogenerated                              ||
||     Generated: by Command Generator v.0.1                 ||
||                                                           ||
\*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/

#ifndef VERSION_STRING 
#define ulong unsigned long long int
#ifdef  SET_VERSION_STRING
#define VERSION_STRING SET_VERSION_STRING
#elif defined(SET_VERSION_NUMBER)
#define VERSION_NUMBER SET_VERSION_NUMBER
#elif defined(HEX_VERSION_NUMBER)
#define VERSION_NUMBER HEX_VERSION_NUMBER
#endif

#define DIGIT_SHIFTED(d,n,s) ( ( ( ( d / n ) % 10 ) + 48 ) << s )
#define VERSION_STRING_INTDEC( dn ) (const ulong) ( \
        DIGIT_SHIFTED( dn,10000000, 0 ) | (46 << 8) | \
        DIGIT_SHIFTED( dn, 100000, 16 ) | (46 << 24) | \
        DIGIT_SHIFTED( dn,   1000, 32 ) | (46 << 40) | \
        DIGIT_SHIFTED( dn,     10, 48 ) & 0x00ffffffffffffff )
         
#define DIGIT_ARRAYED(d,n) ( ( ( d / n ) % 10 ) + 48 )
#define VERSION_ARRAY_INTDEC( dn ) { \
        DIGIT_ARRAYED( dn,1000000 ), 46, \
        DIGIT_ARRAYED( dn, 10000 ), 46, \
        DIGIT_ARRAYED( dn,   100 ), 46, \
        DIGIT_ARRAYED( dn,     1 ), '\0' }

#define VERSION_LONGCC_INTDEC( dn ) (const ulong)( \
        ((((const ulong)dn/10000000ul)%10)+0x30) | \
        ((((const ulong)dn/1000000ul)%10)+0x30) << 8 | \
        ((((const ulong)dn/100000ul)%10)+0x30) << 16 | \
        ((((const ulong)dn/10000ul)%10)+0x30) << 24 | \
        ((((const ulong)dn/1000ul)%10)+0x30) << 32 | \
        ((((const ulong)dn/100ul)%10)+0x30) << 40 | \
        ((((const ulong)dn/10ul)%10)+0x30) << 48 | \
        ((((const ulong)dn/1ul)%10)+0x30) << 56 )

#define HEX_HEX_DIGIT( lcc, n ) \
        ((((lcc >> ((n+n)*8)) & 0x00000000000000ffu) - 0x30)*10) \
      + (((lcc >> ((n+n)*8)) & 0x000000000000ff00u) - 0x30)
#define VERSION_INTHEX_LONGCC( longcc ) (const unsigned) ( \
        ( HEX_HEX_DIGIT( longcc, 0 ) << 24 ) | \
        ( HEX_HEX_DIGIT( longcc, 1 ) << 16 ) | \
        ( HEX_HEX_DIGIT( longcc, 2 ) << 8 ) | \
        ( HEX_HEX_DIGIT( longcc, 3 ) << 0 ) )

#define VERSION_FOURCC_INTDEC( dn ) (const ulong)( \
        ((((const ulong)dn/1000000ul)%10)+0x30) << 0 | \
        ((((const ulong)dn/10000ul)%10)+0x30) << 8 | \
        ((((const ulong)dn/100ul)%10)+0x30) << 16 | \
        ((((const ulong)dn/1ul)%10)+0x30) << 24 )

#define VERSION_INTHEX_FOURCC(fourcc) (const unsigned)( \
        ( (   (fourcc&0x000000ffu)         - 0x30 ) << 24 ) | \
        ( ( ( (fourcc&0x0000ff00u) >> 8 )  - 0x30 ) << 16 ) | \
        ( ( ( (fourcc&0x00ff0000u) >> 16 ) - 0x30 ) << 8 ) | \
        ( ( ( (fourcc&0xff000000u) >> 24 ) - 0x30 ) ) )

#define VERSION_INTDEC_FOURCC(fourcc) (const unsigned)( \
        ( (   (fourcc&0x000000ffu)         - 0x30 ) * 1000000 ) + \
        ( ( ( (fourcc&0x0000ff00u) >> 8 )  - 0x30 ) * 10000 ) + \
        ( ( ( (fourcc&0x00ff0000u) >> 16 ) - 0x30 ) * 100 ) + \
        ( ( ( (fourcc&0xff000000u) >> 24 ) - 0x30 ) * 1) )

#define VERSION_FOURCC_LONGCC(longcc) (const unsigned)( \
        ( longcc & 0x00000000000000ff ) | \
        ( longcc & 0x0000000000ff0000 ) >> 8 | \
        ( longcc & 0x000000ff00000000 ) >> 16 | \
        ( longcc & 0x00ff000000000000 ) >> 24 )

#define VERSION_NUMBER_STRING( string, modus ) \
        VERSION_INT ## modus ## _FOURCC( VERSION_FOURCC_LONGCC( *(const ulong*)string ) )


#if (!defined( VERSION_NUMBER )) || defined(HEX_VERSION_NUMBER)
#ifdef  HEX_VERSION_NUMBER
#define VERSION_HEXNUM \
        VERSION_INTHEX_LONGCC( VERSION_LONGCC_INTDEC( VERSION_NUMBER ) )
#define VERSION_STRING \
        VERSION_ARRAY_INTDEC( VERSION_NUMBER )
#else
#define VERSION_NUMBER \
        VERSION_NUMBER_STRING( VERSION_STRING, DEC )
#define VERSION_HEXNUM \
        VERSION_NUMBER_STRING( VERSION_STRING, HEX )
//#define string_string (1)
#endif
#else
#define VERSION_HEXNUM \
        VERSION_INTHEX_FOURCC( VERSION_FOURCC_INTDEC( VERSION_NUMBER ) ) 
#define VERSION_STRING \
        VERSION_ARRAY_INTDEC( VERSION_NUMBER )
#endif

#undef ulong
#endif
