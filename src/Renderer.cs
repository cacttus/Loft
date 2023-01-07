using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Loft
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
    [Description("DEF_PIPELINE_STAGE_EFFECT")] Effect,
    [Description("DEF_PIPELINE_STAGE_DEBUG")] Debug,
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
    #region Members
    //**Note do not set this to be anything but full alpha, if blending is enabled teh blend will screw up this value.
    public const uint c_iInvalidPickId = 0;
    public const uint c_iSelectedFlag = (1 << 30);//object is selected
    public const uint c_iPickedFlag = (1 << 29);//object is picked
    public const uint c_iActiveFlag = (1 << 28);//object is picked

    private uint _iid = 1;
    private WeakReference<Renderer> _pRenderer;
    private uint _uiLastSelectedPixelId = c_iInvalidPickId;//Note: This is relative to the last UserSelectionSet - the Id here is not fixed.
    private object _pickedObjectFrame = null;
    private object _pickedObjectFrameLast = null;

    public uint SelectedPixelId { get { return _uiLastSelectedPixelId; } }
    public object PickedObjectFrameLast
    {
      get
      {
        return _pickedObjectFrameLast;
      }
      set
      {
        _pickedObjectFrameLast = value;
      }
    }
    public object PickedObjectFrame
    {
      get
      {
        return _pickedObjectFrame;
      }
      set
      {
        if (_pickedObjectFrame != null && value != null && (_pickedObjectFrame != value))
        {
          //this should not happen
          Gu.DebugBreak();
        }

        _pickedObjectFrame = value;
      }
    }
    public string PickedObjectName
    {
      get
      {
        string ret = "";
        if (PickedObjectFrame == null)
        {
          ret = "<None>";
        }
        else
        {
          if (PickedObjectFrame is IUiElement)
          {
            ret = (PickedObjectFrame as IUiElement).Name;
          }
          else if (PickedObjectFrame is WorldObject)
          {
            ret = (PickedObjectFrame as WorldObject).Name;
          }

          ret += $" ({PickedObjectFrame.GetType().Name})";

          if (PickedObjectFrame is Drawable)
          {
            if ((PickedObjectFrame as Drawable).MeshView != null)
            {
              ret += $" (Mesh:{(PickedObjectFrame as Drawable).MeshView.Name})";
            }
            else
            {
              return "Drawable (no mesh)";
            }
          }
        }
        return ret;
      }
    }

    #endregion
    #region Public:Static Methods

    public static uint RemoveFlagsFromPickID(uint pickid)
    {
      pickid = pickid & (~Picker.c_iSelectedFlag);
      pickid = pickid & (~Picker.c_iPickedFlag);
      pickid = pickid & (~Picker.c_iActiveFlag);
      return pickid;
    }
    public static uint AddFlagsToPickID(uint pickid, bool pickable, bool selected, bool picked, bool active)
    {
      if (pickable == false)
      {
        pickid = RemoveFlagsFromPickID(pickid);
      }
      else if (pickable == true)
      {
        if (selected)
        {
          pickid |= Picker.c_iSelectedFlag;
        }
        else
        {
          pickid = pickid & (~Picker.c_iSelectedFlag);
        }
        if (picked)
        {
          pickid |= Picker.c_iPickedFlag;
        }
        else
        {
          pickid = pickid & (~Picker.c_iPickedFlag);
        }
        if (active)
        {
          pickid |= Picker.c_iActiveFlag;
        }
        else
        {
          pickid = pickid & (~Picker.c_iActiveFlag);
        }
      }

      return pickid;
    }

    #endregion
    #region Public:Methods

    public Picker(Renderer rp)
    {
      _pRenderer = new WeakReference<Renderer>(rp);
    }
    public void ResetPickedObject()
    {
      //call after all windows update, other windows will rely on this
      if (_pickedObjectFrameLast != _pickedObjectFrame)
      {
        if (_pickedObjectFrameLast != null && _pickedObjectFrameLast is WorldObject)
        {
          (_pickedObjectFrameLast as WorldObject).IsPicked = false;
        }
        if (_pickedObjectFrame != null && _pickedObjectFrame is WorldObject)
        {
          (_pickedObjectFrame as WorldObject).IsPicked = true;
        }
      }

      PickedObjectFrameLast = PickedObjectFrame;
      PickedObjectFrame = null;
    }
    public void UpdatePickedPixel()
    {
      //Call before all windows update
      if (Gu.Context.GameWindow.IsFocused)
      {
        UpdatePickedPixel((int)Gu.Context.PCMouse.Pos.x, (int)Gu.Context.PCMouse.Pos.y);
      }
      else
      {
        _uiLastSelectedPixelId = c_iInvalidPickId;
      }
    }
    public uint GenPickId()
    {
      if(Gu.EngineConfig.Debug_PickIDs){
      _iid = _iid + 100;
      }
      else
      {
        _iid++;
      }
      //Creates a pick ID, note that this ID is colored so we can see it (alpha off)


      if (_iid > 0xFFFFFF)
      {
        //50 = 335544 possible Id's, 10=1677721.5 id's still possible to wrap
        Gu.Log.Error("Pick Id Generator just wrapped, check if debug mode" );
        Gu.DebugBreak();
        _iid %= 0xFFFFFF;
      }

      //Return an actual color so we can see it. Also, always set full alpha in case blending is enabled by accident.
      uint pickColorId = ((_iid << 8) | 0x000000FF) & 0xFFFFFFFF;

      return pickColorId;
    }

    #endregion
    #region Private:Methods

    private void UpdatePickedPixel(int window_x, int window_y)
    {
      if (_pRenderer != null && _pRenderer.TryGetTarget(out var renderer))
      {
        Gu.Assert(renderer.PickStage != null);

        float xRatio = (float)renderer.PickStage.Size.x / (float)renderer.DefaultFBOSize.x;
        float yRatio = (float)renderer.PickStage.Size.y / (float)renderer.DefaultFBOSize.y;

        int dx = (int)Math.Round((float)window_x * xRatio);
        int dy = (int)Math.Round((float)window_y * yRatio);

        if (dx < 0 || dx >= renderer.PickStage.Size.width || dy < 0 || dy >= renderer.PickStage.Size.height)
        {
          //Mouse is outside window.
          _uiLastSelectedPixelId = c_iInvalidPickId;
        }
        else
        {
          renderer.PickStage.Bind(FramebufferTarget.ReadFramebuffer);
          renderer.PickStage.BindReadBuffer(RenderTargetType.Pick);
          _uiLastSelectedPixelId = SamplePixelId(dx, dy, renderer.PickStage.Size.height);
          _uiLastSelectedPixelId = RemoveFlagsFromPickID(_uiLastSelectedPixelId);
          renderer.PickStage.UnbindReadBuffer();
          renderer.PickStage.Unbind(FramebufferTarget.ReadFramebuffer);
        }
      }
    }
    private uint SamplePixelId(int fbo_x, int fbo_y, int fbo_height)
    {
      uint pixel = 0;

      //https://www.khronos.org/opengles/sdk/docs/man/xhtml/glReadPixels.xml
      //If the currently bound framebuffer is not the default framebuffer object, color components
      // are read from the color image attached to the GL_COLOR_ATTACHMENT0 attachment point.

      GL.ReadPixels(fbo_x - 1,
                   fbo_height - (fbo_y - 1),
                   1, 1,
                   PixelFormat.RedInteger,
                   PixelType.UnsignedInt,
                   ref pixel);

      Gpu.CheckGpuErrorsDbg();
      return pixel;
    }

    #endregion

  }

  [DataContract]
  public class PipelineAttachment
  {
    //Input/Output variable for pipeline
    public static List<PipelineAttachment> NoFramebuffer { get { return new List<PipelineAttachment>() { }; } }

    public bool IsOutput { get { return ShaderOutput != null; } }
    public bool IsInput { get { return ShaderInput != null; } }

    [DataMember] public ShaderInput? ShaderInput = null;
    [DataMember] public ShaderOutput? ShaderOutput = null;
    [DataMember] public FramebufferAttachment? Attachment = null;

    public Texture Texture { get { return Attachment.Texture; } }
    public RenderTargetType TargetType { get { return Attachment.TargetType; } }
    public bool IsMsaaEnabled { get { return Attachment.IsMsaaEnabled; } }

    public ivec2 Size { get { return Texture.Size; } }

    public PipelineAttachment(ShaderOutput output, FramebufferAttachment attachment)
    {
      Gu.Assert(attachment != null);
      ShaderOutput = output;
      ShaderInput = null;
      Attachment = attachment;
    }
    public PipelineAttachment(ShaderInput input, FramebufferAttachment attachment)
    {
      Gu.Assert(attachment != null);
      ShaderOutput = null;
      ShaderInput = input;
      Attachment = attachment;
    }
  }
  [DataContract]
  public class PipelineStage
  {
    public string Name { get { return PipelineStageEnum.Description(); } }
    public int Index { get { return _index; } set { _index = value; } }
    public FramebufferGeneric InputFramebuffer { get { return _inputFramebuffer; } private set { _inputFramebuffer = value; } }
    public FramebufferGeneric OutputFramebuffer { get { return _outputFramebuffer; } private set { _outputFramebuffer = value; } }
    public ClearBufferMask ClearMask { get { return _clearMask; } private set { _clearMask = value; } }
    public vec4 ClearColor { get { return _clearColor; } private set { _clearColor = value; } }
    public List<PipelineAttachment> Inputs { get { return _inputs; } private set { _inputs = value; } }
    public List<PipelineAttachment> Outputs { get { return _outputs; } private set { _outputs = value; } }
    public mat4? BlitMat { get { return _blitMat; } private set { _blitMat = value; } }
    public ivec2 Size { get { return _size; } }
    public PipelineStageEnum PipelineStageEnum { get { return _pipelineStageEnum; } set { _pipelineStageEnum = value; } }

    [DataMember] private int _index = 0;
    [DataMember] public FramebufferGeneric _inputFramebuffer;
    [DataMember] public FramebufferGeneric _outputFramebuffer;
    [DataMember] public ClearBufferMask _clearMask;
    [DataMember] public vec4 _clearColor;
    [DataMember] public List<PipelineAttachment> _inputs;
    [DataMember] public List<PipelineAttachment> _outputs;
    [DataMember] public mat4? _blitMat;
    [DataMember] public PipelineStageEnum _pipelineStageEnum;
    //public Action<RenderView>? BeginRenderAction = null;
    //public Action<RenderView>? EndRenderAction = null;
    public WorldObject? BlitObj = null;
    private ivec2 _size;

    //So, cull, winding .. Gpu state is on material currently
    public PipelineStage(PipelineStageEnum stage, ClearBufferMask mask, vec4 clear,
                          int defaultWidth, int defaultHeight,
                         List<PipelineAttachment> inputs, List<PipelineAttachment> outputs,
                         string? blit_shader = null, bool blend_blitter = false)
    {
      _clearMask = mask;
      _clearColor = clear;
      _pipelineStageEnum = stage;
      _inputs = inputs;
      _outputs = outputs;
      _size = new ivec2(defaultWidth, defaultHeight);

      Validate(inputs, outputs);

      if (outputs.Count > 0)
      {
        _outputFramebuffer = new FramebufferGeneric(Enum.GetName(stage) + "-out-fb", outputs);

        // Gu.Assert(outputs[0].Attachment.Texture.Width == output_width);
        // Gu.Assert(outputs[0].Attachment.Texture.Height == output_height);
      }
      if (inputs.Count > 0)
      {
        _inputFramebuffer = new FramebufferGeneric(Enum.GetName(stage) + "-in-fb", inputs);
      }

      if (blit_shader.IsNotEmpty())
      {
        MakeBlitter(blit_shader, blend_blitter);
      }
    }
    private void Validate(List<PipelineAttachment> inputs, List<PipelineAttachment> outputs)
    {
      string s = "";
      for (int i = 0; i < inputs.Count; ++i)
      {
        Gu.Assert(!inputs[i].IsOutput && inputs[i].IsInput);
        for (int j = i + 1; j < outputs.Count; ++j)
        {
          Gu.Assert(outputs[i].IsOutput && !outputs[i].IsInput);
          if (inputs[i].Attachment == outputs[j].Attachment)
          {
            //an input is also an output. Error
            s += "Framebuffer " + Name + " input is also an output : " + inputs[i].Attachment.Texture.Name + "\n";
          }
          if (inputs[i].Attachment.Texture == null)
          {
            s += "Framebuffer " + Name + " input " + i + " texture was null. " + "\n";

          }
          if (outputs[j].Attachment.Texture == null)
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
    public bool BeginRender(bool forceClear)
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
      return true;
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
    private void MakeBlitter(string generic_name, bool blend)
    {
      int width = Size.width;
      int height = Size.height;

      //Blitter: Instead of drawing objects this stage will draw a full screen quad with a custom shader (deferred/forward/effect)
      Gu.Assert(Inputs.Count > 0);
      string names = $"Shader_{generic_name}";
      BlitObj = new WorldObject($"{names}-wo");
      BlitObj.MeshView = new MeshView(MeshGen.CreateScreenQuadMesh($"{names}-mesh", width, height));

      BlitObj.Material = new Material($"{names}-mat", new Shader($"{names}-shr", generic_name));

      BlitObj.Material.GpuRenderState.CullFace = false;
      BlitObj.Material.GpuRenderState.DepthTest = false;
      BlitObj.Material.GpuRenderState.Blend = blend;
      for (int i = 0; i < this.Inputs.Count; i++)
      {
        Gu.Assert(this.Inputs[i].ShaderInput != null);
        BlitObj.Material.Textures.Add(new TextureSlot(this.Inputs[i].Texture, this.Inputs[i].ShaderInput.Description(), i));
      }
      BlitMat = mat4.ortho(0, width, 0, height, -1, 1);
    }
  }

  public class Renderer : HasGpuResources
  {
    #region Public:Members

    public ShaderControlVars DefaultControlVars = new ShaderControlVars();
    public RendererState RenderState { get; private set; } = RendererState.None;
    public Picker Picker { get; private set; } = null;
    public PipelineStage CurrentStage { get; private set; } = null;
    private RenderView CurrentView = null;
    public FramebufferGeneric PickStage { get; private set; } = null;
    private int _windowWidth = 1;
    private int _windowHeight = 1;
    private int _deferredFBOWidth = 1;
    private int _deferredFBOHeight = 1;
    public List<PipelineStage> PipelineStages { get { return _pipelineStages; } }
    private int _aliasWidthPixels = 1;
    public float _windowAspectRatio = 1;
    public float WindowAspectRatio { get { return _windowAspectRatio; } }
    public ivec2 CurrentStageFBOSize { get { return CurrentStage.Size; } }
    public ivec2 DefaultFBOSize { get { return new ivec2(_windowWidth, _windowHeight); } }

    //FBO sizes can change. os viewport must also change.

    #endregion
    #region Private:Members

    private List<PipelineStage> _pipelineStages = new List<PipelineStage>();
    private List<FramebufferAttachment> Attachments = new List<FramebufferAttachment>();

    private Texture _pEnvTex = null;  //Enviro map - for mirrors (coins)
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
    public void init(int iWindowWidth, int iWindowHeight, FileLoc envTextureLoc)
    {
      Gu.Log.Info("[Renderer] Initializing, Window: " + iWindowWidth + "x" + iWindowHeight);
      if (iWindowWidth <= 0 || iWindowHeight <= 0)
      {
        Gu.BRThrowException("[Renderer] Got framebuffer of width or height < 0: " + iWindowWidth + "," + iWindowHeight);
      }

      //Enable some stuff.
      Gu.Context.Gpu.SetState(new GpuRenderState()
      {
        CullFace = true,
        CullFaceMode = CullFaceMode.Back,
        FrontFaceDirection = FrontFaceDirection.Ccw,
        ScissorTest = true,
        DepthTest = true,
        Blend = false,
        DepthMask = true,
        BlendFunc = BlendEquationMode.FuncAdd,
        BlendFactor = BlendingFactor.OneMinusSrcAlpha,
      }, true);

      releaseFbosAndMesh();

      // - Setup Framebuffers.

      ///**Testing scaling**
      ///**Testing scaling**
      ///**Testing scaling**
      ///**Testing scaling**
      _windowAspectRatio = (float)iWindowWidth / (float)iWindowHeight;// 4.0f / 3.0f;
      _aliasWidthPixels = Gu.EngineConfig.AliasScreenWidthPixels;
      _windowWidth = iWindowWidth;
      _windowHeight = iWindowHeight;
      _deferredFBOWidth = 0;
      _deferredFBOHeight = 0;
      if (!Gu.EngineConfig.Renderer_UseAlias)
      {
        _deferredFBOWidth = iWindowWidth;
        _deferredFBOHeight = iWindowHeight;
      }
      else
      {
        float fbWidth = _aliasWidthPixels;
        float fbHeight = fbWidth / _windowAspectRatio;
        _deferredFBOWidth = (int)Math.Max(Math.Round(fbWidth), 1);
        _deferredFBOHeight = (int)Math.Max(Math.Round(fbHeight), 1);
      }
      Gu.Log.Info("Scaled w/h: " + _deferredFBOWidth + "," + _deferredFBOHeight);

      Gu.Log.Info("[Renderer] Checking FBO Caps");
      CheckDeviceCaps(_deferredFBOWidth, _deferredFBOHeight, Gu.EngineConfig.EnableMSAA ? Gu.EngineConfig.MSAASamples : 0);

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

      //possibly makes sense to make there be 3 fbo sizes, since the menus being the exact screen resolution look kind of ugly, maybe a little less than the actual world.

      Gu.Log.Debug("[Renderer] Creating Attachments");
      FramebufferAttachment color_df = new FramebufferAttachment("Color1", RenderTargetType.Color, _deferredFBOWidth, _deferredFBOHeight, samples);
      FramebufferAttachment color_fw = new FramebufferAttachment("Color2", RenderTargetType.Color, _deferredFBOWidth, _deferredFBOHeight, samples);
      FramebufferAttachment pick = new FramebufferAttachment("Pick", RenderTargetType.Pick, _deferredFBOWidth, _deferredFBOHeight, samples);
      FramebufferAttachment normal = new FramebufferAttachment("Normal", RenderTargetType.Color, _deferredFBOWidth, _deferredFBOHeight, samples);
      FramebufferAttachment position = new FramebufferAttachment("Position", RenderTargetType.Color, _deferredFBOWidth, _deferredFBOHeight, samples);
      FramebufferAttachment depth = new FramebufferAttachment("Depth", RenderTargetType.Depth, _deferredFBOWidth, _deferredFBOHeight, samples);
      FramebufferAttachment postprocess = new FramebufferAttachment("Postprocess", RenderTargetType.Color, _deferredFBOWidth, _deferredFBOHeight, samples);
      FramebufferAttachment material = new FramebufferAttachment("Material", RenderTargetType.Color, _deferredFBOWidth, _deferredFBOHeight, samples);

      Gu.Log.Debug("[Renderer] Creating Pipeline");

      var deferred = new PipelineStage(PipelineStageEnum.Deferred,
        ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit, ClearColor,
        _deferredFBOWidth, _deferredFBOHeight,
        new List<PipelineAttachment>() { },
        new List<PipelineAttachment>() {
          new PipelineAttachment(ShaderOutput.Color, color_df),
          new PipelineAttachment(ShaderOutput.Pick, pick),
          new PipelineAttachment(ShaderOutput.Normal, normal),
          new PipelineAttachment(ShaderOutput.Position, position),
          new PipelineAttachment(ShaderOutput.Material, material),
          new PipelineAttachment(ShaderOutput.Depth, depth) });

      var outline = new PipelineStage(PipelineStageEnum.Effect,
        ClearBufferMask.None, new vec4(0, 0, 0, 0),
        _deferredFBOWidth, _deferredFBOHeight,
        new List<PipelineAttachment>() {
          new PipelineAttachment(ShaderInput.Pick, pick) },
        new List<PipelineAttachment>() {
          new PipelineAttachment(ShaderOutput.Color, color_df) },
        "v_v3x2_outline", true
      );

      var deferredBlit = new PipelineStage(PipelineStageEnum.DeferredBlit,
        ClearBufferMask.None, ClearColor,
        _deferredFBOWidth, _deferredFBOHeight,
        new List<PipelineAttachment>() {
          new PipelineAttachment(ShaderInput.Color, color_df),
          new PipelineAttachment(ShaderInput.Normal, normal),
          new PipelineAttachment(ShaderInput.Position, position),
          new PipelineAttachment(ShaderInput.Material, material),
          new PipelineAttachment(ShaderInput.Depth, depth) },
        new List<PipelineAttachment>() {
          new PipelineAttachment(ShaderOutput.Color, color_fw), },
        "v_v3x2_deferred"
       );

      //detach pick buffer to render debug. render debug before UI
      var debug = new PipelineStage(PipelineStageEnum.Debug,
        ClearBufferMask.None, ClearColor,
        _deferredFBOWidth, _deferredFBOHeight,
        PipelineAttachment.NoFramebuffer,
        new List<PipelineAttachment>() {
          new PipelineAttachment(ShaderOutput.Color, color_fw),
          new PipelineAttachment(ShaderOutput.Depth, depth) });

      var forward = new PipelineStage(PipelineStageEnum.Forward,
        ClearBufferMask.None, ClearColor,
        _deferredFBOWidth, _deferredFBOHeight,
        PipelineAttachment.NoFramebuffer,
        new List<PipelineAttachment>() {
          new PipelineAttachment(ShaderOutput.Color, color_fw),
          new PipelineAttachment(ShaderOutput.Pick, pick),
          new PipelineAttachment(ShaderOutput.Depth, depth) });

      PickStage = forward.OutputFramebuffer;

      var forwardBlit = new PipelineStage(PipelineStageEnum.ForwardBlit,
        ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit, ClearColor,
        _windowWidth, _windowHeight,
        new List<PipelineAttachment>() { new PipelineAttachment(ShaderInput.Color, color_fw) },
        PipelineAttachment.NoFramebuffer,
        "v_v3x2_forward"
      );

      _pipelineStages = new List<PipelineStage>()
      {
        deferred,
        outline,
        deferredBlit,
        debug,
        forward,
        forwardBlit
      };
      for (var psi = 0; psi < _pipelineStages.Count; psi++)
      {
        _pipelineStages[psi].Index = psi;
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
      //Request to save FBOs after rendering is complete.
      _requestSaveFBOs = true;
    }
    public void BeginRenderToWindow()
    {
      //**FULL CLEAR
      GL.Viewport(0, 0, Gu.Context.GameWindow.Width, Gu.Context.GameWindow.Height);
      GL.Scissor(0, 0, Gu.Context.GameWindow.Width, Gu.Context.GameWindow.Height);
      foreach (var ps in this._pipelineStages)
      {
        ps.BeginRender(true);
        ps.EndRender();
      }
    }
    public void EndRenderToWindow()
    {
      Picker.UpdatePickedPixel();
      if (_requestSaveFBOs == true)
      {
        _requestSaveFBOs = false;
      }
    }
    private bool IsActiveStage(RenderView rv, PipelineStage ps)
    {
      //Skip non-ui stuff if we are not rendering UI
      //A more generic way to do this is to set the allowed pipeline stages on the UI mesh
      if (rv.ViewMode == RenderViewMode.UIOnly &&
          ps.PipelineStageEnum != PipelineStageEnum.Forward &&
          ps.PipelineStageEnum != PipelineStageEnum.ForwardBlit)
      {
        return false;
      }
      return true;
    }
    public void RenderViewToWindow(RenderView rv)
    {
      //Make sure the given view has a camera attached.
      if (BeginRenderToView(rv))
      {
        foreach (PipelineStage ps in _pipelineStages)
        {
          Gu.Prof($"Begin {ps.PipelineStageEnum.ToString()}");

          if (!IsActiveStage(rv, ps))
          {
            continue;
          }

          CurrentStage = ps;

          if (rv.BeginPipelineStage(ps))//Set P/V matrix
          {
            if (ps.BeginRender(false))//Bind FBO
            {

              Gu.World.RenderPipeStage(rv, CurrentStage.PipelineStageEnum);

              //If we are a blit stage, execute a blit.
              if (ps.BlitObj != null && ps.BlitMat != null)
              {
                //blit
                rv.BeginRender2D(ps.BlitMat);
                {
                  //Set the viewport to the whole window to blit the fullscreen quad however set the 
                  //scissor to be just the viewport area.
                  //TODO: it would make more sense to have the quad blit just to the given area, and not have to re-set the viewport.
                  //https://stackoverflow.com/questions/33718237/do-you-have-to-call-glviewport-every-time-you-bind-a-frame-buffer-with-a-differe

                  //This w/h should automatically be set to the size of the current output framebuffer
                  GL.Viewport(0, 0, ps.Size.width, ps.Size.height);
                  DrawCall.Draw(Gu.World.WorldProps, rv, ps.BlitObj);
                }
                rv.EndRender2D();
              }
              SaveFBOsPostRender();

              ps.EndRender();
              CurrentStage = null;
            }
          }
          Gu.Prof($"End {ps.PipelineStageEnum.ToString()}");

        }
      }
      EndRenderToView(rv);
    }
    public PipelineStage GetPipelineStage(int stageindex)
    {
      Gu.Assert(_pipelineStages != null && _pipelineStages.Count > 0 && stageindex < _pipelineStages.Count && stageindex >= 0);
      return _pipelineStages[stageindex];
    }

    #endregion
    #region Private:Methods
    private bool BeginRenderToView(RenderView rv)
    {
      Gu.Assert(RenderState == RendererState.EndView || RenderState == RendererState.None);
      RenderState = RendererState.BeginView;
      CurrentView = rv;
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
      rv.Overlay.EndFrame();
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
      _pipelineStages.Clear();
    }
    private void SetShadowEnv(/*LightManager lightman, */bool bSet)
    {

    }
    private void SetInitialGpuRenderState()
    {
      //A default state for a view. The state will also change by shaders and pipeline stages
      Gpu.CheckGpuErrorsDbg();
      Gu.Context.Gpu.SetState(new GpuRenderState()
      {
        CullFace = true,
        CullFaceMode = CullFaceMode.Back,
        FrontFaceDirection = FrontFaceDirection.Ccw,
        ScissorTest = true,
        DepthTest = true,
        Blend = false,
        DepthMask = true,
        BlendFunc = BlendEquationMode.FuncAdd,
        BlendFactor = BlendingFactor.OneMinusSrcAlpha,
      }, true);

    }
    public HashSet<PipelineAttachment> GetAllUniqueAttachments()
    {
      var x = new HashSet<PipelineAttachment>();
      foreach (var ps in _pipelineStages)
      {
        if (ps.OutputFramebuffer != null)
        {
          foreach (var binding in ps.OutputFramebuffer.Bindings)
          {
            x.Add(binding.Attachment);
          }
        }
      }
      return x;
    }
    private void SaveFBOsPostRender()
    {
      if (_requestSaveFBOs || Gu.EngineConfig.Debug_SaveFBOsEveryFrame)
      {
        SaveCurrentStageFBOsImmediately();
      }
    }
    private void SaveCurrentStageFBOsImmediately()
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
      if (CurrentStage != null)
      {
        string suffix = $" (ctx {ctxName}, view {CurrentView.Id}, date {Gu.GetFilenameDateTimeNOW()})";
        int n = 0;
        foreach (var output in CurrentStage.Outputs)// atts)
        {
          var pTarget = output.Attachment;
          string fname = $"{((int)CurrentStage.PipelineStageEnum).ToString()}|{CurrentStage.PipelineStageEnum.ToString()}|{output.ShaderOutput.ToString()}|{n} {suffix}.png";//names may be the same
          fname = System.IO.Path.Combine(Gu.LocalTmpPath, fname);
          Lib.SaveTexture(new FileLoc(fname, FileStorage.Disk), pTarget.Texture, true, true, -1, true);
          Gu.Log.Info("[Renderer] Screenshot '" + fname + "' saved");
          n++;

        }
      }
    }

    #endregion














  }

}
