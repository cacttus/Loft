using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Reflection;
using System.Text;

namespace PirateCraft
{
  public class WrappedBinaryWriter
  {
    public BinaryWriter Writer = null;
    public WrappedBinaryWriter(BinaryWriter bw)
    {
      Writer = bw;
    }
  }
  public class WrappedBinaryReader
  {
    public BinaryReader Reader = null;
    public WrappedBinaryReader(BinaryReader bw)
    {
      Reader = bw;
    }
  }
  public static class SerializeTools
  {
    public static ISerializeBinary? CreateObjectFromType(ResourceType type)
    {
      if (type == ResourceType.FPSInputComponent) { return new FPSInputComponent(null); }
      else if (type == ResourceType.EventComponent) { return new EventComponent(); }
      else if (type == ResourceType.AnimationComponent) { return new AnimationComponent(null); }
      return null;
    }
    public static void SerializeRef<T>(BinaryWriter bw, DataRef<T> db) where T : DataBlock
    {
      //Writing file loc every time is dumb, what if we have 1000 data references.. we need a table of file loc's mapped to Ddatablock
      //unique ID, File Location
      //Writes the datablock reference
      if (db != null)
      {
        var blk = (DataBlock?)db.Get;
        bw.Write((UInt64)blk.UniqueID);
      }
      else
      {
        bw.Write((UInt64)Library.NullID);
      }
    }
    public static DataRef<T> DeserializeRef<T>(BinaryReader br) where T : DataBlock
    {
      DataRef<T> d = new DataRef<T>();
      d._ref = (UInt64)br.ReadUInt64();
      return d;
    }
    // public static void DeserializeRef(BinaryReader br, object d, string fieldName)
    // {
    //   //cal GetMemberName() 
    //   //cal GetMemberName() 
    //   UInt64 id = br.ReadUInt64();
    //   Gu.Lib.AddFixUp(id, d, fieldName);
    // }
    public static bool SerializeNullable(BinaryWriter bw, object? obj, Action? iftrue = null)
    {
      if (obj == null)
      {
        bw.Write((Boolean)false);
        return false;
      }
      else
      {
        bw.Write((Boolean)true);
        if (iftrue != null)
        {
          iftrue();
        }
        return true;
      }
    }
    public static bool SerializeNullable(BinaryWriter bw, ISerializeBinary? obj)
    {
      if (SerializeNullable(bw, (object?)obj))
      {
        obj.Serialize(bw);
        return true;
      }
      return false;
    }
    public static bool DeserializeNullable(BinaryReader br)
    {
      var b = br.ReadBoolean();
      return b;
    }
    public static ISerializeBinary? DeserializeNullable(BinaryReader br, ISerializeBinary? constructed_obj, SerializedFileVersion version)
    {
      if (DeserializeNullable(br))
      {
        Gu.Assert(constructed_obj != null);
        constructed_obj.Deserialize(br, version);
      }
      else
      {
        constructed_obj = null;
      }
      return constructed_obj;
    }
    public static void SerializeBlock(string blockname, BinaryWriter bwFile, bool compress, Action<WrappedBinaryWriter> serializeAction)
    {
      //encapsulates a serialization routine in a file block, with a checksum.
      using (var ms = new System.IO.MemoryStream())
      {
        using (var bwMem = new System.IO.BinaryWriter(ms))
        {
          WrappedBinaryWriter ww = new WrappedBinaryWriter(bwMem);
          serializeAction(ww);
        }
        string compressinfo = "";
        byte[] compressed = null;
        if (compress)
        {
          var bf = ms.GetBuffer();
          compressed = Gu.Compress(bf);
          var precount = bf.Length;
          var postcount = compressed.Length;
          bf = null;
          compressinfo = ($", compressed: {precount} -> {postcount}");
        }
        else
        {
          compressed = ms.GetBuffer();
          compressinfo = ($", length: {compressed.Length}");
        }

        bwFile.Write(blockname);
        bwFile.Write((bool)compress);
        bwFile.Write((UInt32)Proteus.Crc32.Compute(compressed));
        bwFile.Write((Int32)compressed.Length);
        bwFile.Write(compressed);

        Gu.Log.Debug($"Wrote block '{blockname}' {compressinfo}");
      }
    }
    public static void DeserializeBlock(BinaryReader brFile, Action<WrappedBinaryReader> deserializeAction)
    {
      //encapsulates a serialization routine in a file block, with a checksum.

      string blockname = brFile.ReadString();
      bool is_compressed = brFile.ReadBoolean();
      UInt32 crc = brFile.ReadUInt32();
      int compressed_count = brFile.ReadInt32();
      var compressed = brFile.ReadBytes(compressed_count);
      UInt32 crcread = Proteus.Crc32.Compute(compressed);
      if (crc != crcread)
      {
        Gu.BRThrowException($"File Block {blockname}, DataLength:{compressed.Length} CRC32 error.");
      }

      byte[] decompressed = null;
      if (is_compressed)
      {
        decompressed = Gu.Decompress(compressed);
      }
      else
      {
        decompressed = compressed;
      }

      using (var ms = new System.IO.MemoryStream(decompressed))
      {
        using (var brMem = new System.IO.BinaryReader(ms))
        {
          WrappedBinaryReader ww = new WrappedBinaryReader(brMem);
          deserializeAction(ww);
        }
      }
    }
    public static void SerializeListOfStruct<K>(BinaryWriter bw, List<K>? items) where K : struct
    {
      if (SerializeNullable(bw, items))
      {
        bw.Write((Int32)items.Count);
        bw.Write<K>(items.ToArray());
      }
    }
    public static List<K>? DeserializeListOfStruct<K>(BinaryReader br, List<K>? items, SerializedFileVersion ver) where K : struct
    {
      if (DeserializeNullable(br))
      {
        var c = br.ReadInt32();
        var ret = br.Read<K>().ToList();
        return ret;
      }
      return null;
    }
    public static void SerializeList<K>(BinaryWriter bw, List<K>? items) where K : ISerializeBinary
    {
      if (SerializeNullable(bw, items))
      {
        bw.Write((Int32)items.Count);
        foreach (var n in items)
        {
          n.Serialize(bw);
        }
      }
    }
    public static List<K>? DeserializeList<K>(BinaryReader br, SerializedFileVersion ver) where K : ISerializeBinary, new()
    {
      if (DeserializeNullable(br))
      {
        List<K> ret = new List<K>();
        var c = br.ReadInt32();
        for (int i = 0; i < c; i++)
        {
          var x = new K();
          x.Deserialize(br, ver);
          ret.Add(x);
        }
        return ret;
      }
      return null;
    }
    public static void SerializeRefList<K>(BinaryWriter bw, RefList<K>? items) where K : DataBlock
    {
      if (SerializeNullable(bw, items))
      {
        bw.Write((Int32)items.Count);
        foreach (var n in items)
        {
          //bw.Write((Int32)n.ResourceType);
          n.Serialize(bw);
        }
      }
    }
    public static RefList<K>? DeserializeRefList<K>(BinaryReader br, SerializedFileVersion ver) where K : DataBlock
    {
      if (DeserializeNullable(br))
      {
        RefList<K> ret = new RefList<K>();
        var c = br.ReadInt32();
        for (int i = 0; i < c; i++)
        {
          //ResourceType rt = (ResourceType)br.ReadInt32();
          var x = new DataRef<K>();
          x.Deserialize(br, ver);
          ret.AddRef(x);
        }
        return ret;
      }
      return null;
    }
    public static void SerializeDictionary<K, V>(BinaryWriter bw, Dictionary<K, V>? items) where V : ISerializeBinary where K : struct
    {
      if (SerializeNullable(bw, items))
      {
        bw.Write((Int32)items.Count);
        foreach (var n in items)
        {
          bw.Write<K>(n.Key);
          n.Value.Serialize(bw);
        }
      }
    }
    public static Dictionary<K, V>? DeserializeDictionary<K, V>(BinaryReader br, SerializedFileVersion ver) where V : ISerializeBinary, new() where K : struct
    {
      //how to make k be notnull
      if (DeserializeNullable(br))
      {
        var ret = new Dictionary<K, V>();
        var c = br.ReadInt32();
        for (int i = 0; i < c; i++)
        {
          var w = br.Read<K>();
          Gu.Assert(w.Length == 1);
          var x = new V();
          x.Deserialize(br, ver);
          ret.Add(w[0], x);
        }
        return ret;
      }
      return null;
    }
    public static unsafe byte[] Serialize<T>(T data) where T : struct
    {
      var size = Marshal.SizeOf(data);
      var bytes = new byte[size];

      var ptr = Marshal.AllocHGlobal(size);
      Marshal.StructureToPtr(data, ptr, true);
      Marshal.Copy(ptr, bytes, 0 * size, size);
      Marshal.FreeHGlobal(ptr);

      return bytes;
    }
    public static unsafe byte[] Serialize<T>(T[] data) where T : struct
    {
      //This is .. terrible.
      var size = Marshal.SizeOf(data[0]);
      var bytes = new byte[size * data.Length];
      for (int di = 0; di < data.Length; di++)
      {
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(data[di], ptr, true);
        Marshal.Copy(ptr, bytes, di * size, size);
        Marshal.FreeHGlobal(ptr);
      }

      return bytes;
    }
    public static T[] Deserialize<T>(byte[] data) where T : struct
    {
      var tsize = Marshal.SizeOf(default(T));

      //Must be a multiple of the struct.
      Gu.Assert(data.Length % tsize == 0);

      var count = data.Length / tsize;
      T[] ret = new T[count];

      for (int di = 0; di < data.Length; di += tsize)
      {
        var ptr_struct = Marshal.AllocHGlobal(tsize);
        Marshal.StructureToPtr(data[di], ptr_struct, true);
        ret[di / tsize] = (T)Marshal.PtrToStructure(ptr_struct, typeof(T));
        Marshal.FreeHGlobal(ptr_struct);
      }

      return ret;
    }
    public static string SerializeJSON(object? obj, bool indented = true)
    {
      string json = JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
      return json;
    }
    public class FieldOrProp
    {
      public FieldInfo? _field = null;
      public PropertyInfo? _prop = null;
      public object? GetValue(object? parentobj)
      {
        if (_field != null)
        {
          return _field.GetValue(parentobj);
        }
        else
        {
          return _prop.GetValue(parentobj);
        }
      }
      public Type FieldType
      {
        get
        {
          if (_field != null)
          {
            return _field.FieldType;
          }
          else
          {
            return _prop.PropertyType;
          }
        }
      }
      public FieldOrProp(FieldInfo f) { _field = f; }
      public FieldOrProp(PropertyInfo f) { _prop = f; }
      public string Name { get { return _field != null ? _field.Name : _prop.Name; } }
    }
    public static void BuildFieldTable(Type type, Dictionary<Type, List<FieldOrProp>> fi, StringBuilder sb)
    {
      Gu.Assert(fi != null);
      Gu.Assert(sb != null);

      List<FieldOrProp>? fihash = null;
      if (fi.TryGetValue(type, out fihash))
      {
        return;
      }

      fi.Add(type, BuildFieldTableForClass(type, sb));

      if (fi.TryGetValue(type, out var fields))
      {
        foreach (var fp in fields)
        {
          List<Type> types = ExpandField(fp.FieldType);
          foreach (var sub in types)
          {
            BuildFieldTable(sub, fi, sb);
          }
        }
      }

    }
    public static List<Type> ExpandField(Type fieldRootType)
    {
      List<Type> types = new List<Type>();
      types.Add(fieldRootType);
      for (int iii = 0; Gu.WhileTrueGuard(iii, Gu.c_intMaxWhileTrueLoop); iii++)
      {
        bool mayHaveSubTypes = false;
        List<Type> newtypes = new List<Type>();
        foreach (var t in types)
        {
          if (t.IsGenericType)
          {
            //Unhandled generic type. Possibly our own type.
            newtypes.AddRange(t.GetGenericArguments().ToList());
            mayHaveSubTypes = true;
          }
          else if (t.IsEnum || t.IsSystem())
          {
          }
          else
          {
            newtypes.Add(t);
          }
        }
        types = newtypes;
        if (mayHaveSubTypes == false)
        {
          break;
        }
      }
      return types;
    }
    public static bool CanSerializeProperty(Type parentType, PropertyInfo prop, StringBuilder sb)
    {
      //Props backing fields are compiler generated... use GetProperties for actual props
      var cm = prop.GetAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>();
      if (cm == null)
      {
        var da = prop.GetAttribute<DataContractAttribute>();
        var dm = prop.GetAttribute<DataMemberAttribute>();
        if (dm != null || da != null)
        {
          return true;
        }
      }
      return false;
    }
    public static bool CanSerializeField(Type parentType, FieldInfo field, StringBuilder sb)
    {
      //Props backing fields are compiler generated... use GetProperties for actual props
      var cm = field.GetAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>();
      if (cm == null)
      {
        var da = field.GetAttribute<DataContractAttribute>();
        var dm = field.GetAttribute<DataMemberAttribute>();
        if (dm != null || da != null)
        {
          return true;
        }
      }
      return false;
    }
    public static List<FieldOrProp> BuildFieldTableForClass(Type parentType, StringBuilder sb)
    {
      Gu.Assert(sb != null);
      Gu.Assert(parentType != null);
      List<FieldOrProp> fihash = new List<FieldOrProp>();
      FieldInfo[] fields = parentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
      Gu.Trap();
      foreach (var field in fields)
      {
        if (CanSerializeField(parentType, field, sb))
        {
          fihash.Add(new FieldOrProp(field));
        }
      }
      PropertyInfo[] props = parentType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
      foreach (var prop in props)
      {
        if (CanSerializeProperty(parentType, prop, sb))
        {
          fihash.Add(new FieldOrProp(prop));
        }
      }
      return fihash;
    }
    public static void SerializeEverything(BinaryWriter bwFile, object? root, Dictionary<Type, List<FieldOrProp>>? fieldTable = null)
    {
      //Build class field info
      if (fieldTable == null)
      {
        Gu.Assert(root != null);
        StringBuilder sb = new StringBuilder();
        fieldTable = new Dictionary<Type, List<FieldOrProp>>();
        BuildFieldTable(root.GetType(), fieldTable, sb);
        if (sb.Length > 0)
        {
          var s = sb.ToString();
          Gu.Log.Error(s);
          Gu.DebugBreak();
          Gu.BRThrowException(s);
        }

        //Write field table
        SerializeTools.SerializeBlock("FieldTable", bwFile, false, (wbw) =>
        {
          // var ft = SerializeTools.SerializeJSON(fieldTable);
          // wbw.Writer.Write((string)ft);

          //Serialize field table
          wbw.Writer.Write((Int32)fieldTable.Count);
          foreach (var kvp in fieldTable)
          {
            wbw.Writer.Write((String)kvp.Key.Name);
            var nh = kvp.Key.Name.GetHashCode();
            wbw.Writer.Write((Int32)nh);

            wbw.Writer.Write((Int32)kvp.Value.Count);
            foreach (var field in kvp.Value)
            {
              wbw.Writer.Write((string)field.Name);
              wbw.Writer.Write((Int32)field.Name.GetHashCode());
            }
          }
        });

      }

      List<DataBlock> refs = new List<DataBlock>();
      SerializeTools.SerializeBlock(root.GetType().ToString(), bwFile, true, (wbw) =>
      {
        SerializeObject(wbw.Writer, root, refs, fieldTable);
      });

      //Serialize DataBlock refs
      foreach (var r in refs)
      {
        SerializeEverything(bwFile, r, fieldTable);
      }
    }
    private static void SerializeObject(BinaryWriter bw, object? root, List<DataBlock> refs, Dictionary<Type, List<FieldOrProp>> fieldTable)
    {
      Gu.Assert(refs != null);
      Gu.Assert(fieldTable != null);
      Gu.Assert(bw != null);
      if (root == null)
      {
        bw.Write((Byte)0);
      }
      else
      {
        bw.Write((Byte)1);
        bw.Write((Int32)root.GetType().Name.GetHashCode());//type name id
        if (fieldTable.TryGetValue(root.GetType(), out var fields))
        {
          foreach (var f in fields)
          {
            bw.Write((Int32)f.Name.GetHashCode());//field id
            var val = f.GetValue(root);
            if (val != null)
            {
              if (f.FieldType.IsList())
              {
                // bwFile.Write((Int32)val);
              }
              else if (f.FieldType.IsDictionary())
              {
                // SerializeTools.SerializeDictionary<
                //bwFile.Write((Int32)val);
              }
              else if (f.FieldType.IsEnum)
              {
                bw.Write((Int32)val);
              }
              else if (val is DataBlock)
              {
                //This is a ref.
                bw.Write((Int32)(val as DataBlock).UniqueID);
                refs.Add((val as DataBlock));
              }
              else if (fieldTable.Keys.Contains(f.FieldType))
              {
                //this is a struct of ours, like vec3, or a class that is not a ref, so it will have copied data when deserializing
                SerializeObject(bw, val, refs, fieldTable);
              }
              else if (val is String)
              {
                bw.Write((string)val);
              }
              else if (
                  TryWriteSysType<Boolean>(bw, val)
                 || TryWriteSysType<Byte>(bw, val)
                 || TryWriteSysType<Char>(bw, val)
                 || TryWriteSysType<Int16>(bw, val)
                 || TryWriteSysType<UInt16>(bw, val)
                 || TryWriteSysType<Int32>(bw, val)
                 || TryWriteSysType<UInt32>(bw, val)
                 || TryWriteSysType<Int64>(bw, val)
                 || TryWriteSysType<UInt64>(bw, val)
                 || TryWriteSysType<Single>(bw, val)
                 || TryWriteSysType<Double>(bw, val)
                 || TryWriteSysType<Decimal>(bw, val)
                 || TryWriteSysType<DateTime>(bw, val)
                  )
              {
              }
              else
              {
                Gu.BRThrowException($"Field type '{f.FieldType.Name}' was not handled.");
              }


            }
          }
        }
      }

    }
    public static bool TryWriteSysType<T>(BinaryWriter bw, object? val) where T : struct
    {
      if (val is T)
      {
        bw.Write((T)val);
        return true;
      }
      return false;
    }


  }//cs

}//ns