#pragma once

#ifndef __DENDROPARTICLE_H__
#define __DENDROPARTICLE_H__

#include <openvdb/openvdb.h>
#include <vector>

class DendroParticle
{
protected:
	struct Particle {
		openvdb::Vec3R p, v;
		openvdb::Real  r;
	};
	openvdb::Real           mRadiusScale;
	openvdb::Real           mVelocityScale;
	std::vector<Particle>   mParticleList;
public:

	typedef openvdb::Vec3R  PosType;

	DendroParticle(openvdb::Real rScale = 1, openvdb::Real vScale = 1)
		: mRadiusScale(rScale), mVelocityScale(vScale) {}

	void add(const openvdb::Vec3R &p, const openvdb::Real &r,
		const openvdb::Vec3R &v = openvdb::Vec3R(0, 0, 0))
	{
		Particle pa;
		pa.p = p;
		pa.r = r;
		pa.v = v;
		mParticleList.push_back(pa);
	}

	bool IsValid() {
		return (mParticleList.size() > 0) ? true : false;
	}
	void clear() { mParticleList.clear(); }

	/// @return coordinate bbox in the space of the specified transfrom
	openvdb::CoordBBox getBBox(const openvdb::GridBase& grid) {
		openvdb::CoordBBox bbox;
		openvdb::Coord &min = bbox.min(), &max = bbox.max();
		openvdb::Vec3R pos;
		openvdb::Real rad, invDx = 1 / grid.voxelSize()[0];
		for (size_t n = 0, e = this->size(); n<e; ++n) {
			this->getPosRad(n, pos, rad);
			const openvdb::Vec3d xyz = grid.worldToIndex(pos);
			const openvdb::Real   r = rad * invDx;
			for (int i = 0; i<3; ++i) {
				min[i] = openvdb::math::Min(min[i], openvdb::math::Floor(xyz[i] - r));
				max[i] = openvdb::math::Max(max[i], openvdb::math::Ceil(xyz[i] + r));
			}
		}
		return bbox;
	}
	//typedef int AttributeType;
	// The methods below are only required for the unit-tests
	openvdb::Vec3R pos(int n)   const { return mParticleList[n].p; }
	openvdb::Vec3R vel(int n)   const { return mVelocityScale * mParticleList[n].v; }
	openvdb::Real radius(int n) const { return mRadiusScale * mParticleList[n].r; }

	//////////////////////////////////////////////////////////////////////////////
	/// The methods below are the only ones required by tools::ParticleToLevelSet
	/// @note We return by value since the radius and velocities are modified
	/// by the scaling factors! Also these methods are all assumed to
	/// be thread-safe.

	/// Return the total number of particles in list.
	///  Always required!
	size_t size() const { return mParticleList.size(); }

	/// Get the world space position of n'th particle.
	/// Required by ParticledToLevelSet::rasterizeSphere(*this,radius).
	void getPos(size_t n, openvdb::Vec3R&pos) const { pos = mParticleList[n].p; }


	void getPosRad(size_t n, openvdb::Vec3R& pos, openvdb::Real& rad) const {
		pos = mParticleList[n].p;
		rad = mRadiusScale * mParticleList[n].r;
	}
	void getPosRadVel(size_t n, openvdb::Vec3R& pos, openvdb::Real& rad, openvdb::Vec3R& vel) const {
		pos = mParticleList[n].p;
		rad = mRadiusScale * mParticleList[n].r;
		vel = mVelocityScale * mParticleList[n].v;
	}
	// The method below is only required for attribute transfer
	void getAtt(size_t n, openvdb::Index32& att) const { att = openvdb::Index32(n); }
};

#endif // __DENDROPARTICLE_H__