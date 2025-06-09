#!/bin/bash
set -e

# ============================
# Configuration and Constants
# ============================
if [ -z "$1" ]; then
  echo "Usage: $0 /path/to/vcpkg"
  exit 1
fi

VCPKG_PATH="$1"

GENERATOR="Unix Makefiles"
if command -v ninja &> /dev/null; then
  GENERATOR="Ninja"
fi

BUILD_DIR_ARM64="./builds_arm64"
BUILD_DIR_X86="./builds_x86_64"

export PATH="/opt/homebrew/bin:$PATH"
export CC=/usr/bin/clang
export CXX=/usr/bin/clang++
export SDKROOT=$(xcrun --sdk macosx --show-sdk-path)
export CMAKE_MAKE_PROGRAM=$(which ninja)


# ====================
# Build for arm64
# ====================
echo "🔧 Building for arm64..."
rm -rf "$BUILD_DIR_ARM64"

# Since the OPenVDB doesn't work with vcpkg on arm64, we are forcing the triplet to arm64-osx-release
echo "📦 Installing vcpkg dependencies for arm64-osx..."
"${VCPKG_PATH}/vcpkg" install --triplet arm64-osx


cmake -B "$BUILD_DIR_ARM64" -S . \
  -G Ninja \
  -DCMAKE_TOOLCHAIN_FILE="${VCPKG_PATH}/scripts/buildsystems/vcpkg.cmake" \
  -DVCPKG_TARGET_TRIPLET=arm64-osx \
  -DCMAKE_OSX_ARCHITECTURES=arm64 \
  -DCMAKE_OSX_SYSROOT="$SDKROOT" \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=10.15 \
  -DCMAKE_MAKE_PROGRAM="$NINJA_BIN" \
  -DCMAKE_C_COMPILER="$CC" \
  -DCMAKE_CXX_COMPILER="$CXX" \
  -DARCH_SUFFIX="_arm"

cmake --build "$BUILD_DIR_ARM64" -- -v -j15

# ====================
# Build for x86_64
# ====================
echo "🔧 Building for x86_64..."
rm -rf "$BUILD_DIR_X86"

# Since the OPenVDB doesn't work with vcpkg on x64_86, we are forcing the triplet to x64-osx
echo "📦 Installing vcpkg dependencies for x64-osx..."
"${VCPKG_PATH}/vcpkg" install --triplet=x64-osx


cmake -B "$BUILD_DIR_X86" -S . \
  -G "$GENERATOR" \
  -DCMAKE_TOOLCHAIN_FILE="${VCPKG_PATH}/scripts/buildsystems/vcpkg.cmake" \
  -DVCPKG_TARGET_TRIPLET=x64-osx \
  -DCMAKE_OSX_ARCHITECTURES=x86_64 \
  -DCMAKE_OSX_SYSROOT="$SDKROOT" \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=10.15 \
  -DCMAKE_C_COMPILER="$CC" \
  -DCMAKE_CXX_COMPILER="$CXX" \
  -DCMAKE_BUILD_TYPE=Release \
  -DARCH_SUFFIX="_x86_64"

cmake --build "$BUILD_DIR_X86"

# ====================
# Output Summary
# ====================
echo "✅ Build completed. Output bundles:"
echo " - ${BUILD_DIR_ARM64}/DendroAPI_macos_arm.bundle"
echo " - ${BUILD_DIR_X86}/DendroAPI_macos_x86_64.bundle"
