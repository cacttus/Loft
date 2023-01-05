using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Loft
{
  public enum ButtonState
  {
    Up, Press, Hold, Release, Any
  }
  public enum KeyMod
  {
    None, Any, Shift, Ctrl, Alt, CtrlShift, CtrlAlt, AltShift, CtrlAltShift,
  }

  public class PCButton
  {
    public ButtonState State { get; private set; } = ButtonState.Up;
    public ButtonState LastState { get; private set; } = ButtonState.Up;

    public PCButton(bool state)
    {
      UpdateState(state);
    }
    public bool UpdateState(bool down)
    {
      //return true if the state has changed
      LastState = State;
      if (down)
      {
        if (State == ButtonState.Up)
        {
          State = ButtonState.Press;
        }
        else if (State == ButtonState.Press)
        {
          State = ButtonState.Hold;
        }
        else if (State == ButtonState.Hold)
        {
        }
        else if (State == ButtonState.Release)
        {
          State = ButtonState.Press;
        }
      }
      else
      {
        if (State == ButtonState.Up)
        {
        }
        else if (State == ButtonState.Press)
        {
          State = ButtonState.Release;
        }
        else if (State == ButtonState.Hold)
        {
          State = ButtonState.Release;
        }
        else if (State == ButtonState.Release)
        {
          State = ButtonState.Up;
        }
      }
      return LastState != State;
    }
  }
  public class InputEventListener
  {
    public virtual void OnKey(KeyboardKeyEvent key) { }
    public virtual void OnMouseButton(MouseButton button) { }
  }
  public class KeyboardKeyEvent
  {
    public bool Ctrl;
    public bool Alt;
    public bool Shift;
    public ButtonState State;
    public Keys Key;
    public Char Char;
    public KeyboardKeyEvent(bool ctrl, bool alt, bool shift, Keys key, ButtonState state)
    {
      ctrl = Ctrl;
      Alt = alt;
      Shift = shift;
      Key = key;
      State = state;
    }
  }
  public abstract class ButtonInputDevice<TStateClass, TButtonClass> where TStateClass : class
  {
    #region Members

    public bool IsInitialized { get { return _deviceState != null; } }

    protected Dictionary<TButtonClass, PCButton> _keys = new Dictionary<TButtonClass, PCButton>();
    protected TStateClass? _deviceState = null;
    protected abstract TStateClass GetDeviceState();
    protected abstract bool GetDeviceButtonDown(TButtonClass button);
    protected abstract void UpdateInternalStateAfterPoll();
    protected abstract void SendButtonEvent(KeyValuePair<TButtonClass, PCButton> button, InputEventListener lis);

    private List<WeakReference<InputEventListener>> _listeners = null;
    private List<KeyValuePair<TButtonClass, PCButton>> _changed = new List<KeyValuePair<TButtonClass, PCButton>>();

    #endregion
    #region Public Methods
    public ButtonInputDevice()
    {
      _deviceState = GetDeviceState();
    }
    public void AddListener(InputEventListener listener)
    {
      _listeners = _listeners.ConstructIfNeeded();
      _listeners.Add(new WeakReference<InputEventListener>(listener));
    }
    public void RemoveListener(InputEventListener listener)
    {
      if (_listeners != null)
      {
        for (var i = _listeners.Count - 1; i >= 0; i--)
        {
          if (_listeners[i].TryGetTarget(out var lis))
          {
            if (lis == listener)
            {
              _listeners.RemoveAt(i);
            }
          }
          else
          {
            _listeners.RemoveAt(i);
          }
        }
      }
    }
    public virtual void Update()
    {
      _deviceState = GetDeviceState();
      _changed.Clear();

      //poll
      foreach (var pair in _keys)
      {
        if (pair.Value.UpdateState(GetButtonDownWindowFocus(pair.Key)))
        {
          _changed.Add(pair);
        }
      }

      UpdateInternalStateAfterPoll();

      //update listeners
      if (_listeners != null)
      {
        foreach (var c in _changed)
        {
          for (int i = _listeners.Count - 1; i >= 0; i--)
          {
            if (_listeners[i].TryGetTarget(out var lis))
            {
              SendButtonEvent(c, lis);
            }
            else
            {
              _listeners.RemoveAt(i);
            }
          }
        }
      }
    }

    #endregion
    #region Private Methhods

    private bool GetButtonDownWindowFocus(TButtonClass key)
    {
      Gu.Assert(IsInitialized);

      //simple hack to release all buttons when the window loses focus.
      bool isButtonDown = false;
      if (!Gu.Context.GameWindow.IsFocused && Gu.EngineConfig.ReleaseAllButtonsWhenWindowLosesFocus)
      {
        isButtonDown = false;
      }
      else
      {
        isButtonDown = GetDeviceButtonDown(key);
      }
      return isButtonDown;
    }
    public bool Press(TButtonClass key)
    {
      var state = State(key);
      return state == ButtonState.Press;
    }
    public bool PressOrDown(TButtonClass key)
    {
      var state = State(key);
      return (state == ButtonState.Press) || (state == ButtonState.Hold);
    }
    public bool PressOrDown(List<TButtonClass> keys)
    {
      foreach (var key in keys)
      {
        var state = State(key);
        if ((state == ButtonState.Press) || (state == ButtonState.Hold))
        {
          return true;
        }
      }
      return false;
    }
    public bool HasState(TButtonClass key, ButtonState check)
    {
      return State(key) == check;
    }
    public ButtonState State(TButtonClass key)
    {
      //Register the key and/or return its value
      ButtonState ret = ButtonState.Up;
      PCButton but = null;
      if (_keys.TryGetValue(key, out but))
      {
        if (but != null)
        {
          ret = but.State;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
      }
      else
      {
        //Key isn't registered
        but = new PCButton(GetButtonDownWindowFocus(key));
        ret = but.State;
        _keys.Add(key, but);
      }
      return ret;
    }

    #endregion

  }//cls
  public class PCKeyboard : ButtonInputDevice<KeyboardState, Keys>
  {
    private bool _ctrlDown = false; //current state of ctrl/alt/shift
    private bool _shiftDown = false;
    private bool _altDown = false;

    public PCKeyboard() : base()
    {
      //regsiter all keys 
      foreach (var k in Enum.GetValues(typeof(Keys)))
      {
        if ((Keys)k != Keys.Unknown)
        {
          State((Keys)k);
        }
      }
    }

    public static bool IsPrintableChar(Keys k)
    {
      //TODO: language...
      if (k == Keys.Space || k == Keys.Apostrophe || k == Keys.Comma || k == Keys.Minus || k == Keys.Period || k == Keys.Slash
      || k == Keys.D0 || k == Keys.D1 || k == Keys.D2 || k == Keys.D3 || k == Keys.D4 || k == Keys.D5 || k == Keys.D6 || k == Keys.D7 || k == Keys.D8 || k == Keys.D9
      || k == Keys.Semicolon || k == Keys.Equal
      || k == Keys.A || k == Keys.B || k == Keys.C || k == Keys.D || k == Keys.E || k == Keys.F || k == Keys.G || k == Keys.H || k == Keys.I || k == Keys.J || k == Keys.K
      || k == Keys.L || k == Keys.M || k == Keys.N || k == Keys.O || k == Keys.P || k == Keys.Q || k == Keys.R || k == Keys.S || k == Keys.T || k == Keys.U || k == Keys.V || k == Keys.W
      || k == Keys.X || k == Keys.Y || k == Keys.Z
      || k == Keys.LeftBracket || k == Keys.Backslash || k == Keys.RightBracket || k == Keys.GraveAccent
      )
      {
        return true;
      }
      return false;
    }

    public static Keys IntToDigitKey(int n)
    {
      if (n < 0 || n > 9)
      {
        Gu.Log.Error($"digit key {n} invalid");
        Gu.DebugBreak();//Error
      }
      return (Keys)((int)Keys.D0 + n);
    }

    protected override void UpdateInternalStateAfterPoll()
    {
      _ctrlDown = (PressOrDown(Keys.LeftControl) || PressOrDown(Keys.RightControl));
      _shiftDown = (PressOrDown(Keys.LeftShift) || PressOrDown(Keys.RightShift));
      _altDown = (PressOrDown(Keys.LeftAlt) || PressOrDown(Keys.RightAlt));
    }
    protected override void SendButtonEvent(KeyValuePair<Keys, PCButton> button, InputEventListener lis)
    {
      lis.OnKey(new KeyboardKeyEvent(_ctrlDown, _altDown, _shiftDown, button.Key, button.Value.State));
    }
    public bool ModIsDown(KeyMod Mod)
    {
      bool mod = true;
      if (Mod == KeyMod.Any)
      {
        return true;
      }
      bool cd = (PressOrDown(Keys.LeftControl) || PressOrDown(Keys.RightControl));
      bool sd = (PressOrDown(Keys.LeftShift) || PressOrDown(Keys.RightShift));
      bool ad = (PressOrDown(Keys.LeftAlt) || PressOrDown(Keys.RightAlt));
      bool mc = (Mod == KeyMod.Ctrl || Mod == KeyMod.CtrlShift || Mod == KeyMod.CtrlAlt || Mod == KeyMod.CtrlAltShift);
      bool ms = (Mod == KeyMod.Shift || Mod == KeyMod.CtrlShift || Mod == KeyMod.AltShift || Mod == KeyMod.CtrlAltShift);
      bool ma = (Mod == KeyMod.Alt || Mod == KeyMod.CtrlAlt || Mod == KeyMod.AltShift || Mod == KeyMod.CtrlAltShift);
      mod = mod && ((mc && cd) || (!mc && !cd));
      mod = mod && ((ms && sd) || (!ms && !sd));
      mod = mod && ((ma && ad) || (!ma && !ad));
      return mod;
    }
    protected override KeyboardState GetDeviceState()
    {
      return Gu.Context.GameWindow.KeyboardState;
    }
    protected override bool GetDeviceButtonDown(Keys button)
    {
      return _deviceState.IsKeyDown(button);
    }
    public bool AnyNonModKeyWasPressed(List<Keys>? keysOut = null)
    {
      foreach (var k in Enum.GetValues(typeof(Keys)))
      {
        var key = (Keys)k;
        if (key != Keys.Unknown)
        {
          if (!(
            key == Keys.LeftShift ||
            key == Keys.RightShift ||
            key == Keys.LeftControl ||
            key == Keys.RightControl
            ))
          {
            if (Press(key))
            {
              if (keysOut != null)
              {
                keysOut.Add(key);
              }
              else
              {
                return true;
              }
            }
          }
        }
      }
      return keysOut.Count > 0;
    }


  }

  public class PCMouse : ButtonInputDevice<MouseState, MouseButton>
  {
    public enum WarpMode
    {
      Center,
      Wrap,
      Clamp
    }
    protected override void UpdateInternalStateAfterPoll() { }
    protected override void SendButtonEvent(KeyValuePair<MouseButton, PCButton> button, InputEventListener lis) { }

    private long _warp_frame_stamp = 0;
    private vec2 _last = new vec2(0, 0);
    private vec2 _pos = new vec2(0, 0);

    public vec2 LastPos { get { return _last; } }
    public vec2 Pos { get { return _pos; } } //Position relative to Top Left corner of window client area (excluding borders and titlebar)
    public vec2 PosAbsolute
    {
      get
      {
        Gu.BRThrowNotImplementedException();
        return _pos;
      }
    } //Position on screen
    public vec2 PosDelta { get { return _pos - _last; } }
    public vec2 ScrollDelta
    {
      get
      {
        return new vec2(this._deviceState.ScrollDelta.X, this._deviceState.ScrollDelta.Y);
      }
    }
    public bool ShowCursor
    {
      get
      {
        return Gu.Context.GameWindow.CursorVisible;
      }
      set
      {
        Gu.Context.GameWindow.CursorVisible = value;
      }
    }
    public void UpdatePosition(vec2 v)
    {
      _last = _pos;
      _pos = v;
    }
    protected override MouseState GetDeviceState()
    {
      return Gu.Context.GameWindow.MouseState;
    }
    protected override bool GetDeviceButtonDown(MouseButton button)
    {
      return _deviceState.IsButtonDown(button);
    }
    private float Warpx_or_y(float ms_x_or_y, float vp_x_or_y, float vp_w_or_h, WarpMode warpMode, out float delta, float boundary = 0)//warp_boundary < [0,1]
    {
      //Warp X or Y
      float ret = ms_x_or_y;
      float ms_rel = ms_x_or_y - vp_x_or_y; //mpos - vp top left
      delta = 0;
      if (warpMode == WarpMode.Center)
      {
        if ((ms_rel <= vp_w_or_h * boundary) || (ms_rel >= vp_w_or_h - vp_w_or_h * boundary))
        {
          ret = vp_x_or_y + (int)(vp_w_or_h * 0.5f);
          delta = 0.5f;
        }
      }
      else if (warpMode == WarpMode.Clamp)
      {
        if (ms_rel <= vp_w_or_h * boundary)
        {
          ret = vp_x_or_y + vp_w_or_h * boundary;
          delta = -1;
        }
        else if (ms_rel >= vp_w_or_h - vp_w_or_h * boundary)
        {
          ret = vp_x_or_y + vp_w_or_h - vp_w_or_h * boundary;
          delta = 1;
        }
      }
      else if (warpMode == WarpMode.Wrap)
      {
        if (ms_rel <= vp_w_or_h * boundary)
        {
          ret = vp_x_or_y + vp_w_or_h - vp_w_or_h * boundary;
          delta = -1;
        }
        else if (ms_rel >= vp_w_or_h - vp_w_or_h * boundary)
        {
          ret = vp_x_or_y + vp_w_or_h * boundary;
          delta = 1;
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return ret;
    }
    public vec2? WarpMouse(RenderView vp, WarpMode warpMode, float boundary = 0, bool zeroDelta = true)
    {
      //@note Mouse is relative to top left of window
      //@note RenderView can be anywhere in the window
      //@param warpMode: Center - put mouse center of screen
      //                 Wrap - wrap around to other side of screen.
      //@param boundary [0,1]: the amount of padding from edge. Default is zero (exact edge of screen)
      //@param ZeroDelta: set delta to zero after warp, to avoid the system thinking the mouse warp is a valid user movement.
      //Returns the mouse's wrap -1 for negative x/y, +1 for positive x/y, 0.5 for center of screen, or, null if window is not focused
      Gu.Assert(vp != null);
      Gu.Assert(Gu.Context.GameWindow != null);

      if (Gu.Context.GameWindow.IsFocused)
      {
        vec2 ret = new vec2(0, 0);
        float px = Warpx_or_y(_pos.x, vp.Viewport.X, vp.Viewport.Width, warpMode, out ret.x, boundary);
        float py = Warpx_or_y(_pos.y, vp.Viewport.Y, vp.Viewport.Height, warpMode, out ret.y, boundary);

        if ((int)_pos.x != (float)px || (int)_pos.y != (float)py)
        {
          _pos.x = px;
          _pos.y = py;
          //Do not use TK.MousePosition after setting it. It does not update immediately.

          if (_warp_frame_stamp == Gu.Context.FrameStamp)
          {
            Gu.Log.Warn("Mouse warped multiple times in single frame. This may be a bug, and will definitely cause mouse 'wrap' feature to not work.");
            Gu.DebugBreak();
          }
          _warp_frame_stamp = Gu.Context.FrameStamp;

          Gu.Context.GameWindow.MousePosition = new OpenTK.Mathematics.Vector2i((int)_pos.x, (int)_pos.y);

          if (zeroDelta)
          {
            _last = _pos;
          }
        }

        return ret;
      }

      return null;
    }
    public vec2 GetWrappedPosition(RenderView vp, vec2 wrap_sum)
    {
      vec2 p_wrap = new vec2(
        Pos.x + vp.Viewport.Width * wrap_sum.x,
        Pos.y + vp.Viewport.Height * wrap_sum.y
      );
      return p_wrap;
    }

    public override void Update()
    {
      base.Update();
      _last = _pos;
      _pos.x = (float)_deviceState.X;
      _pos.y = (float)_deviceState.Y;
    }
  }

}
