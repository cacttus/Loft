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
    Static,
    Relative
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
    Scrollbar_Pos_Change
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
    //Units are in design-space pixels
    public float Top { get { return _top; } set { _top = value; } }
    public float Left { get { return _left; } set { _left = value; } }
    public float Right { get { return _right; } set { _right = value; } }
    public float Bottom { get { return _bottom; } set { _bottom = value; } }

    public float PadTop { get { return _padTop; } set { _padTop = value; } }
    public float PadRight { get { return _padRight; } set { _padRight = value; } }
    public float PadBot { get { return _padBot; } set { _padBot = value; } }
    public float PadLeft { get { return _padLeft; } set { _padLeft = value; } }

    public float WidthPX { get { return Right - Left; } }
    public float HeightPX { get { return Bottom - Top; } }
    public vec4 Color { get; set; } = new vec4(1, 1, 1, 1);

    protected float _padTop = 0;
    protected float _padRight = 0;
    protected float _padBot = 0;
    protected float _padLeft = 0;
    protected float _top = 0;
    protected float _right = 0;
    protected float _bottom = 0;
    protected float _left = 0;
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
    };
    //Separate UiElement from container because Dictionary footprint is massive and, the glyphs have no children
    private WeakReference<UiElement> _parent = null;
    protected const int BaseLayerSort = 1000;
    public MultiMap<int, UiElement> Children { get; set; } = null;
    private Box2f _b2ComputedQuad = new Box2f();      // Base quad that is calculated from _right/_left.., parent, padding, etc
    private Box2f _b2ComputedQuadLast = new Box2f();
    private Box2f _b2ContentQuad = new Box2f();       // Encompassess ALL of the child quads, plus overflow.
    private Box2f _b2LayoutQuad = new Box2f();        // Transformed from design space into screen space.
    protected Box2f _b2RasterQuad = new Box2f();      // Final raster quad in OpenGL screen coordinates.
    private uint _iPickId = 0;
    private UiDragInfo _dragInfo = null;
    private bool _bPickedLastFrame;
    private ButtonState _eMouseState;
    private long _iPickedFrameId = 0;
    private Dictionary<UiEventId, List<Action<UiEventId, UiElement, PCMouse>>> _events = null;//EventId, list of action (EventId, Object)

    public UiPositionMode Position { get; set; } = UiPositionMode.Static;
    public UiOverflowMode Overflow { get; set; } = UiOverflowMode.Hide;
    public UiDimUnit WidthUnit { get; set; } = UiDimUnit.Pixel;
    public UiDimUnit HeightUnit { get; set; } = UiDimUnit.Pixel;
    public float WidthPct { get; set; } = 100;// this is only set if th DimUnit is set to percent
    public float HeightPct { get; set; } = 100;// this is only set if th DimUnit is set to percent
    public vec2 MinWHPX { get; set; } = new vec2(0, 0);
    public vec2 MaxWHPX { get; set; } = new vec2(9999999, 9999999);
    public bool LayoutChanged { get; private set; } = true;
    public bool LayoutVisible { get; set; } = true;
    public bool RenderVisible { get; set; } = true;
    public bool PickEnabled { get; set; } = true;
    public bool IsPickRoot { get; set; } = true;
    public bool ScaleToDesign { get; set; } = true; // this is true for all elements besides cursor.
    public UiDisplayMode DisplayMode { get; set; } = UiDisplayMode.Block;
    public UiImageSizeMode SizeModeX { get; set; } = UiImageSizeMode.Expand;  // tile = GL_REPEAT, Clamp = GL_CLAMP, Expand - expand tex coords.
    public UiImageSizeMode SizeModeY { get; set; } = UiImageSizeMode.Expand;  // tile = GL_REPEAT, Clamp = GL_CLAMP, Expand - expand tex coords.
    public UiDimUnit PadUnitTop { get; set; } = UiDimUnit.Pixel;
    public UiDimUnit PadUnitRight { get; set; } = UiDimUnit.Pixel;
    public UiDimUnit PadUnitBot { get; set; } = UiDimUnit.Pixel;
    public UiDimUnit PadUnitLeft { get; set; } = UiDimUnit.Pixel;

    protected void SetLayoutChanged()
    {
      if (LayoutChanged == false)
      {
        LayoutChanged = true;
        // if (bChildren)
        // {
        //   // if bChildren is true, we go in reverse **this is expensive and should only be used by gui2d
        //   if (Children != null)
        //   {
        //     foreach (var c in Children)
        //     {
        //       c.Value.SetLayoutChanged();
        //     }
        //   }
        // }
        // else
        //{
        if (_parent != null)
        {
          if (_parent.TryGetTarget(out var par))
          {
            par.SetLayoutChanged();
          }
        }

        // }
      }
    }

    //Texture
    public MtTex Texture = null;
    private Box2f _q2Tex = new Box2f(0, 0, 1, 1);//0,1
    private vec2 _tileScale = new vec2(1, 1); //Scale if UiImageSizeMode is Tile

    // public float PadTop { get; set; }
    // public float PadBottom { get; set; }
    // public float PadLeft { get; set; }
    // public float PadRight { get; set; }

    public virtual void Update(WorldObject wo, WindowContext wc)
    {
      if (LayoutVisible == false || RenderVisible == false)
      {
        return;
      }
      _bPickedLastFrame = false;
      _eMouseState = ButtonState.Up;  // Reset mouse state

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
          p.Value.Update(wo, wc);
        }
      }
    }
    public void AddChild(UiElement e, int sort = BaseLayerSort)
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
          GetQuadVerts(verts, b2ClipRect);
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
      if (Overflow == UiOverflowMode.Hide)
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
    private protected void GetQuadVerts(List<v_v4v4v4v2u2> verts, Box2f b2ClipRect)
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
        float fr = Texture.getSizeRatio();
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

      if (Texture.getWidth() > 0 && v._texsiz.x > 0)
      {
        w1px = 1.0f / Texture.getWidth();
        w1px *= v._texsiz.x;
        w1px *= pixAdjust;
      }
      if (Texture.getHeight() > 0 && v._texsiz.y > 0)
      {
        h1px = 1.0f / Texture.getHeight();
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
      bool bPicked = false;
      Box2f q = _b2LayoutQuad;
      if (LayoutVisible == true)
      {
        if (RenderVisible == true)
        {
          if (PickEnabled == true)
          {
            if (q.ContainsPointInclusive(mous.Pos))
            {
              _iPickedFrameId = frameStamp;

              if (IsPickRoot)
              {
                // Pick root items pick by boundbox
                bPicked = true;
              }
              else
              {
                // Else pick all children to find the pick root
                if (Children != null)
                {
                  foreach (var ele in Children)
                  {
                    if (ele.Value.Pick(mous, frameStamp) == true)
                    {
                      // We pick the first child in the TOP level sort order
                      // If we hit it, break out because we don't want to pick children underneath
                      bPicked = true;
                      break;
                    }
                  }
                }
              }

              // Capture the bubble-up pick event and process events
              if (bPicked)
              {
                DoMouseEvents(mous);
              }
            }
          }
        }
      }
      _bPickedLastFrame = bPicked;
      return bPicked;
    }
    void AddEvent(UiEventId evId, Action<UiEventId, UiElement, PCMouse> f)
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
    void EnableDrag(Action<vec2> func)
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
          if (_events.TryGetValue(UiEventId.Mouse_Lmb_Up, out var actions))
          {
            foreach (var act in actions)
            {
              act(UiEventId.Mouse_Lmb_Up, this, m);
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

      // Set to false if we're controllig coordinates of this element (cursor, or window position)
      //  if (getShouldScalePositionToDesign() == true) {
      _b2LayoutQuad._min.x = _b2ComputedQuad._min.x * w1;
      _b2LayoutQuad._min.y = _b2ComputedQuad._min.y * h1;
      _b2LayoutQuad._max.x = _b2ComputedQuad._max.x * w1;
      _b2LayoutQuad._max.y = _b2ComputedQuad._max.y * h1;
      // }
      // else {
      //   // We are an absolute position (cursor)
      //   _b2LayoutQuad._min.x = final_l;  // These are alerady set
      //   _b2LayoutQuad._min.y = final_t;
      //   _b2LayoutQuad._max.x = final_l + (final_r - final_l) * w1;  // Still Scale the width / height
      //   _b2LayoutQuad._max.y = final_t + (final_b - final_t) * h1;
      // }

      // Raster Quad (for drawing)
      _b2RasterQuad = _b2LayoutQuad;
      GuiQuad2d(ref _b2RasterQuad, viewport_wh);
    }
    protected void GuiQuad2d(ref Box2f pq, vec2 viewport_wh)
    {
      // Transforms a quad for the matrix-less Gui projection.

      // The resulting coordinates for the GPU are -0.5 +0.5 in both axes with the center being in the center of the screen
      // Translate a 2D screen quad to be rendered in a shader.
      // So* our quad is from TOP Left - OpenGL is Bottom Left - this fixes this.
      float w = (float)viewport_wh.x;
      float w2 = w * 0.5f;
      float h = (float)viewport_wh.y;
      float h2 = h * 0.5f;

      // Subtract from viewport center
      pq._min.x -= w2;
      pq._max.x -= w2;

      // Invert text to show rightsize up and divide by perspective
      pq._min.x = pq._min.x / w2;
      pq._min.y = (h2 - pq._min.y - 1) / h2;
      pq._max.x = pq._max.x / w2;
      pq._max.y = (h2 - pq._max.y - 1) / h2;
    }
    protected virtual void PerformLayout(vec2 viewport_wh, bool bForce)
    {
      //Build the UI depth-first. Children elements are built, then we add those
      //positions to the parent to build the elements.
      if (LayoutChanged)
      {
        if (Children != null)
        {
          //PerformLayoutChildren
          foreach (var p in Children)
          {
            UiElement ele = p.Value;
            if (ele.LayoutVisible)
            {
              ele.PerformLayout(viewport_wh, bForce);
            }
          }
          PositionChildren(bForce);
          foreach (var p in Children)
          {
            UiElement ele = p.Value;
            if (ele.LayoutVisible)
            {
              LayoutEleQuad(viewport_wh, ele);
            }
          }
        }

        ComputeContentQuad();

        LayoutChanged = false;
      }
    }
    private void ComputeContentQuad()
    {
      // Reset content quad to 0,0
      _b2ContentQuad._min = _b2ContentQuad._max = new vec2(Left, Top);

      float dbgwidth = WidthPX;
      float dbgheight = HeightPX;
      // Recur and compute bounds for all children
      float fright = -float.MaxValue;
      float fbottom = -float.MaxValue;
      if (Children != null)
      {
        foreach (var p in Children)
        {
          var ele = p.Value;

          if (ele.LayoutVisible)
          {
            // Add padding for static elements
            float effR = ele.Right;
            float effT = ele.Bottom;
            if (ele.Position == UiPositionMode.Static)
            {
              effR -= (ele.PadLeft + ele.PadRight);
            }
            if (ele.Position == UiPositionMode.Static)
            {
              effT -= (ele.PadTop + ele.PadBot);
            }

            // w/h adjust
            effR -= 1;
            effT -= 1;

            fright = Math.Max(fright, Left + effR);
            fbottom = Math.Max(fbottom, Top + effT);

            // expand contenet quad
            _b2ContentQuad.ExpandByPoint(new vec2(fright, fbottom));

            if (_b2ContentQuad.Width() > dbgwidth)
            {
              int nnn = 0;
              nnn++;
            }
            if (_b2ContentQuad.Height() > dbgheight)
            {
              int nnn = 0;
              nnn++;
            }
          }
        }
      }
    }
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
    protected void PositionChildren(bool bForce)
    {
      //Layout each layer specified by the key in the Children multimap.
      //Each new layer will be a new layout set (bucket).
      //Layout static elements sequentially left to right,
      //or, layout relative elements for absolute position.
      if (Children != null)
      {
        int uiLast = int.MaxValue;
        List<UiElement> bucket = new List<UiElement>();
        foreach (var p in Children)
        {
          var ele = p.Value;

          if (ele.LayoutVisible)
          {
            ele._b2ComputedQuadLast = ele._b2ComputedQuad;

            if (ele.Position == UiPositionMode.Static)
            {
              // Static elements - Have a Flow, and computed position
              if (p.Key != uiLast)
              {
                uiLast = p.Key;
                if (bucket.Count > 0)
                {
                  LayoutLayer(bucket);
                  bucket.Clear();
                }
              }
              bucket.Add(p.Value);
            }
            else if (ele.Position == UiPositionMode.Relative)
            {
              // Calc positioned elements (does not follow flow), static position
              ComputePositionalElement(ele);
            }
          }
        }
        if (bucket.Count > 0)
        {
          LayoutLayer(bucket);
          bucket.Clear();
        }
      }
    }
    private void LayoutLayer(List<UiElement> stats)
    {
      // Calc width with all static blocks using 0 width for autos (expandable blocks).
      float fTotalHeight = 0;
      List<UiLine> vecLines = new List<UiLine>();
      vecLines.Add(new UiLine());
      foreach (var ele in stats)
      {
        CalcStaticElement(ele, vecLines, 0.0f, 0.0f);
        fTotalHeight += vecLines[vecLines.Count - 1]._height;
      }

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
    private void CalcStaticElement(UiElement ele, List<UiLine> vecLines, float fAutoWidth, float fAutoHeight)
    {
      if (vecLines.Count == 0)
      {
        Gu.BRThrowException("GUI error - tried to run calc algorithm without any UILines created");
      }
      UiLine line = vecLines[vecLines.Count - 1];

      // Autos get zero first so we an compute the fixed
      // statics then we go throguh here again and assign them computed values

      float wpx = 0, hpx = 0;
      ele.ComputeWH(ref wpx, ref hpx);  // also applies min/max
      // if (ele.WidthUnit == UiDimUnit.Auto)
      // {
      //   wpx = fAutoWidth;
      // }
      // if (ele.HeightUnit == UiDimUnit.Auto)
      // {
      //   hpx = fAutoHeight;
      // }
      ele.ApplyMinMax(ref wpx, ref hpx);

      // Remove unnecessary padding to prevent auto and % widths from growing
      // float parent_w = getParent()->right().px() - getParent()->left().px();
      // float parent_h = getParent()->bottom().px() - getParent()->top().px();
      //
      // wpx + pl + pr

      //*Padding
      float pl = 0, pr = 0, pb = 0, pt = 0;
      ele.ComputePad(this, ref pt, ref pr, ref pb, ref pl);

      float wpx_pad = wpx + pl + pr;
      float hpx_pad = hpx + pb + pt;

      //**Line break
      bool bLineBreak = false;
      if (ele.DisplayMode == UiDisplayMode.InlineWrap)
      {
        if (wpx_pad + line._width > WidthPX) //For label - auto width + expand. ?? 
        {
          bLineBreak = true;
        }
      }
      else if (ele.DisplayMode == UiDisplayMode.Block)
      {
        bLineBreak = true;
      }
      else if (ele.DisplayMode != UiDisplayMode.InlineNoWrap)
      {
        bLineBreak = false;
      }

      if (bLineBreak)
      {
        // new line
        UiLine line2 = new UiLine();
        line2._top = line._top + line._height;
        vecLines.Add(line2);
        line = vecLines[vecLines.Count - 1];
      }

      ele._left = line._left + line._width + pl; // ele->left() = line->_left + line->_width + pl;
      ele._right = ele._left + wpx; // ele->right() = ele->left().px() + wpx;  // wpx, not wpx_pad
      ele._top = line._top + pt; //ele->top() = line->_top + pt;
      ele._bottom = ele._top + hpx; //ele->bottom() = ele->top().px() + hpx;  // hpx, not hpx_pad

      line._width += wpx_pad;

      ele.ValidateQuad();

      // Increse line height WITH PAD
      line._height = Math.Max(line._height, hpx_pad);

      line._eles.Add(ele);
    }
    private void ComputePad(UiElement ele, ref float pt, ref float pr, ref float pb, ref float pl)
    {
      pt = ComputePad_Unit(HeightPX, ele.PadUnitTop, ele._padTop);
      pr = ComputePad_Unit(WidthPX, ele.PadUnitRight, ele._padRight);
      pb = ComputePad_Unit(HeightPX, ele.PadUnitBot, ele._padBot);
      pl = ComputePad_Unit(WidthPX, ele.PadUnitLeft, ele._padLeft);
    }
    private static float ComputePad_Unit(float parentextent, UiDimUnit unit, float ud)
    {
      //Basically we allowed for % paddings. 
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
        Gu.Log.Error("Invalid value for pad");
        return 0;
      }
    }
    private void ComputePositionalElement(UiElement ele)
    {
      //*No padding
      //*No auto sizing
      // Needs to be separate becasue cursor/windwos needs special updating.
      // Comp static wh.
      //**PADDING IS NOT APLIED TO POSITIONAL ELEMENTS**
      float wpx = 0, hpx = 0;
      ele.ComputeWH(ref wpx, ref hpx);  // Cannot be auto
      ele.ApplyMinMax(ref wpx, ref hpx);
      ele.Right = ele.Left + wpx;
      ele.Bottom = ele.Top + hpx;
      ValidateQuad();
    }
    private void ComputeWH(ref float wpx, ref float hpx)
    {
      wpx = 0;
      hpx = 0;

      // Width
      if (WidthUnit == UiDimUnit.Pixel)
      {
        wpx = WidthPX;
      }
      else if (WidthUnit == UiDimUnit.Percent)
      {
        if (_parent != null && _parent.TryGetTarget(out var par))
        {
          wpx = par.WidthPX * WidthPct * 0.01f;
        }
      }
      else if (WidthUnit == UiDimUnit.Auto)
      {
      }
      else
      {
        Gu.Log.Error("Invalid enum");
      }

      // Height
      if (HeightUnit == UiDimUnit.Pixel)
      {
        hpx = HeightPX;
      }
      else if (HeightUnit == UiDimUnit.Percent)
      {
        if (_parent != null && _parent.TryGetTarget(out var par))
        {
          hpx = par.HeightPX * HeightPct * 0.01f;
        }
      }
      else if (HeightUnit == UiDimUnit.Auto)
      {
      }
      else
      {
        Gu.Log.Error("Invalid enum");
      }
      // Make sure it's an actual heght
      if (hpx < 0.0f)
      {
        hpx = 0.0f;
      }
      if (wpx < 0.0f)
      {
        wpx = 0.0f;
      }
    }
    private void ApplyMinMax(ref float wpx, ref float hpx)
    {
      float minw = MinWHPX.x;
      float minh = MinWHPX.y;
      float maxw = MaxWHPX.x;
      float maxh = MaxWHPX.y;

      // apply min/max to box (not in parent space)
      if (wpx > maxw)
      {
        wpx = maxw;
      }
      if (wpx < minw)
      {
        wpx = minw;
      }
      if (hpx > maxh)
      {
        hpx = maxh;
      }
      if (hpx < minh)
      {
        hpx = minh;
      }
    }
    public void ValidateQuad()
    {
      if ((Right < Left) || (Bottom < Top))
      {
        Gu.Log.Error("Computed Quad is invalid, rtbl= " + Right + "," + Left + "," + Bottom + "," + Top + ".");
        Gu.DebugBreak();
      }
    }

  }//UiElement
  public class UIGlyph : UiElementBase
  {
    public Box2f Texs;
    public vec2 Size;
  }
  public class UILabel : UiElement
  {
    private string _strText = "";
    private bool _bChanged = false;
    private Font _font;

    public string Text { get { return _strText; } set { SetLayoutChanged(); _strText = value; } }

    public UILabel(vec2 pos, Font f, string text)
    {
      _left = pos.x;
      _top= pos.y;
      _right = _left + 80;
      _bottom = _top + 100;
      _font = f;
      _strText = text;
    }
    protected override void PerformLayout(vec2 viewport_wh, bool bForce)
    {
      CreateGlyphs();
      base.PerformLayout(viewport_wh, bForce);
    }
    private void CreateGlyphs()
    {
      Children = new MultiMap<int, UiElement>();
      float width = 0;
      int last = 0;
      foreach (int cc in _strText)
      {
        var g = _font.GetGlyph(cc, 20);


        if (g != null)
        {
          float advW = this._font.MtFont.GetKernAdvanceWidth(20, last, cc);

          UiElement e = new UiElement();
          e.PadBot = g.PadBot;
          e.PadRight = g.PadRight;
          e.PadLeft = g.PadLeft + advW;
          e.PadTop = g.PadTop;
          e.Left = 0;
          e.Right = g.Size.x;
          e.Top = 0;
          e.Bottom = g.Size.y;
          e.Texture = new MtTex(null, 0);
          //we need to set this for pixAdjust
          e.Texture.SetWH(this._font.MtFont.GetTexs()[0].getWidth(),this._font.MtFont.GetTexs()[0].getHeight());
          e.Texture.uv0 = g.Texs._min;
          e.Texture.uv1 = g.Texs._max;
          e.DisplayMode = UiDisplayMode.InlineWrap;
          e.Color = new vec4(0,0,0,1);
          e.ValidateQuad();
          AddChild(e);
        }
        last = cc;
      }
    }
  }
  public class UiScreen : UiElement
  {
    public UiScreen()
    {
      int designWidth = 1920;
      int designHeight = 1080;

      Right = designWidth - 1;    // Gu::getViewport()->getWidth();
      Bottom = designHeight - 1;  // Gu::getViewport()->getHeight();
      Top = 0;
      Left = 0;

    }
    public override void Update(WorldObject wo, WindowContext ct)
    {
      base.Update(wo, ct);  // Note: due to buttons resetting themselves update() must come before pick()

      // if (Gu::getFpsMeter()->frameMod(2)) {
      UpdateLayout(wo, ct.PCMouse);
      // }

      // Updating this per frame to indicate if the GUI is picked.
      Pick(ct.PCMouse, ct.FrameStamp);
    }
    void UpdateLayout(WorldObject wo, PCMouse mouse)
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

        PerformLayout(viewport_wh, false);
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
  public class Font
  {
    public FileLoc FileLoc { get; private set; }
    //font size -> list of glyphs based on char code.
    public MtFont MtFont { get; } = null;
    public vec4 Color { get; set; } = new vec4(1, 1, 1, 1);
    //font size -> char code -> gUiElement
    private Dictionary<int, Dictionary<int, UIGlyph>> _glyphs = new Dictionary<int, Dictionary<int, UIGlyph>>();
    private bool _glyphsCreated = false;

    public UIGlyph GetGlyph(int cc, int height)
    {
      if (_glyphsCreated == false)
      {
        _glyphsCreated = true;
        //**TEST
        //**TEST
        //**TEST
        CreateGlyphsForHeight(20);
      }
      if (_glyphs.TryGetValue(height, out var chars))
      {
        if (chars.TryGetValue(cc, out var theglyph))
        {
          return theglyph;
        }
      }
      return null;
    }

    public Font(FileLoc loc, MtFont f)
    {
      FileLoc = loc;
      MtFont = f;

    }
    void CreateGlyphsForHeight(int fontHeight)
    {
      //FontHeight must be an int to prevent floating point error 
      Box2f outTexs = new Box2f();
      float outW = 0, outH = 0, outPadT = 0, outPadR = 0, outPadB = 0, outPadL = 0;
      int nCh = 0;

      //TODO: support other languages.
      for (int c = 10; c < 255; c++)
      {
        //we should use the kerning code when we build the actual string.
        MtFont.getCharQuad(c, (float)fontHeight, ref outW, ref outH, ref outTexs, ref outPadT, ref outPadR, ref outPadB, ref outPadL);

        UIGlyph g = new UIGlyph();

        Dictionary<int, UIGlyph> val = null;
        if (!_glyphs.TryGetValue(fontHeight, out val))
        {
          val = new Dictionary<int, UIGlyph>();
          _glyphs.Add(fontHeight, val);
        }
        val.Add(c, g);

        g.Size = new vec2(outW, outH);

        g.Texs = outTexs;
        g.Color = Color;  // Copy color over Note: font color? I do't know I think we should use gui color

        g.PadTop = outPadT;     // fontHeight - outH;    //this should never be greater
        g.PadRight = outPadR;   // fontHeight - outH;    //this should never be greater
        g.PadBot = outPadB;  // fontHeight - outH;    //this should never be greater
        g.PadLeft = outPadL;    // fontHeight - outH;    //this should never be greater

        nCh++;
      }

    }
  }
  public class GuiComponent : Component
  {
    //private const int MaxGuiVerts = 2048;
    //private v_v4v4v4v2u2[] _verts = new v_v4v4v4v2u2[MaxGuiVerts];
    //private int _numVerts = 0;
    private List<Font> _fonts = new List<Font>();
    private Shader _shader = null;
    private MegaTex _megaTex = null;

    public UiScreen Screen { get; private set; } = null;

    public static FileLoc Fancy = new FileLoc("Lato-Regular.ttf", FileStorage.Embedded);//Parisienne
    public static FileLoc Pixel = new FileLoc("PressStart2P-Regular.ttf", FileStorage.Embedded);
    public static FileLoc Pixel2 = new FileLoc("kenpixel.ttf", FileStorage.Embedded);
    public static FileLoc Pixel3 = new FileLoc("visitor1.ttf", FileStorage.Embedded);
    public static FileLoc Minecraft = new FileLoc("Minecraftia-Regular.ttf", FileStorage.Embedded);
    public static FileLoc Entypo_Symbols = new FileLoc("Entypo.ttf", FileStorage.Embedded);
    public static FileLoc FontAwesome_Symbols = new FileLoc("fontawesome.ttf", FileStorage.Embedded);

    public GuiComponent()
    {
      _megaTex = new MegaTex("gui_megatex", true, true);

      Screen = new UiScreen();
    }
    public Font LoadFont(FileLoc loc)
    {
      var ret = new Font(loc, _megaTex.GetFont(loc));
      _fonts.Add(ret);
      return ret;
    }
    public override void OnCreate(WorldObject myObj)
    {
      _megaTex.LoadImages();
      MegaTex.CompiledTextures tx = _megaTex.Compile(MegaTex.MtClearColor.DebugRainbow);
      if (tx != null)
      {

        //don't filter
       // tx.Albedo.SetFilter(OpenTK.Graphics.OpenGL4.TextureMinFilter.LinearMipmapNearest,OpenTK.Graphics.OpenGL4.TextureMagFilter.Linear);

        _shader = Gu.Resources.LoadShader("v_gui", true, FileStorage.Embedded);
        myObj.Material = new Material("GuiMT", _shader);
        myObj.Material.GpuRenderState.DepthTest = false;
        myObj.Material.GpuRenderState.Blend = true;
        myObj.Material.Textures[Shader.TextureInput.Albedo] = tx.Albedo;
      }
    }
    public override void OnUpdate(double dt, WorldObject obj)
    {
      //this._megaTex.Update();
      Screen.Update(obj, Gu.Context);
    }
    public override void OnDestroy(WorldObject myObj)
    {
    }
    public override Component Clone(bool shallow = true)
    {
      GuiComponent other = new GuiComponent();
      other._fonts = new List<Font>(_fonts);
      other._shader = _shader;
      other._megaTex = _megaTex;
      other.Screen = Screen;
      return other;
    }
    public void DrawText(vec2 pos, string text)
    {
      // Gu.Assert(DefaultFont != null);
      // Gu.Assert(DefaultFont.MtFont != null);
      // float w, h;
      // foreach (char c in text)
      // {
      //   //    _activeFont.MtFont.getCharQuad(c);
      //   //Glyph g = GetGlyphByChar(c) // glyphs cached by by getCharQuad
      //   //    g.x += w;
      //   //w += g.width;
      //   //if (_cur_vert >= MaxGuiVerts)
      //   //{
      //   //  Gu.Log.Error("Used up all our verts.");
      //   //}
      //   //else
      //   //{
      //   //  Verts[_numVerts] = g.GetVert();
      //   //}
      // }
    }

    public MtTex DefaultPixel()
    {
      return _megaTex.DefaultPixel;
    }
    public UiElement CreatePanel(vec4 color, vec2 pos, vec2 wh)
    {
      UiElement e = new UiElement();
      e.Color = color;
      e.Texture = _megaTex.DefaultPixel;
      e.Left = pos.x;
      e.Right = pos.x + wh.x;
      e.Top = pos.y;
      e.Bottom = pos.y + wh.y;
      return e;
    }

  }//class Gui

}//Namespace Pri
