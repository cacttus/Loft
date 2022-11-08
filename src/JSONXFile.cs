using System;
using System.Diagnostics;
using System.Reflection;
namespace PirateCraft
{
  public class JSONXIgnore : Attribute
  {
    //Ignore class field
    public JSONXIgnore() { }
  }
  public class JSONXSerializeAs : Attribute
  {
    //Serialize a class field into a different name
    public string Name { get; private set; } = Lib.UnsetName;
    public JSONXSerializeAs(string JSONname) { Name = JSONname; }
  }

  public class JSONXFile : ByteForByteFile
  {
    //JSON file with extra syntax.
    // -> /* and // comments
    // ->  true, false for booleans instead of "true"
    // -> Include other JSON files with @".."
    // Possibly: allow datatypes.

    #region Enum / Class
    private enum ParseState
    {
      None,
      ClassBegin,
      ClassListContinue,
      ClassFieldNameBegin,
      ClassFieldNameEnd,
      ClassFieldValueBegin,
      ClassFieldValueEnd,
      ClassEnd,
      ArrayBegin,
      ArrayListContinue,
      ArrayValueBegin,
      ArrayValueEnd,
      IncludeFile,
    }
    public class JSONClassOrArray
    {
      //If class Fields !=null
      //If array Values !=null
      public JSONClassOrArray Parent = null;
      public Dictionary<string, JSONValue> Fields = null; // JSONFieldData
      public List<JSONValue> Values = null; // JSONFieldData
      public int Line = -1;
      public int Col = -1;
      public FileLoc FileLoc = null;

      public bool IsClass() { return Fields != null; }
      public bool IsArray() { return Values != null; }
      public object Data { get { if (IsClass()) { return Fields; } else { return Values; } } }

      public JSONClassOrArray(FileLoc f, int line, int col) { Line = line; Col = col; FileLoc = f; }
    }
    public class JSONValue
    {
      public JSONClassOrArray Parent = null;
      public object? Data = null;
      public int Line = -1;
      public int Col = -1;
      public FileLoc FileLoc = null;

      public bool DataAsArray(out JSONClassOrArray? val)
      {
        if (DataAsClassOrArray(out val))
        {
          return val.IsArray();
        }
        return false;
      }
      public bool DataAsClass(out JSONClassOrArray? val)
      {
        if (DataAsClassOrArray(out val))
        {
          return val.IsClass();
        }
        return false;
      }
      public bool DataAsClassOrArray(out JSONClassOrArray? val)
      {
        if (Data is JSONClassOrArray)
        {
          val = (JSONClassOrArray)Data;
        }
        val = null;
        return (val != null);
      }

      public JSONValue(FileLoc f, int line, int col) { Line = line; Col = col; FileLoc = f; }
    }

    #endregion
    #region Members

    public JSONClassOrArray Root = null;

    //Parse State (may be inherited)
    private ParseState _state = ParseState.None;
    private JSONClassOrArray _curClassOrArray = null;//Root;//Array, or class
    private string? _curField = null;

    #endregion
    #region Public:Methods

    public JSONXFile(FileLoc loc, JSONClassOrArray? parentFileRoot = null, List<string>? parentErrors = null, string? curField = null) : base(loc)
    {
      if (parentErrors != null)
      {
        _errors = parentErrors;
      }
      else
      {
        _errors = new List<string>();
      }
      if (parentFileRoot != null)
      {
        Root = parentFileRoot;
        _curClassOrArray = parentFileRoot;
        _state = ParseState.ClassFieldValueBegin;
      }
      else
      {
        Root = null;
        _curClassOrArray = null;
      }
      if (curField != null)
      {
        _curField = curField;
      }
      else
      {
        _curField = null;
      }
    }

    public void FillOutClass(object theClass)
    {
      //Basically fill out a class assuming the JSON properties match the class field names.

      //Since json file will only store Double, convert the units to their proper thing
      var try_cast_and_set = (JSONValue? field, FieldInfo fieldInfo, Type field_type, Type inf_type) =>
      {
        var fdatatype = field.Data.GetType();
        var finftype = fieldInfo.FieldType;
        if (fdatatype == field_type)
        {
          if (finftype == inf_type)
          {
            var casted = Convert.ChangeType(field.Data, inf_type);
            fieldInfo.SetValue(theClass, casted);
            return true;
          }
        }
        return false;
      };

      var theType = theClass.GetType();
      foreach (var fieldinfo in theType.GetFields())
      {
        var ignore = fieldinfo.GetAttribute<JSONXIgnore>();

        if (ignore == null)
        {
          JSONValue? field = null;
          if (Root != null && Root.Fields != null && Root.Fields.TryGetValue(fieldinfo.Name, out field))
          {
            if (field != null)
            {
              if (field.Data != null)
              {
                try
                {
                  var str = typeof(System.String);
                  var dbl = typeof(System.Double);
                  var sgl = typeof(System.Single);
                  var i32 = typeof(System.Int32);
                  var i64 = typeof(System.Int64);
                  var bln = typeof(System.Boolean);

                  var ft = field.Data.GetType();
                  var fit = fieldinfo.FieldType;

                  //Add other typecasts here if needed.
                  if (try_cast_and_set(field, fieldinfo, str, str)) { continue; }
                  if (try_cast_and_set(field, fieldinfo, bln, bln)) { continue; }
                  if (try_cast_and_set(field, fieldinfo, str, bln)) { continue; }
                  if (try_cast_and_set(field, fieldinfo, dbl, dbl)) { continue; }
                  if (try_cast_and_set(field, fieldinfo, dbl, i32)) { continue; }
                  if (try_cast_and_set(field, fieldinfo, dbl, i64)) { continue; }
                  if (try_cast_and_set(field, fieldinfo, dbl, bln)) { continue; }
                  if (try_cast_and_set(field, fieldinfo, dbl, sgl)) { continue; }

                  //Here we must put other data (complex0 structures.
                  //TODO:

                  FillOutError(field.FileLoc, field.Line, field.Col, theType, fieldinfo, $"Could not cast {ft} to {fit}");
                }
                catch (Exception ex)
                {
                  FillOutError(field.FileLoc, field.Line, field.Col, theType, fieldinfo, $" type={fieldinfo.FieldType.ToString()} value={field.Data.ToString()}, Invalid Cast:\n{StringUtil.Indent(ex.ToString(), 4)}");
                }
              }
              else
              {
                FillOutError(field.FileLoc, field.Line, field.Col, theType, fieldinfo, $"JSON Field value was null");
              }
            }
            else
            {
              FillOutError(field.FileLoc, -1, -1, theType, fieldinfo, $" JSON Field was null.");
            }
          }
          else
          {
            FillOutError(null, -1, -1, theType, fieldinfo, $" Class field not found in JSON.");
          }
        }//if !ignore
        else
        {
          int n = 0; n++;
        }
      }//foreach fieldinfo
    }

    #endregion
    #region Private:Members

    protected override bool DoParse( char c, string eated, ref bool requestBreak)
    {
      bool handled = true;

      if (c == '\"')
      {
        if (_state == ParseState.ClassBegin || _state == ParseState.ClassListContinue
          || _state == ParseState.ArrayBegin || _state == ParseState.ArrayListContinue
          || _state == ParseState.ClassFieldNameBegin || _state == ParseState.ClassFieldValueBegin
          || _state == ParseState.ArrayValueBegin || _state == ParseState.IncludeFile)
        {
          ByteParser.EatTo(_data, ref _line, ref _col, ref eated, ref _idx, '\"');
          if (_state == ParseState.ClassBegin || _state == ParseState.ClassListContinue || _state == ParseState.ClassFieldNameBegin)
          {
            //field name, after eating, we are at end of field name
            Gu.Assert(_curField == null);
            _curField = eated;
            _state = ParseState.ClassFieldNameEnd; // "field" : .. 
          }
          else if (_state == ParseState.ClassFieldValueBegin)
          {
            //field val
            DoClassValue(eated);
          }
          else if (_state == ParseState.ArrayBegin || _state == ParseState.ArrayListContinue)
          {
            //value
            DoArrayValue(eated);
          }
          else if (_state == ParseState.IncludeFile)
          {
            TryIncludeFile(eated, _line, _col, _curClassOrArray, _curField);
            _curField = null;
            _state = ParseState.ClassFieldValueEnd;
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
      else if (c == ',')
      {
        Gu.Assert(_curClassOrArray != null);
        if (_state == ParseState.ClassFieldValueEnd || _state == ParseState.ArrayValueEnd)
        {
          if (_curClassOrArray.Values != null)
          {
            _state = ParseState.ArrayListContinue;
          }
          else if (_curClassOrArray.Fields != null)
          {
            _state = ParseState.ClassFieldNameBegin;
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
      else if (c == ':')
      {
        if (_state == ParseState.ClassFieldNameEnd)
        {
          _state = ParseState.ClassFieldValueBegin;
        }
        else
        {
          UnexpectedError(c);
        }
      }
      else if (c == '{' || c == '[')
      {
        if (_state == ParseState.None)
        {
          if (c == '{')
          {
            //If not null, this is an included JSON
            if (_curClassOrArray == null)
            {
              _curClassOrArray = new JSONClassOrArray(_fileLoc, _line, _col);
              Root = _curClassOrArray;
              _curClassOrArray.Fields = _curClassOrArray.Fields.ConstructIfNeeded();
            }
            _state = ParseState.ClassBegin;
          }
          else
          {
            UnexpectedError(c);
          }
        }
        else if (_state == ParseState.ClassFieldValueBegin)
        {
          //value is a class
          DoArrayOrClassFieldValue(c == '{');
        }
        else if (_state == ParseState.ArrayBegin || _state == ParseState.ArrayListContinue)
        {
          if (c == '{')
          {
            DoClassOnly();
          }
          else
          {
            // [[],[]] not valid json
            UnexpectedError(c);
          }
        }
        else
        {
          UnexpectedError(c);
        }
      }
      else if (c == '}')
      {
        //Technically we could check for ClassFieldNameBegin or ArrayListContinue, as well as other valid states
        Gu.Assert(_curClassOrArray != null);
        if (_curClassOrArray.Parent == null)
        {
          requestBreak = true;
        }
        _curClassOrArray = _curClassOrArray.Parent;
        _state = ParseState.ClassFieldValueEnd;
      }
      else if (c == ']')
      {
        Gu.Assert(_curClassOrArray != null);
        _curClassOrArray = _curClassOrArray.Parent;
        //so technically, afaik, arrays can only be fields no? So state here would be "class field end"
        _state = ParseState.ClassFieldValueEnd;
      }
      else if (c == '.' || Char.IsDigit(c))
      {
        //number
        if (_state == ParseState.ClassFieldValueBegin || _state == ParseState.ArrayListContinue)
        {
          _idx -= 1;
          var d = ByteParser.ParseDouble(_data, ref _line, ref _col, ref _idx);
          if (d != null)
          {
            if (_state == ParseState.ClassFieldValueBegin)
            {
              DoClassValue(d);
            }
            else if (_state == ParseState.ArrayListContinue)
            {
              DoArrayValue(d);
            }
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
      else if (c == 't' || c == 'f' || c == 'T' || c == 'F')
      {
        //boolean
        if (_state == ParseState.ClassFieldValueBegin || _state == ParseState.ArrayListContinue)
        {
          _idx -= 1;
          var d = ByteParser.ParseBool(_data, ref _line, ref _col, ref _idx);
          if (d != null)
          {
            if (_state == ParseState.ClassFieldValueBegin)
            {
              DoClassValue(d);
            }
            if (_state == ParseState.ArrayListContinue)
            {
              DoArrayValue(d);
            }
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
      else if (c == '@')
      {
        //Config include
        if (_state == ParseState.ClassFieldValueBegin)
        {
          _state = ParseState.IncludeFile;
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

    }//parse
    private void DoArrayValue(object? eated)
    {
      var v = new JSONValue(_fileLoc, _line, _col);
      v.Data = eated;
      v.Parent = _curClassOrArray;
      Gu.Assert(_curClassOrArray != null);
      Gu.Assert(_curClassOrArray.Values != null);
      _curClassOrArray.Values.Add(v);
      _state = ParseState.ArrayValueEnd;
    }
    private void DoClassValue(object? eated)
    {
      Gu.Assert(_curField != null);
      var v = new JSONValue(_fileLoc, _line, _col);
      v.Data = eated;
      v.Parent = _curClassOrArray;
      Gu.Assert(_curClassOrArray != null);
      Gu.Assert(_curClassOrArray.Fields != null);
      _curClassOrArray.Fields.Add(_curField, v);
      _curField = null;
      _state = ParseState.ClassFieldValueEnd;
    }
    private void DoArrayOrClassFieldValue(bool is_class)
    {
      Gu.Assert(_curClassOrArray != null);
      Gu.Assert(_curField != null);
      var jv = new JSONValue(_fileLoc, _line, _col);
      jv.Parent = _curClassOrArray;
      _curClassOrArray = new JSONClassOrArray(_fileLoc, _line, _col);
      _curClassOrArray.Parent = jv.Parent;
      jv.Data = _curClassOrArray;
      jv.Parent.Fields = jv.Parent.Fields.ConstructIfNeeded();
      jv.Parent.Fields.Add(_curField, jv);
      _curField = null;

      if (is_class)
      {
        _curClassOrArray.Fields = _curClassOrArray.Fields.ConstructIfNeeded();
        _state = ParseState.ClassBegin;
      }
      else
      {
        _curClassOrArray.Values = _curClassOrArray.Values.ConstructIfNeeded();
        _state = ParseState.ArrayBegin;
      }
    }
    private void DoClassOnly()
    {
      var jv = new JSONValue(_fileLoc, _line, _col);
      jv.Parent = _curClassOrArray;
      _curClassOrArray = new JSONClassOrArray(_fileLoc, _line, _col);
      jv.Data = _curClassOrArray;
      _curClassOrArray.Parent = jv.Parent;
      Gu.Assert(jv.Parent.Values != null);
      jv.Parent.Values.Add(jv);
      _curClassOrArray.Fields = _curClassOrArray.Fields.ConstructIfNeeded();
      _state = ParseState.ClassBegin;
    }
    public void BreakOn(int line, int col, string filename)
    {
      if (_line == line && _col == col && StringUtil.Equals(_fileLoc.RawPath, filename))
      {
        Gu.DebugBreak();
      }
    }
    private void FillOutError(FileLoc f, int line, int col, Type t, System.Reflection.FieldInfo inf, string msg)
    {
      var s = $"{t.ToString()}.{inf.Name}({inf.FieldType.ToString()}): {msg}";
      Error(f, line, col, s);
    }

    private void TryIncludeFile(string eated, int line, int col, JSONClassOrArray curClassOrArray, string curField)
    {
      var vals = eated.Split(':');
      if (vals == null || vals.Length != 2)
      {
        Error(_fileLoc, line, col, $"Include File value '{eated}' did not specify '{typeof(FileStorage).Name}', valid values:{typeof(FileStorage).GetEnumValues()}, ex: @\"myfile.json:embedded\"");
      }
      else
      {
        object? storage = null;
        if (Enum.TryParse(typeof(FileStorage), vals[1], true, out storage))
        {
          FileStorage storage_casted = (FileStorage)storage;
          FileLoc fl = new FileLoc(vals[0], storage_casted);
          try
          {
            JSONXFile f = new JSONXFile(fl, curClassOrArray, _errors, curField);
            f.Load();
          }
          catch (Exception ex)
          {
            Error(_fileLoc, line, col, $"Failed to load include file '{eated}'.");
          }
        }
        else
        {
          Error(_fileLoc, line, col, $"Include File value '{eated}' could not parse '{typeof(FileStorage).Name}', valid values:{typeof(FileStorage).GetEnumValues()}, ex: @\"myfile.json:embedded\"");
        }
      }
    }
    #endregion

  }//JSONXFile

}