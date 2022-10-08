using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.ComponentModel;


namespace PirateCraft
{
  #region Enums

  using UiAction = Action<UiEventId, UiElement, PCMouse, Gui2d>;

  public class FontFace : FileLoc
  {
    public string Name = "";
    public FontFace() { }
    protected FontFace(string name, FileLoc loc) : base(loc.RawPath, loc.FileStorage) { Name = name; }
    public static FontFace Parisienne = new FontFace("Parisienne", new FileLoc("Parisienne-Regular.ttf", FileStorage.Embedded));
    public static FontFace RobotoMono = new FontFace("RobotoMono", new FileLoc("RobotoMono-Regular.ttf", FileStorage.Embedded));
    public static FontFace PressStart2P = new FontFace("PressStart2P", new FileLoc("PressStart2P-Regular.ttf", FileStorage.Embedded));
    //public static FontFace Entypo = new FontFace("Entypo", new FileLoc("Entypo.ttf", FileStorage.Embedded));
    public static FontFace Calibri = new FontFace("Calibri", new FileLoc("calibri.ttf", FileStorage.Embedded));
    public static FontFace EmilysCandy = new FontFace("EmilysCandy", new FileLoc("EmilysCandy-Regular.ttf", FileStorage.Embedded));
  }
  public enum UiDisplayMode
  {
    [CSSAttribute("inline")] Inline,
    [CSSAttribute("block")] Block,
    [CSSAttribute("inline-no-wrap")] InlineNoWrap
  }
  public enum UiPositionMode
  {
    //Note: the position terminology here mirrors that of CSS. 
    [CSSAttribute("static")] Static, // elements flow within the page/container.
    [CSSAttribute("relative")] Relative, // elements are positioned absolutely, relative to the container.
    [CSSAttribute("relative-constrain-x")] RelativeConstrainX, //Relative positioning, but cannot go outside parent boundary. For scrollbars.
    [CSSAttribute("relative-constrain-y")] RelativeConstrainY,
    [CSSAttribute("relative-constrain-xy")] RelativeConstrainXY,
    [CSSAttribute("absolute")] Absolute,
  }
  public enum UiSizeMode
  {
    [CSSAttribute("shrink")] Shrink, //Shrink to size of child contents, taking Max Width/Height into account
    [CSSAttribute("expand")] Expand, //Expand to parent
    [CSSAttribute("fixed")] Fixed // Fixed width/height
  }
  public enum UiEventId
  {
    Mouse_Lmb_Up,
    Mouse_Lmb_Press,
    Mouse_Lmb_Hold,
    Mouse_Lmb_Release,
    Mouse_Lmb_None,

    Mouse_Rmb_Up,
    Mouse_Rmb_Press,
    Mouse_Rmb_Hold,
    Mouse_Rmb_Release,
    Mouse_Rmb_None,

    Mouse_Mmb_Up,
    Mouse_Mmb_Press,
    Mouse_Mmb_Hold,
    Mouse_Mmb_Release,
    Mouse_Mmb_None,

    Mouse_Enter,
    Mouse_Move,
    Mouse_Leave,

    Scrollbar_Pos_Change,

    Drag,
    Tick, //tick event like every x ms
  };
  public enum UiOverflowMode
  {
    [CSSAttribute("hide")] Show,
    [CSSAttribute("show")] Hide
  };
  public enum UiImageTiling
  {
    [CSSAttribute("expand")] Expand,
    [CSSAttribute("tile")] Tile,
    [CSSAttribute("computed")] Computed,
    [CSSAttribute("proportion")] Proportion
  }
  public enum UiMouseState
  {
    //This differs from ButtonState in the fact
    None,  // not hovering
    Enter, // hover start
    Hover, // hovering
    Move,  // hover + moved
    Leave, // hover end
    Press, // hover + click
    Hold,  // hover + hold
    Up   // hover + release
  }
  public enum UiPropName
  {
    Top
    , Left
    , Width
    , Height
    , MinWidth
    , MinHeight
    , MaxWidth
    , MaxHeight
    , PadTop
    , PadRight
    , PadBot
    , PadLeft
    , MarginTop
    , MarginRight
    , MarginBot
    , MarginLeft
    , BorderTop
    , BorderRight
    , BorderBot
    , BorderLeft
    , BorderTopLeftRadius
    , BorderTopRightRadius
    , BorderBotRightRadius
    , BorderBotLeftRadius
    , Color
    , BorderColor
    , FontFace
    , FontSize
    , FontStyle
    , FontColor
    , LineHeight
    , Texture
    , ImageTilingX
    , ImageTilingY
    , DisplayMode
    , PositionMode
    , OverflowMode
    , SizeModeWidth
    , SizeModeHeight

    //****
    , MaxUiProps
  }
  public enum UiFontStyle
  {
    [CSSAttribute("normal")] Normal,
    [CSSAttribute("bold")] Bold,
    [CSSAttribute("italic")] Italic
  }

  #endregion

  public class UiDebugDraw
  {
    public bool DisableClip = false;
    public bool ShowOverlay = false;
    public vec4 OverlayColor = new vec4(1, 0, 0, 0.5f);
  }
  public class UiDragInfo
  {
    private bool _bDragStart = false;
    private vec2 _vDragStart;
    private WeakReference<UiElement> Target { get; set; } = null;

    public UiDragInfo() { }
    public void StartDrag(UiElement target, PCMouse ms)
    {
      if (target != null)
      {
        Target = new WeakReference<UiElement>(target);
        _vDragStart = ms.Pos;
        _bDragStart = true;
      }
    }
    public void EndDrag()
    {
      Target = null;
      _bDragStart = false;
    }
    public void UpdateDrag(PCMouse ms)
    {
      float mw = 1.0f / 1; // UiScreen::getDesignMultiplierW the design multiplier - this isntn't accounted for
      float mh = 1.0f / 1; // UiScreen::getDesignMultiplierH the design multiplier - this isntn't accounted for
      if (Target != null && Target.TryGetTarget(out var tar))
      {
        //Check for mouse delta to prevent unnecessary updates.
        bool canDrag = false;
        vec2 dp = ms.Pos - _vDragStart;
        if (MathUtils.FuzzyEquals(dp.x, 0.0f) == false || MathUtils.FuzzyEquals(dp.y, 0.0f) == false)
        {
          canDrag = true;
        }
        else
        {
          canDrag = false;
        }
        if (canDrag)
        {
          //Multiply the distance by the design size.
          dp.x *= mw;
          dp.y *= mh;

          if (tar.DragFunc != null)
          {
            tar.DragFunc(dp);
          }

          //Reset drag start
          _vDragStart = ms.Pos;
        }
      }
      else
      {
        Target = null;
      }

    }
  }
  public class UiProps
  {
    //All styled properties of an element. 
    // All elements contain one properties class with all value type properties set to default (besides texture/font face, but they MUST be set).
    //
    public float Bottom { get { return Top + Height; } }
    public float Right { get { return Left + Width; } }

    public float Top = 0;
    public float Left = 0;
    public float Width = 1;
    public float Height = 1;
    public float MinWidth = 0;
    public float MinHeight = 0;
    public float MaxWidth = Gui2d.MaxSize;
    public float MaxHeight = Gui2d.MaxSize;
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
    public vec4 BorderColor = new vec4(1, 1, 1, 1);
    public PirateCraft.FontFace FontFace = PirateCraft.FontFace.RobotoMono;
    public float FontSize = 12;
    public UiFontStyle FontStyle = UiFontStyle.Normal;
    public vec4 FontColor = new vec4(0, 0, 0, 1);
    public float LineHeight = 1;
    public MtTex Texture = null;
    public UiImageTiling ImageTilingX = UiImageTiling.Expand;
    public UiImageTiling ImageTilingY = UiImageTiling.Expand;
    public UiDisplayMode DisplayMode = UiDisplayMode.Block;
    public UiPositionMode PositionMode = UiPositionMode.Static;
    public UiOverflowMode OverflowMode = UiOverflowMode.Hide;
    public UiSizeMode SizeModeWidth = UiSizeMode.Expand;
    public UiSizeMode SizeModeHeight = UiSizeMode.Expand;

    //Most of this generic field junk can go away and we can manually just return the variables. My hands were huring here so..ugh
    private static UiProps _defaults = new UiProps();//defaults are just set on the field initializer.
    public static Dictionary<UiPropName, System.Reflection.FieldInfo> Fields { get; private set; } = null;

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
      var fi = Fields[p];
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
      var fi = Fields[p];
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
    private static void CreateStaticFieldInfo()
    {
      if (Fields == null)
      {
        Fields = new Dictionary<UiPropName, System.Reflection.FieldInfo>();

        foreach (var f in typeof(UiProps).GetFields())
        {
          var pvals = Enum.GetValues(typeof(UiPropName));
          foreach (var pp in pvals)
          {
            string a = pp.ToString();
            string b = f.Name;
            if (a.Equals(b))
            {
              Fields.Add((UiPropName)pp, f);
              break;
            }
          }
        }
        //Make sure we got all fields.
        for (int i = 0; i < (int)UiPropName.MaxUiProps; ++i)
        {
          UiPropName p = (UiPropName)i;
          Gu.Assert(Fields.ContainsKey(p));
        }
      }
    }
  }
  public class UiStyle
  {
    #region Public: Aggregate Prop Setters

    public string Name { get; set; } = "";

    [CSSAttribute("margin")]
    public float? Margin
    {
      get { return (float?)_props.Get(UiPropName.MarginTop); }
      set
      {
        MarginTop = MarginRight = MarginBot = MarginLeft = value;
      }
    }
    [CSSAttribute("border")]
    public float? Border
    {
      get { return (float?)_props.Get(UiPropName.BorderTop); }
      set
      {
        BorderTop = BorderRight = BorderLeft = BorderBot = value;
      }
    }
    [CSSAttribute("border-radius")]
    public float? BorderRadius
    {
      get { return (float?)_props.Get(UiPropName.BorderTopLeftRadius); }
      set
      {
        BorderTopLeftRadius = BorderTopRightRadius = BorderBotRightRadius = BorderBotLeftRadius = value;
      }
    }
    [CSSAttribute("padding")]
    public float? Padding
    {
      get { return (float?)_props.Get(UiPropName.PadTop); }
      set
      {
        PadTop = PadRight = PadBot = PadLeft = value;
      }
    }
    public float? Bottom { get { return (float?)GetClassValue(UiPropName.Top) + (float?)GetClassValue(UiPropName.Height); } }
    public float? Right { get { return (float?)GetClassValue(UiPropName.Left) + (float?)GetClassValue(UiPropName.Width); } }


    #endregion
    #region Public: User Prop Setters

    //Manual setters.. these will cause this style class to own this property
    //**Note: Do not use nullable<> or ? types on class types here. This will return (null) even if the class type is set on the nullable boxer.
    //OK so you could actually just return _props.Top .. etc here, but for now we're doing this to simplify things (as they are written)
    [CSSAttribute("top")] public float? Top { get { return (float?)GetClassValue(UiPropName.Top); } set { SetClassValue(UiPropName.Top, (float?)value); } }
    [CSSAttribute("left")] public float? Left { get { return (float?)GetClassValue(UiPropName.Left); } set { SetClassValue(UiPropName.Left, (float?)value); } }
    [CSSAttribute("width")] public float? Width { get { return (float?)GetClassValue(UiPropName.Width); } set { SetClassValue(UiPropName.Width, (float?)value); } }
    [CSSAttribute("height")] public float? Height { get { return (float?)GetClassValue(UiPropName.Height); } set { SetClassValue(UiPropName.Height, (float?)value); } }
    [CSSAttribute("min-width")] public float? MinWidth { get { return (float?)GetClassValue(UiPropName.MinWidth); } set { SetClassValue(UiPropName.MinWidth, (float?)value); } }
    [CSSAttribute("min-height")] public float? MinHeight { get { return (float?)GetClassValue(UiPropName.MinHeight); } set { SetClassValue(UiPropName.MinHeight, (float?)value); } }
    [CSSAttribute("max-width")] public float? MaxWidth { get { return (float?)GetClassValue(UiPropName.MaxWidth); } set { SetClassValue(UiPropName.MaxWidth, (float?)value); } }
    [CSSAttribute("max-height")] public float? MaxHeight { get { return (float?)GetClassValue(UiPropName.MaxHeight); } set { SetClassValue(UiPropName.MaxHeight, (float?)value); } }
    [CSSAttribute("padding-top")] public float? PadTop { get { return (float?)GetClassValue(UiPropName.PadTop); } set { SetClassValue(UiPropName.PadTop, (float?)value); } }
    [CSSAttribute("padding-right")] public float? PadRight { get { return (float?)GetClassValue(UiPropName.PadRight); } set { SetClassValue(UiPropName.PadRight, (float?)value); } }
    [CSSAttribute("padding-bottom")] public float? PadBot { get { return (float?)GetClassValue(UiPropName.PadBot); } set { SetClassValue(UiPropName.PadBot, (float?)value); } }
    [CSSAttribute("padding-left")] public float? PadLeft { get { return (float?)GetClassValue(UiPropName.PadLeft); } set { SetClassValue(UiPropName.PadLeft, (float?)value); } }
    [CSSAttribute("margin-top")] public float? MarginTop { get { return (float?)GetClassValue(UiPropName.MarginTop); } set { SetClassValue(UiPropName.MarginTop, (float?)value); } }
    [CSSAttribute("margin-right")] public float? MarginRight { get { return (float?)GetClassValue(UiPropName.MarginRight); } set { SetClassValue(UiPropName.MarginRight, (float?)value); } }
    [CSSAttribute("margin-bottom")] public float? MarginBot { get { return (float?)GetClassValue(UiPropName.MarginBot); } set { SetClassValue(UiPropName.MarginBot, (float?)value); } }
    [CSSAttribute("margin-left")] public float? MarginLeft { get { return (float?)GetClassValue(UiPropName.MarginLeft); } set { SetClassValue(UiPropName.MarginLeft, (float?)value); } }
    [CSSAttribute("border-top")] public float? BorderTop { get { return (float?)GetClassValue(UiPropName.BorderTop); } set { SetClassValue(UiPropName.BorderTop, (float?)value); } }
    [CSSAttribute("border-right")] public float? BorderRight { get { return (float?)GetClassValue(UiPropName.BorderRight); } set { SetClassValue(UiPropName.BorderRight, (float?)value); } }
    [CSSAttribute("border-bottom")] public float? BorderBot { get { return (float?)GetClassValue(UiPropName.BorderBot); } set { SetClassValue(UiPropName.BorderBot, (float?)value); } }
    [CSSAttribute("border-left")] public float? BorderLeft { get { return (float?)GetClassValue(UiPropName.BorderLeft); } set { SetClassValue(UiPropName.BorderLeft, (float?)value); } }
    [CSSAttribute("border-top-left-radius")] public float? BorderTopLeftRadius { get { return (float?)GetClassValue(UiPropName.BorderTopLeftRadius); } set { SetClassValue(UiPropName.BorderTopLeftRadius, (float?)value); } }
    [CSSAttribute("border-top-right-radius")] public float? BorderTopRightRadius { get { return (float?)GetClassValue(UiPropName.BorderTopRightRadius); } set { SetClassValue(UiPropName.BorderTopRightRadius, (float?)value); } }
    [CSSAttribute("border-bottom-right-radius")] public float? BorderBotRightRadius { get { return (float?)GetClassValue(UiPropName.BorderBotRightRadius); } set { SetClassValue(UiPropName.BorderBotRightRadius, (float?)value); } }
    [CSSAttribute("border-bottom-left-radius")] public float? BorderBotLeftRadius { get { return (float?)GetClassValue(UiPropName.BorderBotLeftRadius); } set { SetClassValue(UiPropName.BorderBotLeftRadius, (float?)value); } }
    [CSSAttribute("font-color")] public vec4? Color { get { return (vec4?)GetClassValue(UiPropName.Color); } set { SetClassValue(UiPropName.Color, (vec4?)value); } }
    [CSSAttribute("border-color")] public vec4? BorderColor { get { return (vec4?)GetClassValue(UiPropName.BorderColor); } set { SetClassValue(UiPropName.BorderColor, (vec4?)value); } }
    [CSSAttribute("font-family")] public FontFace FontFace { get { return (FontFace)GetClassValue(UiPropName.FontFace); } set { SetClassValue(UiPropName.FontFace, (FontFace)value); } }
    [CSSAttribute("font-size")] public float? FontSize { get { return (float?)GetClassValue(UiPropName.FontSize); } set { SetClassValue(UiPropName.FontSize, (float?)value); } }
    [CSSAttribute("font-style")] public UiFontStyle? FontStyle { get { return (UiFontStyle?)GetClassValue(UiPropName.FontStyle); } set { SetClassValue(UiPropName.FontStyle, (UiFontStyle?)value); } }
    [CSSAttribute("color")] public vec4? FontColor { get { return (vec4?)GetClassValue(UiPropName.FontColor); } set { SetClassValue(UiPropName.FontColor, (vec4?)value); } }
    [CSSAttribute("line-height")] public float? LineHeight { get { return (float?)GetClassValue(UiPropName.LineHeight); } set { SetClassValue(UiPropName.LineHeight, (float?)value); } }
    [CSSAttribute("position-mode")] public UiPositionMode? PositionMode { get { return (UiPositionMode?)GetClassValue(UiPropName.PositionMode); } set { SetClassValue(UiPropName.PositionMode, (UiPositionMode?)value); } }
    [CSSAttribute("overflow-mode")] public UiOverflowMode? OverflowMode { get { return (UiOverflowMode?)GetClassValue(UiPropName.OverflowMode); } set { SetClassValue(UiPropName.OverflowMode, (UiOverflowMode?)value); } }
    [CSSAttribute("size-mode-width")] public UiSizeMode? SizeModeWidth { get { return (UiSizeMode?)GetClassValue(UiPropName.SizeModeWidth); } set { SetClassValue(UiPropName.SizeModeWidth, (UiSizeMode?)value); } }
    [CSSAttribute("size-mode-height")] public UiSizeMode? SizeModeHeight { get { return (UiSizeMode?)GetClassValue(UiPropName.SizeModeHeight); } set { SetClassValue(UiPropName.SizeModeHeight, (UiSizeMode?)value); } }
    [CSSAttribute("display")] public UiDisplayMode? DisplayMode { get { return (UiDisplayMode?)GetClassValue(UiPropName.DisplayMode); } set { SetClassValue(UiPropName.DisplayMode, (UiDisplayMode?)value); } }
    [CSSAttribute("image-tiling-x")] public UiImageTiling? ImageTilingX { get { return (UiImageTiling?)GetClassValue(UiPropName.ImageTilingX); } set { SetClassValue(UiPropName.ImageTilingX, (UiImageTiling?)value); } }
    [CSSAttribute("image-tiling-y")] public UiImageTiling? ImageTilingY { get { return (UiImageTiling?)GetClassValue(UiPropName.ImageTilingY); } set { SetClassValue(UiPropName.ImageTilingY, (UiImageTiling?)value); } }
    [CSSAttribute("texture")] public MtTex Texture { get { return (MtTex)GetClassValue(UiPropName.Texture); } set { SetClassValue(UiPropName.Texture, (MtTex)value); } }

    #endregion
    #region Public: Methods

    public UiProps _props = new UiProps();//Gets the compiled / final props
    public bool IsPropsOnly { get; set; } = false;//For glyph, don't inherit parent or compile, and re-compile the class every time.. we set _props manually
    public WeakReference<UiStyleSheet> StyleSheet { get; private set; } = null;

    private bool _bMustCompile = true;
    private long _changedFrameId = 0;
    private long _compiledFrameId = 0;
    private HashSet<WeakReference<UiElement>> _eles = null;
    private BitArray _owned = new BitArray((int)UiPropName.MaxUiProps);//This bitset tells us which props were set
    private BitArray _inherited = new BitArray((int)UiPropName.MaxUiProps);
    private BitArray _defaulted = new BitArray((int)UiPropName.MaxUiProps);
    private BitArray _changed = new BitArray((int)UiPropName.MaxUiProps);//props that changed during the last class compile
    private List<UiStyle> _superStyles = null;
    private List<string> _superStylesNames = null;//Translate this with Stylesheet.
    private bool _bMustTranslateInheritedStyles = false;

    private bool IsInline = false;

#if DEBUG
    private Dictionary<UiPropName, object?> _debugChangedNamesList = null;//Properties owned (set) by this class. For visual debug
    private Dictionary<UiPropName, object?> _debugOwnedNamesList = null;//Properties owned (set) by this class. For visual debug
    private Dictionary<UiPropName, object?> _debugInheritedNamesList = null;//Properties owned (set) by this class. For visual debug
    private Dictionary<UiPropName, object?> _debugDefaultNamesList = null;//Properties owned (set) by this class. For visual debug
#endif

    public UiStyle(string name, List<string> inherted_styles = null, bool isInline = false)
    {
      Name = name;
      IsInline = isInline;
      if (inherted_styles != null)
      {
        _superStylesNames = inherted_styles;
        _bMustTranslateInheritedStyles = true;
      }
    }
    public void SetInheritStyles(List<string> styles)
    {
      //Set the styles we inherit, then we can compile them.
      if ((_superStylesNames == null || _superStylesNames.Count == 0) && (styles == null || styles.Count == 0))
      {
        //no change
      }
      else
      {
        _superStylesNames = styles;
        _bMustTranslateInheritedStyles = true;
      }
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
    private void TranslateStyleNames(UiStyleSheet sheet)
    {
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
    private void DebugStorePropDetails()
    {
#if DEBUG
      //Debug / view all props that this class owns/inherits
      _debugChangedNamesList = _debugChangedNamesList.ConstructIfNeeded();
      _debugChangedNamesList.Clear();

      _debugOwnedNamesList = _debugOwnedNamesList.ConstructIfNeeded();
      _debugOwnedNamesList.Clear();

      _debugInheritedNamesList = _debugInheritedNamesList.ConstructIfNeeded();
      _debugInheritedNamesList.Clear();

      _debugDefaultNamesList = _debugDefaultNamesList.ConstructIfNeeded();
      _debugDefaultNamesList.Clear();

      for (int i = 0; i < (int)UiPropName.MaxUiProps; i++)
      {
        var prop = (UiPropName)i;

        if (_changed.Get(i))
        {
          _debugChangedNamesList.Add(prop, GetPropValue(prop));
        }

        if (_owned.Get(i))
        {
          _debugOwnedNamesList.Add(prop, GetPropValue(prop));
        }
        if (_inherited.Get(i))
        {
          _debugInheritedNamesList.Add(prop, GetPropValue(prop));
        }
        if (_defaulted.Get(i))
        {
          _debugDefaultNamesList.Add(prop, GetPropValue(prop));
        }
      }
      //These sets are mutually exclusive.
      BitArray b2 = _owned.AndWith(_defaulted);
      BitArray b3 = _owned.AndWith(_inherited);
      BitArray b4 = _inherited.AndWith(_defaulted);
      Gu.Assert(b2.UInt64Value() == 0);
      Gu.Assert(b3.UInt64Value() == 0);
      Gu.Assert(b4.UInt64Value() == 0);
#endif
    }
    public void CompileStyleTree(UiStyleSheet s, long framestamp, UiStyle style_DOM_parent = null)
    {
      //Compile.. for example: <div top="3"> <div class=" class1 class2 class1 " style="top:3" right="5">  into a single set of properties for each <div>
      // parent style (tag), <style style style> (classes), owned (inline)... <div class/style="parent stuff"> <div class="a b c b a" style="inline stuff.."
      if (!IsPropsOnly)
      {
        TranslateStyleNames(s);

        if (_bMustCompile)
        {
          _inherited.SetAll(false);
          _defaulted.SetAll(false);

          DebugStorePropDetails();

          foreach (var p in UiProps.Fields)
          {
            if (!IsOwned(p.Key))
            {
              //not owned, get the value from a superclass
              if (!InheritFromSuperClasses(s, p.Key, p.Value, framestamp))
              {
                //if subclasses are not set, then try the parent DOM element, otherwise we'll get set to a default value
                if (!InheritFromParentTag(style_DOM_parent, p.Key, p.Value))
                {
                  //No parent element, and, no styles, set to default.
                  SetDefaultValue(p.Key, p.Value);
                }
              }
            }
          }

          DebugStorePropDetails();

          _changed.SetAll(false);
          _compiledFrameId = framestamp;
          _bMustCompile = false;
        }
      }
    }
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
              _superStyles[i].CompileStyleTree(s, framestamp, null);
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
        var val = fromStyle.GetPropValue(p);
        fi.SetValue(this._props, val);
        _inherited.Set((int)p, true);
        return true;
      }
      return false;
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
      UiStyle ret = new UiStyle(this.Name + Library.CopyName, this._superStylesNames);
      ret._props = _props.Clone();
      ret._eles = null;
      ret._bMustCompile = true;
      return ret;
    }
    public void AddReference(UiElement u)
    {
      _eles = _eles.ConstructIfNeeded();
      //Adds a reference to the given element, so when we change the style, teh element gets updated.
      foreach (var e in _eles)
      {
        if (e.TryGetTarget(out var ee))
        {
          if (ee == u)
          {
            Gu.DebugBreak();//Duplicate element reference in the style.
            return;
          }
        }
      }
      _eles.Add(new WeakReference<UiElement>(u));
    }
    public void RemoveReference(UiElement u)
    {
      if (_eles != null)
      {
        _eles.RemoveWhere((x) =>
        {
          if (x.TryGetTarget(out var ee))
          {
            return ee.Equals(u);
          }
          else
          {
            return false;
          }
        });
      }
    }

    #endregion
    #region Private: Methods
    private void SetClassValue(UiPropName p, object? value)
    {
      //Set nullable value for class. Set to null to clear/inherit value
      if (CheckValueModified(p, value))
      {
        SetClassValueDirect(p, value);
      }
    }
    private bool CheckValueModified(UiPropName p, object? new_class_value)
    {
      //Return true if
      var owned = _owned.Get((int)p);
      if (new_class_value == null && owned == false)
      {
        //Class value is unset, and was set it to null again.. no change
        return false;
      }
      else if (new_class_value != null && owned == false)
      {
        //Class value is unset, and we set a fresh value... definite change
        return true;
      }
      else if (new_class_value != null && owned == true)
      {
        //Class value is set, and we set a new value check for value difference (prevent recompiling all classes!)
        //Check hard (prop) value for new value
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
      //Set the class value, skipping over modified value checking.
      if (value != null)
      {
        _owned.Set((int)p, true);
        _props.Set(p, value); //Only set the prop value if not null, as, null is basically the way we say "clear the value"
      }
      else
      {
        _owned.Set((int)p, false);
      }
      _changed.Set((int)p, true);
      _changedFrameId = Gu.Context.FrameStamp;
      _bMustCompile = true;

      //SetLayoutChanged. Currently, classes are compiled for elements, which makes this sub-optimal
      //We really need to use the StyleSHeets to compile classes, then we can call SetLayoutChanged only one time for all _bMustCompile styles.
      //We'll have to figure out how to use Inline (UiElement) styles here though
      IterateElements((e) =>
      {
        e.SetLayoutChanged();
      });
    }
    private object? GetClassValue(UiPropName p)
    {
      //Get the class value (not compiled value)
      var owned = _owned.Get((int)p);
      if (!owned)
      {
        return null;
      }
      else
      {
        return GetPropValue(p);
      }
    }
    private object? GetPropValue(UiPropName p)
    {
      //Get the compiled / or / owned value
      return _props.Get(p);
    }
    private void IterateElements(Action<UiElement> act)
    {
      if (_eles != null)
      {
        List<WeakReference<UiElement>> remove = new List<WeakReference<UiElement>>();
        foreach (var ele in _eles)
        {
          if (ele.TryGetTarget(out var e))
          {
            act(e);
          }
          else
          {
            remove.Add(ele);
          }
        }
        foreach (var e in remove)
        {
          _eles.Remove(e);
        }
      }
    }
    #endregion

  }
  public class UiElement
  {
    #region Inner Classes 

    private class UiLine
    {
      public float _top = 0;
      public float _left = 0;
      public float _height = 0;
      public float _width = 0;
      public List<UiElement> _eles = new List<UiElement>();
      public UiLine(float left, float top)
      {
        _left = left;
        _top = top;
      }
    }
    public class Quads
    {
      public Box2f _b2ComputedQuad = new Box2f();      // Base quad that is calculated from _right/_left.., parent, padding, etc
      public Box2f _b2OffsetQuad = new Box2f();        // Offseted quad
      public Box2f _b2LayoutQuad = new Box2f();        // Transformed from design space into screen space.
      public Box2f _b2RasterQuad = new Box2f();      // Final raster quad in OpenGL screen coordinates. BOTTOM LEFT = min TOP RIGHT = max
      public Box2f _b2PreOffsetRasterQuad = new Box2f();      // Debug, raster quad for pre-offset
      public vec2 _rtl = new vec2(0, 0);
      public vec2 _rtr = new vec2(0, 0);
      public vec2 _rbr = new vec2(0, 0);
      public vec2 _rbl = new vec2(0, 0);
    }

    #endregion
    #region Public: Members

    public string Name { get { return _name; } set { _name = value; } }
    public string Tag { get; set; } = "";
    public string Text
    {
      get { return _strText; }
      set
      {
        _strTextLast = _strText;
        _strText = value;
        SetLayoutChanged();
        _bTextChanged = true;
      }
    }
    public UiStyle Style
    {
      //overrides Style Class
      get
      {
        if (_style == null)
        {
          _style = new UiStyle(StyleName.Inline, null, true);
          _style.AddReference(this);
        }
        return _style;
      }
      //Can't set Style since it must be owned by this class. Inherit the on the style to add subclasses
    }
    public bool DragEnabled { get; private set; } = false;
    public Action<vec2> DragFunc { get; private set; } = null;
    public float MinValue { get; set; } = 0;
    public float MaxValue { get; set; } = 100;
    public bool ScaleToDesign { get; set; } = true; // this is true for all elements besides cursor.
    public bool LayoutChanged { get; private set; } = true;
    public bool LayoutVisible { get { return _layoutVisible; } set { _layoutVisible = value; SetLayoutChanged(); } }
    public bool RenderVisible { get { return _renderVisible; } set { _renderVisible = value; SetLayoutChanged(); } }
    public Dictionary<UiEventId, MultiMap<string, UiAction>> Events { get { return _events; } set { _events = value; } }

    public Box2f Computed { get { return _contentArea._b2ComputedQuad; } }

    #endregion
    #region Private: Members

    protected const int c_AllSortLayers = -1;
    protected const int c_BaseLayerSort = 1000;
    protected const int c_GlyphLayerSort = 2000;
    public const int c_ContextMenuSort = 3000;

    protected string _name = "";
    protected UiStyle _style = null;      //Inline style
    protected MultiMap<int, UiElement> _children { get; set; } = null;
    private WeakReference<UiElement> _parent = null;
    private bool _pickEnabled = false;
    private MtFontLoader _cachedFont = null;//For labels that contain glyphs
    private MtCachedCharData? _cachedGlyph = null;//For glyphs
    private bool _layoutVisible = true;
    private bool _renderVisible = true;
    private bool _bTextChanged = false;
    private vec2 _tileScale = new vec2(1, 1); //Scale if UiImageSizeMode is Tile
    protected Quads _contentArea = new Quads();
    private Quads _borderArea = null;
    private Box2f _b2ContentQuad = new Box2f();       // Encompassess ALL of the child quads, plus overflow.
    protected Box2f? _renderOffset = null;
    public uint _iPickId = 0;
    protected bool _bPickedThisFrame = false;
    protected bool _bPickedPreviousFrame = false;
    private long _iPickedFrameId = 0;
    protected string _strText = "";
    private string _strTextLast = "";
    private bool _bMustRedoTextBecauseOfStyle = false;
    private Dictionary<UiEventId, MultiMap<string, UiAction>> _events = null;

    #endregion
    #region Public: Methods

    public UiElement()
    {
    }
    public UiElement(string name)
    {
      init(null, name);
    }
    public UiElement(List<string> styleClasses, string name)
    {
      init(styleClasses, name);
    }
    public UiElement(List<string> styleClasses, string name, Phrase phrase)
    {
      init(styleClasses, name, Gu.Translator.Translate(phrase));
    }
    public UiElement(List<string> styleClasses, string name, string text)
    {
      init(styleClasses, name, text);
    }
    public UiElement(List<string> styleClasses, string name, Phrase phrase, List<UiElement> children)
    {
      init(styleClasses, name, Gu.Translator.Translate(phrase), children);
    }
    public UiElement(List<string> styleClasses, string name, string text, List<UiElement> children)
    {
      init(styleClasses, name, text, children);
    }

    public void Hide()
    {
      _layoutVisible = _renderVisible = false;
    }
    public void Show()
    {
      _layoutVisible = _renderVisible = true;
    }
    public void ResetPick()
    {
      this._bPickedPreviousFrame = _bPickedThisFrame;
      this._bPickedThisFrame = false;
    }
    public void AddChild(UiElement e, int sort = c_BaseLayerSort)
    {
      //SEt child style if it is not present (e.g. cascading styles) 
      //Actually no, we should compile the style, and not actually set a new one.
      // if (this.StyleClass != null && e.StyleClass == null)
      // {
      //   e.StyleClass = this.StyleClass;
      // }
      if (e._parent != null && e._parent.TryGetTarget(out UiElement p))
      {
        p.RemoveChild(e);
      }
      if (_children == null)
      {
        _children = new MultiMap<int, UiElement>();
      }
      _children.Add(sort, e);
      e._parent = new WeakReference<UiElement>(this);
      e.SetLayoutChanged();
    }
    public bool RemoveChild(UiElement e)
    {
      if (_children != null)
      {
        foreach (var k in _children.Keys)
        {
          if (_children.Remove(k, e))
          {
            e._parent = null;
            e.SetLayoutChanged();
            return true;
          }
        }
        if (_children.Count == 0)
        {
          _children = null;
        }
      }
      //could not remove.
      return false;
    }
    public void ClearChildren(int sort = c_AllSortLayers)
    {
      _children?.Clear();
    }
    public void EnableDrag(Action<vec2> func)
    {
      DragEnabled = true;
      DragFunc = func;
      SetLayoutChanged();//get collected
    }
    public void DisableDrag()
    {
      DragEnabled = false;
      SetLayoutChanged();//get collected
    }
    public void ValidateQuad()
    {
      if (((Style._props.Left + Style._props.Width) < Style._props.Left) || ((Style._props.Top + Style._props.Height) < Style._props.Top))
      {
        Gu.Log.Error("Computed Quad is invalid, rtbl= " + (Style._props.Left + Style._props.Width) + "," + Style._props.Left + "," + (Style._props.Top + Style._props.Height) + "," + Style._props.Top + ".");
        Gu.DebugBreak();
      }
    }
    public T GetFirstChild<T>() where T : UiElement
    {
      Gu.Assert(_children.Count > 0);
      return (T)_children.First().Value;
    }
    public void SetLayoutChanged()
    {
      if (LayoutChanged == false)
      {
        LayoutChanged = true;
        if (_parent != null)
        {
          if (_parent.TryGetTarget(out var par))
          {
            par.SetLayoutChanged();
          }
        }
        //Unfortunately layout changes must take place in siblings and children as well.
        //Basically the entire UI if a STATIC element changes.
        //If a child has size:expand, layout changes won't take place for just parents.
        //However, relative elements (specifically x or y) would not I think.
        if (_children != null)
        {
          foreach (var c in _children)
          {
            c.Value.SetLayoutChanged();
          }
        }
      }
    }
    public void DoMouseEvents(PCMouse mouse, Gui2d g, bool iswindow = false)
    {
      if (_bPickedThisFrame || iswindow)
      {
        if (_bPickedPreviousFrame || iswindow)
        {
          DoEvents(g, UiEventId.Mouse_Move, mouse);
        }
        else
        {
          DoEvents(g, UiEventId.Mouse_Enter, mouse);
        }

        ButtonState eLmb = mouse.State(MouseButton.Left);
        ButtonState eRmb = mouse.State(MouseButton.Right);
        ButtonState eMmb = mouse.State(MouseButton.Middle);

        DoMouseButtonEvent(g, mouse, UiEventId.Mouse_Lmb_Up, eLmb, ButtonState.Up);
        DoMouseButtonEvent(g, mouse, UiEventId.Mouse_Lmb_Hold, eLmb, ButtonState.Hold);
        DoMouseButtonEvent(g, mouse, UiEventId.Mouse_Lmb_Press, eLmb, ButtonState.Press);
        DoMouseButtonEvent(g, mouse, UiEventId.Mouse_Lmb_Release, eLmb, ButtonState.Release);

        DoMouseButtonEvent(g, mouse, UiEventId.Mouse_Rmb_Up, eRmb, ButtonState.Up);
        DoMouseButtonEvent(g, mouse, UiEventId.Mouse_Rmb_Hold, eRmb, ButtonState.Hold);
        DoMouseButtonEvent(g, mouse, UiEventId.Mouse_Rmb_Press, eRmb, ButtonState.Press);
        DoMouseButtonEvent(g, mouse, UiEventId.Mouse_Rmb_Release, eRmb, ButtonState.Release);
      }
      else if (_bPickedPreviousFrame)
      {
        DoEvents(g, UiEventId.Mouse_Leave, mouse);
        _bPickedPreviousFrame = false;
      }
    }

    #endregion
    #region Private and Protected: Methods

    private void init(List<string> styleClasses, string name, string? phrase = null, List<UiElement> children = null)
    {
      this.Style.SetInheritStyles(styleClasses);
      _name = name;
      if (phrase != null)
      {
        Text = phrase;
      }
      if (children != null)
      {
        foreach (var c in children)
        {
          AddChild(c);
        }
      }
    }
    private bool IsFullyClipped(Box2f b2ClipRect)
    {
      Box2f boundsSS = GetScreenSpaceClipQuad();
      // This simple test saves us a ton of GPU pixel tests
      if (boundsSS._max.x < b2ClipRect._min.x)
      {
        return true;
      }
      if (boundsSS._max.y < b2ClipRect._min.y)
      {
        return true;
      }
      if (boundsSS._min.x > b2ClipRect._max.x)
      {
        return true;
      }
      if (boundsSS._min.y > b2ClipRect._max.y)
      {
        return true;
      }
      return false;
    }
    protected virtual void RegenMesh(List<v_v4v4v4v2u2v4v4> verts, MtTex defaultPixel, Box2f b2ClipRect, uint rootPickId, UiDebugDraw dd)
    {
      if (LayoutVisible)
      {
        if (RenderVisible)
        {
          //Set child element pick Ids equal to the parent element so it is picked as one element,
          // or, if parent is pickable, change the pick ID
          uint pickId = rootPickId;
          if (_pickEnabled)
          {
            pickId = _iPickId;
          }

          GetOpenGLQuadVerts(verts, b2ClipRect, defaultPixel, pickId, dd);
          var savedcolor = dd.OverlayColor;
          var t = dd.OverlayColor.x; //flipflop color for sub-elements
          dd.OverlayColor.x = dd.OverlayColor.y;
          dd.OverlayColor.y = dd.OverlayColor.z;
          dd.OverlayColor.z = t;
          if (_children != null)
          {
            foreach (var p in _children)
            {
              UiElement ele = p.Value;

              if (ele.IsFullyClipped(b2ClipRect) == false)
              {
                Box2f clip = ShrinkClipRect(b2ClipRect);
                ele.RegenMesh(verts, defaultPixel, clip, pickId, dd);
              }
            }
          }
          dd.OverlayColor = savedcolor;
        }
      }
    }
    private Box2f ShrinkClipRect(Box2f b2ClipRect)
    {
      // Hide all children that go beyond this container.
      // Must be called in the loop so we reset it with every child.
      Box2f b = GetScreenSpaceClipQuad();
      Box2f ret = b2ClipRect;
      if (Style._props.OverflowMode == UiOverflowMode.Hide)
      {
        // Shrink the box
        if (b._min.x > ret._min.x)
        {
          ret._min.x = b._min.x;
        }
        if (b._min.y > ret._min.y)
        {
          ret._min.y = b._min.y;
        }
        if (b._max.x < ret._max.x)
        {
          ret._max.x = b._max.x;
        }
        if (b._max.y < ret._max.y)
        {
          ret._max.y = b._max.y;
        }
        // Make sure it's valid
        if (ret._min.x > ret._max.x)
        {
          ret._min.x = ret._max.x;
        }
        if (ret._max.y < ret._min.y)
        {
          ret._max.y = ret._min.y;
        }
      }
      return ret;
    }
    private void ComputeVertexTexcoord(ref v_v4v4v4v2u2v4v4 vc, MtTex pTex, UiImageTiling xtile, UiImageTiling ytile, float pixAdjust)
    {
      Box2f q2Tex = new Box2f();
      Gu.Assert(pTex != null);

      if (xtile == UiImageTiling.Expand)
      {
        q2Tex._min.x = pTex.uv0.x;
        q2Tex._max.x = pTex.uv1.x;
      }
      else if (xtile == UiImageTiling.Tile)
      {
        float wPx = Style._props.Width;
        q2Tex._min.x = pTex.uv0.x;
        q2Tex._max.x = pTex.uv1.x + (pTex.uv1.x - pTex.uv0.x) * _tileScale.x;
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
        q2Tex._min.y = pTex.uv0.y;
        q2Tex._max.y = pTex.uv1.y;
      }
      else if (ytile == UiImageTiling.Tile)
      {
        float hPx = Style._props.Height;
        q2Tex._min.y = pTex.uv0.y;
        q2Tex._max.y = pTex.uv1.y + (pTex.uv1.y - pTex.uv0.y) * _tileScale.y;
      }
      else if (ytile == UiImageTiling.Computed)
      {
        // Tex coords are computed (used for UiGlyph)
      }
      else if (ytile == UiImageTiling.Proportion)
      {
        // proportion the Y to the X
        q2Tex._min.y = pTex.uv1.y;
        float fw = q2Tex._max.x - q2Tex._min.x;
        float fr = pTex.GetSizeRatio();
        float fh = fw * fr;
        q2Tex._max.y = q2Tex._min.y + fh;
      }
      else
      {
        Gu.Log.Error("Invalid layout size mode.");
      }

      vc._tex.x = q2Tex._min.x;  // GL - bottom left
      vc._tex.y = q2Tex._min.y;
      vc._tex.z = q2Tex._max.x;  // GL - top right *this essentially flips it upside down
      vc._tex.w = q2Tex._max.y;
      vc._texsiz.x = Math.Abs(pTex.uv1.x - pTex.uv0.x);
      vc._texsiz.y = Math.Abs(pTex.uv1.y - pTex.uv0.y);  // Uv0 - uv1 - because we flipped coords bove

      float w1px = 0;                  // 1 pixel subtract from the u/v to prevent creases during texture modulation
      float h1px = 0;

      if (pTex.GetWidth() > 0 && vc._texsiz.x > 0)
      {
        w1px = 1.0f / pTex.GetWidth();
        w1px *= vc._texsiz.x;
        w1px *= pixAdjust;
      }
      if (pTex.GetHeight() > 0 && vc._texsiz.y > 0)
      {
        h1px = 1.0f / pTex.GetHeight();
        h1px *= vc._texsiz.y;
        h1px *= pixAdjust;
      }
      vc._texsiz.x -= w1px * 2.0f;
      vc._texsiz.y -= h1px * 2.0f;
      vc._tex.x += w1px;
      vc._tex.y += h1px;
      vc._tex.z -= w1px;
      vc._tex.w -= h1px;
    }
    private void ComputeVertexGlyph(ref v_v4v4v4v2u2v4v4 vc, MtCachedCharData? glyph, float pixAdjust)
    {
      Gu.Assert(glyph != null);
      Box2f q2Tex = new Box2f();
      q2Tex._min.x = glyph.uv0.x;
      q2Tex._max.x = glyph.uv1.x;

      //exact y
      q2Tex._min.y = glyph.uv0.y;
      q2Tex._max.y = glyph.uv1.y;

      ///proportion y .. not sure if this is what glyph needs.
      // q2Tex._min.y = glyph.Value.uv1.y;
      // float fw = q2Tex._max.x - q2Tex._min.x;
      // float fr = glyph.Value.GetSizeRatio();
      // float fh = fw * fr;
      // q2Tex._max.y = q2Tex._min.y + fh;

      vc._tex.x = q2Tex._min.x;  // GL - bottom left
      vc._tex.y = q2Tex._min.y;
      vc._tex.z = q2Tex._max.x;  // GL - top right *this essentially flips it upside down
      vc._tex.w = q2Tex._max.y;
      vc._texsiz.x = Math.Abs(glyph.uv1.x - glyph.uv0.x);
      vc._texsiz.y = Math.Abs(glyph.uv1.y - glyph.uv0.y);  // Uv0 - uv1 - because we flipped coords bove

      float w1px = 0;
      float h1px = 0;
      if (glyph.patchTexture_Width > 0 && vc._texsiz.x > 0)
      {
        w1px = 1.0f / glyph.patchTexture_Width;
        w1px *= vc._texsiz.x;
        w1px *= pixAdjust;
      }
      if (glyph.patchTexture_Width > 0 && vc._texsiz.y > 0)
      {
        h1px = 1.0f / glyph.patchTexture_Width;
        h1px *= vc._texsiz.y;
        h1px *= pixAdjust;
      }
      vc._texsiz.x -= w1px * 2.0f;
      vc._texsiz.y -= h1px * 2.0f;
      vc._tex.x += w1px;
      vc._tex.y += h1px;
      vc._tex.z -= w1px;
      vc._tex.w -= h1px;
    }
    private void SetVertexRasterArea(ref v_v4v4v4v2u2v4v4 vc, in Box2f rasterQuad, in Box2f b2ClipRect, UiDebugDraw dd)
    {
      //BL = min TR = max
      vc._rect.x = rasterQuad._min.x;
      vc._rect.y = rasterQuad._min.y;
      vc._rect.z = rasterQuad._max.x;
      vc._rect.w = rasterQuad._max.y;

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
        vc._clip.x = b2ClipRect._min.x;
        vc._clip.y = b2ClipRect._min.y;
        vc._clip.z = b2ClipRect._max.x;
        vc._clip.w = b2ClipRect._max.y;
      }

    }
    private void GetOpenGLQuadVerts(List<v_v4v4v4v2u2v4v4> verts, Box2f b2ClipRect, MtTex defaultPixel, uint rootPickId, UiDebugDraw dd)
    {
      if (LayoutVisible == false)
      {
        return;
      }
      if ((Style._props.Texture == null) && (_cachedGlyph == null))
      {
        //invisible, or container element
        return;
      }

      //**Texture Adjust - modulating repeated textures causes seaming issues, especially with texture filtering
      // adjust the texture coordinates by some pixels to account for that.  0.5f seems to work well.
      float pixAdjust = 0.0f;  // # of pixels to adjust texture by

      if (_borderArea != null)
      {
        v_v4v4v4v2u2v4v4 vb = new v_v4v4v4v2u2v4v4();
        vb._rtl_rtr = new vec4(_borderArea._rtl, _borderArea._rtr);
        vb._rbr_rbl = new vec4(_borderArea._rbr, _borderArea._rbl);
        SetVertexRasterArea(ref vb, in _borderArea._b2RasterQuad, in b2ClipRect, dd);
        ComputeVertexTexcoord(ref vb, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, pixAdjust);
        SetVertexPickAndColor(ref vb, Style._props.BorderColor, rootPickId);
        verts.Add(vb);
      }
      {
        v_v4v4v4v2u2v4v4 vc = new v_v4v4v4v2u2v4v4();
        vc._rtl_rtr = new vec4(_contentArea._rtl, _contentArea._rtr);
        vc._rbr_rbl = new vec4(_contentArea._rbr, _contentArea._rbl);
        SetVertexRasterArea(ref vc, in _contentArea._b2RasterQuad, in b2ClipRect, dd);
        if (_cachedGlyph != null)
        {
          ComputeVertexGlyph(ref vc, _cachedGlyph, pixAdjust);
        }
        else
        {
          ComputeVertexTexcoord(ref vc, Style._props.Texture, Style._props.ImageTilingX, Style._props.ImageTilingY, pixAdjust);
        }
        SetVertexPickAndColor(ref vc, Style._props.Color, rootPickId);
        verts.Add(vc);
      }
      //**DEBUG OVERLAY (can mostly be ignored)** 
      if (dd.ShowOverlay)
      {
        //overlay colored quad.

        v_v4v4v4v2u2v4v4 dbgv;
        if (_renderOffset != null)
        {
          //Draw the area pre-offset.
          dbgv = new v_v4v4v4v2u2v4v4();
          if (_borderArea != null)
          {
            SetVertexRasterArea(ref dbgv, in _borderArea._b2PreOffsetRasterQuad, in b2ClipRect, dd);
          }
          else
          {
            SetVertexRasterArea(ref dbgv, in _contentArea._b2PreOffsetRasterQuad, in b2ClipRect, dd);
          }
          dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
          dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
          ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, pixAdjust);
          SetVertexPickAndColor(ref dbgv, dd.OverlayColor, rootPickId);
          verts.Add(dbgv);
        }

        dbgv = new v_v4v4v4v2u2v4v4();
        if (_borderArea != null)
        {
          SetVertexRasterArea(ref dbgv, in _borderArea._b2RasterQuad, in b2ClipRect, dd);
        }
        else
        {
          SetVertexRasterArea(ref dbgv, in _contentArea._b2RasterQuad, in b2ClipRect, dd);
        }
        dbgv._rtl_rtr = new vec4(0, 0, 0, 0);
        dbgv._rbr_rbl = new vec4(0, 0, 0, 0);
        ComputeVertexTexcoord(ref dbgv, defaultPixel, UiImageTiling.Expand, UiImageTiling.Expand, pixAdjust);
        SetVertexPickAndColor(ref dbgv, dd.OverlayColor, rootPickId);
        verts.Add(dbgv);
      }
    }
    private void SetVertexPickAndColor(ref v_v4v4v4v2u2v4v4 vc, vec4 color, uint rootPickId)
    {
      vc._pick_color = new uvec2(
         //Since we can't discard fragments for the pick buffer, assume the pick id of the parent pick root
         rootPickId,
          ((uint)(color.x * 255.0f) << 24) |
          ((uint)(color.y * 255.0f) << 16) |
          ((uint)(color.z * 255.0f) << 8) |
          ((uint)(color.w * 255.0f) << 0)
      );
    }
    private Box2f GetPickableQuad()
    {
      if (_borderArea != null)
      {
        return _borderArea._b2LayoutQuad;
      }
      return _contentArea._b2LayoutQuad;
    }
    protected virtual bool Pick(PCMouse mous, long frameStamp)
    {
      bool picked = false;
      Box2f q = GetPickableQuad();
      if (LayoutVisible == true && RenderVisible == true)
      {
        if (q.ContainsPointInclusive(mous.Pos))
        {
          if (_children != null)
          {
            //Avoiding picking children was essentially the reason behind PickRoot, however 
            // Automatic picking with MouseEvents now has us doing it this way. It is easier for usability.
            // The other option is to have a big std::map of pickable UIE's. This is probably the way to go, considering picking is exact.
            // the only reason for doing it this way, is to prevent having to add std::map to Gui2d. So we offload the dependnecy.
            foreach (var ele in _children)
            {
              if (ele.Value.Pick(mous, frameStamp))
              {
                //This is ok to skip other children. We use a pixel-precise pick buffer, not just rectangle.
                picked = true;
                break;
              }
              else
              {
                //child wasn't picked, try parent.
                var pixid = Gu.Context.Renderer.Picker.SelectedPixelId;
                if (pixid != Picker.c_iInvalidPickId)
                {
                  if (pixid == _iPickId)
                  {
                    _iPickedFrameId = frameStamp;
                    _bPickedThisFrame = true;
                    picked = true;
                    Gu.Context.Renderer.Picker.PickedObjectFrame = this;
                  }
                }
              }
            }
          }

        }
      }
      return picked;
    }
    private void DoMouseButtonEvent(Gui2d g, PCMouse m, UiEventId evid, ButtonState curstate, ButtonState evstate)
    {
      if (curstate == evstate)
      {
        DoEvents(g, evid, m);
      }
    }
    private void DoEvents(Gui2d g, UiEventId evid, PCMouse m)
    {
      if (Events.TryGetValue(evid, out var s_actions))
      {
        foreach (var act in s_actions)
        {
          act.Value(evid, this, m, g);
        }
      }
    }
    protected virtual void BeforeLayout_SizeElements()
    {
      //override
    }
    protected virtual void PerformLayout_SizeElements(MegaTex mt, bool bForce, vec2 parentMaxWH, UiStyle parent, UiStyleSheet sheet, long framesatmp)
    {
      //Build the UI depth-first. Children elements are sized. Then we got hrough again and position them from the top.
      if (LayoutChanged || bForce)
      {
        BeforeLayout_SizeElements();
        Style.CompileStyleTree(sheet, framesatmp, parent);
        UpdateBorder();

        //in HTML all elements default to zero width without any contents.
        vec2 contentWH = new vec2(Style._props.PadLeft + Style._props.PadRight, Style._props.PadTop + Style._props.PadBot);

        vec2 maxWH = new vec2(
          Math.Max(Math.Min(parentMaxWH.x, Style._props.MaxWidth), 0),
          Math.Max(Math.Min(parentMaxWH.y, Style._props.MaxHeight), 0)
        );

        //textchanged .. font color .. font size .. etc.
        if (_bTextChanged || _bMustRedoTextBecauseOfStyle)
        {
          CreateGlyphs(mt, _bTextChanged && !_bMustRedoTextBecauseOfStyle);
          _bMustRedoTextBecauseOfStyle = false;
          _bTextChanged = false;
        }

        //Do Children.
        if (_children != null)
        {
          foreach (var p in _children)
          {
            UiElement ele = p.Value;
            if (ele.LayoutVisible)
            {
              ele.PerformLayout_SizeElements(mt, bForce, maxWH, this.Style, sheet, framesatmp);
            }
          }

          LayoutChildren(bForce, maxWH, ref contentWH);
        }

        SizeElement(maxWH, contentWH, parent);
      }
    }
    protected void PerformLayout_PositionElements(bool bForce)
    {
      //Position elements after size calculated
      if (LayoutChanged || bForce)
      {
        if (_children != null)
        {
          foreach (var p in _children)
          {
            UiElement ele = p.Value;
            if (ele.LayoutVisible)
            {
              LayoutEleQuads(ele);
              ele.PerformLayout_PositionElements(bForce);
            }
          }
        }
        LayoutChanged = false;
      }
    }
    private void UpdateBorder()
    {
      //Check if border changed
      if (_borderArea == null)
      {
        if (Style._props.BorderTop > 0 || Style._props.BorderRight > 0 || Style._props.BorderBot > 0 || Style._props.BorderLeft > 0)
        {
          _borderArea = new Quads();
        }
      }
      else
      {
        if (Style._props.BorderTop == 0 && Style._props.BorderRight == 0 && Style._props.BorderBot == 0 && Style._props.BorderLeft == 0)
        {
          _borderArea = null;
        }
      }
    }
    private void SizeElement(vec2 maxWH, vec2 contentWH, UiStyle parent)
    {
      //Shrink the element (if not UiScreen) This is how HTML works by default.
      //Also apply min/max to the element. This is specifically how you would fix an element's size.
      //Note: content quad may expand beyond container
      _b2ContentQuad._min = new vec2(Style._props.Left, Style._props.Top);
      _b2ContentQuad._max = _b2ContentQuad._min + contentWH;

      if (Style._props.SizeModeWidth == UiSizeMode.Shrink)
      {
        Style._props.Width = Math.Min(Math.Max(Style._props.MinWidth, Math.Min(maxWH.x, contentWH.x)), Style._props.MaxWidth);
      }
      else if (Style._props.SizeModeWidth == UiSizeMode.Expand)
      {
        if (_parent != null && _parent.TryGetTarget(out var par))
        {
          Style._props.Width = parent._props.Width;// - _props._left;
        }
        else
        {
          //No parent - Fixed. Only possible for (root) element
        }
      }
      else if (Style._props.SizeModeWidth == UiSizeMode.Fixed)
      {
        //Fixed
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      if (Style._props.SizeModeHeight == UiSizeMode.Shrink)
      {
        Style._props.Height = Math.Min(Math.Max(Style._props.MinHeight, Math.Min(maxWH.y, contentWH.y)), Style._props.MaxHeight);
      }
      else if (Style._props.SizeModeHeight == UiSizeMode.Expand)
      {
        if (_parent != null && _parent.TryGetTarget(out var par))
        {
          Style._props.Height = parent._props.Height;
        }
        else
        {
          //No parent - Fixed. Only possible for (root) element
        }
      }
      else if (Style._props.SizeModeHeight == UiSizeMode.Fixed)
      {
        //Fixed
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      if (Style._props.Width < 0 || Style._props.Height < 0)
      {
        int n = 0;
        n++;
      }

    }
    protected void LayoutChildren(bool bForce, vec2 maxWH, ref vec2 contentWH)
    {
      //Layout each layer specified by the key in the Children multimap.
      //Each new layer will be a new layout set (bucket).
      //Layout static elements sequentially left to right,
      //or, layout relative elements for absolute position.
      if (_children != null && _children.Count > 0)
      {
        int uiLast = int.MaxValue;
        List<UiElement> bucket = new List<UiElement>();
        foreach (var p in _children)
        {
          var ele = p.Value;

          if (ele.LayoutVisible)
          {

            if (ele.Style._props.PositionMode == UiPositionMode.Static)
            {
              // Static elements - Have a Flow, and computed position
              if (p.Key != uiLast)
              {
                uiLast = p.Key;
                if (bucket.Count > 0)
                {
                  LayoutLayer(bucket, maxWH, ref contentWH);
                  bucket.Clear();
                }
              }
              bucket.Add(p.Value);
            }
            else if (
              ele.Style._props.PositionMode == UiPositionMode.Absolute ||
              ele.Style._props.PositionMode == UiPositionMode.Relative ||
            ele.Style._props.PositionMode == UiPositionMode.RelativeConstrainX ||
            ele.Style._props.PositionMode == UiPositionMode.RelativeConstrainY ||
            ele.Style._props.PositionMode == UiPositionMode.RelativeConstrainXY)
            {
              // Fixed elements relative to container
              ComputePositionalElement(ele,
              ele.Style._props.PositionMode == UiPositionMode.RelativeConstrainX || ele.Style._props.PositionMode == UiPositionMode.RelativeConstrainXY,
              ele.Style._props.PositionMode == UiPositionMode.RelativeConstrainY || ele.Style._props.PositionMode == UiPositionMode.RelativeConstrainXY);

              //contentWH.x = Math.Max(contentWH.x, ele.Style.Left.Value + ele.Style.Width.Value + Style._props.PadLeft + Style._props.PadRight);
              //contentWH.y = Math.Max(contentWH.y, ele.Style.Top.Value + ele.Style.Height.Value + Style._props.PadTop + Style._props.PadBot);
            }
            else
            {
              Gu.BRThrowNotImplementedException();
            }
            //Absolute - relative to entire document.
          }
        }
        //Position final static layer
        if (bucket.Count > 0)
        {
          LayoutLayer(bucket, maxWH, ref contentWH);
          bucket.Clear();
        }
      }
    }
    private void LayoutLayer(List<UiElement> stats, vec2 maxWH, ref vec2 contentWH)
    {
      // Calc width with all static blocks using 0 width for autos (expandable blocks).
      List<UiLine> vecLines = new List<UiLine>();
      vecLines.Add(new UiLine(Style._props.PadLeft, Style._props.PadTop));
      foreach (var ele in stats)
      {
        CalcStaticElement(ele, vecLines, 0.0f, 0.0f, maxWH);
      }
      float totalHeight = Style._props.PadTop + Style._props.PadBot;
      foreach (var line in vecLines)
      {
        totalHeight += line._height;
        contentWH.x = Math.Max(contentWH.x, line._width + Style._props.PadLeft + Style._props.PadRight);
      }
      contentWH.y = Math.Max(contentWH.y, totalHeight);
    }
    private void CalcStaticElement(UiElement ele, List<UiLine> vecLines, float fAutoWidth, float fAutoHeight, vec2 parentMaxWH)
    {
      /*
         .. | margin | border | padding | ** | pad | bor | mar| .. 
      */
      if (vecLines.Count == 0)
      {
        Gu.BRThrowException("GUI error - tried to run calc algorithm without any UILines created");
      }
      UiLine line = vecLines[vecLines.Count - 1];

      float parent_contentarea_width = parentMaxWH.x - Style._props.PadLeft - Style._props.PadRight;

      //*Padding
      float mt = ele.Style._props.MarginTop;
      float mr = ele.Style._props.MarginRight;
      float mb = ele.Style._props.MarginBot;
      float ml = ele.Style._props.MarginLeft;

      float bt = ele.Style._props.BorderTop;
      float br = ele.Style._props.BorderRight;
      float bb = ele.Style._props.BorderBot;
      float bl = ele.Style._props.BorderLeft;

      float ele_width = Math.Max(ele.Style._props.Width, ele.Style._props.MinWidth);

      //**Line break
      bool bLineBreak = false;
      if (ele.Style._props.DisplayMode == UiDisplayMode.Inline)
      {
        if (ml + mr + bl + br + ele_width + line._width > parent_contentarea_width) //For label - auto width + expand. ?? 
        {
          bLineBreak = true;
        }
      }
      else if (ele.Style._props.DisplayMode == UiDisplayMode.Block)
      {
        //For /n in text. or block elements
        bLineBreak = true;
      }
      else if (ele.Style._props.DisplayMode != UiDisplayMode.InlineNoWrap)
      {
        bLineBreak = false;
      }

      if (bLineBreak)
      {
        // new line
        UiLine line2 = new UiLine(Style._props.PadLeft, 0/*pad top, only for the top uiline*/);
        line2._top = line._top + line._height;
        vecLines.Add(line2);
        line = vecLines[vecLines.Count - 1];
      }

      line._width += ml;
      line._width += bl;
      ele.Style._props.Left = line._left + line._width;
      ele.Style._props.Top = line._top + mt + bt;
      line._width += ele_width;
      line._width += br;
      line._width += mr;

      ele.ValidateQuad();

      // Increse line height WITH PAD
      line._height = Math.Max(line._height, Math.Max(ele.Style._props.Height + mt + mb + bt + bb, ele.Style._props.MinHeight));

      line._eles.Add(ele);
    }
    private void ComputePositionalElement(UiElement ele, bool constrainX, bool constrainY)
    {
      if (constrainX)
      {
        if (ele.Style._props.Right > Style._props.Width)
        {
          ele.Style._props.Left = Style._props.Width - ele.Style._props.Width;
        }
        if (ele.Style._props.Left < 0)
        {
          ele.Style._props.Left = 0;
        }
      }
      if (constrainY)
      {
        if (ele.Style._props.Bottom > Style._props.Height)
        {
          ele.Style._props.Top = Style._props.Height - ele.Style._props.Height;
        }
        if (ele.Style._props.Top < 0)
        {
          ele.Style._props.Top = 0;
        }
      }

      ValidateQuad();
    }
    private void LayoutEleQuads(UiElement ele)
    {
      //Add the child to the parent.
      float t, r, b, l;
      float bt, br, bb, bl;
      float rtl, rtr, rbr, rbl;
      t = _contentArea._b2ComputedQuad._min.y + ele.Style._props.Top;
      r = _contentArea._b2ComputedQuad._min.x + ele.Style._props.Right;
      b = _contentArea._b2ComputedQuad._min.y + ele.Style._props.Bottom;
      l = _contentArea._b2ComputedQuad._min.x + ele.Style._props.Left;
      bt = ele.Style._props.BorderTop;
      br = ele.Style._props.BorderRight;
      bb = ele.Style._props.BorderBot;
      bl = ele.Style._props.BorderLeft;
      rtl = ele.Style._props.BorderTopLeftRadius;
      rtr = ele.Style._props.BorderTopRightRadius;
      rbr = ele.Style._props.BorderBotRightRadius;
      rbl = ele.Style._props.BorderBotLeftRadius;

      ComputeQuads(t + bt, r - br, b - bb, l + bl, rtl, rtr, rbr, rbl, ele._renderOffset, ele._contentArea);

      if (ele._borderArea != null)
      {
        ComputeQuads(t, r, b, l, rtl, rtr, rbr, rbl, ele._renderOffset, ele._borderArea);
      }
    }
    protected static void ComputeQuads(float top, float right, float bot, float left, float rtl, float rtr, float rbr, float rbl, Box2f? offset, Quads quads)
    {
      // Layout Quad (for picking, debug)/*  */
      float w1 = 1.0f, h1 = 1.0f;
      w1 = 1;//UiScreen::getDesignMultiplierW();
      h1 = 1;//UiScreen::getDesignMultiplierH();

      if (left > right)
      {
        Gu.DebugBreak();
        left = right;
      }
      if (top > bot)
      {
        Gu.DebugBreak();
        top = bot;
      }

      quads._b2ComputedQuad._min.y = top;
      quads._b2ComputedQuad._max.x = right;
      quads._b2ComputedQuad._max.y = bot;
      quads._b2ComputedQuad._min.x = left;

      if (offset != null)
      {
        //For glyphs, and other elements that go outside their physical regions
        var origin = quads._b2ComputedQuad.Center();
        var ro = offset.Value;
        quads._b2OffsetQuad._min.x = origin.x + ro.Left - quads._b2ComputedQuad.Width() / 2;
        quads._b2OffsetQuad._min.y = origin.y + ro.Top + quads._b2ComputedQuad.Height() / 4;
        quads._b2OffsetQuad._max.x = origin.x + ro.Right - quads._b2ComputedQuad.Width() / 2;
        quads._b2OffsetQuad._max.y = origin.y + ro.Bottom + quads._b2ComputedQuad.Height() / 4;
      }
      else
      {
        quads._b2OffsetQuad = quads._b2ComputedQuad;
      }

      // Set to false if we're controllig coordinates of this element (cursor, or window position)
      quads._b2LayoutQuad._min.x = quads._b2OffsetQuad._min.x * w1;
      quads._b2LayoutQuad._min.y = quads._b2OffsetQuad._min.y * h1;
      quads._b2LayoutQuad._max.x = quads._b2OffsetQuad._max.x * w1;
      quads._b2LayoutQuad._max.y = quads._b2OffsetQuad._max.y * h1;

      ComputeRasterQuad(quads._b2LayoutQuad, ref quads._b2RasterQuad);
      if (offset != null)
      {
        ComputeRasterQuad(quads._b2ComputedQuad, ref quads._b2PreOffsetRasterQuad);
      }
      quads._rtl = rtl;// new vec2(rtl / (float)viewport_wh.x * 0.5f, rtl / (float)viewport_wh.y * 0.5f);
      quads._rtr = rtr;// new vec2(rtr / (float)viewport_wh.x * 0.5f, rtr / (float)viewport_wh.y * 0.5f);
      quads._rbr = rbr;// new vec2(rbr / (float)viewport_wh.x * 0.5f, rbr / (float)viewport_wh.y * 0.5f);
      quads._rbl = rbl;// new vec2(rbl / (float)viewport_wh.x * 0.5f, rbl / (float)viewport_wh.y * 0.5f);
    }
    private static void ComputeRasterQuad(Box2f _b2LayoutQuad, ref Box2f _b2RasterQuad)
    {
      //TODO: remove raster, use layout
      _b2RasterQuad = _b2LayoutQuad;
    }
    private void CreateGlyphs(MegaTex mt, bool replaceChangedGlyphs)
    {
      //textOnly If just text changed then just create new chars
      //style - if style changed we must redo everything.
      if (_children == null)
      {
        _children = new MultiMap<int, UiElement>();
      }

      //Props must never be null.
      Gu.Assert(Style._props.FontFace != null);

      //Get the font if it isn't already got.
      MtFontLoader font = null;
      if (_cachedFont == null || mt.GetFont(Style._props.FontFace) != _cachedFont)
      {
        font = mt.GetFont(Style._props.FontFace);
      }
      else
      {
        font = _cachedFont;
      }
      if (font == null)
      {
        Gu.Log.ErrorCycle("Font loader could not be found for " + Style._props.FontFace.QualifiedPath + " font possibly loaded with error", 500);
        return;
      }

      float fontHeight = Style._props.FontSize;
      var patch = font.SelectFontPatchInfo(Gu.Translator.LanguageCode, fontHeight);
      if (patch == null)
      {
        return;
      }
      if (replaceChangedGlyphs)
      {
        //Try to "smart" replace only changed text. 
        var glyphs = _children.ItemsAt(c_GlyphLayerSort);
        if (glyphs == null)
        {
          replaceChangedGlyphs = false;
        }
        else if (_strTextLast.Length != glyphs.Count)
        {
          //Something messed up between text/glyphs, redo everything, technically this should never happen, hence debugberak
          replaceChangedGlyphs = false;
          _children.Remove(c_GlyphLayerSort);
          Gu.DebugBreak();
        }
        else
        {
          //Sliding Diff algorithm to create a subset of glyphs.
          int ilast = 0;
          int icur = 0;
          List<UiElement> newChildren = new List<UiElement>();
          //We could cache diff in a static variable if its size becomes a problem.
          int[] diff = StringUtil.SlidingDiff(_strTextLast, _strText, Gui2d.SlidingDiffWindow);
          int debug_numcreated = 0;
          for (int di = 0; di < diff.Length; di += 2)
          {
            int ct = diff[di + 1];
            int code = diff[di + 0];
            if (code == 0) // no change 
            {
              for (int cti = 0; cti < ct; cti++)
              {
                var e = glyphs[ilast + cti];
                e.LayoutChanged = true;
                newChildren.Add(e);
              }
              ilast += ct;
              icur += ct;
            }
            else if (code == 1)//add new
            {
              for (int cti = 0; cti < ct; cti++)
              {
                //TODO: this should be UiElementBase, for simplicity. UiElement is too huge.
                UiElement e = new UiElement();
                DoGlyph(e, icur + cti, _strText, font, patch, fontHeight);
                newChildren.Add(e);
                debug_numcreated++;
              }
              icur += ct;
            }
            else if (code == 2) //remove
            {
              ilast += ct;
            }
          }
          _children.SetValueList(c_GlyphLayerSort, newChildren);

        }
      }
      if (replaceChangedGlyphs == false)
      {
        _children.Remove(c_GlyphLayerSort);
        int debug_redocount = 0;
        for (int ci = 0; ci < _strText.Length; ci++)
        {
          UiElement e = new UiElement();

          DoGlyph(e, ci, _strText, font, patch, fontHeight);

          _children.Add(c_GlyphLayerSort, e);

          debug_redocount++;
        }
      }
    }
    private void DoGlyph(UiElement e, int index, string text, MtFontLoader font, MtFontPatchInfo patch, float fontHeight)
    {
      int cc = _strText[index];
      int ccNext = (index + 1) < _strText.Length ? _strText[index + 1] : 0;
      float adv = font.GetKernAdvanceWidth(patch, Style._props.FontSize, cc, ccNext);
      if (adv != 0)
      {
        int n = 0;
        n++;
      }

      float sca = 0;
      patch.GetChar(cc, fontHeight, out e._cachedGlyph, out sca);

      float gtop = 0, gright = 0, gbot = 0, gleft = 0, gwidth = 0, gheight = 0;
      e._cachedGlyph.ApplyScaling(sca, out gtop, out gright, out gbot, out gleft, out gwidth, out gheight);

      e._pickEnabled = false;
      e._renderOffset = new Box2f(new vec2(gleft, gtop), new vec2(gright, gbot));
      e.Style.IsPropsOnly = true;
      e.Style._props.Left = 0;
      e.Style._props.Top = 0;
      e.Style._props.MinWidth = gwidth;
      e.Style._props.MinHeight = gheight * Style._props.LineHeight;
      e.Style._props.MarginRight = e._cachedGlyph.marginRight + adv;
      e.Style._props.PositionMode = UiPositionMode.Static;
      e.Style._props.SizeModeHeight = UiSizeMode.Shrink;
      e.Style._props.SizeModeWidth = UiSizeMode.Shrink;

      if (cc == '\n')
      {
        e.Style._props.DisplayMode = UiDisplayMode.Block;
      }
      else
      {
        e.Style._props.DisplayMode = UiDisplayMode.Inline;
      }
      e.Style._props.Color = Style._props.FontColor;

      e.ValidateQuad();
    }
    protected Box2f GetScreenSpaceClipQuad()
    {
      if (_borderArea != null)
      {
        return _borderArea._b2RasterQuad;
      }
      return _contentArea._b2RasterQuad;
    }
    public void AddEvent(UiEventId evId, UiAction f, string tag = "")
    {
      // Layman's: For each Mouse Button State we have a list of functions that get called when it is active (like clicked)
      _events = _events.ConstructIfNeeded();

      if (evId.ToString().Contains("Mouse"))
      {
        //has mouse events = pick enabled.
        _pickEnabled = true;
        _iPickId = Gu.Context.Renderer.Picker.GenPickId();
      }

      MultiMap<string, UiAction>? evList = null;
      if (!_events.TryGetValue(evId, out evList))
      {
        evList = new MultiMap<string, UiAction>();
        _events.Add(evId, evList);
      }
      evList.Add(tag, f);
    }
    public bool RemoveEvents(UiEventId evId, string tag)
    {
      Gu.Log.Warn("TODO: remove pick id if no mouse events");
      if (_events.TryGetValue(evId, out var evList))
      {
        return evList.Remove(tag);
      }
      return false;
    }


    #endregion

  }//UiElement
  public class StyleName
  {
    // Helpers .. these will go away when we do a css file.
    public const string Inline = "inline";
    public const string Base = "base";
    public const string Label = "label";
    public const string DebugLabel = "debuglabel";
    public const string Panel = "panel"; // a full width/height panel
    public const string Button = "button";
    public const string Toolbar = "toolbar";
  }
  public class UiStyleSheet
  {
    //* There is only one stylesheet, we must have a global namespace to make this owrk.
    //I keep syaing we dont need this.
    //optimally, we keep styles here, referencing elements and update it all at once for optimization.
    //this would be ideal, plus, it allows us to share styles across the application.

    // private enum LessCompileContext
    // {
    //   None,
    //   ClassName,
    //   ClassBody,
    // }
    // private enum LessVariableType { 
    //   String,
    //   Number,
    // }
    // private class ILessVariable { }
    // private class LessVariable <T> : ILessVariable
    // {
    //   public T Value;
    // }

    private FileLoc _location = null;
    private Dictionary<string, UiStyle> Styles = new Dictionary<string, UiStyle>();
    private List<string> _errors = new List<string>();
    public string Name { get; private set; } = Library.UnsetName;

    public UiStyleSheet(FileLoc loc)
    {
      _location = loc;
      Name = System.IO.Path.GetFileName(loc.RawPath) + "-stylesheet";
      LoadCSSFile();
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
    private void LoadCSSFile()
    {
      //Compile a CSS file 
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
  public class Gui2d : UiElement
  {
    //*note:GUI element sizes translated relative to the current FBO size in the shader, all gui coords are in window coords
    #region Public: Members
#if DEBUG
    private v_v4v4v4v2u2v4v4[] _debug_pt = new v_v4v4v4v2u2v4v4[3]; //save 3 points to see what they are (debug)
#endif

    public const int MaxSize = 9999999;
    public const int SlidingDiffWindow = 16;//16 chars for the string difference window. Replacement of a full float string.

    public WeakReference<RenderView> RenderView { get; private set; } = new WeakReference<RenderView>(null);
    public UiDebugDraw DebugDraw { get; set; } = new UiDebugDraw();
    // public UiDropdown ContextMenu { get; private set; } = null;
    public MeshData Mesh { get; set; } = null;
    public long UpdateMs { get; private set; } = 0;
    public long MeshMs { get; private set; } = 0;
    public long PickMs { get; private set; } = 0;
    public long ObjectEventsMs { get; private set; } = 0;
    public long WindowEventsMs { get; private set; } = 0;
    public MtTex DefaultPixel { get { return _shared.MegaTex.DefaultPixel; } }
    public UiStyleSheet StyleSheet { get; set; } = null;

    private UiDragInfo _dragInfo = new UiDragInfo();
    private vec2 _viewport_wh_last = new vec2(1, 1);
    private Gui2dShared _shared = null;

    #endregion
    #region Public: Methods

    public Gui2d(Gui2dShared shared, RenderView cam)
    {
      StyleSheet = new UiStyleSheet(new FileLoc("ui-default.css", FileStorage.Embedded));
      _shared = shared;
      RenderView = new WeakReference<RenderView>(cam);
      Name = "screen(root)";
      CreateWindowEvents();
    }
    public void OnResize()
    {
      //*note:GUI is translated to the current FBO size in the shader, all gui coords are in window coords
      SetLayoutChanged();
    }
    public void Render(RenderView rv)
    {
      //Swap out the mesh for this instance's mesh
      _shared.Dummy.Mesh = Mesh;
      DrawCall.Draw(Gu.World.WorldProps, rv, _shared.Dummy);
    }
    public void Update(double dt)//MegaTex mt, WindowContext ct, Gui2d g)
    {
      if (RenderView != null && RenderView.TryGetTarget(out var rv))
      {
        long a = Gu.Milliseconds();
        StyleSheet?.Update();
        SetExtentsToViewport(rv);
        UpdateLayout(_shared.MegaTex, Gu.Context.PCMouse, rv);
        this.UpdateMs = Gu.Milliseconds() - a;

        a = Gu.Milliseconds();
        RegenMesh(rv, _shared.MegaTex);
        this.MeshMs = Gu.Milliseconds() - a;

      }
    }
    public void Pick()
    {
      var ct = Gu.Context;
      //See WorldObject->Pick
      if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
      {
        return;
      }

      //Update picked state
      var picker = Gu.Context.Renderer.Picker;
      if (picker.PickedObjectFrameLast != null)
      {
        if (picker.PickedObjectFrameLast is UiElement)
        {
          (picker.PickedObjectFrameLast as UiElement).ResetPick();
        }
      }

      //Do Pick
      Pick(ct.PCMouse, ct.FrameStamp);

      //Do mouse events for picked UIE
      long a = Gu.Milliseconds();
      if (picker.PickedObjectFrameLast != null)
      {
        if (picker.PickedObjectFrameLast is UiElement)
        {
          (picker.PickedObjectFrameLast as UiElement).DoMouseEvents(ct.PCMouse, this);
        }
      }
      if (picker.PickedObjectFrameLast != picker.PickedObjectFrame)
      {
        if (picker.PickedObjectFrame != null)
        {
          if (picker.PickedObjectFrame is UiElement)
          {
            (picker.PickedObjectFrame as UiElement).DoMouseEvents(ct.PCMouse, this);
          }
        }
      }
      this.ObjectEventsMs = Gu.Milliseconds() - a;

      //Window events
      a = Gu.Milliseconds();
      DoMouseEvents(ct.PCMouse, this, true);
      this.WindowEventsMs = Gu.Milliseconds() - a;
    }

    #endregion
    #region Private: Methods

    private void UpdateLayout(MegaTex mt, PCMouse mouse, RenderView rv)
    {
      if (LayoutChanged)
      {
        bool force = false;
        vec2 viewport_wh = new vec2(rv.Viewport.Width, rv.Viewport.Height);
        if ((int)viewport_wh.x != (int)_viewport_wh_last.x || (int)viewport_wh.y != (int)_viewport_wh_last.y)
        {
          force = true;
          _viewport_wh_last = viewport_wh;
        }

        // Gui2d doesn't have a parent, so we have to compute the quads to create a valid clip region.
        ComputeQuads(Style._props.Top, Style._props.Right, Style._props.Bottom, Style._props.Left,
        Style._props.BorderTopLeftRadius, Style._props.BorderTopRightRadius, Style._props.BorderBotRightRadius, Style._props.BorderBotLeftRadius,
        _renderOffset, _contentArea);

        PerformLayout_SizeElements(mt, force, new vec2(Style._props.MaxWidth, Style._props.MaxHeight), null, StyleSheet, Gu.Context.FrameStamp);
        PerformLayout_PositionElements(force);

      }
    }
    private void RegenMesh(RenderView rv, MegaTex mt)
    {
      Box2f b = GetScreenSpaceClipQuad();
      List<v_v4v4v4v2u2v4v4> verts = new List<v_v4v4v4v2u2v4v4>();

      Gu.Assert(mt.DefaultPixel != null);
      RegenMesh(verts, mt.DefaultPixel, b, 0, DebugDraw);

#if DEBUG
      if (verts.Count > 0)
        _debug_pt[0] = verts[0];
      if (verts.Count > 1)
        _debug_pt[1] = verts[1];
      if (verts.Count > 2)
        _debug_pt[2] = verts[2];
#endif

      Mesh = new MeshData(rv.Name + "gui-mesh", OpenTK.Graphics.OpenGL4.PrimitiveType.Points,
      Gpu.CreateVertexBuffer(rv.Name + "gui-mesh", verts.ToArray()),
      null,
      false
      );
      Mesh.DrawMode = DrawMode.Forward;
      Mesh.DrawOrder = DrawOrder.Last;
      //wo.Mesh.DebugBreakRender = true;

    }
    private void SetExtentsToViewport(RenderView rv)
    {
      Style.Top = rv.Viewport.Y;
      Style.Left = rv.Viewport.X;
      Style.Width = rv.Viewport.Width;
      Style.Height = rv.Viewport.Height;
      Style.MinWidth = 0;
      Style.MinHeight = 0;
      Style.MaxWidth = rv.Viewport.Width;//Make sure stuff doesn't go off the screen.
      Style.MaxHeight = rv.Viewport.Height;//Make sure stuff doesn't go off the screen.
      Style.SizeModeWidth = UiSizeMode.Fixed;
      Style.SizeModeHeight = UiSizeMode.Fixed;
      Style.PositionMode = UiPositionMode.Relative;
    }
    private void CreateWindowEvents()
    {
      //Drag Info..
      //Context Menus..
      AddEvent(UiEventId.Mouse_Lmb_Press, (UiEventId evId, UiElement ele, PCMouse m, Gui2d g) =>
      {
        var e = (Gu.Context.Renderer.Picker.PickedObjectFrame as UiElement);
        _dragInfo.StartDrag(e, m);
      });
      AddEvent(UiEventId.Mouse_Move, (UiEventId evId, UiElement ele, PCMouse m, Gui2d g) =>
      {
        _dragInfo.UpdateDrag(m);
      });
      AddEvent(UiEventId.Mouse_Lmb_Release, (UiEventId evId, UiElement ele, PCMouse m, Gui2d g) =>
      {
        _dragInfo.EndDrag();
        // if (ContextMenu.RenderVisible)
        // {
        //   ContextMenu?.Hide();
        // }
      });
      AddEvent(UiEventId.Mouse_Rmb_Press, (UiEventId evId, UiElement ele, PCMouse m, Gui2d g) =>
      {
        // if (ContextMenu != null)
        // {
        //   ContextMenu.Show();
        //   ContextMenu.InlineStyle.Pos = m.Pos;
        // }
      });
      AddEvent(UiEventId.Mouse_Rmb_Release, (UiEventId evId, UiElement ele, PCMouse m, Gui2d g) =>
      {
        // if (ContextMenu.RenderVisible)
        // {
        //   ContextMenu?.Hide();
        // }
      });
    }


    #endregion

  }//Gui2d
  public class UiRef<T> where T : class
  {
    public T Value = null;
    public UiRef() { }
    public UiRef(T val) { Value = val; }
  }
  public class Gui2dShared
  {
    //Shared data between Gui2d instances for each context
    // public UiStyleSheet StyleSheet { get; private set; } = null;
    public WorldObject Dummy { get; private set; } = null;
    public MegaTex MegaTex { get; private set; } = null;
    public string Name { get; private set; } = "<unnamed>";

    public Gui2dShared(string name, List<FileLoc> resources)
    {
      Name = name;
      MegaTex = new MegaTex("gui_megatex", true, MegaTex.MtClearColor.DebugRainbow, true, TexFilter.Linear, false);
      MegaTex.AddResources(resources);
      var tx = MegaTex.Compile();

      //StyleSheet = new UiStyleSheet(MegaTex);

      if (tx != null)
      {
        var shader = Gu.Lib.LoadShader(RName.Shader_GuiShader);
        Dummy = new WorldObject("gui");
        Dummy.Material = new Material("GuiMT", shader);
        Dummy.Material.GpuRenderState.DepthTest = false;
        Dummy.Material.GpuRenderState.Blend = true;
        Dummy.Material.AlbedoSlot.Texture = tx.Albedo;
      }
      else
      {
        Gu.Log.Error("Failed to compile mega tex " + MegaTex.Name);
      }
    }

  }//Gui2dShared

  //We will need to split this. Guimanager isn't a datablock
  public class Gui2dManager : OpenGLContextDataManager<Dictionary<ulong, Gui2dShared>>
  {
    public Gui2dManager() : base("Gui2DManager")
    {
    }
    //Shared GUI data for each context
    protected override Dictionary<ulong, Gui2dShared> CreateNew()
    {
      return new Dictionary<ulong, Gui2dShared>();
    }
    public Gui2dShared GetOrCreateGui2d(string name, List<FileLoc> resources)
    {
      var qualifiedPaths = resources.ConvertAll((x) => { return x.QualifiedPath; });
      var hash = Gu.HashStringArray(qualifiedPaths);

      Gui2dShared? g = null;
      var dict = GetDataForContext(Gu.Context);
      if (!dict.TryGetValue(hash, out g))
      {
        g = new Gui2dShared(name, resources);
        dict.Add(hash, g);
      }
      return g;
    }
  }//Gui2dManager

}//Namespace Pri
