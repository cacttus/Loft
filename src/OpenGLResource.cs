using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace PirateCraft
{
   public abstract class OpenGLResource : IDisposable
   {
      bool _disposed = false;
      protected int _glId;
      public int GetGlId() { return _glId; }
      WindowContext _context = null;//the created context
      public OpenGLResource()
      {
         //This is .. iffy
         Gu.Assert(Gu.Context != null);
         _context = Gu.Context;
      }
      public virtual void Dispose()
      {
         _disposed = true;
         GC.SuppressFinalize(this);
      }
      //public void Dispose()
      //{
      //   //For some reason this causes issues I don't know why.
      //   Dispose(false); //true: safe to free managed resources
      //   GC.SuppressFinalize(this);
      //}
      //protected void Dispose(Boolean isFinalizer)
      //{
      //   if (!isFinalizer)
      //   {
      //      Free();
      //   }
      //   _disposed = true;
      //}
      ~OpenGLResource()
      {
         if (!_disposed)
         {
            //Gu.Log.Warn("OpenGL Object wasn't disposed before being finalized. Must call Dispose() to release GPU resources.");
            var that = this;
            Gpu.RegisterFreeGPUMemory(_context, (WindowContext wc) =>
            {
               that.Dispose();
            });
         }
      }
   }
}