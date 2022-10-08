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

    public VertexFormat Format { get; private set; } = null;//This can be null now for non vertex-input buffers
    public DrawElementsType DrawElementsType { get; private set; } = DrawElementsType.UnsignedInt;//only valid for buffertarget=elementarraybuffer
    public BufferTarget BufferTarget { get; private set; } = BufferTarget.ArrayBuffer;
    public BufferRangeTarget? RangeTarget { get; private set; } = null;//For buffer block objects 
    public int ItemCount { get { return _itemCount; } }
    public int ItemSizeBytes { get { return _itemSize; } }

    public GPUBuffer(string name, VertexFormat fmt, BufferTarget t, int itemSize, int itemCount, object items) : base(name)
    {
      BufferTarget = t;

      if (t == BufferTarget.UniformBuffer) { RangeTarget = BufferRangeTarget.UniformBuffer; }
      else if (t == BufferTarget.ShaderStorageBuffer) { RangeTarget = BufferRangeTarget.ShaderStorageBuffer; }
      else if (t == BufferTarget.ArrayBuffer) { RangeTarget = null; }
      else if (t == BufferTarget.ElementArrayBuffer) { RangeTarget = null; }
      else { Gu.BRThrowNotImplementedException(); }

      Format = fmt;
      _glId = GL.GenBuffer();
      Gpu.CheckGpuErrorsDbg();
      _itemCount = itemCount;
      _itemSize = itemSize;
      Allocate(items);
      SetObjectLabel();

      if (t == BufferTarget.ElementArrayBuffer)
      {
        Gu.Assert(fmt != null);
        if (fmt.VertexSizeBytes == 1)
        {
          DrawElementsType = DrawElementsType.UnsignedByte;
        }
        else if (fmt.VertexSizeBytes == 2)
        {
          DrawElementsType = DrawElementsType.UnsignedShort;
        }
        else if (fmt.VertexSizeBytes == 4)
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
    public GpuDataArray CopyDataFromGPU(int itemOffset = 0, int itemCount = -1, bool useMemoryBarrier = false)
    {
      //TODO: this is super slow, we can use Marshal.Copy(IntPtr, IntPtr[] to be faster, and also template this method)
      //Copies GPU data into a temporary byte array.
      //GpuDataArray is a kind of proxy class that munges data into a managed byte array.

      //**TODO: fix this to use GpuDataPtr and raw copy - Get rid of GpuDataArray
      GpuDataArray d = null;

      int offsetBytes = itemOffset * _itemSize;
      int lengthBytes = (itemCount <= -1) ? (_itemCount * _itemSize) : ((int)itemCount * _itemSize);

      if (useMemoryBarrier || this.BufferTarget == BufferTarget.ShaderStorageBuffer)//SSBOs reads and writes use incoherent memory accesses, so they need the appropriate barriers
      {
        MemoryBarrier();
      }

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
    public void CopyDataToGPU(GpuDataPtr src, int dstOffItems, int srcItemCount = -1, bool useMemoryBarrier = false)
    {
      Gu.Assert(src.ItemSizeBytes == this.ItemSizeBytes);
      Gu.Assert(srcItemCount <= src.Count);
      Gu.Assert(srcItemCount <= this.ItemCount);

      int srclengthBytes = (srcItemCount <= -1) ? (src.Count * _itemSize) : ((int)srcItemCount * _itemSize);
      int dstlengthBytes = this.ItemCount * this.ItemSizeBytes;

      if (useMemoryBarrier || this.BufferTarget == BufferTarget.ShaderStorageBuffer)//SSBOs reads and writes use incoherent memory accesses, so they need the appropriate barriers
      {
        MemoryBarrier();
      }

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

    private void MemoryBarrier()
    {
      MemoryBarrierFlags f = MemoryBarrierFlags.UniformBarrierBit;
      if (this.BufferTarget == BufferTarget.UniformBuffer) { f = MemoryBarrierFlags.UniformBarrierBit; }
      else if (this.BufferTarget == BufferTarget.ShaderStorageBuffer) { f = MemoryBarrierFlags.ShaderStorageBarrierBit; }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      GL.MemoryBarrier(f);
    }
    private void Allocate(object items)
    {
      if (this.BufferTarget == BufferTarget.ShaderStorageBuffer)//SSBOs reads and writes use incoherent memory accesses, so they need the appropriate barriers
      {
        MemoryBarrier();
      }

      Bind();
      var pinnedHandle = GCHandle.Alloc(items, GCHandleType.Pinned);
      GL.BufferData(
                BufferTarget,
          (int)(_itemCount * _itemSize),
          pinnedHandle.AddrOfPinnedObject(),
          BufferUsageHint.StaticDraw
          );
      pinnedHandle.Free();
      Gpu.CheckGpuErrorsDbg();
      Unbind();
    }
  }


}//NS