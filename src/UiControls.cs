
namespace PirateCraft
{
  using UiAction = System.Action<UiEvent>;

  public class UiControl : UiElement
  {
    private bool _hasFocus = false;//pick focus
    private bool _hasClickFocus = false;//user holding  mouse button down

    //base class for controls button/thumb/scrollbar/knob/trackbar.. etc
    public UiControl()
    {
      RegisterStyleEvents();
    }
    public UiControl(string style, string text) : base(style, text)
    {
      RegisterStyleEvents();

    }
    private void RegisterStyleEvents()
    {
      AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        _hasFocus = true;
        Style.ColorMul = new vec4(1.1f, 1.0f);
      });
      AddEvent(UiEventId.Mouse_Leave, (e) =>
      {
        _hasFocus = false;
        Style.ColorMul = new vec4(1.0f, 1.0f);
      });
      AddEvent(UiEventId.LmbPress, (e) =>
      {
        _hasClickFocus = true;
        Style.ColorMul = new vec4(0.82f, 1.0f);
      });
      AddEvent(UiEventId.LmbRelease, (e) =>
      {
        _hasClickFocus = false;
        Style.ColorMul = new vec4(1.1f, 1.0f);
      });
    }
  }
  public class UiMenuItem : UiControl
  {
    public override string NamingPrefix { get { return "mnu"; } }
    private UiContextMenu _contextMenu = null;
    private bool _isTopLevel = true;

    public static UiMenuItem HBar()
    {
      var mn = new UiMenuItem();
      mn.Style.InheritFrom(StyleName.HBar);
      return mn;
    }
    public UiMenuItem AddHBar()
    {
      return (UiMenuItem)AddChild(HBar());
    }
    public UiMenuItem(Phrase p = Phrase.None) : this(Gu.Translator.Translate(p))
    {
    }
    public UiMenuItem(string text) : base(StyleName.MenuItem, text)
    {
      Init();
    }
    public new UiMenuItem Click(UiAction f)
    {
      AddEvent(UiEventId.LmbRelease, f);
      return this;
    }
    public UiMenuItem AddMenuItems(UiMenuItem e1, UiMenuItem e2,
      UiMenuItem? e3 = null, UiMenuItem? e4 = null, UiMenuItem? e5 = null, UiMenuItem? e6 = null,
      UiMenuItem? e7 = null, UiMenuItem? e8 = null, UiMenuItem? e9 = null, UiMenuItem? e10 = null,
      UiMenuItem? e11 = null, UiMenuItem? e12 = null, UiMenuItem? e13 = null, UiMenuItem? e14 = null)
    {
      if (e1 != null) AddMenuItem(e1);
      if (e2 != null) AddMenuItem(e2);
      if (e3 != null) AddMenuItem(e3);
      if (e4 != null) AddMenuItem(e4);
      if (e5 != null) AddMenuItem(e5);
      if (e6 != null) AddMenuItem(e6);
      if (e7 != null) AddMenuItem(e7);
      if (e8 != null) AddMenuItem(e8);
      if (e9 != null) AddMenuItem(e9);
      if (e10 != null) AddMenuItem(e10);
      if (e11 != null) AddMenuItem(e11);
      if (e12 != null) AddMenuItem(e12);
      if (e13 != null) AddMenuItem(e13);
      if (e14 != null) AddMenuItem(e14);

      return this;
    }
    public UiMenuItem AddMenuItem(UiMenuItem item)
    {
      Gu.Assert(item != null);
      if (_contextMenu == null)
      {
        _contextMenu = new UiContextMenu();
        _contextMenu.Hide();
        AddChild(_contextMenu);
      }
      item._isTopLevel = false;
      item.Style.DisplayMode = UiDisplayMode.Block;
      _contextMenu.AddChild(item);
      return this;
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
        else if (e.State.LeftButtonState != ButtonState.Hold &&  !(e.State.PressFocus is UiMenuItem))
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
              _contextMenu.Style.Top = this.FinalQuad.Top;
              _contextMenu.Style.Left = Parent.FinalQuad.Right;
            }
            _contextMenu.Show();
          }
        }
      }
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
      this.Style.MinWidth = 80;

      this.AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        if (Parent != null)
        {
          foreach (var c in Parent.Children)
          {
            if(c!= this){
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

    public UiToolbar() : base(StyleName.Toolbar)
    {
    }
    public void AddVBar()
    {
      AddChild(StyleName.VerticalBar).Name = "vbar";
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
    public UiContextMenu() : base(new List<string>() { StyleName.ContextMenu })
    {
      //context menu should have no pad or margin and wont need events
    }
  }//cls


}//ns