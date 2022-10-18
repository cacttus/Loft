using System;
using System.Collections.Generic;
using System.ComponentModel;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.Serialization;

namespace PirateCraft
{
  [DataContract]
  public class UiWindowBase : NativeWindow
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
    private string _name = Library.UnsetName;//NOT EQUAL TO TITLE
    private List<RenderView> _renderViews = new List<RenderView>();
    private bool _isLoaded = false;
    private int _width = 1;//Do not use Size.X, Y There is currently an OpenTK bug where it does not update on Resize
    private int _height = 1;
    private RenderView? _selectedView = null;//the render view where mouse is pointing

    #endregion
    #region Public:Methods

    public UiWindowBase(string name, string title, bool isMain,
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

      //Create Camera
      var c = Gu.World.CreateCamera("cam-def-" + v.Name, v, vec3.Zero);
      c.Far = 4000.0f;
      c.Position_Local = new vec3(0, .5f, 0);
      c.Rotation_Local = quat.fromAxisAngle(new vec3(-1, 0, 0), -MathUtils.M_PI / 8.0f);

      if (Gu.Lib.TryLoadModel("cam", new FileLoc("camera.glb", FileStorage.Embedded), out var cmod))
      {
        //sword.Rotation_Local *= quat.fromAxisAngle(new vec3(1, 0, 1).normalized(), MathUtils.M_PI_2 * 0.125f);
        cmod.ExcludeFromRenderView = new WeakReference<RenderView>(v);
        c.AddChild(cmod);
      }

      //Create View Player & Input (ViewPlayer)
      var p = Gu.World.CreateAndAddObject("player-empty-" + v.Name, null, null);
      p.Collides = false;
      p.HasPhysics = true;
      p.HasGravity = false;
      p.Position_Local = new vec3(0, 16, -16);
      p.AddChild(c);
      p.AddComponent(new FPSInputComponent(v));

      var l = new WorldObject("player-light");
      l.HasLight = true;
      l.LightRadius = 50;
      l.LightPower = 0.75f;
      l.Position_Local = new vec3(0, 0, 0);
      p.AddChild(l);

      //Set the view Camera
      v.Camera = new WeakReference<Camera3D>(c);
      OnCreateCamera(c);
    }
    public virtual void Load()
    {
      IsLoaded = true;
    }
    public void CullAndPickAllViews()
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
          rv.SetCurrent();
         
          rv.Gui?.Update(Gu.Context.FrameDelta);
 
          Gu.World.BuildAndCull(rv);//Pick
          
          OnUpateGUI(rv);
        }
      }
    }
    public void RenderAllViews()
    {
      Gu.Context.Renderer.BeginRenderToWindow();
      foreach (var rv in this.RenderViews)
      {
        if (rv.Enabled)
        {
          Gu.Assert(rv != null);
          rv.SetCurrent();
          Gu.Context.Renderer.RenderViewToWindow(rv);
        }
      }
      Gu.Context.Renderer.EndRenderToWindow();
      Gu.Context.DebugDraw.EndFrame();
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
      UpdateSelectedView();
    }
    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
      base.OnFocusedChanged(e);
      UpdateSelectedView();
      if (this.IsFocused)
      {
        Gu.FocusedWindow = this;//user selected different window
      }
      else if (Gu.FocusedWindow == this)
      {
        Gu.FocusedWindow = null;//user defocuesed the whole application
      }
    }
    private void UpdateSelectedView()
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
    protected virtual void OnCreateCamera(Camera3D c)
    {
      //Called when a new camera is created
    }
    protected virtual void CreateGUI2DEBUG(RenderView rv)
    {
    }
    public virtual void OnUpdateInput()
    {
      UpdateSelectedView();
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

  public class InfoWindow : UiWindowBase
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

      var background = new UiElement(new List<string> { StyleName.Panel }, "pnlPanel");
      gui.AddChild(background);

      _info = new UiElement(StyleName.Label , Phrase.DebugInfoHeader);
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
      vec3 cpos = vec3.Zero;
      if (rv.Camera != null && rv.Camera.TryGetTarget(out var cm))
      {
        cpos = cm.Position_World;
      }


      //UI Test
      if (rv.WorldDebugInfo != null && rv.WorldDebugInfo.Visible)
      {
        var info = new System.Text.StringBuilder();
        info.AppendLine($"{appname} v{Gu.GetAssemblyVersion()} (Hide=F7)");
        info.AppendLine($"Window:{rv.Name}");
        info.AppendLine($"  FPS:{StringUtil.FormatPrec(Gu.Context.FpsAvg, 1)} (vsync:{(VSync.ToString())})");
        info.AppendLine($"  Mem:{StringUtil.FormatPrec(SystemInfo.BToMB(SystemInfo.MemUsedBytes), 2)}MB");
        info.AppendLine($"  VMem:{StringUtil.FormatPrec(SystemInfo.BToMB(SystemInfo.VMemUsedBytes), 2)}MB");
        info.AppendLine($"  View:{rv.Name}");
        info.AppendLine($"  Mouse:{Gu.Context.PCMouse.Pos.ToString()}");
        info.AppendLine($"  GLProfile:{Profile.ToString()}");
        info.AppendLine($"  CamPos:{cpos.ToString(2)})");
        info.AppendLine($"Render:");
        info.AppendLine($"  DrawElements_Frame:{MeshView.dbg_numDrawElements_Frame}");
        info.AppendLine($"  Arrays_Frame:{MeshView.dbg_numDrawArrays_Frame}");
        info.AppendLine($"  OBs Culled:{Gu.World.NumCulledObjects}");
        info.AppendLine($"UI:");
        info.AppendLine($"  upd={rv.Gui?.UpdateMs}ms pick={rv.Gui?.PickMs}ms mesh={rv.Gui?.MeshMs}ms obj={rv.Gui?._dbg_ObjectEventsMs}ms ");
        info.AppendLine($"  win={rv.Gui?.WindowEventsMs}ms tot={rv.Gui?.MeshMs + rv.Gui?.UpdateMs + rv.Gui?.PickMs}ms");
        info.AppendLine($"World:");
        info.AppendLine($"  Globs: count={Gu.World.NumGlobs} visible={Gu.World.NumVisibleRenderGlobs}");
        info.AppendLine($"  Picked:{Gu.Context.Renderer.Picker.PickedObjectName}");
        info.AppendLine($"  Selected:{Gu.World.Editor.SelectedObjects.ToString()}");
        info.AppendLine($"Gpu:");
        info.AppendLine($"  GPU Mem={Gu.Context.Gpu.GetMemoryInfo().ToString()}");

        rv.WorldDebugInfo.Text = info.ToString();
      }
      if (rv.GpuDebugInfo != null && rv.GpuDebugInfo.Visible)
      {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(GT.ToString());
        sb.AppendLine($"GPU Mem:");
        Gu.Context.Gpu.GetMemoryInfo().ToString(sb, "  ");
        rv.GpuDebugInfo.Text = sb.ToString();
      }
      if (rv.ControlsInfo != null && rv.ControlsInfo.Visible)
      {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        Gu.World.Editor.KeyMap.ToString(sb, "  ");
        sb.AppendLine($"Controls:");
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

      var w = Gu.WorldLoader.CreateNewWorld(new WorldInfo("MyWorld", DELETE_WORLD_START_FRESH, 2));

      Gu.Log.Debug("Debug:Creatingf flat area");
      Gu.WorldLoader.CreateHillsArea();


      TestCreateDebugObjects();
      CreateSky();
      CreateLight();

      SetGameMode(Gu.World.GameMode);

      CursorVisible = true;
    }
    protected void OnCreateCamera(Camera3D c)
    {
      CreateCrosshair(c);
    }
    private void CreateLight()
    {
      var l = new WorldObject("pt");
      l.Position_Local = new vec3(0, 10, 0);
      l.HasLight = true;
      l.LightRadius = 50;
      l.LightPower = 0.75f;
      l.LightColor = new vec3(.9f, .8f, .1f);
      Gu.World.AddObject(l);

      l = new WorldObject("pt2");
      l.Position_Local = new vec3(-10, 10, -10);
      l.HasLight = true;
      l.LightRadius = 50;
      l.LightPower = 0.75f;
      l.LightColor = new vec3(.1f, .85f, .58f);
      Gu.World.AddObject(l);

      l = new WorldObject("pt3");
      l.Position_Local = new vec3(10, 10, 10);
      l.HasLight = true;
      l.LightRadius = 50;
      l.LightPower = 0.75f;
      l.LightColor = new vec3(1, 1, 1);
      Gu.World.AddObject(l);

      l = new WorldObject("pt4");
      l.Position_Local = new vec3(20, 10, 20);
      l.HasLight = true;
      l.LightRadius = 50;
      l.LightPower = 0.75f;
      l.LightColor = new vec3(1, 0, 1);
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

      Texture? tx_sky = Gu.Lib.LoadTexture("tx_sky", new FileLoc("hdri_sky2.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture? tx_sky_stars = Gu.Lib.LoadTexture("tx_sky_stars", new FileLoc("hdri_stars.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture? tx_sun = Gu.Lib.LoadTexture("tx_sun", new FileLoc("tx64_sun.png", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture? tx_moon = Gu.Lib.LoadTexture("tx_moon", new FileLoc("tx64_moon.png", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture? tx_bloom = Gu.Lib.LoadTexture("tx_bloom", new FileLoc("bloom.png", FileStorage.Embedded), true, TexFilter.Trilinear);

      //Sky
      Material sky_mat = new Material("sky", Shader.DefaultObjectShader(), tx_sky);
      sky_mat.Flat = true;
      sky_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      var sky = Gu.World.CreateAndAddObject("sky", MeshGen.GenSphereResource("sky", DayNightCycle.SkyRadius, 128, 128, true, true), sky_mat);
      sky.Selectable = false;
      sky.Pickable = false;
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

        if (Gu.TryGetSelectedViewCamera(out var cm))
        {
          var vp = cm.RootParent;
          obj.Position_Local = vp.WorldMatrix.ExtractTranslation();
        }
        //TODO:
        //sky_mat.SetUniform("_ufSkyBlend")
      };

      //Empty that rotates the sun / moon
      var sun_moon_empty = Gu.World.CreateObject("sun_moon_empty", null, null);
      sun_moon_empty.OnUpdate = (obj) =>
      {
        double ang = Gu.World.WorldProps.DayNightCycle.DayTime_Seconds / Gu.World.WorldProps.DayNightCycle.DayLength_Seconds * Math.PI * 2.0;
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), (float)ang);

        if (Gu.TryGetSelectedViewCamera(out var cm))
        {
          vec3 pe = cm.Position_World;//.WorldMatrix.ExtractTranslation();
          obj.Position_Local = pe;
        }
      };
      sun_moon_empty.Persistence = DataPersistence.Temporary;
      Gu.World.AddObject(sun_moon_empty);
      /*
      view update action
      for all ob
      ob.OnUpdateForView(rv)
      ob.OnBeforeRender()
      */
      Material sun_moon_mat = new Material("sunmoon", new Shader("Shader_SunMoonShader", "v_sun_moon", FileStorage.Embedded));
      sun_moon_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      sun_moon_mat.GpuRenderState.CullFace = false;//Disable depth test.
      sun_moon_mat.GpuRenderState.Blend = false;

      float sun_size = 13;
      float moon_size = 23;

      //Sun
      var sun_mat = sun_moon_mat.Clone() as Material;
      sun_mat.AlbedoSlot.Texture = tx_sun;
      var sun = Gu.World.CreateObject("sun", MeshGen.GenPlaneResource("sun", sun_size, sun_size), sun_mat);
      sun.Mesh.DrawOrder = DrawOrder.First;
      sun.OnUpdate = (obj) =>
      {
        //All this stuff can be script.
        sun_mat.BaseColor = new vec4(.994f, .990f, .8f, 1);
        obj.Position_Local = new vec3(DayNightCycle.SkyRadius, 0, 0);
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), (float)Math.PI / 2);
      };
      sun_moon_empty.AddChild(sun);


      var bloom_mat = sun_moon_mat.Clone() as Material;
      bloom_mat.AlbedoSlot.Texture = tx_bloom;
      var sun_bloom = Gu.World.CreateObject("sun_bloom", MeshGen.GenPlaneResource("sun_bloom", sun_size, sun_size), bloom_mat);
      sun_bloom.Mesh.DrawOrder = DrawOrder.First;
      sun_bloom.OnUpdate = (obj) =>
      {
        if (Gu.TryGetSelectedViewCamera(out var cm))
        {
          vec3 dir = Gu.World.WorldProps.DayNightCycle.MoonDir.ToVec3();
          //ease multiplier so that the glare does not show on the horizon.
          float horizon_mul = (float)MathUtils.Ease(0, 1, (double)dir.dot(new vec3(0, 1, 0)));
          float bloom_dp = dir.dot(cm.BasisZ_World);
          float bloom_dp_pw = (float)Math.Pow(bloom_dp, 64);
          bloom_mat.BaseColor = new vec4(sun_mat.BaseColor.x, sun_mat.BaseColor.y, sun_mat.BaseColor.z, bloom_dp_pw * horizon_mul * 0.9413f);
          obj.Scale_Local = new vec3(1.1f + bloom_dp * 30.0f, 0, 1.1f + bloom_dp * 30.0f);
        }

      };
      sun.AddChild(sun_bloom);

      //Moon
      var moon_mat = sun_moon_mat.Clone() as Material;
      moon_mat.AlbedoSlot.Texture = tx_moon;
      var moon = Gu.World.CreateObject("moon", MeshGen.GenPlaneResource("moon", moon_size, moon_size), moon_mat);
      moon.Mesh.DrawOrder = DrawOrder.First;
      moon.OnUpdate = (obj) =>
      {
        //All this stuff can be script.
        moon_mat.BaseColor = new vec4(.78f, .78f, .92f, 1);
        obj.Position_Local = new vec3(-DayNightCycle.SkyRadius, 0, 0);
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), -(float)Math.PI / 2);
      };
      sun_moon_empty.AddChild(moon);


      var moon_bloom = Gu.World.CreateObject("moon_bloom", MeshGen.GenPlaneResource("moon_bloom", moon_size, moon_size), bloom_mat);
      moon_bloom.Mesh.DrawOrder = DrawOrder.First;
      moon_bloom.OnUpdate = (obj) =>
      {
        //All this stuff can be script.
        if (Gu.TryGetSelectedViewCamera(out var cm))
        {
          vec3 dir = Gu.World.WorldProps.DayNightCycle.MoonDir.ToVec3() * -1.0f;
          //ease multiplier so that the glare does not show on the horizon.
          float horizon_mul = (float)MathUtils.Ease(0, 1, (double)dir.dot(new vec3(0, 1, 0)));
          float bloom_dp = dir.dot(cm.BasisZ_World);
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
      var zuck = new FileLoc("zuck.jpg", FileStorage.Embedded);
      var mainch = new FileLoc("main char.png", FileStorage.Embedded);
      Texture tx_peron = new Texture("tx_peron", Gu.Lib.LoadImage("tx_peron", mainch), true, TexFilter.Bilinear);
      Texture tx_grass = new Texture("tx_grass", Gu.Lib.LoadImage("tx_grass", grass), true, TexFilter.Bilinear);
      Texture tx_gates = new Texture("tx_gates", Gu.Lib.LoadImage("tx_gates", gates), true, TexFilter.Nearest);
      Texture tx_zuck = new Texture("tx_zuck", Gu.Lib.LoadImage("tx_zuck", zuck), true, TexFilter.Bilinear);
      Texture tx_brady = new Texture("tx_brady", Gu.Lib.LoadImage("tx_brady", brady), true, TexFilter.Trilinear);

      //Objects
      Gu.World.CreateAndAddObject("Grass-Plane.", MeshGen.GenPlaneResource("Grass-Plane", 10, 10), new Material("grass-plane", Shader.DefaultObjectShader(), tx_grass, null));

      //normal map test (slow)
      //new Texture2D(ResourceManager.LoadImage(brady).CreateNormalMap(false), true, TexFilter.Linear)

      //Gu.Debug_IntegrityTestGPUMemory();

      testobjs[0] = new Material("sphere_rot", Shader.DefaultObjectShader(), tx_gates);
      testobjs[1] = new Material("sphere_rot2", Shader.DefaultObjectShader(), tx_zuck);
      testobjs[1].Flat = true;
      testobjs[2] = new Material("sphere_rot3", Shader.DefaultObjectShader(), tx_brady, null);

      Sphere_Rotate_Quat_Test = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test", MeshGen.GenSphereResource("Sphere_Rotate_Quat_Test", 1, 12, 12, true), testobjs[0]);
      Sphere_Rotate_Quat_Test2 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test2", MeshGen.GenEllipsoid("Sphere_Rotate_Quat_Test2", new vec3(1f, 1, 1f), 32, 32, true), testobjs[1]);
      Sphere_Rotate_Quat_Test3 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test3", MeshGen.GenEllipsoid("Sphere_Rotate_Quat_Test3", new vec3(1, 1, 1), 32, 32, true), testobjs[2]);
      Sphere_Rotate_Quat_Test.Position_Local = new vec3(0, 3, 0);
      Sphere_Rotate_Quat_Test2.Position_Local = new vec3(-3, 3, 0);
      Sphere_Rotate_Quat_Test3.Position_Local = new vec3(3, 3, 0);

      //Test STB laoding EXR images.
      Texture tx_exr = new Texture("tx_exr", Gu.Lib.LoadImage("tx_exr", new FileLoc("hilly_terrain_01_2k.hdr", FileStorage.Embedded)), true, TexFilter.Bilinear);
      var exr_test = MeshGen.GenPlaneResource("tx_exr", 10, 10);
      var exr_test_mat = new Material("plane", Shader.DefaultObjectShader(), tx_exr);
      var exr_test_ob = Gu.World.CreateAndAddObject("EXR test", exr_test, exr_test_mat);
      exr_test_ob.Position_Local = new vec3(10, 10, 5);

      //Animation test
      vec3 raxis = new vec3(0, 1, 0);
      var adata = new AnimationData("test");
      adata.AddFrame(0, new vec3(0, 0, 0), mat3.getRotation(raxis, 0).toQuat(), new vec3(1, 1, 1));
      adata.AddFrame(1, new vec3(0, 1, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 * 0.5 - 0.001)).toQuat(), new vec3(.5f, .5f, 3));
      adata.AddFrame(2, new vec3(1, 1, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 - 0.002)).toQuat(), new vec3(2, 2, 2));
      adata.AddFrame(3, new vec3(1, 0, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 + MathUtils.M_PI_2 * 0.5 - 0.004)).toQuat(), new vec3(2, 3, 1));
      adata.AddFrame(4, new vec3(0, 0, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI * 2 - 0.006)).toQuat(), new vec3(1, 1, 1));
      var cmp = new AnimationComponent(adata);
      cmp.Repeat = true;
      cmp.Play();
      Sphere_Rotate_Quat_Test.AddComponent(cmp);

      //Check to see if this uses the resource and not the real thing
      var gearob = Gu.Lib.LoadModel(RName.WorldObject_Gear);
      Gu.World.AddObject(gearob);
      gearob.Position_Local = new vec3(4, 8, -4);
      if (gearob.Component<AnimationComponent>(out var x))
      {
        x.Repeat = true;
        x.Play();
      }
      var bare = Gu.Lib.LoadModel(RName.WorldObject_Barrel);
      Gu.World.AddObject(bare);
      bare.Position_Local = new vec3(-4, 8, -7);
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

        rv.Gui?.SetLayoutChanged();
      }
    }
    protected override void DebugKeyboard()
    {
      base.DebugKeyboard();

    }
    #endregion
  }

}