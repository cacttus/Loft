using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PirateCraft
{
  #region Enums

  using UiAction = Action<UiEvent>;

  public class FontFace : FileLoc
  {
    public string Name = "";
    public FontFace() { }
    protected FontFace(string name, FileLoc loc) : base(loc.RawPath, loc.FileStorage) { Name = name; }
    public static FontFace Parisienne = new FontFace("Parisienne", new FileLoc("Parisienne-Regular.ttf", FileStorage.Embedded));
    public static FontFace RobotoMono = new FontFace("RobotoMono", new FileLoc("RobotoMono-Regular.ttf", FileStorage.Embedded));
    public static FontFace PressStart2P = new FontFace("PressStart2P", new FileLoc("PressStart2P-Regular.ttf", FileStorage.Embedded));
    //public static FontFace Entypo = new FontFace("Entypo", new FileLoc("Entypo.ttf", FileStorage.Embedded));
    public static FontFace Calibri = new FontFace("Calibri", new FileLoc("calibri.ttf", FileStorage.Embedded));
    public static FontFace EmilysCandy = new FontFace("EmilysCandy", new FileLoc("EmilysCandy-Regular.ttf", FileStorage.Embedded));
  }
  public enum UiDisplayMode
  {
    [CSSAttribute("inline")] Inline,
    [CSSAttribute("block")] Block,
    [CSSAttribute("inline-no-wrap")] InlineNoWrap
  }
  public enum UiPositionMode
  {
    [CSSAttribute("static")] Static, // flows within page/container, position is ignored (text)
    [CSSAttribute("relative")] Relative, // positioned relative to the container. (image with text flow)
    [CSSAttribute("Absolute")] Absolute, // entiure screen
  }
  public enum UiAlignment
  {
    [CSSAttribute("left")] Left,  // float left, roman text
    [CSSAttribute("center")] Center,// center
    [CSSAttribute("right")] Right, // float right, arabic text
  }
  public enum UiSizeMode
  {
    [CSSAttribute("expand")] Expand, //Expand to parent
    [CSSAttribute("shrink")] Shrink, //Shrink container, grow child expanders to container max w/h
    [CSSAttribute("fixed")] Fixed // Fixed width/height
  }
  public enum UiRenderMode
  {
    [CSSAttribute("none")] None, // not drawn
    [CSSAttribute("color")] Color, // flat color, => the Tex will be default pixel
    [CSSAttribute("textured")] Textured, // texture => uses Texture
  }
  public enum UiBuildOrder
  {
    [CSSAttribute("Horizontal")] Horizontal, //Shrink to size of child contents, taking Max Width/Height into account
    [CSSAttribute("Vertical")] Vertical, //Expand to parent
  }
  public enum UiOverflowMode
  {
    [CSSAttribute("hide")] Show, //show overflow - elements outside of the clip region (not element region)
    [CSSAttribute("show")] Hide // hide overflow 
  };
  public enum UiImageTiling
  {
    [CSSAttribute("expand")] Expand,//expand to fit quad
    [CSSAttribute("tile")] Tile, //tile the image
    [CSSAttribute("computed")] Computed, //tiling is computed
    [CSSAttribute("proportion")] Proportion //height is proportional to width
  }
  public enum UiFontStyle
  {
    [CSSAttribute("normal")] Normal,
    [CSSAttribute("bold")] Bold,
    [CSSAttribute("italic")] Italic
  }
  public enum UiFloatMode
  {
    [CSSAttribute("asdfasdf")] None, // flows within page/container, position is ignored (text)
    [CSSAttribute("relatasdfgdsagive")] Floating, //element "floats" absolute above parent, does not affect container element region, but affects clip region (context menu)
  }
  public enum UiEventId
  {
    Undefined,

    LmbPress,
    LmbHold,
    LmbRelease,
    LmbUp,
    RmbPress,
    RmbHold,
    RmbRelease,
    RmbUp,

    Mouse_Enter,
    Mouse_Move,//Mouse_Hover = Mouse_Move?
    Mouse_Leave,

    Lost_Press_Focus,
    Got_Press_Focus,
    Release_Press_Focus,


    Scrollbar_Pos_Change,

    Drag,
    Tick, //tick event like every x ms
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
    , ColorMul
    , BorderTopColor
    , BorderRightColor
    , BorderBotColor
    , BorderLeftColor
    , FontFace
    , FontSize
    , FontStyle
    , FontColor
    , LineHeight
    , Texture
    , ImageTilingX
    , ImageTilingY
    , DisplayMode
    , PositionMode
    , OverflowMode
    , SizeModeWidth
    , SizeModeHeight
    , MinValue
    , MaxValue
    , ZIndex
    , FloatMode
    , RenderMode
    , TextAlign
    , Alignment

    //****
    , MaxUiProps
  }

  #endregion

  public class UiRef<T> where T : class
  {
    public T Value = null;
    public UiRef() { }
    public UiRef(T val) { Value = val; }
  }
  public class UiDebugDraw
  {
    public bool DisableClip = false;
    public bool ShowOverlay = false;
    public bool DisableMargins = false;
    public bool DisablePadding = false;
    public bool DisableBorders = false;
    public vec4 OverlayColor = new vec4(1, 0, 0, 0.3f);
    public int FrameId = 0;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct UiQuad
  {
    //Using box2f with overriden props was not good, was causing a lot of errors
    public float _left = 0;
    public float _top = 0;
    public float _width = 0;
    public float _height = 0;

    public vec2 Max { get { return new vec2(_left + _width, _top + _height); } }
    public vec2 Min { get { return new vec2(_left, _top); } }
    public float Top { get { return _top; } }
    public float Left { get { return _left; } }
    public float Right { get { return _left + _width; } }
    public float Bottom { get { return _top + _height; } }

    public UiQuad() { }
    public UiQuad Clone()
    {
      return new UiQuad()
      {
        _left = this._left,
        _top = this._top,
        _width = this._width,
        _height = this._height
      };
    }
    public bool Validate(bool debug_break = true, float min_volume = 0)
    {
      return this.ToBox().Validate(debug_break, min_volume);
    }
    public void ExpandByPoint(vec2 v)
    {
      var b = ToBox();
      b.ExpandByPoint(v);
      FromBox(b);
    }
    public Box2f ToBox()
    {
      return new Box2f(_left, _top, _width, _height);
    }
    public void FromBox(Box2f bx)
    {
      this._left = bx._min.x;
      this._top = bx._min.y;
      this._width = bx.Width;
      this._height = bx.Height;
    }
    public bool ShrinkByBox(UiQuad b)
    {
      var bx = this.ToBox();
      bx.ShrinkByBox(b.ToBox());
      FromBox(bx);
      return true;
    }
  }
  public class UiEventState
  {
    //shared UI state for events
    public Gui2d? Gui { get; private set; } = null;//must be set manuallly
    public UiElement? Previous { get; private set; } = null;
    public UiElement? Current { get; private set; } = null;
    public ButtonState LeftButtonState { get; private set; }
    public ButtonState RightButtonState { get; private set; }
    public vec2 MousePosCur { get; private set; }
    public vec2 MousePosLast { get; private set; }
    public UiElement? PressFocus { get; private set; } = null;

    public bool IsAnyGuiItemPicked()
    {
      return Current != null;
    }

    public UiEventState(Gui2d g, vec2 mpos_cur, vec2 mpos_last, UiElement? prev_pick, UiElement? cur_pick, ButtonState leftState, ButtonState rightState, UiElement? pressFocus)
    {
      Gui = g;
      MousePosCur = mpos_cur;
      MousePosLast = mpos_last;
      Previous = prev_pick;
      Current = cur_pick;
      LeftButtonState = leftState;
      RightButtonState = rightState;
      PressFocus = pressFocus;
    }
  }
  public class UiEvent
  {
    public UiEventId EventId { get; private set; } = UiEventId.Undefined;
    //public MouseButton? MouseButton { get; private set; } = null;//not null if this is a mouse event
    // public ButtonState? ButtonState { get; private set; } = null;
    public UiElement Element { get; private set; } //We could store a weak reference here, assuming at some point the Gui system may add/delete non-glyph elements

    public UiEventState? State { get; set; } = null;

    private UiEventThing _thing;

    public UiEvent(UiEventId id, UiElement ele)
    {
      Gu.Assert(ele != null);
      Element = ele;
      EventId = id;
    }
    public void Fire()
    {
      Gu.Assert(Element != null);
      Element.DoMouseEvents(this, false);
    }
  }
  public class UiEventThing
  {
    //captures input from the user while the UI is updating asynchronously.
    //trying to do this without registering for events from children - "passive" events
    private int c_iMaxEvents = 500;

    private ButtonState _eLast_Lmb = ButtonState.Up;
    private ButtonState _eLast_Rmb = ButtonState.Up;
    private ButtonState _eLast_Mmb = ButtonState.Up;
    public List<UiEvent> _events = new List<UiEvent>();
    private UiElement? _pressFocusLast = null;
    private UiElement? _pressFocus = null;
    private List<UiEvent> _new_events_frame = new List<UiEvent>();

    public UiEventThing()
    {
      c_iMaxEvents = Gu.EngineConfig.MaxUIEvents;
      c_iMaxEvents = Math.Clamp(c_iMaxEvents, 0, 9999999);
    }
    public void PollForEvents(Gui2d g)
    {
      //Poll each frame, no timer. currently Press/Release are set on a single frame but if we catch press->up we could always send a release in between.
      Gu.Assert(g != null);
      if (_events.Count >= c_iMaxEvents)
      {
        Gu.Log.Error($"Too many UI events! max={c_iMaxEvents}");
        Gu.DebugBreak();
        return;
      }

      _new_events_frame.Clear();

      var picker = Gu.Context.Renderer.Picker;
      UiElement? elast = null;
      UiElement? ecur = null;

      //update picked
      if (picker.PickedObjectFrameLast is UiElement)
      {
        elast = picker.PickedObjectFrameLast as UiElement;
      }
      if (picker.PickedObjectFrame is UiElement)
      {
        ecur = picker.PickedObjectFrame as UiElement;
      }

      //button events
      var lb = Gu.Context.PCMouse.State(MouseButton.Left);
      var rb = Gu.Context.PCMouse.State(MouseButton.Right);
      var mpos = Gu.Context.PCMouse.Pos;
      var mlast = Gu.Context.PCMouse.LastPos;

      //press focus (drag / mouse down)
      if (Gu.Context.GameWindow.IsFocused == false)
      {
        var pold = _pressFocus;
        _pressFocus = null;
        SendEvent(UiEventId.Lost_Press_Focus, pold);
      }
      else if (lb == ButtonState.Press)
      {
        var pold = _pressFocus;
        if (Gu.TryGetSelectedView(out var vv))
        {
          _pressFocus = ecur;
        }
        SendEvent(UiEventId.Lost_Press_Focus, pold);
        SendEvent(UiEventId.Got_Press_Focus, _pressFocus);
      }
      else if (lb == ButtonState.Release)
      {
        SendEvent(UiEventId.Release_Press_Focus, _pressFocus);
      }

      //lmb / rmb
      if (lb != _eLast_Lmb)
      {
        if (elast != null)
        {
          if (lb == ButtonState.Up) { SendEvent(UiEventId.LmbUp, elast); }
          if (lb == ButtonState.Press) { SendEvent(UiEventId.LmbPress, elast); }
          if (lb == ButtonState.Hold) { SendEvent(UiEventId.LmbHold, elast); }
          if (lb == ButtonState.Release) { SendEvent(UiEventId.LmbRelease, elast); }
        }
        if (ecur != null)
        {
          if (lb == ButtonState.Up) { SendEvent(UiEventId.LmbUp, ecur); }
          if (lb == ButtonState.Press) { SendEvent(UiEventId.LmbPress, ecur); }
          if (lb == ButtonState.Hold) { SendEvent(UiEventId.LmbHold, ecur); }
          if (lb == ButtonState.Release) { SendEvent(UiEventId.LmbRelease, ecur); }
        }
        _eLast_Lmb = lb;
      }
      if (rb != _eLast_Rmb)
      {
        if (elast != null)
        {
          if (rb == ButtonState.Up) { SendEvent(UiEventId.RmbUp, elast); }
          if (rb == ButtonState.Press) { SendEvent(UiEventId.RmbPress, elast); }
          if (rb == ButtonState.Hold) { SendEvent(UiEventId.RmbHold, elast); }
          if (rb == ButtonState.Release) { SendEvent(UiEventId.RmbRelease, elast); }
        }
        if (ecur != null)
        {
          if (rb == ButtonState.Up) { SendEvent(UiEventId.RmbUp, ecur); }
          if (rb == ButtonState.Press) { SendEvent(UiEventId.RmbPress, ecur); }
          if (rb == ButtonState.Hold) { SendEvent(UiEventId.RmbHold, ecur); }
          if (rb == ButtonState.Release) { SendEvent(UiEventId.RmbRelease, ecur); }
        }
        _eLast_Rmb = rb;
      }

      //move events
      if (elast != null && elast != ecur)
      {
        SendEvent(UiEventId.Mouse_Leave, elast);
      }
      if (ecur != null && elast != ecur)
      {
        SendEvent(UiEventId.Mouse_Enter, ecur);
      }
      if (ecur != null && elast == ecur && mpos != mlast)
      {
        SendEvent(UiEventId.Mouse_Move, ecur);
      }

      //send
      if (_new_events_frame.Count > 0)
      {
        var state = new UiEventState(g, mpos, mlast, elast, ecur, lb, rb, _pressFocus);
        foreach (var ev in _new_events_frame)
        {
          ev.State = state;
        }
        _events.AddRange(_new_events_frame);

        _new_events_frame.Clear();
      }
    }
    private void SendEvent(UiEventId evid, UiElement? ele)
    {
      //make sure ele has registered the event
      if (ele != null)
      {
        if (ele.Events.Keys.Contains(evid))
        {
          _new_events_frame.Add(new UiEvent(evid, ele));
        }
      }
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
  public class UiProps
  {
    //All styled properties of an element. 
    // All elements contain one properties class with all value type properties set to default (besides texture/font face, but they MUST be set).
    //
    public float Top = 0;
    public float Left = 0;
    public float MinWidth = 0;
    public float MinHeight = 0;
    public float MaxWidth = Gui2d.MaxSize;
    public float MaxHeight = Gui2d.MaxSize;
    public float PadTop = 0;
    public float PadRight = 0;
    public float PadBot = 0;
    public float PadLeft = 0;
    public float MarginTop = 0;
    public float MarginRight = 0;
    public float MarginBot = 0;
    public float MarginLeft = 0;
    public float BorderTop = 0;
    public float BorderRight = 0;
    public float BorderBot = 0;
    public float BorderLeft = 0;
    public float BorderTopLeftRadius = 0;
    public float BorderTopRightRadius = 0;
    public float BorderBotRightRadius = 0;
    public float BorderBotLeftRadius = 0;
    public vec4 Color = new vec4(1, 1, 1, 1);
    public vec4 ColorMul = new vec4(1, 1, 1, 1);//color multiplier
    public vec4 BorderTopColor = new vec4(0, 0, 0, 1);
    public vec4 BorderRightColor = new vec4(0, 0, 0, 1);
    public vec4 BorderBotColor = new vec4(0, 0, 0, 1);
    public vec4 BorderLeftColor = new vec4(0, 0, 0, 1);
    public PirateCraft.FontFace FontFace = PirateCraft.FontFace.Calibri;
    public float FontSize = 12;
    public UiFontStyle FontStyle = UiFontStyle.Normal;
    public vec4 FontColor = new vec4(0, 0, 0, 1);
    public float LineHeight = 1;
    public MtTex Texture = null;
    public UiImageTiling ImageTilingX = UiImageTiling.Expand;
    public UiImageTiling ImageTilingY = UiImageTiling.Expand;
    public UiDisplayMode DisplayMode = UiDisplayMode.Block;
    public UiPositionMode PositionMode = UiPositionMode.Static;
    public UiOverflowMode OverflowMode = UiOverflowMode.Hide;
    public UiSizeMode SizeModeWidth = UiSizeMode.Expand;
    public UiSizeMode SizeModeHeight = UiSizeMode.Expand;
    public double MinValue = 0;
    public double MaxValue = 100;
    public float ZIndex = 0;
    public UiFloatMode FloatMode = UiFloatMode.None;
    public UiRenderMode RenderMode = UiRenderMode.None;
    public UiAlignment TextAlign = UiAlignment.Left;
    public UiAlignment Alignment = UiAlignment.Left;

    //Most of this generic field junk can go away and we can manually just return the variables. My hands were huring here so..ugh
    private static UiProps _defaults = new UiProps();//defaults are just set on the field initializer.
    public static Dictionary<UiPropName, System.Reflection.FieldInfo> Fields { get; private set; } = null;

    public UiProps()
    {
    }
    public static object? Default(UiPropName p)
    {
      return _defaults.Get(p);
    }
    public object? Get(UiPropName p)
    {
      CreateStaticFieldInfo();
      var fi = Fields[p];
      var val = fi.GetValue(this);
      return val;
    }
    public void Set(UiPropName p, object? value)
    {
      if (value == null && p != UiPropName.Texture)
      {
        //this is an error..props, besides texture, must never have null values.
        Gu.DebugBreak();
      }

      CreateStaticFieldInfo();
      var fi = Fields[p];
      fi.SetValue(this, value);
    }
    public UiProps Clone()
    {
      UiProps ret = new UiProps();

      foreach (var f in GetType().GetFields())
      {
        var v = f.GetValue(this);
        if (v != null)
        {
          f.SetValue(ret, v);
        }
      }

      return ret;
    }
    public new string ToString()
    {
      StringBuilder sb = new StringBuilder();
      foreach (var f in GetType().GetFields())
      {
        if (f.GetValue(this) != null)
        {
          sb.AppendLine($"{f.Name}      {f.GetValue(this).ToString()}");
        }
      }
      return sb.ToString();
    }
    public void Validate()
    {
      if (!Gu.AssertDebug(MinWidth >= 0))
      {
        MinWidth = 0;
      }
      if (!Gu.AssertDebug(MaxWidth >= 0))
      {
        MaxWidth = 0;
      }
      if (!Gu.AssertDebug(MinWidth <= MaxWidth))
      {
        MaxWidth = MinWidth;
      }
      if (!Gu.AssertDebug(MinHeight >= 0))
      {
        MinHeight = 0;
      }
      if (!Gu.AssertDebug(MaxHeight >= 0))
      {
        MaxHeight = 0;
      }
      if (!Gu.AssertDebug(MinHeight <= MaxHeight))
      {
        MaxHeight = MinHeight;
      }
      if (!Gu.AssertDebug(MaxValue >= MinValue))
      {
        MaxValue = MinValue;
      }
    }
    private static void CreateStaticFieldInfo()
    {
      if (Fields == null)
      {
        Fields = new Dictionary<UiPropName, System.Reflection.FieldInfo>();

        foreach (var f in typeof(UiProps).GetFields())
        {
          var pvals = Enum.GetValues(typeof(UiPropName));
          foreach (var pp in pvals)
          {
            string a = pp.ToString();
            string b = f.Name;
            if (a.Equals(b))
            {
              Fields.Add((UiPropName)pp, f);
              break;
            }
          }
        }
        //Make sure we got all fields.
        for (int i = 0; i < (int)UiPropName.MaxUiProps; ++i)
        {
          UiPropName p = (UiPropName)i;
          //You didnt add the prop to the fields.
          Gu.Assert(Fields.ContainsKey(p));
        }
      }
    }
  }
  public class UiStyle
  {
    #region Public: Aggregate Prop Setters

    public string Name { get; set; } = "";

    public float? Margin
    {
      get { return (float?)_props.Get(UiPropName.MarginTop); }
      set
      {
        MarginTop = MarginRight = MarginBot = MarginLeft = value;
      }
    }
    public float? Border
    {
      get { return (float?)_props.Get(UiPropName.BorderTop); }
      set
      {
        BorderTop = BorderRight = BorderLeft = BorderBot = value;
      }
    }
    public vec4? BorderColor
    {
      get { return (vec4?)_props.Get(UiPropName.BorderTopColor); }
      set
      {
        BorderTopColor = BorderRightColor = BorderLeftColor = BorderBotColor = value;
      }
    }
    public float? BorderRadius
    {
      get { return (float?)_props.Get(UiPropName.BorderTopLeftRadius); }
      set
      {
        BorderTopLeftRadius = BorderTopRightRadius = BorderBotRightRadius = BorderBotLeftRadius = value;
      }
    }
    public float? Padding
    {
      get { return (float?)_props.Get(UiPropName.PadTop); }
      set
      {
        PadTop = PadRight = PadBot = PadLeft = value;
      }
    }
    public float? Width
    {
      set
      {
        SetClassValue(UiPropName.MinWidth, (float?)value);
        SetClassValue(UiPropName.MaxWidth, (float?)value);
      }
    }
    public float? Height
    {
      set
      {
        SetClassValue(UiPropName.MinHeight, (float?)value);
        SetClassValue(UiPropName.MaxHeight, (float?)value);
      }
    }
    // public float? Bottom { get { return (float?)GetClassValue(UiPropName.Top) + Width; } }
    // public float? Right { get { return (float?)GetClassValue(UiPropName.Left) + Height; } }


    #endregion
    #region Public: User Prop Setters

    //Manual setters.. these will cause this style class to own this property
    //**Note: Do not use nullable<> or ? types on class types here. This will return (null) even if the class type is set on the nullable boxer.
    //OK so you could actually just return _props.Top .. etc here, but for now we're doing this to simplify things (as they are written)
    public float? Top { get { return (float?)GetClassValue(UiPropName.Top); } set { SetClassValue(UiPropName.Top, (float?)value); } }
    public float? Left { get { return (float?)GetClassValue(UiPropName.Left); } set { SetClassValue(UiPropName.Left, (float?)value); } }
    public float? MinWidth { get { return (float?)GetClassValue(UiPropName.MinWidth); } set { SetClassValue(UiPropName.MinWidth, (float?)value); } }
    public float? MinHeight { get { return (float?)GetClassValue(UiPropName.MinHeight); } set { SetClassValue(UiPropName.MinHeight, (float?)value); } }
    public float? MaxWidth { get { return (float?)GetClassValue(UiPropName.MaxWidth); } set { SetClassValue(UiPropName.MaxWidth, (float?)value); } }
    public float? MaxHeight { get { return (float?)GetClassValue(UiPropName.MaxHeight); } set { SetClassValue(UiPropName.MaxHeight, (float?)value); } }
    public float? PadTop { get { return (float?)GetClassValue(UiPropName.PadTop); } set { SetClassValue(UiPropName.PadTop, (float?)value); } }
    public float? PadRight { get { return (float?)GetClassValue(UiPropName.PadRight); } set { SetClassValue(UiPropName.PadRight, (float?)value); } }
    public float? PadBot { get { return (float?)GetClassValue(UiPropName.PadBot); } set { SetClassValue(UiPropName.PadBot, (float?)value); } }
    public float? PadLeft { get { return (float?)GetClassValue(UiPropName.PadLeft); } set { SetClassValue(UiPropName.PadLeft, (float?)value); } }
    public float? MarginTop { get { return (float?)GetClassValue(UiPropName.MarginTop); } set { SetClassValue(UiPropName.MarginTop, (float?)value); } }
    public float? MarginRight { get { return (float?)GetClassValue(UiPropName.MarginRight); } set { SetClassValue(UiPropName.MarginRight, (float?)value); } }
    public float? MarginBot { get { return (float?)GetClassValue(UiPropName.MarginBot); } set { SetClassValue(UiPropName.MarginBot, (float?)value); } }
    public float? MarginLeft { get { return (float?)GetClassValue(UiPropName.MarginLeft); } set { SetClassValue(UiPropName.MarginLeft, (float?)value); } }
    public float? BorderTop { get { return (float?)GetClassValue(UiPropName.BorderTop); } set { SetClassValue(UiPropName.BorderTop, (float?)value); } }
    public float? BorderRight { get { return (float?)GetClassValue(UiPropName.BorderRight); } set { SetClassValue(UiPropName.BorderRight, (float?)value); } }
    public float? BorderBot { get { return (float?)GetClassValue(UiPropName.BorderBot); } set { SetClassValue(UiPropName.BorderBot, (float?)value); } }
    public float? BorderLeft { get { return (float?)GetClassValue(UiPropName.BorderLeft); } set { SetClassValue(UiPropName.BorderLeft, (float?)value); } }
    public float? BorderTopLeftRadius { get { return (float?)GetClassValue(UiPropName.BorderTopLeftRadius); } set { SetClassValue(UiPropName.BorderTopLeftRadius, (float?)value); } }
    public float? BorderTopRightRadius { get { return (float?)GetClassValue(UiPropName.BorderTopRightRadius); } set { SetClassValue(UiPropName.BorderTopRightRadius, (float?)value); } }
    public float? BorderBotRightRadius { get { return (float?)GetClassValue(UiPropName.BorderBotRightRadius); } set { SetClassValue(UiPropName.BorderBotRightRadius, (float?)value); } }
    public float? BorderBotLeftRadius { get { return (float?)GetClassValue(UiPropName.BorderBotLeftRadius); } set { SetClassValue(UiPropName.BorderBotLeftRadius, (float?)value); } }
    public vec4? Color { get { return (vec4?)GetClassValue(UiPropName.Color); } set { SetClassValue(UiPropName.Color, (vec4?)value); } }
    public vec4? ColorMul { get { return (vec4?)GetClassValue(UiPropName.ColorMul); } set { SetClassValue(UiPropName.ColorMul, (vec4?)value); } }
    public vec4? BorderTopColor { get { return (vec4?)GetClassValue(UiPropName.BorderTopColor); } set { SetClassValue(UiPropName.BorderTopColor, (vec4?)value); } }
    public vec4? BorderRightColor { get { return (vec4?)GetClassValue(UiPropName.BorderRightColor); } set { SetClassValue(UiPropName.BorderRightColor, (vec4?)value); } }
    public vec4? BorderBotColor { get { return (vec4?)GetClassValue(UiPropName.BorderBotColor); } set { SetClassValue(UiPropName.BorderBotColor, (vec4?)value); } }
    public vec4? BorderLeftColor { get { return (vec4?)GetClassValue(UiPropName.BorderLeftColor); } set { SetClassValue(UiPropName.BorderLeftColor, (vec4?)value); } }
    public FontFace FontFace { get { return (FontFace)GetClassValue(UiPropName.FontFace); } set { SetClassValue(UiPropName.FontFace, (FontFace)value); } }
    public float? FontSize { get { return (float?)GetClassValue(UiPropName.FontSize); } set { SetClassValue(UiPropName.FontSize, (float?)value); } }
    public UiFontStyle? FontStyle { get { return (UiFontStyle?)GetClassValue(UiPropName.FontStyle); } set { SetClassValue(UiPropName.FontStyle, (UiFontStyle?)value); } }
    public vec4? FontColor { get { return (vec4?)GetClassValue(UiPropName.FontColor); } set { SetClassValue(UiPropName.FontColor, (vec4?)value); } }
    public float? LineHeight { get { return (float?)GetClassValue(UiPropName.LineHeight); } set { SetClassValue(UiPropName.LineHeight, (float?)value); } }
    public UiPositionMode? PositionMode { get { return (UiPositionMode?)GetClassValue(UiPropName.PositionMode); } set { SetClassValue(UiPropName.PositionMode, (UiPositionMode?)value); } }
    public UiOverflowMode? OverflowMode { get { return (UiOverflowMode?)GetClassValue(UiPropName.OverflowMode); } set { SetClassValue(UiPropName.OverflowMode, (UiOverflowMode?)value); } }
    public UiSizeMode? SizeModeWidth { get { return (UiSizeMode?)GetClassValue(UiPropName.SizeModeWidth); } set { SetClassValue(UiPropName.SizeModeWidth, (UiSizeMode?)value); } }
    public UiSizeMode? SizeModeHeight { get { return (UiSizeMode?)GetClassValue(UiPropName.SizeModeHeight); } set { SetClassValue(UiPropName.SizeModeHeight, (UiSizeMode?)value); } }
    public UiDisplayMode? DisplayMode { get { return (UiDisplayMode?)GetClassValue(UiPropName.DisplayMode); } set { SetClassValue(UiPropName.DisplayMode, (UiDisplayMode?)value); } }
    public UiImageTiling? ImageTilingX { get { return (UiImageTiling?)GetClassValue(UiPropName.ImageTilingX); } set { SetClassValue(UiPropName.ImageTilingX, (UiImageTiling?)value); } }
    public UiImageTiling? ImageTilingY { get { return (UiImageTiling?)GetClassValue(UiPropName.ImageTilingY); } set { SetClassValue(UiPropName.ImageTilingY, (UiImageTiling?)value); } }
    public MtTex Texture { get { return (MtTex)GetClassValue(UiPropName.Texture); } set { SetClassValue(UiPropName.Texture, (MtTex)value); } }
    public double? MaxValue { get { return (double?)GetClassValue(UiPropName.MaxValue); } set { SetClassValue(UiPropName.MaxValue, (double?)value); } }
    public double? MinValue { get { return (double?)GetClassValue(UiPropName.MinValue); } set { SetClassValue(UiPropName.MinValue, (double?)value); } }
    public float? ZIndex { get { return (float?)GetClassValue(UiPropName.ZIndex); } set { SetClassValue(UiPropName.ZIndex, (float?)value); } }
    public UiFloatMode? FloatMode { get { return (UiFloatMode?)GetClassValue(UiPropName.FloatMode); } set { SetClassValue(UiPropName.FloatMode, (UiFloatMode?)value); } }
    public UiRenderMode? RenderMode { get { return (UiRenderMode?)GetClassValue(UiPropName.RenderMode); } set { SetClassValue(UiPropName.RenderMode, (UiRenderMode?)value); } }
    public UiAlignment? TextAlign { get { return (UiAlignment?)GetClassValue(UiPropName.TextAlign); } set { SetClassValue(UiPropName.TextAlign, (UiAlignment?)value); } }
    public UiAlignment? Alignment { get { return (UiAlignment?)GetClassValue(UiPropName.Alignment); } set { SetClassValue(UiPropName.Alignment, (UiAlignment?)value); } }



    #endregion
    #region Public: Methods

    public UiProps _props = new UiProps();//Gets the compiled / final props
    public bool IsPropsOnly { get; set; } = false;//For glyph, don't inherit parent or compile, and re-compile the class every time.. we set _props manually
    public WeakReference<UiStyleSheet> StyleSheet { get; private set; } = null;

    private bool _bMustCompile = true;
    private long _changedFrameId = 0;
    private long _compiledFrameId = 0;
    private HashSet<WeakReference<UiElement>> _eles = null;
    private BitArray _owned = new BitArray((int)UiPropName.MaxUiProps);//This bitset tells us which props were set
    private BitArray _inherited = new BitArray((int)UiPropName.MaxUiProps);
    private BitArray _defaulted = new BitArray((int)UiPropName.MaxUiProps);
    private BitArray _changed = new BitArray((int)UiPropName.MaxUiProps);//props that changed during the last class compile
    private List<UiStyle> _superStyles = null;
    private List<string> _superStylesNames = null;//Translate this with Stylesheet.
    private bool _bMustTranslateInheritedStyles = false;

    public bool IsInline { get; set; }
#if DEBUG
    private Dictionary<UiPropName, object?> _debugChangedNamesList = null;//Properties owned (set) by this class. For visual debug
    private Dictionary<UiPropName, object?> _debugOwnedNamesList = null;//Properties owned (set) by this class. For visual debug
    private Dictionary<UiPropName, object?> _debugInheritedNamesList = null;//Properties owned (set) by this class. For visual debug
    private Dictionary<UiPropName, object?> _debugDefaultNamesList = null;//Properties owned (set) by this class. For visual debug
#endif
    public UiStyle(string name)
      : this(name, new List<string>() { })
    {
    }
    public UiStyle(string name, string inherted_style)
      : this(name, new List<string>() { inherted_style })
    {
    }
    public UiStyle(string name, List<string> inherted_styles)
    {
      Name = name;
      if (inherted_styles != null)
      {
        _superStylesNames = inherted_styles;
        _bMustTranslateInheritedStyles = true;
      }
    }
    public void SetInheritStyles(List<string> styles)
    {
      foreach (var s in styles)
      {
        InheritFrom(s);
      }
    }
    public void InheritFrom(string style)
    {
      _superStylesNames = _superStylesNames.ConstructIfNeeded();
      _superStylesNames.Add(style);
      _bMustTranslateInheritedStyles = true;
    }
    public void SetStyleSheet(UiStyleSheet s)
    {
      //swap stylesheet if already set.
      if (StyleSheet != null && StyleSheet.TryGetTarget(out var ss))
      {
        ss.RemoveStyle(this);
        StyleSheet = null;
      }
      StyleSheet = new WeakReference<UiStyleSheet>(s);
    }
    private void TranslateStyleNames(UiStyleSheet sheet)
    {
      //style names are strings so get their style classes from the stylesheet
      if (_bMustTranslateInheritedStyles == true)
      {
        Gu.Assert(sheet != null);
        _superStyles = _superStyles.ConstructIfNeeded();
        _superStyles?.Clear();
        if (_superStylesNames != null)
        {
          foreach (var sn in _superStylesNames)
          {
            var style = sheet.GetStyle(sn);
            if (style == null)
            {
              sheet.StyleError($"Style '{Name}': Could not find inherited style '{sn}'.");
            }
            else
            {
              _superStyles.Add(style);
            }
          }
        }
        _bMustTranslateInheritedStyles = false;
      }
    }
    private void DebugStorePropDetails()
    {
      // #if DEBUG
      //       //Debug / view all props that this class owns/inherits
      //       _debugChangedNamesList = _debugChangedNamesList.ConstructIfNeeded();
      //       _debugChangedNamesList.Clear();

      //       _debugOwnedNamesList = _debugOwnedNamesList.ConstructIfNeeded();
      //       _debugOwnedNamesList.Clear();

      //       _debugInheritedNamesList = _debugInheritedNamesList.ConstructIfNeeded();
      //       _debugInheritedNamesList.Clear();

      //       _debugDefaultNamesList = _debugDefaultNamesList.ConstructIfNeeded();
      //       _debugDefaultNamesList.Clear();

      //       for (int i = 0; i < (int)UiPropName.MaxUiProps; i++)
      //       {
      //         var prop = (UiPropName)i;

      //         if (_changed.Get(i))
      //         {
      //           _debugChangedNamesList.Add(prop, GetPropValue(prop));
      //         }

      //         if (_owned.Get(i))
      //         {
      //           _debugOwnedNamesList.Add(prop, GetPropValue(prop));
      //         }
      //         if (_inherited.Get(i))
      //         {
      //           _debugInheritedNamesList.Add(prop, GetPropValue(prop));
      //         }
      //         if (_defaulted.Get(i))
      //         {
      //           _debugDefaultNamesList.Add(prop, GetPropValue(prop));
      //         }
      //       }
      //       //These sets are mutually exclusive.
      //       BitArray b2 = _owned.AndWith(_defaulted);
      //       BitArray b3 = _owned.AndWith(_inherited);
      //       BitArray b4 = _inherited.AndWith(_defaulted);
      //       Gu.Assert(b2.UInt64Value() == 0);
      //       Gu.Assert(b3.UInt64Value() == 0);
      //       Gu.Assert(b4.UInt64Value() == 0);
      // #endif
    }
    public void CompileStyleTree(UiStyleSheet s, long framestamp, UiStyle style_DOM_parent = null)
    {
      //Compile.. for example: <div top="3"> <div class=" class1 class2 class1 " style="top:3" right="5">  into a single set of properties for each <div>
      // parent style (tag), <style style style> (classes), owned (inline)... <div class/style="parent stuff"> <div class="a b c b a" style="inline stuff.."
      if (!IsPropsOnly)
      {
        TranslateStyleNames(s);

        if (_bMustCompile)
        {
          //reset all prop bitflags
          _inherited.SetAll(false);
          _defaulted.SetAll(false);

          DebugStorePropDetails();

          foreach (var p in UiProps.Fields)
          {
            if (!IsOwned(p.Key))
            {
              //not owned, get value from superclass
              if (!InheritFromSuperClasses(s, p.Key, p.Value, framestamp))
              {
                //DISABLED auto parent inherit - this is annoying
                //if subclasses are not set, then try the parent DOM element, otherwise set to a default value
                //  if (!InheritFromParentTag(style_DOM_parent, p.Key, p.Value))
                {
                  //No parent element, and, no styles, set to default.
                  SetDefaultValue(p.Key, p.Value);
                }
              }
            }
          }

          DebugStorePropDetails();

          _changed.SetAll(false);
          _compiledFrameId = framestamp;
          _bMustCompile = false;
        }
      }
    }
    private bool IsOwned(UiPropName p)
    {
      return _owned.Get((int)p);
    }
    private bool InheritFromParentTag(UiStyle style_DOM_parent, UiPropName pname, System.Reflection.FieldInfo fieldinfo)
    {
      //Return true if successfully inherited.
      if (style_DOM_parent != null)
      {
        return InheritValue(style_DOM_parent, pname, fieldinfo);
      }
      return false;
    }
    private bool InheritFromSuperClasses(UiStyleSheet s, UiPropName pname, System.Reflection.FieldInfo fieldinfo, long framestamp)
    {
      //Return true if successfully inherited.
      if (_superStyles != null && _superStyles.Count > 0)
      {
        //Apply styles in the reverse order <div class="a b c " .. c..b..a
        for (int i = _superStyles.Count - 1; i >= 0; i--)
        {
          if (_superStyles[i] != null)
          {
            if (!_superStyles[i].IsInline)
            {
              _superStyles[i].CompileStyleTree(s, framestamp, null);
              return InheritValue(_superStyles[i], pname, fieldinfo);
            }
            else
            {
              s.StyleError($"'{Name}' A Super style '{_superStyles[i].Name}' was Inline, but was also inherited. This is a bug.");
              Gu.DebugBreak();
            }
          }
          else
          {
            s.StyleError($"'{Name}' A Super style was null. This is a bug.");
            Gu.DebugBreak();
          }
        }
      }
      return false;
    }
    private void SetDefaultValue(UiPropName p, System.Reflection.FieldInfo fi)
    {
      var val = UiProps.Default(p);
      fi.SetValue(this._props, val);
      _defaulted.Set((int)p, true);
    }
    private bool InheritValue(UiStyle fromStyle, UiPropName p, System.Reflection.FieldInfo fi)
    {
      //if the property is owned by the given class, then, we can set it
      //Return true if successfully inherited.
      if (fromStyle.PropIsOwnedOrInherited(p))
      {
        var val = fromStyle.GetPropValue(p);
        fi.SetValue(this._props, val);
        _inherited.Set((int)p, true);
        return true;
      }
      return false;
    }
    public bool PropIsOwnedOrInherited(UiPropName p)
    {
      var ss_owned = _owned.Get((int)p);
      var ss_inherited = _inherited.Get((int)p);
      //var ss_changed = _changed.Get((int)p);
      //Changed..is an issue we'd need an array of FrameSTamp's that determine that this element hasn't been updated.
      // Then we're duplicating tons of data, and just copying everything would be more performant than Dictionry<>
      bool ret = ss_owned || ss_inherited;
      return ret;
    }
    public UiStyle Clone()
    {
      UiStyle ret = new UiStyle(this.Name + Library.CopyName, this._superStylesNames);
      ret._props = _props.Clone();
      ret._eles = null;
      ret._bMustCompile = true;
      return ret;
    }
    public void AddReference(UiElement u)
    {
      _eles = _eles.ConstructIfNeeded();
      //Adds a reference to the given element, so when we change the style, teh element gets updated.
      foreach (var e in _eles)
      {
        if (e.TryGetTarget(out var ee))
        {
          if (ee == u)
          {
            Gu.DebugBreak();//Duplicate element reference in the style.
            return;
          }
        }
      }
      _eles.Add(new WeakReference<UiElement>(u));
    }
    public void RemoveReference(UiElement u)
    {
      if (_eles != null)
      {
        _eles.RemoveWhere((x) =>
        {
          if (x.TryGetTarget(out var ee))
          {
            return ee.Equals(u);
          }
          else
          {
            return false;
          }
        });
      }
    }

    #endregion
    #region Private: Methods
    private void SetClassValue(UiPropName p, object? value)
    {
      //Set nullable value for class. Set to null to clear/inherit value
      if (CheckValueModified(p, value))
      {
        SetClassValueDirect(p, value);
      }
    }
    private bool CheckValueModified(UiPropName p, object? new_class_value)
    {
      //Return true if
      var owned = _owned.Get((int)p);
      if (new_class_value == null && owned == false)
      {
        //Class value is unset, and was set it to null again.. no change
        return false;
      }
      else if (new_class_value != null && owned == false)
      {
        //Class value is unset, and we set a fresh value... definite change
        return true;
      }
      else if (new_class_value != null && owned == true)
      {
        //Class value is set, and we set a new value check for value difference (prevent recompiling all classes!)
        //Check hard (prop) value for new value
        var cur_prop_val = _props.Get(p);
        if (!cur_prop_val.Equals(new_class_value))
        {
          return true;
        }
      }
      return false;
    }
    private void SetClassValueDirect(UiPropName p, object? value)
    {
      //Set the class value, skipping over modified value checking.
      if (value != null)
      {
        _owned.Set((int)p, true);
        _props.Set(p, value); //Only set the prop value if not null, as, null is basically the way we say "clear the value"
      }
      else
      {
        _owned.Set((int)p, false);
      }
      _changed.Set((int)p, true);
      _changedFrameId = Gu.Context.FrameStamp;
      _bMustCompile = true;

      //SetLayoutChanged. Currently, classes are compiled for elements, which makes this sub-optimal
      //We really need to use the StyleSHeets to compile classes, then we can call SetLayoutChanged only one time for all _bMustCompile styles.
      //We'll have to figure out how to use Inline (UiElement) styles here though
      IterateElements((e) =>
      {
        e.SetContentChanged();
      });
    }
    private object? GetClassValue(UiPropName p)
    {
      //Get the class value (not compiled value)
      var owned = _owned.Get((int)p);
      if (!owned)
      {
        return null;
      }
      else
      {
        return GetPropValue(p);
      }
    }
    private object? GetPropValue(UiPropName p)
    {
      //Get the compiled / or / owned value
      return _props.Get(p);
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
    #endregion

  }
  public class UiQuads
  {
    //*render quad is the origin
    public UiQuad _b2ClipQuad = new UiQuad();      // The clip quad - all floating and contained, elements and min/max w/h. *clip quad may not equal computed quad if there are floating elements
    public UiQuad _b2LocalQuad = new UiQuad();      // local quad
    public UiQuad _b2ContentQuad = new UiQuad();        // Final quad. content without margin /border
    public UiQuad _b2MarginQuad = new UiQuad();
    public UiQuad _b2PaddingQuad = new UiQuad();
    public UiQuad _b2PreOffsetBorderQuad = new UiQuad();
    public UiQuad _b2BorderQuad = new UiQuad();        // Final quad. content area + margin + border area
    public vec2 ContentWH = new vec2(0, 0);
    public vec2 GlyphWH = new vec2(0, 0);//max width/height of all glyphs
    public vec2 OuterMaxWH = new vec2(0, 0);
    public vec2 InnerMaxWH = new vec2(0, 0);
  }
  public class UiGlyph
  {

  }
  public class UiElement
  {
    #region Classes 
    private class UiCol
    {
      //line column
      public float _top = 0;//not null depending on UiBuildOrder
      public float _left = 0;
      public float _height = 0;
      public float _width = 0;
      public List<UiElement> _eles = new List<UiElement>();
    }
    private class UiLine
    {
      public UiCol[] _cols = new UiCol[3] { new UiCol(), new UiCol(), new UiCol() };
      public float _top = 0;//not null depending on UiBuildOrder
      public float _x = 0;
      public float _height { get { return Math.Max(_cols[0]._height, Math.Max(_cols[1]._height, _cols[2]._height)); } }
      public float _width { get { return _cols[0]._width + _cols[1]._width + _cols[2]._width; } }

      public UiLine(float left, float top)
      {
        _x = left;
        _top = top;
      }
    }

    #endregion
    #region Public: Members

    List<UiElement> _glyphs = null;
    public void SetContentChanged() { _contentChanged = true; }

    public virtual string NamingPrefix { get { return "ele"; } }
    public string Name { get { return _name; } set { _name = value; } }
    public string Tag { get; set; } = "";

    public string Text
    {
      get { return _strText; }
      set
      {
        if (!StringUtil.Equals(_strText, value))
        {
          _strTextLast = _strText;
          _strText = value;
          _textChanged = true;
          _contentChanged = true;
        }
      }
    }
    public UiStyle Style
    {
      // do not create a new style for glyphs
      get
      {
        if (_style == null)
        {
          _style = new UiStyle(StyleName.Inline);
          _style.IsInline = true;
          _style.AddReference(this);
        }
        return _style;
      }
    }
    public bool Visible { get { return _visible; } set { _visible = value; } }// ** May be on style .. 
    public bool DragEnabled { get { return _dragEnabled; } private set { _dragEnabled = value; } }

    public Action<vec2>? DragFunc { get; private set; } = null;
    public Dictionary<UiEventId, List<UiAction>> Events { get { return _events; } set { _events = value; } }
    public List<UiElement>? Children { get { return _children; } }
    public UiQuad LocalQuad { get { return _quads._b2LocalQuad; } }
    public UiQuad FinalQuad { get { return _quads._b2BorderQuad; } }
    //public int TreeDepth { get { return _treeDepth; } }
    public UiElement? Parent { get { return _parent; } }
    public bool TopMost { get { return _topMost; } set { _topMost = value; } }

    #endregion
    #region Private: Members

    protected string _name = "";
    protected UiStyle? _style = null;      //Inline style
    protected List<UiElement>? _children = null;
    private UiElement? _parent = null;
    private MtFontLoader? _cachedFont = null;//For labels that contain glyphs
    private MtCachedCharData? _cachedGlyph = null;//For glyphs

    private static long s_idgen = 100;
    protected long _id = 0;
    private vec2 _tileScale = new vec2(1, 1); //Scale if UiImageSizeMode is Tile
    protected UiQuads _quads = new UiQuads();
    //private Quads _borderArea = null;
    protected Box2f? _renderOffset = null;
    public uint _iPickId = 0;
    protected string _strText = "";
    private string _strTextLast = "";
    private bool _bMustRedoTextBecauseOfStyle = false;
    private Dictionary<UiEventId, List<UiAction>>? _events = null;
    private int _treeDepth = -1; // tree depth / child depth when added
    private int _defaultSortKey = 0;

    //Flags
    private bool _pickEnabled = false;
    private bool _visible = true;
    private bool _textChanged = false;
    private bool _dragEnabled = false;
    private bool _contentChanged = true;
    private bool _topMost = false;

    #endregion
    #region Public: Methods

    public UiElement()
    {
      _id = s_idgen++;
    }
    public UiElement(string style) : this()
    {
      Init(new List<string>() { style }, null);
    }
    public UiElement(string style, string text) : this()
    {
      Init(new List<string>() { style }, null, text);
    }
    public UiElement(string name, string? style, string text) : this()
    {
      List<string> styles = new List<string>();
      if (style != null)
      {
        styles.Add(style);
      }
      Init(styles, name, text);
    }
    public UiElement(string style, Phrase p) : this()
    {
      Init(new List<string>() { style }, null, Gu.Translator.Translate(p));
    }
    public UiElement(List<string> styleClasses) : this()
    {
      Init(styleClasses, null);
    }
    public UiElement(List<string> styleClasses, Phrase phrase) : this()
    {
      Init(styleClasses, null, Gu.Translator.Translate(phrase));
    }
    public UiElement(List<string> styleClasses, string text) : this()
    {
      Init(styleClasses, null, text);
    }
    public UiElement(List<string> styleClasses, Phrase phrase, List<UiElement> children) : this()
    {
      Init(styleClasses, null, Gu.Translator.Translate(phrase), children);
    }
    public UiElement(List<string> styleClasses, string text, List<UiElement> children) : this()
    {
      Init(styleClasses, null, text, children);
    }
    public void ToggleVisible()
    {
      ShowOrHide(!_visible);
    }
    public void Hide()
    {
      ShowOrHide(false);
    }
    public void Show()
    {
      ShowOrHide(true);
    }
    public void Hide(string name)
    {
      ShowOrHideByName(name, false);
    }
    public void Show(string name)
    {
      ShowOrHideByName(name, true);
    }
    private string GetDefaultName(string? text)
    {
      if (text != null)
      {
        return NamingPrefix + text;
      }
      else
      {
        return NamingPrefix + this.GetType().Name.ToString();
      }
    }
    public bool ShowOrHideByName(string name, bool show, bool stop_at_first = false)
    {
      if (_children != null)
      {
        foreach (var ele in _children)
        {
          if (StringUtil.Equals(ele.Name, name))
          {
            ele.ShowOrHide(show);
            if (stop_at_first)
            {
              return false;
            }
          }
          else
          {
            bool stopdoing = ele.ShowOrHideByName(name, show, stop_at_first);
            if (stopdoing == false)
            {
              return false;
            }
          }
        }
      }
      return true;
    }
    public void IterateAllElements(Func<UiElement, int, LambdaBool> a)
    {
      _children?.IterateSafe(a);
      _glyphs?.IterateSafe(a);
    }
    public vec4 GetMarginAndBorder(UiDebugDraw dd)
    {
      if (dd.DisableMargins && dd.DisableBorders)
      {
        return vec4.Zero;
      }
      return new vec4(
        (dd.DisableMargins ? 0 : Style._props.MarginTop) + (dd.DisableBorders ? 0 : Style._props.BorderTop),
        (dd.DisableMargins ? 0 : Style._props.MarginRight) + (dd.DisableBorders ? 0 : Style._props.BorderRight),
        (dd.DisableMargins ? 0 : Style._props.MarginBot) + (dd.DisableBorders ? 0 : Style._props.BorderBot),
        (dd.DisableMargins ? 0 : Style._props.MarginLeft) + (dd.DisableBorders ? 0 : Style._props.BorderLeft)
      );
    }
    public vec4 GetMargin(UiDebugDraw dd)
    {
      if (dd.DisableMargins)
      {
        return vec4.Zero;
      }
      return new vec4(
        Style._props.MarginTop,
        Style._props.MarginRight,
        Style._props.MarginBot,
        Style._props.MarginLeft
      );
    }
    public vec4 GetPadding(UiDebugDraw dd)
    {
      if (dd.DisablePadding)
      {
        return vec4.Zero;
      }
      return new vec4(
        Style._props.PadTop,
        Style._props.PadRight,
        Style._props.PadBot,
        Style._props.PadLeft
      );
    }
    public vec4 GetBorder(UiDebugDraw dd)
    {
      if (dd.DisableBorders)
      {
        return vec4.Zero;
      }
      return new vec4(
        Style._props.BorderTop,
        Style._props.BorderRight,
        Style._props.BorderBot,
        Style._props.BorderLeft
      );
    }
    public vec4 GetBorderRadius(UiDebugDraw dd)
    {
      if (dd.DisableBorders)
      {
        return vec4.Zero;
      }
      return new vec4(
        Style._props.BorderTopLeftRadius,
        Style._props.BorderTopRightRadius,
        Style._props.BorderBotRightRadius,
        Style._props.BorderBotLeftRadius
      );
    }
    public UiElement AddChild(string stylename)
    {
      return AddChild(new UiElement(stylename));
    }
    public UiElement AddChild(UiElement e)
    {
      Gu.Assert(e != null);
      Gu.Assert(this != e);

      e._parent?.RemoveChild(e);
      if (_children == null)
      {
        _children = new List<UiElement>();
      }
      _children.Add(e);
      e._parent = this;

      e.UpdateSortKeys();
      e.OnAddedToParent(this);

      return e;
    }
    public bool RemoveChild(UiElement e)
    {
      bool ret = false;
      if (_children != null)
      {
        if (_children.Remove(e))
        {
          e._parent = null;
          e._treeDepth = -1;//this doesnt really matert
          ret = true;
        }
        if (_children.Count == 0)
        {
          _children = null;
        }
      }
      //could not remove.
      return ret;
    }
    public int DefaultSortKey { get { return _defaultSortKey; } }
    private void UpdateSortKeys()
    {
      // //this may not be needed -- remove if we dont use ti

      // if ((_children == null || _children.Count == 0) && (_glyphs == null || _glyphs.Count == 0))
      // {
      //   return;
      // }

      // int c_iSortFactor = 10000;

      // IterateAllElements((e, e_idx) =>
      // {
      //   if (e.Parent == null)
      //   {
      //     e._treeDepth = 0;
      //   }
      //   else
      //   {
      //     e._treeDepth = e.Parent._treeDepth;
      //   }

      //   e._defaultSortKey = e._treeDepth * c_iSortFactor + e_idx;

      //   e.UpdateSortKeys();
      //   return LambdaBool.Continue;
      // });
    }
    public void ClearChildren()
    {
      _children?.Clear();
    }
    public void EnableDrag(Action<vec2> func)
    {
      DragEnabled = true;
      DragFunc = func;
    }
    public void DisableDrag()
    {
      DragEnabled = false;
    }
    public void DoMouseEvents(UiEvent e, bool iswindow = false)
    {
      if (_events.TryGetValue(e.EventId, out var acts))
      {
        foreach (var act in acts)
        {
          act?.Invoke(e);
        }
      }
    }
    public UiElement Click(UiAction f)
    {
      AddEvent(UiEventId.LmbRelease, f);
      return this;
    }
    public void AddEvent(UiEventId evId, UiAction f)
    {
      //add an additional event
      SetOrAddEvent(evId, f, false);
    }
    public void SetEvent(UiEventId evId, UiAction f)
    {
      //erase all events and set the new one
      SetOrAddEvent(evId, f, true);
    }
    private void SetOrAddEvent(UiEventId evId, UiAction f, bool set)
    {
      Gu.Assert(f != null);
      if (set)
      {
        _events.Remove(evId);
      }
      _events = _events.ConstructIfNeeded();
      List<UiAction>? acts = null;
      if (!_events.TryGetValue(evId, out acts))
      {
        acts = new List<UiAction>();
        _events.Add(evId, acts);
      }
      acts.Add(f);
      _pickEnabled = true;

      //Automatic pick root here - this will override GUI root as pick root
      _iPickId = Gu.Context.Renderer.Picker.GenPickId();
    }
    public bool RemoveEvents(UiEventId evId, string tag)
    {
      //remove all events of id
      bool ret = _events.Remove(evId);
      if (_events.Count == 0)
      {
        _pickEnabled = false;
        _iPickId = Picker.c_iInvalidPickId;
      }
      return ret;
    }
    public int FindParent(UiElement e, int depth = 9)
    {
      //Returns [0,depth) if found the given element based on depth, -1 for not found
      var p = this.Parent;
      int level = 0;
      while (p != null)
      {
        if (p == e)
        {
          break;
        }
        level++;
        if (level == depth)
        {
          level = -1;
          break;
        }
        p = p.Parent;
      }
      if (p == null)
      {
        level = -1;
      }

      return level;
    }
    #endregion
    #region Private/Protected: Methods

    protected virtual void OnAddedToParent(UiElement parent) { }

    private void Init(List<string> styleClasses, string? name = null, string? phrase = null, List<UiElement> children = null)
    {
      this.Style.SetInheritStyles(styleClasses);
      if (phrase != null)
      {
        Text = phrase;
      }
      if (name == null)
      {
        _name = GetDefaultName(phrase);
      }
      else
      {
        _name = name;
      }
      if (children != null)
      {
        foreach (var c in children)
        {
          AddChild(c);
        }
      }
    }
    private bool IsFullyClipped(UiQuad b2ClipRect)
    {
      var ret = false;
      // This simple test saves us a ton of GPU pixel tests

      if (Style._props.OverflowMode == UiOverflowMode.Hide)
      {
        if (this._quads._b2ClipQuad.Max.x < b2ClipRect.Min.x)
        {
          ret = true;
        }
        if (this._quads._b2ClipQuad.Max.y < b2ClipRect.Min.y)
        {
          ret = true;
        }
        if (this._quads._b2ClipQuad.Min.x > b2ClipRect.Max.x)
        {
          ret = true;
        }
        if (this._quads._b2ClipQuad.Min.y > b2ClipRect.Max.y)
        {
          ret = true;
        }
      }

      return ret;
    }
    private UiQuad ShrinkClipRect(UiQuad parentClip)
    {
      //clip children that go beyond this container.
      UiQuad ret = parentClip;
      if (Style._props.FloatMode == UiFloatMode.Floating)
      {
        //floating elements go beyond parents
        ret = _quads._b2ClipQuad;
      }
      else if (Style._props.OverflowMode == UiOverflowMode.Hide)
      {
        ret.ShrinkByBox(_quads._b2ClipQuad);
        ret.Validate(true, 0);
      }
      return ret;
    }
    protected virtual void PerformLayout_SizeElements(MegaTex mt, bool bForce, vec2 parentMaxWH, UiStyle? parent, UiStyleSheet sheet, long framesatmp, UiDebugDraw dd)
    {
      //if (_layoutChanged || bForce)
      {
        Style.CompileStyleTree(sheet, framesatmp, parent);
        Style._props.Validate();

        // delay to update glyphs this is taking much less time
        if ((_textChanged || _bMustRedoTextBecauseOfStyle) && (framesatmp % 5 == 0))
        {
          UpdateGlyphs(mt, _textChanged && !_bMustRedoTextBecauseOfStyle);
          _bMustRedoTextBecauseOfStyle = false;
          _textChanged = false;
        }

        if (Style._props.MaxWidth < 0 || Style._props.MaxHeight < 0 || Style._props.MaxWidth < Style._props.MinWidth)
        {
          //must fix style
          Gu.DebugBreak();
        }

        //shrink max rect by parent 
        //remove margins from maxwh before sending into child, then compute our w/h by removing padding from our parent maxwh
        _quads.OuterMaxWH = new vec2(
          Math.Max(Math.Min(parentMaxWH.width, Style._props.MaxWidth), 0),
          Math.Max(Math.Min(parentMaxWH.height, Style._props.MaxHeight), 0)
        );
        //all elements & ele pads + parent margin *margin sizes in the layout lines
        _quads.ContentWH = _quads.GlyphWH; //start with max wh of all glyphs

        //remove margins for child
        var mb = this.GetMarginAndBorder(dd);
        _quads.InnerMaxWH = new vec2(
          Math.Max(_quads.OuterMaxWH.width - mb.left - mb.right, 0),
          Math.Max(_quads.OuterMaxWH.height - mb.top - mb.bot, 0)
        );

        //size, then layout children
        List<UiLine> vecLines = new List<UiLine>();
        vecLines.Add(new UiLine(0, 0));
        int lineidx = 0;

        if (_children != null && _children.Count > 0)
        {
          //compute min content WH first
          foreach (var ele in _children)
          {
            if (ele.Visible)
            {
              ele.PerformLayout_SizeElements(mt, bForce, _quads.InnerMaxWH, this.Style, sheet, framesatmp, dd);

              if (ele.Style._props.FloatMode != UiFloatMode.Floating)
              {
                if (ele.Style._props.PositionMode == UiPositionMode.Relative)
                {
                  //relative elements dont respect margin/padding
                  _quads.ContentWH.x = Math.Max(_quads.ContentWH.x, ele._quads._b2LocalQuad._left + ele._quads._b2LocalQuad._width);
                  _quads.ContentWH.y = Math.Max(_quads.ContentWH.y, ele._quads._b2LocalQuad._top + ele._quads._b2LocalQuad._height);
                }
                else if (ele.Style._props.PositionMode == UiPositionMode.Absolute)
                {
                  //not sure.. we are in relative coords right now
                  _quads.ContentWH.x = Math.Max(_quads.ContentWH.x, ele._quads._b2LocalQuad._width);
                  _quads.ContentWH.y = Math.Max(_quads.ContentWH.y, ele._quads._b2LocalQuad._height);
                }
                else if (ele.Style._props.PositionMode == UiPositionMode.Static)
                {
                  _quads.ContentWH.x = Math.Max(_quads.ContentWH.x, ele._quads._b2LocalQuad._width);
                  _quads.ContentWH.y = Math.Max(_quads.ContentWH.y, ele._quads._b2LocalQuad._height);
                }
              }

            }
          }

          //the algorithm is to layout center, then LR, and not move center, wrap LR but
          // for now try to do this without buckets - just sort into columns based on child order, we
          //we can have a WrapMode for each column to prevent erroneous wrapping 
          //layout with min/max

          //layout all 3 columns to the left, then modify element position later
          lineidx = 0;
          foreach (var ele in _children)
          {
            if (ele.Visible && ele.Style._props.PositionMode == UiPositionMode.Static)
            {
              LayoutStaticElement(ele, Style._props.Alignment, vecLines, _quads.InnerMaxWH, _quads.ContentWH, dd, ref lineidx);
            }
          }


        }
        //**TODO: Unified UI - Glyphs + Elements must be in the same list, & Glyphs must be a BASE class for UiELement and have no _props

        lineidx = 0;
        if (_glyphs != null && _glyphs.Count > 0)
        {
          foreach (var ele in _glyphs)
          {
            if (ele.Style._props.PositionMode == UiPositionMode.Static && ele.Visible == true)
            {
              LayoutStaticElement(ele, Style._props.TextAlign, vecLines, _quads.InnerMaxWH, _quads.ContentWH, dd, ref lineidx);
            }
          }
        }

        //adjust line offsets for center/right
        //note:basically we need to sort the eles into buckets anyway, so the algorithm woulr
        //bneefit from having hte center column go first
        foreach (var line in vecLines)
        {
          var linew = line._cols[0]._width + line._cols[1]._width + line._cols[2]._width;
          foreach (var ele in line._cols[(int)UiAlignment.Center]._eles)
          {
            ele._quads._b2LocalQuad._left += _quads.InnerMaxWH.width / 2 - line._cols[(int)UiAlignment.Center]._width / 2;
          }
          foreach (var ele in line._cols[(int)UiAlignment.Right]._eles)
          {
            if (Translator.TextFlow == LanguageTextFlow.Left)
            {
              //arabic text
              ele._quads._b2LocalQuad._left = _quads.InnerMaxWH.width - ele._quads._b2LocalQuad._left - ele._quads._b2LocalQuad._width;
            }
            else
            {
              //roman
              ele._quads._b2LocalQuad._left += _quads.InnerMaxWH.width - line._cols[(int)UiAlignment.Right]._width - ele._quads._b2LocalQuad._width;
            }

          }
        }


        //TODO: proably putting text on separate layer
        //Calculate content size
        float totalHeight = mb.top + mb.bot;
        foreach (var line in vecLines)
        {
          //add the center/right offsets
          totalHeight += line._height;
          _quads.ContentWH.x = Math.Max(_quads.ContentWH.x, line._width + mb.right + mb.left);
        }
        _quads.ContentWH.y = Math.Max(_quads.ContentWH.y, totalHeight);

        SizeElement(_quads.ContentWH, _quads.OuterMaxWH, dd);

        _quads._b2LocalQuad.Validate();

      }
    }
    protected void PerformLayout_PositionElements(bool bForce, UiDebugDraw dd, ReverseGrowList<v_v4v4v4v2u2v4v4> verts, UiQuad parentClip, MtTex defaultPixel,
      uint rootPickId, ref Dictionary<uint, UiElement>? pickable)
    {
      //Position elements after size and relative position calculated
      //clip regions must be calculated on the position step
      //if (_layoutChanged || bForce)
      {
        ComputeQuads(dd);

        //Set pick root 
        uint pickId = rootPickId;
        if (_pickEnabled)
        {
          pickId = _iPickId;

          if (Gu.AssertDebug(pickId != Picker.c_iInvalidPickId))
          {
            pickable = pickable.ConstructIfNeeded();
            pickable.Add(pickId, this);
          }
        }

        //Overlyay
        //the overlay isnt working right now due to sorting/draw order issue
        var savedcolor = dd.OverlayColor;
        var t = dd.OverlayColor.x; //flipflop color for sub-elements
        dd.OverlayColor.x = dd.OverlayColor.y;
        dd.OverlayColor.y = dd.OverlayColor.z;
        dd.OverlayColor.z = t;

        //copy, shrink clip rect 
        UiQuad clip = ShrinkClipRect(parentClip);

        if (_children != null && _children.Count > 0)
        {
          foreach (var ele in _children)
          {
            if (ele.Visible)
            {
              ele.PerformLayout_PositionElements(bForce, dd, verts, clip, defaultPixel, pickId, ref pickable);

              //expand clip
              _quads._b2ClipQuad.ExpandByPoint(ele._quads._b2ClipQuad.Min);
              _quads._b2ClipQuad.ExpandByPoint(ele._quads._b2ClipQuad.Max);
            }
          }
        }
        if (_glyphs != null)
        {
          foreach (var ele in _glyphs)
          {
            ele.PerformLayout_PositionElements(bForce, dd, verts, clip, defaultPixel, pickId, ref pickable);

            //expand clip
            _quads._b2ClipQuad.ExpandByPoint(ele._quads._b2ClipQuad.Min);
            _quads._b2ClipQuad.ExpandByPoint(ele._quads._b2ClipQuad.Max);
          }
        }

        dd.OverlayColor = savedcolor;

        if (Style._props.FloatMode == UiFloatMode.Floating)
        {
          GetOpenGLQuadVerts(verts, _quads._b2ClipQuad, defaultPixel, pickId, dd);
        }
        else if (IsFullyClipped(parentClip) == false)
        {
          GetOpenGLQuadVerts(verts, parentClip, defaultPixel, pickId, dd);
        }

        _contentChanged = false;

      }
    }
    private bool ShrinkExpanderW()
    {
      //shrink expander to minimum width of parent children, if parent is a shrinking element
      //else - grow child to parent boundary 
      return (Style._props.SizeModeWidth == UiSizeMode.Expand && Parent != null && Parent.Style._props.SizeModeWidth == UiSizeMode.Shrink);
    }
    private bool ShrinkExpanderH()
    {
      return (Style._props.SizeModeHeight == UiSizeMode.Expand && Parent != null && Parent.Style._props.SizeModeHeight == UiSizeMode.Shrink);
    }
    private void SizeElement(vec2 contentWH, vec2 outerMaxWH, UiDebugDraw dd)
    {
      //Compute content minimum width/height of static element to compute size of parent
      //Size is preliminary and static elements will be shortened up to their content size if they go outside parent boundary
      //conttnetwh is min wh 
      var epad = GetPadding(dd);

      //shrink expanders if parent controls child expanders
      bool shrinkExpanderW = ShrinkExpanderW();
      bool shrinkExpanderH = ShrinkExpanderH();

      if (Style._props.SizeModeWidth == UiSizeMode.Shrink || shrinkExpanderW)
      {
        //shrnk to size of contents
        _quads._b2LocalQuad._width = contentWH.width;
      }
      else if (Style._props.SizeModeWidth == UiSizeMode.Expand && !shrinkExpanderW)
      {
        //take up 100% of parent max if Parent is ShrinkMax, or maximum of parent content if parent is ShrinkContent
        _quads._b2LocalQuad._width = Math.Max(outerMaxWH.width - epad.left - epad.right, contentWH.width);
      }
      else if (Style._props.SizeModeWidth == UiSizeMode.Fixed)
      {
        //note: max=min when fixed
        _quads._b2LocalQuad._width = Style._props.MaxWidth - Style._props.MinWidth;
      }

      if (Style._props.SizeModeHeight == UiSizeMode.Shrink || shrinkExpanderH)
      {
        _quads._b2LocalQuad._height = contentWH.height;
      }
      else if (Style._props.SizeModeHeight == UiSizeMode.Expand && !shrinkExpanderH)
      {
        _quads._b2LocalQuad._height = Math.Max(outerMaxWH.height - epad.top - epad.bot, contentWH.height);
      }
      else if (Style._props.SizeModeHeight == UiSizeMode.Fixed)
      {
        _quads._b2LocalQuad._height = Style._props.MaxHeight - Style._props.MinHeight;
      }

      //maxw/h are the penultimate parameters and you cant go past them even if clipping happens
      _quads._b2LocalQuad._width = Math.Clamp(_quads._b2LocalQuad._width, Style._props.MinWidth, Style._props.MaxWidth);
      _quads._b2LocalQuad._height = Math.Clamp(_quads._b2LocalQuad._height, Style._props.MinHeight, Style._props.MaxHeight);

      _quads._b2LocalQuad.Validate();
    }
    private void LayoutStaticElement(UiElement ele, UiAlignment align, List<UiLine> vecLines, vec2 pmaxInnerWH, vec2 pcontentWH, UiDebugDraw dd, ref int lineidx)
    {
      //compute static element left/top
      if (vecLines.Count == 0)
      {
        Gu.BRThrowException("GUI error - tried to run calc algorithm without any UILines created");
      }

      if (ele.ShrinkExpanderW())
      {
        //parent=shrink, child=grow (race condition), set child to min content
        ele._quads._b2LocalQuad._width = Math.Min(pcontentWH.width, pmaxInnerWH.width);
      }
      if (ele.ShrinkExpanderH())
      {
        ele._quads._b2LocalQuad._height = Math.Min(pcontentWH.height, pmaxInnerWH.height);
      }

      UiLine line = vecLines[lineidx];
      var e_width = ele._quads._b2LocalQuad._width;
      var e_pad = ele.GetPadding(dd);
      float pspacex = pmaxInnerWH.x;//maximally equal to the Screen WH

      bool bLineBreak = false;
      if (ele.Style._props.DisplayMode == UiDisplayMode.Inline)
      {
        float e_tot_w = e_pad.left + e_pad.right + e_width; //correct because we remove padding from grow elements
        if (e_tot_w + line._width > pspacex) //For label - auto width + expand. ?? 
        {
          // if (line._width > 0)
          {
            bLineBreak = true;
          }
        }
      }
      else if (ele.Style._props.DisplayMode == UiDisplayMode.Block)
      {
        //For /n in text. or block elements. (html block elements will go past parents y and may clip)
        bLineBreak = true;
      }
      else if (ele.Style._props.DisplayMode != UiDisplayMode.InlineNoWrap)
      {
        bLineBreak = false;
      }

      if (bLineBreak)
      {
        UiLine line2;
        if (lineidx + 1 >= vecLines.Count)
        {
          // new line
          line2 = new UiLine(0, line._top + line._height);
          vecLines.Add(line2);
        }
        else
        {
          line2 = vecLines[lineidx + 1];
        }
        lineidx++;
        line = vecLines[lineidx];
      }

      var pmarb = this.GetMarginAndBorder(dd);

      ele._quads._b2LocalQuad._left = line._x + line._width + pmarb.left + e_pad.left;
      ele._quads._b2LocalQuad._top = line._top + pmarb.top + e_pad.top;
      line._cols[(int)align]._width += e_width + e_pad.left + e_pad.right;
      line._cols[(int)align]._height = Math.Max(line._height, ele._quads._b2LocalQuad._height + e_pad.top + e_pad.bot);
      line._cols[(int)align]._eles.Add(ele);

      ele._quads._b2LocalQuad.Validate();
    }
    // private void LayoutLeft(UiElement ele, float e_width, vec4 e_pad, bool bLineBreak, UiLine line,
    //                         List<UiLine> vecLines, vec2 pmaxInnerWH, vec2 pcontentWH, UiDebugDraw dd, vec4 pmarb, ref int lineidx)
    // {
    //   ele._quads._b2LocalQuad._left = line._x + line._width + pmarb.left + e_pad.left;
    // }
    // private void LayoutRight(UiElement ele, float e_width, vec4 e_pad, bool bLineBreak, UiLine line,
    //                          List<UiLine> vecLines, vec2 pmaxInnerWH, vec2 pcontentWH, UiDebugDraw dd, vec4 pmarb, ref int lineidx)
    // {
    //   //if the line is full the element gets appended to the end of the full line ,essentially gets clipped
    //   ele._quads._b2LocalQuad._left = line._x - line._width + pmarb.right + e_pad.right + e_width;

    //   // ele._quads._b2LocalQuad._top = line._top + pmarb.top + e_pad.top;
    //   // line._cols[2]._width += e_width + e_pad.left + e_pad.right;
    //   // line._cols[2]._height = Math.Max(line._height, ele._quads._b2LocalQuad._height + e_pad.top + e_pad.bot);
    // }
    // private void LayoutRight(UiElement ele, float e_width, vec4 e_pad, bool bLineBreak, UiLine line,
    //                         List<UiLine> vecLines, vec2 pmaxInnerWH, vec2 pcontentWH, UiDebugDraw dd)
    // {
    //   if (bLineBreak)
    //   {
    //     // new line
    //     UiLine line2 = new UiLine(0, line._top + line._height);
    //     vecLines.Add(line2);
    //     line = vecLines[vecLines.Count - 1];
    //   }

    //   var pmarb = this.GetMarginAndBorder(dd);

    //   ele._quads._b2LocalQuad._left = line._left + line._width + e_pad.left + pmarb.left;
    //   ele._quads._b2LocalQuad._top = line._top + e_pad.top + pmarb.top;
    //   line._width += e_width + e_pad.left + e_pad.right;
    //   line._height = Math.Max(line._height, ele._quads._b2LocalQuad._height + e_pad.top + e_pad.bot);

    //   ele._quads._b2LocalQuad.Validate();

    //   line._eles.Add(ele);
    // }
    private void ConstrainValue(float min, float max, ref float x, float size)
    {
      //@param x = ele position (x,y) size = ele w/h
      //@param min/max = parent min/max
      if (min <= max)
      {
        max = min;
        Gu.DebugBreak();//error:max:=min
      }

      if ((x + size) > (max - min))
      {
        x = (max - min) - size;
      }
      if (x < min)
      {
        x = min;
      }
    }
    protected void ComputeQuads(UiDebugDraw dd)
    {
      //Add parent offsets to child quads.
      float w1 = 1.0f, h1 = 1.0f;
      w1 = 1;//UiScreen::getDesignMultiplierW();
      h1 = 1;//UiScreen::getDesignMultiplierH();

      //Position relative/float elements to absolute pixels
      if (this.Style._props.PositionMode == UiPositionMode.Relative || this.Style._props.PositionMode == UiPositionMode.Static)
      {
        this._quads._b2BorderQuad._left = this._quads._b2LocalQuad._left;
        this._quads._b2BorderQuad._top = this._quads._b2LocalQuad._top;
        if (_parent != null)
        {
          this._quads._b2BorderQuad._left += _parent._quads._b2BorderQuad._left;
          this._quads._b2BorderQuad._top += _parent._quads._b2BorderQuad._top;
        }
      }
      else if (this.Style._props.PositionMode == UiPositionMode.Absolute)
      {
        this._quads._b2BorderQuad._left = this._quads._b2LocalQuad._left = this.Style._props.Left;
        this._quads._b2BorderQuad._top = this._quads._b2LocalQuad._top = this.Style._props.Top;
      }

      this._quads._b2BorderQuad._width = this._quads._b2LocalQuad._width;
      this._quads._b2BorderQuad._height = this._quads._b2LocalQuad._height;

      //initial clip

      if (this._quads._b2LocalQuad._left > 99999 || this._quads._b2LocalQuad._width > 99999) { Gu.DebugBreak(); }

      if (_renderOffset != null && disableoff == false)
      {
        //For glyphs, and other elements that go outside their physical regions
        vec2 origin = new vec2(
          //appears the horizontal position is only the horizontal center,
          _quads._b2BorderQuad._left + _quads._b2BorderQuad._width / 2,
          _quads._b2BorderQuad._top
        );
        var ro = _renderOffset.Value;
        _quads._b2PreOffsetBorderQuad = _quads._b2BorderQuad.Clone();
        var cpy = _quads._b2BorderQuad.Clone();
        float minx = origin.x + ro.Left - cpy._width / 2f;
        float miny = origin.y + ro.Top + cpy._height / 1.25f;
        float maxx = origin.x + ro.Right - cpy._width / 2f;
        float maxy = origin.y + ro.Bottom + cpy._height / 1.25f;
        _quads._b2BorderQuad._left = minx;
        _quads._b2BorderQuad._top = miny;
        _quads._b2BorderQuad._width = maxx - minx;
        _quads._b2BorderQuad._height = maxy - miny;
      }

      // Set to false if we're controllig coordinates of this element (cursor, or window position)
      this._quads._b2BorderQuad._left *= w1;
      this._quads._b2BorderQuad._top *= h1;
      this._quads._b2BorderQuad._width *= w1;
      this._quads._b2BorderQuad._height *= h1;

      this._quads._b2ClipQuad = this._quads._b2BorderQuad;

      this._quads._b2BorderQuad.Validate();

      //separate the border quad from the content area quad

      this._quads._b2ContentQuad = this._quads._b2BorderQuad;

      var bd = this.GetBorder(dd);
      this._quads._b2ContentQuad._left += bd.left;
      this._quads._b2ContentQuad._top += bd.top;
      this._quads._b2ContentQuad._width -= (bd.left + bd.right);
      this._quads._b2ContentQuad._height -= (bd.top + bd.bot);
      this._quads._b2ContentQuad.Validate();

      var mg = this.GetMargin(dd);
      this._quads._b2MarginQuad = this._quads._b2BorderQuad.Clone();
      // this._quads._b2MarginQuad._left -= mg.left;
      // this._quads._b2MarginQuad._top -= mg.top;
      // this._quads._b2MarginQuad._width -= (mg.right+ mg.left);
      // this._quads._b2MarginQuad._height -= (mg.bot+mg.top);

      var pd = this.GetPadding(dd);
      this._quads._b2PaddingQuad = this._quads._b2BorderQuad.Clone();
      this._quads._b2PaddingQuad._left -= pd.left;
      this._quads._b2PaddingQuad._top -= pd.top;
      this._quads._b2PaddingQuad._width += (pd.right + pd.left);
      this._quads._b2PaddingQuad._height += (pd.bot + pd.top);


    }
    public static bool disableoff = false;
    private void GetOpenGLQuadVerts(ReverseGrowList<v_v4v4v4v2u2v4v4> all_verts, UiQuad b2ClipRect, MtTex defaultPixel, uint rootPickId, UiDebugDraw dd)
    {
      if (Visible == false)
      {
        return;
      }
      if ((Style._props.RenderMode == UiRenderMode.None) && (_cachedGlyph == null))
      {
        //invisible, or container element
        return;
      }

      //**Texture Adjust - modulating repeated textures causes seaming issues, especially with texture filtering
      // adjust the texture coordinates by some pixels to account for that.  0.5f seems to work well.
      float adjust = 0;// 1.4f;  // # of pixels to adjust texture by


      var bd = GetBorder(dd);
      var radius = this.GetBorderRadius(dd);

      //Debug overlay
      if (dd.ShowOverlay)
      {
        //preoffset border
        v_v4v4v4v2u2v4v4 dbgv = new v_v4v4v4v2u2v4v4();
        SetVertexRasterArea(ref dbgv, in _quads._b2PreOffsetBorderQuad, in b2ClipRect, dd);
        dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
        dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
        dbgv._quadrant = new vec3(0, 0, 999);
        ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
        SetVertexPickAndColor(ref dbgv, (new vec4(1, 0, 0, .3f)), rootPickId);
        all_verts.Add(dbgv);//This is because of the new sorting issue

        SetVertexRasterArea(ref dbgv, in _quads._b2ContentQuad, in b2ClipRect, dd);
        dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
        dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
        dbgv._quadrant = new vec3(0, 0, 999);
        ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
        SetVertexPickAndColor(ref dbgv, (new vec4(0, 1, 0, .3f)), rootPickId);
        all_verts.Add(dbgv);//This is because of the new sorting issue

        SetVertexRasterArea(ref dbgv, in _quads._b2BorderQuad, in b2ClipRect, dd);
        dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
        dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
        dbgv._quadrant = new vec3(0, 0, 999);
        ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
        SetVertexPickAndColor(ref dbgv, (new vec4(1, 0, 1, .3f)), rootPickId);
        all_verts.Add(dbgv);//This is because of the new sorting issue  

        SetVertexRasterArea(ref dbgv, in _quads._b2PaddingQuad, in b2ClipRect, dd);
        dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
        dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
        dbgv._quadrant = new vec3(0, 0, 999);
        ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
        SetVertexPickAndColor(ref dbgv, (new vec4(1, 1, 0, 0.4)), rootPickId);
        all_verts.Add(dbgv);//This is because of the new sorting issue        
      }

      //Content Quad
      v_v4v4v4v2u2v4v4 vc = new v_v4v4v4v2u2v4v4();
      vc._rtl_rtr = new vec4(radius.top, radius.right);
      vc._rbr_rbl = new vec4(radius.bot, radius.left);
      vc._quadrant = new vec3(0, 0, 999);
      SetVertexRasterArea(ref vc, in _quads._b2ContentQuad, in b2ClipRect, dd);
      if (_cachedGlyph != null)
      {
        ComputeVertexGlyphTCoord(ref vc, _cachedGlyph, adjust);
      }
      else
      {
        MtTex tex = null;
        if (Style._props.RenderMode == UiRenderMode.Color)
        {
          tex = defaultPixel;
        }
        else if (Style._props.Texture != null)
        {
          tex = Style._props.Texture;
        }
        else
        {
          Gu.Log.Error($"{Name}: UI: Render mode is texture, but texture was not set. Changing to color.");
          tex = defaultPixel;
          Gu.DebugBreak();
        }

        ComputeVertexTexcoord(ref vc, tex, Style._props.ImageTilingX, Style._props.ImageTilingY, adjust);
      }
      SetVertexPickAndColor(ref vc, (Style._props.Color * Style._props.ColorMul).Clamp(0.0f, 1.0f), rootPickId);
      all_verts.Add(vc);//This is because of the new sorting issue

      //TODO: border radius is still broken 
      //Borders here don't work with border radius.

      //Border
      DoBorders(bd, radius, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, false);
    }
    private void DoBorders(vec4 bd, vec4 radius, ReverseGrowList<v_v4v4v4v2u2v4v4> all_verts,
                           UiQuad b2ClipRect, MtTex defaultPixel, uint rootPickId, UiDebugDraw dd, bool usequad)
    {
      //Quadrant version works with border radius but may not produce correct border
      //Accurate version make perfect borders without radius.
      if (!usequad)
      {
        radius = new vec4(0, 0, 0, 0);
      }
      if (bd.top > 0)
      {
        UiQuad bt = _quads._b2BorderQuad.Clone();
        if (!usequad)
        {
          bt._height = _quads._b2ContentQuad._top - _quads._b2BorderQuad._top;
        }
        var r2 = usequad ? new vec4(radius.x, radius.y, 0, 0) : radius;
        DoBorder(bt, Style._props.BorderTopColor, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(0, 1, usequad ? 0 : 999));
      }
      if (bd.right > 0)
      {
        UiQuad bt = _quads._b2BorderQuad.Clone();
        if (!usequad)
        {
          bt._left += _quads._b2ContentQuad._width;
          bt._width -= _quads._b2ContentQuad._width;
        }
        var r2 = usequad ? new vec4(0, radius.y, radius.z, 0) : radius;
        DoBorder(bt, Style._props.BorderRightColor, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(1, 0, usequad ? 0 : 999));
      }
      if (bd.bot > 0)
      {
        UiQuad bt = _quads._b2BorderQuad.Clone();
        if (!usequad)
        {
          bt._top += _quads._b2ContentQuad._height;
          bt._height -= _quads._b2ContentQuad._height;
        }
        var r2 = usequad ? new vec4(0, 0, radius.z, radius.w) : radius;
        DoBorder(bt, Style._props.BorderBotColor, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(0, -1, usequad ? 0 : 999));
      }
      if (bd.left > 0)
      {
        UiQuad bt = _quads._b2BorderQuad.Clone();
        if (!usequad)
        {
          bt._width = _quads._b2ContentQuad._left - _quads._b2BorderQuad._left;
        }
        var r2 = usequad ? new vec4(radius.x, 0, 0, radius.w) : radius;
        DoBorder(bt, Style._props.BorderLeftColor, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(-1, 0, usequad ? 0 : 999));
      }
    }
    private void DoBorder(UiQuad borderquad, vec4 bodfercolor, vec4 radius, ReverseGrowList<v_v4v4v4v2u2v4v4> all_verts,
                          UiQuad b2ClipRect, MtTex defaultPixel, uint rootPickId, UiDebugDraw dd, vec3 quadrant)
    {
      v_v4v4v4v2u2v4v4 vb = new v_v4v4v4v2u2v4v4();
      vb._rtl_rtr = new vec4(radius.top, radius.right);
      vb._rbr_rbl = new vec4(radius.bot, radius.left);
      vb._quadrant = quadrant;
      SetVertexRasterArea(ref vb, in borderquad, in b2ClipRect, dd);
      ComputeVertexTexcoord(ref vb, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
      SetVertexPickAndColor(ref vb, bodfercolor, rootPickId);
      all_verts.Add(vb);//This is because of the new sorting issue
    }
    private void ComputeVertexTexcoord(ref v_v4v4v4v2u2v4v4 vc, MtTex pTex, UiImageTiling xtile, UiImageTiling ytile, float adjust)
    {
      Box2f q2Tex = new Box2f();
      Gu.Assert(pTex != null);

      if (xtile == UiImageTiling.Expand)
      {
        q2Tex._min.x = pTex.uv0.x;
        q2Tex._max.x = pTex.uv1.x;
      }
      else if (xtile == UiImageTiling.Tile)
      {
        float wPx = _quads._b2LocalQuad._width;
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
        float hPx = _quads._b2LocalQuad._height;
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

      float w1px = 0;                  // 1 pixel subtract from the u/v to prevent creases during texture modulation
      float h1px = 0;

      if (pTex.GetWidth() > 0 && vc._texsiz.x > 0)
      {
        w1px = 1.0f / pTex.GetWidth();
        w1px *= vc._texsiz.x;
        w1px *= adjust;
      }
      if (pTex.GetHeight() > 0 && vc._texsiz.y > 0)
      {
        h1px = 1.0f / pTex.GetHeight();
        h1px *= vc._texsiz.y;
        h1px *= adjust;
      }
      vc._texsiz.x -= w1px * 2.0f;
      vc._texsiz.y -= h1px * 2.0f;
      vc._tex.x += w1px;
      vc._tex.y += h1px;
      vc._tex.z -= w1px;
      vc._tex.w -= h1px;
    }
    private void ComputeVertexGlyphTCoord(ref v_v4v4v4v2u2v4v4 vc, MtCachedCharData? glyph, float adjust)
    {
      Gu.Assert(glyph != null);

      vc._tex.x = glyph.uv0.x; // GL - bottom left
      vc._tex.y = glyph.uv0.y;
      vc._tex.z = glyph.uv1.x;  // GL - top right *this essentially flips it upside down
      vc._tex.w = glyph.uv1.y;
      vc._texsiz.x = Math.Abs(glyph.uv1.x - glyph.uv0.x);
      vc._texsiz.y = Math.Abs(glyph.uv1.y - glyph.uv0.y);  // Uv0 - uv1 - because we flipped coords bove

      //adjust = -5f;
      //this is all for adjust/debug
      if (adjust != 0)
      {
        float w1px = 0;
        float h1px = 0;
        if (glyph.patchTexture_Width > 0 && vc._texsiz.x > 0)
        {
          w1px = vc._texsiz.x / glyph.patchTexture_Width * adjust;
        }
        if (glyph.patchTexture_Height > 0 && vc._texsiz.y > 0)
        {
          h1px = vc._texsiz.y / glyph.patchTexture_Height * adjust;
        }
        vc._texsiz.x -= w1px * 2.0f;
        vc._texsiz.y -= h1px * 2.0f;
        vc._tex.x += w1px;
        vc._tex.y += h1px;
        vc._tex.z -= w1px;
        vc._tex.w -= h1px;
      }
    }
    private void SetVertexRasterArea(ref v_v4v4v4v2u2v4v4 vc, in UiQuad rasterQuad, in UiQuad b2ClipRect, UiDebugDraw dd)
    {
      //BL = min TR = max
      vc._rect.x = rasterQuad.Min.x;
      vc._rect.y = rasterQuad.Min.y;
      vc._rect.z = rasterQuad.Max.x;
      vc._rect.w = rasterQuad.Max.y;

      // Clip Rect.  For discard
      if (dd.DisableClip)
      {
        //We are only flipping Y in the shader now
        vc._clip.x = -Gui2d.MaxSize;
        vc._clip.y = -Gui2d.MaxSize;
        vc._clip.z = Gui2d.MaxSize;
        vc._clip.w = Gui2d.MaxSize;
      }
      else
      {
        vc._clip.x = b2ClipRect.Min.x;
        vc._clip.y = b2ClipRect.Min.y;
        vc._clip.z = b2ClipRect.Max.x;
        vc._clip.w = b2ClipRect.Max.y;
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
    private void UpdateGlyphs(MegaTex mt, bool replaceChangedGlyphs)
    {
      //textOnly If just text changed then just create new chars
      //style - if style changed we must redo everything.
      //Props must never be null.
      Gu.Assert(Style._props.FontFace != null);

      //reset max glyph size
      _quads.GlyphWH = new vec2(0, 0);

      if (String.IsNullOrEmpty(_strText))
      {
        _glyphs = null;
        return;
      }

      //Get the font if it isn't already got.
      MtFontLoader font = null;
      if (_cachedFont == null || mt.GetFont(Style._props.FontFace) != _cachedFont)
      {
        font = mt.GetFont(Style._props.FontFace);
      }
      else
      {
        font = _cachedFont;
      }
      if (font == null)
      {
        Gu.Log.ErrorCycle("Font loader could not be found for " + Style._props.FontFace.QualifiedPath + " font possibly loaded with error", 500);
        return;
      }

      float fontHeight = Style._props.FontSize;
      var patch = font.SelectFontPatchInfo(Gu.Translator.LanguageCode, fontHeight);
      if (patch == null)
      {
        return;
      }
      if (_glyphs == null)
      {
        replaceChangedGlyphs = false;
      }
      else if (replaceChangedGlyphs)
      {
        //Try to "smart" replace only changed text. 
        if (_glyphs.Count == 0)
        {
          replaceChangedGlyphs = false;
        }
        else if (_strTextLast.Length != _glyphs.Count)
        {
          //This will happen if we update the control text, but the control is hidden / does not render.
          //No need to debugbreak, unless we profile this and it becomes a problem.
          replaceChangedGlyphs = false;
          _glyphs = null;
        }
        else
        {
          //Sliding Diff algorithm to create a subset of glyphs.
          int ilast = 0;
          int icur = 0;
          List<UiElement> newChildren = new List<UiElement>();
          //We could cache diff in a static variable if its size becomes a problem.
          int[] diff = StringUtil.SlidingDiff(_strTextLast, _strText, Gui2d.SlidingDiffWindow);
          int debug_numcreated = 0;
          for (int di = 0; di < diff.Length; di += 2)
          {
            int ct = diff[di + 1];
            int code = diff[di + 0];
            if (code == 0) // no change 
            {
              for (int cti = 0; cti < ct; cti++)
              {
                var e = _glyphs[ilast + cti];
                e._contentChanged = true;
                ExpandGlyphWH(e);
                newChildren.Add(e);
              }
              ilast += ct;
              icur += ct;
            }
            else if (code == 1)//add new
            {
              for (int cti = 0; cti < ct; cti++)
              {
                //TODO: this should be UiElementBase, for simplicity. UiElement is too huge.
                UiElement e = new UiElement();
                DoGlyph(e, icur + cti, _strText, font, patch, fontHeight);
                ExpandGlyphWH(e);
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
          _glyphs = newChildren;
        }
      }
      if (replaceChangedGlyphs == false)
      {
        int debug_redocount = 0;
        List<UiElement> newChildren = new List<UiElement>();
        for (int ci = 0; ci < _strText.Length; ci++)
        {
          UiElement e = new UiElement();

          DoGlyph(e, ci, _strText, font, patch, fontHeight);
          ExpandGlyphWH(e);
          newChildren.Add(e);

          debug_redocount++;
        }
        _glyphs = newChildren;
      }
    }
    private void ExpandGlyphWH(UiElement glyph)
    {
      _quads.GlyphWH.x = Math.Max(_quads.GlyphWH.x, glyph._quads._b2LocalQuad._width);
      _quads.GlyphWH.y = Math.Max(_quads.GlyphWH.y, glyph._quads._b2LocalQuad._height);
    }
    private void DoGlyph(UiElement e, int index, string text, MtFontLoader font, MtFontPatchInfo patch, float fontHeight)
    {
      int cc = _strText[index];
      int ccNext = (index + 1) < _strText.Length ? _strText[index + 1] : 0;
      float kern = font.GetKernAdvanceWidth(patch, Style._props.FontSize, cc, ccNext);

      float sca = 0;
      if (!patch.GetChar(cc, fontHeight, out e._cachedGlyph, out sca))
      {
        Gu.DebugBreak();//Unicode ch not found
      }

      float gtop = 0, gright = 0, gbot = 0, gleft = 0, gwidth = 0, gheight = 0;
      e._cachedGlyph.ApplyScaling(sca, out gtop, out gright, out gbot, out gleft, out gwidth, out gheight);

      //avoid setting props
      //We dont set glyph parent.
      e._parent = this;//hmm..
      e._pickEnabled = false;
      e.Style.IsPropsOnly = true;
      e.Style._props.SizeModeWidth = UiSizeMode.Fixed;
      e.Style._props.SizeModeHeight = UiSizeMode.Fixed;

      e._renderOffset = new Box2f(new vec2(gleft, gtop), new vec2(gright, gbot));
      e._quads._b2LocalQuad._left = 0;
      e._quads._b2LocalQuad._top = 0;
      e._quads._b2LocalQuad._width = e._cachedGlyph.advance + kern + 1;//the widths are off somewhere, the +1 prevents sligth clipping of the right leter
      e._quads._b2LocalQuad._height = gheight * Style._props.LineHeight;
      e.Style._props.PadRight = e.Style._props.PadLeft = e.Style._props.PadBot = e.Style._props.PadTop = 0;
      e.Style._props.MarginBot = e.Style._props.MarginTop = e.Style._props.MarginRight = e.Style._props.MarginLeft = 0;

      if (cc == '\n')
      {
        e.Style._props.DisplayMode = UiDisplayMode.Block;
      }
      else
      {
        e.Style._props.DisplayMode = UiDisplayMode.Inline;
      }
      e.Style._props.Color = Style._props.FontColor;

      e._quads._b2LocalQuad.Validate();
    }
    private void ShowOrHide(bool show)
    {
      if (_visible != show)
      {
        _visible = show;
      }
    }
    #endregion

  }//UiElement
  public class UiStyleSheet
  {
    //stylesheet for css-like styling
    private FileLoc _location = null;
    private Dictionary<string, UiStyle> Styles = new Dictionary<string, UiStyle>();
    private List<string> _errors = new List<string>();
    public string Name { get; private set; } = Library.UnsetName;

    public UiStyleSheet(FileLoc loc)
    {
      _location = loc;
      Name = System.IO.Path.GetFileName(loc.RawPath) + "-stylesheet";
      LoadCSSFile();
    }
    public void Update()
    {
      if (_errors != null && _errors.Count > 0)
      {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"");
        sb.AppendLine($"StyleSheet '{Name}', Context '{Gu.Context.Name}' has errors: ");
        foreach (var e in _errors)
        {
          sb.AppendLine("-> " + e);
        }
        _errors.Clear();
        Gu.Log.Error(sb.ToString());
      }
    }
    private void LoadCSSFile()
    {
      //Compile a CSS file 
    }
    public void StyleError(string error)
    {
      this._errors.Add(error);
    }
    public void AddStyles(List<UiStyle> styles)
    {
      foreach (var s in styles)
      {
        AddStyle(s);
      }
    }
    public void AddStyle(UiStyle s)
    {
      //Styles can
      s.SetStyleSheet(this);
      Gu.Assert(s.Name != null);
      if (Styles.ContainsKey(s.Name))
      {
        Gu.Log.Error($"{this.Name} style {s.Name} was already added.");
        Gu.DebugBreak();
      }
      else
      {
        Styles.Add(s.Name, s);
      }
    }
    public bool RemoveStyle(UiStyle s)
    {
      var b = Styles.Remove(s.Name);
      return b;
    }
    public UiStyle GetStyle(string s)
    {
      Styles.TryGetValue(s, out var x);
      return x;
    }
  }
  // public class GVertMap
  // {
  //   //TODO: - expanding buffer so we dont recreate 1000s verts every frame
  //   public Dictionary<long, v_v4v4v4v2u2v4v4> _gverts = new Dictionary<long, v_v4v4v4v2u2v4v4>();
  //   MtTex _defaultPixel;
  //   Box2f _clipRect;
  // }
  public class Gui2d : UiElement
  {
    //@class Gui2d
    //@brief A GUI local to a given render viewport (RenderView)
    //*note:GUI element sizes translated relative to the current FBO size in the shader, all gui coords are in window coords
    #region Public: Members
    private enum UiAsyncUpdateState
    {
      CanUpdate, Updating, Updated, DoingEvents, DidEvents
    }

    public const int MaxSize = 9999999;
    public const int SlidingDiffWindow = 16;//16 chars for the string difference window. Replacement of a full float string.

    public WeakReference<RenderView> RenderView { get; private set; } = new WeakReference<RenderView>(null);
    public UiDebugDraw DebugDraw { get; set; } = new UiDebugDraw();
    public MeshData Mesh { get; set; } = null;
    public MeshView MeshView { get; set; } = null;
    public long _dbg_UpdateMs { get; private set; } = 0;
    public long _dbg_MeshMs { get; private set; } = 0;
    public long _dbg_EventsMs { get; private set; } = 0;

    public UiStyleSheet StyleSheet { get; set; } = null;

    #endregion
    #region Private:Members

    private UiDragInfo _dragInfo = new UiDragInfo();
    private vec2 _viewport_wh_last = new vec2(1, 1);
    private Gui2dShared _shared = null;
    //private GVertMap _gverts = new GVertMap();
    private UiAsyncUpdateState _state = UiAsyncUpdateState.CanUpdate;
    private UiEventThing _eventThing = new UiEventThing();
    private Dictionary<uint, UiElement> _pickable = new Dictionary<uint, UiElement>();
    private bool _async = false;

    #endregion
    #region Public: Methods

    public Gui2d(Gui2dShared shared, RenderView cam)
    {
      StyleSheet = new UiStyleSheet(new FileLoc("ui-default.css", FileStorage.Embedded));
      _shared = shared;
      RenderView = new WeakReference<RenderView>(cam);
      Name = "screen(root)";

      //Default pick id for whole gui - we need this because we need to know whether or not we are ponting at
      //the GUI, or not. Sub-elements override this pick ID with their own "pick root"-s
      _iPickId = Gu.Context.Renderer.Picker.GenPickId();
    }
    public void Update(double dt)
    {
      if (Gu.Context.PCKeyboard.Press(Keys.U))
      {
        disableoff = !disableoff;
      }

      //queue update if processed events.
      if (_state == UiAsyncUpdateState.CanUpdate)
      {
        _state = UiAsyncUpdateState.Updating;
        ThreadPool.QueueUserWorkItem(stateinfo =>
        {
          Dictionary<uint, UiElement>? pickable = null;
          if (RenderView != null && RenderView.TryGetTarget(out var rv))
          {
            long a = Gu.Milliseconds();
            StyleSheet?.Update();
            SetExtentsToViewport(rv);
            UpdateLayout_Async(_shared.MegaTex, Gu.Context.PCMouse, rv, ref pickable);
            this._dbg_UpdateMs = Gu.Milliseconds() - a;
          }
          Gu.Context.Gpu.Post_To_RenderThread(Gu.Context, x =>
          {
            if (pickable != null)
            {
              _pickable = pickable;
            }
            if (RenderView != null && RenderView.TryGetTarget(out var rv2))
            {
              SendMeshToGpu_Sync(rv2);
              _state = UiAsyncUpdateState.Updated;
            }
          });
        });
      }

      //pick, add events
      if (Gu.Context.GameWindow.IsFocused)
      {
        Pick();
      }
      _eventThing.PollForEvents(this);

      //fire events
      if (_state == UiAsyncUpdateState.Updated)
      {
        _state = UiAsyncUpdateState.DoingEvents;

        if (RenderView != null && RenderView.TryGetTarget(out var rv))
        {
          long a = Gu.Milliseconds();
          foreach (var e in _eventThing._events)
          {
            e.Fire();
          }
          _dbg_EventsMs = Gu.Milliseconds() - a;
        }

        _eventThing._events.Clear();
        _state = UiAsyncUpdateState.DidEvents;
        _state = UiAsyncUpdateState.CanUpdate;
      }

    }
    public void Pick()
    {
      //See WorldObject->Pick
      if (Gu.Context.Renderer.Picker.PickedObjectFrame == null)
      {
        //Do Pick
        var pixid = Gu.Context.Renderer.Picker.SelectedPixelId;
        if (_pickable.TryGetValue(pixid, out var ele))
        {
          Gu.Context.Renderer.Picker.PickedObjectFrame = ele;
        }
      }
    }
    public void OnResize()
    {
      //*note:GUI is translated to the current FBO size in the shader, all gui coords are in window coords
      //**This isn't really necessary, as we keep track of the viewport and force a layout change if it changes.
    }
    public Drawable GetDrawable()
    {
      //**THIS MAY BE INVALID - 
      //dummy is shared among contexts.. must test this
      //      return new Drawable(this.Name, MeshView, _worldMaterial_Op, mat4.Identity);
      //Swap out the mesh for this instance's mesh
      _shared.Dummy.MeshView = MeshView;
      return _shared.Dummy;
    }
    #endregion
    #region Private: Methods

    private int _async_framestamp = 0;
    //TODO: use some kind of expanding buffer
    ReverseGrowList<v_v4v4v4v2u2v4v4> _async_verts = new ReverseGrowList<v_v4v4v4v2u2v4v4>();
    private void UpdateLayout_Async(MegaTex mt, PCMouse mouse, RenderView rv, ref Dictionary<uint, UiElement>? pickable)
    {
      //not sure if this is faster doesnt seem to make too much differnts.
      _async_verts.Reset();
      //for now - the layout changed thing does not work, partially due to async, (but the async is actually faster than that anyway).
      bool force = true;

      _async_framestamp++;
      //pass 1 compute minimum sizes for children,  child relative positions, relative clip quads
      //pass 2 compute absolute positions elements, compute quads.
      ComputeQuads(DebugDraw);
      PerformLayout_SizeElements(mt, force, new vec2(Style._props.MaxWidth, Style._props.MaxHeight), null, StyleSheet, _async_framestamp, DebugDraw);
      PerformLayout_PositionElements(force, DebugDraw, _async_verts, this._quads._b2ClipQuad, mt.DefaultPixel, _iPickId, ref pickable);

      //TODO: we need to have a sort - debug &c is not showing
      // _eles_tmp.List.Sort((x, y) => { return x.DefaultSortKey - y.DefaultSortKey; });
      // if (_eles_tmp.Count > _async_verts.Count)
      // {
      //   _async_verts = new List<v_v4v4v4v2u2v4v4>(_eles_tmp.Count);
      // }
      // foreach (var e in _eles_tmp)
      // {
      //   //calc vert
      // }
      //sort the vertexes based on the sort algorithm
      //sort: 
      // tree depth * 10000 + Child index + z-index
      //_async_verts.Sort((x, y) => x.);
    }
    private void SendMeshToGpu_Sync(RenderView rv)
    {
      //RegenMesh
      if (Mesh == null)
      {
        Mesh = new MeshData(rv.Name + "gui-mesh", OpenTK.Graphics.OpenGL4.PrimitiveType.Points,
        Gpu.CreateVertexBuffer(rv.Name + "gui-mesh", _async_verts.ToArray()), null, false);
        Mesh.DrawMode = DrawMode.Forward;
        Mesh.DrawOrder = DrawOrder.Last;
      }
      else
      {
        Gu.Assert(Mesh != null);
        Gu.Assert(Mesh.VertexBuffers != null);
        Gu.Assert(Mesh.VertexBuffers.Count == 1);

        var dat = GpuDataPtr.GetGpuDataPtr(_async_verts.ToArray());
        Mesh.VertexBuffers[0].ExpandBuffer(_async_verts.Count);
        Mesh.VertexBuffers[0].CopyToGPU(dat);
      }
      if (MeshView == null)
      {
        MeshView = new MeshView(Mesh, 0, _async_verts.Count);
      }
      else
      {
        MeshView.SetLimits(0, _async_verts.Count);
      }
    }
    private void SetExtentsToViewport(RenderView rv)
    {
      //We are probably getting rid of width height
      Style.Top = rv.Viewport.Y;
      Style.Left = rv.Viewport.X;
      Style.MinWidth = 0;
      Style.MinHeight = 0;
      Style.MaxWidth = rv.Viewport.Width;//Make sure stuff doesn't go off the screen.
      Style.MaxHeight = rv.Viewport.Height;//Make sure stuff doesn't go off the screen.
      Style.SizeModeWidth = UiSizeMode.Fixed;
      Style.SizeModeHeight = UiSizeMode.Fixed;
      Style.PositionMode = UiPositionMode.Absolute;

      _quads._b2ContentQuad._left = _quads._b2LocalQuad._left = rv.Viewport.X;
      _quads._b2ContentQuad._top = _quads._b2LocalQuad._top = rv.Viewport.Y;
      _quads._b2ContentQuad._width = _quads._b2LocalQuad._width = rv.Viewport.Width;
      _quads._b2ContentQuad._height = _quads._b2LocalQuad._height = rv.Viewport.Height;
      _quads._b2ClipQuad = _quads._b2LocalQuad = _quads._b2ContentQuad;
    }

    #endregion

  }//Gui2d
  public class Gui2dShared
  {
    //Shared data between Gui2d instances for each context
    // public UiStyleSheet StyleSheet { get; private set; } = null;
    public Drawable Dummy { get; private set; } = null;
    public MegaTex MegaTex { get; private set; } = null;
    public string Name { get; private set; } = Library.UnsetName;

    public Gui2dShared(string name, List<FileLoc> resources)
    {
      Name = name;
      //DO NOT USE MIPMAPS
      MegaTex = new MegaTex("gui_megatex", true, MegaTex.MtClearColor.DebugRainbow, false, TexFilter.Linear, false);//nearest
      MegaTex.AddResources(resources);
      var tx = MegaTex.Compile();

      //StyleSheet = new UiStyleSheet(MegaTex);

      if (tx != null)
      {
        var shader = Gu.Lib.LoadShader(RName.Shader_GuiShader);
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
    //Manages GUIs among contexts
    //We will need to split this. Guimanager isn't a datablock
    public Gui2dManager() : base("Gui2DManager")
    {
    }
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
