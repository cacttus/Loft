using System;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace Loft
{
  public enum PBRTextureInput
  {
    [Description("_ufGpuMaterial_s2Albedo")] Albedo,//indexes map to shader index
    [Description("_ufGpuMaterial_s2Normal")] Normal,
    [Description("_ufGpuMaterial_s2Position")] Position,
    [Description("_ufGpuMaterial_s2Roughness")] Rough,
    [Description("_ufGpuMaterial_s2Metalness")] Metal,
    [Description("_ufGpuMaterial_s2Pick")] Pick,
    [Description("_ufGpuMaterial_s2Spec")] Spec,
    [Description("_ufGpuMaterial_s2Height")] Height,
    [Description("_ufGpuWorld_s2EnvironmentMap")] EnvironmentMap,
    [Description("_ufGpuWorld_s2IrradianceMap")] IrradianceMap,

    [Description("_ufGpuMaterial_s2Other")] Other
  }
  [DataContract]
  public class TextureSlot
  {
    [DataMember] private Texture? _texture = null;
    [DataMember] private PBRTextureInput? _texType = null;
    [DataMember] private int? _index = 0;//if not null use index of this
    [DataMember] private string _name = "";//if not null use index of this

    //Has a texture or the default if none supplied
    public Texture? Texture { get { return _texture; } set { _texture = value; } }
    public string Name { get { return _name; } set { _name = value; } }
    public TextureSlot(Texture t, string name, int? index = null)
    {
      _texture = t;
      _name = name;
      _index = index;
      _texType = null;
    }
    public TextureSlot(PBRTextureInput textype, int? index = null)
    {
      _texType = textype;
      _index = index;
      _name = textype.Description();
      if (textype == PBRTextureInput.Other && index != null)
      {
        _name += index.ToString();
      }
    }
    public Texture? GetTextureOrDefault()
    {
      if (Texture == null)
      {
        if (_texType == PBRTextureInput.Normal)
        {
          return Gu.Lib.GetTexture(Rs.Tex2D.DefaultNormalPixel);
        }
        else if (_texType == PBRTextureInput.Pick)
        {
          //TODO: create default zero uint 
          return Gu.Lib.GetTexture(Rs.Tex2D.DefaultBlackPixelNoAlpha);
        }
        return Gu.Lib.GetTexture(Rs.Tex2D.DefaultWhitePixel);
      }
      else
      {
        return Texture;
      }
    }
  }

  public class PBRTextureArray
  {
    //Simply, an array of textures mapped to common PBR enums for convenience.
    public string Name { get; private set; } = Lib.UnsetName;

    public Dictionary<PBRTextureInput, Texture> Texs { get; private set; } = new Dictionary<PBRTextureInput, Texture>();
    public Dictionary<PBRTextureInput, Image> Imgs { get; private set; } = new Dictionary<PBRTextureInput, Image>();

    public Texture Albedo { get { return GetTexture(PBRTextureInput.Albedo); } }
    public Texture Normal { get { return GetTexture(PBRTextureInput.Normal); } }
    //...

    public Image AlbedoImage { get { return GetImage(PBRTextureInput.Albedo); } }
    public Image NormalImage { get { return GetImage(PBRTextureInput.Normal); } }
    //...

    public PBRTextureArray(string name)
    {
      Name = name;
    }
    public void CreateNormalMap(bool generateMipmaps, TexFilter texFilter, float strength = 0.5f, bool tryTextureAlbedoIfImageAlbedoNotFound = false)
    {
      //TODO: implement tryTextureAlabedo, load from GPU, then FLIP
      Gu.Log.Debug("..Creating Normal Map.");
      if (Imgs.TryGetValue(PBRTextureInput.Albedo, out Image img))
      {
        var normal = img.CreateNormalMap(false, strength);
        Imgs.Add(PBRTextureInput.Normal, normal);
        var txNormal = new Texture(normal, generateMipmaps, texFilter);
        Texs.Add(PBRTextureInput.Normal, txNormal);
      }
      else
      {
        Gu.Log.Error($"{Name} Could not creat normal map, albedo image not found. ");
      }
    }
    public Texture GetTexture(PBRTextureInput tex)
    {
      if (Texs.TryGetValue(tex, out var tx))
      {
        return tx;
      }
      return null;
    }
    public Image GetImage(PBRTextureInput tex)
    {
      if (Imgs.TryGetValue(tex, out var img))
      {
        return img;
      }
      return null;
    }
    public Texture CreateTexture(PBRTextureInput type, Image img, bool generateMipmaps, TexFilter filter, bool saveImage)
    {
      var tx = new Texture(img, generateMipmaps, filter);
      this.Texs[type] = tx;
      if (saveImage)
      {
        this.Imgs[type] = img;
      }
      return tx;
    }

  }

  [DataContract]
  public class Material : DataBlock
  {
    //Material, input to a shader & gpu state for material FBO (blending, etc)
    #region Public: Members

    public bool Modified { get; private set; } = true;
    public void SetModified()
    {
      Modified = true;
    }

    public vec4 BaseColor { get { return _baseColor; } set { _baseColor = value; SetModified(); } }
    public float Roughness { get { return _roughness; } set { _roughness = value; SetModified(); } }
    public float Metallic { get { return _metallic; } set { _metallic = value; SetModified(); } }
    public float Specular { get { return _specular; } set { _specular = value; SetModified(); } }
    public float IndexOfRefraction { get { return _indexOfRefraction; } set { _indexOfRefraction = value; SetModified(); } }
    public bool Flat { get { return _flat; } set { _flat = value; SetModified(); } }
    public DrawMode DrawMode { get { return _drawMode; } set { _drawMode = value; SetModified(); } }
    public DrawOrder DrawOrder { get { return _drawOrder; } set { _drawOrder = value; SetModified(); } }
    public bool DoubleSided { get { return _doubleSided; } set { _doubleSided = value; this._gpuRenderState.CullFace = !_doubleSided; SetModified(); } }

    //TODO: we can use PBRTextureARray here instead.
    public TextureSlot AlbedoSlot { get { return _textures[(int)PBRTextureInput.Albedo]; } private set { _textures[(int)PBRTextureInput.Albedo] = value; SetModified(); } }
    public TextureSlot NormalSlot { get { return _textures[(int)PBRTextureInput.Normal]; } private set { _textures[(int)PBRTextureInput.Normal] = value; SetModified(); } }
    public TextureSlot RoughnessSlot { get { return _textures[(int)PBRTextureInput.Rough]; } private set { _textures[(int)PBRTextureInput.Rough] = value; SetModified(); } }
    public TextureSlot MetalnessSlot { get { return _textures[(int)PBRTextureInput.Metal]; } private set { _textures[(int)PBRTextureInput.Metal] = value; SetModified(); } }
    public TextureSlot PositionSlot { get { return _textures[(int)PBRTextureInput.Position]; } private set { _textures[(int)PBRTextureInput.Position] = value; SetModified(); } }
    public TextureSlot PickSlot { get { return _textures[(int)PBRTextureInput.Pick]; } private set { _textures[(int)PBRTextureInput.Pick] = value; SetModified(); } }
    public List<TextureSlot> Textures { get { return _textures; } }

    public Shader? Shader { get { return _shader; } private set { _shader = value; SetModified(); } }
    public GpuRenderState GpuRenderState { get { return _gpuRenderState; } set { _gpuRenderState = value; SetModified(); } } //The rendering state of the material: clipping, depth, alpha, culling, etc



    private GPUBuffer _gpuMaterialBuf = null;
    public GPUBuffer GpuMaterial
    {
      get
      {
        CompileGpuData();
        return _gpuMaterialBuf;
      }
    }

    #endregion

    #region Private:Members
    [DataMember] private GpuRenderState _gpuRenderState = new GpuRenderState();
    [DataMember] private Shader? _shader = null;
    [DataMember] private List<TextureSlot> _textures = new List<TextureSlot>();
    [DataMember] private vec4 _baseColor = new vec4(1, 1, 1, 1);
    [DataMember] private float _roughness = 0.5f;
    [DataMember] private float _metallic = 0.0f;
    [DataMember] private float _specular = 0.0f;//for now this is blinn phong spec pwoer
    [DataMember] private float _indexOfRefraction = 1;//1.45
    [DataMember] private bool _flat = false;
    [DataMember] private DrawMode _drawMode = DrawMode.Deferred;//Future TODO: - draw mode per each pipeline stage, if needed.
    [DataMember] private DrawOrder _drawOrder = DrawOrder.Mid;
    [DataMember] private bool _doubleSided = false;

    public GpuMaterial _gpuMaterial = new GpuMaterial();//default(GpuMaterial);
    public glTFLoader.Schema.Material.AlphaModeEnum AlphaMode = glTFLoader.Schema.Material.AlphaModeEnum.BLEND;
    //**TODO: alpha is actually in the GpuRenderState

    public void SetTexture(Texture tex, int index)
    {
      Gu.Assert(index <= Gu.Context.Gpu.MaxFragmentTextureImageUnits,
       $"Tried to add texture to index: {index}, which exceeded max shader samplers: {Gu.Context.Gpu.MaxFragmentTextureImageUnits}");
      while (_textures.Count < index)
      {
        _textures.Add(new TextureSlot(PBRTextureInput.Other, index));
      }
      _textures[index].Texture = tex;
    }


    #endregion

    #region Public: Methods
    public void CompileGpuData()
    {
      if (_gpuMaterialBuf == null || Modified)
      {
        _gpuMaterial._vPBR_baseColor = _baseColor;
        _gpuMaterial._fPBR_roughness = _roughness;
        _gpuMaterial._fPBR_metallic = _metallic;
        _gpuMaterial._fPBR_indexOfRefraction = _indexOfRefraction;
        _gpuMaterial._fPBR_specular = _specular;
        _gpuMaterial._flat = this._flat ? 1 : 0;

        if (_gpuMaterialBuf == null)
        {
          _gpuMaterialBuf = Gpu.CreateUniformBuffer(Name + "ubbuf", _gpuMaterial);
        }
        else
        {
          _gpuMaterialBuf.CopyToGPU(_gpuMaterial);
        }
        Modified = false;

      }
    }

    protected Material() { }//clone ctor
    public Material(string name, Shader? s = null) : base(name)
    {
      _textures = new List<TextureSlot>(){
          new TextureSlot((PBRTextureInput)0),
          new TextureSlot((PBRTextureInput)1),
          new TextureSlot((PBRTextureInput)2),
          new TextureSlot((PBRTextureInput)3),
          new TextureSlot((PBRTextureInput)4),
          new TextureSlot((PBRTextureInput)5),
       };

      if (s == null)
      {
        //generate
        Shader = Gu.Lib.GetShader(Rs.Shader.DefaultObjectShader);
      }
      else
      {
        Shader = s;
      }

    }
    public Material(string name, Texture albedo, Shader? s = null) : this(name, s)
    {
      this.AlbedoSlot.Texture = albedo;
    }
    public Material(string name, Texture albedo, Texture normal, Shader? s = null) : this(name, s)
    {
      this.AlbedoSlot.Texture = albedo;
      this.NormalSlot.Texture = normal;
    }
    public Material Clone()
    {
      var other = (Material)this.MemberwiseClone();

      other.GpuRenderState = this.GpuRenderState.Clone();
      other._textures = new List<TextureSlot>(this._textures);

      return other;
    }

  }

  #endregion

}

