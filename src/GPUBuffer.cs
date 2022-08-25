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

    private VertexFormat _format = null;
    public VertexFormat Format { get { return _format; } }
    public DrawElementsType DrawElementsType { get; private set; } = DrawElementsType.UnsignedInt;//only valid for buffertarget=elementarraybuffer

    public BufferTarget BufferTarget { get; private set; } = BufferTarget.ArrayBuffer;
    public int ItemCount { get { return _itemCount; } }
    public int ItemSizeBytes { get { return _format.ItemSizeBytes; } }

    public GPUBuffer(string name, VertexFormat fmt, BufferTarget t, GpuDataPtr items) : base(name + "-buffer")
    {
      Gu.Assert(fmt != null);
      Gu.Assert(items != null);
      BufferTarget = t;
      _format = fmt;
      _glId = GL.GenBuffer();
      Allocate(items);
      SetObjectLabel();

      if (t == BufferTarget.ElementArrayBuffer)
      {
        if (fmt.ItemSizeBytes == 1)
        {
          DrawElementsType = DrawElementsType.UnsignedByte;
        }
        else if (fmt.ItemSizeBytes == 2)
        {
          DrawElementsType = DrawElementsType.UnsignedShort;
        }
        else if (fmt.ItemSizeBytes == 4)
        {
          DrawElementsType = DrawElementsType.UnsignedInt;
        }
        else
        {
          //Uh..
          Gu.BRThrowException("Invalid element array buffer type.");
        }
      }
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
      Gpu.CheckGpuErrorsDbg();
    }
    public void Unbind()
    {
      GL.BindBuffer(BufferTarget, 0);
      Gpu.CheckGpuErrorsDbg();
    }
    public static void UnbindBuffer(BufferTarget t)
    {
      GL.BindBuffer(t, 0);
      Gpu.CheckGpuErrorsDbg();
    }
    public GpuDataArray CopyDataFromGPU(int itemOffset = 0, int itemCount = -1)
    {
      //Copies GPU data into a temporary byte array.
      //GpuDataArray is a kind of proxy class that munges data into a managed byte array.

      //**TODO: fix this to use GpuDataPtr and raw copy - Get rid of GpuDataArray
      GpuDataArray d = null;

      int offsetBytes = itemOffset * _itemSize;
      int lengthBytes = (itemCount <= -1) ? (_itemCount * _itemSize) : ((int)itemCount * _itemSize);
      Bind();
      {
        IntPtr pt = GL.MapBufferRange(BufferTarget, (IntPtr)offsetBytes, (IntPtr)lengthBytes, BufferAccessMask.MapReadBit);
        Gpu.CheckGpuErrorsDbg();
        if (pt == IntPtr.Zero)
        {
          Gu.BRThrowException("Failed to map OpenGL Buffer.");
        }
        byte[] managedArray = new byte[lengthBytes];
        Marshal.Copy(pt, managedArray, 0, (int)lengthBytes);
        GL.UnmapBuffer(BufferTarget);
        d = new GpuDataArray(_itemSize, _itemCount, managedArray);
      }
      Unbind();

      return d;
    }
    public void CopyDataToGPU(GpuDataPtr src, int dstOffItems, int srcItemCount = -1)
    {
      Gu.Assert(src.ItemSizeBytes == this.ItemSizeBytes);
      Gu.Assert(srcItemCount <= src.Count);
      Gu.Assert(srcItemCount <= this.ItemCount);

      int srclengthBytes = (srcItemCount <= -1) ? (src.Count * _itemSize) : ((int)srcItemCount * _itemSize);
      int dstlengthBytes = this.ItemCount * this.ItemSizeBytes;

      Bind();
      {
        IntPtr pdst = GL.MapBufferRange(BufferTarget, (IntPtr)dstOffItems, (IntPtr)srclengthBytes, BufferAccessMask.MapWriteBit);
        Gpu.CheckGpuErrorsDbg();
        if (pdst == IntPtr.Zero)
        {
          Gu.BRThrowException("Failed to map OpenGL Buffer.");
        }
        IntPtr psrc = src.Lock();
        unsafe
        {
          System.Buffer.MemoryCopy((void*)psrc, (void*)pdst, dstlengthBytes, srclengthBytes);
        }
        src.Unlock();
      }
      GL.UnmapBuffer(BufferTarget);
      Unbind();
    }
    private void Allocate(GpuDataPtr items)
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
      Gpu.CheckGpuErrorsDbg();
      Unbind();

    }
  }

}