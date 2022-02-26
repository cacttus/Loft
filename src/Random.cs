
namespace PirateCraft
{
  public class Random
  {
    //TODO: this is not thread safe .move to context
    private static System.Random r = new System.Random();
    public static vec3 Normal()
    {
      //returns a random normal H^2 > theta=[0,2pi], phi=[-pi/2 pi/2]
      vec3 v = new vec3(Next11(), Next11(), Next11());
      v.normalize();
      return v;
    }
    public static int NextInt(int min, int max)
    {
      //Inclusive
      int ret = (int)Math.Round((float)min + ((float)max - (float)min) * Next());
      return ret;
    }
    public static float Next(float min, float max)
    {
      //Inclusive
      float ret = (float)min + (max - min) * Next();
      return ret;
    }
    public static float Next11()
    {
      float ret;
      ret = Next(-1, 1);
      return ret;
    }
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
    public static vec4 NextVec4(vec4 a, vec4 b)
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
