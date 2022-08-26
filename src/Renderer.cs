using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
    Begin,
    End
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

    //The world object that was picked.. this MUST be set if picked object was set.
    public WorldObject PickedWorldObjectFrameLast { get; set; } = null;
    public WorldObject PickedWorldObjectFrame { get; set; } = null;
    //The picked this frame (eg gui item, worldobj). Can be any object.
    public object PickedObjectFrameLast { get; set; } = null;
    public object PickedObjectFrame { get; set; } = null;

    public string PickedWorldObjectName
    {
      get
      {
        return PickedWorldObjectFrame == null ? "<None>" : PickedWorldObjectFrame.Name;
      }
    }
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
        return "<Unhandled object type>" + PickedObjectFrame.GetType().Name;
      }
    }

    public Picker(Renderer rp)
    {
      _pRenderer = new WeakReference<Renderer>(rp);
    }
    public void Update(RenderView rv)
    {
      PickedWorldObjectFrameLast = PickedWorldObjectFrame;
      PickedWorldObjectFrame = null;
      PickedObjectFrameLast = PickedObjectFrame;
      PickedObjectFrame = null;

      UpdatePickedPixel(rv, (int)Gu.Mouse.Pos.x, (int)Gu.Mouse.Pos.y);
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

    private void UpdatePickedPixel(RenderView rv, int x, int y)
    {
      if (!rv.Viewport.ContainsPoint_WindowRelative(x, y))
      {
        return;
      }
      if (_pRenderer.TryGetTarget(out var renderer))
      {
        Gu.Assert(renderer.CurrentStage != null);
        renderer.CurrentStage.OutputFramebuffer.Bind(FramebufferTarget.ReadFramebuffer);
        var pick = renderer.CurrentStage.OutputFramebuffer.GetBinding(RenderTargetType.Pick);
        var readbufferMode = FramebufferGeneric.AttachmentIndexToReadBufferMode((OpenTK.Graphics.OpenGL4.FramebufferAttachment)pick.BindingIndex);
        GL.ReadBuffer(readbufferMode); //Note if you change this you must change the pick index in DeferredFramebuffer
        Gpu.CheckGpuErrorsDbg();

        _uiLastSelectedPixelId = SamplePixelId(rv, x, y);
        Gpu.CheckGpuErrorsDbg();

        // #if DEBUG

        //         if (_uiLastSelectedPixelId > 0)
        //         {

        //           if (Gu.Context.FrameStamp % 20 == 0)
        //           {
        //             Gu.Log.Debug("(" + x + "," + y + "), picked " + _uiLastSelectedPixelId);
        //           }
        //         }

        //         Gpu.CheckGpuErrorsDbg();

        // #endif

        GL.ReadBuffer(ReadBufferMode.None);
        Gpu.CheckGpuErrorsDbg();
      }
    }
    private uint SamplePixelId(RenderView rv, int x, int y)
    {
      Gu.Assert(rv.Camera != null);
      uint pixel = 0;

      //https://www.khronos.org/opengles/sdk/docs/man/xhtml/glReadPixels.xml
      //If the currently bound framebuffer is not the default framebuffer object, color components
      // are read from the color image attached to the GL_COLOR_ATTACHMENT0 attachment point.

      // BRLogTODO("Serious hack right here- fix it");
      if (rv.Camera != null)
      {
        int iHeight = rv.Camera.Viewport_Height;

        //var h = GCHandle.Alloc(pixel, GCHandleType.Pinned);
        GL.ReadPixels(x - 1,
                     iHeight - y + 1,
                     1, 1,
                     PixelFormat.RedInteger,
                     PixelType.UnsignedInt,
                     ref pixel);
        //h.Free();

        Gpu.CheckGpuErrorsDbg();
      }
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
    public WorldObject DefaultObj = null;
    public string Name { get { return PipelineStageEnum.Description(); } }
    public Action<RenderView> BeginRenderAction = null;
    public Action<RenderView> EndRenderAction = null;

    //So, cull, winding .. 
    public PipelineStage(PipelineStageEnum stage, ClearBufferMask mask, vec4 clear,
    List<FramebufferAttachment> inputs, List<FramebufferAttachment> outputs, WorldObject defaultObj = null, Action<RenderView> beginRenderAction = null, Action<RenderView> endRenderAction = null)
    {
      ClearMask = mask;
      ClearColor = clear;
      DefaultObj = defaultObj;
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
    public void BeginRender()
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

        //Clear all buffers
        OutputFramebuffer.Bind(FramebufferTarget.DrawFramebuffer);
        FramebufferGeneric.UnbindRenderbuffer();//_depthRenderBufferId);
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

    #endregion
    #region Private:Members

    private int _iLastWidth = 0;
    private int _iLastHeight = 0;  //Last weidth/height gotten from the screen manager.

    private List<PipelineStage> PipelineStages = new List<PipelineStage>();
    private List<FramebufferAttachment> Attachments = new List<FramebufferAttachment>();

    private Texture2D _pEnvTex = null;  //Enviro map - for mirrors (coins)
    private vec4 ClearColor { get; set; } = new vec4(0, 0, 0, 1);//(0.01953, .4114f, .8932f, 1);
    private bool _bRenderInProgress = false;
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
    public void init(int iWidth, int iHeight, FileLoc envTextureLoc)
    {
      Gu.Log.Info("[Renderer] Initializing.");
      if (iWidth <= 0 || iHeight <= 0)
      {
        Gu.BRThrowException("[Renderer] Got framebuffer of width or height < 0: " + iWidth + "," + iHeight);
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

      releaseFbosAndMesh();

      // - Setup Framebuffers.
      _iLastWidth = iWidth;
      _iLastHeight = iHeight;

      Gu.Log.Info("[Renderer] Checking Caps");
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

      //Mesh
      Gu.Log.Info("[Renderer] Creating Quad Mesh");

      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      Gpu.CheckGpuErrorsDbg();
      GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
      Gpu.CheckGpuErrorsDbg();
      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
      Gpu.CheckGpuErrorsDbg();

      Gu.Assert(samples == 0);//TODO: we need to duplicate the targets for MSAA, but we probably won't use MSAA for this game.

      Picker = new Picker(this);

      Gu.Log.Debug("[Renderer] Creating Framebuffer Attachments");
      FramebufferAttachment albedo_df = new FramebufferAttachment("Color", RenderTargetType.Color, iWidth, iHeight, samples);//names must match
      FramebufferAttachment albedo_fw = new FramebufferAttachment("Color", RenderTargetType.Color, iWidth, iHeight, samples);
      FramebufferAttachment pick = new FramebufferAttachment("Pick", RenderTargetType.Pick, iWidth, iHeight, samples);
      FramebufferAttachment normal = new FramebufferAttachment("Normal", RenderTargetType.Color, iWidth, iHeight, samples);
      FramebufferAttachment position = new FramebufferAttachment("Position", RenderTargetType.Color, iWidth, iHeight, samples);
      FramebufferAttachment plane = new FramebufferAttachment("Plane", RenderTargetType.Color, iWidth, iHeight, samples);
      FramebufferAttachment depth = new FramebufferAttachment("Depth", RenderTargetType.Depth, iWidth, iHeight, samples);

      var quadmesh = MeshData.CreateScreenQuadMesh(iWidth, iHeight);

      var dummyD = new WorldObject("dummy-blit-d");
      dummyD.Mesh = quadmesh;
      dummyD.Material = new Material("blit-d-material", Gu.Resources.LoadShader("v_v3x2_deferred", false, FileStorage.Embedded));
      dummyD.Material.GpuRenderState.CullFace = false;
      dummyD.Material.GpuRenderState.DepthTest = false;
      dummyD.Material.GpuRenderState.Blend = false;
      dummyD.Material.AlbedoSlot.Texture = albedo_df.Texture;
      dummyD.Material.NormalSlot.Texture = normal.Texture;
      dummyD.Material.PositionSlot.Texture = position.Texture;

      var dummyF = new WorldObject("dummy-blit-f");
      dummyF.Mesh = quadmesh;
      dummyF.Material = new Material("blit-f-material", Gu.Resources.LoadShader("v_v3x2_forward", false, FileStorage.Embedded));
      dummyF.Material.GpuRenderState.CullFace = false;
      dummyF.Material.GpuRenderState.DepthTest = false;
      dummyF.Material.GpuRenderState.Blend = false;
      dummyF.Material.AlbedoSlot.Texture = albedo_fw.Texture;

      Gu.Log.Debug("[Renderer] Creating Pipeline Stages");

      var deferred = new PipelineStage(PipelineStageEnum.Deferred,
        ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit, ClearColor,
        new List<FramebufferAttachment>() { },
        new List<FramebufferAttachment>() { albedo_df, pick, normal, position, plane, depth },
        null,
        (rv) =>
        {
          Gu.World.RenderDeferred(Gu.Context.Delta, rv);
        },
        (rv) =>
        {
          if (_requestSaveFBOs || Gu.EngineConfig.SaveFBOsEveryFrame)
          {
            //save fbo for each stage.
            SaveCurrentStageFBOsImmediately();
          }
        }
      );

      var deferredBlit = new PipelineStage(PipelineStageEnum.DeferredBlit,
        ClearBufferMask.None, ClearColor,
        new List<FramebufferAttachment>() { albedo_df, normal, position, plane, depth },
        new List<FramebufferAttachment>() { albedo_fw },
        dummyD
       );

      var forward = new PipelineStage(PipelineStageEnum.Forward,
        ClearBufferMask.None, ClearColor,
        new List<FramebufferAttachment>() { },
        new List<FramebufferAttachment>() { albedo_fw, pick, depth },
        null,
        (rv) =>
        {
          Gu.World.RenderForward(Gu.Context.Delta, rv);
        },
        (rv) =>
        {
          Picker.Update(rv);
          if (_requestSaveFBOs || Gu.EngineConfig.SaveFBOsEveryFrame)
          {
            //save fbo for each stage.
            SaveCurrentStageFBOsImmediately();
          }
        }
       );

      var forwardBlit = new PipelineStage(PipelineStageEnum.ForwardBlit,
        ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit, ClearColor,
        new List<FramebufferAttachment>() { albedo_fw },
        new List<FramebufferAttachment>() { }//default
        , dummyF
      );

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
    public void Render(RenderView rv, Dictionary<PipelineStageEnum, Action<double, RenderView>> stuff)
    {
      BeginEverything_New(rv);
      {
        foreach (PipelineStage ps in PipelineStages)
        {
          CurrentStage = ps;

          ps.BeginRender();
          ps.BeginRenderAction?.Invoke(rv);
          if (ps.DefaultObj != null)
          {
            rv.BeginRaster2D();
            {
              //    Gu.BreakRenderState = true;
              DrawCall.Draw(Gu.World.WorldProps, rv, ps.DefaultObj);
            }
            rv.EndRaster2D();
          }
          ps.EndRenderAction?.Invoke(rv);
          ps.EndRender();
        }
        CurrentStage = null;
        _requestSaveFBOs = false;
      }
      EndEverything_New(rv);
    }
    public void BeginEverything_New(RenderView rv)
    {
      Gu.Assert(RenderState == RendererState.End || RenderState == RendererState.None);
      RenderState = RendererState.Begin;
      rv.BeginRender3D();
      Gu.Context.DebugDraw.BeginFrame();
      SetInitialGpuRenderState();
      Gpu.CheckGpuErrorsDbg();
      //  _pMsaaForward.ClearSharedFb();//Must call before deferred. After Picker.
      //PipelineStage = PipelineStageEnum.Unset;
    }
    public void EndEverything_New(RenderView rv)
    {
      Gu.Assert(RenderState == RendererState.Begin);
      RenderState = RendererState.End;
      Gu.Context.GameWindow.Context.SwapBuffers();
      rv.EndRender3D();
      Gu.Context.DebugDraw.EndFrame();
      //PipelineStage = PipelineStageEnum.Unset;
    }

    #endregion
    #region Private:Methods

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
      int iMaxDrawBuffers;
      int iMaxFbWidth;
      int iMaxFbHeight;
      //TODO : check to see if this includes a depth MRT
      GL.GetInteger(GetPName.MaxDrawBuffers, out iMaxDrawBuffers);

      if (iMaxDrawBuffers < 1)
      {
        Gu.BRThrowException("[Renderer] Your GPU only supports " + iMaxDrawBuffers +
                         " MRTs, the system requires at least " + 1 +
                         " MRTs. Consider upgrading graphics card.");
      }

      //uuh..

      //  GL.GetInteger(GetIndexedPName.width, (GLint*)&iMaxFbWidth);
      // GL.GetInteger(GL_MAX_FRAMEBUFFER_HEIGHT, (GLint*)&iMaxFbHeight);

      // if (iMaxFbHeight < iHeight || iMaxFbWidth < iWidth)
      // {
      //   BRThrowException("[Renderer] Your GPU only supports MRTs at " +
      //                    iMaxFbWidth + "x" + iMaxFbHeight +
      //                    " pixels. The system requested " + iWidth + "x" + iHeight + ".");
      // }

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
    private void DebugSaveAllFBOs(string tag)
    {
      if (Gu.EngineConfig.SaveAllFBOsEveryStageOfPipeline)
      {
        SaveCurrentStageFBOsImmediately(tag);
      }
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
    private void SaveCurrentStageFBOsImmediately(string tag = "")
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


      string prefix = Gu.GetFilenameDateTimeNOW() + "_" + this.CurrentStage.Name + "_" + tag + "_context-" + ctxName;

      //ok so pick isn't getting savd..
      //GetAllUniqueAttachments();

      //using input framebuffer as we call saveFBOs in the blit routine (which, uses inputs)
      if (this.CurrentStage.OutputFramebuffer != null)
      {
        foreach (var bind in this.CurrentStage.OutputFramebuffer.Bindings)// atts)
        {
          var pTarget = bind.Attachment;
          string fname = prefix + "_" + pTarget.Texture.Name + "_" + bind.LayoutIndex + "_.png";//names may be the same
          fname = System.IO.Path.Combine(Gu.LocalCachePath, fname);
          ResourceManager.SaveTexture(new FileLoc(fname, FileStorage.Disk), pTarget.Texture, true);
          Gu.Log.Info("[Renderer] Screenshot '" + fname + "' saved");
        }
      }
    }

    #endregion














  }

}
