using System;
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
    public static FontFace Fancy = new FontFace(new FileLoc("Parisienne-Regular.ttf", FileStorage.Embedded));
    public static FontFace Mono = new FontFace(new FileLoc("RobotoMono-Regular.ttf", FileStorage.Embedded));
    public static FontFace Pixel = new FontFace(new FileLoc("PressStart2P-Regular.ttf", FileStorage.Embedded));
    public static FontFace Entypo = new FontFace(new FileLoc("Entypo.ttf", FileStorage.Embedded));
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
    Relative // elements are relative to the container.
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
    Mouse_Rmb_Down,
    Mouse_Rmb_Release,
    Mouse_Rmb_None,
    Mouse_Mmb_Up,
    Mouse_Mmb_Press,
    Mouse_Mmb_Down,
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
      _fontFace = new UiRef<FontFace>(FontFace.Mono);
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

      return ret;
    }
    public void ApplySubclass(UiStyleProps sub)
    {
      //doesnt work
      //WE MUST AVOID SO MANY COPIES
      //Easy out
      if (this.CompiledFrameId >= sub.CompiledFrameId)
      {
        //not working (yawn)
        //   return;
      }

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
    public long CompiledFrameId { get; set; } = 0;
  }
  public class UiDragInfo
  {
    public bool _bDragStart = false;
    public bool _bDrag = false;
    public vec2 _vDragStart;
    public WeakReference<UiElement> Target { get; set; } = null;
    public UiDragInfo() { }
    public void Update(PCMouse ms)
    {
      float mw = 1.0f / 1; // UiScreen::getDesignMultiplierW the design multiplier - this isntn't accounted for
      float mh = 1.0f / 1; // UiScreen::getDesignMultiplierH the design multiplier - this isntn't accounted for
      if (Target != null && Target.TryGetTarget(out var tar))
      {
        if (_bDragStart)
        {
          if (ms.GetButtonState(MouseButton.Left) == ButtonState.Up)
          {
            //Avoid sticking
            _bDrag = false;
            _bDragStart = false;
          }
          else
          {
            //Check for mouse delta to prevent unnecessary updates.
            vec2 dp = ms.Pos - _vDragStart;
            if (MathUtils.FuzzyEquals(dp.x, 0.0f) == false || MathUtils.FuzzyEquals(dp.y, 0.0f) == false)
            {
              _bDrag = true;
            }
            else
            {
              _bDrag = false;
            }
            if (_bDrag)
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
        }
      }
      else
      {
        Target = null;
      }

    }
  }
  #endregion
  #region Classes
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
      public Box2f _b2LayoutQuad = new Box2f();        // Transformed from design space into screen space.
      public Box2f _b2RasterQuad = new Box2f();      // Final raster quad in OpenGL screen coordinates. BOTTOM LEFT = min TOP RIGHT = max
      public vec2 _rtl = new vec2(0, 0);
      public vec2 _rtr = new vec2(0, 0);
      public vec2 _rbr = new vec2(0, 0);
      public vec2 _rbl = new vec2(0, 0);
    }
    //Separate UiElement from container because Dictionary footprint is massive and, the glyphs have no children
    #region Public: Members

    public string Name { get { return _name; } set { _name = value; } }
    public string Text
    {
      get { return _strText; }
      set
      {
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
        _bStyleClassChanged = true;
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
      set { _inlineStyle = value; }
    }
    public bool IsPickRoot { get { return _isPickRoot; } set { _isPickRoot = value; } }
    public bool PickEnabled { get { return _pickEnabled; } set { _pickEnabled = value; } } //Prevents the pick algorithm from running on misc elements (such as glyphs).
    public bool ScaleToDesign { get; set; } = true; // this is true for all elements besides cursor.
    public bool LayoutChanged { get; private set; } = true;
    public bool LayoutVisible { get { return _layoutVisible; } set { _layoutVisible = value; SetLayoutChanged(); } }
    public bool RenderVisible { get { return _renderVisible; } set { _renderVisible = value; SetLayoutChanged(); } }

    #endregion
    #region Private: Members

    protected const int c_BaseLayerSort = 1000;
    protected const int c_GlyphLayerSort = 2000;
    protected UiStyleProps _props = new UiStyleProps();
    protected UiStyle _inlineStyle = null;      //Inline style
    protected UiStyle _styleClass = null;      //class styles
    protected UiStyle _glyphStyle = null; //Style for glyphs, in case this has text.
    protected MultiMap<int, UiElement> _children { get; set; } = null;
    private WeakReference<UiElement> _parent = null;
    private MtFont _cachedFont = null;
    private bool _isPickRoot = false;
    private bool _pickEnabled = true;
    private bool _layoutVisible = true;
    private bool _renderVisible = true;
    private bool _bTextChanged = false;
    private vec2 _tileScale = new vec2(1, 1); //Scale if UiImageSizeMode is Tile
    private FontPatchInfo _patch = null;
    protected Quads _contentArea = new Quads();
    private Quads _borderArea = null;
    private Box2f _b2ContentQuad = new Box2f();       // Encompassess ALL of the child quads, plus overflow.
    protected Box2f? _renderOffset = null;
    public uint _iPickId = 0;
    protected bool _bPickedThisFrame = false;
    protected bool _bPickedPreviousFrame = false;
    private long _iPickedFrameId = 0;
    protected string _strText = "";
    protected string _name = "";
    private bool _bStyleClassChanged = false;
    private UiDragInfo _dragInfo = null;
    public bool DragEnabled { get; private set; } = false;
    public Action<vec2> DragFunc { get; private set; } = null;

    #endregion
    #region Public: Methods

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
      if (boundsSS._max.y > b2ClipRect._min.y)
      {  //**Norte we flipped Y's </> here - because GL min/max runs the oppostie direction in the Y axis
        return true;
      }
      if (boundsSS._min.x > b2ClipRect._max.x)
      {
        return true;
      }
      if (boundsSS._min.y < b2ClipRect._max.y)
      {  //**Norte we flipped Y's </> here - because GL min/max runs the oppostie direction in the Y axis
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
        if (b._min.y < ret._min.y)
        {  //**Norte we flipped Y's </> here - because GL min/max runs the oppostie direction in the Y axis
          ret._min.y = b._min.y;
        }
        if (b._max.x < ret._max.x)
        {
          ret._max.x = b._max.x;
        }
        if (b._max.y > ret._max.y)
        {  //**Norte we flipped Y's </> here - because GL min/max runs the oppostie direction in the Y axis
          ret._max.y = b._max.y;
        }
        // Make sure it's valid
        if (ret._min.x > ret._max.x)
        {
          ret._min.x = ret._max.x;
        }
        if (ret._max.y > ret._min.y)
        {  //**Norte we flipped Y's </> here - because GL min/max runs the oppostie direction in the Y axis
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
      vc._clip = MakeClipRectForRender(b2ClipRect);
      if (dd.DisableClip)
      {
        // Disable clip
        vc._clip.x = -999999;
        vc._clip.y = -999999;
        vc._clip.z = 999999;
        vc._clip.w = 999999;
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
        v_v4v4v4v2u2v4v4 dbgv = new v_v4v4v4v2u2v4v4();
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
    private vec4 MakeClipRectForRender(Box2f b2ClipRect)
    {
      vec4 clipRect;

      // we have a parent, and parent hides stuff
      clipRect.x = b2ClipRect._min.x;
      clipRect.y = b2ClipRect._max.y;  // Note the swap of y here, for OpenGl texture coords
      clipRect.z = b2ClipRect._max.x;
      clipRect.w = b2ClipRect._min.y;

      return clipRect;
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
            if (pixid != 0)
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

        DoMouseButtonEvent(mouse, UiEventId.Mouse_Lmb_Up, eRmb, ButtonState.Up);
        DoMouseButtonEvent(mouse, UiEventId.Mouse_Lmb_Hold, eRmb, ButtonState.Hold);
        DoMouseButtonEvent(mouse, UiEventId.Mouse_Lmb_Press, eRmb, ButtonState.Press);
        DoMouseButtonEvent(mouse, UiEventId.Mouse_Lmb_Release, eRmb, ButtonState.Release);
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
    protected virtual void PerformLayout(MegaTex mt, vec2 viewport_wh, bool bForce, vec2 parentMaxWH)
    {
      //Build the UI depth-first. Children elements are built, then we add those
      //positions to the parent to build the elements.
      if (LayoutChanged || bForce)
      {
        ComputeStyle();
        UpdateBorder();
        vec2 contentWH = new vec2(_props._padLeft.Value + _props._padRight.Value, _props._padTop.Value + _props._padBot.Value); //in HTML all elements default to zero width without any contents.

        //Shrink boundary
        vec2 maxWH = new vec2(
          Math.Max(Math.Min(parentMaxWH.x, _props._maxWHPX.Value.x), 0),
          Math.Max(Math.Min(parentMaxWH.y, _props._maxWHPX.Value.y), 0)
        );

        //Check if we are a label
        //textchanged .. font color .. font size .. etc.
        //if (_bTextChanged == true)
        {
          CreateGlyphs(mt);
        }
        //Do Object Children.
        if (_children != null)
        {
          //PerformLayoutChildren
          //First pass must expand autosize elements up to the maximum w/h
          foreach (var p in _children)
          {
            UiElement ele = p.Value;
            if (ele.LayoutVisible)
            {
              ele.PerformLayout(mt, viewport_wh, bForce, maxWH);
            }
          }

          //Second pass positions elements, and, expands parent
          PositionChildren(bForce, viewport_wh, maxWH, ref contentWH);
        }

        SizeElement(maxWH, contentWH);

        LayoutChanged = false;
      }
    }
    private void ComputeStyle()
    {
      //Compute Style
      if (_styleClass != null)
      {
        if (_bStyleClassChanged)
        {
          //Debugging... this may beneeded tho
          _bStyleClassChanged = false;
        }
        _styleClass.Compile();
        _props.ApplySubclass(_styleClass.Compiled);
      }
      if (_inlineStyle != null)
      {
        _inlineStyle.Compile();
        _props.ApplySubclass(_inlineStyle.Compiled);
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
          _props._width = par._props._width - _props._left;
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
          _props._height = par._props._height - _props._top;
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

    }
    protected void PositionChildren(bool bForce, vec2 viewport_wh, vec2 maxWH, ref vec2 contentWH)
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
            else if (ele._props._positionMode == UiPositionMode.Relative)
            {
              // Fixed elements relative to container
              ComputePositionalElement(ele);
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

        //Layout the quads
        foreach (var p in _children)
        {
          UiElement ele = p.Value;
          if (ele.LayoutVisible)
          {
            LayoutEleQuads(viewport_wh, ele);
          }
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
    private void ComputePositionalElement(UiElement ele)
    {
      ValidateQuad();
    }
    private void LayoutEleQuads(vec2 viewport_wh, UiElement ele)
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

      ComputeQuads(viewport_wh, t + bt, r - br, b - bb, l + bl, rtl, rtr, rbr, rbl, _renderOffset, ele._contentArea);

      if (ele._borderArea != null)
      {
        ComputeQuads(viewport_wh, t, r, b, l, rtl, rtr, rbr, rbl, _renderOffset, ele._borderArea);
      }
    }
    protected static void ComputeQuads(vec2 viewport_wh, float top, float right, float bot, float left, float rtl, float rtr, float rbr, float rbl, Box2f? offset, Quads quads)
    {
      // Layout Quad (for picking, debug)/*  */
      float w1 = 1.0f, h1 = 1.0f;
      w1 = 1;//UiScreen::getDesignMultiplierW();
      h1 = 1;//UiScreen::getDesignMultiplierH();

      if (left > right)
      {
        left = right;
        Gu.DebugBreak();
      }
      if (top > bot)
      {
        top = bot;
        Gu.DebugBreak();
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
        quads._b2ComputedQuad._min.x = origin.x + ro.Left;
        quads._b2ComputedQuad._min.y = origin.y + ro.Top;
        quads._b2ComputedQuad._max.x = origin.x + ro.Right;
        quads._b2ComputedQuad._max.y = origin.y + ro.Bottom;
      }

      // Set to false if we're controllig coordinates of this element (cursor, or window position)
      quads._b2LayoutQuad._min.x = quads._b2ComputedQuad._min.x * w1;
      quads._b2LayoutQuad._min.y = quads._b2ComputedQuad._min.y * h1;
      quads._b2LayoutQuad._max.x = quads._b2ComputedQuad._max.x * w1;
      quads._b2LayoutQuad._max.y = quads._b2ComputedQuad._max.y * h1;

      // The resulting coordinates for the GPU are -0.5 +0.5 in both axes with the center being in the center of the screen
      // Translate a 2D screen quad to be rendered in a shader.
      // So* our quad is from TOP Left - OpenGL is Bottom Left - this fixes this.

      quads._b2RasterQuad = quads._b2LayoutQuad;

      float w = (float)viewport_wh.x;
      float w2 = w * 0.5f;
      float h = (float)viewport_wh.y;
      float h2 = h * 0.5f;

      // Subtract from viewport center
      quads._b2RasterQuad._min.x -= w2;
      quads._b2RasterQuad._max.x -= w2;

      // Invert text to show rightsize up and divide by perspective
      quads._b2RasterQuad._min.x = quads._b2RasterQuad._min.x / w2;
      quads._b2RasterQuad._min.y = (h2 - quads._b2RasterQuad._min.y - 1) / h2;
      quads._b2RasterQuad._max.x = quads._b2RasterQuad._max.x / w2;
      quads._b2RasterQuad._max.y = (h2 - quads._b2RasterQuad._max.y - 1) / h2;

      quads._rtl = new vec2(rtl / w2, rtl / h2);
      quads._rtr = new vec2(rtr / w2, rtr / h2);
      quads._rbr = new vec2(rbr / w2, rbr / h2);
      quads._rbl = new vec2(rbl / w2, rbl / h2);
    }
    private void CreateGlyphs(MegaTex mt)
    {
      if (_children == null)
      {
        _children = new MultiMap<int, UiElement>();
      }
      _children.Remove(c_GlyphLayerSort);

      //Get the font if it isn't already got.
      MtFont font = null;
      if (_cachedFont == null || mt.GetFont(_props._fontFace.Value) != _cachedFont)
      {
        font = mt.GetFont(_props._fontFace.Value);
      }
      else
      {
        font = _cachedFont;
      }

      float fontHeight = _props._fontSize.Value;
      var patch = font.SelectFontPatchInfo(fontHeight);
      if (patch == null)
      {
        return;
      }
      CachedCharData ccd = new CachedCharData();

      // var glyphStyle = new UiStyle();
      // glyphStyle._props.SetAllDefault();
      // glyphStyle.IsStatic = true;

      int index = 0;
      foreach (int cc in _strText)
      {
        int ccNext = (index + 1) < _strText.Length ? _strText[index++] : 0;
        float adv = font.GetKernAdvanceWidth(patch, _props._fontSize.Value, cc, ccNext);
        if (adv != 0)
        {
          int n = 0;
          n++;
        }

        patch.GetChar(cc, fontHeight, out ccd);

        //TODO: this should be UiElementBase, for simplicity. UiElement is too huge.
        UiElement e = new UiElement();
        e._pickEnabled = false;
        e._renderOffset = new Box2f(new vec2(ccd.left, ccd.top), new vec2(ccd.right, ccd.bot));
        e._props.SetDefault();
        e._props._texture = new UiRef<MtTex>(new MtTex(null, 0));
        e._props._texture.Value.SetWH(patch.TextureWidth, patch.TextureHeight);
        e._props._texture.Value.uv0 = ccd.uv0;
        e._props._texture.Value.uv1 = ccd.uv1;
        e._props._left = 0;
        e._props._top = 0;
        e._props._minWHPX = new vec2(ccd.width, ccd.height * _props._lineHeight.Value);
        e._props._marRight = ccd.marginRight + adv;
        e._props._positionMode = UiPositionMode.Static;
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
        AddChild(e, c_GlyphLayerSort);
      }
    }

    #endregion

    protected Box2f GetScreenSpaceClipQuad()
    {
      if (_borderArea != null)
      {
        return _borderArea._b2RasterQuad;
      }
      return _contentArea._b2RasterQuad;
    }
  }//UiElement
  public class UiScreen : UiElement
  {
    private WeakReference<Camera3D> _camera = new WeakReference<Camera3D>(null);
    public UiDebugDraw DebugDraw { get; set; } = new UiDebugDraw();
    private UiDragInfo _dragInfo = new UiDragInfo();

    public UiScreen(Camera3D cam)
    {
      _camera = new WeakReference<Camera3D>(cam);
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
    }
    private void SetExtentsToViewport(Camera3D cam)
    {
      _props._top = cam.Viewport_Y;
      _props._left = cam.Viewport_X;
      _props._width = cam.Viewport_Width - cam.Viewport_X - 1;
      _props._height = cam.Viewport_Height - cam.Viewport_Y - 1;
      _props._maxWHPX = new vec2(cam.Viewport_Width, cam.Viewport_Height);//Make sure stuff doesn't go off the screen.
      _props._minWHPX = new vec2(cam.Viewport_X, cam.Viewport_Y);

      CreateWindowEvents();
    }
    private void CreateWindowEvents()
    {
      InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Press, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        //Dragging
        _dragInfo._bDragStart = true;
        _dragInfo._vDragStart = m.Pos;
        var e = (Gu.Context.Renderer.Picker.PickedObjectFrame as UiElement);
        if (e != null && e.DragEnabled)
        {
          _dragInfo.Target = new WeakReference<UiElement>(e);
        }
      });
      InlineStyle.AddEvent(UiEventId.Mouse_Move, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        //Dragging
        _dragInfo.Update(m);
      });
      InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Release, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        //Dragging
        _dragInfo.Target = null;
        _dragInfo._bDrag = false;
      });
    }
    public static int GetSortLayer(int n)
    {
      // Mnemonic wich gves you the base sort layer, provided n will return additional layers.
      return c_BaseLayerSort + n;
    }
    public void Update(MegaTex mt, WorldObject wo, WindowContext ct)
    {
      if (_dragInfo != null)
      {
        _dragInfo.Update(ct.PCMouse);
      }

      if (_camera.TryGetTarget(out var cam))
      {
        SetExtentsToViewport(cam);

        UpdateLayout(mt, wo, ct.PCMouse, cam);

        RegenMesh(wo, mt);

        Pick(ct);
      }
    }
    private void Pick(WindowContext ct)
    {
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

      //Window events
      DoMouseEvents(ct.PCMouse, true);
    }
    private vec2 _viewport_wh_last = new vec2(1, 1);
    private void UpdateLayout(MegaTex mt, WorldObject wo, PCMouse mouse, Camera3D cam)
    {
      if (LayoutChanged)
      {
        bool force = false;
        vec2 viewport_wh = new vec2(cam.Viewport_Width, cam.Viewport_Height);
        if ((int)viewport_wh.x != (int)_viewport_wh_last.x || (int)viewport_wh.y != (int)_viewport_wh_last.y)
        {
          force = true;
          _viewport_wh_last = viewport_wh;
        }

        // Gui2d doesn't have a parent, so we have to compute the quads to create a valid clip region.
        ComputeQuads(viewport_wh,
        _props._top.Value, _props._right.Value, _props._bottom.Value, _props._left.Value,
        _props._borderTopLeftRadius.Value, _props._borderTopRightRadius.Value, _props._borderBotRightRadius.Value, _props._borderBotLeftRadius.Value,
        _renderOffset, _contentArea);

        PerformLayout(mt, viewport_wh, force, this._props._maxWHPX.Value);
      }
    }
    private void RegenMesh(WorldObject wo, MegaTex mt)
    {
      Box2f b = GetScreenSpaceClipQuad();// _b2RasterQuad;// getGLRasterQuad();
      List<v_v4v4v4v2u2v4v4> verts = new List<v_v4v4v4v2u2v4v4>();

      Gu.Assert(mt.DefaultPixel != null);
      RegenMesh(verts, mt.DefaultPixel, b, 0, DebugDraw);

      wo.Mesh = new MeshData("gui_mesh", OpenTK.Graphics.OpenGL4.PrimitiveType.Points,
      Gpu.CreateVertexBuffer(verts.ToArray()),
      false
      );
      wo.Mesh.DrawOrder = DrawOrder.Last;
    }
  }
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
      }
    }
    private bool AssertDAG()
    {
      UiStyle that = Super;
      for (int n = 0; n < 1000; n++)
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

    public bool IsStatic { get; set; } = false;//If this style is static, it is copied to the element. Also remove temp data. Used for Glyphs and non-updatable (expensive) elements.

    public bool Compile(UiStyle sub = null)
    {
      if (!IsStatic && _bMustCompile)
      {
        _compiled = _props.Clone();
        if (_super != null)
        {
          Gu.Assert(_super.IsStatic == false);//sanity
          _super.Compile(this);
        }
        if (sub != null)
        {
          _compiled.ApplySubclass(sub._props);
        }
        _compiled.CompiledFrameId = Gu.Context.FrameStamp;

        _bMustCompile = false;
        return true;
      }
      else
      {
        return false;
      }
    }
    private void SetMustCompile()
    {
      _bMustCompile = true;
    }
    public UiStyleProps Props { get { return _props; } }
    public UiStyleProps Compiled { get { return _compiled; } }

    private UiStyleProps _props = new UiStyleProps();
    private UiStyleProps _compiled = null; // This is only for classes, not elements. Don't duplciate this data for all glyphs!
    private bool _bMustCompile = true;
    private UiStyle _super = null;
    public List<WeakReference<UiElement>> _eles = null;

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
      ret.IsStatic = this.IsStatic;
      ret._eles = null;
      ret._bMustCompile = true;
      ret._super = this._super;
      return ret;
    }
    private void SetLayoutChanged()
    {
      if (IsStatic == false)
      {
        IterateElements((e) =>
        {
          e.SetLayoutChanged();
        });
        SetMustCompile();
      }
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
    public const string DefaultStyle = "default";
    public const string DefaultHoverStyle = "default_hover";
    public const string DefaultDownStyle = "default_down";

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
      defaultStyle.FontFace = new UiRef<FontFace>(FontFace.Mono);
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
    public void Update()
    {
      //elements will dynamically compile styles when they updt lyotu
      // foreach (var spair in this.Styles)
      // {
      //   spair.Value.CompileIfNeeded();
      // }
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
  }
  public class GuiComponent : Component
  {
    private Shader _shader = null;
    private MegaTex _megaTex = null;
    private UiStyleSheet _styleSheet = null;

    public UiScreen Screen { get; private set; } = null;

    public MtFont GetFont(FileLoc loc)
    {
      return _megaTex.GetFont(loc);
    }
    public GuiComponent(Camera3D cam)
    {
      _megaTex = new MegaTex("gui_megatex", true);
      _styleSheet = new UiStyleSheet(_megaTex);

      GetFont(FontFace.Fancy);
      GetFont(FontFace.Mono);
      GetFont(FontFace.Pixel);
      GetFont(FontFace.Entypo);

      Screen = new UiScreen(cam);
    }
    public override void OnCreate(WorldObject myObj)
    {
      //When the component is create. Compile texture.
      //Linear filtering makes text look very smooth. 
      //Do not use mipmaps in the UI, it messes up the fonts. Or, we must use a separate font texture if we choose to use mipmaps (or custom mipmaps).

      _megaTex.LoadImages();
      MegaTex.CompiledTextures tx = _megaTex.Compile(MegaTex.MtClearColor.DebugRainbow, false, TexFilter.Linear, false);
      if (tx != null)
      {
        _shader = Gu.Resources.LoadShader("v_gui", true, FileStorage.Embedded);
        myObj.Material = new Material("GuiMT", _shader);
        myObj.Material.GpuRenderState.DepthTest = false;
        myObj.Material.GpuRenderState.Blend = true;
        myObj.Material.Textures[Shader.TextureInput.Albedo] = tx.Albedo;
      }
      else
      {
        Gu.Log.Error("Failed to compile mega tex " + _megaTex.Name);
      }
    }
    public override void OnUpdate(double dt, WorldObject obj)
    {
      //this._megaTex.Update();
      _styleSheet.Update();
      Screen.Update(_megaTex, obj, Gu.Context);
    }
    public override void OnDestroy(WorldObject myObj)
    {
    }
    public override Component Clone(bool shallow = true)
    {
      Gu.BRThrowNotImplementedException();
      return null;
    }
    public MtTex DefaultPixel()
    {
      return _megaTex.DefaultPixel;
    }
    private UiElement CreateDefaultStyledElement(string name)
    {
      UiElement e = new UiElement(name);
      if (_styleSheet != null)
      {
        e.StyleClass = _styleSheet.GetClass(UiStyleSheet.DefaultStyle);
      }
      return e;
    }
    public UiElement CreatePanel(string name, vec2? pos, vec2? wh)
    {
      UiElement e = CreateDefaultStyledElement(name);
      e.InlineStyle.Texture = new UiRef<MtTex>(_megaTex.DefaultPixel);
      if (pos != null)
      {
        e.InlineStyle.Pos = pos.Value;
        e.InlineStyle.PositionMode = UiPositionMode.Relative;
      }
      if (wh != null)
      {
        e.InlineStyle.Extent = wh.Value;
      }
      return e;
    }
    public UiElement CreateButton(string name, vec2? pos, string text, Action<UiEventId, UiElement, PCMouse> onClick = null)
    {
      UiElement e = CreateDefaultStyledElement(name);
      e.InlineStyle.Texture = new UiRef<MtTex>(_megaTex.DefaultPixel);
      if (pos != null)
      {
        e.InlineStyle.Pos = pos.Value;
        e.InlineStyle.PositionMode = UiPositionMode.Relative;
      }
      e.Text = text;
      e.InlineStyle.MaxWHPX = new vec2(100, 200);
      e.InlineStyle.Border = 0;
      e.InlineStyle.BorderRadius = 6;
      e.InlineStyle.BorderColor = vec4.rgba_ub(90, 120, 240, 255);
      e.InlineStyle.Color = vec4.rgba_ub(35,47,62, 255);
      e.IsPickRoot = true;
      e.InlineStyle.PadBot = e.InlineStyle.PadLeft = e.InlineStyle.PadTop = e.InlineStyle.PadRight = 15;// Fonts are messed up right now 
      e.InlineStyle.FontColor = vec4.rgba_ub(255, 255, 255, 255);
      e.InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Release, (eid, uie, m) =>
      {
        e.StyleClass = _styleSheet.GetClass(UiStyleSheet.DefaultHoverStyle);
        e.InlineStyle.FontColor = vec4.rgba_ub(255, 255, 255, 255);
      });
      e.InlineStyle.AddEvent(UiEventId.Mouse_Enter, (eid, uie, m) =>
      {
        e.InlineStyle.Border = 1;
        e.InlineStyle.BorderColor = vec4.rgba_ub(255, 255, 255, 255);
        //e.StyleClass = _styleSheet.GetClass(UiStyleSheet.DefaultHoverStyle);
      });
      e.InlineStyle.AddEvent(UiEventId.Mouse_Leave, (eid, uie, m) =>
      {
        e.InlineStyle.Border = 0;
        //e.StyleClass = _styleSheet.GetClass(UiStyleSheet.DefaultStyle);
      });
      e.InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Press, (eid, uie, m) =>
      {
        e.InlineStyle.FontColor = vec4.rgba_ub(200, 200, 200, 255);
      });
      if (onClick != null)
      {
        e.InlineStyle.AddEvent(UiEventId.Mouse_Lmb_Release, onClick);
      }
      return e;
    }
    public UiElement CreateLabel(string name, vec2? pos, string text, bool showbackground = true, FontFace? font = null, float fontSize = 12, vec4? fontColor = null, UiFontStyle fontstyle = UiFontStyle.Normal, float lineheight = 1.0f)
    {
      UiElement e = CreateDefaultStyledElement(name);
      e.InlineStyle.Texture = new UiRef<MtTex>(showbackground ? _megaTex.DefaultPixel : null);
      if (pos != null)
      {
        e.InlineStyle.Pos = pos.Value;
        e.InlineStyle.PositionMode = UiPositionMode.Relative;
      }
      e.Text = text;
      e.InlineStyle.FontFace = new UiRef<FontFace>(font != null ? font : FontFace.Mono);
      e.InlineStyle.FontSize = fontSize;
      e.InlineStyle.FontColor = fontColor != null ? fontColor.Value : new vec4(1, 1, 1, 1);
      e.InlineStyle.FontStyle = fontstyle;
      e.InlineStyle.LineHeight = lineheight;
      e.InlineStyle.PadBot = e.InlineStyle.PadLeft = e.InlineStyle.PadTop = e.InlineStyle.PadRight = 15;// Fonts are messed up right now 
      e.InlineStyle.PadBot = 10;
      e.InlineStyle.PadTop = 10;
      e.InlineStyle.PadLeft = 10;
      e.InlineStyle.PadRight = 10;
      e.IsPickRoot = true;
      return e;
    }
  }//class Gui

  #endregion

}//Namespace Pri
