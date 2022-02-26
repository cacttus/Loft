using System.Runtime.InteropServices;

namespace PirateCraft
{
   //This class doesn't actually make datra copies, instead this is just a GCHandle to managed data
   //Then we can pin it, and then use it in an OpenGL funcion. Much faster than GpuDataArray
   public class GpuDataPtr
   {
      private bool _locked = false;
      private GCHandle pinnedArray;
      object _pt;
      public int ItemSizeBytes { get; private set; } = 0;
      public int Count { get; private set; } = 0;
      public GpuDataPtr(int itemSize, int count, object pt)
      {
         ItemSizeBytes = itemSize;
         Count = count;
         _pt = pt;
      }
      public IntPtr Lock()
      {
         _locked = true;
         pinnedArray = GCHandle.Alloc(_pt, GCHandleType.Pinned);
         return pinnedArray.AddrOfPinnedObject();
      }
      public void Unlock()
      {
         pinnedArray.Free();
         _locked = false;
      }
      ~GpuDataPtr()
      {
         if (_locked)
         {
            Gu.Log.Error("GpuDataPtr unmanaged handle wasn't freed. Must call Unlock().");
            Gu.DebugBreak();
         }
      }
   }
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