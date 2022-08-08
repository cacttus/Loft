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
  public class PCMouse : ButtonInputDevice<OpenTK.Windowing.GraphicsLibraryFramework.MouseState, OpenTK.Windowing.GraphicsLibraryFramework.MouseButton>
  {
    private vec2 _last = new vec2(0, 0);
    private vec2 _pos = new vec2(0, 0);
    public vec2 Last { get { return _last; } }
    public vec2 Pos { get { return _pos; } } //Position relative to Top Left corner of window client area (excluding borders and titlebar)
    public vec2 PosAbsolute { get { 
      Gu.BRThrowNotImplementedException();
      return _pos; } } //Position on screen
    public vec2 Delta { get { return _pos - _last; } }

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
    public void WarpMouse(bool warpX, bool warpY, bool zeroDelta = true)
    {
      //Warp mouse to center of screen
      //ZeroDelta - if true then we set last to the current position to avoid re-rotating back for First person camera movement.
      var w = Gu.Context.GameWindow;
      if (w != null)
      {
        if (w.IsFocused)
        {
          var pt = new OpenTK.Mathematics.Vector2i((int)(w.Size.X / 2), (int)(w.Size.Y / 2));
          var pt_win = pt;// w.PointToScreen(pt);

          if (warpX)
          {
            w.MousePosition = new OpenTK.Mathematics.Vector2i(pt_win.X, (int)_pos.y);
            _pos.x = (float)pt_win.X;
          }
          if (warpY)
          {
            w.MousePosition = new OpenTK.Mathematics.Vector2i((int)_pos.x, pt_win.Y);
            _pos.y = (float)pt_win.Y;
          }
          if (warpX && warpY)
          {
            w.MousePosition = new OpenTK.Mathematics.Vector2i(pt_win.X, pt_win.Y);
            _pos.x = (float)pt_win.X;
            _pos.y = (float)pt_win.Y;
          }

          if (zeroDelta)
          {
            _last = _pos;
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
