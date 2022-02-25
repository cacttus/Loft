using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PirateCraft
{
   public enum PCButtonState
   {
      Up, Press, Hold, Release
   }
   public class PCButton
   {
      public PCButtonState State = PCButtonState.Up;
      //int _lastUpdateFrame = 0;
      public PCButton(bool state)
      {
         UpdateState(state);
      }
      public void UpdateState(bool down)
      {
         if (down)
         {
            if (State == PCButtonState.Up)
            {
               State = PCButtonState.Press;
            }
            else if (State == PCButtonState.Press)
            {
               State = PCButtonState.Hold;
            }
            else if (State == PCButtonState.Hold)
            {
            }
            else if (State == PCButtonState.Release)
            {
               State = PCButtonState.Press;
            }
         }
         else
         {
            if (State == PCButtonState.Up)
            {
            }
            else if (State == PCButtonState.Press)
            {
               State = PCButtonState.Release;
            }
            else if (State == PCButtonState.Hold)
            {
               State = PCButtonState.Release;
            }
            else if (State == PCButtonState.Release)
            {
               State = PCButtonState.Up;
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
         return state == PCButtonState.Press;
      }
      public bool PressOrDown(TButtonClass key)
      {
         var state = GetButtonState(key);
         return (state == PCButtonState.Press) || (state == PCButtonState.Hold);
      }
      public bool PressOrDown(List<TButtonClass> keys)
      {
         foreach (var key in keys)
         {
            var state = GetButtonState(key);
            if ((state == PCButtonState.Press) || (state == PCButtonState.Hold))
            {
               return true;
            }
         }
         return false;
      }
      private PCButtonState GetButtonState(TButtonClass key)
      {
         PCButtonState ret = PCButtonState.Up;
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

      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      static extern bool SetCursorPos(int x, int y);

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
               var pt_win = w.PointToScreen(pt);

               if (warpX)
               {
                  SetCursorPos(pt_win.X, (int)_pos.y);
                  _pos.x = (float)pt.X;
               }
               if (warpY)
               {
                  SetCursorPos((int)_pos.x, pt_win.Y);
                  _pos.y = (float)pt.Y;
               }
               if (warpX && warpY)
               {
                  SetCursorPos(pt_win.X, pt_win.Y);
                  _pos.x = (float)pt.X;
                  _pos.y = (float)pt.Y;
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
         _pos.x = (float)_deviceState.X;//
         _pos.y = (float)_deviceState.Y;//
      }
   }

}
