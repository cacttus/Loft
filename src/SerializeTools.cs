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
    // public static ISerializeBinary? CreateObjectFromType(ResourceType type)
    // {
    //   if (type == ResourceType.FPSInputComponent) { return new FPSInputComponent(null); }
    //   else if (type == ResourceType.EventComponent) { return new EventComponent(); }
    //   else if (type == ResourceType.AnimationComponent) { return new AnimationComponent(null); }
    //   return null;
    // }
    // public static void SerializeRef<T>(BinaryWriter bw, DataRef<T> db) where T : DataBlock
    // {
    //   //Writing file loc every time is dumb, what if we have 1000 data references.. we need a table of file loc's mapped to Ddatablock
    //   //unique ID, File Location
    //   //Writes the datablock reference
    //   if (db != null)
    //   {
    //     var blk = (DataBlock?)db.Get;
    //     bw.Write((UInt64)blk.UniqueID);
    //   }
    //   else
    //   {
    //     bw.Write((UInt64)Library.NullID);
    //   }
    // }
    // public static DataRef<T> DeserializeRef<T>(BinaryReader br) where T : DataBlock
    // {
    //   DataRef<T> d = new DataRef<T>();
    //   d._ref = (UInt64)br.ReadUInt64();
    //   return d;
    // }    
    public static bool SerializeNullable(BinaryWriter bw, ISerializeBinary? obj)
    {
      if (SerializeNullable(bw, (object?)obj))
      {
        obj.Serialize(bw);
        return true;
      }
      return false;
    }
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
          if (Gu.EngineConfig.Debug_LogSerializationDetails)
          {
            compressinfo = ($", compressed: {precount} -> {postcount}");
          }
        }
        else
        {
          compressed = ms.GetBuffer();
          if (Gu.EngineConfig.Debug_LogSerializationDetails)
          {
            compressinfo = ($", length: {compressed.Length}");
          }
        }

        bwFile.Write(blockname);
        bwFile.Write((bool)compress);
        bwFile.Write((UInt32)Proteus.Crc32.Compute(compressed));
        bwFile.Write((Int32)compressed.Length);
        bwFile.Write(compressed);

        if (Gu.EngineConfig.Debug_LogSerializationDetails)
        {
          Gu.Log.Debug($"Wrote block '{blockname}' {compressinfo}");
        }
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
    // public static void SerializeList<K>(BinaryWriter bw, List<K>? items) where K : ISerializeBinary
    // {
    //   if (SerializeNullable(bw, items))
    //   {
    //     bw.Write((Int32)items.Count);
    //     foreach (var n in items)
    //     {
    //       n.Serialize(bw);
    //     }
    //   }
    // }
    // public static List<K>? DeserializeList<K>(BinaryReader br, SerializedFileVersion ver) where K : ISerializeBinary, new()
    // {
    //   if (DeserializeNullable(br))
    //   {
    //     List<K> ret = new List<K>();
    //     var c = br.ReadInt32();
    //     for (int i = 0; i < c; i++)
    //     {
    //       var x = new K();
    //       x.Deserialize(br, ver);
    //       ret.Add(x);
    //     }
    //     return ret;
    //   }
    //   return null;
    // }
    // public static void SerializeRefList<K>(BinaryWriter bw, RefList<K>? items) where K : DataBlock
    // {
    //   if (SerializeNullable(bw, items))
    //   {
    //     bw.Write((Int32)items.Count);
    //     foreach (var n in items)
    //     {
    //       //bw.Write((Int32)n.ResourceType);
    //       n.Serialize(bw);
    //     }
    //   }
    // }
    // public static RefList<K>? DeserializeRefList<K>(BinaryReader br, SerializedFileVersion ver) where K : DataBlock
    // {
    //   if (DeserializeNullable(br))
    //   {
    //     RefList<K> ret = new RefList<K>();
    //     var c = br.ReadInt32();
    //     for (int i = 0; i < c; i++)
    //     {
    //       //ResourceType rt = (ResourceType)br.ReadInt32();
    //       var x = new DataRef<K>();
    //       x.Deserialize(br, ver);
    //       ret.AddRef(x);
    //     }
    //     return ret;
    //   }
    //   return null;
    // }
    // public static void SerializeDictionary<K, V>(BinaryWriter bw, Dictionary<K, V>? items) where V : ISerializeBinary where K : struct
    // {
    //   if (SerializeNullable(bw, items))
    //   {
    //     bw.Write((Int32)items.Count);
    //     foreach (var n in items)
    //     {
    //       bw.Write<K>(n.Key);
    //       n.Value.Serialize(bw);
    //     }
    //   }
    // }
    // public static Dictionary<K, V>? DeserializeDictionary<K, V>(BinaryReader br, SerializedFileVersion ver) where V : ISerializeBinary, new() where K : struct
    // {
    //   //how to make k be notnull
    //   if (DeserializeNullable(br))
    //   {
    //     var ret = new Dictionary<K, V>();
    //     var c = br.ReadInt32();
    //     for (int i = 0; i < c; i++)
    //     {
    //       var w = br.Read<K>();
    //       Gu.Assert(w.Length == 1);
    //       var x = new V();
    //       x.Deserialize(br, ver);
    //       ret.Add(w[0], x);
    //     }
    //     return ret;
    //   }
    //   return null;
    // }
    public static unsafe byte[] Serialize(string data)
    {
      var size = Marshal.SizeOf(data);
      var bytes = new byte[size];

      var ptr = Marshal.AllocHGlobal(size);
      Marshal.StructureToPtr(data, ptr, true);
      Marshal.Copy(ptr, bytes, 0 * size, size);
      Marshal.FreeHGlobal(ptr);

      return bytes;
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



  }//cls

  public enum SerializationState
  {
    None,
    Serializing,
    Error,
    Success
  }
  public class ClassLayout
  {
    //Store class metadata for file upgrading
    public int Version;
    public Type ClassType;
    public List<FieldOrProp> Props = new List<FieldOrProp>();
  }
  public class FieldOrProp
  {
    //field or prop
    public int Version;
    public FieldInfo? _field = null;
    public PropertyInfo? _prop = null;
    public Type? _subItemType = null; //for the items in a list
    public string? _subItemName = null;
    public object? _subItemValue = null;

    public string Name
    {
      get
      {
        if (_field != null)
        {
          return _field.Name;
        }
        else if (_prop != null)
        {
          return _prop.Name;
        }
        else
        {
          return _subItemName;
        }
      }
    }
    public object? GetValue(object? parentobj)
    {
      if (_field != null)
      {
        Gu.Assert(parentobj != null);
        return _field.GetValue(parentobj);
      }
      else if (_prop != null)
      {
        Gu.Assert(parentobj != null);
        return _prop.GetValue(parentobj);
      }
      else
      {
        return _subItemValue;
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
        else if (_prop != null)
        {
          return _prop.PropertyType;
        }
        else
        {
          return _subItemType;
        }
      }
    }
    public FieldOrProp(FieldInfo f)
    {
      _field = f;
    }
    public FieldOrProp(PropertyInfo f)
    {
      _prop = f;
    }
    public FieldOrProp(Type subItemType, string subItemName, object? subItemValue)
    {
      _subItemName = subItemName;
      _subItemType = subItemType;
      _subItemValue = subItemValue;
    }
  }//cls

  public class WorldFile
  {
    private class RWRef
    {
      public bool IsWritten = false;
      public DataBlock? DataBlock = null;
      public FieldOrProp FieldOrProp;
    }

    //world area file: objects + types
    private const string c_strSaveWorldVersion = "0.01";
    private const string c_strSaveWorldHeader = "WorldFilev" + c_strSaveWorldVersion;
    private long _bytesRW = 0;    //bytes written/read
    private SerializationState State = SerializationState.None;
    private Dictionary<Type, ClassLayout>? _fieldTable = null;
    private List<string> _msgs = new List<string>();
    private int dbg_writeObjectCalls = 0;
    private int dbg_worldObjectsWritten = 0;
    private int dbg_materialsWritten = 0;
    private int dbg_meshDatasWritten = 0;
    private int dbg_meshViewsWritten = 0;
    private int dbg_componentsWritten = 0;

    public WorldFile() { }

    public bool SaveWorld(World w)
    {
      var ms = Gu.Milliseconds();
      string worldfn = GetWorldFileName(w);

      dbg_writeObjectCalls = 0;
      dbg_worldObjectsWritten = 0;
      dbg_materialsWritten = 0;
      dbg_meshDatasWritten = 0;
      dbg_meshViewsWritten = 0;
      dbg_componentsWritten = 0;

      try
      {
        _fieldTable = new Dictionary<Type, ClassLayout>();
        BuildFieldTable(w.GetType());

        if (!System.IO.Directory.Exists(Gu.SavePath))
        {
          System.IO.Directory.CreateDirectory(Gu.SavePath);
        }
        Dictionary<ulong, RWRef> refs = new Dictionary<ulong, RWRef>();

        var enc = Encoding.GetEncoding("iso-8859-1");
        using (var fs = System.IO.File.OpenWrite(worldfn))
        {
          using (var bwFile = new System.IO.BinaryWriter(fs, enc))
          {
            bwFile.Write((string)c_strSaveWorldHeader);
            WriteFieldTable(bwFile);
            WriteGlobs(bwFile, w);
            WriteObjectAndRefs(bwFile, null, w, refs, 0);
          }
        }

        var fi = new System.IO.FileInfo(worldfn);
        var bytes = fi.Length;

        Gu.Log.Debug($"Saved {StringUtil.FormatPrec((double)bytes / 1024.0, 1)}kB {Gu.Milliseconds() - ms}ms");
        Gu.Log.Debug($" WriteObject calls:{dbg_writeObjectCalls}");

        Gu.Log.Debug($" WorldObject: {dbg_worldObjectsWritten}");
        Gu.Log.Debug($" Material:    {dbg_materialsWritten}");
        Gu.Log.Debug($" MeshData:    {dbg_meshDatasWritten}");
        Gu.Log.Debug($" MeshView:    {dbg_meshViewsWritten}");
        Gu.Log.Debug($" Component:   {dbg_componentsWritten}");

      }
      catch (Exception ex)
      {
        Gu.Log.Error($"Failed to load world file{worldfn}", ex);
        return false;
      }

      return true;
    }
    public bool LoadWorld(World w)
    {
      string worldfn = GetWorldFileName(w);
      var enc = Encoding.GetEncoding("iso-8859-1");

      try
      {
        if (!System.IO.File.Exists(worldfn))
        {
          return false;
        }
        SerializedFileVersion version = new SerializedFileVersion(1000);
        using (var fs = System.IO.File.OpenRead(worldfn))
        using (var br = new System.IO.BinaryReader(fs, enc))
        {
          string h = br.ReadString();
          if (h != c_strSaveWorldHeader)
          {
            Gu.BRThrowException("World header '" + h + "' does not match current header version '" + c_strSaveWorldHeader + "'");
          }

        }
      }
      catch (Exception ex)
      {
        Gu.Log.Error($"Failed to load world file{worldfn}", ex);
        return false;
      }

      return true;
    }

    #region Serialize

    private void WriteObjectAndRefs(BinaryWriter bw, FieldOrProp? this_forp, object? this_obj, Dictionary<ulong, RWRef> refs, int depth)
    {
      //write object in CRC block, write refs recursively
      SerializeTools.SerializeBlock(this_obj.GetType().ToString(), bw, true, (wbw) =>
      {
        WriteObject(wbw.Writer, this_forp, this_obj, refs, depth);
      });

      var nots = refs.Values.Where((kvp) => kvp.IsWritten == false).ToList();

      if (Gu.EngineConfig.Debug_LogSerializationDetails)
      {
        Gu.Log.Debug($"{new string(' ', depth)} {refs.Keys.Count - nots.Count} refs written,  {nots.Count} new to write");
      }
      //Serialize DataBlock refs
      foreach (var reff in nots)
      {
        if (reff.IsWritten == false)
        {
          var db = reff.DataBlock;
          reff.IsWritten = true;
          WriteObjectAndRefs(bw, reff.FieldOrProp, db, refs, depth + 1);
        }
      }
    }
    private void WriteFieldTable(BinaryWriter writer)
    {
      //Write field table, useful to convert in case of file changes.
      SerializeTools.SerializeBlock("FieldTable", writer, false, (wbw) =>
      {
        //SerializeTools.SerializeJSON(_fieldTable);
        wbw.Writer.Write((Int32)_fieldTable.Count);
        foreach (var kvp in _fieldTable)
        {
          wbw.Writer.Write((String)kvp.Key.Name);
          var nh = kvp.Key.Name.GetHashCode();
          wbw.Writer.Write((Int32)nh);

          wbw.Writer.Write((Int32)kvp.Value.Props.Count);
          foreach (var field in kvp.Value.Props)
          {
            wbw.Writer.Write((string)field.Name);
            wbw.Writer.Write((Int32)field.Name.GetHashCode());
          }
        }
      });
    }
    private void WriteGlobs(BinaryWriter writer, World w)
    {
      Gu.Assert(w != null);
      Gu.Assert(w.Globs != null);

      writer.Write((Int32)w.Globs.Count);//ivec3
      foreach (var g in w.Globs)
      {
        writer.Write(g.Key);//ivec3
        if (g.Value != null)
        {
          writer.Write(true);
          SerializeTools.SerializeBlock(g.Value.Name, writer, false, (b) =>
          {
            g.Value.Serialize(b.Writer);
          });
        }
        else
        {
          writer.Write(false);
        }
      }
    }
    private void WriteTypeHash(BinaryWriter bw, Type t)
    {
      string typename = t.Name;
      ulong hash = Hash.HashString(typename);
      bw.Write((ulong)hash);
    }
    private void LogWriteProp(FieldOrProp forp, int depth, bool isnull = false)
    {
      if (Gu.EngineConfig.Debug_LogSerializationDetails && !IsSystemType(forp.FieldType))
      {
        string dbn = "";
        if (forp._subItemValue != null && forp._subItemValue is DataBlock)
        {
          dbn = (forp._subItemValue as DataBlock).Name;
        }
        Gu.Log.Debug($" {new string(' ', depth)}->{forp.Name.ToString()} {forp.FieldType.Name.ToString()} {(isnull ? "(null)" : dbn)}");
      }
    }
    private void UpdateStats(FieldOrProp obt)
    {
      dbg_writeObjectCalls++;
      if (obt.FieldType == typeof(WorldObject))
      {
        dbg_worldObjectsWritten++;
      }
      if (obt.FieldType == typeof(Material))
      {
        dbg_materialsWritten++;
      }
      if (obt.FieldType == typeof(MeshData))
      {
        dbg_meshDatasWritten++;
      }
      if (obt.FieldType == typeof(MeshView))
      {
        dbg_meshViewsWritten++;
      }
      if (obt.FieldType == typeof(Component))
      {
        dbg_componentsWritten++;
      }
    }
    private void WriteObject(BinaryWriter bw, FieldOrProp? this_forp, object? this_obj, Dictionary<ulong, RWRef> refs, int depth)
    {
      Gu.Assert(refs != null);
      Gu.Assert(_fieldTable != null);
      Gu.Assert(bw != null);

      Type this_ft;

      if (this_forp == null)
      {
        Gu.Assert(depth == 0);
        this_ft = typeof(World);
        Gu.Log.Debug($"--{this_ft.Name.ToString()}--");
      }
      else
      {
        LogWriteProp(this_forp, depth, this_obj == null);
        UpdateStats(this_forp);
        this_ft = this_forp.FieldType;
      }

      if (this_obj == null)
      {
        WriteNull(bw);
      }
      else
      {
        WriteNotNull(bw);
        WriteTypeHash(bw, this_ft);// [byte null] [long id] [long, .., long, .. ]
        if (_fieldTable.TryGetValue(this_ft, out var layout))
        {
          foreach (var fieldorprop in layout.Props)
          {
            Gu.Assert(fieldorprop != null);
            var sub_val = fieldorprop.GetValue(this_obj);
            var sub_ft = fieldorprop.FieldType;

            WriteTypeHash(bw, sub_ft);

            if (TryWriteLists(bw, fieldorprop, sub_val, refs, depth + 1)) { }
            else if (TryWriteDataBlock(bw, fieldorprop, sub_val, refs, depth + 1)) { }
            else if (_fieldTable.Keys.Contains(sub_ft))
            {
              //direct struct 
              //** must account for nullable types.
              //like vec3, or class that is not a ref, will have all data copied
              WriteObject(bw, fieldorprop, sub_val, refs, depth + 1);
            }
            else if (TryWriteSystemTypes(bw, fieldorprop, sub_val, depth + 1)) { }
            else
            {
              string msg = $"Field type '{sub_ft.FullName}' was not handled.";
              Gu.Log.Error(msg);
              Gu.DebugBreak();
              Gu.BRThrowException(msg);
            }


          }
        }
      }

    }
    private bool TryWriteSystemTypes(BinaryWriter bw, FieldOrProp ft, object? val, int depth)
    {
      //write a System type (struct type) 
      //Also must account for nullable system types.

      var nullableType = Nullable.GetUnderlyingType(ft.FieldType);
      //TODO: 
      //|| (nullableType!=null && nullableType.IsEnum)
      if (ft.FieldType.IsEnum)
      {
        LogWriteProp(ft, depth, val == null);
        bw.Write((Int32)val);
        return true;
      }
      else if (val is String)
      {
        LogWriteProp(ft, depth, val == null);
        bw.Write((string)val);
        return true;
      }
      else if (TryWrite<Boolean>(bw, val)
            || TryWrite<Byte>(bw, val)
            || TryWrite<Char>(bw, val)
            || TryWrite<Int16>(bw, val)
            || TryWrite<UInt16>(bw, val)
            || TryWrite<Int32>(bw, val)
            || TryWrite<UInt32>(bw, val)
            || TryWrite<Int64>(bw, val)
            || TryWrite<UInt64>(bw, val)
            || TryWrite<Single>(bw, val)
            || TryWrite<Double>(bw, val)
            || TryWrite<Decimal>(bw, val)
            || TryWrite<DateTime>(bw, val)
            )
      {
        //dont log integral types, maybe some better way to do it
        // LogWriteProp(val.GetType(), depth, val == null);
        return true;
      }
      return false;
    }
    private bool TryWriteLists(BinaryWriter bw, FieldOrProp ft, object? val, Dictionary<ulong, RWRef> refs, int depth)
    {
      if (ft.Name.Contains("_children"))
      {
        Gu.Trap();
      }
      //       foreach(var t in _fieldTable.Keys){
      // //this is much easier.
      //       }
      if (
        TryWriteArray(bw, ft, val, refs, depth)
        || TryWriteList<WorldObject>(bw, ft, val, refs, depth)
        || TryWriteList<Component>(bw, ft, val, refs, depth)
        || TryWriteList<Constraint>(bw, ft, val, refs, depth)
        || TryWriteList<Material>(bw, ft, val, refs, depth)
        || TryWriteList<MeshData>(bw, ft, val, refs, depth)
        || TryWriteList<MeshView>(bw, ft, val, refs, depth)
        || TryWriteList<Image>(bw, ft, val, refs, depth)
        || TryWriteList<Texture>(bw, ft, val, refs, depth)
        || TryWriteList<Shader>(bw, ft, val, refs, depth)
        || TryWriteList<MeshDataLoader>(bw, ft, val, refs, depth)
        || TryWriteList<Camera3D>(bw, ft, val, refs, depth)
        || TryWriteList<ImageFile>(bw, ft, val, refs, depth)
        || TryWriteList<ImageGen>(bw, ft, val, refs, depth)
        || TryWriteList<TextureSlot>(bw, ft, val, refs, depth)
        || TryWriteList<PBRTextureInput>(bw, ft, val, refs, depth)
        || TryWriteList<GPUBuffer>(bw, ft, val, refs, depth)
        )
      {
        return true;
      }
      return false;
    }
    private bool TryWriteArray(BinaryWriter bw, FieldOrProp ft, object? val, Dictionary<ulong, RWRef> refs, int depth)
    {
      //Note: We support serialization of struct arrays with a simple method 
      //find a way to just call bw.Write(..) Marhsal.sizeof<T>

      if (ft.FieldType.IsArray)
      {
        var arrtype = ft.FieldType.GetElementType();

        LogWriteProp(ft, depth, val == null);
        if (val == null)
        {
          WriteNull(bw);
        }
        else
        {
          var ls = (val as System.Collections.IEnumerable); //System.Collections.IEnumerable - non-generic
          int n = 0;
          foreach (var item in ls)
          {
            n++;
          }
          bw.Write((int)n);
          //for (int iitem = 0; iitem < ls.Count(); iitem++)
          n = 0;
          foreach (var item in ls)
          {
            //var item = ls.ElementAt(iitem);
            var itemProp = new FieldOrProp(arrtype, $"{ft.Name}-item{n}", item);
            if (item is DataBlock)
            {
              Gu.Assert(TryWriteDataBlock(bw, itemProp, item, refs, depth));
            }
            else
            {
              WriteObject(bw, itemProp, item, refs, depth);
            }
            n++;
          }

        }
        return true;
      }
      return false;
    }

    private bool TryWriteList<T>(BinaryWriter bw, FieldOrProp ft, object? val, Dictionary<ulong, RWRef> refs, int depth)
    {
      //write List<T> or Array<T>

      //we could strictly use arrays. but then we'd have trouble with Dictionary<>

      //       if (ft.FieldType.IsGenericType)
      // {
      //   var args = ft.FieldType.GetGenericArguments().ToList();

      // }
      //Type x ;
      //object? bb;
      //var y = bb as x;
      //Type.makeMakeGenericType()

      if (ft.FieldType == typeof(List<T>))
      {
        LogWriteProp(ft, depth, val == null);
        if (val == null)
        {
          WriteNull(bw);
        }
        else
        {
          var ls = (val as IEnumerable<T>);
          bw.Write((int)ls.Count());
          for (int iitem = 0; iitem < ls.Count(); iitem++)
          {
            var item = ls.ElementAt(iitem);
            var itemProp = new FieldOrProp(typeof(T), $"{ft.Name}-item{iitem}", item);
            if (item is DataBlock)
            {
              Gu.Assert(TryWriteDataBlock(bw, itemProp, item, refs, depth));
            }
            else
            {
              WriteObject(bw, itemProp, item, refs, depth);
            }
          }

        }
        return true;
      }
      return false;
    }
    private void TruWriteRefDictionary(BinaryWriter bw, FieldOrProp ft, object? val, int depth)
    {
      Gu.BRThrowException($"Could not serialize dict of type '{ft.ToString()}'");
      LogWriteProp(ft, depth);

    }
    private bool TryWriteDataBlock(BinaryWriter bw, FieldOrProp ft, object? val, Dictionary<ulong, RWRef> refs, int depth)
    {
      if (val is DataBlock)
      {
        LogWriteProp(ft, depth, val == null);
        var db = val as DataBlock;
        WriteRef(bw, db);
        if (val != null)
        {
          if (!refs.TryGetValue(db.UniqueID, out var dum))
          {
            refs.Add(db.UniqueID, new RWRef() { DataBlock = db, FieldOrProp = ft });
          }
        }
        return true;
      }
      return false;
    }
    private void WriteRef(BinaryWriter bw, DataBlock? db)
    {
      if (db == null)
      {
        bw.Write((ulong)0);//zero id, not null , invalid < Library.c_iIDStart 
      }
      else
      {
        if (db.UniqueID < Lib.c_iIDStart)
        {
          string msg = $"Object '{db.Name}' ({db.GetType().Name.ToString()}) had no UniqueID";
          Gu.Log.Error(msg);
          Gu.DebugBreak();
          Gu.BRThrowException(msg);
        }
        ulong dbid = db.UniqueID;
        bw.Write((ulong)dbid);
      }
    }
    private void WriteNull(BinaryWriter bw)
    {
      bw.Write((byte)0);
    }
    private void WriteNotNull(BinaryWriter bw)
    {
      bw.Write((byte)1);
    }
    private bool IsSystemType(Type ttt)
    {
      if (
      ttt == typeof(Boolean) ||
      ttt == typeof(Byte) ||
      ttt == typeof(Char) ||
      ttt == typeof(Int16) ||
      ttt == typeof(UInt16) ||
      ttt == typeof(Int32) ||
      ttt == typeof(UInt32) ||
      ttt == typeof(Int64) ||
      ttt == typeof(UInt64) ||
      ttt == typeof(Single) ||
      ttt == typeof(Double) ||
      ttt == typeof(Decimal) ||
      ttt == typeof(DateTime))
      {
        return true;
      }
      return false;
    }    
    private bool TryWrite<T>(BinaryWriter bw, object? val) where T : struct
    {
      if (val is T)
      {
        bw.Write((T)val);
        return true;
      }
      return false;
    }

    #endregion
    #region Metadata

    private void BuildFieldTable(Type type)
    {
      //build a table of serializable fields mapped to types.
      // [DataContract] [DataMember] - are used to mark classes/fields
      Gu.Assert(_fieldTable != null);

      ClassLayout? fihash = null;
      if (_fieldTable.TryGetValue(type, out fihash))
      {
        //Already built this type
        return;
      }

      ClassLayout cl = new ClassLayout();
      cl.ClassType = type;
      cl.Props = BuildFieldTableForClass(type);

      _fieldTable.Add(type, cl);

      foreach (var fp in cl.Props)
      {
        List<Type> types = ExpandField(fp);
        foreach (var sub in types)
        {
          BuildFieldTable(sub);
        }
      }

    }
    private List<Type> ExpandField(FieldOrProp fp)
    {
      //Expand types that are on fields with [DataContract] [DataMember], specifically ones that we care about
      List<Type> types = new List<Type>();

      types.Add(fp.FieldType);

      for (int iii = 0; Gu.WhileTrueGuard(iii, Gu.c_intMaxWhileTrueLoop); iii++)
      {
        bool mayHaveSubTypes = false;
        List<Type> newtypes = new List<Type>();
        foreach (var gt in types)
        {

          //OK so - i think we should ALSO keep the array/nullable/generic types in the field table.
          //FieldOrProp stores these fields as this type - so we need the exact kind.
          //ug

          var nt = Nullable.GetUnderlyingType(gt);//This seems to be skipped for IsGenericType..
          if (nt != null)
          {
            Gu.Trap();//enum?
            newtypes.Add(gt);
          }
          else if (gt.IsArray)
          {
            newtypes.Add(gt.GetElementType());
          }
          else if (gt.IsGenericType)
          {
            //Unhandled generic type. Possibly our own type.
            newtypes.AddRange(gt.GetGenericArguments().ToList());
            mayHaveSubTypes = true;
          }
          else if (gt.IsEnum || gt.IsSystem())
          {
          }
          else if (nt != null)
          {
            newtypes.Add(nt);
          }
          else
          {
            newtypes.Add(gt);
          }
        }
        if (mayHaveSubTypes == false)
        {
          break;
        }
        else
        {
          types = newtypes;
        }
      }
      return types;
    }
    private bool CanSerializeProperty(Type parentType, PropertyInfo prop)
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
    private bool CanSerializeField(Type parentType, FieldInfo field)
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
    private List<FieldOrProp> BuildFieldTableForClass(Type parentType)
    {
      Gu.Assert(parentType != null);
      List<FieldOrProp> fihash = new List<FieldOrProp>();
      FieldInfo[] fields = parentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

      foreach (var field in fields)
      {
        if (CanSerializeField(parentType, field))
        {
          fihash.Add(new FieldOrProp(field));
        }
      }
      PropertyInfo[] props = parentType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
      foreach (var prop in props)
      {
        if (CanSerializeProperty(parentType, prop))
        {
          fihash.Add(new FieldOrProp(prop));
        }
      }
      return fihash;
    }

    #endregion
    #region Utils

    private string GetWorldFileName(World w)
    {
      string worldfile = w.Info.Name + ".world";
      return System.IO.Path.Combine(Gu.SavePath, worldfile);
    }
    public static int StringVersionToVersionID()
    {
      //parse string return version ID
      return 110010;
    }
    protected void msg(string st)
    {
      _msgs.Add($"Serialize Error: {st}");
    }
    public void PrintOutput()
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("");
      sb.AppendLine("Serialization output:");
      foreach (var e in _msgs)
      {
        sb.AppendLine(e);
      }
      sb.AppendLine("Done.");
      Gu.Log.Info(sb.ToString());
    }



    #endregion



















  }//cls



}//ns