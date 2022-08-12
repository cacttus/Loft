using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Windowing.GraphicsLibraryFramework;
namespace PirateCraft
{
  #region Enums
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
  #endregion
  public class UiDragInfo
  {
    public Action<vec2> _func = null;
    public bool _bDragStart = false;
    public bool _bDrag = false;
    public vec2 _vDragStart;
    public UiDragInfo(Action<vec2> func) { _func = func; }
    //vec2 _vPosStartPx;
    // Box2f _b2StartBox;//Computed box (design space) of starting position
    public void Update(PCMouse ms)
    {
      float mw = 1.0f / 1; // UiScreen::getDesignMultiplierW the design multiplier - this isntn't accounted for
      float mh = 1.0f / 1; // UiScreen::getDesignMultiplierH the design multiplier - this isntn't accounted for

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
          vec2 dp = ms.Pos - _vDragStart;// pFingers->getMousePos_Relative()
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

            _func(dp);

            //Reset drag start
            _vDragStart = ms.Pos; //pFingers->getMousePos_Relative();
          }
        }
      }
    }
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
  public class UiElement
  {
    class UiLine
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
    };
    //Separate UiElement from container because Dictionary footprint is massive and, the glyphs have no children
    #region Public: Members

    public vec4 Color { get { return _color; } set { _color = value; } }
    public vec2 Pos { get { return new vec2(_left, _top); } set { _left = value.x; _top = value.y; SetLayoutChanged(); } }
    public vec2 Extent { get { return new vec2(_width, _height); } set { _width = value.x; _height = value.y; SetLayoutChanged(); } }
    public UiPositionMode PositionMode { get { return _positionMode; } set { _positionMode = value; SetLayoutChanged(); } }
    public UiOverflowMode OverflowMode { get { return _overflowMode; } set { _overflowMode = value; SetLayoutChanged(); } }
    public UiSizeMode SizeModeWidth { get { return _sizeModeHeight; } set { _sizeModeHeight = value; SetLayoutChanged(); } }
    public UiSizeMode SizeModeHeight { get { return _sizeModeWidth; } set { _sizeModeWidth = value; SetLayoutChanged(); } }
    public UiDisplayMode DisplayMode { get { return _displayMode; } set { _displayMode = value; SetLayoutChanged(); } }
    public UiImageTiling ImageTilingX { get { return _imageTilingX; } set { _imageTilingX = value; SetLayoutChanged(); } }
    public UiImageTiling ImageTilingY { get { return _imageTilingY; } set { _imageTilingY = value; SetLayoutChanged(); } }
    public vec2 MinWHPX { get { return _minWHPX; } set { _minWHPX = value; SetLayoutChanged(); } }
    public vec2 MaxWHPX { get { return _maxWHPX; } set { _maxWHPX = value; SetLayoutChanged(); } }
    public bool LayoutVisible { get { return _layoutVisible; } set { _layoutVisible = value; SetLayoutChanged(); } }
    public bool RenderVisible { get { return _renderVisible; } set { _renderVisible = value; SetLayoutChanged(); } }
    public float MarginTop { get { return _marTop; } set { _marTop = value; SetLayoutChanged(); } }
    public float MarginRight { get { return _marRight; } set { _marRight = value; SetLayoutChanged(); } }
    public float MarginBot { get { return _marBot; } set { _marBot = value; SetLayoutChanged(); } }
    public float MarginLeft { get { return _marLeft; } set { _marLeft = value; SetLayoutChanged(); } }
    public float PadTop { get { return _padTop; } set { _padTop = value; SetLayoutChanged(); } }
    public float PadRight { get { return _padRight; } set { _padRight = value; SetLayoutChanged(); } }
    public float PadBot { get { return _padBot; } set { _padBot = value; SetLayoutChanged(); } }
    public float PadLeft { get { return _padLeft; } set { _padLeft = value; SetLayoutChanged(); } }
    public float LineHeight { get { return _lineHeight; } set { _lineHeight = value; SetLayoutChanged(); } }
    public FontFace FontFace { get { return _fontFace; } set { _fontFace = value; SetLayoutChanged(); } }
    public float FontSize { get { return _fontSize; } set { _fontSize = value; SetLayoutChanged(); } }
    public FontStyle FontStyle { get { return _fontStyle; } set { _fontStyle = value; SetLayoutChanged(); } }
    public vec4 FontColor { get { return _fontColor; } set { _fontColor = value; SetLayoutChanged(); } }

    public string Text { get { return _strText; } set { _strText = value; SetLayoutChanged(); _bTextChanged = true; } }
    public bool IsPickRoot { get { return _isPickRoot; } set { _isPickRoot = value; } }
    public bool PickEnabled { get { return _pickEnabled; } set { _pickEnabled = value; } } //Prevents the pick algorithm from running on misc elements (such as glyphs).
    public bool ScaleToDesign { get; set; } = true; // this is true for all elements besides cursor.
    public bool LayoutChanged { get; private set; } = true;
    public string Name { get { return _name; } set { _name = value; } }
    public MtTex Texture
    {
      get
      {
        return _texture;
      }
      set
      {
        if (value != null)
        {
          if (_texture == null)
          {
            _iPickId = Gu.Context.Renderer.Picker.GenPickId();
          }
          else
          {
            //keep same id
          }
        }
        else
        {
          _iPickId = 0;
        }
        _texture = value;
      }
    }

    #endregion
    #region Private: Members

    protected const int c_BaseLayerSort = 1000;
    protected const int c_GlyphLayerSort = 2000;
    protected MultiMap<int, UiElement> _children { get; set; } = null;
    private WeakReference<UiElement> _parent = null;
    private MtFont _cachedFont = null;
    private bool _isPickRoot = false;
    private bool _pickEnabled = true;
    private bool _layoutVisible = true;
    private bool _renderVisible = true;
    private bool _bTextChanged = false;
    private Box2f _q2Tex = new Box2f(0, 0, 1, 1);//0,1
    private vec2 _tileScale = new vec2(1, 1); //Scale if UiImageSizeMode is Tile
    private FontPatchInfo _patch = null;
    private Box2f _b2ComputedQuad = new Box2f();      // Base quad that is calculated from _right/_left.., parent, padding, etc
    private Box2f _b2ComputedQuadLast = new Box2f();
    private Box2f _b2ContentQuad = new Box2f();       // Encompassess ALL of the child quads, plus overflow.
    private Box2f _b2LayoutQuad = new Box2f();        // Transformed from design space into screen space.
    protected Box2f _b2RasterQuad = new Box2f();      // Final raster quad in OpenGL screen coordinates.
    private Box2f? _renderOffset = null;
    private uint _iPickId = 999;
    //private UiDragInfo _dragInfo = null;
    protected bool _bPickedThisFrame = false;
    protected bool _bPickedPreviousFrame = false;
    private long _iPickedFrameId = 0;
    private Dictionary<UiEventId, List<Action<UiEventId, UiElement, PCMouse>>> _events = null;//EventId, list of action (EventId, Object)

    protected MtTex _texture = null;
    protected string _strText = "";
    protected vec4 _color = new vec4(1, 1, 1, 1);
    protected FontStyle _fontStyle = FontStyle.Normal;
    protected vec4 _fontColor = new vec4(0, 0, 0, 1);
    protected FontFace _fontFace = FontFace.Mono;
    protected UiImageTiling _imageTilingX = UiImageTiling.Expand;
    protected UiImageTiling _imageTilingY = UiImageTiling.Expand;
    protected UiDisplayMode _displayMode = UiDisplayMode.Block;
    protected float _fontSize = 20;
    protected float _lineHeight = 1;
    protected float _top = 50;
    protected float _left = 50;
    protected float _right { get { return _left + _width; } }
    protected float _bottom { get { return _top + _height; } }
    protected float _width = 100;
    protected float _height = 100;
    protected float _padTop = 0;
    protected float _padRight = 0;
    protected float _padBot = 0;
    protected float _padLeft = 0;
    protected float _marTop = 0;
    protected float _marRight = 0;
    protected float _marBot = 0;
    protected float _marLeft = 0;
    protected vec2 _minWHPX = new vec2(10, 10);
    protected vec2 _maxWHPX = new vec2(99999, 99999);
    protected UiPositionMode _positionMode = UiPositionMode.Static;
    protected UiOverflowMode _overflowMode = UiOverflowMode.Hide;
    protected UiSizeMode _sizeModeWidth = UiSizeMode.Shrink;
    protected UiSizeMode _sizeModeHeight = UiSizeMode.Shrink;
    protected string _name = "";

    #endregion
    #region Public: Methods
    public UiElement()
    {
      //Note: if no Texture is set, the element will not be rendered (i.e. it's a container)
    }
    public UiElement(string name)
    {
      _name = name;
    }
    public UiElement(UiStyle style)
    {
      ApplyStyle(style);
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
    public void AddEvent(UiEventId evId, Action<UiEventId, UiElement, PCMouse> f)
    {
      // Layman's: For each Mouse Button State we have a list of functions that get called when it is active (like clicked)
      if (_events == null)
      {
        _events = new Dictionary<UiEventId, List<Action<UiEventId, UiElement, PCMouse>>>();
      }
      List<Action<UiEventId, UiElement, PCMouse>>? evList = null;
      if (!_events.TryGetValue(evId, out evList))
      {
        evList = new List<Action<UiEventId, UiElement, PCMouse>>();
        _events.Add(evId, evList);
      }
      evList.Add(f);
    }
    // public void EnableDrag(Action<vec2> func)
    // {
    //   _dragInfo = new UiDragInfo(func);
    //   AddEvent(UiEventId.Mouse_Lmb_Press, (UiEventId evId, UiElement ele, PCMouse m) =>
    //   {
    //     ele._dragInfo._bDragStart = true;
    //     ele._dragInfo._vDragStart = m.Pos;
    //   });

    //   AddEvent(UiEventId.Mouse_Lmb_Release, (UiEventId evId, UiElement ele, PCMouse m) =>
    //   {
    //     ele._dragInfo._bDrag = false;
    //   });

    //   AddEvent(UiEventId.Mouse_Lmb_Up, (UiEventId evId, UiElement ele, PCMouse m) =>
    //   {
    //     ele._dragInfo._bDrag = false;
    //   });
    // }
    public void ValidateQuad()
    {
      if (((_left + _width) < _left) || ((_top + _height) < _top))
      {
        Gu.Log.Error("Computed Quad is invalid, rtbl= " + (_left + _width) + "," + _left + "," + (_top + _height) + "," + _top + ".");
        Gu.DebugBreak();
      }
    }
    public T GetFirstChild<T>() where T : UiElement
    {
      Gu.Assert(_children.Count > 0);
      return (T)_children.First().Value;
    }
    public virtual void ApplyStyle(UiStyle style)
    {
      Gu.Assert(style != null);
      if (style.FontColor != null) { this.FontColor = style.FontColor.Value; }
      if (style.Color != null) { this.Color = style.Color.Value; }
      if (style.Texture != null) { this.Texture = style.Texture; }
      if (style.PadTop != null) { this.PadTop = style.PadTop.Value; }
      if (style.PadRight != null) { this.PadRight = style.PadRight.Value; }
      if (style.PadBot != null) { this.PadBot = style.PadBot.Value; }
      if (style.PadLeft != null) { this.PadLeft = style.PadLeft.Value; }
      if (style.MarginTop != null) { this.MarginTop = style.MarginTop.Value; }
      if (style.MarginRight != null) { this.MarginRight = style.MarginRight.Value; }
      if (style.MarginBot != null) { this.MarginBot = style.MarginBot.Value; }
      if (style.MarginLeft != null) { this.MarginLeft = style.MarginLeft.Value; }
      if (style.FontFace != null) { this.FontFace = style.FontFace; }
      if (style.FontStyle != null) { this.FontStyle = style.FontStyle.Value; }
      if (style.FontSize != null) { this.FontSize = style.FontSize.Value; }
      if (style.LineHeight != null) { this.LineHeight = style.LineHeight.Value; }
    }
    #endregion
    #region Private: and Protected: Methods
    protected void SetLayoutChanged()
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
      // This simple test saves us a ton of GPU pixel tests
      Box2f b = _b2RasterQuad;
      if (b._max.x < b2ClipRect._min.x)
      {
        return true;
      }
      if (b._max.y > b2ClipRect._min.y)
      {  //**Norte we flipped Y's </> here - because GL min/max runs the oppostie direction in the Y axis
        return true;
      }
      if (b._min.x > b2ClipRect._max.x)
      {
        return true;
      }
      if (b._min.y < b2ClipRect._max.y)
      {  //**Norte we flipped Y's </> here - because GL min/max runs the oppostie direction in the Y axis
        return true;
      }
      return false;
    }
    protected virtual void RegenMesh(List<v_v4v4v4v2u2> verts, Box2f b2ClipRect, uint rootPickId)
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

          GetOpenGLQuadVerts(verts, b2ClipRect, pickId);
          if (_children != null)
          {
            foreach (var p in _children)
            {
              UiElement ele = p.Value;

              if (ele.IsFullyClipped(b2ClipRect) == false)
              {
                Box2f clip = ShrinkClipRect(b2ClipRect);
                ele.RegenMesh(verts, clip, pickId);
              }
            }
          }
        }
      }
    }
    private Box2f ShrinkClipRect(Box2f b2ClipRect)
    {
      // Hide all children that go beyond this container.
      // Must be called in the loop so we reset it with every child.
      Box2f b = _b2RasterQuad;
      Box2f ret = b2ClipRect;
      if (OverflowMode == UiOverflowMode.Hide)
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
    private protected void GetOpenGLQuadVerts(List<v_v4v4v4v2u2> verts, Box2f b2ClipRect, uint rootPickId)
    {
      if (LayoutVisible == false)
      {
        return;
      }
      if (Texture == null)
      {
        //invisible, or container element
        return;
      }

      if (ImageTilingX == UiImageTiling.Expand)
      {
        _q2Tex._min.x = Texture.uv0.x;
        _q2Tex._max.x = Texture.uv1.x;
      }
      else if (ImageTilingX == UiImageTiling.Tile)
      {
        float wPx = _width;
        _q2Tex._min.x = Texture.uv0.x;
        _q2Tex._max.x = Texture.uv1.x + (Texture.uv1.x - Texture.uv0.x) * _tileScale.x;
      }
      else if (ImageTilingX == UiImageTiling.Computed)
      {
        // Tex coords are computed (used for UiGlyph)
      }
      else
      {
        Gu.Log.Error("Invalid layout X image size mode.");
      }

      if (ImageTilingY == UiImageTiling.Expand)
      {
        _q2Tex._min.y = Texture.uv0.y;
        _q2Tex._max.y = Texture.uv1.y;
      }
      else if (ImageTilingY == UiImageTiling.Tile)
      {
        float hPx = _height;
        _q2Tex._min.y = Texture.uv0.y;
        _q2Tex._max.y = Texture.uv1.y + (Texture.uv1.y - Texture.uv0.y) * _tileScale.y;
      }
      else if (ImageTilingY == UiImageTiling.Computed)
      {
        // Tex coords are computed (used for UiGlyph)
      }
      else if (ImageTilingY == UiImageTiling.Proportion)
      {
        // proportion the Y to the X
        _q2Tex._min.y = Texture.uv1.y;
        float fw = _q2Tex._max.x - _q2Tex._min.x;
        float fr = Texture.GetSizeRatio();
        float fh = fw * fr;
        _q2Tex._max.y = _q2Tex._min.y + fh;
      }
      else
      {
        Gu.Log.Error("Invalid layout size mode.");
      }


      // if(_pQuad != nullptr) {
      // Add the vertexes of this element tot he given buffers
      // Creates an image out of this element.
      v_v4v4v4v2u2 v = new v_v4v4v4v2u2();
      v._rect.x = _b2RasterQuad._min.x;
      v._rect.y = _b2RasterQuad._min.y;
      v._rect.z = _b2RasterQuad._max.x;
      v._rect.w = _b2RasterQuad._max.y;

      // Clip Rect.  For discard
      v._clip = makeClipRectForRender(b2ClipRect);
      if (Gu.EngineConfig.ShowGuiBoxesAndDisableClipping)
      {
        // Disable clip
        v._clip.x = -9999;
        v._clip.y = -9999;
        v._clip.z = 9999;
        v._clip.w = 9999;
      }

      //tex = pos uv
      //texsize = size uv i.e for wrapping?
      // Texs
      //tex z,w arn't used
      v._tex.x = _q2Tex._min.x;  // GL - bottom left
      v._tex.y = _q2Tex._min.y;
      v._tex.z = _q2Tex._max.x;  // GL - top right *this essentially flips it upside down
      v._tex.w = _q2Tex._max.y;

      v._texsiz.x = Math.Abs(Texture.uv1.x - Texture.uv0.x);
      v._texsiz.y = Math.Abs(Texture.uv1.y - Texture.uv0.y);  // Uv0 - uv1 - because we flipped coords bove

      //**Texture Adjust - modulating repeated textures causes seaming issues, especially with texture filtering
      // adjust the texture coordinates by some pixels to account for that.  0.5f seems to work well.
      float pixAdjust = 0.0f;  // # of pixels to adjust texture by
      float w1px = 0;                  // 1 pixel subtract from the u/v to prevent creases during texture modulation
      float h1px = 0;

      if (Texture.GetWidth() > 0 && v._texsiz.x > 0)
      {
        w1px = 1.0f / Texture.GetWidth();
        w1px *= v._texsiz.x;
        w1px *= pixAdjust;
      }
      if (Texture.GetHeight() > 0 && v._texsiz.y > 0)
      {
        h1px = 1.0f / Texture.GetHeight();
        h1px *= v._texsiz.y;
        h1px *= pixAdjust;
      }
      v._texsiz.x -= w1px * 2.0f;
      v._texsiz.y -= h1px * 2.0f;
      v._tex.x += w1px;
      v._tex.y += h1px;
      v._tex.z -= w1px;
      v._tex.w -= h1px;

      //**End texture adjust

      // Pick Color & Accent Color
      v._pick_color = new uvec2(
         //Since we can't discard fragments for the pick buffer, assume the pick id of the parent pick root
         rootPickId,
         ((uint)(Color.x * 255.0f) << 24) |
          ((uint)(Color.y * 255.0f) << 16) |
          ((uint)(Color.z * 255.0f) << 8) |
          ((uint)(Color.w * 255.0f) << 0)
      );

      verts.Add(v);
    }
    private vec4 makeClipRectForRender(Box2f b2ClipRect)
    {
      vec4 clipRect;

      // we have a parent, and parent hides stuff
      clipRect.x = b2ClipRect._min.x;
      clipRect.y = b2ClipRect._max.y;  // Note the swap of y here, for OpenGl texture coords
      clipRect.z = b2ClipRect._max.x;
      clipRect.w = b2ClipRect._min.y;

      return clipRect;
    }
    protected virtual bool Pick(PCMouse mous, long frameStamp)
    {
      //Recursive method to pick visible elements.
      //Of course..quadtree but that's just.. not needed
      Box2f q = _b2LayoutQuad;
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
    public void DoMouseEvents(PCMouse mouse)
    {
      if (_bPickedThisFrame)
      {
        if (_bPickedPreviousFrame)
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
      if (_events != null)
      {
        if (_events.TryGetValue(evid, out var actions))
        {
          foreach (var act in actions)
          {
            act(evid, this, m);
          }
        }
      }
    }
    protected void ComputeQuads(vec2 viewport_wh, float top, float right, float bot, float left)
    {
      // Layout Quad (for picking, debug)
      float w1 = 1.0f, h1 = 1.0f;
      w1 = 1;//UiScreen::getDesignMultiplierW();
      h1 = 1;//UiScreen::getDesignMultiplierH();

      _b2ComputedQuad._min.y = top;
      _b2ComputedQuad._max.x = right;
      _b2ComputedQuad._max.y = bot;
      _b2ComputedQuad._min.x = left;

      if (_renderOffset != null)
      {
        //For glyphs, and other elements that go outside their physical regions
        var origin = _b2ComputedQuad.Center();
        var ro = this._renderOffset.Value;
        _b2ComputedQuad._min.x = origin.x + ro.Left;
        _b2ComputedQuad._min.y = origin.y + ro.Top;
        _b2ComputedQuad._max.x = origin.x + ro.Right;
        _b2ComputedQuad._max.y = origin.y + ro.Bottom;
      }

      // Set to false if we're controllig coordinates of this element (cursor, or window position)
      _b2LayoutQuad._min.x = _b2ComputedQuad._min.x * w1;
      _b2LayoutQuad._min.y = _b2ComputedQuad._min.y * h1;
      _b2LayoutQuad._max.x = _b2ComputedQuad._max.x * w1;
      _b2LayoutQuad._max.y = _b2ComputedQuad._max.y * h1;

      // The resulting coordinates for the GPU are -0.5 +0.5 in both axes with the center being in the center of the screen
      // Translate a 2D screen quad to be rendered in a shader.
      // So* our quad is from TOP Left - OpenGL is Bottom Left - this fixes this.

      _b2RasterQuad = _b2LayoutQuad;

      float w = (float)viewport_wh.x;
      float w2 = w * 0.5f;
      float h = (float)viewport_wh.y;
      float h2 = h * 0.5f;

      // Subtract from viewport center
      _b2RasterQuad._min.x -= w2;
      _b2RasterQuad._max.x -= w2;

      // Invert text to show rightsize up and divide by perspective
      _b2RasterQuad._min.x = _b2RasterQuad._min.x / w2;
      _b2RasterQuad._min.y = (h2 - _b2RasterQuad._min.y - 1) / h2;
      _b2RasterQuad._max.x = _b2RasterQuad._max.x / w2;
      _b2RasterQuad._max.y = (h2 - _b2RasterQuad._max.y - 1) / h2;
    }
    protected virtual void PerformLayout(MegaTex mt, vec2 viewport_wh, bool bForce, vec2 parentMaxWH)
    {
      //Build the UI depth-first. Children elements are built, then we add those
      //positions to the parent to build the elements.
      if (LayoutChanged)
      {
        vec2 contentWH = new vec2(_padLeft + _padRight, _padTop + _padBot); //in HTML all elements default to zero width without any contents.

        //Shrink boundary
        vec2 maxWH = new vec2(
          Math.Max(Math.Min(parentMaxWH.x, MaxWHPX.x), 0),
          Math.Max(Math.Min(parentMaxWH.y, MaxWHPX.y), 0)
        );

        //Check if we are a label
        if (_bTextChanged == true)
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

        //Note: content quad may expand beyond container
        _b2ContentQuad._min = new vec2(_left, _top);
        _b2ContentQuad._max = _b2ContentQuad._min + contentWH;

        if (SizeModeWidth == UiSizeMode.Shrink)
        {
          _width = Math.Min(Math.Max(MinWHPX.x, Math.Min(maxWH.x, contentWH.x)), MaxWHPX.x);
        }
        else if (SizeModeWidth == UiSizeMode.Expand)
        {
          if (this._parent != null && this._parent.TryGetTarget(out var par))
          {
            _width = par._width - _left;
          }
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
        if (SizeModeHeight == UiSizeMode.Shrink)
        {
          _height = Math.Min(Math.Max(MinWHPX.y, Math.Min(maxWH.y, contentWH.y)), MaxWHPX.y);
        }
        else if (SizeModeHeight == UiSizeMode.Expand)
        {
          if (this._parent != null && this._parent.TryGetTarget(out var par))
          {
            _height = par._height - _top;
          }
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }

        //Shrink the element (if not UiScreen)
        //This is how HTML works by default.
        //Also apply min/max to the element. This is specifically how you would fix an element's size.

        LayoutChanged = false;
      }
    }
    private void LayoutEleQuad(vec2 viewport_wh, UiElement ele)
    {
      //Add the child to the parent.
      float fr, fl, ft, fb;
      ft = _b2ComputedQuad._min.y + ele._top;//top
      fr = _b2ComputedQuad._min.x + ele._right;//right
      fb = _b2ComputedQuad._min.y + ele._bottom;//bot
      fl = _b2ComputedQuad._min.x + ele._left;//left

      if (fl > fr || ft > fb)
      {
        int nnn = 0;
        nnn++;
      }

      ele.ComputeQuads(viewport_wh, ft, fr, fb, fl);
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
            ele._b2ComputedQuadLast = ele._b2ComputedQuad;

            if (ele.PositionMode == UiPositionMode.Static)
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
            else if (ele.PositionMode == UiPositionMode.Relative)
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
            LayoutEleQuad(viewport_wh, ele);
          }
        }
      }
    }
    private void LayoutLayer(List<UiElement> stats, vec2 maxWH, ref vec2 contentWH)
    {
      // Calc width with all static blocks using 0 width for autos (expandable blocks).
      List<UiLine> vecLines = new List<UiLine>();
      vecLines.Add(new UiLine(_padLeft, _padTop));
      foreach (var ele in stats)
      {
        CalcStaticElement(ele, vecLines, 0.0f, 0.0f, maxWH);
      }
      float totalHeight = _padTop + _padBot;
      foreach (var line in vecLines)
      {
        totalHeight += line._height;
        contentWH.x = Math.Max(contentWH.x, line._width + _padLeft + _padRight);
      }
      contentWH.y = Math.Max(contentWH.y, totalHeight);
    }
    private void CalcStaticElement(UiElement ele, List<UiLine> vecLines, float fAutoWidth, float fAutoHeight, vec2 parentMaxWH)
    {
      if (vecLines.Count == 0)
      {
        Gu.BRThrowException("GUI error - tried to run calc algorithm without any UILines created");
      }
      UiLine line = vecLines[vecLines.Count - 1];

      float parent_contentarea_width = parentMaxWH.x - _padLeft - _padRight;

      //*Padding
      float mt = ele._marTop;
      float mr = ele._marRight;
      float mb = ele._marBot;
      float ml = ele._marLeft;

      float ele_width = Math.Max(ele._width, ele.MinWHPX.x);

      //**Line break
      bool bLineBreak = false;
      if (ele.DisplayMode == UiDisplayMode.Inline)
      {
        if (ml + mr + ele_width + line._width > parent_contentarea_width) //For label - auto width + expand. ?? 
        {
          bLineBreak = true;
        }
      }
      else if (ele.DisplayMode == UiDisplayMode.Block)
      {
        //For /n in text. or block elements
        bLineBreak = true;
      }
      else if (ele.DisplayMode != UiDisplayMode.InlineNoWrap)
      {
        bLineBreak = false;
      }

      if (bLineBreak)
      {
        // new line
        UiLine line2 = new UiLine(_padLeft, 0/*pad top, only for the top uiline*/);
        line2._top = line._top + line._height;
        vecLines.Add(line2);
        line = vecLines[vecLines.Count - 1];
      }

      line._width += ml;
      ele._left = line._left + line._width;
      ele._top = line._top + mt;
      line._width += ele_width;
      line._width += mr;

      ele.ValidateQuad();

      // Increse line height WITH PAD
      line._height = Math.Max(line._height, Math.Max(ele._height + mt + mb, ele.MinWHPX.y));

      line._eles.Add(ele);
    }
    private void ComputePositionalElement(UiElement ele)
    {
      ValidateQuad();
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
      if (_cachedFont == null || mt.GetFont(FontFace) != _cachedFont)
      {
        font = mt.GetFont(this.FontFace);
      }
      else
      {
        font = _cachedFont;
      }

      float fontHeight = FontSize;
      var patch = font.SelectFontPatchInfo(fontHeight);
      if (patch == null)
      {
        return;
      }
      CachedCharData ccd = new CachedCharData();

      int index = 0;
      foreach (int cc in _strText)
      {
        int ccNext = (index + 1) < _strText.Length ? _strText[index++] : 0;
        float adv = font.GetKernAdvanceWidth(patch, FontSize, cc, ccNext);
        if (adv != 0)
        {
          int n = 0;
          n++;
        }

        patch.GetChar(cc, fontHeight, out ccd);

        //TODO: this should be UiElementBase, for simplicity. UiElement is too huge.
        UiElement e = new UiElement();
        e._pickEnabled = false;
        e._texture = new MtTex(null, 0);
        e._texture.SetWH(patch.TextureWidth, patch.TextureHeight);
        e._renderOffset = new Box2f(new vec2(ccd.left, ccd.top), new vec2(ccd.right, ccd.bot));
        e._texture.uv0 = ccd.uv0;
        e._texture.uv1 = ccd.uv1;
        e._left = 0;
        e._top = 0;
        e._minWHPX = new vec2(ccd.width, ccd.height * LineHeight);
        e._marRight = ccd.marginRight + adv;
        //e.WidthPX = ccd.width;
        //e.HeightPX = ccd.height * LineHeight;
        e._positionMode = UiPositionMode.Static;
        if (cc == '\n')
        {
          e._displayMode = UiDisplayMode.Block;
        }
        else
        {
          e._displayMode = UiDisplayMode.Inline;
        }
        e._fontColor = FontColor;
        e.ValidateQuad();
        AddChild(e, c_GlyphLayerSort);
      }
    }

    #endregion

  }//UiElement
  public class UiStyle
  {
    //Purpose of this class is to encapsulate color + texture
    //The problem with the last system is that there was no default color, so we had to texture every element ugh.
    public string ClassName = "";
    public vec4? FontColor = null;
    public vec4? Color = null;
    public MtTex Texture = null;
    public float? PadTop = null;
    public float? PadRight = null;
    public float? PadBot = null;
    public float? PadLeft = null;
    public float? MarginTop = null;
    public float? MarginRight = null;
    public float? MarginBot = null;
    public float? MarginLeft = null;
    public FontFace? FontFace = null;
    public FontStyle? FontStyle = null;
    public float? FontSize = null;
    public float? LineHeight = null; // % of line height (for tall fonts)
    public UiStyle Clone()
    {
      UiStyle ret = new UiStyle();
      ret.ClassName = this.ClassName;
      ret.FontColor = this.FontColor;
      ret.Color = this.Color;
      ret.Texture = this.Texture;
      ret.PadTop = this.PadTop;
      ret.PadRight = this.PadRight;
      ret.PadBot = this.PadBot;
      ret.PadLeft = this.PadLeft;
      ret.MarginTop = this.MarginTop;
      ret.MarginRight = this.MarginRight;
      ret.MarginBot = this.MarginBot;
      ret.MarginLeft = this.MarginLeft;
      ret.FontFace = this.FontFace;
      ret.FontStyle = this.FontStyle;
      ret.FontSize = this.FontSize;
      ret.LineHeight = this.LineHeight;
      return ret;
    }
  }
  public class UiStyleSheet
  {
    //The purpose of skins is to allow us to texture the UI in the future.
    //For now, we can use the 1 pixel texture and a default color (easier) 
    public enum StyleState
    {
      Default,
      Up,
      Down,
      Hover,
    }
    public UiStyle CurrentStyle = null;
    public MegaTex MegaTex { get; private set; } = null;
    public Dictionary<string, UiStyle> Classes = null; // <class, <state, style>>
    public UiStyle DefaultStyle = null;
    public UiStyle DefaultHoverStyle = null;
    public UiStyle DefaultDownStyle = null;
    public UiStyleSheet(MegaTex tex)
    {
      if (tex.DefaultPixel == null)
      {
        Gu.BRThrowException("Default pixel for UI megatex must be set.");
      }
      MegaTex = tex;

      DefaultStyle = new UiStyle()
      {
        ClassName = "default",
        FontColor = vec4.rgba_ub(10, 10, 12, 1),
        Color = vec4.rgba_ub(150, 150, 150, 255),
        Texture = tex.DefaultPixel, //Flat color
        PadTop = 5,
        PadRight = 5,
        PadBot = 5,
        PadLeft = 5,
        MarginTop = 0,
        MarginRight = 0,
        MarginBot = 0,
        MarginLeft = 0,
        FontFace = FontFace.Mono,
        FontStyle = FontStyle.Normal,
        FontSize = 22,
        LineHeight = 1.0f
      };
      DefaultDownStyle = DefaultStyle.Clone();
      DefaultDownStyle.Color = new vec4(DefaultDownStyle.Color.Value.xyz() * 0.7f, 1);
      DefaultDownStyle.FontColor = vec4.rgba_ub(190, 190, 190, 1);

      DefaultHoverStyle = DefaultStyle.Clone();
      DefaultHoverStyle.Color = new vec4(DefaultHoverStyle.Color.Value.xyz() * 1.3f, 1);
      DefaultHoverStyle.FontColor = vec4.rgba_ub(30, 30, 32, 1);

      Classes = new Dictionary<string, UiStyle>();
      Classes.Add(DefaultStyle.ClassName, DefaultStyle);
    }
  }
  public class UiScreen : UiElement
  {
    private WeakReference<Camera3D> _camera = new WeakReference<Camera3D>(null);
    private UiDragInfo _dragInfo = null;//wer're putting this here...there's only ened for one draginfo..

    public UiScreen(Camera3D cam)
    {
      _camera = new WeakReference<Camera3D>(cam);
      int designWidth = 1920;
      int designHeight = 1080;
      _top = 0;
      _left = 0;
      _width = designWidth - 1;
      _height = designHeight - 1;
      _maxWHPX = new vec2(designWidth, designHeight);//Make sure stuff doesn't go off the screen.
      _minWHPX = new vec2(0, 0);
      _sizeModeWidth = _sizeModeHeight = UiSizeMode.Expand;
    }
    private void SetExtentsToViewport(Camera3D cam)
    {
      _top = cam.Viewport_Y;
      _left = cam.Viewport_X;
      _width = cam.Viewport_Width - cam.Viewport_X - 1;
      _height = cam.Viewport_Height - cam.Viewport_Y - 1;
      _maxWHPX = new vec2(cam.Viewport_Width, cam.Viewport_Height);//Make sure stuff doesn't go off the screen.
      _minWHPX = new vec2(cam.Viewport_X, cam.Viewport_Y);
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

        UpdateMesh(wo);

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

    }
    private void UpdateLayout(MegaTex mt, WorldObject wo, PCMouse mouse, Camera3D cam)
    {
      if (LayoutChanged)
      {
        vec2 viewport_wh = new vec2(cam.Viewport_Width, cam.Viewport_Height);

        // Gui2d doesn't have a parent, so we have to compute the quads to create a valid clip region.
        ComputeQuads(viewport_wh, _top, _right, _bottom, _left);

        PerformLayout(mt, viewport_wh, false, this.MaxWHPX);
      }
    }
    private void UpdateMesh(WorldObject wo)
    {
      Box2f b = _b2RasterQuad;// getGLRasterQuad();
      List<v_v4v4v4v2u2> verts = new List<v_v4v4v4v2u2>();

      RegenMesh(verts, b, 0);

      wo.Mesh = new MeshData("gui_mesh", OpenTK.Graphics.OpenGL4.PrimitiveType.Points,
      Gpu.CreateVertexBuffer(verts.ToArray()),
      false
      );
      wo.Mesh.DrawOrder = DrawOrder.Last;
    }
  }
  public enum FontStyle
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
      _megaTex = new MegaTex("gui_megatex", true, 128);
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
    private UiElement CreateStyledElement(string name)
    {
      UiElement e = new UiElement(name);
      if (_styleSheet != null)
      {
        e.ApplyStyle(_styleSheet.DefaultStyle);
      }
      return e;
    }
    public UiElement CreatePanel(string name, vec2? pos, vec2? wh)
    {
      UiElement e = CreateStyledElement(name);
      e.Texture = _megaTex.DefaultPixel;
      if (pos != null)
      {
        e.Pos = pos.Value;
        e.PositionMode = UiPositionMode.Relative;
      }
      if (wh != null)
      {
        e.Extent = wh.Value;
      }
      return e;
    }
    public UiElement CreateButton(string name, vec2? pos, string text, Action<UiEventId, UiElement, PCMouse> onClick = null)
    {
      UiElement e = CreateStyledElement(name);
      e.Texture = _megaTex.DefaultPixel;
      if (pos != null)
      {
        e.Pos = pos.Value;
        e.PositionMode = UiPositionMode.Relative;
      }
      e.Text = text;
      e.MaxWHPX = new vec2(100, 200);
      e.IsPickRoot = true;
      e.PadBot = e.PadLeft = e.PadTop = e.PadRight = 15;// Fonts are messed up right now 

      e.AddEvent(UiEventId.Mouse_Lmb_Release, (eid, uie, m) =>
      {
        e.ApplyStyle(this._styleSheet.DefaultHoverStyle);
      });
      if (onClick != null)
      {
        e.AddEvent(UiEventId.Mouse_Lmb_Release, onClick);
      }
      e.AddEvent(UiEventId.Mouse_Enter, (eid, uie, m) =>
      {
        e.ApplyStyle(this._styleSheet.DefaultHoverStyle);
      });
      e.AddEvent(UiEventId.Mouse_Leave, (eid, uie, m) =>
      {
        e.ApplyStyle(this._styleSheet.DefaultStyle);
      });
      e.AddEvent(UiEventId.Mouse_Lmb_Press, (eid, uie, m) =>
      {
        e.ApplyStyle(this._styleSheet.DefaultDownStyle);
      });

      return e;
    }
    public UiElement CreateLabel(string name, vec2? pos, string text, bool showbackground = true, FontFace? font = null, float fontSize = 12, vec4? fontColor = null, FontStyle fontstyle = FontStyle.Normal, float lineheight = 1.0f)
    {
      UiElement e = CreateStyledElement(name);
      e.Texture = showbackground ? _megaTex.DefaultPixel : null;
      if (pos != null)
      {
        e.Pos = pos.Value;
        e.PositionMode = UiPositionMode.Relative;
      }
      e.Text = text;
      e.FontFace = font != null ? font : FontFace.Mono;
      e.FontSize = fontSize;
      e.FontColor = fontColor != null ? fontColor.Value : new vec4(0, 0, 0, 1);
      e.FontStyle = fontstyle;
      e.LineHeight = lineheight;
      e.PadBot = e.PadLeft = e.PadTop = e.PadRight = 15;// Fonts are messed up right now 
      e.PadBot = 10;
      e.PadTop = 10;
      e.PadLeft = 10;
      e.PadRight = 10;
      e.IsPickRoot = true;
      return e;
    }
  }//class Gui

}//Namespace Pri
