using System;
using System.Drawing;
using System.Collections.Generic;

namespace Loft
{
  public class Noise3D
  {
    static int[] primes = new int[]
    {10000019
        ,10000079
        ,10000103
        ,10000121
        ,10000139
        ,10000141
        ,10000169
        ,10000189
        ,10000223
        ,10000229
        ,10000247
        ,10000253
        ,10000261
        ,10000271
        ,10000303 };
    public static int Rot(int a, int n)
    {
      return (((a) << (n)) | (((a)) >> (32 - (n))));
    }
    static uint static_mersenne(uint seed)
    {
      uint i = 1, y;

      y = (0x6c078965 * (seed ^ (seed >> 30)) + i) & 0xffffffff;
      y = y ^ (y >> 11);
      y = y ^ ((y << 7) & 0x9d2c5680);
      y = y ^ ((y << 15) & 0xefc60000);
      y = y ^ (y >> 18);

      return y;
    }
    static uint static_mersenne3(uint x, uint y, uint z)
    {
      //This is much worse. More grainy and aliased than xxhash.
      uint x1 = static_mersenne((uint)x);
      uint y1 = static_mersenne((uint)y);
      uint z1 = static_mersenne((uint)z);
      uint rnd = (x1 * (uint)primes[0] + y1 * (uint)primes[1] + z1 * (uint)primes[2]);
      rnd ^= rnd >> (int)15;
      rnd *= (uint)primes[2];
      rnd ^= rnd >> (int)13;
      rnd *= (uint)primes[3];
      rnd ^= rnd >> (int)16;

      return rnd;
    }
    //This is unfortunate. BitConverter is very slow though ( i think). Testing is needed.
    unsafe static float to_float(uint x)
    {
      //Grab the mantissa of a float
      uint a = (x & 0x007fffff) | 0x40000000;
      return (*((float*)&a) - 3.0f);
      //            float f = BitConverter.ToSingle(BitConverter.GetBytes(x), 0);
      //          return f;
    }
    public static Int32 xxHash4D(int seed, ivec4 p)
    {
      return xxHash4D(seed, p.x, p.y, p.z, p.w);
    }
    public static Int32 xxHash4D(int seed, int x, int y, int z, int w)
    {
      //Returns an integer in the range -IntMax,+IntMax
      // 4 blocks
      // xxxxyyyyzzzz

      //variation of 
      //https://richardstartin.github.io/posts/xxhash
      int prime1 = primes[0];
      int prime2 = primes[1];
      int prime3 = primes[2];
      int prime4 = primes[3];
      int prime5 = primes[4];

      Int32 ret = 0;
      ret = 0;
      ret += 4 * 4;//length of input in bytes
      ret += x * prime3;
      ret = Rot(ret, 17) * prime4;
      ret += y * prime3;
      ret = Rot(ret, 17) * prime4;
      ret += z * prime3;
      ret = Rot(ret, 17) * prime4;

      ret += (((w << (int)24) >> (int)24) & (int)255) * prime5;
      ret = Rot(ret, 11) * prime1;
      ret += (((w << (int)16) >> (int)24) & (int)255) * prime5;
      ret = Rot(ret, 11) * prime1;
      ret += (((w << (int)8) >> (int)24) & (int)255) * prime5;
      ret = Rot(ret, 11) * prime1;
      ret += (((w << (int)0) >> (int)24) & (int)255) * prime5;
      ret = Rot(ret, 11) * prime1;

      ret ^= ret >> (int)15;
      ret *= prime2;
      ret ^= ret >> (int)13;
      ret *= prime3;
      ret ^= ret >> (int)16;

      return ret;
    }
    static float smootherstep(float edge0, float edge1, float x)
    {
      float y = (x - edge0) / (edge1 - edge0);
      y = MathUtils.Clamp(y, 0.0f, 1.0f);
      return y * y * y * (y * (y * 6 - 15) + 10);

    }
    static float linearstep(float e0, float e1, float x)
    {
      return e0 + (e1 - e0) * x;
    }
    static float smoothstep(float edge0, float edge1, float x)
    {
      // Scale, bias and saturate x to 0..1 range
      x = MathUtils.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
      // Evaluate polynomial
      return x * x * (3 - 2 * x);
    }
    static float Perlin3D(vec3 p, int seed, int scale = 1)
    {
      if (scale <= 0)
      {
        scale = 1; //avoid / zero
      }
      //PNoise only works in integer multiples of voxels. There is no sub-voxel. Scale will be aliased.
      //Generate a 3d grid 
      //for each corner of the overlayed grid, generate a random vector that points inside the "cell" from a hash function.
      //Take the dot product of this vector with the input to the grid point. Note that the random vector must point within this "cell."
      ivec3 grid0 = new ivec3((int)(p.x - (p.x % scale)), (int)(p.y - (p.y % scale)), (int)(p.z - (p.z % scale)));
      ivec3 grid1 = grid0 + new ivec3((int)scale, (int)scale, (int)scale);// new vec3((int)(p.x / scale)+ scale, (int)(p.y/ scale) + scale, (int)(p.z / scale) + scale);
      /*

                 6       7
              2      3

                 4        5
              0       1 

      */
      float[] vals = new float[8];
      vals[0] = DotGrad3D(seed, p, grid0.x, grid0.y, grid0.z);
      vals[1] = DotGrad3D(seed, p, grid1.x, grid0.y, grid0.z);
      vals[2] = DotGrad3D(seed, p, grid0.x, grid1.y, grid0.z);
      vals[3] = DotGrad3D(seed, p, grid1.x, grid1.y, grid0.z);
      vals[4] = DotGrad3D(seed, p, grid0.x, grid0.y, grid1.z);
      vals[5] = DotGrad3D(seed, p, grid1.x, grid0.y, grid1.z);
      vals[6] = DotGrad3D(seed, p, grid0.x, grid1.y, grid1.z);
      vals[7] = DotGrad3D(seed, p, grid1.x, grid1.y, grid1.z);//This is a reused computation, we coudl compute gradients all at once.

      //trilinear interpolate all values.
      //(01-23)-(45-67)
      float sx = (p.x % (float)scale) / (float)scale;
      float sy = (p.y % (float)scale) / (float)scale;
      float sz = (p.z % (float)scale) / (float)scale;
      float ex0 = linearstep(vals[0], vals[1], sx);
      float ex1 = linearstep(vals[2], vals[3], sx);
      float ex2 = linearstep(vals[4], vals[5], sx);
      float ex3 = linearstep(vals[6], vals[7], sx);
      float ey0 = linearstep(ex0, ex1, sy);
      float ey1 = linearstep(ex2, ex3, sy);
      float ez0 = linearstep(ey0, ey1, sz);

      return ez0;
    }
    public static float DotGrad3D(int seed, vec3 p, int gx, int gy, int gz)
    {
      vec3 v = GradPoint3D(seed, gx, gy, gz, 0);
      float f = (p - new vec3(gx, gy, gz)).normalized().dot(v);
      return f;
    }
    static Dictionary<int, Dictionary<int, HashSet<int>>> uniquePoints = new Dictionary<int, Dictionary<int, HashSet<int>>>();
    static Dictionary<vec3, HashSet<ivec3>> uniqueNormals = new Dictionary<vec3, System.Collections.Generic.HashSet<ivec3>>();
    static Dictionary<int, HashSet<ivec3>> uniqueHashes = new Dictionary<int, System.Collections.Generic.HashSet<ivec3>>();
    public static vec3 GradPoint3D(int seed, int x, int y, int z, int w = 0)
    {
      //Random number that interpolates along a cube side into a vector component
      //vec3 (rand(), rand(), rand()) - normalized.
      //the vector is a random value that points in arbitrary 3D space from the gradient grid point

      //Add an axis index.
      int n = xxHash4D(seed, x, y, z, w);
      //if (!uniquePoints.ContainsKey(x))
      //{
      //    uniquePoints.Add(x, new Dictionary<int,HashSet<int>>());
      //}
      //Dictionary<int, HashSet<int>> dick =null;
      //if(uniquePoints.TryGetValue(x, out dick))
      //{
      //    if (!dick.ContainsKey(y))
      //    {
      //        dick.Add(y, new HashSet<int>());
      //    }
      //    HashSet<int> dick2 = null;
      //    if(dick.TryGetValue(y,out dick2))
      //    {
      //        dick2.Add(z);
      //    }
      //}



      ////  float r = to_float((uint)n);
      //if (!uniqueHashes.ContainsKey(n))
      //{
      //    uniqueHashes.Add(n, new System.Collections.Generic.HashSet<ivec3>());
      //}
      //System.Collections.Generic.HashSet<ivec3> set=null;
      //if(uniqueHashes.TryGetValue(n,out set))
      //{
      //    set.Add(new ivec3(x,y,z));

      //}
      //totalHashes++;

      //10 bits
      float xx = (float)(((n >> 0) & 0x3FF) - 512) / 1024.0f;
      float yy = (float)(((n >> 10) & 0x3FF) - 512) / 1024.0f;
      float zz = (float)(((n >> 20) & 0x3FF) - 512) / 1024.0f;

      vec3 grad = (new vec3(xx, yy, zz)).normalized();

      if (!uniqueNormals.ContainsKey(grad))
      {
        uniqueNormals.Add(grad, new System.Collections.Generic.HashSet<ivec3>());
      }
      HashSet<ivec3> set2 = null;
      if (uniqueNormals.TryGetValue(grad, out set2))
      {
        set2.Add(new ivec3(x, y, z));
      }
      //float theta = r * (float)Math.PI * 2.0f;
      //float phi = (1 - r) * (float)Math.PI * 2.0f;

      //float cp = (float)Math.Cos(theta); //TODO: put this back to 3d
      //float sp = (float)Math.Sin(theta);
      //float ct = (float)Math.Cos(theta);
      //float st = (float)Math.Sin(theta);
      //vec3 grad = (new vec3(cp * st, sp * st, ct)).normalize(); //may not be necessary.
      ////vec3 grad = (new vec3(cp, sp, 0)).normalize();

      return grad;
    }
    static int _bsiz = 128;
    static int _scale = 8;
    static int _ngridpoints = (int)Math.Pow(_bsiz / _scale, 3);
    static int _seed = 940275;
    public static Texture TestNoise()
    {
      int nCount = 0;
      //   Bitmap b = new Bitmap(_bsiz, _bsiz, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
      Image b = new Image("noise-image", _bsiz, _bsiz, Image.ImagePixelFormat.RGBA32ub);
      int z = 0;// (int z = 0; z < _bsiz; z++)
      {
        for (int y = 0; y < _bsiz; y++)
        {
          for (int x = 0; x < _bsiz; x++)
          {
            float frnd = 0;
            //Noise Test
            //Int32 rnd = Noise3D.Static4D(0, new iVec4f(x, y, z, 0));
            //frnd = (float)((double)rnd / (double)Int32.MaxValue);

            //Perlin Test
            frnd = Perlin3D(new vec3(x, y, z), _seed, _scale);
            frnd = (frnd + 1.0f) / 2.0f;

            byte brnd = (byte)((frnd + 1) / 2 * 255);
            Pixel4ub c = new Pixel4ub(255, brnd, brnd, brnd);
            b.SetPixel_RGBA32ub(x, y, c);
            nCount++;
          }
        }


      }
      Texture t = new Texture(b, true, TexFilter.Trilinear);
      return t;
    }


  }
  public class CloudThing
  {

  }
}
