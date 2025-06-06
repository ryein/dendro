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
    #/opt/homebrew/include
)
link_directories(/usr/local/lib
    #/opt/homebrew/lib
)

# Optional architecture suffix override (set via -DARCH_SUFFIX)
set(ARCH_SUFFIX "" CACHE STRING "Architecture suffix for output name")

# Set the source files
set(SOURCES
    DendroAPI.cpp
    DendroGrid.cpp
    DendroMesh.cpp
    dllmain.cpp
    stdafx.cpp
)

# Create target MacOS (DendroAPI_macos.bundle)

if(APPLE)
    add_library(DendroAPI MODULE ${SOURCES})

    set_target_properties(DendroAPI PROPERTIES
        BUNDLE TRUE
        MACOSX_BUNDLE TRUE
        SUFFIX ".bundle"
        OUTPUT_NAME "DendroAPI_macos${ARCH_SUFFIX}" # Name of the output library to contain the _macos suffix with optional architecture suffix (arm / x86_64)
    )

# Add library for Windows (DendroAPI.dll) and Linux(DendroAPI.so)
else()
    add_library(DendroAPI SHARED ${SOURCES})
endif()


# Preprocessor definitions
target_compile_definitions(DendroAPI PRIVATE
    OPENVDB_OPENEXR_STATICLIB
    OPENVDB_STATICLIB
    _USE_MATH_DEFINES
    NOMINMAX
    DENDROAPI_EXPORTS
	IMATH_HALF_NO_LOOKUP_TABLE
)

# Platform-specific linking
target_link_libraries(DendroAPI
    OpenVDB::openvdb
    TBB::tbb
    ${Boost_LIBRARIES}
)

# For Apple, link against blosc if available
if(APPLE)
    target_link_libraries(DendroAPI blosc_static)
endif()

# Windows-specific compile option
if (WIN32)
	set_source_files_properties(DendroGrid.cpp PROPERTIES COMPILE_OPTIONS "/bigobj")
endif()

# Position-independent code for shared objects and bundles
set_property(TARGET DendroAPI PROPERTY POSITION_INDEPENDENT_CODE ON)

# Optional: Define post-build commands if needed
# add_custom_command(TARGET DendroAPI POST_BUILD ...)
