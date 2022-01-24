using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using Mat4f = OpenTK.Matrix4;
//using Microsoft.Xna.Framework;

//Copied this stuff from legend of \kevin

namespace PirateCraft
{


    #region MathUtils

    public class MathUtils
    {
        public static float Clamp(float f, float a, float b)
        {
            return Math.Max(Math.Min(f,b),a);
        }
        public static Mat4f m4f16(float[] mv)
        {
            if (mv.Length != 16)
                throw new Exception("matrix was not 16 elements wide.");

            return new Mat4f(
                  mv[0],
                  mv[1],
                  mv[2],
                  mv[3],
                  mv[4],
                  mv[5],
                  mv[6],
                  mv[7],
                  mv[8],
                  mv[9],
                  mv[10],
                  mv[11],
                  mv[12],
                  mv[13],
                  mv[14],
                  mv[15]
                  );
        }

        public static float ToDegrees(float rads)
        {
            return (rads / ((float)Math.PI)) * 180.0f;
        }
        public static float ToRadians(float degs)
        {
            return (degs / 180.0f) * (float)Math.PI;
        }
        public static double ToDegrees(double rads)
        {
            return (rads / ((double)Math.PI)) * 180.0f;
        }
        public static double ToRadians(double degs)
        {
            return (degs / 180.0f) * (double)Math.PI;
        }
        public static vec2 DecomposeRotation(float r)
        {
            //Turn a rotation into a vector (for chars mostly)
            float r2 = r - (float)Math.PI * 0.5f;
            vec2 dxy = new vec2(
                (float)Math.Cos(r2),
                (float)Math.Sin(r2)
                );

            return dxy;
        }
        public static float GetRotationFromLine(float x, float y, float x2, float y2)
        {
            //https://stackoverflow.com/questions/270138/how-do-i-draw-lines-using-xna
            //this returns the angle between two points in radians 
            float adj = x - x2;
            float opp = y - y2;
            float tan = opp / adj;
            float res = MathUtils.ToDegrees((float)Math.Atan2(opp, adj));
            res = (res - 180) % 360;
            if (res < 0) { res += 360; }
            res = MathUtils.ToRadians(res);
            return res;
        }
        //http://xboxforums.create.msdn.com/forums/t/34356.aspx
        public static float? RayIntersect(Box2f box, ProjectedRay ray)
        {
            float num = 0f;
            float maxValue = float.MaxValue;
            if (Math.Abs(ray.Dir.x) < 1E-06f)
            {
                if ((ray.Origin.x < box.Min.x) || (ray.Origin.x > box.Max.x))
                {
                    return null;
                }
            }
            else
            {
                float num11 = 1f / ray.Dir.x;
                float num8 = (box.Min.x - ray.Origin.x) * num11;
                float num7 = (box.Max.x - ray.Origin.x) * num11;
                if (num8 > num7)
                {
                    float num14 = num8;
                    num8 = num7;
                    num7 = num14;
                }
                num = Math.Max(num8, num);
                maxValue = Math.Min(num7, maxValue);
                if (num > maxValue)
                {
                    return null;
                }
            }
            if (Math.Abs(ray.Dir.y) < 1E-06f)
            {
                if ((ray.Origin.y < box.Min.y) || (ray.Origin.y > box.Max.y))
                {
                    return null;
                }
            }
            else
            {
                float num10 = 1f / ray.Dir.y;
                float num6 = (box.Min.y - ray.Origin.y) * num10;
                float num5 = (box.Max.y - ray.Origin.y) * num10;
                if (num6 > num5)
                {
                    float num13 = num6;
                    num6 = num5;
                    num5 = num13;
                }
                num = Math.Max(num6, num);
                maxValue = Math.Min(num5, maxValue);
                if (num > maxValue)
                {
                    return null;
                }
            }

            return new float?(num);
        }
    }

    #endregion
    public class Plane2f
    {
        public Plane2f() { }
        public float D;
        public vec2 N;
        public Plane2f(vec2 n, vec2 pt)
        {
            D = -n.Dot(pt);
            N = n;
        }
        public float IntersectLine(vec2 p1, vec2 p2)
        {
            float t = -(N.Dot(p1) + D) / ((p2 - p1).Dot(N));
            return t;
        }
    }
    public class vec2EqualityComparer : IEqualityComparer<vec2>
    {
        public bool Equals(vec2 x, vec2 y)
        {
            return x.x == y.x && x.y == y.y;
        }

        public int GetHashCode(vec2 x)
        {
            return x.x.GetHashCode() + x.y.GetHashCode();
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    class mat2
    {
        //0 1
        //2 3
        public static int m00 = 0;
        public static int m01 = 1;
        public static int m10 = 2;
        public static int m11 = 3;
        public float[] m = new float[4];

        public static vec2 operator *(mat2 a, vec2 b)
        {
            vec2 r = new vec2(0, 0);

            r.x = a.m[0] * b.x + a.m[1] * b.y;
            r.y = a.m[2] * b.x + a.m[3] * b.y;
            return r;
        }
        public static mat2 GetRot(float theta)
        {
            mat2 ret = new mat2();

            //This is an incorrect rotation function - sin 10 shouldn't be negative.
            ret.m[m00] = (float)Math.Cos(theta);
            ret.m[m10] = (float)Math.Sin(theta);
            ret.m[m01] = -(float)Math.Sin(theta);
            ret.m[m11] = (float)Math.Cos(theta);

            return ret;
        }

    }
    public class ivec2EqualityComparer : IEqualityComparer<ivec2>
    {
        public bool Equals(ivec2 x, ivec2 y)
        {
            return x.x == y.x && x.y == y.y;
        }

        public int GetHashCode(ivec2 x)
        {
            return x.x.GetHashCode() + x.y.GetHashCode();
        }
    }
        [StructLayout(LayoutKind.Sequential)]
    public struct vec2
    {
        public vec2(Point p)
        {
            x = (float)p.X;
            y = (float)p.Y;
        }
        public float x, y;
        public void Construct(float a, float b) { x = a; y = b; }
        //public vec2() { }
        public vec2(vec2 dxy) { x = dxy.x; y = dxy.y; }
        public vec2(float dx, float dy) { x = dx; y = dy; }
        public vec2(OpenTK.Vector2 v) { x = v.X; y = v.Y; }//From XNA's Vector2
        public float Len() { return (float)Math.Sqrt((x * x) + (y * y)); }

        public vec2 Perp()
        {
            //Perpendicular
            return new vec2(y, -x);
        }
        public void Normalize()
        {
            float l = Len();
            if (l != 0)
            {
                x /= l;
                y /= l;
            }
            else
            {
                x = 0; y = 0;
            }

        }
        public vec2 Normalized()
        {
            vec2 v = new vec2(this);
            v.Normalize();
            return v;

        }
        public float Len2() { return Dot(this, this); }
        public OpenTK.Vector2 toXNA() { return new OpenTK.Vector2(x, y); }


        static public implicit operator vec2(float f)
        {
            return new vec2(f, f);
        }
        //public static vec2 operator =(vec2 a, float f)
        //{
        //    return new vec2(f, f);
        //}
        public static float Dot(vec2 a, vec2 b)
        {
            return (a.x * b.x) + (a.y * b.y);
        }
        public float Dot(vec2 b)
        {
            return (x * b.x) + (y * b.y);
        }
        public static vec2 operator -(vec2 d)
        {
            return new vec2(-d.x, -d.y);
        }
        public static vec2 operator +(vec2 a, vec2 b)
        {
            return new vec2(a.x + b.x, a.y + b.y);
        }
        public static vec2 operator -(vec2 a, vec2 b)
        {
            return new vec2(a.x - b.x, a.y - b.y);
        }
        public static vec2 operator *(vec2 a, float b)
        {
            return new vec2(a.x * b, a.y * b);
        }
        public static vec2 operator *(vec2 a, vec2 b)
        {
            return new vec2(a.x * b.x, a.y * b.y);
        }
        public static vec2 operator /(vec2 a, float b)
        {
            return new vec2(a.x / b, a.y / b);
        }
        public static vec2 operator -(vec2 a, float f)
        {
            return new vec2(a.x - f, a.y - f);
        }
        public static vec2 Minv(vec2 a, vec2 b)
        {
            vec2 ret = new vec2();
            ret.x = (float)Math.Min(a.x, b.x);
            ret.y = (float)Math.Min(a.y, b.y);

            return ret;
        }
        public static vec2 Maxv(vec2 a, vec2 b)
        {
            vec2 ret = new vec2();
            ret.x = (float)Math.Max(a.x, b.x);
            ret.y = (float)Math.Max(a.y, b.y);
            return ret;
        }

    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ivec2
    {
        public ivec2(int dx, int dy) { x = dx; y = dy; }
        public Int32 x { get; set; }
        public Int32 y { get; set; }
        static public implicit operator ivec2(int f)
        {
            return new ivec2(f, f);
        }
        public static ivec2 operator -(ivec2 d)
        {
            return new ivec2(-d.x, -d.y);
        }
        public static ivec2 operator +(ivec2 a, ivec2 b)
        {
            return new ivec2(a.x + b.x, a.y + b.y);
        }
        public static ivec2 operator -(ivec2 a, ivec2 b)
        {
            return new ivec2(a.x - b.x, a.y - b.y);
        }
        public static ivec2 operator *(ivec2 a, int b)
        {
            return new ivec2(a.x * b, a.y * b);
        }
        public static ivec2 operator *(ivec2 a, ivec2 b)
        {
            return new ivec2(a.x * b.x, a.y * b.y);
        }
        public static ivec2 operator /(ivec2 a, int b)
        {
            return new ivec2(a.x / b, a.y / b);
        }
        public static ivec2 operator -(ivec2 a, int f)
        {
            return new ivec2(a.x - f, a.y - f);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct uvec2
    {
        public uvec2(int dx, int dy) { x = dx; y = dy; }
        public Int32 x { get; set; }
        public Int32 y { get; set; }
        static public implicit operator uvec2(int f)
        {
            return new uvec2(f, f);
        }
        public static uvec2 operator -(uvec2 d)
        {
            return new uvec2(-d.x, -d.y);
        }
        public static uvec2 operator +(uvec2 a, uvec2 b)
        {
            return new uvec2(a.x + b.x, a.y + b.y);
        }
        public static uvec2 operator -(uvec2 a, uvec2 b)
        {
            return new uvec2(a.x - b.x, a.y - b.y);
        }
        public static uvec2 operator *(uvec2 a, int b)
        {
            return new uvec2(a.x * b, a.y * b);
        }
        public static uvec2 operator *(uvec2 a, uvec2 b)
        {
            return new uvec2(a.x * b.x, a.y * b.y);
        }
        public static uvec2 operator /(uvec2 a, int b)
        {
            return new uvec2(a.x / b, a.y / b);
        }
        public static uvec2 operator -(uvec2 a, int f)
        {
            return new uvec2(a.x - f, a.y - f);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct vec4
    {
        public float x, y, z, w;

        public vec4(vec3 d, float dw) { x = d.x; y = d.y; z = d.z; w = dw; }
        public vec4(vec4 dxy) { x = dxy.x; y = dxy.y; z = dxy.z; w = dxy.w; }
        public vec4(float dx, float dy, float dz, float dw) { x = dx; y = dy; z = dz; w = dw; }
        public vec4(OpenTK.Vector4 v) { x = v.X; y = v.Y; z = v.Z; w = v.W; }//From XNA's Vector2


        public static vec4 Clamp(vec4 v, float a, float b)
        {
            vec4 ret = new vec4();
            ret.z = OpenTK.MathHelper.Clamp(v.z, a, b);
            ret.x = OpenTK.MathHelper.Clamp(v.x, a, b);
            ret.y = OpenTK.MathHelper.Clamp(v.y, a, b);
            ret.w = OpenTK.MathHelper.Clamp(v.w, a, b);
            return ret;
        }
        public void Clamp(float a, float b)
        {
            this = Clamp(this, a, b);
        }
        public void SetMinLightValue(float val)
        {
            //Make sure there's enough light for this.
            //Val = the minimum amount of light.
            //This isn't perfect
            float tot = x + y + z;
            if (tot < val)
            {
                float add = (2 - tot) / val;
                x += add;
                y += add;
                z += add;
                x = OpenTK.MathHelper.Clamp(x, 0, 1);
                y = OpenTK.MathHelper.Clamp(y, 0, 1);
                z = OpenTK.MathHelper.Clamp(z, 0, 1);
            }

        }

        public OpenTK.Vector4 toOpenTK() { return new OpenTK.Vector4(x, y, z, w); }
        public OpenTK.Graphics.Color4 toOpenTKColor()
        {
            var x = toOpenTK();
            return new OpenTK.Graphics.Color4(x.X, x.Y, x.Z, x.W);
        }
        public static vec4 operator -(vec4 d)
        {
            return new vec4(-d.x, -d.y, -d.z, -d.w);
        }

        public static vec4 operator +(vec4 a, vec4 b)
        {
            return new vec4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
        }
        public static vec4 operator -(vec4 a, vec4 b)
        {
            return new vec4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
        }
        public static vec4 operator *(vec4 a, vec4 b)
        {
            return new vec4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
        }
        public static vec4 operator *(vec4 a, float b)
        {
            return new vec4(a.x * b, a.y * b, a.z * b, a.w * b);
        }
        public static vec4 operator /(vec4 a, float b)
        {
            return new vec4(a.x / b, a.y / b, a.z / b, a.w / b);
        }
        public static vec4 Minv(vec4 a, vec4 b)
        {
            vec4 ret = new vec4();
            ret.x = (float)Math.Min(a.x, b.x);
            ret.y = (float)Math.Min(a.y, b.y);
            ret.z = (float)Math.Min(a.z, b.z);
            ret.w = (float)Math.Min(a.w, b.w);
            return ret;
        }
        public static vec4 Maxv(vec4 a, vec4 b)
        {
            vec4 ret = new vec4();
            ret.x = (float)Math.Max(a.x, b.x);
            ret.y = (float)Math.Max(a.y, b.y);
            ret.z = (float)Math.Max(a.z, b.z);
            ret.w = (float)Math.Max(a.w, b.w);
            return ret;
        }
        public vec3 xyz()
        {
            return new vec3(x, y, z);
        }

    }
    public struct RaycastHit
    {
        public bool _bHit;    // Whether the ray intersected the box.
        public bool _p1Contained;
        public bool _p2Contained;
        public float _t; // - Time to hit [0,1]
                         //  public void* _pPickData; // picked object (BvhObject3*)
        public vec2 _vNormal; //The normal of the plane the raycast hit.
                              //Do not include ray data for optimization.

        //public RaycastHit()
        //{
        //    reset();
        //}
        public bool trySetClosestHit(ref float closest_t)
        {
            //Easy way of iterating a closest hit.
            if (_bHit && (_t < closest_t))
            {
                closest_t = _t;
                return true;
            }
            return false;
        }
        public void reset()
        {
            _bHit = false;
            _p1Contained = false;
            _p2Contained = false;
            _t = float.MaxValue;
            //  _pPickData = NULL;
        }
        public void copyFrom(RaycastHit bh)
        {
            _bHit = bh._bHit;
            _p1Contained = bh._p1Contained;
            _p2Contained = bh._p2Contained;
            _t = bh._t;
        }
    }
    public struct ProjectedRay
    {
        public vec2 Origin;
        public vec2 Dir;
        public float _t;
        public vec2 _vNormal;

        // Found the following two cool optimizations on WIlliams et. al (U. Utah)
        public vec2 InvDir;
        public int[] Sign;

        public bool IsOpt { get; private set; }    // - return true if  we optimized this

        public float Length;// Max length

        public vec2 Begin() { return Origin; }
        public vec2 End() { return Origin + Dir; }

        public ProjectedRay(vec2 origin, vec2 dir)
        {
            Sign = new int[2];
            Origin = origin;
            Dir = dir;

            IsOpt = false;
            Length = float.MaxValue;//Must be maxvalue
            _t = float.MaxValue;
            _vNormal = new vec2(0, 0);

            //opt()
            //            //**New - optimization
            //http://people.csail.mit.edu/amy/papers/box-jgt.pdf
            //Don't set to zero. We need infinity (or large value) here.
            InvDir.x = 1.0f / Dir.x;
            InvDir.y = 1.0f / Dir.y;

            Sign[0] = (InvDir.x < 0) ? 1 : 0;
            Sign[1] = (InvDir.y < 0) ? 1 : 0;

            IsOpt = true;
        }
        //public void opt()
        //{



        //}
        public bool isHit()
        {
            return _t >= 0.0f && _t <= 1.0f;
        }
        public vec2 HitPoint()
        {
            vec2 ret = Begin() + (End() - Begin()) * _t;
            return ret;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Box2f
    {
        public vec2 Min;
        public vec2 Max;

        public float Width() { return Max.x - Min.x; }
        public float Height() { return Max.y - Min.y; }

        public vec2 TopRight() { return new vec2(Max.x, Min.y); }
        public vec2 BotRight() { return new vec2(Max.x, Max.y); }
        public vec2 BotLeft() { return new vec2(Min.x, Max.y); }
        public vec2 TopLeft() { return new vec2(Min.x, Min.y); }

        public void Construct(vec2 min, vec2 max)
        {
            Min = min; Max = max;
        }
        public Box2f(float x, float y, float w, float h)
        {
            Min = new vec2(x, y);
            Max = new vec2(w, h) + Min;
        }
        public Box2f(vec2 min, vec2 max)
        {
            Min = min;
            Max = max;
        }
        public vec2 Center()
        {
            return Min + (Max - Min) * 0.5f;
        }
        public static Box2f FlipBoxH(Box2f b, float w)
        {
            //Flip the box inside of a larger box (w)
            Box2f ret = new Box2f();
            ret.Min.x = w - b.Max.x;
            ret.Max.x = w - b.Min.x;

            ret.Min.y = b.Min.y;
            ret.Max.y = b.Max.y;
            return ret;
        }
        public static Box2f FlipBoxV(Box2f b, float h)
        {
            //Flip the box inside of a larger box (h)
            Box2f ret = new Box2f();
            ret.Min.y = h - b.Max.y;
            ret.Max.y = h - b.Min.y;

            ret.Min.x = b.Min.x;
            ret.Max.x = b.Max.x;
            return ret;
        }
        public Rectangle ToXNARect()
        {
            Rectangle r = new Rectangle();

            r.X = (int)(Min.x);
            r.Y = (int)(Min.y);
            r.Width = (int)(Max.x - Min.x);
            r.Height = (int)(Max.y - Min.y);

            return r;
        }

        public static Box2f GetIntersectionBox_Inclusive(Box2f a, Box2f b)
        {
            Box2f ret = new Box2f();

            ret.Min.x = Single.MaxValue;
            ret.Min.y = Single.MaxValue;
            ret.Max.x = -Single.MaxValue;
            ret.Max.y = -Single.MaxValue;


            if (a.Min.x >= b.Min.x && a.Min.x <= b.Max.x)
            {
                ret.Min.x = Math.Min(ret.Min.x, a.Min.x);
            }
            if (a.Max.x <= b.Max.x && a.Max.x >= b.Min.x)
            {
                ret.Max.x = Math.Max(ret.Max.x, a.Max.x);
            }
            if (a.Min.y >= b.Min.y && a.Min.y <= b.Max.y)
            {
                ret.Min.y = Math.Min(ret.Min.y, a.Min.y);
            }
            if (a.Max.y <= b.Max.y && a.Max.y >= b.Min.y)
            {
                ret.Max.y = Math.Max(ret.Max.y, a.Max.y);
            }

            if (b.Min.x >= a.Min.x && b.Min.x <= a.Max.x)
            {
                ret.Min.x = Math.Min(ret.Min.x, b.Min.x);
            }
            if (b.Max.x <= a.Max.x && b.Max.x >= a.Min.x)
            {
                ret.Max.x = Math.Max(ret.Max.x, b.Max.x);
            }
            if (b.Min.y >= a.Min.y && b.Min.y <= a.Max.y)
            {
                ret.Min.y = Math.Min(ret.Min.y, b.Min.y);
            }
            if (b.Max.y <= a.Max.y && b.Max.y >= a.Min.y)
            {
                ret.Max.y = Math.Max(ret.Max.y, b.Max.y);
            }
            return ret;
        }

        public void GenResetExtents()
        {
            Min = new vec2(Single.MaxValue, Single.MaxValue);
            Max = new vec2(-Single.MaxValue, -Single.MaxValue);
        }
        public void ExpandByPoint(vec2 v)
        {
            Min = vec2.Minv(Min, v);
            Max = vec2.Maxv(Max, v);
        }
        public bool BoxIntersect_EasyOut_Inclusive(Box2f cc)
        {
            return cc.Min.x <= Max.x && cc.Min.y <= Max.y && Min.x <= cc.Max.x && Min.y <= cc.Max.y;
        }
        public bool ContainsPointInclusive(vec2 point)
        {
            if (point.x < Min.x)
                return false;
            if (point.y < Min.y)
                return false;
            if (point.x > Max.x)
                return false;
            if (point.y > Max.y)
                return false;
            return true;
        }
        private vec2 bounds(int x)
        {
            if (x == 0) return Min;
            return Max;
        }
        public bool RayIntersect(ProjectedRay ray, ref RaycastHit bh)
        {
            if (ray.IsOpt == false)
            {
                //Error.
                System.Diagnostics.Debugger.Break();
            }

            float txmin, txmax, tymin, tymax;
            bool bHit;

            txmin = (bounds(ray.Sign[0]).x - ray.Origin.x) * ray.InvDir.x;
            txmax = (bounds(1 - ray.Sign[0]).x - ray.Origin.x) * ray.InvDir.x;

            tymin = (bounds(ray.Sign[1]).y - ray.Origin.y) * ray.InvDir.y;
            tymax = (bounds(1 - ray.Sign[1]).y - ray.Origin.y) * ray.InvDir.y;

            if ((txmin > tymax) || (tymin > txmax))
            {
                // if (bh != null)
                // {
                bh._bHit = false;
                // }
                return false;
            }
            if (tymin > txmin)
            {
                txmin = tymin;
            }
            if (tymax < txmax)
            {
                txmax = tymax;
            }

            bHit = ((txmin >= 0.0f) && (txmax <= ray.Length));

            //**Note changed 20151105 - this is not [0,1] this is the lenth along the line in which 
            // the ray enters and exits the cube, so any value less than the maximum is valid

            // if (bh != null)
            // {
            bh._bHit = bHit;
            bh._t = txmin;
            // }

            return bHit;
        }
    }
    public class BoxAAHit
    {
        public bool _bHit;  // Whether the ray intersected the box.
        public bool _p1Contained;
        public bool _p2Contained;
        public float _t; // - Time to hit [0,1]
    };
    public class PickRay
    {
        public vec3 Origin;
        public vec3 Dir;
        public float _length;
        public bool _isOpt;
        public vec3 InvDir;
        public int[] Sign = new int[3];

        public void Opt()
        {
            //**New - optimization
            //http://people.csail.mit.edu/amy/papers/box-jgt.pdf
            // if (Dir.X != 0.0f)
            InvDir.x = 1.0f / Dir.x;
            //   else
            //       InvDir.X = 0.0f;
            //   if (Dir.Y != 0.0f)
            InvDir.y = 1.0f / Dir.y;
            //      else
            //         InvDir.Y = 0.0f;
            //     if (Dir.Z != 0.0f)
            InvDir.z = 1.0f / Dir.z;
            //     else
            //         InvDir.Z = 0.0f;

            Sign[0] = Convert.ToInt32(InvDir.x < 0);
            Sign[1] = Convert.ToInt32(InvDir.y < 0);
            Sign[2] = Convert.ToInt32(InvDir.z < 0);

            _isOpt = true;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Box3f
    {
        public vec3 _vmin;
        public vec3 _vmax;

        public Box3f(vec3 min, vec3 max)
        {
            _vmin = min;
            _vmax = max;
        }

        public void Validate()
        {
            if (_vmax.x < _vmin.x)
            {
                throw new Exception("Bound box X was invalid.");
            }
            if (_vmax.y < _vmin.y)
            {
                throw new Exception("Bound box Y was invalid.");
            }
            if (_vmax.z < _vmin.z)
            {
                throw new Exception("Bound box Z was invalid.");
            }
        }
        /**
        *   @fn RayIntersect
        *   @brief Returns true if the given ray intersects this Axis aligned
        *   cube volume.
        *   @param bh - Reference to a BoxHit structure.
        *   @prarm ray - The ray to test against the box.
        *   @return true if ray intersects, false otherwise.
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
        private vec3 bounds(int in__)
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

            txmin = (bounds(ray.Sign[0]).x - ray.Origin.x) * ray.InvDir.x;
            txmax = (bounds(1 - ray.Sign[0]).x - ray.Origin.x) * ray.InvDir.x;

            tymin = (bounds(ray.Sign[1]).y - ray.Origin.y) * ray.InvDir.y;
            tymax = (bounds(1 - ray.Sign[1]).y - ray.Origin.y) * ray.InvDir.y;

            if ((txmin > tymax) || (tymin > txmax))
            {
                bh._bHit = false;
                return false;
            }
            if (tymin > txmin)
                txmin = tymin;
            if (tymax < txmax)
                txmax = tymax;

            tzmin = (bounds(ray.Sign[2]).z - ray.Origin.z) * ray.InvDir.z;
            tzmax = (bounds(1 - ray.Sign[2]).z - ray.Origin.z) * ray.InvDir.z;

            if ((txmin > tzmax) || (tzmin > txmax))
            {
                bh._bHit = false;
                return false;
            }
            if (tzmin > txmin)
                txmin = tzmin;
            if (tzmax < txmax)
                txmax = tzmax;

            bh._bHit = ((txmin > 0.0f) && (txmax <= ray._length));
            bh._t = txmin;

            return bh._bHit;
        }

        private bool containsInclusive(vec3 v)
        {
            return (
                (v.x >= _vmin.x) && (v.x <= _vmax.x) &&
                (v.y >= _vmin.y) && (v.y <= _vmax.y) &&
                (v.z >= _vmin.z) && (v.z <= _vmax.z)
                );
        }

    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ivec3
    {
        public Int32 x;
        public Int32 y;
        public Int32 z;
        public ivec3(Int32 dx, Int32 dy, Int32 dz)
        {
            x=dx;y=dy;z=dz;
        }
        public static ivec3 operator -(in ivec3 d)
        {
            return new ivec3(-d.x, -d.y, -d.z);
        }
        public static ivec3 operator +(in ivec3 a, in ivec3 b)
        {
            return new ivec3(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static ivec3 operator -(in ivec3 a, in ivec3 b)
        {
            return new ivec3(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        public static ivec3 operator *(in ivec3 a, in ivec3 b)
        {
            return new ivec3(a.x * b.x, a.y * b.y, a.z * b.z);
        }
        public static ivec3 operator /(in ivec3 a, in ivec3 b)
        {
            return new ivec3(a.x / b.x, a.y / b.y, a.z / b.z);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ivec4
    {
        public Int32 x;
        public Int32 y;
        public Int32 z;
        public Int32 w;
        public ivec4(Int32 dx, Int32 dy, Int32 dz, Int32 dw)
        {
            x = dx; y = dy; z = dz; w=dw;
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct vec3
    {
        public float x;
        public float y;
        public float z;

        public vec3(vec3 rhs)
        {
            this.x = rhs.x;
            this.y = rhs.y;
            this.z = rhs.z;
        }
        public vec3(float dx, float dy, float dz)
        {
            x = dx;
            y = dy;
            z = dz;
        }
        public vec3(int rhs)
        {
            x = (float)rhs;
            y = (float)rhs;
            z = (float)rhs;
        }
        public vec3(float rhs)
        {
            x = rhs;
            y = rhs;
            z = rhs;
        }
        public override string ToString()
        {
            return "" + x + "," + y+"," + z;
        }
        // template <class Tx>
        // vec3(const Vec2x<float>& rhs) {
        //   x = (float)rhs.x;
        //   y = (float)rhs.y;
        //   z = 0;  // This isn't correct. The problem is we get auto casted when we add.
        // }
        // template <class Tx>
        // vec3(const Vec2x<double>& rhs) {
        //   x = (float)rhs.x;
        //   y = (float)rhs.y;
        //   z = 0;  // This isn't correct. The problem is we get auto casted when we add.
        // }
        // template <class Tx>
        // vec3(const Vec4x<Tx>& rhs) {
        //   x = (float)rhs.x;
        //   y = (float)rhs.y;
        //   z = (float)rhs.z;
        // }

        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////

        public vec3 minv(in vec3 v_a, in vec3 v_b)
        {
            vec3 outv = new vec3();

            outv.x = Math.Min(v_a.x, v_b.x);
            outv.y = Math.Min(v_a.y, v_b.y);
            outv.z = Math.Min(v_a.z, v_b.z);

            return outv;
        }
        public vec3 maxv(in vec3 v_a, in vec3 v_b)
        {
            vec3 outv = new vec3();

            outv.x = Math.Max(v_a.x, v_b.x);
            outv.y = Math.Max(v_a.y, v_b.y);
            outv.z = Math.Max(v_a.z, v_b.z);

            return outv;
        }
        public vec3 maxv_a(in vec3 v_a, in vec3 v_b)
        {
            vec3 outv = new vec3();

            outv.x = Math.Max(Math.Abs(v_a.x), Math.Abs(v_b.x));
            outv.y = Math.Max(Math.Abs(v_a.y), Math.Abs(v_b.y));
            outv.z = Math.Max(Math.Abs(v_a.z), Math.Abs(v_b.z));
            return outv;
        }
        public float maxf_a(in vec3 v_a, in vec3 v_b)
        {
            vec3 tmp = maxv_a(v_a, v_b);
            return Math.Max(Math.Abs(tmp.x), Math.Max(Math.Abs(tmp.y), Math.Abs(tmp.z)));
        }
        public vec2 xz()
        {
            return new vec2(x, z);
        }
        public vec2 xy()
        {
            return new vec2(x, y);
        }


        //vec3 VEC3X_MIN()
        //{
        //    return vec3(-COMP_MAX < float >::m(), -COMP_MAX < float >::m(), -COMP_MAX < float >::m());
        //}

        //vec3 VEC3X_MAX()
        //{
        //    return vec3(COMP_MAX < float >::m(), COMP_MAX < float >::m(), COMP_MAX < float >::m());
        //}
        //#define VEC3_MIN (vec3(-FLT_MAX,-FLT_MAX,-FLT_MAX))
        //#define VEC3_MAX (vec3(FLT_MAX,FLT_MAX,FLT_MAX))
        public float length()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }
        public double lengthd()
        {
            double dx = (double)x;
            double dy = (double)y;
            double dz = (double)z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        public float length2()
        {
            return (x * x + y * y + z * z);
        }
        public float squaredLength()
        {
            return length2();
        }
        public vec3 normalize()
        {
            //we may be able to get away with rsqrt here...
            //but maybe not.
            float a = length();
            return normalize(a);
        }
        public vec3 normalize(float len)
        {
            //we may be able to get away with rsqrt here...
            //but maybe not.
            // We should allow the float to hit infinity if we try to divide zero
            if (len != 0)
            {
                float a1 = 1.0f / len;
                x *= a1;
                y *= a1;
                z *= a1;
            }
            else
            {
                x = y = z = 0;
            }
            return this;
        }
        public void len_and_norm(out vec3 n, out float len)
        {
            //Computes length and normal to avoid having do do len() then norm()
            len = length();
            n = this;
            n.normalize(len);
        }
        public vec3 normalized()
        {
            vec3 ret = new vec3(this);
            return ret.normalize();
        }
        public vec3 abs()
        {
            vec3 ret = new vec3(this);
            ret.x = Math.Abs(ret.x);
            ret.y = Math.Abs(ret.y);
            ret.z = Math.Abs(ret.z);
            return ret;
        }
        public float dot(in vec3 v)
        {
            return (x * v.x + y * v.y + z * v.z);
        }
        public float distance(in vec3 v1)
        {
            return ((this) - v1).length();
        }
        public float distance2(in vec3 v1)
        {
            return ((this) - v1).length2();
        }
        public vec3 cross(in vec3 v1)
        {
            vec3 vt;
            vt.x = (y * v1.z) - (v1.y * z);
            vt.y = (z * v1.x) - (v1.z * x);
            vt.z = (x * v1.y) - (v1.x * y);

            return vt;
        }
        public vec3 lerpTo(in vec3 v1, float t)
        {
            vec3 ret = this + (v1 - this) * t;
            return ret;
        }
        public vec3 clampTo(in vec3 vMin, in vec3 vMax)
        {
            //Technically we can just use the #define for clamp() to get the same result.
            //but my brain isn't working right now and i want to see this line for line
            vec3 outv = new vec3(this);

            if (outv.x < vMin.x)
            {
                outv.x = vMin.x;
            }
            if (outv.y < vMin.y)
            {
                outv.y = vMin.y;
            }
            if (outv.z < vMin.z)
            {
                outv.z = vMin.z;
            }

            if (outv.x > vMax.x)
            {
                outv.x = vMax.x;
            }
            if (outv.y > vMax.y)
            {
                outv.y = vMax.y;
            }
            if (outv.z > vMax.z)
            {
                outv.z = vMax.z;
            }

            return outv;
        }
        public static vec3 operator -(in vec3 d)
        {
            return new vec3(-d.x, -d.y, -d.z);
        }
        public static vec3 operator +(in vec3 a, in vec3 b)
        {
            return new vec3(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static vec3 operator -(in vec3 a, in vec3 b)
        {
            return new vec3(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        public static vec3 operator *(in vec3 a, in vec3 b)
        {
            return new vec3(a.x * b.x, a.y * b.y, a.z * b.z);
        }
        public static vec3 operator /(in vec3 a, in vec3 b)
        {
            return new vec3(a.x / b.x, a.y / b.y, a.z / b.z);
        }
        public static vec3 operator +(in vec3 a, float f)
        {
            return new vec3(a.x + f, a.y + f, a.z + f);
        }
        public static vec3 operator -(in vec3 a, float f)
        {
            return new vec3(a.x - f, a.y - f, a.z - f);
        }
        public static vec3 operator *(in vec3 a, float b)
        {
            return new vec3(a.x * b, a.y * b, a.z * b);
        }
        public static vec3 operator /(in vec3 a, float b)
        {
            return new vec3(a.x / b, a.y / b, a.z / b);
        }
        public static bool operator >(in vec3 v1, in vec3 v2)
        {
            return (v1.x > v2.x && v1.y > v2.y && v1.z > v2.z);
        }
        public static bool operator >=(in vec3 v1, in vec3 v2)
        {
            return (v1.x >= v2.x && v1.y >= v2.y && v1.z >= v2.z);
        }
        public static bool operator <(in vec3 v1, in vec3 v2)
        {
            return (v1.x < v2.x && v1.y < v2.y && v1.z < v2.z);
        }
        public static bool operator <=(in vec3 v1, in vec3 v2)
        {
            return (v1.x <= v2.x && v1.y <= v2.y && v1.z <= v2.z);
        }

        // template <class Tx>
        // Vec3x<Tx> Vec3x<Tx>::operator*(const Mat3x<Tx>& m) {
        //   Vec3x<Tx> ret;
        //   ret.x = (Tx)(m._m11 * x + m._m21 * x + m._m31 * x);
        //   ret.y = (Tx)(m._m12 * y + m._m22 * y + m._m32 * y);
        //   ret.z = (Tx)(m._m13 * z + m._m23 * z + m._m33 * z);
        //   return ret;
        // }
        //bool operator>(float f) {
        //  return (x > f && y > f && z > f);
        //}

        //bool operator>=(float f) {
        //  return (x >= f && y >= f && z >= f);
        //}

        //bool operator<(float f) {
        //  return (x<f && y<f && z<f);
        //}

        //bool operator<=(float f) {
        //  return (x <= f && y <= f && z <= f);
        //}

        //// Constructors

        //void construct(in vec3 rhs)
        //{
        //    x = rhs.x;
        //    y = rhs.y;
        //    z = rhs.z;
        //}

        //void construct(float dx, float dy, float dz)
        //{
        //    x = dx;
        //    y = dy;
        //    z = dz;
        //}

        //vec3 zero()
        //{
        //    return vec3(0, 0, 0);
        //}


        //uint32_t toUint() {
        //  uint32_t ret = (uint32_t)(
        //      ((uint32_t)0 << 16) |
        //      ((uint32_t)r() << 16) |
        //      ((uint32_t)g() << 8) |
        //      ((uint32_t)b()));
        //  return ret;
        //}

        //void fromUint(const uint32_t& i)
        //{
        //    r() = (i >> 16) & 0xFF;
        //    g() = (i >> 8) & 0xFF;
        //    b() = (i) & 0xFF;
        //}

        bool compareTo(in vec3 rhs)
        {
            vec3 lhs = this;

            if (lhs.x < rhs.x)
            {
                return true;
            }
            else if (lhs.x > rhs.x)
            {
                return false;
            }
            else
            {
                if (lhs.y < rhs.y)
                {
                    return true;
                }
                else if (lhs.y > rhs.y)
                {
                    return false;
                }
                else
                {
                    if (lhs.z < rhs.z)
                    {
                        return true;
                    }
                    else
                    {//if(lhs->z > rhs->z)
                        return false;
                    }
                }
            }
        }

        ////template < class float >
        ////vec3( const dvec3& rhs )
        ////{
        ////  x = (float)rhs.x;
        ////  y = (float)rhs.y;
        ////  z = (float)rhs.z;
        ////}

        //// - Vector shorthands
        public static vec3 normalize(in vec3 v1)
        {
            return (new vec3(v1)).normalized();
        }
        public static vec3 cross(in vec3 v1, in vec3 v2)
        {
            return (new vec3(v1)).cross(new vec3(v2));
        }
        ////template < typename float >
        ////vec3 rotate(in vec3 v1, const float& angle, in vec3 normal)
        ////{
        ////  //TODO: test.
        ////  mat3 m = mat3::getRotationRad(angle, normal);
        ////  vec3 ret = v1;
        ////  ret = ret*m;
        ////  return ret;
        ////}
        //template<typename Tx>
        public static float dot(in vec3 v1, in vec3 v2)
        {
            return (new vec3(v1)).dot(new vec3(v2));
        }
        //template<typename Tx>
        //void bilinear_interpolate(
        //    in vec3 a,
        //    in vec3 b,
        //    in vec3 c,
        //    in vec3 d,
        //    vec3& __out_ avg,
        //    float pct)
        //{
        //    vec3 v1, v2, v3;
        //    v1 = a + (b - a) * pct;
        //    v2 = c + (d - c) * pct;
        //    avg = v1 + (v2 - v1) * pct;
        //}
        //template<typename Tx>
        static void reflect(in vec3 v, in vec3 n, out vec3 v_r)
        {
            v_r = v - (n * n.dot(v)) * 2.0f;
        }
        //template<typename Tx>
        //void checkNormalOrZero()
        //{
        //    //Make sure the number is a normal FP number
        //    int cx = std::fpclassify(x);
        //    int cy = std::fpclassify(y);
        //    int cz = std::fpclassify(z);
        //    if (cx != FP_ZERO && cx != FP_NORMAL)
        //        x = 0.0f;
        //    if (cy != FP_ZERO && cy != FP_NORMAL)
        //        y = 0.0f;
        //    if (cz != FP_ZERO && cz != FP_NORMAL)
        //        z = 0.0f;
        //}
        //template<typename Tx>
        //bool isNormalFloat()
        //{
        //    bool b = true;

        //    //Make sure the number is a normal FP number
        //    int cx = std::fpclassify(x);
        //    int cy = std::fpclassify(y);
        //    int cz = std::fpclassify(z);
        //    //NAN
        //    if (cx == FP_NAN)
        //    {
        //        b = false;
        //    }
        //    if (cy == FP_NAN)
        //    {
        //        b = false;
        //    }
        //    if (cz == FP_NAN)
        //    {
        //        b = false;
        //    }
        //    ////DEN
        //    //If the number is too small who cares. Let it round to zero.
        //    //AssertOrThrow2(cx!= FP_SUBNORMAL);
        //    //AssertOrThrow2(cy!= FP_SUBNORMAL);
        //    //AssertOrThrow2(cz!= FP_SUBNORMAL);
        //    //INF
        //    if (cx == FP_INFINITE)
        //    {
        //        b = false;
        //    }
        //    if (cy == FP_INFINITE)
        //    {
        //        b = false;
        //    }
        //    if (cz == FP_INFINITE)
        //    {
        //        b = false;
        //    }

        //    return b;
        //}
        //template<typename Tx>
        //void checkNormalOrZeroAndLimitVector(float fMaxLength, bool bShowWarningMessage)
        //{
        //    //Normalize number
        //    checkNormalOrZero();

        //    // Make sure the vector length isn't too big.
        //    if (squaredLength() >= (fMaxLength * fMaxLength))
        //    {
        //        if (bShowWarningMessage == true)
        //            BRLogWarn("Object has launched into orbit: v=(", x, " ", y, " ", z, ")");
        //        *this = normalized() * fMaxLength;
        //    }
        //}

        //class Vec3Basis : public VirtualMemory {
        //public:
        //    vec3 _x, _y, _z;
        //};
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct mat3
    {
        public float _m11, _m12, _m13;
        public float _m21, _m22, _m23;
        public float _m31, _m32, _m33;
        public static int CompSize() { return 9; }
        public void setIdentity()
        {
            _m11 = _m22 = _m33 = 1.0f;
            _m12 = _m13 = _m21 = _m23 = _m31 = _m32 = 0;
        }
        public static mat3 operator +(in mat3 a, in mat3 b)
        {
            mat3 ret = new mat3();
            for (int i = 0; i < CompSize(); ++i)
            {
                ret[i] = a[i] + b[i];
            }
            return ret;
        }
        //operator[]
        public float this[int i]
        {
            get
            {
                if (i == 0)
                {
                    return this._m11;
                }
                else if (i == 1)
                {
                    return this._m12;
                }
                else if (i == 2)
                {
                    return this._m13;
                }
                else if (i == 3)
                {
                    return this._m21;
                }
                else if (i == 4)
                {
                    return this._m22;
                }
                else if (i == 5)
                {
                    return this._m23;
                }
                else if (i == 6)
                {
                    return this._m31;
                }
                else if (i == 7)
                {
                    return this._m32;
                }
                else if (i == 8)
                {
                    return this._m33;
                }
                else
                {
                    throw new Exception("mat3 array index out of bounds.");
                }
            }
            set {
                if (i == 0)
                {
                     this._m11 = value;
                }
                else if (i == 1)
                {
                     this._m12 = value;
                }
                else if (i == 2)
                {
                     this._m13 = value;
                }
                else if (i == 3)
                {
                     this._m21 = value;
                }
                else if (i == 4)
                {
                     this._m22 = value;
                }
                else if (i == 5)
                {
                     this._m23 = value;
                }
                else if (i == 6)
                {
                     this._m31 = value;
                }
                else if (i == 7)
                {
                     this._m32 = value;
                }
                else if (i == 8)
                {
                     this._m33 = value;
                }
                else
                {
                    throw new Exception("mat3 array index out of bounds.");
                }
            }
        }
        public void set(float val, int index)
        {
            if (index == 0) { _m11 = val; }
            else if (index == 1) { _m12 = val; }
            else if (index == 2) { _m13 = val; }
            else if (index == 3) { _m21 = val; }
            else if (index == 4) { _m22 = val; }
            else if (index == 5) { _m23 = val; }
            else if (index == 6) { _m31 = val; }
            else if (index == 7) { _m32 = val; }
            else if (index == 8) { _m33 = val; }
            else throw new Exception("Mat3 index out of range");
        }
        public mat3 adj()
        {
            // - The expanded cofactor adjoint.
            mat3 m = new mat3();
            m._m11 = 0.0f * ((_m22 * _m33) - (_m23 * _m32));
            m._m12 = 1.0f * ((_m21 * _m33) - (_m23 * _m31));
            m._m13 = 0.5f * ((_m21 * _m32) - (_m22 * _m31));
            m._m21 = 1.0f * ((_m12 * _m33) - (_m13 * _m32));
            m._m22 = 0.5f * ((_m11 * _m33) - (_m13 * _m31));
            m._m23 = (float)Math.Pow(-1.00f, 2 + 1) * ((_m12 * _m31) - (_m11 * _m32));
            m._m31 = 0.5f * ((_m12 * _m23) - (_m13 * _m22));
            m._m32 = (float)Math.Pow(-1.00f, 2 + 1) * ((_m11 * _m23) - (_m13 * _m21));
            m._m33 = 0.25f * ((_m11 * _m22) - (_m12 * _m21));
            return m;
        }
        public float det()
        {
            return (
                _m11 * _m22 * _m33 +
                _m21 * _m32 * _m13 +
                _m12 * _m23 * _m31 - (_m13 * _m22 * _m31) - (_m12 * _m21 * _m33) - (_m23 * _m32 * _m11));
        }
        public mat3 inverse()
        {
            mat3 m = adj();

            float d = m.det();
            for (int i = 0; i < CompSize(); ++i)
            {
                m[i] /= d;
            }
            return m;
        }


    }//mat3
    [StructLayout(LayoutKind.Sequential)]
    public struct mat4
    {
        public float _m11, _m12, _m13, _m14;   // indexes: 0,1,2,3
        public float _m21, _m22, _m23, _m24;  // 4,5,6,7...
        public float _m31, _m32, _m33, _m34;  //
        public float _m41, _m42, _m43, _m44;  //
        public int CompSize() { return 16; }
        public mat4(in mat4 rhs)
        {
            _m11 = rhs._m11;
            _m12 = rhs._m12;
            _m13 = rhs._m13;
            _m14 = rhs._m14;

            _m21 = rhs._m21;
            _m22 = rhs._m22;
            _m23 = rhs._m23;
            _m24 = rhs._m24;

            _m31 = rhs._m31;
            _m32 = rhs._m32;
            _m33 = rhs._m33;
            _m34 = rhs._m34;

            _m41 = rhs._m41;
            _m42 = rhs._m42;
            _m43 = rhs._m43;
            _m44 = rhs._m44;

        }
        public mat4(float t0, float t1, float t2, float t3,
            float t4, float t5, float t6, float t7,
        float t8, float t9, float t10, float t11,
        float t12, float t13, float t14, float t15)
        {
            _m11 = t0;
            _m12 = t1;
            _m13 = t2;
            _m14 = t3;
            _m21 = t4;
            _m22 = t5;
            _m23 = t6;
            _m24 = t7;
            _m31 = t8;
            _m32 = t9;
            _m33 = t10;
            _m34 = t11;
            _m41 = t12;
            _m42 = t13;
            _m43 = t14;
            _m44 = t15;
        }
        public OpenTK.Matrix4 ToOpenTK()
        {
            OpenTK.Matrix4 ret = new Mat4f(
            _m11,
            _m12,
            _m13,
            _m14,
            _m21,
            _m22,
            _m23,
            _m24,
            _m31,
            _m32,
            _m33,
            _m34,
            _m41,
            _m42,
            _m43,
            _m44
                );
            return ret;
        }
        public void set(float val, int index)
        {
            if (index == 0) { _m11 = val; }
            else if (index == 1) { _m12 = val; }
            else if (index == 2) { _m13 = val; }
            else if (index == 3) { _m14 = val; }
            else if (index == 4) { _m21 = val; }
            else if (index == 5) { _m22 = val; }
            else if (index == 6) { _m23 = val; }
            else if (index == 7) { _m24 = val; }
            else if (index == 8) { _m31 = val; }
            else if (index == 9) { _m32 = val; }
            else if (index == 10) { _m33 = val; }
            else if (index == 11) { _m34 = val; }
            else if (index == 12) { _m41 = val; }
            else if (index == 13) { _m42 = val; }
            else if (index == 14) { _m43 = val; }
            else if (index == 15) { _m44 = val; }
            else throw new Exception("Mat4 index out of range");
        }
        public static mat4 Identity()
        {
            return new mat4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
        }
        public void CopyTo(out mat4 to)
        {
            to._m11 = _m11;
            to._m12 = _m12;
            to._m13 = _m13;
            to._m14 = _m14;
            to._m21 = _m21;
            to._m22 = _m22;
            to._m23 = _m23;
            to._m24 = _m24;
            to._m31 = _m31;
            to._m32 = _m32;
            to._m33 = _m33;
            to._m34 = _m34;
            to._m41 = _m41;
            to._m42 = _m42;
            to._m43 = _m43;
            to._m44 = _m44;
        }
        public mat4 SetTranslation(float x, float y, float z)
        {
            _m41 = x;
            _m42 = y;
            _m43 = z;

            return this;
        }
        public Quaternion GetQuaternion()
        {
            float s0, s1, s2;
            int k0, k1, k2, k3;
            float[] q = new float[4];
            if ((_m11 + _m22 + _m33) > 0.0f)
            {
                k0 = 3;
                k1 = 2;
                k2 = 1;
                k3 = 0;
                s0 = 1.0f;
                s1 = 1.0f;
                s2 = 1.0f;
            }
            else if ((_m11 + _m22 > 0.0f) && (_m11 > _m33))
            {
                k0 = 0;
                k1 = 1;
                k2 = 2;
                k3 = 3;
                s0 = 1.0f;
                s1 = -1.0f;
                s2 = -1.0f;
            }
            else if (_m22 > _m33)
            {
                k0 = 1;
                k1 = 0;
                k2 = 3;
                k3 = 2;
                s0 = -1.0f;
                s1 = 1.0f;
                s2 = -1.0f;
            }
            else
            {
                k0 = 2;
                k1 = 3;
                k2 = 0;
                k3 = 1;
                s0 = -1.0f;
                s1 = -1.0f;
                s2 = 1.0f;
            }
            float t = (float)(s0 * _m11 + s1 * _m22 + s2 * _m33 + 1.0f);
            //assert(t>=0.0);
            //if( t==0.0 ) t=1e-10f;
            float s = (float)((1.0 / Math.Sqrt(t)) * 0.5f);

            q[k0] = s * t;

            q[k1] = (float)((_m12 - s2 * _m21) * s);
            q[k2] = (float)((_m31 - s1 * _m13) * s);
            q[k3] = (float)((_m23 - s0 * _m32) * s);

            Quaternion ret = new Quaternion(q[k0], q[k1], q[k2], q[k3]);
            return ret;
        }
        public static mat4 GetTranslation(in vec3 vTrans)
        {
            return GetTranslation(vTrans.x, vTrans.y, vTrans.z);
        }
        public static mat4 GetTranslation(float x, float y, float z)
        {
            mat4 m = Identity();

            m._m41 = x;
            m._m42 = y;
            m._m43 = z;

            return m;
        }
        public static mat4 GetRotation(float radians, in vec3 vAxis)
        {
            return GetRotation(radians, vAxis.x, vAxis.y, vAxis.z);
        }
        public static mat4 GetRotation(float radians, float x, float y, float z)
        {
            // - Reference: The openGL reference.http://pyopengl.sourceforge.net/documentation/manual/reference-GL.html
            mat4 Temp = Identity();

            float c = (float)Math.Cos(radians);
            float s = (float)Math.Sin(radians);
            float nc = 1 - c;
            // row matrix

            Temp._m11 = (x * x) * nc + c;
            Temp._m12 = (x * y) * nc + (z * s);
            Temp._m13 = (x * z) * nc - (y * s);
            Temp._m14 = (float)0.0;

            Temp._m21 = (y * x) * nc - (z * s);
            Temp._m22 = (y * y) * nc + c;
            Temp._m23 = (y * z) * nc + (x * s);
            Temp._m24 = (float)0.0;

            Temp._m31 = (z * x) * nc + (y * s);
            Temp._m32 = (z * y) * nc - (x * s);
            Temp._m33 = (z * z) * nc + c;
            Temp._m34 = (float)0.0;

            Temp._m41 = (float)0.0;
            Temp._m42 = (float)0.0;
            Temp._m43 = (float)0.0;
            Temp._m44 = (float)1.0;

            return Temp;
        }
        /**
        *  @fn getRotationToVector()
        *  @brief Returns a matrix that would, when multiplied by a vector, rotate that vector to align with this input vector v. 
        */
        public mat4 getRotationToVector(in vec3 v, in vec3 up)
        {
            if (v.x + v.y + v.z != 1.0)
            {
                v.normalize();
            }
            if (up.x + up.y + up.z != 1.0)
            {
                up.normalize();
            }

            float ang = (float)Math.Acos(v.dot(up));

            if (ang == 0.0f)
            {
                // no need for rotation
                return Identity();
            }

            vec3 perp = up.cross(v);

            if (perp.x + perp.y + perp.z == 0.0)  // vectors are direct opposites.
            {
                vec3 scuz = new vec3(up.y, -up.z, up.x);
                scuz.normalize();
                perp = scuz.cross(v);
            }

            //TODO: possible error may rotate opposite. (would need to do opposite cross of up.cross(v) scuz cross v

            return GetRotation(ang, perp.x, perp.y, perp.z);
        }
        public static mat4 getScale(in vec3 vScale)
        {
            return getScale(vScale.x, vScale.y, vScale.z);
        }
        public static mat4 getScale(float x, float y, float z)
        {
            mat4 m = Identity();
            m._m11 = x;
            m._m22 = y;
            m._m33 = z;

            return m;
        }
        public static mat4 projection(float fov_radians, float viewport_w, float viewport_h, float z_near, float z_far)
        {
            //setup a 3D projection matrix.
            //fov = field of view (radians)
            //viewport_w - width of viewport (swapchain image)
            //viewport_h - height of viewport.
            //near, far = near and far clipping planes.
            float e = (float)0.000001f;
            if (viewport_w == 0)
            {
                viewport_w = 1;
            }
            if (fov_radians > (float)Math.PI / 2.0f - e)
            {
                fov_radians = (float)Math.PI / 2.0f - e;
            }
            if (fov_radians < 1.0f + e)
            {
                fov_radians = 1.0f + e;
            }
            float vpWidth_2 = (float)Math.Tan(fov_radians * (float)0.5f) * z_near;
            float arat_1 = viewport_h / viewport_w;  // 1 / (w/h)
            float vw = vpWidth_2;
            float vh = vpWidth_2 * arat_1;

            return mat4.projection(
                z_near, z_far,
                -vw, vw,
                -vh, vh);
        }
        public static mat4 projection(float n, float f, float l, float r, float t, float b)
        {

            if (Gu.CoordinateSystem == CoordinateSystem.Lhs)
            {
                r = -r;
                l = -l;
            }

            mat4 m = new mat4();
            m._m11 = (float)(2 * n) / (r - l);
            m._m12 = (float)0;
            m._m13 = (float)0;
            m._m14 = (float)0;

            m._m21 = (float)0;
            m._m22 = (float)(2 * n) / (t - b);  // *-1.0f; // we added a neagtive here because IDK WHY this is not right
            m._m23 = (float)0;
            m._m24 = (float)0;

            m._m31 = (float)(r + l) / (r - l);
            m._m32 = (float)(t + b) / (t - b);
            m._m33 = (float)-(f + n) / (f - n);
            m._m34 = (float)-1;

            m._m41 = (float)0;
            m._m42 = (float)0;
            m._m43 = (float)-(2 * f * n) / (f - n);
            m._m44 = (float)0;

            return m;
        }
        public mat4 getOrientToVector(in vec3 iv, in vec3 iup)
        {
            mat4 m = Identity();

            vec3 v = new vec3(iv);
            vec3 up = new vec3(iup);

            if (up.x + up.y + up.z != 1.0)
            {
                up = up.normalize();
            }

            vec3 s = v.cross(up);
            vec3 u = s.cross(v);

            m._m11 = s.x;
            m._m21 = u.x;
            m._m31 = -v.x;

            m._m12 = s.y;
            m._m22 = u.y;
            m._m32 = -v.y;

            m._m13 = s.z;
            m._m23 = u.z;
            m._m33 = -v.z;

            return m;
        }
        public static mat4 getLookAt(in vec3 eye, in vec3 center, in vec3 up)
        {
            vec3 zaxis = (center - eye).normalize();
            vec3 xaxis = zaxis.cross(up).normalize();//This produces -y,+z -> -x
            vec3 yaxis = xaxis.cross(zaxis);
            zaxis *= -1;

            mat4 mm = new mat4();
            mm._m11 = xaxis.x; mm._m12 = yaxis.x; mm._m13 = zaxis.x; mm._m14 = 0;
            mm._m21 = xaxis.y; mm._m22 = yaxis.y; mm._m23 = zaxis.y; mm._m24 = 0;
            mm._m31 = xaxis.z; mm._m32 = yaxis.z; mm._m33 = zaxis.z; mm._m34 = 0;
            mm._m41 = -xaxis.dot(eye); mm._m42 = -yaxis.dot(eye); mm._m43 = -zaxis.dot(eye); mm._m44 = 1;

            return mm;
        }
        public mat4 translate(float x, float y, float z)
        {
            this *= GetTranslation(x, y, z);
            return this;
        }
        public mat4 translate(in vec3 v)
        {
            this *= GetTranslation(v.x, v.y, v.z);
            return this;
        }
        public mat4 transpose()
        {
            mat4 ret = new mat4(
                  _m11, _m21, _m31, _m41,
                  _m12, _m22, _m32, _m42,
                  _m13, _m23, _m33, _m43,
                  _m14, _m24, _m34, _m44
                );
            ret.CopyTo(out this);
            return this;
        }
        public mat4 transposed()
        {
            mat4 ret = new mat4(this);
            ret.transpose();
            return ret;
        }
        public void setIdentity()
        {
            _m11 = _m22 = _m33 = _m44 = 1.0f;
            _m21 = _m31 = _m41 = _m12 = 0.0f;
            _m32 = _m42 = _m13 = _m23 = 0.0f;
            _m43 = _m14 = _m24 = _m34 = 0.0f;
        }
        public float at(int row, int col)
        {
            return this[4 * row + col];
        }
        public int colNum(int ind)
        {
            return (int)Math.Floor((double)((double)ind / 4.0));
        }
        public int rowNum(int ind)
        {
            return ind % 4;
        }
        public static int nRows() { return 4; }
        public static int nCols() { return 4; }
        public mat3 minor(int r, int c)
        {
            //Returns the minor at the specified row and column.
            mat3 m = new mat3();
            m.setIdentity();
            if (r < 0 || r > 3 || c < 0 || c > 3)
            {
                return m;
            }

            int ind, mind = 0;
            for (ind = 0; ind < CompSize(); ++ind)
            {
                if (rowNum(ind) != r && colNum(ind) != c)
                {
                    m[mind++] = this[ind];
                }
            }

            return m;
        }
        float cofactor(int r, int c)
        {
            //Returns the cofactor of this matrix at the specified row and column.
            // I.E. The determinant of the minor.
            return (float)Math.Pow(-1.00f, (float)r + (float)c) * minor(r, c).det();  // ** May be incorrect
        }
        public float det()
        {
            float a = _m11 * ((_m22 * _m33 * _m44) + (_m23 * _m34 * _m42) + (_m24 * _m32 * _m43) - (_m22 * _m34 * _m43) - (_m23 * _m32 * _m44) - (_m24 * _m33 * _m42));
            float b = -_m12 * ((_m21 * _m33 * _m44) + (_m23 * _m34 * _m41) + (_m24 * _m31 * _m43) - (_m21 * _m34 * _m43) - (_m23 * _m31 * _m44) - (_m24 * _m33 * _m41));
            float c = +_m13 * ((_m21 * _m32 * _m44) + (_m22 * _m34 * _m41) + (_m24 * _m31 * _m42) - (_m21 * _m34 * _m42) - (_m22 * _m31 * _m44) - (_m24 * _m32 * _m41));
            float d = -_m14 * ((_m21 * _m32 * _m43) + (_m22 * _m33 * _m41) + (_m23 * _m31 * _m42) - (_m21 * _m33 * _m42) - (_m22 * _m31 * _m43) - (_m23 * _m32 * _m41));
            return a + b + c + d;
        }
        public mat4 adj()
        {
            //TODO:Optimize (transpose)
            mat4 m = new mat4(
                cofactor(0, 0), cofactor(0, 1), cofactor(0, 2), cofactor(0, 3),
                cofactor(1, 0), cofactor(1, 1), cofactor(1, 2), cofactor(1, 3),
                cofactor(2, 0), cofactor(2, 1), cofactor(2, 2), cofactor(2, 3),
                cofactor(3, 0), cofactor(3, 1), cofactor(3, 2), cofactor(3, 3));
            return m;
        }
        public mat4 invert()
        {
            //**Note:
            //Transpose of an orthogonal matrix is it's inverse
            //If we're orthogonal return the transpose.
            //   return this.transposed();

            // - Convert the matrix to Reduced RE form
            mat4 m = new mat4();
            // - If the determinant is zero, return m.
            if (det() == 0)
            {
                return m;
            }
            // - Classical adjoint is favored here over Gaussian and reduced row-echlon form.
            m = adj();
            float d = m.det();
            for (int i = 0; i < 16; ++i)
            {
                m[i] /= d;
            }

            m.CopyTo(out this);

            return this;
        }
        public mat4 inverseOf()
        {
            mat4 ret = new mat4(this);
            ret.invert();
            return ret;
        }
        public static mat4 operator +(in mat4 a, in mat4 b)
        {
            mat4 ret = new mat4();
            for (int i = 0; i < 16; ++i)
            {
                ret[i] = a[i] + b[i];
            }
            return ret;
        }
        public float this[int i]
        {
            //operator[]
            get
            {

                if (i == 0)
                {
                    return this._m11;
                }
                else if (i == 1)
                {
                    return this._m12;
                }
                else if (i == 2)
                {
                    return this._m13;
                }
                else if (i == 3)
                {
                    return this._m14;
                }
                else if (i == 4)
                {
                    return this._m21;
                }
                else if (i == 5)
                {
                    return this._m22;
                }
                else if (i == 6)
                {
                    return this._m23;
                }
                else if (i == 7)
                {
                    return this._m24;
                }
                else if (i == 8)
                {
                    return this._m31;
                }
                else if (i == 9)
                {
                    return this._m32;
                }
                else if (i == 10)
                {
                    return this._m33;
                }
                else if (i == 11)
                {
                    return this._m34;
                }
                else if (i == 12)
                {
                    return this._m41;
                }
                else if (i == 13)
                {
                    return this._m42;
                }
                else if (i == 14)
                {
                    return this._m43;
                }
                else if (i == 15)
                {
                    return _m44;
                }
                else
                {
                    throw new Exception("mat4 array index out of bounds.");
                }

            }
            set {
                if (i == 0)
                {
                     this._m11 = value;
                }
                else if (i == 1)
                {
                     this._m12 = value;
                }
                else if (i == 2)
                {
                     this._m13 = value;
                }
                else if (i == 3)
                {
                     this._m14 = value;
                }
                else if (i == 4)
                {
                     this._m21 = value;
                }
                else if (i == 5)
                {
                     this._m22 = value;
                }
                else if (i == 6)
                {
                     this._m23 = value;
                }
                else if (i == 7)
                {
                     this._m24 = value;
                }
                else if (i == 8)
                {
                     this._m31 = value;
                }
                else if (i == 9)
                {
                     this._m32 = value;
                }
                else if (i == 10)
                {
                     this._m33 = value;
                }
                else if (i == 11)
                {
                     this._m34 = value;
                }
                else if (i == 12)
                {
                     this._m41 = value;
                }
                else if (i == 13)
                {
                     this._m42 = value;
                }
                else if (i == 14)
                {
                     this._m43 = value;
                }
                else if (i == 15)
                {
                     _m44=value;
                }
                else
                {
                    throw new Exception("mat4 array index out of bounds.");
                }

            }
        }
        public static mat4 operator *(in mat4 a, in mat4 b)
        {
            if (a == b)
            {
                return a;
            }

            mat4 tMat = new mat4();
            //|11 21 31 41|   |11 21 31 41|
            //|12 22 32 42|   |12 22 32 42|
            //|13 23 33 43| * |13 23 33 43|
            //|14 24 34 44|   |14 24 34 44|
            //64 mul
            //48 add
            tMat._m11 = (a._m11 * b._m11) + (a._m12 * b._m21) + (a._m13 * b._m31) + (a._m14 * b._m41);
            tMat._m21 = (a._m21 * b._m11) + (a._m22 * b._m21) + (a._m23 * b._m31) + (a._m24 * b._m41);
            tMat._m31 = (a._m31 * b._m11) + (a._m32 * b._m21) + (a._m33 * b._m31) + (a._m34 * b._m41);
            tMat._m41 = (a._m41 * b._m11) + (a._m42 * b._m21) + (a._m43 * b._m31) + (a._m44 * b._m41);

            tMat._m12 = (a._m11 * b._m12) + (a._m12 * b._m22) + (a._m13 * b._m32) + (a._m14 * b._m42);
            tMat._m22 = (a._m21 * b._m12) + (a._m22 * b._m22) + (a._m23 * b._m32) + (a._m24 * b._m42);
            tMat._m32 = (a._m31 * b._m12) + (a._m32 * b._m22) + (a._m33 * b._m32) + (a._m34 * b._m42);
            tMat._m42 = (a._m41 * b._m12) + (a._m42 * b._m22) + (a._m43 * b._m32) + (a._m44 * b._m42);

            tMat._m13 = (a._m11 * b._m13) + (a._m12 * b._m23) + (a._m13 * b._m33) + (a._m14 * b._m43);
            tMat._m23 = (a._m21 * b._m13) + (a._m22 * b._m23) + (a._m23 * b._m33) + (a._m24 * b._m43);
            tMat._m33 = (a._m31 * b._m13) + (a._m32 * b._m23) + (a._m33 * b._m33) + (a._m34 * b._m43);
            tMat._m43 = (a._m41 * b._m13) + (a._m42 * b._m23) + (a._m43 * b._m33) + (a._m44 * b._m43);

            tMat._m14 = (a._m11 * b._m14) + (a._m12 * b._m24) + (a._m13 * b._m34) + (a._m14 * b._m44);
            tMat._m24 = (a._m21 * b._m14) + (a._m22 * b._m24) + (a._m23 * b._m34) + (a._m24 * b._m44);
            tMat._m34 = (a._m31 * b._m14) + (a._m32 * b._m24) + (a._m33 * b._m34) + (a._m34 * b._m44);
            tMat._m44 = (a._m41 * b._m14) + (a._m42 * b._m24) + (a._m43 * b._m34) + (a._m44 * b._m44);

            return tMat;
        }
        public static vec4 operator *(in mat4 m, in vec4 v)
        {
            vec4 vret = new vec4(
                    m._m11 * v.x + m._m21 * v.y + m._m31 * v.z + m._m41 * v.w,
                    m._m12 * v.x + m._m22 * v.y + m._m32 * v.z + m._m42 * v.w,
                    m._m13 * v.x + m._m23 * v.y + m._m33 * v.z + m._m43 * v.w,
                    m._m14 * v.x + m._m24 * v.y + m._m34 * v.z + m._m44 * v.w);
            return vret;
        }
        public static bool operator ==(in mat4 lhs, in mat4 rhs)
        {
            for (int i = 0; i < 16; ++i)
            {
                if (lhs[i] != rhs[i])
                {
                    return false;
                }
            }
            return true;
        }
        public static bool operator !=(in mat4 lhs, in mat4 rhs)
        {
            return !(lhs == rhs);
        }
        public vec3 getTranslation()
        {
            vec3 ret = new vec3();
            ret.x = _m41;
            ret.y = _m42;
            ret.z = _m43;
            return ret;
        }
        public void setTranslation(in vec3 vec)
        {
            _m41 = vec.x;
            _m42 = vec.y;
            _m43 = vec.z;
        }
        public void setTranslationX(float x)
        {
            _m41 = x;
        }
        public void setTranslationY(float y)
        {
            _m42 = y;
        }
        public void setTranslationZ(float z)
        {
            _m43 = z;
        }
        public float getTranslationX()
        {
            return _m41;
        }
        public float getTranslationY()
        {
            return _m42;
        }
        public float getTranslationZ()
        {
            return _m43;
        }
        public vec4 getTranslationVector()
        {
            return new vec4(_m41, _m42, _m43, _m44);
        }
        public mat4 getOrtho(float left, float right, float top,
                                                   float bottom, float neard, float fard)
        {
            mat4 mm = new mat4();

            float a1 = (float)2.0 / (right - left);
            float a2 = (float)2.0 / (top - bottom);   //IDK WY
            float a3 = (float)-2.0 / (fard - neard);  //IDK WY
            float t1 = (right + left) / (right - left) * (float)-1.0;
            float t2 = (top + bottom) / (top - bottom) * (float)-1.0;
            float t3 = (fard + neard) / (fard - neard) * (float)-1.0;

            //Row major order version
            //mm._m11 =a1, mm._m12 = 0, mm._m13 = 0, mm._m14 =t1,
            //mm._m21 = 0, mm._m22 =a2, mm._m23 = 0, mm._m24 =t2,
            //mm._m31 = 0, mm._m32 = 0, mm._m33 =a3, mm._m34 =t3,
            //mm._m41 = 0, mm._m42 = 0, mm._m43 = 0, mm._m44 = 1;

            // ** OpenGL version - the transpose of the former.
            mm._m11 = a1; mm._m12 = 0; mm._m13 = 0; mm._m14 = 0;
            mm._m21 = 0; mm._m22 = a2; mm._m23 = 0; mm._m24 = 0;
            mm._m31 = 0; mm._m32 = 0; mm._m33 = a3; mm._m34 = 0;
            mm._m41 = t1; mm._m42 = t2; mm._m43 = t3; mm._m44 = 1;

            return mm;
        }


        public void decompose(out vec4 pos, out mat4 rot, out vec4 scale)
        {
            //http://math.stackexchange.com/questions/237369/given-this-transformation-matrix-how-do-i-decompose-it-into-translation-rotati
            //11  21  31  41 << Don't use gl order
            //12  22  32  42
            //13  23  33  43
            //14  24  34  44
            pos = new vec4();
            rot = new mat4();
            scale = new vec4();

            pos = getTranslationVector();

            scale.x = new vec3(_m11, _m21, _m31).length();
            scale.y = new vec3(_m12, _m22, _m32).length();
            scale.z = new vec3(_m13, _m23, _m33).length();

            rot.setIdentity();
            rot._m11 = _m11 / scale.x;
            rot._m21 = _m21 / scale.x;
            rot._m31 = _m31 / scale.x;

            rot._m12 = _m12 / scale.y;
            rot._m22 = _m22 / scale.y;
            rot._m32 = _m32 / scale.y;

            rot._m13 = _m13 / scale.z;
            rot._m23 = _m23 / scale.z;
            rot._m33 = _m33 / scale.z;
        }

        public void decompose(out vec4 pos, out vec4 rot, out vec4 scale, bool bDegreeRotation)
        {
            pos = new vec4();
            rot = new vec4();
            scale = new vec4();

            mat4 mOut = new mat4();
            decompose(out pos, out mOut, out scale);

            Quaternion q = mOut.GetQuaternion();

            q.getAxisAngle(out rot);
            if (bDegreeRotation)
            {
                rot.w = MathUtils.ToDegrees(rot.w);
            }
        }

        // bool parse(std::string tok, mat4 mOut) {
        //   // - Parse csv matrix string.

        //   size_t n = 0;
        //   char c;
        //   float mat[16];
        //   std::string val = "";
        //   int mat_ind = 0;

        //   while (n < tok.length()) {
        //     c = tok[n++];
        //     if (c == ',' || c == '\n' || n == tok.length()) {
        //       mat[mat_ind++] = TypeConv::strToFloat(val);
        //       val = "";
        //     }
        //     else if (isalnum(c) || c == '-' || c == '.' || c == '+' || c == 'e') {
        //       val += c;
        //     }
        //   }

        //   mOut = mat;

        //   return true;
        // }


    }//mat4
    [StructLayout(LayoutKind.Sequential)]
    public struct Quaternion
    {
        public float x, y, z, w;

        public Quaternion(float dx, float dy, float dz, float dw)
        {
            x = dx;
            y = dy;
            z = dz;
            w = dw;
        }
        public void construct(float dx, float dy, float dz, float dw)
        {
            x = dx;
            y = dy;
            z = dz;
            w = dw;
        }
        public float Dot(in Quaternion rhs)
        {
            return (x * rhs.x + y * rhs.y + z * rhs.z + w * rhs.w);
        }
        public float mag()
        {
            return w * w + (x * x + y * y + z * z);
        }
        public Quaternion inverse()
        {
            float L = 1 / mag();
            Quaternion outv;
            outv.w = L * w;
            outv.x = -L * x;
            outv.y = -L * y;
            outv.z = -L * z;
            return outv;
        }
        public static Quaternion operator *(in Quaternion lhs, in Quaternion rhs)
        {
            Quaternion ret;

            //Normal quaternion multiplication
            //Q(w,v) = w=w1w2 - v1 dot v2, v= w1v2 + w2v1 + v2 cross v1.

            // - This method here is that of backwards multiplication
            // used in the 3D Math Primer For Graphics and Game Development:
            //    w=w1w2 - v1 dot v2, v= w1v2 + w2v1 + v1 cross v2.
            // this allows the quaternion rotations to be concatenated in a left to right order.
            ret.x = lhs.w * rhs.x + rhs.w * lhs.x + (lhs.y * rhs.z) - (rhs.y * lhs.z);
            ret.y = lhs.w * rhs.y + rhs.w * lhs.y + (lhs.z * rhs.x) - (rhs.z * lhs.x);
            ret.z = lhs.w * rhs.z + rhs.w * lhs.z + (lhs.x * rhs.y) - (rhs.x * lhs.y);
            ret.w = lhs.w * rhs.w - (lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z);
            return ret;
        }
        public static Quaternion operator *(in Quaternion q, float f)
        {
            Quaternion outv;
            outv.x = q.x * f;
            outv.y = q.y * f;
            outv.z = q.z * f;
            outv.w = q.w * f;
            return outv;
        }
        public static Quaternion operator +(in Quaternion lhs, in Quaternion rhs)
        {
            Quaternion outv;
            outv.x = lhs.x + rhs.x;
            outv.y = lhs.y + rhs.y;
            outv.z = lhs.z + rhs.z;
            outv.w = lhs.w + rhs.w;
            return outv;
        }
        public Quaternion slerpTo(in Quaternion rhs, float t)
        {
            //SLERP Spherical Linear interpolate this quaternion to rhs.
            // @param rhs The Quat to slerp
            // @param t Interpolation value [0 to 1]
            Quaternion ret;
            float s0, s1, sinAng, ang, cosAng, absAng;
            float sinSqr;

            cosAng = Dot(rhs);
            absAng = (float)Math.Abs(cosAng);

            if ((1 - absAng) > 1e-6f)
            {
                sinSqr = 1.0f - absAng * absAng;
                sinAng = 1.0f / (float)Math.Sqrt(sinSqr);
                ang = (float)Math.Atan2(sinSqr * sinAng, absAng);
                s0 = (float)Math.Sin((1.0f - t) * ang) * sinAng;
                s1 = (float)Math.Cos(t * ang) * sinAng;
            }
            else
            {
                s0 = (1.0f - t);
                s1 = t;
            }
            s1 = (cosAng >= 0.0f) ? s1 : -s1;
            ret.x = s0 * x + s1 * rhs.x;
            ret.y = s0 * y + s1 * rhs.y;
            ret.z = s0 * z + s1 * rhs.z;
            ret.w = s0 * w + s1 * rhs.w;

            return ret;
        }
        public static Quaternion operator *(in Quaternion q, in vec3 v)
        {
            return new Quaternion(q.x * v.x, q.y * v.y, q.z * v.z, q.w);
        }
        public vec3 vectorPart()
        {
            return new vec3(x, y, z);
        }
        public vec3 rotatePoint(in vec3 vin)
        {
            return ((inverse() * vin) * this).vectorPart();
        }
        public void getAxisAngle(out vec4 v)
        {
            //http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToAngle/
            v = new vec4();

            if (w == (float)1.0)
            {
                //Avoid divide by 0,( 1 - (cos(0) = 1)) =0
                v.x = v.z = v.w = 0;
                v.y = 1;
                return;
            }

            v.w = 2.0f * (float)Math.Acos(w);

            float w2 = w * w;
            float srw2_1 = 1.0f / (float)Math.Sqrt(1.0f - w2);
            v.x = x * srw2_1;
            v.y = y * srw2_1;
            v.z = z * srw2_1;
        }
        public mat4 toMat4()
        {
            //Convert quaternion into to mat4
            mat4 m = new mat4();
            m._m11 = 1 - 2 * y * y - 2 * z * z;
            m._m12 = 2 * x * y + 2 * w * z;
            m._m13 = 2 * x * z - 2 * w * y;
            m._m14 = 0;
            m._m21 = 2 * x * y - 2 * w * z;
            m._m22 = 1 - 2 * x * x - 2 * z * z;
            m._m23 = 2 * y * z + 2 * w * x;
            m._m24 = 0;
            m._m31 = 2 * x * z + 2 * w * y;
            m._m32 = 2 * y * z - 2 * w * x;
            m._m33 = 1 - 2 * x * x - 2 * y * y;
            m._m34 = 0;
            m._m41 = 0;
            m._m42 = 0;
            m._m43 = 0;
            m._m44 = 1;

            return m;
        }

    }//Quaternion


}