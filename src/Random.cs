using Quat = OpenTK.Quaternion;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;

namespace PirateCraft
{
   public class Random
   {
      private static System.Random r = new System.Random();
      public static float Next()
      {
         float ret;
         ret = (float)r.NextDouble();
         return ret;
      }
      public static Vec2f NextVec2()
      {
         Vec2f ret;
         ret.X = (float)r.NextDouble();
         ret.Y = (float)r.NextDouble();
         return ret;
      }
      public static Vec3f NextVec3()
      {
         Vec3f ret;
         ret.X = (float)r.NextDouble();
         ret.Y = (float)r.NextDouble();
         ret.Z = (float)r.NextDouble();
         return ret;
      }
      public static Vec4f NextVec4(Vec4f a , Vec4f b )
      {
         Vec4f ret;
         ret.X = a.X + (float)r.NextDouble() * (b.X - a.X);
         ret.Y = a.Y + (float)r.NextDouble() * (b.Y - a.Y);
         ret.Z = a.Z + (float)r.NextDouble() * (b.Z - a.Z);
         ret.W = a.W + (float)r.NextDouble() * (b.W - a.W);
         return ret;
      }
   }
}
