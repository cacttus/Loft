using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using OpenTK.Graphics.OpenGL4;
namespace PirateCraft
{
  public abstract class HasGpuResources : IDisposable
  {
    //We may need weak reference here
    protected WindowContext Context {get; private set;}= null;
    bool _disposed = false;
    public abstract void Dispose_OpenGL_RenderThread();
    public HasGpuResources()
    {
      Context = Gu.Context;
    }
    public void Dispose()
    {
      _disposed = true;
      //Gu.Log.Warn("OpenGL Object wasn't disposed before being finalized. Must call Dispose() to release GPU resources.");
      var that = this;
      if (Thread.CurrentThread.ManagedThreadId != Context?.Gpu.RenderThreadID)
      {
        Context?.Gpu.Post_To_RenderThread(Context, (WindowContext wc) =>
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
    ~HasGpuResources()
    {
      if (!_disposed)
      {
        Dispose();
      }
    }
  }
  // public abstract class OpenGLContextData : HasGpuResources
  // {
  //   Dictionary<WeakReference<WindowContext>, int> Resources;
  //   public OpenGLContextResource()
  //   {

  //   }

  // }

  public abstract class OpenGLResource : HasGpuResources
  {
    protected int _glId;
    public int GetGlId() { return _glId; }

    protected string _name = "";
    public string Name { get { return _name; } }

    public OpenGLResource(string name) : base()
    {
      //The context that existed when this was created.
      Gu.Assert(Gu.Context != null);
      _name = name;
    }
    public void SetObjectLabel()
    {
      ObjectLabelIdentifier? ident = null;
      if (this is Texture2D)
      {
        ident = ObjectLabelIdentifier.Texture;
      }
      else if (this is GPUBuffer)
      {
        ident = ObjectLabelIdentifier.Buffer;
      }
      else if (this is FramebufferGeneric)
      {
        ident = ObjectLabelIdentifier.Framebuffer;
      }
      else if (this is ShaderStage)
      {
        ident = ObjectLabelIdentifier.Shader;
      }
      else if (this is ContextShader)
      {
        ident = ObjectLabelIdentifier.Program;
      }
      else if (this is VertexArrayObject)
      {
        ident = ObjectLabelIdentifier.VertexArray;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      if (ident != null)
      {
        GL.ObjectLabel(ident.Value, _glId, _name.Length, _name);
      }
    }

  }

  public abstract class OpenGLContextDataManager<T> where T : class
  {
    private Dictionary<WindowContext, T> _contextData = new Dictionary<WindowContext, T>();
    protected abstract T CreateNew();

    public OpenGLContextDataManager()
    {
    }
    protected T GetDataForContext(WindowContext ct)
    {
      T? ret = null;
      if (!_contextData.TryGetValue(ct, out ret))
      {
        ret = CreateNew();
        _contextData.Add(ct, ret);
      }
      return ret;
    }
  }



}