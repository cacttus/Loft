using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;
namespace PirateCraft
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

    public OpenGLResource(string name) : base()
    {
      //The context that existed when this was created.
      Gu.Assert(Gu.Context != null);
      _name = Library.MakeDatapathName(name, GetType());
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
        //var name = Lib.MakeDatapathName(this.Name, GetType());
        GL.ObjectLabel(ident.Value, _glId, _name.Length, _name);
      }
    }
  }
  [DataContract]
  public class DynamicFileLoader : DataBlock
  {
    public DateTime MaxModifyTime { get { return _maxModifyTime; } private set { _maxModifyTime = value; } }
    public DateTime _maxModifyTime = DateTime.MinValue;
    public virtual bool ShouldCheck { get { return false; } } //return true to check for chaned fiesl
    protected virtual List<FileLoc> Files { get { return new List<FileLoc>(); } }
    //[DataMember] protected HashSet<FileLoc> _files = new HashSet<FileLoc>(new FileLoc.EqualityComparer());

    protected DynamicFileLoader() { } //clone/serialize
    public DynamicFileLoader(string name) : base(name) { }

    protected virtual void OnSourceChanged(List<FileLoc> changed) { }

    protected void UpdateMaxModifyTime()
    {
      //Set max modify time, this lets us compile the shaders dynamically on file changes, it also lets us load the GLBinary on file changes.
      var oldmod = MaxModifyTime;
      foreach (var f in this.Files)
      {
        MaxModifyTime = MathUtils.Max(MaxModifyTime, f.GetLastWriteTime(true));
      }
      if (MaxModifyTime != oldmod)
      {
        //debug
        int n = 0;
        n++;
      }
    }

    public void CheckSourceChanged()
    {
      if (this.Files == null || this.Files.Count == 0)
      {
        //The shader has been gott, but not initialized.
        return;
      }
      List<FileLoc> changed = new List<FileLoc>();

      foreach (var f in this.Files)
      {
        var wt = f.GetLastWriteTime(true);
        if (wt > MaxModifyTime)
        {
          // ** Set the modify time to the maximum file mod - even if compile fails. This prevents infinite checking
          MaxModifyTime = wt;
          changed.Add(f);
        }
      }
      if (changed.Count > 0)
      {
        Gu.Log.Info($"Resource {Name} has changed, hot-re-loading");
        OnSourceChanged(changed);
      }
    }
  }

  [DataContract]
  public abstract class OpenGLContextDataManager<T> : DynamicFileLoader where T : class
  {
    protected Dictionary<WindowContext, T> _contextData = new Dictionary<WindowContext, T>();
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