using System;
using System.Collections.Generic;
using System.Linq;

namespace Loft
{
  public class EditGuiScript : IUIScript
  {
    public EditGuiScript() { }
    public string GetName() { return GetType().Name; }
    public void OnCreate(IGui2d gggg)
    {
      var g = gggg as Gui2d;

      //************************************************************
      // root
      var editgui_root = new UiElement();
      editgui_root.Name = "guiroot";
      editgui_root.Style.SizeModeWidth = UiSizeMode.Percent;
      editgui_root.Style.SizeModeHeight = UiSizeMode.Percent;
      editgui_root.Style.PercentWidth = 100;
      editgui_root.Style.PercentHeight = 100;
      editgui_root.Style.MinWidth = 0;
      editgui_root.Style.MinHeight = 0;
      editgui_root.Style.MaxWidth = Gui2d.MaxSize;
      editgui_root.Style.MaxHeight = Gui2d.MaxSize;
      editgui_root.Style.DisplayMode = UiDisplayMode.Inline;
      editgui_root.Style.PositionMode = UiPositionMode.Fixed;
      editgui_root.Style.Top = 0;
      editgui_root.Style.Left = 0;
      g.AddChild(editgui_root);

      //************************************************************
      //toolbar
      var toolbar = new UiToolbar();

      toolbar.AddItem(new UiToolbarButton(Phrase.File))
        .AddItem("Toggle VSync", e => Gu.World.Editor.DoEvent(WorldEditEvent.Debug_ToggleVSync))
        .AddItem("Quit", e => Gu.World.Editor.DoEvent(WorldEditEvent.Quit))
        ;
      toolbar.AddItem(new UiToolbarButton(Phrase.Edit))
        .AddItem("Undo", "Ctrl+Z", e => Gu.World.Editor.DoEvent(WorldEditEvent.Edit_Undo))
        .AddItem("Redo", "Ctrl+Shift+Z", e => Gu.World.Editor.DoEvent(WorldEditEvent.Edit_Redo))
        .AddItem("Delete", "X", e => Gu.World.Editor.DoEvent(WorldEditEvent.Edit_Delete))
        ;
      toolbar.AddItem(new UiToolbarButton("View"))
        .AddItem("Debug", e => ToggleInfo(true, false, false, false))
        .AddItem("Gpu", e => ToggleInfo(false, true, false, false))
        .AddItem("Controls", e => ToggleInfo(false, false, true, false))
        .AddItem("Prof", e => ToggleInfo(false, false, false, true))
        .AddItem("Hide Debug", e => ToggleInfo(false, false, false, false))
        ;
      toolbar.AddItem(new UiToolbarButton("Window"))
        .AddItem("Reset Windows", (e) => { g.ResetWindowLayout(); })
        .AddItem("Reset Scale", (e) => { g.Scale = new vec2(1, 1); })
        ;

      toolbar.AddItem(new UiToolbarButton("Test"))
        .AddItem("Show Message Box", e => Gu.MessageBox("Guess what?", $"Calling you direct from 1-800-5555"))
        .AddSubMenu("toasts..")
          .AddItem("do toast", (e) =>
          {
            //e.State.Gui.RenderView.Toast.Style.FontSize = null;
            //e.State.Gui.RenderView.Toast.Style.Color = null;
            //e.State.Gui.RenderView.Toast.Style.FontColor = null;
            //e.State.Gui?.RenderView?.Toast.Show("Hello world!");
          })
            //.AddItem("error toast", (e) =>
            //{
            //  e.State.Gui.RenderView.Toast.Style.FontSize = null;
            //  e.State.Gui.RenderView.Toast.Style.Color = new vec4(.9f, .3f, .4f, 1);
            //  e.State.Gui.RenderView.Toast.Style.FontColor = new vec4(.991f, .979f, .899f, 1);
            //  e.State.Gui?.RenderView?.Toast.Show("Error:\nSome really long error message:");
            //})
            //.AddItem("long toast", (e) =>
            //{

            //  e.State.Gui.RenderView.Toast.Style.FontSize = null;
            //  e.State.Gui.RenderView.Toast.Style.Color = null;
            //  e.State.Gui.RenderView.Toast.Style.FontColor = null;
            //  e.State.Gui?.RenderView?.Toast.Show($"{Gu.Translator.Translate(Phrase.LongText)}");
            //})
            //.AddItem("huge text toast", (e) =>
            //{
            //  e.State.Gui.RenderView.Toast.Style.FontSize = 90;
            //  e.State.Gui.RenderView.Toast.Style.Color = null;
            //  e.State.Gui.RenderView.Toast.Style.FontColor = null;
            //  e.State.Gui?.RenderView?.Toast.Show($"This is some HUGE text.\n0123456789");
            //})
            //.AddItem("5px toast", (e) =>
            //{
            //  e.State.Gui.RenderView.Toast.Style.FontSize = 5;
            //  e.State.Gui.RenderView.Toast.Style.Color = null;
            //  e.State.Gui.RenderView.Toast.Style.FontColor = null;
            //  e.State.Gui?.RenderView?.Toast.Show($"This is some smol text.\n0123456789");
            //})
            //.AddItem("8px bigger toast", (e) =>
            //{
            //  e.State.Gui.RenderView.Toast.Style.FontSize = 8;
            //  e.State.Gui.RenderView.Toast.Style.Color = null;
            //  e.State.Gui.RenderView.Toast.Style.FontColor = null;
            //  e.State.Gui?.RenderView?.Toast.Show($"This is some smol text.\n0123456789");
            //})
            //.AddItem("10px bigger toast", (e) =>
            //{
            //  e.State.Gui.RenderView.Toast.Style.FontSize = 10;
            //  e.State.Gui.RenderView.Toast.Style.Color = null;
            //  e.State.Gui.RenderView.Toast.Style.FontColor = null;
            //  e.State.Gui?.RenderView?.Toast.Show($"This is some smol text.\n0123456789");
            //})
            //.AddItem("12px bigger toast", (e) =>
            //{
            //  e.State.Gui.RenderView.Toast.Style.FontSize = 12;
            //  e.State.Gui.RenderView.Toast.Style.Color = null;
            //  e.State.Gui.RenderView.Toast.Style.FontColor = null;
            //  e.State.Gui?.RenderView?.Toast.Show($"This is some smol text.\n0123456789");
            //})
            .ParentMenuItem
            .AddSubMenu(Phrase.Atlas)
              .AddSubMenu(Phrase.Dark)
                .AddItem(Phrase.CopyItem)
                .AddItem(Phrase.NewProject)
                .AddItem(Phrase.TileSize)
                .AddSubMenu(Phrase.Version)
                  .AddSubMenu(Phrase.Info)
                    .AddSubMenu(Phrase.Version)
                      .AddItem(Phrase.AtlasParameters)
                      .AddItem(Phrase.RemoveItem)
                      ;

      //************************************************************
      // FOV 
      double defaultfov = 45;
      if (Gu.TryGetSelectedViewCamera(out var cc33))
      {
        defaultfov = cc33.FOV;
      }
      var slider = new UiSlider(1.0, 170.0, defaultfov, UiSlider.LabelDisplayMode.Inside, UiOrientation.Horizontal, (e, val) =>
      {
        if (Gu.TryGetSelectedViewCamera(out var cc))
        {
          cc.FOV = (float)MathUtils.ToRadians(val);
        }
      });
      slider.Style.MaxWidth = 200;
      slider.Name = "FOV";
      toolbar.AddChild(slider);

      //************************************************************
      toolbar.AddChild(new FPSLabel()); //last
      toolbar.LastChild().Style.DisplayMode = UiDisplayMode.Inline;

      //************************************************************
      editgui_root.AddChild(toolbar);

      //************************************************************
      //debug panels

      //*** Debug info panels
      var wi = new UiWindow("worlddebuginfo", new vec2(60, 60), new vec2(800, 600), UiWindow.UiWindowStyle.Sizable);
      wi.AddStyle(UiStyleName.DebugInfo.ToString());
      wi.Style.MaxWidth = 800;
      wi.Style.MaxHeight = 600;
      g.RenderView.WorldInfo = wi;
      editgui_root.AddChild(g.RenderView.WorldInfo);

      var di = new UiWindow("debuginfo", new vec2(30, 30), new vec2(400, 400), UiWindow.UiWindowStyle.Sizable);
      di.AddStyle(UiStyleName.DebugInfo.ToString());
      di.Style.MaxWidth = 400;
      di.Style.MaxHeight = 600;
      g.RenderView.GpuInfo = di;
      editgui_root.AddChild(g.RenderView.GpuInfo);

      var ci = new UiWindow("controlsinfo", new vec2(20, 620), new vec2(400, 400), UiWindow.UiWindowStyle.Sizable);
      ci.AddStyle(UiStyleName.DebugInfo.ToString());
      ci.Style.MaxWidth = 400;
      ci.Style.MaxHeight = 600;
      g.RenderView.ControlsInfo = ci;
      editgui_root.AddChild(g.RenderView.ControlsInfo);

      wi.Style.Margin = di.Style.Margin = ci.Style.Margin = 5;
      wi.Style.Border = di.Style.Border = ci.Style.Border = 1;

      var win = new UiWindow("Profile", new vec2(60, 60), new vec2(400, 400), UiWindow.UiWindowStyle.Sizable);
      win.OnClose += (e) => { e.PreventDefault = true; win.Hide(); };
      (win.Content as UiElement).Style.Padding = 5;
      //g.RenderView.ProfInfo.Content.Style.Alignment=UiAlignment.Center;
      //g.RenderView.ProfInfo.Content.Style.TextAlign=UiAlignment.Center;
      win.Style.MaxWidth = 700;
      win.Style.MaxHeight = 800;

      win.Text = Ipsum.GetIpsum(1);

      g.RenderView.ProfInfo = win;
      editgui_root.AddChild(win);
      g.Windows.Add(g.RenderView.ProfInfo);

      ToggleInfo(false, false, false, true, g.RenderView);

      //************************************************************

      g.RenderView.Toast = new UiToast("This is a toast!");
      editgui_root.AddChild(g.RenderView.Toast);

      //************************************************************
    }
    public void OnUpdate(RenderView rv)
    {
      var g = rv.Gui as Gui2d;
      string build = "-r";
#if DEBUG
      build = "-d";
#endif
      string appname = "Loft" + build;
      vec3 cpos = rv.Camera.Position_World;

      if (rv.ProfInfo != null && rv.ProfInfo.Visible)
      {
        rv.ProfInfo.Content.Text = Ipsum.GetIpsum(1);// Gu.Profiler.ToString(" ");
      }
      if (rv.WorldInfo != null && rv.WorldInfo.Visible)
      {
        var win = Gu.Context.GameWindow;
        var info = new System.Text.StringBuilder();
        info.AppendLine($"{appname} v{SystemInfo_Fast.AssemblyVersion} (Hide=F7)");
        info.AppendLine($"Window:");
        info.AppendLine($" FPS:{StringUtil.FormatPrec(Gu.Context.FpsAvg, 1)} (vsync:{(win.VSync.ToString())})");
        info.AppendLine($" Uptime:{StringUtil.Seconds_ToString_HMSU(Gu.Context.UpTime)}");
        info.AppendLine($" Mem:{StringUtil.FormatPrec(SystemInfo_Fast.BToMB(SystemInfo_Fast.MemUsedBytes), 2)}MB");
        info.AppendLine($" VMem:{StringUtil.FormatPrec(SystemInfo_Fast.BToMB(SystemInfo_Fast.VMemUsedBytes), 2)}MB");
        info.AppendLine($" View:{rv.Name}");
        info.AppendLine($" Mouse:{Gu.Context.PCMouse.Pos.ToString()}");
        info.AppendLine($" Profile:{win.Profile.ToString()}");
        info.AppendLine($" Camera:{cpos.ToString(2)} ");
        info.AppendLine($" FOV:{StringUtil.FormatPrec(MathUtils.ToDegrees(rv.Camera.FOV), 0)}°,{StringUtil.FormatPrec(rv.Camera.Near, 1)},{StringUtil.FormatPrec(rv.Camera.Far, 1)},{rv.Camera.ProjectionMode.ToString()} ");
        info.AppendLine($"Stats:");
        info.AppendLine($"{Gu.FrameStats.ToString()}");
        info.AppendLine($"UI:");
        info.AppendLine($" update={g?._dbg_UpdateMs}ms mesh={g?._dbg_MeshMs}ms event={g?._dbg_EventsMs}ms");
        info.AppendLine($"Scripts:");
        info.AppendLine($" {StringUtil.FormatPrec((float)CSharpScript.TotalLoadedScriptAssemblyBytes / (float)(1024 * 1024), 1)}MB");
        info.AppendLine($"World:");
        info.AppendLine($" globs: count={Gu.World.NumGlobs} visible={Gu.World.NumVisibleRenderGlobs}");
        info.AppendLine($" objs: visible={Gu.World.NumVisibleObjects} culled={Gu.World.NumCulledObjects}");
        info.AppendLine($" picked={Gu.Context.Picker.PickedObjectName}");
        info.AppendLine($" selected={String.Join(",", Gu.World.Editor.SelectedObjects.Select((i) => i.Name)).ToString()}");
        info.AppendLine($"Gpu:");
        info.AppendLine($"{Gu.Context.Gpu.GetMemoryInfo().ToString()}");
        info.AppendLine($"ThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapT hisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrapThisIsLongTextNoWrap");

        rv.WorldInfo.Text = info.ToString();
      }
      if (rv.GpuInfo != null && rv.GpuInfo.Visible)
      {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        GpuDebugInfo.DebugPrintShaderLimitsAndState(sb, "  ");
        sb.AppendLine($"---------------- GL Allocations ----------------");
        sb.Append(GT.ToString());
        sb.AppendLine($"---------------- GPU Memory ----------------");
        Gu.Context.Gpu.GetMemoryInfo().ToString(sb, "  ");
        rv.GpuInfo.Text = sb.ToString();
      }
      if (rv.ControlsInfo != null && rv.ControlsInfo.Visible)
      {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Controls:");
        Gu.World.Editor.KeyMap.ToString(sb, "  ");
        rv.ControlsInfo.Text = sb.ToString();
      }
    }
    public void ToggleInfo(bool world, bool gpu, bool controls, bool prof, RenderView? rv = null)
    {
      if (rv == null)
      {
        Gu.TryGetSelectedView(out rv);
      }
      if (rv != null)
      {
        rv.WorldInfo.Visible = world;
        rv.GpuInfo.Visible = gpu;
        rv.ControlsInfo.Visible = controls;
        rv.ProfInfo.Visible = prof;
        if (world) { (rv.WorldInfo as UiWindow).Style.Opacity = 1; }
        if (gpu) { (rv.GpuInfo as UiWindow).Style.Opacity = 1; }
        if (controls) { (rv.ControlsInfo as UiWindow).Style.Opacity = 1; }
        if (prof) { (rv.ProfInfo as UiWindow).Style.Opacity = 1; }
      }
    }
  }//cls


}//ns