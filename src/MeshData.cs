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
  public class MeshView : DataBlock, IClone, ICopy<MeshView>, ISerializeBinary
  {
    // @class MeshView
    // @brief Subset of mesh
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

    //junk
    public static int dbg_numDrawElements_Frame = 0;
    public static int dbg_numDrawArrays_Frame = 0;
    private static long dbg_frame = 0;
    public bool dbg_break_render = false;

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

      if (Gu.Context.FrameStamp != dbg_frame)
      {
        dbg_frame = Gu.Context.FrameStamp;
        dbg_numDrawArrays_Frame = 0;
        dbg_numDrawElements_Frame = 0;
      }
      Gpu.CheckGpuErrorsDbg();
      vao.Bind();

      //*****
      if (Gu.BreakRenderState || dbg_break_render)
      {
        GpuDebugInfo.DebugGetRenderState(true);
        Gu.DebugBreak();
        Gu.BreakRenderState = false;
      }
      string n = "sky";
      if (_meshData.Name.ToLower().Contains(n))
      {
        Gu.Trap();
      }
      //*****

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
          dbg_numDrawElements_Frame++;
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
          dbg_numDrawElements_Frame++;
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
            dbg_numDrawArrays_Frame++;
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
            dbg_numDrawArrays_Frame++;
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
    public object? Clone(bool? shallow = null)
    {
      var other = new MeshView();
      other.CopyFrom(this, shallow);
      return other;
    }
    public void CopyFrom(MeshView? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      base.CopyFrom(other);
      this._meshData = other._meshData;
      this._start = other._start;
      this._count = other._count;
    }
    public override void Serialize(BinaryWriter bw)
    {
      Gu.BRThrowNotImplementedException();
      base.Serialize(bw);
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      Gu.BRThrowNotImplementedException();
      base.Deserialize(br, version);
    }

  }
  [DataContract]
  public class MeshData : OpenGLContextDataManager<VertexArrayObject>, IClone, ICopy<MeshData>
  {
    // @class MeshData
    // @brief Manages mesh data and meshes among GL contexts.

    #region Public: Members

    public DrawMode DrawMode { get { return _drawMode; } set { _drawMode = value; } }
    public DrawOrder DrawOrder { get { return _drawOrder; } set { _drawOrder = value; } }
    public bool BoundBoxComputed { get { return _boundBoxComputed; } private set { _boundBoxComputed = value; } }
    public Box3f BoundBox_Extent { get { return _boundBoxExtent; } } //Bond box of mesh extenss
    public PrimitiveType PrimitiveType { get { return _primitiveType; } }
    public List<GPUBuffer> VertexBuffers { get { return _vertexBuffers; } }
    public GPUBuffer IndexBuffer { get { return _indexBuffer; } }
    public GPUBuffer FaceData { get { return _faceData; } private set { _faceData = value; } }
    public bool HasIndexes { get { return _indexBuffer != null; } }
    public static MeshData DefaultBox
    {
      get
      {
        if (_defaultBox == null)
        {
          _defaultBox = Gu.Lib.LoadMesh(RName.Mesh_DefaultBox, new MeshGenBoxParams() { _w = 1, _h = 1, _d = 1 });
        }
        return _defaultBox;
      }
    }

    #endregion
    #region Private: Members

    private static MeshData? _defaultBox = null;
    [DataMember] private GPUBuffer _indexBuffer = null;
    [DataMember] private List<GPUBuffer> _vertexBuffers = null;
    [DataMember] private bool _boundBoxComputed = false;
    [DataMember] private Box3f _boundBoxExtent = new Box3f();
    [DataMember] private PrimitiveType _primitiveType = PrimitiveType.Triangles;
    [DataMember] private DrawMode _drawMode = DrawMode.Deferred;
    [DataMember] private DrawOrder _drawOrder = DrawOrder.Mid; //This is a sloppy ordered draw routine to prevent depth test issues. In the future it goes away in favor of a nicer draw routine.
    [DataMember] private GPUBuffer _faceData = null;

    #endregion
    #region Public: Methods

    protected MeshData(string name) : base(name) { }//clone/copy
    public MeshData(string name, PrimitiveType pt, GPUBuffer vertexBuffer, GPUBuffer faceData = null, bool computeBoundBox = true) :
      this(name, pt, new List<GPUBuffer> { vertexBuffer }, null, faceData, computeBoundBox)
    {
    }
    public MeshData(string name, PrimitiveType pt, GPUBuffer vertexBuffer, GPUBuffer indexBuffer, GPUBuffer faceData = null, bool computeBoundBox = true) :
      this(name, pt, new List<GPUBuffer> { vertexBuffer }, indexBuffer, faceData, computeBoundBox)
    {
    }
    public MeshData(string name, PrimitiveType pt, List<GPUBuffer> vertexBuffers, GPUBuffer indexBuffer, GPUBuffer faceData = null, bool computeBoundBox = true) :
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
    public object? Clone(bool? shallow = null)
    {
      var other = new MeshData(this.Name);
      other.CopyFrom(this, shallow);
      return other;
    }
    public void CopyFrom(MeshData? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      base.CopyFrom(other);

      //TODO: clone the index / vbo and vao
      this.Name = other.Name;
      if (shallow != null && shallow.Value == true)
      {
        this._indexBuffer = other._indexBuffer;
        this._vertexBuffers = new List<GPUBuffer>(other._vertexBuffers);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      this._boundBoxExtent = other._boundBoxExtent;
      this._primitiveType = other._primitiveType;

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
      int voff = b.Format.GetComponentOffset(ShaderVertexType.v3, 1);
      if (voff >= 0)
      {
        var vertexes = b.CopyFromGPU();

        unsafe
        {
          //Yeah, this is fun. Loop verts / indexes by casting bytes.
          fixed (byte* vbarr = vertexes.Bytes)
          {
            if (HasIndexes)
            {
              var indexes = _indexBuffer.CopyFromGPU();
              fixed (byte* ibarr = indexes.Bytes)
              {
                for (int ii = 0; ii < indexes.Count; ++ii)
                {
                  int index = 0;
                  if (indexes.ItemSizeBytes == 2)
                  {
                    index = Convert.ToInt32(*((ushort*)(ibarr + ii * indexes.ItemSizeBytes)));
                  }
                  else if (indexes.ItemSizeBytes == 4)
                  {
                    index = *((int*)(ibarr + ii * indexes.ItemSizeBytes));
                  }
                  else
                  {
                    Gu.BRThrowException("Invalid index type.");
                  }
                  if (index >= vertexes.Count)
                  {
                    Gu.BRThrowException("Index outside range.");
                  }
                  vec3 vv = *((vec3*)(vbarr + index * vertexes.ItemSizeBytes + voff));
                  _boundBoxExtent.genExpandByPoint(vv);
                }
              }
            }
            else
            {
              for (int vi = 0; vi < vertexes.Count; ++vi)
              {
                vec3 vv = *((vec3*)(vbarr + vi * vertexes.ItemSizeBytes + voff));
                _boundBoxExtent.genExpandByPoint(vv);
              }
            }//if hasindexes
          }//fixed
        }//unsafe
      }
      else
      {
        Gu.Log.Warn("Could not compute bound box for mesh " + this.Name + " No default position data supplied.");
      }
    }

    #endregion

  }//MeshData





}//NS
