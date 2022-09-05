using System;
using System.Collections;
using System.Reflection;

namespace PirateCraft
{
  public enum ActionState
  {
    Pause, Run, Stop
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
    #region Public: Members

    /// FileLoc represents a virtual file location on disk, embed, or web
    //The name here has to be unique or it will cause conflicts.
    public static FileLoc Generated = new FileLoc("<generated>", FileStorage.Generated);
    public FileStorage FileStorage { get; private set; } = FileStorage.Disk;
    public string RawPath { get; private set; } = "";
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
    public FileLoc(string path, string filename, FileStorage storage)
    {
      RawPath = System.IO.Path.Combine(path, filename);
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
    public DateTime GetLastWriteTime()
    {
      //Returns : the Modified Time of the 
      //Disk file resource
      //Embedded file's original resource, if present
      //DateTime.MinVal if file was Generated
      DateTime wt = DateTime.MinValue;
      if (FileStorage == FileStorage.Embedded)
      {
        Gu.Assert(Exists);
        //Embedded files don't hjave mod time, instead try to find the file pre-embed and use that.
        //Otherwise we return minvalue.
        if (System.IO.File.Exists(WorkspacePath))
        {
          wt = System.IO.File.GetLastWriteTime(WorkspacePath);
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
      for (int xx = 0; xx < Gu.c_intMaxWhileTrueLoop; xx++)
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

    private static int s_dataBlockIdGen = 1;
    private static int s_dataBlockTypeIdGen = 1;
    private string _name = "<Unnamed>";
    private Int64 _typeId = 1; // When Clone() is called this gets duplicated
    private Int64 _uniqueId = 0; //Never duplicated, unique for all objs

    public string Name { get { return _name; } set { _name = value; SetModified(); } }
    public Int64 UniqueID { get { return _uniqueId; } private set { _uniqueId = value; SetModified(); } }
    public Int64 TypeID { get { return _typeId; } private set { _typeId = value; SetModified(); } }

    #endregion
    #region Public Static: Methods

    public static int GetNewId()
    {
      return s_dataBlockIdGen++;
    }
    public static int GetNewType()
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
      _typeId = br.ReadInt64();
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
    public Action Action { get; set; } = null;
    public bool Repeat { get; set; } = false;

    private DeltaTimer() { }//clone
    public DeltaTimer(double frequency_seconds, bool repeat, Action? act = null)
    {
      Frequency = frequency_seconds;
      Repeat = repeat;
      Action = act;
    }
    public int Update(double dt)
    {
      //Returns the number of times this timer fired, and executes optional action
      int fires = 0;
      if (State != ActionState.Stop)
      {
        Time += dt;
        while (Time > Frequency)
        {
          Time -= Frequency;
          Action?.Invoke();
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
}
