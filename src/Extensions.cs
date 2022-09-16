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
    public static string GetEnumValues(this Type enumtype, string delim_in = ",")
    {
      Gu.Assert(enumtype.IsEnum);
      string s = "";
      string delim = "";
      foreach (var v in Enum.GetValues(enumtype))
      {
        s += delim + v.ToString().ToLower();
        delim = delim_in;
      }
      return s;
    }

    public static void CheckIfSortedByKey_Slow_AndThrowIfNot<T>(this List<T> items, Func<T, int> keyfunc) where T : class
    {
      var val = keyfunc(items[1]);
      for (int xi = 1; xi < items.Count; xi++)
      {
        var val2 = keyfunc(items[xi]);
        Gu.Assert(val < val2);
        val = val2;
      }
    }
    public static System.Collections.BitArray AndWith(this System.Collections.BitArray readonly_copy, System.Collections.BitArray other)
    {
      //And a BitArray with another, but do not modify the array, return the And'd array.
      var copy = (System.Collections.BitArray)readonly_copy.Clone();
      copy.And(other);
      return copy;
    }
    public static UInt64 UInt64Value(this System.Collections.BitArray b)
    {
      Gu.Assert(b.Count < 64);//Can only do 64 right now..we'll never need anything else..

      UInt64 ret = 0;
      for (int i = 0; i < b.Count; i++)
      {
        UInt64 val = (UInt64)(b.Get(i) == true ? 1 : 0);
        ret = ret | (val << i);
      }
      return ret;
    }
    public static void Clear(this System.Collections.BitArray value)
    {
      value.SetAll(false);
    }
    public static string Description(this Enum value)
    {
      //Returns null if the given attribute is not found
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
      //Returns null if the given attribute is not found
      T attribute;
      System.Reflection.MemberInfo info = value.GetType().GetMember(value.ToString()).FirstOrDefault();
      if (info != null)
      {
        attribute = (T)info.GetCustomAttributes(typeof(T), false).FirstOrDefault();
        return attribute;
      }
      return null;
    }
    public static T GetAttribute<T>(this System.Reflection.FieldInfo value) where T : Attribute
    {
      //Returns null if the given attribute is not found
      T attribute;
      if (value != null)
      {
        attribute = (T)value.GetCustomAttributes(typeof(T), false).FirstOrDefault();
        return attribute;
      }
      return null;
    }
    public static T GetAttribute<T>(this System.Reflection.PropertyInfo value) where T : Attribute
    {
      //Returns null if the given attribute is not found
      T attribute;
      if (value != null)
      {
        attribute = (T)value.GetCustomAttributes(typeof(T), false).FirstOrDefault();
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
