#pragma once

#include "export.h"
#include "DendroGrid.h"

#ifdef __cplusplus
extern "C"
{
#endif

	struct NativePoint
	{
		float x, y, z;
	};
	struct NativeFace
	{
		int a, b, c, d;
	};

	static_assert(sizeof(NativePoint) == 12, "NativePoint must be 12 bytes");
	static_assert(sizeof(NativeFace) == 16, "NativeFace must be 16 bytes");

	// ovdb volume class constructors
	extern DENDRO_API DendroGrid *DendroCreate();
	extern DENDRO_API void DendroDelete(DendroGrid *grid);
	extern DENDRO_API DendroGrid *DendroDuplicate(DendroGrid *grid);

	extern DENDRO_API bool DendroRead(DendroGrid *grid, const char *filename);
	extern DENDRO_API bool DendroWrite(DendroGrid *grid, const char *filename);

	// volume conversion methods
	extern DENDRO_API bool DendroFromPoints(DendroGrid *grid, const NativePoint *vPoints, size_t pCount, const double *vRadius, int rCount, double voxelSize, double bandwidth);
	extern DENDRO_API bool DendroFromMesh(DendroGrid *grid, const NativePoint *vPoints, int vCount, const NativeFace *vFaces, int fCount, double voxelSize, double bandwidth);
	extern DENDRO_API bool DendroToMesh(DendroGrid *grid, NativePoint **vPoints, int *vCount, NativeFace **vFaces, int *fCount, double isovalue, double adaptivity);
	extern DENDRO_API void DendroFree(void *ptr);

	// volume transformation methods
	extern DENDRO_API bool DendroTransform(DendroGrid *grid, double *matrix, int mCount);

	// volume csg methods
	extern DENDRO_API void DendroUnion(DendroGrid *grid, DendroGrid *csgGrid);
	extern DENDRO_API void DendroDifference(DendroGrid *grid, DendroGrid *csgGrid);
	extern DENDRO_API void DendroIntersection(DendroGrid *grid, DendroGrid *csgGrid);

	// volume filter methods
	extern DENDRO_API void DendroOffset(DendroGrid *grid, double amount);
	extern DENDRO_API void DendroOffsetMask(DendroGrid *grid, double amount, DendroGrid *mask, double min, double max, bool invert);
	extern DENDRO_API void DendroSmooth(DendroGrid *grid, int type, int iterations, int width);
	extern DENDRO_API void DendroSmoothMask(DendroGrid *grid, int type, int iterations, int width, DendroGrid *mask, double min, double max, bool invert);
	extern DENDRO_API void DendroBlend(DendroGrid *bGrid, DendroGrid *eGrid, double bPosition, double bEnd);
	extern DENDRO_API void DendroBlendMask(DendroGrid *bGrid, DendroGrid *eGrid, double bPosition, double bEnd, DendroGrid *mask, double min, double max, bool invert);

	// utilities and analysis
	extern DENDRO_API float *DendroClosestPoint(DendroGrid *grid, float *vPoints, int vCount, int *rSize);

#ifdef __cplusplus
}
#endif