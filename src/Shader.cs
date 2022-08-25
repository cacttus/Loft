using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL4;

namespace PirateCraft
{
  #region GPU Structs
  public enum ShaderUniformName
  {
    [Description("_ufGpuMaterial_s2Albedo")] _ufGpuMaterial_s2Albedo,
    [Description("_ufGpuMaterial_s2Normal")] _ufGpuMaterial_s2Normal,
    [Description("_ufGpuMaterial_s2Roughness")] _ufGpuMaterial_s2Roughness,
    [Description("_ufGpuMaterial_s2Metalness")] _ufGpuMaterial_s2Metalness,
    [Description("_ufGpuMaterial_s2Position")] _ufGpuMaterial_s2Position,
    [Description("_ufGpuMaterial_Block")] _ufGpuMaterial_Block,
    [Description("_ufGpuWorld_Block")] _ufGpuWorld_Block,
    [Description("_ufGpuCamera_Block")] _ufGpuCamera_Block,
    [Description("_ufGpuWorld_s2EnvironmentMap")] _ufGpuWorld_s2EnvironmentMap,
    [Description("_ufGpuWorld_s2IrradianceMap")] _ufGpuWorld_s2IrradianceMap,
    [Description("_ufGpuPointLights_Block")] _ufGpuPointLights_Block,
    [Description("_ufGpuDirLights_Block")] _ufGpuDirLights_Block,
    [Description("_ufGpuInstanceData_Block")] _ufGpuInstanceData_Block,
    [Description("_m4Projection_Debug")] _m4Projection_Debug,
    [Description("_m4View_Debug")] _m4View_Debug,
    [Description("_m4Model_Debug")] _m4Model_Debug,
  }
  public enum ShaderDataType
  {
    Float,
    UInt,
    Int,
    Vec2,
    Vec3,
    Vec4,
    Mat3,
    Mat4,
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuInstanceData
  {
    public GpuInstanceData() { }
    public mat4 _model = mat4.Identity;
    public uvec2 _pickId = 0;
    public float _pad0 = 0.0f;
    public float _pad1 = 0.0f;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuPointLight
  {
    public GpuPointLight() { }
    //
    public vec3 _pos = new vec3(0, 0, 0);
    public float _radius = 1;
    //
    public vec3 _color = new vec3(1, 1, 1);
    public float _power = 1;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct GpuDirLight
  {
    public GpuDirLight() { }
    public vec3 _pos = new vec3(0, 0, 0);
    public float _radius = 1000; // Radius=maxdist
    //
    public vec3 _color = new vec3(1, 1, 1);
    public float _power = 10;
    //
    public vec3 _dir = new vec3(0, -1, 0);
    public float _pad = 0;
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
    public int _iPointLightCount = 0;
    public int _iDirLightCount = 0;
    public float _fHdrSampleExp = 1;
    public float _fHdrToneMapExp = 1;
    //
    public int _iShadowBoxCount = 0;
    public float _fTimeSeconds = 0;
    public int _pad0 = 0;
    public int _pad1 = 0;
    //
    public vec3 _vAmbientColor = new vec3(1, 1, 1);
    public float _fAmbientIntensity = 0.1f;
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
    public float _pad0 = 0;
    //
    public vec3 _vViewDir = vec3.Zero;
    public float _pad1 = 0;
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
  }
  #endregion

  public class ShaderControlVars
  {
    public ShaderControlVars() { }
    public ShaderControlVars(PipelineStage stage, ShaderType type)
    {
      ShaderType = type;
      PipelineStage = stage;
    }
    public ShaderType? ShaderType { get; private set; } = null;
    //public PipelineStageEnum? PipelineStage { get; private set; } = null;
    public int MaxPointLights { get; set; } = 8;
    public int MaxDirLights { get; set; } = 2;
    public int MaxCubeShadowSamples { get; set; } = 4;
    public int MaxFrusShadowSamples { get; set; } = 4;
    public int MaxInstances { get; set; } = 8;
    public bool IsInstanced { get; set; } = true;  //this is, technically going to always be set now, but later we can add non-instanced for performance improvements.
    private PipelineStage PipelineStage = null;

    public ShaderControlVars Clone(PipelineStage stage, ShaderType type)
    {
      Gu.Assert(stage != null);

      var ret = new ShaderControlVars(stage, type);
      ret.MaxPointLights = this.MaxPointLights;
      ret.MaxDirLights = this.MaxDirLights;
      ret.MaxCubeShadowSamples = this.MaxCubeShadowSamples;
      ret.MaxFrusShadowSamples = this.MaxFrusShadowSamples;
      return ret;
    }
    private void AddDef<T>(StringBuilder sb, string key, T val)
    {
      sb.Append("#define " + key + " " + val.ToString() + "\n");
    }
    private void AddDef(StringBuilder sb, string key)
    {
      sb.Append("#define " + key + "\n");
    }
    public string DefinesString()
    {
      var sb = new StringBuilder();

      AddDef(sb, "DEF_MAX_POINT_LIGHTS", MaxPointLights);
      AddDef(sb, "DEF_MAX_DIR_LIGHTS", MaxDirLights);
      AddDef(sb, "DEF_MAX_CUBE_SHADOW_SAMPLES", MaxCubeShadowSamples);
      AddDef(sb, "DEF_MAX_FRUS_SHADOW_SAMPLES", MaxFrusShadowSamples);
      AddDef(sb, "DEF_MAX_INSTANCES", MaxInstances);

      if (IsInstanced)
      {
        AddDef(sb, "DEF_INSTANCED");
      }

      ShaderType st = this.ShaderType.Value;
      PipelineStageEnum ps = this.PipelineStage.PipelineStageEnum;
      if (st == OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader || st == OpenTK.Graphics.OpenGL4.ShaderType.FragmentShaderArb) { AddDef(sb, "DEF_SHADER_STAGE_FRAGMENT"); }
      else if (st == OpenTK.Graphics.OpenGL4.ShaderType.VertexShader || st == OpenTK.Graphics.OpenGL4.ShaderType.VertexShaderArb) { AddDef(sb, "DEF_SHADER_STAGE_VERTEX"); }
      else if (st == OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader) { AddDef(sb, "DEF_SHADER_STAGE_GEOMETRY"); }
      else { Gu.BRThrowNotImplementedException(); }

      //Ooh.thanks Description[]
      AddDef(sb, ps.Description());

      return sb.ToString();
    }
    public string OutputsString()
    {
      StringBuilder sb = new StringBuilder();

      //Get all pipeline outputs and set blank set() functions for outputs that are disabled in the given stage.
      //HashSet<string> outputs = new HashSet<string>();
      var all_outputs = Gu.Context.Renderer.GetAllUniqueAttachments();
      var unique_outputs = new Dictionary<string, FramebufferAttachment>();
      foreach (var pp in all_outputs)
      {
        if (!unique_outputs.ContainsKey(pp.OutputName))
        {
          unique_outputs.Add(pp.OutputName, pp);
        }
      }
      if (PipelineStage.OutputFramebuffer == null)
      {
        SetOutputString(sb, 0, "vec4", "Color");//default fbo
          Gu.Assert(unique_outputs.Remove("Color"));//output must be in the global outputs array
      }
      else
      {
        foreach (var output in PipelineStage.OutputFramebuffer.Bindings)
        {
          var outname = output.Attachment.OutputName;
          Gu.Assert(unique_outputs.Remove(outname));//output must be in the global outputs array
          string datatype = PixelInternalFormatToShaderDataType(output.Attachment.Texture.PixelInternalFormat);
          SetOutputString(sb, output.LayoutIndex, datatype, outname);
        }
      }

      foreach (var output in unique_outputs)
      {
        string datatype = PixelInternalFormatToShaderDataType(output.Value.Texture.PixelInternalFormat);
        sb.Append("void setOutput_" + output.Value.OutputName + "(in " + datatype + " val) { }\n");
      }
      return sb.ToString();
    }
    private void SetOutputString(StringBuilder sb, int layoutidx, string datatype, string outname)
    {
      sb.Append($"layout(location = {layoutidx}) out {datatype} _mrtOutput_{outname};\n");
      sb.Append("void setOutput_" + outname + "(in " + datatype + " val) { _mrtOutput_" + outname + " = val; }\n");
    }
    private string PixelInternalFormatToShaderDataType(PixelInternalFormat fmt)
    {
      if (fmt == PixelInternalFormat.DepthComponent32f) { return "float"; }
      else if (fmt == PixelInternalFormat.DepthComponent32) { return "float"; }
      else if (fmt == PixelInternalFormat.DepthComponent24) { return "float"; }
      else if (fmt == PixelInternalFormat.DepthComponent16) { return "float"; }
      //else if (fmt == PixelInternalFormat.Rgba) { return "vec4"; }
      else if (fmt == PixelInternalFormat.Rgba32f) { return "vec4"; }
      else if (fmt == PixelInternalFormat.R32ui) { return "uint"; }
      //These are the only supported image->buffer formats right now..
      Gu.BRThrowNotImplementedException();
      return "";
    }
  }
  public class WorldProps : DataBlock
  {
    #region Public:Members
    //similar to "world" in Blender, Envmap + Volume
    public Texture2D EnvironmentMap { get { return _environmentMap; } set { _environmentMap = value; SetModified(); } }
    public Texture2D IrradianceMap { get { return _irradianceMap; } set { _irradianceMap = value; SetModified(); } }
    public float FogDamp { get { return _fogDamp; } set { _fogDamp = value; SetModified(); } }
    public float FogBlend { get { return _fogBlend; } set { _fogBlend = value; SetModified(); } }
    public float FogDivisor { get { return _fogDivisor; } set { _fogDivisor = value; SetModified(); } }
    public vec3 FogColor { get { return _fogColor; } set { _fogColor = value; SetModified(); } }
    public vec3 Ambient { get { return _ambient; } set { _ambient = value; SetModified(); } }
    public float AmbientIntensity { get { return _ambientIntensity; } set { _ambientIntensity = value; SetModified(); } }
    public DayNightCycle DayNightCycle { get { return _dayNightCycle; } set { _dayNightCycle = value; SetModified(); } }
    public ModifiedList<Light> Lights { get { return _lights; } set { _lights = value; SetModified(); } }
    public GpuWorld GpuWorld { get { return _gpuWorld; } }
    public GpuDirLight[] GpuDirLights { get { return _gpuDirLights; } }
    public GpuPointLight[] GpuPointLights { get { return _gpuPointLights; } }

    #endregion
    #region Private: Members

    private Texture2D _environmentMap = null;
    private Texture2D _irradianceMap = null;
    private float _fogDamp = 2.8f;
    private float _fogBlend = 0.56361f;
    private float _fogDivisor = 1200.0f; //Begin of fog distance
    private vec3 _fogColor = new vec3(0.8407f, 0.89349f, 0.981054f);
    private vec3 _ambient = new vec3(1, 1, 1);
    private float _ambientIntensity = 0.1f;
    private DayNightCycle _dayNightCycle = null;
    private ModifiedList<Light> _lights = new ModifiedList<Light>();
    private GpuWorld _gpuWorld = new GpuWorld();
    private GpuDirLight[] _gpuDirLights = null;
    private GpuPointLight[] _gpuPointLights = null;

    #endregion

    public void Reset()
    {
      _lights.Clear();
      SetModified();
    }

    protected WorldProps() { }
    public WorldProps(string name) : base(name + "-env") { }
    public void CompileGpuData()
    {
      if (Modified || Lights.Modified || Gu.EngineConfig.AlwaysCompileAndReloadGpuUniformData)
      {
        _gpuWorld._fFogDamp = this._fogDamp;
        _gpuWorld._fFogBlend = this._fogBlend;
        _gpuWorld._fFogDivisor = this._fogDivisor;
        _gpuWorld._vFogColor = this._fogColor;
        _gpuWorld._vAmbientColor = this._ambient;
        _gpuWorld._fAmbientIntensity = this._ambientIntensity;

        _gpuWorld._iPointLightCount = 0;
        _gpuWorld._iDirLightCount = 0;

        IterateLights((light) => { _gpuWorld._iPointLightCount++; }, (light) => { _gpuWorld._iDirLightCount++; });

        _gpuPointLights = new GpuPointLight[_gpuWorld._iPointLightCount];
        _gpuDirLights = new GpuDirLight[_gpuWorld._iDirLightCount];

        IterateLights(
          (light) =>
          {
            _gpuPointLights[light]._color = _lights[light].Color;
            _gpuPointLights[light]._pos = _lights[light].Position_World;
            _gpuPointLights[light]._power = _lights[light].Power;
            _gpuPointLights[light]._radius = _lights[light].Radius;
          },
          (light) =>
          {
            _gpuDirLights[light]._color = _lights[light].Color;
            _gpuDirLights[light]._dir = _lights[light].Heading;
            _gpuDirLights[light]._pos = _lights[light].Position_World;
            _gpuDirLights[light]._power = _lights[light].Power;
            _gpuDirLights[light]._radius = _lights[light].Radius;
            _gpuWorld._iDirLightCount++;
          });

        _gpuWorld._fHdrSampleExp = 1;
        _gpuWorld._fHdrToneMapExp = 1;
        _gpuWorld._iShadowBoxCount = 0;
        _gpuWorld._fTimeSeconds = (float)(Gu.Milliseconds() % 1000) / 1000.0f;
        _gpuWorld._fFocalDepth = 0;
        _gpuWorld._fFocalRange = 25.0f;

        Modified = false;
        Lights.Modified = false;
      }
    }
    private void IterateLights(Action<int> point, Action<int> dir)
    {
      if (_lights != null)
      {
        //foreach (var light in _lights)
        for (int iLight = 0; iLight < _lights.Count; iLight++)
        {
          var light = _lights[iLight];
          if (light.Type == LightType.Point)
          {
            point(iLight);
          }
          else if (light.Type == LightType.Direction)
          {
            dir(iLight);
          }
          else
          {
            Gu.BRThrowNotImplementedException();
          }
        }
      }
    }
  }
  public class VisibleObjectInstances : MutableState
  {
    public List<WorldObject> Objects = null;
    public GpuInstanceData[] GpuInstanceData = null;
    public MeshData Mesh
    {
      get
      {
        if (Objects != null)
        {
          if (Objects.Count > 0)
          {
            return Objects[0].Mesh;
          }
        }
        return null;
      }
    }
    public void Add(WorldObject ob)
    {
      Objects = Objects.ConstructIfNeeded();
      Objects.Add(ob);
      SetModified();
    }
    public void CompileGpuData()
    {
      if (Modified || Gu.EngineConfig.AlwaysCompileAndReloadGpuUniformData)
      {
        Gu.Assert(Objects != null);
        GpuInstanceData = new GpuInstanceData[Objects.Count];
        for (int iob = 0; iob < Objects.Count; iob++)
        {
          Gu.Assert(Objects[iob] != null);
          //we could have the objects also contain a GpuInstanceData themselves.. this may be too much extra data though
          GpuInstanceData[iob]._model = Objects[iob].WorldMatrix;
          GpuInstanceData[iob]._pickId.x = Objects[iob].PickId;
        }
        Modified = false;
      }
    }
  }
  public class VisibleObjects : MutableState
  {
    public Dictionary<Material, Dictionary<Int64, VisibleObjectInstances>> Objects { get; set; } = null;
    public GpuMaterial GpuMaterial { get { return _gpuMaterial; } }

    private GpuMaterial _gpuMaterial = new GpuMaterial();

    public VisibleObjects()
    {
    }
    public VisibleObjects(List<WorldObject> obs)
    {
      foreach (var ob in obs)
      {
        //This is inefficient we could do better.
        Add(ob);
      }
    }
    public void Draw(WorldProps wp, RenderView rv)
    {
      if (Objects != null)
      {
        CompileGpuData();

        foreach (var mk in Objects)
        {
          var mat = mk.Key;
          mat.GpuRenderState.SetState();
          var cs = mat.Shader.GetShaderForCurrentContext();
          cs.BeginRender(wp, rv, mat);
          foreach (var ob_set in mk.Value)
          {
            ob_set.Value.CompileGpuData();
            cs.BindInstanceUniforms(ob_set.Value.GpuInstanceData);

            cs.CheckAllUniformsSet();

            var mesh = ob_set.Value.Mesh;
            Gu.Assert(mesh != null);
            mesh.Draw(ob_set.Value.GpuInstanceData);
          }
          cs.EndRender();
        }
      }
    }
    public void CompileGpuData()
    {
      if (Modified || Gu.EngineConfig.AlwaysCompileAndReloadGpuUniformData)
      {
        if (Objects != null)
        {
          foreach (var mat_dic in Objects)
          {
            mat_dic.Key.CompileGpuData();
            foreach (var type_instances in mat_dic.Value)
            {
              type_instances.Value.CompileGpuData();
            }
          }
        }

        Modified = false;
      }
    }
    public void Clear()
    {
      if (Objects != null)
      {
        Objects.Clear();
        SetModified();
      }
    }
    public void Add(WorldObject ob)
    {
      Gu.Assert(ob.Material != null);

      var typeId = ob.TypeID;
      //**TEST
      Objects = Objects.ConstructIfNeeded();
      Dictionary<Int64, VisibleObjectInstances>? matList = null;
      if (!Objects.TryGetValue(ob.Material, out matList))
      {
        matList = matList.ConstructIfNeeded();
        Objects.Add(ob.Material, matList);
      }
      VisibleObjectInstances? objList = null;
      if (!matList.TryGetValue(ob.TypeID, out objList))
      {
        objList = objList.ConstructIfNeeded();
        matList.Add(ob.TypeID, objList);
      }
      objList.Add(ob);
      SetModified();
    }
  }
  //stuff needed to render
  public class DrawCall
  {
    public double? Delta { get { return _delta; } set { _delta = value; } }
    public VisibleObjects VisibleObjects { get { return _visibleObjects; } set { _visibleObjects = value; } }
    public Func<ShaderUniform, bool> CustomUniforms = null;//Input: a uniform variable name that you must bind to, output: true if you handled it, false if not
    public Func<ContextShader, ShaderUniformBlock, bool> CustomUniformBlocks = null;//Input: a uniform variable name that you must bind to, output: true if you handled it, false if not

    private double? _delta = null;
    private VisibleObjects _visibleObjects = new VisibleObjects();

    public static void Draw(WorldProps p, RenderView rv, WorldObject ob)
    {
      DrawCall dc = new DrawCall();
      dc.VisibleObjects.Add(ob);
      dc.Draw(p, rv);
    }

    public DrawCall() { }
    public void BeginCollectVisibleObjects()
    {
      _visibleObjects.Clear();
    }
    public void AddVisibleObject(WorldObject ob)
    {
      _visibleObjects = VisibleObjects.ConstructIfNeeded();
      _visibleObjects.Add(ob);
    }
    public void Draw(WorldProps p, RenderView rv)
    {
      Gu.Assert(p != null);
      Gu.Assert(rv != null);
      if (_visibleObjects != null)
      {
        _visibleObjects.Draw(p, rv);
      }
    }
  }
  public enum ShaderLoadState
  {
    None,
    Loading,
    Failed,
    Success
  }
  public class ShaderStage : OpenGLResource
  {
    public ShaderType ShaderType { get; private set; } = ShaderType.VertexShader;
    public ShaderStage(string name, ShaderType tt, string src) : base(name + "-shr")
    {
      ShaderType = tt;
      _glId = GL.CreateShader(tt);
      Gpu.CheckGpuErrorsRt();
      GL.ShaderSource(_glId, src);
      Gpu.CheckGpuErrorsRt();
      GL.CompileShader(_glId);
      Gpu.CheckGpuErrorsRt();
    }

    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsShader(_glId))
      {
        GL.DeleteShader(_glId);
      }
    }
  }
  public class ShaderUniform
  {
    public bool HasBeenSet { get; set; } = false;
    public int Location { get; private set; } = 0;
    public string Name { get; private set; } = "unset";
    public string Value { get; private set; } = "unset";
    public int SizeBytes { get; private set; } = 0;
    public ActiveUniformType Type { get; private set; } = ActiveUniformType.Int;
    public bool Active { get; private set; } = false;
    public ShaderUniform(int location, int u_size, ActiveUniformType u_type, string u_name, bool active)
    {
      Location = location; ;
      Name = u_name;
      Type = u_type;
      SizeBytes = u_size;
      Active = active;
    }
  }
  public class ShaderUniformBlock : OpenGLResource
  {
    public int UboId { get; private set; } = -2;
    public int BlockIndex { get; private set; } = -1;
    public int BindingIndex { get; private set; } = -1;
    public bool HasBeenSet { get; set; } = false;
    public bool Active { get; private set; } = false;
    public int BufferSizeBytes { get; private set; } = 0;

    public override void Dispose_OpenGL_RenderThread()
    {
      GL.DeleteBuffer(UboId);
    }
    public ShaderUniformBlock(string name, int iBlockIndex, int iBindingIndex, int iBufferByteSize, bool active) : base(name + "-ubk")
    {
      BufferSizeBytes = iBufferByteSize;
      BindingIndex = iBindingIndex;
      BlockIndex = iBlockIndex;
      Active = active;

      UboId = GL.GenBuffer();
      GL.BindBuffer(BufferTarget.UniformBuffer, UboId);
      Gpu.CheckGpuErrorsDbg();
      GL.BufferData(BufferTarget.UniformBuffer, BufferSizeBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
      Gpu.CheckGpuErrorsDbg();
      GL.BindBuffer(BufferTarget.UniformBuffer, 0);
      Gpu.CheckGpuErrorsDbg();
    }
  }

  //Shader, program on the GPU. Contextual.
  public class ContextShader : OpenGLResource
  {
    private static string c_strGlobalDefineString = "<GLSL_CONTROL_DEFINES_HERE>";
    private static string c_strGlobalOutputString = "<GLSL_CONTROL_OUTPUTS_HERE>";
    private ShaderStage _vertexStage = null;
    private ShaderStage _fragmentStage = null;
    private ShaderStage _geomStage = null;

    public PipelineStageEnum PipelineStage { get; private set; } = PipelineStageEnum.Unset;

    private Dictionary<string, ShaderUniform> _uniforms = new Dictionary<string, ShaderUniform>();
    private Dictionary<string, ShaderUniformBlock> _uniformBlocks = new Dictionary<string, ShaderUniformBlock>();

    private TextureUnit _currUnit = TextureUnit.Texture0;
    //Technically this is a GL context thing. But it's ok for now.
    private Dictionary<TextureUnit, Texture2D> _boundTextures = new Dictionary<TextureUnit, Texture2D>();

    private List<string> _shaderErrors = new List<string>();

    private ShaderLoadState State = ShaderLoadState.None;

    private string Name = "";

    public ContextShader(string name, WindowContext ct, string vsSrc_raw = "", string psSrc_raw = "", string gsSrc_raw = "") : base(name + "-prog")
    {
      Gu.Assert(ct != null);
      Gu.Assert(ct.Renderer != null);
      Gu.Assert(ct.Renderer.CurrentStage != null);

      Name = name;
      PipelineStage = ct.Renderer.CurrentStage.PipelineStageEnum;
      Gu.Log.Debug("Compiling shader '" + Name + "'");

      Gpu.CheckGpuErrorsDbg();
      {
        State = ShaderLoadState.Loading;

        string vsSrc = ProcessShaderSource(ct, vsSrc_raw, ShaderType.VertexShader);
        string gsSrc = ProcessShaderSource(ct, gsSrc_raw, ShaderType.GeometryShader);
        string psSrc = ProcessShaderSource(ct, psSrc_raw, ShaderType.FragmentShader);

        CreateShaders(vsSrc, psSrc, gsSrc);

        CreateProgram();

        //Just leave this on. Always save the copmiled shader source.
        string v_src = GetReadableShaderSourceCode(vsSrc);
        string g_src = GetReadableShaderSourceCode(gsSrc);
        string f_src = GetReadableShaderSourceCode(psSrc);
        SaveShaderSource(v_src, Name + ".vs.glsl", null);
        SaveShaderSource(g_src, Name + ".gs.glsl", null);
        SaveShaderSource(f_src, Name + ".fs.glsl", null);

        if (State != ShaderLoadState.Failed)
        {
          GL.UseProgram(_glId);
          Gpu.CheckGpuErrorsRt();

          ParseUniforms();

          State = ShaderLoadState.Success;
        }
        else
        {
          string errors = String.Join(Environment.NewLine, _shaderErrors.ToArray());
          string blip = "--------------------------------------------------------------------------------------";
          string all_src_errs = "";
          if (!String.IsNullOrEmpty(vsSrc))
          {
            all_src_errs += Environment.NewLine + blip + Environment.NewLine + "--VERTEX SOURCE--" + Environment.NewLine + blip + Environment.NewLine + v_src + Environment.NewLine;
            SaveShaderSource(v_src, Name + ".vs.glsl", errors);
          }
          if (!String.IsNullOrEmpty(gsSrc))/*  */
          {
            all_src_errs += Environment.NewLine + blip + Environment.NewLine + "--GEOM SOURCE--" + Environment.NewLine + blip + Environment.NewLine + g_src + Environment.NewLine;
            SaveShaderSource(v_src, Name + ".gs.glsl", errors);
          }
          if (!String.IsNullOrEmpty(psSrc))
          {
            all_src_errs += Environment.NewLine + blip + Environment.NewLine + "--FRAG SOURCE--" + Environment.NewLine + blip + Environment.NewLine + f_src + Environment.NewLine;
            SaveShaderSource(v_src, Name + ".fs.glsl", errors);
          }
          Gu.Log.Info(all_src_errs);
          Gu.Log.Error("Failed to load shader '" + Name + "'." + Environment.NewLine + errors);
          Gu.DebugBreak();
        }
      }
      Gpu.CheckGpuErrorsDbg();
      SetObjectLabel();
    }
    private void SaveShaderSource(string src, string filename, string? errors)
    {
      System.IO.File.WriteAllText(System.IO.Path.Combine(Gu.LocalCachePath, filename), src + Environment.NewLine + (errors == null ? "" : errors));
    }
    private string ProcessShaderSource(WindowContext ct, string src_raw, ShaderType type)
    {
      string src_cpy = src_raw;
      if (StringUtil.IsNotEmpty(src_cpy))
      {
        string e = "" + Name + " (" + type.ToString() + "): Shader vars tag '";

        if (!src_cpy.Contains(ContextShader.c_strGlobalDefineString))
        {
          _shaderErrors.Add(e + c_strGlobalDefineString + "' was not found.");
          State = ShaderLoadState.Failed;
        }
        else if (!src_cpy.Contains(ContextShader.c_strGlobalOutputString))
        {
          _shaderErrors.Add(e + c_strGlobalOutputString + "' was not found.");
          State = ShaderLoadState.Failed;
        }
        else
        {
          var vars = ct.Renderer.DefaultControlVars.Clone(ct.Renderer.CurrentStage, type);

          var defines_string = vars.DefinesString();
          src_cpy = src_cpy.Replace(ContextShader.c_strGlobalDefineString, defines_string);

          var outputs_string = vars.OutputsString();
          src_cpy = src_cpy.Replace(ContextShader.c_strGlobalOutputString, outputs_string);
        }
      }
      return src_cpy;
    }
    private string GetReadableShaderSourceCode(string vs)
    {
      StringBuilder stringBuilder = new StringBuilder();
      var lines = vs.Split('\n');
      for (int iLine = 0; iLine < lines.Length; iLine++)
      {
        string line = lines[iLine];
        string r = String.Format("{0,5}", (iLine + 1).ToString());
        stringBuilder.Append("/*!!!COMPILED!!!*/ ");
        stringBuilder.Append(r);
        stringBuilder.Append("  ");
        stringBuilder.Append(line);
        stringBuilder.Append(Environment.NewLine);
      }
      return stringBuilder.ToString();
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsProgram(_glId))
      {
        GL.DeleteProgram(_glId);
      }
    }
    private void Bind()
    {
      Gpu.CheckGpuErrorsDbg();
      {
        GL.UseProgram(_glId);
      }
      Gpu.CheckGpuErrorsDbg();
    }
    private void Unbind()
    {
      Gpu.CheckGpuErrorsDbg();
      {
        GL.UseProgram(0);
      }
      Gpu.CheckGpuErrorsDbg();
    }
    public void BeginRender(WorldProps world, RenderView rv, Material mat)
    {
      //**Pre - render - update uniforms.
      Gpu.CheckGpuErrorsDbg();
      {
        //Reset
        _currUnit = TextureUnit.Texture0;
        _boundTextures.Clear();

        Bind();
        BindUniformBlock(ShaderUniformName._ufGpuCamera_Block.Description(), new GpuWorld[] { world.GpuWorld });
        BindViewUniforms(rv);
        BindWorldUniforms(world);
        BindMaterialUniforms(mat);
      }
      Gpu.CheckGpuErrorsDbg();
    }
    public void EndRender()
    {
      Unbind();
      foreach (var tu in _boundTextures)
      {
        if (tu.Value != null)
        {
          tu.Value.Unbind();
        }
      }
      _currUnit = TextureUnit.Texture0;
      _boundTextures.Clear();
    }

    #region Private

    private void CreateShaders(string vs, string ps, string gs = "")
    {
      Gpu.CheckGpuErrorsRt();
      {
        _vertexStage = new ShaderStage(this.Name + "-VS", ShaderType.VertexShader, vs);
        _fragmentStage = new ShaderStage(this.Name + "-FS", ShaderType.FragmentShader, ps);
        if (!string.IsNullOrEmpty(gs))
        {
          _geomStage = new ShaderStage(this.Name + "-GS", ShaderType.GeometryShader, gs);
        }
      }
      Gpu.CheckGpuErrorsRt();
    }
    private void CreateProgram()
    {
      Gpu.CheckGpuErrorsRt();
      {
        _glId = GL.CreateProgram();

        GL.AttachShader(_glId, _vertexStage.GetGlId());
        Gpu.CheckGpuErrorsRt();
        GL.AttachShader(_glId, _fragmentStage.GetGlId());
        Gpu.CheckGpuErrorsRt();
        if (_geomStage != null)
        {
          GL.AttachShader(_glId, _geomStage.GetGlId());
          Gpu.CheckGpuErrorsRt();
        }

        GL.LinkProgram(_glId);
        Gpu.CheckGpuErrorsRt();

        string programInfoLog = "";
        GL.GetProgramInfoLog(_glId, out programInfoLog);
        if (_shaderErrors == null)
        {
          _shaderErrors = new List<string>();
        }
        _shaderErrors.AddRange(programInfoLog.Split('\n').ToList());

        if (_shaderErrors.Count > 0 && programInfoLog.ToLower().Contains("error"))
        {
          State = ShaderLoadState.Failed;
        }

      }
      Gpu.CheckGpuErrorsRt();
    }
    private void ParseUniforms()
    {
      int u_count = 0;
      GL.GetProgram(_glId, GetProgramParameterName.ActiveUniforms, out u_count);
      Gpu.CheckGpuErrorsRt();

      //TODO: blocks
      for (var i = 0; i < u_count; i++)
      {
        ActiveUniformType u_type;
        int u_size = 0;
        string u_name = "DEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEAD";//idk.
        int u_name_len = 0;

        GL.GetActiveUniform(GetGlId(), i, out u_size, out u_type);
        Gpu.CheckGpuErrorsRt();
        GL.GetActiveUniformName(GetGlId(), i, u_name.Length, out u_name_len, out u_name);
        Gpu.CheckGpuErrorsRt();

        if (u_name.Contains("["))
        {
          //This is a unifrom block
          continue;
        }

        bool active = true;
        int location = GL.GetUniformLocation(GetGlId(), u_name);

        u_name = u_name.Substring(0, u_name_len);

        if (location < 0)
        {
          active = false;
          if (!u_name.Contains('.'))
          {
            //There will be tons of inactive uniforms for structures. struct.name
            //But for what we're doing if it isn't a structure it should be used.
            Gu.DebugBreak();
          }
          Gu.Log.Debug(Name + ": Inactive uniform: " + u_name);
          //Not necessarily an error
          //GetUniformLocation "This function returns -1 if name does not correspond to an active uniform variable in program,
          //if name starts with the reserved prefix "gl_", or if name is associated with an atomic counter or a named uniform block."
          //Uniform variables that are structures or arrays of structures may be queried by calling glGetUniformLocation for each
          //field within the structure. The array element operator "[]" and the structure field operator "." may be used in name in
          //order to select elements within an array or fields within a structure. The result of using these operators is not allowed
          //to be another structure, an array of structures, or a subcomponent of a vector or a matrix. Except if the last part of name
          //indicates a uniform variable array, the location of the first element of an array can be retrieved by using the name of the
          //array, or by using the name appended by "[0]".
        }
        else
        {
          Gu.Log.Debug(Name + ": Active uniform: " + u_name);
        }

        ShaderUniform su = new ShaderUniform(location, u_size, u_type, u_name, active);
        _uniforms.Add(u_name, su);
      }

      int u_block_count = 0;
      GL.GetProgram(_glId, GetProgramParameterName.ActiveUniformBlocks, out u_block_count);
      Gpu.CheckGpuErrorsRt();
      for (var iBlock = 0; iBlock < u_block_count; iBlock++)
      {
        int buffer_size_bytes = 0;
        GL.GetActiveUniformBlock(GetGlId(), iBlock, ActiveUniformBlockParameter.UniformBlockDataSize, out buffer_size_bytes);
        Gpu.CheckGpuErrorsRt();

        int binding = 0;
        GL.GetActiveUniformBlock(GetGlId(), iBlock, ActiveUniformBlockParameter.UniformBlockBinding, out binding);
        Gpu.CheckGpuErrorsRt();

        string u_name = "DEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEAD";//idk.
        int u_name_len = 0;
        GL.GetActiveUniformBlockName(GetGlId(), iBlock, u_name.Length, out u_name_len, out u_name);
        Gpu.CheckGpuErrorsRt();
        u_name = u_name.Substring(0, u_name_len);

        bool active = true;
        if (binding < 0)
        {
          active = false;
          Gu.Log.Debug(Name + ": Inactive uniform block: " + u_name);
        }
        else
        {
          Gu.Log.Debug(Name + ": Active Uniform block: " + u_name);
        }

        ShaderUniformBlock su = new ShaderUniformBlock(u_name, iBlock, binding, buffer_size_bytes, active);// u_size, u_type, u_name);
        _uniformBlocks.Add(u_name, su);
      }
    }
    private void BindWorldUniforms(WorldProps world)
    {
      Gu.Assert(world != null);
      world.CompileGpuData();
      BindUniformBlock(ShaderUniformName._ufGpuWorld_Block.Description(), new GpuWorld[] { world.GpuWorld });
      BindTexture(ShaderUniformName._ufGpuWorld_s2EnvironmentMap.Description(), world.EnvironmentMap);
      BindTexture(ShaderUniformName._ufGpuWorld_s2IrradianceMap.Description(), world.IrradianceMap);
      BindUniformBlock(ShaderUniformName._ufGpuPointLights_Block.Description(), world.GpuPointLights);
      BindUniformBlock(ShaderUniformName._ufGpuDirLights_Block.Description(), world.GpuDirLights);
    }
    private void BindViewUniforms(RenderView rv)
    {
      Gu.Assert(rv != null);
      rv.CompileGpuData();
      BindUniformBlock(ShaderUniformName._ufGpuCamera_Block.Description(), new GpuCamera[] { rv.GpuCamera });
    }
    private void BindMaterialUniforms(Material mat)
    {
      Gu.Assert(mat != null);
      mat.CompileGpuData();
      BindUniformBlock(ShaderUniformName._ufGpuMaterial_Block.Description(), new GpuMaterial[] { mat.GpuMaterial });
      BindTexture(ShaderUniformName._ufGpuMaterial_s2Albedo.Description(), mat.AlbedoSlot.GetTextureOrDefault());
      BindTexture(ShaderUniformName._ufGpuMaterial_s2Normal.Description(), mat.NormalSlot.GetTextureOrDefault());
      BindTexture(ShaderUniformName._ufGpuMaterial_s2Position.Description(), mat.PositionSlot.GetTextureOrDefault());
      BindTexture(ShaderUniformName._ufGpuMaterial_s2Roughness.Description(), mat.RoughnessSlot.GetTextureOrDefault());
      BindTexture(ShaderUniformName._ufGpuMaterial_s2Metalness.Description(), mat.MetalnessSlot.GetTextureOrDefault());
    }
    public void BindInstanceUniforms(GpuInstanceData[] inst)
    {
      Gu.Assert(inst != null);
      if (!BindUniformBlock(ShaderUniformName._ufGpuInstanceData_Block.Description(), inst))
      {
        //Int he current system Instance data is required.
        Gu.DebugBreak();
      }

      BindUniform_Mat4(ShaderUniformName._m4Model_Debug.Description(), inst[0]._model);
    }
    public void BindUniform_Mat4(string name, mat4 m)
    {
      if (_uniforms.TryGetValue(name, out var u))
      {
        var mat = m.ToOpenTK();
        GL.UniformMatrix4(u.Location, false, ref mat);
        u.HasBeenSet = true;
      }
    }
    public void CheckAllUniformsSet()
    {
      string notset = "";
      int n = 0;
      foreach (var ub in this._uniformBlocks)
      {
        if (ub.Value.Active == true && ub.Value.HasBeenSet == false)
        {
          notset += " " + ub.Value.Name + ",";
          n++;
        }
      }
      foreach (var u in this._uniforms)
      {
        if (u.Value.Active == true && u.Value.HasBeenSet == false)
        {
          notset += " " + u.Value.Name + ",";
          n++;
        }
      }
      if (notset.Length > 0)
      {
        Gu.Log.Warn($"{Name}: {n} Uniforms were not set: {notset}");
        Gu.DebugBreak();
      }
    }
    private bool BindUniformBlock<T>(string uname, T[] items)
    {
      if (_uniformBlocks.TryGetValue(uname, out var block))
      {
        BindUniformBlock(block, items);
        return true;
      }
      else
      {
        ReportUniformNotFound(uname, true);
        return false;
      }
    }
    private void BindUniformBlock<T>(ShaderUniformBlock u, T[] items)
    {
      Gu.Assert(items != null);

      int item_size = 0;
      if (!typeof(T).IsValueType)
      {
        throw new Exception("Input items must be value type. If items are array, use array value type.");
      }
      item_size = Marshal.SizeOf(typeof(T));//default(T) 

      int num_bytes_to_copy = item_size * items.Length;// dat.instanceData.Length;
      if (num_bytes_to_copy > u.BufferSizeBytes)
      {
        num_bytes_to_copy = u.BufferSizeBytes;
        Gu.Log.WarnCycle("Exceeded max index count of " + u.BufferSizeBytes / item_size + " matrices. Tried to copy " + items.Length + " block instances.");
      }
      var handle = GCHandle.Alloc(items, GCHandleType.Pinned);
      CopyUniformBlockData(u, handle.AddrOfPinnedObject(), num_bytes_to_copy);
      handle.Free();

      BindUniformBlockFast(u);
    }
    public void CopyUniformBlockData(ShaderUniformBlock u, IntPtr pData, int copySizeBytes)
    {
      //Copy to the shader buffer
      Gu.Assert(copySizeBytes <= u.BufferSizeBytes);

      Gpu.CheckGpuErrorsDbg();

      GL.BindBuffer(BufferTarget.UniformBuffer, u.UboId);
      Gpu.CheckGpuErrorsDbg();

      GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, copySizeBytes, pData);
      Gpu.CheckGpuErrorsDbg();

      GL.BindBuffer(BufferTarget.UniformBuffer, 0);
      Gpu.CheckGpuErrorsDbg();

      if (u.HasBeenSet == true)
      {
        Gu.Log.WarnCycle(this.Name + ": Uniform  " + u.Name + " was already set.");
      }
      u.HasBeenSet = true;
    }
    public void BindUniformBlockFast(ShaderUniformBlock u)
    {
      if (u.HasBeenSet == false)
      {
        Gu.Log.WarnCycle(this.Name + ": Shader Uniform Block '" + u.Name + "' value was not set before binding.");
        Gu.DebugBreak();
      }
      GL.BindBufferBase(BufferRangeTarget.UniformBuffer, u.BindingIndex, u.UboId);
      Gpu.CheckGpuErrorsDbg();
      GL.BindBuffer(BufferTarget.UniformBuffer, u.UboId);
      Gpu.CheckGpuErrorsDbg();
    }
    private void BindTexture(string uniform_name, Texture2D tex)
    {
      Gu.Assert(_uniforms != null);
      if (_uniforms.TryGetValue(uniform_name, out var su))
      {
        if (tex != null)
        {
          if (su.HasBeenSet)
          {
            Gu.Log.WarnCycle(this.Name + ": Texture uniform " + su.Name + " has already been set.");
          }
          GL.Uniform1(su.Location, (int)(_currUnit - TextureUnit.Texture0));
          tex.Bind(_currUnit);
          _boundTextures.Add(_currUnit, tex);
          su.HasBeenSet = true;
        }
        else
        {
          Gu.Log.WarnCycle(this.Name + ": Texture unit " + su.Name + " was not found in material and had no default.");
        }

        _currUnit++;
      }
      else
      {
        ReportUniformNotFound(uniform_name, false);
      }
    }
    private void ReportUniformNotFound(string uniform_name, bool is_block, bool error = false)
    {
      if (error)
      {
        Gu.Log.Error(this.Name + ": Unknown uniform " + (is_block ? "block " : "") + " '" + uniform_name + "' (possibly optimized out).");
        Gu.DebugBreak();
      }
      else
      {
        Gu.Log.WarnCycle(this.Name + ": Unknown uniform " + (is_block ? "block " : "") + " '" + uniform_name + "' (possibly optimized out).");

      }
    }
    private static string ParseIncludeLine(string line)
    {
      int part = 0;
      string filename = "";
      foreach (char c in line)
      {
        if (c == '"')
        {
          if (part == 1)
          {
            break;
          }
          part = 1;
        }
        else if (part == 1)
        {
          filename += c;
        }
      }
      return filename;
    }
    public static string ProcessFile(FileLoc loc, List<string> errors)
    {
      var file_lines = new StringBuilder();

      ProcessFile(loc, file_lines, errors);

      return file_lines.ToString();
    }
    private static void ProcessFile(FileLoc loc, StringBuilder file_lines, List<string> errors)
    {
      Gu.Assert(loc != null);
      Gu.Assert(file_lines != null);
      //Returns the entire processed string on the first function invocation. 
      //Do not set file_lines if you want the return value

      string file_text = ResourceManager.ReadTextFile(loc);
      string[] lines = file_text.Split("\n");
      int iLine = 0;
      for (iLine = 0; iLine < lines.Length; iLine++)
      {
        //Replace all \r
        string line_proc = lines[iLine].Replace("\r\n", "\n");
        if (!line_proc.EndsWith("\n"))
        {
          line_proc += "\n";//Avoid the last line ending with \0
        }
        lines[iLine] = line_proc;
      }

      //Try to Parse Includes First, note it's not an error to have includes later, but we technically shouldn't for simplicity.
      for (iLine = 0; iLine < lines.Length; iLine++)
      {
        if (!CheckInclude(iLine, lines, loc, file_lines, errors))
        {
          break;
        }
      }

      file_lines.Append("//\n");
      file_lines.Append("// BEGIN: " + loc.RawPath + " (" + loc.QualifiedPath + ")\n");
      file_lines.Append("//\n");

      for (; iLine < lines.Length; iLine++)
      {
        if (CheckInclude(iLine, lines, loc, file_lines, errors))
        {
          errors.Add("File: '" + loc.RawPath + "': #include should be at the top of the file to avoid invalid file commenting (may not be a GLSL error).");
        }
        file_lines.Append(lines[iLine]);
      }

      file_lines.Append("//\n");
      file_lines.Append("// END: " + loc.RawPath + " (" + loc.QualifiedPath + ")\n");
      file_lines.Append("//\n");


    }
    private static bool CheckInclude(int iLine, string[] lines, FileLoc loc, StringBuilder file_lines, List<string> errors)
    {
      string line = lines[iLine];
      if (line.StartsWith("#include "))//note the space
      {
        var inc = ParseIncludeLine(line);

        string? dir = System.IO.Path.GetDirectoryName(loc.RawPath);
        if (dir != null)
        {
          string fs = "";
          if (!String.IsNullOrEmpty(dir))
          {
            fs = System.IO.Path.Combine(dir, inc);
          }
          else
          {
            fs = inc;
          }

          ProcessFile(new FileLoc(fs, loc.FileStorage), file_lines, errors);
        }
        else
        {
          Gu.BRThrowException("Directory name" + loc.RawPath + " was null");
        }
        return true;
      }
      else
      {
        return false;
      }
    }
  }
  public class TextureInput : DataBlock
  {
    public Texture2D Texture { get { return _texture; } set { _texture = value; SetModified(); } }
    public Texture2D GetTextureOrDefault()
    {
      if (Texture == null)
      {
        Gu.Assert(_default != null);//default shader inputs cant be null
        return _default;
      }
      else
      {
        return Texture;
      }
    }

    private Texture2D _default = null;
    private Texture2D _texture = null;

    public TextureInput(Texture2D default_tex)
    {
      _default = default_tex;
    }

  }
  //to make contexts transparent.
  public class Shader : OpenGLContextDataManager<Dictionary<PipelineStageEnum, ContextShader>>
  {


    public string Name { get; private set; } = "<unset>";
    private string _vs = "", _fs = "", _gs = "";
    private static Shader _defaultDiffuseShader = null;
    private static Shader _defaultBillboardPoints = null;
    private static Shader _defaultFlatColorShader = null;

    public static Shader DefaultFlatColorShader()
    {
      if (_defaultFlatColorShader == null)
      {
        _defaultFlatColorShader = Gu.Resources.LoadShader("v_v3", false, FileStorage.Embedded);
      }
      return _defaultFlatColorShader;
    }
    public static Shader DefaultObjectShader()
    {
      //Returns a basic v3 n3 x2 lambert+blinn-phong shader.
      if (_defaultDiffuseShader == null)
      {
        _defaultDiffuseShader = Gu.Resources.LoadShader("v_v3n3x2", false, FileStorage.Embedded);
      }
      return _defaultDiffuseShader;
    }
    public static Shader DefaultBillboardPoints()
    {
      //Returns a basic v3 n3 x2 lambert+blinn-phong shader.
      if (_defaultBillboardPoints == null)
      {
        _defaultBillboardPoints = Gu.Resources.LoadShader("v_billboard_points", true, FileStorage.Embedded);
      }
      return _defaultBillboardPoints;
    }

    public Shader(string name, string vsSrc = "", string psSrc = "", string gsSrc = "")
    {
      Name = name;
      _vs = vsSrc;
      _fs = psSrc;
      _gs = gsSrc;
    }
    public ContextShader GetShaderForCurrentContext()
    {
      return GetOrCreateShader(Gu.Context);
    }
    protected override Dictionary<PipelineStageEnum, ContextShader> CreateNew()
    {
      return new Dictionary<PipelineStageEnum, ContextShader>();
    }
    private ContextShader GetOrCreateShader(WindowContext ct)
    {
      Gu.Assert(ct != null);
      PipelineStageEnum stage = ct.Renderer.CurrentStage.PipelineStageEnum;
      var dict = GetDataForContext(ct);
      Gu.Assert(dict != null);

      ContextShader? shader = null;
      if (!dict.TryGetValue(stage, out shader))
      {
        shader = new ContextShader(Name, ct, _vs, _fs, _gs);
        dict.Add(stage, shader);
      }
      Gu.Assert(shader.PipelineStage == stage);

      return shader;
    }

  }

  #endregion

  //TODO: shaderCompiler, ShaderCache



}
