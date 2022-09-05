﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Windowing.GraphicsLibraryFramework;
namespace PirateCraft
{
  #region Enums

  public enum UiFontStyle
  {
    Normal,
    Bold,
    Italic
  }
  public class FontFace : FileLoc
  {
    protected FontFace(FileLoc loc) : base(loc.RawPath, loc.FileStorage) { }
    public static FontFace Parisienne = new FontFace(new FileLoc("Parisienne-Regular.ttf", FileStorage.Embedded));
    public static FontFace RobotoMono = new FontFace(new FileLoc("RobotoMono-Regular.ttf", FileStorage.Embedded));
    public static FontFace PressStart2P = new FontFace(new FileLoc("PressStart2P-Regular.ttf", FileStorage.Embedded));
    public static FontFace Entypo = new FontFace(new FileLoc("Entypo.ttf", FileStorage.Embedded));
    public static FontFace Calibri = new FontFace(new FileLoc("calibri.ttf", FileStorage.Embedded));
    public static FontFace EmilysCandy = new FontFace(new FileLoc("EmilysCandy-Regular.ttf", FileStorage.Embedded));
  }
  public enum UiDisplayMode
  {
    Inline,
    Block,
    InlineNoWrap
  }
  public enum UiDimUnit
  {
    Percent,
    Pixel,
    Auto /*, Initial*/
  }
  public enum UiPositionMode
  {
    //Note: the position terminology here mirrors that of CSS. 
    Static, // elements flow within the page.
    Relative, // elements are relative to the container.
    RelativeConstrainX, //Relative positioning, but cannot go outside parent boundary
    RelativeConstrainY,
    RelativeConstrainXY
    //absolute: relative to the whole document.
  }
  public enum UiSizeMode
  {
    Shrink,
    Expand,
  }
  public enum UiEventId
  {
    Mouse_Lmb_Up,
    Mouse_Lmb_Press,
    Mouse_Lmb_Hold,
    Mouse_Lmb_Release,
    Mouse_Lmb_None,

    Mouse_Rmb_Up,
    Mouse_Rmb_Press,
    Mouse_Rmb_Hold,
    Mouse_Rmb_Release,
    Mouse_Rmb_None,

    Mouse_Mmb_Up,
    Mouse_Mmb_Press,
    Mouse_Mmb_Hold,
    Mouse_Mmb_Release,
    Mouse_Mmb_None,

    Scrollbar_Pos_Change,

    Mouse_Enter,
    Mouse_Move,
    Mouse_Leave,
    Drag,
    Tick, //tick event like every x ms
  };
  public enum UiOverflowMode
  {
    Show,
    Hide
  };
  public enum UiImageTiling
  {
    Expand,
    Tile,
    Computed,
    Proportion
  }
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

  #endregion
  #region Helpers

  public class UiDebugDraw
  {
    public bool DisableClip = false;
    public bool ShowOverlay = false;
    public vec4 OverlayColor = new vec4(1, 0, 0, 0.5f);
  }
  public class UiStyleProps
  {
    //All styled properties of an element. Props are initially null because they are cascading. Any null property defers to its sub, or the default value.
    public UiStyleProps()
    {
    }
    public void SetDefault()
    {
      _texture = new UiRef<MtTex>(null);
      _color = new vec4(1, 1, 1, 1);
      _borderColor = new vec4(1, 1, 1, 1);
      _fontStyle = UiFontStyle.Normal;
      _fontColor = new vec4(0, 0, 0, 1);
      _fontFace = new UiRef<FontFace>(FontFace.RobotoMono);
      _imageTilingX = UiImageTiling.Expand;
      _imageTilingY = UiImageTiling.Expand;
      _displayMode = UiDisplayMode.Block;
      _minWHPX = new vec2(10, 10);
      _maxWHPX = new vec2(99999, 99999);
      _positionMode = UiPositionMode.Static;
      _overflowMode = UiOverflowMode.Hide;
      _sizeModeWidth = UiSizeMode.Shrink;
      _sizeModeHeight = UiSizeMode.Shrink;
      _fontSize = 20;
      _lineHeight = 1;
      _top = 50;
      _left = 50;
      _width = 100;
      _height = 100;
      _padTop = 0;
      _padRight = 0;
      _padBot = 0;
      _padLeft = 0;
      _marTop = 0;
      _marRight = 0;
      _marBot = 0;
      _marLeft = 0;
      _borderTop = 0;
      _borderRight = 0;
      _borderBot = 0;
      _borderLeft = 0;
      _borderTopLeftRadius = 0;
      _borderTopRightRadius = 0;
      _borderBotRightRadius = 0;
      _borderBotLeftRadius = 0;
      _width = 10;
      _height = 10;
      _events = null;//EventId, list of action (EventId, Object)
    }
    public UiStyleProps Clone()
    {
      UiStyleProps ret = new UiStyleProps();

      if (this._texture != null) { ret._texture = this._texture; }
      if (this._color != null) { ret._color = this._color.Value; }
      if (this._borderColor != null) { ret._borderColor = this._borderColor.Value; }
      if (this._fontStyle != null) { ret._fontStyle = this._fontStyle.Value; }
      if (this._fontColor != null) { ret._fontColor = this._fontColor.Value; }
      if (this._fontFace != null) { ret._fontFace = this._fontFace; }
      if (this._imageTilingX != null) { ret._imageTilingX = this._imageTilingX.Value; }
      if (this._imageTilingY != null) { ret._imageTilingY = this._imageTilingY.Value; }
      if (this._displayMode != null) { ret._displayMode = this._displayMode.Value; }
      if (this._minWHPX != null) { ret._minWHPX = this._minWHPX.Value; }
      if (this._maxWHPX != null) { ret._maxWHPX = this._maxWHPX.Value; }
      if (this._positionMode != null) { ret._positionMode = this._positionMode.Value; }
      if (this._overflowMode != null) { ret._overflowMode = this._overflowMode.Value; }
      if (this._sizeModeWidth != null) { ret._sizeModeWidth = this._sizeModeWidth.Value; }
      if (this._sizeModeHeight != null) { ret._sizeModeHeight = this._sizeModeHeight.Value; }
      if (this._fontSize != null) { ret._fontSize = this._fontSize.Value; }
      if (this._lineHeight != null) { ret._lineHeight = this._lineHeight.Value; }
      if (this._top != null) { ret._top = this._top.Value; }
      if (this._left != null) { ret._left = this._left.Value; }
      if (this._width != null) { ret._width = this._width.Value; }
      if (this._height != null) { ret._height = this._height.Value; }
      if (this._padTop != null) { ret._padTop = this._padTop.Value; }
      if (this._padRight != null) { ret._padRight = this._padRight.Value; }
      if (this._padBot != null) { ret._padBot = this._padBot.Value; }
      if (this._padLeft != null) { ret._padLeft = this._padLeft.Value; }
      if (this._marTop != null) { ret._marTop = this._marTop.Value; }
      if (this._marRight != null) { ret._marRight = this._marRight.Value; }
      if (this._marBot != null) { ret._marBot = this._marBot.Value; }
      if (this._marLeft != null) { ret._marLeft = this._marLeft.Value; }
      if (this._borderTop != null) { ret._borderTop = this._borderTop.Value; }
      if (this._borderRight != null) { ret._borderRight = this._borderRight.Value; }
      if (this._borderBot != null) { ret._borderBot = this._borderBot.Value; }
      if (this._borderLeft != null) { ret._borderLeft = this._borderLeft.Value; }
      if (this._events != null) { ret._events = this._events; }
      if (this._borderTopLeftRadius != null) { ret._borderTopLeftRadius = this._borderTopLeftRadius; }
      if (this._borderTopRightRadius != null) { ret._borderTopRightRadius = this._borderTopRightRadius; }
      if (this._borderBotRightRadius != null) { ret._borderBotRightRadius = this._borderBotRightRadius; }
      if (this._borderBotLeftRadius != null) { ret._borderBotLeftRadius = this._borderBotLeftRadius; }
      if (this._width != null) { ret._width = this._width; }
      if (this._height != null) { ret._height = this._height; }

      return ret;
    }
    public void ApplySubclass(UiStyleProps sub)
    {
      Gu.Assert(sub != null);

      if (sub._texture != null) { this._texture = sub._texture; }
      if (sub._color != null) { this._color = sub._color; }
      if (sub._borderColor != null) { this._borderColor = sub._borderColor; }
      if (sub._fontStyle != null) { this._fontStyle = sub._fontStyle; }
      if (sub._fontColor != null) { this._fontColor = sub._fontColor; }
      if (sub._fontFace != null) { this._fontFace = sub._fontFace; }
      if (sub._imageTilingX != null) { this._imageTilingX = sub._imageTilingX; }
      if (sub._imageTilingY != null) { this._imageTilingY = sub._imageTilingY; }
      if (sub._displayMode != null) { this._displayMode = sub._displayMode; }
      if (sub._minWHPX != null) { this._minWHPX = sub._minWHPX; }
      if (sub._maxWHPX != null) { this._maxWHPX = sub._maxWHPX; }
      if (sub._positionMode != null) { this._positionMode = sub._positionMode; }
      if (sub._overflowMode != null) { this._overflowMode = sub._overflowMode; }
      if (sub._sizeModeWidth != null) { this._sizeModeWidth = sub._sizeModeWidth; }
      if (sub._sizeModeHeight != null) { this._sizeModeHeight = sub._sizeModeHeight; }
      if (sub._fontSize != null) { this._fontSize = sub._fontSize; }
      if (sub._lineHeight != null) { this._lineHeight = sub._lineHeight; }
      if (sub._top != null) { this._top = sub._top; }
      if (sub._left != null) { this._left = sub._left; }
      if (sub._width != null) { this._width = sub._width; }
      if (sub._height != null) { this._height = sub._height; }
      if (sub._padTop != null) { this._padTop = sub._padTop; }
      if (sub._padRight != null) { this._padRight = sub._padRight; }
      if (sub._padBot != null) { this._padBot = sub._padBot; }
      if (sub._padLeft != null) { this._padLeft = sub._padLeft; }
      if (sub._marTop != null) { this._marTop = sub._marTop; }
      if (sub._marRight != null) { this._marRight = sub._marRight; }
      if (sub._marBot != null) { this._marBot = sub._marBot; }
      if (sub._marLeft != null) { this._marLeft = sub._marLeft; }
      if (sub._borderTop != null) { this._borderTop = sub._borderTop; }
      if (sub._borderRight != null) { this._borderRight = sub._borderRight; }
      if (sub._borderBot != null) { this._borderBot = sub._borderBot; }
      if (sub._borderLeft != null) { this._borderLeft = sub._borderLeft; }
      if (sub._events != null) { this._events = sub._events; }
      if (sub._borderTopLeftRadius != null) { this._borderTopLeftRadius = sub._borderTopLeftRadius; }
      if (sub._borderTopRightRadius != null) { this._borderTopRightRadius = sub._borderTopRightRadius; }
      if (sub._borderBotRightRadius != null) { this._borderBotRightRadius = sub._borderBotRightRadius; }
      if (sub._borderBotLeftRadius != null) { this._borderBotLeftRadius = sub._borderBotLeftRadius; }
      if (sub._width != null) { this._width = sub._width; }
      if (sub._height != null) { this._height = sub._height; }
    }

    public UiRef<MtTex> _texture = null;
    public vec4? _color = null;
    public vec4? _borderColor = null;
    public UiFontStyle? _fontStyle = null;
    public vec4? _fontColor = null;
    public UiRef<FontFace> _fontFace = null;
    public UiImageTiling? _imageTilingX = null;
    public UiImageTiling? _imageTilingY = null;
    public UiDisplayMode? _displayMode = null;
    public vec2? _minWHPX = null;
    public vec2? _maxWHPX = null;
    public UiPositionMode? _positionMode = null;
    public UiOverflowMode? _overflowMode = null;
    public UiSizeMode? _sizeModeWidth = null;
    public UiSizeMode? _sizeModeHeight = null;
    public float? _fontSize = null;
    public float? _lineHeight = null;
    public float? _top = null;
    public float? _left = null;
    public float? _right { get { return _left + _width; } }
    public float? _bottom { get { return _top + _height; } }
    public float? _width = null;
    public float? _height = null;
    public float? _padTop = null;
    public float? _padRight = null;
    public float? _padBot = null;
    public float? _padLeft = null;
    public float? _marTop = null;
    public float? _marRight = null;
    public float? _marBot = null;
    public float? _marLeft = null;
    public float? _borderTop = null;
    public float? _borderRight = null;
    public float? _borderBot = null;
    public float? _borderLeft = null;
    public float? _borderTopLeftRadius = null;
    public float? _borderTopRightRadius = null;
    public float? _borderBotRightRadius = null;
    public float? _borderBotLeftRadius = null;
    public UiRef<Dictionary<UiEventId, MultiMap<string, Action<UiEventId, UiElement, PCMouse>>>> _events = null;

    public new string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine($"_texture                {this._texture.ToString()}");
      sb.AppendLine($"_color                  {this._color.ToString()}");
      sb.AppendLine($"_borderColor            {this._borderColor.ToString()}");
      sb.AppendLine($"_fontStyle              {this._fontStyle.ToString()}");
      sb.AppendLine($"_fontColor              {this._fontColor.ToString()}");
      sb.AppendLine($"_fontFace               {this._fontFace.ToString()}");
      sb.AppendLine($"_imageTilingX           {this._imageTilingX.ToString()}");
      sb.AppendLine($"_imageTilingY           {this._imageTilingY.ToString()}");
      sb.AppendLine($"_displayMode            {this._displayMode.ToString()}");
      sb.AppendLine($"_minWHPX                {this._minWHPX.ToString()}");
      sb.AppendLine($"_maxWHPX                {this._maxWHPX.ToString()}");
      sb.AppendLine($"_positionMode           {this._positionMode.ToString()}");
      sb.AppendLine($"_overflowMode           {this._overflowMode.ToString()}");
      sb.AppendLine($"_sizeModeWidth          {this._sizeModeWidth.ToString()}");
      sb.AppendLine($"_sizeModeHeight         {this._sizeModeHeight.ToString()}");
      sb.AppendLine($"_fontSize               {this._fontSize.ToString()}");
      sb.AppendLine($"_lineHeight             {this._lineHeight.ToString()}");
      sb.AppendLine($"_top                    {this._top.ToString()}");
      sb.AppendLine($"_left                   {this._left.ToString()}");
      sb.AppendLine($"_width                  {this._width.ToString()}");
      sb.AppendLine($"_height                 {this._height.ToString()}");
      sb.AppendLine($"_padTop                 {this._padTop.ToString()}");
      sb.AppendLine($"_padRight               {this._padRight.ToString()}");
      sb.AppendLine($"_padBot                 {this._padBot.ToString()}");
      sb.AppendLine($"_padLeft                {this._padLeft.ToString()}");
      sb.AppendLine($"_marTop                 {this._marTop.ToString()}");
      sb.AppendLine($"_marRight               {this._marRight.ToString()}");
      sb.AppendLine($"_marBot                 {this._marBot.ToString()}");
      sb.AppendLine($"_marLeft                {this._marLeft.ToString()}");
      sb.AppendLine($"_borderTop              {this._borderTop.ToString()}");
      sb.AppendLine($"_borderRight            {this._borderRight.ToString()}");
      sb.AppendLine($"_borderBot              {this._borderBot.ToString()}");
      sb.AppendLine($"_borderLeft             {this._borderLeft.ToString()}");
      sb.AppendLine($"_events                 {this._events.ToString()}");
      sb.AppendLine($"_borderTopLeftRadius    {this._borderTopLeftRadius.ToString()}");
      sb.AppendLine($"_borderTopRightRadius   {this._borderTopRightRadius.ToString()}");
      sb.AppendLine($"_borderBotRightRadius   {this._borderBotRightRadius.ToString()}");
      sb.AppendLine($"_borderBotLeftRadius    {this._borderBotLeftRadius.ToString()}");
      sb.AppendLine($"_width                  {this._width.ToString()}");
      sb.AppendLine($"_height                 {this._height.ToString()}");
      return sb.ToString();
    }
  }
  public class UiDragInfo
  {
    private bool _bDragStart = false;
    private vec2 _vDragStart;
    private WeakReference<UiElement> Target { get; set; } = null;

    public UiDragInfo() { }
    public void StartDrag(UiElement target, PCMouse ms)
    {
      if (target != null)
      {
        Target = new WeakReference<UiElement>(target);
        _vDragStart = ms.Pos;
        _bDragStart = true;
      }
    }
    public void EndDrag()
    {
      Target = null;
      _bDragStart = false;
    }
    public void UpdateDrag(PCMouse ms)
    {
      float mw = 1.0f / 1; // UiScreen::getDesignMultiplierW the design multiplier - this isntn't accounted for
      float mh = 1.0f / 1; // UiScreen::getDesignMultiplierH the design multiplier - this isntn't accounted for
      if (Target != null && Target.TryGetTarget(out var tar))
      {
        //Check for mouse delta to prevent unnecessary updates.
        bool canDrag = false;
        vec2 dp = ms.Pos - _vDragStart;
        if (MathUtils.FuzzyEquals(dp.x, 0.0f) == false || MathUtils.FuzzyEquals(dp.y, 0.0f) == false)
        {
          canDrag = true;
        }
        else
        {
          canDrag = false;
        }
        if (canDrag)
        {
          //Multiply the distance by the design size.
          dp.x *= mw;
          dp.y *= mh;

          if (tar.DragFunc != null)
          {
            tar.DragFunc(dp);
          }

          //Reset drag start
          _vDragStart = ms.Pos;
        }
      }
      else
      {
        Target = null;
      }

    }
  }

  #endregion

  public class UiElement
  {
    private class UiLine
    {
      public float _top = 0;
      public float _left = 0;
      public float _height = 0;
      public float _width = 0;
      public List<UiElement> _eles = new List<UiElement>();
      public UiLine(float left, float top)
      {
        _left = left;
        _top = top;
      }
    }
    public class Quads
    {
      public Box2f _b2ComputedQuad = new Box2f();      // Base quad that is calculated from _right/_left.., parent, padding, etc
      public Box2f _b2OffsetQuad = new Box2f();        // Offseted quad
      public Box2f _b2LayoutQuad = new Box2f();        // Transformed from design space into screen space.
      public Box2f _b2RasterQuad = new Box2f();      // Final raster quad in OpenGL screen coordinates. BOTTOM LEFT = min TOP RIGHT = max
      public Box2f _b2PreOffsetRasterQuad = new Box2f();      // Debug, raster quad for pre-offset
      public vec2 _rtl = new vec2(0, 0);
      public vec2 _rtr = new vec2(0, 0);
      public vec2 _rbr = new vec2(0, 0);
      public vec2 _rbl = new vec2(0, 0);
    }

    #region Public: Members

    public string Name { get { return _name; } set { _name = value; } }
    public string Tag { get; set; } = "";
    public string Text
    {
      get { return _strText; }
      set
      {
        _strTextLast = _strText;
        _strText = value;
        SetLayoutChanged();
        _bTextChanged = true;
      }
    }
    public UiStyle StyleClass
    {
      get { return _styleClass; }
      set
      {
        if (_styleClass == value)
        {
          return;
        }
        if (_styleClass != null && value._eles != null)
        {
          _styleClass._eles.Remove(new WeakReference<UiElement>(this));
        }
        _styleClass = value;
        if (_styleClass != null)
        {
          if (value._eles == null)
          {
            value._eles = new List<WeakReference<UiElement>>();
          }
          value._eles.Add(new WeakReference<UiElement>(this));
        }
        SetLayoutChanged();
      }
    }
    public UiStyle InlineStyle
    {
      //overrides Style Class
      get
      {
        if (_inlineStyle == null)
        {
          _inlineStyle = new UiStyle();
          _inlineStyle._eles = new List<WeakReference<UiElement>>();
          _inlineStyle._eles.Add(new WeakReference<UiElement>(this));
        }
        return _inlineStyle;
      }
      set
      {
        _inlineStyle = value;
      }
    }
    public bool IsPickRoot { get { return _isPickRoot; } set { _isPickRoot = value; } }
    public bool PickEnabled { get { return _pickEnabled; } set { _pickEnabled = value; } } //Prevents the pick algorithm from running on misc elements (such as glyphs).
    public bool DragEnabled { get; private set; } = false;
    public Action<vec2> DragFunc { get; private set; } = null;
    public float MinValue { get; set; } = 0;
    public float MaxValue { get; set; } = 100;
    public bool ScaleToDesign { get; set; } = true; // this is true for all elements besides cursor.
    public bool LayoutChanged { get; private set; } = true;
    public bool LayoutVisible { get { return _layoutVisible; } set { _layoutVisible = value; SetLayoutChanged(); } }
    public bool RenderVisible { get { return _renderVisible; } set { _renderVisible = value; SetLayoutChanged(); } }
    public UiStyleProps Props { get { return _props; } }

    #endregion
    #region Private: Members

    protected const int c_AllSortLayers = -1;
    protected const int c_BaseLayerSort = 1000;
    protected const int c_GlyphLayerSort = 2000;
    public const int c_ContextMenuSort = 3000;
    protected UiStyleProps _props = new UiStyleProps();
    protected UiStyle _inlineStyle = null;      //Inline style
    protected UiStyle _styleClass = null;      //class styles
    protected UiStyle _glyphStyle = null; //Style for glyphs, in case this has text.
    protected MultiMap<int, UiElement> _children { get; set; } = null;
    private WeakReference<UiElement> _parent = null;
    private MtFontLoader _cachedFont = null;
    private bool _isPickRoot = false;
    private bool _pickEnabled = true;
    private bool _layoutVisible = true;
    private bool _renderVisible = true;
    private bool _bTextChanged = false;
    private vec2 _tileScale = new vec2(1, 1); //Scale if UiImageSizeMode is Tile
    protected Quads _contentArea = new Quads();
    private Quads _borderArea = null;
    private Box2f _b2ContentQuad = new Box2f();       // Encompassess ALL of the child quads, plus overflow.
    protected Box2f? _renderOffset = null;
    public uint _iPickId = 0;
    protected bool _bPickedThisFrame = false;
    protected bool _bPickedPreviousFrame = false;
    private long _iPickedFrameId = 0;
    protected string _strText = "";
    private string _strTextLast = "";
    protected string _name = "";
    private long _iCompiledParentClassFrameId = 0;
    private long _iCompiledClassFrameId = 0;
    private long _iCompiledInlineFrameId = 0;
    private bool _bMustRedoTextBecauseOfStyle = false;

    int _char; //debug -- for text;

    #endregion
    #region Public: Methods

    public void Hide()
    {
      _layoutVisible = _renderVisible = false;
    }
    public void Show()
    {
      _layoutVisible = _renderVisible = true;
    }

    public UiElement()
    {
      //Note: if no Texture is set, the element will not be rendered (i.e. it's a container)
      _props.SetDefault();
    }
    public UiElement(string name)
    {
      _name = name;
      _props.SetDefault();
    }
    public UiElement(UiStyle styleClass)
    {
      StyleClass = styleClass;
      _props.SetDefault();
    }
    public UiElement(string name, UiStyle styleClass)
    {
      StyleClass = styleClass;
      _name = name;
      _props.SetDefault();
    }
    public void ResetPick()
    {
      this._bPickedPreviousFrame = _bPickedThisFrame;
      this._bPickedThisFrame = false;
    }
    public void AddChild(UiElement e, int sort = c_BaseLayerSort)
    {
      //SEt child style if it is not present (e.g. cascading styles) 
      //Actually no, we should compile the style, and not actually set a new one.
      // if (this.StyleClass != null && e.StyleClass == null)
      // {
      //   e.StyleClass = this.StyleClass;
      // }
      if (e._parent != null && e._parent.TryGetTarget(out UiElement p))
      {
        p.RemoveChild(e);
      }
      if (_children == null)
      {
        _children = new MultiMap<int, UiElement>();
      }
      _children.Add(sort, e);
      e._parent = new WeakReference<UiElement>(this);
      e.SetLayoutChanged();
    }
    public bool RemoveChild(UiElement e)
    {
      if (_children != null)
      {
        foreach (var k in _children.Keys)
        {
          if (_children.Remove(k, e))
          {
            e._parent = null;
            e.SetLayoutChanged();
            return true;
          }
        }
        if (_children.Count == 0)
        {
          _children = null;
        }
      }
      //could not remove.
      return false;
    }
    public void ClearChildren(int sort = c_AllSortLayers)
    {
      _children?.Clear();
    }
    // public void IterateChildrenSafe(Func<UiElement, LambdaBool> act)
    // {
    //   //If we remove components while iterating components..
    //   for (int c = _children.Keys.Count - 1; c >= 0; c--)
    //   {
    //     if (c < _children.Keys.Count)
    //     {
    //       var list = _children[c];
    //       if (list != null)
    //       {
    //         for (int li = list.Count - 1; li >= 0; li--)
    //         {
    //           if (li < list.Count)
    //           {
    //             if (act(list[li]) == LambdaBool.Break)
    //             {
    //               break;
    //             }
    //           }
    //         }
    //       }
    //     }
    //   }
    // }
    public void EnableDrag(Action<vec2> func)
    {
      DragEnabled = true;
      DragFunc = func;
      SetLayoutChanged();//get collected
    }
    public void DisableDrag()
    {
      DragEnabled = false;
      SetLayoutChanged();//get collected
    }
    public void ValidateQuad()
    {
      if (((_props._left + _props._width) < _props._left) || ((_props._top + _props._height) < _props._top))
      {
        Gu.Log.Error("Computed Quad is invalid, rtbl= " + (_props._left + _props._width) + "," + _props._left + "," + (_props._top + _props._height) + "," + _props._top + ".");
        Gu.DebugBreak();
      }
    }
    public T GetFirstChild<T>() where T : UiElement
    {
      Gu.Assert(_children.Count > 0);
      return (T)_children.First().Value;
    }

    #endregion
    #region Private: and Protected: Methods

    public void SetLayoutChanged()
    {
      if (LayoutChanged == false)
      {
        LayoutChanged = true;
        if (_parent != null)
        {
          if (_parent.TryGetTarget(out var par))
          {
            par.SetLayoutChanged();
          }
        }
        //Unfortunately layout changes must take place in siblings and children as well.
        //Basically the entire UI if a STATIC element changes.
        //If a child has size:expand, layout changes won't take place for just parents.
        //However, relative elements (specifically x or y) would not I think.
        if (_children != null)
        {
          foreach (var c in _children)
          {
            c.Value.SetLayoutChanged();
          }
        }
      }

    }
    private bool IsFullyClipped(Box2f b2ClipRect)
    {
      Box2f boundsSS = GetScreenSpaceClipQuad();
      // This simple test saves us a ton of GPU pixel tests
      if (boundsSS._max.x < b2ClipRect._min.x)
      {
        return true;
      }
      if (boundsSS._max.y < b2ClipRect._min.y)
      {
        return true;
      }
      if (boundsSS._min.x > b2ClipRect._max.x)
      {
        return true;
      }
      if (boundsSS._min.y > b2ClipRect._max.y)
      {
        return true;
      }
      return false;
    }
    protected virtual void RegenMesh(List<v_v4v4v4v2u2v4v4> verts, MtTex defaultPixel, Box2f b2ClipRect, uint rootPickId, UiDebugDraw dd)
    {
      if (LayoutVisible)
      {
        if (RenderVisible)
        {
          uint pickId = rootPickId;
          if (IsPickRoot && PickEnabled)
          {
            pickId = _iPickId;
          }

          GetOpenGLQuadVerts(verts, b2ClipRect, defaultPixel, pickId, dd);
          var savedcolor = dd.OverlayColor;
          var t = dd.OverlayColor.x; //flipflop color for sub-elements
          dd.OverlayColor.x = dd.OverlayColor.y;
          dd.OverlayColor.y = dd.OverlayColor.z;
          dd.OverlayColor.z = t;
          if (_children != null)
          {
            foreach (var p in _children)
            {
              UiElement ele = p.Value;

              if (ele.IsFullyClipped(b2ClipRect) == false)
              {
                Box2f clip = ShrinkClipRect(b2ClipRect);
                ele.RegenMesh(verts, defaultPixel, clip, pickId, dd);
              }
            }
          }
          dd.OverlayColor = savedcolor;
        }
      }
    }
    private Box2f ShrinkClipRect(Box2f b2ClipRect)
    {
      // Hide all children that go beyond this container.
      // Must be called in the loop so we reset it with every child.
      Box2f b = GetScreenSpaceClipQuad();
      Box2f ret = b2ClipRect;
      if (_props._overflowMode == UiOverflowMode.Hide)
      {
        // Shrink the box
        if (b._min.x > ret._min.x)
        {
          ret._min.x = b._min.x;
        }
        if (b._min.y > ret._min.y)
        {
          ret._min.y = b._min.y;
        }
        if (b._max.x < ret._max.x)
        {
          ret._max.x = b._max.x;
        }
        if (b._max.y < ret._max.y)
        {
          ret._max.y = b._max.y;
        }
        // Make sure it's valid
        if (ret._min.x > ret._max.x)
        {
          ret._min.x = ret._max.x;
        }
        if (ret._max.y < ret._min.y)
        {
          ret._max.y = ret._min.y;
        }
      }
      return ret;
    }
    private void ComputeVertexTexcoord(ref v_v4v4v4v2u2v4v4 vc, MtTex pTex, UiImageTiling xtile, UiImageTiling ytile)
    {
      Box2f q2Tex = new Box2f();

      if (xtile == UiImageTiling.Expand)
      {
        q2Tex._min.x = pTex.uv0.x;
        q2Tex._max.x = pTex.uv1.x;
      }
      else if (xtile == UiImageTiling.Tile)
      {
        float wPx = _props._width.Value;
        q2Tex._min.x = pTex.uv0.x;
        q2Tex._max.x = pTex.uv1.x + (pTex.uv1.x - pTex.uv0.x) * _tileScale.x;
      }
      else if (xtile == UiImageTiling.Computed)
      {
        // Tex coords are computed (used for UiGlyph)
      }
      else
      {
        Gu.Log.Error("Invalid layout X image size mode.");
      }

      if (ytile == UiImageTiling.Expand)
      {
        q2Tex._min.y = pTex.uv0.y;
        q2Tex._max.y = pTex.uv1.y;
      }
      else if (ytile == UiImageTiling.Tile)
      {
        float hPx = _props._height.Value;
        q2Tex._min.y = pTex.uv0.y;
        q2Tex._max.y = pTex.uv1.y + (pTex.uv1.y - pTex.uv0.y) * _tileScale.y;
      }
      else if (ytile == UiImageTiling.Computed)
      {
        // Tex coords are computed (used for UiGlyph)
      }
      else if (ytile == UiImageTiling.Proportion)
      {
        // proportion the Y to the X
        q2Tex._min.y = pTex.uv1.y;
        float fw = q2Tex._max.x - q2Tex._min.x;
        float fr = pTex.GetSizeRatio();
        float fh = fw * fr;
        q2Tex._max.y = q2Tex._min.y + fh;
      }
      else
      {
        Gu.Log.Error("Invalid layout size mode.");
      }

      vc._tex.x = q2Tex._min.x;  // GL - bottom left
      vc._tex.y = q2Tex._min.y;
      vc._tex.z = q2Tex._max.x;  // GL - top right *this essentially flips it upside down
      vc._tex.w = q2Tex._max.y;
      vc._texsiz.x = Math.Abs(pTex.uv1.x - pTex.uv0.x);
      vc._texsiz.y = Math.Abs(pTex.uv1.y - pTex.uv0.y);  // Uv0 - uv1 - because we flipped coords bove

      //**Texture Adjust - modulating repeated textures causes seaming issues, especially with texture filtering
      // adjust the texture coordinates by some pixels to account for that.  0.5f seems to work well.
      float pixAdjust = 0.0f;  // # of pixels to adjust texture by
      float w1px = 0;                  // 1 pixel subtract from the u/v to prevent creases during texture modulation
      float h1px = 0;

      if (pTex.GetWidth() > 0 && vc._texsiz.x > 0)
      {
        w1px = 1.0f / pTex.GetWidth();
        w1px *= vc._texsiz.x;
        w1px *= pixAdjust;
      }
      if (pTex.GetHeight() > 0 && vc._texsiz.y > 0)
      {
        h1px = 1.0f / pTex.GetHeight();
        h1px *= vc._texsiz.y;
        h1px *= pixAdjust;
      }
      vc._texsiz.x -= w1px * 2.0f;
      vc._texsiz.y -= h1px * 2.0f;
      vc._tex.x += w1px;
      vc._tex.y += h1px;
      vc._tex.z -= w1px;
      vc._tex.w -= h1px;
    }
    private void SetVertexRasterArea(ref v_v4v4v4v2u2v4v4 vc, in Box2f rasterQuad, in Box2f b2ClipRect, UiDebugDraw dd)
    {
      //BL = min TR = max
      vc._rect.x = rasterQuad._min.x;
      vc._rect.y = rasterQuad._min.y;
      vc._rect.z = rasterQuad._max.x;
      vc._rect.w = rasterQuad._max.y;

      // Clip Rect.  For discard
      if (dd.DisableClip)
      {
        //We are only flipping Y in the shader now
        vc._clip.x = -999999;
        vc._clip.y = -999999;
        vc._clip.z = 999999;
        vc._clip.w = 999999;
      }
      else
      {
        vc._clip.x = b2ClipRect._min.x;
        vc._clip.y = b2ClipRect._min.y;
        vc._clip.z = b2ClipRect._max.x;
        vc._clip.w = b2ClipRect._max.y;
      }

    }
    private void GetOpenGLQuadVerts(List<v_v4v4v4v2u2v4v4> verts, Box2f b2ClipRect, MtTex defaultPixel, uint rootPickId, UiDebugDraw dd)
    {
      if (LayoutVisible == false)
      {
        return;
      }
      if (_props._texture.Value == null)
      {
        //invisible, or container element
        return;
      }

      if (_borderArea != null)
      {
        v_v4v4v4v2u2v4v4 vb = new v_v4v4v4v2u2v4v4();
        vb._rtl_rtr = new vec4(_borderArea._rtl, _borderArea._rtr);
        vb._rbr_rbl = new vec4(_borderArea._rbr, _borderArea._rbl);
        SetVertexRasterArea(ref vb, in _borderArea._b2RasterQuad, in b2ClipRect, dd);
        ComputeVertexTexcoord(ref vb, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand);
        SetVertexPickAndColor(ref vb, _props._borderColor.Value, rootPickId);
        verts.Add(vb);
      }
      {
        v_v4v4v4v2u2v4v4 vc = new v_v4v4v4v2u2v4v4();
        vc._rtl_rtr = new vec4(_contentArea._rtl, _contentArea._rtr);
        vc._rbr_rbl = new vec4(_contentArea._rbr, _contentArea._rbl);
        SetVertexRasterArea(ref vc, in _contentArea._b2RasterQuad, in b2ClipRect, dd);
        ComputeVertexTexcoord(ref vc, _props._texture.Value, _props._imageTilingX.Value, _props._imageTilingY.Value);
        SetVertexPickAndColor(ref vc, _props._color.Value, rootPickId);
        verts.Add(vc);
      }
      if (dd.ShowOverlay)
      {
        //overlay colored quad.

        v_v4v4v4v2u2v4v4 dbgv;
        if (_renderOffset != null)
        {
          //Draw the area pre-offset.
          dbgv = new v_v4v4v4v2u2v4v4();
          if (_borderArea != null)
          {
            SetVertexRasterArea(ref dbgv, in _borderArea._b2PreOffsetRasterQuad, in b2ClipRect, dd);
          }
          else
          {
            SetVertexRasterArea(ref dbgv, in _contentArea._b2PreOffsetRasterQuad, in b2ClipRect, dd);
          }
          dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
          dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
          ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand);
          SetVertexPickAndColor(ref dbgv, dd.OverlayColor, rootPickId);
          verts.Add(dbgv);
        }

        dbgv = new v_v4v4v4v2u2v4v4();
        if (_borderArea != null)
        {
          SetVertexRasterArea(ref dbgv, in _borderArea._b2RasterQuad, in b2ClipRect, dd);
        }
        else
        {
          SetVertexRasterArea(ref dbgv, in _contentArea._b2RasterQuad, in b2ClipRect, dd);
        }
        dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
        dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
        ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand);
        SetVertexPickAndColor(ref dbgv, dd.OverlayColor, rootPickId);
        verts.Add(dbgv);

      }
    }
    private void SetVertexPickAndColor(ref v_v4v4v4v2u2v4v4 vc, vec4 color, uint rootPickId)
    {
      vc._pick_color = new uvec2(
         //Since we can't discard fragments for the pick buffer, assume the pick id of the parent pick root
         rootPickId,
         ((uint)(color.x * 255.0f) << 24) |
          ((uint)(color.y * 255.0f) << 16) |
          ((uint)(color.z * 255.0f) << 8) |
          ((uint)(color.w * 255.0f) << 0)
      );
    }
    private Box2f GetPickableQuad()
    {
      if (_borderArea != null)
      {
        return _borderArea._b2LayoutQuad;
      }
      return _contentArea._b2LayoutQuad;
    }
    protected virtual bool Pick(PCMouse mous, long frameStamp)
    {
      //Recursive method to pick visible elements.
      //Of course..quadtree but that's just.. not needed
      Box2f q = GetPickableQuad();
      if (LayoutVisible == true && RenderVisible == true && PickEnabled == true)
      {
        if (q.ContainsPointInclusive(mous.Pos))
        {
          if (IsPickRoot)
          {
            //Pick root means we don't pick any children deeper than this element.
            var pixid = Gu.Context.Renderer.Picker.GetSelectedPixelId();
            if (pixid != Picker.c_iInvalidPickId)
            {
              if (pixid == _iPickId)
              {
                _iPickedFrameId = frameStamp;
                _bPickedThisFrame = true;
                Gu.Context.Renderer.Picker.PickedObjectFrame = this;
              }
            }
          }
          else if (_children != null)
          {
            foreach (var ele in _children)
            {
              if (ele.Value.Pick(mous, frameStamp))
              {
                //This is ok to skip other children, reason being, we use a pixel-precise pick buffer, not just rectangle.
                break;
              }
            }
          }
        }
      }
      return _bPickedThisFrame;
    }
    public void DoMouseEvents(PCMouse mouse, bool iswindow = false)
    {
      if (_bPickedThisFrame || iswindow)
      {
        if (_bPickedPreviousFrame || iswindow)
        {
          DoMouseEvent(mouse, UiEventId.Mouse_Move);
        }
        else
        {
          DoMouseEvent(mouse, UiEventId.Mouse_Enter);
        }

        ButtonState eLmb = mouse.GetButtonState(MouseButton.Left);
        ButtonState eRmb = mouse.GetButtonState(MouseButton.Right);
        ButtonState eMmb = mouse.GetButtonState(MouseButton.Middle);

        DoMouseButtonEvent(mouse, UiEventId.Mouse_Lmb_Up, eLmb, ButtonState.Up);
        DoMouseButtonEvent(mouse, UiEventId.Mouse_Lmb_Hold, eLmb, ButtonState.Hold);
        DoMouseButtonEvent(mouse, UiEventId.Mouse_Lmb_Press, eLmb, ButtonState.Press);
        DoMouseButtonEvent(mouse, UiEventId.Mouse_Lmb_Release, eLmb, ButtonState.Release);

        DoMouseButtonEvent(mouse, UiEventId.Mouse_Rmb_Up, eRmb, ButtonState.Up);
        DoMouseButtonEvent(mouse, UiEventId.Mouse_Rmb_Hold, eRmb, ButtonState.Hold);
        DoMouseButtonEvent(mouse, UiEventId.Mouse_Rmb_Press, eRmb, ButtonState.Press);
        DoMouseButtonEvent(mouse, UiEventId.Mouse_Rmb_Release, eRmb, ButtonState.Release);
      }
      else if (_bPickedPreviousFrame)
      {
        DoMouseEvent(mouse, UiEventId.Mouse_Leave);
        _bPickedPreviousFrame = false;
      }
    }
    private void DoMouseButtonEvent(PCMouse m, UiEventId evid, ButtonState curstate, ButtonState evstate)
    {
      if (curstate == evstate)
      {
        DoMouseEvent(m, evid);
      }
    }
    private void DoMouseEvent(PCMouse m, UiEventId evid)
    {
      if (_props._events != null)
      {
        if (_props._events.Value.TryGetValue(evid, out var actions))
        {
          foreach (var act in actions)
          {
            act.Value(evid, this, m);
          }
        }
      }
    }
    protected virtual void PerformLayout_SizeElements(MegaTex mt, bool bForce, vec2 parentMaxWH, UiStyle st)
    {
      //Build the UI depth-first. Children elements are sized. Then we got hrough again and position them from the top.
      if (LayoutChanged || bForce)
      {
        ComputeStyle(st);
        UpdateBorder();

        //in HTML all elements default to zero width without any contents.
        vec2 contentWH = new vec2(_props._padLeft.Value + _props._padRight.Value, _props._padTop.Value + _props._padBot.Value);

        vec2 maxWH = new vec2(
          Math.Max(Math.Min(parentMaxWH.x, _props._maxWHPX.Value.x), 0),
          Math.Max(Math.Min(parentMaxWH.y, _props._maxWHPX.Value.y), 0)
        );

        //textchanged .. font color .. font size .. etc.
        if (_bTextChanged || _bMustRedoTextBecauseOfStyle)
        {
          CreateGlyphs(mt, _bTextChanged && !_bMustRedoTextBecauseOfStyle);
          _bMustRedoTextBecauseOfStyle = false;
          _bTextChanged = false;
        }

        //Do Children.
        if (_children != null)
        {
          foreach (var p in _children)
          {
            UiElement ele = p.Value;
            if (ele.LayoutVisible)
            {
              ele.PerformLayout_SizeElements(mt, bForce, maxWH, StyleClass);
            }
          }

          LayoutChildren(bForce, maxWH, ref contentWH);
        }

        SizeElement(maxWH, contentWH);
      }
    }
    protected void PerformLayout_PositionElements(bool bForce)
    {
      //Position elements after size calculated
      if (LayoutChanged || bForce)
      {
        if (_children != null)
        {
          foreach (var p in _children)
          {
            UiElement ele = p.Value;
            if (ele.LayoutVisible)
            {
              LayoutEleQuads(ele);
              ele.PerformLayout_PositionElements(bForce);
            }
          }
        }
        LayoutChanged = false;
      }
    }
    private void ComputeStyle(UiStyle parentStyle)
    {
      //Compute Style, update "_props"
      // 1 apply parent's compiled style, to the child's style (if present)
      // 2 compile style and sub-classes (if present)
      // 3 apply child's inline style. (if present)
      // --> _props

      //Assu;ming: parent's class is already compiled since we go top->down
      //** Parent style thing is messing stuff up **
      //** Parent style thing is messing stuff up **
      //** Parent style thing is messing stuff up **
      // UiScreen for example would end up with a textured style. 
      if (parentStyle != null)
      {
        if (parentStyle.ChangedFrameId >= _iCompiledParentClassFrameId)
        {
          _props.ApplySubclass(parentStyle.Compiled);
          _iCompiledParentClassFrameId = Gu.Context.FrameStamp;
          _bMustRedoTextBecauseOfStyle = true;
        }
      }
      if (_styleClass != null)
      {
        _styleClass.Compile();
        if (_styleClass.ChangedFrameId >= _iCompiledClassFrameId)
        {
          _props.ApplySubclass(_styleClass.Compiled);
          _iCompiledClassFrameId = Gu.Context.FrameStamp;
          _bMustRedoTextBecauseOfStyle = true;
        }
      }
      if (_inlineStyle != null)
      {
        _inlineStyle.Compile();
        if (_inlineStyle.ChangedFrameId >= _iCompiledInlineFrameId)
        {
          _props.ApplySubclass(_inlineStyle.Compiled);
          _iCompiledInlineFrameId = Gu.Context.FrameStamp;
          _bMustRedoTextBecauseOfStyle = true;
        }
      }
    }
    private void UpdateBorder()
    {
      //Check if border changed
      if (_borderArea == null)
      {
        if (_props._borderTop > 0 || _props._borderRight > 0 || _props._borderBot > 0 || _props._borderLeft > 0)
        {
          _borderArea = new Quads();
        }
      }
      else
      {
        if (_props._borderTop == 0 && _props._borderRight == 0 && _props._borderBot == 0 && _props._borderLeft == 0)
        {
          _borderArea = null;
        }
      }
    }
    private void SizeElement(vec2 maxWH, vec2 contentWH)
    {
      //Shrink the element (if not UiScreen) This is how HTML works by default.
      //Also apply min/max to the element. This is specifically how you would fix an element's size.
      //Note: content quad may expand beyond container
      _b2ContentQuad._min = new vec2(_props._left.Value, _props._top.Value);
      _b2ContentQuad._max = _b2ContentQuad._min + contentWH;

      if (_props._sizeModeWidth == UiSizeMode.Shrink)
      {
        _props._width = Math.Min(Math.Max(_props._minWHPX.Value.x, Math.Min(maxWH.x, contentWH.x)), _props._maxWHPX.Value.x);
      }
      else if (_props._sizeModeWidth == UiSizeMode.Expand)
      {
        if (this._parent != null && this._parent.TryGetTarget(out var par))
        {
          _props._width = par._props._width;// - _props._left;
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      if (_props._sizeModeHeight == UiSizeMode.Shrink)
      {
        _props._height = Math.Min(Math.Max(_props._minWHPX.Value.y, Math.Min(maxWH.y, contentWH.y)), _props._maxWHPX.Value.y);
      }
      else if (_props._sizeModeHeight == UiSizeMode.Expand)
      {
        if (this._parent != null && this._parent.TryGetTarget(out var par))
        {
          _props._height = par._props._height;// - _props._top;
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      if (_props._width < 0 || _props._height < 0)
      {
        int n = 0;
        n++;
      }

    }
    protected void LayoutChildren(bool bForce, vec2 maxWH, ref vec2 contentWH)
    {
      //Layout each layer specified by the key in the Children multimap.
      //Each new layer will be a new layout set (bucket).
      //Layout static elements sequentially left to right,
      //or, layout relative elements for absolute position.
      if (_children != null && _children.Count > 0)
      {
        int uiLast = int.MaxValue;
        List<UiElement> bucket = new List<UiElement>();
        foreach (var p in _children)
        {
          var ele = p.Value;

          if (ele.LayoutVisible)
          {

            if (ele._props._positionMode == UiPositionMode.Static)
            {
              // Static elements - Have a Flow, and computed position
              if (p.Key != uiLast)
              {
                uiLast = p.Key;
                if (bucket.Count > 0)
                {
                  LayoutLayer(bucket, maxWH, ref contentWH);
                  bucket.Clear();
                }
              }
              bucket.Add(p.Value);
            }
            else if (ele._props._positionMode == UiPositionMode.Relative ||
            ele._props._positionMode == UiPositionMode.RelativeConstrainX ||
            ele._props._positionMode == UiPositionMode.RelativeConstrainY ||
            ele._props._positionMode == UiPositionMode.RelativeConstrainXY)
            {
              // Fixed elements relative to container
              ComputePositionalElement(ele,
              ele._props._positionMode == UiPositionMode.RelativeConstrainX || ele._props._positionMode == UiPositionMode.RelativeConstrainXY,
              ele._props._positionMode == UiPositionMode.RelativeConstrainY || ele._props._positionMode == UiPositionMode.RelativeConstrainXY);
            }
            else
            {
              Gu.BRThrowNotImplementedException();
            }
            //Absolute - relative to entire document.
          }
        }
        //Position final layer
        if (bucket.Count > 0)
        {
          LayoutLayer(bucket, maxWH, ref contentWH);
          bucket.Clear();
        }
      }
    }
    private void LayoutLayer(List<UiElement> stats, vec2 maxWH, ref vec2 contentWH)
    {
      // Calc width with all static blocks using 0 width for autos (expandable blocks).
      List<UiLine> vecLines = new List<UiLine>();
      vecLines.Add(new UiLine(_props._padLeft.Value, _props._padTop.Value));
      foreach (var ele in stats)
      {
        CalcStaticElement(ele, vecLines, 0.0f, 0.0f, maxWH);
      }
      float totalHeight = _props._padTop.Value + _props._padBot.Value;
      foreach (var line in vecLines)
      {
        totalHeight += line._height;
        contentWH.x = Math.Max(contentWH.x, line._width + _props._padLeft.Value + _props._padRight.Value);
      }
      contentWH.y = Math.Max(contentWH.y, totalHeight);
    }
    private void CalcStaticElement(UiElement ele, List<UiLine> vecLines, float fAutoWidth, float fAutoHeight, vec2 parentMaxWH)
    {
      /*
         .. | margin | border | padding | ** | pad | bor | mar| .. 
      */
      if (vecLines.Count == 0)
      {
        Gu.BRThrowException("GUI error - tried to run calc algorithm without any UILines created");
      }
      UiLine line = vecLines[vecLines.Count - 1];

      float parent_contentarea_width = parentMaxWH.x - _props._padLeft.Value - _props._padRight.Value;

      //*Padding
      float mt = ele._props._marTop.Value;
      float mr = ele._props._marRight.Value;
      float mb = ele._props._marBot.Value;
      float ml = ele._props._marLeft.Value;

      float bt = ele._props._borderTop.Value;
      float br = ele._props._borderRight.Value;
      float bb = ele._props._borderBot.Value;
      float bl = ele._props._borderLeft.Value;

      float ele_width = Math.Max(ele._props._width.Value, ele._props._minWHPX.Value.x);

      //**Line break
      bool bLineBreak = false;
      if (ele._props._displayMode == UiDisplayMode.Inline)
      {
        if (ml + mr + bl + br + ele_width + line._width > parent_contentarea_width) //For label - auto width + expand. ?? 
        {
          bLineBreak = true;
        }
      }
      else if (ele._props._displayMode == UiDisplayMode.Block)
      {
        //For /n in text. or block elements
        bLineBreak = true;
      }
      else if (ele._props._displayMode != UiDisplayMode.InlineNoWrap)
      {
        bLineBreak = false;
      }

      if (bLineBreak)
      {
        // new line
        UiLine line2 = new UiLine(_props._padLeft.Value, 0/*pad top, only for the top uiline*/);
        line2._top = line._top + line._height;
        vecLines.Add(line2);
        line = vecLines[vecLines.Count - 1];
      }

      line._width += ml;
      line._width += bl;
      ele._props._left = line._left + line._width;
      ele._props._top = line._top + mt + bt;
      line._width += ele_width;
      line._width += br;
      line._width += mr;

      ele.ValidateQuad();

      // Increse line height WITH PAD
      line._height = Math.Max(line._height, Math.Max(ele._props._height.Value + mt + mb + bt + bb, ele._props._minWHPX.Value.y));

      line._eles.Add(ele);
    }
    private void ComputePositionalElement(UiElement ele, bool constrainX, bool constrainY)
    {
      if (constrainX)
      {
        if (ele._props._right > _props._width)
        {
          ele._props._left = _props._width - ele._props._width;
        }
        if (ele._props._left < 0)
        {
          ele._props._left = 0;
        }
      }
      if (constrainY)
      {
        if (ele._props._bottom > _props._height)
        {
          ele._props._top = _props._height - ele._props._height;
        }
        if (ele._props._top < 0)
        {
          ele._props._top = 0;
        }
      }

      ValidateQuad();
    }
    private void LayoutEleQuads(UiElement ele)
    {
      //Add the child to the parent.
      float t, r, b, l;
      float bt, br, bb, bl;
      float rtl, rtr, rbr, rbl;
      t = _contentArea._b2ComputedQuad._min.y + ele._props._top.Value;
      r = _contentArea._b2ComputedQuad._min.x + ele._props._right.Value;
      b = _contentArea._b2ComputedQuad._min.y + ele._props._bottom.Value;
      l = _contentArea._b2ComputedQuad._min.x + ele._props._left.Value;
      bt = ele._props._borderTop.Value;
      br = ele._props._borderRight.Value;
      bb = ele._props._borderBot.Value;
      bl = ele._props._borderLeft.Value;
      rtl = ele._props._borderTopLeftRadius.Value;
      rtr = ele._props._borderTopRightRadius.Value;
      rbr = ele._props._borderBotRightRadius.Value;
      rbl = ele._props._borderBotLeftRadius.Value;

      ComputeQuads(t + bt, r - br, b - bb, l + bl, rtl, rtr, rbr, rbl, ele._renderOffset, ele._contentArea);

      if (ele._borderArea != null)
      {
        ComputeQuads(t, r, b, l, rtl, rtr, rbr, rbl, ele._renderOffset, ele._borderArea);
      }
    }
    protected static void ComputeQuads(float top, float right, float bot, float left, float rtl, float rtr, float rbr, float rbl, Box2f? offset, Quads quads)
    {
      // Layout Quad (for picking, debug)/*  */
      float w1 = 1.0f, h1 = 1.0f;
      w1 = 1;//UiScreen::getDesignMultiplierW();
      h1 = 1;//UiScreen::getDesignMultiplierH();

      if (left > right)
      {
        Gu.DebugBreak();
        left = right;
      }
      if (top > bot)
      {
        Gu.DebugBreak();
        top = bot;
      }

      quads._b2ComputedQuad._min.y = top;
      quads._b2ComputedQuad._max.x = right;
      quads._b2ComputedQuad._max.y = bot;
      quads._b2ComputedQuad._min.x = left;

      if (offset != null)
      {
        //For glyphs, and other elements that go outside their physical regions
        var origin = quads._b2ComputedQuad.Center();
        var ro = offset.Value;
        quads._b2OffsetQuad._min.x = origin.x + ro.Left - quads._b2ComputedQuad.Width() / 2;
        quads._b2OffsetQuad._min.y = origin.y + ro.Top + quads._b2ComputedQuad.Height() / 4;
        quads._b2OffsetQuad._max.x = origin.x + ro.Right - quads._b2ComputedQuad.Width() / 2;
        quads._b2OffsetQuad._max.y = origin.y + ro.Bottom + quads._b2ComputedQuad.Height() / 4;
      }
      else
      {
        quads._b2OffsetQuad = quads._b2ComputedQuad;
      }

      // Set to false if we're controllig coordinates of this element (cursor, or window position)
      quads._b2LayoutQuad._min.x = quads._b2OffsetQuad._min.x * w1;
      quads._b2LayoutQuad._min.y = quads._b2OffsetQuad._min.y * h1;
      quads._b2LayoutQuad._max.x = quads._b2OffsetQuad._max.x * w1;
      quads._b2LayoutQuad._max.y = quads._b2OffsetQuad._max.y * h1;

      ComputeRasterQuad(quads._b2LayoutQuad, ref quads._b2RasterQuad);
      if (offset != null)
      {
        ComputeRasterQuad(quads._b2ComputedQuad, ref quads._b2PreOffsetRasterQuad);
      }
      quads._rtl = rtl;// new vec2(rtl / (float)viewport_wh.x * 0.5f, rtl / (float)viewport_wh.y * 0.5f);
      quads._rtr = rtr;// new vec2(rtr / (float)viewport_wh.x * 0.5f, rtr / (float)viewport_wh.y * 0.5f);
      quads._rbr = rbr;// new vec2(rbr / (float)viewport_wh.x * 0.5f, rbr / (float)viewport_wh.y * 0.5f);
      quads._rbl = rbl;// new vec2(rbl / (float)viewport_wh.x * 0.5f, rbl / (float)viewport_wh.y * 0.5f);
    }
    private static void ComputeRasterQuad(Box2f _b2LayoutQuad, ref Box2f _b2RasterQuad)
    {
      //TODO: remove raster, use layout
      _b2RasterQuad = _b2LayoutQuad;
    }
    private void CreateGlyphs(MegaTex mt, bool replaceChangedGlyphs)
    {
      //textOnly If just text changed then just create new chars
      //style - if style changed we must redo everything.
      if (_children == null)
      {
        _children = new MultiMap<int, UiElement>();
      }

      //Get the font if it isn't already got.
      MtFontLoader font = null;
      if (_cachedFont == null || mt.GetFont(_props._fontFace.Value) != _cachedFont)
      {
        font = mt.GetFont(_props._fontFace.Value);
      }
      else
      {
        font = _cachedFont;
      }
      if (font == null)
      {
        Gu.Log.ErrorCycle("Font loader could not be found for " + _props._fontFace.Value.QualifiedPath + " font possibly loaded with error", 500);
        return;
      }

      float fontHeight = _props._fontSize.Value;
      var patch = font.SelectFontPatchInfo(fontHeight);
      if (patch == null)
      {
        return;
      }
      //replaceChangedGlyphs=false;
      int[] diff = null;
      if (replaceChangedGlyphs)
      {
        //Try to "smart" replace only changed text. 
        var glyphs = _children.ItemsAt(c_GlyphLayerSort);
        if (glyphs == null)
        {
          replaceChangedGlyphs = false;
        }
        else if (_strTextLast.Length != glyphs.Count)
        {
          //Something messed up between text/glyphs, redo everything, technically this should never happen, hence debugberak
          replaceChangedGlyphs = false;
          _children.Remove(c_GlyphLayerSort);
          Gu.DebugBreak();
        }
        else
        {
          int ilast = 0;
          int icur = 0;
          List<UiElement> newChildren = new List<UiElement>();
          diff = StringUtil.SlidingDiff(_strTextLast, _strText, 16);
          int debug_numcreated = 0;
          for (int di = 0; di < diff.Length; di += 2)
          {
            int ct = diff[di + 1];
            int code = diff[di + 0];
            if (code == 0) // no change 
            {
              for (int cti = 0; cti < ct; cti++)
              {
                var e = glyphs[ilast + cti];
                e.LayoutChanged = true;
                newChildren.Add(e);
              }
              ilast += ct;
              icur += ct;
            }
            else if (code == 1)//add new
            {
              for (int cti = 0; cti < ct; cti++)
              {
                UiElement e = new UiElement();
                DoGlyph(e, icur + cti, _strText, font, patch, fontHeight);
                newChildren.Add(e);
                debug_numcreated++;
              }
              icur += ct;
            }
            else if (code == 2) //remove
            {
              ilast += ct;
            }
          }

          // //test
          // string test1="";
          // foreach(var c in newChildren){
          // test1+=(char)c._char;
          // }
          //       string test2 = "";
          //       ilast = 0;
          //       icur = 0;
          //       int nadd = 0;
          //       int nrem = 0;
          //       for (var xi = 0; xi < diff.Length; xi += 2)
          //       {
          //           int ct = diff[xi + 1];
          //         if (diff[xi + 0] == 0)
          //         {
          //           test2 += _strTextLast.Substring(ilast, ct);
          //           ilast += ct;
          //           icur += ct;
          //         }
          //         else if (diff[xi + 0] == 1)
          //         {
          //           test2 += _strText.Substring(icur, ct);
          //           icur += ct;
          //           nadd += ct;
          //         }
          //         else if (diff[xi + 0] == 2)
          //         {
          //           ilast += ct;
          //           nrem += ct;
          //         }
          //       }
          //       bool didWork = StringUtil.Equals(_strText, test2);
          //       bool didWork2 = StringUtil.Equals(_strText, test1);


          _children.SetValueList(c_GlyphLayerSort, newChildren);
        }
      }
      else
      {
        _children.Remove(c_GlyphLayerSort);
        int debug_redocount = 0;
        for (int ci = 0; ci < _strText.Length; ci++)
        {
          UiElement e = new UiElement();

          DoGlyph(e, ci, _strText, font, patch, fontHeight);

          _children.Add(c_GlyphLayerSort, e);

          debug_redocount++;
        }
      }
    }
    private void DoGlyph(UiElement e, int index, string text, MtFontLoader font, MtFontPatchInfo patch, float fontHeight)
    {
      int cc = _strText[index];
      int ccNext = (index + 1) < _strText.Length ? _strText[index + 1] : 0;
      float adv = font.GetKernAdvanceWidth(patch, _props._fontSize.Value, cc, ccNext);
      if (adv != 0)
      {
        int n = 0;
        n++;
      }

      MtCachedCharData ccd = new MtCachedCharData();
      patch.GetChar(cc, fontHeight, out ccd);

      //TODO: this should be UiElementBase, for simplicity. UiElement is too huge.

      e._pickEnabled = false;
      e._renderOffset = new Box2f(new vec2(ccd.left, ccd.top), new vec2(ccd.right, ccd.bot));
      e._props.SetDefault();
      e._props._texture = new UiRef<MtTex>(new MtTex());
      e._props._texture.Value.SetWH(patch.TextureWidth, patch.TextureHeight);
      e._props._texture.Value.uv0 = ccd.uv0;
      e._props._texture.Value.uv1 = ccd.uv1;
      e._props._left = 0;
      e._props._top = 0;
      e._props._minWHPX = new vec2(ccd.width, ccd.height * _props._lineHeight.Value);
      e._props._marRight = ccd.marginRight + adv;
      e._props._positionMode = UiPositionMode.Static;
      e._props._sizeModeHeight = UiSizeMode.Shrink;
      e._props._sizeModeWidth = UiSizeMode.Shrink;
      e._char = cc;
      if (cc == '\n')
      {
        e._props._displayMode = UiDisplayMode.Block;
      }
      else
      {
        e._props._displayMode = UiDisplayMode.Inline;
      }
      e._props._color = _props._fontColor;

      e.ValidateQuad();
    }
    protected Box2f GetScreenSpaceClipQuad()
    {
      if (_borderArea != null)
      {
        return _borderArea._b2RasterQuad;
      }
      return _contentArea._b2RasterQuad;
    }

    #endregion

  }//UiElement
  public class UiGridRow : UiElement
  {
  }
  public class UiGrid : UiElement
  {
    public void AddRows(int nr, int nc)
    {
    }
  }
  public class UiMenuItem : UiButtonBase
  {
    public UiMenuItem(string name, string text, Action<UiEventId, UiElement, PCMouse> onClick = null, UiStyle styleClass = null) : base(name, text, null, styleClass)
    {
      if (onClick != null)
      {
        this.InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Release, onClick);
      }
    }
    public UiMenuItem AddMenuItem(UiMenuItem item)
    {
      AddChild(item);
      return this;
    }
    public UiMenuItem AddMenuItems(List<UiMenuItem> items)
    {
      foreach (var item in items)
      {
        AddMenuItem(item);
      }
      return this;
    }
  }
  public class UiToolbar : UiElement
  {
    public UiToolbar(string name, vec2? pos, vec2? wh, UiStyle style = null) : base(name, style)
    {
      this.InlineStyle.Texture = Gui2d.SolidColorTexture;
      this.InlineStyle.MinWHPX = new vec2(0, 25);
      this.InlineStyle.SizeModeWidth = UiSizeMode.Expand;
      this.InlineStyle.SizeModeHeight = UiSizeMode.Shrink;
      this.InlineStyle.Color = vec4.rgba_ub(220, 220, 230, 100);
      this.InlineStyle.Padding = 5;
      if (pos != null)
      {
        this.InlineStyle.Pos = pos.Value;
        this.InlineStyle.PositionMode = UiPositionMode.Relative;
      }
      if (wh != null)
      {
        this.InlineStyle.Extent = wh.Value;
      }
    }
  }
  public class UiLabel : UiElement
  {
    public UiLabel(string name, vec2? pos, string text, bool showbackground = true, FontFace? font = null, float fontSize = 12, vec4? fontColor = null, UiFontStyle fontstyle = UiFontStyle.Normal, float lineheight = 1.0f)
    {
      if (pos != null)
      {
        this.InlineStyle.Pos = pos.Value;
        this.InlineStyle.PositionMode = UiPositionMode.Relative;
      }
      this.Text = text;
      if (showbackground)
      {
        this.InlineStyle.Texture = Gui2d.SolidColorTexture;
      }
      this.InlineStyle.FontFace = new UiRef<FontFace>(font != null ? font : FontFace.RobotoMono);
      this.InlineStyle.FontSize = fontSize;
      this.InlineStyle.FontColor = fontColor != null ? fontColor.Value : new vec4(1, 1, 1, 1);
      this.InlineStyle.FontStyle = fontstyle;
      this.InlineStyle.LineHeight = lineheight;
      this.InlineStyle.Padding = 15;
      this.InlineStyle.PadBot = 10;
      this.InlineStyle.PadTop = 10;
      this.InlineStyle.PadLeft = 10;
      this.InlineStyle.PadRight = 10;
      this.IsPickRoot = true;
    }
  }
  public class UiButtonBase : UiElement
  {
    public UiButtonBase(string name, string text, vec2? pos = null, UiStyle styleClass = null) : base(name, styleClass)
    {
      this.Text = text;
      this.IsPickRoot = true;

      if (pos != null)
      {
        this.InlineStyle.Pos = pos.Value;
        this.InlineStyle.PositionMode = UiPositionMode.Relative;
      }
      this.InlineStyle.Texture = Gui2d.SolidColorTexture;
      this.InlineStyle.DisplayMode = UiDisplayMode.Inline;
      this.InlineStyle.MaxWHPX = new vec2(100, 200);
      this.InlineStyle.Border = 0;
      this.InlineStyle.BorderRadius = 3;
      this.InlineStyle.BorderColor = vec4.rgba_ub(90, 120, 240, 255);
      this.InlineStyle.Color = vec4.rgba_ub(35 + 90, 47 + 90, 62 + 90, 255);
      this.InlineStyle.Padding = 3;
      this.InlineStyle.PadRight = 7;
      this.InlineStyle.PadLeft = 7;
      this.InlineStyle.Margin = 2;
      this.InlineStyle.FontColor = vec4.rgba_ub(255, 255, 255, 255);
      this.InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Release, (eid, uie, m) =>
      {
        this.InlineStyle.FontColor = vec4.rgba_ub(255, 255, 255, 255);
      });
      this.InlineStyle.AddEvent(UiEventId.Mouse_Enter, (eid, uie, m) =>
      {
        this.InlineStyle.Border = 2;
        this.InlineStyle.BorderColor = vec4.rgba_ub(255, 255, 255, 255);
      });
      this.InlineStyle.AddEvent(UiEventId.Mouse_Leave, (eid, uie, m) =>
      {
        this.InlineStyle.Border = 0;
      });
      this.InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Press, (eid, uie, m) =>
      {
        this.InlineStyle.FontColor = vec4.rgba_ub(200, 200, 200, 255);
      });

    }
  }
  public class UiButton : UiButtonBase
  {
    public UiButton(string name, string text, vec2? pos = null, Action<UiEventId, UiElement, PCMouse> onClick = null, UiStyle styleClass = null) : base(name, text, pos, styleClass)
    {
      if (onClick != null)
      {
        this.InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Release, onClick);
      }
    }
  }

  public class UiScrollbar : UiElement
  {
    public UiScrollbar(string name, Action<float> scrollFunc, bool horizontal = false, UiStyle styleClass = null) : base(name, styleClass)
    {
      UiElement thumb = new UiElement(name + "-thumb", styleClass);
      //thumb.InlineStyle.Texture = new UiRef<MtTex>(null);//this.DefaultPixel());
      thumb.InlineStyle.PadBot = thumb.InlineStyle.PadLeft = thumb.InlineStyle.PadTop = thumb.InlineStyle.PadRight = 15;// Fonts are messed up right now 
      thumb.InlineStyle.PadBot = 5;
      thumb.InlineStyle.PadTop = 5;
      thumb.InlineStyle.PadLeft = 5;
      thumb.InlineStyle.PadRight = 5;
      thumb.InlineStyle.MaxWHPX = new vec2(999999, 999999);
      thumb.InlineStyle.MinWHPX = new vec2(5, 5);
      thumb.InlineStyle.Border = 1;
      thumb.InlineStyle.BorderColor = vec4.rgba_ub(200, 200, 220);
      thumb.IsPickRoot = true;
      thumb.InlineStyle.PositionMode = UiPositionMode.RelativeConstrainXY;
      thumb.InlineStyle.Top = 0;
      thumb.InlineStyle.Left = 0;
      thumb.IsPickRoot = true;

      //UiElement cont = new UiElement(name, styleClass);//CreateDefaultStyledElement(name);
      // cont.Name = name;
      // cont.InlineStyle.Texture = new UiRef<MtTex>(null);//this.DefaultPixel());
      this.InlineStyle.Padding = 3;
      if (horizontal)
      {
        this.InlineStyle.MaxWHPX = new vec2(999999, 20);
        this.InlineStyle.MinWHPX = new vec2(20, 20);
        this.InlineStyle.SizeModeWidth = UiSizeMode.Expand;
      }
      else
      {
        this.InlineStyle.MaxWHPX = new vec2(20, 999999);
        this.InlineStyle.MinWHPX = new vec2(20, 20);
        this.InlineStyle.SizeModeHeight = UiSizeMode.Expand;
      }
      this.InlineStyle.Color = vec4.rgba_ub(90, 90, 90);
      this.AddChild(thumb);

      thumb.EnableDrag((v) =>
      {
        float valpct = 0;
        if (horizontal)
        {
          valpct = (float)thumb.Props._left / ((float)this.Props._width - (float)thumb.Props._width);
        }
        else
        {
          valpct = (float)thumb.Props._top / ((float)this.Props._height - (float)thumb.Props._height);
        }
        thumb.InlineStyle.Left = thumb.Props._left;
        thumb.InlineStyle.Top = thumb.Props._top;
        thumb.InlineStyle.Left += v.x;
        thumb.InlineStyle.Top += v.y;
        scrollFunc?.Invoke(valpct * (thumb.MinValue + thumb.MaxValue));
      });

    }

  }
  public class UiDropdown : UiElement
  {
    WeakReference<Gui2d> gui = null;
    public UiDropdown(Gui2d g)
    {
      gui = new WeakReference<Gui2d>(g);
    }
    public void Show(vec2 pos, Dictionary<string, string> items)
    {
      InlineStyle.Pos = pos;

      ClearChildren();
      if (gui.TryGetTarget(out var g))
      {
        Gu.BRThrowNotImplementedException();
        // foreach (var kvp in items)
        // {
        //   var item = g.CreateDefaultStyledElement("item-" + kvp.Key);
        //   item.InlineStyle.Color *= 1.5f;

        //   item.InlineStyle.PositionMode = UiPositionMode.Relative;
        //   item.InlineStyle.SizeModeWidth = UiSizeMode.Expand;
        //   item.InlineStyle.SizeModeHeight = UiSizeMode.Shrink;
        //   item.InlineStyle.MarginBot = 1;
        //   item.Text = kvp.Value;
        //   item.Tag = kvp.Key;
        //   AddChild(item);
        // }
      }
      Show();
    }
  }
  public class Gui2d : UiElement
  {
    #region Public: Members
#if DEBUG
    private v_v4v4v4v2u2v4v4[] _debug_pt = new v_v4v4v4v2u2v4v4[3]; //save 3 points to see what they are (debug)
#endif

    public static UiRef<MtTex> SolidColorTexture { get { return new UiRef<MtTex>(new MtTex()); } }

    public WeakReference<RenderView> RenderView { get; private set; } = new WeakReference<RenderView>(null);
    public UiDebugDraw DebugDraw { get; set; } = new UiDebugDraw();
    public UiDropdown ContextMenu { get; private set; } = null;
    public MeshData Mesh { get; set; } = null;
    public long UpdateMs { get; private set; } = 0;
    public long MeshMs { get; private set; } = 0;
    public long PickMs { get; private set; } = 0;
    public long ObjectEventsMs { get; private set; } = 0;
    public long WindowEventsMs { get; private set; } = 0;
    public MtTex DefaultPixel { get { return _shared.MegaTex.DefaultPixel; } }

    private UiDragInfo _dragInfo = new UiDragInfo();
    private vec2 _viewport_wh_last = new vec2(1, 1);
    private Gui2dShared _shared = null;

    #endregion
    #region Public: Methods

    public Gui2d(Gui2dShared shared, RenderView cam, string styleClass)
    {
      _shared = shared;
      RenderView = new WeakReference<RenderView>(cam);
      int designWidth = 1920;
      int designHeight = 1080;
      _props.SetDefault();
      _props._top = 0;
      _props._left = 0;
      _props._width = designWidth - 1;
      _props._height = designHeight - 1;
      _props._maxWHPX = new vec2(designWidth, designHeight);//Make sure stuff doesn't go off the screen.
      _props._minWHPX = new vec2(0, 0);
      _props._sizeModeWidth = _props._sizeModeHeight = UiSizeMode.Expand;
      this.Name = "screen(root)";
      CreateWindowEvents();

      this.StyleClass = shared.StyleSheet.GetClass(styleClass);
      this.StyleClass.Texture = null;//Null out texture.
                                     //Technically not an error, however, we use the root styleclass to
                                     //define default Texture Pixel for non-textured elements.
      Gu.Assert(this.StyleClass != null);

      ContextMenu = new UiDropdown(this);
      //CreateDefaultStyledElement(ContextMenu);
      ContextMenu.InlineStyle.PositionMode = UiPositionMode.Relative;
      ContextMenu.InlineStyle.SizeModeHeight = UiSizeMode.Expand;
      ContextMenu.InlineStyle.SizeModeWidth = UiSizeMode.Expand;
      AddChild(ContextMenu);
      ContextMenu.Hide();
    }
    public void OnResize()
    {
      SetLayoutChanged();
    }
    public void Render(RenderView rv)
    {
      //Swap out the mesh for this instance's mesh
      _shared.Dummy.Mesh = Mesh;
      DrawCall.Draw(Gu.World.WorldProps, rv, _shared.Dummy);
    }
    public void Update(double dt)//MegaTex mt, WindowContext ct, Gui2d g)
    {
      if (RenderView != null && RenderView.TryGetTarget(out var rv))
      {
        long a = Gu.Milliseconds();
        SetExtentsToViewport(rv);
        UpdateLayout(_shared.MegaTex, Gu.Context.PCMouse, rv);
        this.UpdateMs = Gu.Milliseconds() - a;

        a = Gu.Milliseconds();
        RegenMesh(rv, _shared.MegaTex);
        this.MeshMs = Gu.Milliseconds() - a;
      }
    }
    public void Pick()
    {
      var ct = Gu.Context;
      //See WorldObject->Pick
      if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
      {
        return;
      }

      //Update picked state
      var picker = Gu.Context.Renderer.Picker;
      if (picker.PickedObjectFrameLast != null)
      {
        if (picker.PickedObjectFrameLast is UiElement)
        {
          (picker.PickedObjectFrameLast as UiElement).ResetPick();
        }
      }

      //Do Pick
      Pick(ct.PCMouse, ct.FrameStamp);

      //Fire events
      long a = Gu.Milliseconds();
      if (picker.PickedObjectFrameLast != null)
      {
        if (picker.PickedObjectFrameLast is UiElement)
        {
          (picker.PickedObjectFrameLast as UiElement).DoMouseEvents(ct.PCMouse);
        }
      }
      if (picker.PickedObjectFrameLast != picker.PickedObjectFrame)
      {
        if (picker.PickedObjectFrame != null)
        {
          if (picker.PickedObjectFrame is UiElement)
          {
            (picker.PickedObjectFrame as UiElement).DoMouseEvents(ct.PCMouse);
          }
        }
      }
      this.ObjectEventsMs = Gu.Milliseconds() - a;

      //Window events
      a = Gu.Milliseconds();
      DoMouseEvents(ct.PCMouse, true);
      this.WindowEventsMs = Gu.Milliseconds() - a;
    }


    #endregion
    #region Private: Methods

    private void UpdateLayout(MegaTex mt, PCMouse mouse, RenderView rv)
    {
      if (LayoutChanged)
      {
        bool force = false;
        vec2 viewport_wh = new vec2(rv.Viewport.Width, rv.Viewport.Height);
        if ((int)viewport_wh.x != (int)_viewport_wh_last.x || (int)viewport_wh.y != (int)_viewport_wh_last.y)
        {
          force = true;
          _viewport_wh_last = viewport_wh;
        }

        // Gui2d doesn't have a parent, so we have to compute the quads to create a valid clip region.
        ComputeQuads(_props._top.Value, _props._right.Value, _props._bottom.Value, _props._left.Value,
        _props._borderTopLeftRadius.Value, _props._borderTopRightRadius.Value, _props._borderBotRightRadius.Value, _props._borderBotLeftRadius.Value,
        _renderOffset, _contentArea);

        PerformLayout_SizeElements(mt, force, this._props._maxWHPX.Value, null);
        PerformLayout_PositionElements(force);

      }
    }
    private void RegenMesh(RenderView rv, MegaTex mt)
    {
      Box2f b = GetScreenSpaceClipQuad();
      List<v_v4v4v4v2u2v4v4> verts = new List<v_v4v4v4v2u2v4v4>();

      Gu.Assert(mt.DefaultPixel != null);
      RegenMesh(verts, mt.DefaultPixel, b, 0, DebugDraw);

#if DEBUG
      if (verts.Count > 0)
        _debug_pt[0] = verts[0];
      if (verts.Count > 1)
        _debug_pt[1] = verts[1];
      if (verts.Count > 2)
        _debug_pt[2] = verts[2];
#endif

      Mesh = new MeshData(rv.Name + "gui-mesh", OpenTK.Graphics.OpenGL4.PrimitiveType.Points,
      Gpu.CreateVertexBuffer(rv.Name + "gui-mesh", verts.ToArray()),
      false
      );
      Mesh.DrawMode = DrawMode.Forward;
      Mesh.DrawOrder = DrawOrder.Last;
      //wo.Mesh.DebugBreakRender = true;

    }
    private void SetExtentsToViewport(RenderView rv)
    {
      _props._top = rv.Viewport.Y;
      _props._left = rv.Viewport.X;
      _props._width = rv.Viewport.Width;
      _props._height = rv.Viewport.Height;
      _props._maxWHPX = new vec2(rv.Viewport.Width, rv.Viewport.Height);//Make sure stuff doesn't go off the screen.
      _props._minWHPX = new vec2(0, 0);
    }
    private void CreateWindowEvents()
    {
      //Drag Info..
      //Context Menus..
      InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Press, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        var e = (Gu.Context.Renderer.Picker.PickedObjectFrame as UiElement);
        _dragInfo.StartDrag(e, m);
      });
      InlineStyle.AddEvent(UiEventId.Mouse_Move, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        _dragInfo.UpdateDrag(m);
      });
      InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Release, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        _dragInfo.EndDrag();
        if (ContextMenu.RenderVisible)
        {
          ContextMenu?.Hide();
        }
      });
      InlineStyle.AddEvent(UiEventId.Mouse_Rmb_Press, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        if (ContextMenu != null)
        {
          ContextMenu.Show();
          ContextMenu.InlineStyle.Pos = m.Pos;
        }
      });
      InlineStyle.AddEvent(UiEventId.Mouse_Rmb_Release, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        if (ContextMenu.RenderVisible)
        {
          ContextMenu?.Hide();
        }
      });
    }


    #endregion

  }//Gui2d

  public class UiRef<T> where T : class
  {
    public T Value = null;
    public UiRef() { }
    public UiRef(T val) { Value = val; }
  }
  public class UiStyle
  {
    //Purpose of this class is to encapsulate all style properties and "cascade" similar to CSS
    //I imagine we won't use this so much. Just one element (usually) will be associated with a style
    //The problem with the last system is that there was no default color, so we had to texture every element ugh.
    public string Name { get; set; } = "<unset>";
    public UiStyle Super
    {
      get { return _super; }
      set
      {
        if (AssertDAG())
        {
          _super = value;
          SetLayoutChanged();
        }
        else
        {
          Gu.DebugBreak();
        }
      }
    }
    private bool AssertDAG()
    {
      UiStyle that = Super;
      for (int n = 0; n < Gu.c_intMaxWhileTrueLoop; n++)
      {
        if (that == this)
        {
          Gu.Log.Error("Cycle in UI class '" + this.Name + "' and '" + Super.Name + "'. Subclass not set.");
          return false;
        }
        if (that != null)
        {
          that = that.Super;
        }
        else
        {
          break;
        }
      }
      return true;
    }
    public UiRef<MtTex> Texture
    {
      get
      {
        return _props._texture;
      }
      set
      {
        if (value != null)
        {
          if (_props._texture == null)
          {
            IterateElements((e) =>
            {
              e._iPickId = Gu.Context.Renderer.Picker.GenPickId();
            });
          }
          else
          {
            //keep same id
          }
        }
        else
        {
          IterateElements((e) =>
          {
            e._iPickId = 0;
          });
        }
        _props._texture = value;
        SetMustCompile();
      }
    }
    public float? Margin
    {
      get { return _props._marTop.Value; }
      set
      {
        _props._marTop = _props._marRight = _props._marLeft = _props._marBot = value;
        SetLayoutChanged();
        SetMustCompile();
      }
    }
    public float? Border
    {
      get { return _props._borderTop.Value; }
      set
      {
        _props._borderTop = _props._borderRight = _props._borderLeft = _props._borderBot = value;
        SetLayoutChanged();
        SetMustCompile();
      }
    }
    public float? BorderRadius
    {
      get { return _props._borderTopLeftRadius.Value; }
      set
      {
        _props._borderTopLeftRadius = _props._borderBotLeftRadius = _props._borderBotRightRadius = _props._borderTopRightRadius = value;
        SetLayoutChanged();
        SetMustCompile();
      }
    }
    public float? Padding
    {
      get { return _props._padTop.Value; }
      set
      {
        _props._padTop = _props._padRight = _props._padLeft = _props._padBot = value;
        SetLayoutChanged();
      }
    }
    public vec2 Pos { get { return new vec2(_props._left.Value, _props._top.Value); } set { _props._left = value.x; _props._top = value.y; SetLayoutChanged(); } }
    public vec2 Extent { get { return new vec2(_props._width.Value, _props._height.Value); } set { _props._width = value.x; _props._height = value.y; SetLayoutChanged(); } }
    public vec4? BorderColor { get { return _props._borderColor; } set { _props._borderColor = value; SetLayoutChanged(); } }
    public vec4? Color { get { return _props._color; } set { _props._color = value; SetLayoutChanged(); } }
    public vec4? FontColor { get { return _props._fontColor; } set { _props._fontColor = value; SetLayoutChanged(); } }
    public UiPositionMode? PositionMode { get { return _props._positionMode; } set { _props._positionMode = value; SetLayoutChanged(); } }
    public UiOverflowMode? OverflowMode { get { return _props._overflowMode; } set { _props._overflowMode = value; SetLayoutChanged(); } }
    public UiSizeMode? SizeModeWidth { get { return _props._sizeModeWidth; } set { _props._sizeModeWidth = value; SetLayoutChanged(); } }
    public UiSizeMode? SizeModeHeight { get { return _props._sizeModeHeight; } set { _props._sizeModeHeight = value; SetLayoutChanged(); } }
    public UiDisplayMode? DisplayMode { get { return _props._displayMode; } set { _props._displayMode = value; SetLayoutChanged(); } }
    public UiImageTiling? ImageTilingX { get { return _props._imageTilingX; } set { _props._imageTilingX = value; SetLayoutChanged(); } }
    public UiImageTiling? ImageTilingY { get { return _props._imageTilingY; } set { _props._imageTilingY = value; SetLayoutChanged(); } }
    public vec2? MinWHPX { get { return _props._minWHPX; } set { _props._minWHPX = value; SetLayoutChanged(); } }
    public vec2? MaxWHPX { get { return _props._maxWHPX; } set { _props._maxWHPX = value; SetLayoutChanged(); } }
    public float? Top { get { return _props._top; } set { _props._top = value; SetLayoutChanged(); } }
    public float? Left { get { return _props._left; } set { _props._left = value; SetLayoutChanged(); } }
    public float? MarginTop { get { return _props._marTop; } set { _props._marTop = value; SetLayoutChanged(); } }
    public float? MarginRight { get { return _props._marRight; } set { _props._marRight = value; SetLayoutChanged(); } }
    public float? MarginBot { get { return _props._marBot; } set { _props._marBot = value; SetLayoutChanged(); } }
    public float? MarginLeft { get { return _props._marLeft; } set { _props._marLeft = value; SetLayoutChanged(); } }
    public float? BorderTop { get { return _props._borderTop; } set { _props._borderTop = value; SetLayoutChanged(); } }
    public float? BorderRight { get { return _props._borderRight; } set { _props._borderRight = value; SetLayoutChanged(); } }
    public float? BorderBot { get { return _props._borderBot; } set { _props._borderBot = value; SetLayoutChanged(); } }
    public float? BorderLeft { get { return _props._borderLeft; } set { _props._borderLeft = value; SetLayoutChanged(); } }
    public float? BorderTopLeftRadius { get { return _props._borderTopLeftRadius; } set { _props._borderTopLeftRadius = value; SetLayoutChanged(); } }
    public float? BorderTopRightRadius { get { return _props._borderTopRightRadius; } set { _props._borderTopRightRadius = value; SetLayoutChanged(); } }
    public float? BorderBotRightRadius { get { return _props._borderBotRightRadius; } set { _props._borderBotRightRadius = value; SetLayoutChanged(); } }
    public float? BorderBotLeftRadius { get { return _props._borderBotLeftRadius; } set { _props._borderBotLeftRadius = value; SetLayoutChanged(); } }
    public float? PadTop { get { return _props._padTop; } set { _props._padTop = value; SetLayoutChanged(); } }
    public float? PadRight { get { return _props._padRight; } set { _props._padRight = value; SetLayoutChanged(); } }
    public float? PadBot { get { return _props._padBot; } set { _props._padBot = value; SetLayoutChanged(); } }
    public float? PadLeft { get { return _props._padLeft; } set { _props._padLeft = value; SetLayoutChanged(); } }
    public float? LineHeight { get { return _props._lineHeight; } set { _props._lineHeight = value; SetLayoutChanged(); } }
    public UiRef<FontFace> FontFace { get { return _props._fontFace; } set { _props._fontFace = value; SetLayoutChanged(); } }
    public float? FontSize { get { return _props._fontSize; } set { _props._fontSize = value; SetLayoutChanged(); } }
    public UiFontStyle? FontStyle { get { return _props._fontStyle; } set { _props._fontStyle = value; SetLayoutChanged(); } }
    public UiRef<Dictionary<UiEventId, MultiMap<string, Action<UiEventId, UiElement, PCMouse>>>> Events { get { return _props._events; } set { _props._events = value; } }

    public void Compile(UiStyle sub = null)
    {
      if (_bMustCompile)
      {
        _compiled = _props.Clone();
        if (_super != null)
        {
          _super.Compile(this);
        }
        if (sub != null)
        {
          _compiled.ApplySubclass(sub._props);
        }
        _bMustCompile = false;
      }
    }

    public UiStyleProps Props { get { return _props; } }
    public UiStyleProps Compiled { get { return _compiled; } }
    private UiStyleProps _props = new UiStyleProps();
    private UiStyleProps _compiled = null; // This is only for classes, not elements. Don't duplciate this data for all glyphs!
    private bool _bMustCompile = true;
    private UiStyle _super = null;
    public List<WeakReference<UiElement>> _eles = null;
    public long ChangedFrameId { get; set; } = 0;

    public UiStyle()
    {
    }
    public void AddEvent(UiEventId evId, Action<UiEventId, UiElement, PCMouse> f, string tag = "")
    {
      // Layman's: For each Mouse Button State we have a list of functions that get called when it is active (like clicked)
      if (_props._events == null)
      {
        _props._events = new UiRef<Dictionary<UiEventId, MultiMap<string, Action<UiEventId, UiElement, PCMouse>>>>(new Dictionary<UiEventId, MultiMap<string, Action<UiEventId, UiElement, PCMouse>>>());
      }
      MultiMap<string, Action<UiEventId, UiElement, PCMouse>>? evList = null;
      if (!_props._events.Value.TryGetValue(evId, out evList))
      {
        evList = new MultiMap<string, Action<UiEventId, UiElement, PCMouse>>();
        _props._events.Value.Add(evId, evList);
      }
      evList.Add(tag, f);
    }
    public bool RemoveEvents(UiEventId evId, string tag)
    {
      if (_props._events.Value.TryGetValue(evId, out var evList))
      {
        return evList.Remove(tag);
      }
      return false;
    }
    public UiStyle Clone()
    {
      UiStyle ret = new UiStyle();
      ret._props = this._props.Clone();
      ret._eles = null;
      ret._bMustCompile = true;
      ret._super = this._super;
      return ret;
    }
    private void SetLayoutChanged()
    {
      IterateElements((e) =>
      {
        e.SetLayoutChanged();
      });
      SetMustCompile();
    }
    private void SetMustCompile()
    {
      ChangedFrameId = Gu.Context.FrameStamp;
      _bMustCompile = true;
    }
    private void IterateElements(Action<UiElement> act)
    {
      if (_eles != null)
      {
        List<WeakReference<UiElement>> remove = new List<WeakReference<UiElement>>();
        foreach (var ele in _eles)
        {
          if (ele.TryGetTarget(out var e))
          {
            act(e);
          }
          else
          {
            remove.Add(ele);
          }
        }
        foreach (var e in remove)
        {
          _eles.Remove(e);
        }
      }
    }

  }
  public class UiStyleSheet
  {
    //The purpose of skins is to allow us to texture the UI in the future.
    //For now, we can use the 1 pixel texture and a default color (easier) 
    public const string DefaultStyle = "style-default";
    public const string DefaultHoverStyle = "style-default-hover";
    public const string DefaultDownStyle = "style-default-down";

    public UiStyle CurrentStyle = null;
    public MegaTex MegaTex { get; private set; } = null;
    public Dictionary<string, UiStyle> Styles = new Dictionary<string, UiStyle>();
    public UiStyleSheet(MegaTex tex)
    {
      if (tex.DefaultPixel == null)
      {
        Gu.BRThrowException("Default pixel for UI megatex must be set.");
      }
      MegaTex = tex;

      UiStyle defaultStyle = new UiStyle();

      defaultStyle.Name = DefaultStyle;
      defaultStyle.FontColor = vec4.rgba_ub(10, 10, 12, 1);
      defaultStyle.Color = vec4.rgba_ub(150, 150, 150, 255);
      defaultStyle.Texture = new UiRef<MtTex>(tex.DefaultPixel); //Flat color
      defaultStyle.PadTop = 5;
      defaultStyle.PadRight = 5;
      defaultStyle.PadBot = 5;
      defaultStyle.PadLeft = 5;
      defaultStyle.MarginTop = 0;
      defaultStyle.MarginRight = 0;
      defaultStyle.MarginBot = 0;
      defaultStyle.MarginLeft = 0;
      defaultStyle.FontFace = new UiRef<FontFace>(FontFace.RobotoMono);//What if this doesn't exist? Default?
      defaultStyle.FontStyle = UiFontStyle.Normal;
      defaultStyle.FontSize = 22;
      defaultStyle.LineHeight = 1.0f;
      Styles.Add(defaultStyle.Name, defaultStyle);

      var defaultDownStyle = new UiStyle()
      {
        Name = DefaultDownStyle,
        Super = defaultStyle,
        Color = new vec4(defaultStyle.Color.Value.xyz() * 0.7f, 1),
        FontColor = vec4.rgba_ub(190, 190, 190, 1)
      };
      Styles.Add(defaultDownStyle.Name, defaultDownStyle);

      var defaultHoverStyle = new UiStyle()
      {
        Name = DefaultHoverStyle,
        Super = defaultStyle,
        Color = new vec4(defaultStyle.Color.Value.xyz() * 1.3f, 1),
        FontColor = vec4.rgba_ub(30, 30, 32, 1)
      };
      Styles.Add(defaultHoverStyle.Name, defaultHoverStyle);
    }
    public UiStyle GetClass(string name)
    {
      UiStyle ret = null;
      Styles.TryGetValue(name, out ret);
      if (ret == null)
      {
        Gu.Log.Error("Failed to find UI Style " + name);
        Gu.DebugBreak();
      }
      return ret;
    }
  }//UiStyleSheet
  public class Gui2dShared
  {
    //Shared data between Gui2d instances for each context
    public UiStyleSheet StyleSheet { get; private set; } = null;
    public WorldObject Dummy { get; private set; } = null;
    public MegaTex MegaTex { get; private set; } = null;
    public string Name { get; private set; } = "<unnamed>";

    public Gui2dShared(string name, List<FileLoc> resources)
    {
      Name = name;
      MegaTex = new MegaTex("gui_megatex", true, MegaTex.MtClearColor.DebugRainbow, true, TexFilter.Linear, false);
      MegaTex.AddResources(resources);
      MegaTex.CompiledTextures tx = MegaTex.Compile();

      StyleSheet = new UiStyleSheet(MegaTex);

      if (tx != null)
      {
        var shader = Gu.Resources.LoadShader("v_gui", true, FileStorage.Embedded);
        Dummy = new WorldObject("gui");
        Dummy.Material = new Material("GuiMT", shader);
        Dummy.Material.GpuRenderState.DepthTest = false;
        Dummy.Material.GpuRenderState.Blend = true;
        Dummy.Material.AlbedoSlot.Texture = tx.Albedo;
      }
      else
      {
        Gu.Log.Error("Failed to compile mega tex " + MegaTex.Name);
      }
    }

  }//Gui2dShared
  public class Gui2dManager : OpenGLContextDataManager<Dictionary<ulong, Gui2dShared>>
  {

    //Shared GUI data for each context
    protected override Dictionary<ulong, Gui2dShared> CreateNew()
    {
      return new Dictionary<ulong, Gui2dShared>();
    }
    public Gui2dShared GetOrCreateGui2d(string name, List<FileLoc> resources)
    {

      var qualifiedPaths = resources.ConvertAll((x) => { return x.QualifiedPath; });
      var hash = Gu.HashStringArray(qualifiedPaths);

      Gui2dShared? g = null;
      var dict = GetDataForContext(Gu.Context);
      if (!dict.TryGetValue(hash, out g))
      {
        g = new Gui2dShared(name, resources);
        dict.Add(hash, g);
      }
      return g;
    }
  }//Gui2dManager

}//Namespace Pri
