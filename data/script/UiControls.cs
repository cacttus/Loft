using System;
using System.Collections;
using System.Collections.Generic;
using Loft;

namespace Loft
{
  using OpenTK.Windowing.GraphicsLibraryFramework;
  using UiAction = System.Action<UiEvent>;

  //this is going away.
  public enum UiStyleName
  {
    Inline,
    BaseControl,
    Label,
    DebugInfo,
    Panel,
    Button,
    Toolbar,
    StatusBar,
    ContextMenu,
    EditGuiRoot,
    VerticalBar,
    MenuItem,
    HBar,
    ToolbarButton,
  }
  public interface IUIScript
  {
    public string GetName();
    public void OnCreate(IGui2d g);
    public void OnUpdate(RenderView rv);
  }
  //******************************************************************************************
  public static class UiResources
  {
    //Resources needed to build Megatex
    public static FileLoc Icon_Light_AppbarClose = new FileLoc("light/appbar.close.png", EmbeddedFolder.Icon);
    public static FileLoc Icon_Dark_AppbarClose = new FileLoc("dark/appbar.close.png", EmbeddedFolder.Icon);
    public static FileLoc Icon_Dark_AppbarCrop = new FileLoc("dark/appbar.crop.png", EmbeddedFolder.Icon);
    public static List<FileLoc> GetResources()
    {
      return new List<FileLoc>()
      {
        FontFace.Parisienne
        //,FontFace.PressStart2P
        //,FontFace.EmilysCandy
        //,FontFace.Entypo
        , FontFace.Calibri
        , Icon_Light_AppbarClose
        , Icon_Dark_AppbarClose
        , Icon_Dark_AppbarCrop
      };
    }
  }
  //******************************************************************************************
  public class UIControls : IUiControls
  {
    //interface class for the application -> UI
    private IUIScript? _script = null;
    private Gui2dManager? _manager = null;

    private IUIScript Script
    {
      get
      {
        if (_script == null)
        {
          //Change GUI here
          //_script = new EditGuiScript();
          _script = new TestGuiScript();
        }
        return _script;
      }
    }
    public IGui2d CreateForView(RenderView rv)
    {
      if (_manager == null)
      {
        _manager = new Gui2dManager();
      }
      var g = _manager.GetOrCreateGui2d(Script.GetName(), UiResources.GetResources(), rv);
      Script.OnCreate(g);
      return g;
    }
    public void UpdateForView(RenderView rv)
    {
      Script.OnUpdate(rv);
    }
    public IUiWindow CreateWindow(string title, vec2 pos, vec2 size)
    {
      //**THIS IS A PROBLEM Any creation code is not accounting for a script reload, and would hold on to stale objects.
      //we need opaque types OR
      //We would have to have a callback update the creation code for any Loft.dll side UI creation
      return new UiWindow(title, pos, size);
    }
  }//cls
  public class UiControl : UiElement
  {
    public static vec4 v_defaultUp = new vec4(.88, .88, .89, 1);
    public static vec4 v_defaultHover = new vec4(.96, .96, .97, 1);
    public static vec4 v_defaultDown = new vec4(.66, .66, .67, 1);

    public UiControl(string name = "") : base(name) { }
    public void SetControlStyle()
    {
      this.Style.Padding = 0;
      this.Style.Margin = 0;
      this.Style.Border = 0;
      this.Style.MinWidth = 0;
      this.Style.MinHeight = 0;
      this.Style.MaxWidth = Gui2d.MaxSize;
      this.Style.MaxHeight = Gui2d.MaxSize;
      this.Style.PositionMode = UiPositionMode.Static;
      this.Style.SizeModeHeight = UiSizeMode.Content;
      this.Style.SizeModeWidth = UiSizeMode.Content;
      this.Style.DisplayMode = UiDisplayMode.Inline;
      this.Style.OverflowMode = UiOverflowMode.Content;
      this.Style.FloatMode = UiFloatMode.None;
      this.Style.Color = vec4.rgba_ub(230, 230, 230, 255);
      this.Style.BorderColorLeft = this.Style.BorderColorBot = OffColor.Charcoal;
      this.Style.BorderColorRight = this.Style.BorderColorTop = OffColor.VeryLightGray;
    }
    public void RegisterStyleEvents()
    {
      RegisterStyleEvents(v_defaultUp, v_defaultHover * 1.2f, v_defaultDown);
    }
    public void RegisterStyleEvents(vec4 up, vec4 hover, vec4 down)
    {
      AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        if (e.State.Focused != this)
        {
          Style.Color = hover;
        }
      });
      AddEvent(UiEventId.Mouse_Leave, (e) =>
      {
        if (e.State.Focused != this)
        {
          Style.Color = up;
        }
      });
      AddEvent(UiEventId.LmbPress, (e) =>
      {
        Style.Color = down;
      });
      AddEvent(UiEventId.Lost_Focus, (e) =>
      {
        Style.Color = up;
      });
      AddEvent(UiEventId.LmbRelease, (e) =>
      {
        Style.Color = up;
      });
    }
  }
  public class UiImage : UiElement
  {
    public UiImage(FileLoc loc, string name = "UiImage") : base(name)
    {
      this.Style.Texture = new UiTexture(loc);
      this.Style.SizePercent = 100;
      this.PickEnabled = false;
    }
  }
  public class UiButton : UiControl
  {
    public bool FocusClickOnly { get; set; } = true;
    public UiImage? Image
    {
      get { return _image; }
      set
      {
        if (_image == null && value != null)
        {
          _image = value;
          AddChild(_image);
        }
        else if (_image != null && value == null)
        {
          RemoveChild(_image);
          _image = value;
        }
        else if (_image != null && value != null)
        {
          RemoveChild(_image);
          _image = value;
          AddChild(_image);
        }
      }
    }
    private UiImage? _image = null;
    public UiButton(string name = "", bool styleEvents = true) : base(name)
    {
      SetControlStyle();
      if (styleEvents)
      {
        RegisterStyleEvents();
      }
    }
    public new UiControl Click(UiAction f)
    {
      AddEvent(UiEventId.LmbRelease, (e) =>
      {
        if (FocusClickOnly == false || e.State.Focused == this)
        {
          f.Invoke(e);
        }
      });
      return this;
    }
  }
  public class UiMenuItem : UiButton
  {
    private UiContextMenu _contextMenu = null;
    private bool _isTopLevel = true;
    private UiMenuItem? _parentMenuItem = null;
    public UiMenuItem? ParentMenuItem { get { return _parentMenuItem; } }
    protected UiElement _label;

    public UiMenuItem(string text, Action<UiEvent>? act = null, string name = "") : base(name)
    {
      FocusClickOnly = false;

      this.AddStyle(UiStyleName.Label.ToString());

      Init(text);

      if (act != null)
      {
        Click(act);
      }
    }
    public UiMenuItem AddSubMenu(Phrase p)
    {
      return AddSubMenu(Gu.Translator.Translate(p));
    }
    public UiMenuItem AddSubMenu(String s)
    {
      return AddMenuItemOrSubMenu(s, true, null);
    }
    public UiMenuItem AddItem(Phrase p, Action<UiEvent>? click = null)
    {
      return AddItem(Gu.Translator.Translate(p), click);
    }
    public UiMenuItem AddItem(string text, Action<UiEvent>? click = null)
    {
      AddMenuItemOrSubMenu(text, false, click);
      return this;
    }
    public UiMenuItem AddItem(string text, string shortcut, Action<UiEvent>? click = null)
    {
      var item = AddMenuItemOrSubMenu(text, false, click);
      item.CreateShortcut(shortcut);
      return this;
    }
    public UiMenuItem AddMenuItemOrSubMenu(string text, bool submenu, Action<UiEvent>? click = null)
    {
      UiMenuItem item = new UiMenuItem(text, click);
      item._parentMenuItem = this;
      item._isTopLevel = false;
      item.Style.DisplayMode = UiDisplayMode.Block;

      if (submenu)
      {
        item.Style.BorderRight = 16;
        item.Style.BorderColorRight = vec4.rgba_ub(250, 250, 250);
      }
      if (item._parentMenuItem != null && !(item._parentMenuItem is UiToolbarButton) && (_contextMenu == null || _contextMenu.ChildCount == 0))
      {
        item.Style.BorderLeft = 0;
      }
      Gu.Assert(item != null);
      if (_contextMenu == null)
      {
        _contextMenu = new UiContextMenu();
        _contextMenu.Hide();
        AddChild(_contextMenu);
      }
      _contextMenu.AddChild(item);

      return item;
    }
    public void CollapseUp(UiElement? stopat = null)
    {
      ShowContextMenu(false);
      if (Parent != null && Parent is UiContextMenu)
      {
        if (Parent.Parent != null && Parent.Parent is UiMenuItem && Parent.Parent != stopat)
        {
          (Parent.Parent as UiMenuItem).CollapseUp();
        }
      }
    }
    public void CollapseDown(UiElement? stopat = null)
    {
      ShowContextMenu(false);

      if (_contextMenu != null)
      {
        _contextMenu.IterateChildren((c) =>
        {
          if (c != null && c is UiMenuItem)
          {
            (c as UiMenuItem).CollapseDown();
          }
          return LambdaBool.Continue;
        });
      }
    }
    private UiElement? _shortcut = null;
    public void CreateShortcut(string text)
    {
      if (text != "")
      {
        if (_shortcut != null)
        {
          this.RemoveChild(_shortcut);
        }

        _shortcut = new UiText(text, "MenuShortcut");
        _shortcut.Style.PositionMode = UiPositionMode.Static;
        _shortcut.Style.SizeModeWidth = UiSizeMode.AutoContent; //** This should be a content auto
        _shortcut.Style.SizeModeHeight = UiSizeMode.Content;
        _shortcut.Style.DisplayMode = UiDisplayMode.NoWrap;
        _shortcut.Style.ContentAlignX = UiAlignment.Right;
        _shortcut.Style.MBP = 0;
        this.AddChild(_shortcut);
      }
    }
    private void Init(string text)
    {
      this.Style.DisplayMode = UiDisplayMode.Inline;
      this.Style.BorderLeft = 1;
      this.Style.BorderRight = 1;
      this.Style.PadTop = this.Style.PadBot = 5;
      this.Style.PadLeft = this.Style.PadRight = 6;
      this.Style.Margin = 0;

      this.Style.BorderColor = vec4.rgba_ub(190, 190, 190);
      this.Style.SizeModeWidth = UiSizeMode.AutoContent;
      this.Style.SizeModeHeight = UiSizeMode.Content;
      this.Style.PositionMode = UiPositionMode.Static;
      this.Style.MaxWidth = 600;
      this.Style.MinWidth = 0;

      this.Style.AutoModeWidth = UiAutoMode.Content;
      this.Style.AutoModeHeight = UiAutoMode.Line;

      _label = new UiText(text);
      _label.Style.PositionMode = UiPositionMode.Static;
      _label.Style.SizeModeWidth = UiSizeMode.AutoContent;
      _label.Style.SizeModeHeight = UiSizeMode.Content;
      _label.Style.DisplayMode = UiDisplayMode.NoWrap;
      _label.Style.ContentAlignX = UiAlignment.Left;
      _label.Style.MinWidth = 100;
      _label.Style.MBP = 0;
      this.AddChild(_label);

      var that = this;
      this.AddEvent(UiEventId.Lost_Focus, (e) =>
      {
        CollapseDown();
        CollapseUp();
      });
      this.AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        //mouse enters but PressFocus are a focus sitem
        if ((e.State.LeftButtonState == ButtonState.Hold ||
         e.State.LeftButtonState == ButtonState.Press ||
              e.State.LeftButtonState == ButtonState.Up) && (e.State.Focused is UiMenuItem))
        {
          ShowContextMenu(true);
        }
      });
      this.AddEvent(UiEventId.LmbPress, (e) =>
      {
        ShowContextMenu(true);
      });
      this.AddEvent(UiEventId.Mouse_Leave, (e) =>
      {
        //mouse leave to other menu item -> close submennus
        //mouse leave to something else, button up -> close all
        if (e.State.Current != null)
        {
          if (e.State.Current.Parent == this.Parent)
          {
            //moved to another menu item in same container
            CollapseDown();
          }
          else if (e.State.Current is UiMenuItem && (e.State.Current as UiMenuItem)._contextMenu == this.Parent)
          {
            //moved to parent
            CollapseDown();
          }
          else if (e.State.Current.Parent != this._contextMenu)
          {
            //current is not a child,
            CollapseDown();
            CollapseUp();
          }
        }
        else if (e.State.LeftButtonState != ButtonState.Hold && !(e.State.Focused is UiMenuItem))
        {
          CollapseDown();
        }
      });
      this.AddEvent(UiEventId.LmbRelease, (e) =>
      {
        if (e.State.Current != null)
        {
          //current should be this 
          CollapseDown(e.State.Current);
          CollapseUp(e.State.Current);
        }
      });
    }
    private void ShowContextMenu(bool show)
    {
      if (_contextMenu != null)
      {
        if (!show)
        {
          if (_contextMenu.Visible == true)
          {
            _contextMenu.Hide();
          }
        }
        else
        {
          if (_contextMenu.Visible == false)
          {
            //** Fixed offset is relative to content quad so this will be off. 
            if (_isTopLevel)
            {
              _contextMenu.Style.Top = BorderQuad._height;
              _contextMenu.Style.Left = 0;
            }
            else
            {
              _contextMenu.Style.Top = 0 - _contextMenu.Style._props.BorderTop;
              _contextMenu.Style.Left = Parent.BorderQuad._width;
            }
            _contextMenu.Show();
          }
        }
      }
    }
  }
  public class UiContextMenu : UiElement
  {
    public UiContextMenu() : base()
    {
      this.Style.PositionMode = UiPositionMode.Fixed;
      this.Style.SizeModeWidth = UiSizeMode.Content;
      this.Style.SizeModeHeight = UiSizeMode.Content;
      this.Style.Padding = 0;
      this.Style.Margin = 0;
      this.Style.Border = 0;
      this.Style.BorderBot = 1;
      this.Style.BorderTop = 1;
      this.Style.BorderColor = vec4.rgba_ub(190, 190, 190);
      this.Style.FloatMode = UiFloatMode.Floating;
      this.Style.MaxWidth = 500;
      this.Style.MinWidth = 10;

      //context menu should have no pad or margin and wont need events
    }
  }//cls    
  public class UiToolbarButton : UiMenuItem
  {
    public UiToolbarButton(Phrase p = Phrase.None) : this(Gu.Translator.Translate(p))
    {
    }
    public UiToolbarButton(string text) : base(text)
    {
      if (this._label != null)
      {
        this._label.Style.MinWidth = 0;
      }
      this.Style.MinWidth = 0;
      this.Style.BorderLeft = 0;
      this.Style.Border = 0;
      this.Style.Color = vec4.rgba_ub(240, 240, 240);
      this.Style.PadTop = this.Style.PadBot = 5;
      this.Style.PadLeft = this.Style.PadRight = 10;
      this.Style.MaxWidth = null;
      this.Style.SizeModeWidth = UiSizeMode.Content;

      this.AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        if (Parent != null)
        {
          Parent.IterateChildren((c) =>
          {
            if (c != this)
            {
              if (c is UiMenuItem)
              {
                (c as UiMenuItem).CollapseDown();
              }
            }
            return LambdaBool.Continue;
          });
        }
      });

    }
  }
  public class UiToolbar : UiElement
  {
    public UiToolbar(string name = "") : base(name)
    {
      this.Style.MinWidth = 0;
      this.Style.MinHeight = 20;
      this.Style.MaxWidth = Gui2d.MaxSize;
      this.Style.SizeModeWidth = UiSizeMode.Percent;
      this.Style.SizeModeHeight = UiSizeMode.Content;
      this.Style.MaxHeight = 200;
      this.Style.Margin = 0;
      this.Style.Padding = 0;
      this.Style.BorderBot = 1;
      this.Style.Color = vec4.rgba_ub(240, 240, 240);
      this.Style.BorderColorBot = vec4.rgba_ub(110, 110, 110);
      this.Style.FontSize = 24;
      this.Style.FontColor = OffColor.Charcoal;
      this.Style.FontFace = FontFace.Calibri;
    }
    public UiMenuItem AddItem(UiMenuItem item)
    {
      Gu.Assert(item != null);
      AddChild(item);
      return item;
    }
  }
  public class UiText : UiControl //UiLabel
  {
    //Label also can be a text box so "UiText" seems appropriate.
    public bool IsEditable { get { return _isEditable; } set { if (_isEditable != value) { _isEditable = value; UpdateEditEvents(); } } }
    private bool _isEditable = false;
    private UiElement? _cursor = null;
    private int _editChar = 0;
    private System.Timers.Timer? _cursorBlink = null;
    private System.Timers.Timer? _keyRepeat = null;
    private Keys? _keyRepeatKey = null;
    private bool _isEditing = false;

    private int _keyRepeatDelay = 800;
    private int _keyRepeatSpeed = 50;
    private int _cursorBlinkSpeed = 850;

    public UiText(string text, string name = "") : base(name)
    {
      //Dont set default font properties let them inherit
      this.Style.RenderMode = UiRenderMode.None;
      this.Style.TextWrap = UiWrapMode.Word;
      this.Text = text;
      this.Style.SizeModeWidth = UiSizeMode.Content;
      this.Style.SizeModeHeight = UiSizeMode.Content;
      if (Translator.TextFlow == LanguageTextFlow.Left)
      {
        this.Style.LayoutDirection = UiLayoutDirection.RightToLeft;
      }
      else if (Translator.TextFlow == LanguageTextFlow.Right)
      {
        this.Style.LayoutDirection = UiLayoutDirection.LeftToRight;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    public override void OnGotKeyboardFocus()
    {
      _isEditing = true;
      _cursorBlink.Start();
    }
    public override void OnLostKeyboardFocus()
    {
      EndEdit();
    }
    private void EndEdit()
    {
      _isEditing = false;
      _cursorBlink.Stop();
      StopKeyRepeat();
    }
    public override void OnKey(KeyboardKeyEvent key)
    {
      if (key.State == ButtonState.Press)
      {
        if (IsNonFunctional(key.Key))
        {
          FixEditChar();
          TypeKey(key.Key);
          StartKeyRepeat(key.Key, _keyRepeatDelay);
          FixEditChar();
        }
      }
      else if (key.State == ButtonState.Release)
      {
        if (_keyRepeatKey != null && _keyRepeatKey.Value == key.Key)
        {
          StopKeyRepeat();
        }
      }
    }//synchronous
    private bool IsNonFunctional(Keys key)
    {
      return !(
          key == Keys.LeftControl || key == Keys.RightControl
          || key == Keys.RightAlt || key == Keys.LeftAlt
          || key == Keys.RightShift || key == Keys.LeftShift
          || key == Keys.F1 || key == Keys.F2 || key == Keys.F3 || key == Keys.F4 || key == Keys.F5 || key == Keys.F6
          || key == Keys.F7 || key == Keys.F8 || key == Keys.F9 || key == Keys.F10 || key == Keys.F11 || key == Keys.F12
          );
    }
    private void TypeKey(Keys key)
    {
      //Type a valid key into the box at the cursor location
      //undo history etc .. not need but..
      //**TODO: input commands
      if (key == Keys.Left) { _editChar -= 1; }
      else if (key == Keys.Right) { _editChar += 1; }
      else if (key == Keys.Up) { } //for up/down we can just convert the cursor location into Absolute coordinates and move down by 1 line
      else if (key == Keys.Down) { }
      else if (key == Keys.Home) { }
      else if (key == Keys.End) { }
      else if (key == Keys.Tab) { }
      else if (key == Keys.CapsLock) { }
      else if (key == Keys.PageDown) { }
      else if (key == Keys.PageUp) { }
      else if (key == Keys.Escape) { SetFocus(false); }
      else if (key == Keys.Backspace)
      {
        if (!string.IsNullOrEmpty(this.Text) && _editChar > 0)
        {
          _editChar -= 1;
          this.Text = this.Text.Remove(_editChar, 1);
        }
      }
      else if (key == Keys.Delete)
      {
        if (!string.IsNullOrEmpty(this.Text) && _editChar < this.Text.Length)
        {
          this.Text = this.Text.Remove(_editChar, 1);
        }
      }
      else
      {
        Char c = (Char)(int)(key);
        this.Text = this.Text.Insert(_editChar, "" + c);
        _editChar += 1;
      }
    }
    private void StartKeyRepeat(Keys key, int ms)
    {
      _keyRepeatKey = key;
      _keyRepeat = new System.Timers.Timer();
      _keyRepeat.Interval = ms;
      _keyRepeat.Elapsed += (e, x) =>
      {
        TypeKey(_keyRepeatKey.Value);
        StartKeyRepeat(_keyRepeatKey.Value, _keyRepeatSpeed);
      };
      _keyRepeat.AutoReset = false;
      _keyRepeat.Start();
    }

    private void StopKeyRepeat()
    {
      _keyRepeatKey = null;
      if (_keyRepeat != null)
      {
        _keyRepeat.Stop();
        _keyRepeat = null;
      }
    }
    private void FixEditChar()
    {
      if (_editChar > Text.Length)
      {
        _editChar = Text.Length;
      }
      else if (_editChar < 0)
      {
        _editChar = 0;
      }
    }
    private void UpdateEditEvents()
    {
      if (_isEditable)
      {
        _cursor = new UiElement("Cursor");
        _cursor.Style.Color = OffColor.DarkSlateGray;
        _cursor.Style.FixedWidth = 3;
        _cursor.Style.FixedHeight = this.Style._props.FontSize;
        _cursor.Style.Margin = 0;
        _cursor.Style.Padding = 0;
        _cursor.Style.Border = 0;
        _cursor.Style.PositionMode = UiPositionMode.Fixed;
        AddChild(_cursor);

        _cursorBlink = new System.Timers.Timer();
        _cursorBlink.Interval = _cursorBlinkSpeed;
        _cursorBlink.Elapsed += (e, x) =>
        {
          _cursor.ToggleVisible();
        };
        _cursorBlink.AutoReset = true;

        this.AddEvent(UiEventId.LmbRelease, (e) =>
        {
          SetFocus(true);
          ClickGlyph(e.State.MousePosCur);
        });

      }
      else
      {
        _cursorBlink?.Stop();
        _cursorBlink = null;
        RemoveChild(_cursor);
        _cursor = null;
      }

    }
    private void ClickGlyph(vec2 mousePos)
    {
      Gu.DebugBreak();//TODO: fix this 
                      //for (int gi = 0; gi < _glyphs.Count; gi++)
                      //{
                      //  var g = _glyphs[gi];
                      //  // if (g._layout._b2LineQuad.ContainsPointBLI(mousePos))
                      //  // {
                      //  //   bool isRight = mousePos.x > g._layout._b2LineQuad._left + g._layout._b2LineQuad._width / 2;

      //  //   _cursor.Style.Left = g._layout._b2LineQuad._left;
      //  //   _cursor.Style.Top = g._layout._b2LineQuad._top;
      //  //   _cursor.Style.FixedHeight = this.Style._props.FontSize;
      //  //   _editChar = gi + (isRight ? 1 : 0);
      //  //   break;
      //  // }
      //}

    }
    private void SetFocus(bool focus)
    {
      if (TryGetGui2dRoot(out var g))
      {
        g.SetKeyboardFocus(focus ? this : null);
      }
      else
      {
        Gu.Log.Warn("Could not get associated GUI for text input.");
        Gu.DebugBreak();
      }
    }
  }
  public class UiToast : UiButton, IUiToast
  {
    public UiToast(string text) : base(string.Empty)
    {
      //toast at the top corner
      this.Style.PositionMode = UiPositionMode.Static;
      this.Style.RenderMode = UiRenderMode.Color;
      this.Style.SizeMode = UiSizeMode.Content;
      this.Text = text;
      this.Style.Padding = 20;
      this.Style.PadLeft = this.Style.PadRight = 60;
      this.Style.MarginTop = this.Style.MarginBot = 4;
      this.Style.MarginLeft = this.Style.MarginRight = 30;
      this.Style.FontSize = 16;
      this.Style.ContentAlignX = UiAlignment.Center;
      //this.Style.Alignment = UiAlignment.Right;
      this.Style.BorderRadius = 2;
      this.Style.Border = 2;
      this.Style.BorderColor = vec4.rgba_ub(230, 230, 230, 180);

      RemoveEvents(UiEventId.Mouse_Enter);
      Click((e) =>
      {
        this.Animate(UiPropName.Opacity, 0, 200);
      });
    }
    public void Show(string text)
    {

      this.Text = text;
      double d = this.Style._props.Opacity;
      this.Animate(UiPropName.Opacity, 1, 90);
    }
  }
  public class FPSLabel : UiText
  {
    System.Timers.Timer _timer;
    public FPSLabel() : base("127")
    {
      this.Style.FontSize = 16;
      this.Style.Color = OffColor.LightGreen * 1.2f;
      this.Style.ContentAlignX = UiAlignment.Right;
      this.Style.FixedWidth = 90;
      this.Style.Margin = 5;
      this.Style.Padding = 5;
      this.Style.PadRight = 16;
      this.Style.PadTop = this.Style.PadBot = 3;
      _timer = new System.Timers.Timer();
      _timer.Interval = 520;
      _timer.Elapsed += (e, x) =>
      {
        if (Gu.Context != null)
        {
          this.Text = StringUtil.FormatPrec(Gu.Context.FpsFrame, 1);
        }
      };
      _timer.AutoReset = true;//run once, then reset if Repeat is set
      _timer.Start();
    }
  }
  public class UiSlider : UiControl
  {
    //scroll
    public const int c_iMinThumbSize = 10;

    public enum LabelDisplayMode
    {
      Outside,
      Inside,
      None
    }
    public double Value { get { return _value; } set { _value = value; UpdateValuesChanged(); } }
    public double MinValue { get { return _minvalue; } set { _minvalue = value; UpdateValuesChanged(); } }
    public double MaxValue { get { return _maxvalue; } set { _maxvalue = value; UpdateValuesChanged(); } }
    public LabelDisplayMode LabelDisplay { get { return _labelDisplayMode; } set { _labelDisplayMode = value; UpdateLabelDisplayMode(); } }
    public float Thickness { get { return _thickness; } set { _thickness = value; UpdateStyle(); } }
    public int Precision { get { return _precision; } set { _precision = value; UpdateLabels(); } }
    public float ThumbSize { get { return _thumbsize; } set { _thumbsize = value; UpdateValuesChanged(); } }
    public float ScrollSize { get { return _scrollSize; } set { _scrollSize = value; } }
    public UiSizeMode ExpandMode { get { return _expandMode; } set { _expandMode = value; UpdateStyle(); } }
    private UiOrientation _dir { get { return this.Style.LayoutOrientation.Value; } }

    private float _scrollSize = 10;
    private double _value = 0;
    private double _minvalue = 0;
    private double _maxvalue = 100;
    private UiElement _labelRow;
    private UiElement _trackRow;
    private UiElement _lblMin;
    private UiElement _lblMax;
    private UiElement _lblVal;
    private UiControl _thumb;
    private Action<UiElement, double> _onValueChange;
    private LabelDisplayMode _labelDisplayMode;
    private bool _ismaxmin = false;
    private int _precision = 1;
    private float _thickness = 20;
    private float _thumbsize = 10;
    private int _minSize = c_iMinThumbSize;
    private UiSizeMode _expandMode = UiSizeMode.Auto;

    public UiSlider(double leftval, double rightval, double defaultval, LabelDisplayMode labeldisply, UiOrientation direction, Action<UiElement, double> onValueChange, string name = "") : base(name)
    {
      //**Leftval & Rightval - we allow having min/max on either end of the slider.. 
      //allow for left/right to be less or equal, not necessarily LTR
      Gu.Assert(onValueChange != null);

      _onValueChange = onValueChange;
      _minvalue = leftval;
      _maxvalue = rightval;
      _value = defaultval;
      _labelDisplayMode = labeldisply;

      SetControlStyle();

      float roundness = 3.5f;

      this.Style.LayoutOrientation = direction;
      this.Style.RenderMode = UiRenderMode.Color;
      this.Style.Margin = 0;
      this.Style.Border = 0;
      this.Style.Padding = 0;
      this.Style.Color = OffColor.ControlColor * 0.66f;
      this.Style.BorderColor = OffColor.Charcoal;
      this.Style.FontSize = 14;
      this.Style.BorderRadius = roundness;
      this.Style.FontColor = new vec4(OffColor.DarkGray.rgb * 0.6f, 0.85f);
      this.Style.Margin = 2;

      _trackRow = new UiElement("TrackRow");
      _trackRow.Style.RenderMode = UiRenderMode.None;
      _trackRow.Style.Margin = 0;
      _trackRow.Style.Border = 0;
      _trackRow.Style.Padding = 0;
      _trackRow.Style.AutoMode = UiAutoMode.Content;
      AddChild(_trackRow);

      _thumb = new UiControl("Thumb");
      _thumb.RegisterStyleEvents(UiControl.v_defaultUp, UiControl.v_defaultHover, UiControl.v_defaultDown * 1.3f);
      _thumb.Style.Border = 1;
      _thumb.Style.Color = (OffColor.ControlColor * 1.13f).setW(1);
      _thumb.Style.BorderColor = OffColor.LightGray * 1.2f;
      _thumb.Style.BorderRadius = roundness;

      _trackRow.AddChild(_thumb);

      UpdateLabelDisplayMode();
      UpdateValuesChanged();

      this.AddEvent(UiEventId.LmbPress, (e) =>
      {
        UpdateMovedThumb(e.State.MousePosCur, false);
      });
      this.AddEvent(UiEventId.LmbDrag, (e) =>
      {
        UpdateMovedThumb(e.State.MousePosCur, false);
      });
      _thumb.AddEvent(UiEventId.LmbDrag, (e) =>
      {
        UpdateMovedThumb(e.State.DragDelta.Value, true);
      });
      _thumb.AddEvent(UiEventId.Mouse_Scroll, (e) =>
      {
        ScrollThumb(e.State.Scroll.y);
      });
      this.AddEvent(UiEventId.Mouse_Scroll, (e) =>
      {
        ScrollThumb(e.State.Scroll.y);
      });
    }
    public void ScrollThumb(float scrollamt)
    {
      vec2 scrollpx = new vec2();
      if (_dir == UiOrientation.Vertical)
      {
        scrollpx = new vec2(0, -scrollamt * _scrollSize);
      }
      else if (_dir == UiOrientation.Horizontal)
      {
        scrollpx = new vec2(-scrollamt * _scrollSize, 0);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      UpdateMovedThumb(scrollpx, true);
    }
    private void UpdateLabelDisplayMode()
    {
      if (_labelDisplayMode == LabelDisplayMode.Outside)
      {
        _labelRow = new UiElement("LabelRow");
        _labelRow.Style.RenderMode = UiRenderMode.None;
        _labelRow.Style.Margin = _labelRow.Style.Border = 0;
        _labelRow.Style.BorderColorTop = OffColor.VeryLightGray;
        _labelRow.Style.MarginLeft = _labelRow.Style.MarginRight = 0;

        _lblMin = new UiText(_minvalue.ToString(), "MinValue");
        _lblMax = new UiText(_maxvalue.ToString(), "MaxValue");
        _lblVal = new UiText(_value.ToString(), "Value");

        _labelRow.AddChild(_lblMin);
        _labelRow.AddChild(_lblMax);
        _labelRow.AddChild(_lblVal);
        AddChild(_labelRow);
      }
      else if (_labelDisplayMode == LabelDisplayMode.Inside)
      {
        _lblVal = new UiText(_value.ToString(), "Value");
        this.AddChild(_lblVal);
      }
      else if (_labelDisplayMode == LabelDisplayMode.None)
      {
        if (_lblVal != null && _lblVal.Parent != null)
        {
          _lblVal.Parent.RemoveChild(_lblVal);
          _lblVal = null;
        }
        if (_labelRow != null && _labelRow.Parent != null)
        {
          _labelRow.Parent.RemoveChild(_labelRow);
        }
      }

      UpdateStyle();
    }
    private void UpdateStyle()
    {
      if (_dir == UiOrientation.Horizontal)
      {
        Style.SizeModeWidth = _expandMode;
        Style.SizeModeHeight = UiSizeMode.Content;
        Style.MinHeight = _thickness;

        _trackRow.Style.SizeModeWidth = _expandMode;
        _trackRow.Style.SizeModeHeight = UiSizeMode.Content;
        _trackRow.Style.MinHeight = _thickness;
        _trackRow.Style.MinWidth = _minSize;

        if (_labelDisplayMode == LabelDisplayMode.Outside)
        {
          _labelRow.Style.SizeModeWidth = _expandMode;
          _labelRow.Style.SizeModeHeight = UiSizeMode.Content;
          _labelRow.Style.MinWidth = _minSize;
          _labelRow.Style.MinHeight = 0;
          _lblMin.Style.ContentAlignX = UiAlignment.Left;
          _lblMax.Style.ContentAlignX = UiAlignment.Right;
          _lblVal.Style.ContentAlignX = UiAlignment.Center;
          _lblMin.Style.SizeMode = UiSizeMode.Content;
          _lblMax.Style.SizeMode = UiSizeMode.Content;
          _lblVal.Style.SizeMode = UiSizeMode.Content;
        }
        else if (_labelDisplayMode == LabelDisplayMode.Inside)
        {
          _lblVal.Style.ContentAlignX = UiAlignment.Center;
          _lblVal.Style.PositionModeX = UiPositionMode.Fixed;
          _lblVal.Style.PositionModeY = UiPositionMode.Fixed;
          _lblVal.Style.PercentWidth = 100;
          _lblVal.Style.PercentHeight = 100;
        }
      }
      else if (_dir == UiOrientation.Vertical)
      {
        Style.SizeModeWidth = UiSizeMode.Content;
        Style.SizeModeHeight = _expandMode;
        Style.MinWidth = _thickness;

        _trackRow.Style.SizeModeWidth = UiSizeMode.Content;
        _trackRow.Style.SizeModeHeight = _expandMode;
        _trackRow.Style.MinHeight = _minSize;
        _trackRow.Style.MinWidth = _thickness;

        if (_labelDisplayMode == LabelDisplayMode.Outside)
        {
          _labelRow.Style.SizeModeWidth = UiSizeMode.Content;
          _labelRow.Style.SizeModeHeight = _expandMode;
          _labelRow.Style.MinWidth = 0;
          _labelRow.Style.MinHeight = _minSize;
          _lblMin.Style.ContentAlignX = UiAlignment.Left;
          _lblMax.Style.ContentAlignX = UiAlignment.Right;
          _lblVal.Style.ContentAlignX = UiAlignment.Center;
          _lblMin.Style.SizeMode = UiSizeMode.Content;
          _lblMax.Style.SizeMode = UiSizeMode.Content;
          _lblVal.Style.SizeMode = UiSizeMode.Content;
        }
        else if (_labelDisplayMode == LabelDisplayMode.Inside)
        {
          _lblVal.Style.ContentAlignY = UiAlignment.Center;
          _lblVal.Style.PositionModeX = UiPositionMode.Fixed;
          _lblVal.Style.PositionModeY = UiPositionMode.Fixed;
          _lblVal.Style.PercentWidth = 100;
          _lblVal.Style.PercentHeight = 100;
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      //make sure text doesn't bloop.
      if (_lblMin != null) { _lblMin.Style.LayoutOrientation = UiOrientation.Horizontal; }
      if (_lblMax != null) { _lblMax.Style.LayoutOrientation = UiOrientation.Horizontal; }
      if (_lblVal != null) { _lblVal.Style.LayoutOrientation = UiOrientation.Horizontal; }

      SizeThumb();
    }
    //_precision
    private void UpdateValuesChanged()
    {
      if (_minvalue > _maxvalue)
      {
        var tmp = _minvalue;
        _minvalue = _maxvalue;
        _maxvalue = tmp;
        _ismaxmin = !_ismaxmin;
      }
      _value = Math.Clamp(_value, _minvalue, _maxvalue);

      SetThumbToValue(_value);
      SizeThumb();
      UpdateLabels();
    }
    private void SizeThumb()
    {
      if (_dir == UiOrientation.Horizontal)
      {
        _thumb.Style.SizeModeWidth = UiSizeMode.Fixed;
        _thumb.Style.SizeModeHeight = UiSizeMode.Auto;
        _thumb.Style.FixedWidth = _thumbsize;
        _thumb.Style.MaxHeight = Gui2d.MaxSize;
        _thumb.Style.MinHeight = 0;
        _thumb.Style.PositionModeX = UiPositionMode.Fixed;
        _thumb.Style.PositionModeY = UiPositionMode.Static;
      }
      else if (_dir == UiOrientation.Vertical)
      {
        _thumb.Style.SizeModeWidth = UiSizeMode.Auto;
        _thumb.Style.SizeModeHeight = UiSizeMode.Fixed;
        _thumb.Style.FixedHeight = _thumbsize;
        _thumb.Style.MaxWidth = Gui2d.MaxSize;
        _thumb.Style.MinWidth = 0;
        _thumb.Style.PositionModeX = UiPositionMode.Static;
        _thumb.Style.PositionModeY = UiPositionMode.Fixed;
      }
    }
    private void SetThumbToValue(double value)
    {
      Gu.Assert(_thumb != null);
      Gu.Assert(_thumb.Parent != null);

      double valw = ScrollWH();

      value = Math.Clamp(value, _minvalue, _maxvalue);

      var denom = (_maxvalue - _minvalue);
      double pct = 0;
      if (denom != 0)
      {
        pct = value / denom;
      }
      else
      {
        Gu.Trap();
      }
      if (_ismaxmin)
      {
        pct = 1 - pct;
      }
      _thumb.Style.L_Left(_dir, (float)(pct * valw));
    }
    private double ScrollWH()
    {
      var d = (double)(_thumb.Parent._block._b2ContentQuad.L_Width(_dir) - _thumb._block._b2MarginQuad.L_Width(_dir));
      return d;
    }
    private void UpdateMovedThumb(vec2 mpos, bool relative)
    {
      Gu.Assert(_thumb != null);
      Gu.Assert(_thumb.Parent != null);

      float mp = _dir == UiOrientation.Horizontal ? mpos.x : mpos.y;

      if (relative)
      {
        _thumb.Style.L_Left(_dir, _thumb.Style.L_Left(_dir).Value + mp);
      }
      else
      {
        var tpos = mp - _thumb.Parent._block._b2MarginQuad.L_Left(_dir) - _thumb._block._b2MarginQuad.L_Width(_dir) / 2;
        _thumb.Style.L_Left(_dir, tpos);
      }

      double valw = ScrollWH();

      if (_thumb.Style.L_Left(_dir) < 0)
      {
        _thumb.Style.L_Left(_dir, 0);
      }
      else if (_thumb.Style.L_Left(_dir) > valw)
      {
        _thumb.Style.L_Left(_dir, (float)valw);
      }

      double pct = 0;
      if (valw > 0)
      {
        pct = (double)_thumb.Style.L_Left(_dir).Value / valw;
      }
      else
      {
        Gu.Trap();
      }

      if (_ismaxmin)
      {
        pct = 1 - pct;
      }

      _value = _minvalue + (_maxvalue - _minvalue) * pct;
      _value = Math.Clamp(_value, _minvalue, _maxvalue);

      UpdateLabels();

      _onValueChange?.Invoke(this, Value);
    }
    private void UpdateLabels()
    {
      if (_lblMax != null && _lblMin != null)
      {
        if (_ismaxmin)
        {
          _lblMin.Text = StringUtil.FormatPrec(_maxvalue, _precision);
          _lblMax.Text = StringUtil.FormatPrec(_minvalue, _precision);
        }
        else
        {
          _lblMax.Text = StringUtil.FormatPrec(_maxvalue, _precision);
          _lblMin.Text = StringUtil.FormatPrec(_minvalue, _precision);
        }
      }

      if (_lblVal != null)
      {
        _lblVal.Text = StringUtil.FormatPrec(_value, _precision);
      }
    }
  }//cls
  public class UiScrollRegion : UiElement
  {
    private UiElement _content;
    private UiSlider _vscroll;
    private UiSlider _hscroll;

    public bool AlwaysShowScrollbars { get; set; } = true;
    public UiElement Content { get { return _content; } }
    public override string Text { get { return _content.Text; } set { _content.Text = value; } }
    public bool ScrollPastEOLX { get { return _scrollPastEOLX; } set { _scrollPastEOLX = value; } }
    public bool ScrollPastEOLY { get { return _scrollPastEOLY; } set { _scrollPastEOLY = value; } }

    private bool _scrollPastEOLX = false;
    private bool _scrollPastEOLY = true;
    private float _scrollPastEOLPercentX = 0.1f;//scrolls % of the way past the EOL
    private float _scrollPastEOLPercentY = 0.1f;

    public UiScrollRegion(string name = "") : base(name)
    {
      this.Style.RenderMode = UiRenderMode.Color;
      this.Style.LayoutDirection = UiLayoutDirection.LeftToRight;
      this.Style.LayoutOrientation = UiOrientation.Horizontal;
      this.Style.Margin = 0;
      this.Style.Padding = 0;
      this.Style.MinWidth = this.Style.MinHeight = 0;
      this.Style.MaxWidth = this.Style.MaxHeight = Gui2d.MaxSize;
      this.Style.SizeMode = UiSizeMode.AutoContent;
      this.Style.AutoMode = UiAutoMode.Content;

      float sb_size = 12;

      _content = new UiElement("ScrollRegionContent");
      _content.Style.DisplayMode = UiDisplayMode.NoWrap;
      _content.Style.SizeModeWidth = UiSizeMode.Auto;
      _content.Style.SizeModeHeight = UiSizeMode.Auto;
      _content.Style.Margin = 0;
      _content.Style.Padding = 0;
      _content.Style.Border = 0;
      _content.Style.ContentAlignX = UiAlignment.Left;
      _content.Style.OverflowMode = UiOverflowMode.Content;
      this.AddChild(_content);

      _vscroll = new UiSlider(0, 1, 0, UiSlider.LabelDisplayMode.None, UiOrientation.Vertical, (e, v) =>
      {
        Scroll(UiOrientation.Vertical, (float)v);
      }, "ScrollRegionVScroll");
      _vscroll.Style.PercentHeight = 100;//SizeModeHeight = UiSizeMode.Auto;
      _vscroll.Thickness = sb_size;
      _vscroll.Style.DisplayMode = UiDisplayMode.NoWrap;
      this.AddChild(_vscroll);

      _hscroll = new UiSlider(0, 1, 0, UiSlider.LabelDisplayMode.None, UiOrientation.Horizontal, (e, v) =>
      {
        Scroll(UiOrientation.Horizontal, (float)v);
      }, "ScrollRegionHScroll");
      _hscroll.Style.DisplayMode = UiDisplayMode.Block;
      _hscroll.Style.SizeModeWidth = UiSizeMode.Auto;
      //_hscroll.Style.AutoMode = UiAutoMode.Line;
      _hscroll.Thickness = sb_size;
      this.AddChild(_hscroll);

      var thumb = new UiElement("Scroll_Region_Corner_Thumb");
      thumb.Style.FixedWidth = sb_size;
      thumb.Style.FixedHeight = sb_size;
      thumb.Style.DisplayMode = UiDisplayMode.NoWrap;
      thumb.Style.Color = OffColor.LightGray;
      this.AddChild(thumb);

      //This will end up incorrect since the items are not yet laid out.
      SizeThumb(UiOrientation.Horizontal);
      SizeThumb(UiOrientation.Vertical);

      _content.OnResize += () =>
      {
        SizeThumb(UiOrientation.Horizontal);
        SizeThumb(UiOrientation.Vertical);
        Scroll(UiOrientation.Horizontal,(float) _hscroll.Value);
        Scroll(UiOrientation.Vertical,  (float)_vscroll.Value);
      };

      this.AddEvent(UiEventId.Mouse_Scroll, (e) =>
      {
        if (_vscroll.Visible)
        {
          _vscroll.ScrollThumb(e.State.Scroll.y);
        }
        else if (_hscroll.Visible)
        {
          _hscroll.ScrollThumb(e.State.Scroll.y);
        }
      });
    }
    private void SizeThumb(UiOrientation dir)
    {
      UiSlider scroll = (dir == UiOrientation.Vertical) ? _vscroll : _hscroll;

      if (_content._block._container != null)
      {
        float contsz = _content._block._container._contentWH.L_Width(dir);
        if (contsz > 0)
        {
          float thpct = Math.Clamp(_content._block._b2ContentQuad.L_Width(dir) / contsz, 0, 1);
          if (thpct >= 0.999f && AlwaysShowScrollbars == false)
          {
            scroll.Visible = false;
          }
          else
          {
            scroll.Visible = true;
            float thsiz = thpct * scroll._block._b2ContentQuad.L_Width(dir);
            scroll.ThumbSize = Math.Max(UiSlider.c_iMinThumbSize, thsiz);
          }
        }
      }
    }
    private void Scroll(UiOrientation dir, float v)
    {
      if (_content._block._container != null)
      {
        float contw = _content._block._container._contentWH.L_Width(dir); //_content._block._b2ContentQuad.L_Width(dir);

        float offset = 0;
        if (dir == UiOrientation.Horizontal) { offset = contw + (_scrollPastEOLX ? contw * (_scrollPastEOLPercentX) : 0); }
        if (dir == UiOrientation.Vertical) { offset = contw + (_scrollPastEOLY ? contw * (_scrollPastEOLPercentY) : 0); }

        float overflow = Math.Max(contw-_content._block._b2ContentQuad.L_Width(dir), 0);

        _content.Style.L_Left(dir, -overflow * v);

        SizeThumb(dir);
      }
    }
  }//cls
  public class UiAutoRow : UiElement
  {
    //An equally distributed row of left, center, right elements.
    public class UiAutoCol : UiElement
    {
      public UiElement Element { get { return _element; } set { SetElement(ref _element, value); } }
      private UiElement? _element = null;

      public UiAutoCol(UiAlignment align)
      {
        this.Style._props.ContentAlignX = align;
        this.Style._props.SizeModeWidth = UiSizeMode.AutoContent;
        this.Style._props.DisplayMode = UiDisplayMode.NoWrap;
      }
      private void SetElement(ref UiElement? cur, UiElement? next)
      {
        if (cur != null)
        {
          RemoveChild(cur);
        }
        if (next != null)
        {
          AddChild(next);
        }
        cur = next;
      }
    }

    public UiAutoCol LeftCol { get { return _left; } }
    public UiAutoCol CenterCol { get { return _center; } }
    public UiAutoCol RightCol { get { return _right; } }

    public UiElement Left { get { return _left.Element; } set { _left.Element = value; } }
    public UiElement Center { get { return _center.Element; } set { _center.Element = value; } }
    public UiElement Right { get { return _right.Element; } set { _right.Element = value; } }

    public UiAutoCol? _left = null;
    public UiAutoCol? _center = null;
    public UiAutoCol? _right = null;

    public UiAutoRow(string name = "") : base(name)
    {
      Init();
      this.Style.PercentWidth = 100;
      this.Style.SizeModeHeight = UiSizeMode.Content;
      this.Style.DisplayMode = UiDisplayMode.Block;
    }
    public UiAutoRow(UiElement center, string name = "") : this(name)
    {
      Center = center;
    }
    public UiAutoRow(UiElement left, UiElement right, string name = "") : this(name)
    {
      Left = left;
      Right = right;
    }
    public UiAutoRow(UiElement left, UiElement center, UiElement right, string name = "") : this(name)
    {
      Left = left;
      Center = center;
      Right = right;
    }
    private void Init()
    {
      _left = new UiAutoCol(UiAlignment.Left);
      _center = new UiAutoCol(UiAlignment.Center);
      _right = new UiAutoCol(UiAlignment.Right);
      AddChild(_left);
      AddChild(_center);
      AddChild(_right);
    }
  }//cls
  public class UiWindow : UiControl, IUiWindow
  {
    public enum UiWindowStyle
    {
      Sizable,
      Fixed,
      Tool,
    }

    public IUiElement Content { get { return _region.Content; } }
    public Action<UiEvent>? OnClose { get; set; } = null;

    public UiScrollRegion Region { get { return _region; } }
    private UiAutoRow _titleBar;
    private UiAutoRow _statusBar;
    private UiScrollRegion _region;
    private UiWindowStyle _style;
    private vec2 _initialPos;
    private vec2 _initialWH;

    public UiWindow(string title, vec2 pos, vec2 wh, UiWindowStyle style = UiWindow.UiWindowStyle.Sizable, string name = "") : base(name)
    {
      _style = style;
      this.PickEnabled = true; //force pick to obscure other elements.

      _initialPos = pos;
      _initialWH = wh;

      var WINDOW_COLOR = new vec4(0.997f, 0.997f, 0.998f, 1);
      var TITLEBAR_COLOR = OffColor.WhiteSmoke;
      var STATUSBAR_COLOR = OffColor.WhiteSmoke;// OffColor.LightGray;

      this.Style.FloatMode = UiFloatMode.Floating; //sort domain
      this.Style.PositionMode = UiPositionMode.Fixed;
      this.Style.Margin = 0;
      this.Style.Padding = 0;
      this.Style.Border = 2;
      this.Style.BorderColor = OffColor.DimGray;
      this.Style.BorderRadius = 8;
      this.Style.LayoutOrientation = UiOrientation.Horizontal;
      this.Style.FontSize = 17;
      this.Style.FontFace = FontFace.Calibri;
      this.Style.FontColor = OffColor.Black;
      this.Style.Color = WINDOW_COLOR;

      SetDefaultPosition();

      var closebt = new UiButton("win_close_button");
      closebt.RegisterStyleEvents();
      closebt.Style.FixedWidth = closebt.Style.FixedHeight = 20;
      closebt.Style.DisplayMode = UiDisplayMode.Inline;
      closebt.Style.Color = TITLEBAR_COLOR;
      closebt.Image = new UiImage(UiResources.Icon_Dark_AppbarClose);
      closebt.Style.Border = 1;
      closebt.Style.BorderColor = OffColor.DimGray;
      closebt.Style.BorderRadius = 3;
      closebt.Click((e) =>
      {
        if (OnClose != null)
        {
          OnClose.Invoke(e);
        }
        if (!e.PreventDefault)
        {
          this.Close();
        }
      });

      _titleBar = new UiAutoRow("win_title_bar");
      _titleBar.Style.Padding = 5;
      _titleBar.Style.BorderBot = 1;
      _titleBar.Style.BorderColorBot = this.Style.BorderColor;
      _titleBar.Style.BorderTopLeftRadius = this.Style.BorderTopLeftRadius;
      _titleBar.Style.BorderTopRightRadius = this.Style.BorderTopRightRadius;
      _titleBar.Style.Color = TITLEBAR_COLOR;
      _titleBar.AddEvent(UiEventId.LmbDrag, (e) =>
      {
        var delta = e.State.DragDelta.Value;
        this.Style.Left += delta.x;
        this.Style.Top += delta.y;
      });
      _titleBar.Right = closebt;
      _titleBar.Center = new UiText(title);
      _titleBar.Left = new UiElement();

      _region = new UiScrollRegion("WindowContent");
      _region.Style.SizeModeWidth = UiSizeMode.Auto;
      _region.Style.SizeModeHeight = UiSizeMode.Auto;
      _region.Style.OverflowMode = UiOverflowMode.Content;
      _region.Style.DisplayMode = UiDisplayMode.Block;

      _statusBar = new UiAutoRow("win_status_bar");
      _statusBar.Style.Padding = 3;
      _statusBar.Style.PadLeft = 16;
      _statusBar.Style.PadTop = 2;
      _statusBar.Style.PadRight = 0;
      _statusBar.Style.PadBot = 0;
      _statusBar.Style.Color = STATUSBAR_COLOR;
      _statusBar.Style.FontColor = OffColor.Gray;
      _statusBar.Style.BorderBotRightRadius = this.Style.BorderBotRightRadius;
      _statusBar.Style.BorderBotLeftRadius = this.Style.BorderBotLeftRadius;
      _statusBar.LeftCol.Style.ContentAlignY = UiAlignment.Center;

      var sizeThumb = new UiButton("win_size_thumb", false);
      sizeThumb.Style.FixedWidth = sizeThumb.Style.FixedHeight = 20;
      sizeThumb.Style.ContentAlignX = UiAlignment.Right;
      sizeThumb.Style.Color = _statusBar.Style.Color;
      sizeThumb.AddEvent(UiEventId.LmbDrag, (e) =>
      {
        var delta = e.State.DragDelta.Value;
        this.Style.FixedWidth += delta.x;
        if (this.Style.FixedWidth < 100)
        {
          this.Style.FixedWidth = 100;
        }
        this.Style.FixedHeight += delta.y;
        var minh = this.Style.BorderTop + this.Style.BorderBot + this.Style.PadTop + this.Style.PadBot;
        if (_titleBar.Visible == true)
        {
          minh += _titleBar._block._b2MarginQuad._height;
        }
        if (_statusBar.Visible == true)
        {
          minh += _statusBar._block._b2MarginQuad._height;
        }
        if (this.Style.FixedHeight < minh)
        {
          this.Style.FixedHeight = minh;
        }
      });
      sizeThumb.Image = new UiImage(UiResources.Icon_Dark_AppbarCrop);
      //**TODO: clip radius.
      sizeThumb.Image.Style.BorderBotRightRadius = this.Style.BorderBotRightRadius;

      _statusBar.Left = new UiText("info");
      _statusBar.Left.Style.FontSize = 12;
      _statusBar.Left.Style.FontColor = OffColor.MediumGray;
      _statusBar.Left.Style.BorderBotRightRadius = this.Style.BorderBotRightRadius;
      _statusBar.Left.Style.ContentAlignX = _statusBar.LeftCol.Style.ContentAlignX = UiAlignment.Left;
      _statusBar.Left.Style.ContentAlignY = _statusBar.LeftCol.Style.ContentAlignY = UiAlignment.Center;
      _statusBar.LeftCol.Style.SizeModeHeight = UiSizeMode.Auto;

      _statusBar.Center = new UiElement();

      _statusBar.Right = sizeThumb;
      _statusBar.Right.Style.BorderBotLeftRadius = this.Style.BorderBotLeftRadius;

      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //vTESTING
      //  _titleBar.Style.FixedHeight = 30;
      //  _statusBar.Style.FixedHeight = 30;
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      //TESTING
      AddChild(_titleBar);
      AddChild(_region);
      AddChild(_statusBar);
    }
    public void SetDefaultPosition()
    {
      this.Style.Left = _initialPos.x;
      this.Style.Top = _initialPos.y;
      this.Style.FixedWidth = _initialWH.width;
      this.Style.FixedHeight = _initialWH.height;
    }
    public void Close()
    {
      if (Parent != null)
      {
        Parent.RemoveChild(this);
      }
    }
  }//cls

}//ns