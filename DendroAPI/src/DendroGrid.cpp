#include "DendroGrid.h"

#include <openvdb/tools/VolumeToMesh.h>
#include <openvdb/tools/MeshToVolume.h>
#include <openvdb/tools/Composite.h>
#include <openvdb/tools/LevelSetFilter.h>
#include <openvdb/tools/LevelSetMorph.h>
#include <openvdb/tools/LevelSetUtil.h>
#include <openvdb/tools/GridTransformer.h>
#include <openvdb/tools/ParticlesToLevelSet.h>
#include <openvdb/tools/LevelSetTubes.h>
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

void DendroGrid::ToMesh(std::vector<openvdb::Vec3s> &vertices, std::vector<openvdb::Vec3I> &triangles, std::vector<openvdb::Vec4I> &quads, double isovalue, double adaptivity)
{
	// converts isovalue from world units to voxel space
	isovalue /= mGrid->voxelSize().x();

	// extracts a polygonal mesh from the level set grid
	openvdb::tools::volumeToMesh<openvdb::FloatGrid>(*mGrid, vertices, triangles, quads, isovalue, adaptivity);
}

bool DendroGrid::FromMesh(const std::vector<openvdb::Vec3s> &vertices, const std::vector<openvdb::Vec3I> &triangles, const std::vector<openvdb::Vec4I> &quads, double voxelSize, double bandwidth)
{
	// create linear transform that maps voxel indices to world space.
	openvdb::math::Transform::Ptr xform = openvdb::math::Transform::createLinearTransform(voxelSize);

	// meshToLevelSet expects the narrow-band half-width in voxel units.
	mGrid = openvdb::tools::meshToLevelSet<openvdb::FloatGrid>(
		*xform,
		vertices,
		triangles,
		quads,
		static_cast<float>(bandwidth));

	return true;
}

bool DendroGrid::FromPoints(NativeParticle plist, double voxelSize, double bandwidth)
{
	using GridT = openvdb::FloatGrid;

	// Grid values are signed distances in world units, so the background value
	// is the voxel-space half-width multiplied by the voxel size.
	GridT::Ptr sdf = GridT::create(static_cast<float>(bandwidth * voxelSize));
	sdf->setTransform(openvdb::math::Transform::createLinearTransform(voxelSize));
	sdf->setGridClass(openvdb::GRID_LEVEL_SET);
	sdf->setName("sdf");

	// v12 ctor: just the grid (optionally an interrupter)
	openvdb::tools::ParticlesToLevelSet<GridT> raster(*sdf);

	// Optional clamps in *voxel units* if you want them:
	// raster.setRmin(minRadiusWorld / voxelSize);
	// raster.setRmax(maxRadiusWorld / voxelSize);

	raster.rasterizeSpheres(plist); // per-particle radius is in world units
	raster.finalize();

	mGrid = std::move(sdf);
	return true;
}

bool DendroGrid::FromCurves(const std::vector<openvdb::Vec3s> &points, const std::vector<openvdb::Vec2I> &segments, const std::vector<float> &radii, double voxelSize, double bandwidth)
{
	// createLevelSetTubeComplex expects radii and voxel size in world units,
	// and the narrow-band half-width in voxel units.
	if (radii.size() == 1)
	{
		mGrid = openvdb::tools::createLevelSetTubeComplex<openvdb::FloatGrid>(
			points,
			segments,
			radii.front(),
			static_cast<float>(voxelSize),
			static_cast<float>(bandwidth));
	}
	else
	{
		mGrid = openvdb::tools::createLevelSetTubeComplex<openvdb::FloatGrid>(
			points,
			segments,
			radii,
			static_cast<float>(voxelSize),
			static_cast<float>(bandwidth),
			openvdb::tools::TUBE_SEGMENT_RADII);
	}

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
