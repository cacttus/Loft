using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PirateCraft
{
  //Extension methods - google what this is if you don't know
  public static class OtherExtensions
  {
    public static List<int> AllIndexesOf(this string str, string value)
    {
      if (String.IsNullOrEmpty(value))
      {
        throw new ArgumentException("the string to find may not be empty", "value");
      }
      List<int> indexes = new List<int>();
      for (int index = 0; ; index += value.Length)
      {
        index = str.IndexOf(value, index);
        if (index == -1)
        {
          return indexes;
        }
        indexes.Add(index);
      }
    }
    public static uint[] AsUIntArray(this uint[] arr)
    {
      return arr;
    }
    public static uint[] AsUIntArray(this ushort[] arr)
    {
      uint[] ret = new uint[arr.Length];
      for (var ix = 0; ix < arr.Length; ix++)
      {
        ret[ix] = (uint)arr[ix];
      }
      return ret;
    }

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
    public static List<T> Clone<T>(this List<T> source, bool? shallow = null) where T : IClone
    {
      List<T> ret = new List<T>();
      foreach (var item in source)
      {
        var t = item.Clone(shallow);
        Gu.Assert(t == null || t is T);
        ret.Add((T?)t);// item.Clone(shallow));
      }
      return ret;
    }
    public static bool IsNotEmpty(this string s)
    {
      return StringUtil.IsNotEmpty(s);
    }
    public static bool IsEmpty(this string s)
    {
      return StringUtil.IsEmpty(s);
    }
    public static void IterateSafe<T>(this List<T> list, Func<T, int, LambdaBool> act)
    {
      if (list != null)
      {
        //If we remove components while iterating components..
        for (int idx = list.Count - 1; idx >= 0; idx--)
        {
          if (idx < list.Count)
          {
            if (act(list[idx], idx) == LambdaBool.Break)
            {
              break;
            }
          }
          else
          {
            idx = list.Count - 1;
          }
        }
      }
    }    
    public static void IterateSafe<T>(this List<T> list, Func<T, LambdaBool> act)
    {
      if (list != null)
      {
        //If we remove components while iterating components..
        for (int idx = list.Count - 1; idx >= 0; idx--)
        {
          if (idx < list.Count)
          {
            if (act(list[idx]) == LambdaBool.Break)
            {
              break;
            }
          }
          else
          {
            idx = list.Count - 1;
          }
        }
      }
    }
    public static bool IsSystem(this Type type)
    {
      return type.Namespace.StartsWith("System");
    }
    public static bool IsList(this Type type)
    {
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }
    public static bool IsDictionary(this Type type)
    {
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }

  }//cls


  public static class BinaryWriterExtensions
  {
    public static void Write(this System.IO.BinaryWriter writer, vec2 v)
    {
      writer.Write((float)v.x);
      writer.Write((float)v.y);
    }
    public static void Write(this System.IO.BinaryWriter writer, vec3 v)
    {
      writer.Write((float)v.x);
      writer.Write((float)v.y);
      writer.Write((float)v.z);
    }
    public static void Write(this System.IO.BinaryWriter writer, vec4 v)
    {
      writer.Write((float)v.x);
      writer.Write((float)v.y);
      writer.Write((float)v.z);
      writer.Write((float)v.w);
    }
    public static void Write(this System.IO.BinaryWriter writer, ivec2 v)
    {
      writer.Write((Int32)v.x);
      writer.Write((Int32)v.y);
    }
    public static void Write(this System.IO.BinaryWriter writer, ivec3 v)
    {
      writer.Write((Int32)v.x);
      writer.Write((Int32)v.y);
      writer.Write((Int32)v.z);
    }
    public static void Write(this System.IO.BinaryWriter writer, ivec4 v)
    {
      writer.Write((Int32)v.x);
      writer.Write((Int32)v.y);
      writer.Write((Int32)v.z);
      writer.Write((Int32)v.w);
    }
    public static void Write(this System.IO.BinaryWriter writer, mat3 v)
    {
      writer.Write((float)v._m11);
      writer.Write((float)v._m12);
      writer.Write((float)v._m13);

      writer.Write((float)v._m21);
      writer.Write((float)v._m22);
      writer.Write((float)v._m23);

      writer.Write((float)v._m31);
      writer.Write((float)v._m32);
      writer.Write((float)v._m33);
    }
    public static void Write(this System.IO.BinaryWriter writer, mat4 v)
    {
      writer.Write((float)v._m11);
      writer.Write((float)v._m12);
      writer.Write((float)v._m13);
      writer.Write((float)v._m14);

      writer.Write((float)v._m21);
      writer.Write((float)v._m22);
      writer.Write((float)v._m23);
      writer.Write((float)v._m24);

      writer.Write((float)v._m31);
      writer.Write((float)v._m32);
      writer.Write((float)v._m33);
      writer.Write((float)v._m34);

      writer.Write((float)v._m41);
      writer.Write((float)v._m42);
      writer.Write((float)v._m43);
      writer.Write((float)v._m44);
    }
    public static void Write(this System.IO.BinaryWriter writer, quat v)
    {
      writer.Write((float)v.x);
      writer.Write((float)v.y);
      writer.Write((float)v.z);
      writer.Write((float)v.w);
    }
    public static void Write(this System.IO.BinaryWriter writer, Box3f box)
    {
      writer.Write((vec3)box._min);
      writer.Write((vec3)box._max);
    }
    public static void Write(this System.IO.BinaryWriter writer, OOBox3f box)
    {
      writer.Write<vec3>(box.Verts);
    }
    public static void Write(this System.IO.BinaryWriter writer, DateTime dt)
    {
      //https://stackoverflow.com/questions/15919598/serialize-datetime-as-binary
      writer.Write((long)dt.Ticks);
    }
    public static void Write<T>(this System.IO.BinaryWriter writer, T item) where T : struct
    {
      var d = SerializeTools.Serialize(item);
      writer.Write((Int32)d.Length);
      writer.Write(d);
    }
    public static void Write<T>(this System.IO.BinaryWriter writer, T[] items) where T : struct
    {
      Gu.Assert(items != null);
      var d = SerializeTools.Serialize(items);
      writer.Write((Int32)d.Length);
      writer.Write(d);
    }
    public static vec2 ReadVec2(this System.IO.BinaryReader reader)
    {
      vec2 ret = new vec2();
      ret.x = reader.ReadSingle();
      ret.y = reader.ReadSingle();
      return ret;
    }
    public static vec3 ReadVec3(this System.IO.BinaryReader reader)
    {
      vec3 ret = new vec3();
      ret.x = reader.ReadSingle();
      ret.y = reader.ReadSingle();
      ret.z = reader.ReadSingle();
      return ret;
    }
    public static vec4 ReadVec4(this System.IO.BinaryReader reader)
    {
      vec4 v = new vec4();
      v.x = reader.ReadSingle();
      v.y = reader.ReadSingle();
      v.z = reader.ReadSingle();
      v.w = reader.ReadSingle();
      return v;
    }
    public static ivec2 ReadIVec2(this System.IO.BinaryReader reader)
    {
      ivec2 v = new ivec2();
      v.x = reader.ReadInt32();
      v.y = reader.ReadInt32();
      return v;
    }
    public static ivec3 ReadIVec3(this System.IO.BinaryReader reader)
    {
      ivec3 v = new ivec3();
      v.x = reader.ReadInt32();
      v.y = reader.ReadInt32();
      v.z = reader.ReadInt32();
      return v;
    }
    public static ivec4 ReadIVec4(this System.IO.BinaryReader reader)
    {
      ivec4 v = new ivec4();
      v.x = reader.ReadInt32();
      v.y = reader.ReadInt32();
      v.z = reader.ReadInt32();
      v.w = reader.ReadInt32();
      return v;
    }
    public static mat3 ReadMat3(this System.IO.BinaryReader reader)
    {
      mat3 ret = new mat3();
      ret._m11 = reader.ReadSingle();
      ret._m12 = reader.ReadSingle();
      ret._m13 = reader.ReadSingle();
      ret._m21 = reader.ReadSingle();
      ret._m22 = reader.ReadSingle();
      ret._m23 = reader.ReadSingle();
      ret._m31 = reader.ReadSingle();
      ret._m32 = reader.ReadSingle();
      ret._m33 = reader.ReadSingle();
      return ret;
    }
    public static mat4 ReadMat4(this System.IO.BinaryReader reader)
    {
      mat4 ret = new mat4();
      ret._m11 = reader.ReadSingle();
      ret._m12 = reader.ReadSingle();
      ret._m13 = reader.ReadSingle();
      ret._m14 = reader.ReadSingle();
      ret._m21 = reader.ReadSingle();
      ret._m22 = reader.ReadSingle();
      ret._m23 = reader.ReadSingle();
      ret._m24 = reader.ReadSingle();
      ret._m31 = reader.ReadSingle();
      ret._m32 = reader.ReadSingle();
      ret._m33 = reader.ReadSingle();
      ret._m34 = reader.ReadSingle();
      ret._m41 = reader.ReadSingle();
      ret._m42 = reader.ReadSingle();
      ret._m43 = reader.ReadSingle();
      ret._m44 = reader.ReadSingle();
      return ret;
    }
    public static quat ReadQuat(this System.IO.BinaryReader reader)
    {
      quat v = new quat();
      v.x = reader.ReadSingle();
      v.y = reader.ReadSingle();
      v.z = reader.ReadSingle();
      v.w = reader.ReadSingle();
      return v;
    }
    public static Box2f ReadBox2f(this System.IO.BinaryReader reader)
    {
      Box2f box = new Box2f();
      box._min = reader.ReadVec2();
      box._max = reader.ReadVec2();
      return box;
    }
    public static Box3f ReadBox3f(this System.IO.BinaryReader reader)
    {
      Box3f box = new Box3f();
      box._min = reader.ReadVec3();
      box._max = reader.ReadVec3();
      return box;
    }
    public static OOBox3f ReadOOBox3f(this System.IO.BinaryReader reader)
    {
      OOBox3f b = new OOBox3f();
      b.Verts = reader.Read<vec3>();
      return b;
    }
    public static DateTime ReadDateTime(this System.IO.BinaryReader reader)
    {
      //https://stackoverflow.com/questions/15919598/serialize-datetime-as-binary
      DateTime dt = new DateTime();
      var dat = reader.ReadInt64();

      long ticks = (long)(dat & 0x3FFFFFFFFFFFFFFF);
      DateTimeKind kind = (DateTimeKind)(dat >> 62);
      DateTime date = new DateTime(ticks, kind);

      return date;
    }
    public static T[] Read<T>(this System.IO.BinaryReader reader) where T : struct
    {
      Int32 count = reader.ReadInt32();
      byte[] buf = new byte[count];
      reader.Read(buf, 0, (Int32)count);

      return SerializeTools.Deserialize<T>(buf);
    }
  }



}//ns
