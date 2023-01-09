using System;
using System.Collections.Generic;
using System.ComponentModel;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Loft
{
  //Script-side interfaces to implement in the .cs script file (Interfaces/data only)
  public interface IFunctionScript
  {
    public object? DoThing(object? param);
  }
  public interface IObjectScript
  {
    public void OnCreate();
    public void OnUpdate(double delta, WorldObject? ob);
    public void OnDestroy();
  }
  public interface IWorldScript
  {
    public void OnLoad(World w);
    public void OnUpdate(World w, double delta);
    public void OnExit(World w);
  }

  //UI

  public interface IUiElement
  {
    public bool Visible { get; set; }
    public string Text { get; set; }
    public string Name { get; set; }
    public IUiElement AddChild(IUiElement ele);
  }
  public interface IUiScrollRegion : IUiElement
  {
  }
  public interface IUiWindow : IUiElement
  {
    public void SetDefaultPosition();
    public IUiElement Content { get; }
    public bool TopMost { get; set; }
  }
  public interface IUiToast : IUiElement
  {
  }
  public interface IUiControls
  {
    public IGui2d CreateForView(RenderView rv);
    public void UpdateForView(RenderView rv);
    public IUiWindow CreateWindow(string title, vec2 pos, vec2 size);
  }
  public interface IGui2d : IUiElement
  {
    vec2 Scale { get; set; }
    IUiLayoutGlobals Globals { get; }

    public void OnResize();
    public void Update(double frameDelta);
    public void Show(string c_EditGUI_Root);
    public void Hide(string c_EditGUI_Root);
    public Drawable GetDrawable();
  }
  public class UiTextColor
  {
    public const string TextRed = "<rgba(.884,.15,.12,1)>";
    public const string TextYellow = "<rgba(.884,.899,.12,1)>";
    public const string TextDarkYellow = "<rgba(.484,.499,.12,1)>";
    public const string TextReset = "<reset>";
  }
  public interface IUiLayoutGlobals
  {
    public bool DisableClip { get; set; }
    public bool ShowDebug { get; set; } 
    public bool DisableMargins { get; set; } 
    public bool DisablePadding { get; set; } 
    public bool DisableBorders { get; set; } 
    public bool DisableAutos { get; set; } 
  }


}//ns


