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
  //Hard coded User interface stuff.
  //Gui build
  public class GuiBuilder
  {
    public static List<FileLoc> GetSharedGUIResources()
    {
      return new List<FileLoc>(){
       FontFace.Parisienne
      ,FontFace.RobotoMono
      ,FontFace.PressStart2P 
      ,FontFace.EmilysCandy
      ,FontFace.Entypo// -- stb doesn't do this
      ,FontFace.Calibri
      , new FileLoc("gates.jpg",FileStorage.Embedded)
      , new FileLoc("zuck.jpg",FileStorage.Embedded)
      , new FileLoc("brady.jpg",FileStorage.Embedded)
      };
    }
    public static Gui2d GetOrCreateSharedGuiForView(string name, RenderView rv)
    {
      return new Gui2d(Gu.Gui2dManager.GetOrCreateGui2d(name, GetSharedGUIResources()), rv, UiStyleSheet.DefaultStyle);
    }
    public static Gui2d GameGui(RenderView rv)
    {
      return GetOrCreateSharedGuiForView("game-gui", rv);
    }
    public static Gui2d EditGui(RenderView rv)
    {
      var gui = GetOrCreateSharedGuiForView("edit-gui", rv);

      var test = new Dictionary<string, string>(){
          {"generate-base-land","Generate Base Land"}
          ,{"clear-all-land","Clear All Land"}
          ,{"destroy-all-objects","Destroy All Objects"}
          };

      //Toolbar 
      var tb = new UiToolbar("tb", null, null);
      tb.AddChild(new UiButton("name", "test", null, (i, e, m) => { System.Console.WriteLine("hi"); }));
      // new List<UiMenuItem>() {
      //   new UiMenuItem("mnuFile", "File").AddMenuItems(new List<UiMenuItem>() {
      //     new UiMenuItem("mnuExit", "Exit", (i,e,m)=>{ Environment.Exit(0); })
      //   }),
      //   new UiMenuItem("mnuView", "View").AddMenuItems(new List<UiMenuItem>() {
      //     new UiMenuItem("mnuFullscreen", "Fullscreen")
      //   }),
      //   new UiMenuItem("mnuOpts", "Options").AddMenuItems(new List<UiMenuItem>(){}),
      //   new UiMenuItem("mnuShowDbg", "Show Debug").AddMenuItems(new List<UiMenuItem>(){}),
      // });

      // tb.AddChild(gui.CreateMenuButton("file", null, "File", (i, e, m) =>
      // {
      //   e.InlineStyle.BorderRadius += 5;

      //   gui.ContextMenu.Show(m.Pos, new );
      // }));
      // tb.AddChild(gui.CreateMenuButton("options", null, "Options", (i, e, m) => { }));
      // tb.AddChild(gui.CreateMenuButton("showdbg", null, "ShowDbg", (i, e, m) =>
      // {
      //   gui.DebugDraw.DisableClip = !gui.DebugDraw.DisableClip;
      //   gui.DebugDraw.ShowOverlay = !gui.DebugDraw.ShowOverlay;
      // }));
      // tb.AddChild(gui.CreateMenuButton("rough+", null, "rough+", (i, e, m) => { }));
      // tb.AddChild(gui.CreateMenuButton("rough-", null, "rough-", (i, e, m) => { }));
      // tb.AddChild(gui.CreateMenuButton("spec+", null, "spec+", (i, e, m) => { }));
      // tb.AddChild(gui.CreateMenuButton("spec-", null, "spec-", (i, e, m) => { }));
      gui.AddChild(tb);

      // //Drag Panel
      // var dragpanel = gui.CreateLabel("drag", new vec2(400, 400), "DragMe", true, FontFace.Fancy, 35, vec4.rgba_ub(8, 70, 100, 255));
      // dragpanel.InlineStyle.MaxWHPX = new vec2(100, 100);
      // dragpanel.InlineStyle.Border = 2;
      // dragpanel.InlineStyle.BorderRadius = 10;
      // dragpanel.InlineStyle.BorderColor = vec4.rgba_ub(220, 119, 12, 180);
      // dragpanel.InlineStyle.Color = vec4.rgba_ub(240, 190, 100, 200);
      // dragpanel.InlineStyle.SizeModeWidth = UiSizeMode.Shrink;
      // dragpanel.InlineStyle.SizeModeHeight = UiSizeMode.Shrink;
      // dragpanel.InlineStyle.MaxWHPX = null;
      // dragpanel.EnableDrag((v) =>
      // {
      //   dragpanel.InlineStyle.Left += v.x;
      //   dragpanel.InlineStyle.Top += v.y;
      //   dragpanel.Text = "Thanks for dragging.";
      // });
      // gui.AddChild(dragpanel);

      // //Debug Info
      rv.DebugInfo = new UiLabel("debugInfo", null, "testxx", false, FontFace.RobotoMono, 25);
      gui.AddChild(rv.DebugInfo);

      // //Scrol
      // var test = gui.CreateLabel("scrollva", null, "Scroll Val:", true, FontFace.Pixel, 25, vec4.rgba_ub(18, 70, 90, 255));
      // gui.AddChild(test);

      // var cont = gui.CreateScrollbar("scrollbar", false, (f) =>
      // {
      //   test.Text = "Scroll Val:" + f;
      // });
      // gui.AddChild(cont);

      return gui;
    }
  }
}