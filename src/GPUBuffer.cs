using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Loft
{
  public class GPUBuffer : OpenGLResource
  {
    //@brief GPU, vertex buffer, index buffer, ubo, ssbo, .. 
    //@note Mutability:
    //      glBufferStorage and glBufferData?
    //      https://stackoverflow.com/questions/27810542/what-is-the-difference-between-glbufferstorage-and-glbufferdata
    //      There are three hints for how frequently the user will be changing the buffer's data.
    //          STATIC: The user will set the data once.
    //          DYNAMIC: The user will set the data occasionally.
    //          STREAM: The user will be changing the data after every use. Or almost every use.

    #region Public: Members

    public int BufferSizeBytes { get { return _itemCount * _format.SizeBytes; } }
    public GPUDataFormat? Format { get { return _format; } private set { _format = value; } }
    public DrawElementsType DrawElementsType { get { return _drawElementsType; } private set { _drawElementsType = value; } }
    public BufferTarget BufferTarget { get { return _bufferTarget; } private set { _bufferTarget = value; } }
    public BufferRangeTarget? RangeTarget { get { return _rangeTarget; } private set { _rangeTarget = value; } }
    public int ItemCount { get { return _itemCount; } }
    public int ItemSizeBytes { get { return _format.SizeBytes; } }
    public bool CopyToGpuCalled { get; private set; }
    public long CopyToGpuCalledFrameId { get; private set; }

    #endregion
    #region Private: Members

    private BufferUsageHint? _usageHint = null;//Whether these flags are set defines whether this is an immutable buffer.
    private BufferStorageFlags? _storageFlags = null;
    private int _itemCount = 0;
    private GPUDataFormat _format;
    private DrawElementsType _drawElementsType = DrawElementsType.UnsignedInt;//only valid for buffertarget=elementarraybuffer
    private BufferTarget _bufferTarget = BufferTarget.ArrayBuffer;
    private BufferRangeTarget? _rangeTarget = null;//For buffer block objects 
    protected bool _allocated = false;

    #endregion
    #region Public: Static Methods

    public static void UnbindBuffer(BufferTarget t)
    {
      GL.BindBuffer(t, 0);
      Gpu.CheckGpuErrorsDbg();
    }

    #endregion
    #region Public: Methods
    //not used rn
    // public GPUBuffer(string name, GPUDataFormat? fmt, BufferTarget t, int item_size_bytes, int itemCount, BufferStorageFlags flags, object? items = null) : base(name)
    // {
    //   _storageFlags = flags;
    // }
    public GPUBuffer(string name, GPUDataFormat fmt, int itemCount, BufferTarget t, BufferUsageHint hint) : base(name)
    {
      //, int item_size_bytes, int itemCount, 
      Init(fmt, itemCount, t, hint);
    }

    protected override string DataPathName() { return "-buf" + base.DataPathName(); }

    private void Init(GPUDataFormat fmt, int count, BufferTarget t, BufferUsageHint hint)
    {
      //Gu.Assert(itemCount > 0, $"{Name}: Count was zero.");
      Gu.Assert(fmt != null);
      
      _bufferTarget = t;
      _format = fmt;
      _usageHint = hint;
      _itemCount = count;

      if (_bufferTarget == BufferTarget.UniformBuffer) { RangeTarget = BufferRangeTarget.UniformBuffer; }
      else if (_bufferTarget == BufferTarget.ShaderStorageBuffer) { RangeTarget = BufferRangeTarget.ShaderStorageBuffer; }
      else if (_bufferTarget == BufferTarget.ArrayBuffer) { RangeTarget = null; }
      else if (_bufferTarget == BufferTarget.ElementArrayBuffer) { RangeTarget = null; }
      else { Gu.BRThrowNotImplementedException(); }

      _glId = GT.GenBuffer();
      Gpu.CheckGpuErrorsDbg();

      Allocate(_format.SizeBytes * count);
      SetObjectLabel();

      if (t == BufferTarget.ElementArrayBuffer)
      {
        Gu.Assert(fmt != null);
        if (fmt.SizeBytes == 1)
        {
          DrawElementsType = DrawElementsType.UnsignedByte;
        }
        else if (fmt.SizeBytes == 2)
        {
          DrawElementsType = DrawElementsType.UnsignedShort;
        }
        else if (fmt.SizeBytes == 4)
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
        GT.DeleteBuffer(_glId);
      }
    }
    public GPUBuffer Clone()
    {
      var b = (GPUBuffer)this.MemberwiseClone();
      var data = this.CopyFromGPU();
      b.CopyToGPU(data);
      return b;
    }
    public bool Bind()
    {
      // if (!GL.IsBuffer(_glId))
      // {
      //   Gu.DebugBreak();
      //   return false;
      // }
      GL.BindBuffer(BufferTarget, _glId);
      Gpu.CheckGpuErrorsDbg();
      return true;
    }
    public void Unbind()
    {
      GL.BindBuffer(BufferTarget, 0);
      Gpu.CheckGpuErrorsDbg();
    }

    public GpuDataPtr CopyFromGPU(int itemOffset = 0, int itemCount = -1, bool useMemoryBarrier = false)
    {
      Gu.Assert(this._allocated);
      //Copies GPU data into a temporary byte array.
      int offsetBytes = itemOffset * _format.SizeBytes;
      int lengthBytes = (itemCount <= -1) ? (_itemCount * _format.SizeBytes) : ((int)itemCount * _format.SizeBytes);

      if (useMemoryBarrier || this.BufferTarget == BufferTarget.ShaderStorageBuffer)//SSBOs reads and writes use incoherent memory accesses, so they need the appropriate barriers
      {
        MemoryBarrier();
      }

      Bind();
      IntPtr pt = GL.MapBufferRange(BufferTarget, (IntPtr)offsetBytes, (IntPtr)lengthBytes, BufferAccessMask.MapReadBit);
      Gpu.CheckGpuErrorsDbg();
      if (pt == IntPtr.Zero)
      {
        Gu.BRThrowException("Failed to map OpenGL Buffer.");
      }
      byte[] managedArray = new byte[lengthBytes];
      Marshal.Copy(pt, managedArray, 0, (int)lengthBytes);
      GL.UnmapBuffer(BufferTarget);
      GpuDataPtr ret = new GpuDataPtr(_format.SizeBytes, _itemCount, managedArray);
      Unbind();

      return ret;
    }
    public void ExpandCopy<T>(GrowList<T> items)
    {
      //expand the size of this VBO to input data, if it is greater, and copy the input data to the VBO starting at the first index
      Gu.Assert(items != null);
      ExpandBuffer(items.Count);
      CopyToGPU(GpuDataPtr.GetGpuDataPtr(items.ToArray()));
    }
    public void CopyToGPU<T>(T[] items, bool useMemoryBarrier = false) where T : struct
    {
      var t_size = GetStructSize<T>();
      var handle = GCHandle.Alloc(items, GCHandleType.Pinned);
      CopyToGPURaw(handle.AddrOfPinnedObject(), 0, 0, t_size * items.Length, useMemoryBarrier);
      handle.Free();
    }
    public void CopyToGPU<T>(T item, bool useMemoryBarrier = false) where T : struct
    {
      var t_size = GetStructSize<T>();
      var ptr = Marshal.AllocHGlobal(t_size);
      Marshal.StructureToPtr(item, ptr, true);
      CopyToGPURaw(ptr, 0, 0, t_size, useMemoryBarrier);
      Marshal.FreeHGlobal(ptr);
    }
    public bool CopyToGPU(GpuDataPtr src, int srcOff = 0, int dstOff = 0, int item_count = -1, bool useMemoryBarrier = false)
    {
      bool ret = false;
      if (src.Count == 0)
      {
        return false;
      }
      if (item_count == -1)
      {
        item_count = src.Count;
      }
      Gu.Assert(item_count >= -1, $"{Name}, Invalid Count.");
      Gu.Assert(src.ItemSizeBytes == ItemSizeBytes, $"{Name}, Item (vertex) sizes did not match");

      int countBytes = item_count * _format.SizeBytes;
      int srcOffBytes = srcOff * _format.SizeBytes;
      int dstOffBytes = dstOff * _format.SizeBytes;

      IntPtr psrc = src.Lock();
      ret = CopyToGPURaw(psrc, srcOffBytes, dstOffBytes, countBytes, useMemoryBarrier);
      src.Unlock();
      return ret;
    }
    public bool CopyToGPURaw(IntPtr psrc, int srcOff_bytes, int dstOff_bytes, int count_bytes, bool useMemoryBarrier)
    {
      Gu.Assert(_allocated);

      //Return true if the copy operation took place.
      bool ret = true;
      Gu.Assert(psrc != null, $"{Name}, Null source.");

      if (count_bytes == 0)
      {
        return false;
      }
      Gu.Assert(count_bytes > 0, $"{Name}, Invalid Count.");

      if (useMemoryBarrier || this.BufferTarget == BufferTarget.ShaderStorageBuffer)//SSBOs reads and writes use incoherent memory accesses, so they need the appropriate barriers
      {
        MemoryBarrier();
      }
      Gu.Assert(dstOff_bytes + count_bytes <= BufferSizeBytes, $"{Name} - Buffer overflow.");

      Bind();
      {
        IntPtr pdst = GL.MapBufferRange(BufferTarget, (IntPtr)dstOff_bytes, (IntPtr)count_bytes, BufferAccessMask.MapWriteBit);
        Gpu.CheckGpuErrorsDbg();
        {
          if (pdst == IntPtr.Zero)
          {
            Gu.Log.Error("Failed to map OpenGL Buffer.");
            Gu.DebugBreak();
            ret = false;
          }
          else
          {
            unsafe
            {
              System.Buffer.MemoryCopy((void*)psrc, (void*)pdst, this.BufferSizeBytes, count_bytes);
              CopyToGpuCalled = true;
              CopyToGpuCalledFrameId = Gu.Context.FrameStamp;
              ret = true;
            }
          }
        }
        GL.UnmapBuffer(BufferTarget);
        Gpu.CheckGpuErrorsDbg();
      }
      Unbind();

      return ret;
    }
    public int ExpandBuffer(int new_item_count, int maxsize_bytes = Int32.MaxValue)
    {
      //return bytes the buffer was expanded.
      if (new_item_count == 0)
      {
        return 0;
      }

      Gu.Assert(new_item_count >= 0, $"{Name}, Invalid Count.");
      int countBytes = new_item_count * _format.SizeBytes;
      int overflow = Math.Max(countBytes - BufferSizeBytes, 0);

      Gu.Assert(countBytes < maxsize_bytes);

      if (overflow > 0)
      {
        Allocate(countBytes);
        _itemCount = new_item_count;
      }
      else
      {
        overflow = 0;
      }

      return overflow;
    }
    #endregion
    #region Private: Methods

    protected void MemoryBarrier()
    {
      MemoryBarrierFlags f = MemoryBarrierFlags.UniformBarrierBit;

      if (BufferTarget == BufferTarget.UniformBuffer)
      {
        f = MemoryBarrierFlags.UniformBarrierBit;
      }
      else if (BufferTarget == BufferTarget.ShaderStorageBuffer)
      {
        f = MemoryBarrierFlags.ShaderStorageBarrierBit;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      GL.MemoryBarrier(f);
    }
    protected void Allocate(object items, int item_count)
    {
      Gu.Assert(items.GetType().IsValueType);
      Gu.Assert(items.GetType().IsArray);

      _itemCount = item_count;

      var pinnedHandle = GCHandle.Alloc(items, GCHandleType.Pinned);
      Allocate(pinnedHandle.AddrOfPinnedObject(), _itemCount * _format.SizeBytes);
      pinnedHandle.Free();
    }
    protected void Allocate(int count_bytes)
    {
      Allocate(IntPtr.Zero, count_bytes);
    }
    private void Allocate(IntPtr pt, int count_bytes)
    {
      if (BufferTarget == BufferTarget.ShaderStorageBuffer)//SSBOs reads and writes use incoherent memory accesses, so they need the appropriate barriers
      {
        MemoryBarrier();
      }

      Bind();
      {
        Gu.Assert(!(_usageHint != null && _storageFlags != null));
        if (_usageHint != null)
        {
          GL.BufferData(BufferTarget, count_bytes, pt, _usageHint.Value);
        }
        else if (_storageFlags != null)
        {
          //this is for that new immutable buffer thingy but not being used rn
          if (_allocated == true)
          {
            Gu.Log.Warn("Reallocated an immutable buffer. Buffer should be set to Immutable, with a storage flags.");
            Gu.DebugBreak();
          }

          GL.BufferStorage(BufferTarget, count_bytes, pt, _storageFlags.Value);
        }
        else { Gu.BRThrowNotImplementedException(); }

        _allocated = true;

      }
      Unbind();
    }
    private int GetStructSize<T>() where T : struct
    {
      int item_size = Marshal.SizeOf(typeof(T));//default(T) 
      return item_size;
    }

    #endregion

  }//gpubuffer


}//NS