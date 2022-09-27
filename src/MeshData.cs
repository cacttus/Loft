using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Runtime.InteropServices;


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
    public VertexArrayObject(string name) : base(name + "-mesh")
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

  public class MeshDataBlock
  {
    public VertexArrayObject VAO { get; set; } = null;
    private WeakReference<MeshData> _meshData = null;
    public MeshDataBlock(MeshData parent)
    {
      _meshData = new WeakReference<MeshData>(parent);
    }
  }

  public class MeshData : OpenGLContextDataManager<VertexArrayObject>
  {
    private GPUBuffer _indexBuffer = null;
    private List<GPUBuffer> _vertexBuffers = null;
    private Box3f _boundBoxExtent = new Box3f();
    private PrimitiveType _primitiveType = PrimitiveType.Triangles;

    public DrawMode DrawMode { get; set; } = DrawMode.Deferred;
    public DrawOrder DrawOrder { get; set; } = DrawOrder.Mid; //This is a sloppy ordered draw routine to prevent depth test issues. In the future it goes away in favor of a nicer draw routine.
    public bool BoundBoxComputed { get; private set; } = false;
    public Box3f BoundBox_Extent { get { return _boundBoxExtent; } } //Bond box of mesh extenss
    public PrimitiveType PrimitiveType { get { return _primitiveType; } }
    public List<GPUBuffer> VertexBuffers { get { return _vertexBuffers; } }
    public GPUBuffer FaceData { get; private set; } = null;
    public bool HasIndexes { get { return _indexBuffer != null; } }
    public string Name { get; protected set; }

    private static StaticContextData<MeshData> _defaultBox = new StaticContextData<MeshData>();
    public static MeshData DefaultBox//DefaultCube
    {
      get
      {
        MeshData box = _defaultBox.Get();
        if (box == null)
        {
          box = MeshData.GenBox(1, 1, 1);
          _defaultBox.Set(box);
        }
        return _defaultBox.Get();
      }
    }

    public static int dbg_numDrawElements_Frame = 0;
    public static int dbg_numDrawArrays_Frame = 0;
    private static long dbg_frame = 0;

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

    protected MeshData() { }
    public MeshData(string name, PrimitiveType pt, GPUBuffer vertexBuffer, GPUBuffer faceData = null, bool computeBoundBox = true) :
      this(name, pt, new List<GPUBuffer> { vertexBuffer }, null, faceData, computeBoundBox)
    {
    }
    public MeshData(string name, PrimitiveType pt, GPUBuffer vertexBuffer, GPUBuffer indexBuffer, GPUBuffer faceData = null, bool computeBoundBox = true) :
      this(name, pt, new List<GPUBuffer> { vertexBuffer }, indexBuffer, faceData, computeBoundBox)
    {
    }
    public MeshData(string name, PrimitiveType pt, List<GPUBuffer> vertexBuffers, GPUBuffer indexBuffer, GPUBuffer faceData = null, bool computeBoundBox = true)
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
      var other = new MeshData();

      //TODO: clone the index / vbo and vao
      Gu.BRThrowNotImplementedException();
      other.Name = Name;
      other._indexBuffer = _indexBuffer;
      //**NOTE: this is not a deep copy
      other._vertexBuffers = new List<GPUBuffer>(this._vertexBuffers);

      other._boundBoxExtent = _boundBoxExtent;
      other._primitiveType = _primitiveType;

      return other;
    }
    public bool DebugBreakRender = false;

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
    public static ushort[] GenerateQuadIndices(int numQuads, bool flip = false)
    {
      //Generate proper winding quad indexes
      //0  1
      //2  3
      ushort idx = 0;
      ushort[] inds = new ushort[numQuads * 6];
      for (int face = 0; face < numQuads; ++face)
      {
        inds[face * 6 + 0] = (ushort)(idx + 0);
        inds[face * 6 + 1] = (ushort)(idx + (flip ? 2 : 3));
        inds[face * 6 + 2] = (ushort)(idx + (flip ? 3 : 2));
        inds[face * 6 + 3] = (ushort)(idx + 0);
        inds[face * 6 + 4] = (ushort)(idx + (flip ? 3 : 1));
        inds[face * 6 + 5] = (ushort)(idx + (flip ? 1 : 3));
        idx += 4;
      }
      return inds;
    }
    public static MeshData GenPlane(float w, float h, vec2[] side = null, string name = "generated-plane")
    {
      //Left Righ, Botom top, back front
      vec3[] box = new vec3[4];
      float w2 = w * 0.5f;
      float h2 = h * 0.5f;
      box[0] = new vec3(-w2, 0, -h2);
      box[1] = new vec3(w2, 0, -h2);
      box[2] = new vec3(-w2, 0, h2);
      box[3] = new vec3(w2, 0, h2);

      vec3[] norms = new vec3[1];//lrbtaf
      norms[0] = new vec3(0, 1, 0);

      vec2[] texs = new vec2[4];
      texs[0] = new vec2(0, 0);
      texs[1] = new vec2(1, 0);
      texs[2] = new vec2(0, 1);
      texs[3] = new vec2(1, 1);

      v_v3n3x2t3u1[] verts = new v_v3n3x2t3u1[4];
      verts[0 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[0], _n = norms[0], _x = (side != null) ? side[0] : texs[0] };
      verts[0 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[1], _n = norms[0], _x = (side != null) ? side[1] : texs[1] };
      verts[0 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[2], _n = norms[0], _x = (side != null) ? side[2] : texs[2] };
      verts[0 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[3], _n = norms[0], _x = (side != null) ? side[3] : texs[3] };

      ushort[] qinds = GenerateQuadIndices(verts.Length / 4, false);
      var fd = ComputeNormalsAndTangents(verts, qinds.AsUIntArray());

      return new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts),
        Gpu.CreateIndexBuffer(name, qinds),
        Gpu.CreateShaderStorageBuffer(name, fd)
        );
    }
    public static void GenBoxVerts(ref v_v3n3x2t3u1[] verts, float w, float h, float d,
    vec2[]? top = null, vec2[]? side = null, vec2[]? bot = null, vec3? translate = null, bool origin_bot_left = false)
    {
      //Create box verts ADDing to the array if is not creatd, or making a new one.
      // translate = translate the box.
      //origin_bot_left - if true, origin is moved to bot left, otherwise it is center

      //Left Righ, Botom top, back front
      vec3[] box = new vec3[8];
      float w2 = w * 0.5f, h2 = h * 0.5f, d2 = d * 0.5f;
      box[0] = new vec3(-w2, -h2, -d2);
      box[1] = new vec3(w2, -h2, -d2);
      box[2] = new vec3(-w2, h2, -d2);
      box[3] = new vec3(w2, h2, -d2);
      box[4] = new vec3(-w2, -h2, d2);
      box[5] = new vec3(w2, -h2, d2);
      box[6] = new vec3(-w2, h2, d2);
      box[7] = new vec3(w2, h2, d2);

      if (origin_bot_left)
      {
        for (var bi = 0; bi < 8; ++bi)
        {
          box[bi].x += w2;
          box[bi].y += h2;
          box[bi].z += d2;
        }
      }

      vec3[] norms = new vec3[6];//lrbtaf
      norms[0] = new vec3(-1, 0, 0);
      norms[1] = new vec3(1, 0, 0);
      norms[2] = new vec3(0, -1, 0);
      norms[3] = new vec3(0, 1, 0);
      norms[4] = new vec3(0, 0, -1);
      norms[5] = new vec3(0, 0, 1);

      vec2[] texs = new vec2[4];
      texs[0] = new vec2(0, 1);
      texs[1] = new vec2(1, 1);
      texs[2] = new vec2(0, 0);
      texs[3] = new vec2(1, 0);

      //     6       7
      // 2      3
      //     4       5
      // 0      1
      int vertCount = 6 * 4;
      int off=0;
      if (verts == null)
      {
        verts = new v_v3n3x2t3u1[vertCount];//lrbtaf
      }
      else
      {
        off = verts.Length;
        Array.Resize(ref verts, verts.Length + vertCount);
      }
      verts[off + 0 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[4], _n = norms[0], _x = (side != null) ? side[0] : texs[0] };
      verts[off + 0 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[0], _n = norms[0], _x = (side != null) ? side[1] : texs[1] };
      verts[off + 0 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[6], _n = norms[0], _x = (side != null) ? side[2] : texs[2] };
      verts[off + 0 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[2], _n = norms[0], _x = (side != null) ? side[3] : texs[3] };

      verts[off + 1 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[1], _n = norms[1], _x = (side != null) ? side[0] : texs[0] };
      verts[off + 1 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[5], _n = norms[1], _x = (side != null) ? side[1] : texs[1] };
      verts[off + 1 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[3], _n = norms[1], _x = (side != null) ? side[2] : texs[2] };
      verts[off + 1 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[7], _n = norms[1], _x = (side != null) ? side[3] : texs[3] };

      verts[off + 2 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[4], _n = norms[2], _x = (bot != null) ? bot[0] : texs[0] };
      verts[off + 2 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[5], _n = norms[2], _x = (bot != null) ? bot[1] : texs[1] };
      verts[off + 2 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[0], _n = norms[2], _x = (bot != null) ? bot[2] : texs[2] };
      verts[off + 2 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[1], _n = norms[2], _x = (bot != null) ? bot[3] : texs[3] };

      verts[off + 3 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[2], _n = norms[3], _x = (top != null) ? top[0] : texs[0] };
      verts[off + 3 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[3], _n = norms[3], _x = (top != null) ? top[1] : texs[1] };
      verts[off + 3 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[6], _n = norms[3], _x = (top != null) ? top[2] : texs[2] };
      verts[off + 3 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[7], _n = norms[3], _x = (top != null) ? top[3] : texs[3] };

      verts[off + 4 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[0], _n = norms[4], _x = (side != null) ? side[0] : texs[0] };
      verts[off + 4 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[1], _n = norms[4], _x = (side != null) ? side[1] : texs[1] };
      verts[off + 4 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[2], _n = norms[4], _x = (side != null) ? side[2] : texs[2] };
      verts[off + 4 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[3], _n = norms[4], _x = (side != null) ? side[3] : texs[3] };

      verts[off + 5 * 4 + 0] = new v_v3n3x2t3u1() { _v = box[5], _n = norms[5], _x = (side != null) ? side[0] : texs[0] };
      verts[off + 5 * 4 + 1] = new v_v3n3x2t3u1() { _v = box[4], _n = norms[5], _x = (side != null) ? side[1] : texs[1] };
      verts[off + 5 * 4 + 2] = new v_v3n3x2t3u1() { _v = box[7], _n = norms[5], _x = (side != null) ? side[2] : texs[2] };
      verts[off + 5 * 4 + 3] = new v_v3n3x2t3u1() { _v = box[6], _n = norms[5], _x = (side != null) ? side[3] : texs[3] };

      if (translate != null)
      {
        for (var vi = off; vi < verts.Length; vi++)
        {
          verts[vi]._v += translate.Value;
        }
      }

    }
    public static MeshData GenBox(float w, float h, float d, vec2[] top = null, vec2[] side = null, vec2[] bot = null, string name = "generated-box", vec3? translate = null)
    {
      v_v3n3x2t3u1[]? verts = null;
      GenBoxVerts(ref verts, w, h, d, top, side, bot, translate);

      ushort[] qinds = GenerateQuadIndices(verts.Length / 4, false);
      var fd = ComputeNormalsAndTangents(verts, qinds.AsUIntArray());

      return new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts),
        Gpu.CreateIndexBuffer(name, qinds),
        Gpu.CreateShaderStorageBuffer(name, fd)
        );
    }
    public static MeshData GenSphere(float radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false, string name = "gen-ellipsoid")
    {
      return GenEllipsoid(new vec3(radius, radius, radius), slices, stacks, smooth, flip_tris, name);
    }
    public static MeshData GenEllipsoid(vec3 radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false, string name = "gen-ellipsoid")
    {
      v_v3n3x2t3u1[] verts;
      ushort[] inds;

      GenEllipsoid(out verts, out inds, radius, slices, stacks, smooth, flip_tris);

      var fd = ComputeNormalsAndTangents(verts, inds.AsUIntArray());

      return new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts),
        Gpu.CreateIndexBuffer(name, inds),
        Gpu.CreateShaderStorageBuffer(name, fd)
        );
    }
    public static void GenEllipsoid(out v_v3n3x2t3u1[] verts, out ushort[] inds, vec3 radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false)
    {
      int vcount = slices * stacks * 4;
      verts = new v_v3n3x2t3u1[vcount];

      //Use a 2D grid as a sphere. This is less optimal but doesn't mess up the tex coords.
      for (int stack = 0; stack < stacks; stack++)
      {
        for (int slice = 0; slice < slices; slice++)
        {
          float[] phi = new float[2];
          float[] theta = new float[2];
          phi[0] = MathUtils.M_PI * ((float)stack / (float)stacks);
          phi[1] = MathUtils.M_PI * ((float)(stack + 1) / (float)stacks); //0<phi<pi
          theta[0] = MathUtils.M_2PI * ((float)slice / (float)slices);
          theta[1] = MathUtils.M_2PI * ((float)(slice + 1) / (float)slices);//0<theta<2pi

          int vind = (stack * slices + slice) * 4;
          for (int p = 0; p < 2; ++p)
          {
            for (int t = 0; t < 2; ++t)
            {
              // 2 3
              // 0 1  
              // >x ^y
              int voff = vind + p * 2 + t;

              verts[voff]._v = new vec3(
                  radius.x * MathUtils.sinf(phi[p]) * MathUtils.cosf(theta[t]),
                  radius.y * MathUtils.cosf(phi[p]),
                  radius.z * MathUtils.sinf(phi[p]) * MathUtils.sinf(theta[t])
              );
              verts[voff]._n = verts[voff]._v.normalized();

              //del f = x2/a2+y2/b2+z2/c2=1
              verts[voff]._n = new vec3(
                  2.0f * MathUtils.sinf(phi[p]) * MathUtils.cosf(theta[t]) / radius.x * radius.x,
                  2.0f * MathUtils.cosf(phi[p]) / radius.y * radius.y,
                  2.0f * MathUtils.sinf(phi[p]) * MathUtils.sinf(theta[t]) / radius.z * radius.z
              ).normalized();

            }
          }

          if (smooth)
          {
            //TODO:
            // verts[vind + 0]._n = verts[vind + 0]._v.normalized();
            // verts[vind + 1]._n = verts[vind + 1]._v.normalized();
            // verts[vind + 2]._n = verts[vind + 2]._v.normalized();
            // verts[vind + 3]._n = verts[vind + 3]._v.normalized();
          }
          else
          {
            vec3 n = (verts[vind + 1]._v - verts[vind + 0]._v).cross(verts[vind + 2]._v - verts[vind + 0]._v).normalized();
            verts[vind + 0]._n = n;
            verts[vind + 1]._n = n;
            verts[vind + 2]._n = n;
            verts[vind + 3]._n = n;
          }

          //Caps
          float tx0 = (float)slice / (float)slices;
          float ty0 = (float)stack / (float)stacks;
          float tx1 = (float)(slice + 1) / (float)slices;
          float ty1 = (float)(stack + 1) / (float)stacks;
          verts[vind + 0]._x = new vec2(tx0, ty0);
          verts[vind + 1]._x = new vec2(tx1, ty0);
          verts[vind + 2]._x = new vec2(tx0, ty1);
          verts[vind + 3]._x = new vec2(tx1, ty1);

        }
      }

      inds = GenerateQuadIndices(verts.Length / 4, !flip_tris);
    }
    public static MeshData CreateScreenQuadMesh(float fw, float fh, string name = "screenquad")
    {
      v_v3x2[] verts = new v_v3x2[] {
        new v_v3x2() { _v = new vec3(0, 0, 0), _x = new vec2(0, 1) } ,
        new v_v3x2() { _v = new vec3(fw, 0, 0), _x = new vec2(1, 1)} ,
        new v_v3x2() { _v = new vec3(0, fh, 0), _x = new vec2(0, 0)} ,
        new v_v3x2() { _v = new vec3(fw, fh, 0), _x = new vec2(1, 0)} ,
      };

      ushort[] inds = new ushort[] { 0, 1, 3, 0, 3, 2, };

      return new MeshData(name, PrimitiveType.Triangles,
        Gpu.CreateVertexBuffer(name, verts.ToArray()),
        Gpu.CreateIndexBuffer(name, inds),
        null,
        true
        );
    }
    public static GpuFaceData[] ComputeNormalsAndTangents(object verts_in, uint[] inds = null, bool doNormals = true, bool doTangents = true)
    {
      Gu.Assert(inds != null);
      Gu.Assert(verts_in != null);

      VertexPointer verts = new VertexPointer(verts_in);

      GpuFaceData[] faceData = null;
      if (inds.Length == 0)
      {
        return null;
      }
      int ilen = (inds != null ? inds.Length : verts.Length);
      if (ilen % 3 != 0)
      {
        Gu.Log.Error("Index or vertex length was not a multiple of 3");
      }
      faceData = new GpuFaceData[ilen / 3];
      float[] count = new float[verts.Length];
      for (int vi = 0; vi < verts.Length; vi++)
      {
        count[vi] = 0;
        if (doNormals)
        {
          verts[vi]._n = vec3.Zero;
        }
        if (doTangents)
        {
          verts[vi]._t = vec3.Zero;
        }
      }
      for (int vi = 0; vi < ilen; vi += 3)
      {
        int vi0 = 0, vi1 = 0, vi2 = 0;
        if (inds != null)
        {
          vi0 = (int)inds[vi + 0];
          vi1 = (int)inds[vi + 1];
          vi2 = (int)inds[vi + 2];
        }
        else
        {
          vi0 = vi + 0;
          vi1 = vi + 1;
          vi2 = vi + 2;
        }

        vec3 out_n, out_t;
        VertexFormat.ComputeNormalAndTangent(
          verts[vi0]._v, verts[vi1]._v, verts[vi2]._v,
          verts[vi0]._x, verts[vi1]._x, verts[vi2]._x,
          out out_n, out out_t);
        uint faceId = (uint)(vi/3);

        faceData[faceId]._index = faceId;
        faceData[faceId]._normal = out_n;
        faceData[faceId]._tangent = out_t;

        if (doNormals)
        {
          verts[vi0]._n += out_n;
          verts[vi1]._n += out_n;
          verts[vi2]._n += out_n;
        }
        if (doTangents)
        {
          verts[vi0]._t += out_t;
          verts[vi1]._t += out_t;
          verts[vi2]._t += out_t;
        }
        verts[vi0]._u = faceId;
        verts[vi1]._u = faceId;
        verts[vi2]._u = faceId;

        count[vi0] += 1;
        count[vi1] += 1;
        count[vi2] += 1;
      }

      //Average vertex normals and tangents
      for (int vi = 0; vi < verts.Length; vi++)
      {
        if (doTangents)
        {
          if (count[vi] > 0)
          {
            verts[vi]._t = (verts[vi]._t / count[vi]).normalize();
          }
          else
          {
            verts[vi]._t = vec3.Zero;
          }
        }
        if (doNormals)
        {
          if (count[vi] > 0)
          {
            verts[vi]._n = (verts[vi]._n / count[vi]).normalize();
          }
          else
          {
            verts[vi]._n = vec3.Zero;
          }
        }
      }

      return faceData;
    }



  }//MeshData





}//NS
