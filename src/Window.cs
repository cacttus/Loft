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

  public class UiWindowBase : NativeWindow
  {
    #region Public:Members

    public bool IsMain { get; private set; } = false;
    public string Name { get; private set; } = "unset";
    public bool IsLoaded { get; private set; } = false;
    public List<RenderView> RenderViews { get; set; } = new List<RenderView>();
    public int Width { get; private set; } = 1;//Do not use Size.X, Y There is currently an OpenTK bug where it does not update on Resize
    public int Height { get; private set; } = 1;
    public bool DrawWorld { get; set; } = true;

    protected List<PipelineStageEnum> _pipelineStages = null; //set this to render only to specific stages.
    protected InfoWindow InfoWindow { get; set; } = null;
    //protected int iActiveView { get; set; } = 0;

    public RenderView ActiveView { get; private set; } = null;
    public void SetActiveView()
    {
      ActiveView = null;
      //pick the active view based on cursor location
      foreach (var rv in this.RenderViews)
      {
        if (rv.Viewport.Contains_Point_Window_Relative_BR_Exclusive(Gu.Mouse.Pos))
        {
          if (ActiveView != null)
          {
            Gu.Log.Error("Multiple viewports picked!!" + ActiveView.Name + ", and " + rv.Name);
            Gu.DebugBreak();
          }
          ActiveView = rv;
        }
      }
    }
    public new OpenTK.Mathematics.Vector2i Size
    {
      get
      {
        Gu.Log.Error("Do not use Size, it is incorrect in OpenTK.");
        Gu.DebugBreak();
        return base.Size;
      }
    }
    // public RenderView ActiveView
    // {
    //   //So this is the view that is being interacted with by the user.
    //   //Mouse click / etc.
    //   get
    //   {
    //     if (iActiveView >= RenderViews.Count || iActiveView < 0)
    //     {
    //       return null;
    //     }
    //     return this.RenderViews[iActiveView];
    //   }
    // }
    public Camera3D ActiveViewCamera
    {
      //returns the camera for the given active view, or null if there is
      // 1. no active view
      // 2. the active view has no camera attached.
      get
      {
        var v = ActiveView;
        if (v != null)
        {
          if (v.Camera != null & v.Camera.TryGetTarget(out var cm))
          {
            return cm;
          }
        }
        return null;
      }
    }
    #endregion
    #region Private:Members

    private int _polygonMode = 0;

    #endregion
    #region Public:Methods

    public UiWindowBase(string title, bool isMain, ivec2 pos, ivec2 size, vec2? scale = null, WindowBorder border = WindowBorder.Resizable, bool visible = true, IGLFWGraphicsContext sharedCtx = null) : base(
       new NativeWindowSettings()
       {
         Profile = ContextProfile.Core,//Forward compati
         Flags = ContextFlags.Debug,
         AutoLoadBindings = true,
         APIVersion = new Version(4, 1),//BlendFuncSeparate>=4.0
         Title = "Slaver",
         StartFocused = true,
         StartVisible = visible,
         WindowState = WindowState.Normal,
         WindowBorder = border,
         Location = new OpenTK.Mathematics.Vector2i(pos.x, pos.y),
         Size = new OpenTK.Mathematics.Vector2i((int)(size.x * ((scale != null) ? scale.Value.x : 1.0f)), (int)(size.y * ((scale != null) ? scale.Value.y : 1.0f))),
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
      Gu.Log.Info("Creating window " + title);
      Name = title;
      Title = title;
      IsMain = isMain;

      Width = base.Size.X;
      Height = base.Size.Y;
      //Register this window with the system. We load all window data in the main loop.
      Gu.CreateContext("ctx" + Gu.Contexts.Count, this);
    }
    private void CreateCameraView(vec2 xy, vec2 wh)
    {
      string viewname = "renderview-" + RenderViews.Count;
      //Create View
      var v = new RenderView(viewname, xy, wh, this.Width, this.Height);
      RenderViews.Add(v);

      //Create Camera
      var c = Gu.World.CreateCamera("cam-def-" + viewname, v, vec3.Zero);
      c.Far = 4000.0f;
      c.Position_Local = new vec3(0, .5f, 0);

      if (Gu.Resources.LoadObject(new FileLoc("camera.glb", FileStorage.Embedded), out var cmod))
      {
        //sword.Rotation_Local *= quat.fromAxisAngle(new vec3(1, 0, 1).normalized(), MathUtils.M_PI_2 * 0.125f);
        cmod.ExcludeFromRenderView = new WeakReference<RenderView>(v);
        c.AddChild(cmod);
      }

      //Create View Player & Input (ViewPlayer)
      var p = Gu.World.CreateAndAddObject("player-empty-" + viewname, null, null);
      p.Collides = false;
      p.HasPhysics = true;
      p.HasGravity = false;
      p.Position_Local = new vec3(0, 10, 0);
      p.AddChild(c);
      p.Components.Add(new FPSInputComponent(v));

      //Set the view Camera
      v.Camera = new WeakReference<Camera3D>(c);

      //Do Callbacks
      OnCreateEditGUI(v);
      OnCreateGameGUI(v);
      OnCreateCamera(c);
    }
    public virtual void Load()
    {
      SetGameMode(Gu.World.GameMode);
      IsLoaded = true;
    }
    // public void SetActiveView()
    // {
    //   if (iActiveView >= 0 && iActiveView < RenderViews.Count)
    //   {
    //     RenderViews[iActiveView].SetCurrent();
    //   }
    //   else
    //   {
    //     //   Gu.Log.Debug("TODO: fix this");
    //     //Camera.View = null;
    //   }
    // }
    public void UpdateAsync()
    {
      OnUpdateInput();
      OnUpdateFrame();
      foreach (var rv in RenderViews)
      {
        rv.SetCurrent();
        if (IsFocused)
        {
          rv.ActiveGui?.Pick();
        }
        OnView(rv);
        rv.ActiveGui?.Update(Gu.Context.Delta);
      }
    }
    public void RenderAsync()
    {
      Gu.Context.Renderer.BeginRenderToWindow();
      foreach (var rv in this.RenderViews)
      {
        Gu.Assert(rv != null);
        Gu.Assert(rv.Camera != null);

        rv.SetCurrent();

        //Skiping this skips rendering the world to the scren.
        if (_pipelineStages == null || _pipelineStages.Contains(PipelineStageEnum.Deferred))
        {
          CullView(rv);
        }

        Gu.Context.Renderer.RenderViewToWindow(rv, _pipelineStages);
      }
      Gu.Context.Renderer.EndRenderToWindow();
      Gu.Context.DebugDraw.EndFrame();
      Gu.Context.GameWindow.Context.SwapBuffers();
      Gu.Context.Gpu.ExecuteCallbacks_RenderThread(Gu.Context);
    }

    #endregion
    #region Protected:Methods

    protected void SetGameMode(GameMode g)
    {
      //Global Game mode
      Gu.World.GameMode = g;

      //Destroy all views / cams
      foreach (var rv in RenderViews)
      {
        if (rv.Camera != null && rv.Camera.TryGetTarget(out var c))
        {
          if (c.RootParent != null)
          {
            Gu.World.DestroyObject(c.RootParent);
          }
        }
      }
      RenderViews.Clear();
      GC.Collect();

      //Create new view
      if (Gu.World.GameMode == GameMode.Edit)
      {
        if (Gu.World.EditState.EditView == 1)
        {
          CreateCameraView(new vec2(0.0f, 0.0f), new vec2(1.0f, 1.0f));
        }
        else if (Gu.World.EditState.EditView == 2)
        {
          CreateCameraView(new vec2(0.0f, 0.0f), new vec2(0.5f, 1.0f));
          CreateCameraView(new vec2(0.5f, 0.0f), new vec2(1.0f, 1.0f));
        }
        else if (Gu.World.EditState.EditView == 3)
        {
          CreateCameraView(new vec2(0.0f, 0.0f), new vec2(0.5f, 0.5f));
          CreateCameraView(new vec2(0.5f, 0.0f), new vec2(1.0f, 0.5f));
          CreateCameraView(new vec2(0.0f, 0.5f), new vec2(1.0f, 1.0f));
        }
        else if (Gu.World.EditState.EditView == 4)
        {
          //4-up
          CreateCameraView(new vec2(0.0f, 0.0f), new vec2(0.5f, 0.5f));
          CreateCameraView(new vec2(0.5f, 0.0f), new vec2(1.0f, 0.5f));
          CreateCameraView(new vec2(0.0f, 0.5f), new vec2(0.5f, 1.0f));
          CreateCameraView(new vec2(0.5f, 0.5f), new vec2(1.0f, 1.0f));
        }
      }
      else if (Gu.World.GameMode == GameMode.Play)
      {
        CreateCameraView(new vec2(0.0f, 0.0f), new vec2(1.0f, 1.0f));
      }

      //Set / Update GUI, Set Input Mode
      foreach (var rv in RenderViews)
      {
        if (g == GameMode.Play)
        {
          rv.ActiveGui = rv.GameGui;
          rv.ViewInputMode = ViewInputMode.Play;
        }
        else if (g == GameMode.Edit)
        {
          rv.ActiveGui = rv.EditGui;
          rv.ViewInputMode = ViewInputMode.Edit;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }

        rv.ActiveGui?.SetLayoutChanged();
      }
    }
    protected void ToggleGameMode()
    {
      if (Gu.World.GameMode == GameMode.Edit)
      {
        SetGameMode(GameMode.Play);
      }
      else if (Gu.World.GameMode == GameMode.Play)
      {
        SetGameMode(GameMode.Edit);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
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
      Gu.Log.Info("Resize Window " + this.Name + " " + e.Width + "," + e.Height);

      // this event comes at random in the pipeline, Swap back context when the buffers changed.
      var ct = Gu.Context;
      Gu.SetContext(this);

      Width = e.Width;
      Height = e.Height;
      if (Width <= 0 || Height <= 0)
      {
        Gu.Log.Error("Window width and height are zero (OnResize), setting to 1");
        if (Width <= 0) Width = 1;
        if (Height <= 0) Height = 1;
      }

      foreach (var rv in RenderViews)
      {
        rv.OnResize(Width, Height);
      }
      Gu.Context.Renderer.ResizeScreenBuffers(Width, Height);

      if (ct != null)
      {
        Gu.SetContext(ct.GameWindow);
      }
    }
    protected virtual void OnCreateEditGUI(RenderView rv)
    {
    }
    protected virtual void OnCreateGameGUI(RenderView rv)
    {
    }
    protected virtual void OnCreateCamera(Camera3D c)
    {
    }
    protected virtual void CreateGUI2DEBUG(RenderView rv)
    {
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
    }
    private void IterateActiveViews(Action<RenderView> act)
    {
      //Iterates over the given active view, or all views if "global" is currently selected
      if (ActiveView != null)
      {
        act(ActiveView);
      }
      else
      {
        foreach (var rv in RenderViews)
        {
          act(rv);

        }
      }
    }

    private void DebugKeyboard()
    {
      if (Gu.Keyboard.Press(Keys.O))
      {
        if (ActiveViewCamera != null)
        {
          ActiveViewCamera.RootParent.Position_Local = new vec3(0, 0, 0);
          ActiveViewCamera.RootParent.Velocity = vec3.Zero;
        }
      }

      if (Gu.World.GameMode == GameMode.Edit)
      {
        if (Gu.Keyboard.Press(Keys.D1))
        {
          Gu.World.EditState.EditView = 1;
          SetGameMode(Gu.World.GameMode);
        }
        else if (Gu.Keyboard.Press(Keys.D2))
        {
          Gu.World.EditState.EditView = 2;
          SetGameMode(Gu.World.GameMode);
        }
        else if (Gu.Keyboard.Press(Keys.D3))
        {
          Gu.World.EditState.EditView = 3;
          SetGameMode(Gu.World.GameMode);
        }
        else if (Gu.Keyboard.Press(Keys.D4))
        {
          Gu.World.EditState.EditView = 4;
          SetGameMode(Gu.World.GameMode);
        }
      }
      if (Gu.Keyboard.Press(Keys.F1))
      {
        ToggleGameMode();
      }
      if (Gu.Keyboard.Press(Keys.F2))
      {
        VSync = (VSync == VSyncMode.Off) ? VSyncMode.On : VSyncMode.Off;
      }
      if (Gu.Keyboard.Press(Keys.F3))
      {
        if (InfoWindow == null)
        {
          InfoWindow = new InfoWindow(new ivec2(200, 200), new ivec2(500, 400));
        }
        IterateActiveViews((rv) =>
        {
          if (_polygonMode == 0)
          {

            rv.PolygonMode = PolygonMode.Line;
            _polygonMode = 1;
          }
          else if (_polygonMode == 1)
          {
            rv.PolygonMode = PolygonMode.Fill;
            _polygonMode = 0;
          }
        });
      }
      if (Gu.Keyboard.Press(Keys.F4))
      {
        Gu.Context.DebugDraw.DrawBoundBoxes = !Gu.Context.DebugDraw.DrawBoundBoxes;
      }
      if (Gu.Keyboard.Press(Keys.F5))
      {
        //Box3f.nugs = (Box3f.nugs + 1) % Box3f.maxnugs;
        //Material.DefaultDiffuse().Shader.GGX_X = (Material.DefaultDiffuse().Shader.GGX_X + 0.01f) % 3.0f;
      }
      if (Gu.Keyboard.Press(Keys.F6))
      {
        Gu.SaveFBOs();
      }
      if (Gu.Keyboard.Press(Keys.F7))
      {
        var w = new UiWindowBase("ui_popup" + Gu.Contexts.Count, false, new ivec2(100, 100), new ivec2(500, 500), new vec2(1, 1), WindowBorder.Resizable, true, this.Context);
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
    protected virtual void CullView(RenderView rv)
    {
      //TODO: you can have any number of cameras / areas in a window
      Gu.World.BuildAndCull(rv);
    }

    #endregion
  }

  public class InfoWindow : UiWindowBase
  {
    static string nl = "\n";
    string _text = "Info Window" + nl
    + "This is some info.. " + nl
    + "This is some info.. " + nl
    + "This is some info.. " + nl
    + "This is some info.. " + nl
    + "This is some info.. " + nl
    + "   aa     This is some info.. " + nl
    + "          This is some info.. " + nl
    + "   sss   This is some info.. " + nl
    + "          This is some info.. " + nl
    + "    aa   This is some info.. " + nl
    ;

    private bool _textChanged = true;
    public string Text { get { return _text; _textChanged = true; } set { _text = value; _textChanged = true; } }

    public InfoWindow(ivec2 pos, ivec2 size) :
    base("Info", false, pos, size, null, WindowBorder.Resizable, true, Gu.Context.GameWindow.Context)
    {
      DrawWorld = false;

      //this is a UI only window, just draw the UI.
      _pipelineStages = new List<PipelineStageEnum>(){
            PipelineStageEnum.Forward,
            PipelineStageEnum.ForwardBlit
          };
    }

    protected override void OnView(RenderView rv)
    {
      if (_textChanged && rv.DebugInfo != null)
      {
        rv.DebugInfo.Text = _text;
        _textChanged = false;
      }
    }
    protected override void OnCreateGameGUI(RenderView rv)
    {
      rv.GameGui = null;
    }
    protected override void OnCreateEditGUI(RenderView rv)
    {
      var gui = GuiBuilder.GetOrCreateSharedGuiForView("info-win", rv);

      var background = new UiElement();
      background.InlineStyle.SizeModeHeight = background.InlineStyle.SizeModeWidth = UiSizeMode.Expand;
      background.InlineStyle.Color = new vec4(.7f, .7f, .7f, 1);
      background.InlineStyle.PositionMode = UiPositionMode.Static;
      gui.AddChild(background, 100);

      rv.DebugInfo = new UiLabel("debugInfo", null, "test", false, FontFace.Calibri, 25);
      rv.DebugInfo.InlineStyle.SizeModeWidth = UiSizeMode.Expand;
      rv.DebugInfo.InlineStyle.SizeModeHeight = UiSizeMode.Expand;
      rv.DebugInfo.InlineStyle.FontColor = new vec4(0, 0, 0, 1);

      gui.AddChild(rv.DebugInfo);

      rv.EditGui = gui;
      rv.GameGui = gui;
    }
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
    private Material[] testobjs = new Material[3];
    private vec3 second_y_glob = new vec3(2.5f, 2.0f, 2.5f);


    #endregion
    #region Public:Methods

    public MainWindow(ivec2 pos, ivec2 size, vec2 scale) : base("main", true, pos, size, scale, WindowBorder.Resizable, true)
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
      vec3 cpos = vec3.Zero;
      if (rv.Camera != null && rv.Camera.TryGetTarget(out var cm))
      {
        cpos = cm.Position_World;
      }

      var info = ""
          + $"{rv.Name}\n"
          + $"{Profile.ToString()}\n"
          + $"{build} {Gu.GetAssemblyVersion()}\n"
          + $"(Cam = {cpos.ToString(2)})\n"
          + $"FPS: {(int)Gu.Context.Fps}\n"
          + $"nyugs b: {Box3f.nugs}\n"
          + $"Globs: {Gu.World.NumGlobs}\n"
          + $"Visible Glob: {Gu.World.NumVisibleRenderGlobs}\n"
          + $"DrawElements_Frame:{MeshData.dbg_numDrawElements_Frame}\n"
          + $"Arrays_Frame: {MeshData.dbg_numDrawArrays_Frame}\n"
          + $"OBs culled:{Gu.World.NumCulledObjects}\n"
          + $"Mouse:{Gu.Mouse.Pos.x},{Gu.Mouse.Pos.y}\n"
          + $"Memory:{StringUtil.FormatPrec((float)System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024, 2)}MB\n"
          + $"UI Update:{rv.ActiveGui?.UpdateMs}ms\n"
          + $"UI pick:{rv.ActiveGui?.PickMs}ms\n"
          + $"UI mesh:{rv.ActiveGui?.MeshMs}ms\n"
          + $"UI obj events:{rv.ActiveGui?.ObjectEventsMs}ms\n"
          + $"UI window events:{rv.ActiveGui?.WindowEventsMs}ms\n"
          + $"UI tot:{rv.ActiveGui?.MeshMs + rv.ActiveGui?.UpdateMs + rv.ActiveGui?.PickMs}ms\n"
          + $"Picked Ob:{Gu.Context.Renderer.Picker.PickedObjectName}\n"
          ;

      //UI Test
      if (rv.DebugInfo != null)
      {
        rv.DebugInfo.Text = info;
      }

      UpdateInfoWindowInfo();
    }
    protected override void OnCreateGameGUI(RenderView rv)
    {
      rv.GameGui = GuiBuilder.GameGui(rv);
    }
    protected override void OnCreateEditGUI(RenderView rv)
    {
      rv.EditGui = GuiBuilder.EditGui(rv);
    }

    #endregion
    #region Private:Methods

    private void UpdateInfoWindowInfo()
    {
      if (this.InfoWindow == null)
      {
        return;
      }
      if (Gu.Context.Renderer.Picker.PickedObjectFrame == Gu.Context.Renderer.Picker.PickedObjectFrameLast)
      {
        return;
      }

      var sb = new System.Text.StringBuilder();
      var ob = Gu.Context.Renderer.Picker.PickedObjectFrame;
      if (ob != null)
      {
        if (ob is WorldObject)
        {
          var wo = ob as WorldObject;
          sb.AppendLine($"{wo.GetType().ToString()}");
          sb.AppendLine($"Name: {wo.Name}");
          sb.AppendLine($"Pos: {wo.Position_World}");
        }
        else if (ob is UiElement)
        {
          var e = ob as UiElement;
          sb.AppendLine($"{e.GetType().ToString()}");
          sb.AppendLine($"Props:");
          sb.AppendLine($"{e.Props.ToString()}");
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
      }

      InfoWindow.Text = sb.ToString();
    }

    private void InitMainWindow()
    {
#if DEBUG
      OperatingSystem.ShowConsole();
#else
        OperatingSystem.HideConsole();
#endif
      Title = "Slaver " + VersionId.ToString();

      Gu.World.Initialize("MyWorld", DELETE_WORLD_START_FRESH, 2);
      Gu.Context.DebugDraw.DrawBoundBoxes = false;

      TestCreateDebugObjects();
      CreateSky();
      CreateLight();

      CursorVisible = true;
    }
    protected void OnCreateCamera(Camera3D c)
    {
      InitHandObjects(c);
      CreateCrosshair(c);
    }
    private void CreateLight()
    {
      var l = new Light("pt");
      l.Radius = 1000;
      l.Power = 1;
      l.Position_Local = new vec3(0, 10, 0);
      Gu.World.AddObject(l);

    }
    private void InitHandObjects(Camera3D c)
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

        c.AddChild(pick);
      }
      objs = Gu.Resources.LoadObjects(new FileLoc("sword.glb", FileStorage.Embedded));
      if (objs?.Count > 0)
      {
        sword = objs[0];
        sword.Position_Local = right_pos;
        sword.Scale_Local *= new vec3(0.7f, 0.7f, 0.7f);
        sword.Rotation_Local *= quat.fromAxisAngle(new vec3(1, 0, 1).normalized(), MathUtils.M_PI_2 * 0.125f);

        c.AddChild(sword);
      }
    }
    private void CreateCrosshair(Camera3D c)
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
      c.AddChild(Crosshair);
    }
    private void CreateSky()
    {
      var that = this;

      Texture2D tx_sky = Gu.Resources.LoadTexture(new FileLoc("hdri_sky2.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture2D tx_sky_stars = Gu.Resources.LoadTexture(new FileLoc("hdri_stars.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture2D tx_sun = Gu.Resources.LoadTexture(new FileLoc("tx64_sun.png", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture2D tx_moon = Gu.Resources.LoadTexture(new FileLoc("tx64_moon.png", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture2D tx_bloom = Gu.Resources.LoadTexture(new FileLoc("bloom.png", FileStorage.Embedded), true, TexFilter.Trilinear);

      //Sky
      Material sky_mat = new Material("sky", Shader.DefaultObjectShader(), tx_sky);
      sky_mat.Flat = true;
      sky_mat.GpuRenderState.DepthTest = false;//Disable depth test.
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

        if (that.ActiveViewCamera != null)
        {
          var vp = that.ActiveViewCamera.RootParent;
          obj.Position_Local = vp.WorldMatrix.ExtractTranslation();
        }
        //TODO:
        //sky_mat.SetUniform("_ufSkyBlend")
      };

      //Empty that rotates the sun / moon
      var sun_moon_empty = Gu.World.CreateObject("sun_moon_empty", null, null);
      sun_moon_empty.OnView = (obj, rv) =>
      {
        double ang = Gu.World.WorldProps.DayNightCycle.DayTime_Seconds / Gu.World.WorldProps.DayNightCycle.DayLength_Seconds * Math.PI * 2.0;
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), (float)ang);

        if (that.ActiveViewCamera != null)
        {
          vec3 pe = that.ActiveViewCamera.Position_World;//.WorldMatrix.ExtractTranslation();
          obj.Position_Local = pe;
        }
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
        if (that.ActiveViewCamera != null)
        {
          vec3 dir = Gu.World.WorldProps.DayNightCycle.MoonDir.ToVec3();
          //ease multiplier so that the glare does not show on the horizon.
          float horizon_mul = (float)MathUtils.Ease(0, 1, (double)dir.dot(new vec3(0, 1, 0)));
          float bloom_dp = dir.dot(that.ActiveViewCamera.BasisZ);
          float bloom_dp_pw = (float)Math.Pow(bloom_dp, 64);
          bloom_mat.BaseColor = new vec4(sun_mat.BaseColor.x, sun_mat.BaseColor.y, sun_mat.BaseColor.z, bloom_dp_pw * horizon_mul * 0.9413f);
          obj.Scale_Local = new vec3(1.1f + bloom_dp * 30.0f, 0, 1.1f + bloom_dp * 30.0f);
        }

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
        if (that.ActiveViewCamera != null)
        {
          vec3 dir = Gu.World.WorldProps.DayNightCycle.MoonDir.ToVec3() * -1.0f;
          //ease multiplier so that the glare does not show on the horizon.
          float horizon_mul = (float)MathUtils.Ease(0, 1, (double)dir.dot(new vec3(0, 1, 0)));
          float bloom_dp = dir.dot(that.ActiveViewCamera.BasisZ);
          float bloom_dp_pw = (float)Math.Pow(bloom_dp, 64);
          obj.Material.BaseColor = new vec4(moon_mat.BaseColor.x, moon_mat.BaseColor.y, moon_mat.BaseColor.z, bloom_dp_pw * horizon_mul * 0.3f);
          obj.Scale_Local = new vec3(1.1f + bloom_dp * 4.0f, 0, 1.1f + bloom_dp * 4.0f);
        }
      };
      moon.AddChild(moon_bloom);


    }
    private void TestCreateDebugObjects()
    {
      //Textures
      //   Texture2D noise = Noise3D.TestNoise();
      Texture2D tx_peron = Gu.Resources.LoadTexture(new FileLoc("main char.png", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D tx_grass = Gu.Resources.LoadTexture(new FileLoc("grass_base.png", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D tx_mclovin = Gu.Resources.LoadTexture(new FileLoc("gates.jpg", FileStorage.Embedded), true, TexFilter.Nearest);
      Texture2D tx_usopp = Gu.Resources.LoadTexture(new FileLoc("zuck.jpg", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D tx_hogback = Gu.Resources.LoadTexture(new FileLoc("brady.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);

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
      //   Gu.World.CreateAndAddObject("TextureFront", MeshData.GenTextureFront(this.Camera, 0, 0, this.W9idth, this.Height), new Material("texturefront", Shader.DefaultObjectShader(), tx_peron));
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
    // private vec2 Get_Interaction_Pos()
    // {
    //   //Depending on game mode
    //   //Returns either world editing projection position, or, the mouse position if we are in inventory
    //   vec2 projec_pos = new vec2(0, 0);
    //   if (this.InputState == InputState.Edit)
    //   {
    //     projec_pos = Gu.Mouse.Pos;
    //   }
    //   else if (this.InputState == InputState.Play)
    //   {
    //     projec_pos = new vec2(this.Camera.Viewport.Width * 0.5f, this.Camera.Viewport.Height * 0.5f);
    //   }
    //   else
    //   {
    //     Gu.Log.Error("Invalid projection position for raycast blocks.");
    //   }
    //   return projec_pos;
    // }

    #endregion
  }

}