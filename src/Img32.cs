using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace PirateCraft
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
  public class Img32
  {
    public int Width { get; private set; } = 0;
    public int Height { get; private set; } = 0;
    public byte[] Data { get; private set; }
    public int BytesPerPixel { get { return 4; } private set { } }//Always 4BPP in our system.
    public static Img32 Default1x1(byte r, byte g, byte b, byte a) { return new Img32(1, 1, new byte[] { r, g, b, a }); }

    public Img32() 
    { 
    }
    public Img32 Clone()
    {
      Img32 m = new Img32();
      m.Width = Width;
      m.Height = Height;
      m.BytesPerPixel = BytesPerPixel;
      m.Data = new byte[Data.Length];
      Buffer.BlockCopy(Data, 0, m.Data, 0, Data.Length);
      return m;
    }
    public Img32(int w, int h)
    {
      byte[] rgbValues = new byte[w * h * BytesPerPixel];
      init(w, h, rgbValues);
    }
    public Img32(int w, int h, byte[] rgbValues)
    {
      if (rgbValues.Length % BytesPerPixel != 0)
      {
        Gu.Log.Error("the input RGB values were not divisible by the given bytes per pixel. This will result in undefined behavior.");
      }
      init(w, h, rgbValues);
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
    public void init(int w, int h, byte[] data)
    {
      Width = w;
      Height = h;
      Data = data;
    }
    public Img32 copySubImageTo(ivec2 off, ivec2 size)
    {
      Img32 ret = new Img32(size.x, size.y);
      // ret.init(size.x, size.y);//ret.create(size.x, size.y);
      ret.copySubImageFrom(new ivec2(0, 0), off, size, this);
      return ret;
    }
    //Image formats must be identical
    public void copySubImageFrom(ivec2 myOff, ivec2 otherOff, ivec2 size, Img32 pOtherImage)
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
        int srcff = vofftos(otherOff.x, otherOff.y + (nLines - iScanLine - 1), pOtherImage.Width) * pOtherImage.BytesPerPixel;

        //BlockCopy: src,srcoff, dst, dstoff, count
        Buffer.BlockCopy(pOtherImage.Data, srcff, Data, dstff, scanLineByteSize);
      }
    }
    public int vofftos(int row, int col, int items_per_row)
    {
      return (col * items_per_row + row);
    }

    public Img32 createNormalMap()
    {
      if (Data == null)
      {
        return null;
      }
      Img32 ret = this.Clone();

      for (int j = 0; j < ret.Height; ++j)
      {
        for (int i = 0; i < ret.Width; ++i)
        {
          ret.setPixel32(i, j, normalizePixel32(i, j));
        }
      }

      return ret;
    }
    byte toGray(Pixel4ub pix)
    {
      return (byte)((11 * pix.r + 16 * pix.g + 5 * pix.b) / 32);
    }
    public Pixel4ub getPixel32(int x, int y)
    {
      Pixel4ub pix;

      int off = vofftos(x, y, Width) * BytesPerPixel;  //StaticBufffer is a char array so we must scale the size
      pix.r = Data[off + 0];
      pix.g = Data[off + 1];
      pix.b = Data[off + 2];
      pix.a = Data[off + 3];

      return pix;
    }
    public void setPixel32(int x, int y, Pixel4ub pix)
    {
      int off = vofftos(x, y, Width) * BytesPerPixel;  //StaticBufffer is a char array so we must scale the size
      Data[off + 0] = pix.r;
      Data[off + 1] = pix.g;
      Data[off + 2] = pix.b;
      Data[off + 3] = pix.a;
    }
    int hwrap(int off)
    {
      int ret = off % Width;
      if (ret < 0)
        ret += Width;
      return ret;
    }
    int vwrap(int off)
    {
      int ret = off % Height;
      if (ret < 0)
        ret += Height;
      return ret;
    }
    Pixel4ub normalizePixel32(int x, int y)
    {
      Gu.Assert(Data != null);

      Pixel4ub pix;
      int Gh = 0, Gv = 0, i;
      float len;
      int[] mat = {
            toGray(getPixel32(hwrap(x - 1), vwrap(y - 1))), toGray(getPixel32(hwrap(x - 0), vwrap(y - 1))), toGray(getPixel32(hwrap(x + 1), vwrap(y - 1))),
            toGray(getPixel32(hwrap(x - 1), vwrap(y + 0))), toGray(getPixel32(hwrap(x - 0), vwrap(y + 0))), toGray(getPixel32(hwrap(x + 1), vwrap(y + 0))),
            toGray(getPixel32(hwrap(x - 1), vwrap(y + 1))), toGray(getPixel32(hwrap(x - 0), vwrap(y + 1))), toGray(getPixel32(hwrap(x + 1), vwrap(y + 1)))};
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

      //Max value of a sobel kernel with char bytes is 255 * +-4 = [-1020,1020] mapped [-1,1]=>[0,1]
      float Fh = (float)(Gh + 1020.0f) / 2040.0f;
      float Fv = (float)(Gv + 1020.0f) / 2040.0f;

      //To get a full hemisphere of values.
      //Partial depth can be blended with surface normal if less bump is needed.
      float depth = ((2 - Fh - Fv) + 1.0f) / 2.0f;

      len = (float)Math.Sqrt((Fh * Fh) + (Fv * Fv) + depth * depth);
      if (len > 0)
      {
        Fh /= len;
        Fv /= len;
        depth /= len;
      }
      else
      {
        Fh = Fv = depth = 0;
      }

      pix.r = (byte)(Math.Floor(Fh * 255.0f));
      pix.g = (byte)(Math.Floor(depth * 255.0f));
      pix.b = (byte)(Math.Floor(Fv * 255.0f));
      pix.a = 255;

      //This format must match the Texutre.default normal map
      if (Texture2D.NormalMapFormat == NormalMapFormat.Zup)
      {
        byte tmp = pix.g;
        pix.g = pix.b;
        pix.b = tmp;
      }

      return pix;
    }




  }
}
