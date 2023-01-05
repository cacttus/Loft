﻿using OpenTK.Graphics.OpenGL4;

using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using System.Text;

namespace Loft
{
  public enum GPUVendor
  {
    Undefined, ATI, NVIDIA, INTEL
  }

  public class Gpu
  {
    private Dictionary<WindowContext, List<Action<WindowContext>>> RenderThreadActions = new Dictionary<WindowContext, List<Action<WindowContext>>>();

    //Limits
    public int MaxTextureSize { get; private set; } = 0;
    public int MaxFragmentTextureImageUnits { get; private set; } = 0;
    public int MaxVertexTextureImageUnits { get; private set; } = 0;
    public int[] MaxWorkGroupDims { get; private set; } = new int[3] { 0, 0, 0 };

    public int RenderThreadID { get; private set; } = -1;

    public GPUVendor Vendor = GPUVendor.Undefined;
    public string VendorString = "";

    //control GPU state with minimal enable switching
    private bool _cullFaceLast = false;
    private CullFaceMode _cullFaceModeLast = CullFaceMode.Back;
    private FrontFaceDirection _frontFaceDirectionLast = FrontFaceDirection.Ccw;
    private bool _scissorTestLast = false;
    private bool _depthTestLast = false;
    private bool _blendLast = false;
    private bool _depthMaskLast = true;
    private BlendEquationMode _blendFuncLast = BlendEquationMode.FuncAdd;
    private BlendingFactor _blendFactorLast = BlendingFactor.OneMinusSrcAlpha;

    public Gpu()
    {
      //Initializes gpu info
      RenderThreadID = Thread.CurrentThread.ManagedThreadId;

      ComputeGPULimitsOpenGL();

      VendorString = GL.GetString​(StringName.Vendor);
      if (VendorString.Contains("ATI")) { Vendor = GPUVendor.ATI; }
      else if (VendorString.Contains("NVIDIA")) { Vendor = GPUVendor.NVIDIA; }
      else if (VendorString.Contains("INTEL") || VendorString.Contains("Intel")) { Vendor = GPUVendor.INTEL; }
      else
      {
        Gu.BRThrowException("Invalid GPU vendor string: " + VendorString);
      }
    }
    private bool testing__ = false;
    public void SetState(GpuRenderState state, bool force = false)
    {
      if (testing__)
      {
        StringBuilder strState = new StringBuilder();
        GpuDebugInfo.DebugGetLegacyViewAndMatrixStack(strState);
        Gu.Log.Info(strState.ToString());
        testing__ = false;
      }
      if (state.CullFace != _cullFaceLast || force)
      {
        if (state.CullFace)
        {
          GL.Enable(EnableCap.CullFace);
        }
        else
        {
          GL.Disable(EnableCap.CullFace);
        }
        _cullFaceLast = state.CullFace;
      }
      if (state.DepthTest != _depthTestLast || force)
      {
        if (state.DepthTest)
        {
          GL.Enable(EnableCap.DepthTest);
        }
        else
        {
          GL.Disable(EnableCap.DepthTest);
        }
        _depthTestLast = state.DepthTest;
      }
      if (state.ScissorTest != _scissorTestLast || force)
      {
        if (state.ScissorTest)
        {
          GL.Enable(EnableCap.ScissorTest);
        }
        else
        {
          GL.Disable(EnableCap.ScissorTest);
        }
        _scissorTestLast = state.ScissorTest;
      }

      if (state.CullFace)
      {
        if (state.CullFaceMode != _cullFaceModeLast || force)
        {
          GL.CullFace(state.CullFaceMode);
          _cullFaceModeLast = state.CullFaceMode;
        }
        if (state.FrontFaceDirection != _frontFaceDirectionLast || force)
        {
          GL.FrontFace(state.FrontFaceDirection);
          _frontFaceDirectionLast = state.FrontFaceDirection;
        }
      }

      if (state.DepthTest)
      {
        if (state.DepthMask != _depthMaskLast || force)
        {
          GL.DepthMask(state.DepthMask);
          _depthMaskLast = state.DepthMask;
        }
      }

      if (state.Blend != _blendLast ||
          state.BlendFactor != _blendFactorLast ||
          state.BlendFunc != _blendFuncLast || force)
      {
        Gu.Assert(Gu.Context != null);
        if (Gu.Context.Renderer != null &&
            Gu.Context.Renderer.CurrentStage != null &&
            Gu.Context.Renderer.CurrentStage.OutputFramebuffer != null)
        {
          if (state.Blend)
          {
            //Blending is now controlled per-framebuffer attachment
            GL.Enable(EnableCap.Blend);
            Gu.Context.Renderer.CurrentStage.OutputFramebuffer.SetBlendParams();

            _blendFactorLast = state.BlendFactor;
            _blendFuncLast = state.BlendFunc;
          }
          else
          {
            GL.Disable(EnableCap.Blend);
          }

          _blendLast = state.Blend;
        }
      }
    }
    private void ComputeGPULimitsOpenGL()
    {

      int[] maxTextureSize = new int[2];
      GL.GetInteger(GetPName.MaxTextureSize, maxTextureSize);
      Gpu.CheckGpuErrorsRt();
      MaxTextureSize = maxTextureSize[0];

      int tmp = 0;
      GL.GetInteger(GetPName.MaxTextureImageUnits, out tmp);
      Gpu.CheckGpuErrorsRt();
      MaxFragmentTextureImageUnits = tmp;

      GL.GetInteger(GetPName.MaxVertexTextureImageUnits, out tmp);
      Gpu.CheckGpuErrorsRt();
      MaxVertexTextureImageUnits = tmp;

      // GL.GetInteger(GetPName.MaxCombinedTextureImageUnits, out tmp);
      // Gpu.CheckGpuErrorsRt();
      // MaxCombinedTextureImageUnits = tmp;

      GL.GetInteger((GetIndexedPName)GLenum.GL_MAX_COMPUTE_WORK_GROUP_COUNT, 0, out MaxWorkGroupDims[0]);
      GL.GetInteger((GetIndexedPName)GLenum.GL_MAX_COMPUTE_WORK_GROUP_COUNT, 1, out MaxWorkGroupDims[1]);
      GL.GetInteger((GetIndexedPName)GLenum.GL_MAX_COMPUTE_WORK_GROUP_COUNT, 2, out MaxWorkGroupDims[2]);
      Gpu.CheckGpuErrorsRt();
      // GL.GetInteger((GetIndexedPName)GLenum.GL_MAX_COMPUTE_WORK_GROUP_COUNT, 0, MaxWorkGroupDims);
      // GL.GetInteger((GetIndexedPName)GLenum.GL_MAX_COMPUTE_WORK_GROUP_COUNT, 0, MaxWorkGroupDims);
    }
    public static TextureUnit GetActiveTexture()
    {
      int tex_unit = 0;
      GL.GetInteger(GetPName.ActiveTexture, out tex_unit);
      return (TextureUnit)tex_unit;
    }
    public static unsafe T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
      //TODO:Duplicate REsourceManager.Serialize / Deserialize is essentially the same thing.
      fixed (byte* ptr = &bytes[0])
      {
        return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
      }
    }
    public void Post_To_RenderThread(WindowContext wc, Action<WindowContext> a)
    {
      //This is super important for disposing Render (opengl) stuff.
      //Posts this operation to a render thread to cleanup OpenGL stuff.
      //This is also for any async call that requires render thread synchronization.
      //These get executed after literally all rendering (right now)
      lock (RenderThreadActions)
      {
        //Register an action to delete GPU memory on the main thread.
        //This is for C# finalizers (called on the GC thread)
        List<Action<WindowContext>> actions = null;
        if (!RenderThreadActions.TryGetValue(wc, out actions))
        {
          RenderThreadActions.Add(wc, new List<Action<WindowContext>> { a });
        }
        else
        {
          actions.Add(a);
        }
      }
    }
    public void ExecuteCallbacks_RenderThread(WindowContext wc)
    {
      List<Action<WindowContext>> actions_cpy = null;
      lock (RenderThreadActions)
      {
        RenderThreadActions.TryGetValue(wc, out actions_cpy);
        RenderThreadActions.Remove(wc);
      }
      if (actions_cpy != null)
      {
        //Call this at the end of render thread (or beginning)
        foreach (var action in actions_cpy)
        {
          action(wc);
        }
        actions_cpy.Clear();
      }
    }
    private static GPULog GPULog = new GPULog();
    public static bool CheckGpuErrorsRt(bool donotbreak = false, bool donotlog = false)
    {
      if (Gu.EngineConfig.EnableRuntimeErrorChecks == true)
      {
        return GPULog.CheckErrors(donotbreak, donotlog);
      }
      return false;
    }
    public static bool CheckGpuErrorsDbg(bool donotbreak = false, bool donotlog = false)
    {
#if DEBUG
      if (Gu.EngineConfig.EnableDebugErrorChecks == true)
      {
        return GPULog.CheckErrors(donotbreak, donotlog);
      }
#endif
      return false;
    }
    public static Image GetTextureDataFromGpu(int iGLTexId, TextureTarget eTexTargetBase, ref PixelFormat outFormat, ref PixelType outType, ref PixelInternalFormat outInternalFormat, int iCubeMapSide = -1)
    {
      //Input image32 must be not nulll
      int iSavedTextureBinding;
      GetPName eTexBinding = texTargetToTexBindingQuery(eTexTargetBase);

      TextureTarget eTexTargetSide = eTexTargetBase;
      if (eTexTargetBase == TextureTarget.TextureCubeMap)
      {
        Gu.Assert(iCubeMapSide >= 0 && iCubeMapSide < 6);
        eTexTargetSide = TextureTarget.TextureCubeMapPositiveX + iCubeMapSide;
      }

      iSavedTextureBinding = GL.GetInteger(eTexBinding);
      Gpu.CheckGpuErrorsRt();

      GL.ActiveTexture(TextureUnit.Texture0);
      GL.BindTexture(eTexTargetBase, iGLTexId);
      Gpu.CheckGpuErrorsRt();

      int w = 0, h = 0, tmp = 0, iMipLevel = 0;
      PixelInternalFormat internalFormat = PixelInternalFormat.Rgba;
      GL.GetTexLevelParameter(eTexTargetSide, iMipLevel, GetTextureParameter.TextureWidth, out w);
      Gpu.CheckGpuErrorsRt();
      GL.GetTexLevelParameter(eTexTargetSide, iMipLevel, GetTextureParameter.TextureHeight, out h);
      Gpu.CheckGpuErrorsRt();
      GL.GetTexLevelParameter(eTexTargetSide, iMipLevel, GetTextureParameter.TextureInternalFormat, out tmp);
      Gpu.CheckGpuErrorsRt();
      internalFormat = (PixelInternalFormat)tmp;

      PixelFormat calculatedFmt = PixelFormat.Rgba;
      PixelType calculatedType = PixelType.UnsignedByte;
      int bufsiz_bytes = 0;

      if (internalFormat == PixelInternalFormat.Rgba)
      {
        calculatedFmt = PixelFormat.Rgba;
        calculatedType = PixelType.UnsignedByte;
        bufsiz_bytes = w * h * 4;
      }
      else if (internalFormat == PixelInternalFormat.Rgba8)
      {
        calculatedFmt = PixelFormat.Rgba;
        calculatedType = PixelType.UnsignedByte;
        bufsiz_bytes = w * h * 4;
      }
      else if (internalFormat == PixelInternalFormat.Rgba16f)
      {
        calculatedFmt = PixelFormat.Rgba;
        calculatedType = PixelType.UnsignedByte;
        bufsiz_bytes = w * h * 4;
      }
      else if (internalFormat == PixelInternalFormat.Rgba32f)
      {  //All color buffers
        calculatedFmt = PixelFormat.Rgba;
        calculatedType = PixelType.UnsignedByte;
        bufsiz_bytes = w * h * 4 * 4;
      }
      else if (internalFormat == PixelInternalFormat.Rgba32ui)
      {  //Pick buffer
        calculatedFmt = PixelFormat.RedInteger;
        calculatedType = PixelType.UnsignedInt;
        bufsiz_bytes = w * h * 4 * 4;
      }
      else if (internalFormat == PixelInternalFormat.R32f)
      {
        /*
        https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glGetTexImage.xhtml
        If the selected texture image does not contain four components, the following mappings are applied.
        Single-component textures are treated as RGBA buffers with red set to the single-component value,
        green set to 0, blue set to 0, and alpha set to 1.
        */
        calculatedFmt = PixelFormat.RedInteger;
        calculatedType = PixelType.Float;// valid ?
        bufsiz_bytes = w * h * 4;
      }
      else if (internalFormat == PixelInternalFormat.R16f)
      {
        calculatedFmt = PixelFormat.RedInteger; // ? Look at r32ui
        calculatedType = PixelType.UnsignedShort; // valid ?
        bufsiz_bytes = w * h * 2;
      }
      else if (internalFormat == PixelInternalFormat.R32ui)
      {
        calculatedFmt = PixelFormat.RedInteger;
        calculatedType = PixelType.UnsignedInt;
        bufsiz_bytes = w * h * 4;//4 for ui? 
      }
      else if (internalFormat == PixelInternalFormat.DepthComponent32f)
      {
        calculatedFmt = PixelFormat.DepthComponent;
        calculatedType = PixelType.Float;
        bufsiz_bytes = w * h * 4;//4 for ui? 
      }
      else if (internalFormat == PixelInternalFormat.DepthComponent24)
      {
        calculatedFmt = PixelFormat.DepthComponent;
        calculatedType = PixelType.Float;
        bufsiz_bytes = w * h * 4;//4 for ui? 
      }
      else if (internalFormat == PixelInternalFormat.DepthComponent16)
      {
        calculatedFmt = PixelFormat.DepthComponent;
        calculatedType = PixelType.Float; ;
        bufsiz_bytes = w * h * 4;//4 for ui? 
      }
      else
      {
        Gu.Log.Error("Invalid or Unsupported texture internal format when reading from GPU" + (int)internalFormat);
        Gu.DebugBreak();
      }

      if (false)
      {
        //Print all image values as floats (tests to see if buffer was written to
        byte[] ts = new byte[w * h * 4];
        int iNonZero = 0;
        byte lastr, lastg, lastb, lasta;
        var handle2 = GCHandle.Alloc(ts, GCHandleType.Pinned);
        GL.GetTexImage(eTexTargetSide, iMipLevel, calculatedFmt, calculatedType, handle2.AddrOfPinnedObject());
        handle2.Free();
        for (int ih = 0; ih < h; ++ih)
        {
          for (int iw = 0; iw < w; ++iw)
          {
            float r = ts[ih * (w * 4) + iw * 4 + 0];
            float g = ts[ih * (w * 4) + iw * 4 + 1];
            float b = ts[ih * (w * 4) + iw * 4 + 2];
            float a = ts[ih * (w * 4) + iw * 4 + 3];
            if (lastr != r || lastg != g || lastb != b || lasta != a)
            {
              Console.Write(" ,(" + r + "," + g + "," + b + "," + a + ")");
              iNonZero++;
            }
            lastr = (byte)r;
            lastg = (byte)g;
            lastb = (byte)b;
            lasta = (byte)a;
          }
        }
        int nnn = 0;
        nnn++;
      }

      // char* buf = new char[bufsiz_bytes];
      // glReadPixels(0, 0, w, h, GL_RGBA, GL_UNSIGNED_BYTE, (GLvoid*)bi.getData()->ptr());
      //glGetTexImage(GL_TEXTURE_2D, iMipLevel, GL_RGBA, GL_UNSIGNED_BYTE, (GLvoid*)bi.getData()->ptr());
      Image image = new Image("GpuTexture", w, h, null, Image.ImagePixelFormat.RGBA32ub);
      var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
      GL.GetTexImage(eTexTargetSide, iMipLevel, calculatedFmt, calculatedType, handle.AddrOfPinnedObject());
      handle.Free();
      Gpu.CheckGpuErrorsRt();

      GL.BindTexture(eTexTargetBase, iSavedTextureBinding);
      Gpu.CheckGpuErrorsRt();

      outFormat = calculatedFmt;
      outType = calculatedType;
      outInternalFormat = internalFormat;

      return image;
    }
    private static GetPName texTargetToTexBindingQuery(TextureTarget target)
    {
      if (target == TextureTarget.Texture1D)
      {
        return GetPName.TextureBinding1D;
      }
      else if (target == TextureTarget.Texture2D)
      {
        return GetPName.TextureBinding2D;
      }
      else if (target == TextureTarget.Texture3D)
      {
        return GetPName.TextureBinding3D;
      }
      else if (target == TextureTarget.TextureRectangle)
      {
        return GetPName.TextureBindingRectangle;
      }
      else if (target == TextureTarget.TextureBuffer)
      {
        return GetPName.TextureBindingBuffer;
      }
      else if (target == TextureTarget.TextureCubeMap)
      {
        return GetPName.TextureBindingCubeMap;
      }
      else if (target == TextureTarget.Texture1DArray)
      {
        return GetPName.TextureBinding1DArray;
      }
      else if (target == TextureTarget.Texture2DArray)
      {
        return GetPName.TextureBinding2DArray;
      }
      else if (target == TextureTarget.TextureCubeMapArray)
      {
        Gu.BRThrowException("OpenTK doesn't have this parameter GL_TEXTURE_BINDING_CUBE_MAP_ARRAY");
        return GetPName.TextureBindingCubeMap;
      }
      else if (target == TextureTarget.Texture2DMultisample)
      {
        return GetPName.TextureBinding2DMultisample;
      }
      else if (target == TextureTarget.Texture2DMultisampleArray)
      {
        return GetPName.TextureBinding2DMultisampleArray;
      }
      return 0;
    }
    public static string GetObjectLabelWithId(ObjectLabelIdentifier idt, int id)
    {
      string label;
      int length;
      if (id == 0)
      {
        return " 0 (none)";
      }
      GL.GetObjectLabel(idt, id, 256, out length, out label);
      label += $" ({id})";
      return label;
    }
    //This is private now, because we have no need for it yet, but we may need it in the future to check against objects.
    private static string GetObjectLabel(ObjectLabelIdentifier idt, int id)
    {
      string label;
      int length;
      if (id == 0)
      {
        return " 0 (none)";
      }
      GL.GetObjectLabel(idt, id, 256, out length, out label);
      return label;
    }
    public static GPUBuffer CreateUniformBuffer<T>(string name, T item) where T : struct
    {
      var b = new GPUBuffer(name + "-ubo", GpuDataFormat.GetDataFormat<T>(), 1, BufferTarget.UniformBuffer, BufferUsageHint.StreamDraw);
      b.CopyToGPU(item);
      return b;
    }
    public static GPUBuffer CreateUniformBuffer<T>(string name, T[] items) where T : struct
    {
      var b = new GPUBuffer(name + "-ubo", GpuDataFormat.GetDataFormat<T>(), items.Length, BufferTarget.UniformBuffer, BufferUsageHint.StreamDraw);
      b.CopyToGPU(items);
      return b;
    }
    public static GPUBuffer CreateShaderStorageBuffer<T>(string name, T[] items) where T : struct
    {
      var b = new GPUBuffer(name + "-ssbo", GpuDataFormat.GetDataFormat<T>(), items.Length, BufferTarget.ShaderStorageBuffer, BufferUsageHint.StreamDraw);
      b.CopyToGPU(items);
      return b;
    }
    // public static GPUBuffer CreateShaderStorageBuffer(string name, int item_size_bytes, int item_count)
    // {
    //   return new GPUBuffer(name + "-ssbo", GPUDataFormat.GetDataFormat<T>(), BufferTarget.ShaderStorageBuffer, item_count, BufferUsageHint.StreamDraw);
    // }
    public static GPUBuffer CreateVertexBuffer<T>(string name, T[] verts) where T : struct
    {
      Gu.Assert(verts != null);
      var b = new GPUBuffer(name + "-vbo", GpuDataFormat.GetDataFormat<T>(), verts.Length, BufferTarget.ArrayBuffer, BufferUsageHint.StreamDraw);
      b.CopyToGPU(verts);
      return b;
    }
    public static GPUBuffer CreateIndexBuffer<T>(string name, T[] inds) where T : struct
    {
      Gu.Assert(inds != null);
      var b = new GPUBuffer(name + "-ibo", GpuDataFormat.GetDataFormat<T>(), inds.Length, BufferTarget.ElementArrayBuffer, BufferUsageHint.StreamDraw);
      b.CopyToGPU(inds);
      return b;
    }
    public class GPUMemInfo
    {
      public int? Free = null;
      public int? Total = null;

      public int? GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX = null;
      public int? GPU_MEMORY_INFO_EVICTION_COUNT_NVX = null;
      public int? GPU_MEMORY_INFO_EVICTED_MEMORY_NVX = null;

      public int? VBO_FREE_MEMORY_ATI = null;
      public int? TEXTURE_FREE_MEMORY_ATI = null;
      public int? RENDERBUFFER_FREE_MEMORY_ATI = null;
      public override string ToString()
      {
        StringBuilder s = new StringBuilder();
        ToString(s, " ");
        return s.ToString();
      }
      public void ToString(StringBuilder s, string tab = "")
      {
        if (this.Free != null) { s.AppendLine($"{tab}Free :{StringUtil.FormatPrec((float)Free / 1024.0f, 1)}MB"); }
        if (this.Total != null) { s.AppendLine($"{tab}Total:{StringUtil.FormatPrec((float)Total / 1024.0f, 1)}MB"); }
        if (this.GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX != null) { s.AppendLine($"{tab}GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX :{StringUtil.FormatPrec((float)GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX / 1024.0f, 1)}MB"); }
        if (this.GPU_MEMORY_INFO_EVICTION_COUNT_NVX != null) { s.AppendLine($"{tab}GPU_MEMORY_INFO_EVICTION_COUNT_NVX :{this.GPU_MEMORY_INFO_EVICTION_COUNT_NVX}"); }
        if (this.GPU_MEMORY_INFO_EVICTED_MEMORY_NVX != null) { s.AppendLine($"{tab}GPU_MEMORY_INFO_EVICTED_MEMORY_NVX :{this.GPU_MEMORY_INFO_EVICTED_MEMORY_NVX}"); }
        if (this.VBO_FREE_MEMORY_ATI != null) { s.AppendLine($"{tab}VBO_FREE_MEMORY_ATI :{this.VBO_FREE_MEMORY_ATI}kB"); }
        if (this.TEXTURE_FREE_MEMORY_ATI != null) { s.AppendLine($"{tab}TEXTURE_FREE_MEMORY_ATI :{this.TEXTURE_FREE_MEMORY_ATI}kB"); }
        if (this.RENDERBUFFER_FREE_MEMORY_ATI != null) { s.AppendLine($"{tab}RENDERBUFFER_FREE_MEMORY_ATI :{this.RENDERBUFFER_FREE_MEMORY_ATI}kB"); }
      }
    }
    public GPUMemInfo GetMemoryInfo()
    {
      Gpu.CheckGpuErrorsRt();
      GPUMemInfo m = new GPUMemInfo();
      if (Vendor == GPUVendor.NVIDIA)
      {
        //NV
        //https://developer.download.nvidia.com/opengl/specs/GL_NVX_gpu_memory_info.txt
        const int GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX = 0x9047;
        const int GPU_MEMORY_INFO_TOTAL_AVAILABLE_MEMORY_NVX = 0x9048;
        const int GPU_MEMORY_INFO_CURRENT_AVAILABLE_VIDMEM_NVX = 0x9049;
        const int GPU_MEMORY_INFO_EVICTION_COUNT_NVX = 0x904A;
        const int GPU_MEMORY_INFO_EVICTED_MEMORY_NVX = 0x904B;

        //All values return kb
        int current = 0, total = 0, dedicated = 0, eviction = 0, evicted = 0;
        GL.GetInteger((GetPName)GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX, out dedicated);
        GL.GetInteger((GetPName)GPU_MEMORY_INFO_TOTAL_AVAILABLE_MEMORY_NVX, out total);
        GL.GetInteger((GetPName)GPU_MEMORY_INFO_CURRENT_AVAILABLE_VIDMEM_NVX, out current);
        GL.GetInteger((GetPName)GPU_MEMORY_INFO_EVICTION_COUNT_NVX, out eviction);
        GL.GetInteger((GetPName)GPU_MEMORY_INFO_EVICTED_MEMORY_NVX, out evicted);
        m.Free = current;
        m.Total = total;
        m.GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX = dedicated;
        m.GPU_MEMORY_INFO_EVICTION_COUNT_NVX = eviction;
        m.GPU_MEMORY_INFO_EVICTED_MEMORY_NVX = evicted;
      }
      else if (Vendor == GPUVendor.ATI)
      {
        //ATI
        //https://registry.khronos.org/OpenGL/extensions/ATI/ATI_meminfo.txt
        //All values return kb, like NV
        //      param[0] - total memory free in the pool
        //        param[1] - largest available free block in the pool
        //        param[2] - total auxiliary memory free
        //        param[3] - largest auxiliary free block

        const int VBO_FREE_MEMORY_ATI = 0x87FB;
        const int TEXTURE_FREE_MEMORY_ATI = 0x87FC;
        const int RENDERBUFFER_FREE_MEMORY_ATI = 0x87FD;
        int[] vbo = new int[4];
        int[] texture = new int[4];
        int[] renderbuffer = new int[4];
        GL.GetInteger((GetPName)VBO_FREE_MEMORY_ATI, vbo);
        GL.GetInteger((GetPName)TEXTURE_FREE_MEMORY_ATI, texture);
        GL.GetInteger((GetPName)RENDERBUFFER_FREE_MEMORY_ATI, renderbuffer);
        m.Free = vbo[0] + texture[0] + renderbuffer[0];
        m.Total = m.Free;
        m.VBO_FREE_MEMORY_ATI = vbo[0];
        m.TEXTURE_FREE_MEMORY_ATI = texture[0];
        m.RENDERBUFFER_FREE_MEMORY_ATI = renderbuffer[0];
      }
      else
      {
        Gu.Log.Error($"Vendor {Vendor.ToString()} not supported for memoryinfo()");
      }
      Gpu.CheckGpuErrorsRt();

      return m;
    }

  }//Gpu

  public class GpuDebugInfo
  {
    private static bool _bGettingRenderState = false;
    public static string DebugGetRenderState(bool bForceRun = false, bool bPrintToStdout = true, bool bSaveFramebufferTexture = false) //DebugGetGpuState
    {
      // This method is called in frames to drag down the debug arrow
      //  and we skip it unless we force it to run.
      // Do not comment
      if (!bForceRun)
      {
        return "";  // Do not comment
      }
      System.Text.StringBuilder strState = new StringBuilder();

      if (_bGettingRenderState == true)
      {
        return "Render State tried to be called recursively.";  // Prevent recursive calls.
      }
      _bGettingRenderState = true;

      // Gd::verifyRenderThread();//We must be in render thread

      var ct = Gu.Context;
      if (ct == null)
      {
        Gu.Log.Error("Context was null for DebugGetRenderState");
        return "";
      }

      strState.AppendLine($"");
      strState.AppendLine($"==============================================");
      strState.AppendLine($"=                RENDER STATE                =");
      strState.AppendLine($"==============================================");
      Gpu.CheckGpuErrorsRt();

      DebugPrintShaderLimitsAndState(strState);

      DebugGetLegacyViewAndMatrixStack(strState);
      Gpu.CheckGpuErrorsRt();
      DebugGetBufferState(strState);
      Gpu.CheckGpuErrorsRt();
      // debugGetAttribState(); // This is redundant with vertexarraystate
      //     CheckGpuErrorsDbg();
      DebugGetTextureState(strState);
      Gpu.CheckGpuErrorsRt();

      DebugGetVertexArrayState(strState);
      Gpu.CheckGpuErrorsRt();
      DebugGetFramebufferAttachmentState(strState);
      Gpu.CheckGpuErrorsRt();

      if (bPrintToStdout)
      {
        Gu.Log.Info(strState.ToString());
      }
      // if (bSaveFramebufferTexture) {
      //   string fname = FileSystem::getScreenshotFilename();
      //   saveFramebufferAsPng(std::move(fname));
      // }

      _bGettingRenderState = false;

      return strState.ToString();
    }

    #region Private

    private static void DebugPrintGLGetInteger(StringBuilder strState, GetPName pname)
    {
      //deosnt work in some cases due to dupes
      int val = 0;
      GL.GetInteger(GetPName.Blend, out val);
      strState.AppendLine(((GLenum)pname).Description() + ": " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
    }
    private static string EnabledString(int val)
    {
      return ((val > 0) ? ("Enabled") : ("Disabled"));
    }
    public static void DebugGetLegacyViewAndMatrixStack(StringBuilder strState)
    {
      int[] iScissorBox = new int[4];
      int[] iViewportBox = new int[4];
      strState.AppendLine("---------------- Legcay State ----------------");

      int val = 0;
      GL.GetInteger(GetPName.Blend, out val);
      strState.AppendLine("Blending: " + EnabledString(val));
      Gpu.CheckGpuErrorsRt();

      GL.GetInteger(GetPName.CullFace, out val);
      strState.AppendLine("Culling: " + EnabledString(val));
      Gpu.CheckGpuErrorsRt();

      GL.GetInteger(GetPName.CullFaceMode, out val);
      strState.AppendLine("CullMode: " + ((CullFaceMode)val).ToString());
      Gpu.CheckGpuErrorsRt();

      GL.GetInteger(GetPName.FrontFace, out val);
      strState.AppendLine("FrontFace: " + ((FrontFaceDirection)val).ToString());
      Gpu.CheckGpuErrorsRt();

      GL.GetInteger(GetPName.DepthTest, out val);
      strState.AppendLine("Depth Test: " + EnabledString(val));
      Gpu.CheckGpuErrorsRt();

      GL.GetInteger(GetPName.ScissorTest, out val);
      strState.AppendLine("Scissor Test: " + EnabledString(val));
      Gpu.CheckGpuErrorsRt();

      // View Bounds (Legacy)
      GL.GetInteger(GetPName.ScissorBox, iScissorBox);
      GL.GetInteger(GetPName.Viewport, iViewportBox);
      //GL.GetInteger(GL_SCISSOR_BOX, (int*)iScissorBox);
      //GL.GetInteger(GL_VIEWPORT, (int*)iViewportBox);
      strState.AppendLine("Scissor Box (GL 0,0=bl): " + iScissorBox[0] + "," + iScissorBox[1] + "," + iScissorBox[2] + "," + iScissorBox[3]);
      strState.AppendLine("Viewport Box (GL 0,0=bl): " + iViewportBox[0] + "," + iViewportBox[1] + "," + iViewportBox[2] + "," + iViewportBox[3]);

      GL.GetInteger(GetPName.MaxViewportDims, iViewportBox);
      strState.AppendLine("Viewport max dims: " + iViewportBox[0] + "," + iViewportBox[1]);


      // TODO: legacy matrix array state.
      Gpu.CheckGpuErrorsRt();
    }
    private static void DebugGetBufferState(StringBuilder strState)
    {
      strState.AppendLine("----------------SHADER STATE----------------");

      int iBoundBuffer;
      int iCurrentProgram;
      int iElementArrayBufferBinding;
      int iSsboBinding;  // shader storage
      int iVertexArrayBinding;

      GL.GetInteger(GetPName.ArrayBufferBinding, out iBoundBuffer);
      GL.GetInteger(GetPName.ElementArrayBufferBinding, out iElementArrayBufferBinding);
      //GL.GetInteger(GetPName.binding out iSsboBinding);
      GL.GetInteger(GetPName.VertexArrayBinding, out iVertexArrayBinding);
      GL.GetInteger(GetPName.CurrentProgram, out iCurrentProgram);
      Gpu.CheckGpuErrorsRt();

      strState.AppendLine("Bound Shader Program: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Program, iCurrentProgram));
      Gpu.CheckGpuErrorsRt();
      strState.AppendLine("Bound Vertex Array Buffer (VBO): " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Buffer, iBoundBuffer));
      Gpu.CheckGpuErrorsRt();
      strState.AppendLine("Bound Element Array Buffer (IBO): " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Buffer, iElementArrayBufferBinding));
      Gpu.CheckGpuErrorsRt();
      strState.AppendLine("Bound Shader Storage Buffer (SSBO): Not avialable in opentk?");
      // List<int> binds = new List<int>();
      // int iMaxUniformBindings;
      // GL.GetInteger(GetPName.MaxUniformBufferBindings, out iMaxUniformBindings);
      // for (int xxx = 0; xxx < iMaxUniformBindings; xxx++)
      // {
      //   int iUniformBufferBindingxx = 0;
      //   GL.GetInteger(GetIndexedPName.UniformBufferBinding, xxx, out iUniformBufferBindingxx);
      //   strState.AppendLine("Bound Uniform Buffer (UBO): " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Buffer, iUniformBufferBindingxx));
      // }
      Gpu.CheckGpuErrorsRt();
      strState.AppendLine("Bound Vertex Array Object (VAO): " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.VertexArray, iVertexArrayBinding));
      Gpu.CheckGpuErrorsRt();

      if (iCurrentProgram > 0)
      {
        DebugPrintActiveUniforms(iCurrentProgram, strState);
      }
    }
    private static void DebugPrintActiveUniforms(int iGlProgramId, StringBuilder strState)
    {
      int nUniforms;
      string uniformName;
      int name_len = -1;
      int iArraySize = -1;
      ActiveUniformType uniformType;
      int nActiveUniformBlocks;
      int nMaxUniformLocations;


      // - Get the number of uniforms
      GL.GetProgram(iGlProgramId, GetProgramParameterName.ActiveUniforms, out nUniforms);
      GL.GetProgram(iGlProgramId, GetProgramParameterName.ActiveUniformBlocks, out nActiveUniformBlocks);
      // GL.GetInteger(max uniform locations.., ref nMaxUniformLocations);

      //GL.GetInteger(GL_MAX_COMPUTE_UNIFORM_COMPONENTS, ref nMaxComponentsComp);
      Gpu.CheckGpuErrorsRt();

      strState.AppendLine("Active Uniform Blocks: " + nActiveUniformBlocks);

      strState.AppendLine("Active Uniforms (" + nUniforms + "): ");
      strState.AppendLine("  (Name, Type, Location, ArraySize)");

      // Get all uniform names and types into a list.
      for (Int32 i = 0; i < nUniforms; ++i)
      {
        // Get name an d type
        GL.GetActiveUniform(iGlProgramId, i, 256, out name_len, out iArraySize, out uniformType, out uniformName);

        // get location
        int glLocation = GL.GetUniformLocation(iGlProgramId, uniformName);

        strState.AppendLine(" " + uniformName + ", " + ((GLenum)uniformType).Description() + ", " + glLocation + ", " + iArraySize);

        // Uniform Block Data.
        Gpu.CheckGpuErrorsRt();

        int iCurrentBlockIdx;
        iCurrentBlockIdx = GL.GetUniformBlockIndex(iGlProgramId, uniformName);

        if (iCurrentBlockIdx != -1)
        {
          int iBlockBinding;
          int iBlockDataSize;
          int iBlockNameLength;
          int iBlockActiveUniforms;
          int iBlockActiveUniformIndices;

          Gpu.CheckGpuErrorsRt();
          GL.GetActiveUniformBlock(iGlProgramId, iCurrentBlockIdx, ActiveUniformBlockParameter.UniformBlockBinding, out iBlockBinding);
          Gpu.CheckGpuErrorsRt();
          GL.GetActiveUniformBlock(iGlProgramId, iCurrentBlockIdx, ActiveUniformBlockParameter.UniformBlockDataSize, out iBlockDataSize);
          Gpu.CheckGpuErrorsRt();
          GL.GetActiveUniformBlock(iGlProgramId, iCurrentBlockIdx, ActiveUniformBlockParameter.UniformBlockNameLength, out iBlockNameLength);
          Gpu.CheckGpuErrorsRt();
          GL.GetActiveUniformBlock(iGlProgramId, iCurrentBlockIdx, ActiveUniformBlockParameter.UniformBlockActiveUniforms, out iBlockActiveUniforms);
          Gpu.CheckGpuErrorsRt();
          GL.GetActiveUniformBlock(iGlProgramId, iCurrentBlockIdx, ActiveUniformBlockParameter.UniformBlockActiveUniformIndices, out iBlockActiveUniformIndices);
          Gpu.CheckGpuErrorsRt();

          strState.AppendLine("  Block Index: " + iCurrentBlockIdx);
          strState.AppendLine("  Block Binding: " + iBlockBinding);
          strState.AppendLine("  Block Data Size: " + iBlockDataSize);
          strState.AppendLine("  Block Name Length: " + iBlockNameLength);
          strState.AppendLine("  Block Active Uniforms: " + iBlockActiveUniforms);
          strState.AppendLine("  Block Active Uniform Indices: " + iBlockActiveUniformIndices);
        }

        // strState.AppendLine("  TODO: dump UBO buffer data");
        // Data
        // if (Gu::isManagerConstructed(ManagerType::ShaderMaker))
        // {
        //   // We can call this anywhere. SM is lazy initialized, so this may not be available.
        //   if (Gu::getShaderMaker()->getBound() != nullptr)
        //   {
        //     std::shared_ptr<ShaderUniform> uf = Gu::getShaderMaker()->getBound()->getUniformByName(uniformName);
        //     if (uf != nullptr)
        //     {
        //       strState.AppendLine(("  Buffer Data:"));
        //       if (uf->hasBeenSet() == false)
        //       {
        //         strState.AppendLine(("  not set."));
        //       }
        //       else
        //       {
        //         strState.AppendLine("  Text:" + (uf->debugGetUniformValueAsString(false)));
        //         strState.AppendLine("   Raw:" + (uf->debugGetUniformValueAsString(true)));
        //       }
        //     }
        //     else
        //     {
        //       strState.AppendLine("Uniform " + uniformName + " was not found.  It may be a uniform buffer.");
        //     }
        //   }
        // }
        // else
        // {
        //   strState.AppendLine(" Bound uniform Data not available. Shader manager has not been constructed yet.");
        // }
      }
    }
    private static void DebugGetAttribState(StringBuilder strState)
    {
      //// - print bound attributes
      // int iMaxAttribs;
      // int iBoundAttrib;
      // GL.GetInteger(GL_MAX_VERTEX_ATTRIBS,out iMaxAttribs);
      // std::cout<<"Attribs: max count = "<<iMaxAttribs<<std::endl;
      // for(int xx=0; xx<iMaxAttribs; ++xx)
      //{
      //     GL.GetInteger(GL_VERTEX_ATTRIB_ARRAY0_NV+xx,out iBoundAttrib);
      //     std::cout<<"attrib "<<xx<<": "<<iBoundAttrib<<std::endl;
      // }
    }
    private static void DebugGetTextureState(StringBuilder strState)
    {
      Gpu.CheckGpuErrorsRt();
      strState.AppendLine("----------------Texture State----------------");


      int iActiveTexture;
      GL.GetInteger(GetPName.ActiveTexture, out iActiveTexture);  // 0x84C0 is TEXTURE0
      strState.AppendLine("Active Texture : " + "GL_TEXTURE" + (iActiveTexture - 0x84c0));

      // Get the max id (possibly)
      Gpu.CheckGpuErrorsRt();
      int maxId = 0;
      GT.GenTextures(1, out maxId);
      GT.DeleteTexture(maxId);
      Gpu.CheckGpuErrorsRt();
      int ntexs = 0;
      for (var iTexId = 0; iTexId < maxId; ++iTexId)
      {
        if (GL.IsTexture(iTexId))
        {
          ntexs++;
        }
      }
      strState.AppendLine($"----------------All Textures ({ntexs})----------------");
      // Show all registered texture parameters
      for (var iTexId = 0; iTexId < maxId; ++iTexId)
      {
        DebugPrintTextureInfo(strState, iTexId);
      }

      strState.AppendLine("----------------Bound Textures----------------");

      Gpu.CheckGpuErrorsRt();

      // - Get bound texture units.
      int iMaxVertexTextureUnits;
      GL.GetInteger(GetPName.MaxVertexTextureImageUnits, out iMaxVertexTextureUnits);
      for (int i = 0; i < iMaxVertexTextureUnits; ++i)
      {
        int iTextureId = 0;  // Texture ID
        GL.ActiveTexture(TextureUnit.Texture0 + i);
        strState.AppendLine("  Channel " + i);
        GL.GetInteger(GetPName.TextureBinding1D, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     1D: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding1DArray, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     1D_ARRAY: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding2D, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     2D: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding1DArray, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     2D_ARRAY: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding2DMultisample, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     2D_MULTISAMPLE: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding2DMultisampleArray, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     2D_MULTISAMPLE_ARRAY: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBinding3D, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     3D: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBindingBuffer, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     BUFFER: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBindingCubeMap, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     CUBE_MAP: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        iTextureId = 0;
        GL.GetInteger(GetPName.TextureBindingRectangle, out iTextureId);
        if (iTextureId > 0)
        {
          strState.AppendLine("     RECTANGLE: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTextureId));
        }
        Gpu.CheckGpuErrorsRt();
      }
    }
    private static void DebugPrintTextureInfo(StringBuilder strState, int iTexId)
    {
      if (!GL.IsTexture(iTexId))
      {
        return;
      }
      Gpu.CheckGpuErrorsRt();

      GL.ActiveTexture(TextureUnit.Texture0);

      string texName = Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, iTexId);
      Gpu.CheckGpuErrorsRt();

      int tex_target;
      GL.GetTextureParameter(iTexId, GetTextureParameter.TextureTarget, out tex_target);
      Gpu.CheckGpuErrorsRt();

      if (tex_target == 0)
      {
        strState.AppendLine("  " + texName + " - Texture Target was zero (error).");
      }
      else
      {
        int get_binding = (int)TexTargetToTexBindingQuery((GLenum)tex_target);
        if (get_binding == 0)
        {
          strState.AppendLine("  " + texName + " - Texture Binding information (" + tex_target + ") was invalid.");
        }
        else
        {
          strState.AppendLine($"Texture: {texName} ");
          strState.AppendLine("  Target: " + ((GLenum)tex_target).Description());
          strState.AppendLine("  Binding: " + ((GLenum)tex_target).Description());

          int iSavedTextureId = 0;
          GL.GetInteger((GetPName)get_binding, out iSavedTextureId);
          Gpu.CheckGpuErrorsRt();
          GL.BindTexture((TextureTarget)tex_target, iTexId);
          Gpu.CheckGpuErrorsRt();
          {
            DebugPrintBoundTextureAttribs(strState, texName, tex_target);
          }
          GL.BindTexture((TextureTarget)tex_target, iSavedTextureId);
          Gpu.CheckGpuErrorsRt();
        }
      }
    }
    private static GLenum TexTargetToTexBindingQuery(GLenum target)
    {
      if (target == GLenum.GL_TEXTURE_1D)
      {
        return GLenum.GL_TEXTURE_BINDING_1D;
      }
      else if (target == GLenum.GL_TEXTURE_2D)
      {
        return GLenum.GL_TEXTURE_BINDING_2D;
      }
      else if (target == GLenum.GL_TEXTURE_3D)
      {
        return GLenum.GL_TEXTURE_BINDING_3D;
      }
      else if (target == GLenum.GL_TEXTURE_RECTANGLE)
      {
        return GLenum.GL_TEXTURE_BINDING_RECTANGLE;
      }
      else if (target == GLenum.GL_TEXTURE_BUFFER)
      {
        return GLenum.GL_TEXTURE_BINDING_BUFFER;
      }
      else if (target == GLenum.GL_TEXTURE_CUBE_MAP)
      {
        return GLenum.GL_TEXTURE_BINDING_CUBE_MAP;
      }
      else if (target == GLenum.GL_TEXTURE_1D_ARRAY)
      {
        return GLenum.GL_TEXTURE_BINDING_1D_ARRAY;
      }
      else if (target == GLenum.GL_TEXTURE_2D_ARRAY)
      {
        return GLenum.GL_TEXTURE_BINDING_2D_ARRAY;
      }
      else if (target == GLenum.GL_TEXTURE_CUBE_MAP_ARRAY)
      {
        return GLenum.GL_TEXTURE_BINDING_CUBE_MAP_ARRAY;
      }
      else if (target == GLenum.GL_TEXTURE_2D_MULTISAMPLE)
      {
        return GLenum.GL_TEXTURE_BINDING_2D_MULTISAMPLE;
      }
      else if (target == GLenum.GL_TEXTURE_2D_MULTISAMPLE_ARRAY)
      {
        return GLenum.GL_TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY;
      }
      return (int)0;
    }
    private static GLenum TexBindingToTexTargetQuery(GLenum binding)
    {
      if (binding == GLenum.GL_TEXTURE_BINDING_1D)
      {
        return GLenum.GL_TEXTURE_1D;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_2D)
      {
        return GLenum.GL_TEXTURE_2D;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_3D)
      {
        return GLenum.GL_TEXTURE_3D;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_RECTANGLE)
      {
        return GLenum.GL_TEXTURE_RECTANGLE;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_BUFFER)
      {
        return GLenum.GL_TEXTURE_BUFFER;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_CUBE_MAP)
      {
        return GLenum.GL_TEXTURE_CUBE_MAP;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_1D_ARRAY)
      {
        return GLenum.GL_TEXTURE_1D_ARRAY;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_2D_ARRAY)
      {
        return GLenum.GL_TEXTURE_2D_ARRAY;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_CUBE_MAP_ARRAY)
      {
        return GLenum.GL_TEXTURE_CUBE_MAP_ARRAY;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_2D_MULTISAMPLE)
      {
        return GLenum.GL_TEXTURE_2D_MULTISAMPLE;
      }
      else if (binding == GLenum.GL_TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY)
      {
        return GLenum.GL_TEXTURE_2D_MULTISAMPLE_ARRAY;
      }
      return 0;
    }
    private static void DebugPrintBoundTextureAttribs(StringBuilder strState, string texName, int tex_target)
    {
      int val;
      if (Gu.AllowOpenTKFaults)
      {
        GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureWidth, out val);
        strState.AppendLine("  TextureWidth: " + val);
        Gpu.CheckGpuErrorsRt();
        GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureHeight, out val);
        strState.AppendLine("  TextureHeight: " + val);
        Gpu.CheckGpuErrorsRt();
      }
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureMagFilter, out val);
      strState.AppendLine("  TextureMagFilter: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureMinFilter, out val);
      strState.AppendLine("  TextureMinFilter: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureMinLod, out val);
      strState.AppendLine("  TextureMinLod: " + val);
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureBaseLevel, out val);
      strState.AppendLine("  TextureBaseLevel: " + val);
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureMaxLevel, out val);
      strState.AppendLine("  TextureMaxLevel: " + val);
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureWrapS, out val);
      strState.AppendLine("  TextureWrapS: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureWrapT, out val);
      strState.AppendLine("  TextureWrapT: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureWrapR, out val);
      strState.AppendLine("  TextureWrapR: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureCompareMode, out val);
      strState.AppendLine("  TextureCompareMode: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.TextureCompareFunc, out val);
      strState.AppendLine("  TextureCompareFunc: " + ((GLenum)val).Description());
      Gpu.CheckGpuErrorsRt();
      if (Gu.Context.GameWindow.Profile == OpenTK.Windowing.Common.ContextProfile.Compatability)
      {
        GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.DepthTextureMode, out val);
        strState.AppendLine("  DepthTextureMode: " + ((GLenum)val).Description());
        Gpu.CheckGpuErrorsRt();
        GL.GetTexParameter((TextureTarget)tex_target, GetTextureParameter.GenerateMipmap, out val);
        strState.AppendLine("  GenerateMipmap: " + ((GLenum)val).Description());
        Gpu.CheckGpuErrorsRt();
      }

    }
    private static void DebugGetFramebufferAttachmentState(StringBuilder strState)
    {
      strState.AppendLine("----------------Framebuffers----------------");
      int eDrawBuffer;
      int iDrawFramebufferBinding;  // name of fb beijmg drawn to
      int iReadFramebufferBinding;  // name of fb beijmg drawn to
      int iRenderbufferBinding;
      int eReadBuffer;
      int iSamplerBinding;  //! Texture sampler (should be 2d??)
      int boundFramebuffer;

      // Reference enums
      //#define GL_FRONT 0x0404
      //#define GL_BACK 0x0405

      // Framebuffers
      GL.GetInteger(GetPName.DrawBuffer, out eDrawBuffer);  // 0x08CE0 is the COLOR ATTACHMENT 1, 0x0405 is the default BACK buffer.
      GL.GetInteger(GetPName.ReadBuffer, out eReadBuffer);  // Default: GL_BACK
      GL.GetInteger(GetPName.DrawFramebufferBinding, out iDrawFramebufferBinding);
      GL.GetInteger(GetPName.ReadFramebufferBinding, out iReadFramebufferBinding);
      GL.GetInteger(GetPName.RenderbufferBinding, out iRenderbufferBinding);
      GL.GetInteger(GetPName.SamplerBinding, out iSamplerBinding);
      GL.GetInteger(GetPName.FramebufferBinding, out boundFramebuffer);
      Gpu.CheckGpuErrorsRt();


      //strState.AppendLine(" Max Fragment Texture Image Units: " + maxFragmentTextureImageUnits);
      strState.AppendLine("Current Bound Framebuffer: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Framebuffer, boundFramebuffer));
      strState.AppendLine("Current Draw Framebuffer Binding: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Framebuffer, iDrawFramebufferBinding));
      strState.AppendLine("Current Read Framebuffer Binding: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Framebuffer, iReadFramebufferBinding));
      if (iDrawFramebufferBinding != iReadFramebufferBinding)
      {
        strState.AppendLine("   NOTE: Draw and Read framebuffers are bound different!");
      }
      strState.AppendLine("Current Draw Framebuffer Attachment: " + ((GLenum)eDrawBuffer).Description());
      strState.AppendLine("Current Read Framebuffer Attachment: " + ((GLenum)eReadBuffer).Description());
      strState.AppendLine("Current RenderBuffer Binding: " + iRenderbufferBinding);
      strState.AppendLine("Current Sampler Binding: " + iSamplerBinding);

      if (boundFramebuffer == 0)
      {
        return;
      }


      // Print details about hte bound buffer.
      int maxColorAttachments;
      GL.GetInteger(GetPName.MaxColorAttachments, out maxColorAttachments);
      strState.AppendLine("Current Attachments: (max=" + maxColorAttachments + ")");

      for (int i = 0; i < maxColorAttachments; ++i)
      {
        DebugPrintFBOAttachment(strState, (OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment0 + i), i);
      }
      DebugPrintFBOAttachment(strState, OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment, -1);
      DebugPrintFBOAttachment(strState, OpenTK.Graphics.OpenGL4.FramebufferAttachment.StencilAttachment, -1);
      //DebugPrintFBOAttachment(strState, OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthStencilAttachment);
    }
    private static void DebugPrintFBOAttachment(StringBuilder strState, OpenTK.Graphics.OpenGL4.FramebufferAttachment attachment, int icoloratt)
    {
      int attachmentName = 0;
      int attachmentType = 0;
      int mipmapLevel = 0;

      // string strAttachment = "";

      // if (attachment == OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment)
      // {
      //   strAttachment = ("GL_DEPTH_ATTACHMENT");
      // }
      // else if (attachment == OpenTK.Graphics.OpenGL4.FramebufferAttachment.StencilAttachment)
      // {
      //   strAttachment = ("GL_STENCIL_ATTACHMENT");
      // }
      // else if (attachment >= OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment0 && attachment <= OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment15)
      // {
      //   strAttachment = "GL_COLOR_ATTACHMENT" + (attachment - OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment0);
      // }

      strState.AppendLine("  Attachment: " + ((GLenum)(attachment)).Description());

      GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, attachment, FramebufferParameterName.FramebufferAttachmentObjectType, out attachmentType);
      Gpu.CheckGpuErrorsRt();
      if (attachmentType == 0)//GL_NONE is zero
      {
        strState.AppendLine("    Type: " + "GL_NONE");
      }
      else if (attachmentType == 0x8D41)//GL_RENDERBUFFER
      {
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, attachment, FramebufferParameterName.FramebufferAttachmentObjectName, out attachmentName);
        Gpu.CheckGpuErrorsRt();
        strState.AppendLine("    Type: " + "GL_RENDERBUFFER");
        strState.AppendLine("    Name: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Renderbuffer, attachmentName));
      }
      else if (attachmentType == 0x1702)//GL_TEXTURE
      {
        if (icoloratt >= 0)
        {
          int blend = 0;
          int[] rgb = new int[4];
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_SRC, icoloratt, out blend);
          strState.AppendLine("    BlendSrc: " + ((blend == 0) ? "GL_ZERO" : (blend == 1 ? "GL_ONE" : ((GLenum)blend).Description())));
          Gpu.CheckGpuErrorsRt();
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_DST, icoloratt, out blend);
          strState.AppendLine("    BlendDst: " + ((blend == 0) ? "GL_ZERO" : (blend == 1 ? "GL_ONE" : ((GLenum)blend).Description())));
          Gpu.CheckGpuErrorsRt();
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_SRC_RGB, icoloratt, rgb);
          strState.AppendLine("    BlendSrcRGB: " + (float)rgb[0] + "," + (float)rgb[1] + "," + (float)rgb[2] + "," + (float)rgb[3]);
          Gpu.CheckGpuErrorsRt();
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_DST_RGB, icoloratt, rgb);
          strState.AppendLine("    BlendDstRGB: " + (float)rgb[0] + "," + (float)rgb[1] + "," + (float)rgb[2] + "," + (float)rgb[3]);
          Gpu.CheckGpuErrorsRt();
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_SRC_ALPHA, icoloratt, out blend);
          strState.AppendLine("    BlendSrcAlpha: " + ((blend == 0) ? "GL_ZERO" : (blend == 1 ? "GL_ONE" : ((GLenum)blend).Description())));
          Gpu.CheckGpuErrorsRt();
          GL.GetInteger((GetIndexedPName)GLenum.GL_BLEND_DST_ALPHA, icoloratt, out blend);
          strState.AppendLine("    BlendDstAlpha: " + ((blend == 0) ? "GL_ZERO" : (blend == 1 ? "GL_ONE" : ((GLenum)blend).Description())));
          Gpu.CheckGpuErrorsRt();
        }

        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, attachment, FramebufferParameterName.FramebufferAttachmentObjectName, out attachmentName);
        Gpu.CheckGpuErrorsRt();
        GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, attachment, FramebufferParameterName.FramebufferAttachmentTextureLevel, out mipmapLevel);
        Gpu.CheckGpuErrorsRt();
        strState.AppendLine("    Type: " + "GL_TEXTURE");
        strState.AppendLine("    Name: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Texture, attachmentName));
        strState.AppendLine("    Mipmap Level: " + mipmapLevel);
      }
    }
    private static void DebugGetVertexArrayState(StringBuilder strState)
    {
      strState.AppendLine(("----------------Vertex Array State----------------"));
      int nMaxAttribs;
      int iVertexArrayBinding;
      GL.GetInteger(GetPName.MaxVertexAttribs, out nMaxAttribs);
      GL.GetInteger(GetPName.VertexArrayBinding, out iVertexArrayBinding);

      strState.AppendLine("Bound Vertex Array Id (VAO): " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.VertexArray, iVertexArrayBinding));
      strState.AppendLine("Max Allowed Atribs: " + nMaxAttribs);

      int nact = 0;
      for (int iAttrib = 0; iAttrib < nMaxAttribs; ++iAttrib)
      {
        int aaaa = 0;
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArrayEnabled, out aaaa);
        Gpu.CheckGpuErrorsRt();
        nact++;
      }
      strState.AppendLine("Active Vertex Attribs (" + nact + "): ");

      Gpu.CheckGpuErrorsRt();

      // - Disable all arrays by default.
      for (int iAttrib = 0; iAttrib < nMaxAttribs; ++iAttrib)
      {
        // TODO:
        int iArrayBufferBinding = 0;
        int iArrayEnabled = 0;
        int iAttribArraySize = 0;
        int iAttribArrayType = 0;
        int iAttribArrayStride = 0;
        int iAttribArrayInteger = 0;

        int iAttribArrayNormalized;
        // int iAttribArrayDivisor;
        //memset(fCurAttrib, 0, sizeof(GLfloat) * 4);
        //memset(iCurAttrib, 0, sizeof(int) * 4);
        //memset(uiCurAttrib, 0, sizeof(GLuint) * 4);

        GL.GetVertexAttrib(iAttrib, (VertexAttribParameter)(GLenum.GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING), out iArrayBufferBinding);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArrayEnabled, out iArrayEnabled);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArraySize, out iAttribArraySize);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArrayType, out iAttribArrayType);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArrayStride, out iAttribArrayStride);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.VertexAttribArrayInteger, out iAttribArrayInteger);
        Gpu.CheckGpuErrorsRt();
        GL.GetVertexAttrib(iAttrib, VertexAttribParameter.ArrayNormalized, out iAttribArrayNormalized);
        Gpu.CheckGpuErrorsRt();
        // glGetVertexAttribiv(iAttrib, GL_VERTEX_ATTRIB_ARRAY_DIVISOR, ref iAttribArrayDivisor);
        // CheckGpuErrorsDbg();

        strState.AppendLine("  Attrib " + iAttrib + "  Enabled:" + (iArrayEnabled > 0 ? "Y" : "N"));

        if (iArrayEnabled == 0)
        {
          continue;
        }

        strState.AppendLine("    Array Buffer Binding: " + Gpu.GetObjectLabelWithId(ObjectLabelIdentifier.Buffer, iArrayBufferBinding));
        strState.AppendLine("    Size: " + iAttribArraySize);
        strState.AppendLine("    Stride: " + iAttribArrayStride);
        strState.AppendLine("    Is Integer: " + (iAttribArrayInteger > 0 ? "Y" : "N"));
        strState.AppendLine("    Normalized: " + (iAttribArrayNormalized > 0 ? "Y" : "N"));
        strState.AppendLine("    Type: " + ((GLenum)(iAttribArrayType)).Description());


        // https://registry.khronos.org/OpenGL-Refpages/gl4/html/glGetVertexAttrib.xhtml
        // Generic vertex attribute 0 is unique in that it has no current state,
        // so an error will be generated if index is 0. The initial value for all
        // other generic vertex attributes is (0,0,0,1).
        if (iAttrib != 0)
        {
          // unsafe
          {
            int[] attri = new int[4];
            //int[] attr = new int[4];
            float[] attrf = new float[4];
            double[] attrd = new double[4];
            //We recommend using Span<T> or ReadOnlySpan<T> types to work with stack allocated memory whenever possible. MSDN
            switch (iAttribArrayType)
            {
              case (int)GLenum.GL_INT:
                GL.GetVertexAttrib(iAttrib, (VertexAttribParameter)GLenum.GL_CURRENT_VERTEX_ATTRIB, attri);
                Gpu.CheckGpuErrorsRt();
                strState.AppendLine("    Cur Value(int): " + attri[0] + "," + attri[1] + "," + attri[2] + "," + attri[3]);
                break;
              case (int)GLenum.GL_UNSIGNED_INT:
                //May be wrong. OpenTK doesn't suupport uint specifically
                GL.GetVertexAttrib(iAttrib, (VertexAttribParameter)GLenum.GL_CURRENT_VERTEX_ATTRIB, attri);
                Gpu.CheckGpuErrorsRt();
                strState.AppendLine("    Cur Value(uint): " + (uint)attri[0] + "," + (uint)attri[1] + "," + (uint)attri[2] + "," + (uint)attri[3]);
                break;
              case (int)GLenum.GL_FLOAT:
                GL.GetVertexAttrib(iAttrib, (VertexAttribParameter)GLenum.GL_CURRENT_VERTEX_ATTRIB, attrf);
                Gpu.CheckGpuErrorsRt();
                strState.AppendLine("    Cur Value(float): " + attrf[0] + "," + attrf[1] + "," + attrf[2] + "," + attrf[3]);
                break;
              case (int)GLenum.GL_DOUBLE:
                GL.GetVertexAttrib(iAttrib, (VertexAttribParameter)GLenum.GL_CURRENT_VERTEX_ATTRIB, attrd);
                Gpu.CheckGpuErrorsRt();
                strState.AppendLine("    Cur Value(double): " + attrd[0] + "," + attrd[1] + "," + attrd[2] + "," + attrd[3]);
                break;
              default:
                strState.AppendLine("    Cur Value:  NOT SUPPORTED****** TODO:::: ");
                break;
            };
          }
        }
        // This reads the attrib values such as float, int etc.
        // int iCurrentVertexAttrib;
        // glGetVertexAttribiv(iAttrib, GL_CURRENT_VERTEX_ATTRIB, ref iCurrentVertexAttrib);
      }
    }
    public static void DebugPrintShaderLimitsAndState(StringBuilder strState, string tab = "")
    {
      strState.AppendLine("---------------- Window Info ----------------");
      strState.AppendLine($"{tab}Cur Context: '{Gu.Context.GameWindow.Title.ToString()}'");
      strState.AppendLine($"{tab}  Title: '{Gu.Context.GameWindow.Title.ToString()}'");
      strState.AppendLine($"{tab}  WinDims: {Gu.Context.GameWindow.Width}x{Gu.Context.GameWindow.Height}");
      strState.AppendLine($"{tab}  RenderDims: {(Gu.Context.Renderer.CurrentStage != null ? Gu.Context.Renderer.CurrentStageFBOSize.ToString() : "Not Rendering")}");
      //strState.AppendLine($"Screen Dims: {Gu.Context.GameWindow.monitor}x{Gu.Context.GameWindow.Height}");
      //strState.AppendLine($"This API: {Gu.Context.GameWindow.API.ToString()}");
      strState.AppendLine($"{tab}  GL Profile: {Gu.Context.GameWindow.Profile.ToString()}");
      strState.AppendLine($"{tab}  GL Version: {Gu.Context.GameWindow.APIVersion.ToString()}");
      strState.AppendLine($"{tab}All Windows:");
      foreach (var c in Gu.Contexts)
      {
        strState.AppendLine($"  Title: '{c.Key.Title}'  Context is Current: {c.Key.Context.IsCurrent}");
      }
      strState.AppendLine("---------------- Gpu Info ----------------");
      strState.AppendLine($"{tab}GPU: {GL.GetString​(StringName.Renderer)}");
      strState.AppendLine($"{tab}Vendor: {GL.GetString​(StringName.Vendor)}");
      strState.AppendLine($"{tab}Supported GL: {GL.GetString​(StringName.Version)}");
      strState.AppendLine($"{tab}Supported GLSL: {GL.GetString​(StringName.ShadingLanguageVersion)}");

      Gpu.CheckGpuErrorsRt();

      List<GetPName> geti32 = new List<GetPName>()
      {
        (GetPName)GLenum.GL_MAX_TEXTURE_SIZE,
        (GetPName)GLenum.GL_MAX_IMAGE_UNITS,
        (GetPName)GLenum.GL_MAX_IMAGE_SAMPLES,
        (GetPName)GLenum.GL_MAX_COLOR_ATTACHMENTS,
        (GetPName)GLenum.GL_MAX_DRAW_BUFFERS,
          (GetPName)0,

        (GetPName)GLenum.GL_MAX_TEXTURE_SIZE,
        (GetPName)GLenum.GL_MAX_TEXTURE_IMAGE_UNITS,
        (GetPName)GLenum.GL_MAX_COMBINED_TEXTURE_IMAGE_UNITS,
        (GetPName)GLenum.GL_MAX_ARRAY_TEXTURE_LAYERS,
          (GetPName)0,

        (GetPName)GLenum.GL_MAX_UNIFORM_LOCATIONS,
        (GetPName)GLenum.GL_MAX_UNIFORM_BLOCK_SIZE,
        (GetPName)GLenum.GL_MAX_UNIFORM_BUFFER_BINDINGS,
        (GetPName)GLenum.GL_MAX_COMBINED_UNIFORM_BLOCKS,
        (GetPName)GLenum.GL_MAX_COMBINED_IMAGE_UNIFORMS,
        (GetPName)GLenum.GL_MAX_COMBINED_SHADER_STORAGE_BLOCKS,
        (GetPName)GLenum.GL_MAX_SHADER_STORAGE_BLOCK_SIZE,
        (GetPName)GLenum.GL_MAX_SHADER_STORAGE_BUFFER_BINDINGS,
          (GetPName)0,

        (GetPName)GLenum.GL_MAX_VERTEX_IMAGE_UNIFORMS,
        (GetPName)GLenum.GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS,
        (GetPName)GLenum.GL_MAX_VERTEX_SHADER_STORAGE_BLOCKS,
        (GetPName)GLenum.GL_MAX_VERTEX_UNIFORM_COMPONENTS,
        (GetPName)GLenum.GL_MAX_VERTEX_UNIFORM_BLOCKS,
        (GetPName)GLenum.GL_MAX_VERTEX_UNIFORM_VECTORS,
        (GetPName)GLenum.GL_MAX_COMBINED_VERTEX_UNIFORM_COMPONENTS,
          (GetPName)0,

        (GetPName)GLenum.GL_MAX_GEOMETRY_IMAGE_UNIFORMS,
        (GetPName)GLenum.GL_MAX_GEOMETRY_TEXTURE_IMAGE_UNITS,
        (GetPName)GLenum.GL_MAX_GEOMETRY_SHADER_STORAGE_BLOCKS,
        (GetPName)GLenum.GL_MAX_GEOMETRY_UNIFORM_COMPONENTS,
        (GetPName)GLenum.GL_MAX_GEOMETRY_UNIFORM_BLOCKS,
        (GetPName)GLenum.GL_MAX_COMBINED_GEOMETRY_UNIFORM_COMPONENTS,
          (GetPName)0,

        (GetPName)GLenum.GL_MAX_FRAGMENT_IMAGE_UNIFORMS,
        (GetPName)GLenum.GL_MAX_TEXTURE_IMAGE_UNITS,
        (GetPName)GLenum.GL_MAX_FRAGMENT_SHADER_STORAGE_BLOCKS,
        (GetPName)GLenum.GL_MAX_FRAGMENT_UNIFORM_COMPONENTS,
        (GetPName)GLenum.GL_MAX_FRAGMENT_UNIFORM_BLOCKS,
        (GetPName)GLenum.GL_MAX_FRAGMENT_UNIFORM_VECTORS,
        (GetPName)GLenum.GL_MAX_COMBINED_FRAGMENT_UNIFORM_COMPONENTS,
          (GetPName)0,

        (GetPName)GLenum.GL_MAX_COMPUTE_IMAGE_UNIFORMS,
        (GetPName)GLenum.GL_MAX_COMPUTE_TEXTURE_IMAGE_UNITS,
        (GetPName)GLenum.GL_MAX_COMPUTE_SHADER_STORAGE_BLOCKS,
        (GetPName)GLenum.GL_MAX_COMPUTE_UNIFORM_COMPONENTS,
        (GetPName)GLenum.GL_MAX_COMPUTE_UNIFORM_BLOCKS,
        (GetPName)GLenum.GL_MAX_COMBINED_COMPUTE_UNIFORM_COMPONENTS,
          (GetPName)0,

      };
      foreach (var get in geti32)
      {
        if ((int)get == 0)
        {
          strState.AppendLine("");
        }
        else
        {
          strState.AppendLine($"{tab}{((GLenum)get).ToString()}: {GL.GetInteger(get)}");
        }
      }

      Gpu.CheckGpuErrorsRt();
    }

    #endregion

  }//GpuRenderSTate

  public class GpuComputeSync : HasGpuResources
  {
    /*
     Stores information pertaining to the dispatching of computes to the compute shader system
    Usage
    For Gpu
        call createFence()
        check isComputeComplete()==true

    for Cpu
        call createCpuFence();
        when done call signalCpu()
        check isComputeComplete()==true
    */
    private IntPtr _glSyncObject = IntPtr.Zero;
    private bool _bCpuDispatched = false;
    private bool _bGpuDispatched = false;
    private bool _bCpuSignaled = false;
    private bool isDispatched() { return (_bCpuDispatched || _bGpuDispatched); }
    private bool isCpuDispatched() { return _bCpuDispatched; }

    public GpuComputeSync() { }
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsSync(_glSyncObject))
      {
        GT.DeleteSync(_glSyncObject);
      }
    }
    public void CreateCpuFence()
    {
      if (_bGpuDispatched)
      {
        Gu.BRThrowException("Signal object tried to create fence on cpu when gpu was signaled.");
      }
      _bGpuDispatched = false;
      _bCpuDispatched = true;
      _bCpuSignaled = false;
    }
    public void SignalCpu()
    {
      _bCpuSignaled = true;
    }
    public void CreateFence()
    {
      if (_bCpuDispatched)
      {
        Gu.BRThrowException("Signal object tried to create fence on gpu when cpu was signaled.");
      }
      _bGpuDispatched = true;
      _bCpuDispatched = false;
      _glSyncObject = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, 0);
    }
    public bool IsComputeComplete()
    {
      /**
      *    @fn isComputeComplete
      *    @brief Returns true if the given operation is complete.
      */
      GLenum result = GLenum.GL_UNSIGNALED;
      if (_bCpuDispatched)
      {
        if (_bCpuSignaled)
        {
          result = GLenum.GL_SIGNALED;
        }
      }
      else
      {
        if (GL.IsSync(_glSyncObject))
        {
          int length = 0;
          int res_int = 0;
          result = (GLenum)res_int;
          GL.GetSync(_glSyncObject, SyncParameterName.SyncStatus, sizeof(int), out length, out res_int);
          //This was causing memory leak
          if (result == GLenum.GL_SIGNALED)
          {
            GT.DeleteSync(_glSyncObject);
          }
        }
        else
        {
          result = GLenum.GL_SIGNALED;
        }
      }
      if (result == GLenum.GL_SIGNALED)
      {
        _bCpuDispatched = false;
        _bGpuDispatched = false;
      }
      return result == GLenum.GL_SIGNALED;
    }
  }

  public class GpuDataPtr
  {
    //these could be one class
    //Sizing information from GPU data, and object pointer destined for gpu
    private bool _locked = false;
    private GCHandle pinnedArray;
    private object _pt;

    public int ItemSizeBytes { get; private set; } = 0;
    public int Count { get; private set; } = 0;
    public object Data { get { return _pt; } }

    public static GpuDataPtr GetGpuDataPtr<T>(T[] data)
    {
      //takes an array of vertexes and marshals them for copying to the GPU
      GpuDataPtr p = null;
      if (data.Length == 0)
      {
        p = new GpuDataPtr(0, data.Length, data);
      }
      else
      {
        var size = Marshal.SizeOf(data[0]);
        p = new GpuDataPtr(size, data.Length, data);
      }
      return p;
    }

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

  [DataContract]
  public class GpuRenderState
  {
    //Stores rendering state flags for GPU prior to drawing.
    public bool DepthTest { get { return _depthTestEnabled; } set { _depthTestEnabled = value; } }
    public bool CullFace { get { return _cullFaceEnabled; } set { _cullFaceEnabled = value; } }
    public bool ScissorTest { get { return _scissorTestEnabled; } set { _scissorTestEnabled = value; } }
    public bool Blend { get { return _blendEnabled; } set { _blendEnabled = value; } }
    public BlendingFactor BlendFactor { get { return _blendFactor; } set { _blendFactor = value; } }
    public BlendEquationMode BlendFunc { get { return _blendFunc; } set { _blendFunc = value; } }
    public FrontFaceDirection FrontFaceDirection { get { return _frontFaceDirection; } set { _frontFaceDirection = value; } }
    public bool DepthMask { get { return _depthMask; } set { _depthMask = value; } }
    public CullFaceMode CullFaceMode { get { return _cullFaceMode; } set { _cullFaceMode = value; } }

    //State switches to prevent unnecessary gpu context changes.
    [DataMember] private bool _depthTestEnabled = true;
    [DataMember] private bool _cullFaceEnabled = true;
    [DataMember] private bool _scissorTestEnabled = true;
    [DataMember] private bool _blendEnabled = false;
    [DataMember] private BlendingFactor _blendFactor = BlendingFactor.OneMinusSrcAlpha;
    [DataMember] private BlendEquationMode _blendFunc = BlendEquationMode.FuncAdd;
    [DataMember] private FrontFaceDirection _frontFaceDirection = FrontFaceDirection.Ccw;
    [DataMember] private bool _depthMask = true;//enable writing to depth bufer
    [DataMember] private CullFaceMode _cullFaceMode = CullFaceMode.Back;

    public GpuRenderState Clone()
    {
      return (GpuRenderState)this.MemberwiseClone();
    }
    public void SetState()
    {
      Gu.Context.Gpu.SetState(this);
    }
  }

  public class GPULog
  {
    #region Members

    private enum GpuLogLevel
    {
      Err_,
      Wrn_,
      Inf_,
      Dbg_
    }
    bool _bPrintingGPULog = false;
    int _maxMsgLen = -1;

    #endregion
    #region Public:Methods

    public GPULog()
    {
#if _DEBUG
      GL.Enable(EnableCap.DebugOutput);
      GL.Enable(EnableCap.DebugOutputSynchronous);
#endif
    }
    public bool CheckErrors(bool bDoNotBreak, bool doNotLog)
    {
      StringBuilder sb = new StringBuilder();
      StringBuilder eb = new StringBuilder();

      GetAndFlushGPULog(sb);
      var hasErrors = GetOpenGLErrors(eb);

      if (hasErrors)
      {
        if (doNotLog == false)
        {
          eb.Append(Environment.NewLine);
          eb.Append(sb);
          Gu.Log.Error(eb.ToString());
        }

        if (bDoNotBreak == false && Gu.EngineConfig.BreakOnGraphicsError == true)
        {
          Gu.DebugBreak();
        }
      }
      else
      {
        if (sb != null && sb.Length > 0 && doNotLog == false)
        {
          Gu.Log.Error(sb.ToString());
        }
      }

      return hasErrors;
    }
    public void ClearGPULog()
    {
      GetAndFlushGPULog(null);
    }

    #endregion
    #region Private:Methods

    private bool GetOpenGLErrors(StringBuilder? eb)
    {
      bool bError = false;

      for (int ierr = 0; Gu.WhileTrueGuard(ierr, Gu.c_intMaxWhileTrueLoop); ierr++)
      {
        ErrorCode err = GL.GetError();
        if (err != ErrorCode.NoError)
        {
          if (eb != null)
          {
            string errmsg = $"GL Error: {glErrToStr(err)} ({(int)err})";
            eb.Append(errmsg);
          }
          bError = true;
        }
        else
        {
          break;
        }
      }

      return bError;
    }
    private void GetAndFlushGPULog(StringBuilder? sb)
    {
      int numMsgs = 1;
      int numFound;

      if (_maxMsgLen == -1)
      {
        _maxMsgLen = GL.GetInteger((GetPName)0x9143 /*GL_MAX_DEBUG_MESSAGE_LENGTH*/);
      }
      if (_maxMsgLen <= 0)
      {
        Gu.Log.Error("GL_MAX_DEBUG_MESSAGE_LENGTH returned 0.");
        _maxMsgLen = -2;
        return;
      }

      bool graphicsLogHigh = Gu.EngineConfig.GraphicsErrorLogging_High;
      bool graphicsLogMed = Gu.EngineConfig.GraphicsErrorLogging_Medium;
      bool graphicsLogLow = Gu.EngineConfig.GraphicsErrorLogging_Low;
      bool graphicsLogInfo = Gu.EngineConfig.GraphicsErrorLogging_Info;

      do
      {
        DebugSource[] sources = new DebugSource[numMsgs];
        DebugType[] types = new DebugType[numMsgs];
        DebugSeverity[] severities = new DebugSeverity[numMsgs];
        int[] ids = new int[numMsgs];
        int[] lengths = new int[numMsgs];

        string msgcopy = "";
        numFound = GL.GetDebugMessageLog(numMsgs, numMsgs * _maxMsgLen, sources, types, ids, severities, lengths, out msgcopy);

        if (numFound == 0)
        {
          return;
        }
        if (sb == null)//clearOnly
        {
          continue;
        }

        Array.Resize(ref sources, numFound);
        Array.Resize(ref types, numFound);
        Array.Resize(ref severities, numFound);
        Array.Resize(ref ids, numFound);
        Array.Resize(ref lengths, numFound);

        int currPos = 0;
        for (int iMsg = 0; iMsg < lengths.Length; ++iMsg)
        {
          int id = ids[iMsg];
          if (!skipNVIDIA(id) && !skipATI(id))
          {
            DebugSeverity severity = severities[iMsg];
            DebugType type = types[iMsg];
            DebugSource source = sources[iMsg];
            LogGPUMessageText(sb, msgcopy, id, severity, type, source, graphicsLogHigh, graphicsLogMed, graphicsLogLow, graphicsLogInfo);
          }
          currPos = currPos + lengths[iMsg];
        }

      } while (numFound > 0);
    }
    private void LogGPUMessageText(StringBuilder sb, string cstrMsg, int msgId, DebugSeverity severity, DebugType type,
      DebugSource source, bool graphicsLogHigh, bool graphicsLogMed, bool graphicsLogLow, bool graphicsLogInfo)
    {
      string msg = "";
      string shaderMsg = "";
      Gu.Assert(sb != null);
      string strId = " [id=0x" + msgId.ToString("X") + "]";

      //Skip if the config.xml has turned off this kind of logging.
      if (severity == DebugSeverity.DebugSeverityHigh && graphicsLogHigh == false)
      {
        return;
      }
      else if (severity == DebugSeverity.DebugSeverityMedium && graphicsLogMed == false)
      {
        return;
      }
      else if (severity == DebugSeverity.DebugSeverityLow && graphicsLogLow == false)
      {
        return;
      }
      else if (severity == DebugSeverity.DebugSeverityNotification && graphicsLogInfo == false)
      {
        return;
      }

      string strSev = "";
      string strType = "";
      string strSource = "";
      GpuLogLevel level = GpuLogLevel.Dbg_;
      GetTypeSevSourceLevel(type, severity, source, ref strType, ref strSev, ref strSource, ref level);

      msg = $"GPU Log:{strId}{strType}{strSev}{strSource}{cstrMsg}";

      if (sb != null)
      {
        if (type == DebugType.DebugTypeError)
        {
          sb.AppendLine(msg);
        }
        else if (severity == DebugSeverity.DebugSeverityNotification)
        {
          sb.AppendLine(msg);
        }
        else
        {
          sb.AppendLine(msg);
        }
      }
    }
    private bool skipNVIDIA(int id)
    {
      //NVidia - redundant messages / infos
      return id == 0x00020071     // GL_DYANMIC_DRAW or GL_STATIC_DRAW memory usgae
             || id == 0x00020084  // Texture state usage warning: Texture 0 is base level inconsistent. Check texture size.
                                  // else if (id == 0x00020061) {
                                  //   return true;
                                  // }  // Framebuffer detailed info: The driver allocated storage for renderbuffer 1.
                                  // else if (id == 0x00020004) {
                                  //   return true;
                                  // }  // Usage warning: Generic vertex attribute array ... uses a pointer with a small value (...). Is this intended to be used as an offset into a buffer object?
                                  // else if (id == 0x00020072) {
                                  //   return true;
                                  // }  // Buffer performance warning: Buffer object ... (bound to ..., usage hint is GL_STATIC_DRAW) is being copied/moved from VIDEO memory to HOST memory.
                                  // else if (id == 0x00020074) {
                                  //   return true;
                                  // }  // Buffer usage warning: Analysis of buffer object ... (bound to ...) usage indicates that the GPU is the primary producer and consumer of data for this buffer object.  The usage hint s upplied with this buffer object, GL_STATIC_DRAW, is inconsistent with this usage pattern.  Try using GL_STREAM_COPY_ARB, GL_STATIC_COPY_ARB, or GL_DYNAMIC_COPY_ARB instead.
                                  // else if (id == 0x00020070) {
                                  //   return true;
                                  // }  // Total VBO Usage in the system... (Useful information)
                                  // else if (id == 0x00020043) {
                                  //   return true;
                                  // }  // A non-Fullscreen clear caused a fallback from CSAA to MSAA; - probolem in clearing cube shadow buffers
                                  //Other (mom's house) driver
                                  // else if (id == 0x07) {
                                  //   return true;
                                  // }  // glLineWidth Deprecated (other driver)

          ;

      return false;
    }
    private bool skipATI(int id)
    {
      return false;
    }
    private static void GetTypeSevSourceLevel(DebugType type, DebugSeverity severity, DebugSource source, ref string strType, ref string strSev, ref string strSource, ref GpuLogLevel level)
    {
      if (type == DebugType.DebugTypeError)
      {
        strType = "[type=ERROR]";
      }
      else if (type == DebugType.DebugTypeDeprecatedBehavior)
      {
        strType = "[type=DEPRECATED_BEHAVIOR]";
      }
      else if (type == DebugType.DebugTypeUndefinedBehavior)
      {
        strType = "[type=UNDEFINED_BEHAVIOR]";
      }
      else if (type == DebugType.DebugTypePortability)
      {
        strType = "[type=PORTABILITY]";
      }
      else if (type == DebugType.DebugTypePerformance)
      {
        strType = "[type=PERFORMANCE]";
      }
      else if (type == DebugType.DebugTypeOther)
      {
        strType = "[type=OTHER]";
      }
      else
      {
        strType = "[type=(undefined(" + type + "))]";
      }

      if (severity == DebugSeverity.DebugSeverityHigh)
      {
        strSev = "[severity=HIGH]";
        level = GpuLogLevel.Err_;
      }
      else if (severity == DebugSeverity.DebugSeverityMedium)
      {
        strSev = "[severity=MEDIUM]";
        level = GpuLogLevel.Wrn_;
      }
      else if (severity == DebugSeverity.DebugSeverityLow)
      {
        strSev = "[severity=LOW]";
        level = GpuLogLevel.Inf_;
      }
      else if (severity == DebugSeverity.DebugSeverityNotification)
      {
        strSev = "[severity=NOTIFICATION]";
        level = GpuLogLevel.Inf_;
      }
      else
      {
        strSev = "[severity=(undefined(" + severity + ")))]";
        level = GpuLogLevel.Inf_;
      }

      if (source == DebugSource.DebugSourceApi)
      {
        strSource = "[source=API]";
      }
      else if (source == DebugSource.DebugSourceWindowSystem)
      {
        strSource = "[source=WINDOW_SYSTEM]";
      }
      else if (source == DebugSource.DebugSourceShaderCompiler)
      {
        strSource = "[source=SHADER_COMPILER]";
      }
      else if (source == DebugSource.DebugSourceThirdParty)
      {
        strSource = "[source=THIRD_PARTY]";
      }
      else if (source == DebugSource.DebugSourceApplication)
      {
        strSource = "[source=APPLICATION]";
      }
      else if (source == DebugSource.DebugSourceOther)
      {
        strSource = "[source=OTHER]";
      }
    }
    private static string glErrToStr(ErrorCode err)
    {
      switch (err)
      {
        case ErrorCode.NoError:
          return "GL_NO_ERROR         ";
        case ErrorCode.InvalidEnum:
          return "GL_INVALID_ENUM     ";
        case ErrorCode.InvalidValue:
          return "GL_INVALID_VALUE    ";
        case ErrorCode.InvalidOperation:
          return "GL_INVALID_OPERATION";
        case (ErrorCode)0x0503:
          return "GL_STACK_OVERFLOW   ";
        case (ErrorCode)0x0504:
          return "GL_STACK_UNDERFLOW  ";
        case ErrorCode.OutOfMemory:
          return "GL_OUT_OF_MEMORY    ";
      }
      return " *GL Error code not recognized.";
    }

    #endregion
  }

  public class BufferView
  {
    //view of mapped buffer memory
    public GpuDataFormat Format { get { return _region.Format; } }
    public int ItemCount { get { return _count_items; } }
    public int ItemOffset { get { return _offset_items; } }
    public int ArrayIndex { get { return _arrayIndex; } }
    public GpuMemoryBuffer Buffer { get { return _region.Buffer; } }

    private GpuMemoryBlock _region;
    private int _count_bytes = -1;
    private int _offset_bytes = -1;
    private int _count_items = -1;
    private int _offset_items = -1;
    private int _arrayIndex = -1; // index into region array

    public BufferView() { }
    public BufferView(GpuMemoryBlock region, int arrayIndex, int offset_bytes, int count_bytes)
    {
      Gu.Assert(region != null);
      _region = region;
      _arrayIndex = arrayIndex;
      _count_bytes = count_bytes;
      _offset_bytes = offset_bytes;
      _count_items = _count_bytes / region.Format.SizeBytes;
      _offset_items = _offset_bytes / region.Format.SizeBytes;
    }
    public GpuDataPtr CopyFromGPU(int itemOffset = 0, int itemCount = -1)
    {
      int byte_off = _offset_bytes + itemOffset * Format.SizeBytes;
      int byte_count = (itemCount < 0 ? _count_bytes : itemCount);

      Gu.Assert(Buffer != null);
      Gu.Assert(itemCount <= _count_bytes);
      Gu.Assert(_offset_bytes >= 0);

      byte[] managedArray = Buffer.CopyFromGPU(byte_off, byte_count);
      return new GpuDataPtr(Format.SizeBytes, byte_count / Format.SizeBytes, managedArray);
    }
    public void CopyToGPU<T>(T[] items, int srcItemOffset = 0, int dstItemOffset = 0, int itemCount = -1) where T : struct
    {
      int byte_off_src = _offset_bytes + srcItemOffset * Format.SizeBytes;
      int byte_count = (itemCount < 0 ? items.Length : itemCount);
      int byte_off_dst = _offset_bytes + dstItemOffset * Format.SizeBytes;

      Gu.Assert(byte_off_src >= 0);
      Gu.Assert((byte_count - byte_off_src) < _count_bytes);

      var handle = GCHandle.Alloc(items, GCHandleType.Pinned);
      Buffer.CopyToGPU(handle.AddrOfPinnedObject(), byte_off_src, byte_off_dst, byte_count);
      handle.Free();
    }
    public void Release()
    {
      //release this view - free memery
      _region.Release(this);
    }
  }//cls
  
  public class GpuByteBuffer : OpenGLResource
  {
    //Allows for any kind of data - not just "items"
    //GpuByteBuffer, GpuRawBuffer
    
    public BufferTarget BufferTarget { get { return _bufferTarget; } private set { _bufferTarget = value; } }
    private BufferTarget _bufferTarget = BufferTarget.ShaderStorageBuffer;
    private BufferRangeTarget? _rangeTarget = null;//For buffer block objects 
    protected bool _allocated = false;
    private int _sizeBytes = 0;
    private BufferUsageHint _hint;

    private List<BufferView> _views = new List<BufferView>();

    public GpuByteBuffer(string name, int size_bytes, BufferTarget target, BufferUsageHint hint) : base(name)
    {
      _sizeBytes = size_bytes;
      _hint = hint;
      _bufferTarget = target;

      _glId = GT.GenBuffer();
      Gpu.CheckGpuErrorsDbg();
      SetObjectLabel();
      if (size_bytes > 0)
      {
        Allocate(size_bytes);
      }
    }
    protected void Allocate(int size_bytes)
    {
      byte[] arr = new byte[size_bytes];
      Array.Clear(arr);

      var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
      GL.BufferData(_bufferTarget, (int)size_bytes, handle.AddrOfPinnedObject(), _hint);
      handle.Free();

      GL.BindBuffer(_bufferTarget, 0);
      _allocated = true;
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsBuffer(_glId))
      {
        GT.DeleteBuffer(_glId);
      }
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
    public byte[] CopyFromGPU(int offset_bytes, int length_bytes)
    {
      Gu.Assert(this._allocated);
      //Copies GPU data into a temporary byte array.

      if (BufferTarget == BufferTarget.ShaderStorageBuffer)//SSBOs reads and writes use incoherent memory accesses, so they need the appropriate barriers
      {
        MemoryBarrier();
      }

      Bind();
      IntPtr pt = GL.MapBufferRange(BufferTarget, (IntPtr)offset_bytes, (IntPtr)length_bytes, BufferAccessMask.MapReadBit);
      Gpu.CheckGpuErrorsDbg();
      if (pt == IntPtr.Zero)
      {
        Gu.BRThrowException("Failed to map OpenGL Buffer.");
      }
      byte[] managedArray = new byte[length_bytes];
      Marshal.Copy(pt, managedArray, 0, (int)length_bytes);
      GL.UnmapBuffer(BufferTarget);

      Unbind();

      return managedArray;
    }
    public bool CopyToGPU(IntPtr psrc, int srcOff_bytes, int dstOff_bytes, int count_bytes)
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

      if (_bufferTarget == BufferTarget.ShaderStorageBuffer)//SSBOs reads and writes use incoherent memory accesses, so they need the appropriate barriers
      {
        MemoryBarrier();
      }
      Gu.Assert(dstOff_bytes + count_bytes <= _sizeBytes, $"{Name} - Buffer overflow.");

      Bind();
      {
        IntPtr pdst = GL.MapBufferRange(_bufferTarget, (IntPtr)dstOff_bytes, (IntPtr)count_bytes, BufferAccessMask.MapWriteBit);
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
              System.Buffer.MemoryCopy((void*)psrc, (void*)pdst, _sizeBytes, count_bytes);
              ret = true;
            }
          }
        }
        GL.UnmapBuffer(_bufferTarget);
        Gpu.CheckGpuErrorsDbg();
      }
      Unbind();

      return ret;
    }
    protected void MemoryBarrier()
    {
      MemoryBarrierFlags f = MemoryBarrierFlags.UniformBarrierBit;

      if (_bufferTarget == BufferTarget.UniformBuffer)
      {
        f = MemoryBarrierFlags.UniformBarrierBit;
      }
      else if (_bufferTarget == BufferTarget.ShaderStorageBuffer)
      {
        f = MemoryBarrierFlags.ShaderStorageBarrierBit;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      GL.MemoryBarrier(f);
    }
  }
  public class GpuMemoryBuffer : GpuByteBuffer
  {
    //Partitioned buffer - maps data items into arrays, and maps them to objs.
    // Purpose: lets us put all the verts into a single buffer and map them back to meshes.
    //type() -> fixed array size, <bufferView <-> object>

    private int _maxSizeBytes = 0;
    private List<GpuMemoryBlock> _regions = new List<GpuMemoryBlock>();

    public GpuMemoryBuffer(string name, BufferTarget target, BufferUsageHint hint, int maxSizeBytes = 0) : base(name, 0, target, hint)
    {
      //set sizebytes to 0 to not allocate
      _maxSizeBytes = maxSizeBytes;
      if (_maxSizeBytes == 0)
      {
        _maxSizeBytes = 2000000000;
      }

      _regions = new List<GpuMemoryBlock>();
      _regions.Add(new GpuMemoryBlock(0, _maxSizeBytes));

      if (maxSizeBytes > 0)
      {
        Allocate(maxSizeBytes);
      }
    }
    public BufferView? AllocateSingleItem<T>(T[] items, object? obj = null) where T : struct
    {
      BufferView? bv = null;
      if (TryAllocateRegion<T>(out var region, 1, 1))
      {
        region.TryGetView<T>(obj, out bv);
      }
      Gu.Assert(bv != null);

      bv.CopyToGPU(items);

      return bv;
    }
    public bool TryAllocateRegion<T>(out GpuMemoryBlock? region, int array_length = 1, int block_size = 1)
    {
      var fmt = GpuDataFormat.GetDataFormat<T>();
      return TryAllocateRegion(fmt, out region, array_length, block_size);
    }
    private bool TryAllocateRegion(GpuDataFormat fmt, out GpuMemoryBlock? region, int array_length = 1, int block_size = 1)
    {
      Gu.Assert(array_length > 0);
      Gu.Assert(block_size > 0);

      //find region if one exists
      region = AllocateRegion(fmt, array_length, block_size);

      if (region == null)
      {
        //todo; combine neighbor empty blocks, (move data as last resort)
        Gu.Trap();//TEST**
        Consolidate();
        region = AllocateRegion(fmt, array_length, block_size);
      }

      return region != null;
    }
    private GpuMemoryBlock? AllocateRegion(GpuDataFormat fmt, int array_length, int block_size)
    {
      GpuMemoryBlock? region = null;
      int item_size = fmt.SizeBytes;
      int region_size = array_length * block_size * item_size;
      for (int ri = _regions.Count - 1; ri >= 0; ri--)
      {
        var er = _regions[ri];
        if (er.IsEmpty && er.ByteSize >= region_size)
        {
          var sliced = new GpuMemoryBlock(er.ByteOffset + region_size, er.ByteSize - region_size);
          region = new GpuMemoryBlock(this, fmt, er.ByteOffset, region_size, block_size, array_length);
          //TODO: test that insert works here
          _regions.Insert(ri, region);
          _regions.Insert(ri + 1, sliced);
          break;
        }
      }
      return region;
    }
    private void Consolidate()
    {
      //TODO: rellocate the buffer and all the sub-data (last resort)
      for (int ri = _regions.Count - 1; ri >= 0; ri--)
      {
        _regions[ri].Consolidate();
        if (ri + 1 < _regions.Count && _regions[ri].IsEmpty && _regions[ri + 1].IsEmpty)
        {
          var a = _regions[ri + 1];
          var b = _regions[ri];

          _regions.RemoveAt(ri + 1);
          _regions[ri] = new GpuMemoryBlock(b.ByteOffset, a.ByteSize + b.ByteSize);
        }
      }
    }

  }//cls

  public class GpuMemoryBlock
  {
    //Memory Region in block - with array size. 
    //    blocksize = 3, arraysize = 2 -> { [v_v3x2,v_v3x2,v_v3x2], [v_v3x2,v_v3x2,v_v3x2] }
    //                                              ^ index 0                 ^ index 1    
    public class ArrayObject
    {
      public BufferView? View;
      public WeakReference<object> Object;
    }
    public GpuMemoryBuffer Buffer { get { return _buffer; } }
    public GpuDataFormat Format { get { return _format; } }
    public int ByteSize { get { return _byte_size; } }
    public int ByteOffset { get { return _byte_offset; } }
    public bool IsEmpty { get { return _arrayObjects == null; } }

    private Dictionary<BufferView, int>? _view_to_arrayindex = null;//cache - speed
    private List<ArrayObject?>? _arrayObjects = null;
    private GpuMemoryBuffer _buffer;
    private GpuDataFormat _format;
    private int _byte_offset = 0; // start of region
    private int _array_size = 0;//array size of 1 = no array.
    private int _block_size = 0; // number of sub-items in one array "block"
    private int _byte_size = 0; // number of sub-items in one array "block"

    public GpuMemoryBlock(int byte_offset, int byte_size)
    {
      //Empty ctor
      _byte_offset = byte_offset;
      _byte_size = byte_size;
    }
    public GpuMemoryBlock(GpuMemoryBuffer buffer, GpuDataFormat fmt, int byte_offset, int byte_size, int block_size, int array_size) : this(byte_size, byte_offset)
    {
      _buffer = buffer;
      _format = fmt;
      _array_size = array_size;
      _block_size = block_size;
    }
    public bool TryGetView<T>(object? obj, out BufferView? bv)
    {
      //get a new view for array index , or return a view if this is not arrayed
      Gu.Assert(obj != null);

      bv = null;
      int slot = FindEmptyArraySlot(obj);

      if (slot == -1 && _arrayObjects.Count < _array_size)
      {
        slot = AddArraySlot();
      }

      if (slot != -1)
      {
        bv = CreateView(slot);
        _arrayObjects[slot] = new ArrayObject() { Object = obj == null ? null : new WeakReference<object>(obj), View = bv };

        // if (obj is DataBlock)
        // {
        //   var db = obj as DataBlock;
        //   db.BufferViews = db.BufferViews.ConstructIfNeeded();
        //   db.BufferViews.Add(typeof(T), bv);
        // }
      }
      else
      {
        Gu.Log.Error($"Too many array instances / views. limit={_array_size} this objtype={obj.GetType().Name}'");
        Gu.DebugBreak();
      }

      return bv != null;
    }
    public void Release(BufferView bv)
    {
      if (_view_to_arrayindex.TryGetValue(bv, out int idx))
      {
        _view_to_arrayindex.Remove(bv);
        //must maintain array integrtity
        _arrayObjects[idx] = null;
      }
      else
      {
        //error - ould not find
        Gu.DebugBreak();
      }
    }
    private bool CanClearView(int ikey)
    {
      bool ret = (_arrayObjects[ikey] == null) || (_arrayObjects[ikey] != null && !_arrayObjects[ikey].Object.TryGetTarget(out var xt));
      return ret;
    }
    public void Consolidate()
    {
      if (IsEmpty)
      {
        return;
      }
      bool inside = false;
      for (int ikey = _arrayObjects.Count - 1; ikey >= 0; ikey--)
      {
        var val = _arrayObjects[ikey];
        //note objects can be null
        //array view was Released() or the using object was GC'd
        if (CanClearView(ikey))
        {
          //free dangling ref
          if (inside)
          {
            _arrayObjects[ikey] = null;
          }
          else
          {
            _arrayObjects.RemoveAt(ikey);
          }
        }
        else
        {
          inside = true;
        }

      }

      if (_arrayObjects.Count == 0)
      {
        //Mark empty
        _arrayObjects = null;
        _view_to_arrayindex = null;
      }
    }

    private int FindEmptyArraySlot(object? obj)
    {
      int slot = -1;

      if (_arrayObjects != null)
      {
        //find free slot if object went away or is null
        for (int ikey = 0; ikey < _arrayObjects.Count; ikey++)
        {
          if (CanClearView(ikey))//object values can be null in which case we don't have a mpaping, something just owns it.
          {
            //allowing for null object identifiers and requesting to release buffers with Release()
            slot = ikey;
            break;
          }
        }
      }
      return slot;
    }
    private int AddArraySlot()
    {
      _arrayObjects = _arrayObjects.ConstructIfNeeded();
      _view_to_arrayindex = _view_to_arrayindex.ConstructIfNeeded();

      Gu.Assert(_arrayObjects.Count + 1 <= _array_size);

      _arrayObjects.Add(null);

      return _arrayObjects.Count - 1;
    }
    private BufferView CreateView(int array_index)
    {
      int view_offset_bytes = _byte_offset + array_index * _format.SizeBytes;
      var bv = new BufferView(this, array_index, view_offset_bytes, _format.SizeBytes);
      _view_to_arrayindex.Add(bv, array_index);
      return bv;
    }


  }//cls


}//ns 
