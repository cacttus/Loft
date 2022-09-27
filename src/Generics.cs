using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace PirateCraft
{
  public enum ActionState
  {
    Pause, Run, Stop
  }
  public enum ActionRepeat
  {
    Repeat,
    DoNotRepeat
  }
  public enum LambdaBool
  {
    Break,
    Continue
  }
  public class MultiMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull
  {
    public SortedDictionary<TKey, List<TValue>> _dict = new SortedDictionary<TKey, List<TValue>>();
    //Returns the number of Keys in the dictionary
    public int Count
    {
      get
      {
        return _dict.Count;
      }
    }
    public MultiMap() { }
    public MultiMap(MultiMap<TKey, TValue> rhs)
    {
      _dict = new SortedDictionary<TKey, List<TValue>>(rhs._dict);
    }
    public SortedDictionary<TKey, List<TValue>>.KeyCollection Keys
    {
      get
      {
        return _dict.Keys;
      }
    }
    public KeyValuePair<TKey, TValue> First()
    {
      var l = _dict.First();
      if (l.Value.Count > 0)
      {
        return new KeyValuePair<TKey, TValue>(l.Key, l.Value[0]);
      }
      return default(KeyValuePair<TKey, TValue>);
    }
    public void Add(TKey x, TValue y)
    {
      List<TValue> ret = null;
      if (!_dict.TryGetValue(x, out ret))
      {
        ret = new List<TValue>();
        _dict.Add(x, ret);
      }
      ret.Add(y);
    }
    public void Add(TKey x, List<TValue> y)
    {
      List<TValue> ret = null;
      if (!_dict.TryGetValue(x, out ret))
      {
        ret = new List<TValue>();
        _dict.Add(x, ret);
      }
      ret.AddRange(y);
    }
    public List<TValue> this[TKey k]
    {
      //operator[]
      get
      {
        return ItemsAt(k);
      }
      set
      {
        Add(k, value);
      }
    }
    public List<TValue> ItemsAt(TKey key)
    {
      if (_dict.TryGetValue(key, out var ret))
      {
        return ret;
      }
      return null;
    }
    public void SetValueList(TKey key, List<TValue> val)
    {
      _dict.Remove(key);
      _dict.Add(key, val);
    }
    public bool Remove(KeyValuePair<TKey, TValue> val)
    {
      return Remove(val.Key, val.Value);
    }
    public bool Remove(TKey key)
    {
      return _dict.Remove(key);
    }
    public bool Remove(TKey x, TValue y)
    {
      List<TValue> ret = null;
      if (_dict.TryGetValue(x, out ret))
      {
        ret.Remove(y);

        if (ret.Count == 0)
        {
          _dict.Remove(x);
        }

        return true;
      }
      return false;
    }
    public void Clear()
    {
      _dict.Clear();
    }
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
      foreach (var di in _dict)
      {
        foreach (var li in di.Value)
        {
          yield return new KeyValuePair<TKey, TValue>(di.Key, li);
        }
      }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      foreach (var di in _dict)
      {
        foreach (var li in di.Value)
        {
          yield return new KeyValuePair<TKey, TValue>(di.Key, li);
        }
      }
    }
  }

  public class FileLoc
  {
    /// FileLoc represents a virtual file location on disk, embed, or web
    //The name here has to be unique or it will cause conflicts.

    #region Public: Members
    private int c_newline = '\n';
    private int c_EOF = -1;//I guess, -1 in .net

    public static FileLoc Generated = new FileLoc("<generated>", FileStorage.Generated);
    public FileStorage FileStorage { get; set; } = FileStorage.Disk;
    public string RawPath { get; set; } = "";
    public FileLoc Clone()
    {
      FileLoc ret = new FileLoc();
      ret.RawPath = this.RawPath;
      ret.FileStorage = this.FileStorage;
      return ret;
    }
    public string FileName
    {
      get
      {
        string fn = System.IO.Path.GetFileName(this.QualifiedPath);
        return fn;
      }
    }
    public string QualifiedPath
    {
      //Returns the full path with base storage location (disk/embed..)
      get
      {
        string path = RawPath;
        if (FileStorage == FileStorage.Embedded)
        {
          path = Gu.EmbeddedDataPath + path;
        }
        else if (FileStorage == FileStorage.Generated)
        {
          //noop
        }
        else if (FileStorage == FileStorage.Disk)
        {
          //noop
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
        return path;
      }
    }
    public bool ExistsOnDisk()
    {
      return File.Exists(WorkspacePath);
    }
    public bool Exists
    {
      get
      {
        if (FileStorage == FileStorage.Embedded)
        {
          bool exist = Assembly.GetExecutingAssembly().GetManifestResourceNames().Contains(QualifiedPath);
          return exist;
        }
        else if (FileStorage == FileStorage.Disk)
        {
          return File.Exists(QualifiedPath);
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
        return false;
      }
    }

    #endregion
    #region Public: Methods

    public FileLoc() { }
    public FileLoc(string directory, string filename, FileStorage storage)
    {
      RawPath = System.IO.Path.Combine(directory, filename);
      FileStorage = storage;
    }
    public FileLoc(string path, FileStorage storage)
    {
      RawPath = path;
      FileStorage = storage;
    }
    public override bool Equals(object? obj)
    {
      FileLoc other = obj as FileLoc;
      if (other != null)
      {
        return other.RawPath.Equals(RawPath) && other.FileStorage.Equals(FileStorage);
      }
      else
      {
        // other was not a file loc I guess
        Gu.BRThrowNotImplementedException();
      }
      return false;
    }
    public void AssertExists()
    {
      if (!Exists)
      {
        throw new Exception("File " + QualifiedPath + " does not exist.");
      }
    }
    public byte[] GetBytes()
    {
      byte[] bytes = null;
      using (var fs = OpenRead())
      {
        if (fs != null)
        {
          byte[] buffer = new byte[16 * 1024];
          using (MemoryStream ms = new MemoryStream())
          {
            int read = 0;
            while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
              ms.Write(buffer, 0, read);
            }
            bytes = ms.ToArray();
          }
        }
        else
        {
          Gu.Log.Error("Could not find or open the file '" + this.QualifiedPath + "'");
          Gu.DebugBreak();
        }
      }
      return bytes;
    }
    public Stream? OpenRead()
    {
      if (FileStorage == FileStorage.Embedded)
      {
        return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(QualifiedPath);
      }
      else if (FileStorage == FileStorage.Disk)
      {
        return File.OpenRead(QualifiedPath);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return null;
    }
    private int CountStreamChars(StreamReader s, List<int> chars)
    {
      Gu.Assert(chars != null);
      Gu.Assert(chars.Count > 0);
      //Count all characters in stream from beginning of stream.
      long p = s.BaseStream.Position;
      int count = 1;

      s.BaseStream.Position = 0;//Seek(0, SeekOrigin.Begin);
      s.DiscardBufferedData();

      int symbol = s.Peek();
      while (symbol != c_EOF)
      {
        symbol = s.Read();
        foreach (var n in chars)
        {
          if (symbol == n)
          {
            count++;
          }
        }
      }

      //Reset Position
      //This is apparently what needs to be done. There's some bufering, and Seek() is problematic.
      s.BaseStream.Position = p;
      s.DiscardBufferedData();

      return count;
    }
    private int ReadLine(StreamReader stream, out string ss)
    {
      //Read a line, return the terminating symbol.
      StringBuilder sb = new StringBuilder();
      int symbol = stream.Peek();
      while (symbol != c_EOF)
      {
        symbol = stream.Read();
        if (symbol == c_newline)
        {
          break;
        }
        else
        {
          sb.Append((char)symbol);
        }
      }
      ss = sb.ToString();
      return symbol;
    }
    public string[] ReadAllLines()
    {
      //Read all lines with the embedded file, disk file, net file .. et.
      string[] ret = null;
      Gu.Assert(this.Exists);
      int lines = 0;
      using (var s = this.OpenRead())
      {
        if (s != null)
        {
          using (var stream = new StreamReader(s))
          {
            lines = CountStreamChars(stream, new List<int>() { c_newline });

            ret = new string[lines];
            int nline = 0;
            string line = "";
            for (int xxx = 0; Gu.WhileTrueGuard(xxx, Gu.c_intMaxWhileTrueLoopLONG); xxx++)
            {
              var delim = ReadLine(stream, out line);
              Gu.Assert(nline < lines);
              ret[nline] = line;
              if (delim == c_EOF)
              {
                break;
              }
              nline++;
            }
          }
        }
      }
      return ret;
    }
    public byte[] ReadAllData()
    {
      //Read all lines with the embedded file, disk file, net file .. et.
      Gu.Assert(this.Exists);
      byte[] bytes = null;
      using (var s = this.OpenRead())
      {
        if (s != null)
        {
          using (var stream = new StreamReader(s))
          {
            //Debug this..make sure lenght is correct
            var len = (int)s.Length;
            if (len > 0)
            {
              bytes = new byte[len];
              s.Read(bytes, 0, len);
            }
          }
        }
      }
      return bytes;
    }
    public Stream? OpenWrite()
    {
      if (FileStorage == FileStorage.Disk)
      {
        return File.OpenWrite(QualifiedPath);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return null;
    }
    public class EqualityComparer : IEqualityComparer<FileLoc>
    {
      //Use for Dictionary<>
      public bool Equals(FileLoc a, FileLoc b)
      {
        return a.Equals(b);
      }

      public int GetHashCode(FileLoc a)
      {
        return a.QualifiedPath.GetHashCode();
      }
    }
    public class SortedComparer : IComparer<FileLoc>
    {
      //Use for SortedDictionary()
      public int Compare(FileLoc? a, FileLoc? b)
      {
        return a.QualifiedPath.CompareTo(b.QualifiedPath);
      }

      public int GetHashCode(FileLoc a)
      {
        return a.QualifiedPath.GetHashCode();
      }
    }
    public void Create()
    {
      if (FileStorage == FileStorage.Disk)
      {
        using (var fs = System.IO.File.Create(QualifiedPath))
        {
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    public string WorkspacePath
    {
      get
      {
        var p = System.IO.Path.Combine(Gu.WorkspaceDataPath, this.RawPath);
        return p;
      }
    }
    public DateTime GetLastWriteTime(bool If_Is_Embedded_Then_Check_Data_Directory = true)
    {
      //Returns : the Modified Time of the 
      //Disk file resource
      //Embedded file's original resource, if present
      //DateTime.MinVal if file was Generated
      DateTime wt = DateTime.MinValue;
      if (FileStorage == FileStorage.Embedded)
      {
        if (If_Is_Embedded_Then_Check_Data_Directory)
        {
          //Embedded files don't hjave mod time, instead try to find the file pre-embed and use that.
          //Otherwise we return minvalue.
          if (System.IO.File.Exists(WorkspacePath))
          {
            wt = System.IO.File.GetLastWriteTime(WorkspacePath);
          }
        }
      }
      else if (FileStorage == FileStorage.Disk)
      {
        wt = System.IO.File.GetLastWriteTime(this.QualifiedPath);
      }
      //Net - We'd get it over th einternet
      //Generated - has no mod time
      return wt;
    }
    public void Serialize(BinaryWriter bw)
    {
      bw.Write((Int32)FileStorage);
      bw.Write((string)RawPath);
    }
    public void Deserialize(BinaryReader br)
    {
      FileStorage = (FileStorage)br.ReadInt32();
      RawPath = br.ReadString();
    }

    #endregion
  }

  public class Minimax<T>
  {
    public T Min;
    public T Max;
    public Minimax(T min, T max)
    {
      Min = min;
      Max = max;
    }

  }

  public class StringUtil
  {
    public static bool IsNotEmpty(string s)
    {
      return !IsEmpty(s);
    }
    public static bool IsEmpty(string s)
    {
      return String.IsNullOrEmpty(s);
    }
    public static bool Equals(string a, string b)
    {
      return a.CompareTo(b) == 0;
    }
    public static string FormatPrec(float x, int prec)
    {
      return String.Format("{0:0." + new string('0', prec) + "}", x);
    }
    public static string FormatPrec(double x, int prec)
    {
      return String.Format("{0:0." + new string('0', prec) + "}", x);
    }
    public static string Indent(string x, int count, char indentChar = ' ')
    {
      //indent newlines
      if (StringUtil.IsEmpty(x))
      {
        return x;
      }
      var spaces = new string(indentChar, count);
      var y = spaces + x.Replace("\n", "\n" + spaces);
      y = y.Remove(y.Length - count, count);
      return y;
    }

    public static int[] SlidingDiff(string strlast, string strcur, int window = 16)
    {
      //The purpose of this is to test for subtle changes to a string, say if we replace a number in the debug. So we don't need an entire LCS matrix, or remove all glyphs.
      //The greater the size of window() the less add/remove there will be, at the cost of algorithm time.

      // long msa = Gu.Microseconds();

      //int
      // - = remove char x from last
      // + = add char x from cur
      //abcad fxxy = ls, 
      //ab  def    = cu  nc 0,1 remove 2,3 add 3, nc 4, remove 5,6,7
      //0022010
      //xabdxf = ls, 
      // abdefgh = cu   remove 1-1, same 1-3, replace 4-4, same 5-5, add 6-7
      //20003011
      //same=0, add = 1, remove = 2, replace = 3
      //if there is a diff, find next like char in last, up to maxchange

      //we use RLE, so we need to double the potential changes.
      int maxlen = Math.Max(strlast.Length, strcur.Length) * 2 + 2;
      var ret = new int[maxlen];
      int ri = 0;

      int no_c = 0;
      int add_c = 1;
      int rem_c = 2;

      int li = 0;
      int ci = 0;
      int ca;
      int cb;

      bool exit = false;
      for (int xx = 0; Gu.WhileTrueGuard(xx, Gu.c_intMaxWhileTrueLoop); xx++)
      {
        //Find runs
        int change_none = 0;
        for (; ci < strcur.Length && li < strlast.Length; ci++, li++)
        {
          if (strcur[ci] == strlast[li])
          {
            change_none++;
          }
          else
          {
            break;
          }
        }
        if (change_none > 0)
        {
          ret[ri++] = no_c;
          ret[ri++] = change_none;
        }
        if (ci >= strcur.Length && li >= strlast.Length)
        {
          break;
        }
        else if (ci >= strcur.Length)
        {
          ret[ri++] = rem_c;
          ret[ri++] = strlast.Length - li;
          break;
        }
        else if (li >= strlast.Length)
        {
          ret[ri++] = add_c;
          ret[ri++] = strcur.Length - ci;
          break;
        }

        //use a sliding window of maxchange to find next thing
        //number of chars that are good to go in the best lookahead.
        //instead of distance I think the heuristic should be "lookahead similarity"
        int ci_c = -1;
        int li_c = -1;
        int best_lookahead = -1;
        for (int li2 = li; li2 < strlast.Length && (li2 - li) < window; li2++)
        {
          for (int ci2 = ci; ci2 < strcur.Length && (ci2 - ci) < window; ci2++)
          {
            char a = strlast[li2];
            char b = strcur[ci2];
            if (a == b)
            {
              int cur_lookahead = 0;
              for (int li_look = li2, ci_look = ci2;
              ((li_look < strlast.Length) && ((li_look - li2) < window)) &&
              ((ci_look < strcur.Length) && ((ci_look - ci2) < window));
              li_look++, ci_look++)
              {
                if (strlast[li_look] == strcur[ci_look])
                {
                  cur_lookahead++;
                }
                else
                {
                  break;
                }
              }

              if (cur_lookahead > best_lookahead)
              {
                best_lookahead = cur_lookahead;
                li_c = li2; //set the values to the next best match, based on the best lookahead within our window
                ci_c = ci2;
                if (li_c < 0) li_c = 0;
                if (ci_c < 0) ci_c = 0;
              }
            }

          }
        }
        if (ci_c == -1)
        {
          //drop
          ret[ri++] = rem_c;
          ret[ri++] = strlast.Length - ci;
          ret[ri++] = add_c;
          ret[ri++] = strcur.Length - ci;
          break;
        }
        else
        {
          ret[ri++] = rem_c;
          ret[ri++] = li_c - li;
          ret[ri++] = add_c;
          ret[ri++] = ci_c - ci;
          li = li_c;
          ci = ci_c;
        }

      }
      Array.Resize<int>(ref ret, ri);

      // //***TESTING
      // string test = "";
      // int ilast = 0;
      // int icur = 0;
      // int nadd = 0;
      // int nrem = 0;
      // for (var xi = 0; xi < ret.Length; xi += 2)
      // {
      //   if (ret[xi + 0] == 0)
      //   {
      //     int ct = ret[xi + 1];
      //     test += strlast.Substring(ilast, ct);
      //     ilast += ct;
      //     icur += ct;
      //   }
      //   else if (ret[xi + 0] == 1)
      //   {
      //     int ct = ret[xi + 1];
      //     test += strcur.Substring(icur, ct);
      //     icur += ct;
      //     nadd += ct;
      //   }
      //   else if (ret[xi + 0] == 2)
      //   {
      //     int ct = ret[xi + 1];
      //     ilast += ct;
      //     nrem += ct;
      //   }
      // }
      // bool didWork = StringUtil.Equals(strcur, test);
      // long msb = Gu.Microseconds () - msa;


      return ret;
    }
  }

  public abstract class Cloneable<T>
  {
    public abstract T Clone(bool shallow = true);
  }
  public class MutableState
  {
    public bool Modified { get; protected set; } = false;
    public void SetModified()
    {
      Modified = true;
    }
  }
  public class DataBlock : MutableState
  {
    #region Members

    private static UInt64 s_dataBlockIdGen = 1;//This is a problem, what if we load a model again?
    private static UInt64 s_dataBlockTypeIdGen = 1;
    private string _name = "<Unnamed>";
    private UInt64 _typeId = 1; // When Clone() is called this gets duplicated
    private UInt64 _uniqueId = 0; //Never duplicated, unique for all objs

    public string Name { get { return _name; } set { _name = value; SetModified(); } }
    public UInt64 UniqueID { get { return _uniqueId; } private set { _uniqueId = value; SetModified(); } }
    //Type ID is used to instance mesh objects.
    public UInt64 TypeID { get { return _typeId; } private set { _typeId = value; SetModified(); } }

    #endregion
    #region Public Static: Methods

    public static UInt64 GetNewId()
    {
      return s_dataBlockIdGen++;
    }
    public static UInt64 GetNewType()
    {
      return s_dataBlockTypeIdGen++;
    }

    #endregion
    #region Methods

    protected DataBlock() { } //clone ctor
    public DataBlock(string name)
    {
      _name = name;
      _uniqueId = GetNewId();
      _typeId = GetNewType();
      SetModified();
    }
    public DataBlock Clone()
    {
      var d = new DataBlock();
      Copy(d);
      return d;
    }
    protected void Copy(DataBlock d)
    {
      d._name = this._name;
      d._typeId = this._typeId;
      d._uniqueId = s_dataBlockIdGen++;
      d.SetModified();
    }
    public virtual void MakeUnique()
    {
      _typeId = GetNewType();
      SetModified();
    }
    public void Serialize(BinaryWriter br)
    {
      br.Write(_name);
      br.Write(_typeId);
    }
    public void Deserialize(BinaryReader br)
    {
      _name = br.ReadString();
      _typeId = br.ReadUInt64();
    }

    #endregion
  }
  public class ModifiedList<T> : List<T>
  {
    public bool Modified { get; set; } = false;

    public ModifiedList() : base()
    {
    }
    public ModifiedList(IEnumerable<T> collection) : base(collection)
    {
      Modified = true;
    }
    public ModifiedList(int capacity) : base(capacity)
    {
      Modified = true;
    }
    public new T this[int index]
    {
      get
      {
        return base[index];
      }
      set
      {
        base[index] = value;
        Modified = true;
      }
    }
    public new void Add(T item)
    {
      base.Add(item);
      Modified = true;
    }
    public new void AddRange(IEnumerable<T> collection)
    {
      base.AddRange(collection);
      Modified = true;
    }
    public new void Clear()
    {
      base.Clear();
      Modified = true;
    }
    public new int Capacity
    {
      get
      {
        return base.Capacity;
      }
      set
      {
        base.Capacity = value;
        Modified = true;
      }
    }
    public new void Insert(int index, T item)
    {
      base.Insert(index, item);
      Modified = true;
    }
    public new void InsertRange(int index, IEnumerable<T> collection)
    {
      base.InsertRange(index, collection);
      Modified = true;
    }
    public bool Remove(T item)
    {
      return base.Remove(item);
      Modified = true;
    }
    public int RemoveAll(Predicate<T> match)
    {
      return base.RemoveAll(match);
      Modified = true;
    }
    public void RemoveAt(int index)
    {
      base.RemoveAt(index);
      Modified = true;
    }
    public void RemoveRange(int index, int count)
    {
      base.RemoveRange(index, count);
      Modified = true;
    }
    public void Reverse()
    {
      base.Reverse();
      Modified = true;
    }
    public void Reverse(int index, int count)
    {
      base.Reverse(index, count);
      Modified = true;
    }
    public void Sort(Comparison<T> comparison)
    {
      base.Sort(comparison);
      Modified = true;
    }
    public void Sort(int index, int count, IComparer<T>? comparer)
    {
      base.Sort(index, count, comparer);
      Modified = true;
    }
    public void Sort()
    {
      base.Sort();
      Modified = true;
    }
    public void Sort(IComparer<T>? comparer)
    {
      base.Sort(comparer);
      Modified = true;
    }
    public void TrimExcess()
    {
      base.TrimExcess();
      Modified = true;
    }

  }

  public class DeltaTimer : Cloneable<DeltaTimer>
  {

    public double Frequency { get; private set; } = 0;
    public double Time { get; private set; } = 0;
    public ActionState State { get; private set; } = ActionState.Stop;
    public ActionRepeat Repeat { get; set; } = ActionRepeat.DoNotRepeat;
    public Action Action { get; set; } = null;

    private DeltaTimer() { }//clone
    public DeltaTimer(double frequency_seconds, ActionRepeat repeat, ActionState start, Action? act = null)
    {
      Frequency = frequency_seconds;
      Repeat = repeat;
      Action = act;
      if (start == ActionState.Run)
      {
        Start();
      }
    }
    public int Update(double dt, Action? act = null)
    {
      //Putting action here makes it more sense, since it keeps the action code within the update code.
      //Returns the number of times this timer fired, and executes optional action
      int fires = 0;
      if (State != ActionState.Stop)
      {
        Time += dt;
        while (Time > Frequency)
        {
          Time -= Frequency;
          Action?.Invoke();
          act?.Invoke();
          fires++;
        }
      }
      return fires;
    }
    public void Start()
    {
      State = ActionState.Run;
    }
    public void Stop()
    {
      State = ActionState.Stop;
    }
    public override DeltaTimer Clone(bool shallow = true)
    {
      DeltaTimer d = new DeltaTimer();
      d.Frequency = this.Frequency;
      d.Time = this.Time;
      d.State = this.State;
      d.Action = this.Action;
      d.Repeat = this.Repeat;

      return d;
    }
  }

  public class ByteParser
  {
    //Parse utilities
    public static bool IsWS(char c)
    {
      return c == ' ' || c == '\n' || c == '\r' || c == '\t';
    }
    public static bool IsDelim(char c)
    {
      return IsWS(c) || c == '{' || c == '}' || c == '[' || c == ']' || c == '\"' || c == '\'';
    }
    public static char PeekChar(byte[] data, int index)
    {
      byte b = data[index];
      char c = (char)b;
      return c;
    }
    public static char GetChar(byte[] data, ref int index)
    {
      byte b = data[index];
      char c = (char)b;
      index++;
      return c;
    }
    public static bool IsDigit(char c)
    {
      return c == '0' || c == '1' || c == '2' || c == '3' || c == '4' || c == '5' || c == '6' || c == '7' || c == '8' || c == '9';
    }
    public static bool IsAlpha(char c)
    {
      return
      c == 'a' || c == 'b' || c == 'c' || c == 'd' || c == 'e' || c == 'f' || c == 'g' || c == 'h' || c == 'i' || c == 'j' || c == 'k' ||
      c == 'l' || c == 'm' || c == 'n' || c == 'o' || c == 'p' || c == 'q' || c == 'r' || c == 's' || c == 't' || c == 'u' || c == 'v' || c == 'w' ||
      c == 'x' || c == 'y' || c == 'z' ||
      c == 'A' || c == 'B' || c == 'C' || c == 'D' || c == 'E' || c == 'F' || c == 'G' || c == 'H' || c == 'I' || c == 'J' || c == 'K' ||
      c == 'L' || c == 'M' || c == 'N' || c == 'O' || c == 'P' || c == 'Q' || c == 'R' || c == 'S' || c == 'T' || c == 'U' || c == 'V' || c == 'W' ||
      c == 'X' || c == 'Y' || c == 'Z';
    }
    public static bool? ParseBool(byte[] data, ref int line, ref int col, ref int index)
    {
      string tok = ParseAlphaToken(data, ref line, ref col, ref index);
      if (tok.ToLower() == "true")
      {
        return true;
      }
      if (tok.ToLower() == "false")
      {
        return false;
      }
      return null;
    }
    public static string ParseIdentifier(byte[] data, ref int line, ref int col, ref int index)
    {
      //parse [a-zA-Z0-9_]
      string tok = "";
      bool first = true;
      while (index < data.Length)
      {
        char c = PeekChar(data, index);

        if (IsAlpha(c) || (IsDigit(c) && first == false) || (c == '_') || (c == '-' && first == false))
        {
          tok += c;
          index++;
          first = false;
        }
        else if ((IsDigit(c) && first == true))
        {
          throw new Exception("Invalid identifier [0-9]..");
        }
        else if ((c == '-' && first == true))
        {
          throw new Exception("Invalid identifier [-]..");
        }
        else
        {
          break;
        }
      }
      return tok;
    }
    public static string ParseAlphaToken(byte[] data, ref int line, ref int col, ref int index)
    {
      //parse alphanumeric char to a delimiter
      string tok = "";
      while (index < data.Length)
      {
        char c = PeekChar(data, index);

        if (IsAlpha(c)) //TODO: locale.. . = ,
        {
          tok += c;
          index++;
        }
        else
        {
          break;
        }
      }
      return tok;
    }
    public static double? ParseDouble(byte[] data, ref int line, ref int col, ref int index)
    {
      string tok = "";

      //Back up the previous character.
      bool dotted = false;
      while (index < data.Length)
      {
        char c = PeekChar(data, index);

        if (Char.IsDigit(c) || (c == '.' && !dotted)) //TODO: locale.. . = ,
        {
          tok += c;
          if (c == '.')
          {
            dotted = true;
          }
          if (c == '\n')
          {
            line++;
            col = 0;
          }
          col++;
          index++;
        }
        else
        {
          break;
        }
      }
      double d;
      if (!Double.TryParse(tok, out d))
      {
        return null;
      }
      return d;
    }
    public static void EatTo(byte[] data, ref int line, ref int col, ref string eated, ref int index, char delim = '\n', char? delimprev = null)
    {
      //Eat to .. including the delimiter.
      //delimprev - if defined, prev dlim == /* .. 
      char clast = '\0';
      eated = "";
      while (index < data.Length)
      {
        char c = GetChar(data, ref index);
        if (c == '\n')
        {
          line++;
          col = 0;
        }
        col++;

        if (delimprev != null && (clast == delimprev && c == delim))
        {
          break;
        }
        else if (c == delim)
        {
          break;
        }
        else
        {
          eated += c;
        }
      }
    }
    public static bool ParseFunc_NO_ARG_PARENS(string token, out string funcname, out List<string> parms, bool allowDashesInName)
    {
      parms = new List<string>();
      funcname = "";
      token = token.Trim().Trim(';');

      var vals = token.Split('(');
      if (vals.Length == 2)
      {
        funcname = vals[0].Trim();
        if (!CheckValidId(funcname, allowDashesInName))
        {
          return false;
        }

        vals[1] = vals[1].Trim(')');
        parms = vals[1].Split(',').ToList();
        for (int i = 0; i < parms.Count; i++)
        {
          parms[i] = parms[i].Trim();
        }
        return true;
      }
      return false;
    }
    public static bool CheckValidId(string tok, bool allowDash)
    {
      //check that token is a valid identifier.
      if (tok.Length == 0)
      {
        return false;
      }
      if (IsDigit(tok[0]))
      {
        return false;
      }
      if (tok[0] == '-')
      {
        return false;
      }
      for (int i = 0; i < tok.Length; i++)
      {
        char c = tok[i];
        if (IsAlpha(c) || c == '_' || (allowDash && c == '-'))
        {
        }
        else
        {
          return false;
        }
      }
      return true;
    }
  }

  public abstract class ByteForByteFile
  {
    //text file we parse each byte

    #region Members

    protected List<string> _errors = new List<string>();
    protected List<string> _warnings = new List<string>();
    protected FileLoc _fileLoc = null;
    protected int _line = 1;//current line
    protected int _col = 1;//current col
    protected string _tok = ""; //cur token
    protected char _clast = ' ';//last char
    protected byte[] _data = null;
    protected int _idx = 0;//index of _data

    protected abstract bool DoParse(char c, string eated, ref bool requestBreak);//return true if the given character was handled.

    protected virtual void BeforeParse() { }
    protected virtual void AfterParse() { }

    protected bool EatWhiteSpace = true;
    protected bool EatLineComments = true;
    protected bool EatStarComments = true;

    #endregion
    #region Public methods

    public ByteForByteFile(FileLoc loc)
    {
      _fileLoc = loc;
    }
    public void DebugWriteFileToPos()
    {
      //Just because.. vscode can't show blocks of text afaik
#if DEBUG
      System.IO.File.WriteAllBytes(System.IO.Path.Combine(Gu.LocalTmpPath, _fileLoc.RawPath + "_debug.txt"), _data.Take(_idx).ToArray());
#endif
    }
    public bool Load()
    {
      Gu.Assert(_fileLoc.Exists);
      var sw = new Stopwatch();
      sw.Start();

      _data = _fileLoc.ReadAllData();

      BeforeParse();
      ParseLoop();
      AfterParse();

      Gu.Log.Debug($"Parse: {_data.Length}B took: {sw.ElapsedMilliseconds}ms");
      return true;
    }
    public bool PrintErrors()
    {
      //Return true if there were errors
      if (_errors != null && _errors.Count > 0)
      {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("");
        sb.AppendLine($"---{_fileLoc.RawPath} Errors---");
        foreach (var e in _errors)
        {
          sb.AppendLine($"  {e}");
        }
        Gu.Log.Error(sb.ToString());
        return true;
      }
      if (_warnings != null && _warnings.Count > 0)
      {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("");
        sb.AppendLine($"---{_fileLoc.RawPath} Warnings---");
        foreach (var e in _warnings)
        {
          sb.AppendLine($"  {e}");
        }
        Gu.Log.Error(sb.ToString());
      }

      return false;
    }

    #endregion
    #region Protected: Methods

    private void ParseLoop()
    {
      string eated = "";

      bool requestBreak = false;
      _idx = 0;
      while (_idx < _data.Length)
      {
        char c = ByteParser.GetChar(_data, ref _idx);
        if (c == '\n')
        {
          _line++;
          _col = 0;
        }
        _col++;

        if (EatWhiteSpace && ByteParser.IsWS(c))
        {
          //eat
        }
        else if (EatLineComments && ((c == '/') && (_clast == '/')))
        {
          ByteParser.EatTo(_data, ref _line, ref _col, ref eated, ref _idx, '\n');
        }
        else if (EatStarComments && ((c == '*') && (_clast == '/')))
        {
          ByteParser.EatTo(_data, ref _line, ref _col, ref eated, ref _idx, '*', '\\');
        }
        else if (DoParse(c, eated, ref requestBreak))
        {
          //User parse routine
        }
        else
        {
          _tok += c;
        }

        if (requestBreak)
        {
          break;
        }

        _clast = c;
      }
    }
    protected void UnexpectedError(char c)
    {
      Gu.DebugBreak();
      Error(_fileLoc, _line, _col, $"Unexpected '{c}'");
    }
    protected void Error(FileLoc file, int line, int col, string s, bool warning = false)
    {
      List<string> msg = warning ? _warnings : _errors;

      if (line == -1 && file == null)
      {
        msg.Add($"{_fileLoc}: {s}");
      }
      else if (line == -1 && file != null)
      {
        msg.Add($"{file.RawPath}: {s}");
      }
      else
      {
        msg.Add($"{file.RawPath}:{line}:{col}: {s}");
      }
    }
    protected void Warning(FileLoc file, int line, int col, string s)
    {
      Error(file, line, col, s, true);
    }
    #endregion
  }


  public interface ISerializeBinary
  {
    public abstract void Serialize(BinaryWriter br);
    public abstract void Deserialize(BinaryReader br);
  }
  //We will probably delete the range stuff, just keeping it here for now.
  public interface IRangeItem
  {
    public ushort Min { get; }
    public ushort Max { get; }
  }
  // public class RangeList
  // {
  //   //Range list for [max,min] IRangeItem.  
  //   // Constraint: Max!=Min, 
  //   // elements max/min can *coincide*
  //   // a--a b-------bc--c   d------d <example
  //   // a----ab-----b < ok
  //   // aa < bad
  //   // ab < ok

  //   public const int c_iNotFound = -1;
  //   private List<IRangeItem>? _items = new List<IRangeItem>();
  //   public RangeList()
  //   {
  //   }
  //   public List<IRangeItem> GetCoincidentRanges(IRangeItem ri)
  //   {
  //     //return all coinciding ranges
  //     List<IRangeItem> ret = null;

  //     if (_items.Count == 0)
  //     {
  //       return new List<IRangeItem>();
  //     }

  //     Gu.Assert(ri.Min != ri.Max);
  //     FindClosest_LeftGuaranteed(ri.Min, out int min_left, out int min_right);
  //     FindClosest_LeftGuaranteed(ri.Max, out int max_left, out int max_right);

  //     Gu.Assert(max_left >= min_left);
  //     if (_items[min_left].Max >= ri.Min && _items[max_left].Min <= ri.Max)
  //     {
  //       ret = _items.GetRange(min_left, max_left - min_left);
  //     }

  //     return ret;
  //   }
  //   public void Add(IRangeItem e)
  //   {
  //     Gu.Assert(e != null);
  //     int tindl = 0, bindl = 0, tindr = 0, bindr = 0;

  //     //Max=min would mess this whole thing up  could return infinite elements
  //     if (e.Max == e.Min)
  //     {
  //       Gu.BRThrowException("Beam max must not equal min.");
  //     }
  //     Gu.Assert(_items != null);

  //     if (_items.Count == 0)
  //     {
  //       _items.Add(e);
  //     }
  //     else if (!FindClosest_LeftGuaranteed(e.Max, out tindl, out tindr) && !FindClosest_LeftGuaranteed(e.Min, out bindl, out bindr))
  //     {
  //       //bind is guaranteed in the array > 0
  //       // we sort by bot value.
  //       _items.Insert(bindl, e);
  //     }
  //     else
  //     {
  //       Gu.Log.Error("edge was coincident");
  //       Gu.DebugBreak();
  //     }
  //   }
  //   public bool Find(ushort value, out IRangeItem left, out IRangeItem right)
  //   {
  //     return Find(value, out left, out right, out int l, out int r);
  //   }
  //   public bool Find(ushort value, out IRangeItem left, out IRangeItem right, out int closest_left, out int closest_right)
  //   {
  //     //Returns 1 or 2 matching range items.
  //     left = right = null;
  //     var ret = false;

  //     //Note this finds the CLOSEST value
  //     if (FindClosest_LeftGuaranteed(value, out closest_left, out closest_right))
  //     {
  //       if (closest_left != c_iNotFound)
  //       {
  //         if (left == null)
  //         {
  //           if (_items[closest_left].Min <= value && _items[closest_left].Max >= value)
  //           {
  //             left = _items[closest_left];
  //             ret = true;
  //           }
  //         }
  //         else
  //         {
  //           Gu.Assert(left == _items[closest_left]);//Extra check - the left/right order would always be the same, and the left/right beam would always be the same.
  //         }
  //       }
  //       if (closest_right != c_iNotFound)
  //       {
  //         if (right == null)
  //         {
  //           if (_items[closest_right].Min <= value && _items[closest_right].Max >= value)
  //           {
  //             right = _items[closest_right];
  //             ret = true;
  //           }
  //         }
  //         else
  //         {
  //           Gu.Assert(right == _items[closest_right]);//Extra check - the left/right order would always be the same, and the left/right beam would always be the same.
  //         }
  //       }
  //     }

  //     return ret;
  //   }
  //   public bool FindClosest_LeftGuaranteed(ushort h, out int left, out int right)
  //   {
  //     //Get closest left and right indexes to a given value, one may be null if they do not coincide
  //     // The closest value is for valid index for array insertion
  //     // Return TRUE if h coincides with a range (left || right != c_iNotFound).
  //     // *** left is guaranteed to be in the bounds of the array if the array size >0.
  //     //this may return 2 values since the range values may be equal
  //     left = right = c_iNotFound;
  //     if (_items == null || _items.Count == 0)
  //     {
  //       return false;
  //     }

  //     bool ret = false;
  //     int pos = _items.Count / 2;
  //     int range = _items.Count / 2;

  //     IRangeItem el = null, er = null;

  //     for (int xi = 0; Gu.WhileTrueGuard(xi, Gu.c_intMaxWhileTrueLoopBinarySearch); xi++)
  //     {
  //       left = pos;
  //       right = left + 1;

  //       Gu.Assert(left < _items.Count);

  //       el = _items[left];
  //       if (right < _items.Count)
  //       {
  //         er = _items[right];
  //       }
  //       else
  //       {
  //         right = c_iNotFound;
  //       }

  //       if (el.Min <= h && el.Max >= h)
  //       {
  //         //in exclusive range
  //         ret = true;
  //         break;
  //       }

  //       if (er != null && er.Min <= h && er.Max > h)//note > 
  //       {
  //         el = er;
  //         er = null;
  //         left = right;
  //         right = c_iNotFound;
  //         ret = true;
  //         break;
  //       }

  //       if (range <= 1)
  //       {
  //         //no hit
  //         break;
  //       }

  //       //continue divide
  //       range /= 2;
  //       if (h < el.Min)
  //       {
  //         pos = pos - range;
  //       }
  //       else if (h > el.Max)
  //       {
  //         pos = pos + range;
  //       }

  //       if (pos < _items.Count || pos > _items.Count)
  //       {
  //         //no hit
  //         break;
  //       }
  //     }

  //     //l must be a count
  //     Gu.Assert(left <= _items.Count);
  //     if (right != c_iNotFound)
  //     {
  //       Gu.Assert(right <= _items.Count);
  //     }

  //     return ret;
  //   }
  // }//cl
  // public class RangeList
  // {
  //     public List<IRangeItem> Children = null;// new List<RangeNode>();//sort by min<=min

  //   private class RangeNode
  //   {
  //     public ushort Min = 0;
  //     public ushort Max = 0;
  //     public IRangeItem Item = null;
  //     public RangeNode(ushort min, ushort max)
  //     {
  //       Min = min;
  //       Max = max;
  //     }
  //   }
  //   public int Count {get; private set;}= 0;
  //   //Something like a range K-D tree 
  //   //New list for beams, with entire beam being min/max
  //   //Range list for [max,min] IRangeItem, Overlapping
  //   // Constraint: a is not contained within B
  //   // a---b-a----c--b--c   d------d <example

  //   public const int c_iNotFound = -1;
  //   //private List<IRangeItem>? _items = new List<IRangeItem>();
  //   //private RangeNode _root = null;
  //   //private Dictionary<ushort,ushort[]> _index = new Dictionary<ushort, ushort[]>();// O --> oo
  //   private RangeNode _root=null;
  //   private ushort _maxRange = 0;
  //   public RangeList(ushort range)
  //   {
  //     _maxRange = range;
  //     _root = new RangeNode(range);
  //   }
  //   public void Add(IRangeItem ri, RangeNode? root=null)
  //   {
  //     Count++;
  //     if(root==null){
  //       root = _root;
  //     }
  //     if(ri.Max)

  //     Gu.Assert(_items.Count < ushort.MaxValue-2);
  //     _items.Add(ri);
  //     ushort idx = (ushort)_items.Count;
  //     if(_root ==0){

  //     }
  //   }
  //   private void Add(IRangeItem ri){

  //   }
  //   public List<IRangeItem> GetRanges(ushort s)
  //   {

  //   }
  //   public List<IRangeItem> GetRanges(IRangeItem ri)
  //   {

  //   }

  //   public List<IRangeItem> GetCoincidentRanges(IRangeItem ri)
  //   {
  //     //return all coinciding ranges
  //     List<IRangeItem> ret = null;

  //     if (_items.Count == 0)
  //     {
  //       return new List<IRangeItem>();
  //     }

  //     Gu.Assert(ri.Min != ri.Max);
  //     FindClosest_LeftGuaranteed(ri.Min, out int min_left, out int min_right);
  //     FindClosest_LeftGuaranteed(ri.Max, out int max_left, out int max_right);

  //     Gu.Assert(max_left >= min_left);
  //     if (_items[min_left].Max >= ri.Min && _items[max_left].Min <= ri.Max)
  //     {
  //       ret = _items.GetRange(min_left, max_left - min_left);
  //     }

  //     return ret;
  //   }
  //   public void Add(IRangeItem e)
  //   {
  //     Gu.Assert(e != null);
  //     int tindl = 0, bindl = 0, tindr = 0, bindr = 0;

  //     //Max=min would mess this whole thing up  could return infinite elements
  //     if (e.Max == e.Min)
  //     {
  //       Gu.BRThrowException("Beam max must not equal min.");
  //     }
  //     Gu.Assert(_items != null);

  //     if (_items.Count == 0)
  //     {
  //       _items.Add(e);
  //     }
  //     else if (!FindClosest_LeftGuaranteed(e.Max, out tindl, out tindr) && !FindClosest_LeftGuaranteed(e.Min, out bindl, out bindr))
  //     {
  //       //bind is guaranteed in the array > 0
  //       // we sort by bot value.
  //       _items.Insert(bindl, e);
  //     }
  //     else
  //     {
  //       Gu.Log.Error("edge was coincident");
  //       Gu.DebugBreak();
  //     }
  //   }
  //   public bool Find(ushort value, out IRangeItem left, out IRangeItem right)
  //   {
  //     return Find(value, out left, out right, out int l, out int r);
  //   }
  //   public bool Find(ushort value, out IRangeItem left, out IRangeItem right, out int closest_left, out int closest_right)
  //   {
  //     //Returns 1 or 2 matching range items.
  //     left = right = null;
  //     var ret = false;

  //     //Note this finds the CLOSEST value
  //     if (FindClosest_LeftGuaranteed(value, out closest_left, out closest_right))
  //     {
  //       if (closest_left != c_iNotFound)
  //       {
  //         if (left == null)
  //         {
  //           if (_items[closest_left].Min <= value && _items[closest_left].Max >= value)
  //           {
  //             left = _items[closest_left];
  //             ret = true;
  //           }
  //         }
  //         else
  //         {
  //           Gu.Assert(left == _items[closest_left]);//Extra check - the left/right order would always be the same, and the left/right beam would always be the same.
  //         }
  //       }
  //       if (closest_right != c_iNotFound)
  //       {
  //         if (right == null)
  //         {
  //           if (_items[closest_right].Min <= value && _items[closest_right].Max >= value)
  //           {
  //             right = _items[closest_right];
  //             ret = true;
  //           }
  //         }
  //         else
  //         {
  //           Gu.Assert(right == _items[closest_right]);//Extra check - the left/right order would always be the same, and the left/right beam would always be the same.
  //         }
  //       }
  //     }

  //     return ret;
  //   }
  //   public bool FindRangeIndexes(ushort h, out int min_index, out int max_index)
  //   {
  //     //BSearch and Return indexs into _items for the ranges that contain h.
  //     min_index = max_index = c_iNotFound;
  //     if (_items == null || _items.Count == 0)
  //     {
  //       return false;
  //     }

  //     bool ret = false;
  //     int pos = _items.Count / 2;
  //     int range = _items.Count / 2;

  //     for (int xi = 0; Gu.WhileTrueGuard(xi, Gu.c_intMaxWhileTrueLoopBinarySearch); xi++)
  //     {
  //       min_index = pos;

  //       Gu.Assert(min_index < _items.Count);

  //       var el = _items[min_index];

  //       if (el.Min <= h && el.Max >= h)
  //       {
  //         //in exclusive range
  //         ret = true;
  //         break;
  //       }

  //       if (range <= 1)
  //       {
  //         //no hit
  //         break;
  //       }

  //       //continue divide
  //       range /= 2;
  //       if (h < el.Min)
  //       {
  //         pos = pos - range;
  //       }
  //       else if (h > el.Max)
  //       {
  //         pos = pos + range;
  //       }

  //       if (pos < _items.Count || pos > _items.Count)
  //       {
  //         //no hit
  //         break;
  //       }
  //     }

  //     //l must be a count
  //     Gu.Assert(left <= _items.Count);
  //     if (right != c_iNotFound)
  //     {
  //       Gu.Assert(right <= _items.Count);
  //     }

  //     return ret;
  //   }
  // }//cl
  public interface ISimpleFlags
  {
    public bool Test(int x);
    public int Set(int x);
    public int Clear(int x);
    protected static int GetMask(int bits, int shift, int mask)
    {
      int lower = (int)(((bits << shift) >> shift) & mask);
      Gu.Assert((bits & (~((int)(lower)))) == 0); //Make sure the int has correct flags.
      return lower;
    }
  }
  public struct ByteFlags : ISimpleFlags
  {
    //C# bitwise stuff is integer only so yeah
    byte _flags = 0;
    public ByteFlags() { }
    public bool Test(int bits)
    {
      int lower = ISimpleFlags.GetMask(bits, 24, 0xFF);
      bool ret = ((int)lower & (int)_flags) > 0;
      return ret;
    }
    public int Set(int bits)
    {
      int lower = ISimpleFlags.GetMask(bits, 24, 0xFF);
      byte ret = (byte)((int)lower | (int)_flags);
      return ret;
    }
    public int Clear(int bits)
    {
      int lower = ISimpleFlags.GetMask(bits, 24, 0xFF);
      byte ret = (byte)(~((int)lower) & (int)_flags);
      return ret;
    }
  }//cl
  public struct ShortFlags : ISimpleFlags
  {
    //C# bitwise stuff is integer only so yeah
    short _flags = 0;
    public ShortFlags() { }
    public bool Test(int bits)
    {
      int lower = ISimpleFlags.GetMask(bits, 16, 0xFFFF);
      bool ret = ((int)lower & (int)_flags) > 0;
      return ret;
    }
    public int Set(int bits)
    {
      int lower = ISimpleFlags.GetMask(bits, 16, 0xFFFF);
      byte ret = (byte)((int)lower | (int)_flags);
      return ret;
    }
    public int Clear(int bits)
    {
      int lower = ISimpleFlags.GetMask(bits, 16, 0xFFFF);
      byte ret = (byte)(~((int)lower) & (int)_flags);
      return ret;
    }
  }//cl


}//ns
