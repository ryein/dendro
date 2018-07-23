#include "stdafx.h"
#include "DendroMesh.h"


DendroMesh::DendroMesh()
{
	mVertices.clear();
	mFaces.clear();
}

DendroMesh DendroMesh::Duplicate()
{
	DendroMesh mesh;
	mesh.AddVertice(mVertices);
	mesh.AddFace(mFaces);

	return mesh;
}

DendroMesh::~DendroMesh()
{
}

bool DendroMesh::IsValid()
{
	if (mFaces.size() > 0 && mVertices.size() > 0) {
		return true;
	}

	return false;
}

std::vector<openvdb::Vec3s> DendroMesh::Vertices()
{
	return mVertices;
}

std::vector<openvdb::Vec4I> DendroMesh::Faces()
{
	return mFaces;
}

void DendroMesh::AddVertice(openvdb::Vec3s v)
{
	mVertices.push_back(v);
}

void DendroMesh::AddVertice(std::vector<openvdb::Vec3s> v)
{
	mVertices.insert(mVertices.end(), v.begin(), v.end());
}

void DendroMesh::AddFace(openvdb::Vec4I f)
{
	mFaces.push_back(f);
}

void DendroMesh::AddFace(std::vector<openvdb::Vec4I> f)
{
	mFaces.insert(mFaces.end(), f.begin(), f.end());
}

void DendroMesh::Clear()
{
	mVertices.clear();
	mFaces.clear();
}

