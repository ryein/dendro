#pragma once

#ifndef __DENDROMESH_H__
#define __DENDROMESH_H__

#include <openvdb/openvdb.h>
#include <vector>

class DendroMesh {

public:

	DendroMesh();
	~DendroMesh();

	DendroMesh Duplicate();

	bool IsValid();

	std::vector<openvdb::Vec3s> Vertices();
	std::vector<openvdb::Vec4I> Faces();

	void AddVertice(openvdb::Vec3s v);
	void AddVertice(std::vector<openvdb::Vec3s> v);

	void AddFace(openvdb::Vec4I f);
	void AddFace(std::vector<openvdb::Vec4I> f);

	void Clear();

private:
	std::vector<openvdb::Vec3s> mVertices;
	std::vector<openvdb::Vec4I> mFaces;
};

#endif // __DENDROMESH_H__