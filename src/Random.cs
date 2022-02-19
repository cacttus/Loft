
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
      public static vec2 NextVec2()
      {
         vec2 ret;
         ret.x = (float)r.NextDouble();
         ret.y = (float)r.NextDouble();
         return ret;
      }
      public static vec3 NextVec3()
      {
         vec3 ret;
         ret.x = (float)r.NextDouble();
         ret.y = (float)r.NextDouble();
         ret.z = (float)r.NextDouble();
         return ret;
      }
      public static vec4 NextVec4(vec4 a , vec4 b )
      {
         vec4 ret;
         ret.x = a.x + (float)r.NextDouble() * (b.x - a.x);
         ret.y = a.y + (float)r.NextDouble() * (b.y - a.y);
         ret.z = a.z + (float)r.NextDouble() * (b.z - a.z);
         ret.w = a.w + (float)r.NextDouble() * (b.w - a.w);
         return ret;
      }
   }
}
