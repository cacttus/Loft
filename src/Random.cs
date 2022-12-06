
namespace Loft
{
  public class Random
  {
    //TODO: this is not thread safe .move to context
    //private static System.Random r = new System.Random(845934029);
    private static long _last = 845934029;
    private static long mint(long seed, long off = 0x9d2c5680)
    {
      long x;
      x = (0x6c078965 * (seed ^ (seed >> 30)) + (off)) & 0xffffffff;
      x = x ^ (x >> 11);
      x = x ^ ((x << 7) & 0x9d2c5680);
      x = x ^ ((x << 15) & 0xefc60000);
      x = x ^ (x >> 18);
      return x;
    }
    static float flotc01(int ix)
    {
      //0x007fffff is the fractional portion of a floating point.
      float x;
      unsafe
      {
        int a = (ix & 0x007fffff) | 0x3f800000;
        x = (*((float*)&a) - 1.0f);
      }
      return x;
    }
    private static long NextIntTtt()
    {
      _last = mint(_last);
      return _last;
    }
    public static vec3 Normal()
    {
      //returns a random normal H^2 > theta=[0,2pi], phi=[-pi/2 pi/2]
      vec3 v = new vec3(Next11(), Next11(), Next11());
      v.normalize();
      return v;
    }
    //Inclusive [min,max]
    public static int NextInt(int min, int max)
    {
      //Inclusive
      int ret = (int)Math.Round((float)min + ((float)max - (float)min) * NextF());
      return ret;
    }
    public static int Next(Minimax<int> ia)
    {
      //Inclusive
      return NextInt(ia.Min, ia.Max);
    }
    public static float Next(Minimax<float> ia)
    {
      //Exclusive
      return Next(ia.Min, ia.Max);
    }
    public static float Next(float min, float max)
    {
      //Inclusive
      float ret = (float)min + (max - min) * NextF();
      return ret;
    }
    public static double NextD(double min, double max)
    {
      //Inclusive
      double ret = min + (max - min) * NextF();
      return ret;
    }
    public static vec3 Next(Minimax<vec3> v)
    {
      return new vec3(
        (Random.Next(v.Min.x, v.Max.x)),
        (Random.Next(v.Min.y, v.Max.y)),
        (Random.Next(v.Min.z, v.Max.z))
        );
    }
    public static float Next11()
    {
      float ret;
      ret = Next(-1, 1);
      return ret;
    }
    public static float NextF()
    {
      return flotc01((int)NextIntTtt());
      //float ret;
      //ret = (float)r.NextDouble();
      //return ret;
    }
    public static vec2 NextVec2()
    {
      vec2 ret;
      ret.x = (float)NextF();
      ret.y = (float)NextF();
      return ret;
    }
    public static vec3 NextVec3()
    {
      //returns a vec3 within [0,1)
      vec3 ret;
      ret.x = (float)NextF();
      ret.y = (float)NextF();
      ret.z = (float)NextF();
      return ret;
    }
    public static vec3 NextVec3(vec3 min, vec3 max)
    {
      vec3 ret;
      ret.x = min.x + (max.x - min.x) * (float)NextF();
      ret.y = min.y + (max.y - min.y) * (float)NextF();
      ret.z = min.z + (max.z - min.z) * (float)NextF();
      return ret;
    }
    public static vec3 RandomVelocity(vec3 min, vec3 max, float speed_meters_per_second)
    {
      var v = Random.NextVec3(min, max);
      v = v.normalize() * speed_meters_per_second;
      return v;
    }
    public static vec4 NextVec4(vec4 a, vec4 b)
    {
      vec4 ret;
      ret.x = a.x + (float)NextF() * (b.x - a.x);
      ret.y = a.y + (float)NextF() * (b.y - a.y);
      ret.z = a.z + (float)NextF() * (b.z - a.z);
      ret.w = a.w + (float)NextF() * (b.w - a.w);
      return ret;
    }
  }
}
