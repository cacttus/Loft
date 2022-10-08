﻿using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;

namespace PirateCraft
{
  #region Enums

  public enum ShaderLoadState
  {
    None,
    Loading,
    Compiled,
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
    [Description("_ufGpuPointLights_Block")] _ufGpuPointLights_Block,
    [Description("_ufGpuDirLights_Block")] _ufGpuDirLights_Block,
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
    public float _fHdrSampleExp = 1;
    public float _fHdrToneMapExp = 1;
    //
    public int _iShadowBoxCount = 0;
    public float _fTimeSeconds = 0;
    public int _pad0 = 0;
    public int _pad1 = 0;
    //
    public vec3 _vAmbientColor = new vec3(1, 1, 1);
    public float _fAmbientIntensity = 0.7f;
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
    public float _lineWidth = 1.0f;
    public float _pad0 = 0.0f;
    public float _pad1 = 0.0f;
  }

  #endregion

  public class ShaderControlVars
  {
    public ShaderType? ShaderType { get; private set; } = null;
    public int MaxPointLights { get; set; } = 8;
    public int MaxDirLights { get; set; } = 2;
    public int MaxCubeShadowSamples { get; set; } = 4;
    public int MaxFrusShadowSamples { get; set; } = 4;
    public int MaxInstances { get; set; } = 32;
    public bool IsInstanced { get; set; } = true;  //this is, technically going to always be set now, but later we can add non-instanced for performance improvements.
    private PipelineStageEnum PipelineStageEnum = PipelineStageEnum.Unset;
    private int PipelineStageIndex = -1;

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

      ShaderType st = ShaderType.Value;
      if (st == OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader || st == OpenTK.Graphics.OpenGL4.ShaderType.FragmentShaderArb) { AddDef(sb, "DEF_SHADER_STAGE_FRAGMENT"); }
      else if (st == OpenTK.Graphics.OpenGL4.ShaderType.VertexShader || st == OpenTK.Graphics.OpenGL4.ShaderType.VertexShaderArb) { AddDef(sb, "DEF_SHADER_STAGE_VERTEX"); }
      else if (st == OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader) { AddDef(sb, "DEF_SHADER_STAGE_GEOMETRY"); }
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
        if (input.Texture.PixelInternalFormat == PixelInternalFormat.Rgba32f)
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
        sb.AppendLine($"{output_type} getInput_{inpts}({input_type} val){{ return {func}({uniform_name}, {coord}{bias}){swizzle}; }}");
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
    public ModifiedList<Light> Lights { get { return _lights; } set { _lights = value; SetModified(); } }
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
    //A list of instanced objects that all share the same mesh material
    public List<IDrawable> Objects = null;
    public GpuInstanceData[] GpuInstanceData = null;
    public void Add(IDrawable ob)
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
          GpuInstanceData[iob]._pickId.x = Picker.AddFlagsToPickID(Objects[iob].PickId, Objects[iob].Pickable, Objects[iob].Selected, Objects[iob].Picked);
        }
        Modified = false;
      }
    }
  }
  public class VisibleObjects : MutableState
  {
    public Dictionary<Material, Dictionary<MeshData, VisibleObjectInstances>> MatMeshInstances { get; set; } = null;
    public GpuMaterial GpuMaterial { get { return _gpuMaterial; } }
    private GpuMaterial _gpuMaterial = new GpuMaterial();

    public VisibleObjects()
    {
    }
    public VisibleObjects(List<IDrawable> obs)
    {
      foreach (var ob in obs)
      {
        //This is inefficient we could do better.
        Add(ob);
      }
    }
    public void Draw(WorldProps wp, RenderView rv, Material? customMaterial)
    {
      if (MatMeshInstances != null)
      {
        CompileGpuData();

        foreach (var mk in MatMeshInstances)
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
    private void DrawForMaterial(WorldProps wp, RenderView rv, Material mat, Dictionary<MeshData, VisibleObjectInstances> mesh_instances)
    {
      mat.GpuRenderState.SetState();
      var cs = mat.Shader.GetShaderForCurrentContext();
      cs.BeginRender(wp, rv, mat);
      foreach (var ob_set in mesh_instances)
      {
        if (ob_set.Value.Objects[0] is SoloMesh)
        {
          Gu.Trap();
        }
        var mesh = ob_set.Key;
        ob_set.Value.CompileGpuData();
        cs.BindInstanceUniforms(ob_set.Value.GpuInstanceData);

        Gu.Assert(mesh != null);
        cs.BindMeshUniforms(mesh);

        cs.CheckAllUniformsSet();

        mesh.Draw(ob_set.Value.GpuInstanceData, mat.Shader.GSPrimType);
      }
      cs.EndRender();
    }
    public void CompileGpuData()
    {
      if (Modified || Gu.EngineConfig.Debug_AlwaysCompileAndReloadGpuUniformData)
      {
        if (MatMeshInstances != null)
        {
          foreach (var mat_dic in MatMeshInstances)
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
      if (MatMeshInstances != null)
      {
        MatMeshInstances.Clear();
        SetModified();
      }
    }
    public void Add(IDrawable ob)
    {
      Gu.Assert(ob.Material != null);
      Gu.Assert(ob.Mesh != null);

      MatMeshInstances = MatMeshInstances.ConstructIfNeeded();
      Dictionary<MeshData, VisibleObjectInstances>? meshInstances = null;
      if (!MatMeshInstances.TryGetValue(ob.Material, out meshInstances))
      {
        meshInstances = meshInstances.ConstructIfNeeded();
        MatMeshInstances.Add(ob.Material, meshInstances);
      }
      //The instancing system needs to be reworked
      //This is correct an instance shares A) material B) mesh, but also components, skeletons, etc, so it is not mesh id it is ob.id
      VisibleObjectInstances? objList = null;
      if (!meshInstances.TryGetValue(ob.Mesh, out objList))
      {
        objList = objList.ConstructIfNeeded();
        meshInstances.Add(ob.Mesh, objList);
      }
      objList.Add(ob);
      SetModified();
    }


  }
  public class DrawCall
  {
    public double? Delta { get { return _delta; } set { _delta = value; } }
    public VisibleObjects VisibleObjects { get { return _visibleObjects; } set { _visibleObjects = value; } }
    public Func<ShaderUniform, bool> CustomUniforms = null;//Input: a uniform variable name that you must bind to, output: true if you handled it, false if not

    private double? _delta = null;
    private VisibleObjects _visibleObjects = new VisibleObjects();


    public static void Draw(WorldProps p, RenderView rv, IDrawable ob)
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
    public void AddVisibleObject(IDrawable ob)
    {
      Gu.Assert(ob != null);
      _visibleObjects = VisibleObjects.ConstructIfNeeded();
      _visibleObjects.Add(ob);
    }
    public void Draw(WorldProps p, RenderView rv, Material? customMaterial = null)
    {
      Gu.Assert(p != null);
      Gu.Assert(rv != null);
      if (_visibleObjects != null)
      {
        _visibleObjects.Draw(p, rv, customMaterial);
      }
    }
  }
  public class ShaderStage : OpenGLResource
  {
    public ShaderType ShaderType { get; private set; } = ShaderType.VertexShader;
    public ShaderStage(string name, ShaderType tt, string src) : base(name)
    {
      //Here: we can load from shader cache.
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
  public abstract class ShaderMemoryBlock : OpenGLResource
  {
    public int BlockIndex { get; private set; } = -1;
    public int BindingIndex { get; private set; } = -1;
    public bool HasBeenSet { get; set; } = false;
    public bool Active { get; private set; } = false;
    public int BufferSizeBytes { get; private set; } = 0;
    public GPUBuffer Buffer { get; protected set; } = null;//Optional buffer to copy to, or we can set it from mesh data, or elsewhere

    public override void Dispose_OpenGL_RenderThread()
    {
      Buffer = null;
    }

    public ShaderMemoryBlock(string name, int iBlockIndex, int iBindingIndex, int iBufferByteSize, bool active) : base(name)
    {
      BufferSizeBytes = iBufferByteSize;
      BindingIndex = iBindingIndex;
      BlockIndex = iBlockIndex;
      Active = active;
    }
    public abstract GPUBuffer GetOrCreateBuffer();
  }
  public class ShaderUniformBlock : ShaderMemoryBlock
  {
    public ShaderUniformBlock(string name, int iBlockIndex, int iBindingIndex, int iBufferByteSize, bool active) :
    base(name, iBlockIndex, iBindingIndex, iBufferByteSize, active)
    {
    }
    public override GPUBuffer GetOrCreateBuffer()
    {
      if (Buffer == null)
      {
        Buffer = Gpu.CreateUniformBuffer(this.Name, 1, BufferSizeBytes);
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
    public override GPUBuffer GetOrCreateBuffer()
    {
      if (Buffer == null)
      {
        Buffer = Gpu.CreateShaderStorageBuffer(this.Name, 1, BufferSizeBytes);
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
    //Shader, program on the GPU based on GL Context...
    //Assuming if I'm right, probably not, that OpenTK's context sharing can't share OpenGL programs.
    //So we need one per context, and the way the system generates shaders, we need an additional shader per pipeline stage.
    // Context -> Pipeline Stage -> Shader
    private static string c_strGlobalDefineString = "<GLSL_CONTROL_DEFINES_HERE>";
    private static string c_strGlobalConstantsString = "<GLSL_CONTROL_CONSTANTS_HERE>";
    private static string c_strGlobalInputString = "<GLSL_CONTROL_INPUTS_HERE>";
    private static string c_strGlobalOutputString = "<GLSL_CONTROL_OUTPUTS_HERE>";
    private static string c_strGlobalMaterialTextureString = "<GLSL_CONTROL_INPUT_MATERIAL_TEXTURES>";
    private static string c_strUBOBindingString = "<UBO_BINDING_ID>";
    private static string c_strSSBOBindingString = "<SSBO_BINDING_ID>";

    private ShaderStage _vertexStage = null;
    private ShaderStage _fragmentStage = null;
    private ShaderStage _geomStage = null;

    public int PipelineStageIndex { get; private set; } = -1;
    public PipelineStageEnum PipelineStageEnum { get; private set; } = PipelineStageEnum.MaxPipelineStages;
    public ShaderLoadState State { get; private set; } = ShaderLoadState.None;

    private List<ShaderAttrib> _attribs = new List<ShaderAttrib>();
    private Dictionary<string, ShaderStorageBlock> _ssbos = new Dictionary<string, ShaderStorageBlock>();
    private Dictionary<string, ShaderUniform> _uniforms = new Dictionary<string, ShaderUniform>();
    private Dictionary<string, ShaderUniformBlock> _uniformBlocks = new Dictionary<string, ShaderUniformBlock>();

    private TextureUnit _currUnit = TextureUnit.Texture0;
    //Technically this is a GL context thing. But it's ok for now.
    private Dictionary<TextureUnit, Texture> _boundTextures = new Dictionary<TextureUnit, Texture>();

    private List<string> _shaderErrors = new List<string>();


    private string _genericName = "";//generic name for all shader files that compile into this program

    public GpuShader(string genericName, WindowContext ct, string vsSrc_raw, string psSrc_raw, string gsSrc_raw,
                         DateTime maxmodifytime, int stageindex, bool is_hot_reload) : base(genericName)
    {
      Gu.Assert(ct != null);
      Gu.Assert(ct.Renderer != null);
      //Gu.Assert(ct.Renderer.CurrentStage != null);

      PipelineStageEnum = Gu.Context.Renderer.PipelineStages[stageindex].PipelineStageEnum;
      PipelineStageIndex = stageindex;

      _genericName = genericName;
      Gu.Log.Info($"{_genericName}: ..Compiling shader.");
      Gu.Log.Info($"{_genericName}: ..Context = {ct.Name}");
      Gu.Log.Info($"{_genericName}: ..Stage = {PipelineStageEnum.Description()}");

      Gpu.CheckGpuErrorsRt();
      {
        State = ShaderLoadState.Loading;

        //Attempt to load cached binary
        bool loaded = false;
        if (Gu.EngineConfig.ShaderCaching)
        {
          if (TryLoadCachedBinary(maxmodifytime))
          {
            if (ValidateProgram())
            {
              Gu.Log.Info($"{_genericName}: ..Successfully loaded cached shader binary.");
              loaded = true;
            }
          }
          //Log our errors, if any, and clear it. 
          string errors = String.Join(Environment.NewLine, _shaderErrors.ToArray());
          if (errors.Length > 0)
          {
            Gu.Log.Info(errors);
          }

        }

        // Re-Compile binary
        if (!loaded)
        {
          _shaderErrors.Clear();
          State = ShaderLoadState.Loading; //reset back to loading to try compiling the files.
          string vsSrc = ProcessShaderSource(ct, vsSrc_raw, ShaderType.VertexShader, PipelineStageEnum, PipelineStageIndex);
          string gsSrc = ProcessShaderSource(ct, gsSrc_raw, ShaderType.GeometryShader, PipelineStageEnum, PipelineStageIndex);
          string psSrc = ProcessShaderSource(ct, psSrc_raw, ShaderType.FragmentShader, PipelineStageEnum, PipelineStageIndex);

          Gu.Log.Info($"{_genericName}: ..Fully compiling shader.");
          CreateShaders(vsSrc, psSrc, gsSrc);
          CreateProgramFromShaders();
          ValidateProgram();

          //Save all compiled shaders now for debug.
          string v_src = GetReadableShaderSourceCode(vsSrc);
          string g_src = GetReadableShaderSourceCode(gsSrc);
          string f_src = GetReadableShaderSourceCode(psSrc);
          SaveShaderSource(v_src, _genericName + ".vs.glsl", null);
          if (gsSrc_raw != null)
          {
            SaveShaderSource(g_src, _genericName + ".gs.glsl", null);
          }
          SaveShaderSource(f_src, _genericName + ".fs.glsl", null);

          if (State == ShaderLoadState.Failed)
          {
            string errors = String.Join(Environment.NewLine, _shaderErrors.ToArray());
            string blip = "--------------------------------------------------------------------------------------";
            string all_src_errs = "";
            if (!String.IsNullOrEmpty(vsSrc_raw))
            {
              all_src_errs += Environment.NewLine + blip + Environment.NewLine + "--VERTEX SOURCE--" + Environment.NewLine + blip + Environment.NewLine + v_src + Environment.NewLine;
            }
            if (!String.IsNullOrEmpty(gsSrc_raw))/*  */
            {
              all_src_errs += Environment.NewLine + blip + Environment.NewLine + "--GEOM SOURCE--" + Environment.NewLine + blip + Environment.NewLine + g_src + Environment.NewLine;
            }
            if (!String.IsNullOrEmpty(psSrc_raw))
            {
              all_src_errs += Environment.NewLine + blip + Environment.NewLine + "--FRAG SOURCE--" + Environment.NewLine + blip + Environment.NewLine + f_src + Environment.NewLine;
            }
            Gu.Log.Info(all_src_errs);
            Gu.Log.Error($"{_genericName}: ..Failed to load shader '" + _genericName + "'." + Environment.NewLine + errors);
            // only break on first compile. If we're a hot reload don't break.
            if (is_hot_reload == false)
            {
              Gu.DebugBreak();
            }
          }
        }
        //If either state has failed, print

        if (State == ShaderLoadState.Validated)
        {
          Bind();
          ParseAttribs();
          ParseUniforms();
          ParseSSBOs();
          if (State == ShaderLoadState.Validated)
          {
            Unbind();

            State = ShaderLoadState.Success;
            SetObjectLabel();

            Gu.Log.Info($"{_genericName}: ..Succssfully processed and loaded shader to GPU (glId={GlId})");
            if (Gu.EngineConfig.ShaderCaching)
            {
              SaveBinary();
            }
          }
        }
      }
      Gpu.CheckGpuErrorsRt();
    }
    private void SaveShaderSource(string src, string filename, string? errors)
    {
      System.IO.File.WriteAllText(System.IO.Path.Combine(Gu.LocalTmpPath, filename), src + Environment.NewLine + (errors == null ? "" : errors));
    }
    private void CreateBindingIndexes(ref string src_cpy, string index_identifier)
    {
      for (int binding = 0, idx = src_cpy.IndexOf(index_identifier); idx >= 0; idx = src_cpy.IndexOf(index_identifier), binding++)
      {
        //This is inefficnient these files can be very huge, TODO:optimize
        src_cpy = src_cpy.Substring(0, idx) + binding.ToString() + src_cpy.Substring(idx + index_identifier.Length);
      }
    }
    private string ProcessShaderSource(WindowContext ct, string src_raw, ShaderType type, PipelineStageEnum stage, int stageindex)
    {
      string src_cpy = src_raw;
      if (StringUtil.IsNotEmpty(src_cpy))
      {
        CreateBindingIndexes(ref src_cpy, GpuShader.c_strUBOBindingString);
        CreateBindingIndexes(ref src_cpy, GpuShader.c_strSSBOBindingString);

        var vars = ct.Renderer.DefaultControlVars.Clone(stage, stageindex, type);
        SetControlVar(GpuShader.c_strGlobalDefineString, vars.DefinesString(), type, ref src_cpy);
        SetControlVar(GpuShader.c_strGlobalInputString, vars.InputsString(), type, ref src_cpy);
        SetControlVar(GpuShader.c_strGlobalOutputString, vars.OutputsString(), type, ref src_cpy);
        SetControlVar(GpuShader.c_strGlobalConstantsString, vars.ConstantsString(), type, ref src_cpy);
        SetControlVar(GpuShader.c_strGlobalMaterialTextureString, GetMaterialInputs(src_raw), type, ref src_cpy);
      }
      return src_cpy;
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
      if (!src_cpy.Contains(tag))
      {
        _shaderErrors.Add($"{_genericName} ({type.ToString()}): Shader vars tag '{tag}' was not found.");
        State = ShaderLoadState.Failed;
      }
      else
      {
        src_cpy = src_cpy.Replace(tag, value);
      }
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
      DestroyForGC();//Call again if it hasn't been.
      DeleteProgram();
    }

    public void DestroyForGC()
    {
      _vertexStage = null;
      _fragmentStage = null;
      _geomStage = null;
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
    private void DeleteProgram()
    {
      if (GL.IsProgram(_glId))
      {
        int p = GL.GetInteger(GetPName.CurrentProgram);
        GL.UseProgram(0);
        GL.DeleteProgram(_glId);
        if (p != _glId)
        {
          GL.UseProgram(p);
        }
      }
      this._glId = 0;
    }
    private void CreateShaders(string vs, string ps, string gs = "")
    {
      Gpu.CheckGpuErrorsRt();
      {
        _vertexStage = new ShaderStage(this._genericName + "-VS", ShaderType.VertexShader, vs);
        _fragmentStage = new ShaderStage(this._genericName + "-FS", ShaderType.FragmentShader, ps);
        if (!string.IsNullOrEmpty(gs))
        {
          _geomStage = new ShaderStage(this._genericName + "-GS", ShaderType.GeometryShader, gs);
        }
      }
      Gpu.CheckGpuErrorsRt();
    }
    private void CreateProgramFromShaders()
    {
      Gpu.CheckGpuErrorsRt();
      {
        _glId = GL.CreateProgram();

        GL.AttachShader(_glId, _vertexStage.GlId);
        Gpu.CheckGpuErrorsRt();
        GL.AttachShader(_glId, _fragmentStage.GlId);
        Gpu.CheckGpuErrorsRt();
        if (_geomStage != null)
        {
          GL.AttachShader(_glId, _geomStage.GlId);
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
      if (_shaderErrors == null)
      {
        _shaderErrors = new List<string>();
      }
      _shaderErrors.AddRange(programInfoLog.Split('\n').ToList());

      if (_shaderErrors.Count > 0 && programInfoLog.ToLower().Contains("error"))
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
        _shaderErrors.Add("[ShaderCache] glValidateProgram says program binary load failed.  Check the above logs for errors.");
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
        _shaderErrors.Add("[ShaderCache] glIsProgram says program was not valid after loading to GPU");
      }

      //Try binding/using 
      Bind();

      // - If the program failed to load it will raise an error after failing to bind.
      var e = GL.GetError();
      if (e != ErrorCode.NoError)
      {
        State = ShaderLoadState.Failed;
        _shaderErrors.Add("[ShaderCache], GL error " + e + " , program was not valid after binding.");
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
    int _maxBufferBindingIndex = -1;
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
            Gu.Log.Debug($"{_genericName}: .. Inactive uniform: {u_name}");
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
            Gu.Log.Debug($"{_genericName}: .. Active uniform: {u_name}");
          }
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
            Gu.Log.Debug($"{_genericName}: ..Inactive uniform block: {u_name}");
          }
        }
        else
        {
          if (Gu.EngineConfig.Debug_Print_Shader_Uniform_Details_Verbose_NotFound)
          {
            Gu.Log.Debug($"{_genericName}: ..Active uniform block: {u_name}");
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
    private void BindWorldUniforms(WorldProps world)
    {
      Gu.Assert(world != null);
      world.CompileGpuData();
      BindUniformBlock(ShaderUniformName._ufGpuWorld_Block.Description(), new GpuWorld[] { world.GpuWorld });
      BindUniformBlock(ShaderUniformName._ufGpuPointLights_Block.Description(), world.GpuPointLights);
      BindUniformBlock(ShaderUniformName._ufGpuDirLights_Block.Description(), world.GpuDirLights);
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
    public void BindUniform_Mat4(string name, mat4 m)
    {
      if (_uniforms.TryGetValue(name, out var u))
      {
        var mat = m.ToOpenTK();
        GL.UniformMatrix4(u.Location, false, ref mat);
        Gpu.CheckGpuErrorsDbg();
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
        Gu.Log.Warn($"{_genericName}: {n} Uniforms were not set: {notset}");
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
    private void BindUniformBlock<T>(ShaderUniformBlock ub, T[] items)
    {
      Gu.Assert(items != null);

      int item_size = 0;
      if (!typeof(T).IsValueType)
      {
        throw new Exception("Input items must be value type. If items are array, use array value type.");
      }
      item_size = Marshal.SizeOf(typeof(T));//default(T) 

      int num_bytes_to_copy = item_size * items.Length;// dat.instanceData.Length;
      if (num_bytes_to_copy > ub.BufferSizeBytes)
      {
        num_bytes_to_copy = ub.BufferSizeBytes;
        Gu.Log.WarnCycle($"Uniform Block '{ub.Name}' exceeded max count of " + ub.BufferSizeBytes / item_size + " items. Tried to copy " + items.Length + " items.");
      }
      var handle = GCHandle.Alloc(items, GCHandleType.Pinned);
      CopyUniformBlockData(ub, handle.AddrOfPinnedObject(), num_bytes_to_copy);
      handle.Free();

      BindBlockFast(ub);
    }
    public void CopyUniformBlockData(ShaderUniformBlock u, IntPtr pData, int copySizeBytes)
    {
      //Copy to the shader buffer
      Gu.Assert(copySizeBytes <= u.BufferSizeBytes);

      Gpu.CheckGpuErrorsDbg();

      var ubo = u.GetOrCreateBuffer();

      GL.BindBuffer(BufferTarget.UniformBuffer, ubo.GlId);
      Gpu.CheckGpuErrorsDbg();

      GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, copySizeBytes, pData);
      Gpu.CheckGpuErrorsDbg();

      GL.BindBuffer(BufferTarget.UniformBuffer, 0);
      Gpu.CheckGpuErrorsDbg();

      if (u.HasBeenSet == true && Gu.EngineConfig.Debug_Print_Shader_Uniform_Details_Verbose_AlreadySet)
      {
        Gu.Log.WarnCycle(this._genericName + ": Uniform  " + u.Name + " was already set.", 120 * 10);
      }
      u.HasBeenSet = true;
    }
    public void BindUniformBlock(string uname, GPUBuffer b)
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
    public void BindSSBOBlock(string uname, GPUBuffer b)
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
        Gu.Log.WarnCycle(this._genericName + ": Shader Uniform Block '" + u.Name + "' value was not set before binding.");
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
            Gu.Log.WarnCycle(this._genericName + ": Texture uniform " + su.Name + "  was already set.", 120 * 10);
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
          Gu.Log.WarnCycle(this._genericName + ": Texture unit " + su.Name + " was not found in material and had no default.");
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
        Gu.Log.Error(this._genericName + ": Unknown uniform " + (is_block ? "block " : "") + " '" + uniform_name + "' (possibly optimized out).");
        Gu.DebugBreak();
      }
      else if (Gu.EngineConfig.Debug_Print_Shader_Uniform_Details_Verbose_NotFound)
      {
        Gu.Log.WarnCycle(this._genericName + ": Unknown uniform " + (is_block ? "block " : "") + " '" + uniform_name + "' (possibly optimized out).", 120 * 10);
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
    public static string IncludeHeaders(FileLoc loc, List<string> errors, HashSet<FileLoc> uniqueFiles)
    {
      var file_lines = new StringBuilder();

      IncludeHeaders(loc, file_lines, errors, uniqueFiles);

      return file_lines.ToString();
    }
    private static void IncludeHeaders(FileLoc loc, StringBuilder file_lines, List<string> errors, HashSet<FileLoc> uniqueFiles)
    {
      Gu.Assert(loc != null);
      Gu.Assert(file_lines != null);
      //Returns the entire processed string on the first function invocation. 
      //Do not set file_lines if you want the return value

      uniqueFiles.Add(loc);

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
    private static bool CheckInclude(int iLine, string[] lines, FileLoc loc, StringBuilder file_lines, List<string> errors, HashSet<FileLoc> uniqueFiles)
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
      string binaryname = $"{_genericName}-{stage}.sb";
      string binloc = System.IO.Path.Combine(Gu.LocalCachePath, binaryname);
      FileLoc fn = new FileLoc(binloc, FileStorage.Disk);
      return fn;
    }
    private void SaveBinary()
    {
      Gu.Log.Info($"{_genericName}: ..Attempting to save shader binary.");
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

      FileLoc binloc = GetBinaryLocation();
      Gu.Log.Info($"{_genericName}: ..Saving to {binloc.QualifiedPath}.");

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
      Gu.Log.Info($"{_genericName}: ..Saved.");
    }
    public bool TryLoadCachedBinary(DateTime sourceFilesMaxWriteTime)
    {
      State = ShaderLoadState.Failed;

      FileLoc binloc = GetBinaryLocation();
      if (!binloc.Exists)
      {
        _shaderErrors.Add($"{_genericName}: ..{binloc.QualifiedPath} cached binary does not exist...must recompile.");
        State = ShaderLoadState.Failed;
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
          _glId = GL.CreateProgram();
          Gpu.CheckGpuErrorsRt();

          var pinnedHandle = GCHandle.Alloc(binaryData, GCHandleType.Pinned);
          GL.ProgramBinary(_glId, fmt, pinnedHandle.AddrOfPinnedObject(), binaryLength);
          pinnedHandle.Free();

          if (Gpu.CheckGpuErrorsRt(true, true))
          {
            State = ShaderLoadState.Failed;
            _shaderErrors.Add("glProgramBinary Failed to load cached binary. This is not necessarily an error, the binary may have been stale.");
          }
          else
          {
            State = ShaderLoadState.Compiled;
          }
        }
        catch (Exception ex)
        {
          State = ShaderLoadState.Failed;
          Gu.Log.Warn("[ShaderCache] Loading program binary threw exception:", ex);
          //deleteBinaryFromDisk(programName);
        }
      }
      else
      {
        State = ShaderLoadState.Failed;
        _shaderErrors.Add("Shader source files were newer than the compiled binary.");
      }

      return State == ShaderLoadState.Compiled;
    }
  }
  [Serializable]
  [DataContract]
  public class Shader : OpenGLContextDataManager<Dictionary<int, GpuShader>>
  {
    //Opaque type for shaders that creates shaders for GL contexts.
    #region Public: Members

    public const int c_iNormalizedFileCount = 3; // Number of ALL shader files if we used all pipeline stages ex. v,g,f=3.
    public DateTime MaxModifyTime { get { return _maxModifyTime; } private set { _maxModifyTime = value; } }
    public OpenTK.Graphics.OpenGL4.PrimitiveType? GSPrimType { get { return _gSPrimType; } private set { _gSPrimType = value; } }

    #endregion
    #region Private: Members

    [DataMember] public OpenTK.Graphics.OpenGL4.PrimitiveType? _gSPrimType { get; private set; } = null; //if we have a GS, this must be set.
    [DataMember] private HashSet<FileLoc> _files = null;
    [DataMember] private bool _hasGS = false;
    [DataMember] private FileStorage _storage = FileStorage.Disk;
    [NonSerialized] public DateTime _maxModifyTime = DateTime.MinValue;
    [NonSerialized] private string _vsSrcOld = String.Empty;
    [NonSerialized] private string _fsSrcOld = String.Empty;
    [NonSerialized] private string _gsSrcOld = String.Empty; //**GS source must be string.empty to be unused
    [NonSerialized] private string _vsSrc = String.Empty;
    [NonSerialized] private string _fsSrc = String.Empty;
    [NonSerialized] private string _gsSrc = String.Empty;
    [NonSerialized] private bool _bInitHeaders = false;
    [NonSerialized] private string _genericName = Library.UnsetName;

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
    #region Public:Methods 

    public Shader(string name, string generic_name, bool gs, FileStorage storage, OpenTK.Graphics.OpenGL4.PrimitiveType? gs_primType = null)
          : base(name)
    {
      _genericName = generic_name;
      _hasGS = gs;
      _storage = storage;
      if (gs)
      {
        //GS must have a primitive type
        Gu.Assert(gs_primType != null);
      }
      GSPrimType = gs_primType;
    }
    public void CheckSourceChanged()
    {
      if (_files == null)
      {
        //The shader has been gott, but not initialized.
        return;
      }

      bool mustUpdate = false;
      foreach (var f in this._files)
      {
        var wt = f.GetLastWriteTime(true);
        if (wt > MaxModifyTime)
        {
          mustUpdate = true;

          // ** Set the modify time to the maximum file mod - even if compile fails. This prevents infinite checking
          MaxModifyTime = wt;

          break;
        }
      }
      if (mustUpdate)
      {
        InitHeaders();

        var cur_ctx = Gu.Context;
        foreach (var context_pipeshader in _contextData)
        {
          //Grab contxt and save
          Gu.SetContext(context_pipeshader.Key.GameWindow);

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
            }
            else
            {
              newshader.DestroyForGC();
              newshader = null;
            }
            Gpu.CheckGpuErrorsRt();
            GC.Collect();
          }

        }
        //set back our old ocntext
        Gu.SetContext(cur_ctx.GameWindow);
      }

    }
    public GpuShader GetShaderForCurrentContext()
    {
      return GetOrCreateShader(Gu.Context);
    }

    #endregion
    #region Private:Methods

    private bool InitHeaders()
    {
      //WE lazy initialize the header compiler for the shader when we need to use it. 
      Gu.Log.Info("-------------------------------------");
      Gu.Log.Info($"{Name}({_genericName}): ..Loading shader source. ");
      Gu.Log.Info($"{Name}({_genericName}): ..has GS = {_hasGS.ToString()}");
      Gu.Log.Info($"{Name}({_genericName}): ..storage = {_storage.ToString()}");
      //This simply loads the shader source, it doesn't proces vars or create shaders. This is done when we begin rendering.
      string vert_name = _genericName + ".vs.glsl";
      string geom_name = _hasGS ? _genericName + ".gs.glsl" : "";
      string frag_name = _genericName + ".fs.glsl";
      string fileloc_name = vert_name + "-" + geom_name + "-" + frag_name; //hacky, but it will work


      List<string> errors = new List<string>();
      bool hasErrors = false;

      _files = new HashSet<FileLoc>(new FileLoc.EqualityComparer());

      //Save old sources in case we update and fail.
      _vsSrcOld = _vsSrc;
      _fsSrcOld = _fsSrc;
      _gsSrcOld = _gsSrc;

      //vert
      var vloc = new FileLoc(vert_name, FileStorage.Embedded);
      if (vloc.Exists)
      {
        _vsSrc = GpuShader.IncludeHeaders(vloc, errors, _files);
      }
      else
      {
        Gu.BRThrowException($"Shader source {vloc.QualifiedPath} was not found.");
      }

      //geom
      if (_hasGS)
      {
        var gloc = new FileLoc(geom_name, FileStorage.Embedded);
        if (gloc.Exists)
        {
          _gsSrc = GpuShader.IncludeHeaders(gloc, errors, _files);
        }
        else
        {
          Gu.BRThrowException($"Shader source {gloc.QualifiedPath} was not found.");
        }
      }

      //frag
      var floc = new FileLoc(frag_name, FileStorage.Embedded);
      if (floc.Exists)
      {
        _fsSrc = GpuShader.IncludeHeaders(floc, errors, _files);
      }
      else
      {
        Gu.BRThrowException($"Shader source {floc.QualifiedPath} was not found.");
      }

      //Check errors
      if (errors.Count > 0)
      {
        Gu.Log.Warn("Shader preprocessing errors: \n" + string.Join("\n", errors));
        Gu.DebugBreak();
        return false;
      }

      //Set max modify time, this lets us compile the shaders dynamically on file changes, it also lets us load the GLBinary on file changes.
      var oldmod = MaxModifyTime;
      foreach (var f in this._files)
      {
        MaxModifyTime = MathUtils.Max(MaxModifyTime, f.GetLastWriteTime(true));
      }
      if (MaxModifyTime != oldmod)
      {
        //debug
        int n = 0;
        n++;
      }

      _bInitHeaders = true;

      return true;
    }
    protected override Dictionary<int, GpuShader> CreateNew()
    {
      return new Dictionary<int, GpuShader>();
    }
    private GpuShader GetOrCreateShader(WindowContext ct)
    {
      if (!_bInitHeaders)
      {
        InitHeaders();
      }
      Gu.Assert(ct != null);
      var stageindex = ct.Renderer.CurrentStage.Index;
      var dict = GetDataForContext(ct);
      Gu.Assert(dict != null);

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
      var shader = new GpuShader(_genericName, ct, _vsSrc, _fsSrc, _gsSrc, MaxModifyTime, stageindex, is_hot_reload);
      return shader;
    }
    #endregion
  }

}//NS Piratecraft
