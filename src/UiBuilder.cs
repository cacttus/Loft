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
  public enum UiStyleName
  {
     Inline ,
     BaseControl ,
     Label ,
     DebugLabel ,
     Panel ,
     Button ,
     Toolbar ,
     StatusBar ,
     ContextMenu ,
     EditGuiRoot ,
     VerticalBar ,
     MenuItem ,
     HBar ,
     ToolbarButton ,
  }
  //Hard coded User interface stuff.
  //Gui build
  public class UiBuilder
  {
  
    public static List<FileLoc> GetSharedGUIResources()
    {
      return new List<FileLoc>(){
       FontFace.Parisienne,
      FontFace.RobotoMono,
      //,FontFace.PressStart2P
   //   ,FontFace.EmilysCandy
      //,FontFace.Entypo// -- stb doesn't do this
      FontFace.Calibri,
      };
    }
    public static Gui2d GetOrCreateSharedGuiForView(string name, RenderView rv)
    {
      return new Gui2d(Gu.Gui2dManager.GetOrCreateGui2d(name, GetSharedGUIResources()), rv);
    }
    public static List<UiStyle> GetGlobalStyles(Gui2d gui)
    {
      return new List<UiStyle>()
      {
        new UiStyle(UiStyleName.BaseControl)
        {
          RenderMode = UiRenderMode.Color,
          FontColor = vec4.rgba_ub(42,42,42, 255),
          Color = vec4.rgba_ub(230,230,230, 255),
          BorderColor = new vec4(0.14f, 0.16f, 0.16f, 1),
          Padding = 0,
          Margin = 0,
          Border = 0,
          MinWidth = 0,//Min width of zero should be allowed
          MinHeight = 0,
          MaxWidth = Gui2d.MaxSize,
          MaxHeight = Gui2d.MaxSize,
          FontFace = FontFace.Calibri,
          FontStyle = UiFontStyle.Normal,
          FontSize = 22,
          LineHeight = 1.0f,
          PositionMode = UiPositionMode.Static,
          SizeModeHeight = UiSizeMode.Shrink,
          SizeModeWidth = UiSizeMode.Shrink,
          DisplayMode = UiDisplayMode.Inline,
          OverflowMode = UiOverflowMode.Hide,
          FloatMode = UiFloatMode.None
        },
        new UiStyle(UiStyleName.Label, UiStyleName.BaseControl)
        {
          SizeModeWidth = UiSizeMode.Shrink,
          SizeModeHeight = UiSizeMode.Shrink,
          PositionMode = UiPositionMode.Static,
          FontSize = 18,
          MarginLeft = 5,
          MarginRight = 5,
          MarginTop = 1,
          MarginBot = 3,
        },
        new UiStyle(UiStyleName.VerticalBar, UiStyleName.BaseControl)
        {
          SizeModeWidth = UiSizeMode.Shrink,
          SizeModeHeight = UiSizeMode.Expand,
          PositionMode = UiPositionMode.Static,
          DisplayMode = UiDisplayMode.Inline,
          Width = 1,
          MarginTop= 2,
          MarginBot= 2,
          MarginLeft=2,
          MarginRight=2,
          Padding = 0,
          Color = new vec4(0.6f, 0.6f, 0.6f, 0.8f),
        },
        new UiStyle(UiStyleName.HBar)
        {
          RenderMode = UiRenderMode.Color,
          SizeModeWidth = UiSizeMode.Expand,
          SizeModeHeight = UiSizeMode.Shrink,
          PositionMode = UiPositionMode.Static,
          DisplayMode = UiDisplayMode.Inline,
          Height = 1,
          MinHeight = 1,
          MarginTop = 2,
          MarginBot = 2,
          MarginLeft = 2,
          MarginRight = 2,
          PadLeft = 2,
          PadRight = 2,
          Color = new vec4(0.6f, 0.6f, 0.6f, 1),
        },
        new UiStyle(UiStyleName.DebugLabel, UiStyleName.Label)
        {
          MaxWidth = 500
          ,FontSize = 16
          ,DisplayMode = UiDisplayMode.Block
          ,PadTop = 2
          ,PadLeft = 2
          , FontFace = FontFace.RobotoMono
        },

        new UiStyle(UiStyleName.Panel, UiStyleName.BaseControl)
        {
          SizeModeWidth = UiSizeMode.Expand,
          SizeModeHeight = UiSizeMode.Expand,
          Padding = 0,
          Margin = 10,
          BorderRadius = 0,
          FontFace = FontFace.Calibri,
          FontSize = 16
        },
        new UiStyle(UiStyleName.EditGuiRoot)
        {
          SizeModeWidth = UiSizeMode.Expand,
          SizeModeHeight = UiSizeMode.Expand,
          MinWidth = 0,
          MinHeight = 0,
          MaxWidth = Gui2d.MaxSize,
          MaxHeight = Gui2d.MaxSize,
          DisplayMode = UiDisplayMode.Inline,
          PositionMode = UiPositionMode.Relative
        },
        new UiStyle(UiStyleName.ContextMenu, UiStyleName.Label)
        {
          Color = vec4.rgba_ub(245,245,245, 255),
          PositionMode = UiPositionMode.Absolute,
          FloatMode = UiFloatMode.Floating,
          MaxWidth = 500,
          MinWidth = 10,
          SizeModeWidth = UiSizeMode.Shrink,
          //OverflowMode = UiOverflowMode.Show,
          SizeModeHeight = UiSizeMode.Shrink,
          Padding=0,
          Border=0,
          Margin=0,
          ColorMul = new vec4(1,1)
        },
        new UiStyle(UiStyleName.MenuItem, UiStyleName.Label)
        {
          SizeModeWidth = UiSizeMode.Expand,
          SizeModeHeight = UiSizeMode.Shrink,
          PositionMode = UiPositionMode.Static,
          MaxWidth = 500,
          MinWidth = 0,
          MarginTop = 6,
          MarginBot = 6,
          MarginLeft = 20,
          MarginRight = 20,
          Padding=0,
        },
        new UiStyle(UiStyleName.Toolbar, UiStyleName.BaseControl)
        {
          MinWidth = 0,
          MinHeight = 20,
          MaxWidth = Gui2d.MaxSize,
          SizeModeWidth = UiSizeMode.Expand,
          SizeModeHeight = UiSizeMode.Shrink,
          MaxHeight = 60,
          Margin=0,
          Padding=0,
          BorderBot=1,
          Color = vec4.rgba_ub(240,240,240),
          BorderBotColor = vec4.rgba_ub(110,110,110)
        },
      };
    }
    public static void MakeGui(RenderView rv)
    {
      var toolbar = new UiToolbar();

      toolbar.AddItem(new UiToolbarButton(Phrase.File))
        .AddItem("Toggle VSync", e => Gu.World.Editor.DoEvent(WorldEditEvent.Debug_ToggleVSync))
        .AddItem("Quit", e => Gu.World.Editor.DoEvent(WorldEditEvent.Quit))
        ;

      toolbar.AddItem(new UiToolbarButton(Phrase.Edit))
        .AddItem("Undo", e => Gu.World.Editor.DoEvent(WorldEditEvent.Edit_Undo))
        .AddItem("Redo", e => Gu.World.Editor.DoEvent(WorldEditEvent.Edit_Redo))
        .AddItem("Delete", e => Gu.World.Editor.DoEvent(WorldEditEvent.Edit_Delete))
        ;

string ipsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Id cursus metus aliquam eleifend mi. Cras ornare arcu dui vivamus arcu felis. Eu ultrices vitae auctor eu augue ut lectus arcu bibendum. Etiam tempor orci eu lobortis elementum nibh. Aliquet porttitor lacus luctus accumsan tortor posuere ac ut. Nulla aliquet enim tortor at auctor urna nunc. Ultricies lacus sed turpis tincidunt id aliquet. Elit pellentesque habitant morbi tristique senectus et netus. Ac turpis egestas integer eget aliquet nibh. Amet commodo nulla facilisi nullam vehicula ipsum a arcu cursus. Tellus in hac habitasse platea dictumst vestibulum rhoncus. Dis parturient montes nascetur ridiculus. Augue interdum velit euismod in pellentesque massa placerat duis. Consectetur adipiscing elit ut aliquam purus sit amet luctus venenatis. Egestas purus viverra accumsan in nisl nisi.";
string ipsum_long = "Sodales ut etiam sit amet nisl purus in. Porta non pulvinar neque laoreet suspendisse interdum consectetur. Gravida arcu ac tortor dignissim. Nisl suscipit adipiscing"+
 "bibendum est ultricies integer quis auctor. Molestie at elementum eu facilisis sed odio morbi. Magna ac placerat vestibulum lectus mauris ultrices eros in cursus. Lacus laoreet non curabitur "+
 "gravida arcu ac. Est velit egestas dui id ornare arcu odio ut sem. Dictum varius duis at consectetur. Mus mauris vitae ultricies leo integer. Tellus orci ac auctor augue mauris augue neque gravida "+
 "in. Purus non enim praesent elementum facilisis leo vel fringilla est. Quam adipiscing vitae proin sagittis nisl rhoncus mattis rhoncus urna. Proin sagittis nisl "+
"rhoncus mattis rhoncus. Velit scelerisque in dictum non. Tristique senectus et netus et. Egestas maecenas pharetra convallis posuere. Bibendum arcu vitae elementum curabitur."+
"Turpis egestas maecenas pharetra convallis posuere morbi leo. Elit sed vulputate mi sit amet mauris commodo. Nulla facilisi morbi tempus iaculis urna id volutpat. Convallis posuere morbi leo"+
 "urna molestie at elementum. Nec dui nunc mattis enim ut. Pharetra sit amet aliquam id diam maecenas ultricies mi. Elementum curabitur vitae nunc sed velit dignissim. Nec ultrices dui sapien"+
  "eget mi proin sed libero enim. Nibh cras pulvinar mattis nunc sed blandit libero volutpat sed. Sed augue lacus viverra vitae. Sed ullamcorper morbi tincidunt ornare massa eget egestas. Eget nunc "+
  "scelerisque viverra mauris in. Maecenas volutpat blandit aliquam etiam. Sagittis vitae et leo duis. Vel orci porta non pulvinar. Praesent elementum facilisis leo vel fringilla.";

      toolbar.AddItem(new UiToolbarButton("Test"))
        .AddItem("Show Message Box", e => Gu.MessageBox("Guess what?", $"Calling you direct from 1-800-{e.Element._iPickId}"))
        .AddItem("DO TOAST", e=> e.State.Gui?.RenderView?.Toast.Show("Hello world!"))
        .AddSubMenu(Phrase.Atlas)
          .AddItem("big toast", e=> e.State.Gui?.RenderView?.Toast.Show($"This is a particularly long toast:{ipsum}"))
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

      //*** Debug info panels
      rv.WorldDebugInfo = new UiElement(UiStyleName.DebugLabel);
      rv.WorldDebugInfo.Visible = true;

      rv.GpuDebugInfo = new UiElement(UiStyleName.DebugLabel);
      rv.GpuDebugInfo.Style.MaxWidth = 600;
      rv.GpuDebugInfo.Visible = false;

      rv.ControlsInfo = new UiElement(UiStyleName.DebugLabel);
      rv.ControlsInfo.Style.MaxWidth = 999;
      rv.ControlsInfo.Visible = false;

      //Root the edit GUI so we can hide it.
      var editgui_root = new UiElement(UiStyleName.EditGuiRoot);
      editgui_root.AddChild(toolbar);//testing all the jacked up chagnes
      editgui_root.AddChild(rv.WorldDebugInfo);
      editgui_root.AddChild(rv.GpuDebugInfo);
      editgui_root.AddChild(rv.ControlsInfo);
      rv.Toast =new UiToast("this is toast");
      editgui_root.AddChild(rv.Toast);
      //editgui_root.AddChild(TestTextAlign());

      var g = GetOrCreateSharedGuiForView("main-edit-gui", rv);
      g.StyleSheet.AddStyles(UiBuilder.GetGlobalStyles(g));
      g.AddChild(editgui_root);
      rv.Gui = g;
    }

    public static UiElement TestTextAlign()
    {
      UiElement lefttest = new UiElement("left");
      lefttest.Style.RenderMode = UiRenderMode.Color;
      lefttest.Style.Color = new vec4(.7f, 1, 1, 1);
      lefttest.Style.SizeModeWidth = UiSizeMode.Expand;
      lefttest.Style.SizeModeHeight = UiSizeMode.Shrink;
      lefttest.Style.TextAlign = UiAlignment.Left;
      lefttest.Style.FontSize = 22;
      lefttest.Style.Padding = 5;
      lefttest.Style.Margin = 5;
      lefttest.Style.PositionMode = UiPositionMode.Static;

      UiElement righttest = new UiElement("ret");
      righttest.Style.RenderMode = UiRenderMode.Color;
      righttest.Style.Color = new vec4(.7f, 1, .7f, 1);
      righttest.Style.SizeModeWidth = UiSizeMode.Expand;
      righttest.Style.SizeModeHeight = UiSizeMode.Shrink;
      righttest.Style.TextAlign = UiAlignment.Right;
      righttest.Style.FontSize = 22;
      righttest.Style.Padding = 5;
      righttest.Style.Margin = 5;
      righttest.Style.PositionMode = UiPositionMode.Static;

      UiElement centertest = new UiElement("ret");
      centertest.Style.RenderMode = UiRenderMode.Color;
      centertest.Style.Color = new vec4(.7f, .7f, .7f, 1);
      centertest.Style.PositionMode = UiPositionMode.Static;
      centertest.Style.SizeModeWidth = UiSizeMode.Expand;
      centertest.Style.SizeModeHeight = UiSizeMode.Shrink;
      centertest.Style.TextAlign = UiAlignment.Center;
      centertest.Style.FontSize = 22;
      centertest.Style.Padding = 5;
      centertest.Style.Margin = 5;

      UiElement testcont = new UiElement("testcont");
      testcont.Style.RenderMode = UiRenderMode.Color;
      testcont.Style.Color = new vec4(1, 1, 1, 1);
      testcont.Style.Left = 700;
      testcont.Style.Top = 200;
      testcont.Style.Width = 200;
      testcont.Style.Margin = 0;
      testcont.Style.SizeModeHeight = UiSizeMode.Shrink;
      testcont.Style.SizeModeWidth = UiSizeMode.Fixed;
      testcont.Style.PositionMode = UiPositionMode.Absolute;
      testcont.AddChild(righttest);
      testcont.AddChild(lefttest);
      testcont.AddChild(centertest);

      return testcont;
    }

      // //testing new border stuff
      // UiElement eee = new UiElement("testbor", null, "hElLo!");
      // eee.Style.Width = 60;
      // eee.Style.Height = 30;
      // eee.Style.SizeModeWidth = UiSizeMode.Shrink;
      // eee.Style.SizeModeHeight = UiSizeMode.Fixed;
      // eee.Style.BorderRadius = 4;
      // eee.Style.BorderTop = 5;
      // eee.Style.DisplayMode = UiDisplayMode.Inline;
      // eee.Style.BorderTopColor = new vec4(1, 0, 0, 1);
      // eee.Style.BorderRight = 5;
      // eee.Style.BorderRightColor = new vec4(0, 1, 0, 1);
      // eee.Style.BorderBot = 5;
      // eee.Style.BorderBotColor = new vec4(0, 0, 1, 1);
      // eee.Style.BorderLeft = 5;
      // eee.Style.BorderLeftColor = new vec4(1, 0, 1, 1);
      // eee.Style.FontSize = 14;
      // eee.Style.Padding = 0;
      // eee.Style.PadLeft = 1;
      // eee.Style.Margin = 0;
      // toolbar.AddChild(eee);


  }//cls




}//ns


