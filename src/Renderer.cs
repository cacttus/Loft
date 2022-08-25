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

  public enum PipelineStage
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
        Gu.Assert(renderer.CurrentPipelineFramebuffer != null);
        renderer.CurrentPipelineFramebuffer.Bind(FramebufferTarget.ReadFramebuffer);
        var pick = renderer.CurrentPipelineFramebuffer.GetTargetByName(FramebufferBase.c_strPickMRT_DF);
        var readbufferMode = FramebufferBase.AttachmentIndexToReadBufferMode(pick._eAttachment);
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

  public class Renderer : HasGpuResources
  {
    #region Public:Members

    public ShaderControlVars DefaultControlVars = new ShaderControlVars();
    public RendererState RenderState { get; private set; } = RendererState.None;
    public PipelineStage PipelineStage { get; private set; } = PipelineStage.Unset;
    public Picker Picker { get; private set; } = null;
    public FramebufferBase CurrentPipelineFramebuffer { get; private set; } = null;

    #endregion
    #region Private:Members

    private bool _bMsaaEnabled = false;
    private int _nMsaaSamples = 0;
    private MeshData _pQuadMesh = null;

    private WorldObject _dummyForward = null;
    private WorldObject _dummyDeferred = null;

    private int _iLastWidth = 0;
    private int _iLastHeight = 0;  //Last weidth/height gotten from the screen manager.

    private FramebufferAttachment _pMsaaDepth = null;
    private FramebufferAttachment _pBlittedDepth = null;
    private FramebufferAttachment _pPick = null;
    // RenderTarget _pPickDepth = null;

    private DeferredFramebuffer _pMsaaDeferred = null;  //If no multisampling is enabled this is equal to the blittedFramebuffer object
    private DeferredFramebuffer _pBlittedDeferred = null;

    private ForwardFramebuffer _pMsaaForward = null;
    private ForwardFramebuffer _pBlittedForward = null;

    // private ShadowBox _pShadowBoxFboMaster = null;
    // private ShadowFrustum _pShadowFrustumMaster = null;
    // private DOFFbo _pDOFFbo = null;
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
      _bMsaaEnabled = Gu.EngineConfig.EnableMSAA;// Gu::getEngineConfig().getEnableMSAA();
      _nMsaaSamples = Gu.EngineConfig.MSAASamples;
      _iLastWidth = iWidth;
      _iLastHeight = iHeight;

      Gu.Log.Info("[Renderer] Checking Caps");
      CheckDeviceCaps(iWidth, iHeight);

      if (_bMsaaEnabled)
      {
        GL.Enable(EnableCap.Multisample);
        Gpu.CheckGpuErrorsRt();
      }

      //Mesh
      Gu.Log.Info("[Renderer] Creating Quad Mesh");

      //Shaders
      //   Gu.BRThrowNotImplementedException();
      //if (_pDeferredShader == null)
      //{
      //  _pDeferredShader = Gu::getShaderMaker().makeShader(List<string>{
      //    "d_v3x2_lighting.vs", "d_v3x2_lighting.ps"});
      //}
      //if (_pForwardShader == null)
      //{
      //  _pForwardShader = Gu::getShaderMaker().makeShader(List<string>{
      //    "f_v3x2_fbo.vs", "f_v3x2_fbo.ps"});
      //}

      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      Gpu.CheckGpuErrorsDbg();
      GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
      Gpu.CheckGpuErrorsDbg();
      GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
      Gpu.CheckGpuErrorsDbg();

      //Guess what?  Headache time.
      //This deletion of the shared stuff is super important 2/9/18
      //this must come before all the code below.  In short: RenderTarget automatically deletes its texure
      //TODO: in the future remove deferred/forward targets.  Make the render targets refer to a shared_ptr Texture2DSpec so that
      //deletion of the texture is natural when it gets unreferenced.  Make the creation of all render target textures be independent
      //of their framebuffers so that we can share them across multiple render stages.
      _pMsaaDeferred = null;
      _pMsaaForward = null;
      _pMsaaDepth = null;
      _pBlittedDepth = null;
      _pBlittedForward = null;
      _pBlittedDeferred = null;
      _pPick = null;
      //_pDOFFbo = null;

      //Base FBOs
      _pBlittedDepth = FramebufferBase.CreateTarget(FramebufferBase.c_strBlittedDepthMRT_DF, iWidth, iHeight, RenderTargetType.Depth, 0, false, 0, null, null, null);

      //Do not cahnge "Pick" name.  This is shared.
      _pPick = FramebufferBase.CreateTarget(FramebufferBase.c_strPickMRT_DF, iWidth, iHeight, RenderTargetType.Pick, 0, false, 0, PixelInternalFormat.R32ui, PixelFormat.RedInteger, PixelType.UnsignedInt);  //4

      _pBlittedDeferred = new DeferredFramebuffer(_bMsaaEnabled, 0, ClearColor);
      _pBlittedDeferred.Init(iWidth, iHeight, _pBlittedDepth, _pPick);

      _pBlittedForward = new ForwardFramebuffer(_bMsaaEnabled, 0, ClearColor);
      _pBlittedForward.Init(iWidth, iHeight, _pBlittedDepth, _pPick);

      _pQuadMesh = MeshData.CreateScreenQuadMesh(iWidth, iHeight);

      //Pick
      Picker = new Picker(this);

      //Multisamplecell
      if (_bMsaaEnabled == true)
      {
        Gu.Log.Info("[Renderer] Creating deferred MSAA lighting buffer");
        _pMsaaDepth = FramebufferBase.CreateTarget(FramebufferBase.c_strBlittedDepthMRT_DF, iWidth, iHeight, RenderTargetType.Depth, 0, _bMsaaEnabled, _nMsaaSamples, null, null, null);
        _pMsaaDeferred = new DeferredFramebuffer(true, _nMsaaSamples, ClearColor);
        _pMsaaDeferred.Init(iWidth, iHeight, _pMsaaDepth, _pPick);  // Yeah I don't know if the "pick" here will work
        _pMsaaForward = new ForwardFramebuffer(true, _nMsaaSamples, ClearColor);
        _pMsaaForward.Init(iWidth, iHeight, _pMsaaDepth, _pPick);  // Yeah I don't know if the "pick" here will work
      }
      else
      {
        Gu.Log.Info("[Renderer] Multisample not enabled.");
        _pMsaaDeferred = _pBlittedDeferred;
        _pMsaaForward = _pBlittedForward;
        _pMsaaDepth = _pBlittedDepth;
      }

      _dummyForward = new WorldObject("dummyforward");
      _dummyForward.Material = new Material("forwardMaterial", Gu.Resources.LoadShader("v_v3x2_forward", false, FileStorage.Embedded));
      _dummyForward.Material.GpuRenderState.CullFace = false;
      _dummyForward.Material.GpuRenderState.DepthTest = false;
      _dummyForward.Material.GpuRenderState.Blend = false;
      _dummyForward.Material.AlbedoSlot.Texture = _pMsaaForward.GetTargetByName(ForwardFramebuffer.c_strColorMRT_FW)._texture;
      _dummyForward.Mesh = _pQuadMesh;

      _dummyDeferred = new WorldObject("dummydeferred");
      _dummyDeferred.Material = new Material("deferredMaterial", Gu.Resources.LoadShader("v_v3x2_deferred", false, FileStorage.Embedded));
      _dummyDeferred.Material.GpuRenderState.CullFace = false;
      _dummyDeferred.Material.GpuRenderState.DepthTest = false;
      _dummyDeferred.Material.GpuRenderState.Blend = false;
      _dummyDeferred.Material.AlbedoSlot.Texture = _pMsaaDeferred.GetTargetByName(DeferredFramebuffer.c_strColorMRT_DF)._texture;
      _dummyDeferred.Material.NormalSlot.Texture = _pMsaaDeferred.GetTargetByName(DeferredFramebuffer.c_strNormalMRT_DF)._texture;
      _dummyDeferred.Material.PositionSlot.Texture = _pMsaaDeferred.GetTargetByName(DeferredFramebuffer.c_strPositionMRT_DF)._texture;
      _dummyDeferred.Material.RoughnessSlot.Texture = _pMsaaDeferred.GetTargetByName(DeferredFramebuffer.c_strPlaneMRT_DF)._texture;
      _dummyDeferred.Mesh = _pQuadMesh;

      //These are here SOLELY for shadow map blending.
      //If we don't do any shadow blending then these are useless.
      int iShadowMapRes = Gu.EngineConfig.ShadowMapResolution;
      //_pShadowBoxFboMaster = new ShadowBox(null, iShadowMapRes, iShadowMapRes);
      //_pShadowBoxFboMaster.init();
      //_pShadowFrustumMaster = new ShadowFrustum(null, iShadowMapRes, iShadowMapRes);
      //_pShadowFrustumMaster.init();

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
    public void BeginEverything_New(RenderView rv)
    {
      Gu.Assert(RenderState == RendererState.End || RenderState == RendererState.None);
      RenderState = RendererState.Begin;
      CurrentPipelineFramebuffer = _pMsaaDeferred;
      rv.BeginRender3D();
      Gu.Context.DebugDraw.BeginFrame();
      SetInitialGpuRenderState();
      Gpu.CheckGpuErrorsDbg();
      Picker.Update(rv);
      _pMsaaForward.ClearSharedFb();//Must call before deferred. After Picker.
      PipelineStage = PipelineStage.Unset;
    }
    public void EndEverything_New(RenderView rv)
    {
      Gu.Assert(RenderState == RendererState.Begin);
      RenderState = RendererState.End;
      Gu.Context.GameWindow.Context.SwapBuffers();
      CurrentPipelineFramebuffer = null;
      rv.EndRender3D();
      Gu.Context.DebugDraw.EndFrame();
      PipelineStage = PipelineStage.Unset;
    }
    public void BeginRenderDeferred()
    {
      EnableDisablePipeBits();
      CurrentPipelineFramebuffer = _pMsaaDeferred;

      DebugSaveAllFBOs("BeginRenderDeferred1");
      _pMsaaDeferred.BeginRender();
      PipelineStage = PipelineStage.Deferred;
      DebugSaveAllFBOs("BeginRenderDeferred2");
    }
    public void EndRenderDeferred()
    {
      _pMsaaDeferred.EndRender();
      DebugSaveAllFBOs("EndRenderDeferred");
      PipelineStage = PipelineStage.Unset;
    }
    public void BeginRenderForward()
    {
      DebugSaveAllFBOs("BeginRenderForward1");
      _pMsaaForward.BeginRender();
      CurrentPipelineFramebuffer = _pMsaaForward;
      DebugSaveAllFBOs("BeginRenderForward2");
      PipelineStage = PipelineStage.Forward;
    }
    public void EndRenderForward()
    {
      DebugSaveAllFBOs("EndRenderForward1");
      _pMsaaForward.EndRender();
      DebugSaveAllFBOs("EndRenderForward2");
      PipelineStage = PipelineStage.Unset;
    }
    public void BlitDeferredRender(RenderView rv)
    {
      PipelineStage = PipelineStage.DeferredBlit;
      //NOTE:
      //Bind the forward framebuffer (_pBlittedForward is equal to _pMsaaForward if MSAA is disabled, if it isn't we call copyMSAASamples later)
      GL.UseProgram(0);

      GL.BindFramebuffer(FramebufferTarget.Framebuffer, _pMsaaForward.GetGlId());
      Gpu.CheckGpuErrorsDbg();
      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      Gpu.CheckGpuErrorsDbg();

//Disable writes to pick (debug)

      rv.BeginRaster2D();
      {
        //  //*The clear here isn't necessary. If we're copying all of the contents of the deferred buffer.
        //  // - Clear the color and depth buffers (back and front buffers not the Mrts)
        //  //glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        //  BindDeferredTargets(true);
        {
          //Set the light uniform blocks for the deferred shader.
          //_pDeferredShader.setLightUf(lightman);
          SetShadowEnv(/*lightman,*/ true);
          {
            if (_requestSaveFBOs || Gu.EngineConfig.SaveFBOsEveryFrame)
            {
              SaveAllFBOsImmediately("BlitDeferredRender");
            }
            //    Gu.BreakRenderState = true;
            DrawCall.Draw(Gu.World.WorldProps, rv, _dummyDeferred);
          }
          SetShadowEnv(/*lightman,*/ false);  //Fix this, we should be able to clear the texture units before the next operation.
        }
        //  BindDeferredTargets(false);
      }
      rv.EndRaster2D();

      PipelineStage = PipelineStage.Unset;
    }
    public void BlitFinalRender(RenderView rv)
    {
      PipelineStage = PipelineStage.ForwardBlit;
      //Blits the final deferred Color image (after our deferred rendering step) to a quad.
      //Do not bind anything - default framebuffer.
      //Gu::getShaderMaker().shaderBound(null);  //Unbind and reset shader.
      GL.UseProgram(0);
      GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);//switch to default framebuffer
      Gpu.CheckGpuErrorsDbg();
      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);//switch to default depth buffer
      Gpu.CheckGpuErrorsDbg();

      // - Clear the DEFAULT color and depth buffers 
      GL.ClearColor(ClearColor.r, ClearColor.g, ClearColor.b, ClearColor.a);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      Gpu.CheckGpuErrorsDbg();

      rv.BeginRaster2D();
      {
        if (_requestSaveFBOs || Gu.EngineConfig.SaveFBOsEveryFrame)
        {
          SaveAllFBOsImmediately("BlitFinalRender");
          _requestSaveFBOs = false;
        }
        Gpu.CheckGpuErrorsDbg();

        //_dummyForward.Material.Textures[TextureInput.Albedo] = _ufGpuMaterial_s2Albedo
        DrawCall.Draw(Gu.World.WorldProps, rv, _dummyForward);
      }
      rv.EndRaster2D();
      PipelineStage = PipelineStage.Unset;

      DebugSaveAllFBOs("EBlitFinalRender2");
    }
    //protected void renderShadows(LightManager lightman, CameraNode cam)
    //{
    //  lightman.update(_pShadowBoxFboMaster, _pShadowFrustumMaster);
    //  Gu::checkErrorsDbg();

    //  //Force refresh teh viewport.
    //  cam.getViewport().bind();
    //****Pipeline stage ** 
    //}
    //protected void beginRenderShadows()
    //{
    //  Gu::checkErrorsDbg();
    //  //See GLLightManager in BRO
    //  getContext().pushDepthTest();
    //  getContext().pushBlend();
    //  getContext().pushCullFace();
    //  glCullFace(GL_FRONT);
    //  glEnable(GL_DEPTH_TEST);
    //  glEnable(GL_CULL_FACE);
    //  glDisable(GL_BLEND);
    //  Gu::checkErrorsDbg();
    //****Pipeline stage ** 
    //}
    //protected void endRenderShadows()
    //{
    //  Gu::checkErrorsDbg();
    //  glCullFace(GL_BACK);
    //  getContext().popDepthTest();
    //  getContext().popBlend();
    //  getContext().popCullFace();
    //  Gu::checkErrorsDbg();
    //****Pipeline stage ** 
    //}
    //protected void postProcessDOF(LightManager lightman, CameraNode cam)
    //{
    //  //If MSAA is enabled downsize the MSAA buffer to the _pBlittedForward buffer so we can execute post processing.
    //  //copyMsaaSamples(_pMsaaForward, _pBlittedForward);

    //  if (Gu::getRenderSettings().getDOF())
    //  {
    //    ShaderBase pDofShader = Gu::getShaderMaker().getDepthOfFieldShader();
    //    if (pDofShader == null || cam == null)
    //    {
    //      Gu.Log.ErrorCycle("Error: nulls 348957");
    //      return;
    //    }
    //    vec3 pos = cam.getFinalPos();
    //    FramebufferAttachment rtPos = _pMsaaDeferred.getTargetByName(DeferredFramebuffer::c_strPositionMRT_DF);
    //    FramebufferAttachment rtColor = _pMsaaForward.getTargetByName(ForwardFramebuffer::c_strColorMRT_FW);  //**Note** Forward

    //    if (rtPos == null || rtColor == null)
    //    {
    //      Gu.Log.ErrorCycle("oen or more Render targets were null");
    //      return;
    //    }

    //    //Blend color + position and store it in the color.
    //    pDofShader.BeginRaster2D(cam.getViewport());
    //    {
    //      //This could be removed if we used Texture2DSpec for the RenderTarget texturs..
    //      GLuint i0;
    //      GL.ActiveTexture(GL_TEXTURE0);
    //      GL.BindTexture(GL_TEXTURE_2D, rtPos.getGlTexId());
    //      i0 = 0;
    //      pDofShader.setUf("_ufTextureId_i0", (GLvoid*)&i0);

    //      //This could be removed if we used Texture2DSpec for the RenderTarget texturs..
    //      GL.ActiveTexture(GL_TEXTURE1);
    //      GL.BindTexture(GL_TEXTURE_2D, rtColor.getGlTexId());
    //      i0 = 1;
    //      pDofShader.setUf("_ufTextureId_i1", (GLvoid*)&i0);

    //      //Camera params
    //      //pDofShader.setUf("_fFocalDepth", (GLvoid*)&Gu::getLightManager().getDeferredParams()._fFocalDepth);
    //      pDofShader.setUf("_fFocalRange", (GLvoid*)&lightman.getDeferredParams()._fFocalRange);
    //      //View pos
    //      pDofShader.setUf("_ufViewPos", (void*)&pos);

    //      //Draw Round 1
    //      GL.BindFramebuffer(GL_DRAW_FRAMEBUFFER, _pDOFFbo._uiDOFFboId);
    //      GL.FramebufferTexture2D(GL_DRAW_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _pDOFFbo._uiTexId0, 0);
    //      GLint horiz = 0;
    //      pDofShader.setUf("_ufHorizontal", (GLvoid*)&horiz);
    //      pDofShader.draw(_pQuadMesh);
    //      Gu::checkErrorsDbg();

    //      //Draw Round 2
    //      //Bind rtColor back to the output.
    //      GL.BindFramebuffer(GL_DRAW_FRAMEBUFFER, _pDOFFbo._uiDOFFboId);
    //      GL.FramebufferTexture2D(GL_DRAW_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, rtColor.getGlTexId(), 0);

    //      //Bind the previously rendered color to the color buffer so we can pingpong it back.
    //      GL.ActiveTexture(GL_TEXTURE1);
    //      GL.BindTexture(GL_TEXTURE_2D, _pDOFFbo._uiTexId0);

    //      i0 = 1;
    //      pDofShader.setUf("_ufTextureId_i1", (GLvoid*)&i0);

    //      horiz = 1;
    //      pDofShader.setUf("_ufHorizontal", (GLvoid*)&horiz);
    //      pDofShader.draw(_pQuadMesh);

    //      Gu::checkErrorsDbg();
    //    }
    //    pDofShader.endRaster();
    //    Gu::checkErrorsDbg();

    //    //Unbind / Delete
    //    GL.BindFramebuffer(GL_DRAW_FRAMEBUFFER, 0);
    //    GL.ActiveTexture(GL_TEXTURE0);
    //    GL.BindTexture(GL_TEXTURE_2D, 0);
    //    GL.ActiveTexture(GL_TEXTURE1);
    //    GL.BindTexture(GL_TEXTURE_2D, 0);
    //  }
    //}

    #endregion
    #region Private:Methods

    private void EnableDisablePipeBits()
    {
      //TODO: make sure the given input window is in focus.
      //if (_pWindow!=null && _pWindow.hasFocus()) {
      //    if (Gu::getFingers().keyPress(SDL_SCANCODE_F8)) {
      //        Gu::incrementEnum<PipeBit::e>(_pipeBits, PipeBit::e::MaxPipes);
      //        if (_ePipeBit == PipeBit::e::Full) {
      //            _pipeBits.set();
      //        }
      //        else {
      //            _pipeBits.reset();
      //            _pipeBits.set(_ePipeBit);
      //        }
      //    }
      //}
    }
    private void CheckMultisampleParams()
    {
      int iMaxSamples;
      GL.GetInteger(GetPName.MaxSamples, out iMaxSamples);
      Gu.Log.Info("Max OpenGL MSAA Samples " + iMaxSamples);

      if (_bMsaaEnabled)
      {
        if ((int)_nMsaaSamples > iMaxSamples)
        {
          Gu.Log.Warn("[Renderer] MSAA sample count of '" + _nMsaaSamples +
                    "' was larger than the card's maximum: '" + iMaxSamples + "'. Truncating.");
          _nMsaaSamples = iMaxSamples;
          Gu.DebugBreak();
        }
        if (!MathUtils.IsPowerOfTwo(_nMsaaSamples))
        {
          Gu.Log.Warn("[Renderer] Error, multisampling: The number of samples must be 2, 4, or 8.  Setting to 2.");
          _nMsaaSamples = iMaxSamples > 2 ? 2 : iMaxSamples;
          Gu.DebugBreak();
        }
      }
    }
    private void CheckDeviceCaps(int iWidth, int iHeight)
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

      CheckMultisampleParams();
    }
    private void copyMsaaSamples(FramebufferBase msaa, FramebufferBase blitted)
    {
      //Downsize the MSAA sample buffer.
      if (_bMsaaEnabled)
      {
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        Gpu.CheckGpuErrorsDbg();

        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, msaa.GetGlId());
        Gpu.CheckGpuErrorsDbg();
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, blitted.GetGlId());
        Gpu.CheckGpuErrorsDbg();

        foreach (var inf in msaa.Targets)
        {
          BlitFramebufferFilter blendMode;
          ClearBufferMask bitMask;
          if (inf.getTargetType() == RenderTargetType.Depth)
          {
            //GL_LINEAR is only a valid interpolation method for the color buffer.
            blendMode = BlitFramebufferFilter.Nearest;
          }
          else
          {
            GL.ReadBuffer((ReadBufferMode)inf.getAttachment()); // This is the same enum in most cases
            Gpu.CheckGpuErrorsDbg();
            GL.DrawBuffer((DrawBufferMode)inf.getAttachment());
            Gpu.CheckGpuErrorsDbg();
            blendMode = BlitFramebufferFilter.Linear;
          }

          //GL_DEPTH_BUFFER_BIT 0x00000100
          //GL_COLOR_BUFFER_BIT  0x00004000
          bitMask = inf.getBlitBit();

          //GL_DEPTH_BUFFER_BIT;
          GL.BlitFramebuffer(0, 0, _iLastWidth, _iLastHeight, 0, 0, _iLastWidth, _iLastHeight, bitMask, blendMode);
          Gpu.CheckGpuErrorsDbg();
        }
        Gpu.CheckGpuErrorsDbg();
      }
    }
    private void releaseFbosAndMesh()
    {
      GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
      _pBlittedDeferred = null;
      _pBlittedForward = null;
      _pBlittedDepth = null;
      if (_bMsaaEnabled)
      {
        _pMsaaForward = null;
        _pMsaaDeferred = null;
        _pMsaaDepth = null;
      }

      //  _pShadowBoxFboMaster = null;
      //  _pShadowFrustumMaster = null;
    }
    private void SetShadowEnv(/*LightManager lightman, */bool bSet)
    {
      //@param bSet - Set Shadow Textures and Uniforms, and ENV map for the GBUFFER. False: Clear texture slots.
      //Set Shadow and Environment map Uniforms.
      // int iMaxTexs = getContext().maxGLTextureUnits();

      ////TEST
      ////This is to get the lights to work again.
      //BRLogTODO("Disabling ALL Shadowboxes here...");
      //bool DISABLE_ALL_SHADOW_BOXES_AND_SUCH = true;
      ////TEST

      //int iIndex = 0;

      ////We loop this way because we MUST fill all texture units used by the GPU.
      //List<GLint> boxSamples;
      //List<GLint> frustSamples;
      //int iNumGpuShadowBoxes = Gu::getEngineConfig().getMaxCubeShadowSamples();
      //int iNumGpuShadowFrustums = Gu::getEngineConfig().getMaxFrustShadowSamples();

      //if (lightman.getGpuShadowBoxes().size() > iNumGpuShadowBoxes)
      //{
      //  Gu.Log.WarnCycle("More than " + iNumGpuShadowBoxes + " boxes - some shadows will not show.");
      //}
      //for (int iShadowBox = 0; iShadowBox < iNumGpuShadowBoxes; ++iShadowBox)
      //{
      //  //Starting after the textures in the GBuffer.
      //  int iTextureIndex = _pMsaaDeferred.getNumNonDepthTargets() + iIndex;

      //  if (iTextureIndex < iMaxTexs)
      //  {
      //    //Ok, so we are using textureSize in the pixel shader to indicate that the shadow here is "used"
      //    GLuint texId = 0;  // Leave to zero to clear the texture slot
      //    if (bSet)
      //    {
      //      if (!DISABLE_ALL_SHADOW_BOXES_AND_SUCH && iShadowBox < lightman.getGpuShadowBoxes().size())
      //      {
      //        ShadowBox pBox = lightman.getGpuShadowBoxes()[iShadowBox];
      //        if (pBox != null)
      //        {
      //          texId = pBox.getGlTexId();
      //        }
      //        else
      //        {
      //          texId = Gu::getTexCache().getDummy1x1TextureCube();
      //        }
      //      }
      //      else
      //      {
      //        texId = Gu::getTexCache().getDummy1x1TextureCube();
      //      }
      //      boxSamples.push_back(iTextureIndex);
      //    }
      //    GL.ActiveTexture(GL_TEXTURE0 + iTextureIndex);
      //    Gu::checkErrorsDbg();
      //    GL.BindTexture(GL_TEXTURE_CUBE_MAP, texId);
      //    Gu::checkErrorsDbg();
      //    iIndex++;
      //  }
      //  else
      //  {
      //    Gu.Log.Warn("Deferred Step: Too many textures bound: " + iTextureIndex);
      //  }
      //}
      //if (bSet)
      //{
      //  _pDeferredShader.setUf("_ufShadowBoxSamples", boxSamples.data(), (GLint)boxSamples.size());
      //  Gu::checkErrorsDbg();
      //}
      ////We loop this way because we MUST fill all texture units used by the GPU.
      //if (lightman.getGpuShadowBoxes().size() > iNumGpuShadowFrustums)
      //{
      //  Gu.Log.WarnCycle("More than " + iNumGpuShadowFrustums + " frustum - some shadows will not show.");
      //}
      //for (int iShadowFrustum = 0; iShadowFrustum < iNumGpuShadowFrustums; ++iShadowFrustum)
      //{
      //  int iTextureIndex = _pMsaaDeferred.getNumNonDepthTargets() + iIndex;
      //  if (iTextureIndex < iMaxTexs)
      //  {
      //    GLuint texId = 0;
      //    if (bSet)
      //    {
      //      if (!DISABLE_ALL_SHADOW_BOXES_AND_SUCH && iShadowFrustum < lightman.getGpuShadowFrustums().size())
      //      {
      //        ShadowFrustum pFrust = lightman.getGpuShadowFrustums()[iShadowFrustum];
      //        if (pFrust != null)
      //        {
      //          texId = pFrust.getGlTexId();
      //        }
      //        else
      //        {
      //          texId = Gu::getTexCache().getDummy1x1Texture2D();
      //        }
      //      }
      //      else
      //      {
      //        texId = Gu::getTexCache().getDummy1x1Texture2D();
      //      }
      //      frustSamples.push_back(iTextureIndex);
      //    }

      //    GL.ActiveTexture(GL_TEXTURE0 + iTextureIndex);
      //    Gu::checkErrorsDbg();
      //    GL.BindTexture(GL_TEXTURE_2D, texId);
      //    Gu::checkErrorsDbg();
      //    iIndex++;
      //  }
      //  else
      //  {
      //    Gu.Log.Warn("Deferred Step: Too many textures bound: " + iTextureIndex);
      //  }
      //}
      //if (bSet)
      //{
      //  _pDeferredShader.setUf("_ufShadowFrustumSamples", frustSamples.data(), (GLint)frustSamples.size());
      //  Gu::checkErrorsDbg();
      //}

      //Set Mirror Environment map.
      //if (_pEnvTex != null)
      //{
      //  int iTextureIndex = _pMsaaDeferred.getNumNonDepthTargets() + iIndex;
      //  if (iTextureIndex < iMaxTexs)
      //  {
      //    GL.ActiveTexture(GL_TEXTURE0 + iTextureIndex);
      //    if (bSet)
      //    {
      //      GL.BindTexture(GL_TEXTURE_2D, _pEnvTex.getGlId());
      //      _pDeferredShader.setUf("_ufTexEnv0", (GLvoid*)&iTextureIndex);
      //    }
      //    else
      //    {
      //      GL.BindTexture(GL_TEXTURE_2D, 0);
      //    }
      //    iIndex++;
      //  }
      //  else
      //  {
      //    Gu.Log.Warn("Deferred Step: Too many textures bound: " + iTextureIndex);
      //  }
      //}
      //else
      //{
      //  Gu.Log.Warn("You didn't set the enviro texture.");
      //  Gu::debugBreak();
      //}
      //Gu::checkErrorsDbg();
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
        SaveAllFBOsImmediately(tag);
      }
    }
    private void SaveAllFBOsImmediately(string tag = "")
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

      string prefix = tag + "_context-" + ctxName + "_" + Gu.GetFilenameDateTimeNOW();
      iTarget = 0;
      foreach (FramebufferAttachment pTarget in _pBlittedDeferred.Targets)
      {
        string fname = prefix + "_deferred_" + pTarget.getName() + "_" + iTarget++ + "_.png";
        fname = System.IO.Path.Combine(Gu.LocalCachePath, fname);
        ResourceManager.SaveTexture(new FileLoc(fname, FileStorage.Disk), pTarget._texture, true);
        Gu.Log.Info("[Renderer] Screenshot '" + fname + "' saved");
      }
      iTarget = 0;
      foreach (FramebufferAttachment pTarget in _pBlittedForward.Targets)
      {
        string fname = prefix + "_forward_" + pTarget.getName() + "_" + iTarget++ + "_.png";
        fname = System.IO.Path.Combine(Gu.LocalCachePath, fname);
        ResourceManager.SaveTexture(new FileLoc(fname, FileStorage.Disk), pTarget._texture, true);
        Gu.Log.Info("[Renderer] Screenshot '" + fname + "' saved");
      }
    }

    #endregion














  }

}
