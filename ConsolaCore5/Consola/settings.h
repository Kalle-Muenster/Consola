#ifndef ProjectSettingsIncluded
#define ProjectSettingsIncluded


#ifndef CONSOLA_VERSION
#define SET_VERSION_STRING "0.0.1.6"
#else
#define SET_VERSION_STRING CONSOLA_VERSION
#endif
#include <versionmacro.h>
#define CONSOLA_VERSION_NUMBER VERSION_NUMBER
#define CONSOLA_VERSION_STRING VERSION_STRING


#ifdef _DEBUG
#define CONSOLA_CONFIGU L"Debug"
#undef  DEBUG
#define CYCLE_COUNT (16)
#else
#define CONSOLA_CONFIGU L"Release"
#define CYCLE_COUNT (32)
#endif
#define DEBUG (0)


#define SET_NAMESPACE (-1)
#define NO_CHECKPOINT_MODE
#define COMMANDLINER_DISABLE_DEBUG (1)
#define THREAD_WAITSTATE_CYCLE_TIME (3)
#define CYCLE_SIZE (1024)
#define COMMANDLINER_EXTERN_C extern "C" {
#define COMMANDCLOSER_EXTERN_C }

#define _CRT_SECURE_NO_WARNINGS

#endif