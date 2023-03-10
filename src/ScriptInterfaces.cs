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
  #region General

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

  #endregion
  #region UI Enums

  public enum UiDisplayMode // display mode for static elements
  {
    Inline, //stays on line until end of line and wraps
    Block, //always wraps
    Word, //sticks with neighboring word elements (word wrap)
    NoWrap //never wraps
  }
  public enum UiPositionMode
  {
    Static, // computed position
    Fixed, // relative to parent content origin
    //Removed absolute position due to sorting issues
  }
  public enum UiAlignment
  {
    Left,
    Center,
    Right,
    Justify,
  }
  public enum UiSizeMode
  {
    Fixed, // Fixed width/height
    Content, //Shrink to size of contents
    Percent, //Expand to parent's maximum inherited width/height% (not MaxWidth, but the maximum clip width)
    Auto, // Auto 
          // 1 Fits remaining space based on AutoMode. 
          // 2 Multiple Autos take up equal space (limited by MaxWH)
          // 3 Can have zero width
          // 4 WILL WRAP or PUSH CONTENT if min > 0 or display==block
          // 6 Respects min/max property
    AutoContent, //same as Auto but will not shrink below content
  }
  public enum UiAutoMode
  {
    Line,//Expand autos up to the width of the computed static area
    Content,//Expand autos up to the parent's content area.
  }
  public enum UiRenderMode
  {
    None, //Note: DisplayMode = none, CSS.
    Color, // Flat color (Texture = default pixel)
    Textured, // Use custom Texture
  }
  public enum UiBuildOrder
  {
    Horizontal, //Shrink to size of child contents, taking Max Width/Height into account
    Vertical, //Expand to parent
  }
  public enum UiOverflowMode
  {
    Show, //default - shows all elements outside of content 
    Content, //hide everyting within the clip region, including elements overlapping parent padding
    Border, // allow elements to go over the border if they overflow.
    Padding // allow elements to go over the padding.
  };
  public enum UiImageTiling
  {
    Expand,//expand to fit quad
    Tile, //tile the image
    Computed, //tiling is computed
    Proportion //height is proportional to width
  }
  public enum UiFontStyle
  {
    Normal,
    Bold,
    Italic
  }
  public enum UiFloatMode
  {
    None, // flows within page/container, position is ignored (text)
    Floating, //Floats above parent. Element does not affect container region, or static elements. BUT affects clip region (for context menu).
  }
  public enum UiOrientation
  {
    //https://www.w3.org/TR/CSS2/visuren.html#propdef-direction
    Horizontal,
    Vertical,
  }
  public enum UiWrapMode
  {
    None,
    Char, //wrap at chars / elements (default)
    Word, //word wrap, see TextHyphens to hyphenate chars
    Line, //only wrap newlines (\n)
  }
  public enum UiLayoutDirection
  {
    LeftToRight, //roman (top to bot in vertical layout)
    RightToLeft, //arabic (bot to top in vertical layout)
  }
  public enum UiVStatMode
  {
    //this may not be necessary
    Line, //make new line for VAuto
    ParentContent, //Stretch across parent content
  }
  public enum UiTextHyphens
  {
    //Hyphenation rules when wrapping text
    None,
    Manual, // hyphens will be treated as line breaks
    //Auto,this is not necessary. removed 
  }
  public enum UiEventId
  {
    Undefined,

    LmbPress,
    LmbHold,
    LmbRelease,
    LmbUp,
    LmbDrag,
    RmbPress,
    RmbHold,
    RmbRelease,
    RmbUp,

    Mouse_Enter,
    Mouse_Move,
    Mouse_Leave,
    Mouse_Scroll,

    Lost_Focus,
    Got_Focus,
  };
  public enum UiMouseState
  {
    //This differs from ButtonState in the fact
    None,  // not hovering
    Enter, // hover start
    Hover, // hovering
    Move,  // hover + moved
    Leave, // hover end
    Press, // hover + click
    Hold,  // hover + hold
    Up   // hover + release
  }
  public enum UiPropName
  {
    Top
    , Left
    , MinWidth
    , MinHeight
    , MaxWidth
    , MaxHeight
    , FixedWidth
    , FixedHeight
    , PercentWidth
    , PercentHeight
    , PadTop
    , PadRight
    , PadBot
    , PadLeft
    , MarginTop
    , MarginRight
    , MarginBot
    , MarginLeft
    , BorderTop
    , BorderRight
    , BorderBot
    , BorderLeft
    , BorderTopLeftRadius
    , BorderTopRightRadius
    , BorderBotRightRadius
    , BorderBotLeftRadius
    , Color
    , MultiplyColor
    , BorderColorTop
    , BorderColorRight
    , BorderColorBot
    , BorderColorLeft
    , FontFace
    , FontSize
    , FontStyle
    , FontColor
    , LineHeight
    , Tracking
    , Texture
    , ImageTilingX
    , ImageTilingY
    , ImageScaleX
    , ImageScaleY
    , DisplayMode
    , PositionModeY
    , PositionModeX
    , OverflowMode
    , SizeModeWidth
    , SizeModeHeight
    , FloatMode
    , RenderMode
    , ContentAlignX
    , ContentAlignY
    , Opacity
    , LayoutOrientation
    , LayoutDirection
    , TextWrap
    , FontWeight
    , AutoModeWidth
    , AutoModeHeight
    , VStatMode
    , Hyphens

    //****
    , MaxUiProps
  }

  #endregion
  #region UI Classes

  public interface IUiElement
  {
    public bool Visible { get; set; }
    public string Text { get; set; }
    public string Name { get; set; }
    public IUiElement AddChild(IUiElement ele);
    public IUiStyle Style { get; }
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
  public static class UiFontFace
  {
    public static FileLoc Default { get { return UiFontFace.RobotoMono; } }
    public static FileLoc RobotoMono = new FileLoc("RobotoMono-Regular.ttf", EmbeddedFolder.Font, "RobotoMono");
    public static FileLoc Parisienne = new FileLoc("Parisienne-Regular.ttf", EmbeddedFolder.Font, "Parisienne");
    public static FileLoc PressStart2P = new FileLoc("PressStart2P-Regular.ttf", EmbeddedFolder.Font, "PressStart2P");
    public static FileLoc Calibri = new FileLoc("calibri.ttf", EmbeddedFolder.Font, "Calibri");
    public static FileLoc EmilysCandy = new FileLoc("EmilysCandy-Regular.ttf", EmbeddedFolder.Font, "EmilysCandy");
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
  public interface IUiTexture
  {
    public MtTex? Image { get; set; }
    public FileLoc? Loc { get; set; }
    public bool Modified { get; set; }
  }
  public interface IUiStyle
  {
    public string Name { get; set; }
    public bool Modified { get; }
    public bool IsInline { get; set; }

    public float MBP { set; }
    public float? Margin { get; set; }
    public float? Border { get; set; }
    public vec4? BorderColor { get; set; }
    public float? BorderRadius { get; set; }
    public float? Padding { get; set; }

    public UiSizeMode SizeMode { set; }
    public UiAutoMode AutoMode { set; }
    public float SizePercent { set; }
    public UiPositionMode PositionMode { set; }
    public float Min { set; }
    public float Max { set; }

    public float? Top { get; set; }
    public float? Left { get; set; }
    public float? MinWidth { get; set; }
    public float? MinHeight { get; set; }
    public float? MaxWidth { get; set; }
    public float? MaxHeight { get; set; }
    public float? FixedWidth { get; set; }
    public float? FixedHeight { get; set; }
    public float? PadTop { get; set; }
    public float? PadRight { get; set; }
    public float? PadBot { get; set; }
    public float? PadLeft { get; set; }
    public float? MarginTop { get; set; }
    public float? MarginRight { get; set; }
    public float? MarginBot { get; set; }
    public float? MarginLeft { get; set; }
    public float? BorderTop { get; set; }
    public float? BorderRight { get; set; }
    public float? BorderBot { get; set; }
    public float? BorderLeft { get; set; }
    public float? BorderTopLeftRadius { get; set; }
    public float? BorderTopRightRadius { get; set; }
    public float? BorderBotRightRadius { get; set; }
    public float? BorderBotLeftRadius { get; set; }
    public vec4? Color { get; set; }
    public vec4? MultiplyColor { get; set; }
    public vec4? BorderColorTop { get; set; }
    public vec4? BorderColorRight { get; set; }
    public vec4? BorderColorBot { get; set; }
    public vec4? BorderColorLeft { get; set; }
    public FileLoc FontFace { get; set; }
    public float? FontSize { get; set; }
    public UiFontStyle? FontStyle { get; set; }
    public vec4? FontColor { get; set; }
    public float? FontWeight { get; set; }
    public float? LineHeight { get; set; }
    public float? Tracking { get; set; }
    public UiPositionMode? PositionModeX { get; set; }
    public UiPositionMode? PositionModeY { get; set; }
    public UiOverflowMode? OverflowMode { get; set; }
    public UiSizeMode? SizeModeWidth { get; set; }
    public UiSizeMode? SizeModeHeight { get; set; }
    public UiAutoMode? AutoModeWidth { get; set; }
    public UiAutoMode? AutoModeHeight { get; set; }
    public UiDisplayMode? DisplayMode { get; set; }
    public UiImageTiling? ImageTilingX { get; set; }
    public UiImageTiling? ImageTilingY { get; set; }
    public float? ImageScaleX { get; set; }
    public float? ImageScaleY { get; set; }
    public IUiTexture Texture { get; set; }
    public UiFloatMode? FloatMode { get; set; }
    public UiRenderMode? RenderMode { get; set; }
    public UiAlignment? ContentAlignX { get; set; }
    public UiAlignment? ContentAlignY { get; set; }
    public float? Opacity { get; set; }
    public UiOrientation? LayoutOrientation { get; set; }
    public UiLayoutDirection? LayoutDirection { get; set; }
    public float? PercentWidth { get; set; }
    public float? PercentHeight { get; set; }
    public UiWrapMode? TextWrap { get; set; }
    public UiVStatMode? VAutoMode { get; set; }
    public UiTextHyphens? Hyphens { get; set; }

    public object? GetProp(UiPropName p);
    public void SetProp(UiPropName p, object? value);

    public float? L_Left(UiOrientation dir);
    public void L_Left(UiOrientation dir, float value);
    public void L_FixedWidth(UiOrientation dir, float value);

  }

  #endregion


}//ns


