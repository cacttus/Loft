using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;
using System.Reflection;

namespace PirateCraft
{
  #region Enums

  public enum ShaderLoadState
  {
    None,
    Precompiling,
    CheckingPreprocessorChanges,
    CheckingToLoadBinary,
    LoadingBinary,
    LoadingBinaryFailed,
    CompilingShaders,
    CompiledShadersSuccess,
    Compiled, //Means compiled on GPU or shaderc, (not preproc)
    Validated,
    Success,
    Failed,
    MaxShaderLoadStates,
  }

  #endregion

  #region GPU Structs
  public enum ShaderUniformName
  {
    [Description("_ufGpuMaterial_Block")] _ufGpuMaterial_Block,
    [Description("_ufGpuWorld_Block")] _ufGpuWorld_Block,
    [Description("_ufGpuDebug_Block")] _ufGpuDebug_Block,
    [Description("_ufGpuCamera_Block")] _ufGpuCamera_Block,
    [Description("_ufGpuPointLight_Block")] _ufGpuPointLight_Block,
    [Description("_ufGpuDirLight_Block")] _ufGpuDirLight_Block,
    [Description("_ufGpuInstanceData_Block")] _ufGpuInstanceData_Block,
    [Description("_ufGpuFaceData_Block")] _ufGpuFaceData_Block,
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
    public mat4 _model_inverse = mat4.Identity;
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
    public float _fTimeSeconds = 0;
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
    public float _pad0 = 0;
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
    //
    public float _flat = 0;
    public float _pad0 = 0;
    public float _pad1 = 0;
    public float _pad2 = 0;
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
    public float _normalLength = 0.1f;
    public float _lineWidth = 0.1f;
    public float _fWireframeCageDist = 0.002f; // % of 1 unit
    public float _pad2 = 0.0f;
    //
    public vec4 _wireframeColor = new vec4(.793f, .779f, .783f, 1);
  }

  #endregion

  public class ShaderControlVars
  {
    public ShaderType? ShaderType { get; private set; } = null;
    public int MaxPointLights { get; set; } = 16;
    public int MaxDirLights { get; set; } = 2;
    public int MaxCubeShadowSamples { get; set; } = 4;
    public int MaxFrusShadowSamples { get; set; } = 4;
    public int MaxInstances { get; set; } = 32;
    public bool IsInstanced { get; set; } = true;  //this is, technically going to always be set now, but later we can add non-instanced for performance improvements.
    private PipelineStageEnum PipelineStageEnum = PipelineStageEnum.Unset;
    private int PipelineStageIndex = -1;

    public Dictionary<Type, int> GlobalStructTypes
    {
      get
      {
        return new Dictionary<Type, int>(){
          {typeof(GpuWorld), 1},
          {typeof(GpuMaterial), 1},
          {typeof(GpuCamera), 1},
          {typeof(GpuDebug), 1},
          {typeof(GpuDirLight), MaxDirLights},
          {typeof(GpuPointLight), MaxPointLights},
          {typeof(GpuFaceData), 0},
        };
      }
    }
    public Dictionary<Type, int> InstanceDataStructTypes
    {
      get
      {
        return new Dictionary<Type, int>(){
          {typeof(GpuInstanceData), MaxInstances},
        };
      }
    }
    public ShaderControlVars() { }
    public ShaderControlVars(PipelineStageEnum stage, int stageindex, ShaderType type)
    {
      ShaderType = type;
      PipelineStageEnum = stage;
      PipelineStageIndex = stageindex;
    }
    public ShaderControlVars Clone(PipelineStageEnum stage, int stageindex, ShaderType type)
    {
      Gu.Assert(stage != null);

      var ret = new ShaderControlVars(stage, stageindex, type);
      ret.MaxPointLights = this.MaxPointLights;
      ret.MaxDirLights = this.MaxDirLights;
      ret.MaxCubeShadowSamples = this.MaxCubeShadowSamples;
      ret.MaxFrusShadowSamples = this.MaxFrusShadowSamples;
      return ret;
    }

    private List<string> _defines = new List<string>();

    private void AddDef<T>(StringBuilder sb, string key, T val)
    {
      sb.Append("#define " + key + " " + val.ToString() + "\n");
      _defines.Add(key);
    }
    private void AddDef(StringBuilder sb, string key)
    {
      sb.Append("#define " + key + "\n");
      _defines.Add(key);
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

      ShaderType st = ShaderType.Value;
      if (st == OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader || st == OpenTK.Graphics.OpenGL4.ShaderType.FragmentShaderArb)
      {
        AddDef(sb, "DEF_SHADER_STAGE_FRAGMENT");
      }
      else if (st == OpenTK.Graphics.OpenGL4.ShaderType.VertexShader || st == OpenTK.Graphics.OpenGL4.ShaderType.VertexShaderArb)
      {
        AddDef(sb, "DEF_SHADER_STAGE_VERTEX");
      }
      else if (st == OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader)
      {
        AddDef(sb, "DEF_SHADER_STAGE_GEOMETRY");
      }
      else { Gu.BRThrowNotImplementedException(); }

      //Ooh.thanks Description[]
      AddDef(sb, PipelineStageEnum.Description());

      return sb.ToString();
    }
    public string OutputsString()
    {
      StringBuilder sb = new StringBuilder();

      var pstage = Gu.Context.Renderer.GetPipelineStage(PipelineStageIndex);

      Gu.Assert(pstage != null);

      //Get all pipeline outputs and set blank set() functions for outputs that are disabled in the given stage.
      //HashSet<string> outputs = new HashSet<string>();
      var all_outputs = Gu.Context.Renderer.GetAllUniqueAttachments();
      var unique_outputs = new Dictionary<string, PipelineAttachment>();
      foreach (var pp in all_outputs)
      {
        if (!unique_outputs.ContainsKey(pp.ShaderOutput.Description()))
        {
          unique_outputs.Add(pp.ShaderOutput.Description(), pp);
        }
      }
      if (pstage.OutputFramebuffer == null)
      {
        SetOutputString(sb, 0, "vec4", ShaderOutput.Color.Description());//default fbo
        Gu.Assert(unique_outputs.Remove(ShaderOutput.Color.Description()));//output must be in the global outputs array
      }
      else
      {
        foreach (var output in pstage.OutputFramebuffer.Bindings)
        {
          var outname = output.Attachment.ShaderOutput.Description();
          Gu.Assert(unique_outputs.Remove(outname));//output must be in the global outputs array
          string datatype = PixelInternalFormatToShaderDataType(output.Attachment.Texture.PixelInternalFormat);
          SetOutputString(sb, output.LayoutIndex, datatype, outname);
        }
      }

      foreach (var output in unique_outputs)
      {
        string datatype = PixelInternalFormatToShaderDataType(output.Value.Texture.PixelInternalFormat);
        sb.Append("void setOutput_" + output.Value.ShaderOutput.Description() + "(in " + datatype + " val) { }\n");
      }
      return sb.ToString();
    }
    public string InputsString()
    {
      StringBuilder sb = new StringBuilder();

      var pstage = Gu.Context.Renderer.GetPipelineStage(PipelineStageIndex);

      Gu.Assert(pstage != null);

      //Get all pipeline outputs and set blank set() functions for outputs that are disabled in the given stage.
      //HashSet<string> outputs = new HashSet<string>();
      foreach (var input in pstage.Inputs)
      {
        string sampler = "sampler2D";
        string output_type = "vec4";
        string func = "texture";
        string input_type = "vec2";
        string bias = "";
        string swizzle = "";
        string coord = "val";
        if (input.Texture.PixelInternalFormat == PixelInternalFormat.Rgba16f ||
            input.Texture.PixelInternalFormat == PixelInternalFormat.Rgba32f)
        {
          sampler = "sampler2D";
          output_type = "vec4";
        }
        else if (input.Texture.PixelInternalFormat == PixelInternalFormat.R32ui)
        {
          sampler = "usampler2D";
          output_type = "uint";
          swizzle = ".r";
        }
        else if (input.Texture.PixelInternalFormat == PixelInternalFormat.DepthComponent32f)
        {
          //The result of accessing a shadow texture is always a single float value. This value is on the range [0, 1],
          // which is proportional to the number of samples in the shadow texture that pass the comparison. Therefore, 
          //if the resulting value is 0.25, then only 1 out of the 4 values sampled by the comparison operation passed. 
          //https://stackoverflow.com/questions/48551587/shadowmapping-and-sampler2dshadow
          sampler = "sampler2DShadow";
          output_type = "float";
          func = "texture";
          input_type = "vec4";
          coord = "vec3(val.xy/val.w, val.z/val.w)";
        }
        else
        {
          Gu.Log.Error($"Unsupported pixel output format for stage:{pstage.Name}, texture:{input.Texture.Name}, format:{input.Texture.PixelInternalFormat.ToString()}");
          Gu.DebugBreak();
        }

        Gu.Assert(input.ShaderInput != null);
        var uniform_name = input.ShaderInput.Description();
        var inpts = input.ShaderInput.ToString();
        if (inpts == "Albedo")
        {
          inpts = "Color";
        }

        sb.AppendLine($"uniform {sampler} {uniform_name};");
        string gl_textureFunc = $"{func}({uniform_name}, {coord}{bias}){swizzle}";
        string base_inputFuncName = $"getInput_{inpts}";
        string HDR_inputFuncName = $"getInput_{inpts}HDR";
        sb.AppendLine($"{output_type} {base_inputFuncName}({input_type} val){{ return {gl_textureFunc}; }}");
        sb.AppendLine($"{output_type} {HDR_inputFuncName}({input_type} val){{ return {base_inputFuncName}(val); }}");
      }

      return sb.ToString();
    }
    public string ConstantsString()
    {
      //shaders have a limit on their constant data so these must be spared
      StringBuilder sb = new StringBuilder();

      var pstage = Gu.Context.Renderer.GetPipelineStage(PipelineStageIndex);
      Gu.Assert(pstage != null);

      bool pickflags_set = false;

      if (pstage.InputFramebuffer != null)
      {
        foreach (var b in pstage.InputFramebuffer.Bindings)
        {
          if (pickflags_set == false && b.Attachment.TargetType == RenderTargetType.Pick)
          {
            //for pick/selection shader
            sb.AppendLine($"bool isPicked(uint pickid) {{ return ({Picker.c_iPickedFlag} & pickid) > 0; }}");
            sb.AppendLine($"bool isSelected(uint pickid) {{ return ({Picker.c_iSelectedFlag} & pickid) > 0; }}");
            sb.AppendLine($"bool isActive(uint pickid) {{ return ({Picker.c_iActiveFlag} & pickid) > 0; }}");
            pickflags_set = true;

            break;
          }
        }
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
      else if (fmt == PixelInternalFormat.Rgba16f) { return "vec4"; }
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
    public Texture EnvironmentMap { get { return _environmentMap; } set { _environmentMap = value; SetModified(); } }
    public Texture IrradianceMap { get { return _irradianceMap; } set { _irradianceMap = value; SetModified(); } }
    public float FogDamp { get { return _fogDamp; } set { _fogDamp = value; SetModified(); } }
    public float FogBlend { get { return _fogBlend; } set { _fogBlend = value; SetModified(); } }
    public float FogDivisor { get { return _fogDivisor; } set { _fogDivisor = value; SetModified(); } }
    public vec3 FogColor { get { return _fogColor; } set { _fogColor = value; SetModified(); } }
    public vec3 Ambient { get { return _ambient; } set { _ambient = value; SetModified(); } }
    public float AmbientIntensity { get { return _ambientIntensity; } set { _ambientIntensity = value; SetModified(); } }
    public DayNightCycle DayNightCycle { get { return _dayNightCycle; } set { _dayNightCycle = value; SetModified(); } }
    public ModifiedList<WorldObject> Lights { get { return _lights; } set { _lights = value; SetModified(); } }
    public GpuWorld GpuWorld { get { return _gpuWorld; } }
    public GpuDirLight[] GpuDirLights { get { return _gpuDirLights; } }
    public GpuPointLight[] GpuPointLights { get { return _gpuPointLights; } }
    public GpuDebug GpuDebug { get; private set; } = new GpuDebug();//just set it directly

    #endregion
    #region Private: Members

    private Texture _environmentMap = null;
    private Texture _irradianceMap = null;
    private float _fogDamp = 2.8f;
    private float _fogBlend = 0.56361f;
    private float _fogDivisor = 1200.0f; //Begin of fog distance
    private vec3 _fogColor = new vec3(0.8407f, 0.89349f, 0.981054f);
    private vec3 _ambient = new vec3(1, 1, 1);
    private float _ambientIntensity = 0.13f;
    private DayNightCycle _dayNightCycle = null;
    private ModifiedList<WorldObject> _lights = new ModifiedList<WorldObject>();
    private GpuWorld _gpuWorld = new GpuWorld();
    private GpuDirLight[] _gpuDirLights = null;
    private GpuPointLight[] _gpuPointLights = null;

    #endregion

    public void ClearLights()
    {
      _lights.Clear();
      SetModified();
    }

    protected WorldProps() { }
    public WorldProps(string name) : base(name) { }
    public void CompileGpuData()
    {
      if (Modified || Lights.Modified || Gu.EngineConfig.Debug_AlwaysCompileAndReloadGpuUniformData)
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
            _gpuPointLights[light]._color = _lights[light].LightColor;
            _gpuPointLights[light]._pos = _lights[light].Position_World;
            _gpuPointLights[light]._power = _lights[light].LightPower;
            _gpuPointLights[light]._radius = _lights[light].LightRadius;
          },
          (light) =>
          {
            _gpuDirLights[light]._color = _lights[light].LightColor;
            _gpuDirLights[light]._dir = _lights[light].Heading;
            _gpuDirLights[light]._pos = _lights[light].Position_World;
            _gpuDirLights[light]._power = _lights[light].LightPower;
            _gpuDirLights[light]._radius = _lights[light].LightRadius;
            _gpuWorld._iDirLightCount++;
          });

        //**Leave these as default
        //_gpuWorld._fHdrSampleExp = 1.1f;
        //_gpuWorld._fHdrGamma = 2.2f;
        //_gpuWorld._fHdrExposure = 1.1f;

        _gpuWorld._iShadowBoxCount = 0;
        _gpuWorld._fTimeSeconds = (float)(Gu.Milliseconds() % 1000) / 1000.0f;
        _gpuWorld._fFocalDepth = 0;
        _gpuWorld._fFocalRange = 25.0f;

        ClearModified();
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
          if (light.LightType == LightType.Point)
          {
            point(iLight);
          }
          else if (light.LightType == LightType.Direction)
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
    //A list of instanced objects that all share the same mesh material
    public List<Drawable> Objects = null;
    public GpuInstanceData[] GpuInstanceData = null;
    public void Add(Drawable ob)
    {
      Objects = Objects.ConstructIfNeeded();
      Objects.Add(ob);
      SetModified();
    }
    public void CompileGpuData()
    {
      if (Modified || Gu.EngineConfig.Debug_AlwaysCompileAndReloadGpuUniformData)
      {
        Gu.Assert(Objects != null);
        GpuInstanceData = new GpuInstanceData[Objects.Count];
        for (int iob = 0; iob < Objects.Count; iob++)
        {
          Gu.Assert(Objects[iob] != null);
          //we could have the objects also contain a GpuInstanceData themselves.. this may be too much extra data though
          GpuInstanceData[iob]._model = Objects[iob].WorldMatrix;
          GpuInstanceData[iob]._model_inverse = GpuInstanceData[iob]._model.inverseOf();//We can cache this
          GpuInstanceData[iob]._pickId.x = Picker.AddFlagsToPickID(Objects[iob].PickId, Objects[iob].Pickable, Objects[iob].IsSelected, Objects[iob].IsPicked, Objects[iob].IsActive);
        }
        Modified = false;
      }
    }
  }
  public class DrawCall : MutableState
  {
    //needs to be some kind of depth sorting..
    public GpuMaterial GpuMaterial { get { return _gpuMaterial; } }

    private GpuMaterial _gpuMaterial = new GpuMaterial();
    private Dictionary<Material, Dictionary<MeshView, VisibleObjectInstances>> _matMeshInstances { get; set; } = null;

    public DrawCall() { }
    public DrawCall(List<Drawable> obs)
    {
      foreach (var ob in obs)
      {
        AddVisibleObject(ob);
      }
    }
    public static void Draw(WorldProps p, RenderView rv, Drawable ob)
    {
      DrawCall dc = new DrawCall();
      dc.AddVisibleObject(ob);
      dc.Draw(p, rv);
    }
    public void Draw(WorldProps wp, RenderView rv, Material? customMaterial = null)
    {
      if (_matMeshInstances != null)
      {
        CompileGpuData();

        foreach (var mk in _matMeshInstances)
        {
          if (customMaterial != null)
          {
            // Draw for a given material / shader
            DrawForMaterial(wp, rv, customMaterial, mk.Value);
          }
          else
          {
            // Draw object material
            DrawForMaterial(wp, rv, mk.Key, mk.Value);
          }
        }
      }
    }
    private void DrawForMaterial(WorldProps wp, RenderView rv, Material mat, Dictionary<MeshView, VisibleObjectInstances> mesh_instances)
    {
      mat.GpuRenderState.SetState();
      var cs = mat.Shader.GetShaderForCurrentContext();
      cs.BeginRender(wp, rv, mat);

      foreach (var ob_set in mesh_instances)
      {
        if (ob_set.Value.Objects[0] is Drawable)
        {
          Gu.Trap();
        }
        var mesh_view = ob_set.Key;
        ob_set.Value.CompileGpuData();
        cs.BindInstanceUniforms(ob_set.Value.GpuInstanceData);

        Gu.Assert(mesh_view != null);
        Gu.Assert(mesh_view.MeshData != null);
        cs.BindMeshUniforms(mesh_view.MeshData);

        cs.CheckAllRequiredDataWasSet();

        mesh_view.Draw(ob_set.Value.GpuInstanceData, mat.Shader.GSPrimType);
      }
      cs.EndRender();
    }
    public void CompileGpuData()
    {
      if (Modified || Gu.EngineConfig.Debug_AlwaysCompileAndReloadGpuUniformData)
      {
        if (_matMeshInstances != null)
        {
          foreach (var mat_dic in _matMeshInstances)
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
      if (_matMeshInstances != null)
      {
        _matMeshInstances.Clear();
        SetModified();
      }
    }
    public void AddVisibleObject(Drawable ob, Material? customMaterial = null)
    {
      Material? mat = ob.Material;
      if (customMaterial != null)
      {
        mat = customMaterial;
      }

      Gu.Assert(mat != null);
      Gu.Assert(ob.MeshView != null);
      Gu.Assert(ob.MeshView.MeshData != null);

      _matMeshInstances = _matMeshInstances.ConstructIfNeeded();
      Dictionary<MeshView, VisibleObjectInstances>? meshInstances = null;
      if (!_matMeshInstances.TryGetValue(mat, out meshInstances))
      {
        meshInstances = meshInstances.ConstructIfNeeded();
        _matMeshInstances.Add(mat, meshInstances);
      }
      VisibleObjectInstances? objList = null;
      if (!meshInstances.TryGetValue(ob.MeshView, out objList))
      {
        objList = objList.ConstructIfNeeded();
        meshInstances.Add(ob.MeshView, objList);
      }
      objList.Add(ob);
      SetModified();
    }

  }//drawca
  public class ShaderStage : OpenGLResource
  {
    public string InfoLog { get { return _errors; } }
    public bool Success { get { return _success; } }
    private string _errors = "";
    private bool _success = false;
    public ShaderType ShaderType { get; private set; } = ShaderType.VertexShader;
    public ShaderStage(string name, ShaderType tt, string src) : base(name)
    {
      Gpu.CheckGpuErrorsRt();

      //Here: we can load from shader cache.
      ShaderType = tt;
      _glId = GT.CreateShader(tt);
      Gpu.CheckGpuErrorsRt();
      GL.ShaderSource(_glId, src);
      Gpu.CheckGpuErrorsRt();
      GL.CompileShader(_glId);
      Gpu.CheckGpuErrorsRt();
      int length = 0;
      int bufsize = 8192 * 2 * 2;
      GL.GetShaderInfoLog(_glId, bufsize, out length, out _errors);
      if (length >= bufsize - 1)
      {
        Gu.DebugBreak();
      }
      Gpu.CheckGpuErrorsRt();
      GL.GetShader(_glId, ShaderParameter.CompileStatus, out int stat);
      _success = (stat == (int)GLenum.GL_TRUE);
      Gpu.CheckGpuErrorsRt();
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsShader(_glId))
      {
        GT.DeleteShader(_glId);
      }
    }
  }
  public abstract class ShaderDataBlock : OpenGLResource
  {
    public bool HasBeenSet { get; set; } = false; //TODO: share shaderUniform and ShaderMemory block data like hasbeenset and wasskipped
    public bool WasSkipped { get; set; } = false;
    public bool Active { get; private set; } = false;
    public int SizeBytes { get; private set; } = 0;
    public ShaderDataBlock(string name, bool active, int size_bytes)
      : base(name)
    {
      Active = active; SizeBytes = size_bytes;
    }
  }
  public class ShaderUniform : ShaderDataBlock
  {
    public int Location { get; private set; } = 0;
    //public string Name { get; private set; } = "unset";
    public string Value { get; private set; } = "unset";
    public ActiveUniformType Type { get; private set; } = ActiveUniformType.Int;

    public ShaderUniform(int location, int u_size, ActiveUniformType u_type, string u_name, bool active)
      : base(u_name, active, u_size)
    {
      Location = location;
      Type = u_type;
    }

    public override void Dispose_OpenGL_RenderThread()
    {
    }
  }
  public abstract class ShaderMemoryBlock : ShaderDataBlock
  {
    public int BlockIndex { get; private set; } = -1;
    public int BindingIndex { get; private set; } = -1;
    public GPUBuffer Buffer { get; protected set; } = null;//Optional buffer to copy to, or we can set it from mesh data, or elsewhere

    public override void Dispose_OpenGL_RenderThread()
    {
      Buffer = null;
    }

    public ShaderMemoryBlock(string name, int iBlockIndex, int iBindingIndex, int iBufferByteSize, bool active)
      : base(name, active, iBufferByteSize)
    {
      BindingIndex = iBindingIndex;
      BlockIndex = iBlockIndex;
    }
    public abstract GPUBuffer GetOrCreateBuffer(int size);
  }
  public class ShaderUniformBlock : ShaderMemoryBlock
  {
    public ShaderUniformBlock(string name, int iBlockIndex, int iBindingIndex, int iBufferByteSize, bool active) :
    base(name, iBlockIndex, iBindingIndex, iBufferByteSize, active)
    {
    }
    public override GPUBuffer GetOrCreateBuffer(int size)
    {
      Gu.Assert(size <= SizeBytes);
      if (Buffer == null)
      {
        Buffer = Gpu.CreateUniformBuffer(Name, SizeBytes, 1);
      }
      return Buffer;
    }
  }
  public class ShaderStorageBlock : ShaderMemoryBlock
  {
    public ShaderStorageBlock(string name, int iBlockIndex, int iBindingIndex, int iBufferByteSize, bool active) :
    base(name, iBlockIndex, iBindingIndex, iBufferByteSize, active)
    {
    }
    public override GPUBuffer GetOrCreateBuffer(int size)
    {
      Gu.Assert(size <= SizeBytes);
      if (Buffer == null)
      {
        Buffer = Gpu.CreateShaderStorageBuffer(this.Name, SizeBytes, 1);
      }
      return Buffer;
    }
  }
  public class ShaderAttrib
  {
    public ActiveAttribType ActiveAttribType { get; private set; }
    public int Size { get; private set; }
    public int Index { get; private set; }
    public ShaderAttrib(int index, int size, ActiveAttribType type)
    {
      Index = index;
      Size = size;
      ActiveAttribType = type;
    }
  }
  public class GpuShader : OpenGLResource
  {
    #region Static: Members

    //Shader, program on the GPU based on GL Context...
    //Assuming if I'm right, probably not, that OpenTK's context sharing can't share OpenGL programs.
    //So we need one per context, and the way the system generates shaders, we need an additional shader per pipeline stage.
    // Context -> Pipeline Stage -> Shader
    private static string c_strGlobalDefineString = "<GLSL_CONTROL_DEFINES_HERE>";
    private static string c_strGlobalConstantsString = "<GLSL_CONTROL_CONSTANTS_HERE>";
    private static string c_strGlobalInputString = "<GLSL_CONTROL_INPUTS_HERE>";
    private static string c_strGlobalOutputString = "<GLSL_CONTROL_OUTPUTS_HERE>";
    private static string c_strGlobalMaterialTextureString = "<GLSL_CONTROL_INPUT_MATERIAL_TEXTURES>";
    private static string c_strBufferBindingString = "<BUFFER_BINDING_ID>";
    private static string c_strGlobalStructsString = "<GLSL_GLOBAL_STRUCTS>";
    private static string c_strInstanceDataStructString = "<GLSL_INSTANCE_DATA_STRUCT>";

    #endregion
    #region Public: Members

    public int PipelineStageIndex { get; private set; } = -1;
    public PipelineStageEnum PipelineStageEnum { get; private set; } = PipelineStageEnum.MaxPipelineStages;
    public ShaderLoadState State
    {
      get { return _state; }
      private set
      {
        if (value == ShaderLoadState.Failed)
        {
          Gu.Trap();
        }
        _state = value;
      }
    }

    #endregion
    #region Private: Members

    private List<ShaderStage> _stages = null;
    private List<ShaderAttrib> _attribs = new List<ShaderAttrib>();
    private Dictionary<string, ShaderStorageBlock> _ssbos = new Dictionary<string, ShaderStorageBlock>();
    private Dictionary<string, ShaderUniform> _uniforms = new Dictionary<string, ShaderUniform>();
    private Dictionary<string, ShaderUniformBlock> _uniformBlocks = new Dictionary<string, ShaderUniformBlock>();
    private List<ShaderDataBlock> _allActiveDataBlocks = new List<ShaderDataBlock>();

    private int _maxBufferBindingIndex = -1;//temp used to prevent high binding index
    private TextureUnit _currUnit = TextureUnit.Texture0;
    private Dictionary<TextureUnit, Texture> _boundTextures = new Dictionary<TextureUnit, Texture>();//Technically this is a GL context thing. But it's ok for now.
    private List<string> _programErrors = new List<string>();
    private ShaderLoadState _state = ShaderLoadState.None;

    #endregion
    #region Public: Methods

    public GpuShader(string name, WindowContext ct, Dictionary<FileLoc, ShaderSrc> srcs, DateTime maxmodifytime, int stageindex, bool is_hot_reload)
      : base(name)
    {
      Gpu.CheckGpuErrorsRt();

      Gu.Assert(ct != null);
      Gu.Assert(ct.Renderer != null);

      PipelineStageEnum = Gu.Context.Renderer.PipelineStages[stageindex].PipelineStageEnum;
      PipelineStageIndex = stageindex;

      Gu.Log.Info($"{Name}: ..Compiling shader.");
      Gu.Log.Info($"{Name}: ..Context = {ct.Name}");
      Gu.Log.Info($"{Name}: ..Stage = {PipelineStageEnum.Description()}");

      bool loaded_binary = false;

      //Check for Preprocessor Changes, Binary changes, load binary if no changes
      loaded_binary = CheckCachedShader(ct, srcs, maxmodifytime, is_hot_reload);

      // Re-Compile binary
      if (!loaded_binary)
      {
        CompileShaderProgram(ct, srcs, is_hot_reload);
      }

      if (State == ShaderLoadState.Validated)
      {
        LoadShaderToGPU(loaded_binary);
      }

      if (State != ShaderLoadState.Success)
      {
        //Finally delete cached files if we failed.
        DeleteCachedSourceFiles(srcs);
      }
    }
    private bool CheckCachedShader(WindowContext ct, Dictionary<FileLoc, ShaderSrc> srcs, DateTime maxmodifytime, bool is_hot_reload)
    {
      bool loaded_binary = false;
      if (Gu.EngineConfig.EnableShaderCaching)
      {
        bool can_load_binary = true;
        if (is_hot_reload == false)
        {
          PreprocessSourceFiles(ct, srcs);
          can_load_binary = CheckForPrecompilerChanges(ct, srcs);
        }

        //Attempt to load cached binary if no preproc changes
        if (can_load_binary)
        {
          loaded_binary = CheckLoadCachedBinary(maxmodifytime);
        }
      }
      return loaded_binary;
    }
    private void CompileShaderProgram(WindowContext ct, Dictionary<FileLoc, ShaderSrc> srcs, bool is_hot_reload)
    {
      Gpu.CheckGpuErrorsRt();
      _programErrors.Clear();
      Gu.Assert(srcs.Count > 0);

      if (is_hot_reload == false)
      {
        //recompile source, and check against existing cached source.
      }
      PreprocessSourceFiles(ct, srcs);
      CacheSourceFiles(srcs);

      Gu.Log.Info($"{Name}: ..Fully compiling shader.");
      if (CreateShaders(srcs, is_hot_reload))
      {
        CreateProgramFromShaders();
        ValidateProgram();
        PrintProgramErrors(srcs, is_hot_reload);
      }

    }
    private void LoadShaderToGPU(bool loaded_binary)
    {
      Gu.Log.Info($"{Name}: Creating Shader Data.");
      Bind();
      ParseAttribs();
      ParseUniforms();
      ParseUniformBlocks();
      ParseSSBOs();
      MakeListOfActiveDataBlocks();

      if (State == ShaderLoadState.Validated)
      {
        Unbind();
        SetObjectLabel();

        State = ShaderLoadState.Success;

        Gu.Log.Info($"{Name}: ..Succssfully processed and loaded shader to GPU (glId={GlId})");
        if (!loaded_binary && Gu.EngineConfig.EnableShaderCaching)
        {
          SaveProgramBinary();
        }
      }
      Gpu.CheckGpuErrorsRt();
    }
    private void DeleteCachedSourceFiles(Dictionary<FileLoc, ShaderSrc> srcs)
    {
      //FileLoc binloc = GetBinaryLocation();
      //Gu.Log.Info($"{Name}: ..Deleting binary to {binloc.QualifiedPath}.");
      CacheSourceFiles(srcs, true);
    }
    private void PrintProgramErrors(Dictionary<FileLoc, ShaderSrc> srcs, bool is_hot_reload)
    {
      if (State == ShaderLoadState.Failed)
      {
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in srcs)
        {
          AppendShaderDebugSource(sb, kvp.Key, kvp.Value);
        }
        sb.AppendLine(String.Join(Environment.NewLine, _programErrors.ToArray()));
        Gu.Log.Error($"{Name}: ..Failed to load shader '{Name}'.{Environment.NewLine}{sb.ToString()}");

        // only break on first compile. If we're a hot reload don't break.
        if (is_hot_reload == false)
        {
          Gu.DebugBreak();
        }
      }
    }
    private void AppendShaderDebugSource(StringBuilder sb, FileLoc fn, ShaderSrc src)
    {
      string blip = "--------------------------------------------------------------------------------------";

      //dont put any text at the top of the source!!! the line number is the error line!!
      var lines = src._src.Split('\n');
      for (int iLine = 0; iLine < lines.Length; iLine++)
      {
        string line = lines[iLine];
        string r = String.Format("{0,5}", (iLine + 1).ToString());
        sb.Append("/*!!!COMPILED!!!*/ ");
        sb.Append(r);
        sb.Append("  ");
        sb.Append(line);
        sb.Append(Environment.NewLine);
      }
      sb.AppendLine(blip);
      sb.AppendLine($"--{fn.FileName} DEBUG SOURCE--");
      sb.AppendLine(blip);
    }
    private void PreprocessSourceFiles(WindowContext ct, Dictionary<FileLoc, ShaderSrc> srcs)
    {
      foreach (var kvp in srcs)
      {
        Gu.Assert(kvp.Value.ShaderType != null);//must not be header
        Gu.Assert(kvp.Value._srcFile.IsNotEmpty());//file must have loaded data
        kvp.Value._src = ProcessShaderSource(ct, kvp.Value._srcFile, kvp.Value.ShaderType.Value, PipelineStageEnum, PipelineStageIndex);
      }
    }
    private bool CheckForPrecompilerChanges(WindowContext ct, Dictionary<FileLoc, ShaderSrc> srcs)
    {
      //If this is the first time, we must check the precompiled source against the cached source for changes.
      //Hot reload can skip this.
      bool can_load_binary = true;
      foreach (var kvp in srcs)
      {
        var cachepath = GetShaderSourceFilePath(kvp.Value, Gu.LocalCachePath);
        if (!System.IO.File.Exists(cachepath))
        {
          Gu.Log.Debug($"{Name}:{kvp.Key.FileName}: Cached source does not exist, must recompile.");
          can_load_binary = false;
          break;
        }
        else
        {
          try
          {
            string src = System.IO.File.ReadAllText(cachepath);
            bool notchanged = StringUtil.Equals(src, kvp.Value._src);
            if (notchanged)
            {
              Gu.Log.Debug($"{Name}:{kvp.Key.FileName}: Cached source has not changed.");
            }
            else
            {
              Gu.Log.Debug($"{Name}:{kvp.Key.FileName}: Cached source changed, must recompile shader program.");
              can_load_binary = false;
              break;
            }
          }
          catch (Exception ex)
          {
            Gu.Log.Error(ex);
            can_load_binary = false;
          }
        }
      }
      return can_load_binary;
    }
    private void CacheSourceFiles(Dictionary<FileLoc, ShaderSrc> srcs, bool delete = false)
    {
      foreach (var kvp in srcs)
      {
        if (Gu.EngineConfig.EnableShaderCaching)
        {
          string cachefile = GetShaderSourceFilePath(kvp.Value, Gu.LocalCachePath);

          if (delete)
          {
            Gu.SafeDeleteLocalFile(cachefile);
          }
          else
          {
            Gu.Log.Info($"{Name}: Caching shader source to '{cachefile}'");
            SaveShaderSource(kvp.Value._src, cachefile, null);
          }
        }
      }
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      DestroyForGC();//Call again if it hasn't been.
      DeleteProgram();
    }
    public void DestroyForGC()
    {
      _stages?.Clear();
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
      foreach (var u in _allActiveDataBlocks)
      {
        u.HasBeenSet = false;
        u.WasSkipped = false;
      }
      _currUnit = TextureUnit.Texture0;
      _boundTextures.Clear();
    }
    public void DispatchCompute(int elementCount)
    {
      double f;
      int remainder = elementCount;
      int x = 0, y = 0, z = 0;
      int nf, npow;

      for (int xi = 0; Gu.WhileTrueGuard(xi, Gu.c_intMaxWhileTrueLoopLONG) && remainder > 0; xi++)
      {
        f = Math.Pow((double)remainder, 1.0 / 3.0);
        if (f < 1.0f)
        {
          // add the remaining to X
          if (x + remainder < Gu.Context.Gpu.MaxWorkGroupDims[0])
          {
            x += remainder;
            remainder = 0;
            break;
          }
          else
          {
            Gu.BRThrowException("Compute shader distribution was out of range.");
          }
        }
        nf = (int)f;
        npow = nf * nf * nf;
        remainder = remainder - npow;
        x += nf;
        y += nf;
        z += nf;
      }
      int tv = x * y * z;

      DispatchCompute(x, y, z);
    }
    public GpuComputeSync DispatchCompute(int x, int y, int z)
    {
      if ((x > Gu.Context.Gpu.MaxWorkGroupDims[0]) || (y > Gu.Context.Gpu.MaxWorkGroupDims[1]) || (z > Gu.Context.Gpu.MaxWorkGroupDims[2]))
      {
        Gu.BRThrowException($"[Compute] group {x},{y},{z} greater than max work group GPU can handle which is " +
        $"{Gu.Context.Gpu.MaxWorkGroupDims[0]},{Gu.Context.Gpu.MaxWorkGroupDims[1]},{Gu.Context.Gpu.MaxWorkGroupDims[2]}");
      }
      else if ((x == 0) || (y == 0) || (z == 0))
      {
        Gu.BRThrowException("[Compute] Can't dispatch a compute with a zero dimension brosaurus. if need be use glDisbatchCompute(x,1,1)");
      }

      Bind();
      GL.DispatchCompute(x, y, z);
      GpuComputeSync sync = new GpuComputeSync();
      sync.CreateFence();
      Unbind();

      // getContext()->glDispatchCompute(x, y, z);
      return sync;

      // Gu::checkErrorsDbg();
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
    public void BindMeshUniforms(MeshData m)
    {
      Gu.Assert(m != null);
      if (m.FaceData != null)
      {
        BindSSBOBlock(ShaderUniformName._ufGpuFaceData_Block.Description(), m.FaceData);
      }
    }
    public void CheckAllRequiredDataWasSet()
    {
      string notset = "";
      int n_unset = 0;
      foreach (var u in _allActiveDataBlocks)
      {
        Gu.Assert(u.Active == true);//must be active to be in here.
        if (u.HasBeenSet == false && u.WasSkipped == false)
        {
          notset += " " + u.Name + ",";
          n_unset++;
        }
      }

      if (notset.Length > 0)
      {
        Gu.Log.Warn($"{Name}: {n_unset} Uniforms were not set: {notset}");
        Gu.DebugBreak();
      }
    }


    #endregion
    #region Public: Static Methods

    public static string IncludeHeaders(FileLoc loc, List<string> errors, Dictionary<FileLoc, ShaderSrc> uniqueFiles)
    {
      var file_lines = new StringBuilder();

      IncludeHeaders(loc, file_lines, errors, uniqueFiles);

      return file_lines.ToString();
    }

    #endregion
    #region Private: Methods

    private void SaveShaderSource(string src, string fn, string? errors)
    {
      string src_witherrors = src + ((errors == null) ? "" : (Environment.NewLine + errors));
      System.IO.File.WriteAllText(fn, src_witherrors);
    }
    private string ProcessShaderSource(WindowContext ct, string src_raw, ShaderType type, PipelineStageEnum stage, int stageindex)
    {
      string src_cpy = src_raw;
      if (StringUtil.IsNotEmpty(src_cpy))
      {
        var vars = ct.Renderer.DefaultControlVars.Clone(stage, stageindex, type);


        AddStructs(GpuShader.c_strGlobalStructsString, type, StructStorage.UniformBlock, vars.GlobalStructTypes, ref src_cpy);
        AddStructs(GpuShader.c_strInstanceDataStructString, type, StructStorage.UniformBlock, vars.InstanceDataStructTypes, ref src_cpy);

        CreateBindingIndexes(GpuShader.c_strBufferBindingString, ref src_cpy);

        SetControlVar(GpuShader.c_strGlobalDefineString, vars.DefinesString(), type, ref src_cpy);
        SetControlVar(GpuShader.c_strGlobalInputString, vars.InputsString(), type, ref src_cpy);
        SetControlVar(GpuShader.c_strGlobalOutputString, vars.OutputsString(), type, ref src_cpy);
        SetControlVar(GpuShader.c_strGlobalConstantsString, vars.ConstantsString(), type, ref src_cpy);
        SetControlVar(GpuShader.c_strGlobalMaterialTextureString, GetMaterialInputs(src_raw), type, ref src_cpy);
      }
      return src_cpy;
    }
    private void AddStructs(string tag, ShaderType shadertype, StructStorage storage, Dictionary<Type, int> structs, ref string src_cpy)
    {
      if (CheckTagExists(tag, shadertype, ref src_cpy))
      {
        var ct = StringUtil.StringMatches(src_cpy, tag);
        Gu.Assert(ct.Count == 1, $"Invalid number of '{tag}' tags (count={ct.Count}).");
        StringBuilder sb = new StringBuilder();

        foreach (var kvp in structs)
        {
          var structtype = kvp.Key;
          var count = kvp.Value;
          AddStruct(structtype, storage, count, ref sb);
        }

        src_cpy = src_cpy.Replace(tag, sb.ToString());
      }
    }
    private enum StructStorage { None, UniformBlock, ShaderStorageBufferBlock }
    private void AddStruct(Type struct_type, StructStorage storage, int count, ref StringBuilder sb)
    {
      string structname = struct_type.Name;
      //Thanks .net, this makes things so easy, imagine doing this in c++
      int size = Marshal.SizeOf(struct_type);
      int v4size = Marshal.SizeOf(typeof(vec4));
      Gu.Assert(size % v4size == 0, $"Struct {structname} must be aligned to vec4 sized boundary.");

      Gu.Assert(struct_type.IsValueType, $"Struct {structname} must be value type.");
      bool is_sequential = (struct_type.Attributes & TypeAttributes.SequentialLayout) == TypeAttributes.SequentialLayout;
      Gu.Assert(is_sequential, $"Struct {structname} must  have sequential structlayout attribute.");

      sb.AppendLine($"struct {structname} {{");
      foreach (var field in struct_type.GetFields())
      {
        string typename = CSharpToGLSLTypeName(field.FieldType);
        sb.AppendLine($"  {typename} {field.Name};");
      }
      sb.AppendLine($"}};");

      if (count > 0)
      {
        if (storage == StructStorage.UniformBlock || storage == StructStorage.ShaderStorageBufferBlock)
        {
          if (storage == StructStorage.UniformBlock)
          {
            sb.AppendLine($"layout(std140, binding = <BUFFER_BINDING_ID>) uniform _uf{structname}_Block {{");
          }
          else if (storage == StructStorage.ShaderStorageBufferBlock)
          {
            sb.AppendLine($"layout(std430, binding = <BUFFER_BINDING_ID>) buffer _uf{structname}_Block {{");
          }
          else
          {
            Gu.BRThrowNotImplementedException();
          }
          string index = "";
          if (count > 1)
          {
            index = $"[{count - 1}]";
          }
          sb.AppendLine($"  {structname} _uf{structname}{index};");
          sb.AppendLine($"}};");
        }
        else if (storage == StructStorage.None)
        {
          sb.AppendLine($"//{structname}: No storage defined.");
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
      }
    }
    private string CSharpToGLSLTypeName(Type type)
    {
      //ex: reflection returns Single for typeof(float).Name
      if (type == typeof(Int16)) { return "short"; }
      else if (type == typeof(UInt16)) { return "ushort"; }
      else if (type == typeof(Int32)) { return "int"; }
      else if (type == typeof(UInt32)) { return "uint"; }
      else if (type == typeof(Single)) { return "float"; }
      else if (type == typeof(Double)) { return "double"; }
      else if (type == typeof(vec2)) { return "vec2"; }
      else if (type == typeof(vec3)) { return "vec3"; }
      else if (type == typeof(vec4)) { return "vec4"; }
      else if (type == typeof(ivec2)) { return "ivec2"; }
      else if (type == typeof(ivec3)) { return "ivec3"; }
      else if (type == typeof(ivec4)) { return "ivec4"; }
      else if (type == typeof(uvec2)) { return "uvec2"; }
      else if (type == typeof(uvec3)) { return "uvec3"; }
      else if (type == typeof(uvec4)) { return "uvec4"; }
      else if (type == typeof(dvec2)) { return "dvec2"; }
      else if (type == typeof(dvec3)) { return "dvec3"; }
      else if (type == typeof(dvec4)) { return "dvec4"; }
      else if (type == typeof(mat2)) { return "mat2"; }
      else if (type == typeof(mat3)) { return "mat3"; }
      else if (type == typeof(mat4)) { return "mat4"; }
      else { Gu.BRThrowNotImplementedException(); }
      return "<invalid_type>";
    }
    private void CreateBindingIndexes(string index_identifier, ref string src_cpy)
    {
      for (int binding = 0, idx = src_cpy.IndexOf(index_identifier); idx >= 0; idx = src_cpy.IndexOf(index_identifier), binding++)
      {
        //This is inefficnient these files can be very huge, TODO:optimize
        src_cpy = src_cpy.Substring(0, idx) + binding.ToString() + src_cpy.Substring(idx + index_identifier.Length);
      }
    }
    private string GetMaterialInputs(string src_raw)
    {
      //search for the material _ufGpuMaterial... inside each shader to automatically create material bindings.
      StringBuilder sb = new StringBuilder();
      foreach (var e in Enum.GetValues(typeof(PBRTextureInput)))
      {
        var desc = ((PBRTextureInput)e).Description();
        if (src_raw.Contains(desc))
        {
          sb.AppendLine($"uniform sampler2D {desc};");
        }
      }
      return sb.ToString();
    }
    private void SetControlVar(string tag, string value, ShaderType type, ref string src_cpy)
    {
      if (CheckTagExists(tag, type, ref src_cpy))
      {
        src_cpy = src_cpy.Replace(tag, value);
      }
    }
    private bool CheckTagExists(string tag, ShaderType type, ref string src_cpy)
    {
      if (!src_cpy.Contains(tag))
      {
        PreprocError($"Shader vars tag '{tag}' was not found.", type);
        return false;
      }
      return true;
    }
    private void PreprocError(string e, ShaderType type)
    {
      _programErrors.Add($"{Name} ({type.ToString()}): {e}");
      State = ShaderLoadState.Failed;
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
    private void DeleteProgram()
    {
      if (GL.IsProgram(_glId))
      {
        int p = GL.GetInteger(GetPName.CurrentProgram);
        GL.UseProgram(0);
        GT.DeleteProgram(_glId);
        if (p != _glId)
        {
          GL.UseProgram(p);
        }
      }
      this._glId = 0;
    }
    private bool CreateShaders(Dictionary<FileLoc, ShaderSrc> files, bool is_hot_reload)
    {
      _state = ShaderLoadState.CompilingShaders;
      _stages = _stages.ConstructIfNeeded();
      _stages.Clear();

      foreach (var kvp in files)
      {
        Gu.Assert(kvp.Value.ShaderType != null);
        var stage = new ShaderStage(this.Name + kvp.Value.ShaderType.ToString(), kvp.Value.ShaderType.Value, kvp.Value._src);

        //append possible log warns, however if the shader failed also append the src

        StringBuilder sb = new StringBuilder();

        if (stage.Success == false)
        {
          AppendShaderDebugSource(sb, kvp.Key, kvp.Value);
        }

        if (stage.InfoLog.Length > 0)
        {
          sb.AppendLine(stage.InfoLog);
        }

        if (stage.Success == false)
        {
          _state = ShaderLoadState.Failed;
          if (Gu.EngineConfig.Debug_SaveDebuggShaderSource)
          {
            LaunchFileWithSelectedShaderErrorLine(kvp.Value, sb.ToString(), stage.InfoLog, is_hot_reload);
          }
          Gu.Log.Error(sb.ToString());
        }
        else if (sb.Length > 0)
        {
          Gu.Log.Warn(sb.ToString());
        }

        _stages.Add(stage);
      }

      if (_state == ShaderLoadState.Failed)
      {
        if (is_hot_reload == false)
        {
          Gu.DebugBreak();
        }
      }
      else
      {
        _state = ShaderLoadState.CompiledShadersSuccess;
      }

      return _state != ShaderLoadState.Failed;
    }
    private void LaunchFileWithSelectedShaderErrorLine(ShaderSrc src, string source_with_errors, string infolog, bool is_hot_reload)
    {
      string tmpfile = GetShaderSourceFilePath(src, Gu.LocalTmpPath);
      Gu.Log.Info($"{Name}: Saving debug shader source to '{tmpfile}'");
      SaveShaderSource(source_with_errors, tmpfile, null);

      if (infolog.Length > 0)
      {
        var lines = infolog.Split("\n");
        foreach (var line in lines)
        {
          if (Gu.Context.Gpu.Vendor == GPUVendor.NVIDIA)
          {
            if (line.Contains("error"))
            {

              //NV error string
              //0(467) : error ..
              if (Gu.EngineConfig.Debug_ShowFailedShaderSourceInVSCOode)
              {
                var matches = System.Text.RegularExpressions.Regex.Matches(line, @"^.\(([0-9]+)\)");
                if (matches.Count == 1)
                {
                  if (matches[0].Groups.Count == 2)
                  {
                    var match = matches[0].Groups[1].Value;
                    //Code.exe --goto "C:\file path\filename.txt:450:11" 

                    string nw_aw = "";// "-n";//new window
                    if (!is_hot_reload)//gets annoygin
                    {
                      //   nw_aw += "-w";//wait fro winwo to close
                      // string sleep2 = "";
                      // if(OperatingSystem.GetPlatform()== OperatingSystem.Platform.Linux){
                      //   sleep2 = "sleep 2000 &";//{sleep2}  UGHGHGHGHG
                      // }

                      //So i'm doing this to make it easier in vscode I guess ..
                      //if (Gu.LaunchProgram($"code", $"--goto \"{tmpfile}:{match}:0\" {nw_aw}")) //file:line:char
                      if (Gu.LaunchProgram($"code", $"-g \"{tmpfile}:{match}:0\" {nw_aw}")) //file:line:char
                      {
                        //TODO: Hot Reload + Edit
                        //with -w  vscode will wait to close
                        // we could also edit+ RE-compile the file if we wanted and do that kind of thing. 
                        //We would need the pre-preprocess line. that is for later..
                        //we will probably use something like this for the scripts though .. so it's worth noting to work for both
                        Gu.Log.Info(line);
                      }

                      Gu.DebugBreak();
                      return;
                    }

                  }
                }
              }
              else if (!is_hot_reload)
              {
                Gu.DebugBreak();
                return;
              }
            }
            else
            {
              Gu.Log.Error("ATI &c file open errors aren't supported yet");
              break;
            }
          }
        }
      }
    }
    private void CreateProgramFromShaders()
    {
      Gpu.CheckGpuErrorsRt();
      {
        _glId = GT.CreateProgram();
        foreach (var stage in _stages)
        {
          GL.AttachShader(_glId, stage.GlId);
          Gpu.CheckGpuErrorsRt();
        }
        GL.LinkProgram(_glId);
        Gpu.CheckGpuErrorsRt();
      }
      Gpu.CheckGpuErrorsRt();

      State = ShaderLoadState.Compiled;
    }
    private bool ValidateProgram()
    {
      string programInfoLog = "";
      GL.GetProgramInfoLog(_glId, out programInfoLog);
      if (_programErrors == null)
      {
        _programErrors = new List<string>();
      }
      _programErrors.AddRange(programInfoLog.Split('\n').ToList());
      if (_programErrors.Count > 0 && programInfoLog.ToLower().Contains("error"))
      {
        State = ShaderLoadState.Failed;
      }

      //validate program.
      GL.ValidateProgram(this.GlId);
      Gpu.CheckGpuErrorsRt();

      int iValid = 0;
      GL.GetProgram(this._glId, GetProgramParameterName.ValidateStatus, out iValid);
      Gpu.CheckGpuErrorsRt();

      if (iValid == (int)GLenum.GL_FALSE)
      {
        // Program load faiiled
        _programErrors.Add($"{this.Name}:glValidateProgram failed.  Check the above logs for errors.");
        DeleteProgram();
        State = ShaderLoadState.Failed;
        return false;
      }

      bool b2 = GL.IsProgram(GlId);
      Gpu.CheckGpuErrorsRt();
      if (b2 == false)
      {
        DeleteProgram();
        State = ShaderLoadState.Failed;
        _programErrors.Add($"{this.Name}: glIsProgram failed.");
      }

      //Try binding/using 
      Bind();

      // - If the program failed to load it will raise an error after failing to bind.
      var e = GL.GetError();
      if (e != ErrorCode.NoError)
      {
        State = ShaderLoadState.Failed;
        _programErrors.Add($"{this.Name}: GL error '{((GLenum)e).ToString()}': program was not valid after binding.");
        return false;
      }

      Unbind();

      State = ShaderLoadState.Validated;

      return true;
    }
    private void ParseAttribs()
    {
      int u_count = 0;
      GL.GetProgram(_glId, GetProgramParameterName.ActiveAttributes, out u_count);
      for (var i = 0; i < u_count; i++)
      {
        ActiveAttribType u_type;
        int u_size = 0;

        GL.GetActiveAttrib(_glId, i, out u_size, out u_type);

        _attribs.Add(new ShaderAttrib(i, u_size, u_type));

        //** TODO: this will allow us to match up the attribs with other components.
        //GL.GetAttribLocation(_glId, GetProgramParameterName.attrib)
      }
    }
    private void ParseSSBOs()
    {
      Gpu.CheckGpuErrorsRt();

      //So how we get teh friggin SSBO layout?? I don't know.
      //Apparently you can just set it to whatever (reasonably low value)
      //Would do diligence to check the GPU's limitations on binding points

      for (int i = 0; Gu.WhileTrueGuard(i, Gu.c_intMaxWhileTrueLoop); i++)
      {
        string ssbo_name = "DEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEAD";//idk.
        int u_name_len = 0;
        GL.GetProgramResourceName(_glId, ProgramInterface.ShaderStorageBlock, i, ssbo_name.Length, out u_name_len, out ssbo_name);
        if (GL.GetError() == ErrorCode.NoError)
        {
          var block_index = GL.GetProgramResourceIndex(_glId, ProgramInterface.ShaderStorageBlock, ssbo_name);
          int my_binding = _maxBufferBindingIndex + 1;
          _maxBufferBindingIndex++;
          //This does the same thing as binding=.. apparently. But I believe that we generate bindings in our shader maker so this may be valid.
          GL.ShaderStorageBlockBinding(_glId, block_index, my_binding);
          ShaderStorageBlock sb = new ShaderStorageBlock(ssbo_name, block_index, my_binding, 0, true);
          _ssbos.Add(ssbo_name, sb);
        }
        else
        {
          Gpu.CheckGpuErrorsRt(true, true);
          break;
        }
      }
      Gpu.CheckGpuErrorsRt();

    }
    private void ParseUniforms()
    {
      int u_count = 0;
      GL.GetProgram(_glId, GetProgramParameterName.ActiveUniforms, out u_count);
      Gpu.CheckGpuErrorsRt();
      for (var i = 0; i < u_count; i++)
      {
        ActiveUniformType u_type;
        int u_size = 0;
        string u_name = "DEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEAD";//idk.
        int u_name_len = 0;

        GL.GetActiveUniform(GlId, i, out u_size, out u_type);
        Gpu.CheckGpuErrorsRt();
        GL.GetActiveUniformName(GlId, i, u_name.Length, out u_name_len, out u_name);
        Gpu.CheckGpuErrorsRt();

        if (u_name.Contains("["))
        {
          //This is a unifrom block
          continue;
        }

        bool active = true;
        int location = GL.GetUniformLocation(GlId, u_name);

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
          if (Gu.EngineConfig.Debug_Print_Shader_Uniform_Details_Verbose_NotFound)
          {
            Gu.Log.Debug($"{Name}: .. Inactive uniform: {u_name}");
          }
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
          if (Gu.EngineConfig.Debug_Print_Shader_Uniform_Details_Verbose_NotFound)
          {
            Gu.Log.Debug($"{Name}: .. Active uniform: {u_name}");
          }
        }

        ShaderUniform su = new ShaderUniform(location, u_size, u_type, u_name, active);
        _uniforms.Add(u_name, su);
      }

    }
    private void ParseUniformBlocks()
    {
      int u_block_count = 0;
      GL.GetProgram(_glId, GetProgramParameterName.ActiveUniformBlocks, out u_block_count);
      Gpu.CheckGpuErrorsRt();
      for (var iBlock = 0; iBlock < u_block_count; iBlock++)
      {
        int buffer_size_bytes = 0;
        GL.GetActiveUniformBlock(GlId, iBlock, ActiveUniformBlockParameter.UniformBlockDataSize, out buffer_size_bytes);
        Gpu.CheckGpuErrorsRt();

        int binding = 0;
        GL.GetActiveUniformBlock(GlId, iBlock, ActiveUniformBlockParameter.UniformBlockBinding, out binding);
        Gpu.CheckGpuErrorsRt();

        string u_name = "DEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEADDEAD";//idk.
        int u_name_len = 0;
        GL.GetActiveUniformBlockName(GlId, iBlock, u_name.Length, out u_name_len, out u_name);
        Gpu.CheckGpuErrorsRt();
        u_name = u_name.Substring(0, u_name_len);

        bool active = true;
        if (binding < 0)
        {
          active = false;
          if (Gu.EngineConfig.Debug_Print_Shader_Uniform_Details_Verbose_NotFound)
          {
            Gu.Log.Debug($"{Name}: ..Inactive uniform block: {u_name}");
          }
        }
        else
        {
          if (Gu.EngineConfig.Debug_Print_Shader_Uniform_Details_Verbose_NotFound)
          {
            Gu.Log.Debug($"{Name}: ..Active uniform block: {u_name}");
          }

          _maxBufferBindingIndex = Math.Max(_maxBufferBindingIndex, binding);
        }

        ShaderUniformBlock su = new ShaderUniformBlock(u_name, iBlock, binding, buffer_size_bytes, active);// u_size, u_type, u_name);
        _uniformBlocks.Add(u_name, su);
      }
      //check duplicate binding indexes for blocks
      for (int dupe_loc = 0; dupe_loc < _uniformBlocks.Count; dupe_loc++)
      {
        for (int dupe_loc2 = dupe_loc + 1; dupe_loc2 < _uniformBlocks.Count; dupe_loc2++)
        {
          var ub0 = _uniformBlocks.ElementAt(dupe_loc).Value;
          var ub1 = _uniformBlocks.ElementAt(dupe_loc2).Value;

          if (ub0.BindingIndex == ub1.BindingIndex)
          {
            Gu.Log.Error($"Duplicate Uniform buffer binding index {ub0.BindingIndex} for {ub0.Name} and {ub1.Name} ");
            Gu.DebugBreak();
            this.State = ShaderLoadState.Failed;
          }
        }
      }
    }
    private void MakeListOfActiveDataBlocks()
    {
      foreach (var ub in _uniformBlocks.Values)
      {
        if (ub.Active)
        {
          _allActiveDataBlocks.Add(ub);
        }
      }
      foreach (var ub in _ssbos.Values)
      {
        if (ub.Active)
        {
          _allActiveDataBlocks.Add(ub);
        }
      }
      foreach (var ub in _uniforms.Values)
      {
        if (ub.Active)
        {
          _allActiveDataBlocks.Add(ub);
        }
      }
    }
    private void BindWorldUniforms(WorldProps world)
    {
      Gu.Assert(world != null);
      world.CompileGpuData();
      BindUniformBlock(ShaderUniformName._ufGpuWorld_Block.Description(), new GpuWorld[] { world.GpuWorld });
      BindUniformBlock(ShaderUniformName._ufGpuPointLight_Block.Description(), world.GpuPointLights, world.GpuPointLights.Length == 0);
      BindUniformBlock(ShaderUniformName._ufGpuDirLight_Block.Description(), world.GpuDirLights, world.GpuDirLights.Length == 0);
      BindUniformBlock(ShaderUniformName._ufGpuDebug_Block.Description(), new GpuDebug[] { world.GpuDebug });
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
      if (mat.Name.ToLower().Contains("gear") || mat.Name.ToLower().Contains("plane"))
      {
        int n = 0;
        n++;
      }
      BindUniformBlock(ShaderUniformName._ufGpuMaterial_Block.Description(), new GpuMaterial[] { mat.GpuMaterial });
      foreach (var input in mat.Textures)
      {
        BindTexture(input.Name, input.GetTextureOrDefault());
      }
    }
    private void BindUniform_Mat4(string name, mat4 m)
    {
      if (_uniforms.TryGetValue(name, out var u))
      {
        var mat = m.ToOpenTK();
        GL.UniformMatrix4(u.Location, false, ref mat);
        Gpu.CheckGpuErrorsDbg();
        u.HasBeenSet = true;
      }
    }
    private bool BindUniformBlock<T>(string uname, T[] items, bool canHaveNoItems = false)
    {
      if (_uniformBlocks.TryGetValue(uname, out var block))
      {
        BindUniformBlock(block, items, canHaveNoItems);
        return true;
      }
      else
      {
        ReportUniformNotFound(uname, true);
        return false;
      }
    }
    private void BindUniformBlock<T>(ShaderUniformBlock ub, T[] items, bool canHaveNoItems)
    {
      Gu.Assert(items != null);

      if (items.Length == 0)
      {
        if (canHaveNoItems)
        {
          ub.WasSkipped = true;//avoid reporting if it had nothing
        }
        return;
      }

      Gu.Assert(typeof(T).IsValueType, $"{Name}:'{ub.Name}': Type '{typeof(T).Name}' must be value type.");

      int item_size = Marshal.SizeOf(typeof(T));//default(T) 
      Gu.Assert((ub.SizeBytes % item_size) == 0, $"{Name}:'{ub.Name}': Struct size does not match struct size reported from shader. (Precompiler/cache bug)");

      int num_bytes_to_copy = item_size * items.Length;// dat.instanceData.Length;
      if (num_bytes_to_copy > ub.SizeBytes)
      {
        num_bytes_to_copy = ub.SizeBytes;
        Gu.Log.WarnCycle($"Uniform Block '{ub.Name}' exceeded max count of '{(ub.SizeBytes / item_size)}' items. Tried to copy '{items.Length}' items.");
      }
      var handle = GCHandle.Alloc(items, GCHandleType.Pinned);
      CopyUniformBlockData(ub, handle.AddrOfPinnedObject(), num_bytes_to_copy);
      handle.Free();

      BindBlockFast(ub);
    }
    private void CopyUniformBlockData(ShaderUniformBlock u, IntPtr pData, int copySizeBytes)
    {
      Gu.Assert(copySizeBytes <= u.SizeBytes);

      var ubo = u.GetOrCreateBuffer(copySizeBytes);
      ubo.CopyToGPURaw(pData, 0, 0, copySizeBytes, false);

      if (u.HasBeenSet == true && Gu.EngineConfig.Debug_Print_Shader_Uniform_Details_Verbose_AlreadySet)
      {
        Gu.Log.WarnCycle(this.Name + ": Uniform  " + u.Name + " was already set.", 120 * 10);
      }
      u.HasBeenSet = true;
    }
    private void BindUniformBlock(string uname, GPUBuffer b)
    {
      if (_uniformBlocks.TryGetValue(uname, out var block))
      {
        BindBlockFast(block, b);
      }
      else
      {
        ReportUniformNotFound(uname, true);
      }
    }
    private void BindSSBOBlock(string uname, GPUBuffer b)
    {
      if (_ssbos.TryGetValue(uname, out var block))
      {
        BindBlockFast(block, b);
      }
      else
      {
        ReportUniformNotFound(uname, true);
      }
    }
    private void BindBlockFast(ShaderMemoryBlock u)
    {
      if (u.HasBeenSet == false)
      {
        Gu.Log.WarnCycle(this.Name + ": Shader Uniform Block '" + u.Name + "' value was not set before binding.");
        Gu.DebugBreak();
      }
      BindBlockFast(u, u.Buffer);
    }
    private void BindBlockFast(ShaderMemoryBlock u, GPUBuffer b)
    {
      Gu.Assert(b.RangeTarget != null);
      GL.BindBufferBase(b.RangeTarget.Value, u.BindingIndex, b.GlId);
      Gpu.CheckGpuErrorsDbg();
      GL.BindBuffer(b.BufferTarget, b.GlId);
      Gpu.CheckGpuErrorsDbg();
      u.HasBeenSet = true;
    }
    private void BindTexture(string uniform_name, Texture tex)
    {
      Gu.Assert(_uniforms != null);
      if (_uniforms.TryGetValue(uniform_name, out var su))
      {
        if (tex != null)
        {
          if (su.HasBeenSet && Gu.EngineConfig.Debug_Print_Shader_Uniform_Details_Verbose_AlreadySet)
          {
            Gu.Log.WarnCycle(this.Name + ": Texture uniform " + su.Name + "  was already set.", 120 * 10);
          }
          Gpu.CheckGpuErrorsDbg();
          GL.Uniform1(su.Location, (int)(_currUnit - TextureUnit.Texture0));
          Gpu.CheckGpuErrorsDbg();
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
      else if (Gu.EngineConfig.Debug_Print_Shader_Uniform_Details_Verbose_NotFound)
      {
        Gu.Log.WarnCycle(this.Name + ": Unknown uniform " + (is_block ? "block " : "") + " '" + uniform_name + "' (possibly optimized out).", 120 * 10);
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
    private static void IncludeHeaders(FileLoc loc, StringBuilder file_lines, List<string> errors, Dictionary<FileLoc, ShaderSrc> uniqueFiles)
    {
      Gu.Assert(loc != null);
      Gu.Assert(file_lines != null);
      //Returns the entire processed string on the first function invocation. 
      //Do not set file_lines if you want the return value

      if (!uniqueFiles.Keys.Contains(loc))
      {
        uniqueFiles.Add(loc, new ShaderSrc(loc));
      }

      string file_text = Library.ReadTextFile(loc, true);
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
        if (!CheckInclude(iLine, lines, loc, file_lines, errors, uniqueFiles))
        {
          break;
        }
      }

      file_lines.Append("//\n");
      file_lines.Append("// BEGIN: " + loc.RawPath + " (" + loc.QualifiedPath + ")\n");
      file_lines.Append("//\n");

      for (; iLine < lines.Length; iLine++)
      {
        if (CheckInclude(iLine, lines, loc, file_lines, errors, uniqueFiles))
        {
          errors.Add("File: '" + loc.RawPath + "': #include should be at the top of the file to avoid invalid file commenting (may not be a GLSL error).");
        }
        file_lines.Append(lines[iLine]);
      }

      file_lines.Append("//\n");
      file_lines.Append("// END: " + loc.RawPath + " (" + loc.QualifiedPath + ")\n");
      file_lines.Append("//\n");
    }
    private static bool CheckInclude(int iLine, string[] lines, FileLoc loc, StringBuilder file_lines, List<string> errors, Dictionary<FileLoc, ShaderSrc> uniqueFiles)
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

          IncludeHeaders(new FileLoc(fs, loc.FileStorage), file_lines, errors, uniqueFiles);
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
    private FileLoc GetBinaryLocation()
    {
      string stage = PipelineStageEnum.Description();
      string binaryname = $"{Name}-{stage}.sb";
      string binloc = System.IO.Path.Combine(Gu.LocalCachePath, binaryname);
      FileLoc fn = new FileLoc(binloc, FileStorage.Disk);
      return fn;
    }
    private void SaveProgramBinary()
    {
      try
      {
        FileLoc binloc = GetBinaryLocation();
        Gu.Log.Info($"{Name}: ..Saving binary to {binloc.QualifiedPath}.");

        int binBufSz = 0;
        GL.GetProgram(GlId, (GetProgramParameterName)GLenum.GL_PROGRAM_BINARY_LENGTH, out binBufSz);
        Gpu.CheckGpuErrorsRt();

        var binaryData = new byte[binBufSz];
        int outlen;
        BinaryFormat outfmt;

        var pinnedHandle = GCHandle.Alloc(binaryData, GCHandleType.Pinned);
        GL.GetProgramBinary(GlId, binBufSz, out outlen, out outfmt, pinnedHandle.AddrOfPinnedObject());
        Gpu.CheckGpuErrorsRt();
        pinnedHandle.Free();

        byte[] bytes = BitConverter.GetBytes((Int32)outfmt);

        using (var stream = binloc.OpenWrite())
        {
          using (var bw = new BinaryWriter(stream))
          {
            bw.Write((Int32)outfmt);
            bw.Write((Int32)binaryData.Length);
            bw.Write(binaryData);
          }
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Failed to save shader binary", ex);
      }
    }

    private bool CheckLoadCachedBinary(DateTime maxmodifytime)
    {
      _state = ShaderLoadState.CheckingToLoadBinary;
      bool loaded = false;
      if (Gu.EngineConfig.EnableShaderCaching)
      {
        FileLoc binloc = GetBinaryLocation();

        if (TryLoadCachedBinary(maxmodifytime, binloc))
        {
          if (ValidateProgram())
          {
            Gu.Log.Info($"{Name}: {binloc.QualifiedPath}: ..Successfully loaded cached shader binary.");
            loaded = true;
          }
        }
        //Log our errors, if any, and clear it. 
        string errors = String.Join(Environment.NewLine, _programErrors.ToArray());
        if (errors.Length > 0)
        {
          Gu.Log.Info(errors);
        }
      }
      return loaded;
    }
    private bool TryLoadCachedBinary(DateTime sourceFilesMaxWriteTime, FileLoc binloc)
    {
      State = ShaderLoadState.LoadingBinary;

      if (!binloc.Exists)
      {
        Gu.Log.Debug($"{Name}: '{binloc}': Binary does not exist...must recompile.");
        State = ShaderLoadState.LoadingBinaryFailed;
      }
      else if (binloc.GetLastWriteTime() >= sourceFilesMaxWriteTime)
      {
        //pProgram has already asked GL for an ID.
        Gpu.CheckGpuErrorsRt();
        try
        {
          //Load file
          byte[] binaryData = null;
          BinaryFormat fmt;
          int binaryLength = 0;
          using (var stream = binloc.OpenRead())
          {
            using (var br = new BinaryReader(stream))
            {
              fmt = (BinaryFormat)br.ReadInt32();
              binaryLength = br.ReadInt32();
              binaryData = br.ReadBytes(binaryLength);
            }
          }
          _glId = GT.CreateProgram();
          Gpu.CheckGpuErrorsRt();

          var pinnedHandle = GCHandle.Alloc(binaryData, GCHandleType.Pinned);
          GL.ProgramBinary(_glId, fmt, pinnedHandle.AddrOfPinnedObject(), binaryLength);
          pinnedHandle.Free();

          if (Gpu.CheckGpuErrorsRt(true, true))
          {
            Gu.Log.Debug($"{Name}: '{binloc}': glProgramBinary Failed to load cached binary. This is not necessarily an error, the binary may have been stale.");
            State = ShaderLoadState.LoadingBinaryFailed;
          }
          else
          {
            State = ShaderLoadState.Compiled;
          }
        }
        catch (Exception ex)
        {
          Gu.Log.Warn($"{Name}: '{binloc}': Loading program binary threw exception:", ex);
          State = ShaderLoadState.LoadingBinaryFailed;
        }
      }
      else
      {
        Gu.Log.Warn($"{Name}: '{binloc}': Shader source files were newer than the compiled binary.");
        State = ShaderLoadState.LoadingBinaryFailed;
      }

      //Hesitant to delete the binary.
      //deleteBinaryFromDisk(programName);

      return State == ShaderLoadState.Compiled;
    }
    private string GetShaderSourceFilePath(ShaderSrc s, string dir)
    {
      string fn = $"{Name}_stage{PipelineStageIndex}{s.Extension}";
      string p = System.IO.Path.Combine(dir, fn);
      return p;
    }

    #endregion
  }

  // public enum ShaderType{

  // }
  public class ShaderSrc
  {
    public ShaderType? ShaderType = null;//if null it is a shader header or system file
    public FileLoc FileLoc;
    public string Extension { get; private set; } = "";
    public ShaderSrc(FileLoc loc)
    {
      FileLoc = loc;
    }
    public ShaderSrc(ShaderType type, FileLoc loc, string extension)
    {
      FileLoc = loc;
      ShaderType = type;
      Extension = extension;
    }
    public string _srcFile = String.Empty;//src from file
    public string _src = String.Empty; //processed
    public string _srcOld = String.Empty;
  }
  [DataContract]
  public class Shader : OpenGLContextDataManager<Dictionary<int, GpuShader>>
  {
    //Opaque type for shaders that creates shaders for GL contexts.
    #region Public: Members

    public const int c_iNormalizedFileCount = 3; // Number of ALL shader files if we used all pipeline stages ex. v,g,f=3.
    public OpenTK.Graphics.OpenGL4.PrimitiveType? GSPrimType { get { return _gsPrimType; } private set { _gsPrimType = value; } }
    public DynamicFileLoader? _loader = null;

    #endregion
    #region Private: Members

    [DataMember] private OpenTK.Graphics.OpenGL4.PrimitiveType? _gsPrimType = null; //if we have a GS, this must be set.
    private List<ShaderSrc> _inputFiles;
    private Dictionary<FileLoc, ShaderSrc>? _all_unique_files = null;//All files + headers (for hot reload) to the _files array
    private bool _bInit = false;
    private bool _isFS = false;
    private bool _isGS = false;
    private bool _isCompute = false;

    #endregion
    #region Public: Static methods

    public static Shader DefaultFlatColorShader()
    {
      return Gu.Lib.LoadShader(RName.Shader_DefaultFlatColorShader);
    }
    public static Shader DefaultObjectShader()
    {
      return Gu.Lib.LoadShader(RName.Shader_DefaultObjectShader);
    }
    public static Shader DefaultBillboardPoints()
    {
      return Gu.Lib.LoadShader(RName.Shader_DefaultBillboardPoints);
    }

    #endregion
    #region Public: Methods  
    public Shader(string name, string generic_name, FileStorage storage, OpenTK.Graphics.OpenGL4.PrimitiveType? gs_primType = null) : base(name)
    {
      //We must do away with the generic name thing, if files dont exist then they wont be reported.
      var locs = new List<FileLoc>() {
        new FileLoc($"{generic_name}.vs.glsl", storage),
        new FileLoc($"{generic_name}.gs.glsl", storage),
        new FileLoc($"{generic_name}.fs.glsl", storage),
        new FileLoc($"{generic_name}.cs.glsl", storage)
      };
      Init(locs, gs_primType, false);
    }
    public Shader(string name, List<FileLoc> locs, OpenTK.Graphics.OpenGL4.PrimitiveType? gs_primType = null) : base(name)
    {
      Init(locs, gs_primType, true);
    }
    public GpuShader GetShaderForCurrentContext()
    {
      return GetOrCreateShader(Gu.Context);
    }

    #endregion
    #region Private & Protected: Methods

    private void Init(List<FileLoc> locs, OpenTK.Graphics.OpenGL4.PrimitiveType? gs_primType, bool mustexist)
    {
      _gsPrimType = gs_primType;
      _inputFiles = new List<ShaderSrc>();

      foreach (var fl in locs)
      {
        if (mustexist)
        {
          Gu.Assert(fl.Exists, $"{Name} file '{fl.QualifiedPath}' does not exist.");
        }

        if (fl.Exists)
        {
          ShaderType type = ShaderType.VertexShader;
          var path = fl.QualifiedPath;
          var ext = path.Substring(path.Length - 8, 8);

          if (StringUtil.Equals(ext, ".vs.glsl"))
          {
            type = ShaderType.VertexShader;
          }
          else if (StringUtil.Equals(ext, ".gs.glsl"))
          {
            type = ShaderType.GeometryShader;
            Gu.Assert(_gsPrimType != null);
            _isGS = true;
          }
          else if (StringUtil.Equals(ext, ".fs.glsl"))
          {
            type = ShaderType.FragmentShader;
            _isFS = true;
          }
          else if (StringUtil.Equals(ext, ".cs.glsl"))
          {
            type = ShaderType.ComputeShader;
            _isCompute = true;
          }
          else
          {
            Gu.BRThrowNotImplementedException();
          }
          ShaderSrc sf = new ShaderSrc(type, fl, ext);
          _inputFiles.Add(sf);
        }
      }
      Gu.Assert(_inputFiles != null && _inputFiles.Count > 0);
      Gu.Assert(!(_isFS && _isCompute));

    }
    private bool InitHeaders()
    {
      Gu.Assert(_inputFiles != null && _inputFiles.Count > 0);
      //We lazy initialize the header compiler for the shader when we need to use it. 
      //This simply loads the shader source, it doesn't proces vars or create shaders. This is done when we begin rendering.
      Gu.Log.Info("-------------------------------------");
      Gu.Log.Info($"{Name}: ..Loading shader source: {string.Join(",", _inputFiles)} ");
      List<string> errors = new List<string>();
      bool hasErrors = false;

      //Reset files.
      _all_unique_files = new Dictionary<FileLoc, ShaderSrc>(new FileLoc.EqualityComparer());//Maps to the _files array
      foreach (var f in _inputFiles)
      {
        _all_unique_files.Add(f.FileLoc, f);
      }

      //Save old sources in case we update and fail.
      foreach (var kvp in _inputFiles)
      {
        var src = kvp;
        src._srcOld = src._src;

        if (src.FileLoc.Exists)
        {
          src._srcFile = GpuShader.IncludeHeaders(src.FileLoc, errors, _all_unique_files);
        }
        else
        {
          errors.Add($"Shader source {src.FileLoc.QualifiedPath} was not found.");
        }
      }

      //Check errors
      if (errors.Count > 0)
      {
        Gu.Log.Error($"Shader '{Name}' preprocessing errors: \n" + string.Join("\n", errors));
        Gu.DebugBreak();
        return false;
      }

      var flist = _all_unique_files.Keys.ToList();
      if (_loader == null)
      {
        _loader = new DynamicFileLoader(flist, OnFilesChanged);
      }
      else
      {
        _loader.Files = flist;
      }

      _bInit = true;

      return true;
    }
    protected bool OnFilesChanged(List<FileLoc> changed)
    {
      InitHeaders();

      bool ret = true;

      var cur_ctx = Gu.Context;
      foreach (var context_pipeshader in _contextData)
      {
        //Grab contxt and save
        Gu.SetContext(context_pipeshader.Key);

        foreach (var pipeshader in context_pipeshader.Value.Keys)
        {
          //Attempt to compile the shader (context is set)
          //if it fails, it won't save the binary, so we still have the old shader technically
          //If it fails, it won't break the program, we just keep the old shader in memory.
          var oldShader = context_pipeshader.Value[pipeshader];

          var newshader = CreateNewShaderForContextPipe(context_pipeshader.Key, pipeshader, true);
          if (newshader.State == ShaderLoadState.Success)
          {
            //remove old shader, set new shader.
            context_pipeshader.Value[pipeshader] = newshader;
            oldShader.DestroyForGC();
            oldShader = null;
            ret = true;
          }
          else
          {
            newshader.DestroyForGC();
            newshader = null;
            ret = false;
          }
          Gpu.CheckGpuErrorsRt();
          GC.Collect();
        }

      }
      //set back our old ocntext
      Gu.SetContext(cur_ctx);

      return ret;
    }
    protected override Dictionary<int, GpuShader> CreateNew()
    {
      return new Dictionary<int, GpuShader>();
    }
    private GpuShader GetOrCreateShader(WindowContext ct)
    {
      Gu.Assert(ct != null);
      if (!_bInit)
      {
        InitHeaders();
      }
      var dict = GetDataForContext(ct);
      Gu.Assert(dict != null);
      var stageindex = ct.Renderer.CurrentStage.Index;

      GpuShader? shader = null;
      if (!dict.TryGetValue(stageindex, out shader))
      {
        shader = CreateNewShaderForContextPipe(ct, stageindex, false);
        dict.Add(stageindex, shader);
      }
      Gu.Assert(shader.PipelineStageIndex == stageindex);

      return shader;
    }
    private GpuShader CreateNewShaderForContextPipe(WindowContext ct, int stageindex, bool is_hot_reload)
    {
      var non_header = _all_unique_files.Where(p => p.Value.ShaderType != null).ToDictionary(p => p.Key, p => p.Value);
      var shader = new GpuShader(Name, ct, non_header, _loader.MaxModifyTime, stageindex, is_hot_reload);
      return shader;
    }
    #endregion
  }

}//NS 
