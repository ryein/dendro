// DendroAPI.cpp : Defines the exported functions for the DLL application.
#include "DendroAPI.h"

#include "DendroParticle.h"
#include "DendroMesh.h"
#include <openvdb/util/Util.h>
#include <vector>
#include <cstdlib>
#include <cstring>

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
DENDRO_API bool DendroFromPoints(DendroGrid *grid, const NativePoint *vPoints, size_t pCount, const double *vRadius, int rCount, double voxelSize, double bandwidth)
{
	std::vector<openvdb::Vec3R> particleList;
	particleList.reserve(pCount);

	if constexpr (std::is_same_v<openvdb::Real, double>)
	{
		// Real == double → layouts match (3 doubles) → memcpy
		particleList.resize(pCount);
		std::memcpy(particleList.data(), vPoints, pCount * sizeof(NativePoint));
	}
	else
	{
		// Real == float → single pass cast
		for (size_t i = 0; i < pCount; ++i)
		{
			const auto &q = vPoints[i];
			particleList.emplace_back(
				openvdb::Real(q.x),
				openvdb::Real(q.y),
				openvdb::Real(q.z));
		}
	}

	DendroParticle ps;
	ps.clear();

	if (particleList.size() == rCount)
	{

		int i = 0;
		for (auto it = particleList.begin(); it != particleList.end(); ++it)
		{
			ps.add((*it), openvdb::Real(vRadius[i]));
			i++;
		}
	}
	else
	{

		double average = 0.0;
		for (int i = 0; i < rCount; i++)
		{
			average += vRadius[i];
		}
		average /= rCount;
		openvdb::Real radius = openvdb::Real(average);

		for (auto it = particleList.begin(); it != particleList.end(); ++it)
		{
			ps.add((*it), radius);
		}
	}

	return grid->CreateFromPoints(ps, voxelSize, bandwidth);
}

DENDRO_API bool DendroFromMesh(DendroGrid *grid, const NativePoint *vPoints, int vCount, const NativeFace *vFaces, int fCount, double voxelSize, double bandwidth)
{
	if (!grid || !vPoints || !vFaces)
		return false;

	std::vector<openvdb::Vec3d> vertices(vCount);
	std::memcpy(vertices.data(), vPoints, vCount * sizeof(NativePoint));

	std::vector<openvdb::Vec3I> triangles;
	triangles.reserve(fCount);
	for (int i = 0; i < fCount; ++i)
	{
		const auto &f = vFaces[i];
		triangles.emplace_back(f.a, f.b, f.c);
	}

	std::vector<openvdb::Vec4I> quads;

	return grid->CreateFromMesh(vertices, triangles, quads, voxelSize, bandwidth);
}

DENDRO_API bool DendroToMesh(DendroGrid *grid, NativePoint **vPoints, int *vCount, NativeFace **vFaces, int *fCount, double isovalue, double adaptivity)
{
	if (!grid || !vPoints || !vCount || !vFaces || !fCount)
		return false;

	std::vector<openvdb::Vec3d> vertices;
	std::vector<openvdb::Vec3I> triangles;
	std::vector<openvdb::Vec4I> quads;

	grid->ToMesh(vertices, triangles, quads, isovalue, adaptivity);

	size_t vertCount = vertices.size();
	size_t triCount = triangles.size() + quads.size() * 2;

	NativePoint *pVerts = reinterpret_cast<NativePoint *>(malloc(vertCount * sizeof(NativePoint)));
	NativeFace *pFaces = reinterpret_cast<NativeFace *>(malloc(triCount * sizeof(NativeFace)));

	if (!pVerts || !pFaces)
	{
		free(pVerts);
		free(pFaces);
		return false;
	}

	for (size_t i = 0; i < vertices.size(); ++i)
	{
		const auto &v = vertices[i];
		pVerts[i].x = v.x();
		pVerts[i].y = v.y();
		pVerts[i].z = v.z();
	}

	size_t idx = 0;
	for (const auto &t : triangles)
	{
		pFaces[idx].a = t[0];
		pFaces[idx].b = t[1];
		pFaces[idx].c = t[2];
		++idx;
	}
	for (const auto &q : quads)
	{
		pFaces[idx].a = q[0];
		pFaces[idx].b = q[1];
		pFaces[idx].c = q[2];
		++idx;
		pFaces[idx].a = q[0];
		pFaces[idx].b = q[2];
		pFaces[idx].c = q[3];
		++idx;
	}

	*vPoints = pVerts;
	*vFaces = pFaces;
	*vCount = static_cast<int>(vertCount);
	*fCount = static_cast<int>(triCount);

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