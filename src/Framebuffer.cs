using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace PirateCraft
{
  public enum RenderTargetType
  {
    Color,
    Depth,
    Alpha,
    Bloom,
    Pick,
    Shadow,
    Position
  }
  public enum FramebufferState
  {
    Not_Initialized,
    Initialized
  }
  public abstract class RenderTarget : OpenGLResource
  {
    public RenderTarget() { }

    public int _iWidth = 0;
    public int _iHeight = 0;
    public int getWidth() { return _iWidth; }
    public int getHeight() { return _iHeight; }
  }
  public class BufferRenderTarget : RenderTarget
  {
    public string _strName;
    public int _iGlTexId;    // Texture Id
    public TextureTarget _eTextureTarget; //GL_TEXTURE_2D, or other
    public FramebufferAttachment _eAttachment;//GL_COLORATTACHMENT_0 + n
    public int _iLayoutIndex;// The (layout = 0).. in the shader
    public TextureUnit _eTextureChannel;//GL_TEXTURE0 +..
    public ClearBufferMask _eBlitBit; // GL_COLOR_BUFFER_BIT or GL_DEPTH_BUFFER_BIT
    public RenderTargetType _eTargetType;
    public bool _bShared = false;

    //  virtual int getWidth() override;
    //virtual int getHeight() override;

    public BufferRenderTarget(string name, bool bShared)
    {
      _bShared = bShared;
      _strName = name;
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      if (GL.IsTexture(_iGlTexId))
      {
        GL.DeleteTexture(_iGlTexId);
      }
    }

    public bool getShared() { return _bShared; }
    public int getGlTexId() { return _iGlTexId; }
    public string getName() { return _strName; }
    public TextureUnit getTextureChannel() { return _eTextureChannel; }
    public FramebufferAttachment getAttachment() { return _eAttachment; }
    public TextureTarget getTextureTarget() { return _eTextureTarget; }
    public int getTexId() { return _iGlTexId; }
    public int getLayoutIndex() { return _iLayoutIndex; }
    public RenderTargetType getTargetType() { return _eTargetType; }
    public ClearBufferMask getBlitBit() { return _eBlitBit; }

    public bool getMsaaEnabled()
    {
      if (_eTextureTarget == TextureTarget.Texture2DMultisample)//GL_TEXTURE_2D_MULTISAMPLE
      {
        return true;
      }
      else if (_eTextureTarget == TextureTarget.Texture2D)//GL_TEXTURE_2D
      {
        return false;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return false;
    }

    public void bind(FramebufferAttachment eAttachment = (int)0)
    {
      //Gu.DebugBreak();
      //ju7st say if eAttachment == depthattachment then FramebufferTarget = Framebuffer, or DrawFramebuffer

      if (_eTargetType == RenderTargetType.Depth)
      {
        if (getMsaaEnabled())
        {
          GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2DMultisample, _iGlTexId, 0);
        }
        else
        {
          GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _iGlTexId, 0);
        }
      }
      else
      {
        if ((int)eAttachment == 0)
        {
          eAttachment = _eAttachment;
        }
        if (getMsaaEnabled())
        {
          GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, eAttachment, TextureTarget.Texture2DMultisample, _iGlTexId, 0);
        }
        else
        {
          GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, eAttachment, TextureTarget.Texture2D, _iGlTexId, 0);
        }
      }
      Gpu.CheckGpuErrorsDbg();
    }
  }
  public abstract class FramebufferBase : OpenGLResource
  {
    protected bool _bMsaaEnabled = false;
    protected int _nMsaaSamples = 0;
    protected int _uiGlFramebufferId = 0;
    protected List<BufferRenderTarget> _vecTargets = new List<BufferRenderTarget>();  //The order in this array is important.
    protected Dictionary<string, BufferRenderTarget> _mapTargets = new Dictionary<string, BufferRenderTarget>();
    protected FramebufferState _eState = FramebufferState.Not_Initialized;
    protected vec4 _vClear;
    protected string _label = "";
    private const int c_iMaxAttachments = 64;

    public FramebufferBase(string label, bool bMsaa, int nMsaa, vec4 vClear)
    {
      _vClear = vClear;
      _bMsaaEnabled = bMsaa;
      _nMsaaSamples = nMsaa;
      _label = label;
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      deleteTargets();
    }

    public abstract void init(int iWidth, int iHeight, BufferRenderTarget sharedDepth, BufferRenderTarget sharedPick);
    //virtual void resizeScreenBuffers(int iWidth, int iHeight, std::shared_ptr<RenderTarget> pShared);
    public abstract void beginRender();
    public abstract void endRender();

    public int getGlId() { return _uiGlFramebufferId; }

    public List<BufferRenderTarget> getTargets() { return _vecTargets; }
    public int getFramebufferId() { return _uiGlFramebufferId; }

    public BufferRenderTarget getTargetByName(string name)
    {
      _mapTargets.TryGetValue(name, out var target);
      return target;
    }
    public void setClear(vec4 v) { _vClear = v; }
    public vec4 getClear() { return _vClear; }

    public string getLabel() { return _label; }

    //private TextureTarget _eTextureTarget; //GL_TEXTURE_2D, or other
    //private FramebufferAttachment _eAttachment;//GL_COLORATTACHMENT_0 + n
    //private int _iLayoutIndex;// The (layout = 0).. in the shader
    //private TextureUnit _eTextureChannel;//GL_TEXTURE0 +..
    //private ClearBufferMask _eBlitBit; // GL_COLOR_BUFFER_BIT or GL_DEPTH_BUFFER_BIT
    //private RenderTargetType _eTargetType;
    //private bool _bShared = false;

    public static BufferRenderTarget createTarget(string strName, PixelInternalFormat internalFormat, PixelFormat texFormat,
                                                                    PixelType dataType, int w, int h, RenderTargetType eTargetType, int iIndex, bool bMsaaEnabled, int nMsaaSamples)
    {

      BufferRenderTarget inf = new BufferRenderTarget(strName, false);
      inf._iLayoutIndex = iIndex;
      inf._eTextureTarget = TextureTarget.Texture2D;
      inf._eAttachment = FramebufferAttachment.ColorAttachment0 + iIndex;
      inf._eTextureChannel = TextureUnit.Texture0 + iIndex;
      inf._eBlitBit = ClearBufferMask.ColorBufferBit;
      inf._eTargetType = eTargetType;
      inf._iWidth = w;
      inf._iHeight = h;

      int maxAttach = 0;
      GL.GetInteger(GetPName.MaxColorAttachments, out maxAttach);

      if (iIndex > maxAttach)
      {
        Gu.BRThrowException("GPU Does not support enough color attachments, wanted: " + iIndex + " max supported: " + maxAttach);
      }

      makeRenderTexture(ref inf._iGlTexId, internalFormat, texFormat, dataType, w, h,
                        ref inf._eTextureTarget, bMsaaEnabled, nMsaaSamples);
      GL.ObjectLabel(ObjectLabelIdentifier.Texture, inf._iGlTexId, inf._strName.Length, inf._strName);

      return inf;
    }
    public static BufferRenderTarget createDepthTarget(string strName, int w, int h, int iIndex, bool bMsaaEnabled, int nMsaaSamples)
    {
      BufferRenderTarget inf = new BufferRenderTarget(strName, true);
      //**Note: index doesn't matter for depth target since we simply bind it to GL_Depth_attachment.
      inf._iLayoutIndex = iIndex;
      if (bMsaaEnabled)
      {
        //query max depth samples
        //TODO: GL_MAX_DEPTH_TEXTURE_SAMPLES

        inf._eTextureTarget = TextureTarget.Texture2DMultisample;
      }
      else
      {
        inf._eTextureTarget = TextureTarget.Texture2D;
      }
      inf._eAttachment = FramebufferAttachment.DepthAttachment;  //GL_COLOR_ATTACHMENT0 + iLayoutIndex;
      inf._eTextureChannel = TextureUnit.Texture0 + iIndex;
      inf._eBlitBit = ClearBufferMask.DepthBufferBit;
      inf._eTargetType = RenderTargetType.Depth;
      inf._iWidth = w;
      inf._iHeight = h;

      //This will cycle through depth formats and choose the most precise.
      createDepthTexture(strName, ref inf._iGlTexId, w, h, bMsaaEnabled, nMsaaSamples, PixelInternalFormat.DepthComponent32f);
      GL.ObjectLabel(ObjectLabelIdentifier.Texture, inf._iGlTexId, inf._strName.Length, inf._strName);

      return inf;
    }
    private static void createDepthTexture(string owner, ref int texId, int w, int h, bool bMsaaEnabled, int nMsaaSamples, PixelInternalFormat eRequestedDepth)
    {
      //Creates a depth texture, or multisample depth texture on texture channel 0
      //This will query the device to make sure the depth format is supported.
      Gpu.CheckGpuErrorsRt();
      TextureTarget texTarget;

      string label = owner + "_DepthTexture";

      if (bMsaaEnabled)
      {
        texTarget = TextureTarget.Texture2DMultisample;
        label += "_Multisample";
      }
      else
      {
        texTarget = TextureTarget.Texture2D;
      }
      GL.ActiveTexture(TextureUnit.Texture0);
      texId = GL.GenTexture();
      Gpu.CheckGpuErrorsRt();

      GL.BindTexture(texTarget, texId);
      //THe following parameters are for depth textures only
      Gpu.CheckGpuErrorsRt();

      if (bMsaaEnabled == false)
      {
        //For some reason you can't use this with multisample.

        //**NOTE: we changed this from GL.TexparameterI
        GL.TexParameter(texTarget, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);  //GL_NONE
        Gpu.CheckGpuErrorsRt();

        //OpenTK.Graphics.OpenGL.TextureCompareMode
        // GL.TexParameter(texTarget, TextureParameterName.TextureCompareFunc,  OpenTK.Graphics.OpenGL4.depthtexture  GL_LEQUAL);
        // Gpu.CheckGpuErrorsRt();
        Gu.Log.Warn("Commented out Texturecomparefunc");

        GL.TexParameter(texTarget, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        Gpu.CheckGpuErrorsRt();

        GL.TexParameter(texTarget, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        Gpu.CheckGpuErrorsRt();

        GL.TexParameter(texTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        Gpu.CheckGpuErrorsRt();

        GL.TexParameter(texTarget, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        Gpu.CheckGpuErrorsRt();

      }

      GL.ObjectLabel(ObjectLabelIdentifier.Texture, texId, label.Length, label);

      //Loop over creating a texture until we get no error
      getCompatibleDepthComponent(32, (eDepth) =>
      {
        if (bMsaaEnabled)
        {
          //texTarget = TextureTargetMultisample.Texture2D;
          //..ok .. it's the same Enum 
          GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, nMsaaSamples, eDepth, w, h, true);
          Gpu.CheckGpuErrorsRt();

        }
        else
        {
          GL.TexImage2D(texTarget, 0, eDepth, w, h, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
          Gpu.CheckGpuErrorsRt();
        }
      });

      Gpu.CheckGpuErrorsRt();

      GL.BindTexture(texTarget, 0);
      Gpu.CheckGpuErrorsRt();

    }
    private static void getCompatibleDepthComponent(int max_bits, Action<PixelInternalFormat> func)
    {
      //Try a bunch of depth formats.
      //max_bits isn't used
      //we don't use stencil here. Use as much as we can get.
      func(PixelInternalFormat.DepthComponent32f);
      if (GL.GetError() == ErrorCode.NoError)
      {
        return;
      }
      func(PixelInternalFormat.DepthComponent32);
      if (GL.GetError() == ErrorCode.NoError)
      {
        return;
      }
      func(PixelInternalFormat.DepthComponent24);//The O.G.
      if (GL.GetError() == ErrorCode.NoError)
      {
        return;
      }
      func(PixelInternalFormat.DepthComponent16);
      if (GL.GetError() == ErrorCode.NoError)
      {
        Gu.Log.Warn("Selected 16 bit depth buffer");
        return;
      }
      Gu.BRThrowException("Could not find suitable depth buffer pixelformat.");
    }
    protected void attachAllTargets()
    {
      foreach (var inf in _vecTargets)
      {
        inf.bind();
        //GL.FramebufferTexture2D(GL_DRAW_FRAMEBUFFER, inf->getAttachment(), inf->getTextureTarget(), inf->getTexId(), 0);
        //Gpu.CheckGpuErrorsRt();
      }
    }
    protected void setDrawAllTargets()
    {
      DrawBuffersEnum[] attachments = new DrawBuffersEnum[c_iMaxAttachments];
      int iCount = 0;
      for (int i = 0; i < c_iMaxAttachments; ++i)
      {
        if (i < (int)_vecTargets.Count)
        {
          if (_vecTargets[i].getTargetType() == RenderTargetType.Color ||
              _vecTargets[i].getTargetType() == RenderTargetType.Pick ||
              _vecTargets[i].getTargetType() == RenderTargetType.Shadow)
          {
            //**Warning - OpenTK bifurcated this OpenGL enum into two enums. The basic codes are the same .. this could result in an error
            attachments[i] = (DrawBuffersEnum)_vecTargets[i].getAttachment();
            iCount++;
          }
        }
        else
        {
          attachments[i] = 0;
        }
      }
      GL.DrawBuffers(iCount, attachments);
      Gpu.CheckGpuErrorsDbg();
    }
    protected void checkFramebufferComplete()
    {
      Gpu.CheckGpuErrorsRt();

      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _uiGlFramebufferId);
      Gpu.CheckGpuErrorsRt();

      attachAllTargets();
      setDrawAllTargets();

      FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
      Gpu.CheckGpuErrorsRt();


      if (status != FramebufferErrorCode.FramebufferComplete)
      {
        if (status == FramebufferErrorCode.FramebufferIncompleteMultisample)
        {
          Gu.Log.Error("Framebuffer is not complete.  Multisampling error.  Make sure that you enable " +
                     "multisampling on ALL textures, additionally make sure all textures have the same setting for FIXED_SAMPLE_LOCATIONS");
        }
        Gpu.CheckGpuErrorsRt();

        Gu.BRThrowException("Failed to create framebuffer.");
      }
    }
    protected void addTarget(string strName, PixelInternalFormat internalFormat, PixelFormat texFormat,
                                    PixelType dataType, int w, int h, RenderTargetType eTargetType)
    {
      int iIndex = (int)_vecTargets.Count;

      BufferRenderTarget inf = createTarget(strName, internalFormat, texFormat, dataType, w, h,
                                                             eTargetType, iIndex, _bMsaaEnabled, _nMsaaSamples);
      _vecTargets.Add(inf);
      _mapTargets.Add(strName, inf);
    }

    protected void addTarget(BufferRenderTarget other)
    {
      int iIndex = (int)_vecTargets.Count;

      BufferRenderTarget inf = new BufferRenderTarget(other._strName, true);
      inf._iLayoutIndex = iIndex;
      inf._eTextureTarget = other._eTextureTarget;
      inf._eAttachment = FramebufferAttachment.ColorAttachment0 + iIndex;
      inf._eTextureChannel = TextureUnit.Texture0 + iIndex;
      inf._eBlitBit = ClearBufferMask.ColorBufferBit;
      inf._eTargetType = other._eTargetType;
      inf._iGlTexId = other._iGlTexId;
      inf._iWidth = other._iWidth;
      inf._iHeight = other._iHeight;

      _vecTargets.Add(inf);
      _mapTargets.Add(other._strName, inf);
    }
    protected void deleteTargets()
    {
      
      GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

      if (_uiGlFramebufferId > 0)
      {
        GL.DeleteFramebuffer(_uiGlFramebufferId);
      }
      
      for (int i = 0; i < _vecTargets.Count; ++i)
      {
        _vecTargets[i] = null;
      }
      _vecTargets.Clear();
      _mapTargets.Clear();
    }

    protected static void makeRenderTexture(ref int iTexId, PixelInternalFormat eInternalFormat, PixelFormat eTextureFormat, PixelType eDataType, int iWidth, int iHeight,
                                            ref TextureTarget eOutTarget, bool bMultisample, int nSamples)
    {
      iTexId = GL.GenTexture();
      Gpu.CheckGpuErrorsRt();


      if (bMultisample)
      {
        GL.BindTexture(TextureTarget.Texture2DMultisample, iTexId);
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


        eOutTarget = TextureTarget.Texture2DMultisample;
      }
      else
      {
        GL.BindTexture(TextureTarget.Texture2D, iTexId);
        Gpu.CheckGpuErrorsRt();

        //if (Gu::GetEngineDisplayParams()->getEnableAnisotropicFiltering())
        //{
        //   GL.TexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAX_ANISOTROPY_EXT, Gu::GetEngineDisplayParams()->getTextureAnisotropyLevel());
        //    Gu::getGraphicsContext()->chkErrRt();
        //}
        GL.TexImage2D(TextureTarget.Texture2D, 0, eInternalFormat, iWidth, iHeight, 0, eTextureFormat, eDataType, IntPtr.Zero);
        Gpu.CheckGpuErrorsRt();

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        Gpu.CheckGpuErrorsRt();

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        Gpu.CheckGpuErrorsRt();


        eOutTarget = TextureTarget.Texture2D;
      }
      GL.Disable(EnableCap.Dither);  //Dithering gets enabled for some reason

      Gpu.CheckGpuErrorsRt();
    }

  }//Framebufferbase
  public class DeferredFramebuffer : FramebufferBase
  {
    const string c_strPositionMRT_DF = "Position MRT (Deferred)";
    const string c_strColorMRT_DF = "Color MRT (Deferred)";
    const string c_strNormalMRT_DF = "Normal MRT (Deferred)";
    const string c_strPlaneMRT_DF = "Plane_Or_spec MRT (Deferred)";

    private bool _bMultisample;
    private int _nMsaaSamples;
    private bool _bUf0, _bUf1;  //For the offscreen stuff (later)
    //private std::shared_ptr<VaoDataGeneric> _pBloomVaoPass;

    public DeferredFramebuffer(bool bMultisample, int nSamples, vec4 vClear) : base("Deferred_FBO", bMultisample, nSamples, vClear)
    {
      _bMultisample = bMultisample;
      _nMsaaSamples = nSamples;

      //  _pBloomVaoPass = NULL;
    }
    //virtual ~DeferredFramebuffer() override;

    public override void init(int w, int h, BufferRenderTarget pSharedDepthTarget, BufferRenderTarget sharedPick)
    {
      deleteTargets();

      bool _bUseRenderBuffer = false;

      //TODO: later we'll create this async.
      //Gd::verifyRenderThread();
      _uiGlFramebufferId = GL.GenFramebuffer();
      Gpu.CheckGpuErrorsRt();

      GL.BindFramebuffer(FramebufferTarget.Framebuffer, _uiGlFramebufferId);
      Gpu.CheckGpuErrorsRt();

      GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultWidth, w);
      Gpu.CheckGpuErrorsRt();
      GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultHeight, h);
      Gpu.CheckGpuErrorsRt();
      GL.ActiveTexture(TextureUnit.Texture0);
      Gpu.CheckGpuErrorsRt();

      // - Textures
      //Don't change the names here, we reference them elsewhere *yikes*
      addTarget(c_strPositionMRT_DF, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Color);//0GL_RGBA32F GL_RGBA GL_FLOAT
      addTarget(c_strColorMRT_DF, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Color);//1
      addTarget(c_strNormalMRT_DF, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Color);//2
      addTarget(c_strPlaneMRT_DF, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Color);//3
      addTarget(sharedPick);//4
                            //  sharedPick->bind(GL_COLOR_ATTACHMENT4);

      //Depth Buffer
      pSharedDepthTarget.bind();
      // attachDepthTarget(pSharedDepthTarget);

      checkFramebufferComplete();

      GL.ObjectLabel(ObjectLabelIdentifier.Framebuffer, _uiGlFramebufferId, getLabel().Length, getLabel());



      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
      Gpu.CheckGpuErrorsRt();

      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      Gpu.CheckGpuErrorsRt();

      _eState = FramebufferState.Initialized;
    }
    BufferRenderTarget getTarget(RenderTargetType eType)
    {
      foreach (var inf in _vecTargets)
      {
        if (inf.getTargetType() == eType)
        {
          return inf;
        }
      }
      return null;
    }
    public override void beginRender()
    {
      if (_eState != FramebufferState.Initialized)
      {
        Gu.BRThrowException("Framebuffer was not initialized.");
      }

      //GLenum attachments[32];
      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _uiGlFramebufferId);
      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      setDrawAllTargets();

      //NOTE:
      //CRITICAL that clear color is zero here.
      // Otherwise the color somehow shows up in random places getting
      // blended with other colors..
      GL.ClearColor(getClear().x, getClear().y, getClear().z, getClear().w);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      Gpu.CheckGpuErrorsDbg();

      // Perf::pushPerf();

      //getContext()->pushDepthTest();
      //getContext()->pushCullFace();
      //getContext()->pushBlend();
      //getContext()->enableBlend(false);
      //getContext()->enableCullFace(true);
      //getContext()->enableDepthTest(true);
    }
    public override void endRender()
    {
      //getContext()->popBlend();
      //getContext()->popCullFace();
      //getContext()->popDepthTest();

      //  Perf::popPerf();

      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
    }
    public int getNumNonDepthTargets()
    {
      int ret = 0;
      foreach (var inf in _vecTargets)
      {
        if (inf.getTargetType() != RenderTargetType.Depth)
        {
          ret++;
        }
      }
      return ret;
    }
    private int getNumTargets()
    {
      return (int)_vecTargets.Count;
      //uh..?
      //int ret = 0;
      //for (std::shared_ptr<RenderTarget> inf : _vecTargets) {
      //    ret++;
      //}
      //return ret;
    }
    private bool getIsBloomEnabled()
    {
      return false;
    }
  };
  public class ForwardFramebuffer : FramebufferBase
  {
    public const string c_strColorMRT_FW = "Color MRT (Forward)";

    public ForwardFramebuffer(bool bMsaa, int nMsaa, vec4 vClear) : base("Forward_Framebuffer", bMsaa, nMsaa, vClear)
    {
    }
    public override void init(int iWidth, int iHeight, BufferRenderTarget sharedDepth, BufferRenderTarget sharedPick)
    {
      deleteTargets();

      GL.UseProgram(0);
      GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
      Gpu.CheckGpuErrorsRt();

      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      Gpu.CheckGpuErrorsRt();


      _uiGlFramebufferId = GL.GenFramebuffer();
      GL.BindFramebuffer(FramebufferTarget.Framebuffer, _uiGlFramebufferId);
      GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultWidth, iWidth);
      GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultHeight, iHeight);
      Gpu.CheckGpuErrorsRt();

      attachColorTargets(iWidth, iHeight);
      addTarget(sharedPick);
      // sharedPick->bind(GL_COLOR_ATTACHMENT1);
      sharedDepth.bind();

      checkFramebufferComplete();

      GL.ObjectLabel(ObjectLabelIdentifier.Framebuffer, _uiGlFramebufferId, getLabel().Length, getLabel());

      //Return to default.
      GL.UseProgram(0);

      GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
      Gpu.CheckGpuErrorsRt();
      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);//. The value zero is reserved, but there is no default renderbuffer object. Instead, renderbuffer set to zero effectively unbinds any renderbuffer object previously bound. 
      Gpu.CheckGpuErrorsRt();


      _eState = FramebufferState.Initialized;
    }
    public override void beginRender()
    {
      if (_eState != FramebufferState.Initialized)
      {
        Gu.BRThrowException("Framebuffer was not initialized.");
      }

      //Clear all buffers
      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _uiGlFramebufferId);
      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);//_depthRenderBufferId);

      //Do not clear! - previous deferred operation is in here. (clear happens in clearFb)
      //**Do not clear***
      //**Do not clear***
      //**Do not clear***
      //**Do not clear***
      //**Do not clear***
      //**Do not clear***

    }
    public override void endRender()
    {
      //noting
    }
    public void attachColorTargets(int iWidth, int iHeight)
    {
      //VV < don't change "Color" Name
      addTarget(c_strColorMRT_FW, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, iWidth, iHeight, RenderTargetType.Color);
    }
    public int getGlColorBufferTexId()
    {
      Gu.Assert(_vecTargets.Count > 0);
      return _vecTargets[0].getGlTexId();
    }
    public void clearFb()
    {
      //Call this before we begin the defrred
      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _uiGlFramebufferId);
      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);//_depthRenderBufferId);
      setDrawAllTargets();

      GL.ClearColor(getClear().x, getClear().y, getClear().z, getClear().w);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
    }
  };

}//NS piratecarft
