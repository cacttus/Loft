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
    public class PCKeyboard
    {
        public static Dictionary<OpenTK.Input.Key, PCButton> _keys = new Dictionary<OpenTK.Input.Key, PCButton>();
        public static OpenTK.Input.KeyboardState _keyboardState;

        public void Update()
        {
            _keyboardState = OpenTK.Input.Keyboard.GetState();
            foreach (var pair in _keys)
            {
                pair.Value.UpdateState(_keyboardState.IsKeyDown(pair.Key));
            }
        }
        public bool KeyPressOrDown(OpenTK.Input.Key key)
        {
            var state = GetState(key);
            return (state == PCButtonState.Press) || (state == PCButtonState.Hold);
        }
        public bool KeyPress(OpenTK.Input.Key key)
        {
            var state = GetState(key);
            return state == PCButtonState.Press;
        }
        public bool AnyKeysPressedOrHeld(List<OpenTK.Input.Key> keys)
        {
            foreach(var key in keys)
            {
                var state = GetState(key);
                if ((state == PCButtonState.Press) || (state == PCButtonState.Hold))
                {
                    return true;
                }
            }
            return false;
        }
        public bool AllKeysPressedOrHeld(List<OpenTK.Input.Key> keys)
        {
            foreach (var key in keys)
            {
                var state = GetState(key);
                if ((state != PCButtonState.Press) && (state != PCButtonState.Hold))
                {
                    return false;
                }
            }
            return true;
        }
        private PCButtonState GetState(OpenTK.Input.Key key)
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
                but = new PCButton(_keyboardState.IsKeyDown(key));
                ret = but.State;
                _keys.Add(key, but);
            }
            return ret;
        }
    }

}
