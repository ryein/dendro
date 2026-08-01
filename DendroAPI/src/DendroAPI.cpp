// DendroAPI.cpp : Defines the exported functions for the DLL application.
#include "DendroAPI.h"

#include <openvdb/util/Util.h>
#include <vector>
#include <cmath>
#include <cstdlib>
#include <cstring>
#include <limits>
#include <type_traits>

// grid class constructors
DENDRO_API DendroGrid *DendroCreate()
{
	DendroGrid *grid = new DendroGrid();
	return grid;
}

DENDRO_API void DendroDelete(DendroGrid *grid)
{
	if (grid != NULL)
	{
		delete grid;
		grid = NULL;
	}
}

DENDRO_API DendroGrid *DendroDuplicate(DendroGrid *grid)
{
	DendroGrid *dup = new DendroGrid(grid);
	return dup;
}

DENDRO_API bool DendroRead(DendroGrid *grid, const char *filename)
{
	return grid->Read(filename);
}

DENDRO_API bool DendroWrite(DendroGrid *grid, const char *filename)
{
	return grid->Write(filename);
}

// grid conversion methods
DENDRO_API bool DendroFromPoints(DendroGrid *grid, const NativePoint *vPoints, size_t pCount, const float *vRadius, size_t rCount, double voxelSize, double bandwidth)
{
	const double background = bandwidth * voxelSize;
	if (!grid || !vPoints || !vRadius || pCount == 0 ||
		(rCount != 1 && rCount != pCount) ||
		pCount > static_cast<size_t>(std::numeric_limits<openvdb::Index32>::max()) ||
		!std::isfinite(voxelSize) || voxelSize <= 0.0 ||
		!std::isfinite(bandwidth) || bandwidth <= 0.0 ||
		!std::isfinite(background) ||
		!std::isfinite(static_cast<float>(background)) ||
		static_cast<float>(background) <= 0.0f)
		return false;

	const double coordLimit = static_cast<double>(std::numeric_limits<openvdb::Int32>::max()) - 1.0;
	for (size_t i = 0; i < pCount; ++i)
	{
		const NativePoint &point = vPoints[i];
		if (!std::isfinite(point.x) || !std::isfinite(point.y) || !std::isfinite(point.z))
			return false;

		const float radius = vRadius[rCount == 1 ? 0 : i];
		if (!std::isfinite(radius) || radius <= 0.0f)
			return false;

		const double radiusInVoxels = static_cast<double>(radius) / voxelSize;
		const double extent = radiusInVoxels + bandwidth + 1.0;
		const double x = static_cast<double>(point.x) / voxelSize;
		const double y = static_cast<double>(point.y) / voxelSize;
		const double z = static_cast<double>(point.z) / voxelSize;
		if (!std::isfinite(radiusInVoxels) || !std::isfinite(extent) ||
			!std::isfinite(x) || !std::isfinite(y) || !std::isfinite(z) ||
			std::abs(x) + extent > coordLimit ||
			std::abs(y) + extent > coordLimit ||
			std::abs(z) + extent > coordLimit)
			return false;
	}

	try
	{
		NativeParticle plist(vPoints, pCount, vRadius, rCount);
		return grid->FromPoints(plist, voxelSize, bandwidth);
	}
	catch (...)
	{
		return false;
	}
}

DENDRO_API bool DendroFromMesh(DendroGrid *grid, const NativePoint *vPoints, int vCount, const NativeFace *vFaces, int fCount, double voxelSize, double bandwidth)
{
	// vertices
	static_assert(std::is_trivially_copyable<openvdb::Vec3f>::value, "Vec3f must be trivially copyable");
	static_assert(sizeof(openvdb::Vec3f) == sizeof(NativePoint), "Vec3f must match NativePoint");
	std::vector<openvdb::Vec3f> vertices(static_cast<size_t>(vCount));
	std::memcpy(vertices.data(), vPoints, static_cast<size_t>(vCount) * sizeof(NativePoint));

	// faces
	size_t triCount = 0, quadCount = 0;
	for (int i = 0; i < fCount; ++i)
	{
		const auto &f = vFaces[i];
		(f.d == f.c) ? ++triCount : ++quadCount;
	}
	std::vector<openvdb::Vec3I> triangles(triCount);
	std::vector<openvdb::Vec4I> quads(quadCount);

	size_t ti = 0, qi = 0;
	for (int i = 0; i < fCount; ++i)
	{
		const auto &f = vFaces[i];
		if (f.d == f.c)
			triangles[ti++] = openvdb::Vec3I(f.a, f.b, f.c);
		else
			quads[qi++] = openvdb::Vec4I(f.a, f.b, f.c, f.d);
	}

	return grid->FromMesh(vertices, triangles, quads, voxelSize, bandwidth);
}

DENDRO_API bool DendroFromCurves(DendroGrid *grid, const NativePoint *pts, size_t pCount, const NativeSegment *segs, size_t sCount, const float *radii, size_t rCount, double voxelSize, double bandwidth)
{
	if (!grid || !pts || !segs || !radii || pCount == 0 || sCount == 0 ||
		(rCount != 1 && rCount != sCount))
		return false;

	try
	{
		// reinterpret incoming buffers and copy into vectors
		const auto *vdbPts = reinterpret_cast<const openvdb::Vec3s *>(pts);
		const auto *vdbSeg = reinterpret_cast<const openvdb::Vec2I *>(segs);

		std::vector<openvdb::Vec3s> points(vdbPts, vdbPts + pCount);
		std::vector<openvdb::Vec2I> segments(vdbSeg, vdbSeg + sCount);
		std::vector<float> curveRadii(radii, radii + rCount);

		return grid->FromCurves(points, segments, curveRadii, voxelSize, bandwidth);
	}
	catch (...)
	{
		return false;
	}
}

// helper
static void packFaces(const std::vector<openvdb::Vec3I> &tris, const std::vector<openvdb::Vec4I> &quads, NativeFace *outFaces)
{
	size_t k = 0;

	for (const auto &t : tris)
	{
		outFaces[k++] = NativeFace{static_cast<int>(t.x()), static_cast<int>(t.y()), static_cast<int>(t.z()), static_cast<int>(t.z())};
	}

	for (const auto &q : quads)
	{
		outFaces[k++] = NativeFace{static_cast<int>(q.x()), static_cast<int>(q.y()), static_cast<int>(q.z()), static_cast<int>(q.w())};
	}
}

DENDRO_API bool DendroToMesh(DendroGrid *grid, NativePoint **vPoints, int *vCount, NativeFace **vFaces, int *fCount, double isovalue, double adaptivity)
{
	if (!grid || !vPoints || !vCount || !vFaces || !fCount)
		return false;

	// internal mesh data
	std::vector<openvdb::Vec3f> verts;
	std::vector<openvdb::Vec3I> tris;
	std::vector<openvdb::Vec4I> quads;

	grid->ToMesh(verts, tris, quads, isovalue, adaptivity);

	// allocate output buffers
	const size_t vN = verts.size();
	const size_t fN = tris.size() + quads.size();

	if (vN == 0 || fN == 0)
		return false;

	auto *vBuf = static_cast<NativePoint *>(std::malloc(vN * sizeof(NativePoint)));
	auto *fBuf = static_cast<NativeFace *>(std::malloc(fN * sizeof(NativeFace)));
	if (!vBuf || !fBuf)
	{
		std::free(vBuf);
		std::free(fBuf);
		return false;
	}

	// vertices
	static_assert(sizeof(openvdb::Vec3f) == sizeof(NativePoint), "Vec3f must match NativePoint layout");
	std::memcpy(vBuf, verts.data(), vN * sizeof(NativePoint));

	// faces
	packFaces(tris, quads, fBuf);

	// publish to caller
	*vPoints = vBuf;
	*vCount = static_cast<int>(vN);
	*vFaces = fBuf;
	*fCount = static_cast<int>(fN);
	return true;
}

DENDRO_API void DendroFree(void *ptr)
{
	if (ptr != nullptr)
	{
		free(ptr);
	}
}

// grid transformation methods
DENDRO_API bool DendroTransform(DendroGrid *grid, double *matrix, int mCount)
{
	if (mCount != 16)
	{
		return false;
	}

	openvdb::math::Mat4d xform = openvdb::math::Mat4d(matrix[0], matrix[1], matrix[2], matrix[3],
													  matrix[4], matrix[5], matrix[6], matrix[7],
													  matrix[8], matrix[9], matrix[10], matrix[11],
													  matrix[12], matrix[13], matrix[14], matrix[15]);

	grid->Transform(xform);

	return true;
}

// grid csg methods
DENDRO_API void DendroUnion(DendroGrid *grid, DendroGrid *csgGrid)
{
	grid->BooleanUnion(*csgGrid);
}

DENDRO_API void DendroDifference(DendroGrid *grid, DendroGrid *csgGrid)
{
	grid->BooleanDifference(*csgGrid);
}

DENDRO_API void DendroIntersection(DendroGrid *grid, DendroGrid *csgGrid)
{
	grid->BooleanIntersection(*csgGrid);
}

// grid filter methods
DENDRO_API void DendroOffset(DendroGrid *grid, double amount)
{
	grid->Offset(amount);
}

DENDRO_API void DendroOffsetMask(DendroGrid *grid, double amount, DendroGrid *mask, double min, double max, bool invert)
{
	grid->Offset(amount, *mask, min, max, invert);
}

DENDRO_API void DendroSmooth(DendroGrid *grid, int type, int iterations, int width)
{
	grid->Smooth(type, iterations, width);
}

DENDRO_API void DendroSmoothMask(DendroGrid *grid, int type, int iterations, int width, DendroGrid *mask, double min, double max, bool invert)
{
	grid->Smooth(type, iterations, width, *mask, min, max, invert);
}

DENDRO_API void DendroBlend(DendroGrid *bGrid, DendroGrid *eGrid, double bPosition, double bEnd)
{
	bGrid->Blend(*eGrid, bPosition, bEnd);
}

DENDRO_API void DendroBlendMask(DendroGrid *bGrid, DendroGrid *eGrid, double bPosition, double bEnd, DendroGrid *mask, double min, double max, bool invert)
{
	bGrid->Blend(*eGrid, bPosition, bEnd, *mask, min, max, invert);
}

// volume utilities
DENDRO_API float *DendroClosestPoint(DendroGrid *grid, float *vPoints, int vCount, int *rSize)
{
	std::vector<openvdb::Vec3R> points;
	std::vector<float> distances;

	int i = 0;
	while (i < vCount)
	{

		openvdb::Vec3R vertex(vPoints[i], vPoints[i + 1], vPoints[i + 2]);

		points.push_back(vertex);

		i += 3;
	}

	grid->ClosestPoint(points, distances);

	*rSize = points.size() * 3;

	float *pArray = reinterpret_cast<float *>(malloc(*rSize * sizeof(float)));

	i = 0;
	for (auto it = points.begin(); it != points.end(); ++it)
	{
		pArray[i] = it->x();
		pArray[i + 1] = it->y();
		pArray[i + 2] = it->z();
		i += 3;
	}

	return pArray;
}
