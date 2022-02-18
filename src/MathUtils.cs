﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
//using Microsoft.Xna.Framework;

//Copied this stuff from legend of \kevin
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;

namespace PirateCraft
{


   #region MathUtils

   public class MathUtils
   {
      public const float M_PI = (float)(Math.PI);
      public const float M_2PI = (float)(Math.PI * 2.0f);
      public const float M_PI_2 = (float)(Math.PI * 2.0f);
      public static float sqrtf(float f)
      {
         return (float)Math.Sqrt(f);
      }
      public static float sinf(float f)
      {
         return (float)Math.Sin(f);
      }
      public static float cosf(float f)
      {
         return (float)Math.Cos(f);
      }
      public static float tanf(float f)
      {
         return (float)Math.Tan(f);
      }
      public static float asinf(float f)
      {
         return (float)Math.Asin(f);
      }
      public static float acosf(float f)
      {
         return (float)Math.Acos(f);
      }
      public static float atanf(float f)
      {
         return (float)Math.Atan(f);
      }
      public static float Clamp(float f, float a, float b)
      {
         return Math.Max(Math.Min(f, b), a);
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
      public static Vec2f DecomposeRotation(float r)
      {
         //Turn a rotation into a vector (for chars mostly)
         float r2 = r - (float)Math.PI * 0.5f;
         Vec2f dxy = new Vec2f(
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
         if (Math.Abs(ray.Dir.X) < 1E-06f)
         {
            if ((ray.Origin.X < box.Min.X) || (ray.Origin.X > box.Max.X))
            {
               return null;
            }
         }
         else
         {
            float num11 = 1f / ray.Dir.X;
            float num8 = (box.Min.X - ray.Origin.X) * num11;
            float num7 = (box.Max.X - ray.Origin.X) * num11;
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
         if (Math.Abs(ray.Dir.Y) < 1E-06f)
         {
            if ((ray.Origin.Y < box.Min.Y) || (ray.Origin.Y > box.Max.Y))
            {
               return null;
            }
         }
         else
         {
            float num10 = 1f / ray.Dir.Y;
            float num6 = (box.Min.Y - ray.Origin.Y) * num10;
            float num5 = (box.Max.Y - ray.Origin.Y) * num10;
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
   //public class Plane2f
   //{
   //    public Plane2f() { }
   //    public float D;
   //    public Vec2f N;
   //    public Plane2f(Vec2f n, Vec2f pt)
   //    {
   //        D = -n.Dot(pt);
   //        N = n;
   //    }
   //    public float IntersectLine(Vec2f p1, Vec2f p2)
   //    {
   //        float t = -(N.Dot(p1) + D) / ((p2 - p1).Dot(N));
   //        return t;
   //    }
   //}
   //public class Vec2fEqualityComparer : IEqualityComparer<Vec2f>
   //{
   //    public bool Equals(Vec2f x, Vec2f y)
   //    {
   //        return x.X == y.X && x.Y == y.Y;
   //    }

   //    public int GetHashCode(Vec2f x)
   //    {
   //        return x.x.GetHashCode() + x.y.GetHashCode();
   //    }
   //}
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

      public static Vec2f operator *(mat2 a, Vec2f b)
      {
         Vec2f r = new Vec2f(0, 0);

         r.X = a.m[0] * b.X + a.m[1] * b.Y;
         r.Y = a.m[2] * b.X + a.m[3] * b.Y;
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
   //public class iVec2fEqualityComparer : IEqualityComparer<iVec2f>
   //{
   //    public bool Equals(iVec2f x, iVec2f y)
   //    {
   //        return x.X == y.X && x.Y == y.Y;
   //    }

   //    public int GetHashCode(iVec2f x)
   //    {
   //        return x.x.GetHashCode() + x.y.GetHashCode();
   //    }
   //}
   [StructLayout(LayoutKind.Sequential)]
   public struct Line2f
   {
      public Vec2f p0;
      public Vec2f p1;
   }
   [StructLayout(LayoutKind.Sequential)]
   public struct Line3f
   {
      public Vec3f p0;
      public Vec3f p1;
   }
   //[StructLayout(LayoutKind.Sequential)]
   //public struct Vec2f
   //{
   //    public Vec2f(Point p)
   //    {
   //        x = (float)p.X;
   //        y = (float)p.Y;
   //    }
   //    public float x, y;
   //    public Vec2f construct(float a, float b) { x = a; y = b; return this; }
   //    //public Vec2f() { }
   //    public Vec2f(Vec2f dxy) { x = dxy.X; y = dxy.Y; }
   //    public Vec2f(float dx, float dy) { x = dx; y = dy; }
   //    public Vec2f(OpenTK.Vector2 v) { x = v.X; y = v.Y; }//From XNA's Vector2
   //    public float Len() { return (float)Math.Sqrt((x * x) + (y * y)); }

   //    public Vec2f Perp()
   //    {
   //        //Perpendicular
   //        return new Vec2f(y, -x);
   //    }
   //    public void Normalize()
   //    {
   //        float l = Len();
   //        if (l != 0)
   //        {
   //            x /= l;
   //            y /= l;
   //        }
   //        else
   //        {
   //            x = 0; y = 0;
   //        }

   //    }
   //    public Vec2f Normalized()
   //    {
   //        Vec2f v = new Vec2f(this);
   //        v.Normalize();
   //        return v;

   //    }
   //    public float Len2() { return Dot(this, this); }
   //    public OpenTK.Vector2 toXNA() { return new OpenTK.Vector2(x, y); }


   //    static public implicit operator Vec2f(float f)
   //    {
   //        return new Vec2f(f, f);
   //    }
   //    //public static Vec2f operator =(Vec2f a, float f)
   //    //{
   //    //    return new Vec2f(f, f);
   //    //}
   //    public static float Dot(Vec2f a, Vec2f b)
   //    {
   //        return (a.X * b.X) + (a.Y * b.Y);
   //    }
   //    public float Dot(Vec2f b)
   //    {
   //        return (x * b.X) + (y * b.Y);
   //    }
   //    public static Vec2f operator -(Vec2f d)
   //    {
   //        return new Vec2f(-d.X, -d.Y);
   //    }
   //    public static Vec2f operator +(Vec2f a, Vec2f b)
   //    {
   //        return new Vec2f(a.X + b.X, a.Y + b.Y);
   //    }
   //    public static Vec2f operator -(Vec2f a, Vec2f b)
   //    {
   //        return new Vec2f(a.X - b.X, a.Y - b.Y);
   //    }
   //    public static Vec2f operator *(Vec2f a, float b)
   //    {
   //        return new Vec2f(a.X * b, a.Y * b);
   //    }
   //    public static Vec2f operator *(Vec2f a, Vec2f b)
   //    {
   //        return new Vec2f(a.X * b.X, a.Y * b.Y);
   //    }
   //    public static Vec2f operator /(Vec2f a, float b)
   //    {
   //        return new Vec2f(a.X / b, a.Y / b);
   //    }
   //    public static Vec2f operator -(Vec2f a, float f)
   //    {
   //        return new Vec2f(a.X - f, a.Y - f);
   //    }
   //    public static Vec2f Minv(Vec2f a, Vec2f b)
   //    {
   //        Vec2f ret = new Vec2f();
   //        ret.X = (float)Math.Min(a.X, b.X);
   //        ret.Y = (float)Math.Min(a.Y, b.Y);

   //        return ret;
   //    }
   //    public static Vec2f Maxv(Vec2f a, Vec2f b)
   //    {
   //        Vec2f ret = new Vec2f();
   //        ret.X = (float)Math.Max(a.X, b.X);
   //        ret.Y = (float)Math.Max(a.Y, b.Y);
   //        return ret;
   //    }

   //}
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
   public struct uVec2f
   {
      public uVec2f(int dx, int dy) { X = dx; Y = dy; }
      public Int32 X { get; set; }
      public Int32 Y { get; set; }
      static public implicit operator uVec2f(int f)
      {
         return new uVec2f(f, f);
      }
      public static uVec2f operator -(uVec2f d)
      {
         return new uVec2f(-d.X, -d.Y);
      }
      public static uVec2f operator +(uVec2f a, uVec2f b)
      {
         return new uVec2f(a.X + b.X, a.Y + b.Y);
      }
      public static uVec2f operator -(uVec2f a, uVec2f b)
      {
         return new uVec2f(a.X - b.X, a.Y - b.Y);
      }
      public static uVec2f operator *(uVec2f a, int b)
      {
         return new uVec2f(a.X * b, a.Y * b);
      }
      public static uVec2f operator *(uVec2f a, uVec2f b)
      {
         return new uVec2f(a.X * b.X, a.Y * b.Y);
      }
      public static uVec2f operator /(uVec2f a, int b)
      {
         return new uVec2f(a.X / b, a.Y / b);
      }
      public static uVec2f operator -(uVec2f a, int f)
      {
         return new uVec2f(a.X - f, a.Y - f);
      }
   }


   public struct RaycastHit
   {
      public bool _bHit;    // Whether the ray intersected the box.
      public bool _p1Contained;
      public bool _p2Contained;
      public float _t; // - Time to hit [0,1]
                       //  public void* _pPickData; // picked object (BvhObject3*)
      public Vec2f _vNormal; //The normal of the plane the raycast hit.
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
      public Vec2f Origin;
      public Vec2f Dir;
      public float _t;
      public Vec2f _vNormal;

      // Found the following two cool optimizations on WIlliams et. al (U. Utah)
      public Vec2f InvDir;
      public int[] Sign;

      public bool IsOpt { get; private set; }    // - return true if  we optimized this

      public float Length;// Max length

      public Vec2f Begin() { return Origin; }
      public Vec2f End() { return Origin + Dir; }

      public ProjectedRay(Vec2f origin, Vec2f dir)
      {
         Sign = new int[2];
         Origin = origin;
         Dir = dir;

         IsOpt = false;
         Length = float.MaxValue;//Must be maxvalue
         _t = float.MaxValue;
         _vNormal = new Vec2f(0, 0);

         //opt()
         //            //**New - optimization
         //http://people.csail.mit.edu/amy/papers/box-jgt.pdf
         //Don't set to zero. We need infinity (or large value) here.
         InvDir.X = 1.0f / Dir.X;
         InvDir.Y = 1.0f / Dir.Y;

         Sign[0] = (InvDir.X < 0) ? 1 : 0;
         Sign[1] = (InvDir.Y < 0) ? 1 : 0;

         IsOpt = true;
      }
      //public void opt()
      //{



      //}
      public bool isHit()
      {
         return _t >= 0.0f && _t <= 1.0f;
      }
      public Vec2f HitPoint()
      {
         Vec2f ret = Begin() + (End() - Begin()) * _t;
         return ret;
      }
   }
   [StructLayout(LayoutKind.Sequential)]
   public struct Box2f
   {
      public Vec2f Min;
      public Vec2f Max;

      public float Width() { return Max.X - Min.X; }
      public float Height() { return Max.Y - Min.Y; }

      public Vec2f TopRight() { return new Vec2f(Max.X, Min.Y); }
      public Vec2f BotRight() { return new Vec2f(Max.X, Max.Y); }
      public Vec2f BotLeft() { return new Vec2f(Min.X, Max.Y); }
      public Vec2f TopLeft() { return new Vec2f(Min.X, Min.Y); }

      public void Construct(Vec2f min, Vec2f max)
      {
         Min = min; Max = max;
      }
      public Box2f(float x, float y, float w, float h)
      {
         Min = new Vec2f(x, y);
         Max = new Vec2f(w, h) + Min;
      }
      public Box2f(Vec2f min, Vec2f max)
      {
         Min = min;
         Max = max;
      }
      public Vec2f Center()
      {
         return Min + (Max - Min) * 0.5f;
      }
      public static Box2f FlipBoxH(Box2f b, float w)
      {
         //Flip the box inside of a larger box (w)
         Box2f ret = new Box2f();
         ret.Min.X = w - b.Max.X;
         ret.Max.X = w - b.Min.X;

         ret.Min.Y = b.Min.Y;
         ret.Max.Y = b.Max.Y;
         return ret;
      }
      public static Box2f FlipBoxV(Box2f b, float h)
      {
         //Flip the box inside of a larger box (h)
         Box2f ret = new Box2f();
         ret.Min.Y = h - b.Max.Y;
         ret.Max.Y = h - b.Min.Y;

         ret.Min.X = b.Min.X;
         ret.Max.X = b.Max.X;
         return ret;
      }
      public Rectangle ToXNARect()
      {
         Rectangle r = new Rectangle();

         r.X = (int)(Min.X);
         r.Y = (int)(Min.Y);
         r.Width = (int)(Max.X - Min.X);
         r.Height = (int)(Max.Y - Min.Y);

         return r;
      }

      public static Box2f GetIntersectionBox_Inclusive(Box2f a, Box2f b)
      {
         Box2f ret = new Box2f();

         ret.Min.X = Single.MaxValue;
         ret.Min.Y = Single.MaxValue;
         ret.Max.X = -Single.MaxValue;
         ret.Max.Y = -Single.MaxValue;


         if (a.Min.X >= b.Min.X && a.Min.X <= b.Max.X)
         {
            ret.Min.X = Math.Min(ret.Min.X, a.Min.X);
         }
         if (a.Max.X <= b.Max.X && a.Max.X >= b.Min.X)
         {
            ret.Max.X = Math.Max(ret.Max.X, a.Max.X);
         }
         if (a.Min.Y >= b.Min.Y && a.Min.Y <= b.Max.Y)
         {
            ret.Min.Y = Math.Min(ret.Min.Y, a.Min.Y);
         }
         if (a.Max.Y <= b.Max.Y && a.Max.Y >= b.Min.Y)
         {
            ret.Max.Y = Math.Max(ret.Max.Y, a.Max.Y);
         }

         if (b.Min.X >= a.Min.X && b.Min.X <= a.Max.X)
         {
            ret.Min.X = Math.Min(ret.Min.X, b.Min.X);
         }
         if (b.Max.X <= a.Max.X && b.Max.X >= a.Min.X)
         {
            ret.Max.X = Math.Max(ret.Max.X, b.Max.X);
         }
         if (b.Min.Y >= a.Min.Y && b.Min.Y <= a.Max.Y)
         {
            ret.Min.Y = Math.Min(ret.Min.Y, b.Min.Y);
         }
         if (b.Max.Y <= a.Max.Y && b.Max.Y >= a.Min.Y)
         {
            ret.Max.Y = Math.Max(ret.Max.Y, b.Max.Y);
         }
         return ret;
      }

      public void GenResetExtents()
      {
         Min = new Vec2f(Single.MaxValue, Single.MaxValue);
         Max = new Vec2f(-Single.MaxValue, -Single.MaxValue);
      }
      public void ExpandByPoint(Vec2f v)
      {
         Min = Vec2f.ComponentMin(Min, v);// Vec2f.Minv(Min, v);
         Max = Vec2f.ComponentMax(Max, v);// Vec2f.Maxv(Max, v);
      }
      public bool BoxIntersect_EasyOut_Inclusive(Box2f cc)
      {
         return cc.Min.X <= Max.X && cc.Min.Y <= Max.Y && Min.X <= cc.Max.X && Min.Y <= cc.Max.Y;
      }
      public bool ContainsPointInclusive(Vec2f point)
      {
         if (point.X < Min.X)
            return false;
         if (point.Y < Min.Y)
            return false;
         if (point.X > Max.X)
            return false;
         if (point.Y > Max.Y)
            return false;
         return true;
      }
      private Vec2f bounds(int x)
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

         txmin = (bounds(ray.Sign[0]).X - ray.Origin.X) * ray.InvDir.X;
         txmax = (bounds(1 - ray.Sign[0]).X - ray.Origin.X) * ray.InvDir.X;

         tymin = (bounds(ray.Sign[1]).Y - ray.Origin.Y) * ray.InvDir.Y;
         tymax = (bounds(1 - ray.Sign[1]).Y - ray.Origin.Y) * ray.InvDir.Y;

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
      public Vec3f Origin;
      public Vec3f Dir;
      public float _length;
      public bool _isOpt;
      public Vec3f InvDir;
      public int[] Sign = new int[3];

      public void Opt()
      {
         //**New - optimization
         //http://people.csail.mit.edu/amy/papers/box-jgt.pdf
         // if (Dir.X != 0.0f)
         InvDir.X = 1.0f / Dir.X;
         //   else
         //       InvDir.X = 0.0f;
         //   if (Dir.Y != 0.0f)
         InvDir.Y = 1.0f / Dir.Y;
         //      else
         //         InvDir.Y = 0.0f;
         //     if (Dir.Z != 0.0f)
         InvDir.Z = 1.0f / Dir.Z;
         //     else
         //         InvDir.Z = 0.0f;

         Sign[0] = Convert.ToInt32(InvDir.X < 0);
         Sign[1] = Convert.ToInt32(InvDir.Y < 0);
         Sign[2] = Convert.ToInt32(InvDir.Z < 0);

         _isOpt = true;
      }
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct Box3i
   {
      public ivec3 _min;
      public ivec3 _max;
      public Box3i(in ivec3 min, in ivec3 max)
      {
         _min = min;
         _max = max;
      }
   }

   [StructLayout(LayoutKind.Sequential)]
   public struct Box3f
   {
      public Vec3f _min;
      public Vec3f _max;
      public Box3f(in Vec3f min, in Vec3f max)
      {
         _min = min;
         _max = max;
      }
      public float Height()
      {
         return _max.Y - _min.Y;
      }
      public float Width()
      {
         return _max.X - _min.X;
      }
      public float Depth()
      {
         return _max.Z - _min.Z;
      }
      public void Validate()
      {
         if (_max.X < _min.X)
         {
            throw new Exception("Bound box X was invalid.");
         }
         if (_max.Y < _min.Y)
         {
            throw new Exception("Bound box Y was invalid.");
         }
         if (_max.Z < _min.Z)
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
      private Vec3f bounds(int in__)
      {
         if (in__ == 0)
         {
            return _min;
         }
         else
         {
            return _max;
         }
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

         bh._bHit = ((txmin > 0.0f) && (txmax <= ray._length));
         bh._t = txmin;

         return bh._bHit;
      }

      private bool containsInclusive(Vec3f v)
      {
         return (
             (v.X >= _min.X) && (v.X <= _max.X) &&
             (v.Y >= _min.Y) && (v.Y <= _max.Y) &&
             (v.Z >= _min.Z) && (v.Z <= _max.Z)
             );
      }
      public void genResetLimits()
      {
         _min = new Vec3f(float.MaxValue, float.MaxValue, float.MaxValue);
         _max = new Vec3f(float.MinValue, float.MinValue, float.MinValue);
      }
      public void genExpandByPoint(in Vec3f pt)
      {
         _min = Vec3f.ComponentMin(_min, pt);
         _max = Vec3f.ComponentMax(_max, pt);
      }
      public void genExpandByBox(in Box3f pc)
      {
         genExpandByPoint(pc._min);
         genExpandByPoint(pc._max);
      }
      public bool getHasVolume(float epsilon)
      {
         if (getVolumePositiveOnly() == 0.0)
         {
            return false;
         }
         return true;
      }
      private float getVolumePositiveOnly()
      {
         float ax = (_max.X - _min.X);
         float ay = (_max.Y - _min.Y);
         float az = (_max.Z - _min.Z);
         if (ax < 0.0f) ax = 0.0f;
         if (ay < 0.0f) ay = 0.0f;
         if (az < 0.0f) az = 0.0f;
         return ax * ay * az;
      }
      private float getVolumeArbitrary()
      {
         return (_max.X - _min.X) * (_max.Y - _min.Y) * (_max.Z - _min.Z);
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
         x = dx; y = dy; z = dz;
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
      public static bool operator ==(in ivec3 a, in ivec3 b)
      {
         return (a.x==b.x) && (a.y==b.y) && (a.z==b.z);  
      }
      public static bool operator !=(in ivec3 a, in ivec3 b)
      {
         return (a.x != b.x) || (a.y != b.y) || (a.z != b.z);
      }
   }
   public class ivec3EqualityComparer : IEqualityComparer<ivec3>
   {
      public bool Equals(ivec3 a, ivec3 b)
      {
         return a.x == b.x && a.y == b.y && a.z == b.z;
      }

      public int GetHashCode(ivec3 a)
      {
         return a.GetHashCode();//.x.GetHashCode() + a.y.GetHashCode() + a.z.GetHashCode();
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
         x = dx; y = dy; z = dz; w = dw;
      }
   }


   [StructLayout(LayoutKind.Sequential)]
   public struct vec3
   {
      public float x;
      public float y;
      public float z;

      public static vec3 VEC3_MIN()
      {
         return new vec3(float.MinValue, float.MinValue, float.MinValue);
      }
      public static vec3 VEC3_MAX()
      {
         return new vec3(float.MaxValue, float.MaxValue, float.MaxValue);
      }

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
      public vec3 construct(float dx, float dy, float dz)
      {
         x = dx; y = dy; z = dz;
         return this;
      }
      public override string ToString()
      {
         return "" + x + "," + y + "," + z;
      }
      //    public static vec3 minv(in vec3 v_a, in vec3 v_b) {
      //      vec3 outt = new vec3();

      //      outt.x = Math.Min(v_a.x, v_b.x);
      //      outt.y = Math.Min(v_a.y, v_b.y);
      //      outt.z = Math.Min(v_a.z, v_b.z);

      //      return outt;
      //    }
      //public static vec3 maxv(in vec3 v_a, in vec3 v_b)
      //{
      //      vec3 outt = new vec3();

      //      outt.x = Math.Max(v_a.x, v_b.x);
      //      outt.y = Math.Max(v_a.y, v_b.y);
      //      outt.z = Math.Max(v_a.z, v_b.z);

      //      return outt;
      //}
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

      public static vec3 minv(in vec3 v_a, in vec3 v_b)
      {
         vec3 outv = new vec3();

         outv.x = Math.Min(v_a.x, v_b.x);
         outv.y = Math.Min(v_a.y, v_b.y);
         outv.z = Math.Min(v_a.z, v_b.z);

         return outv;
      }
      public static vec3 maxv(in vec3 v_a, in vec3 v_b)
      {
         vec3 outv = new vec3();

         outv.x = Math.Max(v_a.x, v_b.x);
         outv.y = Math.Max(v_a.y, v_b.y);
         outv.z = Math.Max(v_a.z, v_b.z);

         return outv;
      }
      public static vec3 maxv_a(in vec3 v_a, in vec3 v_b)
      {
         vec3 outv = new vec3();

         outv.x = Math.Max(Math.Abs(v_a.x), Math.Abs(v_b.x));
         outv.y = Math.Max(Math.Abs(v_a.y), Math.Abs(v_b.y));
         outv.z = Math.Max(Math.Abs(v_a.z), Math.Abs(v_b.z));
         return outv;
      }
      public static float maxf_a(in vec3 v_a, in vec3 v_b)
      {
         vec3 tmp = maxv_a(v_a, v_b);
         return Math.Max(Math.Abs(tmp.x), Math.Max(Math.Abs(tmp.y), Math.Abs(tmp.z)));
      }
      public Vec2f xz()
      {
         return new Vec2f(x, z);
      }
      public Vec2f xy()
      {
         return new Vec2f(x, y);
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
      ////  Mat3f m = Mat3f::getRotationRad(angle, normal);
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
      //            BRLogWarn("Object has launched into orbit: v=(", x, ", y, " ", z, ")");
      //        *this = normalized() * fMaxLength;
      //    }
      //}

      //class Vec3Basis : public VirtualMemory {
      //public:
      //    vec3 _x, _y, _z;
      //};
   }
   [StructLayout(LayoutKind.Sequential)]
   public struct vec4
   {
      public float x, y, z, w;

      public vec4(vec3 d, float dw) { x = d.x; y = d.y; z = d.z; w = dw; }
      public vec4(vec4 dxy) { x = dxy.x; y = dxy.y; z = dxy.z; w = dxy.w; }
      public vec4(float dx, float dy, float dz, float dw) { x = dx; y = dy; z = dz; w = dw; }
      public vec4(OpenTK.Vector4 v) { x = v.X; y = v.Y; z = v.Z; w = v.W; }//From XNA's Vector2

      public override string ToString() { return "(" + x + "," + y + "," + z + "," + w + ")"; }
      public vec4 construct(float dx, float dy, float dz, float dw)
      {
         x = dx; y = dy; z = dz; w = dw;
         return this;
      }

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
      public OpenTK.Vector4 toOpenTK()
      {
         return new OpenTK.Vector4(x, y, z, w);
      }
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

}