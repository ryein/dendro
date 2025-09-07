# Minimum CMake version required by presets (CMakePresets.json uses 3.23) and features we rely on.
cmake_minimum_required(VERSION 3.23)

# Prefer <Package>Config.cmake over Find<Package>.cmake when both exist (better with vcpkg).
set(CMAKE_FIND_PACKAGE_PREFER_CONFIG ON)

# Use MSVC runtime library selection via target property CMAKE_MSVC_RUNTIME_LIBRARY (CMake 3.15+).
# Windows preset sets this to MultiThreaded for static CRT builds.
if(POLICY CMP0091)
  cmake_policy(SET CMP0091 NEW)
endif()

# Allow overriding the project version from presets/tooling; defaults to 0.0.0 otherwise.
# (Windows/macOS presets set DENDRO_VERSION_OVERRIDE to 2.0.0.)
if(NOT DEFINED DENDRO_VERSION_OVERRIDE)
  set(DENDRO_VERSION_OVERRIDE "0.0.0")
endif()

# Project declaration; version taken from override above; we only build C++.
project(DendroAPI VERSION ${DENDRO_VERSION_OVERRIDE} LANGUAGES CXX)

# Guard: only 64-bit builds are supported on Windows.
if(WIN32 AND NOT CMAKE_SIZEOF_VOID_P EQUAL 8)
  message(FATAL_ERROR "Only x64 builds are supported on Windows.")
endif()

# Collect sources and public headers.
# GLOB with CONFIGURE_DEPENDS lets CMake re-scan when files are added (on re-configure).
file(GLOB_RECURSE DENDROAPI_SOURCES CONFIGURE_DEPENDS
  "${CMAKE_CURRENT_SOURCE_DIR}/src/*.[ch]pp"
  "${CMAKE_CURRENT_SOURCE_DIR}/src/*.[cC][cx]"
)
file(GLOB_RECURSE DENDROAPI_HEADERS CONFIGURE_DEPENDS
  "${CMAKE_CURRENT_SOURCE_DIR}/include/*.h"
  "${CMAKE_CURRENT_SOURCE_DIR}/include/*.hpp"
  "${CMAKE_CURRENT_SOURCE_DIR}/include/*.hh"
)

# Nice IDE grouping mirroring the source tree.
source_group(TREE "${CMAKE_CURRENT_SOURCE_DIR}" FILES ${DENDROAPI_SOURCES} ${DENDROAPI_HEADERS})

# Dependencies (resolved via config packages; vcpkg pins versions in vcpkg.json).
find_package(OpenVDB CONFIG REQUIRED)
find_package(TBB CONFIG REQUIRED)
find_package(Eigen3 CONFIG REQUIRED)
find_package(ZLIB REQUIRED)
find_package(libigl CONFIG REQUIRED)
find_package(Blosc CONFIG REQUIRED)

# libigl has multiple exported target names depending on how it was built.
# Try common options in order and record the first that exists.
set(_IGL_TARGET "")

foreach(t IN ITEMS igl::core igl::igl libigl::igl)
  if(TARGET ${t})
    set(_IGL_TARGET ${t})
    break()
  endif()
endforeach()

# If no imported target exists (e.g., header-only libigl or custom layout), synthesize a header-only interface target.
# This code also supports vcpkg installs by probing VCPKG_INSTALLED_DIR/VCPKG_TARGET_TRIPLET/include.
if(NOT _IGL_TARGET)
  if(DEFINED VCPKG_INSTALLED_DIR AND DEFINED VCPKG_TARGET_TRIPLET)
    set(_IGL_HINT "${VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}")
  endif()

  find_path(LIBIGL_INCLUDE_DIR igl/igl_inline.h
    HINTS "${_IGL_HINT}" "${_IGL_HINT}/.." PATH_SUFFIXES include)

  if(NOT LIBIGL_INCLUDE_DIR)
    message(FATAL_ERROR "libigl includes not found")
  endif()

  add_library(igl_core_headeronly INTERFACE)
  target_include_directories(igl_core_headeronly INTERFACE "${LIBIGL_INCLUDE_DIR}")
  add_library(igl::core ALIAS igl_core_headeronly)
  set(_IGL_TARGET igl::core)
  message(STATUS "libigl: synthesized igl::core at ${LIBIGL_INCLUDE_DIR}")
endif()

# Normalize Blosc imported target name across different package layouts.
set(_BLOSC_TARGET Blosc::blosc)

if(NOT TARGET ${_BLOSC_TARGET})
  foreach(tgt IN ITEMS blosc::blosc blosc blosc_shared blosc_static)
    if(TARGET ${tgt})
      set(_BLOSC_TARGET ${tgt})
      break()
    endif()
  endforeach()

  if(NOT TARGET ${_BLOSC_TARGET})
    message(FATAL_ERROR "Blosc found, but no known imported targets are present.")
  endif()
endif()

# Build the shared library for DendroAPI (even if BUILD_SHARED_LIBS is OFF, we explicitly choose SHARED).
# Presets request static third-party linkage on Windows, but our library itself is shared.
add_library(DendroAPI SHARED ${DENDROAPI_SOURCES} ${DENDROAPI_HEADERS})
add_library(DendroAPI::DendroAPI ALIAS DendroAPI)

# Require C++17 and build with PIC for all platforms (helps if linked into other shared libs).
target_compile_features(DendroAPI PUBLIC cxx_std_17)
set_target_properties(DendroAPI PROPERTIES POSITION_INDEPENDENT_CODE ON)

# MSVC-specific: avoid macro pollution and large obj limits.
if(MSVC)
  target_compile_definitions(DendroAPI PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN)
  target_compile_options(DendroAPI PRIVATE /bigobj)
endif()

# Public include dirs: headers are under include/ (exported) and include/ in build tree (for IDEs).
target_include_directories(DendroAPI
  PUBLIC
  $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
  $<INSTALL_INTERFACE:include>
)

# Link deps:
# - PUBLIC: headers/types used by consumers (Eigen, libigl)
# - PRIVATE: implementation details (OpenVDB, TBB, ZLIB, Blosc)
target_link_libraries(DendroAPI
  PUBLIC Eigen3::Eigen ${_IGL_TARGET}
  PRIVATE OpenVDB::openvdb TBB::tbb ZLIB::ZLIB ${_BLOSC_TARGET}
)

# If building against vcpkg's windows-static triplet, tell OpenVDB we expect static libs.
if(WIN32 AND DEFINED VCPKG_TARGET_TRIPLET AND VCPKG_TARGET_TRIPLET MATCHES "windows-static")
  target_compile_definitions(DendroAPI PRIVATE OPENVDB_STATICLIB)
endif()

# Versioning and output directories: put all artifacts in build/out/<config>/ to keep things tidy.
set_target_properties(DendroAPI PROPERTIES
  VERSION ${PROJECT_VERSION}
  SOVERSION ${PROJECT_VERSION_MAJOR}
  RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out/$<CONFIG>"
  LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out/$<CONFIG>"
  ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out/$<CONFIG>"
)

# Export compile-time version macros and an internal build tag.
target_compile_definitions(DendroAPI PRIVATE
  DENDRO_VERSION_MAJOR=${PROJECT_VERSION_MAJOR}
  DENDRO_VERSION_MINOR=${PROJECT_VERSION_MINOR}
  DENDRO_VERSION_PATCH=${PROJECT_VERSION_PATCH}
  DENDROAPI_BUILD
)
