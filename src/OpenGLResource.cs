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
    WindowContext _context = null;

    public abstract void Dispose_OpenGL_RenderThread();

    public OpenGLResource()
    {
      //The context that existed when this was created.
      Gu.Assert(Gu.Context != null);
      _context = Gu.Context;
    }
    public void Dispose()
    {
      _disposed = true;
      //Gu.Log.Warn("OpenGL Object wasn't disposed before being finalized. Must call Dispose() to release GPU resources.");
      var that = this;
      if (Thread.CurrentThread.ManagedThreadId != _context?.Gpu.RenderThreadID)
      {
        _context?.Gpu.Post_To_RenderThread(_context, (WindowContext wc) =>
        {
          that.Dispose_OpenGL_RenderThread();
        });
      }
      else
      {
        Dispose_OpenGL_RenderThread();
      }
      GC.SuppressFinalize(this);
    }
    ~OpenGLResource()
    {
      if (!_disposed)
      {
        Dispose();

      }
    }
  }
}