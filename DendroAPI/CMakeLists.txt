cmake_minimum_required(VERSION 3.30)
project(DendroAPI)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED True)

# Find Boost
find_package(Boost REQUIRED)
find_package(OpenVDB REQUIRED)
find_package(TBB CONFIG REQUIRED)
find_package(blosc CONFIG REQUIRED)

# Mihai: these dirs don't seem to hurt, so I'll leave them here for the future
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
	IMATH_HALF_NO_LOOKUP_TABLE
)

target_link_libraries(DendroAPI
    OpenVDB::openvdb
    TBB::tbb
    ${Boost_LIBRARIES}
)
if (WIN32)
	set_source_files_properties(DendroGrid.cpp PROPERTIES COMPILE_OPTIONS "/bigobj")
endif()

# Optional: Define post-build commands if needed
# add_custom_command(TARGET DendroAPI POST_BUILD ...)
