using System;
using System.Collections.Generic;
using System.ComponentModel;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.Serialization;

namespace Loft
{
  [DataContract]
  public class AppWindowBase : NativeWindow
  {
    #region Public:Members

    public bool IsMain { get { return _isMain; } private set { _isMain = value; } }
    public string Name { get { return _name; } private set { _name = value; } }
    public List<RenderView> RenderViews { get { return _renderViews; } protected set { _renderViews = value; } }
    public bool IsLoaded { get { return _isLoaded; } private set { _isLoaded = value; } }
    public int Width { get { return _width; } private set { _width = value; } }
    public int Height { get { return _height; } private set { _height = value; } }
    public RenderView? SelectedView { get { return _selectedView; } private set { _selectedView = value; } }
    public new OpenTK.Mathematics.Vector2i Size { get { Gu.Log.Error("Do not use Size, it is incorrect in OpenTK."); Gu.DebugBreak(); return base.Size; } }

    #endregion
    #region Private:Members

    private bool _isMain = false;
    private string _name = Lib.UnsetName;//NOT EQUAL TO TITLE
    private List<RenderView> _renderViews = new List<RenderView>();
    private bool _isLoaded = false;
    private int _width = 1;//Do not use Size.X, Y There is currently an OpenTK bug where it does not update on Resize
    private int _height = 1;
    private RenderView? _selectedView = null;//the render view where mouse is pointing

    #endregion
    #region Public:Methods

    public AppWindowBase(string name, string title, bool isMain,
                        ivec2 pos, ivec2 size, vec2? scale = null,
                        WindowBorder border = WindowBorder.Resizable,
                        bool visible = true, IGLFWGraphicsContext sharedCtx = null)
      : base(new NativeWindowSettings()
      {
        Profile = Gu.EngineConfig.Debug_EnableCompatibilityProfile ? ContextProfile.Compatability : ContextProfile.Core,
        Flags = ContextFlags.Debug,
        AutoLoadBindings = true,
        APIVersion = new Version(4, 1),//BlendFuncSeparate>=4.0
        Title = title,
        StartFocused = false,// explicitly focus the window
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
      })
    {
      Gu.Log.Info($"Creating window name={Name},title={title}");
      Name = name;
      Title = title;
      IsMain = isMain;
      Width = base.Size.X;
      Height = base.Size.Y;
      VSync = VSyncMode.Off;

      Gu.CreateContext($"{Name}-ctx-{Gu.Contexts.Count}", this, sharedCtx);
    }
    protected RenderView CreateRenderView(RenderViewMode mode, vec2 xy_pct, vec2 wh_pct)
    {
      string viewname = $"{Name}-rv-{RenderViews.Count}";
      var v = new RenderView(viewname, mode, xy_pct, wh_pct, this.Width, this.Height);
      RenderViews.Add(v);

      OnCreateGUI(v);

      return v;
    }
    protected void CreateCameraView(vec2 xy_pct, vec2 wh_pct)
    {
      //Technically since this is a "world view" this should be on a separate window class that can view the world.
      var v = CreateRenderView(RenderViewMode.UIAndWorld, xy_pct, wh_pct);
      v.CreateDefaultCamera();
    }
    public virtual void Load()
    {
      IsLoaded = true;
    }
    public void CullAllViews()
    {
      if (RenderViews.Count == 0)
      {
        Gu.Log.ErrorCycle($"{Name}:Window had no render views");
        Gu.DebugBreak();//nothing will draw.
      }

      foreach (var rv in RenderViews)
      {
        Gu.Assert(rv != null);

        if (rv.Enabled)
        {
          rv.Gui?.Update(Gu.Context.FrameDelta);

          Gu.World.BuildAndCull(rv);//Pick

          OnUpateGUI(rv);
        }
      }
    }
    public void RenderAllViews()
    {
      Gu.Context.Renderer.BeginRenderToWindow();
      foreach (var rv in RenderViews)
      {
        if (rv.Enabled)
        {
          Gu.Assert(rv != null);
          Gu.Context.Renderer.RenderViewToWindow(rv);
        }
      }
      Gu.Context.Renderer.EndRenderToWindow();
      Gu.Context.GameWindow.Context.SwapBuffers();
      Gu.Context.Gpu.ExecuteCallbacks_RenderThread(Gu.Context);
    }

    #endregion
    #region Protected:Methods

    protected void ForceResize()
    {
      OnResize(new ResizeEventArgs(new OpenTK.Mathematics.Vector2i(this.Width, this.Height)));
    }
    protected virtual void OnUpateGUI(RenderView rv)
    {
      //makes changes to GUI before we perform layout
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
        if (rv.Enabled)
        {
          rv.OnResize(Width, Height);
        }
      }
      Gu.Context.Renderer.ResizeScreenBuffers(Width, Height);

      if (ct != null)
      {
        Gu.SetContext(ct.GameWindow);
      }
    }
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
      base.OnMouseMove(e);
    }
    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
      base.OnFocusedChanged(e);
      if (this.IsFocused)
      {
        Gu.FocusedWindow = this;//user selected different window
      }
      else if (Gu.FocusedWindow == this)
      {
        Gu.FocusedWindow = null;//user defocuesed the whole application
      }
    }
    public void UpdateSelectedView()
    {
      SelectedView = null;
      //pick the active view based on cursor location
      if (this.IsFocused)
      {
        foreach (var rv in this.RenderViews)
        {
          if (rv.Enabled)
          {
            if (rv.Viewport.Contains_Point_Window_Relative_BR_Exclusive(Gu.Context.PCMouse.Pos))
            {
              if (SelectedView != null)
              {
                Gu.Log.Error("Multiple viewports picked!!" + SelectedView.Name + ", and " + rv.Name);
                Gu.DebugBreak();
              }
              SelectedView = rv;
            }
          }
        }
      }
    }
    protected virtual void OnCreateGUI(RenderView rv)
    {
      //Called when a new render view is created, so you can set the Gui
    }
    protected virtual void CreateGUI2DEBUG(RenderView rv)
    {
    }
    public virtual void OnUpdateInput()
    {
      if (!IsFocused)
      {
        return;
      }

      DebugKeyboard();
    }
    protected virtual void DebugKeyboard()
    {
      //TODO: move all this junk to the Keymap

      if (Gu.Context.PCKeyboard.Press(Keys.F12))
      {
        Gu.BreakRenderState = true;
        // Gu.PostCustomDebugBreak();
      }
    }

    #endregion
  }
  public class InfoWindow : AppWindowBase
  {
    private UiElement? _info = null;

    public InfoWindow(string name, string title, ivec2 pos, ivec2 size) :
      base(name, title, false, pos, size, null, WindowBorder.Resizable, true, Gu.Context.GameWindow.Context)
    {
      //CreateCameraView(new vec2(0, 0), new vec2(1, 1));
      CreateRenderView(RenderViewMode.UIOnly, new vec2(0, 0), new vec2(1, 1));
    }
    protected override void OnUpateGUI(RenderView rv)
    {
      UpdateInfo();
    }
    protected override void OnCreateGUI(RenderView rv)
    {
      var gui = UiBuilder.GetOrCreateSharedGuiForView("info-win", rv);
      rv.Gui = gui;

      var styles = UiBuilder.GetGlobalStyles(gui);
      styles.AddRange(new List<UiStyle>() { });
      gui.StyleSheet.AddStyles(styles);

      var background = new UiPanel();
      gui.AddChild(background);

      _info = new UiLabel(Phrase.DebugInfoHeader);//UiElement(UiStyleName.Label, Phrase.DebugInfoHeader);
      background.AddChild(_info);
    }
    private void UpdateInfo()
    {
      Gu.Assert(Gu.World != null);
      Gu.Assert(Gu.World.UpdateContext != null);
      Gu.Assert(Gu.World.UpdateContext.Renderer != null);
      Gu.Assert(Gu.World.UpdateContext.Renderer.Picker != null);

      var picker = Gu.World.UpdateContext.Renderer.Picker;

      if (picker.PickedObjectFrame == picker.PickedObjectFrameLast)
      {
        return;
      }

      var sb = new System.Text.StringBuilder();
      var ob = picker.PickedObjectFrame;
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
      _info.Text = sb.ToString();
    }
  }
  public class MainWindow : AppWindowBase
  {
    #region Private:Members

    private string VersionId = "0.01";
    private bool DELETE_WORLD_START_FRESH = true;
    private WorldObject _boxMeshThing = null;
    private int meshIdx = 0;
    private const float scale = 0.75f; //RESOLUTION scale
    private NativeWindowSettings _ns = NativeWindowSettings.Default;
    private WorldObject pick = null;
    private WorldObject sword = null;
    private WorldObject left_hand = null;
    private WorldObject right_hand = null;
    private vec3 second_y_glob = new vec3(2.5f, 2.0f, 2.5f);

    #endregion
    #region Public: Methods

    public MainWindow(ivec2 pos, ivec2 size, vec2 scale) : base("mainwindow", "Welcome!", true, pos, size, scale, WindowBorder.Resizable, true)
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

    protected override void OnUpateGUI(RenderView rv)
    {
      string build = "-r";
#if DEBUG
      build = "-d";
#endif
      string appname = "Slaver" + build;
      vec3 cpos = rv.Camera.Position_World;

      //UI Test
      if (rv.WorldDebugInfo != null && rv.WorldDebugInfo.Visible)
      {
        var info = new System.Text.StringBuilder();
        info.AppendLine($"{appname} v{Gu.GetAssemblyVersion()} (Hide=F7)");
        info.AppendLine($"Window:");
        info.AppendLine($" FPS:{StringUtil.FormatPrec(Gu.Context.FpsAvg, 1)} (vsync:{(VSync.ToString())})");
        info.AppendLine($" Uptime:{StringUtil.Seconds_ToString_HMSU(Gu.Context.UpTime)}");
        info.AppendLine($" Mem:{StringUtil.FormatPrec(SystemInfo.BToMB(SystemInfo.MemUsedBytes), 2)}MB");
        info.AppendLine($" VMem:{StringUtil.FormatPrec(SystemInfo.BToMB(SystemInfo.VMemUsedBytes), 2)}MB");
        info.AppendLine($" View:{rv.Name}");
        info.AppendLine($" Mouse:{Gu.Context.PCMouse.Pos.ToString()}");
        info.AppendLine($" Profile:{Profile.ToString()}");
        info.AppendLine($" Camera:{cpos.ToString(2)} ");
        info.AppendLine($" FOV:{StringUtil.FormatPrec(MathUtils.ToDegrees(rv.Camera.FOV),0)}°,{StringUtil.FormatPrec(rv.Camera.Near,1)},{StringUtil.FormatPrec(rv.Camera.Far,1)},{rv.Camera.ProjectionMode.ToString()} ");
        info.AppendLine($"Stats:");
        info.AppendLine($"{Gu.FrameStats.ToString()}");
        info.AppendLine($"UI:");
        info.AppendLine($" update={rv.Gui?._dbg_UpdateMs}ms mesh={rv.Gui?._dbg_MeshMs}ms event={rv.Gui?._dbg_EventsMs}ms");
        info.AppendLine($"Scripts:");
        info.AppendLine($" {StringUtil.FormatPrec((float)CSharpScript.TotalLoadedScriptAssemblyBytes/(float)(1024*1024),1)}MB");
        info.AppendLine($"World:");
        info.AppendLine($" globs: count={Gu.World.NumGlobs} visible={Gu.World.NumVisibleRenderGlobs}");
        info.AppendLine($" objs: visible={Gu.World.NumVisibleObjects} culled={Gu.World.NumCulledObjects}");
        info.AppendLine($" icked={Gu.Context.Renderer.Picker.PickedObjectName}");
        info.AppendLine($" selected={Gu.World.Editor.SelectedObjects.ToString()}");
        info.AppendLine($"Gpu:");
        info.AppendLine($"{Gu.Context.Gpu.GetMemoryInfo().ToString()}");

        rv.WorldDebugInfo.Text = info.ToString();
      }
      if (rv.GpuDebugInfo != null && rv.GpuDebugInfo.Visible)
      {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        GpuDebugInfo.DebugPrintShaderLimitsAndState(sb, "  ");
        sb.AppendLine($"---------------- GL Allocations ----------------");
        sb.Append(GT.ToString());
        sb.AppendLine($"---------------- GPU Memory ----------------");
        Gu.Context.Gpu.GetMemoryInfo().ToString(sb, "  ");
        rv.GpuDebugInfo.Text = sb.ToString();
      }
      if (rv.ControlsInfo != null && rv.ControlsInfo.Visible)
      {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Controls:");
        Gu.World.Editor.KeyMap.ToString(sb, "  ");
        rv.ControlsInfo.Text = sb.ToString();
      }
    }
    protected override void OnCreateGUI(RenderView rv)
    {
      UiBuilder.MakeGui(rv);
    }

    #endregion
    #region Private:Methods

    private void InitMainWindow()
    {
      //This all must come in a script or something
#if DEBUG
      OperatingSystem.ShowConsole();
#else
        OperatingSystem.HideConsole();
#endif
      Title = "Slaver " + VersionId.ToString();
      Gu.WorldLoader = new WorldLoader(Gu.GetContextForWindow(this));

      //uh.
      var w = Gu.WorldLoader.GoToWorld(new WorldInfo("MyWorld", new FileLoc("MyWorldScript.cs", FileStorage.Embedded), DELETE_WORLD_START_FRESH, 2));

      SetGameMode(Gu.World.GameMode);

      CursorVisible = true;
    }
    public void ToggleGameMode()
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
    public void SetGameMode(GameMode g)
    {
      //Global Game mode
      Gu.World.GameMode = g;

      // 4 views, disable if they are not rendering
      if (this.RenderViews.Count == 0)
      {
        //Create initial 4-up view
        CreateCameraView(new vec2(0.0f, 0.0f), new vec2(0.5f, 0.5f));
        CreateCameraView(new vec2(0.5f, 0.0f), new vec2(1.0f, 0.5f));
        CreateCameraView(new vec2(0.0f, 0.5f), new vec2(0.5f, 1.0f));
        CreateCameraView(new vec2(0.5f, 0.5f), new vec2(1.0f, 1.0f));
      }
      Gu.Assert(this.RenderViews.Count == 4);

      foreach (var rv in this.RenderViews)
      {
        rv.Enabled = false; //disable all views
      }

      //Set view size
      if ((Gu.World.GameMode == GameMode.Edit && Gu.World.Editor.EditView == 1) || Gu.World.GameMode == GameMode.Play)
      {
        RenderViews[0].SetSize(new vec2(0.0f, 0.0f), new vec2(1.0f, 1.0f), this.Width, this.Height);
        RenderViews[0].Enabled = true;
      }
      else if (Gu.World.GameMode == GameMode.Edit)
      {
        if (Gu.World.Editor.EditView == 2)
        {
          RenderViews[0].SetSize(new vec2(0.0f, 0.0f), new vec2(0.5f, 1.0f), this.Width, this.Height);
          RenderViews[1].SetSize(new vec2(0.5f, 0.0f), new vec2(1.0f, 1.0f), this.Width, this.Height);
          RenderViews[0].Enabled = true;
          RenderViews[1].Enabled = true;
        }
        else if (Gu.World.Editor.EditView == 3)
        {
          RenderViews[0].SetSize(new vec2(0.0f, 0.0f), new vec2(0.5f, 0.5f), this.Width, this.Height);
          RenderViews[1].SetSize(new vec2(0.5f, 0.0f), new vec2(1.0f, 0.5f), this.Width, this.Height);
          RenderViews[2].SetSize(new vec2(0.0f, 0.5f), new vec2(1.0f, 1.0f), this.Width, this.Height);
          RenderViews[0].Enabled = true;
          RenderViews[1].Enabled = true;
          RenderViews[2].Enabled = true;
        }
        else if (Gu.World.Editor.EditView == 4)
        {
          //4-up, this.Width, this.Height
          RenderViews[0].SetSize(new vec2(0.0f, 0.0f), new vec2(0.5f, 0.5f), this.Width, this.Height);
          RenderViews[1].SetSize(new vec2(0.5f, 0.0f), new vec2(1.0f, 0.5f), this.Width, this.Height);
          RenderViews[2].SetSize(new vec2(0.0f, 0.5f), new vec2(0.5f, 1.0f), this.Width, this.Height);
          RenderViews[3].SetSize(new vec2(0.5f, 0.5f), new vec2(1.0f, 1.0f), this.Width, this.Height);
          RenderViews[0].Enabled = true;
          RenderViews[1].Enabled = true;
          RenderViews[2].Enabled = true;
          RenderViews[3].Enabled = true;
        }
      }

      //Set / Update GUI, Set Input Mode
      foreach (var rv in RenderViews)
      {
        if (g == GameMode.Play)
        {
          rv.Gui.Hide(RenderView.c_EditGUI_Root);//TODO: show / hide debug 
          rv.ViewInputMode = ViewInputMode.Play;
          Gu.EngineConfig.Renderer_UseAlias = true;
          ForceResize();
        }
        else if (g == GameMode.Edit)
        {
          rv.Gui.Show(RenderView.c_EditGUI_Root);//TODO: show / hide debug 
          rv.ViewInputMode = ViewInputMode.Edit;
          Gu.EngineConfig.Renderer_UseAlias = false;
          ForceResize();
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }

      }
    }
    protected override void DebugKeyboard()
    {
      base.DebugKeyboard();

    }
    #endregion
  }

}