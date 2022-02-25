using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;

namespace PirateCraft
{
   public class GpuRenderState
   {
      //State switches to prevent unnecessary gpu context changes.
      private bool _depthTestEnabledLast = false;
      private bool _depthTestEnabled = true;
      private bool _cullFaceEnabledLast = false;
      private bool _cullFaceEnabled = true;
      private bool _scissorTestEnabledLast = false;
      private bool _scissorTestEnabled = true;
      private bool _blendEnabledLast = false;
      private bool _blendEnabled = false;
      private BlendEquationMode _blendFuncLast = BlendEquationMode.FuncAdd;
      private BlendEquationMode _blendFunc = BlendEquationMode.FuncAdd;
      private BlendingFactor _blendFactorLast = BlendingFactor.OneMinusSrcAlpha;
      private BlendingFactor _blendFactor = BlendingFactor.OneMinusSrcAlpha;

      public GpuRenderState Clone()
      {
         GpuRenderState clone = new GpuRenderState();
         clone._depthTestEnabledLast = _depthTestEnabledLast;
         clone._depthTestEnabled = _depthTestEnabled;
         clone._cullFaceEnabledLast = _cullFaceEnabledLast;
         clone._cullFaceEnabled = _cullFaceEnabled;
         clone._scissorTestEnabledLast = _scissorTestEnabledLast;
         clone._scissorTestEnabled = _scissorTestEnabled;
         clone._blendEnabled = _blendEnabled;
         clone._blendFunc = _blendFunc;
         clone._blendFactor = _blendFactor;
         return clone;
      }

      public bool CullFace { get { return _cullFaceEnabled; } set { _cullFaceEnabledLast = _cullFaceEnabled; _cullFaceEnabled = value; } }
      public bool DepthTest { get { return _depthTestEnabled; } set { _depthTestEnabledLast = _depthTestEnabled; _depthTestEnabled = value; } }
      public bool ScissorTest { get { return _scissorTestEnabled; } set { _scissorTestEnabledLast = _scissorTestEnabled; _scissorTestEnabled = value; } }
      public bool Blend { get { return _blendEnabled; } set { _blendEnabledLast = _blendEnabled;  _blendEnabled = value; } }
      //public BlendEquationMode BlendFunc { get { return _blendFunc; } set { _blendFuncLast = _blendFunc; _blendFunc = value; } }
      //public BlendingFactor BlendingFactor { get { return _blendFactor; } set { _blendFactorLast = _blendFactor;  _blendFactor = value; } }

      public void SetState()
      {
         if (_blendEnabled != _blendEnabledLast)
         {
            if (_blendEnabled)
            {
               GL.Enable(EnableCap.Blend);
               //Just default to basic blending for now
               GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }
            else
            {
               GL.Disable(EnableCap.Blend);
            }
         }
         //if (_blendFunc != _blendFuncLast)
         //{
         //   if (_blendEnabled)
         //   {
         //      GL.Enable(EnableCap.Blend);
         //   }
         //   else
         //   {
         //      GL.Disable(EnableCap.Blend);
         //   }
         //}
         if (_depthTestEnabled != _depthTestEnabledLast)
         {
            if (_depthTestEnabled)
            {
               GL.Enable(EnableCap.DepthTest);
            }
            else
            {
               GL.Disable(EnableCap.DepthTest);
            }
         }
         if (_scissorTestEnabled != _scissorTestEnabledLast)
         {
            if (_scissorTestEnabled)
            {
               GL.Enable(EnableCap.ScissorTest);
            }
            else
            {
               GL.Disable(EnableCap.ScissorTest);
            }
         }
         if (_cullFaceEnabled != _cullFaceEnabledLast)
         {
            if (_cullFaceEnabled)
            {
               GL.Enable(EnableCap.CullFace);
            }
            else
            {
               GL.Disable(EnableCap.CullFace);
            }
         }
      }
   }
   //This instance must be per-context.
   public class Gpu
   {
      private static Dictionary<WindowContext, List<Action<WindowContext>>> OpenGLCleanupActions = new Dictionary<WindowContext, List<Action<WindowContext>>>();
      private int _maxTextureSize = 1;
      //  public GpuRenderState GpuRenderState { get; set; } = new GpuRenderState();

      public Gpu()
      {
         //Initializes gpu info
         int[] maxTextureSize = new int[2];
         GL.GetInteger(GetPName.MaxTextureSize, maxTextureSize);
         _maxTextureSize = maxTextureSize[0];
      }
      public int GetMaxTextureSize()
      {
         return _maxTextureSize;
      }
      public static GpuDataPtr GetGpuDataPtr<T>(T[] data)
      {
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
      public static unsafe T ByteArrayToStructure<T>(byte[] bytes) where T : struct
      {
         fixed (byte* ptr = &bytes[0])
         {
            return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
         }
      }
      public static GpuDataArray SerializeGpuData<T>(T[] data) where T : struct
      {
         var size = Marshal.SizeOf(data[0]);

         var bytes = new byte[size * data.Length];
         var ptr = Marshal.AllocHGlobal(size);
         for (int di = 0; di < data.Length; di++)
         {
            Marshal.StructureToPtr(data[di], ptr, false);
            Marshal.Copy(ptr, bytes, di * size, size);
         }
         Marshal.FreeHGlobal(ptr);
         GpuDataArray arr = new GpuDataArray(size, data.Length, bytes);

         return arr;
      }

      private static object _freeingGPUMemory = new object();
      public static void RegisterFreeGPUMemory(WindowContext wc, Action<WindowContext> a)
      {
         lock (_freeingGPUMemory)
         {

            //Register an action to delete GPU memory on the main thread.
            //This is for C# finalizers (called on the GC thread)
            List<Action<WindowContext>> actions = null;
            if (!OpenGLCleanupActions.TryGetValue(wc, out actions))
            {
               OpenGLCleanupActions.Add(wc, new List<Action<WindowContext>> { a });
            }
            else
            {
               actions.Add(a);
            }
         }
      }
      public static void FreeGPUMemory(WindowContext wc)
      {
         lock (_freeingGPUMemory)
         {
            List<Action<WindowContext>> actions = null;
            //Call this at the end of render thread (or beginning)
            if (OpenGLCleanupActions.TryGetValue(wc, out actions))
            {
               foreach (var action in actions)
               {
                  action(wc);
               }
               actions.Clear();
            }
         }
      }
      public static void CheckGpuErrorsRt()
      {
         ErrorCode c = GL.GetError();
         if (c != ErrorCode.NoError)
         {
            if (Gu.EngineConfig.LogErrors)
            {
               Gu.Log.Error("OpenGL Error " + c.ToString());
            }
            if (Gu.EngineConfig.BreakOnOpenGLError)
            {
               System.Diagnostics.Debugger.Break();
            }
         }
      }
      public static void CheckGpuErrorsDbg()
      {
#if DEBUG
         CheckGpuErrorsRt();
#endif
      }


   }
}
