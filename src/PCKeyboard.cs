using System;
using System.Collections.Generic;

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

   //buttonclass = OPentk.input.key or opentk.input.mousebutton
   //stateclass = opentk.input.keyboardstate or opentk.input.mousestae
   public abstract class ButtonInputDevice<TStateClass, TButtonClass>
   {
      public Dictionary<TButtonClass, PCButton> _keys = new Dictionary<TButtonClass, PCButton>();
      public TStateClass _deviceState;

      public abstract TStateClass GetDeviceState();// OpenTK.Input.Keyboard.GetState();
      public abstract bool GetDeviceButtonDown(TButtonClass button); // _keyboardState.IsKeyDown(key)

      public virtual void Update()
      {
         _deviceState = GetDeviceState();// OpenTK.Input.Keyboard.GetState();
         foreach (var pair in _keys)
         {
            pair.Value.UpdateState(GetDeviceButtonDown(pair.Key));// _keyboardState.IsKeyDown(pair.Key));
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

   public class PCKeyboard : ButtonInputDevice<OpenTK.Input.KeyboardState, OpenTK.Input.Key>
   {
      public override OpenTK.Input.KeyboardState GetDeviceState()
      {
         return OpenTK.Input.Keyboard.GetState();
      }
      public override bool GetDeviceButtonDown(OpenTK.Input.Key button)
      {
         return _deviceState.IsKeyDown(button);
      }
   }
   public class PCMouse : ButtonInputDevice<OpenTK.Input.MouseState, OpenTK.Input.MouseButton>
   {
      private vec2 _last = new vec2(0, 0);
      private vec2 _pos = new vec2(0, 0);
      public vec2 Last { get { return _last; } }
      public vec2 Pos { get { return _pos; } } //Position relative to Top Left corner of window client area (excluding borders and titlebar)

      public bool CenterCursor { get; set; } = false;
      public bool ShowCursor
      {
         get
         {
            return Gu.CurrentWindowContext.GameWindow.CursorVisible;
         }
         set
         {
            Gu.CurrentWindowContext.GameWindow.CursorVisible = value;
         }
      }
      public void UpdatePosition(vec2 v)
      {
         _last = _pos;
         _pos = v;
      }
      public override OpenTK.Input.MouseState GetDeviceState()
      {
         return OpenTK.Input.Mouse.GetState();
      }
      public override bool GetDeviceButtonDown(OpenTK.Input.MouseButton button)
      {
         return _deviceState.IsButtonDown(button);
      }
      public override void Update()
      {
         base.Update();
         var w = Gu.CurrentWindowContext.GameWindow;
         if (w != null)
         {
            if (CenterCursor)
            {
               if (w.Focused)
               {
                  OpenTK.Input.Mouse.SetPosition(w.X + w.Width / 2, w.Y + w.Height / 2);
               }
            }
         }
         else
         {
            Gu.Log.Error("Mouse Input: Window was null...");
         }
         //OpenTK mouse - not working.
         _last = _pos;
         _pos.x = (float)_deviceState.X;
         _pos.y = (float)_deviceState.Y;
      }
   }

}
