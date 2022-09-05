
using OpenTK.Graphics.OpenGL4;

namespace PirateCraft
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
  public class Texture2D : OpenGLResource
  {
    private class BoundTexture2DState
    {
      public TextureUnit Unit;
      public int BindingId;
    }
    #region Public: Members

    public static NormalMapFormat NormalMapFormat { get; private set; } = NormalMapFormat.Yup;

    public int Width { get; private set; } = 0;
    public int Height { get; private set; } = 0;
    public TexFilter Filter { get; private set; } = TexFilter.Nearest;
    public TextureWrapMode WrapMode { get; private set; } = TextureWrapMode.ClampToEdge;
    public PixelFormat PixelFormat { get; private set; }
    public PixelInternalFormat PixelInternalFormat { get; private set; }
    public PixelType PixelType { get; private set; }
    public TextureTarget TextureTarget { get; private set; }

    #endregion
    #region Private:Members

    private TextureUnit _boundUnit = TextureUnit.Texture0;
    private int _numMipmaps = 0; // if zero mipmapping disabledd
    private Stack<BoundTexture2DState> _states = new Stack<BoundTexture2DState>();
    private static StaticContextData<Texture2D> _default1x1ColorPixel_RGBA32ub = new StaticContextData<Texture2D>();
    private static StaticContextData<Texture2D> _default1x1NormalPixel_RGBA32ub = new StaticContextData<Texture2D>();

    #endregion
    #region Public:Methods
    public Texture2D(string name, Img32 img, bool mipmaps, TexFilter filter, TextureWrapMode wrap = TextureWrapMode.Repeat) : base(name + "-tex2d")
    {
      LoadToGpu(img, mipmaps, filter, wrap);
    }
    public Texture2D(Img32 img, bool mipmaps, TexFilter filter, TextureWrapMode wrap = TextureWrapMode.Repeat) : base(img.Name)
    {
      LoadToGpu(img, mipmaps, filter, wrap);
    }
    public Texture2D(FileLoc loc, bool mipmaps, TexFilter filter, TextureWrapMode wrap = TextureWrapMode.Repeat) : base(loc.QualifiedPath)
    {
      Img32 img = null;
      try
      {
        img = ResourceManager.LoadImage(loc);
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Image not found. Loading default image.",ex);
        img = Img32.Default1x1_RGBA32ub(255, 0, 255, 255);
      }

      LoadToGpu(img, mipmaps, filter, wrap);
    }
    public Texture2D(string name, PixelInternalFormat eInternalFormat, PixelFormat eTextureFormat, PixelType eDataType, int iWidth, int iHeight, int nSamples) : base(name + "-tex2d")
    {
      //MakeRenderTexture
      Gpu.CheckGpuErrorsRt();
      GL.ActiveTexture(TextureUnit.Texture0);

      this._glId = GL.GenTexture();
      this.PixelFormat = eTextureFormat;
      this.PixelType = eDataType;
      this.PixelInternalFormat = eInternalFormat;

      Gpu.CheckGpuErrorsRt();

      //this is for mipmaps or shadows .. array textures
      //      GL.TexStorage2D(TextureTarget2d.Texture2D, _numMipmaps, this.SizedInternalFormat, (int)Width, (int)Height

      if (nSamples > 0)
      {
        TextureTarget = TextureTarget.Texture2DMultisample;
        GL.BindTexture(TextureTarget, this._glId);
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
        GL.BindTexture(TextureTarget, this._glId);
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

      SetObjectLabel();
    }
    public Texture2D(string owner, int w, int h, int nMsaaSamples) : base(owner + "_DepthTexture" + (nMsaaSamples > 0 ? "_Multisample" : ""))
    {
      //createdepthtexture
      //Creates a depth texture, or multisample depth texture on texture channel 0
      //This will query the device to make sure the depth format is supported.
      Gpu.CheckGpuErrorsRt();
      //TextureTarget texTarget;

      GL.ActiveTexture(TextureUnit.Texture0);

      PixelFormat = PixelFormat.DepthComponent;
      PixelType = PixelType.Float;

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
      this._glId = GL.GenTexture();
      Gpu.CheckGpuErrorsRt();
      GL.BindTexture(this.TextureTarget, this._glId);

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

      GL.BindTexture(this.TextureTarget, 0);
      Gpu.CheckGpuErrorsRt();

      SetObjectLabel();
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsTexture(GetGlId()))
      {
        GL.DeleteTexture(GetGlId());
      }
    }
    public static Texture2D Default1x1ColorPixel_RGBA32ub(vec4ub color)
    {
      var t = _default1x1ColorPixel_RGBA32ub.Get();
      if (t == null)
      {
        t = new Texture2D("default1x1-color", Img32.Default1x1_RGBA32ub(Byte.MaxValue, Byte.MaxValue, Byte.MaxValue, Byte.MaxValue), false, TexFilter.Nearest);
        _default1x1ColorPixel_RGBA32ub.Set(t);
      }
      return t;
    }
    public static Texture2D Default1x1NormalPixel_RGBA32ub()
    {
      var t = _default1x1NormalPixel_RGBA32ub.Get();
      if (t == null)
      {
        if (NormalMapFormat == NormalMapFormat.Yup)
        {
          t = new Texture2D("default1x1-normal", Img32.Default1x1_RGBA32ub(0, Byte.MaxValue, 0, Byte.MaxValue), false, TexFilter.Nearest);
        }
        else if (NormalMapFormat == NormalMapFormat.Zup)
        {
          t = new Texture2D("default1x1-normal", Img32.Default1x1_RGBA32ub(0, 0, Byte.MaxValue, Byte.MaxValue), false, TexFilter.Nearest);
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
        _default1x1NormalPixel_RGBA32ub.Set(t);
      }
      return t;
    }
    public void SetWrap(TextureWrapMode wrap)
    {
      Gu.Assert(GL.IsTexture(this.GetGlId()));
      WrapMode = wrap;
      PushState(TextureTarget);
      Bind(Gpu.GetActiveTexture());
      GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapS, (int)WrapMode);
      GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapT, (int)WrapMode);
      PopState(TextureTarget);
    }
    public void SetFilter(TextureMinFilter min, TextureMagFilter mag)
    {
      Gu.Assert(GL.IsTexture(this.GetGlId()));
      Filter = TexFilter.Separate;
      PushState(TextureTarget);
      Bind(Gpu.GetActiveTexture());
      GL.TexParameter(TextureTarget, TextureParameterName.TextureMinFilter, (int)min);//LinearMipmapLinear
      GL.TexParameter(TextureTarget, TextureParameterName.TextureMagFilter, (int)mag);
      PopState(TextureTarget);
    }
    public void SetFilter(TexFilter filter)
    {
      Filter = filter;

      TextureMinFilter min = TextureMinFilter.Nearest;
      TextureMagFilter mag = TextureMagFilter.Nearest;
      if (filter == TexFilter.Linear)
      {
        min = TextureMinFilter.Linear;
        mag = TextureMagFilter.Linear;
      }
      else if (filter == TexFilter.Nearest)
      {
        if (IsMipmappingEnabled())
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
        if (IsMipmappingEnabled())
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
        if (IsMipmappingEnabled())
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
      SetFilter(min, mag);
    }
    public void Bind(TextureUnit unit)
    {
      if (GetGlId() == 0)
      {
        throw new System.Exception("Texture ID was 0 when binding texture.");
      }

      _boundUnit = unit;
      Gpu.CheckGpuErrorsDbg();
      GL.ActiveTexture(unit);
      Gpu.CheckGpuErrorsDbg();
      GL.BindTexture(TextureTarget, GetGlId());
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
    private void LoadToGpu(Img32 bmp, bool mipmaps, TexFilter filter, TextureWrapMode wrap)
    {
      Width = bmp.Width;
      Height = bmp.Height;
      Filter = filter;
      WrapMode = WrapMode;
      PixelFormat = PixelFormat.Rgba;
      PixelType = PixelType.UnsignedByte;
      PixelInternalFormat = PixelInternalFormat.Rgba;
      TextureTarget = TextureTarget.Texture2D;

      int ts = Gu.Context.Gpu.GetMaxTextureSize();
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

      _glId = GL.GenTexture();
      GL.BindTexture(TextureTarget, GetGlId());
      SetFilter(filter);
      SetWrap(wrap);

      //This calls glteximage2d for every mip level, or array texture (shadow array)
      //Allocates storage (buffers) on the GPU for all levels
      GL.TexStorage2D(TextureTarget2d.Texture2D, _numMipmaps, GetSizedInternalFormat(), (int)Width, (int)Height);

      //var raw = Gpu.SerializeGPUData(bmp.Data);
      var raw = Gpu.GetGpuDataPtr(bmp.Data);

      GL.TexSubImage2D(TextureTarget,
          0, //mipmap level
          0, 0, //x.y
          this.Width,
          this.Height,
          this.PixelFormat,
          this.PixelType,
          raw.Lock());
      raw.Unlock();

      if (mipmaps)
      {
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
      }

      SetObjectLabel();

      GL.BindTexture(TextureTarget, 0);
    }

    #endregion
  }


}
