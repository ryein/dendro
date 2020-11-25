#include "stdafx.h"
#include "DendroGrid.h"

#include <openvdb/tools/VolumeToMesh.h>
#include <openvdb/tools/MeshToVolume.h>
#include <openvdb/tools/Composite.h>
#include <openvdb/tools/LevelSetFilter.h>
#include <openvdb/tools/LevelSetMorph.h>
#include <openvdb/tools/GridTransformer.h>
#include <openvdb/tools/ParticlesToLevelSet.h>
#include <openvdb/Types.h>
#include <openvdb/tools/VolumeToSpheres.h>

#include <cmath>

DendroGrid::DendroGrid()
{
	openvdb::initialize();
}

DendroGrid::DendroGrid(DendroGrid * grid)
{
	openvdb::initialize();
	mGrid = grid->Grid()->deepCopy();
	mDisplay = grid->Display().Duplicate();
}

DendroGrid::~DendroGrid()
{
}

openvdb::FloatGrid::Ptr DendroGrid::Grid()
{
	return mGrid;
}

bool DendroGrid::Read(const char * vFile)
{
	openvdb::io::File file(vFile);

	file.open();

	openvdb::io::File::NameIterator nameIter = file.beginName();
	if (nameIter == file.endName()) {
		return false;
	}

	mGrid = openvdb::gridPtrCast<openvdb::FloatGrid>(file.readGrid(nameIter.gridName()));

	return true;
}

bool DendroGrid::Write(const char * vFile)
{
	openvdb::GridPtrVec grids;
	grids.push_back(mGrid);

	openvdb::io::File file(vFile);
	file.write(grids);
	file.close();

	return true;
}

bool DendroGrid::CreateFromMesh(DendroMesh vMesh, double voxelSize, double bandwidth)
{
	if (!vMesh.IsValid()) {
		return false;
	}

	openvdb::math::Transform xform;
	xform.preScale(voxelSize);

	auto vertices = vMesh.Vertices();
	auto faces = vMesh.Faces();

	openvdb::tools::QuadAndTriangleDataAdapter<openvdb::Vec3s, openvdb::Vec4I> mesh(vertices, faces);
	mGrid = openvdb::tools::meshToVolume<openvdb::FloatGrid>(mesh, xform, static_cast<float>(bandwidth), static_cast<float>(bandwidth), 0, NULL);

	mDisplay = vMesh;

	return true;
}

bool DendroGrid::CreateFromPoints(DendroParticle vPoints, double voxelSize, double bandwidth)
{
	if (!vPoints.IsValid()) {
		return false;
	}

	mGrid = openvdb::createLevelSet<openvdb::FloatGrid>(voxelSize, bandwidth);
	openvdb::tools::ParticlesToLevelSet<openvdb::FloatGrid> raster(*mGrid);

	openvdb::math::Transform::Ptr xform = openvdb::math::Transform::createLinearTransform(voxelSize);
	mGrid->setTransform(xform);

	raster.setGrainSize(1);
	raster.rasterizeSpheres(vPoints);
	raster.finalize();

	return true;
}

void DendroGrid::Transform(openvdb::math::Mat4d xform)
{
	mGrid->transform().postMult(xform);
}

void DendroGrid::BooleanUnion(DendroGrid vAdd)
{
	auto csgGrid = vAdd.Grid();

	// store current tranforms of both csg volumes
	const openvdb::math::Transform
		&sourceXform = csgGrid->transform(),
		&targetXform = mGrid->transform();

	// create a copy of the source grid for resampling
	openvdb::FloatGrid::Ptr cGrid = openvdb::createLevelSet<openvdb::FloatGrid>(mGrid->voxelSize()[0]);
	cGrid->transform() = mGrid->transform();

	// compute a source grid to target grid transform
	openvdb::Mat4R xform =
		sourceXform.baseMap()->getAffineMap()->getMat4() *
		targetXform.baseMap()->getAffineMap()->getMat4().inverse();

	// create the transformer
	openvdb::tools::GridTransformer transformer(xform);

	// resample using trilinear interpolation 
	transformer.transformGrid<openvdb::tools::BoxSampler, openvdb::FloatGrid>(*csgGrid, *cGrid);

	// solve for the csg operation with result being stored in mGrid
	openvdb::tools::csgUnion(*mGrid, *cGrid, true);
}

void DendroGrid::BooleanIntersection(DendroGrid vIntersect)
{
	auto csgGrid = vIntersect.Grid();

	// store current tranforms of both csg volumes
	const openvdb::math::Transform
		&sourceXform = csgGrid->transform(),
		&targetXform = mGrid->transform();

	// create a copy of the source grid for resampling
	openvdb::FloatGrid::Ptr cGrid = openvdb::createLevelSet<openvdb::FloatGrid>(mGrid->voxelSize()[0]);
	cGrid->transform() = mGrid->transform();

	// compute a source grid to target grid transform
	openvdb::Mat4R xform =
		sourceXform.baseMap()->getAffineMap()->getMat4() *
		targetXform.baseMap()->getAffineMap()->getMat4().inverse();

	// create the transformer
	openvdb::tools::GridTransformer transformer(xform);

	// resample using trilinear interpolation 
	transformer.transformGrid<openvdb::tools::BoxSampler, openvdb::FloatGrid>(*csgGrid, *cGrid);

	// solve for the csg operation with result being stored in mGrid
	openvdb::tools::csgIntersection(*mGrid, *cGrid, true);
}

void DendroGrid::BooleanDifference(DendroGrid vSubtract)
{
	auto csgGrid = vSubtract.Grid();

	// store current tranforms of both csg volumes
	const openvdb::math::Transform
		&sourceXform = csgGrid->transform(),
		&targetXform = mGrid->transform();

	// create a copy of the source grid for resampling
	openvdb::FloatGrid::Ptr cGrid = openvdb::createLevelSet<openvdb::FloatGrid>(mGrid->voxelSize()[0]);
	cGrid->transform() = mGrid->transform();

	// compute a source grid to target grid transform
	openvdb::Mat4R xform =
		sourceXform.baseMap()->getAffineMap()->getMat4() *
		targetXform.baseMap()->getAffineMap()->getMat4().inverse();

	// create the transformer
	openvdb::tools::GridTransformer transformer(xform);

	// resample using trilinear interpolation 
	transformer.transformGrid<openvdb::tools::BoxSampler, openvdb::FloatGrid>(*csgGrid, *cGrid);

	// solve for the csg operation with result being stored in mGrid
	openvdb::tools::csgDifference(*mGrid, *cGrid, true);
}

void DendroGrid::Offset(double amount)
{
	// create a new filter to operate on grid with
	openvdb::tools::LevelSetFilter<openvdb::FloatGrid> filter(*mGrid);

	filter.setGrainSize(1);

	amount = amount * -1;

	// apply offset to grid of supplied amount
	filter.offset((float)amount);
}

void DendroGrid::Offset(double amount, DendroGrid vMask, double min, double max, bool invert)
{
	// create a new filter to operate on grid with
	openvdb::tools::LevelSetFilter<openvdb::FloatGrid> filter(*mGrid);

	filter.invertMask(invert);
	filter.setMaskRange((float)min, (float)max);
	filter.setGrainSize(1);

	// create filter mask
	openvdb::Grid<openvdb::FloatTree> mMask(*vMask.Grid());

	amount = amount * -1;

	// apply offset to grid of supplied amount
	filter.offset((float)amount, &mMask);
}

void DendroGrid::Smooth(int type, int iterations, int width)
{
	// create a new filter to operate on grid with
	openvdb::tools::LevelSetFilter<openvdb::FloatGrid> filter(*mGrid);
	filter.setGrainSize(1);

	// apply filter for the number iterations supplied
	for (int i = 0; i < iterations; i++) {

		// filter by desired type supplied
		switch (type) {
		case 0:
			filter.gaussian(width);
			break;
		case 1:
			filter.laplacian();
			break;
		case 2:
			filter.mean(width);
			break;
		case 3:
			filter.median(width);
			break;
		default:
			filter.laplacian();
			break;
		}
	}
}

void DendroGrid::Smooth(int type, int iterations, int width, DendroGrid vMask, double min, double max, bool invert)
{
	// create a new filter to operate on grid with
	openvdb::tools::LevelSetFilter<openvdb::FloatGrid> filter(*mGrid);

	filter.invertMask(invert);
	filter.setMaskRange((float)min, (float)max);
	filter.setGrainSize(1);

	// create filter mask
	openvdb::Grid<openvdb::FloatTree> mMask(*vMask.Grid());

	// apply filter for the number iterations supplied
	for (int i = 0; i < iterations; i++) {

		// filter by desired type supplied
		switch (type) {
		case 0:
			filter.gaussian(width, &mMask);
			break;
		case 1:
			filter.laplacian(&mMask);
			break;
		case 2:
			filter.mean(width, &mMask);
			break;
		case 3:
			filter.median(width, &mMask);
			break;
		default:
			filter.laplacian(&mMask);
			break;
		}
	}
}

void DendroGrid::Blend(DendroGrid bGrid, double bPosition, double bEnd)
{
	openvdb::tools::LevelSetMorphing<openvdb::FloatGrid> morph(*mGrid, *bGrid.Grid());
	morph.setSpatialScheme(openvdb::math::HJWENO5_BIAS);
	morph.setTemporalScheme(openvdb::math::TVD_RK3);
	morph.setTrackerSpatialScheme(openvdb::math::HJWENO5_BIAS);
	morph.setTrackerTemporalScheme(openvdb::math::TVD_RK2);
	morph.setGrainSize(1);

	double bStart = bPosition * bEnd;
	morph.advect(bStart, bEnd);
}

void DendroGrid::Blend(DendroGrid bGrid, double bPosition, double bEnd, DendroGrid vMask, double mMin, double mMax, bool invert)
{
	openvdb::tools::LevelSetMorphing<openvdb::FloatGrid> morph(*mGrid, *bGrid.Grid());
	morph.setSpatialScheme(openvdb::math::HJWENO5_BIAS);
	morph.setTemporalScheme(openvdb::math::TVD_RK3);
	morph.setTrackerSpatialScheme(openvdb::math::HJWENO5_BIAS);
	morph.setTrackerTemporalScheme(openvdb::math::TVD_RK2);

	morph.setAlphaMask(*vMask.Grid());
	morph.invertMask(invert);
	morph.setMaskRange((float)mMin, (float)mMax);
	morph.setGrainSize(1);

	double bStart = bPosition * bEnd;
	morph.advect(bStart, bEnd);
}

void DendroGrid::ClosestPoint(std::vector<openvdb::Vec3R>& points, std::vector<float>& distances)
{
	auto csp = openvdb::tools::ClosestSurfacePoint<openvdb::FloatGrid>::create(*mGrid);
	csp->searchAndReplace(points, distances);
}

DendroMesh DendroGrid::Display()
{
	return mDisplay;
}

void DendroGrid::UpdateDisplay()
{
	using openvdb::Index64;

	openvdb::tools::VolumeToMesh mesher(mGrid->getGridClass() == openvdb::GRID_LEVEL_SET ? 0.0 : 0.01);
	mesher(*mGrid);

	mDisplay.Clear();

	for (Index64 n = 0, i = 0, N = mesher.pointListSize(); n < N; ++n)
	{
		auto v = mesher.pointList()[n];
		mDisplay.AddVertice(v);
	}

	openvdb::tools::PolygonPoolList &polygonPoolList = mesher.polygonPoolList();

	for (Index64 n = 0, N = mesher.polygonPoolListSize(); n < N; ++n)
	{
		const openvdb::tools::PolygonPool &polygons = polygonPoolList[n];
		for (Index64 i = 0, I = polygons.numQuads(); i < I; ++i)
		{
			auto face = polygons.quad(i);
			mDisplay.AddFace(face);
		}
	}
}

void DendroGrid::UpdateDisplay(double isovalue, double adaptivity)
{
	isovalue /= mGrid->voxelSize().x();

	std::vector<openvdb::Vec3s> points;
	std::vector<openvdb::Vec4I> quads;
	std::vector<openvdb::Vec3I> triangles;

	openvdb::tools::volumeToMesh<openvdb::FloatGrid>(*mGrid, points, triangles, quads, isovalue, adaptivity);

	mDisplay.Clear();

	mDisplay.AddVertice(points);

	auto begin = triangles.begin();
	auto end = triangles.end();

	for (auto it = begin; it != end; ++it) {
		int w = -1;
		int x = it->x();
		int y = it->y();
		int z = it->z();

		openvdb::Vec4I face(x,y,z,w);

		mDisplay.AddFace(face);
	}

	mDisplay.AddFace(quads);
}

float * DendroGrid::GetMeshVertices()
{
	auto vertices = mDisplay.Vertices();
	
	mVertexCount = vertices.size() * 3;

	float *verticeArray = reinterpret_cast<float*>(malloc(mVertexCount * sizeof(float)));

	int i = 0;
	for (auto it = vertices.begin(); it != vertices.end(); ++it) {
		verticeArray[i] = it->x();
		verticeArray[i + 1] = it->y();
		verticeArray[i + 2] = it->z();
		i += 3;
	}

	return verticeArray;
}

int * DendroGrid::GetMeshFaces()
{
	auto faces = mDisplay.Faces();

	mFaceCount = faces.size() * 4;

	int *faceArray = reinterpret_cast<int*>(malloc(mFaceCount * sizeof(int)));

	int i = 0;
	for (auto it = faces.begin(); it != faces.end(); ++it) {
		faceArray[i] = it->w();
		faceArray[i + 1] = it->x();
		faceArray[i + 2] = it->y();
		faceArray[i + 3] = it->z();
		i += 4;
	}

	return faceArray;
}

int DendroGrid::GetVertexCount()
{
	return mVertexCount;
}

int DendroGrid::GetFaceCount()
{
	return mFaceCount;
}
