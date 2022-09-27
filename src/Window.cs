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
    public Camera3D ActiveViewCamera
    {
      //returns the camera for the given active view, or null if there is
      // 1. no active view
      // 2. the active view has no camera attached.
      // *Active View* = The current view on which rendering is taking place.
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
         Profile = Gu.EngineConfig.Debug_EnableCompatibilityProfile ? ContextProfile.Compatability : ContextProfile.Core,
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

      //Init with vsync off.
      VSync = VSyncMode.Off;
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
      IsLoaded = true;
    }
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

        rv.Update_PostView();
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
      //The closing event does not seem to propogate for some reason..?
      base.OnClosing(e);
      Gu.CloseWindow(this);
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

      //exit if we are not editing.
      if (Gu.Keyboard.Press(Keys.Escape))
      {
        foreach (var rv in this.RenderViews)
        {
          if (rv.ObjectSelector != null)
          {
            if (rv.ObjectSelector.InputState != InputState.Select)
            {
              //we are moving sth..
              return;
            }
          }
        }
        Gu.CloseWindow(InfoWindow);
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
        Gu.Context.DebugDraw.DrawVertexNormals = !Gu.Context.DebugDraw.DrawVertexNormals;
        Gu.Context.DebugDraw.DrawFaceNormals = !Gu.Context.DebugDraw.DrawFaceNormals;
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
      if (Gu.Keyboard.Press(Keys.F9))
      {
        if (InfoWindow == null)
        {
          InfoWindow = new InfoWindow(new ivec2(200, 200), new ivec2(500, 400));
        }
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
      if (Gu.Keyboard.Press(Keys.F12))
      {
        Gu.PostCustomDebugBreak();
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
    string _text = "";

    private bool _textChanged = true;
    public string Text
    {
      get { return _text; }
      set
      {
        if (value.Equals(_text) == false)
        {
          _textChanged = true;
          _text = value;
        }
      }
    }

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

      //Added styles
      var styles = GuiBuilder.GetGlobalStylesThatWeWillLaterLoadViaCSSFile(gui);
      styles.AddRange(new List<UiStyle>() {
        new UiStyle("lighterLabel") {
          Color = new vec4(.8f,.8f,.8f,1),
          SizeModeWidth = UiSizeMode.Expand,
          SizeModeHeight = UiSizeMode.Shrink,
          PositionMode = UiPositionMode.Static,
          DisplayMode = UiDisplayMode.Block,
        }
      });
      gui.StyleSheet.AddStyles(styles);

      var background = new UiElement(new List<string> { StyleName.Panel }, "pnlPanel");
      gui.AddChild(background);

      //Header
      background.AddChild(new UiElement(new List<string> { StyleName.Label, "lighterLabel" }, "lblDebugInfoHeader", Phrase.DebugInfoHeader));

      //Debug info
      rv.DebugInfo = new UiElement(new List<string> { StyleName.Label }, "lblDebugInfo", "N/A");
      rv.DebugInfo.Style.SizeModeWidth = UiSizeMode.Expand;
      rv.DebugInfo.Style.SizeModeHeight = UiSizeMode.Shrink;
      background.AddChild(rv.DebugInfo);

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

      var info = new System.Text.StringBuilder();
      info.AppendLine($"{rv.Name}");
      info.AppendLine($"{Profile.ToString()}");
      info.AppendLine($"{build} {Gu.GetAssemblyVersion()}");
      info.AppendLine($"(Cam = {cpos.ToString(2)})");
      info.AppendLine($"FPS: {(int)Gu.Context.FpsAvg}");
      info.AppendLine($"nyugs b: {Box3f.nugs}");
      info.AppendLine($"Globs: {Gu.World.NumGlobs}");
      info.AppendLine($"Visible Glob: {Gu.World.NumVisibleRenderGlobs}");
      info.AppendLine($"DrawElements_Frame:{MeshData.dbg_numDrawElements_Frame}");
      info.AppendLine($"Arrays_Frame: {MeshData.dbg_numDrawArrays_Frame}");
      info.AppendLine($"OBs culled:{Gu.World.NumCulledObjects}");
      info.AppendLine($"Mouse:{Gu.Mouse.Pos.x},{Gu.Mouse.Pos.y}");
      info.AppendLine($"Memory:{StringUtil.FormatPrec((float)System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024, 2)}MB");
      info.AppendLine($"UI Update:{rv.ActiveGui?.UpdateMs}ms");
      info.AppendLine($"UI pick:{rv.ActiveGui?.PickMs}ms");
      info.AppendLine($"UI mesh:{rv.ActiveGui?.MeshMs}ms");
      info.AppendLine($"UI obj events:{rv.ActiveGui?.ObjectEventsMs}ms");
      info.AppendLine($"UI window events:{rv.ActiveGui?.WindowEventsMs}ms");
      info.AppendLine($"UI tot:{rv.ActiveGui?.MeshMs + rv.ActiveGui?.UpdateMs + rv.ActiveGui?.PickMs}ms");
      info.AppendLine($"Picked Ob:{Gu.Context.Renderer.Picker.PickedObjectName}");
      info.AppendLine($"Selected Ob:{rv.ObjectSelector.SelectedObjects.Count}");
      info.AppendLine($"GPU Memory");
      info.AppendLine(Gu.Context.Gpu.GetMemoryInfo().ToString());

      //UI Test
      if (rv.DebugInfo != null)
      {
        rv.DebugInfo.Text = info.ToString();
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
          sb.AppendLine($"Style:");
          sb.AppendLine($"{e.Style.ToString()}");
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
      }
      else
      {
        sb.Append(Gu.Translator.Translate(Phrase.DebugInfoMustSelect));
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
      Gu.WorldLoader = new WorldLoader(Gu.GetContextForWindow(this));

      var w = Gu.WorldLoader.CreateNewWorld(new WorldInfo("MyWorld", DELETE_WORLD_START_FRESH, 2));

      Gu.Log.Debug("Debug:Creatingf flat area");
      Gu.WorldLoader.CreateFlatArea();

      SetGameMode(Gu.World.GameMode);

      TestCreateDebugObjects();
      CreateSky();
      CreateLight();

      CursorVisible = true;
    }
    protected void OnCreateCamera(Camera3D c)
    {
      CreateCrosshair(c);
    }
    private void CreateLight()
    {
      var l = new Light("pt");
      l.Radius = 5000;
      l.Power = 200;
      l.Position_Local = new vec3(0, 10, 0);
      Gu.World.AddObject(l);

      l = new Light("pt");
      l.Radius = 5000;
      l.Power = 200;
      l.Position_Local = new vec3(-10, 10, -10);
      Gu.World.AddObject(l);

       l = new Light("pt");
      l.Radius = 5000;
      l.Power = 200;
      l.Position_Local = new vec3(10, 10, 10);
      Gu.World.AddObject(l);
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
      sky.Selectable = false;
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
      var grass = new FileLoc("grass_base.png", FileStorage.Embedded);
      var gates = new FileLoc("gates.jpg", FileStorage.Embedded);
      var brady = new FileLoc("brady.jpg", FileStorage.Embedded);
      Texture2D tx_peron = Gu.Resources.LoadTexture(new FileLoc("main char.png", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D tx_grass = Gu.Resources.LoadTexture(grass, true, TexFilter.Bilinear);
      Texture2D tx_gates = Gu.Resources.LoadTexture(gates, true, TexFilter.Nearest);
      Texture2D tx_zuck = Gu.Resources.LoadTexture(new FileLoc("zuck.jpg", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D tx_brady = Gu.Resources.LoadTexture(brady, true, TexFilter.Trilinear);

      //Objects
      Gu.World.CreateAndAddObject("Grass-Plane.", MeshData.GenPlane(10, 10), new Material("grass-plane", Shader.DefaultObjectShader(), tx_grass, null ));

//normal map test (slow)
      //new Texture2D(ResourceManager.LoadImage(brady).CreateNormalMap(false), true, TexFilter.Linear)

      //Gu.Debug_IntegrityTestGPUMemory();

      testobjs[0] = new Material("sphere_rot", Shader.DefaultObjectShader(), tx_gates);
      testobjs[1] = new Material("sphere_rot2", Shader.DefaultObjectShader(), tx_zuck);
      testobjs[1].Flat = true;
      testobjs[2] = new Material("sphere_rot3", Shader.DefaultObjectShader(), tx_brady, null);

      Sphere_Rotate_Quat_Test = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test", MeshData.GenSphere(1, 12, 12, true), testobjs[0]);
      Sphere_Rotate_Quat_Test2 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test2", MeshData.GenEllipsoid(new vec3(1f, 1, 1f), 32, 32, true), testobjs[1]);
      Sphere_Rotate_Quat_Test3 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test3", MeshData.GenEllipsoid(new vec3(1, 1, 1), 32, 32, true), testobjs[2]);
      Sphere_Rotate_Quat_Test.Position_Local = new vec3(0, 3, 0);
      Sphere_Rotate_Quat_Test2.Position_Local = new vec3(-3, 3, 0);
      Sphere_Rotate_Quat_Test3.Position_Local = new vec3(3, 3, 0);

      //Test STB laoding EXR images.
      Texture2D tx_exr = Gu.Resources.LoadTexture(new FileLoc("hilly_terrain_01_2k.hdr", FileStorage.Embedded), true, TexFilter.Bilinear);
      var exr_test = MeshData.GenPlane(10, 10);
      var exr_test_mat = new Material("plane", Shader.DefaultObjectShader(), tx_exr);
      var exr_test_ob = Gu.World.CreateAndAddObject("EXR test", exr_test, exr_test_mat);
      exr_test_ob.Position_Local = new vec3(10, 10, 5);

      //Animation test
      var cmp = new AnimationComponent();
      cmp.Repeat = true;
      vec3 raxis = new vec3(0, 1, 0);
      cmp.AddFrame(0, new vec3(0, 0, 0), mat3.getRotation(raxis, 0).toQuat(), new vec3(1, 1, 1));
      cmp.AddFrame(1, new vec3(0, 1, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 * 0.5 - 0.001)).toQuat(), new vec3(.5f, .5f, 3));
      cmp.AddFrame(2, new vec3(1, 1, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 - 0.002)).toQuat(), new vec3(2, 2, 2));
      cmp.AddFrame(3, new vec3(1, 0, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 + MathUtils.M_PI_2 * 0.5 - 0.004)).toQuat(), new vec3(2, 3, 1));
      cmp.AddFrame(4, new vec3(0, 0, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI * 2 - 0.006)).toQuat(), new vec3(1, 1, 1));
      cmp.Play();
      Sphere_Rotate_Quat_Test.Components.Add(cmp);

      WorldObject gearob = null;
      Gu.Resources.LoadObject(new FileLoc("gear.glb", FileStorage.Embedded), out gearob);
      Gu.World.AddObject(gearob);
      if (gearob.Component<AnimationComponent>(out var x))
      {
        x.Repeat = true;
        x.Play();
      }

    }

    #endregion
  }

}