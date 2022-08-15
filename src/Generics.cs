using System;
using System.Collections;
using System.Reflection;

namespace PirateCraft
{
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
    public List<TValue> ItemsAt(TKey key)
    {
      if (_dict.TryGetValue(key, out var ret))
      {
        return ret;
      }
      return null;
    }
    public void SetValueList(TKey key, List<TValue> val){
      _dict.Remove(key);
      _dict.Add(key,val);
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

  //I couldn't figure how to implement this one. It didn't appear to work as intended.
  //It could be faster than the other multimap (tuple vs kvp) would need testing
  //public class Multimap<TKey, TValue> : SortedSet<Tuple<TKey, TValue>>
  //This is a basic multimap implementation. Multiple keys to multiple values
  // Note that you cannot add the same tuple itself (which is class type). Just call Add() to add new.
  //  where TKey : IComparable
  //{
  //  public Multimap() : base(new MultimapComparer()) { }

  //  private class MultimapComparer : Comparer<Tuple<TKey, TValue>>
  //  {
  //    public override int Compare(Tuple<TKey, TValue> x, Tuple<TKey, TValue> y)
  //    {
  //      if (x == null || y == null)
  //      {
  //        return 0;
  //      }
  //      if (x == y)
  //      {
  //        return 0;
  //      }

  //      var d = x.Item1.Equals(y.Item1) ? 1 : x.Item1.CompareTo(y.Item1);
  //      return d;
  //    }
  //  }
  //  public Multimap(Multimap<TKey, TValue> other) : base(other, new MultimapComparer())
  //  {
  //  }
  //  public void Add(TKey key, TValue value)
  //  {
  //    Add(new Tuple<TKey, TValue>(key, value));
  //  }
  //  public void RemoveFirst(TKey key)
  //  {
  //    var d = this.First(x => x.Item1.Equals(key));
  //    if (d != null)
  //    {
  //      Remove(d);
  //    }
  //  }
  //  public List<Tuple<TKey, TValue>> FindAll(TKey key)
  //  {
  //    List<Tuple<TKey, TValue>> ret = new List<Tuple<TKey, TValue>>();
  //    ret = this.Where(x => x.Item1.Equals(key)).ToList();
  //    return ret;
  //  }
  //}
  /// <summary>
  /// FileLoc represents a virtual file location on disk, embed, or web
  /// </summary>
  public class FileLoc
  {
    //The name here has to be unique or it will cause conflicts.
    public static FileLoc Generated = new FileLoc("<generated>", FileStorage.Generated);
    public FileStorage FileStorage { get; private set; } = FileStorage.Disk;
    public string RawPath { get; private set; } = "";

    public byte[] GetBytes()
    {
      byte[] bytes = null;
      using (var fs = GetStream())
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
    public Stream? GetStream()
    {
      string qualifiedPath = this.QualifiedPath;

      if (FileStorage == FileStorage.Embedded)
      {
        return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(qualifiedPath);
      }
      else if (FileStorage == FileStorage.Disk)
      {
        return File.OpenRead(qualifiedPath);
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return null;
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
    public class Comparer : IEqualityComparer<FileLoc>
    {
      public bool Equals(FileLoc a, FileLoc b)
      {
        return a.Equals(b);
      }

      public int GetHashCode(FileLoc a)
      {
        return a.QualifiedPath.GetHashCode();
      }
    }

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
      for (int xx = 0; xx < 10000; xx++)//dummy infinite loop blocker
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


}
