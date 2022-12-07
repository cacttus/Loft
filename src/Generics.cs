using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.Serialization;

namespace Loft
{
  public enum FileMode
  {
    Text, Binary
  }
  public enum ActionState
  {
    None, Pause, Run, Stop
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
  public class TypeAttribute : Attribute
  {
    public Type? Type = null;
    public TypeAttribute(Type t) { Type = t; }
  }
  public class FileLoc : ISerializeBinary
  {
    /// FileLoc represents a virtual file location on disk, embed, or web
    //The name here has to be unique or it will cause conflicts.

    #region Public: Members
    private int c_newline = '\n';
    private int c_EOF = -1;//I guess, -1 in .net

    public static FileLoc Generated = new FileLoc("<generated>", FileStorage.Generated);
    public FileStorage FileStorage { get; set; } = FileStorage.Disk;
    public string RawPath { get; set; } = "";

    public string Extension
    {
      get
      {
        return System.IO.Path.GetExtension(this.QualifiedPath);
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
    public string FileName
    {
      get
      {
        string fn = "";
        if (this.FileStorage == FileStorage.Embedded)
        {
          //embedded path filename will not work - must update raw path to be input file.
          fn = RawPath;
        }
        else if (FileStorage == FileStorage.Disk)
        {
          fn = System.IO.Path.GetFileName(this.QualifiedPath);
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
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
    public bool ExistsOnDisk()
    {
      return File.Exists(WorkspacePath);
    }
    public bool CopyFile(FileLoc fl)
    {
      try
      {
        System.IO.File.Copy(this.QualifiedPath, fl.QualifiedPath);
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Failed to copy file: ", ex);
        return false;
      }
      return true;
    }
    public FileLoc Clone()
    {
      FileLoc ret = new FileLoc();
      ret.RawPath = this.RawPath;
      ret.FileStorage = this.FileStorage;
      return ret;
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
        AssertExists();
        return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(QualifiedPath);
      }
      else if (FileStorage == FileStorage.Disk)
      {
        AssertExists();
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
    public void WriteAllText(string text)
    {
      using (var s = this.OpenWrite())
      {
        if (s != null)
        {
          using (var stream = new StreamWriter(s))
          {
            stream.Write(text);
          }
        }
        else
        {
          Gu.BRThrowException($"'{this.QualifiedPath}' Failed to get write stream.");
        }
      }
    }
    public string? ReadAllText()
    {
      string? ret = null;
      using (var s = this.OpenRead())
      {
        if (s != null)
        {
          using (var stream = new StreamReader(s))
          {
            ret = stream.ReadToEnd();
          }
        }
        else
        {
          Gu.BRThrowException($"'{this.QualifiedPath}' Failed to get read stream.");
        }
      }
      return ret;
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
        else
        {
          Gu.BRThrowException($"'{this.QualifiedPath}' Failed to get read stream.");
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
    public void Touch()
    {
      //Create if doesn't exist
      if (!Exists)
      {
        Create();
      }
    }
    public void Create(bool createDirs = true)
    {
      if (FileStorage == FileStorage.Disk)
      {
        if (createDirs)
        {
          var d = System.IO.Path.GetDirectoryName(QualifiedPath);
          if (!System.IO.Directory.Exists(d))
          {
            System.IO.Directory.CreateDirectory(d);
          }
        }

        using (var fs = System.IO.File.Create(QualifiedPath))
        {
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
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
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      FileStorage = (FileStorage)br.ReadInt32();
      RawPath = br.ReadString();
    }

    public new string ToString()
    {
      Gu.MustTest();
      string s = "{" + $"path:\"{RawPath}\", storage:{FileStorage.ToString()}" + "}";
      return s;
    }
    public static FileLoc Parse(string loc)
    {
      Gu.MustTest();
      Gu.Assert(StringUtil.IsNotEmpty(loc));
      Gu.Assert(loc[0] == '{' && loc[loc.Length] == '}');
      string loc2 = loc.Trim('{').Trim('}');
      var i1 = loc2.IndexOf(':');
      var i2 = loc2.IndexOf(',');
      string rawpath = StringUtil.CutOut(ref loc2, i1, i2);
      i1 = loc2.IndexOf(':');
      i2 = loc2.Length;
      FileStorage storage = Gu.ParseEnum<FileStorage>(StringUtil.CutOut(ref loc2, i1, i2));
      FileLoc fs = new FileLoc(rawpath, storage);
      return fs;
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
    public static string CutOut(ref string instr, int id0, int id1)
    {
      Gu.Assert(id0 <= id1);
      if (id0 == id1)
      {
        return "";
      }
      var s = instr.Substring(id0, id1 - id0);
      instr.Remove(id0, id1 - id0);
      return s;
    }
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
    public static bool DoesNotEqual(string a, string b)
    {
      return !Equals(a, b);
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
    public static List<int> StringMatches(string textToQuery, string stringToFind)
    {
      //Get list of indexes of a string in another string
      //https://stackoverflow.com/questions/17892237/occurrences-of-a-liststring-in-a-string-c-sharp
      int currentIndex = 0;
      List<int> ret = new List<int>();
      for (int xi = 0; Gu.WhileTrueGuard(xi, Gu.c_intMaxWhileTrueLoop); xi++)
      {
        currentIndex = textToQuery.IndexOf(stringToFind, currentIndex, StringComparison.Ordinal);
        if (currentIndex == -1)
        {
          break;
        }
        ret.Add(currentIndex);
        currentIndex++;
      }

      return ret;
    }

    public static string Seconds_ToString_HMSU(double seconds, bool ms = false)
    {
      //seconds to string
      // seconds = 71.33;
      var ut = seconds;
      string ret = "";
      double weekd = 60 * 60 * 24 * 7;
      double dayd = 60 * 60 * 24;
      double hourd = 60 * 60;
      double mind = 60;

      var weeks = (int)Math.Floor(ut / weekd);
      if (weeks < 1)
      {
        weeks = 0;
      }
      else
      {
        ut -= weeks * weekd;
        ret += $" {(int)weeks}w";
      }
      var days = (int)Math.Floor(ut / dayd);
      if (days < 1)
      {
        days = 0;
      }
      else
      {
        ut -= days * dayd;
        ret += $" {(int)days}d";
      }
      var hours = (int)Math.Floor(ut / hourd);
      if (hours < 1)
      {
        hours = 0;
      }
      else
      {
        ut -= hours * hourd;
        ret += $" {(int)hours}h";
      }
      var mins = (int)Math.Floor(ut / mind);
      if (mins < 1)
      {
        mins = 0;
      }
      else
      {
        ut -= mins * mind;
        ret += $" {(int)mins}m";
      }
      var secs = (int)Math.Floor(ut);
      if (secs < 1)
      {
        secs = 0;
      }
      else
      {
        ret += $" {(int)secs}s";
      }

      return ret.Trim();
    }



  }
  public interface IMutableState
  {
    public bool Modified { get; }
    public void SetModified();
  }
  [Serializable()]
  public class MutableState : IMutableState
  {
    private bool _modified = false;
    public virtual bool Modified { get { return _modified; } set { _modified = value; } }
    public virtual void SetModified()
    {
      _modified = true;
    }
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
  public class BaseTimer
  {
    public long PeriodMS { get { return _periodMS; } private set { _periodMS = value; } }
    public ActionState State { get { return _state; } private set { _state = value; } }
    public ActionRepeat Repeat { get { return _repeat; } private set { _repeat = value; } }
    public Action Action { get { return _action; } private set { _action = value; } }

    protected long _periodMS = 0;
    protected ActionState _state = ActionState.Stop;
    protected ActionRepeat _repeat = ActionRepeat.DoNotRepeat;
    protected Action? _action = null;

    public BaseTimer() { }//clone
    public BaseTimer(long periodMS, ActionRepeat repeat, ActionState start, Action? act = null)
    {
      _periodMS = periodMS;
      _repeat = repeat;
      _action = act;
      if (start == ActionState.Run)
      {
        Start();
      }
    }
    public virtual void Start()
    {
      _state = ActionState.Run;
    }
    public virtual void Stop()
    {
      _state = ActionState.Stop;
    }
    public virtual void Pause()
    {
      _state = ActionState.Pause;
    }
    public void Restart()
    {
      Stop();
      Start();
    }
    public BaseTimer Clone()
    {
      return (BaseTimer)this.MemberwiseClone();
    }
  }
  public class DeltaTimer : BaseTimer
  {
    public double ElapsedSeconds { get; private set; } = 0;
    public double PeriodSeconds { get; private set; } = 0;

    public DeltaTimer(long periodMS, ActionRepeat repeat, ActionState start, Action? act)
    : base(periodMS, repeat, start, act)
    {
      //act can be null in which case we just use the timer.
      PeriodSeconds = (double)periodMS / 1000.0;
      this._action = act;
    }

    //Timer that runs in sync with the program / frame loop
    public int Update(double dt)
    {
      //Putting action here makes it more sense, since it keeps the action code within the update code.
      //Returns the number of times this timer fired, and executes optional action
      int fires = 0;
      if (State != ActionState.Stop)
      {
        ElapsedSeconds += dt;
        while (ElapsedSeconds > PeriodSeconds)
        {
          ElapsedSeconds -= PeriodSeconds;
          Action?.Invoke();
          fires++;
        }
      }
      return fires;
    }
    public DeltaTimer Clone()
    {
      return (DeltaTimer)this.MemberwiseClone();
    }
  }
  public class AsyncTimer : BaseTimer
  {
    //threaded timer which executes the Tick action on the calling thread
    private System.Timers.Timer? _timer = null;

    public AsyncTimer(int periodMS, ActionRepeat repeat, ActionState start, Action? act = null)
      : base(periodMS, repeat, start, act)
    {
    }
    public override void Start()
    {
      if (_state == ActionState.Pause)
      {
        if (_timer == null)
        {
          StartNewTimer();
        }
        else
        {
          _timer.Start();
        }
      }
      else if (_state == ActionState.Stop)
      {
        StartNewTimer();
      }

      base.Start();
    }
    public override void Stop()
    {
      _timer.Stop();
      _timer = null;
      base.Stop();
    }
    public override void Pause()
    {
      //this may not work - we may need _resumeMS to create a new timer with a new delay parameter
      Gu.MustTest();
      _timer?.Stop();
      base.Pause();
    }
    private void StartNewTimer()
    {
      System.Timers.ElapsedEventHandler func = (sender, args) =>
      {
        if (_state == ActionState.Run)
        {
          _action?.Invoke();
          if (_repeat == ActionRepeat.Repeat)
          {
            _timer.Start();
          }
        }
      };
      _timer = new System.Timers.Timer();
      _timer.Interval = _periodMS;
      _timer.Elapsed += func;
      _timer.AutoReset = false;//run once, then reset if Repeat is set
      _timer.Start();
    }

  }
  public class FrameDataTimer
  {
    //Manages: Delta, FPS, Uptime, Ticks, FrameStamp
    private const long c_dblFpsAvgSampleTimeNs = 250 * (1000 * 1000);
    public Int64 FrameStamp { get; private set; }
    public double UpTime { get; private set; } = 0; //Time since engine started.
    public double Fps { get; private set; } = 60;
    public double FpsAvg { get; private set; } = 60;
    public double Delta { get; private set; } = 1 / 60;

    private List<double> _fpsSamples = new List<double>();

    private DateTime _startTime = DateTime.Now;
    private long _lastTime = Gu.Nanoseconds();
    private long _lastTotal = Gu.Nanoseconds();

    public void Update()
    {
      long curTime = Gu.Nanoseconds();
      if (FrameStamp > 0)
      {
        Delta = (double)((decimal)(curTime - _lastTime) / (decimal)(1000000000));
      }
      _lastTime = curTime;
      FrameStamp++;

      Fps = 1 / Delta;

      if ((curTime - _lastTotal) >= c_dblFpsAvgSampleTimeNs)
      {
        FpsAvg = _fpsSamples.Count > 0 ? _fpsSamples.Average() : 0;
        _fpsSamples.Clear();
        _lastTotal = curTime;
      }
      if (_fpsSamples.Count < 1000)
      {
        _fpsSamples.Add(Fps);
      }

      UpTime = (DateTime.Now - _startTime).TotalSeconds;
    }
  }
  public class AdaptiveDelta
  {
    //computes FPS, and other deltas using a running average
    private const long US_To_NS = 1000;
    private const long MS_To_NS = 1000 * 1000;
    private const long S_To_NS = 1000 * 1000 * 1000;
    private const long S_To_MS = 1000;
    public const long c_lngSampleTimeNs = 1000 * MS_To_NS;//length of time we average samples
    public const int c_intMaxSamples = 512;

    public Int64 FrameStamp { get; private set; }
    public double AverageS { get; private set; }//average over sample period, in seconds
    public double FrameDeltaMS { get; private set; } = 1 / 60 * 1000;//delta for frame, in seconds
    public double FrameDeltaS { get; private set; } = 1 / 60;//delta for frame, in seconds
    public double UpTimeS { get; private set; } = 0; //Time since this timer started, seconds
    public double FpsAvg
    {
      get
      {
        if (AverageS != 0)
        {
          return 1.0 / AverageS;
        }
        else
        {
          return 1.0 / 60.0;
        }
      }
    }
    public double FpsFrame
    {
      get
      {
        if (FrameDeltaS != 0)
        {
          return 1.0 / FrameDeltaS;
        }
        else
        {
          return 1.0 / 60.0;
        }
      }
    }

    protected DateTime _startTime = DateTime.Now;
    protected List<double> _samples = new List<double>();
    protected long _lastTime = Gu.Nanoseconds();
    protected long _lastTotal = Gu.Nanoseconds();
    private double _runningTotal = 0;

    // public List<string>? Recorded { get; }
    // public void CaptureDelta(string tag)
    // {
    //   Recorded.Add($"{tag}{AverageS}");
    //   _samples.Clear
    // }

    public void Update()
    {
      //every time we update, we add a sample, and remove old samples.
      long curTime = Gu.Nanoseconds();
      if (FrameStamp > 0)
      {
        FrameDeltaMS = (double)((decimal)(curTime - _lastTime) / (decimal)MS_To_NS);
        FrameDeltaS = (double)((decimal)FrameDeltaMS / (decimal)S_To_MS);
      }
      _lastTime = curTime;
      FrameStamp++;

      //2fps = 60fps = 60 samples
      //fps = 1/avg
      //60 samples / Monitor Refresh Rate Hz (60, 120, 240, etc)
      //
      // this allows for more samples when the frame rate slows down 
      //Monitor Refresh Rate Hz (60, 120, 240, etc) / fps
      //Ceil(MRR / fps) * MRR

      //we need an adaptive sample count
      if ((curTime - _lastTotal) >= c_lngSampleTimeNs)
      {
        _samples.Clear();
        // while (_samples.Count >= c_intMaxSamples)
        // {
        //   _runningTotal -= _samples[0];
        //   _samples.RemoveAt(0);
        // }
        _lastTotal = curTime;
      }
      //TODO: if we get FP drift we should use average function
      //AverageS = _samples.Count > 0 ? _samples.Average() : 0;

      // double mrr = 240.0;
      // double hz = FpsAvg;
      // if (hz < 1) { hz = 1; }
      // int maxSamples = (int)(Math.Ceiling(mrr / hz) * mrr);
      // while (_samples.Count >= maxSamples)
      // {
      //   _runningTotal -= _samples[0];
      //   _samples.RemoveAt(0);
      // }

      while (_samples.Count >= c_intMaxSamples)
      {
        _runningTotal -= _samples[0];
        _samples.RemoveAt(0);
      }

      _runningTotal += FrameDeltaS;
      _samples.Add(FrameDeltaS);

      AverageS = _runningTotal / (double)_samples.Count;

      UpTimeS = (DateTime.Now - _startTime).TotalSeconds;
    }
  }
  public static class ByteParser
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
    public bool Load(string? inputdata = null)
    {
      Gu.Assert(_fileLoc.Exists);
      var sw = new Stopwatch();
      sw.Start();

      if (inputdata != null)
      {
        _data = Encoding.ASCII.GetBytes(inputdata);
      }
      else
      {
        _data = _fileLoc.ReadAllData();
      }

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
    public abstract void Deserialize(BinaryReader br, SerializedFileVersion version);
  }
  public class SerializedFileVersion
  {
    public int Version { get; } = 0;//for future implementation, file version
    public SerializedFileVersion(int v) { Version = v; }
  }
  public interface ISerializeByteArray
  {
    public void Serialize(ByteBuffer b);
    public void Deserialize(ByteBuffer b);
  }
  public class ByteBuffer
  {
    public enum AccessMode
    {
      Read, Write
    }
    public Byte[] Bytes = null;
    public int ReadOffset { get; set; } = 0;
    public AccessMode Mode { get; set; } = AccessMode.Read;
    public ByteBuffer(AccessMode mode, byte[] initialBytes = null)
    {
      Mode = mode;
      Bytes = initialBytes;

      if (mode == AccessMode.Read)
      {
        Gu.Assert(initialBytes != null);
      }
    }
    public void WriteInt32(Int32 n)
    {
      Gu.Assert(this.Mode == AccessMode.Write);
      var data = new byte[Marshal.SizeOf(typeof(Int32))];
      using (var memoryStream = new MemoryStream(Bytes))
      using (var writer = new BinaryWriter(memoryStream))
      {
        writer.Write(data);
      }
      WriteBuffer(data);
    }
    public Int32 ReadInt32()
    {
      Gu.Assert(this.Mode == AccessMode.Read);

      Int32 ret = 0;
      using (var memoryStream = new MemoryStream(Bytes))
      using (var reader = new BinaryReader(memoryStream))
      {
        ret = reader.ReadInt32();
      }
      ReadOffset += Marshal.SizeOf(typeof(Int32));
      return ret;
    }
    public void WriteStructs<T>(T[] items) where T : struct
    {
      Gu.Assert(this.Mode == AccessMode.Write);

      WriteInt32(items.Length);
      int structsize = Marshal.SizeOf(typeof(T));
      WriteInt32(structsize);

      var data = new byte[structsize * items.Length];
      var pinnedHandle = GCHandle.Alloc(items, GCHandleType.Pinned);
      Marshal.Copy(pinnedHandle.AddrOfPinnedObject(), data, 0, data.Length);
      pinnedHandle.Free();

      using (var memoryStream = new MemoryStream(Bytes))
      using (var writer = new BinaryWriter(memoryStream))
      {
        writer.Write(data);
      }

      WriteBuffer(data);
    }
    public T[] ReadStructs<T>() where T : struct
    {
      Gu.Assert(this.Mode == AccessMode.Read);

      int len = ReadInt32();
      int size = ReadInt32();

      var ret = new T[len];

      var pinnedHandle = GCHandle.Alloc(ret, GCHandleType.Pinned);
      Marshal.Copy(Bytes, ReadOffset, pinnedHandle.AddrOfPinnedObject(), size * len);
      pinnedHandle.Free();

      ReadOffset += size * len;

      return ret;
    }

    public void WriteBuffer(byte[] buf)
    {
      Gu.Assert(this.Mode == AccessMode.Write);
      if (Bytes == null)
      {
        Bytes = new Byte[0];
      }
      var newbytes = new Byte[Bytes.Length + buf.Length];
      Buffer.BlockCopy(Bytes, 0, newbytes, 0, Bytes.Length);
      Buffer.BlockCopy(buf, 0, newbytes, Bytes.Length, buf.Length);
    }
  }
  // public class ArrayUtils
  // {
  //   //ByteArrayUtils

  //   // public static unsafe byte[] Serialize<T>(T[] data) where T : struct
  //   // {
  //   //   //we could optimize this with unsafe code
  //   //   var size = Marshal.SizeOf(data[0]);
  //   //   var bytes = new byte[size * data.Length];
  //   //   for (int di = 0; di < data.Length; di++)
  //   //   {
  //   //     var ptr = Marshal.AllocHGlobal(size);
  //   //     Marshal.StructureToPtr(data[di], ptr, true);
  //   //     Marshal.Copy(ptr, bytes, di * size, size);
  //   //     Marshal.FreeHGlobal(ptr);
  //   //   }

  //   //   return bytes;
  //   // }

  //   // Old copy method - slower by initially about x20 then subsequently by about x3 (11us/200us, then 12us/35us)
  //   // public static unsafe vec3[] ParseVec3fArray(byte[] Data, int item_count, int byte_offset)
  //   // {
  //   //   vec3[] ret = new vec3[item_count];
  //   //   fixed (byte* raw = Data)
  //   //   {
  //   //     int component_byte_size = 4;//sizeof float
  //   //     int tensor_rank = 3;// 3 scalars
  //   //     int tensor_byte_size = component_byte_size * tensor_rank;
  //   //     for (int ioff = 0; ioff < item_count; ioff++)
  //   //     {
  //   //       int offset = byte_offset + ioff * tensor_byte_size;
  //   //       Gu.Assert(offset < Data.Length);
  //   //       vec3 v = *((vec3*)(raw + offset));
  //   //       ret[ioff] = v;
  //   //     }
  //   //   }
  //   //   return ret;
  //   // }


  // }
  public class BList<T> : List<T> where T : IComparable
  {
    //Sorted list O(logn)
    public enum DupeFindMode { AnyDupe, BeforeDupes, AfterDupes }
    public const int c_iNotFound = -1;
    public bool Unique { get; set; } = false;
    public DupeFindMode DupeMode { get; set; } = DupeFindMode.AfterDupes;

    public BList(bool unique_values_only = false)
    {
      //This does not work ** 
      Gu.BRThrowNotImplementedException();
      Unique = unique_values_only;
    }
    public void CheckOrderSlow()
    {
      if (this.Count <= 1) { return; }
      var x1 = this[0];
      foreach (var x2 in this)
      {
        Gu.Assert(x1.CompareTo(x2) <= 0);
        x1 = x2;
      }
    }
    public new void Add(T item)
    {
      base.Add(item);
      // Sort(this,Count);


      // if (Count == 0)
      // {
      //   base.Add(item);
      // }
      // else
      // {
      //   Find(item, out var p, DupeMode);

      //   base.Insert(p, item);
      // }
      // CheckOrderSlow();
    }
    public new bool Remove(T item)
    {

      // if (Count == 1)
      // {
      //   return base.Remove(item);
      // }
      // else if (Find(item, out var p, DupeMode))
      // {
      //   base.RemoveAt(p);
      //   return true;
      // }
      // CheckOrderSlow();
      return false;
    }
    public bool Find(T value, out int pos, DupeFindMode mode = DupeFindMode.AfterDupes)
    {
      //BSearch and Return indexs into _items for the ranges that contain h.
      if (Count == 0)
      {
        pos = c_iNotFound;
        return false;
      }

      bool ret = false;
      //pos = Count / 2;

      pos = Count / 2;
      int range = Count / 2;
      bool even = true;//range % 2 == 0; /// 2x range = even right?
      // even = range % 2 == 0;
      for (int xi = 0; Gu.WhileTrueGuard(xi, Gu.c_intMaxWhileTrueLoopBinarySearch64Bit); xi++)
      {
        Gu.Assert(pos >= 0 && pos <= Count);//can equal count if beyond array
        if (range <= 0)
        {
          break;
        }

        var cmp_left = (pos == 0) ? 1 : value.CompareTo(this.ElementAt(pos - 1));
        var cmp_right = (pos == Count) ? -1 : value.CompareTo(this.ElementAt(pos));
        if ((cmp_left == 0 || cmp_right == 0) && mode == DupeFindMode.AnyDupe)
        {
          ret = true;
          break;
        }
        else if (cmp_left == 0 && cmp_right < 0 && mode == DupeFindMode.AfterDupes)
        {
          ret = true;
          break;
        }
        else if (cmp_left > 0 && cmp_right == 0 && mode == DupeFindMode.BeforeDupes)
        {
          ret = true;
          break;
        }
        else if (cmp_left > 0 && cmp_right < 0)
        {
          ret = true;
          break;
        }
        // else if (cmp_right > 0 && pos == Count - 1)
        // { 
        //   //This is a dud case for 'end' value - probably not worth it - and should be fixed
        //   pos += 1;
        //   ret = true;
        //   break;
        // }
        else if (cmp_right > 0)//search right
        {
          pos += range;
          if (!even)
          {
            pos += 1;
          }
        }
        else if (cmp_left < 0)//search left
        {
          pos -= range;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }

        even = range % 2 == 0;
        range = range / 2;
        //  if(even){range+=1;}

      }


      return ret;
    }
    public new string ToString()
    {
      return String.Join(',', this);
    }
  }//cl
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
  public struct ByteFlags : ISimpleFlags, ISerializeBinary
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
    public void Serialize(BinaryWriter bw)
    {
      bw.Write((byte)_flags);
    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      _flags = br.ReadByte();
    }
  }//cl
  public struct ShortFlags : ISimpleFlags, ISerializeBinary
  {
    //C# bitwise stuff is integer only so yeah
    ushort _flags = 0;
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
    public void Serialize(BinaryWriter bw)
    {
      bw.Write((UInt16)_flags);
    }
    public void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      _flags = br.ReadUInt16();
    }
  }//cl
  public class GrowList<T>
  {
    //Used for UI since sorting isn't supported yet
    public List<T> List = new List<T>();
    public int Count { get { return _count; } }
    private int _count = 0;
    public int MaxCount { get; set; }

    public GrowList(int max = 9999999)
    {
      MaxCount = max;
    }
    public T this[int k]
    {
      //operator[]
      get
      {
        Gu.Assert(k < Count);
        return List[k];
      }
      set
      {
        Gu.Assert(k < Count);
        List[k] = value;
      }
    }
    public T[] ToArray()
    {
      return List.GetRange(0, _count).ToArray();
    }
    public void Reset()
    {
      _count = 0;
    }
    public void Add(T item)
    {
      if (_count == List.Count)
      {
        if (_count >= MaxCount)
        {
          Gu.Log.Error($"grow list exceeded max count {MaxCount}");
          Gu.DebugBreak();
        }
        else
        {
          List.Add(item);
          _count++;
        }
      }
      else
      {
        List[_count] = item;
        _count++;
      }
    }
  }
  public class ReverseGrowList<T>
  {
    //Used for UI since sorting isn't supported yet
    public List<T> List = new List<T>();
    public int Count { get { return List.Count - _countdown; } }
    private int _countdown = 0;
    public int MaxCount { get; set; }

    public ReverseGrowList(int max = 999999)
    {
      MaxCount = max;
    }
    public T[] ToArray()
    {
      var range = List.GetRange(_countdown, List.Count - _countdown);
      return range.ToArray();
    }
    public void Reset()
    {
      _countdown = List.Count;
    }
    public void Add(T item)
    {
      if (_countdown == 0)
      {
        List.Insert(0, item);
        if (List.Count > MaxCount)
        {
          Gu.Log.Error($"reverse grow list exceeded max count {MaxCount}");
          Gu.DebugBreak();//forgot to call reset?
        }
      }
      else
      {
        List[_countdown - 1] = item;
        _countdown--;
      }
    }
  }
  public class FloatSort : IComparer<float>
  {
    public int Compare(float a, float b)
    {
      return a < b ? -1 : 1;
    }
  }
  public class DoubleSort : IComparer<double>
  {
    public int Compare(double a, double b)
    {
      return a < b ? -1 : 1;
    }
  }


}//ns
