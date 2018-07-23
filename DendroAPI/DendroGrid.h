#pragma once

#ifndef __DENDROGRID_H__
#define __DENDROGRID_H__

#include "DendroParticle.h"
#include "DendroMesh.h"
#include <openvdb/openvdb.h>
#include <vector>
#include <string>

class DendroGrid
{
public:
	DendroGrid();
	DendroGrid(DendroGrid * grid);
	~DendroGrid();

	openvdb::FloatGrid::Ptr Grid();

	bool Read(const char *vFile);
	bool Write(const char *vFile);

	bool CreateFromMesh(DendroMesh vMesh, double voxelSize, double bandwidth);
	bool CreateFromPoints(DendroParticle vPoints, double voxelSize, double bandwidth);

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

	DendroMesh Display();

	void UpdateDisplay();
	void UpdateDisplay(double isovalue, double adaptivity);

	float * GetMeshVertices();
	int * GetMeshFaces();
	int GetVertexCount();
	int GetFaceCount();

private:
	openvdb::FloatGrid::Ptr mGrid;
	DendroMesh mDisplay;
	int mFaceCount;
	int mVertexCount;
};

#endif // __DENDROGRID_H__