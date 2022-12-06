using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Loft
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
  public enum UiDisplayMode // display mode for static elements
  {
    Inline, //stays on line until end of line and wraps
    Block, //always wraps
    Word, //sticks with neighboring word elements (word wrap)
    InlineNoWrap //never wraps
  }
  public enum UiPositionMode
  {
    Static, // flows within page/container -221022 left/top is the offset relative to the computed block position
    Relative, // left/top is relative to container element 
    Absolute, // left/top relative to screen (RenderView)
  }
  public enum UiAlignment
  {
    Left,  // float left, roman text
    Center,// center
    Right, // float right, arabic text
  }
  public enum UiSizeMode
  {
    Expand, //Expand to parent
    Shrink, //Shrink container. Note: parent=shrink + child=expand -> child width is minimum of all children mininmum widths in parent
    Fixed // Fixed width/height
  }
  public enum UiRenderMode
  {
    None, // not drawn
    Color, // flat color, => the Tex will be default pixel
    Textured, // texture => uses Texture
  }
  public enum UiBuildOrder
  {
    Horizontal, //Shrink to size of child contents, taking Max Width/Height into account
    Vertical, //Expand to parent
  }
  public enum UiOverflowMode
  {
    Show, //show overflow - elements outside of the clip region (not element region)
    Hide // hide overflow 
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
    Floating, //element "floats" absolute above parent, does not affect container element region, but affects clip region (context menu)
  }
  public enum UiLayoutDirection
  {
    LeftToRight,//https://www.w3.org/TR/CSS2/visuren.html#propdef-direction
    RightToLeft
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

    LmbDrag,

    Mouse_Enter,
    Mouse_Move,//Mouse_Hover = Mouse_Move?
    Mouse_Leave,

    Lost_Focus,
    Got_Focus,

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
    , Texture
    , ImageTilingX
    , ImageTilingY
    , DisplayMode
    , PositionMode
    , OverflowMode
    , SizeModeWidth
    , SizeModeHeight
    , ZIndex
    , FloatMode
    , RenderMode
    , TextAlign
    , Alignment
    , Opacity //48

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
    public UiElement? Focused { get; private set; } = null;

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
      Focused = pressFocus;
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
    private UiElement? _focused = null;
    private List<UiEvent> _new_events_frame = new List<UiEvent>();

    public UiEventThing()
    {
      c_iMaxEvents = Gu.EngineConfig.MaxUIEvents;
      c_iMaxEvents = Math.Clamp(c_iMaxEvents, 0, 9999999);
    }
    public void PollForEvents(Gui2d g)
    {
      //Poll each frame
      //  sends max 20 or so events to at most 2 objects, last/cur picked
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

      //press focus "Focus" (drag / mouse down)
      //  the element being dragged/interacted while mouse HELD, 
      //  mouse need not be hovering over element to have press focus.      
      if ((Gu.Context.GameWindow.IsFocused == false) || ((lb == ButtonState.Release || lb == ButtonState.Up) && (_focused != null)))
      {
        var pold = _focused;
        _focused = null;
        if (pold != ecur)
        {
          //Force a mouse release in case mouse not over focus
          SendEvent(UiEventId.LmbRelease, pold);
        }
        SendEvent(UiEventId.Lost_Focus, pold);
      }
      else if (lb == ButtonState.Press)
      {
        var pold = _focused;
        if (Gu.TryGetSelectedView(out var vv))
        {
          _focused = ecur;
        }
        SendEvent(UiEventId.Lost_Focus, pold);
        SendEvent(UiEventId.Got_Focus, _focused);
      }

      //lmb / rmb
      if (lb != _eLast_Lmb)
      {
        if (ecur != null)
        {
          if (lb == ButtonState.Up) { SendEvent(UiEventId.LmbUp, ecur); }
          if (lb == ButtonState.Press) { SendEvent(UiEventId.LmbPress, ecur); }
          if (lb == ButtonState.Hold) { SendEvent(UiEventId.LmbHold, ecur); }
          if (lb == ButtonState.Release) { SendEvent(UiEventId.LmbRelease, ecur); }
        }
      }
      if (rb != _eLast_Rmb)
      {
        if (ecur != null)
        {
          if (rb == ButtonState.Up) { SendEvent(UiEventId.RmbUp, ecur); }
          if (rb == ButtonState.Press) { SendEvent(UiEventId.RmbPress, ecur); }
          if (rb == ButtonState.Hold) { SendEvent(UiEventId.RmbHold, ecur); }
          if (rb == ButtonState.Release) { SendEvent(UiEventId.RmbRelease, ecur); }
        }
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
  
        //Pressed item 
        if ((mpos != mlast) && (lb == ButtonState.Hold) && (_eLast_Lmb == ButtonState.Press || _eLast_Lmb == ButtonState.Hold))
        {
          SendEvent(UiEventId.LmbDrag, _focused);
        }
      

      //Update state after events are sent (we use state in event)
      _eLast_Lmb = lb;
      _eLast_Rmb = rb;

      //send
      if (_new_events_frame.Count > 0)
      {
        var state = new UiEventState(g, mpos, mlast, elast, ecur, lb, rb, _focused);
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
  // public class UiDragInfo
  // {
  //   private bool _bDragStart = false;
  //   private vec2 _vDragStart;
  //   private WeakReference<UiElement> Target { get; set; } = null;

  //   public UiDragInfo() { }
  //   public void StartDrag(UiElement target, PCMouse ms)
  //   {
  //     if (target != null)
  //     {
  //       Target = new WeakReference<UiElement>(target);
  //       _vDragStart = ms.Pos;
  //       _bDragStart = true;
  //     }
  //   }
  //   public void EndDrag()
  //   {
  //     Target = null;
  //     _bDragStart = false;
  //   }
  //   public void UpdateDrag(PCMouse ms)
  //   {
  //     float mw = 1.0f / 1; // UiScreen::getDesignMultiplierW the design multiplier - this isntn't accounted for
  //     float mh = 1.0f / 1; // UiScreen::getDesignMultiplierH the design multiplier - this isntn't accounted for
  //     if (Target != null && Target.TryGetTarget(out var tar))
  //     {
  //       //Check for mouse delta to prevent unnecessary updates.
  //       bool canDrag = false;
  //       vec2 dp = ms.Pos - _vDragStart;
  //       if (MathUtils.FuzzyEquals(dp.x, 0.0f) == false || MathUtils.FuzzyEquals(dp.y, 0.0f) == false)
  //       {
  //         canDrag = true;
  //       }
  //       else
  //       {
  //         canDrag = false;
  //       }
  //       if (canDrag)
  //       {
  //         //Multiply the distance by the design size.
  //         dp.x *= mw;
  //         dp.y *= mh;

  //         if (tar.DragFunc != null)
  //         {
  //           tar.DragFunc(dp);
  //         }

  //         //Reset drag start
  //         _vDragStart = ms.Pos;
  //       }
  //     }
  //     else
  //     {
  //       Target = null;
  //     }

  //   }
  // }
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
    public vec4 MultiplyColor = new vec4(1, 1, 1, 1);//color multiplier
    public vec4 BorderColorTop = new vec4(0, 0, 0, 1);
    public vec4 BorderColorRight = new vec4(0, 0, 0, 1);
    public vec4 BorderColorBot = new vec4(0, 0, 0, 1);
    public vec4 BorderColorLeft = new vec4(0, 0, 0, 1);
    public Loft.FontFace FontFace = Loft.FontFace.Calibri;
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
    public float ZIndex = 0;
    public UiFloatMode FloatMode = UiFloatMode.None;
    public UiRenderMode RenderMode = UiRenderMode.None;
    public UiAlignment TextAlign = UiAlignment.Left;
    public UiAlignment Alignment = UiAlignment.Left;
    public double Opacity = 1;
    public UiLayoutDirection Direction = UiLayoutDirection.LeftToRight;

    //Most of this generic field junk can go away and we can manually just return the variables. My hands were huring here so..ugh
    private static UiProps _defaults = new UiProps();//defaults are just set on the field initializer.
    public static Dictionary<UiPropName, System.Reflection.FieldInfo> AllFields { get; private set; } = null;
    public static Dictionary<UiPropName, System.Reflection.FieldInfo> InheritableFields { get; private set; } = null;

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
      var fi = AllFields[p];
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
      var fi = AllFields[p];
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

    }
    private static void CreateStaticFieldInfo()
    {
      if (AllFields == null || InheritableFields == null)
      {
        AllFields = new Dictionary<UiPropName, FieldInfo>();
        InheritableFields = new Dictionary<UiPropName, FieldInfo>();
        var enums = Enum.GetValues(typeof(UiPropName));
        var fields = typeof(UiProps).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var pf in fields)
        {
          foreach (var pe in enums)
          {
            string a = pe.ToString();
            string a_priv = "_" + a;
            string b = pf.Name;
            if (a.Equals(b) || a_priv.Equals(b)) //for debugging get/set - we use _ for the private prop
            {
              if (CanInherit((UiPropName)pe))
              {
                InheritableFields.Add((UiPropName)pe, pf);
              }
              AllFields.Add((UiPropName)pe, pf);
              break;
            }

          }
        }
        //Make sure we got all fields.
        for (int i = 0; i < (int)UiPropName.MaxUiProps; ++i)
        {
          UiPropName p = (UiPropName)i;
          //You didnt add the prop to the fields.
          Gu.Assert(AllFields.ContainsKey(p));
        }
      }
    }
    private static bool CanInherit(UiPropName p)
    {
      //https://www.w3.org/TR/CSS/#properties
      if (
         p == UiPropName.Left ||
         p == UiPropName.Top ||
         p == UiPropName.MinWidth ||
         p == UiPropName.MaxWidth ||
         p == UiPropName.MinHeight ||
         p == UiPropName.MaxHeight
      //this is incorrect for CSS but it makes things easier to just inherit these
      //if you do not inherit them you have to set the class style manually as we do not support an "inherit" property
      //  p == UiPropName.MarginTop ||
      //  p == UiPropName.MarginRight ||
      //  p == UiPropName.MarginBot ||
      //  p == UiPropName.MarginLeft ||
      //  p == UiPropName.PadTop ||
      //  p == UiPropName.PadRight ||
      //  p == UiPropName.PadBot ||
      //  p == UiPropName.PadLeft ||
      //  p == UiPropName.BorderColorTop ||
      //  p == UiPropName.BorderColorRight ||
      //  p == UiPropName.BorderColorBot ||
      //  p == UiPropName.BorderColorLeft ||
      //  p == UiPropName.BorderTop ||
      //  p == UiPropName.BorderRight ||
      //  p == UiPropName.BorderBot ||
      //  p == UiPropName.BorderLeft
      )
      {
        return false;
      }

      return true;
    }

  }

  public class SyncThen
  {
    //synchronous promise
    private DeltaTimer _timer;
    public Action<double> _then;
    public SyncThen(Action<double> act) { _then = act; }
    public SyncThen Then(Action<double> act)
    {
      return new SyncThen(act);
    }
    public void Update(double dt)
    {
    }
  }
  public interface IUiPropAnimation
  {
    public UiElement Element { get; }
    public UiPropName Prop { get; }
    public bool Update(double dt);
  }
  public class UiPropAnimation<T> : IUiPropAnimation where T : struct
  {
    public UiElement Element { get; }
    public UiPropName Prop { get; }

    private int _duration;
    private int _current = 0;
    private double _elapsed = 0;
    private double _durationseconds;
    private T? _startValue;
    private T? _endValue;
    private int _repeatCount = 0;
    private int _executed = 0;

    public UiPropAnimation(UiElement e, UiPropName p, T? endValue, int durationMS, int repeatCount = 0)
    {
      Gu.Assert(e != null);
      Element = e;
      Prop = p;
      _repeatCount = repeatCount;

      _durationseconds = (double)durationMS / 1000.0;
      if (_durationseconds <= 0)
      {
        _durationseconds = 0.00001;
      }
      var prop = e.Style.GetProp(p);
      Gu.Assert(prop != null);

      _startValue = (prop as T?).Value;
      _endValue = endValue;

    }
    public bool Update(double dt)
    {
      //return true if it's done
      if (_elapsed < _durationseconds && _executed < (_repeatCount + 1))
      {
        T? newval = null;
        _elapsed += dt;
        double x = Math.Clamp(_elapsed / _durationseconds, 0, 1);

        if (typeof(T) == typeof(vec4))
        {
          var a = _startValue as vec4?;
          var b = _endValue as vec4?;
          newval = (a + (b - a) * (float)x) as T?;
        }
        else if (typeof(T) == typeof(double))
        {
          var a = _startValue as double?;
          var b = _endValue as double?;
          newval = (a + (b - a) * x) as T?;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }

        if (newval != null)
        {
          Element.Style.SetProp(Prop, newval);
        }

        for (int xx = 0; Gu.WhileTrueGuard(xx, Gu.c_intMaxWhileTrueLoopSmall) && _elapsed >= _durationseconds; xx++)
        {
          _elapsed %= _durationseconds;
          _executed++;
        }

      }
      return _executed >= (_repeatCount + 1);
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
      get { return (vec4?)_props.Get(UiPropName.BorderColorTop); }
      set
      {
        BorderColorTop = BorderColorRight = BorderColorLeft = BorderColorBot = value;
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
        SetProp(UiPropName.MinWidth, (float?)value);
        SetProp(UiPropName.MaxWidth, (float?)value);
      }
    }
    public float? Height
    {
      set
      {
        SetProp(UiPropName.MinHeight, (float?)value);
        SetProp(UiPropName.MaxHeight, (float?)value);
      }
    }
    // public float? Bottom { get { return (float?)GetClassValue(UiPropName.Top) + Width; } }
    // public float? Right { get { return (float?)GetClassValue(UiPropName.Left) + Height; } }


    #endregion
    #region Public: User Prop Setters

    //Manual setters.. these will cause this style class to own this property
    //**Note: Do not use nullable<> or ? types on class types here. This will return (null) even if the class type is set on the nullable boxer.
    //OK so you could actually just return _props.Top .. etc here, but for now we're doing this to simplify things (as they are written)
    public float? Top { get { return (float?)GetClassProp(UiPropName.Top); } set { SetProp(UiPropName.Top, (float?)value); } }
    public float? Left { get { return (float?)GetClassProp(UiPropName.Left); } set { SetProp(UiPropName.Left, (float?)value); } }
    public float? MinWidth { get { return (float?)GetClassProp(UiPropName.MinWidth); } set { SetProp(UiPropName.MinWidth, (float?)value); } }
    public float? MinHeight { get { return (float?)GetClassProp(UiPropName.MinHeight); } set { SetProp(UiPropName.MinHeight, (float?)value); } }
    public float? MaxWidth { get { return (float?)GetClassProp(UiPropName.MaxWidth); } set { SetProp(UiPropName.MaxWidth, (float?)value); } }
    public float? MaxHeight { get { return (float?)GetClassProp(UiPropName.MaxHeight); } set { SetProp(UiPropName.MaxHeight, (float?)value); } }
    public float? PadTop { get { return (float?)GetClassProp(UiPropName.PadTop); } set { SetProp(UiPropName.PadTop, (float?)value); } }
    public float? PadRight { get { return (float?)GetClassProp(UiPropName.PadRight); } set { SetProp(UiPropName.PadRight, (float?)value); } }
    public float? PadBot { get { return (float?)GetClassProp(UiPropName.PadBot); } set { SetProp(UiPropName.PadBot, (float?)value); } }
    public float? PadLeft { get { return (float?)GetClassProp(UiPropName.PadLeft); } set { SetProp(UiPropName.PadLeft, (float?)value); } }
    public float? MarginTop { get { return (float?)GetClassProp(UiPropName.MarginTop); } set { SetProp(UiPropName.MarginTop, (float?)value); } }
    public float? MarginRight { get { return (float?)GetClassProp(UiPropName.MarginRight); } set { SetProp(UiPropName.MarginRight, (float?)value); } }
    public float? MarginBot { get { return (float?)GetClassProp(UiPropName.MarginBot); } set { SetProp(UiPropName.MarginBot, (float?)value); } }
    public float? MarginLeft { get { return (float?)GetClassProp(UiPropName.MarginLeft); } set { SetProp(UiPropName.MarginLeft, (float?)value); } }
    public float? BorderTop { get { return (float?)GetClassProp(UiPropName.BorderTop); } set { SetProp(UiPropName.BorderTop, (float?)value); } }
    public float? BorderRight { get { return (float?)GetClassProp(UiPropName.BorderRight); } set { SetProp(UiPropName.BorderRight, (float?)value); } }
    public float? BorderBot { get { return (float?)GetClassProp(UiPropName.BorderBot); } set { SetProp(UiPropName.BorderBot, (float?)value); } }
    public float? BorderLeft { get { return (float?)GetClassProp(UiPropName.BorderLeft); } set { SetProp(UiPropName.BorderLeft, (float?)value); } }
    public float? BorderTopLeftRadius { get { return (float?)GetClassProp(UiPropName.BorderTopLeftRadius); } set { SetProp(UiPropName.BorderTopLeftRadius, (float?)value); } }
    public float? BorderTopRightRadius { get { return (float?)GetClassProp(UiPropName.BorderTopRightRadius); } set { SetProp(UiPropName.BorderTopRightRadius, (float?)value); } }
    public float? BorderBotRightRadius { get { return (float?)GetClassProp(UiPropName.BorderBotRightRadius); } set { SetProp(UiPropName.BorderBotRightRadius, (float?)value); } }
    public float? BorderBotLeftRadius { get { return (float?)GetClassProp(UiPropName.BorderBotLeftRadius); } set { SetProp(UiPropName.BorderBotLeftRadius, (float?)value); } }
    public vec4? Color { get { return (vec4?)GetClassProp(UiPropName.Color); } set { SetProp(UiPropName.Color, (vec4?)value); } }
    public vec4? MultiplyColor { get { return (vec4?)GetClassProp(UiPropName.MultiplyColor); } set { SetProp(UiPropName.MultiplyColor, (vec4?)value); } }
    public vec4? BorderColorTop { get { return (vec4?)GetClassProp(UiPropName.BorderColorTop); } set { SetProp(UiPropName.BorderColorTop, (vec4?)value); } }
    public vec4? BorderColorRight { get { return (vec4?)GetClassProp(UiPropName.BorderColorRight); } set { SetProp(UiPropName.BorderColorRight, (vec4?)value); } }
    public vec4? BorderColorBot { get { return (vec4?)GetClassProp(UiPropName.BorderColorBot); } set { SetProp(UiPropName.BorderColorBot, (vec4?)value); } }
    public vec4? BorderColorLeft { get { return (vec4?)GetClassProp(UiPropName.BorderColorLeft); } set { SetProp(UiPropName.BorderColorLeft, (vec4?)value); } }
    public FontFace FontFace { get { return (FontFace)GetClassProp(UiPropName.FontFace); } set { SetProp(UiPropName.FontFace, (FontFace)value); } }
    public float? FontSize { get { return (float?)GetClassProp(UiPropName.FontSize); } set { SetProp(UiPropName.FontSize, (float?)value); } }
    public UiFontStyle? FontStyle { get { return (UiFontStyle?)GetClassProp(UiPropName.FontStyle); } set { SetProp(UiPropName.FontStyle, (UiFontStyle?)value); } }
    public vec4? FontColor { get { return (vec4?)GetClassProp(UiPropName.FontColor); } set { SetProp(UiPropName.FontColor, (vec4?)value); } }
    public float? LineHeight { get { return (float?)GetClassProp(UiPropName.LineHeight); } set { SetProp(UiPropName.LineHeight, (float?)value); } }
    public UiPositionMode? PositionMode { get { return (UiPositionMode?)GetClassProp(UiPropName.PositionMode); } set { SetProp(UiPropName.PositionMode, (UiPositionMode?)value); } }
    public UiOverflowMode? OverflowMode { get { return (UiOverflowMode?)GetClassProp(UiPropName.OverflowMode); } set { SetProp(UiPropName.OverflowMode, (UiOverflowMode?)value); } }
    public UiSizeMode? SizeModeWidth { get { return (UiSizeMode?)GetClassProp(UiPropName.SizeModeWidth); } set { SetProp(UiPropName.SizeModeWidth, (UiSizeMode?)value); } }
    public UiSizeMode? SizeModeHeight { get { return (UiSizeMode?)GetClassProp(UiPropName.SizeModeHeight); } set { SetProp(UiPropName.SizeModeHeight, (UiSizeMode?)value); } }
    public UiDisplayMode? DisplayMode { get { return (UiDisplayMode?)GetClassProp(UiPropName.DisplayMode); } set { SetProp(UiPropName.DisplayMode, (UiDisplayMode?)value); } }
    public UiImageTiling? ImageTilingX { get { return (UiImageTiling?)GetClassProp(UiPropName.ImageTilingX); } set { SetProp(UiPropName.ImageTilingX, (UiImageTiling?)value); } }
    public UiImageTiling? ImageTilingY { get { return (UiImageTiling?)GetClassProp(UiPropName.ImageTilingY); } set { SetProp(UiPropName.ImageTilingY, (UiImageTiling?)value); } }
    public MtTex Texture { get { return (MtTex)GetClassProp(UiPropName.Texture); } set { SetProp(UiPropName.Texture, (MtTex)value); } }
    public float? ZIndex { get { return (float?)GetClassProp(UiPropName.ZIndex); } set { SetProp(UiPropName.ZIndex, (float?)value); } }
    public UiFloatMode? FloatMode { get { return (UiFloatMode?)GetClassProp(UiPropName.FloatMode); } set { SetProp(UiPropName.FloatMode, (UiFloatMode?)value); } }
    public UiRenderMode? RenderMode { get { return (UiRenderMode?)GetClassProp(UiPropName.RenderMode); } set { SetProp(UiPropName.RenderMode, (UiRenderMode?)value); } }
    public UiAlignment? TextAlign { get { return (UiAlignment?)GetClassProp(UiPropName.TextAlign); } set { SetProp(UiPropName.TextAlign, (UiAlignment?)value); } }
    public UiAlignment? Alignment { get { return (UiAlignment?)GetClassProp(UiPropName.Alignment); } set { SetProp(UiPropName.Alignment, (UiAlignment?)value); } }
    public double? Opacity { get { return (double?)GetClassProp(UiPropName.Opacity); } set { SetProp(UiPropName.Opacity, (double?)value); } }

    #endregion
    #region Public: Methods

    public UiProps _props = new UiProps();//Gets the compiled / final props
    public bool IsPropsOnly { get; set; } = false;//For glyph, don't inherit parent or compile, and re-compile the class every time.. we set _props manually
    public WeakReference<UiStyleSheet> StyleSheet { get; private set; } = null;

    private bool _bMustCompile = true;
    private long _changedFrameId = 0;
    public long CompiledFrameId { get; private set; } = 0;
    private HashSet<WeakReference<UiElement>> _eles = null;
    private BitArray _owned = new BitArray((int)UiPropName.MaxUiProps);//This bitset tells us which props were set
    private BitArray _inherited = new BitArray((int)UiPropName.MaxUiProps);
    private BitArray _defaulted = new BitArray((int)UiPropName.MaxUiProps);
    private BitArray _changed = new BitArray((int)UiPropName.MaxUiProps);//props that changed during the last class compile
    public BitArray Changed { get { return _changed; } }
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
    public UiStyle(UiStyleName name)
      : this(name.ToString(), new List<string>() { })
    {
    }
    public UiStyle(string name)
      : this(name, new List<string>() { })
    {
    }
    public UiStyle(UiStyleName name, UiStyleName inherted_style)
  : this(name.ToString(), new List<string>() { inherted_style.ToString() })
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
    public void SetProp(UiPropName p, object? value)
    {
      //set a property 
      // * set value to null to clear/inherit value
      if (CheckValueEnabled(p, value))
      {
        SetClassValueDirect(p, value);
      }
    }
    public object? GetProp(UiPropName p)
    {
      //get the compiled property 
      //returns the compiled / owned value
      // * value will not be null
      return _props.Get(p);
    }
    public object? GetClassProp(UiPropName p)
    {
      //Get class property
      // * value will be null if prop is not owned.
      var owned = _owned.Get((int)p);
      if (!owned)
      {
        return null;
      }
      else
      {
        return GetProp(p);
      }
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

          //https://www.w3.org/TR/CSS2/cascade.html#cascade
          foreach (var p in UiProps.InheritableFields)
          {
            if (!IsOwned(p.Key))
            {
              //not owned, get value from superclass
              if (!InheritFromSuperClasses(s, p.Key, p.Value, framestamp))
              {
                //if subclasses are not set, then try the parent DOM element, otherwise set to a default value
                //Ok super annoying, but we acutally need this because say you set opacity on a parent element to fade out, then the text should get the same opacity as it all should fade
                if (!InheritFromParentTag(style_DOM_parent, p.Key, p.Value))
                {
                  //No parent element, and, no styles, set to default.
                  SetDefaultValue(p.Key, p.Value);
                }
              }
            }

          }

          DebugStorePropDetails();

          _changed.SetAll(false);
          CompiledFrameId = framestamp;
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
        var val = fromStyle.GetProp(p);
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
      UiStyle ret = new UiStyle(this.Name + Lib.CopyName, this._superStylesNames);
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

    private bool CheckValueEnabled(UiPropName p, object? new_class_value)
    {
      //Check if we set the prop to null (disabled it) 
      var owned = _owned.Get((int)p);
      if (new_class_value == null && owned == false)
      {
        //Class value is unset, and was set it to null again.. no change
        return false;
      }
      else if (new_class_value == null && owned == true)
      {
        //Class value is being cleared
        return true;
      }
      else if (new_class_value != null && owned == false)
      {
        //Class value is unset, and we set a fresh value... definite change
        return true;
      }
      else if (new_class_value != null && owned == true)
      {
        //Class value is set, and we set a new value check for value difference (prevent recompiling all classes!)
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
      //Set the class value for this style, skipping over modified value checking.
      if (value != null)
      {
        //Only set the prop value if not null, as, null is basically the way we say "clear the value"
        _owned.Set((int)p, true);
        _props.Set(p, value);
      }
      else
      {
        //the property was cleared.
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
    public UiQuad _b2ClipQuad = new UiQuad(); // all floating and contained, elements and min/max w/h. *clip quad may not equal computed quad if there are floating elements
    public UiQuad _b2LocalQuad = new UiQuad(); // computed width/height of the container items parent-relative, and the Top/Left offset (if given)
    public UiQuad _dbg_b2ContentQuad = new UiQuad();
    public UiQuad _dbg_b2PaddingQuad = new UiQuad();
    public UiQuad _b2BorderQuad = new UiQuad(); // Final quad. content area + margin + border area
    public vec2 ContentWH = new vec2(0, 0);
    public vec2 GlyphWH = new vec2(0, 0);//max width/height of all glyphs
    public vec2 OuterMaxWH = new vec2(0, 0);
    public vec2 InnerMaxWH = new vec2(0, 0);
  }
  public abstract class UiBlock //base interface for for glyphs / eles
  {
    //interface between UiGlyph and UiElement
    public abstract float Left { get; }
    public abstract float Top { get; }
    public abstract UiDisplayMode DisplayMode { get; }
    public virtual float WordWidth { get { return 0; } }
    public abstract vec4 GetPadding(UiDebugDraw dd);
    public UiQuads _quads = new UiQuads();
  }
  public class UiGlyphChar
  {
    public UiQuad _glyphQuad;
    public vec4 _padding;
    public float _finalLineHeight;
    public MtCachedCharData? _cachedGlyph = null;//For glyphs
    public int _code;
  }
  public class UiGlyph : UiBlock
  {
    public float _left_off = 0;
    public float _top_off = 0;
    public vec4 _padding;
    public int _char = 0;
    public UiDisplayMode _displayMode = UiDisplayMode.Inline;//ok
    public float _wordWidth = 0;
    public UiGlyphChar? _reference = null;

    public override float Left { get { return _left_off; } }
    public override float Top { get { return _top_off; } }
    public override float WordWidth { get { return _wordWidth; } }
    public override UiDisplayMode DisplayMode { get { return _displayMode; } }

    public UiGlyph(UiGlyphChar? dref)
    {
      _reference = dref;
    }
    public override vec4 GetPadding(UiDebugDraw dd)
    {
      return _padding;
    }
  }
  public class UiElement : UiBlock
  {
    #region Classes 
    private class UiCol
    {
      public float _height = 0;
      public float _width = 0;
      public List<UiBlock> _eles = new List<UiBlock>();
    }
    private class UiLine
    {
      public UiCol[] _cols = new UiCol[3] { new UiCol(), new UiCol(), new UiCol() };
      public float _top = 0;//not null depending on UiBuildOrder
      public float _left = 0;
      public float Height { get { return Math.Max(_cols[0]._height, Math.Max(_cols[1]._height, _cols[2]._height)); } }
      public float Width { get { return _cols[0]._width + _cols[1]._width + _cols[2]._width; } }

      public UiLine(float left, float top)
      {
        _left = left;
        _top = top;
      }
    }

    #endregion
    #region Public: Members

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
      get
      {
        if (_style == null)
        {
          _style = new UiStyle("inline");
          _style.IsInline = true;
          _style.AddReference(this);
        }
        return _style;
      }
    }
    public bool Visible { get { return _visible; } set { _visible = value; } }// ** May be on style .. 
    public Dictionary<UiEventId, List<UiAction>> Events { get { return _events; } set { _events = value; } }
    public List<UiElement>? Children { get { return _children; } }
    public UiQuad LocalQuad { get { return _quads._b2LocalQuad; } }
    public UiQuad ContentQuad { get { return _quads._dbg_b2ContentQuad; } }
    public UiQuad FinalQuad { get { return _quads._b2BorderQuad; } }
    public UiElement? Parent { get { return _parent; } }
    public bool TopMost { get { return _topMost; } set { _topMost = value; } }
    public override float Left { get { return Style._props.Left; } }
    public override float Top { get { return Style._props.Top; } }
    public override UiDisplayMode DisplayMode { get { return Style._props.DisplayMode; } }

    #endregion
    #region Private: Members

    protected string _name = "";
    protected UiStyle? _style = null;
    protected List<UiElement>? _children = null;
    private UiElement? _parent = null;
    private MtFontLoader? _cachedFont = null;//For labels that contain glyphs
    private Dictionary<int, UiGlyphChar> _glyphChars = null;
    private List<UiGlyph> _glyphs = null;
    private static long s_idgen = 100;
    protected long _id = 0;
    private vec2 _tileScale = new vec2(1, 1); //Texture Scale (UiImageSizeMode == Tile) this should be on the Props
    public uint _iPickId = 0;
    protected string _strText = "";
    private string _strTextLast = "";
    private bool _bMustRedoTextBecauseOfStyle = false;
    private Dictionary<UiEventId, List<UiAction>>? _events = null;
    private int _treeDepth = -1;
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
    public UiElement(string name) : this()
    {
      Init(new List<UiStyleName>() { }, name);
    }
    public UiElement(UiStyleName style) : this()
    {
      Init(new List<UiStyleName>() { style }, null);
    }
    public UiElement(UiStyleName style, string name) : this()
    {
      Init(new List<UiStyleName>() { style }, null, name);
    }
    public UiElement(UiStyleName style, Phrase p) : this()
    {
      List<UiStyleName> styles = new List<UiStyleName>();
      if (style != null)
      {
        styles.Add(style);
      }
      Init(styles, null, Gu.Translator.Translate(p));
    }
    public UiElement(List<UiStyleName> styleClasses) : this()
    {
      Init(styleClasses, null);
    }
    public UiElement(List<UiStyleName> styleClasses, Phrase phrase) : this()
    {
      Init(styleClasses, null, Gu.Translator.Translate(phrase));
    }
    public UiElement(List<UiStyleName> styleClasses, string text) : this()
    {
      Init(styleClasses, null, text);
    }
    public UiElement(List<UiStyleName> styleClasses, Phrase phrase, List<UiElement> children) : this()
    {
      Init(styleClasses, null, Gu.Translator.Translate(phrase), children);
    }
    public UiElement(List<UiStyleName> styleClasses, string text, List<UiElement> children) : this()
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
    public bool TryGetGui2dRoot(out Gui2d? ret)
    {
      ret = null;
      UiElement? parent = this.Parent;
      while (parent != null && parent.Parent != null)
      {
        parent = parent.Parent;
      }
      if (parent is Gui2d)
      {
        ret = parent as Gui2d;
      }
      return ret != null;
    }
    public bool Animate(UiPropName prop, double value, int durationMS, int repeatCount = 0)
    {
      if (TryGetGui2dRoot(out var g))
      {
        var pa = new UiPropAnimation<double>(this, prop, value, durationMS, repeatCount);
        g.AddAnimation(pa);
        return true;
      }
      return false;
    }
    public bool Animate(UiPropName prop, vec4 value, int durationMS, int repeatCount = 0)
    {
      if (TryGetGui2dRoot(out var g))
      {
        var pa = new UiPropAnimation<vec4>(this, prop, value, durationMS, repeatCount);
        g.AddAnimation(pa);
        return true;
      }
      return false;
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
    public override vec4 GetPadding(UiDebugDraw dd)
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
    public vec4 GetBorder(UiDebugDraw? dd)
    {
      if (dd != null && dd.DisableBorders)
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
    // public UiElement AddChild(string stylename)
    // {
    //   return AddChild(new UiElement(stylename));
    // }
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
    public bool RemoveEvents(UiEventId evId)
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

    private void Init(List<UiStyleName> styleClasses, string? name = null, string? phrase = null, List<UiElement> children = null)
    {
      this.Style.SetInheritStyles(styleClasses.ConvertAll(x => x.ToString()));
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

        if (framesatmp % 5 == 0 || framesatmp < 100)
        {
          UpdateGlyphSpans(mt);
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

        //all elements & ele pads NO PARENT MARGIN OR BORDER
        _quads.ContentWH = _quads.GlyphWH; //start with max wh of all glyphs

        //remove margins for child
        var pmarb = this.GetMarginAndBorder(dd);
        vec4 ppad = vec4.Zero;
        if (this.Style._props.PositionMode == UiPositionMode.Static)
        {
          ppad = this.GetPadding(dd);//shrink also by padding for static elements
        }
        _quads.InnerMaxWH = new vec2(
          Math.Max(_quads.OuterMaxWH.width - pmarb.left - pmarb.right - ppad.left - ppad.right, 0),
          Math.Max(_quads.OuterMaxWH.height - pmarb.top - pmarb.bot - ppad.top - ppad.bot, 0)
        );

        //size, then layout children
        List<UiLine> spanLines = new List<UiLine>();
        spanLines.Add(new UiLine(0, 0));
        int lineidx = 0;

        if (_children != null && _children.Count > 0)
        {
          //compute min content WH first
          foreach (var ele in _children)
          {
            //Hide opacity=0 elements??
            // if(ele.Visible && ele.Style._props.RenderMode != UiRenderMode.None && ele.Style._props.Color.a == 0){
            //   ele.Visible = false;
            // }

            //uh..
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

          //algorithm 
          //layout all 3 columns to the left, then modify element position later
          // future:
          // layout center, then L,R .not move center, wrap LR 
          //   sort into columns based on child order
          lineidx = 0;
          foreach (var ele in _children)
          {
            if (ele.Visible && ele.Style._props.PositionMode == UiPositionMode.Static)
            {
              LayoutStaticElement(ele, ele.Style._props.Alignment, spanLines, _quads.InnerMaxWH, _quads.ContentWH, dd, ref lineidx);
            }
          }

        }

        //glyph spans
        lineidx = 0;
        if (_glyphs != null && _glyphs.Count > 0)
        {
          foreach (var glyp in this._glyphs)
          {
            LayoutBlock(glyp, Style._props.TextAlign, spanLines, _quads.InnerMaxWH, _quads.ContentWH, dd, ref lineidx);
          }
        }

        //Calculate content size
        float totalHeight = pmarb.top + pmarb.bot;
        foreach (var line in spanLines)
        {
          //add the center/right offsets
          totalHeight += line.Height;
          _quads.ContentWH.x = Math.Max(_quads.ContentWH.x, line.Width + pmarb.right + pmarb.left);
        }
        _quads.ContentWH.y = Math.Max(_quads.ContentWH.y, totalHeight);

        //Compute final width 
        SizeElement(_quads.ContentWH, _quads.OuterMaxWH, dd);

        //adjust line offsets for center/right 
        foreach (var line in spanLines)
        {
          var col_l = line._cols[(int)UiAlignment.Left];
          var col_c = line._cols[(int)UiAlignment.Center];
          var col_r = line._cols[(int)UiAlignment.Right];

          // foreach (var ele in col_l._eles)
          // {
          // }
          foreach (var ele in col_c._eles)
          {
            ele._quads._b2LocalQuad._left += _quads._b2LocalQuad._width / 2 - col_c._width / 2;
          }
          foreach (var ele in col_r._eles)
          {
            if (Translator.TextFlow == LanguageTextFlow.Left)
            {
              //arabic text
              ele._quads._b2LocalQuad._left = _quads._b2LocalQuad._width - ele._quads._b2LocalQuad._left - ele._quads._b2LocalQuad._width;
            }
            else
            {
              //roman
              //width inc marg+pad+bo - col (incl marg pad bor)
              ele._quads._b2LocalQuad._left = _quads._b2LocalQuad._width - col_r._width + ele._quads._b2LocalQuad._left;
            }
          }
        }


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

        //glyph spans.
        if (_glyphs != null)
        {
          foreach (var glyph in _glyphs)
          {
            if (glyph != null)
            {
              //apply parent
              glyph._quads._b2BorderQuad = glyph._quads._b2LocalQuad.Clone();
              glyph._quads._b2BorderQuad._left += this._quads._b2BorderQuad._left;
              glyph._quads._b2BorderQuad._top += this._quads._b2BorderQuad._top;

              if (dd.ShowOverlay)
              {
                v_v4v4v4v2u2v4v4 dbgv = new v_v4v4v4v2u2v4v4();
                SetVertexRasterArea(ref dbgv, glyph._quads._b2BorderQuad, _quads._b2ClipQuad, dd);
                dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
                dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
                dbgv._quadrant = new vec3(0, 0, 999);
                ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
                SetVertexPickAndColor(ref dbgv, (new vec4(1, 0, 0, .3f)), pickId);
                verts.Add(dbgv);//This is because of the new sorting issue
              }

              //Make Glyph Vert
              float adjust = 0;
              v_v4v4v4v2u2v4v4 vc = new v_v4v4v4v2u2v4v4();
              SetVertexRasterArea(ref vc, glyph._quads._b2BorderQuad, _quads._b2ClipQuad, dd);
              vc._rtl_rtr = new vec4(0, 0, 0, 0);
              vc._rbr_rbl = new vec4(0, 0, 0, 0);
              vc._quadrant = new vec3(0, 0, 999);
              ComputeVertexGlyphTCoord(ref vc, glyph._reference._cachedGlyph, adjust);
              SetVertexPickAndColor(ref vc, new vec4(Style._props.FontColor.xyz, Style._props.FontColor.w * (float)Style._props.Opacity), pickId);
              verts.Add(vc);//This is because of the new sorting issue
            }
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

      LayoutBlock(ele, align, vecLines, pmaxInnerWH, pcontentWH, dd, ref lineidx);

    }
    private void LayoutBlock(UiBlock ele, UiAlignment align, List<UiLine> vecLines, vec2 pmaxInnerWH, vec2 pcontentWH, UiDebugDraw dd, ref int lineidx)
    {
      //lays out a span of glyph elements, or just the element.
      float pspacex = pmaxInnerWH.x;//maximally equal to the Screen WH
      UiLine line = vecLines[lineidx];
      UiCol col = line._cols[(int)align];

      float e_width = ele._quads._b2LocalQuad._width;
      float e_height = ele._quads._b2LocalQuad._height;

      var e_pad = ele.GetPadding(dd);
      bool bLineBreak = false;
      if (ele.DisplayMode == UiDisplayMode.Inline || ele.DisplayMode == UiDisplayMode.Word)
      {
        float wordwidth = 0;
        if (ele.DisplayMode == UiDisplayMode.Word)
        {
          wordwidth = ele.WordWidth;
        }
        else
        {
          wordwidth = e_width + e_pad.left + e_pad.right;
        }

        if (wordwidth + line.Width > pspacex)
        {
          bLineBreak = true;
        }
      }
      else if (ele.DisplayMode == UiDisplayMode.Block)
      {
        bLineBreak = true;
      }
      else if (ele.DisplayMode == UiDisplayMode.InlineNoWrap)
      {
        bLineBreak = false;
      }

      if (bLineBreak)
      {
        if (lineidx + 1 >= vecLines.Count)
        {
          var line2 = new UiLine(0, line._top + line.Height);
          vecLines.Add(line2);
        }

        lineidx++;
        line = vecLines[lineidx];
        col = line._cols[(int)align];
      }

      var pmarb = this.GetMarginAndBorder(dd);
      float col_adv = 0;
      if (align == UiAlignment.Left)
      {
        col_adv = e_pad.left + pmarb.left;
      }
      else if (align == UiAlignment.Right)
      {
        col_adv = e_pad.right + pmarb.right;
      }
      else if (align == UiAlignment.Center)
      {
        col_adv = e_pad.right + e_pad.left;
      }
      else { Gu.BRThrowNotImplementedException(); }



      ele._quads._b2LocalQuad._left = line._left + col._width + col_adv + ele.Left;//BR:20221021-left/top are render offsets in Static mode
      ele._quads._b2LocalQuad._top = line._top + e_pad.top + ele.Top + pmarb.top;
      col._width += ele.Left + e_width + e_pad.left + e_pad.right;
      col._height = Math.Max(col._height, ele.Top + e_height + e_pad.top + e_pad.bot);
      col._eles.Add(ele);

      ele._quads._b2LocalQuad.Validate();
    }
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
        if (this.Style._props.PositionMode == UiPositionMode.Relative)
        {
          this._quads._b2BorderQuad._left = this._quads._b2LocalQuad._left = this.Left;
          this._quads._b2BorderQuad._top = this._quads._b2LocalQuad._top = this.Top;
        }
        else if (this.Style._props.PositionMode == UiPositionMode.Static)
        {
          this._quads._b2BorderQuad._left = this._quads._b2LocalQuad._left;
          this._quads._b2BorderQuad._top = this._quads._b2LocalQuad._top;
        }

        if (_parent != null)
        {
          this._quads._b2BorderQuad._left += _parent._quads._b2BorderQuad._left;
          this._quads._b2BorderQuad._top += _parent._quads._b2BorderQuad._top;
        }
      }
      else if (this.Style._props.PositionMode == UiPositionMode.Absolute)
      {
        this._quads._b2BorderQuad._left = this._quads._b2LocalQuad._left = this.Left;
        this._quads._b2BorderQuad._top = this._quads._b2LocalQuad._top = this.Top;
      }

      this._quads._b2BorderQuad._width = this._quads._b2LocalQuad._width;
      this._quads._b2BorderQuad._height = this._quads._b2LocalQuad._height;

      //initial clip

      if (this._quads._b2LocalQuad._left > 99999 || this._quads._b2LocalQuad._width > 99999) { Gu.DebugBreak(); }

      // Set to false if we're controllig coordinates of this element (cursor, or window position)
      this._quads._b2BorderQuad._left *= w1;
      this._quads._b2BorderQuad._top *= h1;
      this._quads._b2BorderQuad._width *= w1;
      this._quads._b2BorderQuad._height *= h1;

      this._quads._b2ClipQuad = this._quads._b2BorderQuad.Clone();

      this._quads._b2BorderQuad.Validate();

      //separate the border quad from the content area quad
      var bd = this.GetBorder(dd);
      this._quads._dbg_b2ContentQuad = this._quads._b2BorderQuad.Clone();
      this._quads._dbg_b2ContentQuad._left += bd.left;
      this._quads._dbg_b2ContentQuad._top += bd.top;
      this._quads._dbg_b2ContentQuad._width -= (bd.left + bd.right);
      this._quads._dbg_b2ContentQuad._height -= (bd.top + bd.bot);
      this._quads._dbg_b2ContentQuad.Validate();

      var pd = this.GetPadding(dd);
      this._quads._dbg_b2PaddingQuad = this._quads._b2BorderQuad.Clone();
      this._quads._dbg_b2PaddingQuad._left -= pd.left;
      this._quads._dbg_b2PaddingQuad._top -= pd.top;
      this._quads._dbg_b2PaddingQuad._width += (pd.right + pd.left);
      this._quads._dbg_b2PaddingQuad._height += (pd.bot + pd.top);
      this._quads._dbg_b2PaddingQuad.Validate();
    }
    public static bool disableoff = false;
    private void GetOpenGLQuadVerts(ReverseGrowList<v_v4v4v4v2u2v4v4> all_verts, UiQuad b2ClipRect, MtTex defaultPixel, uint rootPickId, UiDebugDraw dd)
    {
      if (Visible == false || Style._props.RenderMode == UiRenderMode.None)
      {
        return;
      }

      //**Texture Adjust - modulating repeated textures causes seaming issues, especially with texture filtering
      // adjust the texture coordinates by some pixels to account for that.  0.5f seems to work well.
      float adjust = 0;// 1.4f;  // # of pixels to adjust texture by

      //Debug overlay
      if (dd.ShowOverlay)
      {
        //preoffset border
        v_v4v4v4v2u2v4v4 dbgv = new v_v4v4v4v2u2v4v4();
        SetVertexRasterArea(ref dbgv, in _quads._dbg_b2ContentQuad, in b2ClipRect, dd);
        dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
        dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
        dbgv._quadrant = new vec3(0, 0, 999);
        ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
        SetVertexPickAndColor(ref dbgv, (new vec4(0, 1, 0, .3f)), rootPickId);
        all_verts.Add(dbgv);//This is because of the new sorting issue

        // SetVertexRasterArea(ref dbgv, in _quads._b2BorderQuad, in b2ClipRect, dd);
        // dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
        // dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
        // dbgv._quadrant = new vec3(0, 0, 999);
        // ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
        // SetVertexPickAndColor(ref dbgv, (new vec4(1, 0, 1, .3f)), rootPickId);
        // all_verts.Add(dbgv);//This is because of the new sorting issue  

        SetVertexRasterArea(ref dbgv, in _quads._dbg_b2PaddingQuad, in b2ClipRect, dd);
        dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
        dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
        dbgv._quadrant = new vec3(0, 0, 999);
        ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
        SetVertexPickAndColor(ref dbgv, (new vec4(1, 1, 0, 0.3)), rootPickId);
        all_verts.Add(dbgv);//This is because of the new sorting issue        
      }

      var bd = GetBorder(dd);
      var radius = this.GetBorderRadius(dd);

      //Content Quad w/ margins
      v_v4v4v4v2u2v4v4 vc = new v_v4v4v4v2u2v4v4();
      vc._rtl_rtr = new vec4(radius.top, radius.right);
      vc._rbr_rbl = new vec4(radius.bot, radius.left);
      vc._quadrant = new vec3(0, 0, 999);
      SetVertexRasterArea(ref vc, in _quads._dbg_b2ContentQuad, in b2ClipRect, dd);

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

      vec4 cmul = Style._props.Color * Style._props.MultiplyColor;
      SetVertexPickAndColor(ref vc, new vec4(cmul.xyz, cmul.w * (float)Style._props.Opacity).Clamp(0.0f, 1.0f), rootPickId);
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
          bt._height = _quads._dbg_b2ContentQuad._top - _quads._b2BorderQuad._top;
        }
        var r2 = usequad ? new vec4(radius.x, radius.y, 0, 0) : radius;
        DoBorder(bt, Style._props.BorderColorTop, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(0, 1, usequad ? 0 : 999));
      }
      if (bd.right > 0)
      {
        UiQuad bt = _quads._b2BorderQuad.Clone();
        if (!usequad)
        {
          bt._left += _quads._dbg_b2ContentQuad._width;
          bt._width -= _quads._dbg_b2ContentQuad._width;
        }
        var r2 = usequad ? new vec4(0, radius.y, radius.z, 0) : radius;
        DoBorder(bt, Style._props.BorderColorRight, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(1, 0, usequad ? 0 : 999));
      }
      if (bd.bot > 0)
      {
        UiQuad bt = _quads._b2BorderQuad.Clone();
        if (!usequad)
        {
          bt._top += _quads._dbg_b2ContentQuad._height;
          bt._height -= _quads._dbg_b2ContentQuad._height;
        }
        var r2 = usequad ? new vec4(0, 0, radius.z, radius.w) : radius;
        DoBorder(bt, Style._props.BorderColorBot, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(0, -1, usequad ? 0 : 999));
      }
      if (bd.left > 0)
      {
        UiQuad bt = _quads._b2BorderQuad.Clone();
        if (!usequad)
        {
          bt._width = _quads._dbg_b2ContentQuad._left - _quads._b2BorderQuad._left;
        }
        var r2 = usequad ? new vec4(radius.x, 0, 0, radius.w) : radius;
        DoBorder(bt, Style._props.BorderColorLeft, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(-1, 0, usequad ? 0 : 999));
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
      SetVertexPickAndColor(ref vb, new vec4(bodfercolor.xyz, bodfercolor.w * (float)Style._props.Opacity), rootPickId);
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
    private static void ComputeVertexGlyphTCoord(ref v_v4v4v4v2u2v4v4 vc, MtCachedCharData? glyph, float adjust)
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
    private static void SetVertexRasterArea(ref v_v4v4v4v2u2v4v4 vc, in UiQuad rasterQuad, in UiQuad b2ClipRect, UiDebugDraw dd)
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
    private static void SetVertexPickAndColor(ref v_v4v4v4v2u2v4v4 vc, vec4 color, uint rootPickId)
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
    private void UpdateGlyphSpans(MegaTex mt)
    {
      //create spans for glyph words

      Gu.Assert(Style._props.FontFace != null);

      //reset max glyph size
      _quads.GlyphWH = new vec2(0, 0);

      if (String.IsNullOrEmpty(_strText))
      {
        _glyphChars = null;
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

      //  var ch = _strText.Distinct().ToList();//this is interesting
      _glyphs = new List<UiGlyph>();
      _glyphChars = new Dictionary<int, UiGlyphChar>();
      UiGlyph? wordstart = null;
      int chlast = 0;
      int ch = 0;
      //redoing this whole boo
      for (int ci = 0; ci < _strText.Length; ci++)
      {
        chlast = ch;
        ch = _strText[ci];

        UiGlyphChar? gg = null;
        if (!_glyphChars.TryGetValue(ch, out gg))
        {
          gg = new UiGlyphChar();

          int chnext = (ci + 1) < _strText.Length ? _strText[ci + 1] : 0;

          float sscl = 0;
          if (!patch.GetChar(ch, fontHeight, out gg._cachedGlyph, out sscl))
          {
            Gu.DebugBreak();//Unicode ch not found
          }
          float kern = font.GetKernAdvanceWidth(patch, ch, chnext);

          //DoGlyph
          //the question is line gap
          //setting the line gap to be at the bottom does not center text correctly vertically in things like buttons
          // dividing the line gap works for this
          float gleft = gg._cachedGlyph.ch_left * sscl;
          float gtop = gg._cachedGlyph.ch_top * sscl;
          float gright = gg._cachedGlyph.ch_right * sscl;
          float gbot = gg._cachedGlyph.ch_bot * sscl;
          float gwidth = (gright - gleft) + kern;
          float gheight = (gbot - gtop);

          float lineheight = gg._cachedGlyph.font_lineHeight * gg._cachedGlyph.font_scale * sscl;
          float gadvance = gg._cachedGlyph.ch_advance * gg._cachedGlyph.font_scale * sscl;
          float gascent = gg._cachedGlyph.font_ascent * gg._cachedGlyph.font_scale * sscl * Style._props.LineHeight;
          float gkern = kern * sscl;
          float linegap = gg._finalLineHeight - gg._glyphQuad._top - gg._glyphQuad._height;

          gg._finalLineHeight = lineheight * Style._props.LineHeight;
          gg._glyphQuad._left = gleft;
          gg._glyphQuad._top = gtop + gascent;
          gg._glyphQuad._width = gwidth;
          gg._glyphQuad._height = gheight;
          gg._padding = new vec4( //top, right, bot, left
            linegap / 2,
            gadvance - gright,
            linegap / 2,
            0
          );
          gg._glyphQuad.Validate();

          _glyphChars.Add(ch, gg);

          ExpandGlyphWH(gg);
        }

        UiGlyph gc = new UiGlyph(gg);
        gc._quads._b2LocalQuad = gg._glyphQuad.Clone();
        gc._left_off = gc._quads._b2LocalQuad._left;
        gc._top_off = gc._quads._b2LocalQuad._top;
        gc._padding = gg._padding;
        gc._char = ch;
        gc._wordWidth = 0;

        if (wordstart == null)
        {
          wordstart = gc;
        }
        bool chws = char.IsWhiteSpace((char)ch);
        if (chws || char.IsWhiteSpace((char)chlast))
        {
          if (ch == '\n')
          {
            gc._displayMode = UiDisplayMode.Block;
          }
          else if (chws)
          {
            gc._displayMode = UiDisplayMode.Inline;
          }
          else
          {
            gc._displayMode = UiDisplayMode.Word;
          }
          wordstart = gc;
        }
        wordstart._wordWidth += gc._left_off + gc._quads._b2LocalQuad._width + gc._padding.left + gc._padding.right;

        _glyphs.Add(gc);
      }

    }
    private void ExpandGlyphWH(UiGlyphChar glyph)
    {
      _quads.GlyphWH.x = Math.Max(_quads.GlyphWH.x, glyph._glyphQuad._width);
      _quads.GlyphWH.y = Math.Max(_quads.GlyphWH.y, glyph._glyphQuad._height);
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
    public string Name { get; private set; } = Lib.UnsetName;

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

    public RenderView RenderView { get; private set; }
    public UiDebugDraw DebugDraw { get; set; } = new UiDebugDraw();
    public MeshData Mesh { get; set; } = null;
    public MeshView MeshView { get; set; } = null;
    public long _dbg_UpdateMs { get; private set; } = 0;
    public long _dbg_MeshMs { get; private set; } = 0;
    public long _dbg_EventsMs { get; private set; } = 0;

    public UiStyleSheet StyleSheet { get; set; } = null;

    #endregion
    #region Private:Members

    private vec2 _viewport_wh_last = new vec2(1, 1);
    private Gui2dShared _shared = null;
    //private GVertMap _gverts = new GVertMap();
    private UiAsyncUpdateState _state = UiAsyncUpdateState.CanUpdate;
    private UiEventThing _eventThing = new UiEventThing();
    private Dictionary<uint, UiElement> _pickable = new Dictionary<uint, UiElement>();
    private bool _async = false;
    private Dictionary<UiElement, Dictionary<UiPropName, IUiPropAnimation>>? _animations = null;
    //private DeltaTimer _propAnimationTimer ;

    #endregion
    #region Public: Methods

    public Gui2d(Gui2dShared shared, RenderView cam)
    {
      StyleSheet = new UiStyleSheet(new FileLoc("ui-default.css", FileStorage.Embedded));
      _shared = shared;
      RenderView = cam;
      Name = "screen(root)";

      //TODO:
      // _propAnimationTimer = new DeltaTimer(c_iPropAnimationTime, true, ActionState.Run, )

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

      UpdatePropAnimations(dt);

      //queue update if processed events.
      if (_state == UiAsyncUpdateState.CanUpdate)
      {
        _state = UiAsyncUpdateState.Updating;
        ThreadPool.QueueUserWorkItem(stateinfo =>
        {
          Dictionary<uint, UiElement>? pickable = null;
          if (RenderView != null && RenderView.Enabled)
          {
            long a = Gu.Milliseconds();
            StyleSheet?.Update();
            SetExtentsToViewport(RenderView);
            UpdateLayout_Async(_shared.MegaTex, Gu.Context.PCMouse, RenderView, ref pickable);
            this._dbg_UpdateMs = Gu.Milliseconds() - a;
          }
          Gu.Context.Gpu.Post_To_RenderThread(Gu.Context, x =>
          {
            if (pickable != null)
            {
              _pickable = pickable;
            }
            if (RenderView != null && RenderView.Enabled)
            {
              SendMeshToGpu_Sync(RenderView);
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

        if (RenderView != null)
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
    public void AddAnimation(IUiPropAnimation p)
    {
      //add a property animation (see $jquery(element).animate({ prop:value }); )
      Gu.Assert(p != null);
      Gu.Assert(p.Element != null);
      _animations = _animations.ConstructIfNeeded();

      Dictionary<UiPropName, IUiPropAnimation>? plist = null;
      if (!_animations.TryGetValue(p.Element, out plist))
      {
        plist = new Dictionary<UiPropName, IUiPropAnimation>();
        _animations.Add(p.Element, plist);
      }

      //overwrite with new, not allowing multiple animations on a prop.
      plist[p.Prop] = p;

    }

    #endregion
    #region Private: Methods

    private void UpdatePropAnimations(double dt)
    {
      if (_animations != null)
      {
        List<UiElement> toRemoveEles = new List<UiElement>();
        foreach (var ea in _animations)
        {
          List<UiPropName> toRemove = new List<UiPropName>();
          foreach (var ep in ea.Value)
          {
            if (ep.Value.Update(dt))
            {
              toRemove.Add(ep.Key);
            }
          }
          foreach (var k in toRemove)
          {
            ea.Value.Remove(k);
          }
          if (ea.Value.Count == 0)
          {
            toRemoveEles.Add(ea.Key);
          }
        }
        foreach (var k in toRemoveEles)
        {
          _animations.Remove(k);
        }
      }
    }
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
    int _viewx = 0;
    int _viewy = 0;
    int _vieww = 0;
    int _viewh = 0;
    private void SetExtentsToViewport(RenderView rv)
    {
      if (
        rv.Viewport.X != _viewx ||
        rv.Viewport.Y != _viewy ||
        rv.Viewport.Width != _vieww ||
        rv.Viewport.Height != _viewh
      )
      {
        _viewx = rv.Viewport.X;
        _viewy = rv.Viewport.Y;
        _vieww = rv.Viewport.Width;
        _viewh = rv.Viewport.Height;

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

        _quads._dbg_b2ContentQuad._left = _quads._b2LocalQuad._left = rv.Viewport.X;
        _quads._dbg_b2ContentQuad._top = _quads._b2LocalQuad._top = rv.Viewport.Y;
        _quads._dbg_b2ContentQuad._width = _quads._b2LocalQuad._width = rv.Viewport.Width;
        _quads._dbg_b2ContentQuad._height = _quads._b2LocalQuad._height = rv.Viewport.Height;
        _quads._b2ClipQuad = _quads._dbg_b2ContentQuad.Clone();
        _quads._b2LocalQuad = _quads._dbg_b2ContentQuad.Clone();
      }
    }

    #endregion

  }//Gui2d
  public class Gui2dShared
  {
    //Shared data between Gui2d instances for each context
    // public UiStyleSheet StyleSheet { get; private set; } = null;
    public Drawable Dummy { get; private set; } = null;
    public MegaTex MegaTex { get; private set; } = null;
    public string Name { get; private set; } = Lib.UnsetName;

    public Gui2dShared(string name, List<FileLoc> resources)
    {
      Name = name;
      //DO NOT USE MIPMAPS
      MegaTex = new MegaTex("gui_megatex", true, MegaTex.MtClearColor.DebugRainbow, false, TexFilter.Linear, 0);//nearest
      MegaTex.AddResources(resources);
      var tx = MegaTex.Compile();

      //StyleSheet = new UiStyleSheet(MegaTex);

      if (tx != null)
      {
        var shader = Gu.Lib.GetShader(Rs.Shader.GuiShader);
        Dummy = new WorldObject("gui");
        Dummy.Material = new Material("GuiMT", shader);
        Dummy.Material.GpuRenderState.DepthTest = false;
        Dummy.Material.GpuRenderState.Blend = true;
        Dummy.Material.AlbedoSlot.Texture = tx.Albedo;
        Dummy.Material.DrawMode = DrawMode.Forward;
        Dummy.Material.DrawOrder = DrawOrder.Last;
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
    protected override Dictionary<ulong, Gui2dShared> CreateNew()
    {
      return new Dictionary<ulong, Gui2dShared>();
    }
    public Gui2dShared GetOrCreateGui2d(string name, List<FileLoc> resources)
    {
      var qualifiedPaths = resources.ConvertAll((x) => { return x.QualifiedPath; });
      var hash = Hash.HashStringArray(qualifiedPaths);

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
