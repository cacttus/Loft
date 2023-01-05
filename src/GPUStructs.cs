using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;
using System.Reflection;

namespace Loft
{
  #region Classes

  // public class RenderData
  // {
  //   //Context shared GPU data
  //   public GpuRenderData _renderData;
  //   public GpuMemoryBuffer _gpuData;

  //   public Dictionary<GpuDataFormat, GpuMemoryBuffer> _buffers = new Dictionary<GpuDataFormat, GpuMemoryBuffer>();

  //   public GpuMemoryBlock _world;
  //   public GpuMemoryBlock _debug;
  //   public GpuMemoryBlock _nodes;
  //   public GpuMemoryBlock _lights;
  //   public GpuMemoryBlock _cameras;
  //   public GpuMemoryBlock _materials;
  //   public GpuMemoryBlock _meshes;
  //   public GpuMemoryBlock _sampler2Ds;
  //   public GpuMemoryBlock _armatures;

  //   //public Dictionary<GpuDataFormat, GpuMappedBuffer> _ssbos = new Dictionary<GpuDataFormat, GpuMappedBuffer>();

  //   public Dictionary<WindowContext, Dictionary<GpuDataFormat, VertexArrayObject[]>> _vaos = new Dictionary<WindowContext, Dictionary<GpuDataFormat, VertexArrayObject[]>>();

  //   public RenderData()
  //   {
  //     CreateBuffers();
  //   }
  //   private void CreateBuffers()
  //   {
  //     int size_bytes = Gu.EngineConfig.ShaderCV_GpuDataSizeMB * 100000;
  //     _gpuData = new GpuMemoryBuffer("renderdata", BufferTarget.ShaderStorageBuffer, BufferUsageHint.StaticDraw, size_bytes);

  //     _renderData = new GpuRenderData();
  //     _renderData._world = new GpuWorld();
  //     _renderData._debug = new GpuDebug();
  //     _renderData._nodes = new GpuNode[Gu.EngineConfig.ShaderCV_MaxObjects];
  //     _renderData._meshes = new GpuMesh[Gu.EngineConfig.ShaderCV_MaxMeshes];
  //     _renderData._materials = new GpuMaterial[Gu.EngineConfig.ShaderCV_MaxMaterials];
  //     _renderData._lights = new GpuLight[Gu.EngineConfig.ShaderCV_MaxLights];
  //     _renderData._sampler2Ds = new GpuSampler2D[Gu.EngineConfig.ShaderCV_MaxSampler2Ds];
  //     _renderData._cameras = new GpuCamera[Gu.EngineConfig.ShaderCV_MaxCameras];
  //     _renderData._armatures = new GpuArmature[Gu.EngineConfig.ShaderCV_MaxArmatures];

  //     Gu.Assert(_gpuData.TryAllocateRegion<GpuWorld>(out _world, 1, 1));
  //     Gu.Assert(_gpuData.TryAllocateRegion<GpuDebug>(out _debug, 1, 1));
  //     Gu.Assert(_gpuData.TryAllocateRegion<GpuNode>(out _nodes, _renderData._nodes.Length, 1));
  //     Gu.Assert(_gpuData.TryAllocateRegion<GpuMesh>(out _meshes, _renderData._meshes.Length, 1));
  //     Gu.Assert(_gpuData.TryAllocateRegion<GpuMaterial>(out _materials, _renderData._materials.Length, 1));
  //     Gu.Assert(_gpuData.TryAllocateRegion<GpuLight>(out _lights, _renderData._lights.Length, 1));
  //     Gu.Assert(_gpuData.TryAllocateRegion<GpuSampler2D>(out _sampler2Ds, _renderData._sampler2Ds.Length, 1));
  //     Gu.Assert(_gpuData.TryAllocateRegion<GpuCamera>(out _cameras, _renderData._cameras.Length, 1));
  //     Gu.Assert(_gpuData.TryAllocateRegion<GpuArmature>(out _armatures, _renderData._armatures.Length, 1));

  //     var handle = GCHandle.Alloc(_renderData, GCHandleType.Pinned);
  //     int size = Marshal.SizeOf(_renderData);
  //     _gpuData.CopyToGPU(handle.AddrOfPinnedObject(), 0, 0, size);
  //     handle.Free();
  //   }
  //   public BufferView CreateVertexBuffer<T>(string name, T[] verts) where T : struct
  //   {
  //     return CreateXBuffer<T>(name + "-vbo", verts, BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw);
  //   }
  //   public BufferView CreateIndexBuffer<T>(string name, T[] verts) where T : struct
  //   {
  //     return CreateXBuffer<T>(name + "-ibo", verts, BufferTarget.ElementArrayBuffer, BufferUsageHint.StaticDraw);
  //   }
  //   public BufferView CreateFaceDataBuf<T>(string name, T[] verts) where T : struct
  //   {
  //     return CreateXBuffer(name + "-ssbo", verts, BufferTarget.ShaderStorageBuffer, BufferUsageHint.StaticDraw);
  //   }
  //   public BufferView CreateXBuffer<T>(string name, T[] items, BufferTarget target, BufferUsageHint hint) where T : struct
  //   {
  //     Gu.Assert(items != null);
  //     var fmt = GpuDataFormat.GetDataFormat<T>();
  //     GpuMemoryBuffer? buf = null;
  //     if (!_buffers.TryGetValue(fmt, out buf))
  //     {
  //       buf = new GpuMemoryBuffer(name, target, hint, 0);
  //     }
  //     return buf.AllocateSingleItem(items);
  //   }
  //   private VertexArrayObject CreateVAO(string name, bool inds)
  //   {
  //     Gpu.CheckGpuErrorsDbg();
  //     VertexArrayObject vao = new VertexArrayObject(name + "-vao");
  //     vao.Bind();

  //     int bufferBiningID = 0;
  //     GpuDataFormat fmtLast = null;
  //     int maxLocation = 0;

  //     Gu.Assert(_verts != null);
  //     Gu.Assert(_inds != null);

  //     //shit.. attribs bind to vtx format..
  //     //this only works if the buffer has all the same data. Hence, the previous solution

  //     //* the fix for this is to sort rendering by Vertex format, and plug all formats into their own buffer
  //     //  all meshes with same fmt get one buffer. This will reduce VAO binding to number of VTX formats, which we can
  //     //  also normalize for the most part.

  //     //we can split the buffers and have a "Universal Format" for the mapped buffer (vs individual formats for regions)

  //     GL.BindVertexBuffer(bufferBiningID, _verts.GlId, IntPtr.Zero, Marshal.SizeOf(typeof(GpuNode)));
  //     Gu.Assert(_verts.Format != null);
  //     vb.Format.BindVertexAttribs(bufferBiningID, ref maxLocation);
  //     fmtLast = vb.Format;
  //     bufferBiningID++;

  //     if (inds)
  //     {
  //       _inds.Bind();
  //     }

  //     Gpu.CheckGpuErrorsDbg();
  //     vao.Unbind();

  //     GpuBuffer.UnbindBuffer(BufferTarget.ArrayBuffer);
  //     GpuBuffer.UnbindBuffer(BufferTarget.ElementArrayBuffer);

  //     return vao;
  //   }
  //   public VertexArrayObject GetOrCreateVAO(WindowContext ct)
  //   {
  //     //assming we weill need only one VAO - cocky..
  //     VertexArrayObject[]? vaos = null;
  //     if (!_vaos.TryGetValue(ct, out vaos))
  //     {
  //       vaos = new VertexArrayObject[2];
  //       vaos[0] = CreateVAO("drawarrays", false);
  //       vaos[1] = CreateVAO("drawelements", true);
  //     }

  //     return vao;
  //   }

  //   public void BindBlock()
  //   {
  //     Gu.Assert(b != null);
  //     Gu.Assert(b.RangeTarget != null);
  //     GL.BindBufferBase(b.RangeTarget.Value, u.BindingIndex, b.GlId);
  //     Gpu.CheckGpuErrorsDbg();
  //     GL.BindBuffer(b.BufferTarget, b.GlId);
  //     Gpu.CheckGpuErrorsDbg();
  //   }

  //   //todo: global uniform binding across shaders.
  //   // private void BindWorldUniforms(WorldProps world)
  //   // {
  //   //   //Uniforms can be bound across shaders so long as the name is the same.
  //   //   //https://stackoverflow.com/questions/33004962/opengl-uniform-across-multiple-shaders
  //   //   Gu.Assert(world != null);
  //   //   BindUniformBlock(ShaderUniformName._ufGpuWorld_Block.Description(), world._gpuWorldBuf);
  //   //   BindUniformBlock(ShaderUniformName._ufGpuLights_Block.Description(), world._gpuLightsBuf);
  //   //   BindUniformBlock(ShaderUniformName._ufGpuDebug_Block.Description(), world._gpuDebugBuf);
  //   // }
  //   // private void BindViewUniforms(RenderView rv)
  //   // {
  //   //   Gu.Assert(rv != null);
  //   //   BindUniformBlock(ShaderUniformName._ufGpuCamera_Block.Description(), rv.GpuCamera);
  //   // }
  //   // private void BindMaterialUniforms(Material mat)
  //   // {
  //   //   //TODO:copy all materials at once
  //   //   Gu.Assert(mat != null);
  //   //   BindUniformBlock(ShaderUniformName._ufGpuMaterial_Block.Description(), mat.GpuMaterial);
  //   //   foreach (var input in mat.Textures)
  //   //   {
  //   //     BindTexture(input.Name, input.GetTextureOrDefault());
  //   //   }
  //   // }

  // }//cls

  #endregion
  #region Data Structs

  // this will cause headaches and issues, but I like the idea .. maybe later
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuRenderData
  {
    //TODO: instead of having to pad each struct, we now can just manually pad the entire GpuData structure to std430? no each struct needs to be padded for GL to see it.
    public GpuWorld _world;
    public GpuDebug _debug;
    public GpuNode[] _nodes;
    public GpuMesh[] _meshes;
    public GpuLight[] _lights;
    public GpuCamera[] _cameras;
    public GpuMaterial[] _materials;
    public GpuSampler2D[] _sampler2Ds;
    public GpuArmature[] _armatures;

    private static StructUtils.StructInfo? _structinfo = null;
    public static int Length(string fieldname)
    {
      //get length of field in bytes
      if (_structinfo == null)
      {
        _structinfo = StructUtils.GetStructInfo<GpuRenderData>();
      }
      return _structinfo.Length(fieldname);
    }
    public static int Offset(string fieldname)
    {
      //get byte offset of fiedl
      if (_structinfo == null)
      {
        _structinfo = StructUtils.GetStructInfo<GpuRenderData>();
      }
      return _structinfo.Offset(fieldname);

    }

  }//cls

  [StructLayout(LayoutKind.Sequential)]
  public struct DrawArraysIndirectCommand
  {
    //Multi Draw (TODO)
    //Rendering with a different VAO from the last drawing command is usually a relatively expensive operation.
    //many of the optimization mechanisms are based on you storing the data for several meshes in the same buffer objects 
    //with the same vertex formats and other VAO data. 
    // they must use the same shader program with the same uniform values. 
    //If you have a number of meshes that all share the same vertex format, 
    //it would be useful to be able to put them all in a single set of buffer objects, one after the other.
    //Validation is veyr slow on gpu and is the bottleneck
    //glPrimitiveRestartIndex
    //"for large amounts of data (bones) consider SSBO"
    //https://www.slideshare.net/CassEveritt/beyond-porting
    //https://www.khronos.org/opengl/wiki/Vertex_Rendering#Indirect_rendering
    //GL_DRAW_INDIRECT_BUFFER - basically a command buffer in vulkan no?
    //glMultiDrawElementsIndirect
    uint count;
    uint instanceCount;
    uint firstIndex;
    uint baseVertex;   // the index added to the indices that poin ts to the next mesh
    uint baseInstance; //base instance id gl_BaseInstance, gl_DrawIDARB;  gl_BaseVertexARB;
                       //glPrimitiveRestartIndex. allows for special index in the index array (65535 or so) that tells GL to start a new 
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuInstanceData
  {
    public GpuInstanceData() { }
    public mat4 _model = mat4.Identity;
    public mat4 _model_inverse = mat4.Identity;
    public uvec2 _pickId = new uvec2(0,0);
    public float _pad0 = 0.0f;
    public float _pad1 = 0.0f;
  }  
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuNode//GpuInstance
  {
    public GpuNode() { }
    public mat4 _model = mat4.Identity;
    public mat4 _model_inverse = mat4.Identity;
    //
    public uvec2 _pickId = new uvec2(0,0);
    public int _materialID = 0;
    public int _poseID = 0; //offset into _ufGpuPoseBones
    //
    public int _nodeID = 0; //not needed
    public int _meshID = 0;
    public int _pad000 = 0;
    public int _pad00213=0;
    //public int _poseID = 0;//not used
    //public int _armID = 0;//not used
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuMesh
  {
    public GpuMesh() { }
    public int _pad1 = 0;//armature id
    public int _faceDataOffset = 0; //offset into facedata buffer
    public int _pad3 = 0;
    public int _pad2 = 0;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuLight
  {
    public GpuLight() { }
    public vec3 _pos = new vec3(0, 0, 0);
    public float _radius = 1000; // Radius=maxdist radius = 0 = directional
    //
    public vec3 _color = new vec3(1, 1, 1);
    public float _power = 10;
    //
    public vec3 _dir = new vec3(0, -1, 0);
    public int _enabled = 0;
    //
    public int _atten = 1; //0 = disable attenuation
    public int _isDir = 0;
    public float _pad1 = 0;
    public float _pad2 = 0;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuWorld
  {
    public GpuWorld() { }
    //
    public float _fFogDamp = 2.8f;
    public float _fFogBlend = 0.56361f;
    public float _fFogDivisor = 1200.0f; //Begin of fog distance
    public float _fFocalDepth = 0.0f;
    //
    public vec3 _vFogColor = new vec3(0.8407f, 0.89349f, 0.981054f);
    public float _fFocalRange = 25.0f;
    //
    public float _fTimeSeconds = 0;
    public int _pad00 = 0;
    public int _pad001 = 0;
    public int _pad = 0;
    //
    public int _iShadowBoxCount = 0;
    public float _fHdrSampleExp = 1.1f;
    public float _fHdrGamma = 1.0f;
    public float _fHdrExposure = 0.75f;
    //
    public vec3 _vAmbientColor = new vec3(1, 1, 1);
    public float _fAmbientIntensity = 0.01f;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuCamera
  {
    public GpuCamera() { }
    //
    public mat4 _m4View = mat4.Identity;
    public mat4 _m4Projection = mat4.Identity;

    //
    public vec3 _vViewPos = vec3.Zero;
    public float _fWindowWidth = 0;//Dont use - use RenderWidth
    //
    public vec3 _vViewDir = vec3.Zero;
    public float _fWindowHeight = 0;
    //
    public vec4 _vWindowViewport = vec4.Zero;
    //
    public float _fRenderWidth = 0;
    public float _fRenderHeight = 0;
    public float _fZNear = 0;
    public float _fZFar = 0;
    //
    public float _widthNear = 0;
    public float _heightNear = 0;
    public float _widthFar = 0;
    public float _heightFar = 0;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuMaterial
  {
    public GpuMaterial() { }
    //
    public vec4 _vPBR_baseColor = new vec4(1, 1, 1, 1);
    //
    public float _fPBR_roughness = 0.01f;
    public float _fPBR_metallic = 0.0f;
    public float _fPBR_indexOfRefraction = 1.45f;
    public float _fPBR_specular = 0.5f;
    //
    public float _flat = 0;
    public float _alphaCutoff = 0;
    public float _pad2 = 0;
    public float _pad3 = 0;
    //
    public vec4 _vBlinnPhong_Spec = new vec4(.9f, .9f, .9f, 1300);
    //
    public int _albedoIdx = -1; //images in tex 2d array TODO: * FOR NOW WE WILL USE AN ARRYA OF SAMPLER2D
    public int _normalIdx = -1;
    public int _metalIdx = -1;
    public int _roughIdx = -1;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuDebug
  {
    const float cb = 0;//0.3725f;
    const float nb = 1;//0.895f;

    public GpuDebug() { }
    //
    public vec4 _faceTangentColor = new vec4(nb, cb, cb, 1);//kinda make it differntt
    public vec4 _faceNormalColor = new vec4(cb, nb, cb, 1);
    public vec4 _faceBinormalColor = new vec4(cb, cb, nb, 1);
    public vec4 _vertexTangentColor = new vec4(1, 0, 1, 1);
    public vec4 _vertexNormalColor = new vec4(0, 1, 1, 1);
    public vec4 _vertexBinormalColor = new vec4(1, 1, 0, 1);
    //
    public float _normalLength = 0.3f;//for normals/tangents
    public float _fWireframeCageDist = 0.002f; // extrusion of wireframe, % of 1 unit
    public float pad1 = 0;
    public float pad2 = 0;
    //
    public vec4 _wireframeColor = new vec4(.793f, .779f, .783f, 1);
    //
    public vec4 _color = new vec4(.894f, .894f, .890f, 0.759f);
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuFaceData
  {
    public GpuFaceData() { }
    public vec3 _normal = vec3.Zero;
    public uint _index = 0;
    public vec3 _tangent = vec3.Zero;
    public float pad1 = 0;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuWeightOffset
  {
    public int wc;	//joints count
    public int wo;	//joints offset
    public float pad0;
    public float pad1;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuWeight
  {
    public int joff; //offset into matrix palette, i.e. joint id
    public float wt;
    public float pad0;
    public float pad1;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuPoseBone
  {
    public mat4 mat;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuArmature
  {
    //this is converted to sampler2D in the shader, the struct value is ignored.
    public int _weightOffset;
    public int _weightOffsetOffset;
    public int _armatureID; //pose bones offset
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct GpuSampler2D
  {
    //this is converted to sampler2D in the shader, the struct value is ignored.
    public int _value;
  }

  #endregion
  #region Vertex Formats

  //note we removed std430 padding .. this is erroneous.. we need to fix it
  [DataContract]
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3n3x2t3u1
  {
    //Default object vertex
    [DataMember] public vec3 _v;
    [DataMember] public vec3 _n;
    [DataMember] public vec2 _x;
    [DataMember] public vec3 _t;
    [DataMember] public uint _faceID;
    //[DataMember] public uint _nodeID;
  }
  [DataContract]
  [StructLayout(LayoutKind.Sequential)]
  public struct v_debug_draw
  {
    //debug vertex
    [DataMember] public vec3 _v;
    [DataMember] public vec4 _c;
    [DataMember] public vec2 _size;
    [DataMember] public vec3 _outl;//outline color
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v4v4v4v2u2v4v4
  {
    //v_GuiVert
    //26 float = 96B
    [DataMember] public vec4 _rect;
    [DataMember] public vec4 _clip;
    [DataMember] public vec4 _tex;
    [DataMember] public vec2 _texsiz;
    [DataMember] public uvec2 _pick_color;
    [DataMember] public vec4 _rtl_rtr; //css corners = tl, tr, br, bl = xyzw
    [DataMember] public vec4 _rbr_rbl;
    [DataMember] public vec4 _border;
    [DataMember] public uvec4 _border_color;
    [DataMember] public vec4 _font_weight;
  };
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3x2
  {
    //Textured Quad
    [DataMember] public vec3 _v;
    [DataMember] public vec2 _x;
  };

  #endregion




}//ns