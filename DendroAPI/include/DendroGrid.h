#pragma once

#ifndef __DENDROGRID_H__
#define __DENDROGRID_H__

#include "DendroParticle.h"
#include "DendroMesh.h"

#define IMATH_HALF_NO_LOOKUP_TABLE

#include <openvdb/openvdb.h>
#include <vector>
#include <string>

class DendroGrid
{
public:
	DendroGrid();
	DendroGrid(DendroGrid *grid);
	~DendroGrid();

	openvdb::FloatGrid::Ptr Grid();

	bool Read(const char *vFile);
	bool Write(const char *vFile);

	bool CreateFromPoints(DendroParticle vPoints, double voxelSize, double bandwidth);
	bool FromMesh(const std::vector<openvdb::Vec3s> &vertices, const std::vector<openvdb::Vec3I> &triangles, const std::vector<openvdb::Vec4I> &quads, double voxelSize, double bandwidth);
	void ToMesh(std::vector<openvdb::Vec3s> &vertices, std::vector<openvdb::Vec3I> &triangles, std::vector<openvdb::Vec4I> &quads, double isovalue, double adaptivity);

	void Transform(openvdb::math::Mat4d xform);

	void BooleanUnion(DendroGrid vAdd);
	void BooleanIntersection(DendroGrid vIntersect);
	void BooleanDifference(DendroGrid vSubtract);

	void Offset(double amount);
	void Offset(double amount, DendroGrid vMask, double min, double max, bool invert);

	void Smooth(int type, int iterations, int width);
	void Smooth(int type, int iterations, int width, DendroGrid vMask, double min, double max, bool invert);

	void Blend(DendroGrid bGrid, double bPosition, double bEnd);
	void Blend(DendroGrid bGrid, double bPosition, double bEnd, DendroGrid vMask, double min, double max, bool invert);

	void ClosestPoint(std::vector<openvdb::Vec3R> &points, std::vector<float> &distances);

private:
	openvdb::FloatGrid::Ptr mGrid;
};

#endif // __DENDROGRID_H__