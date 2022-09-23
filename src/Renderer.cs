using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using System.ComponentModel;

namespace PirateCraft
{
  #region Enums

  public enum PipelineStageEnum
  {
    [Description("**UNSET**")] Unset,
    [Description("DEF_PIPELINE_STAGE_DEFERRED")] Deferred,
    [Description("DEF_PIPELINE_STAGE_DEFERRED_BLIT")] DeferredBlit,
    [Description("DEF_PIPELINE_STAGE_FORWARD")] Forward,
    [Description("DEF_PIPELINE_STAGE_FORWARD_BLIT")] ForwardBlit,
    [Description("DEF_PIPELINE_STAGE_SHADOW_DEPTH")] Shadow,
    [Description("**MaxPipelineStages**")] MaxPipelineStages
  }
  public enum RendererState
  {
    None,
    BeginView,
    EndView
  }

  #endregion

  public class Picker
  {
    //**Note do not set this to be anything but full alpha, if blending is enabled teh blend will screw up this value.
    public const uint c_iInvalidPickId = 0;//0xFFFFFFFF;

    private uint _iid = 0;
    private WeakReference<Renderer> _pRenderer;
    private uint _uiLastSelectedPixelId = 0;//Note: This is relative to the last UserSelectionSet - the Id here is not fixed.
    public uint GetSelectedPixelId() { return _uiLastSelectedPixelId; }

    //The picked this frame (eg gui item, worldobj). Can be any object.
    public object PickedObjectFrameLast { get; set; } = null;
    public object PickedObjectFrame { get; set; } = null;

    public string PickedObjectName
    {
      get
      {
        if (PickedObjectFrame == null)
        {
          return "<None>";
        }
        else if (PickedObjectFrame is WorldObject)
        {
          return (PickedObjectFrame as WorldObject).Name;
        }
        else if (PickedObjectFrame is UiElement)
        {
          return (PickedObjectFrame as UiElement).Name;
        }
        else if (PickedObjectFrame is SoloMesh)
        {
          if ((PickedObjectFrame as SoloMesh).Mesh != null)
          {
            return (PickedObjectFrame as SoloMesh).Mesh.Name;
          }
          else
          {
            return "SoloMesh (no mesh)";
          }
        }
        return "<Unhandled object type>" + PickedObjectFrame.GetType().Name;
      }
    }

    public Picker(Renderer rp)
    {
      _pRenderer = new WeakReference<Renderer>(rp);
    }
    public void UpdatePick()
    {
      PickedObjectFrameLast = PickedObjectFrame;
      PickedObjectFrame = null;

      UpdatePickedPixel((int)Gu.Mouse.Pos.x, (int)Gu.Mouse.Pos.y);
    }
    public uint GenPickId()
    {
      //Creates a pick ID, note that this ID is colored so we can see it (alpha off)
      uint increment = 1;
#if DEBUG
      increment = 100;//@100 = 167,000 ids
#endif
      //DEBUG pick ID that shows the color of the picked object.
      _iid = (_iid + increment);
      if (_iid > 0xFFFFFF)
      {
        //50 = 335544 possible Id's, 10=1677721.5 id's still possible to wrap
        Gu.Log.Warn("Pick Id Generator just wrapped, check if debug mode,  increment =" + increment);
        _iid %= 0xFFFFFF;
      }

      //Return an actual color so we can see it. Also, always set full alpha in case blending is enabled by accident.
      uint pickColorId = ((_iid << 8) | 0x000000FF) & 0xFFFFFFFF;

      return pickColorId;
    }
    private void UpdatePickedPixel(int x, int y)
    {
      if (_pRenderer != null && _pRenderer.TryGetTarget(out var renderer))
      {
        Gu.Assert(renderer.PickStage != null);

        renderer.PickStage.Bind(FramebufferTarget.ReadFramebuffer);
        renderer.PickStage.BindReadBuffer(RenderTargetType.Pick);

        _uiLastSelectedPixelId = SamplePixelId(x, y);

        renderer.PickStage.UnbindReadBuffer();
        renderer.PickStage.Unbind(FramebufferTarget.ReadFramebuffer);
      }
    }
    private uint SamplePixelId(int x, int y)
    {
      uint pixel = 0;

      //https://www.khronos.org/opengles/sdk/docs/man/xhtml/glReadPixels.xml
      //If the currently bound framebuffer is not the default framebuffer object, color components
      // are read from the color image attached to the GL_COLOR_ATTACHMENT0 attachment point.

      //We sample from the entire window
      int iHeight = Gu.Context.GameWindow.Height;

      GL.ReadPixels(x - 1,
                   iHeight - y + 1,
                   1, 1,
                   PixelFormat.RedInteger,
                   PixelType.UnsignedInt,
                   ref pixel);

      Gpu.CheckGpuErrorsDbg();
      return pixel;
    }


  }

  public class PipelineStage
  {
    public string ShaderDefine = "";
    public PipelineStageEnum PipelineStageEnum { get; set; }
    public FramebufferGeneric InputFramebuffer = null;
    public FramebufferGeneric OutputFramebuffer = null;
    public ClearBufferMask ClearMask;
    public vec4 ClearColor { get; set; } = new vec4(0, 0, 0, 1);
    public List<FramebufferAttachment> Inputs;
    public List<FramebufferAttachment> Outputs;
    public WorldObject BlitObj = null;
    public mat4 BlitMat = mat4.Identity;

    public int BlitWidth = 1;
    public int BlitHeight = 1;
    public string Name { get { return PipelineStageEnum.Description(); } }
    public Action<RenderView> BeginRenderAction = null;
    public Action<RenderView> EndRenderAction = null;

    //So, cull, winding .. 
    public PipelineStage(PipelineStageEnum stage, ClearBufferMask mask, vec4 clear,
    List<FramebufferAttachment> inputs, List<FramebufferAttachment> outputs, Action<RenderView> beginRenderAction = null, Action<RenderView> endRenderAction = null)
    {
      ClearMask = mask;
      ClearColor = clear;
      PipelineStageEnum = stage;
      BeginRenderAction = beginRenderAction;
      EndRenderAction = endRenderAction;
      Inputs = inputs;
      Outputs = outputs;
      Validate(inputs, outputs);

      if (outputs.Count > 0)
      {
        OutputFramebuffer = new FramebufferGeneric(Enum.GetName(stage) + "-out-fb", outputs);
      }
      if (inputs.Count > 0)
      {
        InputFramebuffer = new FramebufferGeneric(Enum.GetName(stage) + "-in-fb", inputs);
      }
    }
    private void Validate(List<FramebufferAttachment> inputs, List<FramebufferAttachment> outputs)
    {
      string s = "";
      for (int i = 0; i < inputs.Count; ++i)
      {
        for (int j = i + 1; j < outputs.Count; ++j)
        {
          if (inputs[i] == outputs[j])
          {
            //an input is also an output. Error
            s += "Framebuffer " + Name + " input is also an output : " + inputs[i].Texture.Name + "\n";
          }
          if (inputs[i].Texture == null)
          {
            s += "Framebuffer " + Name + " input " + i + " texture was null. " + "\n";

          }
          if (outputs[j].Texture == null)
          {
            s += "Framebuffer " + Name + " output " + j + " texture was null. " + "\n";
          }
        }
      }
      if (StringUtil.IsNotEmpty(s))
      {
        Gu.BRThrowException(s);
      }
    }
    public void BeginRender(bool forceClear)
    {
      GL.UseProgram(0);//just for sanity i guess

      if (InputFramebuffer != null)
      {
        InputFramebuffer.Bind(FramebufferTarget.ReadFramebuffer);
      }

      if (OutputFramebuffer != null)
      {
        if (OutputFramebuffer.State != FramebufferState.Initialized)
        {
          Gu.BRThrowException("Framebuffer was not initialized.");
        }

        OutputFramebuffer.Bind(FramebufferTarget.DrawFramebuffer);
        FramebufferGeneric.UnbindRenderbuffer();
        OutputFramebuffer.SetDrawAllTargets();
      }
      else
      {
        //Default FBO
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        Gpu.CheckGpuErrorsDbg();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        Gpu.CheckGpuErrorsDbg();
      }
      if (ClearMask > 0)
      {
        GL.ClearColor(ClearColor.r, ClearColor.g, ClearColor.b, ClearColor.a);
        GL.Clear(ClearMask);
      }
      else if (forceClear)
      {
        GL.ClearColor(ClearColor.r, ClearColor.g, ClearColor.b, ClearColor.a);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      }

    }
    public void EndRender()
    {
      if (InputFramebuffer != null)
      {
        InputFramebuffer.Unbind(FramebufferTarget.ReadFramebuffer);
      }
      if (OutputFramebuffer != null)
      {
        OutputFramebuffer.Unbind(FramebufferTarget.DrawFramebuffer);
      }
    }
  }

  public class Renderer : HasGpuResources
  {
    #region Public:Members

    public ShaderControlVars DefaultControlVars = new ShaderControlVars();
    public RendererState RenderState { get; private set; } = RendererState.None;
    //public PipelineStageEnum PipelineStage { get; private set; } = PipelineStageEnum.Unset;
    public Picker Picker { get; private set; } = null;
    public PipelineStage CurrentStage { get; private set; } = null;
    private RenderView CurrentView = null;
    public FramebufferGeneric PickStage { get; private set; } = null;
    private int _windowWidth = 1;
    private int _windowHeight = 1;
    #endregion
    #region Private:Members

    private List<PipelineStage> PipelineStages = new List<PipelineStage>();
    private List<FramebufferAttachment> Attachments = new List<FramebufferAttachment>();

    private Texture2D _pEnvTex = null;  //Enviro map - for mirrors (coins)
    private vec4 ClearColor { get; set; } = new vec4(1, 1, 0, 1);//(0.01953, .4114f, .8932f, 1);
    private bool _requestSaveFBOs = false;

    #endregion
    #region Public:methods

    public Renderer()
    {
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      releaseFbosAndMesh();
    }
    public void init(int iWindowWidth, int iWindowHeight, FileLoc envTextureLoc, int pixelWidth = -1)
    {
      Gu.Log.Info("[Renderer] Initializing, Window: " + iWindowWidth + "x" + iWindowHeight);
      if (iWindowWidth <= 0 || iWindowHeight <= 0)
      {
        Gu.BRThrowException("[Renderer] Got framebuffer of width or height < 0: " + iWindowWidth + "," + iWindowHeight);
      }

      //Enable some stuff.
#if _DEBUG
      GL.Enable(EnableCap.DebugOutput);
#endif
      GL.Enable(EnableCap.CullFace);
      GL.FrontFace(FrontFaceDirection.Ccw);
      GL.Enable(EnableCap.DepthTest);
      GL.DepthMask(true);
      GL.Disable(EnableCap.Blend);
      GL.Disable(EnableCap.ScissorTest);

      releaseFbosAndMesh();

      // - Setup Framebuffers.

      ///**Testing scaling**
      ///**Testing scaling**
      ///**Testing scaling**
      ///**Testing scaling**
      _windowWidth = iWindowWidth;
      _windowHeight = iWindowHeight;
      int iWidth = 0;
      int iHeight = 0;
      if (pixelWidth == -1)
      {
        iWidth = iWindowWidth;
        iHeight = iWindowHeight;
      }
      else
      {
        float ar = (float)iWindowWidth / (float)iWindowHeight;// 4.0f / 3.0f;
        float fbWidth = 500; //500 px
        float fbHeight = ar * fbWidth; //(int)(iWindowHeight * 0.6f)
        iWidth = (int)Math.Max(Math.Round(fbWidth), 1);
        iHeight = (int)Math.Max(Math.Round(fbHeight), 1);
      }
      Gu.Log.Info("Scaled w/h: " + iWidth + "," + iHeight);

      Gu.Log.Info("[Renderer] Checking FBO Caps");
      CheckDeviceCaps(iWidth, iHeight, Gu.EngineConfig.EnableMSAA ? Gu.EngineConfig.MSAASamples : 0);

      int samples = 0;
      if (Gu.EngineConfig.EnableMSAA)
      {
        GL.Enable(EnableCap.Multisample);
        Gpu.CheckGpuErrorsRt();
        samples = Gu.EngineConfig.MSAASamples;
      }
      else
      {
        samples = 0;
      }

      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      Gpu.CheckGpuErrorsDbg();
      GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
      Gpu.CheckGpuErrorsDbg();
      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
      Gpu.CheckGpuErrorsDbg();

      Gu.Assert(samples == 0);//TODO: we need to duplicate the targets for MSAA, but we probably won't use MSAA for this game.

      Picker = new Picker(this);

      Gu.Log.Debug("[Renderer] Creating Framebuffer Attachments");
      FramebufferAttachment albedo_df = new FramebufferAttachment("Color"/*names must match!*/, RenderTargetType.Color, iWidth, iHeight, samples);
      FramebufferAttachment albedo_fw = new FramebufferAttachment("Color"/*names must match!*/, RenderTargetType.Color, iWidth, iHeight, samples);
      FramebufferAttachment pick = new FramebufferAttachment("Pick", RenderTargetType.Pick, iWidth, iHeight, samples);
      FramebufferAttachment normal = new FramebufferAttachment("Normal", RenderTargetType.Color, iWidth, iHeight, samples);
      FramebufferAttachment position = new FramebufferAttachment("Position", RenderTargetType.Color, iWidth, iHeight, samples);
      FramebufferAttachment plane = new FramebufferAttachment("Plane", RenderTargetType.Color, iWidth, iHeight, samples);
      FramebufferAttachment depth = new FramebufferAttachment("Depth", RenderTargetType.Depth, iWidth, iHeight, samples);

      Gu.Log.Debug("[Renderer] Creating Pipeline Stages");

      var deferred = new PipelineStage(PipelineStageEnum.Deferred,
        ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit, ClearColor,
        new List<FramebufferAttachment>() { },
        new List<FramebufferAttachment>() { albedo_df, pick, normal, position, plane, depth },
        (rv) =>
        {
          Gu.World.RenderDeferred(Gu.Context.Delta, rv);
        },
        (rv) =>
        {
          if (_requestSaveFBOs || Gu.EngineConfig.SaveFBOsEveryFrame)
          {
            SaveCurrentStageFBOsImmediately(CurrentStage.OutputFramebuffer);
          }
        }
      );

      var deferredBlit = new PipelineStage(PipelineStageEnum.DeferredBlit,
        ClearBufferMask.None, ClearColor,
        new List<FramebufferAttachment>() { albedo_df, normal, position, plane, depth },
        new List<FramebufferAttachment>() { albedo_fw }
        , (rv) =>
        {
        },
        (rv) =>
        {
          if (_requestSaveFBOs || Gu.EngineConfig.SaveFBOsEveryFrame)
          {
            SaveCurrentStageFBOsImmediately(CurrentStage.OutputFramebuffer);
          }
        }
       );

      deferredBlit.BlitObj = new WorldObject("dummy-blit-d");
      deferredBlit.BlitObj.Mesh = MeshData.CreateScreenQuadMesh(iWidth, iHeight);
      deferredBlit.BlitObj.Material = new Material("blit-d-material", Gu.Resources.LoadShader("v_v3x2_deferred", false, FileStorage.Embedded));
      deferredBlit.BlitObj.Material.GpuRenderState.CullFace = false;
      deferredBlit.BlitObj.Material.GpuRenderState.DepthTest = false;
      deferredBlit.BlitObj.Material.GpuRenderState.Blend = false;
      deferredBlit.BlitObj.Material.AlbedoSlot.Texture = albedo_df.Texture;
      deferredBlit.BlitObj.Material.NormalSlot.Texture = normal.Texture;
      deferredBlit.BlitObj.Material.PositionSlot.Texture = position.Texture;
      deferredBlit.BlitObj.Material.MetalnessSlot.Texture = plane.Texture;
      deferredBlit.BlitMat = mat4.getOrtho(0, iWidth, 0, iHeight, -1, 1);
      deferredBlit.BlitWidth = iWidth;
      deferredBlit.BlitHeight = iHeight;

      var forward = new PipelineStage(PipelineStageEnum.Forward,
        ClearBufferMask.None, ClearColor,
        new List<FramebufferAttachment>() { },
        new List<FramebufferAttachment>() { albedo_fw, pick, depth },
        (rv) =>
        {
          Gu.World.RenderForward(Gu.Context.Delta, rv);
          Gu.World.RenderDebugForward(Gu.Context.Delta, rv);
          rv.ActiveGui?.Render(rv);
        },
        (rv) =>
        {
          if (_requestSaveFBOs || Gu.EngineConfig.SaveFBOsEveryFrame)
          {
            SaveCurrentStageFBOsImmediately(CurrentStage.OutputFramebuffer);
          }
        }
       );

      PickStage = forward.OutputFramebuffer;

      var forwardBlit = new PipelineStage(PipelineStageEnum.ForwardBlit,
        ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit, ClearColor,
        new List<FramebufferAttachment>() { albedo_fw },
        new List<FramebufferAttachment>() { }//default
        , (rv) =>
        {
        },
        (rv) =>
        {
          if (_requestSaveFBOs || Gu.EngineConfig.SaveFBOsEveryFrame)
          {
            SaveCurrentStageFBOsImmediately(CurrentStage.OutputFramebuffer);
          }
        }
      );
      forwardBlit.BlitObj = new WorldObject("dummy-blit-f");
      forwardBlit.BlitObj.Mesh = MeshData.CreateScreenQuadMesh(iWindowWidth, iWindowHeight);
      forwardBlit.BlitObj.Material = new Material("blit-f-material", Gu.Resources.LoadShader("v_v3x2_forward", false, FileStorage.Embedded));
      forwardBlit.BlitObj.Material.GpuRenderState.CullFace = false;
      forwardBlit.BlitObj.Material.GpuRenderState.DepthTest = false;
      forwardBlit.BlitObj.Material.GpuRenderState.Blend = false;
      forwardBlit.BlitObj.Material.AlbedoSlot.Texture = albedo_fw.Texture;
      forwardBlit.BlitMat = mat4.getOrtho(0, iWindowWidth, 0, iWindowHeight, -1, 1);
      forwardBlit.BlitWidth = iWindowWidth;
      forwardBlit.BlitHeight = iWindowHeight;

      PipelineStages = new List<PipelineStage>(){
        deferred,
        deferredBlit,
        forward,
        forwardBlit
      };

      if (envTextureLoc != null)
      {
        _pEnvTex = Gu.Resources.LoadTexture(envTextureLoc, true, TexFilter.Linear);
      }
      else
      {
        Gu.Log.Warn("No environment texture specified.");
      }
    }
    public void ResizeScreenBuffers(int w, int h)
    {
      //Simply called neow by new camera vp size.
      //recreate everything
      init(w, h, null);
    }
    public void SaveScreenshot()
    {
      //TODO: don't save the FBOs, we should save the default framebuffer so we get an exact screenshot
      Gu.BRThrowNotImplementedException();
      //_requestSaveScreenshot = true;
    }
    public void SaveFBOs()
    {
      //The reason we do it this way is so that we can save the screenshot right before we blit the final render to the screen.
      _requestSaveFBOs = true;
    }
    public void BeginRenderToWindow()
    {
      if (Gu.EngineConfig.Debug_ShowPipelineClearMessage)
      {
        Gu.Log.WarnCycle("Clearing all pipelines, when it is not necessary (debug)");
      }

      //**FULL CLEAR
      GL.Viewport(0, 0, Gu.Context.GameWindow.Width, Gu.Context.GameWindow.Height);
      GL.Scissor(0, 0, Gu.Context.GameWindow.Width, Gu.Context.GameWindow.Height);
      foreach (var ps in this.PipelineStages)
      {
        ps.BeginRender(true);
        ps.EndRender();
      }
    }
    public void EndRenderToWindow()
    {
      Picker.UpdatePick();
      _requestSaveFBOs = false;
    }
    public void RenderViewToWindow(RenderView rv, List<PipelineStageEnum> stages = null)
    {
      //Make sure the given view has a camera attached.
      if (BeginRenderToView(rv))
      {

        foreach (PipelineStage ps in PipelineStages)
        {
          //
          if (stages != null && !stages.Contains(ps.PipelineStageEnum))
          {
            continue;
          }

          //OpenGL Y = Bottom left!!!
          int vx = rv.Viewport.X;
          int vy = _windowHeight - rv.Viewport.Y - rv.Viewport.Height;
          int vw = rv.Viewport.Width;
          int vh = rv.Viewport.Height;
          GL.Viewport(vx, vy, vw, vh);
          GL.Scissor(vx, vy, vw, vh);


          CurrentStage = ps;

          //Bind the output FBO
          ps.BeginRender(false);
          {
            //Do some pre-render stuff
            ps.BeginRenderAction?.Invoke(rv);

            //If we are a blit stage, execute a blit.
            if (ps.BlitObj != null)
            {
              //blit
              rv.BeginRender2D(ps.BlitMat);
              {
                //Set the viewport to the whole window to blit the fullscreen quad however set the 
                //scissor to be just the viewport area.
                //TODO: it would make more sense to have the quad blit just to the given area, and not have to re-set the viewport.
                //https://stackoverflow.com/questions/33718237/do-you-have-to-call-glviewport-every-time-you-bind-a-frame-buffer-with-a-differe
                GL.Viewport(0, 0, ps.BlitWidth, ps.BlitHeight);
                //    Gu.BreakRenderState = true;
                DrawCall.Draw(Gu.World.WorldProps, rv, ps.BlitObj);
              }
              rv.EndRender2D();
            }
            ps.EndRenderAction?.Invoke(rv);
          }
          ps.EndRender();
        }
        CurrentStage = null;
      }
      EndRenderToView(rv);
    }
    public PipelineStage GetPipelineStage(PipelineStageEnum e)
    {
      foreach (var s in PipelineStages)
      {
        if (s.PipelineStageEnum == e)
        {
          return s;
        }
      }
      return null;
    }

    #endregion
    #region Private:Methods
    private bool BeginRenderToView(RenderView rv)
    {
      Gu.Assert(RenderState == RendererState.EndView || RenderState == RendererState.None);
      RenderState = RendererState.BeginView;
      CurrentView = rv;
      if (!rv.BeginRender3D())
      {
        return false;
      }
      Gu.Context.DebugDraw.BeginFrame();
      SetInitialGpuRenderState();
      Gpu.CheckGpuErrorsDbg();
      //  _pMsaaForward.ClearSharedFb();//Must call before deferred. After Picker.
      //PipelineStage = PipelineStageEnum.Unset;
      return true;
    }
    private void EndRenderToView(RenderView rv)
    {
      Gu.Assert(RenderState == RendererState.BeginView);
      RenderState = RendererState.EndView;
      rv.EndRender3D();
      CurrentView = null;

      //PipelineStage = PipelineStageEnum.Unset;
    }
    private void CheckMultisampleParams(int samples)
    {
      int iMaxSamples;
      GL.GetInteger(GetPName.MaxSamples, out iMaxSamples);
      Gu.Log.Info("Max OpenGL MSAA Samples " + iMaxSamples);

      if (samples > 0)
      {
        if ((int)samples > iMaxSamples)
        {
          Gu.Log.Warn("[Renderer] MSAA sample count of '" + samples +
                    "' was larger than the card's maximum: '" + iMaxSamples + "'. Truncating.");
          samples = iMaxSamples;
          Gu.DebugBreak();
        }
        if (!MathUtils.IsPowerOfTwo(samples))
        {
          Gu.Log.Warn("[Renderer] Error, multisampling: The number of samples must be 2, 4, or 8.  Setting to 2.");
          samples = iMaxSamples > 2 ? 2 : iMaxSamples;
          Gu.DebugBreak();
        }
      }
    }
    private void CheckDeviceCaps(int iWidth, int iHeight, int samples)
    {
      //TODO: later we'll create this async.
      //  Gd::verifyRenderThread();

      //Make sure we have enough render targets
      int iMaxDrawBuffers = 0;
      int iMaxFbWidth = 0;
      int iMaxFbHeight = 0;
      //TODO : check to see if this includes a depth MRT
      GL.GetInteger(GetPName.MaxDrawBuffers, out iMaxDrawBuffers);

      if (iMaxDrawBuffers < 1)
      {
        Gu.BRThrowException("[Renderer] Your GPU only supports " + iMaxDrawBuffers +
                         " MRTs, the system requires at least " + 1 +
                         " MRTs. Consider upgrading graphics card.");
      }

      GL.GetInteger((OpenTK.Graphics.OpenGL4.GetPName)GLenum.GL_MAX_FRAMEBUFFER_HEIGHT, out iMaxFbHeight);
      GL.GetInteger((OpenTK.Graphics.OpenGL4.GetPName)GLenum.GL_MAX_FRAMEBUFFER_WIDTH, out iMaxFbWidth);

      if (iMaxFbHeight < iHeight || iMaxFbWidth < iWidth)
      {
        Gu.BRThrowException("[Renderer] Your GPU only supports MRTs at " +
                         iMaxFbWidth + "x" + iMaxFbHeight +
                         " pixels. The system requested " + iWidth + "x" + iHeight + ".");
      }

      CheckMultisampleParams(samples);
    }
    private void releaseFbosAndMesh()
    {
      GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
      PipelineStages.Clear();
    }
    private void SetShadowEnv(/*LightManager lightman, */bool bSet)
    {

    }
    private void SetInitialGpuRenderState()
    {
      Gpu.CheckGpuErrorsDbg();
      GL.Enable(EnableCap.CullFace);
      GL.CullFace(CullFaceMode.Back);
      if (Gu.CoordinateSystem == CoordinateSystem.Lhs)
      {
        GL.FrontFace(FrontFaceDirection.Cw);
      }
      else
      {
        GL.FrontFace(FrontFaceDirection.Ccw);
      }
      GL.Enable(EnableCap.DepthTest);
      GL.Enable(EnableCap.ScissorTest);

    }
    public HashSet<FramebufferAttachment> GetAllUniqueAttachments()
    {
      var x = new HashSet<FramebufferAttachment>();
      foreach (var ps in PipelineStages)
      {
        if (ps.OutputFramebuffer != null)
        {
          foreach (var b in ps.OutputFramebuffer.Bindings)
          {
            x.Add(b.Attachment);
          }
        }
      }
      return x;
    }
    private void SaveCurrentStageFBOsImmediately(FramebufferGeneric fbo, string tag = "")
    {
      string ctxName = Gu.Context.GameWindow.Name;

      //if (Gu::getGlobalInput().keyPress(SDL_SCANCODE_F9))
      //{
      //  if (Gu::getGlobalInput().shiftHeld())
      //  {
      Gu.Log.Info("[Renderer] Saving all MRTs.");
      //Save all deferred textures
      int iTarget;

      //*Don't save the master shadowbox image.
      //for (int i = 0; i < 6; ++i) {
      //    string fname = FileSystem::getScreenshotFilename();
      //    fname = TStr(fname, "_shadowbox_MASTER_", 0, "_side_", i, "_.png");
      //    RenderUtils::saveTexture(std::move(fname), _pShadowBoxFboMaster.getGlTexId(), GL_TEXTURE_CUBE_MAP, i);
      //    BroLogInfo("[Renderer] Screenshot '", fname, "' saved");
      //}
      iTarget = 0;
      //for (ShadowFrustum sf : lightman.getAllShadowFrustums())
      //{
      //  string fname = FileSystem::getScreenshotFilename();
      //  fname = fname + "_shadow_frustum_" + iTarget + "_.png";
      //  getContext().getRenderUtils().saveTexture(std::move(fname), sf.getGlTexId(), GL_TEXTURE_2D);
      //  Gu.Log.Info("[Renderer] Screenshot '" + fname + "' saved");
      //  iTarget++;
      //}
      //iTarget = 0;
      //for (ShadowBox sb : lightman.getAllShadowBoxes())
      //{
      //  for (int i = 0; i < 6; ++i)
      //  {
      //    string fname = FileSystem::getScreenshotFilename();

      //    string side;
      //    if (i + GL_TEXTURE_CUBE_MAP_POSITIVE_X == GL_TEXTURE_CUBE_MAP_POSITIVE_X)
      //      side = "+X";
      //    else if (i + GL_TEXTURE_CUBE_MAP_POSITIVE_X == GL_TEXTURE_CUBE_MAP_POSITIVE_Y)
      //      side = "+Y";
      //    else if (i + GL_TEXTURE_CUBE_MAP_POSITIVE_X == GL_TEXTURE_CUBE_MAP_POSITIVE_Z)
      //      side = "+Z";
      //    else if (i + GL_TEXTURE_CUBE_MAP_POSITIVE_X == GL_TEXTURE_CUBE_MAP_NEGATIVE_X)
      //      side = "-X";
      //    else if (i + GL_TEXTURE_CUBE_MAP_POSITIVE_X == GL_TEXTURE_CUBE_MAP_NEGATIVE_Y)
      //      side = "-Y";
      //    else if (i + GL_TEXTURE_CUBE_MAP_POSITIVE_X == GL_TEXTURE_CUBE_MAP_NEGATIVE_Z)
      //      side = "-Z";

      //    fname = fname + "_shadowbox_" + iTarget + "_side_" + side + "_.png";
      //    getContext().getRenderUtils().saveTexture(std::move(fname), sb.getGlTexId(), GL_TEXTURE_CUBE_MAP, i);
      //    Gu.Log.Info("[Renderer] Screenshot '" + fname + "' saved");
      //  }
      //  iTarget++;
      //}



      //ok so pick isn't getting savd..
      //GetAllUniqueAttachments();

      //using input framebuffer as we call saveFBOs in the blit routine (which, uses inputs)
      if (fbo != null)
      {
        string prefix = Gu.GetFilenameDateTimeNOW() + " ctx-" + ctxName + " " + "view-" + CurrentView.Id + " " + fbo.Name + " " + tag;
        foreach (var bind in fbo.Bindings)// atts)
        {
          var pTarget = bind.Attachment;
          string fname = prefix + " " + pTarget.Texture.Name + " index-" + bind.LayoutIndex + ".png";//names may be the same
          fname = System.IO.Path.Combine(Gu.LocalTmpPath, fname);
          ResourceManager.SaveTexture(new FileLoc(fname, FileStorage.Disk), pTarget.Texture, true, true, -1, true);
          Gu.Log.Info("[Renderer] Screenshot '" + fname + "' saved");
        }
      }
    }

    #endregion














  }

}
