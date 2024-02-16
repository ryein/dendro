cmake_minimum_required(VERSION 3.10)
project(DendroAPI)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED True)

# Find Boost
find_package(Boost REQUIRED)

# Libraries to include and link against
include_directories(/usr/local/include
    ${Boost_INCLUDE_DIRS}
    /opt/homebrew/include
)
link_directories(/usr/local/lib
    /opt/homebrew/lib
)

add_library(DendroAPI SHARED
    DendroAPI.cpp
    DendroGrid.cpp
    DendroMesh.cpp
    dllmain.cpp
    stdafx.cpp
)

target_compile_definitions(DendroAPI PRIVATE
    OPENVDB_OPENEXR_STATICLIB
    OPENVDB_STATICLIB
    _USE_MATH_DEFINES
    NOMINMAX
    DENDROAPI_EXPORTS
)

target_link_libraries(DendroAPI
    openvdb
    tbb
    blosc
    ${Boost_LIBRARIES}
)

# Optional: Define post-build commands if needed
# add_custom_command(TARGET DendroAPI POST_BUILD ...)
