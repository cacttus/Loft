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
    Bloom,
    Pick,
    Shadow,
    Position,
    Normal,
    Mat0,  //Pending target types..not sure how this will go
    Mat1,
    Mat2,
  }
  public enum FramebufferState
  {
    Not_Initialized,
    Initialized
  }
  public class FramebufferAttachment : HasGpuResources
  {
    public string _strName;
    public Texture2D _texture { get; set; } = null;
    public OpenTK.Graphics.OpenGL4.FramebufferAttachment _eAttachment;//GL_COLORATTACHMENT_0 + n
    public int _iLayoutIndex;// The (layout = 0).. in the shader
    public TextureUnit _eTextureChannel;//GL_TEXTURE0 +..
    public ClearBufferMask _eBlitBit; // GL_COLOR_BUFFER_BIT or GL_DEPTH_BUFFER_BIT
    public RenderTargetType _eTargetType;
    public bool _bShared = false;
    public BlendingFactorSrc _srcRGB = BlendingFactorSrc.One;
    public BlendingFactorDest _dstRGB = BlendingFactorDest.Zero;
    public BlendingFactorSrc _srcAlpha = BlendingFactorSrc.One; //one/zero = disable blend
    public BlendingFactorDest _dstAlpha = BlendingFactorDest.Zero;

    public int _iWidth = 0;
    public int _iHeight = 0;
    public int getWidth() { return _iWidth; }
    public int getHeight() { return _iHeight; }

    public bool getShared() { return _bShared; }
    public string getName() { return _strName; }
    public OpenTK.Graphics.OpenGL4.FramebufferAttachment getAttachment() { return _eAttachment; }
    public RenderTargetType getTargetType() { return _eTargetType; }
    public ClearBufferMask getBlitBit() { return _eBlitBit; }
    public bool getMsaaEnabled()
    {
      if (_texture.TextureTarget == TextureTarget.Texture2DMultisample)//GL_TEXTURE_2D_MULTISAMPLE
      {
        return true;
      }
      else if (_texture.TextureTarget == TextureTarget.Texture2D)//GL_TEXTURE_2D
      {
        return false;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return false;
    }

    public FramebufferAttachment(string name, bool bShared)
    {
      _bShared = bShared;
      _strName = name;
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      _texture.Dispose();
      _texture = null;
    }
    public void Attach(OpenTK.Graphics.OpenGL4.FramebufferAttachment eAttachment = (int)0)
    {
      //Gu.DebugBreak();
      //ju7st say if eAttachment == depthattachment then FramebufferTarget = Framebuffer, or DrawFramebuffer

      if (_eTargetType == RenderTargetType.Depth)
      {
        if (getMsaaEnabled())
        {
          GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment, TextureTarget.Texture2DMultisample, _texture.GetGlId(), 0);
        }
        else
        {
          GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _texture.GetGlId(), 0);
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
          GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, eAttachment, TextureTarget.Texture2DMultisample, _texture.GetGlId(), 0);
        }
        else
        {
          GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, eAttachment, TextureTarget.Texture2D, _texture.GetGlId(), 0);
        }
      }
      Gpu.CheckGpuErrorsDbg();
    }

  }
  public abstract class FramebufferBase : OpenGLResource
  {
    public const string c_strPickMRT_DF = "PickMRT-shared";
    public const string c_strBlittedDepthMRT_DF = "DepthMRT-shared";
    public const string c_strBlittedDepthMRT_DF_MSAA = "DepthMRT-shared-MSAA";
    public const int c_iMaxAttachments = 64;

    public List<FramebufferAttachment> Targets { get; private set; } = new List<FramebufferAttachment>();
    public vec4 ClearColor { get; set; } = new vec4(0, 0, 0, 1);

    protected bool _bMsaaEnabled = false;
    protected int _nMsaaSamples = 0;
    protected Dictionary<string, FramebufferAttachment> _mapTargets = new Dictionary<string, FramebufferAttachment>();
    protected FramebufferState _eState = FramebufferState.Not_Initialized;

    public FramebufferBase(string label, bool bMsaa, int nMsaa, vec4 vClear) : base(label)
    {
      ClearColor = vClear;
      _bMsaaEnabled = bMsaa;
      _nMsaaSamples = nMsaa;
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      DeleteTargets();
    }

    public abstract void Init(int iWidth, int iHeight, FramebufferAttachment sharedDepth, FramebufferAttachment sharedPick);
    public abstract void BeginRender();
    public abstract void EndRender();
    public void EnableBlend(bool enable)
    {
      if (enable)
      {
        GL.Enable(EnableCap.Blend);

        //I imagine we could just call blendfuncseparate when we begin frame one time by getting the enable cap and setting it back ..testing for now
        try
        {
          foreach (var at in Targets)
          {
            GL.BlendFuncSeparate(at._iLayoutIndex, at._srcRGB, at._dstRGB, at._srcAlpha, at._dstAlpha);
            Gpu.CheckGpuErrorsDbg();
          }
        }
        catch (Exception ex)
        {
          Gu.Log.ErrorCycle("BlendFuncSeparate wasn't supported");
          GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
      }
      else
      {
        GL.Disable(EnableCap.Blend);
      }
    }
    public static ReadBufferMode AttachmentIndexToReadBufferMode(OpenTK.Graphics.OpenGL4.FramebufferAttachment attachmentIndex)
    {
      ReadBufferMode mode = ReadBufferMode.ColorAttachment0;
      if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment0) { mode = ReadBufferMode.ColorAttachment0; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment1) { mode = ReadBufferMode.ColorAttachment1; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment2) { mode = ReadBufferMode.ColorAttachment2; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment3) { mode = ReadBufferMode.ColorAttachment3; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment4) { mode = ReadBufferMode.ColorAttachment4; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment5) { mode = ReadBufferMode.ColorAttachment5; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment6) { mode = ReadBufferMode.ColorAttachment6; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment7) { mode = ReadBufferMode.ColorAttachment7; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment8) { mode = ReadBufferMode.ColorAttachment8; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment9) { mode = ReadBufferMode.ColorAttachment9; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment10) { mode = ReadBufferMode.ColorAttachment10; }
      else if (attachmentIndex == OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment11) { mode = ReadBufferMode.ColorAttachment11; }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return mode;
    }
    public static FramebufferAttachment CreateTarget(string strName, int w, int h, RenderTargetType eTargetType, int iIndex, bool bMsaaEnabled, int nMsaaSamples, PixelInternalFormat? internalFormat, PixelFormat? texFormat, PixelType? dataType)
    {
      //We could eventually roll depth texture / color texture creation into one thing 
      FramebufferAttachment inf = new FramebufferAttachment(strName, false);
      if (eTargetType == RenderTargetType.Depth)
      {
        inf._iLayoutIndex = -1; //layout index doens't matter for depth
        inf._eAttachment = OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment;
        inf._eBlitBit = ClearBufferMask.DepthBufferBit;
        inf._texture = new Texture2D(strName, w, h, bMsaaEnabled, nMsaaSamples);
      }
      else
      {
        inf._iLayoutIndex = iIndex;
        inf._eAttachment = OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment0 + iIndex;
        inf._eBlitBit = ClearBufferMask.ColorBufferBit;
        inf._texture = new Texture2D(inf._strName, internalFormat.Value, texFormat.Value, dataType.Value, w, h, bMsaaEnabled, nMsaaSamples);
      }

      inf._eTextureChannel = TextureUnit.Texture0 + iIndex;
      inf._eTargetType = eTargetType;
      inf._iWidth = w;
      inf._iHeight = h;

      if (eTargetType == RenderTargetType.Color)
      {
        //Set a basic blend func
        inf._srcRGB = BlendingFactorSrc.SrcAlpha;
        inf._dstRGB = BlendingFactorDest.OneMinusSrcAlpha;
        inf._srcAlpha = BlendingFactorSrc.SrcAlpha;
        inf._dstAlpha = BlendingFactorDest.OneMinusSrcAlpha;
      }
      else
      {
        inf._srcRGB = BlendingFactorSrc.One; 
        inf._dstRGB = BlendingFactorDest.Zero;
        inf._srcAlpha = BlendingFactorSrc.One; 
        inf._dstAlpha = BlendingFactorDest.Zero;
      }

      int maxAttach = 0;
      GL.GetInteger(GetPName.MaxColorAttachments, out maxAttach);

      if (iIndex > maxAttach)
      {
        Gu.BRThrowException("GPU Does not support enough color attachments, wanted at least: " + iIndex + " max supported: " + maxAttach);
      }

      return inf;
    }
    public FramebufferAttachment GetTargetByName(string name)
    {
      _mapTargets.TryGetValue(name, out var target);
      Gu.Assert(target != null);
      return target;
    }
        FramebufferAttachment GetTarget(RenderTargetType eType)
    {
      foreach (var inf in Targets)
      {
        if (inf.getTargetType() == eType)
        {
          return inf;
        }
      }
      return null;
    }
    public void Bind(FramebufferTarget target)
    {
      GL.BindFramebuffer(target, _glId);
      Gpu.CheckGpuErrorsDbg();
    }
    public void Unbind(FramebufferTarget target)
    {
      GL.BindFramebuffer(target, _glId);
      Gpu.CheckGpuErrorsDbg();
    }
    public void UnbindRenderbuffer()
    {
      //. The value zero is reserved, but there is no default renderbuffer object. Instead, renderbuffer set to zero effectively unbinds any renderbuffer object previously bound.
      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      Gpu.CheckGpuErrorsDbg();
    }
    protected void AttachAllTargets()
    {
      foreach (var inf in Targets)
      {
        inf.Attach();
      }
    }
    protected void SetDrawAllTargets()
    {
      DrawBuffersEnum[] attachments = new DrawBuffersEnum[c_iMaxAttachments];
      int iCount = 0;
      for (int i = 0; i < c_iMaxAttachments; ++i)
      {
        if (i < (int)Targets.Count)
        {
          if (Targets[i].getTargetType() == RenderTargetType.Color ||
              Targets[i].getTargetType() == RenderTargetType.Pick ||
              Targets[i].getTargetType() == RenderTargetType.Shadow)
          {
            //**Warning - OpenTK bifurcated this OpenGL enum into two enums. The basic codes are the same .. this could result in an error
            attachments[i] = (DrawBuffersEnum)Targets[i].getAttachment();
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
    protected void CheckFramebufferComplete()
    {
      Gpu.CheckGpuErrorsRt();

      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _glId);
      Gpu.CheckGpuErrorsRt();

      AttachAllTargets();
      SetDrawAllTargets();

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
    protected void AddTarget(string strName, PixelInternalFormat internalFormat, PixelFormat texFormat, PixelType dataType, int w, int h, RenderTargetType eTargetType)
    {
      int iIndex = (int)Targets.Count;

      FramebufferAttachment inf = CreateTarget(strName, w, h, eTargetType, iIndex, _bMsaaEnabled, _nMsaaSamples, internalFormat, texFormat, dataType);
      Targets.Add(inf);
      _mapTargets.Add(strName, inf);
    }
    protected void AddTarget(FramebufferAttachment other)
    {
      int iIndex = (int)Targets.Count;

      FramebufferAttachment inf = new FramebufferAttachment(other._strName, true);
      inf._iLayoutIndex = iIndex;
      inf._eAttachment = OpenTK.Graphics.OpenGL4.FramebufferAttachment.ColorAttachment0 + iIndex;
      inf._eTextureChannel = TextureUnit.Texture0 + iIndex;
      inf._eBlitBit = ClearBufferMask.ColorBufferBit;
      inf._eTargetType = other._eTargetType;
      inf._iWidth = other._iWidth;
      inf._iHeight = other._iHeight;
      inf._texture = other._texture;
      inf._srcAlpha = other._srcAlpha;
      inf._srcRGB = other._srcRGB;
      inf._dstAlpha = other._dstAlpha;
      inf._dstRGB = other._dstRGB;
      inf._bShared = other._bShared;

      Targets.Add(inf);
      _mapTargets.Add(other._strName, inf);
    }
    protected void DeleteTargets()
    {
      GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

      if (_glId > 0)
      {
        GL.DeleteFramebuffer(_glId);
      }

      for (int i = 0; i < Targets.Count; ++i)
      {
        Targets[i] = null;
      }
      Targets.Clear();
      _mapTargets.Clear();
    }
    public int GetNumNonDepthTargets()
    {
      int ret = 0;
      foreach (var inf in Targets)
      {
        if (inf.getTargetType() != RenderTargetType.Depth)
        {
          ret++;
        }
      }
      return ret;
    }
    private int GetNumTargets()
    {
      return (int)Targets.Count;
    }
    private bool GetIsBloomEnabled()
    {
      return false;
    }


  }//Framebufferbase
  public class DeferredFramebuffer : FramebufferBase
  {
    public const string c_strColorMRT_DF = "ColorMRT-deferred";
    public const string c_strNormalMRT_DF = "NormalMRT-deferred";
    public const string c_strPlaneMRT_DF = "PlaneMRT-deferred";
    public const string c_strPositionMRT_DF = "PositionMRT-deferred";

    public DeferredFramebuffer(bool bMultisample, int nSamples, vec4 vClear) : base("Deferred_FBO", bMultisample, nSamples, vClear)
    {
      //  _pBloomVaoPass = NULL;
    }
    //virtual ~DeferredFramebuffer() override;

    public override void Init(int w, int h, FramebufferAttachment sharedDepth, FramebufferAttachment sharedPick)
    {
      DeleteTargets();

      bool _bUseRenderBuffer = false;

      //TODO: later we'll create this async.
      //Gd::verifyRenderThread();
      _glId = GL.GenFramebuffer();
      Gpu.CheckGpuErrorsRt();

      Bind(FramebufferTarget.Framebuffer);

      GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultWidth, w);
      Gpu.CheckGpuErrorsRt();
      GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultHeight, h);
      Gpu.CheckGpuErrorsRt();
      GL.ActiveTexture(TextureUnit.Texture0);
      Gpu.CheckGpuErrorsRt();

      string msaa = this._bMsaaEnabled ? "-msaa" : "";
      // - Textures
      //Don't change the names here, we reference them elsewhere *yikes*
      //These must match the order in the shader
      AddTarget(c_strColorMRT_DF + msaa, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Color);//0
      AddTarget(sharedPick);//1
      AddTarget(c_strNormalMRT_DF + msaa, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Normal);//2
      AddTarget(c_strPositionMRT_DF + msaa, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Position);//3 GL_RGBA32F GL_RGBA GL_FLOAT
      AddTarget(c_strPlaneMRT_DF + msaa, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Mat0);//4
      sharedDepth.Attach();

      CheckFramebufferComplete();

      SetObjectLabel();

      Unbind(FramebufferTarget.DrawFramebuffer);
      UnbindRenderbuffer();

      _eState = FramebufferState.Initialized;
    }

    public override void BeginRender()
    {
      if (_eState != FramebufferState.Initialized)
      {
        Gu.BRThrowException("Framebuffer was not initialized.");
      }

      Bind(FramebufferTarget.DrawFramebuffer);
      UnbindRenderbuffer();
      SetDrawAllTargets();

      //NOTE:
      //CRITICAL that clear color is zero here.
      // Otherwise the color somehow shows up in random places getting
      // blended with other colors..
      GL.ClearColor(ClearColor.r, ClearColor.g, ClearColor.b, ClearColor.a);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      Gpu.CheckGpuErrorsDbg();
    }
    public override void EndRender()
    {
      Unbind(FramebufferTarget.DrawFramebuffer);
    }

  }
  public class ForwardFramebuffer : FramebufferBase
  {
    public const string c_strColorMRT_FW = "ColorMRT-forward";

    public ForwardFramebuffer(bool bMsaa, int nMsaa, vec4 vClear) : base("Forward_Framebuffer", bMsaa, nMsaa, vClear)
    {
    }
    public override void Init(int iWidth, int iHeight, FramebufferAttachment sharedDepth, FramebufferAttachment sharedPick)
    {
      DeleteTargets();

      GL.UseProgram(0);
      Unbind(FramebufferTarget.Framebuffer);
      UnbindRenderbuffer();

      _glId = GL.GenFramebuffer();
      GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glId);
      GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultWidth, iWidth);
      GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultHeight, iHeight);
      Gpu.CheckGpuErrorsRt();
      string msaa = this._bMsaaEnabled ? "-msaa" : "";

      AddTarget(c_strColorMRT_FW + msaa, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, iWidth, iHeight, RenderTargetType.Color);
      AddTarget(sharedPick);
      sharedDepth.Attach();

      CheckFramebufferComplete();

      SetObjectLabel();

      //Return to default.
      GL.UseProgram(0);

      Unbind(FramebufferTarget.Framebuffer);
      UnbindRenderbuffer();

      _eState = FramebufferState.Initialized;
    }
    public override void BeginRender()
    {
      if (_eState != FramebufferState.Initialized)
      {
        Gu.BRThrowException("Framebuffer was not initialized.");
      }

      //Clear all buffers
      Bind(FramebufferTarget.DrawFramebuffer);
      UnbindRenderbuffer();//_depthRenderBufferId);

      //Do not clear! - previous deferred operation is in here. (clear happens in clearFb)
      //**Do not clear***
      //**Do not clear***
      //**Do not clear***
      //**Do not clear***
      //**Do not clear***
      //**Do not clear***

    }
    public override void EndRender()
    {
      //noting
    }
    public void ClearSharedFb()
    {
      //Call this before we begin the defrred
      Bind(FramebufferTarget.DrawFramebuffer);
      UnbindRenderbuffer();
      SetDrawAllTargets();

      GL.ClearColor(ClearColor.r, ClearColor.g, ClearColor.b, ClearColor.a);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      Unbind(FramebufferTarget.DrawFramebuffer);
    }

  };

}//NS piratecarft
