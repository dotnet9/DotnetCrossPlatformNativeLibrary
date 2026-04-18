#pragma once

#ifdef _WIN32
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif

extern "C" API const char* GetTimeMeaning(int timestampSecond);