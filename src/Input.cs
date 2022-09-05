using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PirateCraft
{
  public enum ButtonState
  {
    Up, Press, Hold, Release
  }
  public class PCButton
  {
    public ButtonState State = ButtonState.Up;
    //int _lastUpdateFrame = 0;
    public PCButton(bool state)
    {
      UpdateState(state);
    }
    public void UpdateState(bool down)
    {
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
    }
  }
  public abstract class ButtonInputDevice<TStateClass, TButtonClass>
  {
    public Dictionary<TButtonClass, PCButton> _keys = new Dictionary<TButtonClass, PCButton>();
    public TStateClass _deviceState;

    public abstract TStateClass GetDeviceState();
    public abstract bool GetDeviceButtonDown(TButtonClass button);

    public virtual void Update()
    {
      _deviceState = GetDeviceState();
      foreach (var pair in _keys)
      {
        pair.Value.UpdateState(GetDeviceButtonDown(pair.Key));
      }
    }
    public bool Press(TButtonClass key)
    {
      var state = GetButtonState(key);
      return state == ButtonState.Press;
    }
    public bool PressOrDown(TButtonClass key)
    {
      var state = GetButtonState(key);
      return (state == ButtonState.Press) || (state == ButtonState.Hold);
    }
    public bool PressOrDown(List<TButtonClass> keys)
    {
      foreach (var key in keys)
      {
        var state = GetButtonState(key);
        if ((state == ButtonState.Press) || (state == ButtonState.Hold))
        {
          return true;
        }
      }
      return false;
    }
    public ButtonState GetButtonState(TButtonClass key)
    {
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
        but = new PCButton(GetDeviceButtonDown(key));
        ret = but.State;
        _keys.Add(key, but);
      }
      return ret;
    }
  }
  public class PCKeyboard : ButtonInputDevice<OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState, OpenTK.Windowing.GraphicsLibraryFramework.Keys>
  {
    public override OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState GetDeviceState()
    {
      return Gu.Context.GameWindow.KeyboardState;
    }
    public override bool GetDeviceButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys button)
    {
      return _deviceState.IsKeyDown(button);
    }
  }
  public enum WarpMode
  {
    Center,
    Wrap,
    Clamp
  }
  public class PCMouse : ButtonInputDevice<OpenTK.Windowing.GraphicsLibraryFramework.MouseState, OpenTK.Windowing.GraphicsLibraryFramework.MouseButton>
  {
    private vec2 _last = new vec2(0, 0);
    private vec2 _pos = new vec2(0, 0);
    public vec2 Last { get { return _last; } }
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
    public override OpenTK.Windowing.GraphicsLibraryFramework.MouseState GetDeviceState()
    {
      return Gu.Context.GameWindow.MouseState;
    }
    public override bool GetDeviceButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton button)
    {
      return _deviceState.IsButtonDown(button);
    }
    private float Warpx_or_y(float ms_x_or_y, float vp_x_or_y, float vp_w_or_h, WarpMode warpMode, float boundary = 0)//warp_boundary < [0,1]
    {
      //Warp X or Y
      float ret = ms_x_or_y;
      float ms_rel = ms_x_or_y - vp_x_or_y;
      if (warpMode == WarpMode.Center)
      {
        if ((ms_rel <= vp_w_or_h * boundary) || (ms_rel >= vp_w_or_h - vp_w_or_h * boundary))
        {
          ret = vp_x_or_y + (int)(vp_w_or_h * 0.5f);
        }
      }
      else if (warpMode == WarpMode.Clamp)
      {
        if (ms_rel <= vp_w_or_h * boundary)
        {
          ret = vp_x_or_y + vp_w_or_h * boundary;
        }
        else if (ms_rel >= vp_w_or_h - vp_w_or_h * boundary)
        {
          ret = vp_x_or_y + vp_w_or_h - vp_w_or_h * boundary;
        }
      }
      else if (warpMode == WarpMode.Wrap)
      {
        if (ms_rel <= vp_w_or_h * boundary)
        {
          ret = vp_x_or_y + vp_w_or_h - vp_w_or_h * boundary;
        }
        else if (ms_rel >= vp_w_or_h - vp_w_or_h * boundary)
        {
          ret = vp_x_or_y + vp_w_or_h * boundary;
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return ret;
    }
    long _stamp = 0;
    public void WarpMouse(RenderView vp, WarpMode warpMode, float boundary = 0, bool zeroDelta = true)
    {
      //@note Mouse is relative to top left of window
      //@note RenderView can be anywhere in the window
      //@param warpMode: Center - center of screen
      //           Wrap - wrap around to other side of screen.
      //@param boundary [0,1]: the amount of padding from edge. Default is zero (exact edge of screen)
      //@param ZeroDelta: set delta to zero after warp, to avoid the system thinking the mouse warp is a valid user movement.
      var w = Gu.Context.GameWindow;
      if (w != null)
      {
        if (w.IsFocused)
        {
          float px = Warpx_or_y(_pos.x, vp.Viewport.X, vp.Viewport.Width, warpMode, boundary);
          float py = Warpx_or_y(_pos.y, vp.Viewport.Y, vp.Viewport.Height, warpMode, boundary);

          if ((int)_pos.x != (float)px || (int)_pos.y != (float)py)
          {
            _pos.x = px;
            _pos.y = py;
            //Do not use TK.MousePosition after setting it. It does not update immediately.

            if (_stamp == Gu.Context.FrameStamp)
            {
              Gu.Log.Warn("Mouse warped multiple times in single frame. This may be a bug, and will definitely cause mouse 'wrap' feature to not work.");
              Gu.DebugBreak();
            }
            _stamp = Gu.Context.FrameStamp;

            w.MousePosition = new OpenTK.Mathematics.Vector2i((int)_pos.x, (int)_pos.y);

            if (zeroDelta)
            {
              _last = _pos;
            }
          }


        }
      }
      else
      {
        Gu.Log.Error("Mouse Input: Window was null...");
      }
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
