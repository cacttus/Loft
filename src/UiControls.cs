
namespace Loft
{
  using UiAction = System.Action<UiEvent>;

  public class UiControl : UiElement
  {
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
      this.Style.BorderColorLeft = this.Style.BorderColorBot = OffColor.Charcoal;
      this.Style.BorderColorRight = this.Style.BorderColorTop = OffColor.VeryLightGray;
    }
    private void RegisterStyleEvents()
    {
      AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        Style.MultiplyColor = new vec4(1.08f, 1.0f);
      });
      AddEvent(UiEventId.Mouse_Leave, (e) =>
      {
        Style.MultiplyColor = new vec4(1.0f, 1.0f);
      });
      AddEvent(UiEventId.LmbPress, (e) =>
      {
        Style.MultiplyColor = new vec4(0.82f, 1.0f);
      });
      AddEvent(UiEventId.LmbRelease, (e) =>
      {
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
    public UiMenuItem? ParentMenuItem { get { return _parentMenuItem; } }
    protected UiElement _label;

    public UiMenuItem(Phrase p = Phrase.None) : this(Gu.Translator.Translate(p))
    {
    }
    public UiMenuItem(string text, Action<UiEvent>? act = null) : base(UiStyleName.Label, "")
    {
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
    public void CreateShortcut(string text)
    {
      if (text != "")
      {
        var test_shortcut = new UiElement();
        test_shortcut.Text = text;
        test_shortcut.Style.PositionMode = UiPositionMode.Static;
        test_shortcut.Style.SizeModeWidth = UiSizeMode.Shrink;
        test_shortcut.Style.SizeModeHeight = UiSizeMode.Expand;
        test_shortcut.Style.DisplayMode = UiDisplayMode.InlineNoWrap;
        test_shortcut.Style.Alignment = UiAlignment.Right;
        test_shortcut.Style.TextAlign = UiAlignment.Right;
        test_shortcut.Style.Border = 0;
        test_shortcut.Style.Margin = 0;
        test_shortcut.Style.Padding = 0;
        this.AddChild(test_shortcut);
      }
    }
    private void Init(string text)
    {
      this.Style.DisplayMode = UiDisplayMode.Inline;
      this.Style.BorderLeft = 1;
      this.Style.BorderRight = 1;
      this.Style.BorderColor = vec4.rgba_ub(190, 190, 190);

      _label = new UiElement();
      _label.Text = text;
      _label.Style.SizeModeWidth = UiSizeMode.Shrink;
      _label.Style.SizeModeHeight = UiSizeMode.Shrink;
      _label.Style.PositionMode = UiPositionMode.Static;
      _label.Style.DisplayMode = UiDisplayMode.InlineNoWrap;
      _label.Style.MinWidth = 100;
      _label.Style.Margin = _label.Style.Border = _label.Style.Padding = 0;
      this.AddChild(_label);

      this.Style.FontSize = 16;
      this.Style.SizeModeWidth = UiSizeMode.Expand;
      this.Style.SizeModeHeight = UiSizeMode.Shrink;
      this.Style.PositionMode = UiPositionMode.Static;
      this.Style.MaxWidth = 600;
      this.Style.MinWidth = 0;
      this.Style.MarginTop = this.Style.MarginBot = 5;
      this.Style.MarginLeft = this.Style.MarginRight = 10;
      this.Style.Padding = 0;//Do not use padding

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
            if (_isTopLevel)
            {
              _contextMenu.Style.Top = this.FinalQuad.Bottom;
              _contextMenu.Style.Left = this.FinalQuad.Left;
            }
            else
            {
              _contextMenu.Style.Top = this.FinalQuad.Top - _contextMenu.Style._props.BorderTop;
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
    public UiLabel(Phrase p) : this(Gu.Translator.Translate(p))
    {
    }
    public UiLabel(string text) : base(text)
    {
      if (Translator.TextFlow == LanguageTextFlow.Left)
      {
        this.Style.LayoutDirection = UiLayoutDirection.Left;
      }
      else if (Translator.TextFlow == LanguageTextFlow.Right)
      {
        this.Style.LayoutDirection = UiLayoutDirection.Right;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
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
      if (this._label != null)
      {
        this._label.Style.MinWidth = 0;
      }
      //expand height, but set minimum height to be contents?
      this.Style.SizeModeHeight = UiSizeMode.Expand;
      this.Style.SizeModeWidth = UiSizeMode.Shrink;
      this.Style.PositionMode = UiPositionMode.Static;
      this.Style.MinWidth = 0;
      this.Style.BorderLeft = 0;
      this.Style.Border = 0;
      this.Style.Color = vec4.rgba_ub(240, 240, 240);
      this.Style.MarginTop = this.Style.MarginBot = 5;
      this.Style.MarginLeft = this.Style.MarginRight = 10;

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

    public UiToolbar() : base(UiStyleName.BaseControl)
    {
      this.Style.MinWidth = 0;
      this.Style.MinHeight = 20;
      this.Style.MaxWidth = Gui2d.MaxSize;
      this.Style.SizeModeWidth = UiSizeMode.Expand;
      this.Style.SizeModeHeight = UiSizeMode.Shrink;
      this.Style.MaxHeight = 200;
      this.Style.Margin = 0;
      this.Style.Padding = 0;
      this.Style.BorderBot = 1;
      this.Style.Color = vec4.rgba_ub(240, 240, 240);
      this.Style.BorderColorBot = vec4.rgba_ub(110, 110, 110);

      var fsp = new UiElement();
      fsp.Style.SizeModeHeight = UiSizeMode.Shrink;
      fsp.Style.SizeModeWidth = UiSizeMode.Shrink;
      fsp.Style.FontSize = 16;
      fsp.Style.TextAlign = UiAlignment.Right;
      fsp.Style.RenderMode = UiRenderMode.Color;
      fsp.Text = "127";
      fsp.Style.PadRight = 16;
      fsp.Style.MarginRight = 16;
      fsp.Style.PadTop = fsp.Style.PadBot = 3;
      fsp.Style.Alignment = UiAlignment.Right;
      fsp.Style.Border = 0;
      fsp.Style.Margin = 0;

      _timer = new System.Timers.Timer();
      _timer.Interval = 520;
      _timer.Elapsed += (e, x) =>
      {
        if (fsp != null && Gu.Context != null)
        {
          fsp.Text = StringUtil.FormatPrec(Gu.Context.FpsFrame, 1);

        }
      };
      _timer.AutoReset = true;//run once, then reset if Repeat is set
      _timer.Start();

      this.AddChild(fsp);
    }
    System.Timers.Timer _timer;
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
    public UiContextMenu() : base(UiStyleName.Label)
    {
      this.Style.BorderColor = vec4.rgba_ub(190, 190, 190);
      this.Style.Padding = 0;
      this.Style.Margin = 0;
      this.Style.Border = 0;
      this.Style.BorderBot = 1;
      this.Style.BorderTop = 1;
      this.Style.Color = vec4.rgba_ub(245, 245, 245, 255);
      this.Style.PositionMode = UiPositionMode.Absolute;
      this.Style.FloatMode = UiFloatMode.Floating;
      this.Style.MaxWidth = 500;
      this.Style.MinWidth = 10;
      this.Style.SizeModeWidth = UiSizeMode.Shrink;
      //   this.Style.    //OverflowMode = UiOverflowMode.Show;
      this.Style.SizeModeHeight = UiSizeMode.Shrink;
      this.Style.MultiplyColor = new vec4(1, 1);
      //context menu should have no pad or margin and wont need events
    }
  }//cls

  public class UiPanel : UiElement
  {
    public UiPanel()
    {
      this.Style.SizeModeWidth = UiSizeMode.Expand;
      this.Style.SizeModeHeight = UiSizeMode.Expand;
      this.Style.Padding = 0;
      this.Style.Margin = 10;
      this.Style.BorderRadius = 0;
      this.Style.FontFace = FontFace.Calibri;
      this.Style.FontSize = 16;
    }
  }

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
  public class UiScrollbar : UiControl
  {

  }
  public class UiSlider : UiControl
  {
    public double Value { get { return _value; } set { _value = value; UpdateValuesChanged(); } }
    public double MinValue { get { return _minvalue; } set { _minvalue = value; UpdateValuesChanged(); } }
    public double MaxValue { get { return _maxvalue; } set { _maxvalue = value; UpdateValuesChanged(); } }

    private double _value = 0;
    private double _minvalue = 0;
    private double _maxvalue = 100;
    private UiElement _lblMin;
    private UiElement _lblMax;
    private UiElement _lblVal;
    private UiElement _thumb;
    private Action<UiElement, double> _onValueChange;
    private bool _ismaxmin = false;

    private int _precision = 1;

    private UiLayoutOrientation _direction { get { return this.Style.LayoutOrientation.Value; } }

    private UiElement _labelRow;
    private UiElement _trackRow;

    public UiSlider(double leftval, double rightval, double defaultval, bool showValue, UiLayoutOrientation direction, Action<UiElement, double> onValueChange)
    {
      //allow for left/right to be less or equal, not necessarily LTR
      Gu.Assert(onValueChange != null);

      this.Style.LayoutOrientation = direction;
      //_direction = direction;
      _onValueChange = onValueChange;
      _minvalue = leftval;
      _maxvalue = rightval;
      _value = defaultval;


      Style.RenderMode = UiRenderMode.Color;
      Style.Margin = 2;
      Style.Border = 2;
      Style.BorderColorLeft = Style.BorderColorBot = OffColor.Charcoal;
      Style.BorderColorRight = Style.BorderColorTop = OffColor.VeryLightGray;

      _trackRow = new UiElement();
      _trackRow.Style.Margin = _trackRow.Style.Border = 0;
      AddChild(_trackRow);

      _labelRow = new UiElement();
      _labelRow.Style.Margin = _labelRow.Style.Border = 0;
      _labelRow.Style.BorderColorTop = OffColor.VeryLightGray;
      _labelRow.Style.MarginLeft = _labelRow.Style.MarginRight = 6;
      AddChild(_labelRow);

      _lblMin = new UiLabel(leftval.ToString());
      _lblMax = new UiLabel(rightval.ToString());
      _lblVal = new UiLabel(defaultval.ToString());
      _lblMin.Style.FontSize = _lblMax.Style.FontSize = _lblVal.Style.FontSize = 10;
      _lblMin.Style.FontColor = _lblMax.Style.FontColor = _lblVal.Style.FontColor = OffColor.MediumGray;
      _lblMin.Style.LayoutOrientation = _lblMax.Style.LayoutOrientation = _lblVal.Style.LayoutOrientation = UiLayoutOrientation.Horizontal;

      _labelRow.AddChild(_lblMin);
      _labelRow.AddChild(_lblMax);
      _labelRow.AddChild(_lblVal);



      if (showValue == false)
      {
        _labelRow.Hide();
      }

      _thumb = new UiControl();
      _thumb.Style.PositionMode = UiPositionMode.Relative;
      _thumb.Style.RenderMode = UiRenderMode.Color;
      _thumb.Style.BorderLeft = _thumb.Style.BorderBot = 1;
      _thumb.Style.BorderRight = _thumb.Style.BorderTop = 1;
      _thumb.Style.BorderColorLeft = _thumb.Style.BorderColorBot = OffColor.Charcoal;
      _thumb.Style.BorderColorRight = _thumb.Style.BorderColorTop = OffColor.VeryLightGray;
      _trackRow.AddChild(_thumb);

      UpdateHorV();

      UpdateValuesChanged();

      this.AddEvent(UiEventId.LmbPress, (e) =>
      {
        UpdateMovedThumb(e.State.MousePosCur);
      });
      this.AddEvent(UiEventId.Mouse_Move, (e) =>
      {
        if (e.State.Focused == this || e.State.Focused == _thumb)
        {
          UpdateMovedThumb(e.State.MousePosCur);
        }
      });
      _thumb.AddEvent(UiEventId.LmbDrag, (e) =>
      {
        UpdateMovedThumb(e.State.MousePosCur);
      });
    }
    private void UpdateHorV()
    {
      if (_direction == UiLayoutOrientation.Horizontal)
      {
        Style.SizeModeWidth = UiSizeMode.Fixed;
        Style.SizeModeHeight = UiSizeMode.Shrink;
        Style.Width = 200;
        Style.MinHeight = 20;
        _trackRow.Style.SizeModeWidth = UiSizeMode.Expand;
        _trackRow.Style.SizeModeHeight = UiSizeMode.Shrink;
        _trackRow.Style.MinHeight = 20;
        _trackRow.Style.MinWidth = 10;
        _labelRow.Style.SizeModeWidth = UiSizeMode.Expand;
        _labelRow.Style.SizeModeHeight = UiSizeMode.Shrink;
        _labelRow.Style.MinHeight = 10;
        _labelRow.Style.MinWidth = 10;
        _labelRow.Style.BorderTop = 1;
        _lblMin.Style.Alignment = UiAlignment.Left;
        _lblMax.Style.Alignment = UiAlignment.Right;
        _lblVal.Style.Alignment = UiAlignment.Center;
        _thumb.Style.SizeModeWidth = UiSizeMode.Fixed;
        _thumb.Style.SizeModeHeight = UiSizeMode.Expand;
        _thumb.Style.MinHeight = 19;
        _thumb.Style.Width = 12;
      }
      else if (_direction == UiLayoutOrientation.Vertical)
      {
        Style.SizeModeWidth = UiSizeMode.Shrink;
        Style.SizeModeHeight = UiSizeMode.Fixed;
        Style.Height = 200;
        Style.MinWidth = 20;
        _trackRow.Style.SizeModeWidth = UiSizeMode.Shrink;
        _trackRow.Style.SizeModeHeight = UiSizeMode.Expand;
        _trackRow.Style.MinWidth = 20;
        _trackRow.Style.MinHeight = 10;
        _labelRow.Style.SizeModeWidth = UiSizeMode.Shrink;
        _labelRow.Style.SizeModeHeight = UiSizeMode.Expand;
        _labelRow.Style.MinHeight = 10;
        _labelRow.Style.MinWidth = 10;
        _labelRow.Style.BorderRight = 1; // Label to left
        _lblMin.Style.Alignment = UiAlignment.Left;
        _lblMax.Style.Alignment = UiAlignment.Left;
        _lblVal.Style.Alignment = UiAlignment.Left;
        _thumb.Style.SizeModeWidth = UiSizeMode.Expand;
        _thumb.Style.SizeModeHeight = UiSizeMode.Fixed;
        _thumb.Style.MinWidth = 19;
        _thumb.Style.Height = 12;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

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

      _lblVal.Text = StringUtil.FormatPrec(_value, _precision);

      SetThumbToValue(_value);
    }
    private void SetThumbToValue(double value)
    {
      Gu.Assert(_thumb != null);
      Gu.Assert(_lblVal != null);
      Gu.Assert(_thumb.Parent != null);

      double valw = _thumb.Parent._quads._b2BorderQuad.LSize(_direction) - _thumb._quads._b2BorderQuad.LSize(_direction);

      value = Math.Clamp(value, _minvalue, _maxvalue);

      var pct = value / (_maxvalue - _minvalue);
      if (_ismaxmin)
      {
        pct = 1 - pct;
      }
      _thumb.Style.LMin(_direction, (float)(pct * valw));
    }
    private void UpdateMovedThumb(vec2 mpos)
    {
      Gu.Assert(_thumb != null);
      Gu.Assert(_lblVal != null);
      Gu.Assert(_thumb.Parent != null);

      float mp = _direction == UiLayoutOrientation.Horizontal ? mpos.x : mpos.y;

      _thumb.Style.LMin(_direction, mp - _thumb.Parent._quads._b2BorderQuad.LMin(_direction) - _thumb._quads._b2BorderQuad.LSize(_direction) / 2);

      double valw = _thumb.Parent._quads._b2BorderQuad.LSize(_direction) - _thumb._quads._b2BorderQuad.LSize(_direction);

      if (_thumb.Style.LMin(_direction) < 0)
      {
        _thumb.Style.LMin(_direction, 0);
      }
      else if (_thumb.Style.LMin(_direction) > valw)
      {
        _thumb.Style.LMin(_direction, (float)valw);
      }

      var pct = (double)_thumb.Style.LMin(_direction).Value / valw;

      if (_ismaxmin)
      {
        pct = 1 - pct;
      }

      _value = _minvalue + (_maxvalue - _minvalue) * pct;
      _value = Math.Clamp(_value, _minvalue, _maxvalue);

      _lblVal.Text = StringUtil.FormatPrec(_value, 1).ToString();

      _onValueChange?.Invoke(this, Value);
    }


  }//slider

}//ns