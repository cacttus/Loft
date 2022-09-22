using System;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace PirateCraft
{
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

    public TextureInput(string name, Texture2D default_tex) : base(name + "-textureinput")
    {
      _default = default_tex;
    }

  }
  public enum PBRTextureType
  {
    Albedo,
    Normal,
    Metal,
    Rough,
    Spec,
    Height
  }

  public class PBRTextureArray
  {
    //Simply, an array of textures mapped to common PBR enums for convenience.
    public string Name { get; private set; } = Gu.UnsetName;

    public Dictionary<PBRTextureType, Texture2D> Texs { get; private set; } = new Dictionary<PBRTextureType, Texture2D>();
    public Dictionary<PBRTextureType, Img32> Imgs { get; private set; } = new Dictionary<PBRTextureType, Img32>();

    public Texture2D AlbedoTexture { get { return GetTexture(PBRTextureType.Albedo); } }
    public Texture2D NormalTexture { get { return GetTexture(PBRTextureType.Normal); } }
    //...

    public Img32 AlbedoImage { get { return GetImage(PBRTextureType.Albedo); } }
    public Img32 NormalImage { get { return GetImage(PBRTextureType.Normal); } }
    //...

    public PBRTextureArray(string name)
    {
      Name = name;
    }
    public void CreateNormalMap(bool generateMipmaps, TexFilter texFilter, bool tryTextureAlbedoIfImageAlbedoNotFound = false)
    {
      //TODO: implement tryTextureAlabedo, load from GPU, then FLIP
      Gu.Log.Debug("..Creating Normal Map.");
      if (Imgs.TryGetValue(PBRTextureType.Albedo, out Img32 img))
      {
        var normal = img.CreateNormalMap(false);
        Imgs.Add(PBRTextureType.Normal, normal);
        var txNormal = new Texture2D(normal, generateMipmaps, texFilter);
        Texs.Add(PBRTextureType.Normal, txNormal);
      }
      else
      {
        Gu.Log.Error($"{Name} Could not creat normal map, albedo image not found. ");
      }
    }
    public Texture2D GetTexture(PBRTextureType tex)
    {
      if (Texs.TryGetValue(tex, out var tx))
      {
        return tx;
      }
      return null;
    }
    public Img32 GetImage(PBRTextureType tex)
    {
      if (Imgs.TryGetValue(tex, out var img))
      {
        return img;
      }
      return null;
    }
    public Texture2D CreateTexture(PBRTextureType type, Img32 img, bool generateMipmaps, TexFilter filter, bool saveImage)
    {
      var tx = new Texture2D(img, generateMipmaps, filter);
      this.Texs[type] = tx;
      if (saveImage)
      {
        this.Imgs[type] = img;
      }
      return tx;
    }

  }

  //Material, input to a shader & gpu state for material FBO (blending, etc)
  public class Material : DataBlock
  {
    private static Material _debugdraw_normals_material = null; //Default color material / shader.
    private static Material _defaultObjectMaterial = null; //Default color material / shader.
    private static Material _defaultFlatColorMaterial = null; //Default color material / shader.

    #region Public: Members

    public vec4 BaseColor { get { return _baseColor; } set { _baseColor = value; SetModified(); } }
    public float Roughness { get { return _roughness; } set { _roughness = value; SetModified(); } }
    public float Metallic { get { return _metallic; } set { _metallic = value; SetModified(); } }
    public float Specular { get { return _specular; } set { _specular = value; SetModified(); } }
    public float IndexOfRefraction { get { return _indexOfRefraction; } set { _indexOfRefraction = value; SetModified(); } }
    public bool Flat { get { return _flat; } set { _flat = value; SetModified(); } }

    //TODO: we can use PBRTextureARray here instead.
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
      if (_bMustCompile || Gu.EngineConfig.Debug_AlwaysCompileAndReloadGpuUniformData)
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
      this._albedoSlot = new TextureInput(name + "-albedo", Texture2D.Default1x1ColorPixel_RGBA32ub(vec4ub.White));
      this._normalSlot = new TextureInput(name + "-normal", Texture2D.Default1x1NormalPixel_RGBA32ub());
      this._roughnessSlot = new TextureInput(name + "-roughness", Texture2D.Default1x1ColorPixel_RGBA32ub(vec4ub.White));
      this._metalnessSlot = new TextureInput(name + "-metalness", Texture2D.Default1x1ColorPixel_RGBA32ub(vec4ub.White));
      this._positionSlot = new TextureInput(name + "-position", Texture2D.Default1x1ColorPixel_RGBA32ub(vec4ub.White));

      Shader = s;
    }
    public Material(string name, Shader s, Texture2D albedo) : this(name, s)
    {
      _albedoSlot.Texture = albedo;
    }
    public Material(string name, Shader s, Texture2D albedo, Texture2D normal) : this(name, s)
    {
      _albedoSlot.Texture = albedo;
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
        if (_defaultFlatColorMaterial == null)
        {
          _defaultFlatColorMaterial = new Material(System.Reflection.MethodBase.GetCurrentMethod().Name, Shader.DefaultFlatColorShader());
          _defaultObjectMaterial.AlbedoSlot.Texture = Texture2D.Default1x1ColorPixel_RGBA32ub(new vec4ub(255, 255, 255, 255));
        }
        return _defaultFlatColorMaterial;
      }
    }
    public static Material DefaultObjectMaterial
    {
      get
      {
        if (_defaultObjectMaterial == null)
        {
          _defaultObjectMaterial = new Material(System.Reflection.MethodBase.GetCurrentMethod().Name, Shader.DefaultObjectShader());
          _defaultObjectMaterial.AlbedoSlot.Texture = Texture2D.Default1x1ColorPixel_RGBA32ub(new vec4ub(255, 0, 255, 255));
        }
        return _defaultObjectMaterial;
      }
    }
    public static Material DebugDraw_VertexNormals_FlatColor
    {
      get
      {
        if (_debugdraw_normals_material == null)
        {
          _debugdraw_normals_material = new Material(System.Reflection.MethodBase.GetCurrentMethod().Name, 
          Gu.Resources.LoadShader("v_normals", true, FileStorage.Embedded, OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles));
          _debugdraw_normals_material.GpuRenderState.Blend = false;
          _debugdraw_normals_material.BaseColor = new vec4(0.827f,0.933f,0.113f,1);
        }
        return _debugdraw_normals_material;
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
