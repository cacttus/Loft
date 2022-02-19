using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace PirateCraft
{
   //Represents data from or to the GPU.
   //GpuDataArray is a kind of proxy class that munges data into a managed byte array.
   // Can convert the byte data into an IntPtr to be used by OpenTK.
   public class GpuDataArray
   {
      private bool _locked = false;
      private GCHandle pinnedArray;

      public byte[] Bytes { get; private set; } = null; // Managed Array

      public int ItemSizeBytes { get; private set; } = 0;
      public int Count { get; private set; } = 0;

      public GpuDataArray(int itemSize, int count, byte[] pt)
      {
         ItemSizeBytes = itemSize;
         Count = count;
         Bytes = pt;
      }
      public IntPtr Lock()
      {
         _locked = true;
         pinnedArray = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
         return pinnedArray.AddrOfPinnedObject();
      }
      public void Unlock()
      {
         pinnedArray.Free();
         _locked = false;
      }
      ~GpuDataArray()
      {
         if (_locked)
         {
            Gu.Log.Error("Gpu Data array unmanaged handle wasn't freed. Must call Unlock().");
            Gu.DebugBreak();
         }
      }
   }

}