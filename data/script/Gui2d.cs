using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Reflection;
using System.Threading;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Loft
{
  #region Enums

  using UiAction = Action<UiEvent>;
  using UiBool2 = Loft.UiSize2T<bool>;
  using UiInt2 = Loft.UiSize2T<int>;
  using UiSize2 = Loft.UiSize2T<float>;
  using UiList2 = Loft.UiSize2T<List<UiElement>>;

  public class UiUtils
  {
    public static UiOrientation Perp(UiOrientation ori)
    {
      return ori == UiOrientation.Horizontal ? UiOrientation.Vertical : UiOrientation.Horizontal;
    }
    public static UiElement LineBreak()
    {
      var e = new UiElement();
      e.Style.FixedWidth = 0;
      e.Style.FixedHeight = 0;
      e.Style.DisplayMode = UiDisplayMode.Block;
      return e;
    }
  }

  #endregion
  #region Base / Helpers

  public class UiTexture : IUiTexture
  {
    public UiTexture() { }
    public UiTexture(FileLoc path) { _path = path; Modified = true; }
    public MtTex? Image { get { return _texture; } set { _texture = value; } }
    public FileLoc? Loc { get { return _path; } set { _path = value; Modified = true; } }
    public bool Modified { get; set; }
    private FileLoc? _path = null;
    private MtTex? _texture = null;
  }
  public class UiLayoutGlobals : IUiLayoutGlobals
  {
    public UiLayoutGlobals(Gui2d g, MegaTex mt, UiStyleSheet s, UiGlyphCache cc)
    {
      Gui = g;
      MegaTex = mt;
      StyleSheet = s;
      GlyphCache = cc;
    }

    //Public props
    public Gui2d Gui { get; }
    public MegaTex MegaTex { get; }
    public UiStyleSheet StyleSheet { get; }
    public UiGlyphCache GlyphCache { get; }
    public MtTex DefaultPixel { get { return MegaTex.DefaultPixel; } }
    public bool Force { get; set; } = false;
    public bool ForceText { get; set; } = false;
    public bool DisableClip { get; set; } = false;
    public bool ShowDebug { get; set; } = false;
    public bool DisableMargins { get; set; } = false;
    public bool DisablePadding { get; set; } = false;
    public bool DisableBorders { get; set; } = false;
    public bool DisableAutos { get; set; } = false;

    //Layout Props
    public ReverseGrowList<v_v4v4v4v2u2v4v4> Verts { get; set; } = new ReverseGrowList<v_v4v4v4v2u2v4v4>();
    public List<UiElement> LayoutEvents = new List<UiElement>();// _elementsWithChangedContent = new List<UiElement>();
    public int dbg_DidNotChange = 0;
    public int dbg_FullyClipped = 0;
    public vec2 Scale = new vec2(1, 1);
    public int Framestamp = 0;
  }
  public class UiSize2T<T>
  {
    public T _width = default(T);
    public T _height = default(T);
    public void Zero() { _width = default(T); _height = default(T); }
    public void Set(T w, T h) { _width = w; _height = h; }
    //public static UiSizeT<T> Zero { get { return new UiSizeT<T>(default(T), default(T)); } }
    public UiSize2T() { }
    public UiSize2T(T dw, T dh) { _width = dw; _height = dh; }

    //L_.. "give me the dimension in this layout direction"
    public T L_Width(UiOrientation dir)
    {
      return (dir == UiOrientation.Horizontal) ? _width : _height;
    }
    public void L_Width(UiOrientation dir, T value)
    {
      if (dir == UiOrientation.Horizontal) { _width = value; }
      else if (dir == UiOrientation.Vertical) { _height = value; }
      else { Gu.BRThrowNotImplementedException(); }
    }
    public T L_Height(UiOrientation dir)
    {
      return L_Width(UiUtils.Perp(dir));
    }
    public void L_Height(UiOrientation dir, T value)
    {
      L_Width(UiUtils.Perp(dir), value);
    }
    public void L_WidthHeight(UiOrientation dir, T w, T h)
    {
      if (dir == UiOrientation.Horizontal) { _width = w; _height = h; }
      else if (dir == UiOrientation.Vertical) { _width = h; _height = w; }
      else { Gu.BRThrowNotImplementedException(); }
    }
  }
  public class UiSize4
  {
    public float _top = 0, _right = 0, _bot = 0, _left = 0;
    //public static UiSize4 Zero { get { return new UiSize4(0, 0, 0, 0); } }
    public void Zero() { _top = 0; _right = 0; _bot = 0; _left = 0; }
    public UiSize4() { }
    public UiSize4(float dt, float dr, float db, float dl) { _top = dt; _right = dr; _bot = db; _left = dl; }
    public UiSize4(vec4 v) { _top = v.x; _right = v.y; _bot = v.z; _left = v.w; }
    public vec4 ToVec4() { return new vec4(_top, _right, _bot, _left); }
    public void Set(float t, float r, float b, float l) { _top = t; _right = r; _bot = b; _left = l; }
    public float L_Left(UiOrientation dir)
    {
      return (dir == UiOrientation.Horizontal) ? _left : _top;
    }
    public float L_Top(UiOrientation dir)
    {
      return L_Left(UiUtils.Perp(dir));
    }
    public float L_Right(UiOrientation dir)
    {
      return (dir == UiOrientation.Horizontal) ? _right : _bot;
    }
    public float L_Bot(UiOrientation dir)
    {
      return L_Right(UiUtils.Perp(dir));
    }
    public void L_Left(UiOrientation dir, float value)
    {
      if (dir == UiOrientation.Horizontal) { _left = value; }
      else if (dir == UiOrientation.Vertical) { _top = value; }
      else { Gu.BRThrowNotImplementedException(); }
    }
    public void L_Right(UiOrientation dir, float value)
    {
      if (dir == UiOrientation.Horizontal) { _right = value; }
      else if (dir == UiOrientation.Vertical) { _bot = value; }
      else { Gu.BRThrowNotImplementedException(); }
    }

  }
  public class UiQuad
  {
    public const float c_dbg_maxsize = 999999;

    public float _left = 0;
    public float _top = 0;
    public float _width = 0;
    public float _height = 0;

    public vec2 Max { get { return new vec2(_left + _width, _top + _height); } }
    public vec2 Min { get { return new vec2(_left, _top); } }
    public float Top { get { return _top; } }
    public float Left { get { return _left; } }
    public float Right { get { return _left + _width; } }
    public float Bottom { get { return _top + _height; } }

    public UiQuad() { }
    public UiQuad(float l, float t, float w, float h) { _left = l; _top = t; _width = w; _height = h; }

    public void ShrinkBy(UiSize4 border, vec2 scale)
    {
      _left += border._left * scale.x;
      _top += border._top * scale.y;
      _width -= (border._left * scale.x + border._right * scale.x);
      _height -= (border._top * scale.y + border._bot * scale.y);
    }
    public void Zero()
    {
      _top = 0;
      _left = 0;
      _width = 0;
      _height = 0;
    }
    public bool ContainsPointBLI(vec2 v)
    {
      return _left <= v.x && (_left + _width) > v.x && _top <= v.y && (_top + _height) > v.y;
    }
    public void Scale(vec2 scale)
    {
      _left *= scale.x;
      _top *= scale.y;
      _width *= scale.x;
      _height *= scale.y;
    }
    public bool Equals(UiQuad q)
    {
      return (q._left == this._left &&
              q._top == this._top &&
              q._width == this._width &&
              q._height == this._height
              );
    }
    public UiQuad Clone()
    {
      return new UiQuad()
      {
        _left = this._left,
        _top = this._top,
        _width = this._width,
        _height = this._height
      };
    }
    public void ClampToZero()
    {
      _left = Math.Max(_left, 0);
      _top = Math.Max(_top, 0);
      _width = Math.Max(_width, 0);
      _height = Math.Max(_height, 0);
    }

    static bool _break_on_equal = false;
    public void ValidateQuad(bool debug_break = true)
    {
      if (_width < 0 && debug_break) { Gu.DebugBreak(); _width = 0; }
      if (_height < 0 && debug_break) { Gu.DebugBreak(); _height = 0; }
      if (_left > c_dbg_maxsize && debug_break) { Gu.DebugBreak(); }
      if (_top > c_dbg_maxsize && debug_break) { Gu.DebugBreak(); }
      if (_width > c_dbg_maxsize && debug_break) { Gu.DebugBreak(); }
      if (_height > c_dbg_maxsize && debug_break) { Gu.DebugBreak(); }
      if (_left < -c_dbg_maxsize && debug_break) { Gu.DebugBreak(); }
      if (_top < -c_dbg_maxsize && debug_break) { Gu.DebugBreak(); }

      //not an error
      if (Left == Right && _break_on_equal) { Gu.DebugBreak(); }
      if (Top == Bottom && _break_on_equal) { Gu.DebugBreak(); }

      if (Single.IsNaN(_left) && debug_break) { Gu.DebugBreak(); }
      if (Single.IsNaN(_top) && debug_break) { Gu.DebugBreak(); }
      if (Single.IsNaN(_width) && debug_break) { Gu.DebugBreak(); }
      if (Single.IsNaN(_height) && debug_break) { Gu.DebugBreak(); }

      if (Single.IsInfinity(_left) && debug_break) { Gu.DebugBreak(); }
      if (Single.IsInfinity(_top) && debug_break) { Gu.DebugBreak(); }
      if (Single.IsInfinity(_width) && debug_break) { Gu.DebugBreak(); }
      if (Single.IsInfinity(_height) && debug_break) { Gu.DebugBreak(); }

    }
    public void Expand(vec2 v)
    {
      _left = (float)Math.Min(v.x, _left);
      _top = (float)Math.Min(v.y, _top);
      _width = (float)Math.Max(v.x - _left, _width);
      _height = (float)Math.Max(v.y - _top, _height);
    }
    public void Intersect(UiQuad b)
    {
      float mx = _left + _width;
      float my = _top + _height;
      float bmx = b._left + b._width;
      float bmy = b._top + b._height;

      MathUtils.BoxShrink(
        ref _left, ref _top, ref mx, ref my,
        b._left, b._top, bmx, bmy
        );

      _width = mx - _left;
      _height = my - _top;
    }
    public float L_Width(UiOrientation dir)
    {
      return (dir == UiOrientation.Horizontal) ? _width : _height;
    }
    public void L_Width(UiOrientation dir, float value)
    {
      if (dir == UiOrientation.Horizontal) { _width = value; }
      else if (dir == UiOrientation.Vertical) { _height = value; }
      else { Gu.BRThrowNotImplementedException(); }
    }
    public float L_Height(UiOrientation dir)
    {
      return L_Width(UiUtils.Perp(dir));
    }
    public void L_Height(UiOrientation dir, float value)
    {
      L_Width(UiUtils.Perp(dir), value);
    }
    public float L_Left(UiOrientation dir)
    {
      return (dir == UiOrientation.Horizontal) ? _left : _top;
    }
    public float L_Top(UiOrientation dir)
    {
      return L_Left(UiUtils.Perp(dir));
    }
    public void L_Left(UiOrientation dir, float value)
    {
      if (dir == UiOrientation.Horizontal) { _left = value; }
      else if (dir == UiOrientation.Vertical) { _top = value; }
      else { Gu.BRThrowNotImplementedException(); }
    }
    public void L_Top(UiOrientation dir, float value)
    {
      L_Left(UiUtils.Perp(dir), value);
    }
    public void L_WidthHeight(UiOrientation dir, float w, float h)
    {
      if (dir == UiOrientation.Horizontal) { _width = w; _height = h; }
      else if (dir == UiOrientation.Vertical) { _width = h; _height = w; }
      else { Gu.BRThrowNotImplementedException(); }
    }
    public void L_LeftTop(UiOrientation dir, float x, float y)
    {
      if (dir == UiOrientation.Horizontal) { _left = x; _top = y; }
      else if (dir == UiOrientation.Vertical) { _top = x; _left = y; }
      else { Gu.BRThrowNotImplementedException(); }
    }

    //cant set
  }
  public abstract class UiBlock : InputEventListener
  {
    //interface between UiGlyph and UiElement

    public vec4 _debugcolor;

    public float L_WordWidth(UiOrientation ori) { return ori == UiOrientation.Horizontal ? WordWidth : WordHeight; }
    public abstract UiDisplayMode DisplayMode { get; }
    public virtual float WordWidth { get { return 0; } }
    public virtual float WordHeight { get { return 0; } }
    public virtual int WordID { get { return -1; } }
    public virtual int WordCharID { get { return -1; } }
    public vec4? GlyphColor { get { return null; } set { } }

    //this should be moved to UiElement, glyph needs very little in this, there are thousadnds of glyphs..
    public UiBlockInfo _block = new UiBlockInfo();
    public UiBlock()
    {
      _debugcolor = Rand.NextRGB(0.0f, 1.0f, 0.3f);
    }
    public abstract float L_Left(UiOrientation o);
    public abstract float L_Top(UiOrientation o);
    public abstract float L_Advance();
    public abstract float L_PixelLineHeight();
  }

  #endregion
  #region Props / Stylesheet

  public class UiProps
  {
    public UiPositionMode L_PositionMode(UiOrientation dir) { return dir == UiOrientation.Horizontal ? PositionModeX : PositionModeY; }
    public UiAlignment L_ContentAlign(UiOrientation dir) { return dir == UiOrientation.Horizontal ? ContentAlignX : ContentAlignY; }
    public UiSizeMode L_SizeMode(UiOrientation dir) { return dir == UiOrientation.Horizontal ? SizeModeWidth : SizeModeHeight; }
    public UiAutoMode L_AutoMode(UiOrientation dir) { return dir == UiOrientation.Horizontal ? AutoModeWidth : AutoModeHeight; }
    public float L_PercentWidth(UiOrientation dir) { return dir == UiOrientation.Horizontal ? PercentWidth : PercentHeight; }
    public float L_FixedWidth(UiOrientation dir) { return dir == UiOrientation.Horizontal ? FixedWidth : FixedHeight; }
    public float L_LT(UiOrientation dir) { return dir == UiOrientation.Horizontal ? Left : Top; }
    public float L_MaxWidth(UiOrientation dir) { return dir == UiOrientation.Horizontal ? MaxWidth : MaxHeight; }

    //All styled properties of an element. 
    // All elements contain one properties class with all value type properties set to default (besides texture/font face, but they MUST be set).
    //
    public float Top = 0;
    public float Left = 0;
    public float MinWidth = 0;
    public float MinHeight = 0;
    public float MaxWidth = Gui2d.MaxSize;
    public float MaxHeight = Gui2d.MaxSize;
    public float FixedWidth = 100;
    public float FixedHeight = 100;
    public float PercentWidth = 100;
    public float PercentHeight = 100;
    public float PadTop = 0;
    public float PadRight = 0;
    public float PadBot = 0;
    public float PadLeft = 0;
    public float MarginTop = 0;
    public float MarginRight = 0;
    public float MarginBot = 0;
    public float MarginLeft = 0;
    public float BorderTop = 0;
    public float BorderRight = 0;
    public float BorderBot = 0;
    public float BorderLeft = 0;
    public float BorderTopLeftRadius = 0;
    public float BorderTopRightRadius = 0;
    public float BorderBotRightRadius = 0;
    public float BorderBotLeftRadius = 0;
    public vec4 Color = new vec4(1, 1, 1, 1);
    public vec4 MultiplyColor = new vec4(1, 1, 1, 1);//color multiplier
    public vec4 BorderColorTop = new vec4(0, 0, 0, 1);
    public vec4 BorderColorRight = new vec4(0, 0, 0, 1);
    public vec4 BorderColorBot = new vec4(0, 0, 0, 1);
    public vec4 BorderColorLeft = new vec4(0, 0, 0, 1);
    public FileLoc FontFace = Loft.UiFontFace.Calibri;
    public float FontSize = 12;
    public UiFontStyle FontStyle = UiFontStyle.Normal;
    public vec4 FontColor = new vec4(0, 0, 0, 1);
    public float FontWeight = 1;
    public float LineHeight = 1;
    public float Tracking = 1; //letter spacing
    public UiTexture Texture = null;
    public UiImageTiling ImageTilingX = UiImageTiling.Expand;
    public UiImageTiling ImageTilingY = UiImageTiling.Expand;
    public float ImageScaleX = 1;
    public float ImageScaleY = 1;
    public UiDisplayMode DisplayMode = UiDisplayMode.Inline;
    public UiPositionMode PositionModeY = UiPositionMode.Static;
    public UiPositionMode PositionModeX = UiPositionMode.Static;
    public UiOverflowMode OverflowMode = UiOverflowMode.Content;
    public UiSizeMode SizeModeWidth = UiSizeMode.Content;
    public UiSizeMode SizeModeHeight = UiSizeMode.Content;
    public UiAutoMode AutoModeWidth = UiAutoMode.Content;
    public UiAutoMode AutoModeHeight = UiAutoMode.Content;
    public UiFloatMode FloatMode = UiFloatMode.None;
    public UiRenderMode RenderMode = UiRenderMode.None;
    public UiAlignment ContentAlignX = UiAlignment.Left;
    public UiAlignment ContentAlignY = UiAlignment.Left;
    public UiWrapMode TextWrap = UiWrapMode.Word;
    public float Opacity = 1;
    public UiOrientation LayoutOrientation = UiOrientation.Horizontal;
    public UiLayoutDirection LayoutDirection = UiLayoutDirection.LeftToRight;
    public UiVStatMode VStatMode = UiVStatMode.Line;
    public UiTextHyphens Hyphens = UiTextHyphens.Manual;

    //Most of this generic field junk can go away and we can manually just return the variables. My hands were huring here so..ugh
    private static UiProps _defaults = new UiProps();//defaults are just set on the field initializer.
    public static Dictionary<UiPropName, System.Reflection.FieldInfo> AllFields { get; private set; } = null;
    public static Dictionary<UiPropName, System.Reflection.FieldInfo> InheritableFields { get; private set; } = null;

    public UiProps()
    {
    }
    public static object? Default(UiPropName p)
    {
      return _defaults.Get(p);
    }
    public object? Get(UiPropName p)
    {
      CreateStaticFieldInfo();
      var fi = AllFields[p];
      var val = fi.GetValue(this);
      return val;
    }
    public void Set(UiPropName p, object? value)
    {
      if (value == null && p != UiPropName.Texture)
      {
        //this is an error..props, besides texture, must never have null values.
        Gu.DebugBreak();
      }

      CreateStaticFieldInfo();
      var fi = AllFields[p];
      fi.SetValue(this, value);
    }
    public UiProps Clone()
    {
      UiProps ret = new UiProps();

      foreach (var f in GetType().GetFields())
      {
        var v = f.GetValue(this);
        if (v != null)
        {
          f.SetValue(ret, v);
        }
      }

      return ret;
    }
    public new string ToString()
    {
      StringBuilder sb = new StringBuilder();
      foreach (var f in GetType().GetFields())
      {
        if (f.GetValue(this) != null)
        {
          sb.AppendLine($"{f.Name}      {f.GetValue(this).ToString()}");
        }
      }
      return sb.ToString();
    }
    public void ValidateProps()
    {
      //yes css does let width outside 100 but this is for sanity and well
      if (!Gu.AssertDebug(PercentWidth >= 0 && PercentWidth <= 100))
      {
        PercentWidth = Math.Clamp(PercentWidth, 0, 100);
      }
      if (!Gu.AssertDebug(PercentHeight >= 0 && PercentHeight <= 100))
      {
        PercentHeight = Math.Clamp(PercentHeight, 0, 100);
      }

      if (!Gu.AssertDebug(MinWidth >= 0))
      {
        MinWidth = 0;
      }
      if (!Gu.AssertDebug(MaxWidth >= 0))
      {
        MaxWidth = 0;
      }
      if (!Gu.AssertDebug(MinWidth <= MaxWidth))
      {
        MaxWidth = MinWidth;
      }
      if (!Gu.AssertDebug(MinHeight >= 0))
      {
        MinHeight = 0;
      }
      if (!Gu.AssertDebug(MaxHeight >= 0))
      {
        MaxHeight = 0;
      }
      if (!Gu.AssertDebug(MinHeight <= MaxHeight))
      {
        MaxHeight = MinHeight;
      }

    }
    private static void CreateStaticFieldInfo()
    {
      if (AllFields == null || InheritableFields == null)
      {
        AllFields = new Dictionary<UiPropName, FieldInfo>();
        InheritableFields = new Dictionary<UiPropName, FieldInfo>();
        var enums = Enum.GetValues(typeof(UiPropName));
        var fields = typeof(UiProps).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var pf in fields)
        {
          foreach (var pe in enums)
          {
            string a = pe.ToString();
            string a_priv = "_" + a;
            string b = pf.Name;
            if (a.Equals(b) || a_priv.Equals(b)) //for debugging get/set - we use _ for the private prop
            {
              if (CanInherit((UiPropName)pe))
              {
                InheritableFields.Add((UiPropName)pe, pf);
              }
              AllFields.Add((UiPropName)pe, pf);
              break;
            }

          }
        }
        //Make sure we got all fields.
        for (int i = 0; i < (int)UiPropName.MaxUiProps; ++i)
        {
          UiPropName p = (UiPropName)i;
          //You didnt add the prop to the fields.
          Gu.Assert(AllFields.ContainsKey(p));
        }
      }
    }
    private static bool CanInherit(UiPropName p)
    {
      //not to CSS standards
      if (
        p == UiPropName.Color
        || p == UiPropName.MultiplyColor
        || p == UiPropName.FontFace
        || p == UiPropName.FontSize
        || p == UiPropName.FontStyle
        || p == UiPropName.FontColor
        || p == UiPropName.FontWeight
        || p == UiPropName.LineHeight
        || p == UiPropName.Tracking
        || p == UiPropName.TextWrap
        || p == UiPropName.Opacity
      )
      {
        return true;
      }
      return false;
    }
  }
  public interface IUiPropAnimation
  {
    public UiElement Element { get; }
    public UiPropName Prop { get; }
    public bool Update(double dt);
  }
  public class UiPropAnimation<T> : IUiPropAnimation where T : struct
  {
    public UiElement Element { get; }
    public UiPropName Prop { get; }

    private int _duration;
    private int _current = 0;
    private double _elapsed = 0;
    private double _durationseconds;
    private T? _startValue;
    private T? _endValue;
    private int _repeatCount = 0;
    private int _executed = 0;

    public UiPropAnimation(UiElement e, UiPropName p, T? endValue, int durationMS, int repeatCount = 0)
    {
      Gu.Assert(e != null);
      Element = e;
      Prop = p;
      _repeatCount = repeatCount;

      _durationseconds = (double)durationMS / 1000.0;
      if (_durationseconds <= 0)
      {
        _durationseconds = 0.00001;
      }
      var prop = e.Style.GetProp(p);
      Gu.Assert(prop != null);

      _startValue = (prop as T?).Value;
      _endValue = endValue;
    }
    public bool Update(double dt)
    {
      //return true if it's done
      if (_elapsed < _durationseconds && _executed < (_repeatCount + 1))
      {
        T? newval = null;
        _elapsed += dt;
        double x = Math.Clamp(_elapsed / _durationseconds, 0, 1);

        if (typeof(T) == typeof(vec4))
        {
          var a = _startValue as vec4?;
          var b = _endValue as vec4?;
          newval = (a + (b - a) * (float)x) as T?;
        }
        else if (typeof(T) == typeof(double))
        {
          var a = _startValue as double?;
          var b = _endValue as double?;
          newval = (a + (b - a) * x) as T?;
        }
        else if (typeof(T) == typeof(float))
        {
          var a = _startValue as float?;
          var b = _endValue as float?;
          newval = (a + (b - a) * (float)x) as T?;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }

        if (newval != null)
        {
          Element.Style.SetProp(Prop, newval);
        }

        for (int xx = 0; Gu.WhileTrueGuard(xx, Gu.c_intMaxWhileTrueLoopSmall) && _elapsed >= _durationseconds; xx++)
        {
          _elapsed %= _durationseconds;
          _executed++;
        }

      }
      return _executed >= (_repeatCount + 1);
    }

  }
  public class UiStyle : IUiStyle
  {
    #region Props

    public string Name { get; set; } = "";
    public bool Modified { get { return _bMustCompile; } }
    public bool IsInline { get; set; }

    public float MBP
    {
      set
      {
        Margin = Border = Padding = value;
      }
    }
    public float? Margin
    {
      get { return (float?)_props.Get(UiPropName.MarginTop); }
      set
      {
        MarginTop = MarginRight = MarginBot = MarginLeft = value;
      }
    }
    public float? Border
    {
      get { return (float?)_props.Get(UiPropName.BorderTop); }
      set
      {
        BorderTop = BorderRight = BorderLeft = BorderBot = value;
      }
    }
    public vec4? BorderColor
    {
      get { return (vec4?)_props.Get(UiPropName.BorderColorTop); }
      set
      {
        BorderColorTop = BorderColorRight = BorderColorLeft = BorderColorBot = value;
      }
    }
    public float? BorderRadius
    {
      get { return (float?)_props.Get(UiPropName.BorderTopLeftRadius); }
      set
      {
        BorderTopLeftRadius = BorderTopRightRadius = BorderBotRightRadius = BorderBotLeftRadius = value;
      }
    }
    public float? Padding
    {
      get { return (float?)_props.Get(UiPropName.PadTop); }
      set
      {
        PadTop = PadRight = PadBot = PadLeft = value;
      }
    }
    public UiSizeMode SizeMode { set { SizeModeWidth = value; SizeModeHeight = value; } }
    public UiAutoMode AutoMode { set { AutoModeWidth = value; AutoModeHeight = value; } }
    public float SizePercent { set { PercentWidth = value; PercentHeight = value; } }
    public UiPositionMode PositionMode { set { PositionModeX = value; PositionModeY = value; } }
    public float Min { set { MinWidth = value; MinHeight = value; } }
    public float Max { set { MaxWidth = value; MaxHeight = value; } }

    public float? Top { get { return (float?)GetClassProp(UiPropName.Top); } set { SetProp(UiPropName.Top, (float?)value); } }
    public float? Left { get { return (float?)GetClassProp(UiPropName.Left); } set { SetProp(UiPropName.Left, (float?)value); } }
    public float? MinWidth { get { return (float?)GetClassProp(UiPropName.MinWidth); } set { SetProp(UiPropName.MinWidth, (float?)value); } }
    public float? MinHeight { get { return (float?)GetClassProp(UiPropName.MinHeight); } set { SetProp(UiPropName.MinHeight, (float?)value); } }
    public float? MaxWidth { get { return (float?)GetClassProp(UiPropName.MaxWidth); } set { SetProp(UiPropName.MaxWidth, (float?)value); } }
    public float? MaxHeight { get { return (float?)GetClassProp(UiPropName.MaxHeight); } set { SetProp(UiPropName.MaxHeight, (float?)value); } }
    public float? FixedWidth { get { return (float?)GetClassProp(UiPropName.FixedWidth); } set { SetProp(UiPropName.FixedWidth, (float?)value); } }
    public float? FixedHeight { get { return (float?)GetClassProp(UiPropName.FixedHeight); } set { SetProp(UiPropName.FixedHeight, (float?)value); } }
    public float? PadTop { get { return (float?)GetClassProp(UiPropName.PadTop); } set { SetProp(UiPropName.PadTop, (float?)value); } }
    public float? PadRight { get { return (float?)GetClassProp(UiPropName.PadRight); } set { SetProp(UiPropName.PadRight, (float?)value); } }
    public float? PadBot { get { return (float?)GetClassProp(UiPropName.PadBot); } set { SetProp(UiPropName.PadBot, (float?)value); } }
    public float? PadLeft { get { return (float?)GetClassProp(UiPropName.PadLeft); } set { SetProp(UiPropName.PadLeft, (float?)value); } }
    public float? MarginTop { get { return (float?)GetClassProp(UiPropName.MarginTop); } set { SetProp(UiPropName.MarginTop, (float?)value); } }
    public float? MarginRight { get { return (float?)GetClassProp(UiPropName.MarginRight); } set { SetProp(UiPropName.MarginRight, (float?)value); } }
    public float? MarginBot { get { return (float?)GetClassProp(UiPropName.MarginBot); } set { SetProp(UiPropName.MarginBot, (float?)value); } }
    public float? MarginLeft { get { return (float?)GetClassProp(UiPropName.MarginLeft); } set { SetProp(UiPropName.MarginLeft, (float?)value); } }
    public float? BorderTop { get { return (float?)GetClassProp(UiPropName.BorderTop); } set { SetProp(UiPropName.BorderTop, (float?)value); } }
    public float? BorderRight { get { return (float?)GetClassProp(UiPropName.BorderRight); } set { SetProp(UiPropName.BorderRight, (float?)value); } }
    public float? BorderBot { get { return (float?)GetClassProp(UiPropName.BorderBot); } set { SetProp(UiPropName.BorderBot, (float?)value); } }
    public float? BorderLeft { get { return (float?)GetClassProp(UiPropName.BorderLeft); } set { SetProp(UiPropName.BorderLeft, (float?)value); } }
    public float? BorderTopLeftRadius { get { return (float?)GetClassProp(UiPropName.BorderTopLeftRadius); } set { SetProp(UiPropName.BorderTopLeftRadius, (float?)value); } }
    public float? BorderTopRightRadius { get { return (float?)GetClassProp(UiPropName.BorderTopRightRadius); } set { SetProp(UiPropName.BorderTopRightRadius, (float?)value); } }
    public float? BorderBotRightRadius { get { return (float?)GetClassProp(UiPropName.BorderBotRightRadius); } set { SetProp(UiPropName.BorderBotRightRadius, (float?)value); } }
    public float? BorderBotLeftRadius { get { return (float?)GetClassProp(UiPropName.BorderBotLeftRadius); } set { SetProp(UiPropName.BorderBotLeftRadius, (float?)value); } }
    public vec4? Color { get { return (vec4?)GetClassProp(UiPropName.Color); } set { SetProp(UiPropName.Color, (vec4?)value); } }
    public vec4? MultiplyColor { get { return (vec4?)GetClassProp(UiPropName.MultiplyColor); } set { SetProp(UiPropName.MultiplyColor, (vec4?)value); } }
    public vec4? BorderColorTop { get { return (vec4?)GetClassProp(UiPropName.BorderColorTop); } set { SetProp(UiPropName.BorderColorTop, (vec4?)value); } }
    public vec4? BorderColorRight { get { return (vec4?)GetClassProp(UiPropName.BorderColorRight); } set { SetProp(UiPropName.BorderColorRight, (vec4?)value); } }
    public vec4? BorderColorBot { get { return (vec4?)GetClassProp(UiPropName.BorderColorBot); } set { SetProp(UiPropName.BorderColorBot, (vec4?)value); } }
    public vec4? BorderColorLeft { get { return (vec4?)GetClassProp(UiPropName.BorderColorLeft); } set { SetProp(UiPropName.BorderColorLeft, (vec4?)value); } }
    public FileLoc FontFace { get { return (FileLoc)GetClassProp(UiPropName.FontFace); } set { SetProp(UiPropName.FontFace, (FileLoc)value); } }
    public float? FontSize { get { return (float?)GetClassProp(UiPropName.FontSize); } set { SetProp(UiPropName.FontSize, (float?)value); } }
    public UiFontStyle? FontStyle { get { return (UiFontStyle?)GetClassProp(UiPropName.FontStyle); } set { SetProp(UiPropName.FontStyle, (UiFontStyle?)value); } }
    public vec4? FontColor { get { return (vec4?)GetClassProp(UiPropName.FontColor); } set { SetProp(UiPropName.FontColor, (vec4?)value); } }
    public float? FontWeight { get { return (float?)GetClassProp(UiPropName.FontWeight); } set { SetProp(UiPropName.FontWeight, (float?)value); } }
    public float? LineHeight { get { return (float?)GetClassProp(UiPropName.LineHeight); } set { SetProp(UiPropName.LineHeight, (float?)value); } }
    public float? Tracking { get { return (float?)GetClassProp(UiPropName.Tracking); } set { SetProp(UiPropName.Tracking, (float?)value); } }
    public UiPositionMode? PositionModeX { get { return (UiPositionMode?)GetClassProp(UiPropName.PositionModeX); } set { SetProp(UiPropName.PositionModeX, (UiPositionMode?)value); } }
    public UiPositionMode? PositionModeY { get { return (UiPositionMode?)GetClassProp(UiPropName.PositionModeY); } set { SetProp(UiPropName.PositionModeY, (UiPositionMode?)value); } }
    public UiOverflowMode? OverflowMode { get { return (UiOverflowMode?)GetClassProp(UiPropName.OverflowMode); } set { SetProp(UiPropName.OverflowMode, (UiOverflowMode?)value); } }
    public UiSizeMode? SizeModeWidth { get { return (UiSizeMode?)GetClassProp(UiPropName.SizeModeWidth); } set { SetProp(UiPropName.SizeModeWidth, (UiSizeMode?)value); } }
    public UiSizeMode? SizeModeHeight { get { return (UiSizeMode?)GetClassProp(UiPropName.SizeModeHeight); } set { SetProp(UiPropName.SizeModeHeight, (UiSizeMode?)value); } }
    public UiAutoMode? AutoModeWidth { get { return (UiAutoMode?)GetClassProp(UiPropName.AutoModeWidth); } set { SetProp(UiPropName.AutoModeWidth, (UiAutoMode?)value); } }
    public UiAutoMode? AutoModeHeight { get { return (UiAutoMode?)GetClassProp(UiPropName.AutoModeHeight); } set { SetProp(UiPropName.AutoModeHeight, (UiAutoMode?)value); } }
    public UiDisplayMode? DisplayMode { get { return (UiDisplayMode?)GetClassProp(UiPropName.DisplayMode); } set { SetProp(UiPropName.DisplayMode, (UiDisplayMode?)value); } }
    public UiImageTiling? ImageTilingX { get { return (UiImageTiling?)GetClassProp(UiPropName.ImageTilingX); } set { SetProp(UiPropName.ImageTilingX, (UiImageTiling?)value); } }
    public UiImageTiling? ImageTilingY { get { return (UiImageTiling?)GetClassProp(UiPropName.ImageTilingY); } set { SetProp(UiPropName.ImageTilingY, (UiImageTiling?)value); } }
    public float? ImageScaleX { get { return (float?)GetClassProp(UiPropName.ImageScaleX); } set { SetProp(UiPropName.ImageScaleX, (float?)value); } }
    public float? ImageScaleY { get { return (float?)GetClassProp(UiPropName.ImageScaleY); } set { SetProp(UiPropName.ImageScaleY, (float?)value); } }
    public IUiTexture Texture { get { return (IUiTexture)GetClassProp(UiPropName.Texture); } set { SetProp(UiPropName.Texture, (IUiTexture)value); } }
    public UiFloatMode? FloatMode { get { return (UiFloatMode?)GetClassProp(UiPropName.FloatMode); } set { SetProp(UiPropName.FloatMode, (UiFloatMode?)value); } }
    public UiRenderMode? RenderMode { get { return (UiRenderMode?)GetClassProp(UiPropName.RenderMode); } set { SetProp(UiPropName.RenderMode, (UiRenderMode?)value); } }
    public UiAlignment? ContentAlignX { get { return (UiAlignment?)GetClassProp(UiPropName.ContentAlignX); } set { SetProp(UiPropName.ContentAlignX, (UiAlignment?)value); } }
    public UiAlignment? ContentAlignY { get { return (UiAlignment?)GetClassProp(UiPropName.ContentAlignY); } set { SetProp(UiPropName.ContentAlignY, (UiAlignment?)value); } }
    public float? Opacity { get { return (float?)GetClassProp(UiPropName.Opacity); } set { SetProp(UiPropName.Opacity, (float?)value); } }
    public UiOrientation? LayoutOrientation { get { return (UiOrientation?)GetClassProp(UiPropName.LayoutOrientation); } set { SetProp(UiPropName.LayoutOrientation, (UiOrientation?)value); } }
    public UiLayoutDirection? LayoutDirection { get { return (UiLayoutDirection?)GetClassProp(UiPropName.LayoutDirection); } set { SetProp(UiPropName.LayoutDirection, (UiLayoutDirection?)value); } }
    public float? PercentWidth { get { return (float?)GetClassProp(UiPropName.PercentWidth); } set { SetProp(UiPropName.PercentWidth, (float?)value); } }
    public float? PercentHeight { get { return (float?)GetClassProp(UiPropName.PercentHeight); } set { SetProp(UiPropName.PercentHeight, (float?)value); } }
    public UiWrapMode? TextWrap { get { return (UiWrapMode?)GetClassProp(UiPropName.TextWrap); } set { SetProp(UiPropName.TextWrap, (UiWrapMode?)value); } }
    public UiVStatMode? VAutoMode { get { return (UiVStatMode?)GetClassProp(UiPropName.VStatMode); } set { SetProp(UiPropName.VStatMode, (UiVStatMode?)value); } }
    public UiTextHyphens? Hyphens { get { return (UiTextHyphens?)GetClassProp(UiPropName.Hyphens); } set { SetProp(UiPropName.Hyphens, (UiTextHyphens?)value); } }

    #endregion
    #region Members 

    public UiProps _props = new UiProps(); //compiled / final props
    public bool IsPropsOnly { get; set; } = false;//For glyph, don't inherit parent or compile, and re-compile the class every time.. we set _props manually
    public WeakReference<UiStyleSheet> StyleSheet { get; private set; } = null;
    public long CompiledFrameId { get; private set; } = 0;
    public BitArray Changed { get { return _changed; } }

    private BitArray _owned = new BitArray((int)UiPropName.MaxUiProps);//This bitset tells us which props were set
    private BitArray _inherited = new BitArray((int)UiPropName.MaxUiProps);
    private BitArray _defaulted = new BitArray((int)UiPropName.MaxUiProps);
    private BitArray _changed = new BitArray((int)UiPropName.MaxUiProps);//props that changed during the last class compile
    private List<UiStyle> _superStyles = null;
    private List<string> _superStylesNames = null;//Translate this with Stylesheet.
    private bool _bMustTranslateInheritedStyles = false;
    private bool _bMustCompile = true;
    private long _changedFrameId = 0;

    #endregion
    #region Public: Methods

    public UiStyle(string name)
      : this(name, new List<string>() { })
    {
    }
    public UiStyle(string name, string inherted_style)
      : this(name, new List<string>() { inherted_style })
    {
    }
    public UiStyle(string name, List<string> inherted_styles)
    {
      Name = name;
      if (inherted_styles != null)
      {
        _superStylesNames = inherted_styles;
        _bMustTranslateInheritedStyles = true;
      }
    }
    public void SetInheritStyles(List<string> styles)
    {
      foreach (var s in styles)
      {
        InheritFrom(s);
      }
    }
    public void InheritFrom(string style)
    {
      _superStylesNames = _superStylesNames.ConstructIfNeeded();
      _superStylesNames.Add(style);
      _bMustTranslateInheritedStyles = true;
    }
    public void SetStyleSheet(UiStyleSheet s)
    {
      //swap stylesheet if already set.
      if (StyleSheet != null && StyleSheet.TryGetTarget(out var ss))
      {
        ss.RemoveStyle(this);
        StyleSheet = null;
      }
      StyleSheet = new WeakReference<UiStyleSheet>(s);
    }
    private void AutoSetModesForValues(UiPropName p, object? value)
    {
      if (p == UiPropName.Color)
      {
        if (value != null && this._props.RenderMode != UiRenderMode.Color)
        {
          this.RenderMode = UiRenderMode.Color;
        }
      }
      if (p == UiPropName.Texture)
      {
        if (value != null && this._props.RenderMode != UiRenderMode.Textured)
        {
          this.RenderMode = UiRenderMode.Textured;
        }
        else if (value == null && this._props.RenderMode == UiRenderMode.Textured)
        {
          this.RenderMode = UiRenderMode.Color;
        }
      }
      if (p == UiPropName.PercentWidth && value != null && this._props.SizeModeWidth != UiSizeMode.Percent)
      {
        this.SizeModeWidth = UiSizeMode.Percent;
      }
      if (p == UiPropName.PercentHeight && value != null && this._props.SizeModeHeight != UiSizeMode.Percent)
      {
        this.SizeModeHeight = UiSizeMode.Percent;
      }
      if (p == UiPropName.FixedWidth && value != null && this._props.SizeModeWidth != UiSizeMode.Fixed)
      {
        this.SizeModeWidth = UiSizeMode.Fixed;
      }
      if (p == UiPropName.FixedHeight && value != null && this._props.SizeModeHeight != UiSizeMode.Fixed)
      {
        this.SizeModeHeight = UiSizeMode.Fixed;
      }
    }
    public void SetProp(UiPropName p, object? value)
    {
      ValidateProp(p, value);

      //auto set rendermode 
      AutoSetModesForValues(p, value);

      //set a property 
      // * set value to null to clear/inherit value
      if (CheckValueEnabled(p, value))
      {
        SetClassValueDirect(p, value);
      }
    }
    static bool _debug_break_validate_props = true;
    private void ValidateProp(UiPropName p, object? value)
    {
      //debug/testing validation
      if (value != null && _debug_break_validate_props)
      {
        if (value.GetType() == typeof(float))
        {
          if (float.IsNaN((float)value)) { Gu.DebugBreak(); }
          if (float.IsInfinity((float)value)) { Gu.DebugBreak(); }
          if ((float)value != Gui2d.MaxSize)
          {
            if ((float)value > UiQuad.c_dbg_maxsize) { Gu.DebugBreak(); }
            if ((float)value < -UiQuad.c_dbg_maxsize) { Gu.DebugBreak(); }
          }
        }
        else if (value.GetType() == typeof(double))
        {
          if (double.IsNaN((double)value)) { Gu.DebugBreak(); }
          if (double.IsInfinity((double)value)) { Gu.DebugBreak(); }
          if ((double)value != Gui2d.MaxSize)
          {
            if ((double)value > UiQuad.c_dbg_maxsize) { Gu.DebugBreak(); }
            if ((double)value < -UiQuad.c_dbg_maxsize) { Gu.DebugBreak(); }
          }
        }
      }

    }
    public object? GetProp(UiPropName p)
    {
      //get the compiled property 
      //returns the compiled / owned value
      // * value will not be null
      return _props.Get(p);
    }
    public object? GetClassProp(UiPropName p)
    {
      //Get class property
      // * value will be null if prop is not owned.
      var owned = _owned.Get((int)p);
      if (!owned)
      {
        return null;
      }
      else
      {
        return GetProp(p);
      }
    }
    public float? L_Left(UiOrientation dir)
    {
      if (dir == UiOrientation.Horizontal) { return Left; }
      else if (dir == UiOrientation.Vertical) { return Top; }
      else { Gu.BRThrowNotImplementedException(); }
      return Left;
    }
    public void L_Left(UiOrientation dir, float value)
    {
      if (dir == UiOrientation.Horizontal) { Left = value; }
      else if (dir == UiOrientation.Vertical) { Top = value; }
      else { Gu.BRThrowNotImplementedException(); }
    }
    public void L_FixedWidth(UiOrientation dir, float value)
    {
      if (dir == UiOrientation.Horizontal) { FixedWidth = value; }
      else if (dir == UiOrientation.Vertical) { FixedHeight = value; }
      else { Gu.BRThrowNotImplementedException(); }
    }
    public void CompileStyleTree(UiStyleSheet s, long framestamp, UiStyle? style_DOM_parent, bool force)
    {
      //Compile.. for example: <div top="3"> <div class=" class1 class2 class1 " style="top:3" right="5">  into a single set of properties for each <div>
      // parent style (tag), <style style style> (classes), owned (inline)... <div class/style="parent stuff"> <div class="a b c b a" style="inline stuff.."
      if (!IsPropsOnly)
      {
        TranslateStyleNames(s);

        if (_bMustCompile || force)
        {

          //reset all prop bitflags
          _inherited.SetAll(false);
          _defaulted.SetAll(false);

          // DebugStorePropDetails();

          //https://www.w3.org/TR/CSS2/cascade.html#cascade
          foreach (var p in UiProps.InheritableFields)
          {
            if (!IsOwned(p.Key))
            {
              //not owned, get value from superclass
              if (!InheritFromSuperClasses(s, p.Key, p.Value, framestamp))
              {
                //if subclasses are not set, then try the parent DOM element, otherwise set to a default value
                //Ok super annoying, but we acutally need this because say you set opacity on a parent element to fade out, then the text should get the same opacity as it all should fade
                if (!InheritFromParentTag(style_DOM_parent, p.Key, p.Value))
                {
                  //No parent element, and, no styles, set to default.
                  SetDefaultValue(p.Key, p.Value);
                }
              }
            }

          }

          //DebugStorePropDetails();

          _changed.SetAll(false);
          CompiledFrameId = framestamp;
          _bMustCompile = false;
          _props.ValidateProps();
        }
      }
    }
    public bool PropIsOwnedOrInherited(UiPropName p)
    {
      var ss_owned = _owned.Get((int)p);
      var ss_inherited = _inherited.Get((int)p);
      //var ss_changed = _changed.Get((int)p);
      //Changed..is an issue we'd need an array of FrameSTamp's that determine that this element hasn't been updated.
      // Then we're duplicating tons of data, and just copying everything would be more performant than Dictionry<>
      bool ret = ss_owned || ss_inherited;
      return ret;
    }
    public UiStyle Clone()
    {
      UiStyle ret = new UiStyle(this.Name + Lib.CopyName, this._superStylesNames);
      ret._props = _props.Clone();
      ret._bMustCompile = true;
      return ret;
    }

    #endregion
    #region Private: Methods

    private bool IsOwned(UiPropName p)
    {
      return _owned.Get((int)p);
    }
    private bool InheritFromParentTag(UiStyle style_DOM_parent, UiPropName pname, System.Reflection.FieldInfo fieldinfo)
    {
      //Return true if successfully inherited.
      if (style_DOM_parent != null)
      {
        return InheritValue(style_DOM_parent, pname, fieldinfo);
      }
      return false;
    }
    private bool InheritFromSuperClasses(UiStyleSheet s, UiPropName pname, System.Reflection.FieldInfo fieldinfo, long framestamp)
    {
      //Return true if successfully inherited.
      if (_superStyles != null && _superStyles.Count > 0)
      {
        //Apply styles in the reverse order <div class="a b c " .. c..b..a
        for (int i = _superStyles.Count - 1; i >= 0; i--)
        {
          if (_superStyles[i] != null)
          {
            if (!_superStyles[i].IsInline)
            {
              _superStyles[i].CompileStyleTree(s, framestamp, null, false);
              return InheritValue(_superStyles[i], pname, fieldinfo);
            }
            else
            {
              s.StyleError($"'{Name}' A Super style '{_superStyles[i].Name}' was Inline, but was also inherited. This is a bug.");
              Gu.DebugBreak();
            }
          }
          else
          {
            s.StyleError($"'{Name}' A Super style was null. This is a bug.");
            Gu.DebugBreak();
          }
        }
      }
      return false;
    }
    private void SetDefaultValue(UiPropName p, System.Reflection.FieldInfo fi)
    {
      var val = UiProps.Default(p);
      fi.SetValue(this._props, val);
      _defaulted.Set((int)p, true);
    }
    private bool InheritValue(UiStyle fromStyle, UiPropName p, System.Reflection.FieldInfo fi)
    {
      //if the property is owned by the given class, then, we can set it
      //Return true if successfully inherited.
      if (fromStyle.PropIsOwnedOrInherited(p))
      {
        var val = fromStyle.GetProp(p);
        fi.SetValue(this._props, val);
        _inherited.Set((int)p, true);
        return true;
      }
      return false;
    }
    private bool CheckValueEnabled(UiPropName p, object? new_class_value)
    {
      //Check if we set the prop to null (disabled it) 
      var owned = _owned.Get((int)p);
      if (new_class_value == null && owned == false)
      {
        //Class value is unset, and was set it to null again.. no change
        return false;
      }
      else if (new_class_value == null && owned == true)
      {
        //Class value is being cleared
        return true;
      }
      else if (new_class_value != null && owned == false)
      {
        //Class value is unset, and we set a fresh value... definite change
        return true;
      }
      else if (new_class_value != null && owned == true)
      {
        //Class value is set, and we set a new value check for value difference (prevent recompiling all classes!)
        var cur_prop_val = _props.Get(p);
        if (!cur_prop_val.Equals(new_class_value))
        {
          return true;
        }
      }

      return false;
    }
    private void SetClassValueDirect(UiPropName p, object? value)
    {
      //Set the class value for this style, skipping over modified value checking.
      if (value != null)
      {
        //Only set the prop value if not null, as, null is basically the way we say "clear the value"
        _owned.Set((int)p, true);
        _props.Set(p, value);
      }
      else
      {
        //the property was cleared.
        _owned.Set((int)p, false);
      }
      _changed.Set((int)p, true);
      _changedFrameId = Gu.Context.FrameStamp;
      _bMustCompile = true;
    }
    private void TranslateStyleNames(UiStyleSheet sheet)
    {
      //style names are strings so get their style classes from the stylesheet
      if (_bMustTranslateInheritedStyles == true)
      {
        Gu.Assert(sheet != null);
        _superStyles = _superStyles.ConstructIfNeeded();
        _superStyles?.Clear();
        if (_superStylesNames != null)
        {
          foreach (var sn in _superStylesNames)
          {
            var style = sheet.GetStyle(sn);
            if (style == null)
            {
              sheet.StyleError($"Style '{Name}': Could not find inherited style '{sn}'.");
            }
            else
            {
              _superStyles.Add(style);
            }
          }
        }
        _bMustTranslateInheritedStyles = false;
      }
    }

    #endregion

  }//cls
  public class UiStyleSheet
  {
    //stylesheet for css-like styling
    private Dictionary<string, UiStyle> Styles = new Dictionary<string, UiStyle>();
    private List<string> _errors = new List<string>();
    public string Name { get; private set; } = Lib.UnsetName;

    public UiStyleSheet(string name)
    {
      Name = name + "-ss";
    }
    public void Update()
    {
      if (_errors != null && _errors.Count > 0)
      {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"");
        sb.AppendLine($"StyleSheet '{Name}', Context '{Gu.Context.Name}' has errors: ");
        foreach (var e in _errors)
        {
          sb.AppendLine("-> " + e);
        }
        _errors.Clear();
        Gu.Log.Error(sb.ToString());
      }
    }
    public void StyleError(string error)
    {
      this._errors.Add(error);
    }
    public void AddStyles(List<UiStyle> styles)
    {
      foreach (var s in styles)
      {
        AddStyle(s);
      }
    }
    public void AddStyle(UiStyle s)
    {
      //Styles can
      s.SetStyleSheet(this);
      Gu.Assert(s.Name != null);
      if (Styles.ContainsKey(s.Name))
      {
        Gu.Log.Error($"{this.Name} style {s.Name} was already added.");
        Gu.DebugBreak();
      }
      else
      {
        Styles.Add(s.Name, s);
      }
    }
    public bool RemoveStyle(UiStyle s)
    {
      var b = Styles.Remove(s.Name);
      return b;
    }
    public UiStyle GetStyle(string s)
    {
      Styles.TryGetValue(s, out var x);
      return x;
    }
  }

  #endregion
  #region Glyph/Text

  public class UiGlyph : UiBlock
  {
    public vec4? GlyphColor { get { return _glyphcolor; } set { _glyphcolor = value; } }
    public override float WordWidth { get { return _wordWidth; } }
    public override int WordID { get { return _word_id; } }
    public override int WordCharID { get { return _word_char_id; } }
    public override float WordHeight { get { return _wordHeight; } }
    public override UiDisplayMode DisplayMode { get { return _displayMode; } }

    public UiDisplayMode _displayMode = UiDisplayMode.Inline;//ok
    public float _wordWidth = -1;
    public float _wordHeight = -1;
    public int _word_id = -1; //all chars in word keep same wordid
    public int _word_char_id = -1; //id of character in word
    public vec4? _glyphcolor = null;
    public float _lineHeight = -1;
    public MtFontChar? _cachedGlyph = null;
    public int _code;
    public float _advance = 0; // advance width
    public float _top_off = 0; // top (ceneterline for static layint is center of line but origin is top left)
    public float _left_off = 0; // top (ceneterline for static layint is center of line but origin is top left)
    public float _justify_sp = 0; // top (ceneterline for static layint is center of line but origin is top left)
    public UiGlyph? _next = null;
    public UiGlyph? _prev = null;

    //**DEBUG ONLY
    public string _dbg_word = "";

    public UiGlyph()
    {
    }
    public override float L_Left(UiOrientation o)
    {
      return (o == UiOrientation.Horizontal) ? _left_off : _top_off;
    }
    public override float L_Top(UiOrientation o)
    {
      return L_Left(UiUtils.Perp(o));
    }
    public override float L_Advance()
    {
      return _advance;
    }
    public override float L_PixelLineHeight()
    {
      return _lineHeight;
    }
    public UiGlyph Clone()
    {
      UiGlyph gg = new UiGlyph();
      gg._block = this._block.Clone();
      gg._displayMode = this._displayMode;
      gg._wordWidth = this._wordWidth;
      gg._wordHeight = this._wordHeight;
      gg._word_id = 0;
      gg._word_char_id = 0;
      gg._glyphcolor = this._glyphcolor;
      gg._lineHeight = this._lineHeight;
      gg._cachedGlyph = this._cachedGlyph;
      gg._code = this._code;
      gg._advance = this._advance;
      gg._top_off = this._top_off;
      gg._left_off = this._left_off;
      gg._next = null;
      gg._prev = null;
      return gg;
    }
  }
  public class UiFontLayoutData
  {
    public UiFontLayoutData()
    {
    }
    public float _fontHeight = 0;
    public float _lineHeight;
    public int _word_id = 0;
    public int _word_char_id = 0;
    public UiWrapMode _textWrap = 0;
    public UiGlyph? _last = null;
    public int _chlast = 0;
    public int _ch = 0;
    public int _chnext = 0;
    public int _ci = 0;
    public vec4? _color = null;
    public string _text;
    public float _fontscale = 1;
    public float _emscale = 1;
    public float _tracking = 1;

    public UiGlyph? _wordstart = null;
    public float _wordWidth = 0;
    public float _wordHeight = 0;
    public string _dbg_curWord = "";

    public MtFontPatch? _patch;
    public MtFontLoader? _loader;
    public UiTextHyphens _hyphens;
  }
  public class UiGlyphCache
  {
    //was supposed to be a cache of some kind but there was no reason (right now)
    public UiGlyphCache() { }
    public bool _round = true;//round the position of text

    public void CreateGlyphs(UiFontInfo fontInfo, MtFontLoader loader, MtFontPatch patch, string in_text, FileLoc face,
    float fontHeight, UiWrapMode wrap, float lineheight, float tracking, UiTextHyphens hyphens)
    {
      //create glyphs
      //TODO: we were caching GetFont before. See if this slows things down.
      Gu.Assert(loader != null);
      Gu.Assert(patch != null);

      UiFontLayoutData dat = new UiFontLayoutData();
      dat._text = in_text;
      dat._loader = loader;
      dat._patch = patch;
      dat._fontHeight = fontHeight;
      dat._textWrap = wrap;
      dat._fontscale = patch.GetScaleForPixelSize(dat._fontHeight);
      dat._emscale = patch.ScaleForPixelHeight;
      dat._lineHeight = loader.LineHeight * patch.ScaleForPixelHeight * lineheight;
      dat._tracking = tracking;
      dat._hyphens = hyphens;

      fontInfo._glyphs = new List<UiGlyph>();

      for (dat._ci = 0; dat._ci < dat._text.Length; dat._ci++)
      {
        dat._chlast = dat._ch;
        dat._ch = dat._text[dat._ci];
        dat._chnext = (dat._ci + 1) < dat._text.Length ? dat._text[dat._ci + 1] : 0;

        if (!ProcessFontHTML(dat))
        {
          var gc = CreateGlyph(dat);
          ProcessGlyph(dat, gc);
          fontInfo._glyphs.Add(gc);
          dat._last = gc;
        }
      }

    }
    private UiGlyph CreateGlyph(UiFontLayoutData dat)
    {
      // scale = fontsize / bakedcharsize (pixels)
      // font_scale = converts ttf units to pixel units
      MtFontChar cg = null;
      if (!dat._patch.GetChar(dat._ch, dat._fontHeight, out cg))
      {
        Gu.Log.Debug($"Glpyh char '{(char)dat._ch}' (u={dat._ch}) not found");//Unicode ch not found
        Gu.DebugBreak();//Unicode ch not found
        return null;
      }

      float gadvance = PixAlign(cg.ch_advance * dat._emscale * dat._fontscale * dat._tracking);
      float gascent = PixAlign(dat._loader.Ascent * dat._emscale * dat._fontscale); //  cg.font_ascent) 
      float gkern = PixAlign(dat._loader.GetKerning(dat._patch, dat._ch, dat._chnext) * dat._emscale * dat._fontscale);
      float gleft = PixAlign(cg.ch_left * dat._fontscale);
      float gtop = PixAlign(cg.ch_top * dat._fontscale);
      float gright = PixAlign(cg.ch_right * dat._fontscale);
      float gbot = PixAlign(cg.ch_bot * dat._fontscale);
      float gwidth = (gright - gleft);
      float gheight = (gbot - gtop);

      //we need padding due to the width of the glyph being the texture quad, plus, space to the next (which has no texture!)
      UiGlyph gc = new UiGlyph();

      gc._cachedGlyph = cg;
      gc._code = dat._ch;
      gc._glyphcolor = dat._color;

      gc._block._b2LayoutQuad._left = 0;
      gc._block._b2LayoutQuad._top = 0;
      gc._block._b2LayoutQuad._width = gwidth;
      gc._block._b2LayoutQuad._height = gheight;
      gc._block._b2LayoutQuad.ValidateQuad();
      gc._lineHeight = dat._lineHeight;
      //**NOTE: advance is not the same as font advance, It is additional width added, or removed from quad width to get next position
      gc._advance = (gadvance + gkern) - gwidth - gleft;
      gc._top_off = gtop + gascent;  //push  glyph to bottom  line
      gc._left_off = gleft;
      gc._wordWidth = 0;
      gc._wordHeight = 0;
      if (dat._last != null)
      {
        dat._last._next = gc;
        gc._prev = dat._last;
      }

      //**testing
      //gc._lineHeight = PixAlign(gc._lineHeight);
      //gc._advance = PixAlign(gc._advance);

      return gc;
    }
    private float PixAlign(float value)
    {
      //Must be pixel aligned.
      if (_round)
      {
        return (float)Math.Round(value);
      }
      return value;
    }
    private void ProcessGlyph(UiFontLayoutData dat, UiGlyph gc)
    {
      bool chws = char.IsWhiteSpace((char)dat._ch);
      bool lastws = char.IsWhiteSpace((char)dat._chlast);

      if (dat._textWrap == UiWrapMode.None)
      {
        gc._displayMode = UiDisplayMode.NoWrap;
      }
      else if (dat._hyphens == UiTextHyphens.Manual && (dat._ch == 0x8208))
      {
        //There are several unicode hyphens but this one seems to be the most used in HTML
        gc._displayMode = UiDisplayMode.Block;
      }
      else if (dat._textWrap == UiWrapMode.Char)
      {
        gc._displayMode = UiDisplayMode.Inline;
      }
      else if (dat._textWrap == UiWrapMode.Line)
      {
        if (dat._ch == '\n')
        {
          gc._displayMode = UiDisplayMode.Block;
        }
        else
        {
          gc._displayMode = UiDisplayMode.NoWrap;
        }
      }
      else if (dat._textWrap == UiWrapMode.Word)
      {
        if (dat._ch == '\n')
        {
          //end word
          gc._displayMode = UiDisplayMode.Block;
          EndWord(dat);
        }
        else if (chws)
        {
          //end word
          //keep ws inline / allow wrap
          gc._displayMode = UiDisplayMode.Inline;
          EndWord(dat);
        }
        else if (!chws)
        {
          if (dat._wordstart == null)
          {
            //start word
            dat._word_char_id = 0;
            gc._word_id = dat._word_id++;
            gc._word_char_id = dat._word_char_id++;
            dat._wordstart = gc;
            gc._displayMode = UiDisplayMode.Word;
            dat._wordWidth = dat._wordHeight = 0;
            StringUtil.GetTok(dat._text, dat._ci, ref dat._dbg_curWord);
            if (dat._dbg_curWord.Contains("consectetur"))
            {
              Gu.Trap();
            }
          }
          else
          {
            //continue word
            gc._word_id = dat._wordstart.WordID;
            gc._word_char_id = dat._word_char_id++;
            gc._displayMode = UiDisplayMode.NoWrap;
          }
        }
      }


      if (dat._wordstart != null)
      {
        dat._wordWidth += gc._block._b2LayoutQuad._width + gc._advance;
        dat._wordHeight = Math.Max(dat._wordHeight, gc._block._b2LayoutQuad._height);
      }
      gc._dbg_word = dat._dbg_curWord;
    }
    private void EndWord(UiFontLayoutData dat)
    {
      if (dat._wordstart != null)
      {
        dat._wordstart._wordWidth = dat._wordWidth;// dat._advanceWidth + Math.Abs(dat._head) + dat._tail;
        dat._wordstart._wordHeight = dat._wordHeight;
        //dat._tail = dat._head = dat._advanceWidth = 
        dat._wordWidth = dat._wordHeight = 0;
        dat._wordstart = null;
        dat._dbg_curWord = "";
      }
    }
    private bool ProcessFontHTML(UiFontLayoutData dat)
    {
      if (dat._ch == '<')
      {
        var idx = dat._text.IndexOf('>', dat._ci);
        if (idx >= 0 && (idx - dat._ci) < 30) // prevent long tags that arent supported
        {
          string sub = dat._text.Substring(dat._ci + 1, idx - dat._ci - 1);
          vec4 rgba;
          if (ByteParser.TryParseVec4RGBA(sub, out rgba))
          {
            //**COLOR:
            //allow <rgba(f,f,f,f)> and <reset> reset will reset the color to the FontColor of the element.
            //would be more efficient to just process the html text (well, the whole UI) into blocks with tags, this is a huge thing to do ..in the future.
            dat._color = rgba;
          }
          else if (sub.ToLower().Equals("reset"))
          {
            dat._color = null;
          }
          dat._ci = idx;
        }
        return true;
      }
      return false;
    }

  }

  #endregion
  #region Event

  public class UiEventState
  {
    //shared UI state for events
    public Gui2d? Gui { get; private set; } = null;//must be set manuallly
    public UiElement? Previous { get; private set; } = null;
    public UiElement? Current { get; private set; } = null;
    public ButtonState LeftButtonState { get; private set; }
    public ButtonState RightButtonState { get; private set; }
    public vec2 MousePosCur { get; private set; }
    public vec2 MousePosLast { get; private set; }
    public vec2? DragDelta { get; private set; } = null; //position of mouse before LMB was down
    public UiElement? Focused { get; private set; } = null;//click focus
    public vec2 Scroll { get; private set; }

    public UiEventState(Gui2d g, vec2 mpos_cur, vec2 mpos_last, UiElement? prev_pick, UiElement? cur_pick, ButtonState leftState, ButtonState rightState, UiElement? pressFocus, vec2 scroll, vec2? dragdelta)
    {
      Gui = g;
      MousePosCur = mpos_cur;
      MousePosLast = mpos_last;
      Previous = prev_pick;
      Current = cur_pick;
      LeftButtonState = leftState;
      RightButtonState = rightState;
      Focused = pressFocus;
      Scroll = scroll;
      DragDelta = dragdelta;
    }
    public bool IsAnyGuiItemPicked()
    {
      return Current != null;
    }
  }//cls
  public class UiEvent
  {
    //UiEvent is unique to each event. 
    //UiEventState is shared among events
    public UiEventId EventId { get; private set; } = UiEventId.Undefined;
    public UiElement Element { get; private set; } //We could store a weak reference here, assuming at some point the Gui system may add/delete non-glyph elements
    public UiEventState? State { get; set; } = null;
    public bool PreventDefault { get; set; } = false;

    private UiEventThing _eventThing;

    public UiEvent(UiEventId id, UiElement ele)
    {
      Gu.Assert(ele != null);
      Element = ele;
      EventId = id;
    }
    public void Fire()
    {
      Gu.Assert(Element != null);
      Element.DoMouseEvents(this, false);
    }

  }
  public class UiEventThing
  {
    //captures input from the user while the UI is updating asynchronously.
    //trying to do this without registering for events from children - "passive" events
    private int c_iMaxEvents = 500;

    private ButtonState _eLast_Lmb = ButtonState.Up;
    private ButtonState _eLast_Rmb = ButtonState.Up;
    private ButtonState _eLast_Mmb = ButtonState.Up;
    public List<UiEvent> _events = new List<UiEvent>();
    private UiElement? _pressFocusLast = null;
    private UiElement? _focused = null;
    private List<UiEvent> _new_events_frame = new List<UiEvent>();

    public UiEventThing()
    {
      c_iMaxEvents = Gu.EngineConfig.MaxUIEvents;
      c_iMaxEvents = Math.Clamp(c_iMaxEvents, 0, 9999999);
    }
    public void PollForEvents(Gui2d g)
    {
      //Poll each frame
      //  sends max 20 or so events to at most 2 objects, last/cur picked
      Gu.Assert(g != null);
      if (_events.Count >= c_iMaxEvents)
      {
        Gu.Log.Error($"Too many UI events! max={c_iMaxEvents}");
        Gu.DebugBreak();
        return;
      }

      _new_events_frame.Clear();

      var picker = Gu.Context.Picker;
      UiElement? elast = null;
      UiElement? ecur = null;

      //update picked
      if (picker.PickedObjectFrameLast is UiElement)
      {
        elast = picker.PickedObjectFrameLast as UiElement;
      }
      if (picker.PickedObjectFrame is UiElement)
      {
        ecur = picker.PickedObjectFrame as UiElement;
      }

      //button events
      var lb = Gu.Context.PCMouse.State(MouseButton.Left);
      var rb = Gu.Context.PCMouse.State(MouseButton.Right);
      var mpos = Gu.Context.PCMouse.Pos;
      var mlast = Gu.Context.PCMouse.LastPos;
      var scroll = Gu.Context.PCMouse.ScrollDelta;
      var focus = _focused;

      //press focus "Focus" (drag / mouse down)
      //  "the element being dragged/interacted while mouse HELD"
      //  mouse need not be hovering over element to have press focus.      
      if ((Gu.Context.GameWindow.IsFocused == false) || ((lb == ButtonState.Release || lb == ButtonState.Up) && (_focused != null)))
      {
        var pold = _focused;
        _focused = null;
        SendEvent(UiEventId.Lost_Focus, pold);
      }
      else if (lb == ButtonState.Press)
      {
        var pold = _focused;
        if (Gu.TryGetSelectedView(out var vv))
        {
          _focused = ecur;
        }
        SendEvent(UiEventId.Lost_Focus, pold);
        SendEvent(UiEventId.Got_Focus, _focused);
      }


      //lmb / rmb
      if (lb != _eLast_Lmb)
      {
        if (ecur != null)
        {
          if (lb == ButtonState.Up)
          {
            SendEvent(UiEventId.LmbUp, ecur);
          }
          if (lb == ButtonState.Press)
          {
            SendEvent(UiEventId.LmbPress, ecur);
          }
          if (lb == ButtonState.Hold)
          {
            SendEvent(UiEventId.LmbHold, ecur);
          }
          if (lb == ButtonState.Release)
          {
            SendEvent(UiEventId.LmbRelease, ecur);
          }
        }
      }
      if (rb != _eLast_Rmb)
      {
        if (ecur != null)
        {
          if (rb == ButtonState.Up)
          {
            SendEvent(UiEventId.RmbUp, ecur);
          }
          if (rb == ButtonState.Press)
          {
            SendEvent(UiEventId.RmbPress, ecur);
          }
          if (rb == ButtonState.Hold)
          {
            SendEvent(UiEventId.RmbHold, ecur);
          }
          if (rb == ButtonState.Release)
          {
            SendEvent(UiEventId.RmbRelease, ecur);
          }
        }
      }

      //mouse move
      if (elast != null && elast != ecur)
      {
        SendEvent(UiEventId.Mouse_Leave, elast);
      }
      if (ecur != null && elast != ecur)
      {
        SendEvent(UiEventId.Mouse_Enter, ecur);
      }
      if (ecur != null && elast == ecur && mpos != mlast)
      {
        SendEvent(UiEventId.Mouse_Move, ecur);
      }

      //mouse wheel
      if (scroll.x != 0 || scroll.y != 0)
      {
        SendEvent(UiEventId.Mouse_Scroll, ecur);
      }

      //Pressed item 
      vec2? dragDelta = null;
      if ((mpos != mlast) && (lb == ButtonState.Hold) && (_eLast_Lmb == ButtonState.Press || _eLast_Lmb == ButtonState.Hold))
      {
        dragDelta = mpos - mlast;
        SendEvent(UiEventId.LmbDrag, focus);
      }

      //Update state after events are sent (we use state in event)
      _eLast_Lmb = lb;
      _eLast_Rmb = rb;

      //send
      if (_new_events_frame.Count > 0)
      {
        var state = new UiEventState(g, mpos, mlast, elast, ecur, lb, rb, focus, scroll, dragDelta);
        foreach (var ev in _new_events_frame)
        {
          ev.State = state;
        }
        _events.AddRange(_new_events_frame);

        _new_events_frame.Clear();
      }
    }
    private void SendEvent(UiEventId evid, UiElement? ele)
    {
      if (ele != null && ele.Events != null && ele.Events.Keys.Contains(evid))
      {
        _new_events_frame.Add(new UiEvent(evid, ele));
      }
    }
  }

  #endregion
  #region UiElement

  public class UiSortKeys
  {
    //claculate sub-sort layers for all element quads
    public uint _ekey;  //ele
    public uint _edbg;  //debug
    public uint _bdkey; //border
    public uint _gkey;  //glyphs
    public uint _gdb;   //glyphs debug

    int c_iSubkeyCount = 10;

    public UiSortKeys(int sort, int layer)
    {
      //lower index = higher sort
      _ekey = (uint)(layer * UiElement.c_iMaxLayers + sort * c_iSubkeyCount);
      _gkey = _ekey + 3;
      _gdb = _ekey + 4;
      _bdkey = _ekey + 5;
      _edbg = _ekey + 6;
    }
  }
  public class UiBlockInfo
  {
    //Base Block Information 
    public UiQuad _b2ContentQuad = new UiQuad(); //final w/h of all contents
    public UiQuad _b2LayoutQuad = new UiQuad(); // content + (margin + padding + border)
    public UiQuad _b2LayoutQuadLast = new UiQuad();
    public UiQuad _b2ClipQuad = new UiQuad(); // all floating, and contained elements, clamped to min/max w/h. *clip quad may not equal to border quad if there are floating elements
    public UiQuad _b2MarginQuad = new UiQuad(); // Final quad. MBP+content
    public UiQuad _b2BorderQuad = new UiQuad(); // Final quad. BP+content
    public UiQuad _b2BorderQuadLast = new UiQuad();
    public UiQuad _b2ContentClip = new UiQuad();//intersection of this clip adn parent clip
    public UiInt2 _index = new UiInt2(); // static index of this element relative to other in-line elements (id, lineid)
    public UiContainerInfo? _container = null; // if element, not glyph
    public UiLayoutLine? _line = null; //ref to line if static
    public UiBlock? _previous = null; //ref to previous if static
    public uint _rootPickId = 0;

    public UiBlockInfo Clone()
    {
      var bi = new UiBlockInfo();
      bi._b2ContentQuad = this._b2ContentQuad.Clone();
      bi._b2LayoutQuad = this._b2LayoutQuad.Clone();
      bi._b2LayoutQuadLast = this._b2LayoutQuadLast.Clone();
      bi._b2ClipQuad = this._b2ClipQuad.Clone();
      bi._b2MarginQuad = this._b2MarginQuad.Clone();
      bi._b2BorderQuad = this._b2BorderQuad.Clone();
      bi._b2BorderQuadLast = this._b2BorderQuadLast.Clone();
      bi._b2ContentClip = this._b2ContentClip.Clone();
      bi._index = new UiInt2();
      bi._container = null;
      bi._line = null;
      bi._previous = null;
      bi._rootPickId = 0;
      return bi;
    }
  }
  public class UiContainerInfo
  {
    // uielement
    public UiSize4 _MBP = new UiSize4();
    public UiSize2 _minWH = new UiSize2();
    public UiSize2 _maxWH = new UiSize2();
    public UiSize4 _margin = new UiSize4();
    public UiSize4 _padding = new UiSize4();
    public UiSize4 _border = new UiSize4();
    public UiSize2 _contentWH = new UiSize2(0, 0);//space of all positional elements + all static elements
    public UiSize2 _outerMaxWH = new UiSize2(0, 0);//w/h + Margin + Border + Padding
    public UiSize2 _contentMaxWH = new UiSize2(0, 0);//w/h - (Margin+Border+Padding)
    public UiStaticInfo? _static = null; //static layout info, if has statics
  }
  public class UiStaticInfo
  {
    //static layout
    public UiLayoutLine? _contentLine = null;
    public List<UiLayoutLine>? _lines = null; //rows this element contains
    public List<UiLayoutLine>? _glyphlines = null; //rows this element contains
    public List<UiLayoutLine>? _autoLinesH = null;
    public List<UiLayoutLine>? _autoLinesV = null;
    public UiSize2 _glyphWH = new UiSize2();
    public UiSize2 _elementWH = new UiSize2();
    public UiSize2 _staticWH = new UiSize2(); //space of all static elements
    public UiBlock? _last = null;
  }
  public class UiFontInfo
  {
    public string _strText = "";
    public List<UiGlyph>? _glyphs = null;
    public MtFontLoader? _cachedFont = null;
    public bool _textChanged = false;
    public bool _bNeverDoneText = true;
    public int _baseGlyphCount = 0; // glyphs before generated stuff
  }
  public class UiLayoutLine //uilayoutline , uiline
  {
    public const int UnsetLineIndex = -999999;
    public const int ContainerLineIndex = -1;

    public UiQuad _quad = new UiQuad();
    public List<UiBlock> _eles = new List<UiBlock>();
    public UiAutoInfo? _autoInfo = null; //horizontal autos
    public UiLayoutLine? _previous = null;//previous line
    public int _index = -1;
    public int _wordCount = 0;
    public UiLayoutLine(int index)
    {
      _index = index;
    }
  }
  public class UiAutoInfo
  {
    //Auto Info is for a single dimension
    public List<UiElement> _hautos = new List<UiElement>();
    public List<UiElement> _vautos = new List<UiElement>();
    public float _auto_maxw = 0;//maximum of auto size across layout direction
    public float _auto_maxh = 0; //max perp auto size on line
  }
  public class UiElement : UiBlock, IUiElement
  {
    public const int c_iDefaultSort = 999999;
    public const int c_iMaxLayers = 100000;//should be bigger than count of all glyphs + uielements
    public const int c_iMaxChildren = c_iMaxLayers - 1;

    #region Public: Members

    public string Name { get { return _name; } set { _name = value; } }
    public string Tag { get; set; } = "";
    public virtual string Text
    {
      get
      {
        if (_font != null)
        {
          return _font._strText;
        }
        else
        {
          return "";
        }
      }
      set
      {
        if (_font == null && !String.IsNullOrEmpty(value))
        {
          _font = new UiFontInfo();
          _font._strText = value;
        }
        else if (_font != null && !StringUtil.Equals(_font._strText, value))
        {
          _font._strText = value;
          _font._textChanged = true;
          _contentChanged = true;
        }
      }
    }
    public IUiStyle Style { get { return _style; } }
    public bool Visible { get { return _visible; } set { _visible = value; } }// ** May be on style .. 
    public Dictionary<UiEventId, List<UiAction>> Events { get { return _events; } set { _events = value; } }
    public UiQuad ContentQuad { get { return _block._b2ContentQuad; } }
    public UiQuad BorderQuad { get { return _block._b2BorderQuad; } }
    public UiQuad MarginQuad { get { return _block._b2MarginQuad; } }
    public UiElement? Parent { get { return _parent; } }
    public bool TopMost { get { return _topMost; } set { _topMost = value; } }
    public override UiDisplayMode DisplayMode { get { return _style._props.DisplayMode; } }
    public int Sort { get { return _sort; } set { _sort = value; } }
    public bool PickEnabled { get { return _pickEnabled; } set { _pickEnabled = value; UpdatePickID(); } }
    public int ChildCount { get { return _children == null ? 0 : _children.Count; } }

    //EVENTS
    public void SetContentChanged() { _contentChanged = true; }
    public Action? OnResize = null;
    public virtual void OnGotKeyboardFocus() { }//synchronous
    public virtual void OnLostKeyboardFocus() { }//synchronous
    public virtual void OnKeyPress(Keys key) { }//synchronous

    #endregion
    #region Private: Members

    protected uint _iPickId = Picker.c_iInvalidPickId;
    protected string _name = "";
    protected UiStyle _style = new UiStyle("inline");
    protected List<UiElement>? _children = null;
    protected int _sort = -1;  //sort order within layer
    private UiElement? _parent = null;
    private Dictionary<UiEventId, List<UiAction>>? _events = null;
    private UiStaticInfo _static { get { return this._block._container._static; } set { this._block._container._static = value; } }
    private UiContainerInfo _container { get { return this._block._container; } set { this._block._container = value; } }
    public UiFontInfo? _font = null;

    //Flags (TODO: bitset)
    private bool _pickEnabled = false;
    private bool _visible = true;
    private bool _dragEnabled = false;
    private bool _contentChanged = true;
    private bool _topMost = false;

    #endregion
    #region Methods

    public UiElement()
    {
    }
    public UiElement(string name) : this()
    {
      Init(name);
    }

    public override float L_Left(UiOrientation o)
    {
      return (o == UiOrientation.Horizontal) ? _style._props.Left : _style._props.Top;
    }
    public override float L_Top(UiOrientation o)
    {
      return L_Left(UiUtils.Perp(o));
    }
    public override float L_Advance()
    {
      return 0;//glyphs only
    }
    public override float L_PixelLineHeight()
    {
      return 0;//glyphs only
    }


    public void ToggleVisible()
    {
      ShowOrHide(!_visible, 0);
    }
    public void Hide(int fadems = 0)
    {
      ShowOrHide(false, fadems);
    }
    public void Show(int fadems = 0)
    {
      ShowOrHide(true, fadems);
    }
    public void Hide(string name)
    {
      ShowOrHideByName(name, false);
    }
    public void Show(string name)
    {
      ShowOrHideByName(name, true);
    }
    public bool TryGetGui2dRoot(out Gui2d? ret)
    {
      ret = null;
      UiElement? parent = this.Parent;
      while (parent != null && parent.Parent != null)
      {
        parent = parent.Parent;
      }
      if (parent is Gui2d)
      {
        ret = parent as Gui2d;
      }
      return ret != null;
    }
    public bool Animate(UiPropName prop, double value, int durationMS, int repeatCount = 0)
    {
      if (TryGetGui2dRoot(out var g))
      {
        var pa = new UiPropAnimation<double>(this, prop, value, durationMS, repeatCount);
        g.AddAnimation(pa);
        return true;
      }
      return false;
    }
    public bool Animate(UiPropName prop, float value, int durationMS, int repeatCount = 0)
    {
      if (TryGetGui2dRoot(out var g))
      {
        var pa = new UiPropAnimation<float>(this, prop, value, durationMS, repeatCount);
        g.AddAnimation(pa);
        return true;
      }
      return false;
    }
    public bool Animate(UiPropName prop, vec4 value, int durationMS, int repeatCount = 0)
    {
      if (TryGetGui2dRoot(out var g))
      {
        var pa = new UiPropAnimation<vec4>(this, prop, value, durationMS, repeatCount);
        g.AddAnimation(pa);
        return true;
      }
      return false;
    }
    public bool ShowOrHideByName(string name, bool show, bool stop_at_first = false)
    {
      if (_children != null)
      {
        foreach (var ele in _children)
        {
          if (StringUtil.Equals(ele.Name, name))
          {
            ele.ShowOrHide(show, 0);
            if (stop_at_first)
            {
              return false;
            }
          }
          else
          {
            bool stopdoing = ele.ShowOrHideByName(name, show, stop_at_first);
            if (stopdoing == false)
            {
              return false;
            }
          }
        }
      }
      return true;
    }
    public UiSize4 GetMarginBorderPadding(UiLayoutGlobals dd)
    {
      if (dd.DisableMargins && dd.DisableBorders && dd.DisablePadding)
      {
        return new UiSize4();
      }
      return new UiSize4()
      {
        _top = (dd.DisableMargins ? 0 : _style._props.MarginTop) + (dd.DisableBorders ? 0 : _style._props.BorderTop) + (dd.DisablePadding ? 0 : _style._props.PadTop),
        _right = (dd.DisableMargins ? 0 : _style._props.MarginRight) + (dd.DisableBorders ? 0 : _style._props.BorderRight) + (dd.DisablePadding ? 0 : _style._props.PadRight),
        _bot = (dd.DisableMargins ? 0 : _style._props.MarginBot) + (dd.DisableBorders ? 0 : _style._props.BorderBot) + (dd.DisablePadding ? 0 : _style._props.PadBot),
        _left = (dd.DisableMargins ? 0 : _style._props.MarginLeft) + (dd.DisableBorders ? 0 : _style._props.BorderLeft) + (dd.DisablePadding ? 0 : _style._props.PadLeft)
      };
    }
    public UiSize4 GetMarginAndBorder(UiLayoutGlobals dd)
    {
      if (dd.DisableMargins && dd.DisableBorders)
      {
        return new UiSize4();
      }
      return new UiSize4(
        (dd.DisableMargins ? 0 : _style._props.MarginTop) + (dd.DisableBorders ? 0 : _style._props.BorderTop),
        (dd.DisableMargins ? 0 : _style._props.MarginRight) + (dd.DisableBorders ? 0 : _style._props.BorderRight),
        (dd.DisableMargins ? 0 : _style._props.MarginBot) + (dd.DisableBorders ? 0 : _style._props.BorderBot),
        (dd.DisableMargins ? 0 : _style._props.MarginLeft) + (dd.DisableBorders ? 0 : _style._props.BorderLeft)
      );
    }
    public UiSize4 GetMargin(UiLayoutGlobals dd)
    {
      if (dd.DisableMargins)
      {
        return new UiSize4();
      }
      return new UiSize4(
        _style._props.MarginTop,
        _style._props.MarginRight,
        _style._props.MarginBot,
        _style._props.MarginLeft
      );
    }
    public UiSize4 GetPadding(UiLayoutGlobals dd)
    {
      if (dd.DisablePadding)
      {
        return new UiSize4();
      }
      return new UiSize4(
        _style._props.PadTop,
        _style._props.PadRight,
        _style._props.PadBot,
        _style._props.PadLeft
      );
    }
    public UiSize4 GetBorder(UiLayoutGlobals? dd)
    {
      if (dd != null && dd.DisableBorders)
      {
        return new UiSize4();
      }
      return new UiSize4(
        _style._props.BorderTop,
        _style._props.BorderRight,
        _style._props.BorderBot,
        _style._props.BorderLeft
      );
    }
    public UiSize4 GetBorderRadius(UiLayoutGlobals dd)
    {
      if (dd.DisableBorders)
      {
        return new UiSize4();
      }
      return new UiSize4(
        _style._props.BorderTopLeftRadius,
        _style._props.BorderTopRightRadius,
        _style._props.BorderBotRightRadius,
        _style._props.BorderBotLeftRadius
      );
    }
    public UiElement? LastChild()
    {
      if (_children == null || _children.Count == 0)
      {
        return null;
      }
      return _children[_children.Count - 1];
    }
    public UiElement? ChildAt(int index)
    {
      return _children[index];
    }
    public UiElement? FirstChild(Type t, UiElement? root = null)
    {
      //get first child of type t, dept first
      if (root == null)
      {
        root = this;
      }
      else if (GetType() == t)
      {
        return this;
      }

      foreach (var ch in _children)
      {
        var e = FirstChild(t, ch);
        if (e != null)
        {
          return e;
        }
      }
      return null;
    }
    public IUiElement AddChild(IUiElement ee)
    {
      var e = ee as UiElement;
      Gu.Assert(e != null);
      Gu.Assert(this != e);

      e._parent?.RemoveChild(e);
      if (_children == null)
      {
        _children = new List<UiElement>();
      }
      _children.Add(e);

      e._parent = this;
      e._sort = this._children.Count + 1; // sort 0 is reserved
      if (e._sort > c_iMaxChildren)
      {
        e._sort = c_iMaxChildren;
        Gu.DebugBreak();
      }

      return e;
    }
    public bool RemoveChild(UiElement e)
    {
      bool ret = false;
      if (_children != null)
      {
        if (_children.Remove(e))
        {
          e._parent = null;
          e._sort = -1;
          ret = true;
        }
        if (_children.Count == 0)
        {
          _children = null;
        }
      }
      //could not remove.
      return ret;
    }
    public void ClearChildren()
    {
      _children?.Clear();
    }
    public void IterateChildren(Func<UiElement, LambdaBool> fn)
    {
      if (_children != null)
      {
        for (int i = _children.Count - 1; i >= 0; i--)
        {
          if (fn(_children[i]) == LambdaBool.Break)
          {
            break;
          }
        }
      }
    }
    public LambdaBool IterateTree(Func<UiElement, LambdaBool> fn)
    {
      if (_children != null)
      {
        for (int i = _children.Count - 1; i >= 0; i--)
        {
          if (fn(_children[i]) == LambdaBool.Break)
          {
            return LambdaBool.Break;
          }
          if (_children[i].IterateTree(fn) == LambdaBool.Break)
          {
            return LambdaBool.Break;
          }
        }
      }
      return LambdaBool.Continue;
    }
    public void DoMouseEvents(UiEvent e, bool iswindow = false)
    {
      if (_events.TryGetValue(e.EventId, out var acts))
      {
        foreach (var act in acts)
        {
          act?.Invoke(e);
        }
      }
    }
    public void AddEvent(UiEventId evId, UiAction f)
    {
      //add an additional event
      SetOrAddEvent(evId, f, false);
    }
    public void SetEvent(UiEventId evId, UiAction f)
    {
      //erase all events and set the new one
      SetOrAddEvent(evId, f, true);
    }
    public bool RemoveEvents(UiEventId evId)
    {
      //remove all events of id
      bool ret = _events.Remove(evId);
      if (_events.Count == 0)
      {
        PickEnabled = false;
      }
      return ret;
    }
    public int FindParent(UiElement e, int depth = 9)
    {
      //Returns [0,depth) if found the given element based on depth, -1 for not found
      var p = this.Parent;
      int level = 0;
      while (p != null)
      {
        if (p == e)
        {
          break;
        }
        level++;
        if (level == depth)
        {
          level = -1;
          break;
        }
        p = p.Parent;
      }
      if (p == null)
      {
        level = -1;
      }

      return level;
    }
    public void AddStyle(string name)
    {
      this._style.InheritFrom(name);
    }

    #endregion
    #region Private Methods

    private void Init(string name, string? phrase = null, List<UiElement>? children = null)
    {
      if (!String.IsNullOrEmpty(phrase))
      {
        Text = phrase;
      }
      if (String.IsNullOrEmpty(name))
      {
        _name = this.GetType().Name;
      }
      else
      {
        _name = name;
      }
      if (children != null)
      {
        foreach (var c in children)
        {
          AddChild(c);
        }
      }
    }
    private void UpdatePickID()
    {
      if (_pickEnabled)
      {
        if (_iPickId == Picker.c_iInvalidPickId)
        {
          _iPickId = Gu.Context.Picker.GenPickId();
        }
      }
      else
      {
        _iPickId = Picker.c_iInvalidPickId;
      }
    }
    private void ShowOrHide(bool show, int fadems)
    {
      if (_visible != show)
      {
        _visible = show;
      }
    }
    private void SetOrAddEvent(UiEventId evId, UiAction f, bool set)
    {
      Gu.Assert(f != null);
      if (set)
      {
        _events.Remove(evId);
      }
      _events = _events.ConstructIfNeeded();
      List<UiAction>? acts = null;
      if (!_events.TryGetValue(evId, out acts))
      {
        acts = new List<UiAction>();
        _events.Add(evId, acts);
      }
      acts.Add(f);
      PickEnabled = true;
    }

    #region Layout

    protected virtual void PerformLayout_SizeElements(UiSize2 parentMaxWH, UiStyle? parent, UiLayoutGlobals globals, bool parentstylechanged)
    {
      globals.Gui.NameTrap(this);

      bool styleChanged = _style.Modified || parentstylechanged;//TODO: rework styles to not do this

      _style.CompileStyleTree(globals.StyleSheet, globals.Framestamp, parent, parentstylechanged);

      ComputeLayoutInfo(parentMaxWH, globals);

      bool updateGlyphs = MustUpdateGlyphs(globals, styleChanged);
      if (updateGlyphs)
      {
        UpdateGlyphs(globals);
      }

      if (_style._props.RenderMode == UiRenderMode.Textured)
      {
        UpdateTexture(globals.MegaTex);
      }

      if (_children != null && _children.Count > 0)
      {
        int lineidx = 0;
        for (int eidx = 0; eidx < _children.Count; eidx++)
        {
          var ele = _children[eidx];
          if (ele.Visible)
          {
            ele.PerformLayout_SizeElements(_block._container._contentMaxWH, this._style, globals, styleChanged);
            LayoutElement(ele, lineidx);
          }
        }
      }

      if (_font != null && _font._glyphs != null && _font._glyphs.Count > 0)
      {
        for (int eidx = 0; eidx < _font._glyphs.Count; eidx++)
        {
          var gc = _font._glyphs[eidx];
          LayoutBlock(gc, true, null);
        }
      }

      FinalizeLayout();

      JustifyText();

      SizeElement();
    }
    protected void PerformLayout_PositionElements(UiLayoutGlobals globals, UiQuad? parentClip, uint rootPickId, ref Dictionary<uint, UiElement>? pickable, int layer)
    {
      globals.Gui.NameTrap(this);

      //ExpandPercent();
      if (globals.DisableAutos == false)
      {
        ExpandAutos();
      }

      ComputeQuads(this, _parent, _style._props.PositionModeX, _style._props.PositionModeY, globals.Scale, _style._props.OverflowMode, globals, parentClip);

      CollectEvents(globals);

      SetPickRoot(rootPickId, ref pickable);

      //compute & render children
      if (_children != null && _children.Count > 0)
      {
        foreach (var ele in _children)
        {
          if (ele.Visible)
          {
            var child_layer = ele._topMost ? 0 : layer + 1;

            ele.PerformLayout_PositionElements(globals, _block._b2ContentClip, _block._rootPickId, ref pickable, child_layer);
            ele.AddVerts(globals, _block._b2ContentClip);
          }
        }
      }

      _contentChanged = false;
    }
    private void JustifyText()
    {
      if (_style._props.ContentAlignX == UiAlignment.Justify)
      {
        if (this._static != null && this._static._glyphlines != null)
        {
          for (int li = 0; li < _static._glyphlines.Count - 1; li++)
          {
            var line = _static._glyphlines[li];
            var cw = this._container._contentWH._width - line._quad._width;
            if (cw > 0)
            {
              float ct = 0;
              var sp = cw / (float)Math.Max(line._wordCount, 0);
              foreach (var wc in line._eles)
              {
                if (wc is UiGlyph && (wc as UiGlyph).WordCharID == 0)
                {
                  if (ct > 0)
                  {
                    (wc as UiGlyph)._justify_sp = sp;
                  }
                  ct += 1;
                }
              }
            }

          }
        }
      }
    }
    private void CollectEvents(UiLayoutGlobals globals)
    {
      //TODO: this should tie into the event system and not be a separate thing.
      if (!this._block._b2BorderQuad.Equals(this._block._b2BorderQuadLast) && this.OnResize != null)
      {
        //Fire OnContentChanged events (for sliders)
        globals.LayoutEvents.Add(this);
      }
      else
      {
        globals.dbg_DidNotChange++;
      }

    }
    private void SetPickRoot(uint rootPickId, ref Dictionary<uint, UiElement>? pickable)
    {
      if (_pickEnabled)
      {
        _block._rootPickId = _iPickId;

        if (Gu.AssertDebug(_block._rootPickId != Picker.c_iInvalidPickId))
        {
          pickable = pickable.ConstructIfNeeded();
          pickable.Add(_block._rootPickId, this);
        }
      }
      else
      {
        _block._rootPickId = rootPickId;
      }
    }
    private void AddVerts(UiLayoutGlobals globals, UiQuad? childclip)
    {
      //addvertexes dovertexes addverts setverts setvertexes
      // Font verts
      if (_font != null && _font._glyphs != null && _font._glyphs.Count > 0)
      {
        for (int gi = 0; gi < _font._glyphs.Count; ++gi)
        {
          var glyph = _font._glyphs[gi];
          if (glyph != null)
          {
            ComputeQuads(glyph, this, UiPositionMode.Static, UiPositionMode.Static, globals.Scale, UiOverflowMode.Show, globals, null);

            vec4 color = _style._props.FontColor;
            if (glyph.GlyphColor != null)
            {
              color = glyph.GlyphColor.Value;
            }
            if (!IsFullyClipped(glyph._block._b2MarginQuad, _block._b2ContentClip))
            {
              AddGlyphVerts(glyph, _block._b2ContentClip, globals, _block._rootPickId, color);
            }
            else
            {
              globals.dbg_FullyClipped++;
            }
          }
        }
      }

      // Element verts
      if (this._style._props.FloatMode == UiFloatMode.Floating)
      {
        AddElementVerts(_block._b2BorderQuad, _block._rootPickId, globals);
      }
      else if (!IsFullyClipped(_block._b2BorderQuad, childclip))
      {
        AddElementVerts(childclip, _block._rootPickId, globals);
      }
      else
      {
        globals.dbg_FullyClipped++;
      }

    }
    private void ComputeLayoutInfo(UiSize2 parentMaxWH, UiLayoutGlobals dd)
    {
      _block._b2BorderQuadLast = _block._b2BorderQuad.Clone();
      _block._b2LayoutQuadLast = _block._b2LayoutQuad.Clone();
      _block._b2ContentQuad.Zero();
      _block._b2LayoutQuad.Zero();
      _block._b2ClipQuad.Zero();
      _block._b2MarginQuad.Zero();
      _block._b2BorderQuad.Zero();
      _block._b2ContentClip.Zero();
      _block._index.Zero();
      _block._line = null;
      _block._previous = null;

      _block._container = new UiContainerInfo();

      _container._contentWH.Zero();
      _container._margin = GetMargin(dd);

      //if relative or absolute ignore  margins
      if (this._style._props.PositionModeX != UiPositionMode.Static)
      {
        _container._margin._left = _container._margin._right = 0;
      }
      if (this._style._props.PositionModeY != UiPositionMode.Static)
      {
        _container._margin._top = _container._margin._bot = 0;
      }

      _container._border = GetBorder(dd);
      _container._padding = GetPadding(dd);

      _container._MBP.Set(
        _container._margin._top + _container._border._top + _container._padding._top,
        _container._margin._right + _container._border._right + _container._padding._right,
        _container._margin._bot + _container._border._bot + _container._padding._bot,
        _container._margin._left + _container._border._left + _container._padding._left
      );

      if (_style._props.SizeModeWidth == UiSizeMode.Fixed)
      {
        _container._minWH._width = _style._props.FixedWidth;
        _container._maxWH._width = _style._props.FixedWidth;
      }
      else
      {
        _container._minWH._width = _style._props.MinWidth;
        _container._maxWH._width = _style._props.MaxWidth;
      }

      if (_style._props.SizeModeHeight == UiSizeMode.Fixed)
      {
        _container._minWH._height = _style._props.FixedHeight;
        _container._maxWH._height = _style._props.FixedHeight;
      }
      else
      {
        _container._minWH._height = _style._props.MinHeight;
        _container._maxWH._height = _style._props.MaxHeight;
      }

      ComputeMaxWH(parentMaxWH);

      _container._contentWH.Zero();
    }
    public void ComputeMaxWH(UiSize2 parentMaxWH)
    {
      //shrink max rect by parent 
      _container._outerMaxWH.Set(
        Math.Max(Math.Min(parentMaxWH._width, _container._maxWH._width), 0),
        Math.Max(Math.Min(parentMaxWH._height, _container._maxWH._height), 0)
      );

      _container._contentMaxWH.Set(
        Math.Max(_container._outerMaxWH._width - _container._MBP._left - _container._MBP._right, 0),
        Math.Max(_container._outerMaxWH._height - _container._MBP._top - _container._MBP._bot, 0)
      );
    }
    private void LayoutElement(UiElement ele, int lineidx)
    {
      //Compute the size of the element and any relative element/lines.
      var ori = _style._props.LayoutOrientation;
      var perp = UiUtils.Perp(_style._props.LayoutOrientation);
      var ele_hmode = ele._style._props.L_PositionMode(ori);
      var ele_vmode = ele._style._props.L_PositionMode(perp);

      if (ele_hmode == UiPositionMode.Static)
      {
        LayoutBlock(ele, false, null);
        CollectHAuto(ele, ori);
      }
      if (ele.Text == "PCTLINE 1")
      {
        Gu.Trap();
      }
      if (ele_vmode == UiPositionMode.Static)
      {
        if (ele_hmode != UiPositionMode.Static) //line will be null
        {
          // Special case "VStat" (layout != static) && (perp(layout) == static)
          // Element moves freely along the layout direction, but is static along the vertical
          // Several things we can do: add to first line, but would require new line if none,
          // add to specific line via an ID parameter
          //  or just a default line that is the height of the content. * 
          LayoutBlock(ele, true, _style._props.VStatMode);
        }
        CollectVAuto(ele, perp);
      }
      if (ele._style._props.FloatMode != UiFloatMode.Floating)
      {
        ExpandByPositionalElement(ele, UiOrientation.Horizontal);
        ExpandByPositionalElement(ele, UiOrientation.Vertical);
      }
    }
    private void CollectHAuto(UiElement ele, UiOrientation ori)
    {
      if (IsAuto(ele, ori))
      {
        var line = ele._block._line;
        ele._block._line._autoInfo = line._autoInfo.ConstructIfNeeded();
        ele._block._line._autoInfo._hautos.Add(ele);
        line._autoInfo._auto_maxw = Math.Max(_container._contentMaxWH.L_Width(ori), line._autoInfo._auto_maxw + GetAutoMaxExpansion(ele, ori));
      }
    }
    private void CollectVAuto(UiElement ele, UiOrientation perp)
    {
      if (IsAuto(ele, perp))
      {
        var line = ele._block._line;
        ele._block._line._autoInfo = line._autoInfo.ConstructIfNeeded();
        ele._block._line._autoInfo._vautos.Add(ele);
        line._autoInfo._auto_maxh = Math.Max(line._autoInfo._auto_maxh, GetAutoMaxExpansion(ele, perp));
      }
    }
    private float GetAutoMaxExpansion(UiElement ele, UiOrientation ori)
    {
      var e_max = ele._container._maxWH.L_Width(ori);
      if (ele._style._props.L_AutoMode(ori) == UiAutoMode.Content)
      {
        //remove content from maxwh 
        e_max = Math.Max(e_max - ele._container._contentWH.L_Width(ori), 0);
      }
      return e_max;
    }
    private static bool IsAuto(UiElement ele, UiOrientation ori)
    {
      return ele._style._props.L_SizeMode(ori) == UiSizeMode.Auto || ele._style._props.L_SizeMode(ori) == UiSizeMode.AutoContent;
    }
    private void FinalizeLayout()
    {
      //compute static w/h, expand content, compute autos
      if (_static != null)
      {
        if (_static._glyphlines != null && _static._glyphlines.Count > 0)
        {
          FinishLine(_static._glyphlines[_static._glyphlines.Count - 1], true);
        }
        if (_static._lines != null && _static._lines.Count > 0)
        {
          FinishLine(_static._lines[_static._lines.Count - 1], false);
        }
        //Default line must come last
        FinishDefaultLine();

        //max of glyph/element
        var ori = _style._props.LayoutOrientation;
        _static._staticWH.L_Width(ori, Math.Max(_static._glyphWH.L_Width(ori), _static._elementWH.L_Width(ori)));
        _static._staticWH.L_Height(ori, Math.Max(_static._glyphWH.L_Height(ori), _static._elementWH.L_Height(ori)));
        _container._contentWH.L_Width(ori, Math.Max(_container._contentWH.L_Width(ori), _static._staticWH.L_Width(ori)));
        _container._contentWH.L_Height(ori, Math.Max(_container._contentWH.L_Height(ori), _static._staticWH.L_Height(ori)));
      }
    }
    private void FinishLine(UiLayoutLine line, bool is_glyphs)
    {
      //Line Done.
      //Expand w/h of parent container, update autos
      UpdateAutoLines(line);

      var ori = _style._props.LayoutOrientation;

      //Expand pre-auto static wh
      if (is_glyphs)
      {
        _static._glyphWH.L_Width(ori, Math.Max(_static._glyphWH.L_Width(ori), line._quad.L_Width(ori)));
        _static._glyphWH.L_Height(ori, _static._glyphWH.L_Height(ori) + line._quad.L_Height(ori));
      }
      else
      {
        _static._elementWH.L_Width(ori, Math.Max(_static._elementWH.L_Width(ori), line._quad.L_Width(ori)));
        _static._elementWH.L_Height(ori, _static._elementWH.L_Height(ori) + line._quad.L_Height(ori));
      }
    }
    private void FinishDefaultLine()
    {
      //Configure VStat line.
      if (_static._contentLine != null && _static._contentLine._eles != null && _static._contentLine._eles.Count > 0)
      {
        //set default (vstat) line to static content 
        var perp = UiUtils.Perp(_style._props.LayoutOrientation);
        _static._contentLine._quad.L_Width(perp, _static._staticWH.L_Width(perp));
        UpdateAutoLines(_static._contentLine);
      }
    }
    private void UpdateAutoLines(UiLayoutLine line)
    {
      //Update the auto information 
      if (line._autoInfo != null)
      {
        var ori = _style._props.LayoutOrientation;
        if (line._autoInfo._hautos != null && line._autoInfo._hautos.Count > 0)
        {
          _static._autoLinesH = _static._autoLinesH.ConstructIfNeeded();
          _static._autoLinesH.Add(line);
          //  _container._static._autoMaxW = Math.Max(_container._static._autoMaxW, line._autoInfo._auto_maxw);
        }
        if (line._autoInfo._vautos != null && line._autoInfo._vautos.Count > 0)
        {
          _static._autoLinesV = _static._autoLinesV.ConstructIfNeeded();
          _static._autoLinesV.Add(line);
          // _container._static._autoMaxH += line._autoInfo._auto_maxh;
        }
      }
    }
    private void SizeElement()
    {
      //Compute final Width Height
      SizeElement(UiOrientation.Horizontal);
      SizeElement(UiOrientation.Vertical);
    }
    private void SizeElement(UiOrientation ori)
    {
      //compute minimum width/height of element, using, content + MBP
      var mode = _style._props.L_SizeMode(ori);

      if (mode == UiSizeMode.Fixed)
      {
        _block._b2LayoutQuad.L_Width(ori, _style._props.L_FixedWidth(ori));
      }
      else if (mode == UiSizeMode.Content)
      {
        //shrink to content (autos=0)
        _block._b2LayoutQuad.L_Width(ori, _container._contentWH.L_Width(ori));
      }
      else if (mode == UiSizeMode.Percent)
      {
        //% of parent content area, expand after parent layout
        var w = _parent._container._contentMaxWH.L_Width(ori) * _style._props.L_PercentWidth(ori) * 0.01f;
        _block._b2LayoutQuad.L_Width(ori, w);
      }
      else if (mode == UiSizeMode.Auto)
      {
        //grow or shrink to fill remaining space
        _block._b2LayoutQuad.L_Width(ori, 0);
      }
      else if (mode == UiSizeMode.AutoContent)
      {
        //fill remaining space, but do not shrink below content 
        _block._b2LayoutQuad.L_Width(ori, _container._contentWH.L_Width(ori));
      }

      if (mode != UiSizeMode.Fixed && mode != UiSizeMode.Auto && mode != UiSizeMode.Percent)
      {
        //Add MBP for the  new layout quads which include marigns, also add for content autos
        var MBP_LR = _container._MBP.L_Left(ori) + _container._MBP.L_Right(ori);
        _block._b2LayoutQuad.L_Width(ori, _block._b2LayoutQuad.L_Width(ori) + MBP_LR);
      }

      if (mode != UiSizeMode.Auto && mode != UiSizeMode.AutoContent)
      {
        //clamp to min/max
        _block._b2LayoutQuad.L_Width(ori, Math.Clamp(_block._b2LayoutQuad.L_Width(ori), _container._minWH.L_Width(ori), _container._maxWH.L_Width(ori)));
      }
      else
      {
        //clamp auto to min only, max will be computed after layout
        _block._b2LayoutQuad.L_Width(ori, Math.Max(_block._b2LayoutQuad.L_Width(ori), _container._minWH.L_Width(ori)));
      }

      _block._b2LayoutQuad.ValidateQuad();
    }
    private void LayoutBlock(UiBlock ele, bool glyphs, UiVStatMode? vstatmode)
    {
      //layout elements
      var ori = _style._props.LayoutOrientation;
      if (_static == null)
      {
        _static = new UiStaticInfo();
      }

      if (ele is UiSlider)
      {
        Gu.Trap();
      }

      //note: width for layout includes MBP
      float e_width = ele._block._b2LayoutQuad.L_Width(ori);
      float e_height = ele._block._b2LayoutQuad.L_Height(ori);
      float glyph_advance = ele.L_Advance();
      float glyph_line_height = ele.L_PixelLineHeight();

      UiLayoutLine line = GetLayoutLine(ori, ele, vstatmode, glyphs, e_width, glyph_advance);

      //expand line quad
      float line_width = Math.Max(line._quad.L_Width(ori) + e_width + glyph_advance + ele.L_Left(ori), 0);
      float lineh_max = Math.Max(e_height + ele.L_Top(ori), glyph_line_height);
      float line_height = Math.Max(line._quad.L_Height(ori), lineh_max);
      line._quad.L_WidthHeight(ori, line_width, line_height);
      line._quad.ValidateQuad();

      ele._block._index.L_WidthHeight(ori, line._eles.Count, line._index);
      ele._block._line = line;
      ele._block._previous = _static._last;
      _static._last = ele;

      line._eles.Add(ele);

      if (ele is UiGlyph && (ele as UiGlyph).WordCharID == 0)
      {
        line._wordCount++;
      }

    }
    private UiLayoutLine GetLayoutLine(UiOrientation ori, UiBlock ele, UiVStatMode? vstatmode, bool is_glyphs, float e_width, float glyph_advance)
    {
      //get the layout line or next line
      UiLayoutLine line = null;
      if (vstatmode != null)
      {
        if (vstatmode.Value == UiVStatMode.ParentContent)
        {
          if (_static._contentLine == null)
          {
            _static._contentLine = new UiLayoutLine(-1);
          }
          line = _static._contentLine;
        }
        else if (vstatmode.Value == UiVStatMode.Line)
        {
          //make new line
          var lines = GetLines(false);
          line = lines[lines.Count - 1];
          if (line._eles.Count != 0)
          {
            line = NextLine(lines, line, false);
          }
        }
      }
      else
      {
        var lines = GetLines(is_glyphs);
        line = lines[lines.Count - 1];

        if (ele is UiGlyph)
        {
          if ((ele as UiGlyph)._dbg_word.Contains("consectetur"))
          {
            Gu.Trap();
          }
          if ((ele as UiGlyph)._dbg_word.Contains("amet"))
          {
            Gu.Trap();
          }
          if ((ele as UiGlyph)._dbg_word.Contains("adipiscing"))
          {
            Gu.Trap();
          }
        }

        if (line._eles.Count != 0)//dont line break on first line
        {
          //**this is incorrect - fixing it later.
          var space = _container._contentMaxWH.L_Width(ori);
          var line_w = line._quad.L_Width(ori);

          if (CheckLineBreak(ele, ele.L_WordWidth(ori), line_w, space, e_width + glyph_advance))
          {
            line = NextLine(lines, line, is_glyphs);
          }
        }
      }
      return line;
    }
    private List<UiLayoutLine> GetLines(bool is_glyphs)
    {
      List<UiLayoutLine> lines;
      if (is_glyphs)
      {
        if (_static._glyphlines == null)
        {
          _static._glyphlines = new List<UiLayoutLine>();
          _static._glyphlines.Add(new UiLayoutLine(_static._glyphlines.Count));
          _static._last = null;
        }
        lines = _static._glyphlines;
      }
      else
      {
        if (_static._lines == null)
        {
          _static._lines = new List<UiLayoutLine>();
          _static._lines.Add(new UiLayoutLine(_static._lines.Count));
          _static._last = null;
        }
        lines = _static._lines;
      }
      return lines;
    }
    private UiLayoutLine NextLine(List<UiLayoutLine> lines, UiLayoutLine line, bool is_glyphs)
    {
      FinishLine(line, is_glyphs);

      UiLayoutLine line2;
      line2 = new UiLayoutLine(lines.Count);
      line2._previous = line;
      lines.Add(line2);
      line = line2;
      _static._last = null;
      return line;
    }
    private bool CheckLineBreak(UiBlock ele, float word_size, float line_w, float space, float e_width)
    {
      bool bLineBreak = false;

      if (ele.DisplayMode == UiDisplayMode.Block)
      {
        bLineBreak = true;
      }
      else if (ele.DisplayMode == UiDisplayMode.Inline)
      {
        bLineBreak = (e_width + line_w > space);
      }
      else if (ele.DisplayMode == UiDisplayMode.Word)
      {
        bLineBreak = (word_size + line_w > space);
      }

      return bLineBreak;
    }
    private void ExpandByPositionalElement(UiElement ele, UiOrientation ori)
    {
      //Static elements expand content when laid out
      var mode = ele._style._props.L_PositionMode(ori);
      if (mode == UiPositionMode.Fixed)
      {
        _container._contentWH.L_Width(ori,
          Math.Max(
            _container._contentWH.L_Width(ori),
            ele._block._b2LayoutQuad.L_Left(ori) + ele._block._b2LayoutQuad.L_Width(ori)
          ));
      }
    }
    private void ExpandAutos()
    {
      //Autos must be in position step or a separate step.
      //we dont have parent autos computed yet size step, so maxwh of auto inside an auto would be unkown
      //set auto size to zero..compute children.. what is maxwh.. unknown
      if (_static != null)
      {
        if (_static._autoLinesH.HasElements())
        {
          foreach (var line in _static._autoLinesH)
          {
            ExpandAutosH(line);
          }
        }
        if (_static._autoLinesV.HasElements())
        {
          ExpandAutosV();
        }
      }
    }
    private void ExpandAutosH(UiLayoutLine line)
    {
      /*
        1 Sort all elements by maxw 
        2 compute equal_w & total_w
        3 iterate in order adding back to total_width if max < equal_w
      */
      var ori = _style._props.LayoutOrientation;
      var amode = _style._props.L_AutoMode(ori);

      float total_space = 0;
      if (amode == UiAutoMode.Content)
      {
        //we dont have any quads here except layout
        total_space = Math.Max(GetAutoContentSize(ori) - line._quad.L_Width(ori), 0);
      }
      else if (amode == UiAutoMode.Line)
      {
        float sp = Math.Min(_container._static._staticWH.L_Width(ori), _container._contentMaxWH.L_Width(ori));
        total_space = Math.Max(sp - line._quad.L_Width(ori), 0);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }

      var autocount = (float)line._autoInfo._hautos.Count;
      if (line._autoInfo._hautos.Count > 1)
      {
        line._autoInfo._hautos.Sort((x, y) => x._style._props.L_MaxWidth(ori) - y._style._props.L_MaxWidth(ori) > 0 ? 1 : -1);
      }
      if (line._autoInfo._hautos.Count == 2 && line._autoInfo._hautos[1].Text == "H Auto")
      {
        Gu.Trap();
      }

      foreach (var auto in line._autoInfo._hautos)
      {
        var add_w = total_space / autocount;
        var max_w = auto._container._maxWH.L_Width(ori);
        var cur_w = auto._block._b2LayoutQuad.L_Width(ori);

        if (cur_w + add_w >= max_w)
        {
          //max is less than equal width, clamp to max,  add remainder to auto space
          add_w = Math.Max(max_w - cur_w, 0);
          if (add_w > 0)
          {
            total_space = Math.Max(total_space - add_w, 0);
            autocount--;
          }
        }

        if (add_w > 0)
        {
          var aw = auto._block._b2LayoutQuad.L_Width(ori) + add_w;
          SetAutoQuad(ori, auto, aw);
          ExpandContainerByNewAutoSize(ori, add_w);

          line._quad.L_Width(ori, line._quad.L_Width(ori) + add_w);
          line._quad.ValidateQuad();
        }
      }
    }
    private void ExpandAutosV()
    {
      var ori = UiUtils.Perp(_style._props.LayoutOrientation);
      var amode = _style._props.L_AutoMode(ori);
      if (amode == UiAutoMode.Line)
      {
        foreach (var line in this._static._autoLinesV)
        {
          ExpandAutosVLine(line, ori);
        }
      }
      else if (amode == UiAutoMode.Content)
      {
        if (_static._autoLinesV.Count > 1)
        {
          _static._autoLinesV.Sort((x, y) => x._autoInfo._auto_maxh - y._autoInfo._auto_maxh > 0 ? 1 : -1);
        }

        float total_space = Math.Max(GetAutoContentSize(ori) - _static._elementWH.L_Width(ori), 0);
        float linecount = (float)this._static._autoLinesV.Count;

        foreach (var line in this._static._autoLinesV)
        {
          var equal_w = total_space / linecount;
          var add_w = equal_w;
          var max_w = line._autoInfo._auto_maxh;
          var cur_w = line._quad.L_Width(ori);

          if (cur_w + add_w >= max_w)
          {
            //max is less than equal width, clamp to max,  add remainder to auto space
            add_w = Math.Max(max_w - cur_w, 0);
            if (add_w > 0)
            {
              total_space = Math.Max(total_space - add_w, 0);
              linecount--;
            }
          }

          //udpate staticwh and container wh

          if (add_w > 0)
          {
            line._quad.L_Width(ori, line._quad.L_Width(ori) + add_w);
            line._quad.ValidateQuad();
            ExpandContainerByNewAutoSize(ori, add_w);
            ExpandAutosVLine(line, ori);
          }
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    private void ExpandAutosVLine(UiLayoutLine line, UiOrientation ori)
    {
      //expand all vertical autos up to the line height, limited by max
      var lh = line._quad.L_Width(ori);
      foreach (var auto in line._autoInfo._vautos)
      {
        var aw = auto._block._b2LayoutQuad.L_Width(ori);
        aw = Math.Min(auto._container._maxWH.L_Width(ori), lh);
        SetAutoQuad(ori, auto, aw);
      }
    }
    private void SetAutoQuad(UiOrientation ori, UiElement auto, float aw)
    {
      //this is just for breaking / debug
      if (auto.Text.ToLower().Contains("Info"))
      {
        Gu.Trap();
      }
      auto._block._b2LayoutQuad.L_Width(ori, aw);
      auto._block._b2LayoutQuad.ValidateQuad();
    }
    private void ExpandContainerByNewAutoSize(UiOrientation ori, float add_w)
    {
      this._container._contentWH.L_Width(ori, this._container._contentWH.L_Width(ori) + add_w);
      this._container._static._staticWH.L_Width(ori, this._container._static._staticWH.L_Width(ori) + add_w);
      this._container._static._elementWH.L_Width(ori, this._container._static._elementWH.L_Width(ori) + add_w);
    }
    private float GetAutoContentSize(UiOrientation ori)
    {
      var cont_w = _block._b2LayoutQuad.L_Width(ori) - _container._MBP.L_Left(ori) - _container._MBP.L_Right(ori);
      return cont_w;
    }
    protected static void ComputeQuads(UiBlock block, UiElement parent, UiPositionMode pmodex, UiPositionMode pmodey, vec2 scale,
                                       UiOverflowMode overflowMode, UiLayoutGlobals dd, UiQuad? parentClip)
    {
      //Add parent offsets to child quads; make relative offsets absolute.
      //put layout quad (border+pad + margin) into render quad space.

      SetMarginQuad(block, parent, UiOrientation.Horizontal, pmodex, scale.x);
      SetMarginQuad(block, parent, UiOrientation.Vertical, pmodey, scale.y);

      block._block._b2MarginQuad.Scale(scale);
      block._block._b2MarginQuad.ValidateQuad();

      if (block._block._container != null)
      {
        //remove margin
        block._block._b2BorderQuad = block._block._b2MarginQuad.Clone();
        block._block._b2BorderQuad.ShrinkBy(block._block._container._margin, scale);
        block._block._b2BorderQuad.ClampToZero();//can go negative since border and padding are part of minsize.
        block._block._b2BorderQuad.ValidateQuad();

        //content
        block._block._b2ContentQuad = block._block._b2MarginQuad.Clone();
        block._block._b2ContentQuad.ShrinkBy(block._block._container._MBP, scale);
        block._block._b2ContentQuad.ClampToZero();//can go negative since border and padding are part of minsize.
        block._block._b2ContentQuad.ValidateQuad();


        //initial clip , expands by floating elements
        if (overflowMode == UiOverflowMode.Show || dd.DisableClip)
        {
          block._block._b2ClipQuad = dd.Gui._block._b2MarginQuad.Clone();
        }
        else if (overflowMode == UiOverflowMode.Border)
        {
          block._block._b2ClipQuad = block._block._b2BorderQuad.Clone();
        }
        else if (overflowMode == UiOverflowMode.Padding)
        {
          block._block._b2ClipQuad = block._block._b2BorderQuad.Clone();
          block._block._b2ClipQuad.ShrinkBy(block._block._container._padding, scale);
          block._block._b2ClipQuad.ClampToZero();//can go negative since border and padding are part of minsize.
        }
        else if (overflowMode == UiOverflowMode.Content)
        {
          block._block._b2ClipQuad.Zero();
          block._block._b2ClipQuad = block._block._b2ContentQuad.Clone();
        }

        //Shrink clip
        var ele = (block as UiElement);
        if (parentClip == null)
        {
          block._block._b2ContentClip = block._block._b2MarginQuad.Clone();
        }
        else if (ele._style._props.FloatMode == UiFloatMode.Floating)
        {
          //floating elements can go beyond parent
          block._block._b2ContentClip = block._block._b2ClipQuad.Clone();
        }
        else
        {
          block._block._b2ContentClip = parentClip.Clone();
          block._block._b2ContentClip.Intersect(block._block._b2ClipQuad);
          block._block._b2ContentClip.ValidateQuad();
        }

        //line quad
        //block._block._container._b2LineQuad.Scale(scale);
      }

    }
    private static void SetMarginQuad(UiBlock block, UiElement parent, UiOrientation ori, UiPositionMode pmode, float scale)
    {
      //Add parent offsets to child quads; make relative offsets absolute.
      //layout quad is relative ot parent and includes margin+border+padding+content

      //set border quad min/maxem
      if (pmode == UiPositionMode.Fixed)
      {
        float porig = 0;
        if (parent != null)
        {
          //root
          porig = parent._block._b2MarginQuad.L_Left(ori) / scale;
        }

        //Updated this to be margin quad instead of content quad due to menus - we could just subtract MBP though
        block._block._b2MarginQuad.L_Left(ori, block.L_Left(ori) + porig);
        block._block._b2MarginQuad.L_Width(ori, block._block._b2LayoutQuad.L_Width(ori));
      }
      else if (pmode == UiPositionMode.Static)
      {
        Gu.Assert(block._block._line != null); // all blocks must have a line.

        //get origin for block
        float left = ComputeStaticOrigin(ori, block, parent, scale);

        //add static offset
        if (parent._style._props.LayoutDirection == UiLayoutDirection.LeftToRight || parent._style._props.LayoutOrientation != ori)
        {
          left += block.L_Left(ori);
        }
        else
        {
          left -= block.L_Left(ori);
        }

        if (ori == parent._style._props.LayoutOrientation && block is UiGlyph)
        {
          left += (block as UiGlyph)._justify_sp;
        }

        block._block._b2MarginQuad.L_Left(ori, left);
        block._block._b2MarginQuad.L_Width(ori, block._block._b2LayoutQuad.L_Width(ori));
      }
    }
    private static float ComputeStaticOrigin(UiOrientation ori, UiBlock block, UiElement parent, float scale)
    {
      //compute final position of static block
      float left = 0;
      var align = parent._style._props.L_ContentAlign(ori);
      var layout_dir = parent._style._props.LayoutDirection;
      var line = block._block._line;
      var line_width = line._quad.L_Width(ori);
      var previous = block._block._previous;
      var parent_w = parent._block._b2ContentQuad.L_Width(ori) / scale;
      var parent_x = parent._block._b2ContentQuad.L_Left(ori) / scale;

      //Set line offset
      float line_left = 0;
      if (ori != parent._style._props.LayoutOrientation && line._previous != null)
      {
        //set multiple times, but it's not a huge issue.
        line_left = line._previous._quad.L_Left(ori) + line._previous._quad.L_Width(ori);
        line._quad.L_Left(ori, line_left);
      }

      if (ori == parent._style._props.LayoutOrientation)
      {
        //Horizontal
        if (previous != null)
        {
          var prev_x = previous._block._b2MarginQuad.L_Left(ori) / scale;
          //build from previous element offset, note this requires elements to be processed in order.
          if (layout_dir == UiLayoutDirection.LeftToRight)
          {
            var prev_w = previous._block._b2MarginQuad.L_Width(ori) / scale;
            var prev_adv = GetAdvanceForDirection(previous, ori == parent._style._props.LayoutOrientation);
            left = prev_x + prev_w + prev_adv;//Note: advance is not the same as TTF advance, advance+prev_width = next position
          }
          else
          {
            var block_w = block._block._b2LayoutQuad.L_Width(ori);
            var prev_adv = GetAdvanceForDirection(previous, ori == parent._style._props.LayoutOrientation);
            //L e f t -> t f e L  := e -> prev = L
            //this is not exactly correct rigth now but it is not necessary
            left = prev_x - prev_adv - block_w;
          }
        }
        else
        {
          //compute starting position
          if (layout_dir == UiLayoutDirection.LeftToRight)
          {
            if (align == UiAlignment.Left || align == UiAlignment.Justify)
            {
              left = parent_x;
            }
            else if (align == UiAlignment.Center)
            {
              left = parent_x + parent_w / 2 - line_width / 2;
            }
            else if (align == UiAlignment.Right)
            {
              left = parent_x + parent_w - line_width;
            }
          }
          else //righttoleft
          {
            float block_adv = GetAdvanceForDirection(block, ori == parent._style._props.LayoutOrientation);
            var block_w = block._block._b2LayoutQuad.L_Width(ori);
            var block_off = block.L_Left(ori);
            if (align == UiAlignment.Left || align == UiAlignment.Justify)
            {
              left = parent_x + line_width - block_w - block_off;
            }
            else if (align == UiAlignment.Center)
            {
              left = parent_x + parent_w / 2 + line_width / 2 - block_w - block_off;
            }
            else if (align == UiAlignment.Right)
            {
              left = parent_x + parent_w - block_w - block_off;
            }
          }
        }

      }
      else
      {
        //Vertical
        //Ignoring left-to-right + vertical, it is not necessary
        var stat_width = parent._container._static._staticWH.L_Width(ori);

        //TODO: line left is going away
        var line_origin = parent_x + line_left;

        if (align == UiAlignment.Left || align == UiAlignment.Justify)
        {
          left = line_origin;
        }
        else if (align == UiAlignment.Center)
        {
          left = parent_x + parent_w / 2 - stat_width / 2 + line_left;
        }
        else if (align == UiAlignment.Right)
        {
          left = line_origin + parent_w - stat_width;
        }
      }

      return left;
    }
    private static float GetAdvanceForDirection(UiBlock block, bool islayout)
    {
      //returns the advance width for a block in the layout direction. (glyphs have additional advance since they dont have marigns.)
      return (islayout ? block.L_Advance() : 0);
    }
    private static bool IsFullyClipped(UiQuad quad, UiQuad clip)
    {
      if (quad.Max.x <= clip.Min.x)
      {
        return true;
      }
      if (quad.Max.y <= clip.Min.y)
      {
        return true;
      }
      if (quad.Min.x >= clip.Max.x)
      {
        return true;
      }
      if (quad.Min.y >= clip.Max.y)
      {
        return true;
      }
      return false;
    }
    private void UpdateGlyphs(UiLayoutGlobals dd)
    {
      Gu.Assert(_style._props.FontFace != null);

      //TODO: check if we can remove this mutex, and put the text directly the loader data.
      string copied_text = "";
      lock (_font._strText)
      {
        copied_text = _font._strText;
      }
      if (String.IsNullOrEmpty(copied_text))
      {
        _font._glyphs = null;
        return;
      }

      var loader = GetFontLoader(dd.MegaTex, _style._props.FontFace);
      if (loader == null)
      {
        Gu.Log.ErrorCycle("Loader, or, Default loader could not be found for " + _style._props.FontFace.QualifiedPath + " font possibly loaded with error.", 120);
        return;
      }
      var patch = loader.SelectFontPatch(Gu.Translator.LanguageCode, _style._props.FontSize);
      if (patch == null)
      {
        Gu.Log.ErrorCycle("Null font patch.", 120);
        Gu.DebugBreak();
        return;
      }

      dd.GlyphCache.CreateGlyphs(
        _font,
        loader, patch, copied_text,
        _style._props.FontFace,
        _style._props.FontSize,
        _style._props.TextWrap,
        _style._props.LineHeight,
        _style._props.Tracking,
        _style._props.Hyphens

      );

      _font._textChanged = false;
      _font._bNeverDoneText = false;
      _font._baseGlyphCount = _font._glyphs.Count;
    }
    private bool MustUpdateGlyphs(UiLayoutGlobals dd, bool styleChanged)
    {
      bool dotext = false;
      if (_font == null)
      {
        dotext = false;
      }
      else
      {
        if (dd.ForceText)
        {
          dotext = true;
        }
        else if (_font._textChanged || styleChanged)
        {
          if (_font._bNeverDoneText)
          {
            dotext = true;
          }
          else if (dd.Framestamp % 5 == 0)
          {
            dotext = true;
          }
        }

        if (dd.ForceText)
        {
          dotext = true;
        }
        else if (_font._textChanged || styleChanged)
        {
          if (_font._bNeverDoneText)
          {
            dotext = true;
          }
          else if (dd.Framestamp % 5 == 0)
          {
            dotext = true;
          }
        }
      }
      return dotext;
    }
    private MtFontLoader? GetFontLoader(MegaTex mt, FileLoc face)
    {
      MtFontLoader? loader = null;
      if (_font._cachedFont == null || mt.GetFont(face) != _font._cachedFont)
      {
        loader = mt.GetFont(face);
      }
      else
      {
        loader = _font._cachedFont;
      }
      if (loader == null)
      {
        Gu.Log.ErrorCycle("Font loader could not be found for " + face.QualifiedPath + " font possibly loaded with error", 120);
        loader = mt.GetFont(UiFontFace.Default);
      }

      return loader;
    }


    #endregion
    #region Render

    private void AddElementVerts(UiQuad clip, uint rootPickId, UiLayoutGlobals globals)
    {
      //**Texture Adjust - modulating repeated textures causes seaming issues, especially with texture filtering
      // adjust the texture coordinates by some pixels to account for that.  0.5f seems to work well.
      float adjust = 0;// 1.4f;  // # of pixels to adjust texture by

      //Debug overlay
      if (globals.ShowDebug)
      {
        DebugVert(_block._b2BorderQuad, clip, this._debugcolor, globals, rootPickId);
      }
      if (_style._props.RenderMode != UiRenderMode.None)
      {
        var bd = _container._border;// GetBorder(dd);
        var radius = this.GetBorderRadius(globals);

        //Content Quad w/ margins
        v_v4v4v4v2u2v4v4 vc = new v_v4v4v4v2u2v4v4();
        vc._rtl_rtr = new vec4(radius._top, radius._right);
        vc._rbr_rbl = new vec4(radius._bot, radius._left);
        vc._border = _container._border.ToVec4();
        vc._font_weight = new vec4(1, 0, 0, 0);
        vc._border_color = new uvec4(
          vec4ToUint(_style._props.BorderColorTop * _style._props.Opacity),
          vec4ToUint(_style._props.BorderColorRight * _style._props.Opacity),
          vec4ToUint(_style._props.BorderColorBot * _style._props.Opacity),
          vec4ToUint(_style._props.BorderColorLeft * _style._props.Opacity)
        );

        SetVertexRasterArea(ref vc, in _block._b2BorderQuad, in clip, globals);

        MtTex tex = null;
        if (_style._props.RenderMode == UiRenderMode.Color)
        {
          tex = globals.DefaultPixel;
        }
        else if (_style._props.Texture != null)
        {
          Gu.Assert(_style._props.Texture.Modified == false);
          tex = _style._props.Texture.Image;
        }
        else
        {
          Gu.Log.Error($"{Name}: UI: Render mode is texture, but texture was not set. Changing to color.");
          tex = globals.DefaultPixel;
          Gu.DebugBreak();
        }

        ComputeVertexTexcoord(ref vc, tex, _style._props.ImageTilingX, _style._props.ImageTilingY, adjust);

        vec4 cmul = _style._props.Color * _style._props.MultiplyColor;
        vec4 color = new vec4(cmul.xyz, cmul.w * _style._props.Opacity).Clamp(0.0f, 1.0f);
        vc._pick_color = new uvec2(rootPickId, vec4ToUint(color));
        globals.Verts.Add(vc);

      }
    }
    private void AddGlyphVerts(UiGlyph glyph, UiQuad clip, UiLayoutGlobals globals, uint pickId, vec4 color)
    {
      UiQuad THE_QUAD = glyph._block._b2MarginQuad;

      if (globals.ShowDebug)
      {
        DebugVert(THE_QUAD, clip, glyph._debugcolor, globals, pickId);
      }

      float adjust = 0;
      v_v4v4v4v2u2v4v4 vc = new v_v4v4v4v2u2v4v4();
      SetVertexRasterArea(ref vc, THE_QUAD, clip, globals);
      vc._rtl_rtr = new vec4(0, 0, 0, 0);
      vc._rbr_rbl = new vec4(0, 0, 0, 0);
      vc._border = new vec4(0, 0, 0, 0);
      vc._border_color = new uvec4(0, 0, 0, 0);
      vc._font_weight = new vec4(this._style._props.FontWeight, 0, 0, 0);
      ComputeVertexGlyphTCoord(ref vc, glyph._cachedGlyph, adjust);
      vc._pick_color = new uvec2(pickId, vec4ToUint(color * _style._props.Opacity));
      globals.Verts.Add(vc);
    }
    private void DebugVert(UiQuad quad, UiQuad clip, vec4 c, UiLayoutGlobals dd, uint pick)
    {
      v_v4v4v4v2u2v4v4 vc = new v_v4v4v4v2u2v4v4();
      SetVertexRasterArea(ref vc, quad, in clip, dd);
      vc._rtl_rtr = new vec4(0, 0, 0, 0);
      vc._rbr_rbl = new vec4(0, 0, 0, 0);
      vc._border = new vec4(0, 0, 0, 0);
      vc._border_color = new uvec4(0, 0, 0, 0);
      vc._font_weight = new vec4(1, 0, 0, 0);
      ComputeVertexTexcoord(ref vc, dd.DefaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, 0);
      vc._pick_color = new uvec2(pick, vec4ToUint(c));
      dd.Verts.Add(vc);
    }
    private void ComputeVertexTexcoord(ref v_v4v4v4v2u2v4v4 vc, MtTex pTex, UiImageTiling xtile, UiImageTiling ytile, float adjust)
    {
      Gu.Assert(pTex != null);
      ComputeTCoord(ref vc, pTex.uv0, pTex.uv1, pTex.GetWidth(), pTex.GetHeight(), pTex.GetSizeRatio(), xtile, ytile, _style._props.ImageScaleX, _style._props.ImageScaleY, adjust);
    }
    private void ComputeVertexGlyphTCoord(ref v_v4v4v4v2u2v4v4 vc, MtFontChar? glyph, float adjust)
    {
      Gu.Assert(glyph != null);
      ComputeTCoord(ref vc, glyph.uv0, glyph.uv1, glyph.patchTexture_Width, glyph.patchTexture_Height, 1, UiImageTiling.Expand, UiImageTiling.Expand, 1, 1, adjust);
    }
    private static void ComputeTCoord(ref v_v4v4v4v2u2v4v4 vc, vec2 uv0, vec2 uv1, float texw, float texh, float sizeRatio,
        UiImageTiling xtile, UiImageTiling ytile, float tilex, float tiley, float adjust)
    {

      if (xtile == UiImageTiling.Expand)
      {
        vc._tex.x = uv0.x;
        vc._tex.z = uv1.x;
      }
      else if (xtile == UiImageTiling.Tile)
      {
        vc._tex.x = uv0.x;
        vc._tex.z = uv1.x + (uv1.x - uv0.x) * tilex;
      }
      else if (xtile == UiImageTiling.Computed)
      {
        // Tex coords are computed (used for UiGlyph)
      }
      else
      {
        Gu.Log.Error("Invalid layout X image size mode.");
      }

      if (ytile == UiImageTiling.Expand)
      {
        vc._tex.y = uv0.y;
        vc._tex.w = uv1.y;
      }
      else if (ytile == UiImageTiling.Tile)
      {
        vc._tex.y = uv0.y;
        vc._tex.w = uv1.y + (uv1.y - uv0.y) * tiley;
      }
      else if (ytile == UiImageTiling.Computed)
      {
        // Tex coords are computed (used for UiGlyph)
      }
      else if (ytile == UiImageTiling.Proportion)
      {
        // proportion the Y to the X
        vc._tex.y = uv1.y;
        vc._tex.w = vc._tex.y + (vc._tex.z - vc._tex.x) * sizeRatio;
      }
      else
      {
        Gu.Log.Error("Invalid layout size mode.");
      }

      vc._texsiz.x = Math.Abs(uv1.x - uv0.x);
      vc._texsiz.y = Math.Abs(uv1.y - uv0.y);  // Uv0 - uv1 - because we flipped coords bove

      float w1px = 0;                  // 1 pixel subtract from the u/v to prevent creases during texture modulation
      float h1px = 0;

      if (texw > 0 && vc._texsiz.x > 0)
      {
        w1px = 1.0f / texw;
        w1px *= vc._texsiz.x;
        w1px *= adjust;
      }
      if (texh > 0 && vc._texsiz.y > 0)
      {
        h1px = 1.0f / texh;
        h1px *= vc._texsiz.y;
        h1px *= adjust;
      }
      vc._texsiz.x -= w1px * 2.0f;
      vc._texsiz.y -= h1px * 2.0f;
      vc._tex.x += w1px;
      vc._tex.y += h1px;
      vc._tex.z -= w1px;
      vc._tex.w -= h1px;
    }
    private static void SetVertexRasterArea(ref v_v4v4v4v2u2v4v4 vc, in UiQuad rasterQuad, in UiQuad b2ClipRect, UiLayoutGlobals dd)
    {
      //BL = min TR = max
      vc._rect.x = rasterQuad.Min.x;
      vc._rect.y = rasterQuad.Min.y;
      vc._rect.z = rasterQuad.Max.x;
      vc._rect.w = rasterQuad.Max.y;

      // Clip Rect.  For discard
      if (dd.DisableClip)
      {
        //We are only flipping Y in the shader now
        vc._clip.x = -Gui2d.MaxSize;
        vc._clip.y = -Gui2d.MaxSize;
        vc._clip.z = Gui2d.MaxSize;
        vc._clip.w = Gui2d.MaxSize;
      }
      else
      {
        vc._clip.x = b2ClipRect.Min.x;
        vc._clip.y = b2ClipRect.Min.y;
        vc._clip.z = b2ClipRect.Max.x;
        vc._clip.w = b2ClipRect.Max.y;
      }
    }

    private static uint vec4ToUint(vec4 color)
    {
      uint u = (
        ((uint)(color.x * 255.0f) << 24) |
        ((uint)(color.y * 255.0f) << 16) |
        ((uint)(color.z * 255.0f) << 8) |
        ((uint)(color.w * 255.0f) << 0)
      );
      return u;
    }
    private void UpdateTexture(MegaTex mt)
    {
      //set the correct MtTex if user changed texture id
      var utex = _style._props.Texture;
      if (utex != null && utex.Modified)
      {
        utex.Modified = false;
        if (utex.Loc != null)
        {
          var texfile = mt.GetResource(utex.Loc);
          if (texfile != null && texfile.Texs != null)
          {
            if (texfile.Texs.Count > 1)
            {
              Gu.Log.Warn($"More than one texture found in file '{texfile.FileLoc.QualifiedPath}' when setting UiTexture.");
              Gu.DebugBreak();
            }
            utex.Image = texfile.Texs[0];
          }
          if (utex.Image == null)
          {
            //display pink solid
            utex.Loc.LogNotFound();
            _style.RenderMode = UiRenderMode.Color;
            _style.Color = OffColor.Magenta;
            Gu.DebugBreak();
          }
        }
        else
        {
          _style.RenderMode = UiRenderMode.Color;
        }
      }
    }

    #endregion
    #endregion

  }//UiElement

  #endregion
  #region Gui2D

  public class Gui2d : UiElement, IGui2d
  {
    //@class Gui2d
    //@brief A GUI local to a given render viewport (RenderView)
    //*note:GUI element sizes translated relative to the current FBO size in the shader, all gui coords are in window coords
    #region Public: Members
    private enum UiAsyncUpdateState
    {
      CanUpdate, Updating, Updated, DoingEvents, DidEvents
    }

    public const int MaxSize = 9999999;
    public const int SlidingDiffWindow = 16;//16 chars for the string difference window. Replacement of a full float string.

    public static string Paragraph(string s) { return $"<p>{s}</p>"; }

    public RenderView RenderView { get; private set; }
    public IUiLayoutGlobals Globals { get { return _layoutGlobals; } }
    public MeshData Mesh { get; set; } = null;
    public MeshView MeshView { get; set; } = null;
    public long _dbg_UpdateMs { get; private set; } = 0;
    public long _dbg_MeshMs { get; private set; } = 0;
    public long _dbg_EventsMs { get; private set; } = 0;
    public UiStyleSheet StyleSheet { get; set; } = null;
    public List<IUiWindow> Windows { get { return _windows; } private set { _windows = value; } }
    public vec2 Scale { get; set; } = new vec2(1, 1);
    public List<string> NameTraps { get; } = new List<string>();
    public UiGlyphCache _glyphCache = new UiGlyphCache();

    #endregion
    #region Private:Members

    private vec2 _viewport_wh_last = new vec2(1, 1);
    private Gui2dShared _shared = null;
    private UiAsyncUpdateState _state = UiAsyncUpdateState.CanUpdate;
    private UiEventThing _eventThing = new UiEventThing();
    private Dictionary<uint, UiElement> _pickable = new Dictionary<uint, UiElement>();
    private bool _async = false;
    private Dictionary<UiElement, Dictionary<UiPropName, IUiPropAnimation>>? _animations = null;
    private List<IUiWindow> _windows = new List<IUiWindow>();
    private WeakReference<UiElement>? _keyboardFocus = null;
    private bool _layout_had_error = false;//Allow layout to fail and script to reload.
    private UiLayoutGlobals _layoutGlobals;
    private ivec4 _cachedView = ivec4.Zero;

    #endregion
    #region Public: Methods

    public Gui2d(Gui2dShared shared, RenderView rv)
    {
      StyleSheet = new UiStyleSheet("ui-default");
      _shared = shared;
      RenderView = rv;
      Name = "screen(root)";
      _layoutGlobals = new UiLayoutGlobals(this, _shared.MegaTex, StyleSheet, _glyphCache);

      //TODO:
      // _propAnimationTimer = new DeltaTimer(c_iPropAnimationTime, true, ActionState.Run, )

      //Default pick id for whole gui - we need this because we need to know whether or not we are ponting at
      //the GUI, or not. Sub-elements override this pick ID with their own "pick root"-s
      _iPickId = Gu.Context.Picker.GenPickId();
    }
    public void Update(double dt)
    {
      UpdatePropAnimations(dt);

      if (Gu.Context.PCKeyboard.Press(Keys.M))
      {
        _layoutGlobals.GlyphCache._round = !_layoutGlobals.GlyphCache._round;
        Gu.Log.Info($"Glyph rond = {_layoutGlobals.GlyphCache._round}");
        _layoutGlobals.ForceText = true;
      }

      //queue update if processed events.
      if (_state == UiAsyncUpdateState.CanUpdate)
      {
        _state = UiAsyncUpdateState.Updating;
        ThreadPool.QueueUserWorkItem(stateinfo =>
        {
          Dictionary<uint, UiElement>? pickable = null;
          if (RenderView != null && RenderView.Enabled)
          {
            long a = Gu.Milliseconds();
            StyleSheet?.Update();
            SetExtentsToViewport(RenderView);
            UpdateLayout_Async(Gu.Context.PCMouse, RenderView, ref pickable);
            this._dbg_UpdateMs = Gu.Milliseconds() - a;
          }
          Gu.Context.Gpu.Post_To_RenderThread(() =>
          {
            if (pickable != null)
            {
              _pickable = pickable;
            }
            foreach (var ele in _layoutGlobals.LayoutEvents)
            {
              ele.OnResize?.Invoke();
            }
            _layoutGlobals.LayoutEvents.Clear();
            if (RenderView != null && RenderView.Enabled)
            {
              SendMeshToGpu_Sync(RenderView);
              _state = UiAsyncUpdateState.Updated;
            }
          });
        });
      }

      //pick, add events
      if (Gu.Context.GameWindow.IsFocused)
      {
        Pick();
      }
      _eventThing.PollForEvents(this);

      //fire events
      if (_state == UiAsyncUpdateState.Updated)
      {
        _state = UiAsyncUpdateState.DoingEvents;

        if (RenderView != null)
        {
          long a = Gu.Milliseconds();
          foreach (var e in _eventThing._events)
          {
            e.Fire();
          }
          _dbg_EventsMs = Gu.Milliseconds() - a;
        }

        _eventThing._events.Clear();
        _state = UiAsyncUpdateState.DidEvents;
        _state = UiAsyncUpdateState.CanUpdate;
      }

    }
    public void Pick()
    {
      //See WorldObject->Pick
      if (Gu.Context.Picker.PickedObjectFrame == null)
      {
        //Do Pick
        var pixid = Gu.Context.Picker.SelectedPixelId;
        if (_pickable.TryGetValue(pixid, out var ele))
        {
          Gu.Context.Picker.PickedObjectFrame = ele;
        }
      }
    }
    public void OnResize()
    {
      //*note:GUI is translated to the current FBO size in the shader, all gui coords are in window coords
      //**This isn't really necessary, as we keep track of the viewport and force a layout change if it changes.
    }
    public Drawable GetDrawable()
    {
      //**THIS MAY BE INVALID - 
      //dummy is shared among contexts.. must test this
      //      return new Drawable(this.Name, MeshView, _worldMaterial_Op, mat4.Identity);
      //Swap out the mesh for this instance's mesh
      _shared.Dummy.MeshView = MeshView;
      return _shared.Dummy;
    }
    public void AddAnimation(IUiPropAnimation p)
    {
      //add a property animation (see $jquery(element).animate({ prop:value }); )
      Gu.Assert(p != null);
      Gu.Assert(p.Element != null);
      _animations = _animations.ConstructIfNeeded();

      Dictionary<UiPropName, IUiPropAnimation>? plist = null;
      if (!_animations.TryGetValue(p.Element, out plist))
      {
        plist = new Dictionary<UiPropName, IUiPropAnimation>();
        _animations.Add(p.Element, plist);
      }

      //overwrite with new, not allowing multiple animations on a prop.
      plist[p.Prop] = p;
    }
    public void SetKeyboardFocus(UiElement? newfocus)
    {
      if (_keyboardFocus != null)
      {
        if (_keyboardFocus.TryGetTarget(out var foc))
        {
          Gu.Context.PCKeyboard.RemoveListener(foc);
          foc.OnLostKeyboardFocus();
        }
        _keyboardFocus = null;
      }
      if (newfocus != null)
      {
        Gu.Context.PCKeyboard.AddListener(newfocus);
        newfocus.OnGotKeyboardFocus();
        _keyboardFocus = new WeakReference<UiElement>(newfocus);
      }
    }
    public void ResetWindowLayout()
    {
      foreach (var w in _windows)
      {
        w.SetDefaultPosition();
      }
    }
    public void NameTrap(UiElement e)
    {
      //called in scripts to trap an element by name
      foreach (var name in NameTraps)
      {
        if (e.Name.ToLower().Contains(name.ToLower()))
        {
          Gu.Trap();
        }
      }
    }

    #endregion
    #region Private: Methods

    private void UpdatePropAnimations(double dt)
    {
      if (_animations != null)
      {
        List<UiElement> toRemoveEles = new List<UiElement>();
        foreach (var ea in _animations)
        {
          List<UiPropName> toRemove = new List<UiPropName>();
          foreach (var ep in ea.Value)
          {
            if (ep.Value.Update(dt))
            {
              toRemove.Add(ep.Key);
            }
          }
          foreach (var k in toRemove)
          {
            ea.Value.Remove(k);
          }
          if (ea.Value.Count == 0)
          {
            toRemoveEles.Add(ea.Key);
          }
        }
        foreach (var k in toRemoveEles)
        {
          _animations.Remove(k);
        }
      }
    }
    private void UpdateLayout_Async(PCMouse mouse, RenderView rv, ref Dictionary<uint, UiElement>? pickable)
    {
      //pass 1 compute minimum sizes for children depth first, child relative positions, relative clip quads
      //pass 2 compute absolute positions elements, expand autos, compute quads, build vertex buffer

      if (_layout_had_error == false)
      {
        try
        {
          _layoutGlobals.Verts.Reset();//.Clear();
          _layoutGlobals.Force = true;
          _layoutGlobals.LayoutEvents.Clear();
          _layoutGlobals.Scale = Scale;
          _layoutGlobals.Framestamp++;
          _layoutGlobals.dbg_DidNotChange = 0;
          _layoutGlobals.dbg_FullyClipped = 0;

          var rootsiz = new UiSize2(_style._props.FixedWidth, _style._props.FixedHeight);
          PerformLayout_SizeElements(rootsiz, null, _layoutGlobals, false);
          PerformLayout_PositionElements(_layoutGlobals, null, _iPickId, ref pickable, 1);
          _layoutGlobals.ForceText = false;
        }
        catch (Exception ex)
        {
          Gu.Log.Error(ex);
          Gu.DebugBreak();
          _layout_had_error = true;
        }
      }
    }
    private void SendMeshToGpu_Sync(RenderView rv)
    {
      //RegenMesh
      var vts = _layoutGlobals.Verts.ToArray();

      if (Mesh == null)
      {
        Mesh = new MeshData(rv.Name + "gui-mesh", OpenTK.Graphics.OpenGL4.PrimitiveType.Points,
        Gpu.CreateVertexBuffer(rv.Name + "gui-mesh", vts), null, false);
      }
      else
      {
        Gu.Assert(Mesh != null);
        Gu.Assert(Mesh.VertexBuffers != null);
        Gu.Assert(Mesh.VertexBuffers.Count == 1);

        var dat = GpuDataPtr.GetGpuDataPtr(vts);
        Mesh.VertexBuffers[0].ExpandBuffer(_layoutGlobals.Verts.Count);
        Mesh.VertexBuffers[0].CopyToGPU(dat);
      }
      if (MeshView == null)
      {
        MeshView = new MeshView(Mesh, 0, _layoutGlobals.Verts.Count);
      }
      else
      {
        MeshView.SetLimits(0, _layoutGlobals.Verts.Count);
      }
    }
    private void SetExtentsToViewport(RenderView rv)
    {
      //Set props if we changed, but always set the layout quad
      if (
        rv.Viewport.X != _cachedView.x ||
        rv.Viewport.Y != _cachedView.y ||
        rv.Viewport.Width != _cachedView.z ||
        rv.Viewport.Height != _cachedView.w
      )
      {
        _cachedView.x = rv.Viewport.X;
        _cachedView.y = rv.Viewport.Y;
        _cachedView.z = rv.Viewport.Width;
        _cachedView.w = rv.Viewport.Height;

        _sort = 1;
        //_layer = 1;

        //We are probably getting rid of width height
        Style.Top = rv.Viewport.Y;
        Style.Left = rv.Viewport.X;
        Style.FixedWidth = rv.Viewport.Width;
        Style.FixedHeight = rv.Viewport.Height;
        Style.SizeModeWidth = UiSizeMode.Fixed;
        Style.SizeModeHeight = UiSizeMode.Fixed;
        Style.PositionModeX = UiPositionMode.Fixed;
        Style.PositionModeY = UiPositionMode.Fixed;
      }

      //Alwyas set layout quad.
      _block._b2LayoutQuad._left = rv.Viewport.X;
      _block._b2LayoutQuad._top = rv.Viewport.Y;
      _block._b2LayoutQuad._width = rv.Viewport.Width;
      _block._b2LayoutQuad._height = rv.Viewport.Height;
    }

    #endregion

  }//cls
  public class Gui2dShared
  {
    //Shared data between Gui2d instances for each context
    // public UiStyleSheet StyleSheet { get; private set; } = null;
    public Drawable Dummy { get; private set; } = null;
    public MegaTex MegaTex { get; private set; } = null;
    public string Name { get; private set; } = Lib.UnsetName;

    public Gui2dShared(string name, List<FileLoc> resources)
    {
      Name = name;
      //DO NOT USE MIPMAPS
      MegaTex = new MegaTex("gui_megatex", true, MegaTex.GetSystemDefaultClearColor(), false, TexFilter.Linear, 0);//nearest
      MegaTex.AddResources(resources);
      var tx = MegaTex.Compile();

      if (tx != null)
      {
        var shader = Gu.Lib.GetShader(Rs.Shader.GuiShader);
        Dummy = new WorldObject("gui");
        Dummy.Material = new Material("GuiMT", shader);
        Dummy.Material.GpuRenderState.DepthTest = false;
        Dummy.Material.GpuRenderState.Blend = true;
        Dummy.Material.AlbedoSlot.Texture = tx.Albedo;
        Dummy.Material.DrawMode = DrawMode.Forward;
        Dummy.Material.DrawOrder = DrawOrder.Last;
      }
      else
      {
        Gu.Log.Error("Failed to compile mega tex " + MegaTex.Name);
      }
    }

  }//cls
  public class Gui2dManager : OpenGLContextDataManager<Dictionary<ulong, Gui2dShared>>
  {
    //Manages GUIs among contexts
    //We will need to split this. Guimanager isn't a datablock
    public Gui2dManager() : base("Gui2DManager")
    {
    }
    protected override Dictionary<ulong, Gui2dShared> CreateNew()
    {
      return new Dictionary<ulong, Gui2dShared>();
    }
    public IGui2d GetOrCreateGui2d(string name, List<FileLoc> resources, RenderView rv)
    {
      var gdat = GetOrCreateGui2dShared(name, resources);
      return new Gui2d(gdat, rv);
    }
    private Gui2dShared GetOrCreateGui2dShared(string name, List<FileLoc> resources)
    {
      if (!resources.Contains(UiFontFace.RobotoMono))
      {
        //Must add default font or else we can't render glyphs.
        resources.Add(UiFontFace.RobotoMono);
      }

      //if resources change we need a different mega texture so that is why we hash the resources

      var qualifiedPaths = resources.ConvertAll((x) => { return x.QualifiedPath; });
      var hash = Hash.HashStringArray(qualifiedPaths);

      Gui2dShared? g = null;
      var dict = GetDataForContext(Gu.Context);
      if (!dict.TryGetValue(hash, out g))
      {
        g = new Gui2dShared(name, resources);
        dict.Add(hash, g);
      }
      return g;
    }
  }//cls

  #endregion

}//ns
