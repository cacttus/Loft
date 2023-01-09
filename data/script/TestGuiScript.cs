
using System.Collections;
using System.Collections.Generic;

namespace Loft
{
  public class TestGuiScript : IUIScript
  {
    public TestGuiScript() { }
    public string GetName() { return GetType().Name; }

    public UiElement _hoverinfo = null;

    public void OnUpdate(RenderView rv)
    {
      //called after cull + after pick
      if (_hoverinfo != null)
      {
        if (Gu.Context.Picker.PickedUiElement != null)
        {
          _hoverinfo.Text = Gu.Context.Picker.PickedUiElement.Name;
        }
        else
        {
          _hoverinfo.Text = "<none>";
        }
        _hoverinfo.Text += Gu.Context.Picker.SelectedPixelIdLast;
      }
    }
    public void OnCreate(IGui2d gg)
    {
      //****************************************
      var g = gg as Gui2d;
      //****************************************

      // FPS_LABEL(g);  
       HOVER_INFO(g);

      //****************************************

      // var e = new UiElement();
      // e.Text = "Hi";
      // e.Style.FontSize = 30;
      // e.Style.Color = OffColor.Gray;
      // g.AddChild(e);

      TEST_WINDOW(g);

       TEST_AUTOS_H(g);
      TEST_AUTOS_V(g);
      TEST_AUTO_SIB(g);
         TEST_AUTO_NEST(g);

      //TEST_MENU_ITEM(g);
      //TEST_TOOLBAR_BUTTON(g);
      //  TEST_SLIDER(g);
      //TEST_ALIGNMENT(g, UiLayoutDirection.LeftToRight);
      //TEST_ALIGNMENT(g, UiLayoutDirection.RightToLeft);
      //TEST_TEXT(g);

      //TEST_NESTED_BORDERS(g);

      //****************************************
    }
    private static void TEST_WINDOW(Gui2d g)
    {
      var w = new UiWindow("THIS IS A WINDOW", new vec2(50, 50), new vec2(100, 100), UiWindow.UiWindowStyle.Sizable);

      var e = new UiElement("TestGreenBox");
      e.Style.FixedHeight = e.Style.FixedWidth = 100;
      e.Style.Color = OffColor.LawnGreen;
      w.Content.AddChild(e);
      var e2 = new UiElement("TestRedBox");
      e2.Style.FixedHeight = e2.Style.FixedWidth = 100;
      e2.Style.Color = OffColor.MediumVioletRed;
      w.Content.AddChild(e2);

      //new UiText(Ipsum.GetIpsum(2))

      g.AddChild(w);
    }
    public static void TEST_MENU_ITEM(Gui2d g)
    {
      var tb = new UiToolbar();
      tb.Style.Margin = 4;
      tb.Style.FontSize = 30;
      var mi = new UiMenuItem("Hello World");
      mi.Style.MaxWidth = 300;
      mi.CreateShortcut("Ctrl+F");
      mi.AddSubMenu("SubMenu").AddItem("Hi");
      tb.AddChild(mi);
      g.AddChild(tb);
    }
    public static void TEST_TOOLBAR_BUTTON(Gui2d g)
    {
      var tb = new UiToolbar();
      tb.Style.Margin = 4;
      tb.Style.FontSize = 30;
      var mi = new UiToolbarButton("Hello World");
      var sub = mi.AddItem("asdlkfj asdflk fdssdklfj sdf32lka;");
      sub.CreateShortcut("Ctrl+1,2");
      mi.AddItem("asdlkfj");
      mi.AddItem("asdj");
      mi.AddItem("kfj3423");
      tb.AddChild(mi);
      g.AddChild(tb);
    }
    public static void TEST_SLIDER(Gui2d g)
    {
      g.AddChild(new UiSlider(0, 100, 50, UiSlider.LabelDisplayMode.Inside, UiOrientation.Horizontal, (e, d) =>
      {
        Gu.Log.Info("Hiya " + StringUtil.FormatPrec(d, 1));
      }
      , "testslider"));

      g.LastChild().Style.FixedHeight = 100;
      g.LastChild().Style.PositionMode = UiPositionMode.Fixed;
      g.LastChild().Style.Top = 200;
      g.LastChild().Style.Left = 200;


      //g.LastChild().Style.MinHeight = 100;
      //g.LastChild().Style.MinWidth= 100;

      //((UiSlider)g.LastChild()).LabelDisplay = UiSlider.LabelDisplayMode.None;
    }
    public static void TEST_ALIGNMENT(Gui2d g, UiLayoutDirection dir)
    {
      //test left center right
      var par = ParentBox();
      par.Style.FixedWidth = 600;
      par.Style.FixedHeight = 350;
      float pwh = 33.333333f;

      float fontsize = 20;

      for (int y = 0; y < 3; y++)
      {
        for (int x = 0; x < 3; x++)
        {
          string text = ((UiAlignment)x).ToString() + "," + ((UiAlignment)y).ToString();
          par.AddChild(ChildBox("", text + "Parent"));
          par.LastChild().Style.PercentWidth = par.LastChild().Style.PercentHeight =
          par.LastChild().Style.PercentWidth = par.LastChild().Style.PercentHeight = pwh;
          par.LastChild().Style.ContentAlignX = (UiAlignment)x;
          par.LastChild().Style.ContentAlignY = (UiAlignment)y;
          par.LastChild().Text = text;
          par.LastChild().Style.LayoutDirection = dir;
          par.LastChild().Style.FontSize = fontsize;
        }
      }
      par.Style.DisplayMode = UiDisplayMode.Inline;

      g.AddChild(par);

    }
    public static void FPS_LABEL(Gui2d g)
    {
      //toolbar
      //var tt = new UiToolbar();
      //var bt = new UiToolbarButton("hi");
      //bt.AddSubMenu("hi there").AddSubMenu("Yes, hi, hello!");
      //g.AddChild(tt);

      //**Auto is pooping out
      var e = new UiElement();
      e.AddChild(new FPSLabel());

      e.Style.PercentWidth = 100;
      e.Style.SizeModeHeight = UiSizeMode.Content;
      e.Style.ContentAlignX = UiAlignment.Right;

      e.LastChild().Style.FixedWidth = 80;
      e.LastChild().Style.MinHeight = 5;
      e.LastChild().Style.SizeModeWidth = UiSizeMode.Content;
      e.LastChild().Style.SizeModeHeight = UiSizeMode.Content;

      g.AddChild(e);
    }
    public void HOVER_INFO(Gui2d g)
    {
      var e = new UiElement();
      _hoverinfo = new UiText("<none>", "hover_info");
      e.AddChild(_hoverinfo);
      e.Style.Color = OffColor.Beige;
      e.Style.DisplayMode = UiDisplayMode.Block;
      e.Style.MinWidth = 100;
      e.Style.MinHeight = 20;
      e.Style.FontSize = 30;
      e.Style.SizeModeWidth = UiSizeMode.Content;
      e.Style.SizeModeHeight = UiSizeMode.Content;
      e.Style.ContentAlignX = UiAlignment.Left;

      g.AddChild(e);
      g.AddChild(UiUtils.LineBreak());
    }
    public static void TEST_TEXT(Gui2d g)
    {
      var lab = TestBorderLabel();
      lab.Text = Ipsum.GetIpsum(1);
      lab.Style.Min = 10;
      lab.Style.Max = 700;
      lab.Style.Tracking = 1f;
      lab.Style.FontSize = 20;
      lab.Style.Padding = 20;
      lab.Style.Margin = 5;
      lab.Style.Color = OffColor.FloralWhite;
      lab.Style.BorderColor = OffColor.LightSlateGray;
      lab.Style.Border = 5;
      lab.Style.OverflowMode = UiOverflowMode.Content;
      lab.Style.BorderRadius = 12;
      lab.Style.FontWeight = 1.1f;
      lab.Style.FontFace = FontFace.Parisienne;
      //lab.Style.FontFace = FontFace.Parisienne;
      lab.Style.FontColor = OffColor.Charcoal;
      lab.Style.ContentAlignX = UiAlignment.Left;
      lab.Style.ContentAlignY = UiAlignment.Left;
      lab.Style.LayoutDirection = UiLayoutDirection.LeftToRight;

      lab.Style.SizeMode = UiSizeMode.Content;
      //      lab.Style.FixedHeight = 600;
      //lab.Style.ContentAlignY = UiAlignment.Center;

      g.AddChild(lab);
    }
    public static void TEST_AUTO_SIB(Gui2d g)
    {
      //[Label][auto][Label]
      //should not wrap
      var par = ParentBox();
      par.Style.FixedWidth = 300;
      par.Style.FixedHeight = 200;
      //    par.Style.SizeMode = UiSizeMode.Shrink;
      par.Style.AutoModeWidth = UiAutoMode.Content;
      par.Style.AutoModeHeight = UiAutoMode.Content;
      par.AddChild(TLab("Hello"));

      par.AddChild(ChildBox());
      par.LastChild().Style.SizeModeWidth = UiSizeMode.Auto;
      par.LastChild().Style.SizeModeHeight = UiSizeMode.Auto;
      //par.LastChild().Style.FixedHeight = 30;
      //par.LastChild().Style.MinWidth = 20;
      par.AddChild(TLab("World"));

      g.AddChild(par);
    }
    public static void TEST_AUTO_NEST(Gui2d g)
    {
      //should not wrap
      var par = ParentBox();
      par.Style.FixedWidth = 600;
      par.Style.FixedHeight = 400;
      //    par.Style.SizeMode = UiSizeMode.Shrink;
      par.Style.AutoModeWidth = UiAutoMode.Content;
      par.Style.AutoModeHeight = UiAutoMode.Content;

      var c1 = ChildBox();
      par.AddChild(c1);
      par.LastChild().Text = "AUTO 1";
      par.LastChild().Style.SizeModeWidth = UiSizeMode.Auto;
      par.LastChild().Style.SizeModeHeight = UiSizeMode.Auto;

      var c2 = ChildBox();
      par.AddChild(c2);
      par.LastChild().Text = "AUTO 2";
      par.LastChild().Style.SizeModeWidth = UiSizeMode.Auto;
      par.LastChild().Style.SizeModeHeight = UiSizeMode.Auto;
      par.LastChild().Style.Color = OffColor.Plum;

      c1.AddChild(ChildBox("INNER 1", "inner_auto", new vec4(0.5, 0.5, 0.8, 1)));
      c1.LastChild().Style.SizeModeWidth = UiSizeMode.Auto;
      c1.LastChild().Style.SizeModeHeight = UiSizeMode.Auto;
      c1.LastChild().Style.Color = OffColor.Turquoise;

      c1.LastChild().AddChild(ChildBox("SUB 1", "inner_auto", new vec4(0.95, 0.5, 0.8, 1)));
      c1.LastChild().LastChild().Style.SizeModeWidth = UiSizeMode.Auto;
      c1.LastChild().LastChild().Style.SizeModeHeight = UiSizeMode.Auto;
      c1.LastChild().LastChild().Style.Color = OffColor.SpringGreen;

      c1.LastChild().AddChild(ChildBox("SUB 2", "inner_auto", new vec4(0.95, 0.5, 0.8, 1)));
      c1.LastChild().LastChild().Style.SizeModeWidth = UiSizeMode.Auto;
      c1.LastChild().LastChild().Style.SizeModeHeight = UiSizeMode.Auto;
      c1.LastChild().LastChild().Style.Color = OffColor.Brown;

      c1.LastChild().AddChild(ChildBox("PCTLINE 1", "inner_auto", new vec4(0.95, 0.5, 0.8, 1)));
      c1.LastChild().LastChild().Style.PercentWidth = 100;
      c1.LastChild().LastChild().Style.SizeModeHeight = UiSizeMode.Auto;
      c1.LastChild().LastChild().Style.DisplayMode = UiDisplayMode.Block;
      c1.LastChild().LastChild().Style.Color = OffColor.AliceBlue;

      c1.LastChild().AddChild(ChildBox("PCTLINE 2", "inner_auto", new vec4(0.95, 0.5, 0.8, 1)));
      c1.LastChild().LastChild().Style.PercentWidth = 100;
      c1.LastChild().LastChild().Style.SizeModeHeight = UiSizeMode.Auto;
      c1.LastChild().LastChild().Style.DisplayMode = UiDisplayMode.Block;
      c1.LastChild().LastChild().Style.Color = OffColor.Firebrick;


      c1.LastChild().AddChild(ChildBox("SUB 1", "inner_auto", new vec4(0.95, 0.5, 0.8, 1)));
      c1.LastChild().LastChild().Style.SizeModeWidth = UiSizeMode.Auto;
      c1.LastChild().LastChild().Style.DisplayMode = UiDisplayMode.Block;
      c1.LastChild().LastChild().Style.SizeModeHeight = UiSizeMode.Auto;
      c1.LastChild().LastChild().Style.Color = OffColor.DarkCyan;

      c1.LastChild().AddChild(ChildBox("SUB 2", "inner_auto", new vec4(0.95, 0.5, 0.8, 1)));
      c1.LastChild().LastChild().Style.SizeModeWidth = UiSizeMode.Auto;
      c1.LastChild().LastChild().Style.SizeModeHeight = UiSizeMode.Auto;
      c1.LastChild().LastChild().Style.Color = OffColor.MediumSeaGreen;

      c1.AddChild(ChildBox("INNER 2", "inner_auto", new vec4(0.5, 0.5, 0.8, 1)));
      c1.LastChild().Style.SizeModeWidth = UiSizeMode.Auto;
      c1.LastChild().Style.SizeModeHeight = UiSizeMode.Auto;

      //par.LastChild().Style.FixedHeight = 30;
      //par.LastChild().Style.MinWidth = 20;

      g.AddChild(par);
    }
    public static UiElement TLab(string text)
    {
      var e = new UiElement();
      e.Text = text;
      e.Style.FontSize = 20;
      e.Style.FontColor = OffColor.Honeydew;
      e.Style.Border = 1;
      e.Style.BorderColor = OffColor.LimeGreen;
      return e;
    }
    public static void TEST_AUTOS_V(Gui2d g)
    {
      var par = ParentBox();
      par.Style.FixedWidth = 600;
      par.Style.FixedHeight = 350;
      par.Style.Padding = 5;
      par.Style.Left = 0;
      par.Style.Top = 0;

      //********
      par.Style.AutoModeHeight = UiAutoMode.Content;
      //********

      float minh = 0;
      float maxh = Gui2d.MaxSize;

      //LINE AUTO 
      par.AddChild(ChildBox("First", "", OffColor.Red));
      par.LastChild().Style.FixedHeight = 72;
      par.LastChild().Style.FixedWidth = 50;

      AddVAuto(par, UiSizeMode.Auto, minh, maxh);

      par.AddChild(ChildBox("Second", "", OffColor.Red));
      par.LastChild().Style.FixedHeight = 60;
      par.LastChild().Style.FixedWidth = 50;

      par.AddChild(UiUtils.LineBreak());

      par.AddChild(ChildBox("Line1", "", OffColor.Red));
      par.LastChild().Style.FixedHeight = 50;
      par.LastChild().Style.PercentWidth = 100;
      par.LastChild().Style.ContentAlignX = UiAlignment.Center;

      par.AddChild(ChildBox("Line2A", "", OffColor.Red));
      par.LastChild().Style.FixedHeight = 50;
      par.LastChild().Style.PercentWidth = 20;
      par.LastChild().Style.ContentAlignX = UiAlignment.Center;

      AddVAuto(par, UiSizeMode.Auto, minh, 60);

      par.AddChild(ChildBox("Line2B", "", OffColor.Red));
      par.LastChild().Style.FixedHeight = 30;
      par.LastChild().Style.PercentWidth = 40;
      par.LastChild().Style.ContentAlignX = UiAlignment.Center;

      AddVAuto(par, UiSizeMode.Auto, minh, maxh);

      par.AddChild(ChildBox("Line3", "", OffColor.Red));
      par.LastChild().Style.FixedHeight = 36;
      par.LastChild().Style.PercentWidth = 100;
      par.LastChild().Style.ContentAlignX = UiAlignment.Center;

      AddVAuto(par, UiSizeMode.Auto, minh, maxh);

      par.Style.DisplayMode = UiDisplayMode.Inline;
      g.AddChild(par);
    }
    public static void TEST_AUTOS_H(Gui2d g)
    {
      var par = ParentBox();
      par.Style.FixedWidth = 600;
      par.Style.FixedHeight = 350;
      par.Style.Padding = 5;
      par.Style.Left = 0;
      par.Style.Top = 0;

      par.AddChild(ChildBox("Almost_Full_Line1", "", OffColor.Red));
      par.LastChild().Style.PercentWidth = 100;
      par.LastChild().Style.FixedHeight = 50;

      par.AddChild(ChildBox("Almost_Full_Line2", "", OffColor.Red));
      par.LastChild().Style.PercentWidth = 80;
      par.LastChild().Style.FixedHeight = 50;

      AddHAuto(par, UiSizeMode.Auto, 30);

      par.AddChild(UiUtils.LineBreak());

      //Fixed element (scrollbar)
      par.AddChild(ChildBox("A", "", OffColor.Red));
      par.LastChild().Style.FixedWidth = 50;
      par.LastChild().Style.FixedHeight = 90;

      int LEFT_COUNT = 3;
      int RIGHT_COUNT = 3;
      float min_width = 0;
      par.Style.AutoMode = UiAutoMode.Line; //content autos
      //var amode = UiSizeMode.Auto;//auto or content
      var amode = UiSizeMode.Auto;//auto or content

      //so they are pushed onto next line but offset is not valid.

      for (int i = 0; i < LEFT_COUNT; i++)
      {
        AddHAuto(par, amode, min_width);

        if (i == 2)
        {
          par.LastChild().Style.MinWidth = 200;
          par.LastChild().Style.MaxWidth = 9999;
          par.LastChild().Text = "MinMax";
        }


      }

      //add some fixed crap
      par.AddChild(ChildBox("A", "", OffColor.Red));
      par.LastChild().Style.FixedWidth = 60;
      par.LastChild().Style.FixedHeight = 90;

      //more autos
      for (int i = 0; i < RIGHT_COUNT; i++)
      {
        AddHAuto(par, amode, min_width);
        if (i == 0 || i == 1)
        {
          par.LastChild().Style.MinWidth = 200;
          par.LastChild().Style.MaxWidth = 2200;
          par.LastChild().Text = "MinMax";
        }
      }


      g.AddChild(par);
    }
    private static void AddHAuto(UiElement par, UiSizeMode sm, float minwidth)
    {
      var ch = ChildBox("H Auto", "", OffColor.SkyBlue);
      par.AddChild(ch);
      ch.Style.SizeMode = sm;
      ch.Style.FixedHeight = 60;
      ch.Style.MinWidth = minwidth;
    }
    private static void AddVAuto(UiElement par, UiSizeMode sm, float minh, float maxh)
    {
      var ch = ChildBox("V Auto", "", OffColor.SkyBlue);
      par.AddChild(ch);
      ch.Style.SizeMode = sm;
      ch.Style.FixedWidth = 100;
      ch.Style.MinHeight = minh;
      ch.Style.MaxHeight = maxh;
    }
    public static void TEST_VERTICAL_AUTOS(Gui2d g)
    {
      var par = ParentBox();
      par.Style.FixedWidth = 600;
      par.Style.FixedHeight = 200;
      par.Style.Padding = 5;
      par.Style.Left = 0;
      par.Style.Top = 0;

      //Fixed element (scrollbar)
      par.AddChild(ChildBox("A", "", OffColor.Red));
      par.LastChild().Style.FixedWidth = 30;
      par.LastChild().Style.FixedHeight = 90;

      //so they are pushed onto next line but offset is not valid.
      int COUNT = 4;
      for (int i = 0; i < COUNT; i++)
      {
        var ch = ChildBox("Hello", "", OffColor.SkyBlue);
        par.AddChild(ch);

        ch.Style.SizeMode = UiSizeMode.Auto;
        ch.Style.FixedWidth = 60;
        //ch.Style.MinWidth = 40;
        //ch.Style.Margin = 1;
        //ch.Style.Padding = 4;
        //ch.Style.Border = 3;
        if (i == 7)
        {
          ch.Text = "Boogers";
        }
      }

      // par.AddChild(ChildBox("A", "", OffColor.Red));
      // par.LastChild().Style.FixedWidth = 60;
      // par.LastChild().Style.FixedHeight= 90;

      // for (int i = 0; i < COUNT; i++)
      // {
      //   var ch = ChildBox("Hello", "", OffColor.SkyBlue);
      //   par.AddChild(ch);

      //   ch.Style.SizeMode = UiSizeMode.Content;
      //   ch.Style.FixedHeight = 60;
      //   //ch.Style.Margin = 1;
      //   //ch.Style.Padding = 4;
      //   //ch.Style.Border = 3;
      // }

      g.AddChild(par);
    }
    public static void TEST_AUTOS_1(Gui2d g)
    {
      var par = ParentBox();
      par.Style.FixedWidth = 300;
      par.Style.FixedHeight = 200;

      par.AddChild(ChildBox());
      par.ChildAt(0).AddChild(ChildBox());
      par.ChildAt(0).ChildAt(0).AddChild(ChildBox());

      par.IterateTree((e) =>
      {
        e.Style.SizeMode = UiSizeMode.Auto;
        return LambdaBool.Continue;
      });

      g.AddChild(par);
    }
    public static UiElement ParentBox()
    {
      //MBP left = 15, right = 15
      //MBP Left + Right = 30
      var par = new UiElement("parent_box");
      par.Style.SizeMode = UiSizeMode.Content;
      par.Style.Color = OffColor.PowderBlue;
      par.Style.BorderColor = OffColor.SteelBlue;
      par.Style.Border = 5;
      par.Style.Padding = 5;
      par.Style.Margin = 5;
      return par;
    }
    public static UiElement ChildBox(string text = "", string name = "", vec4? color = null)
    {
      var c = new UiElement(name);
      c.Style.DisplayMode = UiDisplayMode.Inline;
      c.Text = text;
      c.Style.FontSize = 20;
      c.Style.SizeMode = UiSizeMode.Content;
      c.Style.Color = color == null ? OffColor.WhiteSmoke : color.Value;
      c.Style.BorderColor = OffColor.DarkGray;
      c.Style.Border = 5;
      c.Style.Padding = 5;
      c.Style.Margin = 5;
      return c;
    }
    public static void TEST_CHILDREN(Gui2d g)
    {
      g.NameTraps.Add("child0");

      var par = ParentBox();
      par.Style.PercentWidth = 100;
      par.Style.MaxWidth = 700;
      //par.Style.MinWidth = par.Style.MinHeight = 150;

      par.AddChild(ChildBox("child0"));
      par.ChildAt(0).AddChild(ChildBox("child1"));
      par.ChildAt(0).ChildAt(0).AddChild(ChildBox("child2"));
      //par.ChildAt(0).ChildAt(0).ChildAt(0).AddChild(ChildBox("child3"));
      //par.ChildAt(0).ChildAt(0).ChildAt(0).ChildAt(0).AddChild(ChildBox("child4"));

      par.IterateTree((e) =>
      {
        e.Style.PercentHeight = 100;
        e.Style.PercentWidth = 90;
        return LambdaBool.Continue;
      });


      g.AddChild(par);
    }
    public static UiElement TestBorderLabel()
    {
      var lab = new UiText("", "TESTLABEL");
      float mul = 1;
      lab.Style.FontSize = 32;

      lab.Style.Margin = lab.Style.Border = lab.Style.Padding = 3;

      lab.Style.MarginTop = 4 * mul;
      lab.Style.MarginRight = 8 * mul;
      lab.Style.MarginBot = 16 * mul;
      lab.Style.MarginLeft = 32 * mul;

      lab.Style.PadTop = 4 * mul;
      lab.Style.PadRight = 8 * mul;
      lab.Style.PadBot = 16 * mul;
      lab.Style.PadLeft = 32 * mul;

      lab.Style.BorderTop = 4 * mul;
      lab.Style.BorderRight = 8 * mul;
      lab.Style.BorderBot = 16 * mul;
      lab.Style.BorderLeft = 32 * mul;

      lab.Style.Color = OffColor.Yellow;
      lab.Style.BorderColor = OffColor.Red;
      return lab;
    }
    public static UiElement TP0()
    {
      var e2 = new UiElement("ELE0");
      e2.Style.Color = OffColor.Purple;
      e2.Style.BorderColor = OffColor.Blue;
      e2.Style.Margin = 5;
      e2.Style.Padding = 5;
      e2.Style.Border = 5;
      e2.Style.SizeMode = UiSizeMode.Content;
      return e2;
    }
    public static UiElement TP1()
    {
      var e3 = new UiElement("ELE1");
      e3.Style.Color = OffColor.Green;
      e3.Style.BorderColorTop = OffColor.LimeGreen;
      e3.Style.BorderColorRight = OffColor.GreenYellow;
      e3.Style.BorderColorBot = OffColor.DarkGreen;
      e3.Style.BorderColorLeft = OffColor.PaleGreen;
      e3.Style.Margin = 5;
      e3.Style.Padding = 5;
      e3.Style.Border = 5;
      e3.Style.BorderRadius = 4;
      e3.Style.SizeMode = UiSizeMode.Content;
      return e3;
    }
    public static void TEST_NESTED_BORDERS(Gui2d g)
    {
      var lab = TestBorderLabel();
      var tp0 = TP0();
      var tp1 = TP1();
      lab.Text = "Hello World!";
      lab.Style.FontSize = 20;
      tp0.AddChild(lab);
      tp1.AddChild(tp0);
      g.AddChild(tp1);
    }
    public static void NESTED_BORDERS2_SIMPLE(Gui2d g)
    {
      var lab = TestBorderLabel();
      var lab2 = TestBorderLabel();
      lab2.AddChild(lab);
      lab2.Style.MBP = 0;
      lab2.Text = "";
      lab2.Style.Margin = 10;
      g.AddChild(lab2);
    }
    public static void EXPAND_RACE(Gui2d g)
    {
      var par = ParentBox();// new UiElement("parent_shrink");
      par.Style.PercentWidth = 70;
      par.Style.SizeModeHeight = UiSizeMode.Content;

      var ch = ChildBox();
      ch.Style.PercentWidth = 100;
      ch.Style.PercentHeight = 100;

      ch.Text = Ipsum.GetIpsum(1);// "Hello World";
      ch.Style.FontSize = 20;

      par.AddChild(ch);
      g.AddChild(par);
    }
    public static UiElement FixedBox(int w, int h, string name)
    {
      var c = new UiElement("Fixedbox-" + name);
      c.Style.FixedWidth = w;
      c.Style.FixedHeight = h;
      c.Style.Color = OffColor.WhiteSmoke;
      c.Style.BorderColor = OffColor.DarkGray;
      c.Style.Border = 5;
      c.Style.Padding = 5;
      c.Style.Margin = 5;
      return c;
    }
    public static UiElement PctTable(int rows, int cols)
    {
      //talbe using percent size mode
      var par = ParentBox();
      for (int r = 0; r < rows; r++)
      {
        var rn = "row" + r;
        var row = ChildBox(rn, rn);
        for (int c = 0; c < cols; c++)
        {
          var cn = "col" + c;
          var col = ChildBox(cn, cn);
          col.Style.PercentWidth = 100.0f / (float)cols;
          col.Style.PercentHeight = 100;
          row.AddChild(col);
        }
        par.AddChild(row);
      }
      return par;
    }
    public static void Layout1(Gui2d g, UiOrientation dir)
    {
      var par = ParentBox();
      par.Style.DisplayMode = UiDisplayMode.Inline;
      par.Style.LayoutOrientation = dir;
      par.Style.SizeMode = UiSizeMode.Content;
      if (dir == UiOrientation.Horizontal)
      {
        par.Style.MaxWidth = 140;
        par.Style.MaxHeight = Gui2d.MaxSize;
      }
      else if (dir == UiOrientation.Vertical)
      {
        par.Style.MaxWidth = Gui2d.MaxSize;
        par.Style.MaxHeight = 140;
      }

      par.AddChild(FixedBox(100, 100, "c0"));
      par.AddChild(FixedBox(100, 100, "c1"));

      par.ChildAt(0).Style.DisplayMode = UiDisplayMode.Inline;
      par.ChildAt(1).Style.DisplayMode = UiDisplayMode.Inline;

      //par.AddChild( FixedBox(100, 100, "c2"));
      g.AddChild(par);
    }
    public static void Layout2(Gui2d g, UiOrientation ori, UiLayoutDirection dir)
    {
      int max = 220;
      var par = ParentBox();
      par.Style.DisplayMode = UiDisplayMode.Inline;
      par.Style.LayoutOrientation = ori;
      par.Style.LayoutDirection = dir;
      par.Style.SizeMode = UiSizeMode.Content;
      if (ori == UiOrientation.Horizontal)
      {
        par.Style.MaxWidth = max;
        par.Style.MaxHeight = Gui2d.MaxSize;
      }
      else if (ori == UiOrientation.Vertical)
      {
        par.Style.MaxWidth = Gui2d.MaxSize;
        par.Style.MaxHeight = max;
      }

      par.AddChild(ChildBox("first"));
      par.AddChild(ChildBox("second"));
      par.AddChild(ChildBox("third"));

      par.AddChild(ChildBox("fourth"));
      par.AddChild(ChildBox("fifth"));
      par.AddChild(ChildBox("sixth"));

      par.AddChild(ChildBox("seventh"));
      par.AddChild(ChildBox("eighth"));
      par.AddChild(ChildBox("ninth"));

      // par.ChildAt(par.ChildCount - 9).Style.Alignment = UiAlignment.Left;
      // par.ChildAt(par.ChildCount - 8).Style.Alignment = UiAlignment.Left;
      // par.ChildAt(par.ChildCount - 7).Style.Alignment = UiAlignment.Left;

      // par.ChildAt(par.ChildCount - 6).Style.Alignment = UiAlignment.Center;
      // par.ChildAt(par.ChildCount - 5).Style.Alignment = UiAlignment.Center;
      // par.ChildAt(par.ChildCount - 4).Style.Alignment = UiAlignment.Center;

      // par.ChildAt(par.ChildCount - 3).Style.Alignment = UiAlignment.Right;
      // par.ChildAt(par.ChildCount - 2).Style.Alignment = UiAlignment.Right;
      // par.ChildAt(par.ChildCount - 1).Style.Alignment = UiAlignment.Right;


      g.AddChild(par);
    }
    public static void TEST_LAYOUT1(Gui2d g)
    {
      Layout1(g, UiOrientation.Horizontal);
      Layout1(g, UiOrientation.Vertical);
    }
    public static void TEST_LAYOUT2(Gui2d g)
    {
      Layout2(g, UiOrientation.Horizontal, UiLayoutDirection.LeftToRight);
      Layout2(g, UiOrientation.Vertical, UiLayoutDirection.LeftToRight);

      Layout2(g, UiOrientation.Horizontal, UiLayoutDirection.RightToLeft);
      Layout2(g, UiOrientation.Vertical, UiLayoutDirection.RightToLeft);
    }

  }//cls

}//ns