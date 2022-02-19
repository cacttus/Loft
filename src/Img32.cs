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
   }
   public class Img32
   {
      public int Width { get; private set; } = 0;
      public int Height { get; private set; } = 0;
      public byte[] Data { get; private set; }
      public int BytesPerPixel { get { return 4; } private set { } }//Always 4BPP in our system.

      public Img32()
      {
      }
      public Bitmap ToBitmap()
      {
         Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

         var bmpData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.WriteOnly,
            bitmap.PixelFormat);

         int bitmap_size_bytes = Math.Abs(bmpData.Stride) * bitmap.Height;

         Gu.Assert(bitmap_size_bytes == Data.Length);

         //byte[] newData = new byte[Data.Length];
         //Flip Back
         //for (int i = 0; i < newData.Length; i += 4)
         //{
         //   //Argb -> Rgba
         //   var r = Data[i + 0];//This is correct
         //   var g = Data[i + 1];
         //   var b = Data[i + 2];
         //   var a = Data[i + 3];
         //   newData[i + 0] = r;
         //   newData[i + 1] = g;
         //   newData[i + 2] = b;
         //   newData[i + 3] = a;
         //}
         IntPtr ptr = bmpData.Scan0;
         Marshal.Copy(Data, 0, ptr, bitmap_size_bytes);
         bitmap.UnlockBits(bmpData);

         return bitmap;
      }
      public Img32(Bitmap bitmap)
      {
         //Exactly what I needed thanks Microsoft
         Gu.Assert(bitmap != null);
         var bmpData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
         IntPtr ptr = bmpData.Scan0;
         int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
         byte[] rgbValues = new byte[bytes];
         Marshal.Copy(ptr, rgbValues, 0, bytes);
         bitmap.UnlockBits(bmpData);

         //BGRA ==> RGBA
         //for (int i = 0; i < bytes; i += 4)
         //{
         //   //Argb -> Rgba
         //   var b = rgbValues[i + 0];//This is correct
         //   var g = rgbValues[i + 1];
         //   var r = rgbValues[i + 2];
         //   var a = rgbValues[i + 3];
         //   rgbValues[i + 0] = b;
         //   rgbValues[i + 1] = g;
         //   rgbValues[i + 2] = r;
         //   rgbValues[i + 3] = a;
         //}

         //Note we may need to swap rgba data here.

         init(bitmap.Width, bitmap.Height, rgbValues);
      }
      public void init(int w, int h, byte[] data = null)
      {
         Width = w;
         Height = h;
         Data = data;
      }
      public Img32 copySubImageTo(ivec2 off, ivec2 size)
      {
         Img32 ret = new Img32();
         ret.init(size.x, size.y);//ret.create(size.x, size.y);
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

   }
}
