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
    First,
    Mid,
    Last,
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
      _glId = GL.GenVertexArray();
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
      GL.DeleteVertexArray(_glId);
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
  [DataContract] [Serializable]
  public class MeshData : OpenGLContextDataManager<VertexArrayObject>, IClone, ICopy<MeshData>
  {
    [NonSerialized] private static StaticContextData<MeshData> _defaultBox = new StaticContextData<MeshData>();
    [DataMember] private GPUBuffer _indexBuffer = null;
    [DataMember] private List<GPUBuffer> _vertexBuffers = null;
    [DataMember] private bool _boundBoxComputed = false;
    [DataMember] private Box3f _boundBoxExtent = new Box3f();
    [DataMember] private PrimitiveType _primitiveType = PrimitiveType.Triangles;
    [DataMember] public DrawMode _drawMode = DrawMode.Deferred;
    [DataMember] public DrawOrder _drawOrder = DrawOrder.Mid; //This is a sloppy ordered draw routine to prevent depth test issues. In the future it goes away in favor of a nicer draw routine.
    [DataMember] public GPUBuffer _faceData = null;

    public DrawMode DrawMode { get { return _drawMode; } set { _drawMode = value; } }
    public DrawOrder DrawOrder { get { return _drawOrder; } set { _drawOrder = value; } }//This is a sloppy ordered draw routine to prevent depth test issues. In the future it goes away in favor of a nicer draw routine.
    public bool BoundBoxComputed { get { return _boundBoxComputed; } private set { _boundBoxComputed = value; } }
    public Box3f BoundBox_Extent { get { return _boundBoxExtent; } } //Bond box of mesh extenss
    public PrimitiveType PrimitiveType { get { return _primitiveType; } }
    public List<GPUBuffer> VertexBuffers { get { return _vertexBuffers; } }
    public GPUBuffer FaceData { get { return _faceData; } private set { _faceData = value; } }
    public bool HasIndexes { get { return _indexBuffer != null; } }
    public string Name { get; protected set; }

    public static MeshData DefaultBox//DefaultCube
    {
      get
      {
        MeshData box = _defaultBox.Get();
        if (box == null)
        {
          box = Gu.Lib.LoadMesh(RName.Mesh_DefaultBox, new MeshGenBoxParams(){_w=1,_h=1,_d=1});
          _defaultBox.Set(box);
        }
        return _defaultBox.Get();
      }
    }

    [NonSerialized] public static int dbg_numDrawElements_Frame = 0;
    [NonSerialized] public static int dbg_numDrawArrays_Frame = 0;
    [NonSerialized] private static long dbg_frame = 0;
    protected override VertexArrayObject CreateNew()
    {
      Gpu.CheckGpuErrorsDbg();
      var vao = new VertexArrayObject(this.Name + "-VAO");
      vao.Bind();

      VertexFormat fmtLast = null;

      foreach (var vb in _vertexBuffers)
      {
        vb.Bind();
        Gu.Assert(vb.Format != null);
        vb.Format.BindAttribs(fmtLast);
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

    //We will not have an empty serialized constructor, because meshes are loaded from disk, or generated.
    private MeshData(string name) : base(name) { }
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
    [NonSerialized] public bool DebugBreakRender = false;

    public void Draw(GpuInstanceData[] instances, OpenTK.Graphics.OpenGL4.PrimitiveType? primTypeOverride = null)
    {
      //@param instances - Can be null in which case we draw a mesh without an accompanying instance transform

      var vao = this.GetDataForContext(Gu.Context);

      if (Gu.Context.FrameStamp != dbg_frame)
      {
        dbg_frame = Gu.Context.FrameStamp;
        dbg_numDrawArrays_Frame = 0;
        dbg_numDrawElements_Frame = 0;
      }
      Gpu.CheckGpuErrorsDbg();
      vao.Bind();

      //*****
      if (Gu.BreakRenderState || DebugBreakRender)
      {
        GpuDebugInfo.DebugGetRenderState(true);
        Gu.DebugBreak();
        Gu.BreakRenderState = false;
      }
      //*****

      OpenTK.Graphics.OpenGL4.PrimitiveType primType = this.PrimitiveType;
      if (primTypeOverride != null)
      {
        primType = primTypeOverride.Value;
      }

      //This is assuming the VAO and all other bindings are already called.
      if (HasIndexes)
      {
        if (_indexBuffer != null)
        {
          Gu.Assert(_indexBuffer.ItemSizeBytes == 2 || _indexBuffer.ItemSizeBytes == 4);

          if (instances != null && instances.Length > 0)
          {
            GL.DrawElementsInstanced(primType,
              _indexBuffer.ItemCount,
              _indexBuffer.DrawElementsType,
              IntPtr.Zero,
              instances.Length
              );
            Gpu.CheckGpuErrorsDbg();
            dbg_numDrawElements_Frame++;
          }
          else
          {
            //This shouldn't happen anymore (right now) testing out the entire instancing system
            Gu.DebugBreak();

            Gu.Log.ErrorCycle("Instances were not specified for mesh " + this.Name);
            GL.DrawElements(primType,
              _indexBuffer.ItemCount,
              _indexBuffer.DrawElementsType,
              IntPtr.Zero
              );
            Gpu.CheckGpuErrorsDbg();
            dbg_numDrawElements_Frame++;
          }
        }
        else
        {
          Gu.Log.Error("Indexes specified for mesh but index buffer was null. Skipping draw.");
        }
      }
      else
      {

        foreach (var vb in _vertexBuffers)
        {
          if (instances != null && instances.Length > 0)
          {
            GL.DrawArraysInstanced(PrimitiveType, 0, vb.ItemCount, instances.Length);
            Gpu.CheckGpuErrorsDbg();
            dbg_numDrawArrays_Frame++;
          }
          else
          {
            //This shouldn't happen anymore (right now) testing out the entire instancing system
            Gu.DebugBreak();

            Gu.Log.ErrorCycle("Instances were not specified for mesh " + this.Name);
            GL.DrawArrays(PrimitiveType, 0, vb.ItemCount);
            Gpu.CheckGpuErrorsDbg();
            dbg_numDrawArrays_Frame++;
          }
        }
      }
      vao.Unbind();
    }
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
      int voff = b.Format.GetComponentOffset(VertexComponentType.v3_01);
      if (voff >= 0)
      {
        var vertexes = b.CopyDataFromGPU();

        unsafe
        {
          //Yeah, this is fun. Loop verts / indexes by casting bytes.
          fixed (byte* vbarr = vertexes.Bytes)
          {
            if (HasIndexes)
            {
              var indexes = _indexBuffer.CopyDataFromGPU();
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



  }//MeshData





}//NS
