using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PirateCraft
{
  public abstract class Cloneable<T>
  {
    public abstract T Clone(bool shallow = true);
  }
  //Extension methods - google what this is if you don't know
  public static class Extensions
  {
    public static List<T> Clone<T>(this List<T> source, bool shallow=false) where T : Cloneable<T>
    {
      List<T> ret = new List<T>();
      foreach(var item in source)
      {
        ret.Add(item.Clone(shallow));
      }
      return ret;
    }
  }
}
