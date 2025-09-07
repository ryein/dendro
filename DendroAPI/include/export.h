#pragma once
#if defined(_WIN32) || defined(_MSC_VER)
#if defined(DENDROAPI_BUILD)
#define DENDRO_API __declspec(dllexport)
#else
#define DENDRO_API __declspec(dllimport)
#endif
#else
#define DENDRO_API __attribute__((visibility("default")))
#endif
