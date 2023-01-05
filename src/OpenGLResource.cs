using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;
namespace Loft
{
  public abstract class HasGpuResources : IDisposable
  {
    //We may need weak reference here
    protected WindowContext Context { get; private set; } = null;
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
  public abstract class OpenGLResource : HasGpuResources
  {
    protected int _glId;
    public int GlId { get { return _glId; } }

    //this is the object lable name
    protected string _name = "";
    public string Name { get { return _name; } }

    protected virtual string DataPathName() { return "-obj"; }

    public OpenGLResource(string name) : base()
    {
      //The context that existed when this was created.
      Gu.Assert(Gu.Context != null);
      _name = name + DataPathName();
    }
    public void SetObjectLabel()
    {
      ObjectLabelIdentifier? ident = null;
      if (this is GpuTexture)
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
      else if (this is GpuShader)
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
  [DataContract]
  public abstract class OpenGLContextDataManager<T> : DataBlock where T : class
  {
    protected Dictionary<WindowContext, T> _contextData = new Dictionary<WindowContext, T>();
    //override to create a new resource for the current context
    protected abstract T CreateNew();

    //Textures are sharable - override return True if the GL data is sharable.
    public virtual bool IsDataShared { get { return false; } }

    protected OpenGLContextDataManager() { }//clone/serialize

    public OpenGLContextDataManager(string name) : base(name)
    {
    }
    public T GetDataForContext(WindowContext ct)
    {
      if (IsDataShared)
      {
        if (ct.SharedContext != null)
        {
          ct = ct.SharedContext;
        }
        else if (_contextData.Keys.Count >= 1 && _contextData.Keys.First() != ct)
        {
          Gu.Log.Warn("Multiple contexts for textures is not implemented. Texture data will be null, and may need to be copied to additional contexts");
          Gu.DebugBreak();
        }
      }

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