
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
    public UiControl() { }
    public UiControl(bool registerStyleEvents)
    {
      if (registerStyleEvents)
      {
        RegisterStyleEvents();
      }
      //SetControlStyle();//this screws up inheritance Set manually if needed
    }
    protected void SetControlStyle()
    {
//      this.AddStyle(UiStyleName.Control);
      
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
      this.Style.RenderMode = UiRenderMode.Color;
      this.Style.FontColor = vec4.rgba_ub(42, 42, 42, 255);
      this.Style.Color = vec4.rgba_ub(230, 230, 230, 255);
      this.Style.BorderColorLeft = this.Style.BorderColorBot = OffColor.Charcoal;
      this.Style.BorderColorRight = this.Style.BorderColorTop = OffColor.VeryLightGray;
    }
    protected void RegisterStyleEvents()
    {
      AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        if (e.State.Focused != this)
        {
          Style.MultiplyColor = new vec4(1.08f, 1.0f);
        }
      });
      AddEvent(UiEventId.Mouse_Leave, (e) =>
      {
        if (e.State.Focused != this)
        {
          Style.MultiplyColor = vec4.One;
        }
      });
      AddEvent(UiEventId.LmbPress, (e) =>
      {
        Style.MultiplyColor = new vec4(0.82f, 1.0f);
      });
      AddEvent(UiEventId.Lost_Focus, (e) =>
      {
        Style.MultiplyColor = vec4.One;
      });
      AddEvent(UiEventId.LmbRelease, (e) =>
      {
        Style.MultiplyColor = vec4.One;
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
    public UiMenuItem(string text, Action<UiEvent>? act = null) : base(true)
    {
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
        var shortcut = new UiLabel(text);
        shortcut.Style.PositionMode = UiPositionMode.Static;
        shortcut.Style.SizeModeWidth = UiSizeMode.Shrink;
        shortcut.Style.SizeModeHeight = UiSizeMode.Expand;
        shortcut.Style.DisplayMode = UiDisplayMode.InlineNoWrap;
        shortcut.Style.Alignment = UiAlignment.Right;
        shortcut.Style.TextAlign = UiAlignment.Right;
        shortcut.Style.Border = 0;
        shortcut.Style.Margin = 0;
        shortcut.Style.Padding = 0;
        this.AddChild(shortcut);
      }
    }
    private void Init(string text)
    {
      this.Style.FontSize = 16;
      this.Style.DisplayMode = UiDisplayMode.Inline;
      this.Style.BorderLeft = 1;
      this.Style.BorderRight = 1;
      this.Style.BorderColor = vec4.rgba_ub(190, 190, 190);
      this.Style.SizeModeWidth = UiSizeMode.Expand;
      this.Style.SizeModeHeight = UiSizeMode.Shrink;
      this.Style.PositionMode = UiPositionMode.Static;
      this.Style.MaxWidth = 600;
      this.Style.MinWidth = 0;
      this.Style.MarginTop = this.Style.MarginBot = 5;
      this.Style.MarginLeft = this.Style.MarginRight = 10;
      this.Style.Padding = 0;//Do not use padding


      _label = new UiLabel(text);
      _label.Style.SizeModeWidth = UiSizeMode.Shrink;
      _label.Style.SizeModeHeight = UiSizeMode.Shrink;
      _label.Style.PositionMode = UiPositionMode.Static;
      _label.Style.DisplayMode = UiDisplayMode.InlineNoWrap;
      _label.Style.MinWidth = 100;
      _label.Style.Margin = _label.Style.Border = _label.Style.Padding = 0;
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
            if (_isTopLevel)
            {
              _contextMenu.Style.Top = this.FinalQuad.Bottom;
              _contextMenu.Style.Left = this.FinalQuad.Left;
            }
            else
            {
              _contextMenu.Style.Top = this.FinalQuad._top - _contextMenu.Style._props.BorderTop;
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
    public UiLabel(){}
    public UiLabel(Phrase p) : this(Gu.Translator.Translate(p))
    {
    }
    public UiLabel(string text) : base(false)
    {
      this.Text = text;
      this.Style.RenderMode = UiRenderMode.None;
this.Style.SizeModeWidth = UiSizeMode.Shrink;
this.Style.SizeModeHeight = UiSizeMode.Shrink;
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
    public UiToast(string text) : base(true)
    {
      this.Name = "_lblToast";
      //toast at the top corner
      this.Text = text;
      this.Style.Margin = 20;
      this.Style.RenderMode = UiRenderMode.Color;
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
    public UiScrollbar() : base(false) { }

  }

  public class UiSlider : UiControl
  {
    public enum LabelDisplay
    {
      Outside,
      Inside,
      None
    }
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
    private float _length = 200;
    private float _thickness = 20;
    private float _thumbsize = 12;

    private UiLayoutOrientation _direction { get { return this.Style.LayoutOrientation.Value; } }

    private UiElement _labelRow;
    private UiElement _trackRow;
    private LabelDisplay _labelDisplay;

    public UiSlider(double leftval, double rightval, double defaultval, int length, LabelDisplay labeldisply, UiLayoutOrientation direction, Action<UiElement, double> onValueChange)
    {
      //allow for left/right to be less or equal, not necessarily LTR
      Gu.Assert(onValueChange != null);

      _onValueChange = onValueChange;
      _minvalue = leftval;
      _maxvalue = rightval;
      _value = defaultval;
      _labelDisplay = labeldisply;
      _length = length;

      SetControlStyle();

      this.Style.LayoutOrientation = direction;
      this.Style.RenderMode = UiRenderMode.Color;
      this.Style.Margin = 0;
      this.Style.Border = 0;
      this.Style.Color = OffColor.ControlColor * 0.96f;
      this.Style.BorderColor = OffColor.Charcoal;
      this.Style.FontSize = 14;
      this.Style.FontColor = new vec4(OffColor.DarkGray.rgb, 0.75f);

      _trackRow = new UiElement();
      _trackRow.Style.RenderMode = UiRenderMode.None;
      AddChild(_trackRow);

      _thumb = new UiControl(true);
      _thumb.Style.PositionMode = UiPositionMode.Relative;
      _thumb.Style.RenderMode = UiRenderMode.Color;
      _thumb.Style.BorderLeft = _thumb.Style.BorderBot = 0;
      _thumb.Style.BorderRight = _thumb.Style.BorderTop = 0;
      _thumb.Style.Color = OffColor.ControlColor * 1.03f;
      _thumb.Style.BorderColorLeft = _thumb.Style.BorderColorBot = OffColor.Charcoal;
      _thumb.Style.BorderColorRight = _thumb.Style.BorderColorTop = OffColor.VeryLightGray;
      _trackRow.AddChild(_thumb);

      UpdateLabelDisplayMode();
      UpdateStyle();
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
    private void UpdateLabelDisplayMode()
    {
      if (_labelDisplay == LabelDisplay.Outside)
      {
        _labelRow = new UiElement();
        _labelRow.Style.RenderMode = UiRenderMode.None;
        _labelRow.Style.Margin = _labelRow.Style.Border = 0;
        _labelRow.Style.BorderColorTop = OffColor.VeryLightGray;
        _labelRow.Style.MarginLeft = _labelRow.Style.MarginRight = 0;

        _lblMin = new UiLabel(_minvalue.ToString());
        _lblMax = new UiLabel(_maxvalue.ToString());
        _lblVal = new UiLabel(_value.ToString());

        _labelRow.AddChild(_lblMin);
        _labelRow.AddChild(_lblMax);
        _labelRow.AddChild(_lblVal);
        AddChild(_labelRow);
      }
      else if (_labelDisplay == LabelDisplay.Inside)
      {
        _lblVal = new UiLabel(_value.ToString());
        _trackRow.AddChild(_lblVal);
      }
      else if (_labelDisplay == LabelDisplay.None)
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
      if (_direction == UiLayoutOrientation.Horizontal)
      {
        Style.SizeModeWidth = UiSizeMode.Fixed;
        Style.SizeModeHeight = UiSizeMode.Shrink;
        Style.Width = _length;
        Style.MinHeight = _thickness;

        _trackRow.Style.SizeModeWidth = UiSizeMode.Expand;
        _trackRow.Style.SizeModeHeight = UiSizeMode.Shrink;
        _trackRow.Style.MinHeight = _thickness;
        _trackRow.Style.MinWidth = 10;

        if (_labelDisplay == LabelDisplay.Outside)
        {
          _labelRow.Style.SizeModeWidth = UiSizeMode.Expand;
          _labelRow.Style.SizeModeHeight = UiSizeMode.Shrink;
          _labelRow.Style.MinHeight = 10;
          _labelRow.Style.MinWidth = 10;
          _labelRow.Style.BorderTop = 1;
          _lblMin.Style.Alignment = UiAlignment.Left;
          _lblMax.Style.Alignment = UiAlignment.Right;
          _lblVal.Style.Alignment = UiAlignment.Center;
        }
        else if (_labelDisplay == LabelDisplay.Inside)
        {
          _lblVal.Style.Alignment = UiAlignment.Center;
          _lblVal.Style.Height = _thickness; 
        }

        _thumb.Style.SizeModeWidth = UiSizeMode.Fixed;
        _thumb.Style.SizeModeHeight = UiSizeMode.Expand;
        _thumb.Style.MinHeight = _thickness;
        _thumb.Style.Width = _thumbsize;
      }
      else if (_direction == UiLayoutOrientation.Vertical)
      {
        Style.SizeModeWidth = UiSizeMode.Shrink;
        Style.SizeModeHeight = UiSizeMode.Fixed;
        Style.Height = _length;
        Style.MinWidth = _thickness;

        _trackRow.Style.SizeModeWidth = UiSizeMode.Shrink;
        _trackRow.Style.SizeModeHeight = UiSizeMode.Expand;
        _trackRow.Style.MinWidth = _thickness;
        _trackRow.Style.MinHeight = 10;

        if (_labelDisplay == LabelDisplay.Outside)
        {
          _labelRow.Style.SizeModeWidth = UiSizeMode.Shrink;
          _labelRow.Style.SizeModeHeight = UiSizeMode.Expand;
          _labelRow.Style.MinHeight = 10;
          _labelRow.Style.MinWidth = 10;
          _lblMin.Style.Alignment = UiAlignment.Left;
          _lblMax.Style.Alignment = UiAlignment.Right;
          _lblVal.Style.Alignment = UiAlignment.Center;
        }
        else if (_labelDisplay == LabelDisplay.Inside)
        {
          _lblVal.Style.Alignment = UiAlignment.Center;
          _lblVal.Style.Width = _thickness; 
        }

        _thumb.Style.SizeModeWidth = UiSizeMode.Expand;
        _thumb.Style.SizeModeHeight = UiSizeMode.Fixed;
        _thumb.Style.MinWidth = _thickness;
        _thumb.Style.Height = _thumbsize;
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      //make sure text doesn't bloop.
      if (_lblMin != null) { _lblMin.Style.LayoutOrientation = UiLayoutOrientation.Horizontal; }
      if (_lblMax != null) { _lblMax.Style.LayoutOrientation = UiLayoutOrientation.Horizontal; }
      if (_lblVal != null) { _lblVal.Style.LayoutOrientation = UiLayoutOrientation.Horizontal; }
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
      UpdateLabels();
    }
    private void SetThumbToValue(double value)
    {
      Gu.Assert(_thumb != null);
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


  }//slider

}//ns