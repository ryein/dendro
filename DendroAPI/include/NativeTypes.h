#pragma once

#ifndef __NATIVETYPES_H__
#define __NATIVETYPES_H__

#include <openvdb/Types.h>
#include <algorithm>

struct NativePoint
{
	float x, y, z;
};

struct NativeFace
{
	int a, b, c, d;
};

struct NativeSegment
{
	int a, b;
};

static_assert(sizeof(NativePoint) == 12, "NativePoint must be 12 bytes");
static_assert(sizeof(NativeFace) == 16, "NativeFace must be 16 bytes");
static_assert(sizeof(NativeSegment) == 8, "NativeSegment must be 8 bytes");

class NativeParticle
{
public:
	using PosType = openvdb::Vec3R;

	NativeParticle(const NativePoint *pts, size_t pCount,
				   const float *radii, size_t rCount)
		: m_pts(pts), m_pCount(pCount), m_radii(radii), m_rCount(rCount) {}

	size_t size() const { return m_pCount; }

	void getPos(size_t i, PosType &pos) const
	{
		const NativePoint &p = m_pts[i];
		pos[0] = p.x;
		pos[1] = p.y;
		pos[2] = p.z;
	}

	void getPosRad(size_t i, PosType &pos, openvdb::Real &rad) const
	{
		const NativePoint &p = m_pts[i];
		pos[0] = p.x;
		pos[1] = p.y;
		pos[2] = p.z;
		rad = m_radii ? (m_rCount == 1 ? m_radii[0] : m_radii[i]) : openvdb::Real(1.0);
	}

	float getRadius(size_t i) const
	{
		return m_radii ? m_radii[(m_rCount == 1) ? 0 : i] : 1.0f;
	}

	float getMaxRadius() const
	{
		if (!m_radii || m_rCount == 0)
			return 0.0f;

		float maxRadius = m_radii[0];
		for (size_t i = 1; i < m_rCount; ++i)
			maxRadius = std::max(maxRadius, m_radii[i]);
		return maxRadius;
	}

	bool hasUniformRadius() const
	{
		return m_rCount == 1;
	}

private:
	const NativePoint *m_pts = nullptr;
	size_t m_pCount = 0;
	const float *m_radii = nullptr;
	size_t m_rCount = 0;
};

#endif // __NATIVETYPES_H__
