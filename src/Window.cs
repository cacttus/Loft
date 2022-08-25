using System;
using System.Collections.Generic;
using System.ComponentModel;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace PirateCraft
{
  #region Enums

  public enum InputState
  {
    Play, //User is moving inw orld.
    Edit //User has inventory window open.
  }

  #endregion

  public class UiWindowBase : NativeWindow
  {
    #region Public:Members

    public bool IsMain { get; private set; } = false;
    public string Name { get; private set; } = "unset";
    public InputState InputState = InputState.Play;
    public bool IsLoaded { get; private set; } = false;
    public List<RenderView> RenderViews { get; set; } = new List<RenderView>();
    public Camera3D Camera
    {
      get
      {
        Gu.Assert(RenderViews != null);
        Gu.Assert(RenderViews.Count > 0);
        return RenderViews[0].Camera;
      }
    }
    #endregion
    #region Protected:Members

    protected WorldObject DefaultPlayer { get; set; } = null;
    protected GuiComponent Gui { get; private set; } = null;

    #endregion
    #region Private:Members

    private int _polygonMode = 0;

    #endregion
    #region Public:Methods

    public UiWindowBase(string name, bool isMain, float scale, WindowBorder border, bool visible, IGLFWGraphicsContext sharedCtx = null) : base(
       new NativeWindowSettings()
       {
         Profile = ContextProfile.Core,
         Flags = ContextFlags.Debug,
         AutoLoadBindings = true,
         APIVersion = new Version(4, 1),//BlendFuncSeparate>=4.0
         Title = "Slaver",
         StartFocused = true,
         StartVisible = visible,
         WindowState = WindowState.Normal,
         WindowBorder = border,
         Location = new OpenTK.Mathematics.Vector2i(200, 200),
         Size = new OpenTK.Mathematics.Vector2i((int)(1920 * scale), (int)(1080 * scale)),
         NumberOfSamples = 0, //TODO:
         StencilBits = 8,
         DepthBits = 24,
         RedBits = 8,
         GreenBits = 8,
         BlueBits = 8,
         AlphaBits = 8,
         SharedContext = sharedCtx
       }
    )
    {
      Name = name;
      IsMain = isMain;
      Title += ": OpenGL Version: " + GL.GetString(StringName.Version);

      //Register this window with the system. We load all window data in the main loop.
      CreateContext();
    }
    public virtual void Load()
    {
      var c = CreateDefaultCameraView(true);
      DefaultPlayer = CreateFPSPlayerView(c);
      Gu.World.AddObject(DefaultPlayer);
      RenderViews.Add(new RenderView(c, 0, 0, this.Size.X, this.Size.Y));

      CreateGUI();

      IsLoaded = true;
    }
    public void UpdateAsync()
    {
      OnUpdateInput();
      OnUpdateFrame();
      foreach (var rv in RenderViews)
      {
        OnView(rv);
      }
    }
    public void RenderAsync()
    {
      foreach (var v in this.RenderViews)
      {
        CullFrame(v);
        RenderFrame(v);
      }
    }

    #endregion
    #region Protected:Methods

    protected virtual void OnUpdateFrame()
    {
    }
    protected virtual void OnView(RenderView rv)
    {
    }
    protected override void OnClosing(CancelEventArgs e)
    {
      base.OnClosing(e);
    }
    protected override void OnClosed()
    {
      base.OnClosed();
    }
    protected override void OnResize(ResizeEventArgs e)
    {
      if (Camera != null)
      {
        // this event comes at random in the pipeline, Swap back context when the buffers changed.
        var ct = Gu.Context;

        Gu.SetContext(this);
        Camera.Viewport_Width = e.Width;
        Camera.Viewport_Height = e.Height;
        if (Camera.Viewport_Width <= 0 || Camera.Viewport_Height <= 0)
        {
          Gu.Log.Error("Window width and height are zero (OnResize), setting to 1");
          Camera.Viewport_Width = (Camera.Viewport_Width == 0) ? 1 : Camera.Viewport_Width;
          Camera.Viewport_Height = (Camera.Viewport_Height == 0) ? 1 : Camera.Viewport_Height;
        }
        Gu.Context.Renderer.ResizeScreenBuffers(Camera.Viewport_Width, Camera.Viewport_Height);
        if (ct != null)
        {
          Gu.SetContext(ct.GameWindow);
        }
      }
    }
    protected virtual void CreateGUI()
    {
      Gui = new GuiComponent(this.Camera);

      //Create the gui "object" and put it in front of the player so it doesn't get culled.
      var guiobj = Gu.World.CreateObject("guiobj", null, null);
      guiobj.Position_Local = new vec3(0, 0, 10);
      guiobj.ExclusiveRenderContext = Gu.Context;//Only render in this context. e.g. just for this window
      guiobj.Components.Add(Gui);
      Camera.AddChild(guiobj);
    }
    protected virtual void OnUpdateInput()
    {
      if (!IsFocused)
      {
        return;
      }

      if (Gu.Keyboard.PressOrDown(Keys.Escape))
      {
        Gu.CloseWindow(this);
      }
      DebugKeyboard();
      Menu();
    }
    private void DebugKeyboard()
    {
      if (Gu.Keyboard.Press(Keys.O))
      {
        Camera.RootParent.Position_Local = new vec3(0, 0, 0);
        Camera.RootParent.Velocity = vec3.Zero;
      }
      if (Gu.Keyboard.Press(Keys.F1))
      {
        Gu.Context.DebugDraw.DrawBoundBoxes = !Gu.Context.DebugDraw.DrawBoundBoxes;
      }
      if (Gu.Keyboard.Press(Keys.F2))
      {
        VSync = (VSync == VSyncMode.Off) ? VSyncMode.On : VSyncMode.Off;
      }
      if (Gu.Keyboard.Press(Keys.F3))
      {
        if (_polygonMode == 0)
        {
          GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
          _polygonMode = 1;
        }
        else if (_polygonMode == 1)
        {
          GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
          _polygonMode = 0;
        }
      }
      if (Gu.Keyboard.Press(Keys.F5))
      {
        Box3f.nugs = (Box3f.nugs + 1) % Box3f.maxnugs;
        //Material.DefaultDiffuse().Shader.GGX_X = (Material.DefaultDiffuse().Shader.GGX_X + 0.01f) % 3.0f;
      }
      if (Gu.Keyboard.PressOrDown(Keys.F6))
      {
        Gu.SaveFBOs();
      }
      if (Gu.Keyboard.Press(Keys.F7))
      {
        var w = new UiWindowBase("ui_popup" + Gu.Contexts.Count, false, 0.125f, WindowBorder.Resizable, true, this.Context);
      }
      if (Gu.Keyboard.Press(Keys.F8))
      {
        Gu.BreakRenderState = true;
        // if (CamMode == CamMode.Playing) { CamMode = CamMode.Flying; }
        // else if (CamMode == CamMode.Flying) { CamMode = CamMode.Playing; }
        // Player.Collides = !Player.Collides;
        // Player.HasGravity = !Player.HasGravity;
        // Player.AirFriction = MaxAirFriction - Player.AirFriction;// ; //Movement Damping
      }
      if (Gu.Keyboard.Press(Keys.F10))
      {
        OperatingSystem.ToggleShowConsole();
      }
      if (Gu.Keyboard.Press(Keys.F11))
      {
        if (this.WindowState == WindowState.Fullscreen)
        {
          WindowState = WindowState.Normal;
        }
        else
        {
          WindowState = WindowState.Fullscreen;
        }
      }

    }
    private void Menu()
    {
      if (Gu.Keyboard.Press(Keys.I))
      {
        if (InputState == InputState.Play)
        {
          //Stop rotating mouse. And let the super user take over
          InputState = InputState.Edit;
          CursorVisible = true;

        }
        else if (InputState == InputState.Edit)
        {
          InputState = InputState.Play;
          CursorVisible = false;
        }
      }
    }
    protected virtual void CullFrame(RenderView rv)
    {
      //TODO: you can have any number of cameras / areas in a window
      Gu.World.BuildAndCull(rv);
    }
    protected virtual void RenderFrame(RenderView rv)
    {
      Gu.Assert(rv != null);
      Gu.Assert(rv.Camera != null);

      Gu.Context.Renderer.Render(rv, new Dictionary<PipelineStageEnum,Action<double,RenderView>>(){});
      // Gu.Context.Renderer.BeginEverything_New(rv);
      // {
      //   Gu.Context.Renderer.BeginRenderDeferred();
      //   {
      //     Gu.World.RenderDeferred(Gu.Context.Delta, rv);
      //   }
      //   Gu.Context.Renderer.EndRenderDeferred();
      //   Gu.Context.Renderer.BlitDeferredRender(rv);

      //   Gu.Context.Renderer.BeginRenderForward();
      //   {
      //     Gu.World.RenderForward(Gu.Context.Delta, rv);
      //     Gu.World.RenderDebug(Gu.Context.Delta, rv);
      //   }
      //   Gu.Context.Renderer.EndRenderForward();
      //   Gu.Context.Renderer.BlitFinalRender(rv);
      // }
      // Gu.Context.Renderer.EndEverything_New(rv);

      Gu.Context.Gpu.ExecuteCallbacks_RenderThread(Gu.Context);
    }
    protected void CreateContext()
    {
      Gu.CreateContext(this);
    }
    protected Camera3D CreateDefaultCameraView(bool withModel)
    {
      var c = Gu.World.CreateCamera("default" + this.Name, this.Size.X, this.Size.Y, vec3.Zero);
      c.Far = 4000.0f;
      c.Position_Local = new vec3(0, .5f, 0);

      if (withModel)
      {
        if (Gu.Resources.LoadObject(new FileLoc("camera.glb", FileStorage.Embedded), out var cmod))
        {
          //sword.Rotation_Local *= quat.fromAxisAngle(new vec3(1, 0, 1).normalized(), MathUtils.M_PI_2 * 0.125f);
          cmod.ExcludeFromRenderContext = Gu.Context;
          c.AddChild(cmod);
        }
      }
      return c;
    }
    protected WorldObject CreateFPSPlayerView(Camera3D cam)
    {
      var p = Gu.World.CreateAndAddObject("player-empty", null, null);
      p.Collides = false;
      p.HasPhysics = true;
      p.HasGravity = false;
      p.AddChild(cam);
      p.Components.Add(new FPSInputComponent(this));
      return p;
    }

    #endregion
  }

  public class MainWindow : UiWindowBase
  {
    #region Private:Members

    private string VersionId = "0.01";
    private bool DELETE_WORLD_START_FRESH = true;
    private WorldObject _boxMeshThing = null;
    private int meshIdx = 0;
    private const float scale = 0.75f; //RESOLUTION scale
    private NativeWindowSettings _ns = NativeWindowSettings.Default;
    private WorldObject Sphere_Rotate_Quat_Test;
    private WorldObject Sphere_Rotate_Quat_Test2;
    private WorldObject Sphere_Rotate_Quat_Test3;
    private WorldObject pick = null;
    private WorldObject sword = null;
    private WorldObject left_hand = null;
    private WorldObject right_hand = null;
    private UiElement _lblDebugInfo = null;
    private UiElement _buttonTest = null;
    private Material[] testobjs = new Material[3];
    private vec3 second_y_glob = new vec3(2.5f, 2.0f, 2.5f);

    #endregion

    #region Public:Methods

    public MainWindow() : base("main", true, scale, WindowBorder.Resizable, true)
    {
      Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
    }
    public override void Load()
    {
      Gu.World = new World(Gu.GetContextForWindow(this));
      base.Load();
      InitMainWindow();
    }

    #endregion
    #region Protected:Methods

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
      base.OnMouseMove(e);
    }
    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
      base.OnFocusedChanged(e);
    }
    protected override void OnUpdateFrame()
    {
      //_boxMeshThing.Rotation = quaternion.FromAxisAngle(new vec3(0, 1, 0), (float)rot);
      //rot += Math.PI * 2.0f * Gu.CurrentWindowContext.Delta * 0.0125f;

      //checks out
      if (Sphere_Rotate_Quat_Test != null)
      {
        Sphere_Rotate_Quat_Test.Position_Local = new vec3(0, 3, 0);
        Sphere_Rotate_Quat_Test.Rotation_Local = quat.fromAxisAngle(new vec3(1, 1, 1).normalized(), (float)Gu.RotationPerSecond(10));
      }
      if (Sphere_Rotate_Quat_Test2 != null)
      {
        Sphere_Rotate_Quat_Test2.Position_Local = new vec3(-3, 3, 0);
        Sphere_Rotate_Quat_Test2.Rotation_Local = mat4.getRotation(new vec3(-1, -1, -1).normalized(), (float)Gu.RotationPerSecond(10)).toQuat();
      }
      if (Sphere_Rotate_Quat_Test3 != null)
      {
        Sphere_Rotate_Quat_Test3.Position_Local = new vec3(3, 3, 0);
        Sphere_Rotate_Quat_Test3.Rotation_Local = quat.fromAxisAngle(new vec3(1, 1, 1).normalized(), (float)Gu.RotationPerSecond(3));
      }
    }
    protected override void OnView(RenderView rv)
    {
      string build = "Release";
#if DEBUG
      build = "Debug";
#endif
      var info = ""
          + $"{build}\n"
          + $"(Cam = {rv.Camera.Position_World.ToString(2)})\n"
          + $"FPS: {(int)Gu.Context.Fps}\n"
          + $"nyugs b: {Box3f.nugs}\n"
          + $"Globs: {Gu.World.NumGlobs}\n"
          + $"Visible Glob: {Gu.World.NumVisibleRenderGlobs}\n"
          + $"DrawElements_Frame:{MeshData.dbg_numDrawElements_Frame}\n"
          + $"Arrays_Frame: {MeshData.dbg_numDrawArrays_Frame}\n"
          + $"OBs culled:{Gu.World.NumCulledObjects}\n"
          + $"Mouse:{Gu.Mouse.Pos.x},{Gu.Mouse.Pos.y}\n"
          + $"Memory:{StringUtil.FormatPrec((float)System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024, 2)}MB\n"
          + $"UI Update:{Gui.Screen.UpdateMs}ms\n"
          + $"UI pick:{Gui.Screen.PickMs}ms\n"
          + $"UI mesh:{Gui.Screen.MeshMs}ms\n"
          + $"UI obj events:{Gui.Screen.ObjectEventsMs}ms\n"
          + $"UI window events:{Gui.Screen.WindowEventsMs}ms\n"
          + $"UI tot:{Gui.Screen.MeshMs + Gui.Screen.UpdateMs + Gui.Screen.PickMs}ms\n"
          + $"Picked Ob:{Gu.Context.Renderer.Picker.PickedObjectName}\n"
          + $"Picked World Ob:{Gu.Context.Renderer.Picker.PickedWorldObjectName}\n"
          ;

      //UI Test
      if (_lblDebugInfo != null)
      {
        _lblDebugInfo.Text = info;
      }
    }
    protected override void CreateGUI()
    {
      base.CreateGUI();

      var tb = Gui.CreatePanel("tb", null, null);
      tb.InlineStyle.MinWHPX = new vec2(0, 25);
      tb.InlineStyle.SizeModeWidth = UiSizeMode.Expand;
      tb.InlineStyle.Color = vec4.rgba_ub(35, 47, 62, 255);

      var file = Gui.CreateButton("tbb1", null, "File", (i, e, m) =>
      {
        e.InlineStyle.BorderRadius += 5;
      });
      file.InlineStyle.DisplayMode = UiDisplayMode.Inline;

      var options = Gui.CreateButton("tbb2", null, "Options", (i, e, m) => { });
      options.InlineStyle.DisplayMode = UiDisplayMode.Inline;

      var showdb = Gui.CreateButton("opts", null, "ShowDbg", (i, e, m) =>
      {
        Gui.Screen.DebugDraw.DisableClip = !Gui.Screen.DebugDraw.DisableClip;
        Gui.Screen.DebugDraw.ShowOverlay = !Gui.Screen.DebugDraw.ShowOverlay;
      });
      showdb.InlineStyle.DisplayMode = UiDisplayMode.Inline;

      tb.AddChild(file);
      tb.AddChild(options);
      tb.AddChild(showdb);
      tb.AddChild(Gui.CreateButton("rough+", null, "rough+", (i, e, m) =>
            {
              testobjs[0].Roughness = Math.Min(testobjs[0].Roughness + 0.1f, 0.9999f);
              testobjs[1].Roughness = Math.Min(testobjs[1].Roughness + 0.1f, 0.9999f);
              testobjs[2].Roughness = Math.Min(testobjs[2].Roughness + 0.1f, 0.9999f);
            }));
      tb.AddChild(Gui.CreateButton("rough-", null, "rough-", (i, e, m) =>
            {
              testobjs[0].Roughness = Math.Max(testobjs[0].Roughness - 0.1f, 0.0001f);
              testobjs[1].Roughness = Math.Max(testobjs[1].Roughness - 0.1f, 0.0001f);
              testobjs[2].Roughness = Math.Max(testobjs[2].Roughness - 0.1f, 0.0001f);
            }));
      tb.AddChild(Gui.CreateButton("spec+", null, "spec+", (i, e, m) =>
{
  testobjs[0].Specular = Math.Min(testobjs[0].Specular + 0.1f, 0.9999f);
  testobjs[1].Specular = Math.Min(testobjs[1].Specular + 0.1f, 0.9999f);
  testobjs[2].Specular = Math.Min(testobjs[2].Specular + 0.1f, 0.9999f);
}));
      tb.AddChild(Gui.CreateButton("spec-", null, "spec-", (i, e, m) =>
            {
              testobjs[0].Specular = Math.Max(testobjs[0].Specular - 0.1f, 0.0001f);
              testobjs[1].Specular = Math.Max(testobjs[1].Specular - 0.1f, 0.0001f);
              testobjs[2].Specular = Math.Max(testobjs[2].Specular - 0.1f, 0.0001f);
            }));
      Gui.Screen.AddChild(tb);

      var dragpanel = Gui.CreateLabel("drag", new vec2(400, 400), "DragMe", true, FontFace.Fancy, 35, vec4.rgba_ub(8, 70, 100, 255));
      dragpanel.InlineStyle.MaxWHPX = new vec2(100, 100);
      dragpanel.InlineStyle.Border = 2;
      dragpanel.InlineStyle.BorderRadius = 10;
      dragpanel.InlineStyle.BorderColor = vec4.rgba_ub(220, 119, 12, 180);
      dragpanel.InlineStyle.Color = vec4.rgba_ub(240, 190, 100, 200);
      dragpanel.InlineStyle.SizeModeWidth = UiSizeMode.Shrink;
      dragpanel.InlineStyle.SizeModeHeight = UiSizeMode.Shrink;
      dragpanel.InlineStyle.MaxWHPX = null;
      dragpanel.EnableDrag((v) =>
      {
        dragpanel.InlineStyle.Left += v.x;
        dragpanel.InlineStyle.Top += v.y;
        dragpanel.Text = "Thanks for dragging.";
      });
      Gui.Screen.AddChild(dragpanel);
      Gui.Screen.AddChild(_lblDebugInfo = Gui.CreateLabel("debugInfo", null, "testxx", false, FontFace.Mono, 25));

      var test = Gui.CreateLabel("scrollva", null, "Scroll Val:", true, FontFace.Pixel, 25, vec4.rgba_ub(18, 70, 90, 255));
      Gui.Screen.AddChild(test);

      var cont = Gui.CreateScrollbar("scrollbar", false, (f) =>
      {
        test.Text = "Scroll Val:" + f;
      });
      Gui.Screen.AddChild(cont);
    }

    #endregion
    #region Private:Methods

    private void InitMainWindow()
    {
#if DEBUG
      OperatingSystem.ShowConsole();
#else
        OperatingSystem.HideConsole();
#endif
      Title = "Slaver " + VersionId.ToString();

      DefaultPlayer.Position_Local = second_y_glob;
      Gu.World.Initialize("MyWorld", DELETE_WORLD_START_FRESH, 2);
      Gu.Context.DebugDraw.DrawBoundBoxes = false;

      TestCreateDebugObjects();
      InitHandObjects();
      CreateCrosshair();
      CreateSky();
      CreateLight();

      CursorVisible = true;
    }
    private void CreateLight()
    {
      var l = new Light("pt");
      l.Radius = 1000;
      l.Power = 1;
      l.Position_Local = new vec3(0, 10, 0);
      Gu.World.AddObject(l);

    }
    private void InitHandObjects()
    {
      vec3 right_pos = new vec3(3.0f, -2.1f, 4.5f);
      vec3 left_pos = new vec3(-3.0f, -2.1f, 4.5f);

      List<WorldObject> objs;
      objs = Gu.Resources.LoadObjects(new FileLoc("pick.glb", FileStorage.Embedded));
      if (objs?.Count > 0)
      {
        pick = objs[0];
        pick.Position_Local = left_pos;
        pick.Scale_Local *= new vec3(0.7f, 0.7f, 0.7f);
        pick.Rotation_Local *= quat.fromAxisAngle(new vec3(0, 1, 0), MathUtils.M_PI * 0.5f);
        pick.Rotation_Local *= quat.fromAxisAngle(new vec3(1, 0, 1), MathUtils.M_PI_2 * 0.125f);

        var cmp = new AnimationComponent();
        vec3 raxis = new vec3(-1, 1, 0);
        float t_seconds = 0.5f; // swipe time in seconds
        float kf_count = 3;
        cmp.KeyFrames.Add(new Keyframe(t_seconds / kf_count * 0, mat3.getRotation(raxis, 0).toQuat()));
        cmp.KeyFrames.Add(new Keyframe(t_seconds / kf_count * 1, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 * 0.5 - 0.001)).toQuat()));
        cmp.KeyFrames.Add(new Keyframe(t_seconds / kf_count * 2, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 - 0.002)).toQuat())); ;
        pick.Components.Add(cmp);

        this.Camera.AddChild(pick);
      }
      objs = Gu.Resources.LoadObjects(new FileLoc("sword.glb", FileStorage.Embedded));
      if (objs?.Count > 0)
      {
        sword = objs[0];
        sword.Position_Local = right_pos;
        sword.Scale_Local *= new vec3(0.7f, 0.7f, 0.7f);
        sword.Rotation_Local *= quat.fromAxisAngle(new vec3(1, 0, 1).normalized(), MathUtils.M_PI_2 * 0.125f);

        this.Camera.AddChild(sword);
      }
    }
    private void CreateCrosshair()
    {
      vec4 ch_c = new vec4(0.31f, 0, 0, .1f);
      float size = 0.08f;
      v_v3c4[] verts = new v_v3c4[] {
           new v_v3c4() { _v = new vec3(-size, 0, 0), _c =  ch_c },
           new v_v3c4() { _v = new vec3(size, 0, 0), _c =   ch_c },
           new v_v3c4() { _v = new vec3(0, -size, 0), _c =  ch_c },
           new v_v3c4() { _v = new vec3(0, size, 0), _c =   ch_c  }
         };
      WorldObject Crosshair = new WorldObject("crosshair");
      Crosshair.Mesh = new MeshData("crosshair_mesh", PrimitiveType.Lines, Gpu.CreateVertexBuffer("crosshair", verts.ToArray()));
      Crosshair.Mesh.DrawOrder = DrawOrder.Last;
      Crosshair.Position_Local = new vec3(0, 0, 3);
      Material crosshair_mat = new Material("crosshair", Shader.DefaultFlatColorShader());
      crosshair_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      crosshair_mat.GpuRenderState.Blend = true;
      Crosshair.Material = crosshair_mat;
      this.Camera.AddChild(Crosshair);
    }
    private void CreateSky()
    {
      Texture2D tx_sky = Gu.Resources.LoadTexture(new FileLoc("hdri_sky2.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture2D tx_sky_stars = Gu.Resources.LoadTexture(new FileLoc("hdri_stars.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture2D tx_sun = Gu.Resources.LoadTexture(new FileLoc("tx64_sun.png", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture2D tx_moon = Gu.Resources.LoadTexture(new FileLoc("tx64_moon.png", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture2D tx_bloom = Gu.Resources.LoadTexture(new FileLoc("bloom.png", FileStorage.Embedded), true, TexFilter.Trilinear);

      //Sky
      //, tx_sky_stars -- TODO:
      // Gu.Resources.LoadShader("v_sky", false, FileStorage.Embedded)
      Material sky_mat = new Material("sky", Shader.DefaultObjectShader(), tx_sky);
      sky_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      //TODO: flat shade thing.
      var sky = Gu.World.CreateAndAddObject("sky", MeshData.GenSphere(DayNightCycle.SkyRadius, 128, 128, true, true), sky_mat);
      sky.Mesh.DrawOrder = DrawOrder.First;
      sky.Mesh.DrawMode = DrawMode.Deferred;
      //sky.Constraints.Add(new FollowConstraint(Player, FollowConstraint.FollowMode.Snap)); ;
      sky.OnUpdate = (obj) =>
      {
        //Kind of sloppy way to do this whole thing. 
        sky_mat.BaseColor =
        new vec4(
          (float)Gu.World.WorldProps.DayNightCycle.SkyColor.x,
          (float)Gu.World.WorldProps.DayNightCycle.SkyColor.y,
          (float)Gu.World.WorldProps.DayNightCycle.SkyColor.z,
          1
          );
        vec3 pe = DefaultPlayer.WorldMatrix.ExtractTranslation();
        obj.Position_Local = pe;

        //TODO:
        //sky_mat.SetUniform("_ufSkyBlend")
      };

      //Empty that rotates the sun / moon
      var sun_moon_empty = Gu.World.CreateObject("sun_moon_empty", null, null);
      sun_moon_empty.OnView = (obj, rv) =>
      {
        double ang = Gu.World.WorldProps.DayNightCycle.DayTime_Seconds / Gu.World.WorldProps.DayNightCycle.DayLength_Seconds * Math.PI * 2.0;
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), (float)ang);

        vec3 pe = rv.Camera.Position_World;//.WorldMatrix.ExtractTranslation();
        obj.Position_Local = pe;
      };
      Gu.World.AddObject(sun_moon_empty);
      /*
      view update action
      for all ob
      ob.OnUpdateForView(rv)
      ob.OnBeforeRender()


      */
      Material sun_moon_mat = new Material("sunmoon", Gu.Resources.LoadShader("v_sun_moon", false, FileStorage.Embedded));
      sun_moon_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      sun_moon_mat.GpuRenderState.CullFace = false;//Disable depth test.
      sun_moon_mat.GpuRenderState.Blend = false;

      float sun_size = 13;
      float moon_size = 23;

      //Sun
      var sun_mat = sun_moon_mat.Clone();
      sun_mat.AlbedoSlot.Texture = tx_sun;
      var sun = Gu.World.CreateObject("sun", MeshData.GenPlane(sun_size, sun_size), sun_mat);
      sun.Mesh.DrawOrder = DrawOrder.First;
      sun.OnUpdate = (obj) =>
      {
        sun_mat.BaseColor = new vec4(.994f, .990f, .8f, 1);
        obj.Position_Local = new vec3(DayNightCycle.SkyRadius, 0, 0);
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), (float)Math.PI / 2);
      };
      sun_moon_empty.AddChild(sun);


      var bloom_mat = sun_moon_mat.Clone();
      bloom_mat.AlbedoSlot.Texture = tx_bloom;
      var sun_bloom = Gu.World.CreateObject("sun_bloom", MeshData.GenPlane(sun_size, sun_size), bloom_mat);
      sun_bloom.Mesh.DrawOrder = DrawOrder.First;
      sun_bloom.OnUpdate = (obj) =>
      {
        vec3 dir = Gu.World.WorldProps.DayNightCycle.MoonDir.ToVec3();
        //ease multiplier so that the glare does not show on the horizon.
        float horizon_mul = (float)MathUtils.Ease(0, 1, (double)dir.dot(new vec3(0, 1, 0)));
        float bloom_dp = dir.dot(this.Camera.BasisZ);
        float bloom_dp_pw = (float)Math.Pow(bloom_dp, 64);
        bloom_mat.BaseColor = new vec4(sun_mat.BaseColor.x, sun_mat.BaseColor.y, sun_mat.BaseColor.z, bloom_dp_pw * horizon_mul * 0.9413f);
        obj.Scale_Local = new vec3(1.1f + bloom_dp * 30.0f, 0, 1.1f + bloom_dp * 30.0f);
      };
      sun.AddChild(sun_bloom);


      //Moon
      var moon_mat = sun_moon_mat.Clone();
      moon_mat.AlbedoSlot.Texture = tx_moon;
      var moon = Gu.World.CreateObject("moon", MeshData.GenPlane(moon_size, moon_size), moon_mat);
      moon.Mesh.DrawOrder = DrawOrder.First;
      moon.OnUpdate = (obj) =>
      {
        moon_mat.BaseColor = new vec4(.78f, .78f, .92f, 1);
        obj.Position_Local = new vec3(-DayNightCycle.SkyRadius, 0, 0);
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), -(float)Math.PI / 2);
      };
      sun_moon_empty.AddChild(moon);


      var moon_bloom = Gu.World.CreateObject("moon_bloom", MeshData.GenPlane(moon_size, moon_size), bloom_mat);
      moon_bloom.Mesh.DrawOrder = DrawOrder.First;
      moon_bloom.OnUpdate = (obj) =>
      {
        vec3 dir = Gu.World.WorldProps.DayNightCycle.MoonDir.ToVec3() * -1.0f;
        //ease multiplier so that the glare does not show on the horizon.
        float horizon_mul = (float)MathUtils.Ease(0, 1, (double)dir.dot(new vec3(0, 1, 0)));
        float bloom_dp = dir.dot(this.Camera.BasisZ);
        float bloom_dp_pw = (float)Math.Pow(bloom_dp, 64);
        obj.Material.BaseColor = new vec4(moon_mat.BaseColor.x, moon_mat.BaseColor.y, moon_mat.BaseColor.z, bloom_dp_pw * horizon_mul * 0.3f);
        obj.Scale_Local = new vec3(1.1f + bloom_dp * 4.0f, 0, 1.1f + bloom_dp * 4.0f);
      };
      moon.AddChild(moon_bloom);


    }
    private void TestCreateDebugObjects()
    {
      //Textures
      //   Texture2D noise = Noise3D.TestNoise();
      Texture2D tx_peron = Gu.Resources.LoadTexture(new FileLoc("main char.png", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D tx_grass = Gu.Resources.LoadTexture(new FileLoc("grass_base.png", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D tx_mclovin = Gu.Resources.LoadTexture(new FileLoc("mclovin.jpg", FileStorage.Embedded), true, TexFilter.Nearest);
      Texture2D tx_usopp = Gu.Resources.LoadTexture(new FileLoc("usopp.jpg", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D tx_hogback = Gu.Resources.LoadTexture(new FileLoc("hogback.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);

      //Objects
      //Integrity test of GPU memory.
      //for (int i = 0; i < 100; ++i)
      //{
      //  Gu.World.CreateAndAddObject("BoxMesh", MeshData.GenBox(1, 1, 1), new Material(Shader.DefaultDiffuse(), noise));
      //}
      //for (int i = 1; i < 100; ++i)
      //{
      //  Gu.World.RemoveObject("BoxMesh-" + i.ToString());
      //}
      Gu.World.CreateAndAddObject("TextureFront", MeshData.GenTextureFront(this.Camera, 0, 0, this.Size.X, this.Size.Y), new Material("texturefront", Shader.DefaultObjectShader(), tx_peron));
      Gu.World.CreateAndAddObject("Plane.", MeshData.GenPlane(10, 10), new Material("plane", Shader.DefaultObjectShader(), tx_grass));
      //  _boxMeshThing = Gu.World.FindObject("BoxMesh");
      //_boxMeshThing.Position = new vec3(0, _boxMeshThing.BoundBoxMeshBind.Height() * 0.5f, 0);

      testobjs[0] = new Material("sphere_rot", Shader.DefaultObjectShader(), tx_mclovin);
      testobjs[1] = new Material("sphere_rot2", Shader.DefaultObjectShader(), tx_usopp);
      testobjs[2] = new Material("sphere_rot3", Shader.DefaultObjectShader(), tx_hogback);

      Sphere_Rotate_Quat_Test = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test", MeshData.GenSphere(1), testobjs[0]);
      Sphere_Rotate_Quat_Test2 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test2", MeshData.GenEllipsoid(new vec3(1f, 1, 1f), 128, 128, true), testobjs[1]);
      Sphere_Rotate_Quat_Test3 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test3", MeshData.GenEllipsoid(new vec3(1, 1, 1), 128, 128, true), testobjs[2]);

      //Test STB laoding EXR images.
      Texture2D tx_exr = Gu.Resources.LoadTexture(new FileLoc("hilly_terrain_01_2k.hdr", FileStorage.Embedded), true, TexFilter.Bilinear);
      var exr_test = MeshData.GenPlane(10, 10);
      var exr_test_mat = new Material("plane", Shader.DefaultObjectShader(), tx_exr);
      var exr_test_ob = Gu.World.CreateAndAddObject("EXR test", exr_test, exr_test_mat);
      exr_test_ob.Position_Local = new vec3(10, 10, 5);

      //Animation test
      var cmp = new AnimationComponent();
      vec3 raxis = new vec3(0, 1, 0);
      cmp.KeyFrames.Add(new Keyframe(0, mat3.getRotation(raxis, 0).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 0, 0), KeyframeInterpolation.Ease));
      cmp.KeyFrames.Add(new Keyframe(1, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 * 0.5 - 0.001)).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 1, 0), KeyframeInterpolation.Ease));
      cmp.KeyFrames.Add(new Keyframe(2, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 - 0.002)).toQuat(), KeyframeInterpolation.Slerp, new vec3(1, 1, 0), KeyframeInterpolation.Ease)); ;
      cmp.KeyFrames.Add(new Keyframe(3, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 + MathUtils.M_PI_2 * 0.5 - 0.004)).toQuat(), KeyframeInterpolation.Slerp, new vec3(1, 0, 0), KeyframeInterpolation.Ease)); ;
      cmp.KeyFrames.Add(new Keyframe(4, mat3.getRotation(raxis, (float)(MathUtils.M_PI * 2 - 0.006)).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 0, 0), KeyframeInterpolation.Ease)); ;
      cmp.Play();
      Sphere_Rotate_Quat_Test.Components.Add(cmp);

      //Some fun parenting stuff.
      //  Sphere_Rotate_Quat_Test.AddChild(Sphere_Rotate_Quat_Test2.AddChild(Sphere_Rotate_Quat_Test3.AddChild(_boxMeshThing)));
    }
    private vec2 Get_Interaction_Pos()
    {
      //Depending on game mode
      //Returns either world editing projection position, or, the mouse position if we are in inventory
      vec2 projec_pos = new vec2(0, 0);
      if (this.InputState == InputState.Edit)
      {
        projec_pos = Gu.Mouse.Pos;
      }
      else if (this.InputState == InputState.Play)
      {
        projec_pos = new vec2(this.Camera.Viewport_Width * 0.5f, this.Camera.Viewport_Height * 0.5f);
      }
      else
      {
        Gu.Log.Error("Invalid projection position for raycast blocks.");
      }
      return projec_pos;
    }

    #endregion

  }

}