# 🚧 Dendro v2 (Work in Progress)

**⚠️ This is not the main branch.**  
The `v2` branch is under active development and is **not yet functional**.  
If you are looking for a usable version of Dendro, please switch to the `main` branch.

---

## Status

- Current focus: build system overhaul and feature development.
- Expect broken builds, incomplete components, and experimental changes.
- Do not use this branch for production.

---

## Goals & Features in Development

### Core & Compatibility

- New **CMakePresets + vcpkg** build system with reproducible cross-platform builds.
- Dependency pinning and automatic versioning.
- Presets for Windows (x64), macOS Intel, and macOS Apple Silicon.
- Scripts for bootstrap + prerequisite install.
- Food4Rhino/GitHub distributables (signed/notarized zips, Yak package, one-click Actions).
- Component GUID map + automatic upgrader for old → new nodes.
- Clean up P/Invoke logic and implement performance tweaks

### Components

- **Capsule SDF** – pill primitive.
- **Tapered Capsule SDF** – variable radii ends.
- **Tube Complex SDF** – branching tube network.
- **Dilated Mesh SDF** – mesh→volume conversion with inflation.

### Meshing

- **IsoSurface: VDB Adaptive** – OpenVDB mesher with safe presets, pre-smooth, reprojection.
- **IsoSurface: Feature-Preserving** – dual contouring (Hermite gradients, libigl path).

### Visualization

- **Integrated OpenGL Preview** – removing need to generate Rhino mesh just for preview purposes.

---

## Roadmap

- Alpha: core build + basic components online.
- Beta: full feature set complete and stabilized.
- Final: merge into `main` as Dendro 2.0.
