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
      , new FileLoc("gates.jpg",FileStorage.Embedded) //testing many files / fast loading of megatex
      , new FileLoc("zuck.jpg",FileStorage.Embedded)
      , new FileLoc("brady.jpg",FileStorage.Embedded)
      };
    }
    public static Gui2d GetOrCreateSharedGuiForView(string name, RenderView rv)
    {
      return new Gui2d(Gu.Gui2dManager.GetOrCreateGui2d(name, GetSharedGUIResources()), rv);
    }
    public static Gui2d GameGui(RenderView rv)
    {
      //TODO: in-game gui
      return GetOrCreateSharedGuiForView("game-gui", rv);
    }
    public static List<UiStyle> GetGlobalStylesThatWeWillLaterLoadViaCSSFile(Gui2d gui)
    {
      //It works .. well .. ugh this is a nightmare
      // the thing is.. it's easier just to hard code stuff
      // how do i parse behaviors? well that is a problem.
      // font face is a problem
      // CSSFile css = new CSSFile(new FileLoc("ui-default.css", FileStorage.Embedded), gui);
      // css.Load();
      // css.PrintErrors();
      // return css.Styles;

      return new List<UiStyle>()
      {
        new UiStyle(StyleName.Base)
        {
          FontColor = new vec4(0.1f, 0.14f, 0.14f, 1),
          Color = new vec4(0.63f, 0.7f, 0.7f, 1),
          BorderColor = new vec4(0.14f, 0.16f, 0.16f, 1),
          Padding = 2,
          Margin = 0,
          Border = 0,
          MinWidth = 1,
          MinHeight = 1,
          MaxWidth = Gui2d.MaxSize,
          MaxHeight = Gui2d.MaxSize,
          FontFace = FontFace.RobotoMono,
          FontStyle = UiFontStyle.Normal,
          FontSize = 22,
          LineHeight = 1.0f,
          PositionMode = UiPositionMode.Static,
          SizeModeHeight = UiSizeMode.Shrink,
          SizeModeWidth = UiSizeMode.Shrink,
          DisplayMode = UiDisplayMode.Inline,
          OverflowMode = UiOverflowMode.Hide,
        },
        new UiStyle(StyleName.Button)
        {
          PositionMode = UiPositionMode.Static,
          SizeModeHeight = UiSizeMode.Shrink,
          SizeModeWidth = UiSizeMode.Shrink,
          DisplayMode = UiDisplayMode.Inline,
          OverflowMode = UiOverflowMode.Hide,
        },        
        new UiStyle(StyleName.Label, new List<string>() { StyleName.Base })
        {
          Texture = gui.DefaultPixel,
          SizeModeWidth = UiSizeMode.Shrink,
          SizeModeHeight = UiSizeMode.Shrink,
          PositionMode = UiPositionMode.Static,
          FontSize = 30,
          Padding = 10,
        },
        new UiStyle(StyleName.DebugLabel, new List<string>() { StyleName.Label })
        {
          Color = new vec4(0.63f, 0.7f, 0.7f, 0.3f)
        },
        new UiStyle(StyleName.Toolbar, new List<string>() { StyleName.Base })
        {
          Texture = gui.DefaultPixel,
          MinWidth = 0,
          MinHeight = 30,
          SizeModeWidth = UiSizeMode.Expand,
          SizeModeHeight = UiSizeMode.Shrink,
          FontSize = 40,
          FontFace = FontFace.Parisienne,
        },
        new UiStyle(StyleName.Panel, new List<string>() { StyleName.Base })
        {
          Texture = gui.DefaultPixel,
          SizeModeWidth = UiSizeMode.Expand,
          SizeModeHeight = UiSizeMode.Expand,
          Padding = 10,
          Margin = 0,
          BorderRadius = 0,
          FontFace = FontFace.PressStart2P,
          FontSize = 16
        }
      };

    }
    public static Gui2d EditGui(RenderView rv)
    {
      var gui = GetOrCreateSharedGuiForView("edit-gui", rv);

      //TODO: StyleSheet.Load()..

      gui.StyleSheet.AddStyles(GuiBuilder.GetGlobalStylesThatWeWillLaterLoadViaCSSFile(gui));

      var toolbar = new UiElement(new List<string> { StyleName.Toolbar }, "tlbToolbar", Phrase.None);
      gui.AddChild(toolbar);//testing all the jacked up chagnes
      var toolbar_button_file = new UiElement(new List<string> { StyleName.Button }, "btnFile", Phrase.File);
      toolbar.AddChild(toolbar_button_file);
     // toolbar_button_file.DoMouseEvents

      //var toolbar_menu = new UiElement(new List<string> { StyleName.Panel }, "mnuToolbarMenu");

      rv.DebugInfo = new UiElement(new List<string> { StyleName.DebugLabel }, "lblDebugInfo", Phrase.None, null);
      gui.AddChild(rv.DebugInfo);
      //gui.AddChild(new UiElement(new List<string> { StyleName.Panel }, "lblDebugInfo2", Phrase.None, null));

      return gui;
    }
  }


// public class Toolbar : UiElement { 
//  public Toolbar(string name) : base(StyleName.Toolbar) {
//  }
// }
public class PopupMenuButton : UiElement { 
  List<UiElement> _items = new List<UiElement>();
 public PopupMenuButton(string name) : base(StyleName.Label) {



 }
 public void AddMenuItem(UiElement item){
_items.Add(item);
 }
}


}//ns



// var test = new Dictionary<string, string>(){
//     {"generate-base-land","Generate Base Land"}
//     ,{"clear-all-land","Clear All Land"}
//     ,{"destroy-all-objects","Destroy All Objects"}
//     };

// //Toolbar 
// var tb = new UiToolbar("tb", null, null);
// tb.AddChild(new UiButton("name", "test", null, (i, e, m) => { System.Console.WriteLine("hi"); }));
// new List<UiMenuItem>() {
//   new UiMenuItem("mnuFile", "File").AddMenuItems(new List<UiMenuItem>() {
//     new UiMenuItem("mnuExit", "Exit", (i,e,m)=>{ Environment.Exit(0); })
//   }),
//   new UiMenuItem("mnuView", "View").AddMenuItems(new List<UiMenuItem>() {
//     new UiMenuItem("tmnuFullscreen", "Fullscreen")
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
//    gui.AddChild(tb);

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

// //Scrol
// var test = gui.CreateLabel("scrollva", null, "Scroll Val:", true, FontFace.Pixel, 25, vec4.rgba_ub(18, 70, 90, 255));
// gui.AddChild(test);

// var cont = gui.CreateScrollbar("scrollbar", false, (f) =>
// {
//   test.Text = "Scroll Val:" + f;
// });
// gui.AddChild(cont);
