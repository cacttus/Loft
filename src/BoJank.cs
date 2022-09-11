using System.Text;
namespace PirateCraft
{
  //BoJank data library. (It is what it sounds like)
  // Headers and row keys are enum.
  // Datatype is uniform across the table.
  // Also has a CSV parser.
  public class BoJankDataRow<TDataType>
  {
    public List<TDataType> Data { get; set; } = null; //This will be 1-col count
    private void AssertIndex(int i)
    {
      Gu.Assert(i >= 0 && Data != null && i < Data.Count);
    }
    public TDataType this[int i]
    {
      get
      {
        AssertIndex(i);
        return Data[i];
      }
      set
      {
        AssertIndex(i);
        Data[i] = value;
      }
    }
    public BoJankDataRow()
    {
    }
  }
  public class BoJankEnumDataTable<THeaderTypeEnum, TIndexTypeEnum, TDataType> where THeaderTypeEnum : Enum where TIndexTypeEnum : Enum
  {
    //A datatable where header and column indexes are mapped to C# enums.

    public int NumCols { get { Gu.Assert(Headers != null); return Headers.Count; } }
    public int NumRows { get { Gu.Assert(Rows != null); return Rows.Count; } }
    public List<THeaderTypeEnum> Headers { get; set; } = null;//headers ordered as loaded in the file.
    public Dictionary<THeaderTypeEnum, int> HeadersIndex { get; set; } = null; //Header to index
    public List<BoJankDataRow<TDataType>> Rows { get; set; } = null;
    public Dictionary<TIndexTypeEnum, int> RowsIndex { get; set; } = null;
    public string Name { get; private set; } = "";

    public BoJankEnumDataTable(string name)
    {
      Name = name;
    }
    public BoJankDataRow<TDataType>? this[TIndexTypeEnum i]
    {
      get
      {
        return Rows[RowsIndex[i]];
      }
      set
      {
        Rows[RowsIndex[i]] = value;
      }
    }

    public BoJankEnumDataTable()
    {
    }
    public TDataType? Cell(THeaderTypeEnum colid, TIndexTypeEnum rowid, bool errorIfNotFound=false)
    {
      if (HeadersIndex != null && HeadersIndex.TryGetValue(colid, out var hidx))
      {
        if (RowsIndex != null && RowsIndex.TryGetValue(rowid, out var rowidx))
        {
          var row = Rows[rowidx];
          var dat = row[hidx];
          return dat;
        }
        else if(errorIfNotFound)
        {
          Gu.Log.Error($"'{Name}' Data was not found for row index '{rowid.ToString()}' ");
        }
      }
      else
      {
        Gu.Log.Error($"{Name} Data not found for col {colid.ToString()}");
      }
      return default(TDataType);
    }
  }
  public class BoJankEnumCSV<THeaderTypeEnum, TIndexTypeEnum, TDataType> where THeaderTypeEnum : Enum where TIndexTypeEnum : Enum
  {
    //CSV file where the headers are enum values. [Description("desc")] MyEnumVal, ....
    //You can set delim by saying #DELIM=[delim] .. #DELIM=, #DELIM=;
    //Commments are #
    public BoJankEnumDataTable<THeaderTypeEnum, TIndexTypeEnum, TDataType> DataTable = null;

    private FileLoc _fileLoc = null;
    private string c_csvDelimTag = "#DELIM";
    private string c_csvNullColumn = "**";
    private int _delim = ';';
    private string Name = "BoJankEnumCSV";
    private IEnumerable<THeaderTypeEnum> _headerEnumVals = null;
    private IEnumerable<TIndexTypeEnum> _indexEnumVals = null;
    private Func<string, TDataType> DataConverterFunc = null;

    public BoJankEnumCSV(FileLoc loc, Func<string, TDataType> converter)
    {
      _fileLoc = loc;
      _headerEnumVals = Enum.GetValues(typeof(THeaderTypeEnum)).Cast<THeaderTypeEnum>();
      _indexEnumVals = Enum.GetValues(typeof(TIndexTypeEnum)).Cast<TIndexTypeEnum>();
      DataConverterFunc = converter;

      DataTable = new BoJankEnumDataTable<THeaderTypeEnum, TIndexTypeEnum, TDataType>(_fileLoc.RawPath);

      Gu.Assert(_fileLoc != null);
      Gu.Assert(DataConverterFunc != null);

      Load();
    }
    public StringBuilder DebugPrintTable()
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("Datatable: Debug Output:");
      if (this.DataTable != null)
      {
        if (this.DataTable.Headers != null)
        {
          foreach (var h in this.DataTable.Headers)
          {
            sb.Append(h.ToString());
            sb.Append(" ");
          }
        }
        else
        {
          sb.Append("Headers were null.");
        }
        sb.Append("\n");
        if (DataTable.Rows != null)
        {
          foreach (var row in DataTable.Rows)
          {
            foreach (var val in row.Data)
            {
              if (val != null)
              {
                sb.Append(val.ToString());
                sb.Append(" ");
              }
              else
              {
                sb.Append("Value was null");
              }
            }

            sb.Append("\n");
          }
        }
        else
        {
          sb.Append("Datatable.Rows was null.");
        }
      }
      else
      {
        sb.Append("Datatable was null.");
      }


      return sb;
    }
    private bool Load()
    {
      Gu.Assert(_fileLoc != null);
      Gu.Assert(DataConverterFunc != null);

      if (!_fileLoc.Exists)
      {
        Gu.Log.Error($"{Name}: File {_fileLoc.QualifiedPath} does not exist.");
        return false;
      }

      string[] lines = _fileLoc.ReadAllLines();


      for (int iLine = 0; iLine < lines.Length; iLine++)
      {
        var line = lines[iLine];
        if (StringUtil.IsNotEmpty(line))
        {
          if (line.StartsWith(c_csvDelimTag))
          {
            //Special comments
            var parts = line.Split('=');
            if (parts.Length == 2)
            {
              _delim = (int)parts[1].Trim()[0];
            }
            else
            {
              Gu.Log.Error($"{Name}: CSV Invalid {c_csvDelimTag} tag");
              Gu.DebugBreak();
            }
          }
          else if (line.StartsWith("#"))
          {
            //Comment
          }
          else
          {
            var row = line.Split((char)_delim, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (DataTable.Headers == null)
            {
              ParseHeader(row);
            }
            else
            {
              ParseRow(row);
            }
          }
        }
        else
        {
          Gu.Log.Warn($"{Name} Line {iLine} was empty in Datafile {this._fileLoc.QualifiedPath}");
        }
      }
      return true;
    }
    private void ParseHeader(string[] header_row_raw)
    {
      Gu.Assert(header_row_raw != null);

      DataTable.Headers = new List<THeaderTypeEnum>();
      DataTable.HeadersIndex = new Dictionary<THeaderTypeEnum, int>();
      foreach (var headerText in header_row_raw)
      {
        try
        {
          THeaderTypeEnum ht = ParseIndex<THeaderTypeEnum>(headerText, _headerEnumVals);
          if (ht == null)
          {
            Gu.Log.Error($"{Name}: Could not parse header to enum '{headerText}', description did not match.");
            Gu.DebugBreak();
            DataTable.Headers.Add(default(THeaderTypeEnum));
          }
          else
          {
            DataTable.Headers.Add(ht);
          }
          //Add header index.
          DataTable.HeadersIndex.Add(DataTable.Headers[DataTable.Headers.Count - 1], DataTable.Headers.Count-1);
        }
        catch (Exception ex)
        {
          Gu.Log.Error($"{Name}: Could not parse header to enum '{headerText}'", ex);
        }
      }
    }
    private void ParseRow(string[] row_raw)
    {
      try
      {
        Gu.Assert(DataConverterFunc != null);
        Gu.Assert(row_raw != null);
        Gu.Assert(row_raw.Length > 0);

        if (row_raw.Length != DataTable.NumCols)
        {
          int ncols = (row_raw != null ? row_raw.Length : 0);
          Gu.Log.Warn($"{Name}: Row {row_raw} ({ncols} cols) did not have the required number of columns {DataTable.NumCols}");
        }
        else
        {
          TIndexTypeEnum? index = ParseIndex<TIndexTypeEnum>(row_raw[0], _indexEnumVals);
          if (index == null)
          {
            Gu.Log.Error($"{Name}: Phrase Row \n{String.Join((char)_delim, row_raw)}\n could not parse index {row_raw[0]}.");
            Gu.DebugBreak();
          }
          else
          {
            var row = new BoJankDataRow<TDataType>();
            row.Data = row.Data.ConstructIfNeeded();
            for (int ri = 0; ri < row_raw.Length; ++ri)
            {
              var conv = DataConverterFunc(row_raw[ri]);
              row.Data.Add(conv);
            }
            DataTable.Rows = DataTable.Rows.ConstructIfNeeded();
            DataTable.Rows.Add(row);
            DataTable.RowsIndex = DataTable.RowsIndex.ConstructIfNeeded();
            DataTable.RowsIndex.Add(index, DataTable.Rows.Count - 1);
          }
        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error($"{Name}: Phrase Row \n{String.Join((char)_delim, row_raw)}\n threw an exception", ex);
      }
    }
    private T ParseIndex<T>(string index_raw_str, IEnumerable<T> vals) where T : Enum
    {
      //Convert a string into an enum value, based on the actual enum name in code. (not the Description[])
      Gu.Assert(index_raw_str != null);
      var htTrim = index_raw_str.Trim().Trim('\"');
      var st = vals.Where(x => x.ToString().Equals(htTrim)).ToList();

      if (st.Count == 0)
      {
        Gu.Log.Error($"{Name}: Invalid header value {index_raw_str}");
        Gu.DebugBreak();
        return default(T);
      }

      return st[0];
    }

  }//SimpleCSV


}