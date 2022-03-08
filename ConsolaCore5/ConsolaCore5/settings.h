#ifndef ProjectSettingsIncluded
#define ProjectSettingsIncluded


#ifndef CONSOLA_VERSION_NUMBER
#define CONSOLA_VERSION(ma,mi,re,bu) ( \
     (bu&0xff) | ((re&0xff)<<8) | \
    ((mi&0xff)<<16) | ((ma&0xff)<<24) \
)
#define CONSOLA_VERSION_NUMBER \
        CONSOLA_VERSION(0,0,1,5)
#define CONSOLA_VERSION_STRING \
                       "0.0.1.5"
#endif

#ifdef _DEBUG
#undef  DEBUG
#define CYCLE_COUNT (16)
#else
#define CYCLE_COUNT (32)
#endif
#define DEBUG (0)

#define SET_NAMESPACE (-1)
#define NO_CHECKPOINT_MODE

#define THREAD_WAITSTATE_CYCLE_TIME (3)
#define CYCLE_SIZE (1024)
#define COMMANDLINER_EXTERN_C extern "C" {
#define COMMANDCLOSER_EXTERN_C }

#define _CRT_SECURE_NO_WARNINGS

#endif