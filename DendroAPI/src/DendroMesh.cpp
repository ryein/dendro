#include "DendroMesh.h"

DendroMesh::DendroMesh()
{
	mVertices.clear();
	mFaces.clear();
}

DendroMesh DendroMesh::Duplicate() const
{
	DendroMesh mesh;
	mesh.AddVertice(mVertices);
	mesh.AddFace(mFaces);

	return mesh;
}

DendroMesh::~DendroMesh() {}

bool DendroMesh::IsValid() const
{
	if (mFaces.size() > 0 && mVertices.size() > 0)
	{
		return true;
	}

	return false;
}

const std::vector<openvdb::Vec3s> &DendroMesh::Vertices() const
{
	return mVertices;
}

const std::vector<openvdb::Vec4I> &DendroMesh::Faces() const
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
