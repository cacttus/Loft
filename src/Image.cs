using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Loft
{
  [StructLayout(LayoutKind.Sequential)]
  public struct Pixel4ub
  {
    public byte r, g, b, a;
    public Pixel4ub(byte dr, byte dg, byte db, byte da)
    {
      r = dr;
      g = dg;
      b = db;
      a = da;
    }
  }
  public class ImageKernel<T>
  {
    public int RadiusPixelsX { get; private set; } = 0;//Does not include the center pixel.
    public int RadiusPixelsY { get; private set; } = 0;//Does not include the center pixel.
    public int RankX { get; private set; } = 0;
    public int RankY { get; private set; } = 0;
    public T[,] Kernel = null;
    public T this[int i, int j]
    {
      //operator[]
      get
      {
        return Kernel[i, j];
      }
      set
      {
        Kernel[i, j] = value;
      }
    }
    public void Iterate(Func<int, int, LambdaBool> f)
    {
      for (int y = 0; y < RankY; y++)
      {
        for (int x = 0; x < RankX; x++)
        {
          if (f(x, y) == LambdaBool.Break)
          {
            break;
          }
        }
      }
    }
    public ImageKernel(int radius_pixels_x, int radius_pixels_y)
    {
      RadiusPixelsX = radius_pixels_x;
      RadiusPixelsY = radius_pixels_y;
      RankX = RadiusPixelsX * 2 + 1; //prevent odd kernels
      RankY = RadiusPixelsY * 2 + 1; //prevent odd kernels
      Kernel = new T[RankY, RankX];
    }
    public static ImageKernel<double> Gaussian(int radius_pixels, float weight)
    {
      //https://stackoverflow.com/questions/23228226/how-to-calculate-the-gaussian-filter-kernel
      // define an array of two dimensions based on the length value that pass it by the user from the text box.
      var Kernel = new ImageKernel<double>(radius_pixels, radius_pixels);
      double sumTotal = 0;
      double distance = 0;
      double calculatedEuler = 1.0f / (2.0f * (double)Math.PI * (double)Math.Pow(weight, 2)); // Gaussian Function first part

      int kernelRadiusX = Kernel.RadiusPixelsX;
      int kernelRadiusY = Kernel.RadiusPixelsY;
      for (int filterY = -kernelRadiusY; filterY <= kernelRadiusY; filterY++)
      {
        for (int filterX = -kernelRadiusX; filterX <= kernelRadiusX; filterX++)
        {
          distance = ((filterX * filterX) + (filterY * filterY)) / (2 * (weight * weight)); // Gaussian Function Second part
          double t = calculatedEuler * (double)Math.Exp(-distance);
          Kernel[filterY + kernelRadiusY, filterX + kernelRadiusX] = t;
          sumTotal += Kernel[filterY + kernelRadiusY, filterX + kernelRadiusX];
        }
      }

      Kernel.Iterate((x, y) =>
      {
        Kernel[y, x] = Kernel[y, x] * (1.0f / sumTotal);
        return LambdaBool.Continue;
      });

      return Kernel;
    }
  }

  [DataContract]
  public class Image : DataBlock
  {
    //Note: this class initializes the data buffer when you create it. It requires a w/h
    public enum ImagePixelFormat
    {
      Undefined,
      RGB24ub,
      BGR24ub,
      RGBA32ub,
      BGRA32ub,
      R32f
    }
    [DataMember] public int _width = 0;
    [DataMember] public int _height = 0;
    [DataMember] public ImagePixelFormat _format = ImagePixelFormat.Undefined;
    public byte[] _data = null;//This is only if there is no data source

    public int Width { get { return _width; } private set { _width = value; } }
    public int Height { get { return _height; } private set { _height = value; } }
    public byte[] Data { get { return _data; } private set { _data = value; } }
    public ImagePixelFormat Format { get { return _format; } private set { _format = value; } }
    public float SizeRatio { get { return Height != 0 ? Width / Height : 1; } }

    public int BytesPerPixel
    {
      get
      {
        if (Format == ImagePixelFormat.RGBA32ub || Format == ImagePixelFormat.BGRA32ub || Format == ImagePixelFormat.R32f)
        {
          //In this system we should always return 4.
          return 4;
        }
        else if (Format == ImagePixelFormat.RGB24ub || Format == ImagePixelFormat.BGR24ub)
        {
          //This isn't supported explicitly. STB image converts all images to RGBA.
          Gu.DebugBreak();
          return 3;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
          return 0;
        }
      }
      private set
      {
      }
    }//Always 4BPP in our system.

    protected Image() { }
    public static Image Default1x1_RGBA32ub(string name, byte r, byte g, byte b, byte a)
    {
      var img = new Image(name, 1, 1, new byte[] { r, g, b, a }, ImagePixelFormat.RGBA32ub);
      return img;
    }
    public static Image Default1x1_RGBA32ub(string name, vec4ub v4)
    {
      return Default1x1_RGBA32ub(name, v4.r, v4.g, v4.b, v4.a);
    }
    public Image(string name, int w, int h, ImagePixelFormat sc = ImagePixelFormat.RGBA32ub) : this(name, w, h, null, sc)
    {
    }
    public Image(string name, int w, int h, byte[] data, ImagePixelFormat sc = ImagePixelFormat.RGBA32ub) : base(name)
    {
      //Note if data is null data will still get allocated
      init(w, h, data, sc);
    }
    public void FlipBR()
    {
      byte[] newData = new byte[Data.Length];

      for (int i = 0; i < newData.Length; i += 4)
      {
        //Argb -> Rgba
        var r = Data[i + 0];//This is correct
        var g = Data[i + 1];
        var b = Data[i + 2];
        var a = Data[i + 3];
        newData[i + 0] = b;
        newData[i + 1] = g;
        newData[i + 2] = r;
        newData[i + 3] = a;
      }
      Data = newData;
    }
    public void FlipBA()
    {
      byte[] newData = new byte[Data.Length];

      for (int i = 0; i < newData.Length; i += 4)
      {
        //Argb -> Rgba
        var r = Data[i + 0];//This is correct
        var g = Data[i + 1];
        var b = Data[i + 2];
        var a = Data[i + 3];
        newData[i + 0] = r;
        newData[i + 1] = g;
        newData[i + 2] = b;
        newData[i + 3] = a;
      }
      Data = newData;
    }
    public void init(int w, int h, byte[] data, ImagePixelFormat sc)
    {
      Gu.Assert(w>0, $"{Name}: Image Width was zero");
      Gu.Assert(h>0, $"{Name}: Image Height was zero");
      Width = w;
      Height = h;
      Format = sc;
      if (data == null)
      {
        data = new byte[w * h * BytesPerPixel];
      }

      Gu.Assert(data.Length == BytesPerPixel * w * h);

      Data = data;
    }
    public Image copySubImageTo(ivec2 off, ivec2 size)
    {
      Image ret = new Image("subimage-cpy", size.x, size.y, Format);
      // ret.init(size.x, size.y);//ret.create(size.x, size.y);
      ret.copySubImageFrom(new ivec2(0, 0), off, size, this);
      return ret;
    }
    //Image formats must be identical
    public void copySubImageFrom(ivec2 myOff, ivec2 otherOff, ivec2 size, Image pOtherImage)
    {
      if (Data == null)
      {
        Gu.BRThrowException("Copy SubImage 2 - From image was not allocated");
      }
      if (pOtherImage == null)
      {
        Gu.BRThrowException("Copy SubImage 1 - Input Image was null.");
      }
      if (pOtherImage.Data == null)
      {
        Gu.BRThrowException("Copy SubImage 3 - Input Image TO was not allocated");
      }
      //size constraint validation
      if (myOff.x < -1 || myOff.y < -1)
      {
        Gu.BRThrowException("Copy SubImage 4");
      }
      if (myOff.x >= (int)Width || myOff.y >= (int)Height)
      {
        Gu.BRThrowException("Copy SubImage 5.  This hits if you put too many textures in the db_atlas.dat file. There can only be XxX textres(usually 16x16)");
      }
      if (otherOff.x < 0 || otherOff.y < 0)
      {
        Gu.BRThrowException("Copy SubImage 6");
      }
      if (otherOff.x >= pOtherImage.Width || otherOff.y >= pOtherImage.Height)
      {
        Gu.BRThrowException("Copy SubImage 7");
      }

      ivec2 scanPos = myOff;
      int scanLineByteSize = size.x * BytesPerPixel;
      int nLines = size.y;

      Gu.Assert(scanLineByteSize >= 0);

      for (int iScanLine = 0; iScanLine < nLines; ++iScanLine)
      {
        int dstff = vofftos(scanPos.x, scanPos.y + iScanLine, Width) * BytesPerPixel;
        int srcff = vofftos(otherOff.x, otherOff.y + iScanLine, pOtherImage.Width) * pOtherImage.BytesPerPixel;

        //BlockCopy: src,srcoff, dst, dstoff, count
        Buffer.BlockCopy(pOtherImage.Data, srcff, Data, dstff, scanLineByteSize);
      }
    }
    public static int vofftos(int row, int col, int items_per_row)
    {
      return (col * items_per_row + row);
    }
    public Image CreateNormalMap(bool isbumpmap, float depth_amount = 0.70f)
    {
      //This is too slow for C# //TODO: put on GPU
      if (Data == null)
      {
        return null;
      }
      Image ret = (Image?)this.Clone();

      for (int j = 0; j < ret.Height; ++j)
      {
        for (int i = 0; i < ret.Width; ++i)
        {
          ret.SetPixel_RGBA32ub(i, j, normalizePixel32(i, j, isbumpmap, depth_amount));
        }
      }

      return ret;
    }
    public static Image RandomImage_R32f(int size_x, int size_y, Minimax<float> height)
    {
      //Get random image
      Image rando = new Image(Gu.Lib.GetUniqueName(ResourceType.Image, "rand"), size_x, size_y, ImagePixelFormat.R32f);

      for (int j = 0; j < rando.Height; ++j)
      {
        for (int i = 0; i < rando.Width; ++i)
        {
          rando.SetPixel_R32f(i, j, Rand.NextFloat(height));
        }
      }
      return rando;
    }
    public Image CreateHeightMap(int ksize = 1, float kweight = 0.1f, int smooth_iterations = 1)
    {
      //creates a NOT NORMALIZED height map call Normalize to normalize
      //ksize= kernel radius - has little effect on the map, k=1 will produce a little "deeper" map vs k=2,3..
      //kweight= filter weight - 0 means no filtering, 1 = full filter. 0 is an invalid value.
      //iterations= number of filter iterations - will create a much smoother image. 3 = very smooth.
      Gu.Assert(this.Format == ImagePixelFormat.R32f);
      var kern = ImageKernel<double>.Gaussian(ksize, kweight);
      var ret = ApplyKernel_R32f(kern, smooth_iterations);
      return ret;
    }
    private Image ApplyKernel_R32f(ImageKernel<double> kern, int iterations)
    {
      Gu.Assert(this.Format == ImagePixelFormat.R32f);
      Gu.Assert(iterations > 0);
      Image ret_last = this;
      Image ret = this;
      for (int iter = 0; iter < iterations; iter++)
      {
        ret = (Image?)ret.Clone();
        ret_last.Iterate((x, y) =>
        {
          var test_p = GetPixel_R32f(x, y);
          float fval = ret_last.ComputeKernelForPixel_R32f(x, y, kern);
          ret.SetPixel_R32f(x, y, fval);

          return LambdaBool.Continue;
        });
        ret_last = ret;
      }
      return ret;
    }
    public void Iterate(Func<int, int, LambdaBool> f)
    {
      for (int y = 0; y < Height; y++)
      {
        for (int x = 0; x < Width; x++)
        {
          if (f(x, y) == LambdaBool.Break)
          {
            break;
          }
        }
      }
    }
    private float ComputeKernelForPixel_R32f(int ix, int iy, ImageKernel<double> k)
    {
      float sum = 0;
      float count = 0;
      k.Iterate((kx, ky) =>
      {
        var v = k[ky, kx];
        var p = GetPixel_R32f(hwrap(ix + kx - k.RadiusPixelsX), vwrap(iy + ky - k.RadiusPixelsY));
        sum += (float)v * (float)p;
        count += 1;
        return LambdaBool.Continue;
      });

      return sum;
    }
    public Image Normalized_R32f()
    {
      Gu.Assert(this.Format == ImagePixelFormat.R32f);
      //Normalize floating point image from [-inf, inf] to [0,1]
      //Takes Maximum/minimum value from image
      Image ret = (Image?)this.Clone();
      float min = float.MaxValue;
      float max = float.MinValue;
      ret.Iterate((x, y) =>
      {
        var p = ret.GetPixel_R32f(x, y);
        min = Math.Min(p, min);
        max = Math.Max(p, max);
        return LambdaBool.Continue;
      });
      ret.Iterate((x, y) =>
      {
        var p = ret.GetPixel_R32f(x, y);
        p = (max - p) / (max - min);
        ret.SetPixel_R32f(x, y, p);

        return LambdaBool.Continue;
      });
      return ret;
    }
    public Image Convert(ImagePixelFormat toFmt, bool set_rgba_alpha_to_one = true)
    {
      Image cpy = new Image(this.Name + "-converted", this.Width, this.Height, toFmt);
      if (this.Format == ImagePixelFormat.R32f)
      {
        Image normalized = this.Normalized_R32f();
        if (toFmt == ImagePixelFormat.RGBA32ub)
        {
          normalized.Iterate((x, y) =>
          {
            var fp = normalized.GetPixel_R32f(x, y);
            Pixel4ub p = new Pixel4ub();
            p.r = (byte)Math.Round((float)Byte.MaxValue * fp);
            p.g = (byte)Math.Round((float)Byte.MaxValue * fp);
            p.b = (byte)Math.Round((float)Byte.MaxValue * fp);
            p.a = set_rgba_alpha_to_one ? Byte.MaxValue : (byte)Math.Round((float)Byte.MaxValue * fp);
            cpy.SetPixel_RGBA32ub(x, y, p);

            return LambdaBool.Continue;
          });
        }

      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return cpy;
    }
    public static byte toGray(Pixel4ub pix)
    {
      return (byte)((11 * pix.r + 16 * pix.g + 5 * pix.b) / 32);
    }
    public void SetPixel_R32f(int x, int y, float pix)
    {
      int off = vofftos(x, y, Width) * BytesPerPixel;  //StaticBufffer is a char array so we must scale the size
      unsafe
      {
        fixed (byte* b = Data)
        {
          *((float*)(b + off)) = pix;
        }
      }
    }
    public float GetPixel_R32f(int x, int y)
    {
      Gu.Assert(this.Format == ImagePixelFormat.R32f);
      Gu.Assert(x <= this.Width && y <= this.Height && x >= 0 && y >= 0);
      int off = vofftos(x, y, Width) * BytesPerPixel;  //StaticBufffer is a char array so we must scale the size
      unsafe
      {
        fixed (byte* b = Data)
        {
          return *((float*)(b + off));
        }
      }
    }
    public Pixel4ub GetPixel_RGBA32ub(int x, int y)
    {
      Gu.Assert(this.Format == ImagePixelFormat.RGBA32ub);
      Pixel4ub pix;

      int off = vofftos(x, y, Width) * BytesPerPixel;  //StaticBufffer is a char array so we must scale the size
      pix.r = Data[off + 0];
      pix.g = Data[off + 1];
      pix.b = Data[off + 2];
      pix.a = Data[off + 3];

      return pix;
    }
    public void SetPixel_RGBA32ub(int x, int y, Pixel4ub pix)
    {
      Gu.Assert(this.Format == ImagePixelFormat.RGBA32ub);
      int off = vofftos(x, y, Width) * BytesPerPixel;  //StaticBufffer is a char array so we must scale the size
      Data[off + 0] = pix.r;
      Data[off + 1] = pix.g;
      Data[off + 2] = pix.b;
      Data[off + 3] = pix.a;
      //Modify...
    }
    public int hwrap(int off)
    {
      int ret = off % Width;
      if (ret < 0)
        ret += Width;
      return ret;
    }
    public int vwrap(int off)
    {
      int ret = off % Height;
      if (ret < 0)
        ret += Height;
      return ret;
    }
    public Pixel4ub normalizePixel32(int x, int y, bool is_bump_map_pixel, float depth_amount)
    {
      //TODO: use the new Kernel methods here.
      //is_bump_map_pixel - if we are a bump map, use the pixel's grayscale value for the normal depth.
      Gu.Assert(Data != null);

      Pixel4ub pix;
      int Gh = 0, Gv = 0, i;
      float len;
      int[] mat = new int[]{
            toGray(GetPixel_RGBA32ub(hwrap(x - 1), vwrap(y - 1))), toGray(GetPixel_RGBA32ub(hwrap(x - 0), vwrap(y - 1))), toGray(GetPixel_RGBA32ub(hwrap(x + 1), vwrap(y - 1))),
            toGray(GetPixel_RGBA32ub(hwrap(x - 1), vwrap(y + 0))), toGray(GetPixel_RGBA32ub(hwrap(x - 0), vwrap(y + 0))), toGray(GetPixel_RGBA32ub(hwrap(x + 1), vwrap(y + 0))),
            toGray(GetPixel_RGBA32ub(hwrap(x - 1), vwrap(y + 1))), toGray(GetPixel_RGBA32ub(hwrap(x - 0), vwrap(y + 1))), toGray(GetPixel_RGBA32ub(hwrap(x + 1), vwrap(y + 1)))};

      int[] sobel_v = {
            -1, -2, -1,

            0, 0, 0,

            1, 2, 1};
      int[] sobel_h = {
            1, 0, -1,

            2, 0, -2,

            1, 0, -1};
      for (i = 0; i < 9; ++i)
        Gh += mat[i] * sobel_h[i];
      for (i = 0; i < 9; ++i)
        Gv += mat[i] * sobel_v[i];

      float Fh = (float)Gh / (float)(255 * 4);// [-1,1]
      float Fv = (float)Gv / (float)(255 * 4);// [-1,1]

      float depth = 0;
      if (is_bump_map_pixel)
      {
        //Bump map->normal map conversion - Use as bump map  depth
        var pr = GetPixel_RGBA32ub(hwrap(x), vwrap(y)).r;
        var pg = GetPixel_RGBA32ub(hwrap(x), vwrap(y)).g;
        var pb = GetPixel_RGBA32ub(hwrap(x), vwrap(y)).b;
        depth = (float)(pr - 127) / 127.0f;//[-1,1]
        if (depth < -0.1)
        {
          int n = 0;
          n++;
        }
        Fh *= (1.0f - Math.Abs(depth)) * Math.Sign(depth);//use sign to flip h/v direction
        Fv *= (1.0f - Math.Abs(depth)) * Math.Sign(depth);
      }
      else
      {
        depth = 1.0f - depth_amount;
        Fh *= depth_amount;
        Fv *= depth_amount;
      }

      //Normalize
      len = (float)Math.Sqrt((Fh * Fh) + (Fv * Fv) + (depth * depth));
      if (len > 0)
      {
        Fh /= len;
        Fv /= len;
        depth /= len;
      }
      else
      {
        Fh = Fv = 0;
        depth = 1;
      }

      //x * 2 - 1 = y
      //x = (y + 1 )/ 2
      pix.r = (byte)(Math.Floor((Fh + 1.0f) / 2.0f * 255.0f));
      pix.g = (byte)(Math.Floor((depth + 1.0f) / 2.0f * 255.0f));
      pix.b = (byte)(Math.Floor((Fv + 1.0f) / 2.0f * 255.0f));
      pix.a = 255;

      //This format must match the Texutre.default normal map
      if (Texture.NormalMapFormat == NormalMapFormat.Zup)
      {
        byte tmp = pix.g;
        pix.g = pix.b;
        pix.b = tmp;
      }

      return pix;
    }
    public void Flip(bool fliph, bool flipv)
    {
      byte[] st = new byte[this.Data.Length];

      int rowsize = this.BytesPerPixel * this.Width;
      int h = this.Height;

      for (int yi = 0; yi < this.Height; ++yi)
      {
        for (int xi = 0; xi < this.Width; xi++)
        {
          int yoff = yi;
          if (flipv)
          {
            // Swap the scanlines
            yoff = (Height - yi - 1);
          }
          int xoff = xi;
          if (fliph)
          {
            xoff = (Width - xi - 1);
          }

          Buffer.BlockCopy(Data, (yi * Width + xi) * BytesPerPixel, st, (yoff * Width + xoff) * BytesPerPixel, BytesPerPixel);
        }
      }

      this.Data = st;
    }
    public Image Clone()
    {
      Image other = (Image)this.MemberwiseClone();
      if (_data != null)
      {
        other._data = new byte[_data.Length];
        Buffer.BlockCopy(this._data, 0, other._data, 0, this._data.Length);
      }
      return other;
    }

  }
}
