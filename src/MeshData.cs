using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace PirateCraft
{
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuFaceData
  {
    public GpuFaceData() { }
    public vec3 _normal = vec3.Zero;
    public uint _index = 0;
    public vec3 _tangent = vec3.Zero;
    public float pad1 = 0;
  }
  public enum DrawOrder
  {
    First = 0,//The dictionary iterates lower to higher.
    Mid = 1,
    Last = 2,
    MaxDrawOrders
  }
  public enum DrawMode
  {
    Forward,
    Deferred,
    Debug,
    MaxDrawModes
  }

  public class VertexArrayObject : OpenGLResource
  {
    public VertexArrayObject(string name) : base(name)
    {
      _glId = GT.GenVertexArray();
      GL.BindVertexArray(_glId);
      SetObjectLabel();
    }

    protected override string DataPathName() { return "-vao" + base.DataPathName(); }

    public void Bind()
    {
      //Note the vertex array must be bound before this works.
      //If array is a name returned by glGenVertexArrays, by that has not yet been bound through a call to glBindVertexArray, then the name is not a vertex array object and glIsVertexArray returns GL_FALSE.
      if (!GL.IsVertexArray(_glId))
      {
        Gu.Log.Error("Mesh was not a VAO.");
        Gu.DebugBreak();
        return;
      }
      GL.BindVertexArray(_glId);
    }
    //Do not implement finalizer
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsVertexArray(_glId))
      {
        GT.DeleteVertexArray(_glId);
      }
    }
    public void Unbind()
    {
      GL.BindVertexArray(0);
    }
  }
  public class ContextMesh
  {
    public VertexArrayObject VAO { get; set; } = null;
    private WeakReference<MeshData> _meshData = null;
    public ContextMesh(MeshData parent)
    {
      _meshData = new WeakReference<MeshData>(parent);
    }
  }
  [DataContract]
  public class SkinWeights : MutableState
  {
    //Skin weights.
    //  technically we should be using vertex groups and naming them to the joint names on the armature
    //  then compile the groups into a single buffer ordinal by vertex index
    //  this will decouple and "bind" armatures to arbitrary vertex groups as this is actually part of the requirements
    //  e.g. allow "retargeting"

    //  bool IsSHapekey.. et shape key would use vec3 .. 
    public GPUBuffer wd_in_st;
    public GPUBuffer jw_in_st;

    public SkinWeights(string meshname, wd_in_st[] offs, jw_in_st[] weights)
    {
      wd_in_st = Gpu.CreateShaderStorageBuffer($"{meshname}-wd_in_st", offs);
      jw_in_st = Gpu.CreateShaderStorageBuffer($"{meshname}-jw_in_st", weights);
    }
  }
  public class SoftBodyWeights
  {
  }
  [DataContract]
  public class MeshView : DataBlock
  {
    // @class MeshView
    // @brief Visible mesh, or subset of a mesh.
    // @details Lightweight data structure that points to a subset of mesh data.
    //           see glDrawElementsIndexed** functions for more info
    // @note TODO: this class will need to have views for each buffer for vertex arrays, or a view to the index buffer for indexed arrays
    //        should probably contain references to the GPUBuffer's  
    public MeshData? MeshData { get { return _meshData; } set { _meshData = value; SetLimits(null, null); } }
    public int Start { get { return _start; } set { _start = value; CheckLimits(); } }
    public int Count { get { return _count; } set { _count = value; CheckLimits(); } }

    [DataMember] private MeshData? _meshData = null;
    [DataMember] private int _start = 0;
    [DataMember] private int _count = 0;

    protected MeshView() { }//clone/copy
    public MeshView(MeshData? data, int? start = null, int? count = null) : base(data.Name)
    {
      _meshData = data;
      SetLimits(start, count);
    }
    public void Draw(GpuInstanceData[] instances, OpenTK.Graphics.OpenGL4.PrimitiveType? primTypeOverride = null)
    {
      if (_meshData == null)
      {
        Gu.Log.Error($"'{Name}' Tried to draw null mesh");
        return;
      }

      //@param instances - Can be null in which case we draw a mesh without an accompanying instance transform
      var vao = _meshData.GetDataForContext(Gu.Context);

      Gpu.CheckGpuErrorsDbg();
      vao.Bind();

      //***** Render Break
      if (Gu.BreakRenderState || !String.IsNullOrEmpty(Gu.SaveRenderStateFile))
      {
        string rs = GpuDebugInfo.DebugGetRenderState(true, false, false);
        if (!String.IsNullOrEmpty(Gu.SaveRenderStateFile))
        {
          //idk if this will work
          //FileLoc fl = new FileLoc(Gu.SaveRenderStateFile, FileStorage.Disk);
          //fl.WriteAllText(rs);
          Gu.SaveRenderStateFile = "";
        }
        if (Gu.BreakRenderState)
        {
          Gu.Log.Info(rs);
          Gu.DebugBreak();
          Gu.BreakRenderState = false;
        }
      }
      //***** Render Break

      OpenTK.Graphics.OpenGL4.PrimitiveType primType = _meshData.PrimitiveType;
      if (primTypeOverride != null)
      {
        primType = primTypeOverride.Value;
      }

      //This is assuming the VAO and all other bindings are already called.
      var ibo = _meshData.IndexBuffer;
      if (_meshData.IndexBuffer != null)
      {
        Gu.Assert(ibo.ItemSizeBytes == 2 || ibo.ItemSizeBytes == 4);

        if (instances != null && instances.Length > 0)
        {
          //GL.DrawElementsInstancedBaseVertexBaseInstance()
          GL.DrawElementsInstanced(primType,
            _count,
            ibo.DrawElementsType,
            IntPtr.Zero + _start,
            instances.Length
            );
          Gpu.CheckGpuErrorsDbg();
          Gu.FrameStats.NumDrawElements_Frame++;
          Gu.FrameStats.NumElements_Frame += _count;

        }
        else
        {
          //This shouldn't happen anymore (right now) testing out the entire instancing system
          Gu.Log.ErrorCycle("Instances were not specified for mesh " + this.Name);
          Gu.DebugBreak();

          GL.DrawElements(primType,
            _count,
            ibo.DrawElementsType,
            IntPtr.Zero + _start
            );
          Gpu.CheckGpuErrorsDbg();
          Gu.FrameStats.NumDrawElements_Frame++;
          Gu.FrameStats.NumElements_Frame += _count;
        }
      }
      else
      {
        foreach (var vb in _meshData.VertexBuffers)
        {
          if (instances != null && instances.Length > 0)
          {
            GL.DrawArraysInstanced(primType,
              _start,
              _count,
              instances.Length
              );
            Gpu.CheckGpuErrorsDbg();
            Gu.FrameStats.NumDrawArrays_Frame++;
            Gu.FrameStats.NumArrayElements_Frame += _count;
          }
          else
          {
            //This shouldn't happen anymore (right now) testing out the entire instancing system
            Gu.DebugBreak();
            Gu.Log.ErrorCycle("Instances were not specified for mesh " + this.Name);
            GL.DrawArrays(primType,
              _start,
              _count
              );
            Gpu.CheckGpuErrorsDbg();
            Gu.FrameStats.NumDrawArrays_Frame++;
            Gu.FrameStats.NumArrayElements_Frame += _count;
          }
        }
      }
      vao.Unbind();
    }
    public void SetLimits(int? nstart = null, int? ncount = null)
    {
      //Set the number of verts/inds to render
      if (_meshData != null)
      {
        int start = 0, count = -1;
        if (nstart != null)
        {
          start = nstart.Value;
        }
        if (ncount != null)
        {
          count = ncount.Value;
        }

        if (ncount == null)
        {
          count = GetDefaultVboOrIboCount(_meshData);
        }
        _start = start;
        _count = count;
        CheckLimits();
      }
    }
    private void CheckLimits()
    {
      if (_meshData != null)
      {
        var ct = GetDefaultVboOrIboCount(_meshData);
        if (_start > ct)
        {
          _start = 0;
          Gu.Log.Warn($"{Name}: Mesh View was outside range.");
          Gu.DebugBreak();
        }
        if (_count > ct)
        {
          _count = ct;
          Gu.Log.Warn($"{Name}: Mesh View was outside range.");
          Gu.DebugBreak();
        }
      }
    }
    private int GetDefaultVboOrIboCount(MeshData md)
    {
      int ele_count = 0;
      Gu.Assert(md != null);
      if (md.HasIndexes)
      {
        ele_count = md.IndexBuffer.ItemCount;
      }
      else if (md.VertexBuffers != null && md.VertexBuffers.Count == 1)
      {
        ele_count = md.VertexBuffers[0].ItemCount;
      }
      else
      {
        Gu.Log.Error($"{Name}: Could not set mesh view count, there was more than 1 VB and no indexes specified.");
        Gu.DebugBreak();
      }
      return ele_count;
    }
    public MeshView Clone()
    {
      return new MeshView()
      {
        _meshData = this._meshData,
        _start = this._start,
        _count = this._count,
      };
    }
    public void ExpandCopyIndexes<T>(GrowList<T> inds)
    {
      Gu.Assert(_meshData != null);
      Gu.Assert(_meshData.HasIndexes);

      _meshData.IndexBuffer.ExpandCopy(inds);
      Start = 0;
      Count = inds.Count;
    }

  }
  [DataContract]
  public class MeshData : OpenGLContextDataManager<VertexArrayObject>
  {
    // @class MeshData
    // @brief Manages mesh data and meshes among GL contexts.

    #region Public: Members

    public bool BoundBoxComputed { get { return _boundBoxComputed; } private set { _boundBoxComputed = value; } }
    public Box3f BoundBox_Extent { get { return _boundBoxExtent; } } //Bond box of mesh extenss
    public PrimitiveType PrimitiveType { get { return _primitiveType; } }
    public List<GPUBuffer> VertexBuffers { get { return _vertexBuffers; } }
    public SkinWeights SkinWeights { get { return _skinWeights; } set { _skinWeights = value; } }
    public SoftBodyWeights SoftBodyWeights { get { return _softBodyWeights; } }
    public GPUBuffer IndexBuffer { get { return _indexBuffer; } }
    public GPUBuffer FaceData { get { return _faceData; } private set { _faceData = value; } }
    public bool HasIndexes { get { return _indexBuffer != null; } }

    public void UpdateInstanceData(GpuInstanceData[] insts)
    {
      if (_instanceData == null)
      {
        _instanceData = Gpu.CreateUniformBuffer(Name + "-inst", insts);
      }
      else
      {
        _instanceData.CopyToGPU(insts);
      }
    }
    public GPUBuffer InstanceData
    {
      get
      {
        return _instanceData;
      }
    }

    #endregion
    #region Private: Members

    [DataMember] private GPUBuffer? _indexBuffer = null;
    [DataMember] private GPUBuffer? _faceData = null;
    [DataMember] private GPUBuffer? _instanceData = null;
    [DataMember] private List<GPUBuffer> _vertexBuffers = new List<GPUBuffer>();
    [DataMember] private SkinWeights? _skinWeights = null;
    [DataMember] private SoftBodyWeights? _softBodyWeights = null;
    [DataMember] private bool _boundBoxComputed = false;
    [DataMember] private Box3f _boundBoxExtent = new Box3f();
    [DataMember] private PrimitiveType _primitiveType = PrimitiveType.Triangles;

    #endregion
    #region Public: Methods

    protected MeshData(string name) : base(name) { }//clone/copy
    public MeshData(string name, PrimitiveType pt, GPUBuffer vertexBuffer, GPUBuffer? faceData = null, bool computeBoundBox = true) :
      this(name, pt, new List<GPUBuffer> { vertexBuffer }, null, faceData, computeBoundBox)
    {
    }
    public MeshData(string name, PrimitiveType pt, GPUBuffer vertexBuffer, GPUBuffer indexBuffer, GPUBuffer? faceData = null, bool computeBoundBox = true) :
      this(name, pt, new List<GPUBuffer> { vertexBuffer }, indexBuffer, faceData, computeBoundBox)
    {
    }
    public MeshData(string name, PrimitiveType pt, List<GPUBuffer> vertexBuffers, GPUBuffer indexBuffer, GPUBuffer? faceData = null, bool computeBoundBox = true) :
      this(name)
    {
      //Some checking
      Gu.Assert(vertexBuffers != null && vertexBuffers.Count > 0);
      Gu.Assert(faceData == null || faceData.BufferTarget == BufferTarget.ShaderStorageBuffer);
      Gu.Assert(indexBuffer == null || indexBuffer.BufferTarget == BufferTarget.ElementArrayBuffer);
      foreach (var vbo in vertexBuffers)
      {
        Gu.Assert(vbo != null && vbo.BufferTarget == BufferTarget.ArrayBuffer);
      }

      Name = name;//uh.. should be datablock?
      _primitiveType = pt;
      _vertexBuffers = vertexBuffers;
      _indexBuffer = indexBuffer;
      FaceData = faceData;

      GetDataForContext(Gu.Context);

      if (computeBoundBox)
      {
        ComputeBoundBox();
      }
    }
    public MeshData Clone()
    {
      var other = (MeshData)this.MemberwiseClone();

      other._indexBuffer = this._indexBuffer.Clone();
      other._faceData = this._faceData.Clone();
      other._vertexBuffers = new List<GPUBuffer>();
      foreach (var vb in this._vertexBuffers)
      {
        other._vertexBuffers.Add(vb.Clone());
      }
      return other;
    }

    #endregion
    #region Protected: Methods

    protected override VertexArrayObject CreateNew()
    {
      Gpu.CheckGpuErrorsDbg();
      var vao = new VertexArrayObject(this.Name + "-VAO");
      vao.Bind();

      GPUDataFormat fmtLast = null;

      foreach (var vb in _vertexBuffers)
      {
        vb.Bind();
        Gu.Assert(vb.Format != null);
        vb.Format.BindVertexAttribs(fmtLast);
        fmtLast = vb.Format;
      }
      if (_indexBuffer != null)
      {
        _indexBuffer.Bind();
        //_indexBuffer.Format.BindAttribs(null);
      }

      Gpu.CheckGpuErrorsDbg();
      vao.Unbind();

      GPUBuffer.UnbindBuffer(BufferTarget.ArrayBuffer);
      GPUBuffer.UnbindBuffer(BufferTarget.ElementArrayBuffer);

      return vao;
    }

    #endregion
    #region Private: Methods

    private void ComputeBoundBox()
    {
      _boundBoxExtent.genResetLimits();
      foreach (var vb in _vertexBuffers)
      {
        ComputeBoundBox(vb);
      }
      BoundBoxComputed = true;
    }
    private void ComputeBoundBox(GPUBuffer b)
    {
      Gu.Assert(b != null);
      Gu.Assert(b.Format != null);

      VertexPointer verts = new VertexPointer(b.CopyFromGPU(), b.Format);

      //Test

      if (HasIndexes)
      {
        VertexPointer inds = new VertexPointer(_indexBuffer.CopyFromGPU(), _indexBuffer.Format);
        for (int ii = 0; ii < inds.Length; ii++)
        {
          int off = (int)inds[ii].index;
          _boundBoxExtent.genExpandByPoint(verts[off]._v);
        }
      }
      else
      {
        for (int vi = 0; vi < verts.Length; vi++)
        {
          _boundBoxExtent.genExpandByPoint(verts[vi]._v);
        }
      }

    }

    #endregion

  }//MeshData





}//NS
