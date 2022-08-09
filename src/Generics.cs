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
    public bool Remove(KeyValuePair<TKey, TValue> val)
    {
      return Remove(val.Key, val.Value);
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
    public static bool Equals(string a, string b)
    {
      return a.CompareTo(b) == 0;
    }
  }


}
