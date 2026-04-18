cmake_minimum_required(VERSION 3.15)
project(TimeMeaning DLL)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)

add_library(TimeMeaning SHARED
    GetTimeMeaning.cpp
)

target_include_directories(TimeMeaning PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

if(WIN32)
    target_compile_definitions(TimeMeaning PRIVATE _UNICODE UNICODE)
endif()