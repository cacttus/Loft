using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace PirateCraft
{
  public abstract class DataBlock
  {
    private static int IdGen = 1;
    public string Name { get; private set; }
    public int Id { get; private set; }
    public DataBlock(string name)
    {
      Id = IdGen++;
      Name = name;
    }
  }
  public enum DrawOrder
  {
    First,
    Mid,
    Last,
    MaxDrawOrders
  }
  public class VertexArrayObject : OpenGLResource
  {
    public VertexArrayObject()
    {
      _glId = GL.GenVertexArray();
      GL.BindVertexArray(_glId);
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
  public class MeshData : DataBlock
  {
    private GPUBuffer _indexBuffer = null;
    private GPUBuffer _vertexBuffer = null;
    private VertexArrayObject _vao = null;
    private Box3f _boundBoxExtent = new Box3f();
    private VertexFormat _vertexFormat = null;
    private IndexFormat _indexFormat = null;
    private PrimitiveType _primitiveType = PrimitiveType.Triangles;
    private IndexFormatType _indexFmt;

    //private int _intVaoId = 0;
    public DrawOrder DrawOrder { get; set; } = DrawOrder.Mid; //This is a sloppy ordered draw routine to prevent depth test issues. In the future it goes away in favor of a nicer draw routine.
    public bool BoundBoxComputed { get; private set; } = false;
    public Box3f BoundBox_Extent { get { return _boundBoxExtent; } } //Bond box of mesh extenss
    public VertexFormat VertexFormat { get { return _vertexFormat; } }
    public IndexFormat IndexFormat { get { return _indexFormat; } } //If null, then there is no indexes associated with this mesh.
    public PrimitiveType PrimitiveType { get { return _primitiveType; } }
    public bool HasIndexes
    {
      get
      {
        return IndexFormat != null;
      }
    }
    public MeshData(string name, PrimitiveType pt, VertexFormat fmt, IndexFormatType ifmt = IndexFormatType.None) : base(name)
    {
      Gu.Assert(fmt != null);
      _primitiveType = pt;
      _vertexFormat = fmt;
      _indexFmt = ifmt;
      if (ifmt == IndexFormatType.None)
      {
        _indexFormat = null;
      }
      else if (ifmt == IndexFormatType.Uint32)
      {
        _indexFormat = IndexFormat.IFMT_U32;
      }
      else if (ifmt == IndexFormatType.Uint16)
      {
        _indexFormat = IndexFormat.IFMT_U16;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    public MeshData(string name, PrimitiveType pt, VertexFormat fmt, GpuDataPtr verts, IndexFormatType ifmt = IndexFormatType.None, GpuDataPtr indexes = null, bool computeBoundBox = true) : this(name, pt, fmt, ifmt)
    {
      _primitiveType = pt;
      if (verts == null || (ifmt != IndexFormatType.None && indexes == null))
      {
        Gu.Log.Error("MeshData creation: Error: vertexes or indexes are null and were required.");
      }

      CreateBuffers(verts, indexes, computeBoundBox);
    }

    public MeshData Clone()
    {
      var other = new MeshData(Name, PrimitiveType, VertexFormat, _indexFmt);

      //TODO: clone the index / vbo and vao
      Gu.BRThrowNotImplementedException();
      other._indexBuffer = _indexBuffer;
      other._vertexBuffer = _vertexBuffer;
      other._vao = _vao;

      other._boundBoxExtent = _boundBoxExtent;
      other._vertexFormat = _vertexFormat;
      other._indexFormat = _indexFormat;
      other._primitiveType = _primitiveType;

      return other;
    }

    public static int dbg_numDrawElements_Frame = 0;
    public static int dbg_numDrawArrays_Frame = 0;
    private static long dbg_frame = 0;
    public void Draw(mat4[] instances = null)
    {
      if (Gu.Context.FrameStamp != dbg_frame)
      {
        dbg_frame = Gu.Context.FrameStamp;
        dbg_numDrawArrays_Frame = 0;
        dbg_numDrawElements_Frame = 0;
      }
      Gpu.CheckGpuErrorsDbg();
      _vao.Bind();

      //This is assuming the VAO and all other bindings are already called.
      if (HasIndexes)
      {
        if (_indexBuffer != null)
        {
          if (instances != null && instances.Length > 0)
          {
            GL.DrawElementsInstanced(PrimitiveType,
              _indexBuffer.ItemCount,
              _indexBuffer.ItemSize == 2 ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt,
              IntPtr.Zero,
              instances.Length
              );
            Gpu.CheckGpuErrorsDbg();
            dbg_numDrawElements_Frame++;
          }
          else
          {
            GL.DrawElements(PrimitiveType,
              _indexBuffer.ItemCount,
              _indexBuffer.ItemSize == 2 ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt,
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
        if (instances != null && instances.Length > 0)
        {
          GL.DrawArraysInstanced(PrimitiveType, 0, _vertexBuffer.ItemCount, instances.Length);
          Gpu.CheckGpuErrorsDbg();
          dbg_numDrawArrays_Frame++;
        }
        else
        {
          GL.DrawArrays(PrimitiveType, 0, _vertexBuffer.ItemCount);
          Gpu.CheckGpuErrorsDbg();
          dbg_numDrawArrays_Frame++;
        }
      }
      _vao.Unbind();
    }
    public void CreateBuffers(GpuDataPtr verts, GpuDataPtr indexes, bool computeBoundBox)
    {
      Gu.Assert(this.VertexFormat.VertexSizeBytes == verts.ItemSizeBytes);

      Gpu.CheckGpuErrorsDbg();
      _vao = new VertexArrayObject();
      _vao.Bind();

      if (verts != null)
      {
        _vertexBuffer = new GPUBuffer(BufferTarget.ArrayBuffer, verts);
        _vertexBuffer.Bind();
      }
      if (HasIndexes && indexes != null)
      {
        _indexBuffer = new GPUBuffer(BufferTarget.ElementArrayBuffer, indexes);
        _indexBuffer.Bind();
      }

      //Note: we use vec4 size offsets here because of the 16 byte padding required by GPUs.
      foreach (var comp in VertexFormat.Components)
      {
        GL.EnableVertexAttribArray(comp.Value.AttribLocation);
        GL.VertexAttribPointer(comp.Value.AttribLocation, comp.Value.ComponentCount,
        comp.Value.DataType, false, VertexFormat.VertexSizeBytes, (IntPtr)(0 + comp.Value.ByteOffset));
        int n = 0;
        n++;
      }

      Gpu.CheckGpuErrorsDbg();
      _vao.Unbind();
      _vertexBuffer?.Unbind();
      _indexBuffer?.Unbind();

      if (computeBoundBox)
      {

        ComputeBoundBox();
      }

    }
    private void ComputeBoundBox()
    {
      _boundBoxExtent.genResetLimits();

      int voff = VertexFormat.GetComponentOffset(VertexComponentType.v3_01);
      if (voff >= 0)
      {
        var vertexes = _vertexBuffer.CopyDataFromGPU();

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
      BoundBoxComputed = true;
    }
    public static GpuDataPtr GenerateQuadIndicesArray(int numQuads, bool flip = false)
    {
      uint[] uu = GenerateQuadIndices(numQuads, flip);
      var ret = Gpu.GetGpuDataPtr(uu);
      return ret;
    }
    public static uint[] GenerateQuadIndices(int numQuads, bool flip = false)
    {
      //0  1
      //2  3
      uint idx = 0;
      uint[] inds = new uint[numQuads * 6];
      for (int face = 0; face < numQuads; ++face)
      {
        inds[face * 6 + 0] = idx + 0;
        inds[face * 6 + 1] = idx + (uint)(flip ? 2 : 3);
        inds[face * 6 + 2] = idx + (uint)(flip ? 3 : 2);
        inds[face * 6 + 3] = idx + 0;
        inds[face * 6 + 4] = idx + (uint)(flip ? 3 : 1);
        inds[face * 6 + 5] = idx + (uint)(flip ? 1 : 3);
        idx += 4;
      }
      return inds;
    }
    public static MeshData GenPlane(float w, float h, vec2[] side = null)
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

      v_v3n3x2[] verts = new v_v3n3x2[4];
      verts[0 * 4 + 0] = new v_v3n3x2() { _v = box[0], _n = norms[0], _x = (side != null) ? side[0] : texs[0] };
      verts[0 * 4 + 1] = new v_v3n3x2() { _v = box[1], _n = norms[0], _x = (side != null) ? side[1] : texs[1] };
      verts[0 * 4 + 2] = new v_v3n3x2() { _v = box[2], _n = norms[0], _x = (side != null) ? side[2] : texs[2] };
      verts[0 * 4 + 3] = new v_v3n3x2() { _v = box[3], _n = norms[0], _x = (side != null) ? side[3] : texs[3] };

      var indsBoxed = GenerateQuadIndicesArray(verts.Length / 4);
      var vertsBoxed = Gpu.GetGpuDataPtr(verts);
      return new MeshData("Plane", PrimitiveType.Triangles, v_v3n3x2.VertexFormat, vertsBoxed, IndexFormatType.Uint32, indsBoxed);
    }

    public static MeshData GenBox(float w, float h, float d, vec2[] top = null, vec2[] side = null, vec2[] bot = null)
    {
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
      v_v3n3x2[] verts = new v_v3n3x2[6 * 4];//lrbtaf
      verts[0 * 4 + 0] = new v_v3n3x2() { _v = box[4], _n = norms[0], _x = (side != null) ? side[0] : texs[0] };
      verts[0 * 4 + 1] = new v_v3n3x2() { _v = box[0], _n = norms[0], _x = (side != null) ? side[1] : texs[1] };
      verts[0 * 4 + 2] = new v_v3n3x2() { _v = box[6], _n = norms[0], _x = (side != null) ? side[2] : texs[2] };
      verts[0 * 4 + 3] = new v_v3n3x2() { _v = box[2], _n = norms[0], _x = (side != null) ? side[3] : texs[3] };

      verts[1 * 4 + 0] = new v_v3n3x2() { _v = box[1], _n = norms[1], _x = (side != null) ? side[0] : texs[0] };
      verts[1 * 4 + 1] = new v_v3n3x2() { _v = box[5], _n = norms[1], _x = (side != null) ? side[1] : texs[1] };
      verts[1 * 4 + 2] = new v_v3n3x2() { _v = box[3], _n = norms[1], _x = (side != null) ? side[2] : texs[2] };
      verts[1 * 4 + 3] = new v_v3n3x2() { _v = box[7], _n = norms[1], _x = (side != null) ? side[3] : texs[3] };

      verts[2 * 4 + 0] = new v_v3n3x2() { _v = box[4], _n = norms[2], _x = (bot != null) ? bot[0] : texs[0] };
      verts[2 * 4 + 1] = new v_v3n3x2() { _v = box[5], _n = norms[2], _x = (bot != null) ? bot[1] : texs[1] };
      verts[2 * 4 + 2] = new v_v3n3x2() { _v = box[0], _n = norms[2], _x = (bot != null) ? bot[2] : texs[2] };
      verts[2 * 4 + 3] = new v_v3n3x2() { _v = box[1], _n = norms[2], _x = (bot != null) ? bot[3] : texs[3] };

      verts[3 * 4 + 0] = new v_v3n3x2() { _v = box[2], _n = norms[3], _x = (top != null) ? top[0] : texs[0] };
      verts[3 * 4 + 1] = new v_v3n3x2() { _v = box[3], _n = norms[3], _x = (top != null) ? top[1] : texs[1] };
      verts[3 * 4 + 2] = new v_v3n3x2() { _v = box[6], _n = norms[3], _x = (top != null) ? top[2] : texs[2] };
      verts[3 * 4 + 3] = new v_v3n3x2() { _v = box[7], _n = norms[3], _x = (top != null) ? top[3] : texs[3] };

      verts[4 * 4 + 0] = new v_v3n3x2() { _v = box[0], _n = norms[4], _x = (side != null) ? side[0] : texs[0] };
      verts[4 * 4 + 1] = new v_v3n3x2() { _v = box[1], _n = norms[4], _x = (side != null) ? side[1] : texs[1] };
      verts[4 * 4 + 2] = new v_v3n3x2() { _v = box[2], _n = norms[4], _x = (side != null) ? side[2] : texs[2] };
      verts[4 * 4 + 3] = new v_v3n3x2() { _v = box[3], _n = norms[4], _x = (side != null) ? side[3] : texs[3] };

      verts[5 * 4 + 0] = new v_v3n3x2() { _v = box[5], _n = norms[5], _x = (side != null) ? side[0] : texs[0] };
      verts[5 * 4 + 1] = new v_v3n3x2() { _v = box[4], _n = norms[5], _x = (side != null) ? side[1] : texs[1] };
      verts[5 * 4 + 2] = new v_v3n3x2() { _v = box[7], _n = norms[5], _x = (side != null) ? side[2] : texs[2] };
      verts[5 * 4 + 3] = new v_v3n3x2() { _v = box[6], _n = norms[5], _x = (side != null) ? side[3] : texs[3] };

      var indsBoxed = GenerateQuadIndicesArray(verts.Length / 4);
      var vertsBoxed = Gpu.GetGpuDataPtr(verts);
      return new MeshData("box", PrimitiveType.Triangles, v_v3n3x2.VertexFormat, vertsBoxed, IndexFormatType.Uint32, indsBoxed);
    }
    public static MeshData GenTextureFront(Camera3D c, float x, float y, float w, float h)
    {
      //Let's do the UI from bottom left like OpenGL
      v_v3n3x2[] verts = new v_v3n3x2[4];
      verts[0]._v = c.Frustum.ScreenToWorld(new vec2(x, y), TransformSpace.Local, 0.01f).p0;
      verts[1]._v = c.Frustum.ScreenToWorld(new vec2(x + w, y), TransformSpace.Local, 0.01f).p0;
      verts[2]._v = c.Frustum.ScreenToWorld(new vec2(x, y + h), TransformSpace.Local, 0.01f).p0;
      verts[3]._v = c.Frustum.ScreenToWorld(new vec2(x + w, y + h), TransformSpace.Local, 0.01f).p0;

      verts[0]._x = new vec2(1, 0);
      verts[1]._x = new vec2(1, 1);
      verts[2]._x = new vec2(1, 0);
      verts[3]._x = new vec2(0, 0);

      verts[0]._n = new vec3(0, 0, -1);
      verts[1]._n = new vec3(0, 0, -1);
      verts[2]._n = new vec3(0, 0, -1);
      verts[3]._n = new vec3(0, 0, -1);

      var indsBoxed = GenerateQuadIndicesArray(verts.Length / 4);
      var vertsBoxed = Gpu.GetGpuDataPtr(verts);

      return new MeshData("texturefrtont", PrimitiveType.Triangles, v_v3n3x2.VertexFormat, vertsBoxed, IndexFormatType.Uint32, indsBoxed);
    }
    public static MeshData GenSphere(float radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false)
    {
      return GenEllipsoid(new vec3(radius, radius, radius), slices, stacks, smooth, flip_tris);
    }
    public static MeshData GenEllipsoid(vec3 radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false)
    {
      v_v3n3x2[] verts;
      uint[] inds;
      GenEllipsoid(out verts, out inds, radius, slices, stacks, smooth, flip_tris);
      var vertsBoxed = Gpu.GetGpuDataPtr(verts);
      var indsBoxed = Gpu.GetGpuDataPtr(inds);
      return new MeshData("sphere", PrimitiveType.Triangles, v_v3n3x2.VertexFormat, vertsBoxed, IndexFormatType.Uint32, indsBoxed);
    }
    public static void GenEllipsoid(out v_v3n3x2[] verts, out uint[] inds, vec3 radius, int slices = 128, int stacks = 128, bool smooth = false, bool flip_tris = false)
    {
      int vcount = slices * stacks * 4;
      verts = new v_v3n3x2[vcount];

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
              verts[vind + p * 2 + t]._v = new vec3(
                  radius.x * MathUtils.sinf(phi[p]) * MathUtils.cosf(theta[t]),
                  radius.y * MathUtils.cosf(phi[p]),
                  radius.z * MathUtils.sinf(phi[p]) * MathUtils.sinf(theta[t])
              );
            }
          }

          if (smooth)
          {
            verts[vind + 0]._n = verts[vind + 0]._v.normalized();
            verts[vind + 1]._n = verts[vind + 1]._v.normalized();
            verts[vind + 2]._n = verts[vind + 2]._v.normalized();
            verts[vind + 3]._n = verts[vind + 3]._v.normalized();
          }
          else
          {
            vec3 n = (verts[vind + 1]._v - verts[vind + 0]._v).cross(verts[vind + 2]._v - verts[vind + 0]._v).normalized();
            verts[vind + 0]._n = n;
            verts[vind + 1]._n = n;
            verts[vind + 2]._n = n;
            verts[vind + 3]._n = n;
          }

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


  }
}
