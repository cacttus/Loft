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
    InlineWrap,
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
    Mouse_Leave,
    Tick, //tick event like every x ms
  };
  public enum UiOverflowMode
  {
    Show,
    Hide
  };
  public enum UiImageSizeMode
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
  public class UiElementBase
  {
    //We separate this from Uielement because of Child elements / Glyphs.
    // There are a ton of them, and they don't need all the extra information.
    // Glyphs need margin, but no padding. 
    // We MAY put color in the glyph.

    public float Top { get { return _top; } set { _top = value; } }
    public float Left { get { return _left; } set { _left = value; } }
    public float Right { get { return _left + _width; } }
    public float Bottom { get { return _top + _height; } }
    public float WidthPX
    {
      get { return _width; }
      set
      {
        _width = value;
      }
    }
    public float HeightPX { get { return _height; } set { _height = value; } }
    public vec4 Color { get; set; } = new vec4(1, 1, 1, 1);
    public vec2 Pos { get { return new vec2(_left, _top); } set { _left = value.x; _top = value.y; } }
    public vec2 Extent { get { return new vec2(_width, _height); } set { _width = value.x; _height = value.y; } }

    protected float _top = 50;
    protected float _left = 50;
    protected float _width = 100;
    protected float _height = 100;

    // Optional render area, relative to the CENTER of the layed out quad. so left + (right-left)/2, top + (bot-top)/2, 
    // if Null, the quad is rendered at the exact top/left + width/height. 
    // This is specifically for text glyphs, but can be used for anything .
    public Box2f? _renderOffset = null;
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
  public class UiElement : UiElementBase
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

    public MultiMap<int, UiElement> Children { get; set; } = null;
    public UiPositionMode PositionMode { get; set; } = UiPositionMode.Static;
    public UiOverflowMode OverflowMode { get; set; } = UiOverflowMode.Hide;
    public UiDimUnit WidthUnit { get; set; } = UiDimUnit.Pixel;
    public UiDimUnit HeightUnit { get; set; } = UiDimUnit.Pixel;
    public float WidthPct { get; set; } = 100;// this is only set if th DimUnit is set to percent
    public float HeightPct { get; set; } = 100;// this is only set if th DimUnit is set to percent
    public vec2 MinWHPX { get; set; } = new vec2(0, 0);
    public vec2 MaxWHPX { get; set; } = new vec2(9999999, 9999999);
    public bool LayoutChanged { get; private set; } = true;
    public bool LayoutVisible { get; set; } = true;
    public bool RenderVisible { get; set; } = true;
    public bool IsPickRoot { get; set; } = true;
    public bool PickEnabled { get; set; } = true;
    public bool ScaleToDesign { get; set; } = true; // this is true for all elements besides cursor.
    public UiDisplayMode DisplayMode { get; set; } = UiDisplayMode.Block;
    public UiImageSizeMode SizeModeX { get; set; } = UiImageSizeMode.Expand;  // tile = GL_REPEAT, Clamp = GL_CLAMP, Expand - expand tex coords.
    public UiImageSizeMode SizeModeY { get; set; } = UiImageSizeMode.Expand;  // tile = GL_REPEAT, Clamp = GL_CLAMP, Expand - expand tex coords.
    public UiDimUnit MarginUnitTop { get; set; } = UiDimUnit.Pixel;
    public UiDimUnit MarginUnitRight { get; set; } = UiDimUnit.Pixel;
    public UiDimUnit MarginUnitBot { get; set; } = UiDimUnit.Pixel;
    public UiDimUnit MarginUnitLeft { get; set; } = UiDimUnit.Pixel;
    public float MarginTop { get { return _marTop; } set { _marTop = value; } }
    public float MarginRight { get { return _marRight; } set { _marRight = value; } }
    public float MarginBot { get { return _marBot; } set { _marBot = value; } }
    public float MarginLeft { get { return _marLeft; } set { _marLeft = value; } }
    public float PadTop { get { return _padTop; } set { _padTop = value; } }
    public float PadRight { get { return _padRight; } set { _padRight = value; } }
    public float PadBot { get { return _padBot; } set { _padBot = value; } }
    public float PadLeft { get { return _padLeft; } set { _padLeft = value; } }
    public string Text { get { return _strText; } set { _strText = value; SetLayoutChanged(); _bTextChanged = true; } }
    public float LineHeight { get; set; } = 1;
    public FontFace FontFace { get; set; } = FontFace.Mono;
    public float FontSize { get; set; } = 12;
    public FontStyle FontStyle { get; set; } = FontStyle.Normal;
    public vec4 FontColor { get; set; } = new vec4(0, 0, 0, 1);
    public MtTex Texture
    {
      get
      {
        return _texture;
      }
      set
      {
        _texture = value;
        if (_texture == null)
        {
          _iPickId = 0;
        }
        else
        {
          _iPickId = Gu.Context.Renderer.getPicker().genPickId();
        }
      }
    }

    #endregion
    #region Private: Members

    protected const int c_BaseLayerSort = 1000;
    protected const int c_GlyphLayerSort = 2000;
    private MtTex _texture = null;
    private WeakReference<UiElement> _parent = null;
    private MtFont _cachedFont = null;
    private string _strText = "";
    private bool _bTextChanged = false;
    private Box2f _q2Tex = new Box2f(0, 0, 1, 1);//0,1
    private vec2 _tileScale = new vec2(1, 1); //Scale if UiImageSizeMode is Tile
    private FontPatchInfo _patch = null;
    private Box2f _b2ComputedQuad = new Box2f();      // Base quad that is calculated from _right/_left.., parent, padding, etc
    private Box2f _b2ComputedQuadLast = new Box2f();
    private Box2f _b2ContentQuad = new Box2f();       // Encompassess ALL of the child quads, plus overflow.
    private Box2f _b2LayoutQuad = new Box2f();        // Transformed from design space into screen space.
    protected Box2f _b2RasterQuad = new Box2f();      // Final raster quad in OpenGL screen coordinates.
    private uint _iPickId = 0;
    private UiDragInfo _dragInfo = null;
    protected bool _bPickedLastFrame;
    private UiMouseState _eMouseStateLast = UiMouseState.None;
    private UiMouseState _eMouseState = UiMouseState.None;
    private long _iPickedFrameId = 0;
    private Dictionary<UiEventId, List<Action<UiEventId, UiElement, PCMouse>>> _events = null;//EventId, list of action (EventId, Object)
    protected float _padTop = 0;
    protected float _padRight = 0;
    protected float _padBot = 0;
    protected float _padLeft = 0;
    protected float _marTop = 0;
    protected float _marRight = 0;
    protected float _marBot = 0;
    protected float _marLeft = 0;
    protected bool _bShrinkToContents = true;//only for uiscreen

    #endregion
    #region Public: Methods
    public UiElement()
    {
      //Note: if no Texture is set, the element will not be rendered (i.e. it's a container)
    }
    public UiElement(UiStyle style)
    {
      ApplyStyle(style);
    }
    public virtual void Update(MegaTex mt, WorldObject wo, WindowContext wc)
    {
      if (LayoutVisible == false || RenderVisible == false)
      {
        return;
      }
      _bPickedLastFrame = false;

      // Update various events.  Dragging
      if (_dragInfo != null)
      {
        _dragInfo.Update(wc.PCMouse);
      }

      // Recur
      if (Children != null)
      {
        foreach (var p in Children)
        {
          p.Value.Update(mt, wo, wc);
        }
      }
    }
    public void AddChild(UiElement e, int sort = c_BaseLayerSort)
    {
      if (e._parent != null && e._parent.TryGetTarget(out UiElement p))
      {
        p.RemoveChild(e);
      }
      if (Children == null)
      {
        Children = new MultiMap<int, UiElement>();
      }
      Children.Add(sort, e);
      e._parent = new WeakReference<UiElement>(this);
    }
    public bool RemoveChild(UiElement e)
    {
      if (Children != null)
      {
        foreach (var k in Children.Keys)
        {
          if (Children.Remove(k, e))
          {
            e._parent = null;
            return true;
          }
        }
        if (Children.Count == 0)
        {
          Children = null;
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
    public void EnableDrag(Action<vec2> func)
    {
      _dragInfo = new UiDragInfo(func);
      AddEvent(UiEventId.Mouse_Lmb_Press, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        ele._dragInfo._bDragStart = true;
        ele._dragInfo._vDragStart = m.Pos;
      });

      AddEvent(UiEventId.Mouse_Lmb_Release, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        ele._dragInfo._bDrag = false;
      });

      AddEvent(UiEventId.Mouse_Lmb_Up, (UiEventId evId, UiElement ele, PCMouse m) =>
      {
        ele._dragInfo._bDrag = false;
      });
    }
    public void ValidateQuad()
    {
      if ((Right < Left) || (Bottom < Top))
      {
        Gu.Log.Error("Computed Quad is invalid, rtbl= " + Right + "," + Left + "," + Bottom + "," + Top + ".");
        Gu.DebugBreak();
      }
    }
    public T GetFirstChild<T>() where T : UiElement
    {
      Gu.Assert(Children.Count > 0);
      return (T)Children.First().Value;
    }
    public virtual void ApplyStyle(UiStyle style)
    {
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
    protected virtual void RegenMesh(List<v_v4v4v4v2u2> verts, Box2f b2ClipRect)
    {
      if (LayoutVisible)
      {
        if (RenderVisible)
        {
          GetOpenGLQuadVerts(verts, b2ClipRect);
          if (Children != null)
          {
            foreach (var p in Children)
            {
              UiElement ele = p.Value;

              if (ele.IsFullyClipped(b2ClipRect) == false)
              {
                Box2f clip = ShrinkClipRect(b2ClipRect);
                ele.RegenMesh(verts, clip);
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
    private protected void GetOpenGLQuadVerts(List<v_v4v4v4v2u2> verts, Box2f b2ClipRect)
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

      if (SizeModeX == UiImageSizeMode.Expand)
      {
        _q2Tex._min.x = Texture.uv0.x;
        _q2Tex._max.x = Texture.uv1.x;
      }
      else if (SizeModeX == UiImageSizeMode.Tile)
      {
        float wPx = WidthPX;
        _q2Tex._min.x = Texture.uv0.x;
        _q2Tex._max.x = Texture.uv1.x + (Texture.uv1.x - Texture.uv0.x) * _tileScale.x;
      }
      else if (SizeModeX == UiImageSizeMode.Computed)
      {
        // Tex coords are computed (used for UiGlyph)
      }
      else
      {
        Gu.Log.Error("Invalid layout X image size mode.");
      }

      if (SizeModeY == UiImageSizeMode.Expand)
      {
        _q2Tex._min.y = Texture.uv0.y;
        _q2Tex._max.y = Texture.uv1.y;
      }
      else if (SizeModeY == UiImageSizeMode.Tile)
      {
        float hPx = HeightPX;
        _q2Tex._min.y = Texture.uv0.y;
        _q2Tex._max.y = Texture.uv1.y + (Texture.uv1.y - Texture.uv0.y) * _tileScale.y;
      }
      else if (SizeModeY == UiImageSizeMode.Computed)
      {
        // Tex coords are computed (used for UiGlyph)
      }
      else if (SizeModeY == UiImageSizeMode.Proportion)
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
         _iPickId,
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
    private protected bool Pick(PCMouse mous, long frameStamp)
    {
      //Recursive method to pick visible elements.
      bool picked = false;
      Box2f q = _b2LayoutQuad;
      if (LayoutVisible == true)
      {
        if (RenderVisible == true)
        {
          if (PickEnabled == true)
          {
            if (q.ContainsPointInclusive(mous.Pos))
            {

              //Pick texture
              if (_iPickId > 0)
              {
                var pixid = Gu.Context.Renderer.getPicker().getSelectedPixelId();
                if (pixid != 0)
                {
                  if (pixid == _iPickId)
                  {
                    _iPickedFrameId = frameStamp;
                    picked = true;
                  }
                }
              }

              bool pickedChild = false;
              if (!picked)
              {
                // Else pick all children to find the pick root
                if (Children != null)
                {
                  foreach (var ele in Children)
                  {
                    pickedChild = ele.Value.Pick(mous, frameStamp);
                    if (pickedChild == true)
                    {
                      break;
                    }
                  }
                }
              }

              // Capture the bubble-up pick event and process events
              if ((picked || pickedChild) && IsPickRoot)
              {
                DoMouseEvents(mous);
              }
            }
          }
        }
      }
      _bPickedLastFrame = picked;
      return picked;
    }
    private void DoMouseEvents(PCMouse mouse)
    {
      // This caching thing doesn't work because for hover states we need to call teh event every fraem
      //**This is also probably why scrollbar was sluggish
      // This seems like the moste fficient way to do this.
      ButtonState eLmb = mouse.GetButtonState(MouseButton.Left);
      ButtonState eRmb = mouse.GetButtonState(MouseButton.Right);
      ButtonState eMmb = mouse.GetButtonState(MouseButton.Middle);

      DoMouseEvent(mouse, UiEventId.Mouse_Lmb_Up, eLmb, ButtonState.Up);
      DoMouseEvent(mouse, UiEventId.Mouse_Lmb_Hold, eLmb, ButtonState.Hold);
      DoMouseEvent(mouse, UiEventId.Mouse_Lmb_Press, eLmb, ButtonState.Press);
      DoMouseEvent(mouse, UiEventId.Mouse_Lmb_Release, eLmb, ButtonState.Release);

      DoMouseEvent(mouse, UiEventId.Mouse_Lmb_Up, eRmb, ButtonState.Up);
      DoMouseEvent(mouse, UiEventId.Mouse_Lmb_Hold, eRmb, ButtonState.Hold);
      DoMouseEvent(mouse, UiEventId.Mouse_Lmb_Press, eRmb, ButtonState.Press);
      DoMouseEvent(mouse, UiEventId.Mouse_Lmb_Release, eRmb, ButtonState.Release);
    }
    private void DoMouseEvent(PCMouse m, UiEventId evid, ButtonState curstate, ButtonState evstate)
    {
      if (curstate == evstate)
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
        if (Children != null)
        {

          //PerformLayoutChildren
          //First pass must expand autosize elements up to the maximum w/h
          foreach (var p in Children)
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
        _b2ContentQuad._min = new vec2(Left, Top);
        _b2ContentQuad._max = _b2ContentQuad._min + contentWH;

        if (_bShrinkToContents)//not- this is only for uiscreen
        {
          contentWH.x = Math.Min(maxWH.x, contentWH.x);
          contentWH.y = Math.Min(maxWH.y, contentWH.y);

          WidthPX = Math.Min(Math.Max(MinWHPX.x, contentWH.x), MaxWHPX.x);
          HeightPX = Math.Min(Math.Max(MinWHPX.y, contentWH.y), MaxWHPX.y);
        }

        LayoutChanged = false;
      }
    }
    // private void ComputeContentQuad(vec2 contentWH)
    // {
    //   // Reset content quad to 0,0
    //   _b2ContentQuad._min = _b2ContentQuad._max = new vec2(Left, Top);

    //   float dbgwidth = WidthPX;
    //   float dbgheight = HeightPX;
    //   // Recur and compute bounds for all children
    //   float fright = -float.MaxValue;
    //   float fbottom = -float.MaxValue;
    //   if (Children != null)
    //   {
    //     foreach (var p in Children)
    //     {
    //       var ele = p.Value;

    //       if (ele.LayoutVisible)
    //       {
    //         // Add padding for static elements
    //         float effR = ele.Right;
    //         float effT = ele.Bottom;
    //         if (ele.PositionMode == UiPositionMode.Static)
    //         {
    //           effR -= (ele.MarginLeft + ele.MarginRight);
    //         }
    //         if (ele.PositionMode == UiPositionMode.Static)
    //         {
    //           effT -= (ele.MarginTop + ele.MarginBot);
    //         }

    //         // w/h adjust
    //         effR -= 1;
    //         effT -= 1;

    //         fright = Math.Max(fright, Left + effR);
    //         fbottom = Math.Max(fbottom, Top + effT);

    //         // expand contenet quad
    //         _b2ContentQuad.ExpandByPoint(new vec2(fright, fbottom));
    //       }
    //     }
    //   }
    // }
    private void LayoutEleQuad(vec2 viewport_wh, UiElement ele)
    {
      //Add the child to the parent.
      float fr, fl, ft, fb;
      ft = _b2ComputedQuad._min.y + ele.Top;//top
      fr = _b2ComputedQuad._min.x + ele.Right;//right
      fb = _b2ComputedQuad._min.y + ele.Bottom;//bot
      fl = _b2ComputedQuad._min.x + ele.Left;//left

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
      if (Children != null && Children.Count > 0)
      {
        int uiLast = int.MaxValue;
        List<UiElement> bucket = new List<UiElement>();
        foreach (var p in Children)
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
        foreach (var p in Children)
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
      float totalHeight = _padBot + _padTop;
      foreach (var line in vecLines)
      {
        totalHeight += line._height;
        contentWH.x = Math.Max(contentWH.x, line._width + _padLeft + _padRight);
      }
      contentWH.y = Math.Max(contentWH.y, totalHeight);

      //contentWH.y = Math.Max(fTotalHeight, contentWH.y);

      // // auto heights are confusing.  So the problem is that the height of a line is indeterminate.  We build the form by widths of elements and
      // // wrap when we reach the end.   What's the actual height of the line?  What we say is *Any line that has at least one
      // //*auto height is an auto height line, thus the whole line's height gets auto*
      // // 3/4 technically auto height should always shrink whether we are inline, or block element
      // int nAutoHLines = 0;
      // for (int iLine = 0; iLine < vecLines.Count; iLine++)
      // {
      //   var line = vecLines[iLine];
      //   foreach (var ele in line._eles)
      //   {
      //     if (ele.HeightUnit == UiDimUnit.Auto)
      //     {
      //       nAutoHLines++;
      //       break;  // Line height is auto for this line
      //     }
      //   }
      // }

      // // Expand Autos
      // // in css block elements expand "grow" to 100% of parent
      // // inline elements shrink to smallest size.  our auto is somewhat broken
      // // Hmm..Auto size = grow would cause autos to expand to be the size of the max width..which defaults to 99999
      // List<UiLine> vecLines2 = new List<UiLine>();  // Technically this dummy variable should be '1'
      // vecLines2.Add(new UiLine());
      // for (var iLine = 0; iLine < vecLines.Count; iLine++)
      // {
      //   var line = vecLines[iLine];

      //   // Sum Autos and average widths
      //   int nAutoWsLine = 0;
      //   foreach (var ele in line._eles)
      //   {
      //     if (ele.WidthUnit == UiDimUnit.Auto)
      //     {
      //       nAutoWsLine++;
      //     }
      //   }
      //   float auto_width = 0;
      //   float auto_height = 0;
      //   if (nAutoWsLine > 0)
      //   {
      //     auto_width = Math.Max(WidthPX - line._width, 0.0f) / (float)nAutoWsLine;
      //     // So we're going to "sprinkle the width" across other autos
      //     //  for each element whose min width > the computed auto
      //     // subtract from the computed_auto the remaining width minus the auto  (min_width - computed_auto) / remaining_autos4
      //   }
      //   if (nAutoHLines > 0)
      //   {
      //     auto_height = Math.Max(HeightPX - fTotalHeight, 0.0f) / (float)nAutoHLines;
      //   }

      //   // run calculation again, this time with autos
      //   foreach (var ele in line._eles)
      //   {
      //     CalcStaticElement(ele, vecLines2, auto_width, auto_height);
      //   }
      // }

    }
    private void CalcStaticElement(UiElement ele, List<UiLine> vecLines, float fAutoWidth, float fAutoHeight, vec2 parentMaxWH)
    {
      if (vecLines.Count == 0)
      {
        Gu.BRThrowException("GUI error - tried to run calc algorithm without any UILines created");
      }
      UiLine line = vecLines[vecLines.Count - 1];

      // Autos get zero first so we an compute the fixed
      // statics then we go throguh here again and assign them computed values

      //float wpx = 0, hpx = 0;
      //ele.ComputeWH(ref wpx, ref hpx);  // also applies min/max
      // if (ele.WidthUnit == UiDimUnit.Auto)
      // {
      //   wpx = fAutoWidth;
      // }
      // if (ele.HeightUnit == UiDimUnit.Auto)
      // {
      //   hpx = fAutoHeight;
      // }
      //ele.ApplyMinMax(ref wpx, ref hpx); //minimax for element only.

      float parent_contentarea_width = parentMaxWH.x - _padLeft - _padRight;

      //*Padding
      float mt = ComputeMarginPad_Unit(HeightPX, ele.MarginUnitTop, ele._marTop);
      float mr = ComputeMarginPad_Unit(WidthPX, ele.MarginUnitRight, ele._marRight);
      float mb = ComputeMarginPad_Unit(HeightPX, ele.MarginUnitBot, ele._marBot);
      float ml = ComputeMarginPad_Unit(WidthPX, ele.MarginUnitLeft, ele._marLeft);

      float wpx_mar = ml + mr;
      float hpx_mar = mb + mt;

      //**Line break
      bool bLineBreak = false;
      if (ele.DisplayMode == UiDisplayMode.InlineWrap)
      {
        if (wpx_mar + line._width > parent_contentarea_width) //For label - auto width + expand. ?? 
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
        UiLine line2 = new UiLine(_padLeft, _padTop);
        line2._top = line._top + line._height;
        vecLines.Add(line2);
        line = vecLines[vecLines.Count - 1];
      }

      line._width += ml;
      ele._left = line._left + line._width;
      ele._top = line._top + mt;
      line._width += Math.Max(ele._width, ele.MinWHPX.x);
      line._width += mr;

      ele.ValidateQuad();

      // Increse line height WITH PAD
      line._height = Math.Max(line._height, Math.Max(ele._height + mt + mb, ele.MinWHPX.y));

      line._eles.Add(ele);
    }
    private static float ComputeMarginPad_Unit(float parentextent, UiDimUnit unit, float ud)
    {
      //You can't compute a % of a non-fixed size element UNTIL it has been computed.
      if (unit == UiDimUnit.Pixel)
      {
        return ud;
      }
      else if (unit == UiDimUnit.Percent)
      {
        return ud * 0.01f * parentextent;
      }
      else
      {
        Gu.Log.Error("Invalid value for margin unit");
        return 0;
      }
    }
    private void ComputePositionalElement(UiElement ele)
    {
      //*No padding
      //*No auto sizing
      //float wpx = 0, hpx = 0;
      //ele.ComputeWH(ref wpx, ref hpx);  // Cannot be auto
      //ele.ApplyMinMax(ref wpx, ref hpx);
      //ele.Right = ele.Left + wpx;
      //ele.Bottom = ele.Top + hpx;

      //this is nothing now, there is no right/bottom, 
      //it was nothing before, because left +wpx equals right anyway... so what?

      ValidateQuad();
    }
    // private void ComputeWH(ref float wpx, ref float hpx)
    // {
    //   wpx = 0;
    //   hpx = 0;

    //   // Width
    //   if (WidthUnit == UiDimUnit.Pixel)
    //   {
    //     wpx = WidthPX;//TODO: widths are fixed
    //   }
    //   else if (WidthUnit == UiDimUnit.Percent)
    //   {
    //     if (_parent != null && _parent.TryGetTarget(out var par))
    //     {
    //       wpx = par.WidthPX * WidthPct * 0.01f;
    //     }
    //   }
    //   else if (WidthUnit == UiDimUnit.Auto)
    //   {
    //   }
    //   else
    //   {
    //     Gu.Log.Error("Invalid enum");
    //   }

    //   // Height
    //   if (HeightUnit == UiDimUnit.Pixel)
    //   {
    //     hpx = HeightPX;
    //   }
    //   else if (HeightUnit == UiDimUnit.Percent)
    //   {
    //     if (_parent != null && _parent.TryGetTarget(out var par))
    //     {
    //       hpx = par.HeightPX * HeightPct * 0.01f;
    //     }
    //   }
    //   else if (HeightUnit == UiDimUnit.Auto)
    //   {
    //   }
    //   else
    //   {
    //     Gu.Log.Error("Invalid enum");
    //   }
    //   // Make sure it's an actual heght
    //   if (hpx < 0.0f)
    //   {
    //     hpx = 0.0f;
    //   }
    //   if (wpx < 0.0f)
    //   {
    //     wpx = 0.0f;
    //   }
    // }
    // private void ApplyMinMax(ref float wpx, ref float hpx)
    // {
    //   // apply min/max to box (not in parent space)
    //   if (wpx < MinWHPX.x)
    //   {
    //     wpx = MinWHPX.x;
    //   }
    //   if (hpx < MinWHPX.y)
    //   {
    //     hpx = MinWHPX.y;
    //   }
    //   if (wpx > MaxWHPX.x)
    //   {
    //     wpx = MaxWHPX.x;
    //   }
    //   if (hpx > MaxWHPX.y)
    //   {
    //     hpx = MaxWHPX.y;
    //   }
    // }
    private void CreateGlyphs(MegaTex mt)
    {
      if (Children == null)
      {
        Children = new MultiMap<int, UiElement>();
      }
      Children.Remove(c_GlyphLayerSort);

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
        //TODO: this should be UiElementBase, for simplicity. UiElement is too huge.
        UiElement e = new UiElement();
        e.Texture = new MtTex(null, 0);
        e.Texture.SetWH(patch.TextureWidth, patch.TextureHeight);

        patch.GetChar(cc, fontHeight, out ccd);

        e._renderOffset = new Box2f(new vec2(ccd.left, ccd.top), new vec2(ccd.right, ccd.bot));
        e.Texture.uv0 = ccd.uv0;
        e.Texture.uv1 = ccd.uv1;
        e.Left = 0;
        e.Top = 0;
        e.MinWHPX = new vec2(ccd.width, ccd.height * LineHeight);
        e.MarginRight = ccd.marginRight + adv;
        //e.WidthPX = ccd.width;
        //e.HeightPX = ccd.height * LineHeight;
        e.PositionMode = UiPositionMode.Static;
        if (cc == '\n')
        {
          e.DisplayMode = UiDisplayMode.Block;
        }
        else
        {
          e.DisplayMode = UiDisplayMode.InlineWrap;
        }
        e.Color = FontColor;
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
        FontColor = new vec4(0, 0, 0, 1),
        Color = vec4.rgba_ub(210, 220, 230, 255),
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

      Classes = new Dictionary<string, UiStyle>();
      Classes.Add(DefaultStyle.ClassName, DefaultStyle);
    }
    // public void StyleElement(UiElement e, string class_name = DefaultStyleClass)
    // {
    //   if (this.Classes.TryGetValue(class_name, out var style))
    //   {
    //     StyleElement(style, e);
    //   }
    // }
    // public void StyleElement(UiStyle style, UiLabel e)
    // {
    // }
    // public void StyleElement(UiStyle style, UiElement e)
    // {
    //   if (style.Texture == null)
    //   {
    //     e.Texture = MegaTex.DefaultPixel;//Flat color.
    //   }
    //   if (style.Color != null)
    //   {
    //     e.Color = style.Color.Value;
    //   }
    //   if (style.Padding != null)
    //   {
    //     e.PadTop = style.Padding.Value.x;
    //     e.PadRight = style.Padding.Value.y;
    //     e.PadBot = style.Padding.Value.z;
    //     e.PadLeft = style.Padding.Value.w;
    //   }
    // }
  }
  public class UiScreen : UiElement
  {
    public UiScreen()
    {
      //TODO:
      // Gu::getViewport()->getWidth();
      // Gu::getViewport()->getHeight();
      int designWidth = 1920;
      int designHeight = 1080;
      Top = 0;
      Left = 0;
      WidthPX = designWidth - 1;
      HeightPX = designHeight - 1;
      MaxWHPX = new vec2(designWidth, designHeight);//Make sure stuff doesn't go off the screen.
      MinWHPX = new vec2(0, 0);
      _bShrinkToContents = false;
    }
    public static int GetSortLayer(int n)
    {
      // Mnemonic wich gves you the base sort layer, provided n will return additional layers.
      return c_BaseLayerSort + n;
    }
    public override void Update(MegaTex mt, WorldObject wo, WindowContext ct)
    {
      base.Update(mt, wo, ct);  // Note: due to buttons resetting themselves update() must come before pick()

      // if (Gu::getFpsMeter()->frameMod(2)) {
      UpdateLayout(mt, wo, ct.PCMouse);
      // }

      // Updating this per frame to indicate if the GUI is picked.
      Pick(ct.PCMouse, ct.FrameStamp);
    }
    void UpdateLayout(MegaTex mt, WorldObject wo, PCMouse mouse)
    {
      // if (_pint->_pCursor)
      // {
      //   _pint->_pCursor->left() = pInputManager->getMousePos_Relative().x;
      //   _pint->_pCursor->top() = pInputManager->getMousePos_Relative().y;
      // }
      // else
      // {
      //   Gu.Log.Debug("Cursor was not set in UiScreen. (not necessarily an error, but a debug error).");
      // }

      if (LayoutChanged)
      {
        //HACK  - dropping a fixed viewport.
        vec2 viewport_wh = new vec2(Gu.World.Camera.Viewport_Width, Gu.World.Camera.Viewport_Height);

        // Gui2d doesn't have a parent, so we have to compute the quads to create a valid clip region.
        ComputeQuads(viewport_wh, Top, Right, Bottom, Left);

        PerformLayout(mt, viewport_wh, false, this.MaxWHPX);
        //_bDebugForceLayoutChange = false;
      }

      // Cursor
      // if (_pint->_pCursor)
      // {
      //   computePositionalElement(_pint->_pCursor.get());
      //   UiElement::layoutEleQuad(this, _pint->_pCursor.get());
      //   _pint->_pCursor->performLayout(this, false);
      // }

      UpdateMesh(wo);
    }
    void UpdateMesh(WorldObject wo)
    {
      Box2f b = _b2RasterQuad;// getGLRasterQuad();
      List<v_v4v4v4v2u2> verts = new List<v_v4v4v4v2u2>();

      RegenMesh(verts, b);

      // // CURSOR
      // if (_pint->_pCursor)
      // {
      //   b = getGLRasterQuad();
      //   _pint->_pCursor->regenMeshExposed(verts, inds, b);
      // }

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
    public GuiComponent()
    {
      _megaTex = new MegaTex("gui_megatex", true, 128);
      _styleSheet = new UiStyleSheet(_megaTex);

      GetFont(FontFace.Fancy);
      GetFont(FontFace.Mono);
      GetFont(FontFace.Pixel);
      GetFont(FontFace.Entypo);

      Screen = new UiScreen();
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
      GuiComponent other = new GuiComponent();
      other._shader = _shader;
      other._megaTex = _megaTex;
      other.Screen = Screen;
      return other;
    }
    public MtTex DefaultPixel()
    {
      return _megaTex.DefaultPixel;
    }
    private UiElement CreateStyledElement()
    {
      UiElement e = new UiElement();
      if (_styleSheet != null)
      {
        e.ApplyStyle(_styleSheet.DefaultStyle);
      }
      return e;
    }
    public UiElement CreatePanel(vec2 pos, vec2 wh)
    {
      UiElement e = CreateStyledElement();
      e.Texture = _megaTex.DefaultPixel;
      e.Pos = pos;
      e.Extent = wh;
      e.PositionMode = UiPositionMode.Relative;
      return e;
    }
    public UiElement CreateButton(vec2 pos, string text, Action<UiEventId, UiElement, PCMouse> onClick = null)
    {
      UiElement e = CreateStyledElement();
      e.Texture = _megaTex.DefaultPixel;
      e.Pos = pos;
      e.Text = text;
      e.MaxWHPX = new vec2(100, 200);
      e.PositionMode = UiPositionMode.Relative;
      e.IsPickRoot = true;
      e.PadBot = e.PadLeft = e.PadTop = e.PadRight = 15;// Fonts are messed up right now 
      if (onClick != null)
      {
        e.AddEvent(UiEventId.Mouse_Lmb_Release, onClick);
      }
      return e;
    }
    public UiElement CreateLabel(vec2 pos, string text, bool showbackground = true, FontFace? font = null, float fontSize = 12, vec4? fontColor = null, FontStyle fontstyle = FontStyle.Normal, float lineheight = 1.0f)
    {
      UiElement e = CreateStyledElement();
      e.Texture = showbackground ? _megaTex.DefaultPixel : null;
      e.Pos = pos;
      e.Text = text;
      e.FontFace = font != null ? font : FontFace.Mono;
      e.FontSize = fontSize;
      e.FontColor = fontColor != null ? fontColor.Value : new vec4(0, 0, 0, 1);
      e.FontStyle = fontstyle;
      e.LineHeight = lineheight;
      e.PadBot = e.PadLeft = e.PadTop = e.PadRight = 15;// Fonts are messed up right now 
      e.PositionMode = UiPositionMode.Relative;
      e.IsPickRoot = true;
      return e;
    }
  }//class Gui

}//Namespace Pri
