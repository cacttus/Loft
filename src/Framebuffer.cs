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
    Pick,
  }
  public enum FramebufferState
  {
    Not_Initialized,
    Initialized
  }
  public class FramebufferAttachment : HasGpuResources
  {
    public Texture2D Texture { get; set; } = null;
    public RenderTargetType TargetType { get; set; } = RenderTargetType.Color;

    public bool IsMsaaEnabled
    {
      get
      {
        if (Texture.TextureTarget == TextureTarget.Texture2DMultisample)//GLTexture_2D_MULTISAMPLE
        {
          return true;
        }
        else if (Texture.TextureTarget == TextureTarget.Texture2D)//GLTexture_2D
        {
          return false;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
        return false;
      }
    }

    private FramebufferAttachment() { }
    public string OutputName { get; private set; } = Gu.UnsetName;
    public FramebufferAttachment(string outputName, RenderTargetType eTargetType, int w, int h, int nMsaaSamples)
    {
      OutputName = outputName;
      if (eTargetType == RenderTargetType.Depth)
      {
        // inf._iLayoutIndex = -1; //layout index doens't matter for depth
        // inf._eAttachment = OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment;

        //_eBlitBit = ClearBufferMask.DepthBufferBit;
        Texture = new Texture2D(outputName, w, h, nMsaaSamples);
      }
      else
      {
        PixelInternalFormat internalFormat;
        PixelFormat texFormat;
        PixelType dataType;
        if (eTargetType == RenderTargetType.Pick)
        {

          internalFormat = PixelInternalFormat.R32ui;
          texFormat = PixelFormat.RedInteger;
          dataType = PixelType.UnsignedInt;

        }
        else
        {

          internalFormat = PixelInternalFormat.Rgba32f;
          texFormat = PixelFormat.Rgba;
          dataType = PixelType.Float;
        }

        // _eBlitBit = ClearBufferMask.ColorBufferBit;
        Texture = new Texture2D(outputName, internalFormat, texFormat, dataType, w, h, nMsaaSamples);
      }
      TargetType = eTargetType;
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      Texture.Dispose();
      Texture = null;
    }
  }
  public class FramebufferBinding
  {
    public FramebufferAttachment Attachment = null;
    public int LayoutIndex;// The (layout = 0).. in the shader
    public GLenum BindingIndex;//GL_COLORATTACHMENT_0 + n
    public BlendingFactorSrc SrcRGB = BlendingFactorSrc.One;
    public BlendingFactorDest DstRGB = BlendingFactorDest.Zero;
    public BlendingFactorSrc SrcAlpha = BlendingFactorSrc.One; //one/zero = disable blend
    public BlendingFactorDest DstAlpha = BlendingFactorDest.Zero;
    public FramebufferBinding(FramebufferAttachment at, int index)
    {
      Attachment = at;
      LayoutIndex = index;
      if (at.TargetType == RenderTargetType.Color)
      {
        BindingIndex = GLenum.GL_COLOR_ATTACHMENT0 + index;
        SrcRGB = BlendingFactorSrc.SrcAlpha;
        DstRGB = BlendingFactorDest.OneMinusSrcAlpha;
        SrcAlpha = BlendingFactorSrc.SrcAlpha;
        DstAlpha = BlendingFactorDest.OneMinusSrcAlpha;
      }
      else if (at.TargetType == RenderTargetType.Pick)
      {
        BindingIndex = GLenum.GL_COLOR_ATTACHMENT0 + index;
        SrcRGB = BlendingFactorSrc.One;
        DstRGB = BlendingFactorDest.Zero;
        SrcAlpha = BlendingFactorSrc.One;
        DstAlpha = BlendingFactorDest.Zero;
      }
      else if (at.TargetType == RenderTargetType.Depth)
      {
        BindingIndex = GLenum.GL_DEPTH_ATTACHMENT;
        SrcRGB = BlendingFactorSrc.One;
        DstRGB = BlendingFactorDest.Zero;
        SrcAlpha = BlendingFactorSrc.One;
        DstAlpha = BlendingFactorDest.Zero;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    public void Attach()
    {
      //Gu.DebugBreak();
      //ju7st say if eAttachment == depthattachment then FramebufferTarget = Framebuffer, or DrawFramebuffer

      if (Attachment.TargetType == RenderTargetType.Depth)
      {
        if (Attachment.IsMsaaEnabled)
        {
          GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment, TextureTarget.Texture2DMultisample, Attachment.Texture.GetGlId(), 0);
        }
        else
        {
          GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, Attachment.Texture.GetGlId(), 0);
        }
      }
      else
      {

        if (Attachment.IsMsaaEnabled)
        {
          GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, (OpenTK.Graphics.OpenGL4.FramebufferAttachment)BindingIndex, TextureTarget.Texture2DMultisample, Attachment.Texture.GetGlId(), 0);
        }
        else
        {
          GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, (OpenTK.Graphics.OpenGL4.FramebufferAttachment)BindingIndex, TextureTarget.Texture2D, Attachment.Texture.GetGlId(), 0);
        }
      }
      Gpu.CheckGpuErrorsDbg();
    }
  }
  public class FramebufferGeneric : OpenGLResource
  {
    public const string c_strPickMRT_DF = "PickMRT-shared";
    public const string c_strBlittedDepthMRT_DF = "DepthMRT-shared";
    public const string c_strBlittedDepthMRT_DF_MSAA = "DepthMRT-shared-MSAA";
    public const int c_iMaxAttachments = 64;

    public List<FramebufferBinding> Bindings { get; private set; } = new List<FramebufferBinding>();
    public FramebufferState State { get; private set; } = FramebufferState.Not_Initialized;

    public FramebufferGeneric(string label, List<FramebufferAttachment> attachments) : base(label)
    {
      //If no output we're default, in that case, no FBO,use PipelineStage's default.
      Gu.Assert(attachments.Count > 0);

      DeleteTargets();

      _glId = GL.GenFramebuffer();
      Gpu.CheckGpuErrorsRt();

      Bind(FramebufferTarget.Framebuffer);

      int w = attachments[0].Texture.Width;
      int h = attachments[0].Texture.Height;
      GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultWidth, w);
      Gpu.CheckGpuErrorsRt();
      GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultHeight, h);
      Gpu.CheckGpuErrorsRt();
      GL.ActiveTexture(TextureUnit.Texture0);
      Gpu.CheckGpuErrorsRt();

      // string msaa = 

      SetObjectLabel();

      int maxAttach = 0;
      GL.GetInteger(GetPName.MaxColorAttachments, out maxAttach);

      if (attachments.Count > maxAttach)
      {
        Gu.BRThrowException("GPU Does not support enough color attachments, wanted at least: " + attachments.Count + " max supported: " + maxAttach);
      }

      for (int iat = 0; iat < attachments.Count; iat++)
      {
        Bindings.Add(new FramebufferBinding(attachments[iat], iat));
      }

      CheckFramebufferComplete();
    }

    public override void Dispose_OpenGL_RenderThread()
    {
      DeleteTargets();
    }
    public void EnableBlend(bool enable)
    {
      if (enable)
      {
        GL.Enable(EnableCap.Blend);

        //I imagine we could just call blendfuncseparate when we begin frame one time by getting the enable cap and setting it back ..testing for now
        try
        {
          foreach (var at in Bindings)
          {
            GL.BlendFuncSeparate(at.LayoutIndex, at.SrcRGB, at.DstRGB, at.SrcAlpha, at.DstAlpha);
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
    public FramebufferBinding GetBinding(RenderTargetType eType)
    {
      foreach (var inf in Bindings)
      {
        if (inf.Attachment.TargetType == eType)
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
      GL.BindFramebuffer(target, 0);
      Gpu.CheckGpuErrorsDbg();
    }
    public void BindReadBuffer(RenderTargetType target)
    {
      var pick = GetBinding(target);
      Gu.Assert(pick != null);
      var readbufferMode = FramebufferGeneric.AttachmentIndexToReadBufferMode((OpenTK.Graphics.OpenGL4.FramebufferAttachment)pick.BindingIndex);
      GL.ReadBuffer(readbufferMode);
      Gpu.CheckGpuErrorsDbg();
    }
    public void UnbindReadBuffer()
    {
      GL.ReadBuffer(ReadBufferMode.None);
      Gpu.CheckGpuErrorsDbg();
    }
    public static void UnbindRenderbuffer()
    {
      //. The value zero is reserved, but there is no default renderbuffer object. Instead, renderbuffer set to zero effectively unbinds any renderbuffer object previously bound.
      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      Gpu.CheckGpuErrorsDbg();
    }
    protected void AttachAllTargets()
    {
      foreach (var inf in Bindings)
      {
        inf.Attach();
      }
    }
    public void SetDrawAllTargets()
    {
      //Draw to all color buffers, depth is automatic
      DrawBuffersEnum[] attachments = new DrawBuffersEnum[c_iMaxAttachments];
      int iCount = 0;
      for (int i = 0; i < c_iMaxAttachments; ++i)
      {
        if (i < (int)Bindings.Count)
        {
          if (Bindings[i].Attachment.TargetType == RenderTargetType.Color ||
              Bindings[i].Attachment.TargetType == RenderTargetType.Pick
              )
          {
            //**Warning - OpenTK bifurcated this OpenGL enum into two enums. The basic codes are the same .. this could result in an error
            attachments[i] = (DrawBuffersEnum)Bindings[i].BindingIndex;
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
      Unbind(FramebufferTarget.DrawFramebuffer);
      FramebufferGeneric.UnbindRenderbuffer();//should only be called automatically if we have dpeth target

      State = FramebufferState.Initialized;
    }
    protected void DeleteTargets()
    {
      GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

      if (_glId > 0)
      {
        GL.DeleteFramebuffer(_glId);
      }

      for (int i = 0; i < Bindings.Count; ++i)
      {
        Bindings[i] = null;
      }
      Bindings.Clear();
    }

  }//Framebufferbase

  // public class DeferredFramebuffer : FramebufferGeneric
  // {
  //   public const string c_strColorMRT_DF = "ColorMRT-deferred";
  //   public const string c_strNormalMRT_DF = "NormalMRT-deferred";
  //   public const string c_strPlaneMRT_DF = "PlaneMRT-deferred";
  //   public const string c_strPositionMRT_DF = "PositionMRT-deferred";

  //   public DeferredFramebuffer(bool bMultisample, int nSamples, vec4 vClear) : base("Deferred_FBO", bMultisample, nSamples, vClear)
  //   {
  //     //  _pBloomVaoPass = NULL;
  //   }
  //   //virtual ~DeferredFramebuffer() override;

  //   public override void Init(int w, int h, FramebufferAttachment sharedDepth, FramebufferAttachment sharedPick)
  //   {
  //     DeleteTargets();

  //     bool _bUseRenderBuffer = false;

  //     //TODO: later we'll create this async.
  //     //Gd::verifyRenderThread();
  //     _glId = GL.GenFramebuffer();
  //     Gpu.CheckGpuErrorsRt();

  //     Bind(FramebufferTarget.Framebuffer);

  //     GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultWidth, w);
  //     Gpu.CheckGpuErrorsRt();
  //     GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultHeight, h);
  //     Gpu.CheckGpuErrorsRt();
  //     GL.ActiveTexture(TextureUnit.Texture0);
  //     Gpu.CheckGpuErrorsRt();

  //     string msaa = this._bMsaaEnabled ? "-msaa" : "";
  //     // - Textures
  //     //Don't change the names here, we reference them elsewhere *yikes*
  //     //These must match the order in the shader
  //     AddAttachment(c_strColorMRT_DF + msaa, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Color);//0
  //     AddAttachment(sharedPick);//1
  //     AddAttachment(c_strNormalMRT_DF + msaa, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Normal);//2
  //     AddAttachment(c_strPositionMRT_DF + msaa, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Position);//3 GL_RGBA32F GL_RGBA GL_FLOAT
  //     AddAttachment(c_strPlaneMRT_DF + msaa, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, w, h, RenderTargetType.Mat0);//4
  //     sharedDepth.Attach();

  //     CheckFramebufferComplete();

  //     SetObjectLabel();

  //     Unbind(FramebufferTarget.DrawFramebuffer);
  //     UnbindRenderbuffer();

  //     _eState = FramebufferState.Initialized;
  //   }

  //   public override void BeginRender()
  //   {
  //     if (_eState != FramebufferState.Initialized)
  //     {
  //       Gu.BRThrowException("Framebuffer was not initialized.");
  //     }

  //     Bind(FramebufferTarget.DrawFramebuffer);
  //     UnbindRenderbuffer();
  //     SetDrawAllTargets();

  //     //NOTE:
  //     //CRITICAL that clear color is zero here.
  //     // Otherwise the color somehow shows up in random places getting
  //     // blended with other colors..
  //     GL.ClearColor(ClearColor.r, ClearColor.g, ClearColor.b, ClearColor.a);
  //     GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
  //     Gpu.CheckGpuErrorsDbg();
  //   }
  //   public override void EndRender()
  //   {
  //     Unbind(FramebufferTarget.DrawFramebuffer);
  //   }

  // }
  // public class ForwardFramebuffer : FramebufferGeneric
  // {
  //   public const string c_strColorMRT_FW = "ColorMRT-forward";

  //   public ForwardFramebuffer(bool bMsaa, int nMsaa, vec4 vClear) : base("Forward_Framebuffer", bMsaa, nMsaa, vClear)
  //   {
  //   }
  //   public override void Init(int iWidth, int iHeight, FramebufferAttachment sharedDepth, FramebufferAttachment sharedPick)
  //   {
  //     DeleteTargets();

  //     GL.UseProgram(0);
  //     Unbind(FramebufferTarget.Framebuffer);
  //     UnbindRenderbuffer();

  //     _glId = GL.GenFramebuffer();
  //     GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glId);
  //     GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultWidth, iWidth);
  //     GL.FramebufferParameter(FramebufferTarget.Framebuffer, FramebufferDefaultParameter.FramebufferDefaultHeight, iHeight);
  //     Gpu.CheckGpuErrorsRt();
  //     string msaa = this._bMsaaEnabled ? "-msaa" : "";

  //     AddAttachment(c_strColorMRT_FW + msaa, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float, iWidth, iHeight, RenderTargetType.Color);
  //     AddAttachment(sharedPick);
  //     sharedDepth.Attach();

  //     CheckFramebufferComplete();

  //     SetObjectLabel();

  //     //Return to default.
  //     GL.UseProgram(0);

  //     Unbind(FramebufferTarget.Framebuffer);
  //     UnbindRenderbuffer();

  //     _eState = FramebufferState.Initialized;
  //   }


  //   public override void BeginRender()
  //   {
  //     if (_eState != FramebufferState.Initialized)
  //     {
  //       Gu.BRThrowException("Framebuffer was not initialized.");
  //     }

  //     //Clear all buffers
  //     Bind(FramebufferTarget.DrawFramebuffer);
  //     UnbindRenderbuffer();//_depthRenderBufferId);

  //     //Do not clear! - previous deferred operation is in here. (clear happens in clearFb)
  //     //**Do not clear***
  //     //**Do not clear***
  //     //**Do not clear***
  //     //**Do not clear***
  //     //**Do not clear***
  //     //**Do not clear***

  //   }
  //   public override void EndRender()
  //   {
  //     //noting
  //   }
  //   public void ClearSharedFb()
  //   {
  //     //Call this before we begin the defrred
  //     Bind(FramebufferTarget.DrawFramebuffer);
  //     UnbindRenderbuffer();
  //     SetDrawAllTargets();

  //     GL.ClearColor(ClearColor.r, ClearColor.g, ClearColor.b, ClearColor.a);
  //     GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
  //     Unbind(FramebufferTarget.DrawFramebuffer);
  //   }

  // };

}//NS piratecarft
