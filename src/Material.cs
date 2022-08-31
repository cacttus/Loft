using System;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace PirateCraft
{
  //Material, input to a shader & gpu state for material FBO (blending, etc)
  public class Material : DataBlock
  {
    private static Material _defaultObjectMaterial = null; //Default color material / shader.
    private static Material _defaultFlatColorMaterial = null; //Default color material / shader.

    #region Public: Members

    public vec4 BaseColor { get { return _baseColor; } set { _baseColor = value; SetModified(); } }
    public float Roughness { get { return _roughness; } set { _roughness = value; SetModified(); } }
    public float Metallic { get { return _metallic; } set { _metallic = value; SetModified(); } }
    public float Specular { get { return _specular; } set { _specular = value; SetModified(); } }
    public float IndexOfRefraction { get { return _indexOfRefraction; } set { _indexOfRefraction = value; SetModified(); } }
    public bool Flat { get { return _flat; } set { _flat = value; SetModified(); } }

    public TextureInput AlbedoSlot { get { return _albedoSlot; } private set { _albedoSlot = value; SetModified(); } }
    public TextureInput NormalSlot { get { return _normalSlot; } private set { _normalSlot = value; SetModified(); } }
    public TextureInput RoughnessSlot { get { return _roughnessSlot; } private set { _roughnessSlot = value; SetModified(); } }
    public TextureInput MetalnessSlot { get { return _metalnessSlot; } private set { _metalnessSlot = value; SetModified(); } }
    public TextureInput PositionSlot { get { return _positionSlot; } private set { _positionSlot = value; SetModified(); } }

    public Shader Shader { get { return _shader; } private set { _shader = value; SetModified(); } }
    public GpuRenderState GpuRenderState { get; set; } = new GpuRenderState(); //The rendering state of the material: clipping, depth, alpha, culling, etc
    public GpuMaterial GpuMaterial
    {
      get
      {
        CompileGpuData();
        return _gpuMaterial;
      }
    }

    //**TODO: alpha is actually in the GpuRenderState
    public glTFLoader.Schema.Material.AlphaModeEnum AlphaMode = glTFLoader.Schema.Material.AlphaModeEnum.BLEND;

    #endregion

    #region Private:Members

    private Shader _shader = null;

    private vec4 _baseColor = new vec4(1, 1, 1, 1);
    private float _roughness = 0.5f;
    private float _metallic = 0.0f;
    private float _specular = 0.5f;
    private float _indexOfRefraction = 1;//1.45
    private TextureInput _albedoSlot = null;
    private TextureInput _normalSlot = null;
    private TextureInput _roughnessSlot = null;
    private TextureInput _metalnessSlot = null;
    private TextureInput _positionSlot = null;
    private bool _flat = false;

    private Dictionary<TextureInput, Texture2D> _textures = new Dictionary<TextureInput, Texture2D>();

    private GpuMaterial _gpuMaterial = default(GpuMaterial);

    private bool _bMustCompile = false;

    #endregion

    #region Public: Methods

    public void CompileGpuData()
    {
      if (_bMustCompile || Gu.EngineConfig.AlwaysCompileAndReloadGpuUniformData)
      {
        _gpuMaterial._vPBR_baseColor = _baseColor;
        _gpuMaterial._fPBR_roughness = _roughness;
        _gpuMaterial._fPBR_metallic = _metallic;
        _gpuMaterial._fPBR_indexOfRefraction = _indexOfRefraction;
        _gpuMaterial._fPBR_specular = _specular;
        _gpuMaterial._flat = this._flat ? 1 : 0;
        _bMustCompile = false;
      }
    }

    private Material() { }//clone ctor
    public Material(string name, Shader s) : base(name + "-mat")
    {
      this._albedoSlot = new TextureInput(Texture2D.Default1x1ColorPixel_RGBA32ub(vec4ub.White));
      this._normalSlot = new TextureInput(Texture2D.Default1x1NormalPixel_RGBA32ub());
      this._roughnessSlot = new TextureInput(Texture2D.Default1x1ColorPixel_RGBA32ub(vec4ub.White));
      this._metalnessSlot = new TextureInput(Texture2D.Default1x1ColorPixel_RGBA32ub(vec4ub.White));
      this._positionSlot = new TextureInput(Texture2D.Default1x1ColorPixel_RGBA32ub(vec4ub.White));

      Shader = s;
    }
    public Material(string name, Shader s, Texture2D albedo) : this(name, s)
    {
      _albedoSlot.Texture = albedo;
    }
    public Material(string name, Shader s, Texture2D albedo, Texture2D normal) : this(name, s)
    {
      _normalSlot.Texture = normal;
    }
    public Material Clone()
    {
      Material other = new Material();
      Copy(other);
      return other;
    }
    protected void Copy(Material other)
    {
      base.Copy(other);
      other.GpuRenderState = this.GpuRenderState.Clone();
      other._shader = this._shader;
      other._baseColor = this._baseColor;
      other._roughness = this._roughness;
      other._metallic = this._metallic;
      other._specular = this._specular;
      other._indexOfRefraction = this._indexOfRefraction;
      other._albedoSlot = this._albedoSlot;
      other._normalSlot = this._normalSlot;
      other._roughnessSlot = this._roughnessSlot;
      other._metalnessSlot = this._metalnessSlot;
      other._positionSlot = this._positionSlot;
      other._flat = this._flat;
      other.AlphaMode = this.AlphaMode;
    }
    public static Material DefaultFlatColor
    {
      get
      {
        //TODO: - the input shader should also be default.
        if (_defaultFlatColorMaterial == null)
        {
          _defaultFlatColorMaterial = new Material("DefaultFlatColorMaterial", Shader.DefaultFlatColorShader());
          _defaultObjectMaterial.AlbedoSlot.Texture = Texture2D.Default1x1ColorPixel_RGBA32ub(new vec4ub(255, 255, 255, 255));
        }
        return _defaultFlatColorMaterial;
      }
    }
    public static Material DefaultObjectMaterial
    {
      get
      {
        //TODO: - the input shader should also be default.
        if (_defaultObjectMaterial == null)
        {
          _defaultObjectMaterial = new Material("DefaultObjectMaterial", Shader.DefaultObjectShader());
          _defaultObjectMaterial.AlbedoSlot.Texture = Texture2D.Default1x1ColorPixel_RGBA32ub(new vec4ub(255, 0, 255, 255));
        }
        return _defaultObjectMaterial;
      }
    }
    //     public static Material DefaultBillboard()
    // {
    //   //TODO: - the input shader should also be default.
    //   if (_defaultObjectShader == null)
    //   {
    //     _defaultObjectShader = new Material("DefaultBillboard", Shader.DefaultBillboardPoints());
    //   }
    //   return _defaultObjectShader;
    // }

    #endregion

  }
}
