
namespace PirateCraft
{
  using UiAction = System.Action<UiEvent>;

  public class UiControl : UiElement
  {
    private bool _hasFocus = false;//pick focus
    private bool _hasClickFocus = false;//user holding  mouse button down
    public new UiControl Click(UiAction f)
    {
      if (f != null)
      {
        AddEvent(UiEventId.LmbRelease, f);
      }
      return this;
    }

    //base class for controls button/thumb/scrollbar/knob/trackbar.. etc
    public UiControl() : this(UiStyleName.BaseControl, null, true)
    {
      RegisterStyleEvents();
    }
    public UiControl(string text, bool useMouseInputStyle = true) : this(UiStyleName.BaseControl, text, useMouseInputStyle)
    {
    }
    public UiControl(UiStyleName style, string text, bool useMouseInputStyle = true) : base(style, text)
    {
      if (useMouseInputStyle)
      {
        RegisterStyleEvents();
      }
      SetStyle();
    }
    private void SetStyle()
    {
      this.Style.RenderMode = UiRenderMode.Color;
      this.Style.FontColor = vec4.rgba_ub(42, 42, 42, 255);
      this.Style.Color = vec4.rgba_ub(230, 230, 230, 255);
      this.Style.BorderColor = new vec4(0.14f, 0.16f, 0.16f, 1);
      this.Style.Padding = 0;
      this.Style.Margin = 0;
      this.Style.Border = 0;
      this.Style.MinWidth = 0;
      this.Style.MinHeight = 0;
      this.Style.MaxWidth = Gui2d.MaxSize;
      this.Style.MaxHeight = Gui2d.MaxSize;
      this.Style.FontFace = FontFace.Calibri;
      this.Style.FontStyle = UiFontStyle.Normal;
      this.Style.FontSize = 22;
      this.Style.LineHeight = 1.0f;
      this.Style.PositionMode = UiPositionMode.Static;
      this.Style.SizeModeHeight = UiSizeMode.Shrink;
      this.Style.SizeModeWidth = UiSizeMode.Shrink;
      this.Style.DisplayMode = UiDisplayMode.Inline;
      this.Style.OverflowMode = UiOverflowMode.Hide;
      this.Style.FloatMode = UiFloatMode.None;
    }
    private void RegisterStyleEvents()
    {
      AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        _hasFocus = true;
        Style.MultiplyColor = new vec4(1.08f, 1.0f);
      });
      AddEvent(UiEventId.Mouse_Leave, (e) =>
      {
        _hasFocus = false;
        Style.MultiplyColor = new vec4(1.0f, 1.0f);
      });
      AddEvent(UiEventId.LmbPress, (e) =>
      {
        _hasClickFocus = true;
        Style.MultiplyColor = new vec4(0.82f, 1.0f);
      });
      AddEvent(UiEventId.LmbRelease, (e) =>
      {
        _hasClickFocus = false;
        Style.MultiplyColor = new vec4(1.0f, 1.0f);
      });
    }
  }
  public class UiMenuItem : UiControl
  {
    public override string NamingPrefix { get { return "mnu"; } }
    private UiContextMenu _contextMenu = null;
    private bool _isTopLevel = true;
    private UiMenuItem? _parentMenuItem = null;

    public UiMenuItem(Phrase p = Phrase.None) : this(Gu.Translator.Translate(p))
    {
    }
    public UiMenuItem(string text, Action<UiEvent>? act = null) : base(UiStyleName.MenuItem, text)
    {
      Init();
      if (act != null)
      {
        Click(act);
      }
    }
    public UiMenuItem AddSubMenu(Phrase p)
    {
      return AddMenuItemOrSubMenu(Gu.Translator.Translate(p), true, null);
    }
    public UiMenuItem AddItem(Phrase p, Action<UiEvent>? click = null)
    {
      return AddItem(Gu.Translator.Translate(p), click);
    }
    public UiMenuItem AddItem(string text, Action<UiEvent>? click = null)
    {
      //daisy chaining these.
      AddMenuItemOrSubMenu(text, false, click);
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
        item.Style.BorderRightColor = vec4.rgba_ub(250, 250, 250);
      }
      if (item._parentMenuItem != null && !(item._parentMenuItem is UiToolbarButton) && (_contextMenu == null || _contextMenu.Children == null || _contextMenu.Children.Count == 0))
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
      if (_contextMenu != null && _contextMenu.Children != null)
      {
        foreach (var c in _contextMenu.Children)
        {
          if (c != null && c is UiMenuItem)
          {
            (c as UiMenuItem).CollapseDown();
          }
        }
      }
    }
    private void Init()
    {
      this.Style.DisplayMode = UiDisplayMode.Inline;
      this.Style.BorderLeft = 2;
      this.Style.BorderRight = 2;
      this.Style.BorderColor = vec4.rgba_ub(190, 190, 190);

      this.Style.SizeModeWidth = UiSizeMode.Expand;
      this.Style.SizeModeHeight = UiSizeMode.Shrink;
      this.Style.PositionMode = UiPositionMode.Static;
      this.Style.MaxWidth = 500;
      this.Style.MinWidth = 0;
      this.Style.MarginTop = this.Style.MarginBot = 6;
      this.Style.MarginLeft = this.Style.MarginRight = 20;
      this.Style.Padding = 0;

      var that = this;
      this.AddEvent(UiEventId.Lost_Press_Focus, (e) =>
      {
        CollapseDown();
        CollapseUp();
      });
      this.AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        //mouse enters but PressFocus are a focus sitem
        if ((e.State.LeftButtonState == ButtonState.Hold ||
         e.State.LeftButtonState == ButtonState.Press ||
              e.State.LeftButtonState == ButtonState.Up) && (e.State.PressFocus is UiMenuItem))
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
        else if (e.State.LeftButtonState != ButtonState.Hold && !(e.State.PressFocus is UiMenuItem))
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
            if (_isTopLevel)
            {
              _contextMenu.Style.Top = this.FinalQuad.Bottom;
              _contextMenu.Style.Left = this.FinalQuad.Left;
            }
            else
            {
              _contextMenu.Style.Top = this.FinalQuad.Top - 2;
              _contextMenu.Style.Left = Parent.FinalQuad.Right;
            }
            _contextMenu.Show();
          }
        }
      }
    }
  }
  public class UiLabel : UiControl
  {
    public UiLabel(string text) : base(text)
    {
    }
  }
  public class UiToast : UiControl
  {
    public UiToast(string text) : base(text)
    {
      this.Name = "_lblToast";
      //toast at the top corner
      this.Text = text;
      this.Style.Margin = 20;
      this.Style.MarginLeft = this.Style.MarginRight = 60;
      this.Style.PadTop = this.Style.PadBot = 4;
      this.Style.PadLeft = this.Style.PadRight = 30;
      this.Style.FontSize = 16;
      this.Style.PositionMode = UiPositionMode.Static;
      this.Style.TextAlign = UiAlignment.Center;
      this.Style.Alignment = UiAlignment.Right;
      this.Style.BorderRadius = 2;
      this.Style.Border = 2;
      this.Style.BorderColor = vec4.rgba_ub(230, 230, 230, 180);

      RemoveEvents(UiEventId.Mouse_Enter);
      Click((e) =>
      {
        double d = this.Style._props.Opacity;
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
  public class UiToolbarButton : UiMenuItem
  {
    public UiToolbarButton(Phrase p = Phrase.None) : this(Gu.Translator.Translate(p))
    {
    }
    public UiToolbarButton(string text) : base(text)
    {
      //expand height, but set minimum height to be contents?
      this.Style.SizeModeHeight = UiSizeMode.Expand;
      this.Style.SizeModeWidth = UiSizeMode.Shrink;
      this.Style.PositionMode = UiPositionMode.Static;
      this.Style.MinWidth = 0;
      this.Style.BorderLeft = 0;
      this.Style.Border = 0;
      this.Style.Color = vec4.rgba_ub(240, 240, 240);

      this.AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        if (Parent != null)
        {
          foreach (var c in Parent.Children)
          {
            if (c != this)
            {
              if (c is UiMenuItem)
              {
                (c as UiMenuItem).CollapseDown();
              }
            }
          }
        }
      });

    }
  }
  public class UiToolbar : UiElement
  {
    public override string NamingPrefix { get { return "tlb"; } }

    public UiToolbar() : base(UiStyleName.Toolbar)
    {
    }

    public UiMenuItem AddItem(UiMenuItem item)
    {
      Gu.Assert(item != null);
      AddChild(item);
      return item;
    }
  }
  public class UiContextMenu : UiElement
  {
    public override string NamingPrefix { get { return "ctxm"; } }
    public UiContextMenu() : base(UiStyleName.ContextMenu)
    {
      this.Style.BorderColor = vec4.rgba_ub(190, 190, 190);
      this.Style.BorderBot = 2;
      this.Style.BorderTop = 2;
      //context menu should have no pad or margin and wont need events
    }
  }//cls

  public class UiWindow : UiElement
  {
    private UiElement _titleBar;
    public UiWindow(string title, vec2 pos, vec2 wh, string text = "") : base(UiStyleName.BaseControl)
    {
      this.Style.PositionMode = UiPositionMode.Absolute;
      this.Style.RenderMode = UiRenderMode.Color;
      this.Style.SizeModeWidth = UiSizeMode.Fixed;
      this.Style.SizeModeHeight = UiSizeMode.Fixed;
      this.Style.Border = 2;
      this.Style.BorderColor = vec4.rgba_ub(140, 140, 140);
      this.Style.Left = pos.x;
      this.Style.Top = pos.y;
      this.Style.Width = wh.width;
      this.Style.Height = wh.height;

      _titleBar = new UiElement(UiStyleName.BaseControl);
      _titleBar.Style.RenderMode = UiRenderMode.Color;
      _titleBar.Style.SizeModeWidth = UiSizeMode.Expand;
      _titleBar.Style.SizeModeHeight = UiSizeMode.Shrink;
      _titleBar.Style.MinHeight = 20;
      _titleBar.Style.MaxHeight = 80;
      _titleBar.Text = title;
      _titleBar.Style.Margin = 4;
      _titleBar.Style.TextAlign = UiAlignment.Center;

      var closebt = new UiElement(UiStyleName.BaseControl);
      closebt = new UiElement();
      closebt.Style.RenderMode = UiRenderMode.Color;
      closebt.Style.Alignment = UiAlignment.Right;
      closebt.Style.SizeModeWidth = UiSizeMode.Shrink;
      closebt.Style.SizeModeHeight = UiSizeMode.Shrink;
      closebt.Style.Height = 18;
      closebt.Style.DisplayMode = UiDisplayMode.Inline;
      closebt.Text = "X";
      closebt.Style.FontSize = 14;
      closebt.Click(x => this.Close());
      _titleBar.AddChild(closebt);

      var contentArea = new UiElement(UiStyleName.BaseControl);
      contentArea.Style.SizeModeHeight = UiSizeMode.Expand;
      contentArea.Style.SizeModeWidth = UiSizeMode.Expand;
      contentArea.Style.Margin = 10;
      contentArea.Text = text;

      AddChild(_titleBar);
      AddChild(contentArea);
    }
    public void Close()
    {
      if (Parent != null)
      {
        Parent.RemoveChild(this);
      }
    }

  }


}//ns