using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PirateCraft
{
  //A buffer on GPU, vertex buffer, index buffer .. 
  public class GPUBuffer : OpenGLResource
  {
    //private int _glBufferId = 0;
    private int _itemCount = 0;
    private int _itemSize = 0;

    public BufferTarget BufferTarget { get; private set; } = BufferTarget.ArrayBuffer;
    public int ItemCount { get { return _itemCount; } }
    public int ItemSizeBytes { get { return _itemSize; } }

    public GPUBuffer(BufferTarget t, GpuDataPtr items)
    {
      BufferTarget = t;
      _glId = GL.GenBuffer();
      Allocate(items);
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsBuffer(_glId))
      {
        GL.DeleteBuffers(1, ref _glId);
      }
    }
    public void Bind()
    {
      GL.BindBuffer(BufferTarget, _glId);
    }
    public void Unbind()
    {
      GL.BindBuffer(BufferTarget, 0);
      Gpu.CheckGpuErrorsDbg();
    }
    public GpuDataArray CopyDataFromGPU(int itemOffset = 0, int itemCount = -1)
    {
      //Copies GPU data into a temporary byte array.
      //GpuDataArray is a kind of proxy class that munges data into a managed byte array.

      //**TODO: fix this to use GpuDataPtr and raw copy - Get rid of GpuDataArray

      int offsetBytes = itemOffset * _itemSize;
      int lengthBytes = (itemCount <= -1) ? (_itemCount * _itemSize) : ((int)itemCount * _itemSize);
      Bind();
      IntPtr pt = GL.MapBufferRange(BufferTarget, (IntPtr)offsetBytes, (IntPtr)lengthBytes, BufferAccessMask.MapReadBit);
      if (pt == IntPtr.Zero)
      {
        Gu.BRThrowException("Failed to map OpenGL Buffer."); 
      }
      byte[] managedArray = new byte[lengthBytes];
      Marshal.Copy(pt, managedArray, 0, (int)lengthBytes);
      GL.UnmapBuffer(BufferTarget);
      Unbind();

      GpuDataArray d = new GpuDataArray(_itemSize, _itemCount, managedArray);
      return d;
    }
    public void CopyDataToGPU(GpuDataPtr src, int dstOffItems, int srcItemCount = -1)
    {
      Gu.Assert(src.ItemSizeBytes == this.ItemSizeBytes);
      Gu.Assert(srcItemCount <= src.Count);
      Gu.Assert(srcItemCount <= this.ItemCount);

      int srclengthBytes = (srcItemCount <= -1) ? (src.Count * _itemSize) : ((int)srcItemCount * _itemSize);

      Bind();
      {
        IntPtr pdst = GL.MapBufferRange(BufferTarget, (IntPtr)dstOffItems, (IntPtr)srclengthBytes, BufferAccessMask.MapReadBit);
        if (pdst == IntPtr.Zero)
        {
          Gu.BRThrowException("Failed to map OpenGL Buffer.");
        }
        IntPtr psrc = src.Lock();
        unsafe
        {
          System.Buffer.MemoryCopy((void*)psrc, (void*)pdst, this.ItemCount, srclengthBytes);
        }
        src.Unlock();
      }
      Unbind();
    }
    void Allocate(GpuDataPtr items)
    {
      _itemCount = items.Count;
      _itemSize = items.ItemSizeBytes;
      Bind();
      GL.BufferData(
                BufferTarget,
          (int)(items.Count * items.ItemSizeBytes),
          items.Lock(),
          BufferUsageHint.StaticDraw
          );
      items.Unlock();
      Unbind();

    }
  }

}