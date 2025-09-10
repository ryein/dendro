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
#include <vector>
#include <cmath>

DendroGrid::DendroGrid()
{
	openvdb::initialize();
}

DendroGrid::DendroGrid(DendroGrid *grid)
{
	openvdb::initialize();
	mGrid = grid->Grid()->deepCopy();
}

DendroGrid::~DendroGrid()
{
}

openvdb::FloatGrid::Ptr DendroGrid::Grid()
{
	return mGrid;
}

bool DendroGrid::Read(const char *vFile)
{
	openvdb::io::File file(vFile);

	file.open();

	openvdb::io::File::NameIterator nameIter = file.beginName();
	if (nameIter == file.endName())
	{
		file.close();
		return false;
	}

	mGrid = openvdb::gridPtrCast<openvdb::FloatGrid>(file.readGrid(nameIter.gridName()));

	file.close();
	return true;
}

bool DendroGrid::Write(const char *vFile)
{
	openvdb::GridPtrVec grids;
	grids.push_back(mGrid);

	openvdb::io::File file(vFile);
	file.write(grids);
	file.close();

	return true;
}

bool DendroGrid::FromMesh(const std::vector<openvdb::Vec3s> &vertices, const std::vector<openvdb::Vec3I> &triangles, const std::vector<openvdb::Vec4I> &quads, double voxelSize, double bandwidth)
{
	openvdb::math::Transform xform;
	xform.preScale(voxelSize);

	const float halfWidthVox = static_cast<float>(bandwidth / voxelSize);

	mGrid = openvdb::tools::meshToLevelSet<openvdb::FloatGrid>(xform, vertices, triangles, quads, halfWidthVox);

	return true;
}

bool DendroGrid::CreateFromPoints(DendroParticle vPoints, double voxelSize, double bandwidth)
{
	if (!vPoints.IsValid())
	{
		return false;
	}

	mGrid = openvdb::createLevelSet<openvdb::FloatGrid>(voxelSize, float(bandwidth / voxelSize));
	openvdb::tools::ParticlesToLevelSet<openvdb::FloatGrid> raster(*mGrid);

	openvdb::math::Transform::Ptr xform = openvdb::math::Transform::createLinearTransform(voxelSize);
	mGrid->setTransform(xform);

	raster.setGrainSize(1);
	raster.rasterizeSpheres(vPoints);
	raster.finalize();

	return true;
}

void DendroGrid::ToMesh(std::vector<openvdb::Vec3s> &vertices, std::vector<openvdb::Vec3I> &triangles, std::vector<openvdb::Vec4I> &quads, double isovalue, double adaptivity)
{
	openvdb::tools::volumeToMesh<openvdb::FloatGrid>(*mGrid, vertices, triangles, quads, isovalue, adaptivity);
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
	for (int i = 0; i < iterations; i++)
	{

		// filter by desired type supplied
		switch (type)
		{
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
	for (int i = 0; i < iterations; i++)
	{

		// filter by desired type supplied
		switch (type)
		{
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

void DendroGrid::ClosestPoint(std::vector<openvdb::Vec3R> &points, std::vector<float> &distances)
{
	auto csp = openvdb::tools::ClosestSurfacePoint<openvdb::FloatGrid>::create(*mGrid);
	csp->searchAndReplace(points, distances);
}