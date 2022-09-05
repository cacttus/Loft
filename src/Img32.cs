﻿using System;
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
  //Note: this class initializes the data buffer when you create it. It requires a w/h
  public class Img32
  {
    public enum ImagePixelFormat
    {
      Undefined,
      RGB24ub,
      BGR24ub,
      RGBA32ub,
      BGRA32ub,
    }
    public string Name { get; private set; } = "img32-unnamed";
    public int Width { get; private set; } = 0;
    public int Height { get; private set; } = 0;
    public float SizeRatio { get { return Height != 0 ? Width / Height : 1; } }
    public byte[] Data { get; private set; } = null;

    public int BytesPerPixel
    {
      get
      {
        if (Format == ImagePixelFormat.RGBA32ub || Format == ImagePixelFormat.BGRA32ub)
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
    public ImagePixelFormat Format { get; private set; } = ImagePixelFormat.Undefined;

    private Img32()
    {
    }
    public static Img32 Default1x1_RGBA32ub(byte r, byte g, byte b, byte a)
    {
      return new Img32("default1x1", 1, 1, new byte[] { r, g, b, a }, ImagePixelFormat.RGBA32ub);
    }
    public static Img32 Default1x1_RGBA32ub(vec4ub v4)
    {
      return Default1x1_RGBA32ub(v4.r, v4.g, v4.b, v4.a);
    }
    public Img32 Clone()
    {
      Img32 m = new Img32();
      m.Name = Name;
      m.Width = Width;
      m.Height = Height;
      m.Data = new byte[Data.Length];
      m.BytesPerPixel = BytesPerPixel;
      m.Format = Format;
      Buffer.BlockCopy(Data, 0, m.Data, 0, Data.Length);
      return m;
    }
    public Img32(string name, int w, int h, ImagePixelFormat sc = ImagePixelFormat.RGBA32ub)
    {
      //Note if data is null data will still get allocated
      init(name, w, h, null, sc);
    }
    public Img32(string name, int w, int h, byte[] data, ImagePixelFormat sc = ImagePixelFormat.RGBA32ub)
    {
      //Note if data is null data will still get allocated
      init(name, w, h, data, sc);
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
    public void init(string name, int w, int h, byte[] data, ImagePixelFormat sc)
    {
      Width = w;
      Height = h;
      Format = sc;
      Name = name + "-img32";
      if (data == null)
      {
        data = new byte[w * h * BytesPerPixel];
      }

      Gu.Assert(data.Length == BytesPerPixel * w * h);

      Data = data;
    }
    public Img32 copySubImageTo(ivec2 off, ivec2 size)
    {
      Img32 ret = new Img32("subimage-cpy", size.x, size.y, Format);
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
        int srcff = vofftos(otherOff.x, otherOff.y + iScanLine, pOtherImage.Width) * pOtherImage.BytesPerPixel;

        //BlockCopy: src,srcoff, dst, dstoff, count
        Buffer.BlockCopy(pOtherImage.Data, srcff, Data, dstff, scanLineByteSize);
      }
    }
    private int vofftos(int row, int col, int items_per_row)
    {
      return (col * items_per_row + row);
    }
    public Img32 CreateNormalMap()
    {
      //This is too slow for C# //TODO: put on GPU
      if (Data == null)
      {
        return null;
      }
      Img32 ret = this.Clone();

      for (int j = 0; j < ret.Height; ++j)
      {
        for (int i = 0; i < ret.Width; ++i)
        {
          ret.SetPixel32(i, j, normalizePixel32(i, j));
        }
      }

      return ret;
    }
    private byte toGray(Pixel4ub pix)
    {
      return (byte)((11 * pix.r + 16 * pix.g + 5 * pix.b) / 32);
    }
    public Pixel4ub GetPixel32(int x, int y)
    {
      Pixel4ub pix;

      int off = vofftos(x, y, Width) * BytesPerPixel;  //StaticBufffer is a char array so we must scale the size
      pix.r = Data[off + 0];
      pix.g = Data[off + 1];
      pix.b = Data[off + 2];
      pix.a = Data[off + 3];

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
    public void SetPixel32(int x, int y, Pixel4ub pix)
    {
      int off = vofftos(x, y, Width) * BytesPerPixel;  //StaticBufffer is a char array so we must scale the size
      Data[off + 0] = pix.r;
      Data[off + 1] = pix.g;
      Data[off + 2] = pix.b;
      Data[off + 3] = pix.a;
      //Modify...
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
            toGray(GetPixel32(hwrap(x - 1), vwrap(y - 1))), toGray(GetPixel32(hwrap(x - 0), vwrap(y - 1))), toGray(GetPixel32(hwrap(x + 1), vwrap(y - 1))),
            toGray(GetPixel32(hwrap(x - 1), vwrap(y + 0))), toGray(GetPixel32(hwrap(x - 0), vwrap(y + 0))), toGray(GetPixel32(hwrap(x + 1), vwrap(y + 0))),
            toGray(GetPixel32(hwrap(x - 1), vwrap(y + 1))), toGray(GetPixel32(hwrap(x - 0), vwrap(y + 1))), toGray(GetPixel32(hwrap(x + 1), vwrap(y + 1)))};
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
