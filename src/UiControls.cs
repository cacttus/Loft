
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
      AddEvent(UiEventId.MousePress, (e) =>
      {
        _hasClickFocus = true;
        Style.ColorMul = new vec4(0.8f, 1.0f);
      });
      AddEvent(UiEventId.MouseRelease, (e) =>
      {
        _hasClickFocus = false;
        Style.ColorMul = new vec4(1.1f, 1.0f);
      });
    }
  }
  public class UiMenuItem : UiControl
  {
    public override string NamingPrefix { get { return "mnu"; } }
    private UiContextMenu _container = null;
    private bool _isTopLevel = true;

    public UiMenuItem(Phrase p = Phrase.None) : this(Gu.Translator.Translate(p))
    {
    }
    public UiMenuItem(string text) : base(StyleName.MenuItem, text)
    {
      Init();
    }
    public new UiMenuItem Click(UiAction f)
    {
      AddEvent(UiEventId.MouseRelease, f);
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
      if (_container == null)
      {
        _container = new UiContextMenu();
        _container.Hide();
        AddChild(_container, Gui2d.c_ContextMenuSort);
      }
      item._isTopLevel = false;
      item.Style.DisplayMode = UiDisplayMode.Block;
      _container.AddChild(item);
      return item;
    }
    public void Collapse()
    {
      //Collapse all menus
      if (Parent != null && Parent is UiContextMenu)
      {
        if (Parent.Parent != null && Parent.Parent is UiMenuItem)
        {
          (Parent.Parent as UiMenuItem).Collapse();
        }
      }
      ShowContextMenu(false);
    }
    private void Init()
    {
      
      this.Style.DisplayMode = UiDisplayMode.Inline;

      var that = this;
      this.AddEvent(UiEventId.Mouse_Move, (e) =>
      {
        if (e.State.LeftButtonState == ButtonState.Hold)
        {
          ShowContextMenu(true);
        }
      });
      this.AddEvent(UiEventId.Mouse_Enter, (e) =>
      {
        if (e.State.LeftButtonState == ButtonState.Hold)
        {
          ShowContextMenu(true);
        }
      });
      this.AddEvent(UiEventId.MousePress, (e) =>
      {
      });
      this.AddEvent(UiEventId.Mouse_Leave, (e) =>
      {
        if (e.State.Current is UiMenuItem || e.State.Current is UiContextMenu)
        {
          //direct menu ancestor - 
          //state.current is within parent.parent.parent's children parent = context menu, parent = menu item, parent = context menu
          if (Parent != null && Parent.Parent != null && Parent.Parent.Parent != null)
          {
            bool found = false;
            Parent.Parent.Parent.IterateChildrenRaw((x) =>
            {
              if (x == e.State.Current)
              {
                found = true;
                return LambdaBool.Break;
              }
              return LambdaBool.Continue;
            });
            if (found)
            {
              ShowContextMenu(false);
            }
          }

          if ((e.State.Current is UiMenuItem || e.State.Current is UiContextMenu) && e.State.Current.TreeDepth <= this.TreeDepth)
          {
            //went up a menu, or to a sibling item
            ShowContextMenu(false);
          }

        }
      });
      this.AddEvent(UiEventId.MouseRelease, (e) =>
      {
        Collapse();
      });
    }
    private void ShowContextMenu(bool show)
    {
      if (_container != null)
      {
        if (!show)
        {
          if (_container.Visible == true)
          {
            _container.Hide();
          }
        }
        else
        {
          if (_container.Visible == false)
          {
            if (_isTopLevel)
            {
              _container.Style.Top = this.LocalQuad.Bottom;
            }
            else
            {
              _container.Style.Top = this.LocalQuad.Top;
              _container.Style.Left = this.LocalQuad.Right;
            }
            _container.Show();
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
      this.Style.SizeModeWidth= UiSizeMode.Shrink;
      this.Style.PositionMode = UiPositionMode.Static;
      
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
      //if the context menu area has no padding these events should not get called.
      this.AddEvent(UiEventId.Mouse_Leave, (e) =>
      {
        //hide if we leave for an element that is not a descendnt of this element
        if (!
            (e.State.Current != null &&
            (e.State.Current is UiMenuItem || e.State.Current is UiContextMenu) &&
            e.State.Current.TreeDepth > this.TreeDepth)
            )
        {
          if (Parent != null && Parent is UiMenuItem)
          {
            (Parent as UiMenuItem).Collapse();
          }
        }
      });
      this.AddEvent(UiEventId.MouseRelease, (e) =>
      {
        if (Parent != null && Parent is UiMenuItem)
        {
          (Parent as UiMenuItem).Collapse();
        }
      });
    }
  }//cls


}//ns