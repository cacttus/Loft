
namespace Loft
{

  //.NET random - conflict - renamed to rand
  public class Rand
  {
    //TODO: this is not thread safe .move to context
    //private static System.Random r = new System.Random(845934029);
    private static uint _last = 845934029;
    private static ulong _lastUL = 845934029;
    //.NET random is not thread safe but is much better distributed. Noise random is thread safe.

    public static uint NextUInt()
    {
      //Inclusive
      _last = Noise.Get32(_last);
      return _last;
    }
    public static ulong NextULong()
    {
      _lastUL = Noise.Get64(_lastUL);
      return _lastUL;
    }
    public static int NextInt()
    {
      //Inclusive
      return (int)NextUInt();
    }
    public static int NextInt(int min, int max)
    {
      //Inclusive
      int ret = min + (max - min) * NextInt();
      return ret;
    }
    public static int NextInt(Minimax<int> ia)
    {
      //Inclusive
      return NextInt(ia.Min, ia.Max);
    }
    public static float NextFloat()
    {
      return Noise.NoiseToFloat(NextUInt());
    }
    public static float NextFloat(float min, float max)
    {
      //Inclusive
      float ret = (float)min + (max - min) * NextFloat();
      return ret;
    }
    public static float NextFloat(Minimax<float> ia)
    {
      //Exclusive
      return NextFloat(ia.Min, ia.Max);
    }
    public static double NextDouble()
    {
      //Inclusive
      double ret = Noise.NoiseToDouble(NextULong());
      return ret;
    }
    public static double NextDouble(double min, double max)
    {
      //Inclusive
      double ret = min + (max - min) * NextDouble();
      return ret;
    }
    public static vec2 NextVec2()
    {
      vec2 ret;
      ret.x = (float)NextFloat();
      ret.y = (float)NextFloat();
      return ret;
    }
    public static vec3 NextVec3(Minimax<vec3> v)
    {
      return new vec3(
        (Rand.NextFloat(v.Min.x, v.Max.x)),
        (Rand.NextFloat(v.Min.y, v.Max.y)),
        (Rand.NextFloat(v.Min.z, v.Max.z))
        );
    }
    public static vec3 NextVec3()
    {
      //returns a vec3 within [0,1)
      vec3 ret;
      ret.x = (float)NextFloat();
      ret.y = (float)NextFloat();
      ret.z = (float)NextFloat();
      return ret;
    }
    public static vec3 NextVec3(vec3 min, vec3 max)
    {
      vec3 ret;
      ret.x = min.x + (max.x - min.x) * (float)NextFloat();
      ret.y = min.y + (max.y - min.y) * (float)NextFloat();
      ret.z = min.z + (max.z - min.z) * (float)NextFloat();
      return ret;
    }
    public static vec4 NextVec4(vec4 min, vec4 max)
    {
      vec4 ret;
      ret.x = min.x + (float)NextFloat() * (max.x - min.x);
      ret.y = min.y + (float)NextFloat() * (max.y - min.y);
      ret.z = min.z + (float)NextFloat() * (max.z - min.z);
      ret.w = min.w + (float)NextFloat() * (max.w - min.w);
      return ret;
    }
    public static vec3 NextVelocity(vec3 min, vec3 max, float speed_meters_per_second)
    {
      var v = Rand.NextVec3(min, max);
      v = v.normalize() * speed_meters_per_second;
      return v;
    }
    public static vec3 NextNormal()
    {
      //returns random normal 
      vec3 v = new vec3(NextFloat(-1, 1), NextFloat(-1, 1), NextFloat(-1, 1));
      v.normalize();
      return v;
    }
    public static vec4 NextRGB(float min, float max, float alpha)
    {
      return new vec4(
        NextFloat(min, max),
        NextFloat(min, max),
        NextFloat(min, max),
        alpha
      );
    }
  }
}
