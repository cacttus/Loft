using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace PirateCraft
{
  [StructLayout(LayoutKind.Sequential)]
  public struct GLInstance
  {
    public mat4 model_matrix;
  }

  public class Picker : OpenGLResource
  {
    //**Note do not set this to be anything but full alpha, if blending is enabled teh blend will screw up this value.
    public const uint c_iInvalidPickId = 0;//0xFFFFFFFF;

    private uint _iid = 0;
    private WeakReference<Renderer> _pRenderer ;
    private uint _uiLastSelectedPixelId = 0;//Note: This is relative to the last UserSelectionSet - the Id here is not fixed.
    public uint getSelectedPixelId() { return _uiLastSelectedPixelId; }

    public Picker(Renderer rp)
    {
      _pRenderer = new WeakReference<Renderer>(rp);
    }
    public override void Dispose_OpenGL_RenderThread()
    {
      //none
    }
    public void update()
    {
      updatePickedPixel((int)Gu.Mouse.Pos.x, (int)Gu.Mouse.Pos.y);
    }
    public uint genPickId()
    {
      //DEBUG pick ID that shows the color of the picked object.
      _iid++;
      if (_iid == Picker.c_iInvalidPickId)
      {
        _iid = 0;
      }

      return _iid;
    }

    private void updatePickedPixel(int x, int y)
    {
      //vec2 mp = Gu::GetMousePosInWindow();
      //if (!Gu::GetRenderManager().getViewport().containsPointRelativeToWindow(mp)){
      //    return;
      //}

      //getContext().debugGetRenderState();

      if (_pRenderer.TryGetTarget(out var renderer))
      {
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, renderer.getBlittedDeferred().getFramebufferId());
        Gpu.CheckGpuErrorsDbg();
        //**UHH..
        //Gu.DebugBreak();//?
        GL.ReadBuffer(ReadBufferMode.ColorAttachment4); //Note if you change this you must change the pick index in DeferredFramebuffer
        Gpu.CheckGpuErrorsDbg();

        //getContext().debugGetRenderState();

        samplePixelId(x, y, out _uiLastSelectedPixelId);
        Gpu.CheckGpuErrorsDbg();

#if DEBUG

        if (_uiLastSelectedPixelId > 0)
        {

          if (Gu.Context.FrameStamp % 20 == 0)
          {
            Gu.Log.Debug("(" + x + "," + y + "), picked " + _uiLastSelectedPixelId);
          }
        }

        Gpu.CheckGpuErrorsDbg();

#endif

        GL.ReadBuffer(ReadBufferMode.None);
        Gpu.CheckGpuErrorsDbg();
      }
    }
    private void samplePixelId(int x, int y, out uint selectedId)
    {
      uint pixel = 0;

      //https://www.khronos.org/opengles/sdk/docs/man/xhtml/glReadPixels.xml
      //If the currently bound framebuffer is not the default framebuffer object, color components
      // are read from the color image attached to the GL_COLOR_ATTACHMENT0 attachment point.
      //getContext().debugGetRenderState();

      // BRLogTODO("Serious hack right here- fix it");
      if (Gu.World.Camera != null)
      {
        int iHeight = Gu.World.Camera.Viewport_Height;

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
      selectedId = pixel;
    }
  }
  //Rendering pipeline
  //Sets up context, passes, FBOs, and renders to multiple cameras.
  // pipeline . begin (FBO, pass)
  //  camera . begin (viewport, clip, matrices)
  //   material . begin (gpu state)
  //    shader . begin (bind uniforms, textures)
  //     draw elements
  //  unbind so we don't mess up the state.. 
  public class Renderer : OpenGLResource
  {
    public enum RendererlineState
    {
      None,
      Begin,
      End
    }
    public enum RenderPass
    {
      Color,
      Depth1,
      Depth2
    }
    public RendererlineState RenderState { get; private set; } = RendererlineState.None;

    public Picker getPicker() { return _pPicker; }
    //  public GraphicsWindow getWindow() { return _pWindow; }
    public int getBufferWidth() { return _iLastWidth; }
    public int getBufferHeight() { return _iLastHeight; }

    protected bool _bMsaaEnabled = false;
    protected int _nMsaaSamples = 0;
    protected MeshData _pQuadMesh = null;

    protected Material _forwardMaterial = null;
    //protected Shader _pForwardShader = null;

    // protected Shader _pDeferredShader = null;
    protected Material _deferredMaterial = null;

    protected int _iLastWidth, _iLastHeight;  //Last weidth/height gotten from the screen manager.

    protected BufferRenderTarget _pMsaaDepth = null;
    protected BufferRenderTarget _pBlittedDepth = null;
    protected BufferRenderTarget _pPick = null;
    // RenderTarget _pPickDepth = null;

    protected DeferredFramebuffer _pMsaaDeferred = null;  //If no multisampling is enabled this is equal to the blittedFramebuffer object
    protected DeferredFramebuffer _pBlittedDeferred = null;

    protected ForwardFramebuffer _pMsaaForward = null;
    protected ForwardFramebuffer _pBlittedForward = null;

    // protected ShadowBox _pShadowBoxFboMaster = null;
    // protected ShadowFrustum _pShadowFrustumMaster = null;
    // protected DOFFbo _pDOFFbo = null;
    protected Texture2D _pEnvTex = null;  //Enviro map - for mirrors (coins)
    protected vec4 _vClear;
    protected Picker _pPicker = null;
    protected bool _bRenderInProgress = false;

    public Renderer()
    {
      _vClear = new vec4(0, 0, 0, 1);
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
      _bMsaaEnabled = Gu.EngineConfig.MSAA;// Gu::getEngineConfig().getEnableMSAA();
      _nMsaaSamples = Gu.EngineConfig.MSAASamples;
      _iLastWidth = iWidth;
      _iLastHeight = iHeight;

      Gu.Log.Info("[Renderer] Checking Caps");
      checkDeviceCaps(iWidth, iHeight);

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
      _pBlittedDepth = FramebufferBase.createDepthTarget("Blitted_Depth_Buffer", iWidth, iHeight, 0, false, 0);

      //Do not cahnge "Pick" name.  This is shared.
      _pPick = FramebufferBase.createTarget("PickMRT_Deferred_Shared_", PixelInternalFormat.R32ui, PixelFormat.RedInteger, PixelType.UnsignedInt, iWidth, iHeight, RenderTargetType.Pick, 0, false, 0);  //4

      _pBlittedDeferred = new DeferredFramebuffer(false, 0, _vClear);
      _pBlittedDeferred.init(iWidth, iHeight, _pBlittedDepth, _pPick);

      _pBlittedForward = new ForwardFramebuffer(false, 0, _vClear);
      _pBlittedForward.init(iWidth, iHeight, _pBlittedDepth, _pPick);

      _pQuadMesh = MeshData.createScreenQuadMesh(iWidth, iHeight);
      _forwardMaterial = new Material("forwardMaterial",Gu.Resources.LoadShader("v_v3x2_forward", false, FileStorage.Embedded));
      _forwardMaterial.GpuRenderState.CullFace = false;
      _forwardMaterial.GpuRenderState.DepthTest = false;

      //TODO: actual deferred lighting .. is it needed ? idk. d_v3x2_lighting..
      _deferredMaterial = new Material("deferredMaterial",Gu.Resources.LoadShader("v_v3x2_deferred", false, FileStorage.Embedded));
      _deferredMaterial.GpuRenderState.CullFace = true;
      _deferredMaterial.GpuRenderState.DepthTest = true;

      //_pDOFFbo = new DOFFbo(getContext(), iWidth, iHeight);

      //Pick
      _pPicker = new Picker(this);

      //Multisample
      if (_bMsaaEnabled == true)
      {
        Gu.Log.Info("[Renderer] Creating deferred MSAA lighting buffer");
        _pMsaaDepth = FramebufferBase.createDepthTarget("Renderer: Depth MSAA", iWidth, iHeight, 0, _bMsaaEnabled, _nMsaaSamples);
        _pMsaaDeferred = new DeferredFramebuffer(_bMsaaEnabled, _nMsaaSamples, _vClear);
        _pMsaaDeferred.init(iWidth, iHeight, _pMsaaDepth, _pPick);  // Yeah I don't know if the "pick" here will work
        _pMsaaForward = new ForwardFramebuffer(_bMsaaEnabled, _nMsaaSamples, _vClear);
        _pMsaaForward.init(iWidth, iHeight, _pMsaaDepth, _pPick);  // Yeah I don't know if the "pick" here will work
      }
      else
      {
        Gu.Log.Info("[Renderer] Multisample not enabled.");
        _pMsaaDeferred = _pBlittedDeferred;
        _pMsaaForward = _pBlittedForward;
        _pMsaaDepth = _pBlittedDepth;
      }

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
    public DeferredFramebuffer getBlittedDeferred() { return _pBlittedDeferred; }
    //public void renderSceneTexture(PipeBits _pipeBits);
    //public void renderScene(Drawable toDraw, RenderBucket b, CameraNode cam, LightManager lightman, PipeBits pipeBits)
    //{
    //  //Input: Camera, Node
    //  //Output: Rendered scene image
    //  //Light Manager may not really be necessary.
    //  //TODO: to make this even more generic, have another function accept just scene, then loop over all scene cameras and call this.
    //  //this is redundant but could be used for some effects like mirrors, or portals.
    //  if (cam == null)
    //  {
    //    Gu.Log.Warn("camera not set in renderScene()");
    //    return;
    //  }
    //  if (toDraw == null)
    //  {
    //    Gu.Log.Warn("toDraw not set in renderScene()");
    //    return;
    //  }
    //  if (lightman == null)
    //  {
    //    Gu.Log.Warn("lightmanager not set in renderScene()");
    //    return;
    //  }

    //  Gu::checkErrorsDbg();

    //  cam.getViewport().bind();

    //  _pPicker.update(getWindow().getInput());
    //  if (cam == null)
    //  {
    //    Gu.Log.ErrorOnce("Camera was not set for renderScene");
    //    return;
    //  }
    //  if (cam.getViewport() == null)
    //  {
    //    Gu.Log.ErrorOnce("Camera Viewport was not set for renderScene");
    //    return;
    //  }
    //  //Check if the user changed the window size, and reallocate all buffers if they did.
    //  //  TODO: Set a fixed size then scale it correctly instead of reallocating all GBuffers every time the size changes.
    //  RenderViewport pv = cam.getViewport();
    //  if (pv.getWidth() != _iLastWidth || pv.getHeight() != _iLastHeight)
    //  {
    //    init(pv.getWidth(), pv.getHeight(), "");
    //  }

    //  //Only render one thing at a tiem to prevent corrupting the pipe
    //  if (_bRenderInProgress == true)
    //  {
    //    Gu.Log.Error("Tried to render something while another render was currently in progress.");
    //    return;
    //  }
    //  // New (faster) async collection algorithm
    //  //Basically this algorithm is 
    //  // For each camera frustum.
    //  //  if visible
    //  //    For each light 1...n in scene graph S async
    //  //      If light radius is in camera frustum
    //  //        For all shadow frustums in light.
    //  //          Find all visible volumes in S that intersect the camera frustum, AND all light frustums.
    //  //  
    //  //   Note - this will essentially replace the Light*'s update routine() however the update() is a node routine. 
    //  //    Lights are special in that they have a node update routine and a light update routine.
    //  // Essentially we can classify them as Emitters * so light emitters have their own light update routine.
    //  /*
    //  LightNode.getCullParams()
    //    List<RenderBucket> buckets;
    //  buckets.push_back(_pShadowFrustum.getCullParams())
    //    CullParams.cullAsync
    //    */
    //  //Cull objects
    //  //CullParams cparm;
    //  //cparm.setFrustum(cam.getFrustum());
    //  //cparm.setRenderBucket(new RenderBucket());
    //  //toDraw.cull(cparm);

    //  //We might just store cull checked lists on the frustum itself.

    //  //Gu.Log.WarnCycle("TODO: perform light culling here.");
    //  //for (auto light_pair : cparm.getRenderBucket().getDirLights()) {
    //  //  LightNodeDir light = light_pair.second;
    //  //  if (light.getIsShadowsEnabled()) {
    //  //    light.cullShadowVolumesAsync()
    //  //    CullParams light_parm;
    //  //    cp.setFrustum(cam);
    //  //    cp.setRenderBucket(new RenderBucket());
    //  //    toDraw.cull(cparm);
    //  //  }
    //  //}
    //  /*
    //  // *************THIS
    //      List<future> futs
    //    foreach light {
    //      futs.push_back(light.updateandcullasync())
    //    }
    //    //Fence
    //    foreach(fut in futs){
    //      fut.wait();
    //    }
    //  */

    //  //Compute shadow maps
    //  // CullParams p;
    //  // List<std::future<bool>> futs;
    //  for (auto light_pair : b.getLights())
    //  {
    //    //p.setCamera(cam);
    //    //std::future<bool> bf = light_pair.second.cullShadowVolumesAsync(p);
    //    //futs.push_back(std::move(bf));
    //    light_pair.second.calcGPULight();
    //  }
    //  // for (size_t i=0; i<futs.size();++i) {
    //  //   futs[i].wait();
    //  // }


    //  _bRenderInProgress = true;
    //  {
    //    RenderParams rp;
    //    rp.setCamera(cam);
    //    //Algorithm:
    //    // 1. Render models to GBuffer (deferred)
    //    // 2. Render GBuffer to Texture computing shadows & lighting (forward)
    //    // 3. Render UI, FW models. Picking.
    //    // 4. Transparency.
    //    // 5. Draw FWTexture to window framebuffer using quad.

    //    //This doesn't conform ot the new ability to render individual objects.
    //    //This draws all scene shadows
    //    //If we want shadows in the thumbnails, we'll need to fix this to be able to render
    //    //individual object shadows
    //    if (pipeBits.test(PipeBit::e::Shadow))
    //    {
    //      beginRenderShadows();
    //      {
    //        Gu::checkErrorsDbg();
    //        //**This shouldn't be an option **
    //        //Curetly we update lights at the same time as shadows.  this is erroneous
    //        if (pipeBits.test(PipeBit::e::Shadow))
    //        {
    //          renderShadows(lightman, cam);
    //        }
    //      }
    //      endRenderShadows();
    //    }
    //    _pMsaaForward.clearFb();

    //    //1. 3D, Deferred lighting
    //    if (pipeBits.test(PipeBit::e::Deferred))
    //    {
    //      beginRenderDeferred();
    //      {
    //        toDraw.drawDeferred(rp);
    //      }
    //      endRenderDeferred();

    //      //Blit to forward FB
    //      blitDeferredRender(lightman, cam);

    //      if (pipeBits.test(PipeBit::e::Transparent))
    //      {
    //        toDraw.drawTransparent(rp);
    //      }
    //    }

    //    //2. Forward Rendering
    //    if (pipeBits.test(PipeBit::e::Forward))
    //    {
    //      beginRenderForward();

    //      toDraw.drawForward(rp);

    //      //2.1 - Debug
    //      if (pipeBits.test(PipeBit::e::Debug))
    //      {
    //        toDraw.drawForwardDebug(rp);
    //      }

    //      //2.2 - DOF
    //      if (pipeBits.test(PipeBit::e::DepthOfField))
    //      {
    //        postProcessDOF(lightman, cam);
    //      }

    //      //Rebind Forward FBO
    //      beginRenderForward();

    //      //3. Orthographic, Behind the UI
    //      if (pipeBits.test(PipeBit::e::NonDepth))
    //      {
    //        toDraw.drawNonDepth(rp);
    //      }

    //      //4. The UI
    //      if (pipeBits.test(PipeBit::e::UI_Overlay))
    //      {
    //        toDraw.drawUI(rp);
    //      }

    //      endRenderForward();
    //    }
    //    //3. Transparency
    //    //beginRenderTransparent();
    //    //{
    //    //    //TP uses the forward texture.
    //    //  //   toDraw.drawTransparent(rp);
    //    //}
    //    //endRenderTransparent();

    //    //Blit to window
    //    if (pipeBits.test(PipeBit::e::BlitFinal))
    //    {
    //      endRenderAndBlit(lightman, cam);
    //    }
    //  }
    //  _bRenderInProgress = false;
    //}
    public void resizeScreenBuffers(int w, int h)
    {
      //Simply called neow by new camera vp size.
      //recreate everything
      init(w, h, null);
    }
    //public Img32 getResultAsImage()
    //{
    //  Img32 bi = new Img32();

    //  //so wer'e having problem rendering the alpha of rthe forward buffer
    //  //its probably a trivial bug, but my brain is dead once again
    //  //so i'm just exporting the colors of the deferred for now

    //  BufferRenderTarget pTarget;
    //  pTarget = _pBlittedForward.getTargetByName(ForwardFramebuffer.c_strColorMRT_FW);
    //  if (Gpu.GetTextureDataFromGpu(bi, pTarget.getGlTexId(), TextureTarget.Texture2D) == true)
    //  {
    //    //the GL tex image must be flipped to show upriht/
    //  }

    //  //Only transparent method that's working
    //  // pTarget = _pBlittedDeferred.getTargetByName(ForwardFramebuffer::c_strColorMRT);
    //  // if (RenderUtils::getTextureDataFromGpu(bi, pTarget.getGlTexId(), GL_TEXTURE_2D) == true) {
    //  //     //the GL tex image must be flipped to show upriht/
    //  // }

    //  return bi;
    //}
    public vec4 getClear()
    {
      return _vClear;
    }
    public void setClear(vec4 v)
    {
      _vClear = v;
      if (_pBlittedForward != null)
      {
        _pBlittedForward.setClear(_vClear);
      }
      else
      {
        Gu.Log.Error("Framebuffer was null when setting clear color.");
      }
      if (_pBlittedDeferred != null)
      {
        _pBlittedDeferred.setClear(_vClear);
      }
      else
      {
        Gu.Log.Error("Framebuffer was null when setting clear color.");
      }
    }
    public void blitDeferredRender(Camera3D pcam)
    {
      //NOTE:
      //Bind the forward framebuffer (_pBlittedForward is equal to _pMsaaForward if MSAA is disabled, if it isn't we call copyMSAASamples later)

      //Gu::getShaderMaker().shaderBound(null);  //Unbind and reset shader.
      GL.UseProgram(0);

      GL.BindFramebuffer(FramebufferTarget.Framebuffer, _pMsaaForward.getGlId());
      Gpu.CheckGpuErrorsDbg();
      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      Gpu.CheckGpuErrorsDbg();

      Gu.BRThrowNotImplementedException();

      pcam.beginRaster();
      //{
      //  //*The clear here isn't necessary. If we're copying all of the contents of the deferred buffer.
      //  // - Clear the color and depth buffers (back and front buffers not the Mrts)
      //  //glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
     // bindDeferredTargets(true);
      {
        //Set the light uniform blocks for the deferred shader.
        //_pDeferredShader.setLightUf(lightman);
        //setShadowEnv(lightman, true);
        {
          DrawCall_UniformData dc = new DrawCall_UniformData() { 
            cam = pcam,
              customUniforms = (su) =>
              {
                if (su.Name.Equals("_ufTexture2D_Position"))
                {
                  var t = _pMsaaDeferred.getTargets().Where(x => x.getName().ToLower().Contains("position")).First();
                  bindDeferredTarget(true,t);
                  GL.Uniform1(su.Location, (int)(t.getTextureChannel() - TextureUnit.Texture0));
                }
                else if (su.Name.Equals("_ufTexture2D_Color"))
                {
                  var t = _pMsaaDeferred.getTargets().Where(x => x.getName().ToLower().Contains("color")).First();
                  bindDeferredTarget(true, t);
                  GL.Uniform1(su.Location, (int)(t.getTextureChannel() - TextureUnit.Texture0));
                }
                else if (su.Name.Equals("_ufTexture2D_Normal"))
                {
                  var t = _pMsaaDeferred.getTargets().Where(x => x.getName().ToLower().Contains("normal")).First();
                  bindDeferredTarget(true, t);
                  GL.Uniform1(su.Location, (int)(t.getTextureChannel() - TextureUnit.Texture0));
                }
                else if (su.Name.Equals("_ufTexture2D_Spec"))
                {
                  var t = _pMsaaDeferred.getTargets().Where(x => x.getName().ToLower().Contains("spec")).First();
                  bindDeferredTarget(true, t);
                  GL.Uniform1(su.Location, (int)(t.getTextureChannel() - TextureUnit.Texture0));
                }
                else
                {
                  return false;
                }
                return true;
              }
          };
          _deferredMaterial.Draw(_pQuadMesh, dc);

          Gpu.CheckGpuErrorsDbg();
        }
        //setShadowEnv(lightman, false);  //Fix this, we should be able to clear the texture units before the next operation.
      }
      //bindDeferredTargets(false);
      //}
      pcam.endRaster();
    }
    
    private bool _requestSaveScreenshot = false;
    protected void checkMultisampleParams()
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
    protected void checkDeviceCaps(int iWidth, int iHeight)
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

      checkMultisampleParams();
    }
    public void saveScreenshot()
    {
      _requestSaveScreenshot = true;
    }
    private void saveScreenshotInternal(/*LightManager lightman*/)
    {
      //The reason we do it this way is so that we can save the screenshot right before we blit the final render to the screen.
      if (_requestSaveScreenshot)
      {
        _requestSaveScreenshot = false;
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

        iTarget = 0;
        foreach (BufferRenderTarget pTarget in _pBlittedDeferred.getTargets())
        {
          string fname = Filesystem.GetFilenameDateTimeNOW() + "_deferred_" + pTarget.getName() + "_" + iTarget++ + "_.png";
          fname = System.IO.Path.Combine(Gu.LocalCachePath, fname);
          ResourceManager.SaveTexture(new FileLoc(fname, FileStorage.Disk), pTarget.getGlTexId(), pTarget.getTextureTarget());
          Gu.Log.Info("[Renderer] Screenshot '" + fname + "' saved");
        }
        iTarget = 0;
        foreach (BufferRenderTarget pTarget in _pBlittedForward.getTargets())
        {
          string fname = Filesystem.GetFilenameDateTimeNOW() + "_forward_" + pTarget.getName() + "_" + iTarget++ + "_.png";
          fname = System.IO.Path.Combine(Gu.LocalCachePath, fname);
          ResourceManager.SaveTexture(new FileLoc(fname, FileStorage.Disk), pTarget.getGlTexId(), pTarget.getTextureTarget());
          Gu.Log.Info("[Renderer] Screenshot '" + fname + "' saved");
        }
        //}
        //    else
        //    {
        //      //Basic Forward Screenshot
        //      string fname = FileSystem::getScreenshotFilename();
        //getContext().getRenderUtils().saveTexture(std::move(fname), _pBlittedForward.getGlId(), GL_TEXTURE_2D);
        //Gu.Log.Info("[Renderer] Screenshot '" + fname + "' saved");
        //  }
        //}
      }
    }
    protected void copyMsaaSamples(FramebufferBase msaa, FramebufferBase blitted)
    {
      //Downsize the MSAA sample buffer.
      if (_bMsaaEnabled)
      {
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        Gpu.CheckGpuErrorsDbg();

        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, msaa.getGlId());
        Gpu.CheckGpuErrorsDbg();
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, blitted.getGlId());
        Gpu.CheckGpuErrorsDbg();

        foreach (BufferRenderTarget inf in msaa.getTargets())
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
    protected void releaseFbosAndMesh()
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
    protected void setShadowEnv(/*LightManager lightman, */bool bSet)
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
    public void BeginEverything_New(MainWindow g)
    {
      Gu.Assert(RenderState == RendererlineState.End || RenderState == RendererlineState.None);
      RenderState = RendererlineState.Begin;

      Gu.SetContext(g);

      SetInitialGpuRenderState();
      _pPicker.update();
    }
    public void EndEverything_New()
    {
      Gu.Assert(RenderState == RendererlineState.Begin);
      RenderState = RendererlineState.End;
      Gu.Context.GameWindow.SwapBuffers();
    }
    public void beginRenderDeferred()
    {
      enableDisablePipeBits();

      // _pMsaaForward.clearFb();//Must call before deferre
      _pMsaaDeferred.beginRender();
    }
    public void endRenderDeferred()
    {
      _pMsaaDeferred.endRender();
    }
    public void beginRenderForward()
    {
      _pMsaaForward.beginRender();
    }
    public void endRenderForward()
    {
      _pMsaaForward.endRender();
    }
    //protected void renderShadows(LightManager lightman, CameraNode cam)
    //{
    //  lightman.update(_pShadowBoxFboMaster, _pShadowFrustumMaster);
    //  Gu::checkErrorsDbg();

    //  //Force refresh teh viewport.
    //  cam.getViewport().bind();
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
    //}
    //protected void endRenderShadows()
    //{
    //  Gu::checkErrorsDbg();
    //  glCullFace(GL_BACK);
    //  getContext().popDepthTest();
    //  getContext().popBlend();
    //  getContext().popCullFace();
    //  Gu::checkErrorsDbg();
    //}
    protected void bindDeferredTargets(bool bBind)
    {
      //@param bBind True:Bind targets, False: Clear targets.
      foreach (BufferRenderTarget inf in _pMsaaDeferred.getTargets())
      {
        bindDeferredTarget(bBind, inf);
      }
    }
    protected void bindDeferredTarget(bool bBind, BufferRenderTarget inf)
    {
      Gu.Assert(inf != null);
      GL.ActiveTexture(inf.getTextureChannel());
      Gpu.CheckGpuErrorsDbg();
      GL.BindTexture(inf.getTextureTarget(), bBind ? inf.getTexId() : 0);
      Gpu.CheckGpuErrorsDbg();

      if (bBind && inf.getTargetType() == RenderTargetType.Color || inf.getTargetType() == RenderTargetType.Shadow)
      {
        //Don't set depth target.
        Gu.BRThrowNotImplementedException();

        //_pDeferredShader.setTextureUf(inf.getLayoutIndex());
        Gpu.CheckGpuErrorsDbg();
      }
    }
    public void endRenderAndBlit(Camera3D pCam)
    {
      //Blits the final deferred Color image (after our deferred rendering step) to a quad.
      //Do not bind anything - default framebuffer.
      //Gu::getShaderMaker().shaderBound(null);  //Unbind and reset shader.
      GL.UseProgram(0);
      GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
      Gpu.CheckGpuErrorsDbg();
      GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
      Gpu.CheckGpuErrorsDbg();

      // - Clear the color and depth buffers (back and front buffers not the Mrts)
      GL.ClearColor(getClear().x, getClear().y, getClear().z, getClear().w);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      Gpu.CheckGpuErrorsDbg();

      //GL.ActiveTexture(TextureUnit.Texture0);
      //Gpu.CheckGpuErrorsDbg();
      //GL.BindTexture(TextureTarget.Texture2D, _pBlittedForward.getGlColorBufferTexId());
      Gpu.CheckGpuErrorsDbg();
      {
        pCam.beginRaster();
        {
          saveScreenshotInternal();
          Gpu.CheckGpuErrorsDbg();

          DrawCall_UniformData dc = new DrawCall_UniformData()
          {
            cam = pCam,
            customUniforms = (su) =>
            {
              if (su.Name.Equals("_ufTexture_Forward_Blitter_Input"))
              {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _pBlittedForward.getGlColorBufferTexId());
                GL.Uniform1(su.Location, (int)(TextureUnit.Texture0 - TextureUnit.Texture0));
              }
              else
              {
                return false;
              }
              return true;
            }
          };
          _forwardMaterial.Draw(_pQuadMesh, dc);
        }
        pCam.endRaster();
      }
      GL.ActiveTexture(TextureUnit.Texture0);
      GL.BindTexture(TextureTarget.Texture2D, 0);
    }
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
    //    BufferRenderTarget rtPos = _pMsaaDeferred.getTargetByName(DeferredFramebuffer::c_strPositionMRT_DF);
    //    BufferRenderTarget rtColor = _pMsaaForward.getTargetByName(ForwardFramebuffer::c_strColorMRT_FW);  //**Note** Forward

    //    if (rtPos == null || rtColor == null)
    //    {
    //      Gu.Log.ErrorCycle("oen or more Render targets were null");
    //      return;
    //    }

    //    //Blend color + position and store it in the color.
    //    pDofShader.beginRaster(cam.getViewport());
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
    //void postProcessDeferredRender();
    protected void enableDisablePipeBits()
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

    // ** New Renderer stuff 

    public void BeginRender(MainWindow g, vec4 color)
    {
      //here set buffers

      //Begin global rendering, to render to all cameras that want rendering.
      Gu.Assert(RenderState == RendererlineState.End || RenderState == RendererlineState.None);
      RenderState = RendererlineState.Begin;
      Gu.SetContext(g);
      GL.ClearColor(color.x, color.y, color.z, color.w);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      Gpu.CheckGpuErrorsRt();

      SetInitialGpuRenderState();
    }
    public void EndRender()
    {
      Gu.Assert(RenderState == RendererlineState.Begin);
      RenderState = RendererlineState.End;

      Gpu.CheckGpuErrorsRt();
      Gu.Context.GameWindow.SwapBuffers();
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














  }
}
