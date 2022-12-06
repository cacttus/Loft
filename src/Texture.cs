
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using OpenTK.Graphics.OpenGL4;

namespace Loft
{
  public enum NormalMapFormat
  {
    Zup, Yup
  }
  public enum TexFilter
  {
    Nearest, //If mipmaps enabled, then this will also choose nearest mipmap (mipmapping should be specified if possible)
    Linear, //Simple linear filtering of texels (no mipmapping, regardless if it is set)
    Bilinear, //Linear filter texture, but use nearest mipmap (smooth with some noticable mipmap jumps). 
    Trilinear, //Linear filter texture, linear filter between mipmaps. (very smooth / blurry).
    Separate
  }
  public class GpuTexture : OpenGLResource
  {
    public GpuTexture(string name) : base(name)
    {
      this._glId = GT.GenTexture();
    }
    
    protected override string DataPathName() { return "-tex" + base.DataPathName(); }
   
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsTexture(GlId))
      {
        GT.DeleteTexture(GlId);
      }
    }
  }

  [DataContract]
  public class Texture : OpenGLContextDataManager<GpuTexture>
  {
    private class BoundTexture2DState
    {
      public TextureUnit Unit;
      public int BindingId;
    }
    #region Public: Members

    public override bool IsDataShared { get { return true; } }

    public static NormalMapFormat NormalMapFormat { get; private set; } = NormalMapFormat.Yup;

    public int Width { get { return _width; } set { _width = value; } }
    public int Height { get { return _height; } set { _height = value; } }
    public ivec2 Size { get { return new ivec2(_width, _height); } }
    public TextureMinFilter MinFilter { get { return _minFilter; } set { _minFilter = value; } }
    public TextureMagFilter MagFilter { get { return _magFilter; } set { _magFilter = value; } }
    public TextureWrapMode WrapModeS { get { return _wrapModeS; } set { _wrapModeS = value; } }
    public TextureWrapMode WrapModeT { get { return _wrapModeT; } set { _wrapModeT = value; } }
    public PixelFormat PixelFormat { get { return _pixelFormat; } set { _pixelFormat = value; } }
    public PixelInternalFormat PixelInternalFormat { get { return _pixelInternalFormat; } set { _pixelInternalFormat = value; } }
    public PixelType PixelType { get { return _pixelType; } set { _pixelType = value; } }
    public TextureTarget TextureTarget { get { return _textureTarget; } set { _textureTarget = value; } }

    protected override GpuTexture CreateNew()
    {
      return new GpuTexture(this.Name);
    }

    #endregion
    #region Private:Members

    [DataMember] private int _width = 0;
    [DataMember] private int _height = 0;
    [DataMember] private TextureMinFilter _minFilter = TextureMinFilter.Nearest;
    [DataMember] private TextureMagFilter _magFilter = TextureMagFilter.Nearest;
    [DataMember] private TextureWrapMode _wrapModeS = TextureWrapMode.ClampToEdge;
    [DataMember] private TextureWrapMode _wrapModeT = TextureWrapMode.ClampToEdge;
    [DataMember] private Image _image = null;//Data source for this texture.
    [DataMember] private int _numMipmaps = 0; // if zero mipmapping disabledd
    [DataMember] private PixelFormat _pixelFormat;
    [DataMember] private PixelInternalFormat _pixelInternalFormat;
    [DataMember] private PixelType _pixelType;
    [DataMember] private TextureTarget _textureTarget;
    private TextureUnit _boundUnit = TextureUnit.Texture0;
    private Stack<BoundTexture2DState> _states = new Stack<BoundTexture2DState>();
    private static Texture _default1x1ColorPixel_RGBA32ub = null;
    private static Texture _default1x1NormalPixel_RGBA32ub = null;

    public bool IsTexture
    {
      get
      {
        var t = GpuTexture;
        Gu.Assert(t != null);
        return GL.IsTexture(t.GlId);
      }
    }
    public int GlId
    {
      get
      {
        var t = GpuTexture;
        Gu.Assert(t != null);
        return t.GlId;
      }
    }
    public GpuTexture GpuTexture
    {
      get
      {
        return this.GetDataForContext(Gu.Context);
      }
    }

    #endregion
    #region Public:Methods
    protected Texture() { } //Clone/serialie 
    public Texture(string name, Image img, bool mipmaps, TexFilter filter, TextureWrapMode wrap = TextureWrapMode.Repeat) : base(name)
    {
      //set params but do not load unless specified by Load()
      LoadToGpu(img, mipmaps, filter, wrap);
    }
    public Texture(string name, Image img, bool mipmaps, TextureMinFilter minf, TextureMagFilter magf, TextureWrapMode wrapS, TextureWrapMode wrapT) : base(name)
    {
      LoadToGpu(img, mipmaps, minf, magf, wrapS, wrapT);
    }
    public Texture(Image img, bool mipmaps, TexFilter filter, TextureWrapMode wrap = TextureWrapMode.Repeat) : base(img.Name)
    {
      LoadToGpu(img, mipmaps, filter, wrap);
    }
    public Texture(string name, PixelInternalFormat eInternalFormat, PixelFormat eTextureFormat, PixelType eDataType, int iWidth, int iHeight, int nSamples) : base(name)
    {
      //MakeRenderTexture
      //creates an RGBA render texture for framebuffer

      //TODO: -- special texture images need to be refactored - we can mke generic, for tex size etc.
      Gpu.CheckGpuErrorsRt();
      GL.ActiveTexture(TextureUnit.Texture0);

      this.Persistence = DataPersistence.Temporary;

      this.PixelFormat = eTextureFormat;
      this.PixelType = eDataType;
      this.PixelInternalFormat = eInternalFormat;
      this.Width = iWidth;
      this.Height = iHeight;

      Gpu.CheckGpuErrorsRt();

      //this is for mipmaps or shadows .. array textures
      //      GL.TexStorage2D(TextureTarget2d.Texture2D, _numMipmaps, this.SizedInternalFormat, (int)Width, (int)Height

      if (nSamples > 0)
      {
        TextureTarget = TextureTarget.Texture2DMultisample;
        GL.BindTexture(TextureTarget, GlId);
        Gpu.CheckGpuErrorsRt();

        //if (Gu::GetEngineDisplayParams()->getEnableAnisotropicFiltering())
        //{
        //    //CHANGED FROM GL_TEXTURE_2D **MIGHT BE WRONG
        //   GL.TexParameterf(GL_TEXTURE_2D_MULTISAMPLE, GL_TEXTURE_MAX_ANISOTROPY_EXT, Gu::GetEngineDisplayParams()->getTextureAnisotropyLevel());
        //    CheckGpuErrorsDbg();
        //}
        //I think this is stupid. Just add the additional enums to TextureTarget @Microsoft
        GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, nSamples, eInternalFormat, iWidth, iHeight, true);
        Gpu.CheckGpuErrorsRt();
      }
      else
      {
        TextureTarget = TextureTarget.Texture2D;
        GL.BindTexture(TextureTarget, GlId);
        Gpu.CheckGpuErrorsRt();

        //if (Gu::GetEngineDisplayParams()->getEnableAnisotropicFiltering())
        //{
        //   GL.TexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAX_ANISOTROPY_EXT, Gu::GetEngineDisplayParams()->getTextureAnisotropyLevel());
        //    Gu::getGraphicsContext()->chkErrRt();
        //}
        GL.TexImage2D(TextureTarget, 0, eInternalFormat, iWidth, iHeight, 0, eTextureFormat, eDataType, IntPtr.Zero);
        Gpu.CheckGpuErrorsRt();

        GL.TexParameter(TextureTarget, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        Gpu.CheckGpuErrorsRt();

        GL.TexParameter(TextureTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        Gpu.CheckGpuErrorsRt();
      }
      GL.Disable(EnableCap.Dither);  //Dithering gets enabled for some reason

      Gpu.CheckGpuErrorsRt();

      GpuTexture.SetObjectLabel();

      GL.BindTexture(TextureTarget, 0);
      Gpu.CheckGpuErrorsRt();
    }
    public Texture(string owner, int w, int h, int nMsaaSamples) : base(owner + "_DepthTexture" + (nMsaaSamples > 0 ? "_Multisample" : ""))
    {
      //createdepthtexture
      //creates a compatible depth texture for framebuffer

      //TODO: -- special texture images need to be refactored - we can mke generic, for tex size etc.

      //Creates a depth texture, or multisample depth texture on texture channel 0
      //This will query the device to make sure the depth format is supported.
      Gpu.CheckGpuErrorsRt();
      //TextureTarget texTarget;

      this.Persistence = DataPersistence.Temporary;

      GL.ActiveTexture(TextureUnit.Texture0);

      PixelFormat = PixelFormat.DepthComponent;
      PixelType = PixelType.Float;
      this.Width = w;
      this.Height = h;

      //this is for mipmaps or shadows .. array textures
      //      GL.TexStorage2D(TextureTarget2d.Texture2D, _numMipmaps, this.SizedInternalFormat, (int)Width, (int)Height);

      if (nMsaaSamples > 0)
      {
        this.TextureTarget = TextureTarget.Texture2DMultisample;
      }
      else
      {
        this.TextureTarget = TextureTarget.Texture2D;
      }
      GL.ActiveTexture(TextureUnit.Texture0);
      var created = GpuTexture;
      Gpu.CheckGpuErrorsRt();
      GL.BindTexture(this.TextureTarget, GlId);

      //THe following parameters are for depth textures only
      Gpu.CheckGpuErrorsRt();

      if (nMsaaSamples > 0)
      {
        //For some reason you can't use this with multisample.

        //**NOTE: we changed this from GL.TexparameterI
        GL.TexParameter(this.TextureTarget, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);  //GL_NONE
        Gpu.CheckGpuErrorsRt();

        //OpenTK.Graphics.OpenGL.TextureCompareMode
        // GL.TexParameter(texTarget, TextureParameterName.TextureCompareFunc,  OpenTK.Graphics.OpenGL4.depthtexture  GL_LEQUAL);
        // Gpu.CheckGpuErrorsRt();
        Gu.Log.Warn("Commented out Texturecomparefunc");

        GL.TexParameter(this.TextureTarget, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        Gpu.CheckGpuErrorsRt();

        GL.TexParameter(this.TextureTarget, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        Gpu.CheckGpuErrorsRt();

        GL.TexParameter(this.TextureTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        Gpu.CheckGpuErrorsRt();

        GL.TexParameter(this.TextureTarget, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        Gpu.CheckGpuErrorsRt();
      }

      var that = this;
      //This will cycle through depth formats and choose the most precise.
      //Loop over creating a texture until we get no error
      // if(eRequestedDepth == PixelInternalFormat.DepthComponent32f){
      // }
      GetCompatibleDepthComponent(32, (eDepth) =>
      {
        if (nMsaaSamples > 0)
        {
          //texTarget = TextureTargetMultisample.Texture2D;
          //..ok .. it's the same Enum 
          GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, nMsaaSamples, eDepth, w, h, true);
          Gpu.CheckGpuErrorsRt();

        }
        else
        {
          GL.TexImage2D(this.TextureTarget, 0, eDepth, w, h, 0, that.PixelFormat, that.PixelType, IntPtr.Zero);
          Gpu.CheckGpuErrorsRt();
        }
      });

      Gpu.CheckGpuErrorsRt();

      GpuTexture.SetObjectLabel();
      GL.BindTexture(this.TextureTarget, 0);
      Gpu.CheckGpuErrorsRt();
    }
    public void SetWrap(TextureWrapMode wraps, TextureWrapMode wrapt)
    {
      Gu.Assert(GL.IsTexture(this.GlId));
      WrapModeS = wraps;
      WrapModeT = wrapt;
      PushState(TextureTarget);
      Bind(Gpu.GetActiveTexture());
      GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapS, (int)WrapModeS);
      GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapT, (int)WrapModeT);
      PopState(TextureTarget);
    }
    public void SetFilter(TextureMinFilter min, TextureMagFilter mag)
    {
      Gu.Assert(GL.IsTexture(GpuTexture.GlId));
      MinFilter = min;
      MagFilter = mag;
      PushState(TextureTarget);
      Bind(Gpu.GetActiveTexture());
      GL.TexParameter(TextureTarget, TextureParameterName.TextureMinFilter, (int)min);//LinearMipmapLinear
      GL.TexParameter(TextureTarget, TextureParameterName.TextureMagFilter, (int)mag);
      PopState(TextureTarget);
    }
    private static void GetSimpleFilter(TexFilter filter, bool mipmaps, out TextureMinFilter min, out TextureMagFilter mag)
    {
      min = TextureMinFilter.Nearest;
      mag = TextureMagFilter.Nearest;
      if (filter == TexFilter.Linear)
      {
        min = TextureMinFilter.Linear;
        mag = TextureMagFilter.Linear;
      }
      else if (filter == TexFilter.Nearest)
      {
        if (mipmaps)
        {
          //Mipmap nearest is smoother than simply nearest, it looks better
          min = TextureMinFilter.NearestMipmapNearest;
        }
        else
        {
          min = TextureMinFilter.Nearest;
        }
        mag = TextureMagFilter.Nearest;
      }
      else if (filter == TexFilter.Bilinear)
      {
        if (mipmaps)
        {
          min = TextureMinFilter.LinearMipmapNearest;
          mag = TextureMagFilter.Linear;
        }
        else
        {
          Gu.Log.Warn("No mipmaps specified for texure but bilinear / trilinear filtering was specified.");
        }
      }
      else if (filter == TexFilter.Trilinear)
      {
        if (mipmaps)
        {
          min = TextureMinFilter.LinearMipmapLinear;
          mag = TextureMagFilter.Linear;
        }
        else
        {
          Gu.Log.Warn("No mipmaps specified for texure but bilinear / trilinear filtering was specified.");
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    public void Bind(TextureUnit unit)
    {
      if (GlId == 0)
      {
        throw new System.Exception("Texture ID was 0 when binding texture.");
      }

      _boundUnit = unit;
      Gpu.CheckGpuErrorsDbg();
      GL.ActiveTexture(unit);
      Gpu.CheckGpuErrorsDbg();
      GL.BindTexture(TextureTarget, GlId);
      Gpu.CheckGpuErrorsDbg();
    }
    public void Unbind()
    {
      Gpu.CheckGpuErrorsDbg();
      GL.ActiveTexture(_boundUnit);
      Gpu.CheckGpuErrorsDbg();
      GL.BindTexture(TextureTarget, 0);
      Gpu.CheckGpuErrorsDbg();
    }

    #endregion
    #region Private:Methods
    private int GetNumMipmaps(int w, int h)
    {
      int numMipMaps = 0;
      int x = System.Math.Max(w, h);
      for (; x > 0; x = x >> 1)
      {
        numMipMaps++;
      }
      return numMipMaps;
    }
    private void PushState(TextureTarget target)
    {
      TextureUnit tex_unit = Gpu.GetActiveTexture();
      int tex_binding = 0;

      GetPName binding = GetPName.TextureBinding2D;
      if (target == TextureTarget.Texture2D) { binding = GetPName.TextureBinding2D; }
      else if (target == TextureTarget.Texture2DMultisample) { binding = GetPName.TextureBinding2DMultisample; }
      else { Gu.BRThrowNotImplementedException(); }

      GL.GetInteger(binding, out tex_binding);

      _states.Push(new BoundTexture2DState() { Unit = tex_unit, BindingId = tex_binding });
    }
    private void PopState(TextureTarget target)
    {
      BoundTexture2DState state = _states.Pop();
      GL.ActiveTexture(state.Unit);
      GL.BindTexture(target, state.BindingId);
    }
    private void GetCompatibleDepthComponent(int max_bits, Action<PixelInternalFormat> func)
    {
      //Try a bunch of depth formats.
      //max_bits isn't used
      //we don't use stencil here. Use as much as we can get.
      List<PixelInternalFormat> fmts = new List<PixelInternalFormat>(){
        PixelInternalFormat.DepthComponent32f,
        PixelInternalFormat.DepthComponent32,
        PixelInternalFormat.DepthComponent24,
        PixelInternalFormat.DepthComponent16
      };
      foreach (var fmt in fmts)
      {
        func(fmt);
        if (GL.GetError() == ErrorCode.NoError)
        {
          PixelInternalFormat = fmt;
          return;
        }
      }
      Gu.BRThrowException("Could not find suitable depth buffer pixelformat.");
    }
    private bool IsMipmappingEnabled()
    {
      return _numMipmaps > 0;
    }
    private SizedInternalFormat GetSizedInternalFormat()
    {
      if (PixelType == OpenTK.Graphics.OpenGL4.PixelType.UnsignedByte)
      {
        if (PixelInternalFormat == PixelInternalFormat.Rgba)
        {
          return SizedInternalFormat.Rgba8;
        }
      }
      Gu.Log.Error("Invalid texture format.");
      Gu.BRThrowNotImplementedException();
      return SizedInternalFormat.R16;
    }
    private void LoadToGpu(Image bmp, bool mipmaps, TexFilter simpleFilter, TextureWrapMode wrapST)
    {
      TextureMinFilter minf = TextureMinFilter.Nearest;
      TextureMagFilter magf = TextureMagFilter.Nearest;
      GetSimpleFilter(simpleFilter, mipmaps, out minf, out magf);
      LoadToGpu(bmp, mipmaps, minf, magf, wrapST, wrapST);
    }
    private void LoadToGpu(Image bmp, bool mipmaps, TextureMinFilter minf, TextureMagFilter magf, TextureWrapMode wrapS, TextureWrapMode wrapT)
    {
      _image = bmp;
      Width = bmp.Width;
      Height = bmp.Height;
      //Filter = filter;
      //WrapMode = WrapMode;
      WrapModeS = wrapS;
      WrapModeT = wrapT;
      PixelFormat = PixelFormat.Rgba;
      PixelType = PixelType.UnsignedByte;
      PixelInternalFormat = PixelInternalFormat.Rgba;
      TextureTarget = TextureTarget.Texture2D;

      int ts = Gu.Context.Gpu.MaxTextureSize;
      if (Width >= ts)
      {
        Gu.BRThrowException("Texture is too large");
      }
      if (Height >= ts)
      {
        Gu.BRThrowException("Texture is too large");
      }

      _numMipmaps = 1;
      if (mipmaps)
      {
        _numMipmaps = GetNumMipmaps((int)Width, (int)Height);
      }

      var loadtex = GpuTexture;
      GL.BindTexture(TextureTarget, GlId);
      //SetSimpleFilter(filter);
      SetWrap(WrapModeS, WrapModeT);

      GL.TexStorage2D(TextureTarget2d.Texture2D, _numMipmaps, GetSizedInternalFormat(), (int)Width, (int)Height);

      var pinnedHandle = GCHandle.Alloc(bmp.Data, GCHandleType.Pinned);
      GL.TexSubImage2D(TextureTarget,
          0, //mipmap level
          0, 0, //x.y
          this.Width,
          this.Height,
          this.PixelFormat,
          this.PixelType,
          pinnedHandle.AddrOfPinnedObject());
      pinnedHandle.Free();

      if (mipmaps)
      {
        GT.GenerateMipmap(GenerateMipmapTarget.Texture2D);
      }

      GpuTexture.SetObjectLabel();

      GL.BindTexture(TextureTarget, 0);

    }

    #endregion
  }


}
