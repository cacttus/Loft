using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Reflection;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

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
    public static T[] Deserialize<T>(byte[] data) where T : struct
    {
      //Deserialize assuming data equals the size of the element to be deserialized
      var tsize = Marshal.SizeOf(default(T));
      Gu.Assert(data.Length % tsize == 0);
      var count = data.Length / tsize;

      return DeserializeFrom<T>(data, 0, count);
    }
    public static T[] DeserializeFrom<T>(byte[] data, int offset_bytes, int count_items) where T : struct
    {
      //Parse an array of struct out of a byte[] of raw data, the byte[] does not exactly need to match the struct, but must be > the struct
      var size = Marshal.SizeOf(default(T));
      var length = count_items * size;
      Gu.Assert(offset_bytes + length <= data.Length);
      var ret = new T[count_items];

      var pinnedHandle = GCHandle.Alloc(ret, GCHandleType.Pinned);
      Marshal.Copy(data, offset_bytes, pinnedHandle.AddrOfPinnedObject(), length);
      pinnedHandle.Free();

      return ret;
    }
    public static T DeserializeFrom<T>(byte[] data, int offset_bytes) where T : struct
    {
      //Parse an array of struct out of a byte[] of raw data
      var length = Marshal.SizeOf(default(T));
      Gu.Assert(offset_bytes + length <= data.Length);
      var ret = new T();

      var pinnedHandle = GCHandle.Alloc(ret, GCHandleType.Pinned);
      Marshal.Copy(data, offset_bytes, pinnedHandle.AddrOfPinnedObject(), length);
      pinnedHandle.Free();

      return ret;
    }
    public static unsafe byte[] Serialize<T>(T data) where T : struct
    {
      //https://www.genericgamedev.com/general/converting-between-structs-and-byte-arrays/
      var size = Marshal.SizeOf(data);
      var bytes = new byte[size];

      var ptr = Marshal.AllocHGlobal(size);
      Marshal.StructureToPtr(data, ptr, true);
      Marshal.Copy(ptr, bytes, 0 * size, size);
      Marshal.FreeHGlobal(ptr);
      return bytes;
    }
    public static byte[] Serialize<T>(T[] items) where T : struct
    {
      ///** Test this
      var size = Marshal.SizeOf(default(T));
      var ret = new byte[items.Length * size];

      var items_h = GCHandle.Alloc(items, GCHandleType.Pinned);
      Marshal.Copy(items_h.AddrOfPinnedObject(), ret, 0, ret.Length);
      items_h.Free();

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
    //Note:
    //  Reference types that do not subclass DataBlock are directly serialized (no references)
    //  DataBlock references are serialized as an ID (ulong)
    //  List<DataBlock> and DataBlock[] are serialized as a list of ID (ulong)
    //  Value types and arrays/lists of value types are directly serialized.

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

    private ClassLog _log;

    public WorldFile() { }

    public bool SaveWorld(World w)
    {
      var ms = Gu.Milliseconds();
      string worldfn = GetWorldFileName(w);

      _log = new ClassLog(System.IO.Path.GetFileName(worldfn), Gu.EngineConfig.Debug_LogSerializationDetails, true);

      dbg_writeObjectCalls = 0;
      dbg_worldObjectsWritten = 0;
      dbg_materialsWritten = 0;
      dbg_meshDatasWritten = 0;
      dbg_meshViewsWritten = 0;
      dbg_componentsWritten = 0;

      try
      {
        _log.Append($"Writing {worldfn} ...");
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

        _log.AppendLine($"...Saved {StringUtil.FormatPrec((double)bytes / 1024.0, 1)}kB {Gu.Milliseconds() - ms}ms");
        _log.AppendLine($" WriteObject: {dbg_writeObjectCalls}");
        _log.AppendLine($" WorldObject: {dbg_worldObjectsWritten}");
        _log.AppendLine($" Material:    {dbg_materialsWritten}");
        _log.AppendLine($" MeshData:    {dbg_meshDatasWritten}");
        _log.AppendLine($" MeshView:    {dbg_meshViewsWritten}");
        _log.AppendLine($" Component:   {dbg_componentsWritten}");

        _log.Print();
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
      if (!IsSystemType(forp.FieldType))
      {
        string dbn = "";
        if (forp._subItemValue != null && forp._subItemValue is DataBlock)
        {
          dbn = (forp._subItemValue as DataBlock).Name;
        }
        string propstr = $" {new string(' ', depth)}->{forp.Name.ToString()} {forp.FieldType.Name.ToString()} {(isnull ? "(null)" : dbn)}";
        _log.Info(propstr);
      }
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
    private void UpdateStats(FieldOrProp obt)
    {
      if (obt != null)
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
    }
    private void WriteObject(BinaryWriter bw, FieldOrProp? parent_field, object? this_obj, Dictionary<ulong, RWRef> refs, int depth)
    {
      Gu.Assert(refs != null);
      Gu.Assert(_fieldTable != null);
      Gu.Assert(bw != null);

      UpdateStats(parent_field);

      //Write class header
      if (WriteObjectHeader(bw, this_obj))
      {
        Type parent_type = this_obj.GetType();
        if (_fieldTable.TryGetValue(parent_type, out var classlayout))
        {
          foreach (var fieldorprop in classlayout.Props)
          {
            Gu.Assert(fieldorprop != null);
            var sub_val = fieldorprop.GetValue(this_obj);
            var sub_ft = fieldorprop.FieldType;

            //Write field type hash.
            WriteTypeHash(bw, sub_ft);

            //null values are a prolblem because we cant cast it, so we handle them here..
            //check for reference or nullable value.. then write it early.

            //ref types must come before value types here.
            if (TryNullValue(bw, fieldorprop, sub_val, refs, depth + 1)) { }
            else if (TryDataBlock(bw, fieldorprop, sub_val, refs, depth + 1)) { }
            else if (TryRefList(bw, fieldorprop, sub_val, refs, depth + 1)) { }
            else if (TryValueOrValueList(bw, fieldorprop, sub_val, depth + 1)) { }
            else if (_fieldTable.Keys.Contains(sub_ft))
            {
              //direct write - class that is not a ref, will have all data copied
              WriteObject(bw, fieldorprop, sub_val, refs, depth + 1);
            }
            else
            {
              string msg = $"Field type '{sub_ft.FullName}' was not handled.";
              _log.Error(msg);
              Gu.DebugBreak();
              Gu.BRThrowException(msg);
            }
          }
        }
      }

    }
    private bool WriteObjectHeader(BinaryWriter bw, object? obj)
    {
      if (obj != null)
      {
        WriteNotNull(bw);
        var parent_type = obj.GetType();
        WriteTypeHash(bw, parent_type);// [byte null] [long id] [long, .., long, .. ]
        return true;
      }
      else
      {
        WriteNull(bw);
        return false;
      }
    }
    private bool TryNullValue(BinaryWriter bw, FieldOrProp ft, object? val, Dictionary<ulong, RWRef> refs, int depth)
    {
      //Check for nullable value types and write the boolen header
      if (val == null)
      {
        WriteNull(bw);
        return true;
      }
      return false;
    }
    private bool TryValueOrValueList(BinaryWriter bw, FieldOrProp ft, object? val, int depth)
    {
      //write a System type (struct type) and for nullables
      var typ = ft.FieldType;
      var utyp = Nullable.GetUnderlyingType(typ);
      if (utyp != null)
      {
        typ = utyp;
      }

      if (typ.IsEnum)
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
      else if (TryVal<Boolean>(bw, ft, val)
            || TryVal<Byte>(bw, ft, val)
            || TryVal<Char>(bw, ft, val)
            || TryVal<Int16>(bw, ft, val)
            || TryVal<UInt16>(bw, ft, val)
            || TryVal<Int32>(bw, ft, val)
            || TryVal<UInt32>(bw, ft, val)
            || TryVal<Int64>(bw, ft, val)
            || TryVal<UInt64>(bw, ft, val)
            || TryVal<Single>(bw, ft, val)
            || TryVal<Double>(bw, ft, val)
            || TryVal<Decimal>(bw, ft, val)
            || TryVal<DateTime>(bw, ft, val)
            || TryVal<vec2>(bw, ft, val)
            || TryVal<vec3>(bw, ft, val)
            || TryVal<vec4>(bw, ft, val)
            || TryVal<mat2>(bw, ft, val)
            || TryVal<mat3>(bw, ft, val)
            || TryVal<mat4>(bw, ft, val)
            )
      {
        //dont log integral types, maybe some better way to do it
        // LogWriteProp(val.GetType(), depth, val == null);
        return true;
      }
      return false;
    }
   
    private bool TryRefList(BinaryWriter bw, FieldOrProp ft, object? val, Dictionary<ulong, RWRef> refs, int depth)
    {
      //write out any list/array of references or DataBlock
      var typ = ft.FieldType;
      if (val is System.Collections.IEnumerable)
      {
        //TODO: check if is array, list or dictionary.
        List<Type> argTypes = new List<Type>();
        if (typ.IsGenericType)
        {
          argTypes = typ.GetGenericArguments().ToList();
        }
        else if (typ.IsArray)
        {
          argTypes.Add(ft.FieldType.GetElementType());
        }
        var ebl = (val as System.Collections.IEnumerable);

        //you have to enumerate all items here, I think, it's ok we'll do that anyway..
        int n = 0;
        foreach (var item in ebl)
        {
          n++;
        }
        bw.Write((int)n);

        //Write Items, or Key,Value
        n = 0;
        foreach (var item in ebl)
        {
          foreach (var argtype in argTypes)
          {
            var itemProp = new FieldOrProp(argtype, $"{ft.Name}[{n}]", item);
            if (item is DataBlock)
            {
              Gu.Assert(TryDataBlock(bw, itemProp, item, refs, depth));
            }
            else
            {
              WriteObject(bw, itemProp, item, refs, depth);
            }
          }

          n++;
        }

        return true;
      }

      return false;
    }
    private bool TryDataBlock(BinaryWriter bw, FieldOrProp ft, object? val, Dictionary<ulong, RWRef> refs, int depth)
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
          _log.Error(msg);
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
    private bool TryVal<T>(BinaryWriter bw, FieldOrProp ft, object? val) where T : struct
    {
      //Try scalar value, or array/ list 
      //typeof(List<>).MakeGenericType(elementType);
      if (val is T)
      {
        //TODO: OPTIMIZE:
        // writing <T> is incorrect because it calls the generic T serializer here instead of bw.Write(boolean)..
        //however is it so much slower? not sure
        bw.Write((T)val);
        return true;
      }
      if (ft.FieldType == typeof(List<T>))
      {
        bw.Write<T>(((List<T>)val).ToArray());
        return true;
      }

      if (ft.FieldType == typeof(T[]))
      {
        bw.Write<T>((T[])val);
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



    #endregion



















  }//cls



}//ns