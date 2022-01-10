using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;

namespace Oc
{
    public class Box3f
    {
        public Vec3f _vmin;
        public Vec3f _vmax;

        public Box3f()
        {
        }
        public Box3f(Vec3f min, Vec3f max)
        {
            _vmin = min;
            _vmax = max;
        }

        public void Validate()
        {
            if (_vmax.X < _vmin.X)
                throw new Exception("Bound box X was invalid.");
            if (_vmax.Y < _vmin.Y)
                throw new Exception("Bound box Y was invalid.");
            if (_vmax.Z < _vmin.Z)
                throw new Exception("Bound box Z was invalid.");
        }
        /**
        *	@fn RayIntersect
        *	@brief Returns true if the given ray intersects this Axis aligned
        *	cube volume.
        *	@param bh - Reference to a BoxHit structure.
        *	@prarm ray - The ray to test against the box.
        *	@return true if ray intersects, false otherwise.
        */
        public bool LineOrRayIntersectInclusive_EasyOut(PickRay ray, ref BoxAAHit bh)
        {
            if (RayIntersect(ray, ref bh))
                return true;
            // - otherwise check for points contained.
            if (containsInclusive(ray.Origin))
            {
                bh._p1Contained = true;
                bh._bHit = true;
                return true;
            }

            if (containsInclusive(ray.Origin + ray.Dir))
            {
                bh._p2Contained = true;
                bh._bHit = true;
                return true;
            }

            return false;
        }
        private Vec3f bounds(int in__)
        {
            if (in__ == 0)
                return _vmin;
            return _vmax;
        }
        private bool RayIntersect(PickRay ray, ref BoxAAHit bh)
        {
            if (!ray._isOpt)
                throw new Exception("Projected ray was not optimized");

            float txmin, txmax, tymin, tymax, tzmin, tzmax;

            txmin = (bounds(ray.Sign[0]).X - ray.Origin.X) * ray.InvDir.X;
            txmax = (bounds(1 - ray.Sign[0]).X - ray.Origin.X) * ray.InvDir.X;

            tymin = (bounds(ray.Sign[1]).Y - ray.Origin.Y) * ray.InvDir.Y;
            tymax = (bounds(1 - ray.Sign[1]).Y - ray.Origin.Y) * ray.InvDir.Y;

            if ((txmin > tymax) || (tymin > txmax))
            {
                bh._bHit = false;
                return false;
            }
            if (tymin > txmin)
                txmin = tymin;
            if (tymax < txmax)
                txmax = tymax;

            tzmin = (bounds(ray.Sign[2]).Z - ray.Origin.Z) * ray.InvDir.Z;
            tzmax = (bounds(1 - ray.Sign[2]).Z - ray.Origin.Z) * ray.InvDir.Z;

            if ((txmin > tzmax) || (tzmin > txmax))
            {
                bh._bHit = false;
                return false;
            }
            if (tzmin > txmin)
                txmin = tzmin;
            if (tzmax < txmax)
                txmax = tzmax;

            bh._bHit = ((txmin > 0.0f ) && (txmax <= ray._length));
            bh._t = txmin;

            return bh._bHit;
        }

        private bool containsInclusive(Vec3f v)
        {
            return (
                (v.X >= _vmin.X) && (v.X <= _vmax.X) &&
                (v.Y >= _vmin.Y) && (v.Y <= _vmax.Y) &&
                (v.Z >= _vmin.Z) && (v.Z <= _vmax.Z)
                );
        }

    }



}
