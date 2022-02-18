using OpenTK.Graphics.OpenGL;
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
      public bool CullFace { get { return _cullFaceEnabled; } set { _cullFaceEnabledLast = _cullFaceEnabled; _cullFaceEnabled = value; } }
      public bool DepthTest { get { return _depthTestEnabled; } set { _depthTestEnabledLast = _depthTestEnabled; _depthTestEnabled = value; } }
      public bool ScissorTest { get { return _scissorTestEnabled; } set { _scissorTestEnabledLast = _scissorTestEnabled; _scissorTestEnabled = value; } }
      public void SetState()
      {
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
      public GpuRenderState GpuRenderState { get; set; } = new GpuRenderState();

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
      public static GpuDataArray SerializeGPUData<T>(T[] data) where T : struct
      {
         //This method is not to be called at runtime. Loops over all the data sequentially.
         //There may be a faster way, but it works for now.
         //This is essentially meant to be used just for sending vertex and Index data to GpuBuffer data one time.
         var size = Marshal.SizeOf(data[0]);

         //The serializer method messes up the data.
         //var formatter = new BinaryFormatter();
         // var stream = new MemoryStream();
         // formatter.Serialize(stream, data);
         // GpuDataArray arr1 = new GpuDataArray(size, data.Length, stream.ToArray());
         //return arr1;
         //
         var bytes = new byte[size * data.Length];
         var ptr = Marshal.AllocHGlobal(size);
         for (int di = 0; di < data.Length; di++)
         {
            Marshal.StructureToPtr(data[di], ptr, false);
            Marshal.Copy(ptr, bytes, di * size, size);
            //Marshal.DestroyStructure(ptr);
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
   }
}
