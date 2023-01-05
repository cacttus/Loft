using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Loft
{
  public class CSSAttribute : Attribute
  {
    public string Name = "";
    public CSSAttribute(string name)
    {
      Name = name;
    }
  }
  public class CSSFile : ByteForByteFile
  {
    //yes, yes, yes, it's not a very flexible grammar in any way. .. do things but this is fast / simple afternoon file
    //If we really need to, just import a css parser, trash this
    public enum ParseState
    {
      None,
      StyleBlockBegin,
      BlockEnd,
      ClassNameParsed,
      StylePropNameParsed,
      StylePropEnd,
      FontFacePropEnd,
      FontFaceIDParsed,
      FontFaceBlockBegin,
      FontFacePropNameParsed,
    }
    private ParseState _state = ParseState.None;
    private List<string> _classnames = null;
    private string? _propName = null;
    public List<UiStyle> Styles = new List<UiStyle>();
    private UiStyle _curStyle = null;
    private FontFace _curFont = null;
    public List<FontFace> Fonts = new List<FontFace>();
    private Gui2d _gui = null;

    private Dictionary<UiStyle, string> _assignFontFaces = new Dictionary<UiStyle, string>();

    public CSSFile(FileLoc loc, Gui2d g) : base(loc)
    {
      Gu.Assert(g != null);
      _gui = g;
    }
    protected override void AfterParse()
    {
      foreach (var p in _assignFontFaces)
      {
        var xf = Fonts.Where(x => x.Name == p.Value).FirstOrDefault();
        if (xf != null)
        {
          p.Key.FontFace = xf;
        }
        else
        {
          _errors.Add($"Could not assiggn font face {p.Value} to style {p.Key.Name}");
        }
      }
    }
    protected override bool DoParse(char c, string eated, ref bool requestBreak)
    {
      bool handled = true;

     // DebugWriteFileToPos();

      if (c == '.')
      {
        if (_state == ParseState.None || _state == ParseState.BlockEnd || _state == ParseState.ClassNameParsed)
        {
          if (_state == ParseState.None || _state == ParseState.BlockEnd)
          {
            Gu.Assert(_classnames == null);
          }
          else if (_state == ParseState.ClassNameParsed)
          {
            //inherit the other class.
            Gu.Assert(_classnames != null);
          }
          var classname = ByteParser.ParseIdentifier(_data, ref _line, ref _col, ref _idx);
          Gu.Assert(classname != null && classname.Length > 0);
          _classnames = _classnames.ConstructIfNeeded();
          _classnames.Add(classname);

          _state = ParseState.ClassNameParsed;
        }
        else
        {
          UnexpectedError(c);
        }
      }
      else if (ByteParser.IsAlpha(c))
      {
        _idx--;
        if (_state == ParseState.StyleBlockBegin || _state == ParseState.StylePropEnd)
        {
          Gu.Assert(_propName == null);
          _propName = ByteParser.ParseIdentifier(_data, ref _line, ref _col, ref _idx);
          _state = ParseState.StylePropNameParsed;
        }
        else if (_state == ParseState.FontFaceBlockBegin || _state == ParseState.FontFacePropEnd)
        {
          Gu.Assert(_propName == null);
          _propName = ByteParser.ParseIdentifier(_data, ref _line, ref _col, ref _idx);
          _state = ParseState.FontFacePropNameParsed;
        }
        else
        {
          UnexpectedError(c);
        }
      }
      else if (c == ':')
      {
        if (_state == ParseState.StylePropNameParsed)
        {
          Gu.Assert(_propName != null && _propName.Length > 0);
          string propval = "";
          ByteParser.EatTo(_data, ref _line, ref _col, ref propval, ref _idx, ';');
          ParseStyleProp(_propName, propval);
          _propName = null;
          _state = ParseState.StylePropEnd;
        }
        else if (_state == ParseState.FontFacePropNameParsed)
        {
          Gu.Assert(_curFont != null);
          Gu.Assert(_propName != null && _propName.Length > 0);
          string propval = "";
          ByteParser.EatTo(_data, ref _line, ref _col, ref propval, ref _idx, ';');
          var pn = _propName.Trim();
          var pv = propval.Trim();

          if (StringUtil.Equals(pn, "src"))
          {
            var vals = pv.Split('(');
            if (vals.Length == 2)
            {
              if (StringUtil.Equals(vals[0].Trim(), "url"))
              {
                pv = vals[1];
                pv = pv.Trim('(').Trim(')').Trim('\'').Trim('\'').Trim('\"').Trim('\"');
                vals = pv.Split(':');
                if (vals != null && vals.Length == 2)
                {
                  _curFont.RawPath = vals[0];
                  if (StringUtil.Equals(vals[1], "embedded"))
                  {
                    _curFont.FileStorage = FileStorage.Embedded;
                  }
                  else if (StringUtil.Equals(vals[1], "disk"))
                  {
                    _curFont.FileStorage = FileStorage.Disk;
                  }
                  else
                  {
                  }
                }
                else if (vals != null && vals.Length == 1)
                {
                  _curFont.RawPath = vals[0];
                  _curFont.FileStorage = FileStorage.Embedded;
                  Warning(_fileLoc, _line, _col, $"font-face invalid  property, '{pn}', '{pv}' embedded/disk not specified, default to embedded ex; url(xyz:embedded)");
                }
                else
                {
                  Error(_fileLoc, _line, _col, $"font-face invalid  property, '{pn}' invalid value '{pv}'");
                }
              }
              else
              {
                Error(_fileLoc, _line, _col, $"font-face invalid  property, '{pn}' invalid value '{pv}'");
              }
            }
            else
            {
              Error(_fileLoc, _line, _col, $"font-face invalid  property, '{pn}' invalid value '{pv}'");
            }
          }
          else if (StringUtil.Equals(pn, "font-family"))
          {
            _curFont.Name = pv;
          }
          else if (StringUtil.Equals(pn, "font-weight"))
          {
            UiFontStyle s;
            if (StringUtil.Equals(pv, "normal"))
            {

            }

          }
          else
          {
            Error(_fileLoc, _line, _col, $"font-face invalid  property, '{pn}' , value= '{pv}'");
          }
          _propName = null;
          _state = ParseState.FontFacePropEnd;
        }
        else
        {
          UnexpectedError(c);
        }
      }
      else if (c == ';')
      {
        //This shouldn't happen if we "EatTo" smicolon
        //if (_state == ParseState.StylePropEnd || _state == ParseState.FontFacePropEnd){
        //
        //}
      }
      else if (c == '{')
      {
        if (_state == ParseState.FontFaceIDParsed)
        {
          _state = ParseState.FontFaceBlockBegin;
        }
        else if (_state == ParseState.ClassNameParsed)
        {
          Gu.Assert(_curStyle == null);
          Gu.Assert(_classnames != null);
          Gu.Assert(_classnames.Count > 0);
          _curStyle = new UiStyle(_classnames[_classnames.Count - 1], _classnames.Take(_classnames.Count - 1).ToList());
          _classnames = null;
          _state = ParseState.StyleBlockBegin;
        }
        else
        {
          UnexpectedError(c);
        }
      }
      else if (c == '}')
      {
        if (_state == ParseState.StyleBlockBegin || _state == ParseState.FontFacePropEnd || _state == ParseState.StylePropEnd)
        {
          if (_curFont != null)
          {
            this.Fonts.Add(_curFont);
            _curFont = null;
          }
          if (_curStyle != null)
          {
            this.Styles.Add(_curStyle);
            _curStyle = null;
          }
          _state = ParseState.BlockEnd;
        }
        else
        {
          UnexpectedError(c);
        }
      }
      else if (c == '@')
      {
        if (_state == ParseState.None || _state == ParseState.BlockEnd)
        {
          var ident = ByteParser.ParseIdentifier(_data, ref _line, ref _col, ref _idx);
          if (ident.Trim() == "font-face")
          {
            _curFont = new FontFace();
            _state = ParseState.FontFaceIDParsed;
          }
          else
          {
            UnexpectedError(c);
          }
        }
        else
        {
          UnexpectedError(c);
        }
      }
      else
      {
        handled = false;
      }
      return handled;
    }
    private void ParseStyleProp(string pname, string pval)
    {
      bool set = false;
      bool has = false;
      pname = pname.Trim();
      pval = pval.Trim();
      foreach (var p in typeof(UiStyle).GetProperties())
      {
        set = false;
        has = false;
        var pattrib = p.GetAttribute<CSSAttribute>();
        if (pattrib != null)
        {
          has = true;
          if (pattrib.Name == pname)
          {
            object? value = null;
            var ptype = p.PropertyType;

            if (ptype == typeof(UiFontStyle?) || ptype == typeof(UiPositionMode?) || ptype == typeof(UiSizeMode?) ||
                ptype == typeof(UiOverflowMode?) || ptype == typeof(UiDisplayMode?) || ptype == typeof(UiImageTiling?))
            {
              Type etype = null;
              if (ptype == typeof(UiFontStyle?)) { etype = typeof(UiFontStyle); }
              else if (ptype == typeof(UiPositionMode?)) { etype = typeof(UiPositionMode); }
              else if (ptype == typeof(UiSizeMode?)) { etype = typeof(UiSizeMode); }
              else if (ptype == typeof(UiOverflowMode?)) { etype = typeof(UiOverflowMode); }
              else if (ptype == typeof(UiDisplayMode?)) { etype = typeof(UiDisplayMode); }
              else if (ptype == typeof(UiImageTiling?)) { etype = typeof(UiImageTiling); }
              else
              {
                Gu.BRThrowNotImplementedException();
              }
              //Oh boy oh boy
              var vals = Enum.GetValues(etype);
              foreach (var val in vals)
              {
                var members = etype.GetMember(val.ToString());
                Gu.Assert(members.Length == 1);
                var attrib = members[0].GetCustomAttribute<CSSAttribute>();
                //The enum didn't have a CSSAttribute that tells its css name
                Gu.Assert(attrib != null);
                Gu.Assert(attrib is CSSAttribute);
                var cssName = ((CSSAttribute)attrib).Name;
                if (StringUtil.Equals(cssName, pval))
                {
                  value = val;
                }
              }
            }
            else if (ptype == typeof(System.String))
            {
              value = pval;
            }
            else if (ptype == typeof(System.Single?))
            {
              float res = 0;
              if (System.Single.TryParse(pval, out res))
              {
                value = res;
              }
              else
              {
                Error(_fileLoc, _line, _col, $"Property '{pname}' expected float, but got: '{pval}'");
              }
            }
            else if (ptype == typeof(vec4?))
            {
              vec4 res = new vec4();
              if (ByteParser.TryParseVec4RGBA(pval, out res))
              {
                value = res;
              }
              else
              {
                Error(_fileLoc, _line, _col, $"Property '{pname}' expected rgba(f,f,f,f), but got: '{pval}'");
              }
            }
            else if (ptype == typeof(FontFace))
            {
              Gu.Assert(_curStyle != null);
              _assignFontFaces = _assignFontFaces.ConstructIfNeeded();
              _assignFontFaces.Add(_curStyle, pval);
            }
            else if (ptype == typeof(MtTex))
            {
              Gu.Assert(_gui != null);
              if (StringUtil.Equals(pval, "none"))
              {
                value = null;
                set = true;
              }
              else if (StringUtil.Equals(pval, "solidcolor"))
              {
               // value = _gui.DefaultPixel;
              }
              else
              {
                Warning(_fileLoc, _line, _col, $"'{pname}' invalid value '{pval}' For now Textures aren't supported in CSS. (TODO:)");
              }
            }

            if (value != null)
            {
              p.SetValue(_curStyle, value);
            }
            else if (set == false)
            {
              Error(_fileLoc, _line, _col, $"Property '{pname}' invalid value '{pval}'");
            }

            set = true;
          }
        }
        else
        {
          //attrib not on style class prop, ignore
          has = false;
        }
      }
      if (has && !set)
      {
        Error(_fileLoc, _line, _col, $"Property '{pname}' was not set ('{pval}')");
      }
    }




  }

}