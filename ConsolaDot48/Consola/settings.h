#ifndef ProjectSettingsIncluded
#define ProjectSettingsIncluded

#define SET_VERSION_STRING CONSOLA_VERSION
#include <versionmacro.h>
#define CONSOLA_VERSION_STRING VERSION_STRING
#define CONSOLA_VERSION_NUMBER VERSION_NUMBER

#if defined( __x86_64__) || defined(_WIN64)
#define CONSOLA_CPU_ARCHITECTURE L"x64"
#elif defined(_M_IX86) || defined(__x86__)
#define CONSOLA_CPU_ARCHITECTURE L"x86"
#endif

#ifdef _DEBUG
#undef  DEBUG
#define DEBUG (0)
#define CYCLE_COUNT (16)
#else
#define DEBUG (0)
#define CYCLE_COUNT (32)
#endif

#define NOT_USE_ENVIRONMENTOR
#define COMMANDLINER_DISABLE_DEBUG (1)
#define SET_NAMESPACE (-1)
#define NO_CHECKPOINT_MODE
#define COMMANDLINER_EXTERN_C extern "C" {
#define COMMANDCLOSER_EXTERN_C }

#define THREAD_WAITSTATE_CYCLE_TIME (3)
#define CYCLE_SIZE (1024)

#define _CRT_SECURE_NO_WARNINGS

#endif
