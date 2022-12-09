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
    protected FontFace(string name, string file) : base(file, EmbeddedFolder.Font) { Name = name; }
    public static FontFace Parisienne = new FontFace("Parisienne", "Parisienne-Regular.ttf");
    public static FontFace RobotoMono = new FontFace("RobotoMono", "RobotoMono-Regular.ttf");
    public static FontFace PressStart2P = new FontFace("PressStart2P", "PressStart2P-Regular.ttf");
    public static FontFace Calibri = new FontFace("Calibri", "calibri.ttf");
    public static FontFace EmilysCandy = new FontFace("EmilysCandy", "EmilysCandy-Regular.ttf");
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
    Percent, //Expand to parent%
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
    Show, //show elements outside of clip region (not element region)
    Hide
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
  public enum UiLayoutOrientation
  {
    //https://www.w3.org/TR/CSS2/visuren.html#propdef-direction
    Horizontal,
    Vertical,
  }
  public enum UiLayoutDirection
  {
    Right, //roman
    Left, //arabic
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
    , Texture
    , ImageTilingX
    , ImageTilingY
    , ImageScaleX
    , ImageScaleY
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
    , LayoutOrientation
    , LayoutDirection

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
  public class UiDebug
  {

    public bool DisableClip = false;
    public bool ShowDebug = false;
    public bool DisableMargins = false;
    public bool DisablePadding = false;
    public bool DisableBorders = false;

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
      bool v = this.ToBox().Validate(debug_break, min_volume);

      //"sane" parameters for debugging, shold never go beyond 99999 on 4k
      if (_left > 99999 || _width > 99999 || _top > 99999 || _height > 99999 ||
          _left < -99999 || _width < -99999 || _top < -99999 || _height < -99999)
      {
        v = false;
        if (debug_break)
        {
          Gu.DebugBreak();
        }
      }

      return v;
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
    public float LSize(UiLayoutOrientation dir)
    {
      if (dir == UiLayoutOrientation.Horizontal)
      {
        return _width;
      }
      else if (dir == UiLayoutOrientation.Vertical)
      {
        return _height;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return _width;
    }
    public void LSize(UiLayoutOrientation dir, float value)
    {
      if (dir == UiLayoutOrientation.Horizontal)
      {
        _width = value;
      }
      else if (dir == UiLayoutOrientation.Vertical)
      {
        _height = value;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    public float LMin(UiLayoutOrientation dir)
    {
      if (dir == UiLayoutOrientation.Horizontal)
      {
        return _left;
      }
      else if (dir == UiLayoutOrientation.Vertical)
      {
        return _top;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return _left;
    }
    public void LMin(UiLayoutOrientation dir, float value)
    {
      if (dir == UiLayoutOrientation.Horizontal)
      {
        _left = value;
      }
      else if (dir == UiLayoutOrientation.Vertical)
      {
        _top = value;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    public float LMax(UiLayoutOrientation dir)
    {
      if (dir == UiLayoutOrientation.Horizontal)
      {
        return _left + _width;
      }
      else if (dir == UiLayoutOrientation.Vertical)
      {
        return _top + _height;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return _left + _width;
    }
    //cant set
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
    public float FixedWidth = 100;
    public float FixedHeight = 100;
    public float PercentWidth = 100;
    public float PercentHeight = 100;
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
    public float ImageScaleX = 1;
    public float ImageScaleY = 1;
    public UiDisplayMode DisplayMode = UiDisplayMode.Block;
    public UiPositionMode PositionMode = UiPositionMode.Static;
    public UiOverflowMode OverflowMode = UiOverflowMode.Hide;
    public UiSizeMode SizeModeWidth = UiSizeMode.Percent;
    public UiSizeMode SizeModeHeight = UiSizeMode.Percent;
    public float ZIndex = 0;
    public UiFloatMode FloatMode = UiFloatMode.None;
    public UiRenderMode RenderMode = UiRenderMode.None;
    public UiAlignment TextAlign = UiAlignment.Left;
    public UiAlignment Alignment = UiAlignment.Left;
    public double Opacity = 1;
    public UiLayoutOrientation LayoutOrientation = UiLayoutOrientation.Horizontal;
    public UiLayoutDirection LayoutDirection = UiLayoutDirection.Right;

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
      //yes css does let width outside 100 but this is for sanity and well
      if (!Gu.AssertDebug(PercentWidth >= 0 && PercentWidth <= 100))
      {
        PercentWidth = Math.Clamp(PercentWidth, 0, 100);
      }
      if (!Gu.AssertDebug(PercentHeight >= 0 && PercentHeight <= 100))
      {
        PercentHeight = Math.Clamp(PercentHeight, 0, 100);
      }

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
        p == UiPropName.Left
        || p == UiPropName.Top
        || p == UiPropName.MinWidth
        || p == UiPropName.MaxWidth
        || p == UiPropName.MinHeight
        || p == UiPropName.MaxHeight
        //this is incorrect for CSS but it makes things easier to just inherit these
        //if you do not inherit them you have to set the class style manually as we do not support an "inherit" property
        || p == UiPropName.MarginTop
        || p == UiPropName.MarginRight
        || p == UiPropName.MarginBot
        || p == UiPropName.MarginLeft
        || p == UiPropName.PadTop
        || p == UiPropName.PadRight
        || p == UiPropName.PadBot
        || p == UiPropName.PadLeft
        || p == UiPropName.BorderColorTop
        || p == UiPropName.BorderColorRight
        || p == UiPropName.BorderColorBot
        || p == UiPropName.BorderColorLeft
        || p == UiPropName.BorderTop
        || p == UiPropName.BorderRight
        || p == UiPropName.BorderBot
        || p == UiPropName.BorderLeft
        || p == UiPropName.FixedWidth
        || p == UiPropName.FixedHeight
        || p == UiPropName.PercentWidth
        || p == UiPropName.PercentHeight
      //|| p == UiPropName.RenderMode

      )
      {
        return false;
      }

      return true;
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
    #region Props

    public string Name { get; set; } = "";
    public bool Modified { get { return _bMustCompile; } }
    public bool IsInline { get; set; }

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
    public UiSizeMode SizeMode { set { SizeModeWidth = value; SizeModeHeight = value; } }


    //Do not use nullable<> or ? types on class types here. This will return (null) even if the class type is set on the nullable boxed.
    public float? Top { get { return (float?)GetClassProp(UiPropName.Top); } set { SetProp(UiPropName.Top, (float?)value); } }
    public float? Left { get { return (float?)GetClassProp(UiPropName.Left); } set { SetProp(UiPropName.Left, (float?)value); } }
    public float? MinWidth { get { return (float?)GetClassProp(UiPropName.MinWidth); } set { SetProp(UiPropName.MinWidth, (float?)value); } }
    public float? MinHeight { get { return (float?)GetClassProp(UiPropName.MinHeight); } set { SetProp(UiPropName.MinHeight, (float?)value); } }
    public float? MaxWidth { get { return (float?)GetClassProp(UiPropName.MaxWidth); } set { SetProp(UiPropName.MaxWidth, (float?)value); } }
    public float? MaxHeight { get { return (float?)GetClassProp(UiPropName.MaxHeight); } set { SetProp(UiPropName.MaxHeight, (float?)value); } }
    public float? FixedWidth { get { return (float?)GetClassProp(UiPropName.FixedWidth); } set { SetProp(UiPropName.FixedWidth, (float?)value); } }
    public float? FixedHeight { get { return (float?)GetClassProp(UiPropName.FixedHeight); } set { SetProp(UiPropName.FixedHeight, (float?)value); } }
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
    public float? ImageScaleX { get { return (float?)GetClassProp(UiPropName.ImageScaleX); } set { SetProp(UiPropName.ImageScaleX, (float?)value); } }
    public float? ImageScaleY { get { return (float?)GetClassProp(UiPropName.ImageScaleY); } set { SetProp(UiPropName.ImageScaleY, (float?)value); } }
    public MtTex Texture { get { return (MtTex)GetClassProp(UiPropName.Texture); } set { SetProp(UiPropName.Texture, (MtTex)value); } }
    public float? ZIndex { get { return (float?)GetClassProp(UiPropName.ZIndex); } set { SetProp(UiPropName.ZIndex, (float?)value); } }
    public UiFloatMode? FloatMode { get { return (UiFloatMode?)GetClassProp(UiPropName.FloatMode); } set { SetProp(UiPropName.FloatMode, (UiFloatMode?)value); } }
    public UiRenderMode? RenderMode { get { return (UiRenderMode?)GetClassProp(UiPropName.RenderMode); } set { SetProp(UiPropName.RenderMode, (UiRenderMode?)value); } }
    public UiAlignment? TextAlign { get { return (UiAlignment?)GetClassProp(UiPropName.TextAlign); } set { SetProp(UiPropName.TextAlign, (UiAlignment?)value); } }
    public UiAlignment? Alignment { get { return (UiAlignment?)GetClassProp(UiPropName.Alignment); } set { SetProp(UiPropName.Alignment, (UiAlignment?)value); } }
    public double? Opacity { get { return (double?)GetClassProp(UiPropName.Opacity); } set { SetProp(UiPropName.Opacity, (double?)value); } }
    public UiLayoutOrientation? LayoutOrientation { get { return (UiLayoutOrientation?)GetClassProp(UiPropName.LayoutOrientation); } set { SetProp(UiPropName.LayoutOrientation, (UiLayoutOrientation?)value); } }
    public UiLayoutDirection? LayoutDirection { get { return (UiLayoutDirection?)GetClassProp(UiPropName.LayoutDirection); } set { SetProp(UiPropName.LayoutDirection, (UiLayoutDirection?)value); } }
    public float? PercentWidth { get { return (float?)GetClassProp(UiPropName.PercentWidth); } set { SetProp(UiPropName.PercentWidth, (float?)value); } }
    public float? PercentHeight { get { return (float?)GetClassProp(UiPropName.PercentHeight); } set { SetProp(UiPropName.PercentHeight, (float?)value); } }

    #endregion
    #region Members 

    public UiProps _props = new UiProps();//Gets the compiled / final props
    public bool IsPropsOnly { get; set; } = false;//For glyph, don't inherit parent or compile, and re-compile the class every time.. we set _props manually
    public WeakReference<UiStyleSheet> StyleSheet { get; private set; } = null;
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
    private bool _bMustCompile = true;
    private long _changedFrameId = 0;

    #endregion
    #region Public: Methods

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
    public float? LMin(UiLayoutOrientation dir)
    {
      if (dir == UiLayoutOrientation.Horizontal)
      {
        return Left;
      }
      else if (dir == UiLayoutOrientation.Vertical)
      {
        return Top;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return Left;
    }
    public void LMin(UiLayoutOrientation dir, float value)
    {
      if (dir == UiLayoutOrientation.Horizontal)
      {
        Left = value;
      }
      else if (dir == UiLayoutOrientation.Vertical)
      {
        Top = value;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
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

          // DebugStorePropDetails();

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

          //DebugStorePropDetails();

          _changed.SetAll(false);
          CompiledFrameId = framestamp;
          _bMustCompile = false;
          _props.Validate();
        }
      }
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

    #endregion

  }//cls
  public class UiQuads
  {
    //*render quad is the origin
    public UiQuad _b2ClipQuad = new UiQuad(); // all floating and contained, elements and min/max w/h. *clip quad may not equal computed quad if there are floating elements
    public UiQuad _b2LocalQuad = new UiQuad(); // computed width/height of the contained items, parent-relative
    public UiQuad _dbg_b2ContentQuad = new UiQuad();
    public UiQuad _dbg_b2PaddingQuad = new UiQuad();
    public UiQuad _b2BorderQuad = new UiQuad(); // Final quad. content area + margin + border area
    public vec2 ContentWH = new vec2(0, 0);
    public vec2 GlyphWH = new vec2(0, 0);//max width/height of all glyphs
    public vec2 OuterMaxWH = new vec2(0, 0);//parent wh clamped to this element's min/max 
    public vec2 InnerMaxWH = new vec2(0, 0);//outermaxwh - border -margin
  }
  public abstract class UiBlock //base interface for for glyphs / eles
  {
    //for debug only
    private static System.Random r = null;
    public vec4 _debugcolor;

    //interface between UiGlyph and UiElement
    public abstract float Left { get; }
    public abstract float Top { get; }
    public abstract UiDisplayMode DisplayMode { get; }
    public virtual float WordWidth { get { return 0; } }
    public virtual float WordHeight { get { return 0; } }
    public abstract vec4 GetPadding(UiDebug dd);
    public UiQuads _quads = new UiQuads();
    public UiBlock()
    {
      if (r == null) r = new System.Random();
      _debugcolor = new vec4(r.NextSingle(), r.NextSingle(), r.NextSingle(), 0.11f);
    }
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
    public float _wordHeight = 0;
    public UiGlyphChar? _reference = null;

    public override float Left { get { return _left_off; } }
    public override float Top { get { return _top_off; } }
    public override float WordWidth { get { return _wordWidth; } }
    public override float WordHeight { get { return _wordHeight; } }
    public override UiDisplayMode DisplayMode { get { return _displayMode; } }

    public UiGlyph(UiGlyphChar? dref)
    {
      _reference = dref;
    }
    public override vec4 GetPadding(UiDebug dd)
    {
      return _padding;
    }
  }
  public class UiElement : UiBlock
  {
    private const int c_iMaxLayers = 1000000;
    private const int c_iMaxChildren = c_iMaxLayers - 1;

    #region Classes 
    private class SortKeys
    {
      //claculate sub-sort layers for all element quads
      public float _ekey;
      public float _dbgkey;
      public float _bdkey;
      public float _gkey;
      public float _gdbkey;

      public SortKeys(int sort, int layer)
      {
        _ekey = (float)(layer * c_iMaxLayers + sort);
        _gkey = _ekey + 0.3f;
        _gdbkey = _ekey + 0.4f;
        _bdkey = _ekey + 0.5f;
        _dbgkey = _ekey + 0.6f;
      }
    }
    private class UiAlignCol
    {
      public float _height = 0;
      public float _width = 0;
      public List<UiBlock> _eles = new List<UiBlock>();

      public float LSize(UiLayoutOrientation dir)
      {
        if (dir == UiLayoutOrientation.Horizontal)
        {
          return _width;
        }
        else if (dir == UiLayoutOrientation.Vertical)
        {
          return _height;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
        return _width;
      }
      public void LSize(UiLayoutOrientation dir, float value)
      {
        if (dir == UiLayoutOrientation.Horizontal)
        {
          _width = value;
        }
        else if (dir == UiLayoutOrientation.Vertical)
        {
          _height = value;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
      }
    }
    private class UiLine
    {
      public UiAlignCol[] _cols = new UiAlignCol[3] { new UiAlignCol(), new UiAlignCol(), new UiAlignCol() };//left/center/right
      public float _top = 0;//not null depending on UiBuildOrder
      public float _left = 0;
      public float Height(UiLayoutOrientation o)
      {
        if (o == UiLayoutOrientation.Horizontal)
        {
          return Math.Max(_cols[0]._height, Math.Max(_cols[1]._height, _cols[2]._height));
        }
        else if (o == UiLayoutOrientation.Vertical)
        {
          return _cols[0]._height + _cols[1]._height + _cols[2]._height;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
        return 0;
      }
      public float Width(UiLayoutOrientation o)
      {
        if (o == UiLayoutOrientation.Horizontal)
        {
          return _cols[0]._width + _cols[1]._width + _cols[2]._width;
        }
        else if (o == UiLayoutOrientation.Vertical)
        {
          return Math.Max(_cols[0]._width, Math.Max(_cols[1]._width, _cols[2]._width));
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
        return 0;
      }

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
    public int Sort { get { return _sort; } set { _sort = value; } }
    public bool PickEnabled { get { return _pickEnabled; } set { _pickEnabled = value; UpdatePickID(); } }

    #endregion
    #region Private: Members

    protected string _name = "";
    protected UiStyle? _style = null;
    protected List<UiElement>? _children = null;
    private UiElement? _parent = null;
    private MtFontLoader? _cachedFont = null;//For labe(ls that contain glyphs
    private Dictionary<int, UiGlyphChar> _glyphChars = null;
    private List<UiGlyph> _glyphs = null;
    public uint _iPickId = Picker.c_iInvalidPickId;
    protected string _strText = "";
    private string _strTextLast = "";
    private Dictionary<UiEventId, List<UiAction>>? _events = null;
    protected int _sort = -1;  //sort order within layer

    //Flags (TODO: bitset)
    private bool _pickEnabled = false;
    private bool _visible = true;
    private bool _textChanged = false;
    private bool _bNeverDoneText = false;
    private bool _dragEnabled = false;
    private bool _contentChanged = true;
    private bool _topMost = false;

    #endregion
    #region Public: Methods

    public UiElement()
    {
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
    public vec4 GetMarginAndBorder(UiDebug dd)
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
    public vec4 GetMargin(UiDebug dd)
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
    public override vec4 GetPadding(UiDebug dd)
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
    public vec4 GetBorder(UiDebug? dd)
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
    public vec4 GetBorderRadius(UiDebug dd)
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
      // e._layer = this._layer + 1;
      // if (e._layer > c_iMaxLayers)
      // {
      //   e._layer = c_iMaxLayers;
      //   Gu.DebugBreak();//this will cause errors due to sort key.
      // }
      e._sort = this._children.Count + 1; // sort 0 is reserved
      if (e._sort > c_iMaxChildren)
      {
        e._sort = c_iMaxChildren;
        Gu.DebugBreak();
      }

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
          //e._layer = -1;
          e._sort = -1;
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
      PickEnabled = true;
    }
    public bool RemoveEvents(UiEventId evId)
    {
      //remove all events of id
      bool ret = _events.Remove(evId);
      if (_events.Count == 0)
      {
        PickEnabled = false;
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

    public void AddStyle(string name)
    {
      this.Style.InheritFrom(name);
    }
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
    private void UpdatePickID()
    {
      if (_pickEnabled)
      {
        if (_iPickId == Picker.c_iInvalidPickId)
        {
          _iPickId = Gu.Context.Renderer.Picker.GenPickId();
        }
      }
      else
      {
        _iPickId = Picker.c_iInvalidPickId;
      }
    }
    private static bool IsFullyClipped(UiQuad quad, UiQuad clip, UiOverflowMode mode, UiDebug dd)
    {
      if (dd.DisableClip)
      {
        return false;
      }
      if (mode == UiOverflowMode.Hide)
      {
        if (quad.Max.x < clip.Min.x)
        {
          return true;
        }
        if (quad.Max.y < clip.Min.y)
        {
          return true;
        }
        if (quad.Min.x > clip.Max.x)
        {
          return true;
        }
        if (quad.Min.y > clip.Max.y)
        {
          return true;
        }
      }
      return false;
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
    protected virtual void PerformLayout_SizeElements(MegaTex mt, bool bForce, vec2 parentMaxWH, UiStyle? parent, UiStyleSheet sheet, long framesatmp, UiDebug dd, List<UiElement> parentexpanders)
    {
      //if (_layoutChanged || bForce)
      {
        bool styleChanged = Style.Modified;

        Style.CompileStyleTree(sheet, framesatmp, parent);

        if (((_textChanged || styleChanged) && (framesatmp % 5 == 0 || _bNeverDoneText)))
        {
          UpdateGlyphSpans(mt);
          _textChanged = false;
          _bNeverDoneText = false;
        }

        //shrink max rect by parent 
        //remove margins from maxwh before sending into child, then compute our w/h by removing padding from our parent maxwh
        _quads.OuterMaxWH = new vec2(
          Math.Max(Math.Min(parentMaxWH.width, MaxWidthE()), 0),
          Math.Max(Math.Min(parentMaxWH.height, MaxHeightE()), 0)
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
        var spanLines = new List<UiLine>();
        var expanders = new List<UiElement>();
        spanLines.Add(new UiLine(0, 0));
        int lineidx = 0;


        if (_children != null && _children.Count > 0)
        {
          //compute min content WH first
          foreach (var ele in _children)
          {
            //do not hide opacity=0 elements they still take up block space
            if (ele.Visible)
            {
              ele.PerformLayout_SizeElements(mt, bForce, _quads.InnerMaxWH, this.Style, sheet, framesatmp, dd, expanders);

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

          //Layout non-glyphs
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

        ComputeContentWH(pmarb, spanLines, dd);
        SizeElement(_quads.ContentWH, _quads.InnerMaxWH, dd, parentexpanders);
        AlignElements(spanLines);
        FixExpanders(expanders, dd);

      }
    }
    protected void PerformLayout_PositionElements(bool bForce, UiDebug dd, SortedList<float, v_v4v4v4v2u2v4v4> verts, UiQuad parentClip, MtTex defaultPixel,
      uint rootPickId, ref Dictionary<uint, UiElement>? pickable, int layer)
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

        UiQuad clip = ShrinkClipRect(parentClip);

        if (_children != null && _children.Count > 0)
        {
          foreach (var ele in _children)
          {
            if (ele.Visible)
            {
              var child_layer = ele._topMost ? 0 : layer + 1;

              ele.PerformLayout_PositionElements(bForce, dd, verts, clip, defaultPixel, pickId, ref pickable, child_layer);

              //expand clip
              _quads._b2ClipQuad.ExpandByPoint(ele._quads._b2ClipQuad.Min);
              _quads._b2ClipQuad.ExpandByPoint(ele._quads._b2ClipQuad.Max);
            }
          }
        }

        SortKeys keys = new SortKeys(_sort, this._topMost ? 0 : layer);

        //no reason to compute glyph positions if the whole thing is not rendered
        //glyph spans.
        if (_glyphs != null)
        {
          for (int gi = 0; gi < _glyphs.Count; ++gi)
          {
            var glyph = _glyphs[gi];
            if (glyph != null)
            {
              //apply parent
              glyph._quads._b2BorderQuad = glyph._quads._b2LocalQuad.Clone();
              glyph._quads._b2BorderQuad._left += this._quads._b2BorderQuad._left;
              glyph._quads._b2BorderQuad._top += this._quads._b2BorderQuad._top;

              //note glyph border=clipp
              if (IsFullyClipped(glyph._quads._b2BorderQuad, parentClip, Style._props.OverflowMode, dd) == false)
              {
                GetGlyphQuadVerts(glyph, verts, dd, defaultPixel, pickId, keys);
              }
            }
          }
        }

        UiOverflowMode parentOverflowMode = UiOverflowMode.Hide;
        if (this.Parent != null && this.Parent.Style != null)
        {
          parentOverflowMode = this.Parent.Style._props.OverflowMode;
        }

        if (Style._props.FloatMode == UiFloatMode.Floating)
        {
          GetElementQuadVerts(verts, _quads._b2ClipQuad, defaultPixel, pickId, dd, keys);
        }
        else if (IsFullyClipped(this._quads._b2ClipQuad, parentClip, parentOverflowMode, dd) == false)
        {
          GetElementQuadVerts(verts, parentClip, defaultPixel, pickId, dd, keys);
        }

        _contentChanged = false;

      }
    }
    private void ComputeContentWH(vec4 pmarb, List<UiLine> spanLines, UiDebug dd)
    {
      var ori = Style._props.LayoutOrientation;

      //Calculate content size
      if (ori == UiLayoutOrientation.Horizontal)
      {
        float total = pmarb.top + pmarb.bot;
        foreach (var line in spanLines)
        {
          total += line.Height(ori);
          _quads.ContentWH.x = Math.Max(_quads.ContentWH.x, line.Width(ori) + pmarb.right + pmarb.left);
        }
        _quads.ContentWH.y = Math.Max(_quads.ContentWH.y, total);
      }
      else if (ori == UiLayoutOrientation.Vertical)
      {
        float total = pmarb.left + pmarb.right;
        foreach (var line in spanLines)
        {
          total += line.Width(ori);
          _quads.ContentWH.y = Math.Max(_quads.ContentWH.y, line.Height(ori) + pmarb.left + pmarb.right);
        }
        _quads.ContentWH.x = Math.Max(_quads.ContentWH.x, total);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

    }
    private void AlignElements(List<UiLine> spanLines)
    {
      //layout is to the left, align left, center, right
      var ori = this.Style._props.LayoutOrientation;

      if (this.Text == "170.0")
      {
        Gu.Trap();
      }

      foreach (var line in spanLines)
      {
        var col_l = line._cols[(int)UiAlignment.Left];
        var col_c = line._cols[(int)UiAlignment.Center];
        var col_r = line._cols[(int)UiAlignment.Right];

        foreach (var ele in col_c._eles)
        {
          ele._quads._b2LocalQuad.LMin(ori, ele._quads._b2LocalQuad.LMin(ori) + _quads._b2LocalQuad.LSize(ori) / 2 - col_c.LSize(ori) / 2);
        }
        foreach (var ele in col_r._eles)
        {
          if (Style._props.LayoutDirection == UiLayoutDirection.Right)
          {
            //roman
            ele._quads._b2LocalQuad.LMin(ori, _quads._b2LocalQuad.LSize(ori) - col_r.LSize(ori) + ele._quads._b2LocalQuad.LMin(ori));
          }
          else if (Style._props.LayoutDirection == UiLayoutDirection.Left)
          {
            //arabic
            ele._quads._b2LocalQuad.LMin(ori, _quads._b2LocalQuad.LSize(ori) - ele._quads._b2LocalQuad.LMin(ori) - ele._quads._b2LocalQuad.LSize(ori));
          }
          else
          {
            Gu.BRThrowNotImplementedException();
          }
        }

      }
    }
    private void FixExpanders(List<UiElement> expanders, UiDebug dd)
    {
      foreach (var ele in expanders)
      {
        //parent=shrink, child=grow (race condition), set child to min content
        //the cur w/h is the MINIMUM w/h and must be respected.
        var erw = ele.ExpandRaceW();
        var erh = ele.ExpandRaceH();

        if (erw)
        {
          ele._quads._b2LocalQuad._width = Math.Max(_quads._b2BorderQuad._width - ele._quads._b2LocalQuad._left, ele._quads._b2LocalQuad._width);
        }
        if (erh)
        {
          ele._quads._b2LocalQuad._height = Math.Max(_quads._b2BorderQuad._height - ele._quads._b2LocalQuad._top, ele._quads._b2LocalQuad._height);
        }
      }
    }
    private void SizeElement(vec2 contentWH, vec2 innerMaxWH, UiDebug dd, List<UiElement> parentexpanders)
    {
      //Compute final width/h
      //Compute content minimum width/height of static element to compute size of parent
      //Size is preliminary and static elements will be shortened up to their content size if they go outside parent boundary
      //conttnetwh is min wh 
      var epad = GetPadding(dd);

      if (ExpandRaceW() || ExpandRaceH())
      {
        parentexpanders.Add(this);
      }

      if (ExpandRaceW())//|| Style._props.SizeModeWidth == UiSizeMode.Average
      {
        //set expander race condition to contentwh
        _quads._b2LocalQuad._width = contentWH.width;
      }
      else if (Style._props.SizeModeWidth == UiSizeMode.Percent)
      {
        //% of parent w/h
        _quads._b2LocalQuad._width = Math.Max(innerMaxWH.width * (Style._props.PercentWidth * 0.01f), contentWH.width);
      }
      else if (Style._props.SizeModeWidth == UiSizeMode.Shrink)
      {
        //shrnk to size of contents (min size), child will be set to min of parent content in the layout
        _quads._b2LocalQuad._width = contentWH.width;
      }
      else if (Style._props.SizeModeWidth == UiSizeMode.Fixed)
      {
        _quads._b2LocalQuad._width = Style._props.FixedWidth;
      }

      if (ExpandRaceH())
      {
        _quads._b2LocalQuad._height = contentWH.height;
      }
      else if (Style._props.SizeModeHeight == UiSizeMode.Percent)
      {
        _quads._b2LocalQuad._height = Math.Max(innerMaxWH.height * (Style._props.PercentHeight * 0.01f), contentWH.height);
      }
      else if (Style._props.SizeModeHeight == UiSizeMode.Shrink)
      {
        _quads._b2LocalQuad._height = contentWH.height;
      }
      else if (Style._props.SizeModeHeight == UiSizeMode.Fixed)
      {
        _quads._b2LocalQuad._height = Style._props.FixedHeight;
      }

      //for static elements maxw/h are the penultimate parameters and you cant go past them even if clipping happens
      _quads._b2LocalQuad._width = Math.Clamp(_quads._b2LocalQuad._width, MinWidthE(), MaxWidthE());
      _quads._b2LocalQuad._height = Math.Clamp(_quads._b2LocalQuad._height, MinHeightE(), MaxHeightE());

      _quads._b2LocalQuad.Validate();
    }
    private bool ExpandRaceW()
    {
      //race condtion: parent=shrink, child=expand
      return (Style._props.SizeModeWidth == UiSizeMode.Percent && Parent != null && Parent.Style._props.SizeModeWidth == UiSizeMode.Shrink);
    }
    private bool ExpandRaceH()
    {
      return (Style._props.SizeModeHeight == UiSizeMode.Percent && Parent != null && Parent.Style._props.SizeModeHeight == UiSizeMode.Shrink);
    }
    protected float MinWidthE() { return EffectiveMinMax(Style._props.SizeModeWidth, true, true); }
    protected float MinHeightE() { return EffectiveMinMax(Style._props.SizeModeHeight, false, true); }
    protected float MaxWidthE() { return EffectiveMinMax(Style._props.SizeModeWidth, true, false); }
    protected float MaxHeightE() { return EffectiveMinMax(Style._props.SizeModeHeight, false, false); }
    private float EffectiveMinMax(UiSizeMode m, bool w, bool i)
    {
      //effective min/max based on sizing mode.
      if (w == true)
      {
        if (m == UiSizeMode.Fixed)
        {
          return Style._props.FixedWidth;
        }
        else if (i)
        {
          return Style._props.MinWidth;
        }
        else
        {
          return Style._props.MaxWidth;
        }
      }
      else
      {
        if (m == UiSizeMode.Fixed)
        {
          return Style._props.FixedHeight;
        }
        else if (i)
        {
          return Style._props.MinHeight;
        }
        else
        {
          return Style._props.MaxHeight;
        }
      }
    }
    private void LayoutStaticElement(UiElement ele, UiAlignment align, List<UiLine> vecLines, vec2 pmaxInnerWH, vec2 pcontentWH, UiDebug dd, ref int lineidx)
    {
      //compute static element left/top
      if (vecLines.Count == 0)
      {
        Gu.BRThrowException("GUI error - tried to run calc algorithm without any UILines created");
      }

      LayoutBlock(ele, align, vecLines, pmaxInnerWH, pcontentWH, dd, ref lineidx);
    }
    private void LayoutBlock(UiBlock ele, UiAlignment align, List<UiLine> vecLines, vec2 pmaxInnerWH, vec2 pcontentWH, UiDebug dd, ref int lineidx)
    {
      //TODO: remove dupe code using LMin/LSize
      if (Style._props.LayoutOrientation == UiLayoutOrientation.Horizontal)
      {
        LayoutBlockH(ele, align, vecLines, pmaxInnerWH, pcontentWH, dd, ref lineidx);
      }
      else if (Style._props.LayoutOrientation == UiLayoutOrientation.Vertical)
      {
        LayoutBlockV(ele, align, vecLines, pmaxInnerWH, pcontentWH, dd, ref lineidx);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    private void LayoutBlockH(UiBlock ele, UiAlignment align, List<UiLine> vecLines, vec2 pmaxInnerWH, vec2 pcontentWH, UiDebug dd, ref int lineidx)
    {
      //lay out elements
      var ori = UiLayoutOrientation.Horizontal;
      UiLine line = vecLines[lineidx];
      UiAlignCol col = line._cols[(int)align];
      float e_width = ele._quads._b2LocalQuad._width;
      float e_height = ele._quads._b2LocalQuad._height;
      var e_pad = ele.GetPadding(dd);

      if (CheckLineBreak(ele, ele.WordWidth, line.Width(ori), pmaxInnerWH.x, e_width, e_pad.left, e_pad.right))
      {
        if (lineidx + 1 >= vecLines.Count)
        {
          var line2 = new UiLine(0, line._top + line.Height(ori));
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
    private void LayoutBlockV(UiBlock ele, UiAlignment align, List<UiLine> vecLines, vec2 pmaxInnerWH, vec2 pcontentWH, UiDebug dd, ref int lineidx)
    {
      var ori = UiLayoutOrientation.Vertical;
      UiLine line = vecLines[lineidx];
      UiAlignCol col = line._cols[(int)align];
      float e_width = ele._quads._b2LocalQuad._width;
      float e_height = ele._quads._b2LocalQuad._height;
      var e_pad = ele.GetPadding(dd);

      if (CheckLineBreak(ele, ele.WordHeight, line.Height(ori), pmaxInnerWH.y, e_height, e_pad.top, e_pad.bot))
      {
        if (lineidx + 1 >= vecLines.Count)
        {
          var line2 = new UiLine(line._left + line.Width(ori), 0);
          vecLines.Add(line2);
        }

        lineidx++;
        line = vecLines[lineidx];
        col = line._cols[(int)align];
      }

      var pmarb = this.GetMarginAndBorder(dd);
      float col_adv = 0;
      if (align == UiAlignment.Left) // Top
      {
        col_adv = e_pad.top + pmarb.top;
      }
      else if (align == UiAlignment.Right) // Bot
      {
        col_adv = e_pad.bot + pmarb.bot;
      }
      else if (align == UiAlignment.Center)
      {
        col_adv = e_pad.top + e_pad.bot;
      }
      else { Gu.BRThrowNotImplementedException(); }

      ele._quads._b2LocalQuad._top = line._top + col._height + col_adv + ele.Top;//BR:20221021-left/top are render offsets in Static mode
      ele._quads._b2LocalQuad._left = line._left + e_pad.left + ele.Left + pmarb.left;
      col._height += ele.Top + e_height + e_pad.top + e_pad.bot;
      col._width = Math.Max(col._width, ele.Left + e_width + e_pad.left + e_pad.right);
      col._eles.Add(ele);

      ele._quads._b2LocalQuad.Validate();
    }
    private bool CheckLineBreak(UiBlock ele, float word_size, float l_width, float space, float e_size, float e_pad_a, float e_pad_b)
    {
      //ele, ele.WordWidth, line.Width(ori), pspacex, e_width, e_pad.left , e_pad.right; 
      bool bLineBreak = false;
      if (ele.DisplayMode == UiDisplayMode.Inline || ele.DisplayMode == UiDisplayMode.Word)
      {
        float wordwidth = 0;
        if (ele.DisplayMode == UiDisplayMode.Word)
        {
          wordwidth = word_size;
        }
        else
        {
          wordwidth = e_size + e_pad_a + e_pad_b;
        }

        if (wordwidth + l_width > space)
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
      return bLineBreak;
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
    protected void ComputeQuads(UiDebug dd)
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

      this._quads._b2LocalQuad.Validate();
      this._quads._b2BorderQuad.Validate();
    }
    private void GetElementQuadVerts(SortedList<float, v_v4v4v4v2u2v4v4> all_verts, UiQuad b2ClipRect, MtTex defaultPixel, uint rootPickId, UiDebug dd, SortKeys keys)
    {
      //**Texture Adjust - modulating repeated textures causes seaming issues, especially with texture filtering
      // adjust the texture coordinates by some pixels to account for that.  0.5f seems to work well.
      float adjust = 0;// 1.4f;  // # of pixels to adjust texture by

      //Debug overlay
      if (dd.ShowDebug)
      {
        DebugVert(_quads._dbg_b2ContentQuad, b2ClipRect, this._debugcolor, all_verts, keys, dd, defaultPixel, rootPickId);
        DebugVert(_quads._dbg_b2PaddingQuad, b2ClipRect, this._debugcolor + 0.02f, all_verts, keys, dd, defaultPixel, rootPickId);
      }
      if (Style._props.RenderMode != UiRenderMode.None)
      {
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
        all_verts.Add(keys._ekey, vc);

        //TODO: border radius is still broken 
        DoBorders(bd, radius, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, false, keys);
      }
    }
    private void GetGlyphQuadVerts(UiGlyph glyph, SortedList<float, v_v4v4v4v2u2v4v4> verts, UiDebug dd, MtTex defaultPixel, uint pickId, SortKeys keys)
    {
      if (dd.ShowDebug)
      {
        DebugVert(glyph._quads._b2BorderQuad, _quads._b2ClipQuad, glyph._debugcolor, verts, keys, dd, defaultPixel, pickId);
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
      verts.Add(keys._gkey, vc);
    }
    private void DoBorders(vec4 bd, vec4 radius, SortedList<float, v_v4v4v4v2u2v4v4> all_verts,
                           UiQuad b2ClipRect, MtTex defaultPixel, uint rootPickId, UiDebug dd, bool usequad, SortKeys keys)
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
        BorderVert(bt, Style._props.BorderColorTop, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(0, 1, usequad ? 0 : 999), keys);
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
        BorderVert(bt, Style._props.BorderColorRight, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(1, 0, usequad ? 0 : 999), keys);
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
        BorderVert(bt, Style._props.BorderColorBot, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(0, -1, usequad ? 0 : 999), keys);
      }
      if (bd.left > 0)
      {
        UiQuad bt = _quads._b2BorderQuad.Clone();
        if (!usequad)
        {
          bt._width = _quads._dbg_b2ContentQuad._left - _quads._b2BorderQuad._left;
        }
        var r2 = usequad ? new vec4(radius.x, 0, 0, radius.w) : radius;
        BorderVert(bt, Style._props.BorderColorLeft, r2, all_verts, b2ClipRect, defaultPixel, rootPickId, dd, new vec3(-1, 0, usequad ? 0 : 999), keys);
      }
    }
    private void BorderVert(UiQuad borderquad, vec4 bodfercolor, vec4 radius, SortedList<float, v_v4v4v4v2u2v4v4> all_verts,
                          UiQuad b2ClipRect, MtTex defaultPixel, uint rootPickId, UiDebug dd, vec3 quadrant, SortKeys keys)
    {
      v_v4v4v4v2u2v4v4 vb = new v_v4v4v4v2u2v4v4();
      vb._rtl_rtr = new vec4(radius.top, radius.right);
      vb._rbr_rbl = new vec4(radius.bot, radius.left);
      vb._quadrant = quadrant;
      SetVertexRasterArea(ref vb, in borderquad, in b2ClipRect, dd);
      ComputeVertexTexcoord(ref vb, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
      SetVertexPickAndColor(ref vb, new vec4(bodfercolor.xyz, bodfercolor.w * (float)Style._props.Opacity), rootPickId);
      all_verts.Add(keys._bdkey, vb);
    }
    private void DebugVert(UiQuad quad, UiQuad clip, vec4 c, SortedList<float, v_v4v4v4v2u2v4v4> all_verts, SortKeys keys, UiDebug dd, MtTex defaultPixel, uint pick)
    {
      v_v4v4v4v2u2v4v4 dbgv = new v_v4v4v4v2u2v4v4();
      SetVertexRasterArea(ref dbgv, in _quads._dbg_b2ContentQuad, in clip, dd);
      dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
      dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
      dbgv._quadrant = new vec3(0, 0, 999);
      ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
      SetVertexPickAndColor(ref dbgv, c, pick);
      all_verts.Add(keys._dbgkey, dbgv);
    }
    private void ComputeVertexTexcoord(ref v_v4v4v4v2u2v4v4 vc, MtTex pTex, UiImageTiling xtile, UiImageTiling ytile, float adjust)
    {
      Gu.Assert(pTex != null);
      ComputeTCoord(ref vc, pTex.uv0, pTex.uv1, pTex.GetWidth(), pTex.GetHeight(), pTex.GetSizeRatio(), xtile, ytile, Style._props.ImageScaleX, Style._props.ImageScaleY, adjust);
    }
    private void ComputeVertexGlyphTCoord(ref v_v4v4v4v2u2v4v4 vc, MtCachedCharData? glyph, float adjust)
    {
      Gu.Assert(glyph != null);
      ComputeTCoord(ref vc, glyph.uv0, glyph.uv1, glyph.patchTexture_Width, glyph.patchTexture_Height, 1, UiImageTiling.Expand, UiImageTiling.Expand, 1, 1, adjust);
    }
    private static void ComputeTCoord(ref v_v4v4v4v2u2v4v4 vc, vec2 uv0, vec2 uv1, float texw, float texh, float sizeRatio,
        UiImageTiling xtile, UiImageTiling ytile, float tilex, float tiley, float adjust)
    {

      if (xtile == UiImageTiling.Expand)
      {
        vc._tex.x = uv0.x;
        vc._tex.z = uv1.x;
      }
      else if (xtile == UiImageTiling.Tile)
      {
        vc._tex.x = uv0.x;
        vc._tex.z = uv1.x + (uv1.x - uv0.x) * tilex;
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
        vc._tex.y = uv0.y;
        vc._tex.w = uv1.y;
      }
      else if (ytile == UiImageTiling.Tile)
      {
        vc._tex.y = uv0.y;
        vc._tex.w = uv1.y + (uv1.y - uv0.y) * tiley;
      }
      else if (ytile == UiImageTiling.Computed)
      {
        // Tex coords are computed (used for UiGlyph)
      }
      else if (ytile == UiImageTiling.Proportion)
      {
        // proportion the Y to the X
        vc._tex.y = uv1.y;
        vc._tex.w = vc._tex.y + (vc._tex.z - vc._tex.x) * sizeRatio;
      }
      else
      {
        Gu.Log.Error("Invalid layout size mode.");
      }

      vc._texsiz.x = Math.Abs(uv1.x - uv0.x);
      vc._texsiz.y = Math.Abs(uv1.y - uv0.y);  // Uv0 - uv1 - because we flipped coords bove

      float w1px = 0;                  // 1 pixel subtract from the u/v to prevent creases during texture modulation
      float h1px = 0;

      if (texw > 0 && vc._texsiz.x > 0)
      {
        w1px = 1.0f / texw;
        w1px *= vc._texsiz.x;
        w1px *= adjust;
      }
      if (texh > 0 && vc._texsiz.y > 0)
      {
        h1px = 1.0f / texh;
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
    private static void SetVertexRasterArea(ref v_v4v4v4v2u2v4v4 vc, in UiQuad rasterQuad, in UiQuad b2ClipRect, UiDebug dd)
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

      //TODO: optimize, lineheight is literally the onlyt hing prevent this from being cached. doing it every label ..
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
        gc._wordHeight = 0;

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
        wordstart._wordHeight += gc._top_off + gc._quads._b2LocalQuad._height + gc._padding.top + gc._padding.bot;

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
    private Dictionary<string, UiStyle> Styles = new Dictionary<string, UiStyle>();
    private List<string> _errors = new List<string>();
    public string Name { get; private set; } = Lib.UnsetName;

    public UiStyleSheet(string name)
    {
      Name = name + "-ss";
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

    public static string Paragraph(string s) { return $"<p>{s}</p>"; }

    public RenderView RenderView { get; private set; }
    public UiDebug DebugDraw { get; set; } = new UiDebug();
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
    private UiAsyncUpdateState _state = UiAsyncUpdateState.CanUpdate;
    private UiEventThing _eventThing = new UiEventThing();
    private Dictionary<uint, UiElement> _pickable = new Dictionary<uint, UiElement>();
    private bool _async = false;
    private Dictionary<UiElement, Dictionary<UiPropName, IUiPropAnimation>>? _animations = null;
    private int _async_framestamp = 0;
    private SortedList<float, v_v4v4v4v2u2v4v4> _async_verts = new SortedList<float, v_v4v4v4v2u2v4v4>(new FloatSort());

    #endregion
    #region Public: Methods

    public Gui2d(Gui2dShared shared, RenderView cam)
    {
      StyleSheet = new UiStyleSheet("ui-default");
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
    private void UpdateLayout_Async(MegaTex mt, PCMouse mouse, RenderView rv, ref Dictionary<uint, UiElement>? pickable)
    {
      //pass 1 compute minimum sizes for children,  child relative positions, relative clip quads
      //pass 2 compute absolute positions elements, compute absolute quads.
      //for now - the layout changed thing does not work, partially due to async, (but the async is actually faster than that anyway).

      _async_verts.Clear();
      bool force = true;
      _async_framestamp++;
      ComputeQuads(DebugDraw);
      PerformLayout_SizeElements(mt, force, new vec2(MaxWidthE(), MaxHeightE()), null, StyleSheet, _async_framestamp, DebugDraw, new List<UiElement>());
      PerformLayout_PositionElements(force, DebugDraw, _async_verts, this._quads._b2ClipQuad, mt.DefaultPixel, _iPickId, ref pickable, 1);
    }
    private void SendMeshToGpu_Sync(RenderView rv)
    {
      //RegenMesh
      var vts = _async_verts.Values.ToArray();

      if (Mesh == null)
      {
        Mesh = new MeshData(rv.Name + "gui-mesh", OpenTK.Graphics.OpenGL4.PrimitiveType.Points,
        Gpu.CreateVertexBuffer(rv.Name + "gui-mesh", vts), null, false);
      }
      else
      {
        Gu.Assert(Mesh != null);
        Gu.Assert(Mesh.VertexBuffers != null);
        Gu.Assert(Mesh.VertexBuffers.Count == 1);

        var dat = GpuDataPtr.GetGpuDataPtr(vts);
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

        _sort = 1;
        //_layer = 1;

        //We are probably getting rid of width height
        Style.Top = rv.Viewport.Y;
        Style.Left = rv.Viewport.X;
        Style.FixedWidth = rv.Viewport.Width;
        Style.FixedHeight = rv.Viewport.Height;
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
