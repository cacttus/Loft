using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.Serialization;

namespace PirateCraft
{
  public enum RenderTargetType
  {
    Color,//vec4
    Depth,//float
    Pick, //uint
  }
  public enum ShaderOutput
  {
    [Description("Color")] Color,
    [Description("Depth")] Depth,
    [Description("Pick")] Pick,
    [Description("Normal")] Normal,
    [Description("Position")] Position,
    [Description("Plane")] Plane,
    [Description("Effect")] Effect,
  }
  public enum ShaderInput
  {
    [Description("_ufMRT_Color")] Color,
    [Description("_ufMRT_Normal")] Normal,
    [Description("_ufMRT_Position")] Position,//these are tostring()'d so dont change
    [Description("_ufMRT_Pick")] Pick,
    [Description("_ufMRT_Depth")] Depth,
  }
  public enum FramebufferState
  {
    Not_Initialized,
    Initialized
  }
  public class FramebufferAttachment : HasGpuResources
  {
    public Texture Texture { get; set; } = null;
    public RenderTargetType TargetType { get; set; } = RenderTargetType.Color;
    public string Name { get; private set; } = Library.UnsetName;

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
    public FramebufferAttachment(string name, RenderTargetType eTargetType, int w, int h, int nMsaaSamples)
    {
      Name = name;
      if (eTargetType == RenderTargetType.Depth)
      {
        // inf._iLayoutIndex = -1; //layout index doens't matter for depth
        // inf._eAttachment = OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment;

        //_eBlitBit = ClearBufferMask.DepthBufferBit;
        Texture = new Texture(name, w, h, nMsaaSamples);
      }
      else
      {
        PixelInternalFormat internalFormat = PixelInternalFormat.Rgba16f;
        PixelFormat texFormat = PixelFormat.Rgba;
        PixelType dataType = PixelType.Float;
        if (eTargetType == RenderTargetType.Pick)
        {
          internalFormat = PixelInternalFormat.R32ui;
          texFormat = PixelFormat.RedInteger;
          dataType = PixelType.UnsignedInt;
        }
        else
        {
          if (Gu.EngineConfig.ColorBitDepth == ColorBitDepth.FB_16_BIT)
          {
            internalFormat = PixelInternalFormat.Rgba16f;
          }
          else if (Gu.EngineConfig.ColorBitDepth == ColorBitDepth.FB_32_BIT)
          {
            internalFormat = PixelInternalFormat.Rgba32f;
          }
          else
          {
            Gu.BRThrowNotImplementedException();
          }
          texFormat = PixelFormat.Rgba;
          dataType = PixelType.Float;
        }

        // _eBlitBit = ClearBufferMask.ColorBufferBit;
        Texture = new Texture(name, internalFormat, texFormat, dataType, w, h, nMsaaSamples);
      }
      TargetType = eTargetType;
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      Texture.GpuTexture.Dispose();
      Texture = null;
    }
  }
  public class FramebufferBinding
  {
    public PipelineAttachment Attachment = null;
    public int LayoutIndex;// The (layout = 0).. in the shader
    public GLenum BindingIndex;//GL_COLORATTACHMENT_0 + n
    public BlendingFactorSrc SrcRGB = BlendingFactorSrc.One;
    public BlendingFactorDest DstRGB = BlendingFactorDest.Zero;
    public BlendingFactorSrc SrcAlpha = BlendingFactorSrc.One; //one/zero = disable blend
    public BlendingFactorDest DstAlpha = BlendingFactorDest.Zero;
    public FramebufferBinding(PipelineAttachment at, int index)
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
          GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment, TextureTarget.Texture2DMultisample, Attachment.Texture.GlId, 0);
        }
        else
        {
          GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, OpenTK.Graphics.OpenGL4.FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, Attachment.Texture.GlId, 0);
        }
      }
      else
      {

        if (Attachment.IsMsaaEnabled)
        {
          GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, (OpenTK.Graphics.OpenGL4.FramebufferAttachment)BindingIndex, TextureTarget.Texture2DMultisample, Attachment.Texture.GlId, 0);
        }
        else
        {
          GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, (OpenTK.Graphics.OpenGL4.FramebufferAttachment)BindingIndex, TextureTarget.Texture2D, Attachment.Texture.GlId, 0);
        }
      }
      Gpu.CheckGpuErrorsDbg();
    }
  }
  [DataContract]
  public class FramebufferGeneric : OpenGLResource
  {
    public const int c_iMaxAttachments = 64;

    [DataMember] public List<FramebufferBinding> Bindings { get; private set; } = new List<FramebufferBinding>();
    public FramebufferState State { get; private set; } = FramebufferState.Not_Initialized;

    public ivec2 Size { get { return this.Bindings[0].Attachment.Texture.Size; } }

    public FramebufferGeneric(string label, List<PipelineAttachment> attachments) : base(label)
    {
      //If no output we're default, in that case, no FBO,use PipelineStage's default.
      Gu.Assert(attachments.Count > 0);

      DeleteTargets();

      _glId = GT.GenFramebuffer();
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

      foreach (var att in attachments)
      {
        if (att.Texture.Width != w || att.Texture.Height != h)
        {
          /*
          we are setting blit size in the blit stage of the renderer.
          different FBO size may never be needed anyway.
          */
          Gu.Log.Error("Framebuffer textures were not of equal size. This is not yet supported.");
          Gu.DebugBreak();
        }
      }

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
        GT.DeleteFramebuffer(_glId);
      }

      for (int i = 0; i < Bindings.Count; ++i)
      {
        Bindings[i] = null;
      }
      Bindings.Clear();
    }

  }//Framebufferbase

}//NS piratecarft
