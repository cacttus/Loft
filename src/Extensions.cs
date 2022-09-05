using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PirateCraft
{

  //Extension methods - google what this is if you don't know
  public static class Extensions
  {
    public static string Description(this Enum value)
    {
      var d = value.GetAttribute<System.ComponentModel.DescriptionAttribute>();
      if (d == null)
      {
        Gu.Log.Error($"Failed to get description for enum '{value.GetType().Name}' value '{value}'");
        Gu.DebugBreak();
        return null;
      }
      return d.Description;
    }
    public static T GetAttribute<T>(this Enum value) where T : Attribute
    {
      T attribute;
      System.Reflection.MemberInfo info = value.GetType().GetMember(value.ToString()).FirstOrDefault();
      if (info != null)
      {
        attribute = (T)info.GetCustomAttributes(typeof(T), false).FirstOrDefault();
        return attribute;
      }
      return null;
    }

    public static T ConstructIfNeeded<T>(this T xx) where T : class, new()
    {
      if (xx == null)
      {
        xx = new T();
      }
      return xx;
    }

    public static List<T> Clone<T>(this List<T> source, bool shallow = false) where T : Cloneable<T>
    {
      List<T> ret = new List<T>();
      foreach (var item in source)
      {
        ret.Add(item.Clone(shallow));
      }
      return ret;
    }
  }
  
}
