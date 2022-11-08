using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using OpenTK.Graphics.OpenGL4;

namespace PirateCraft
{
  #region Enums

  public enum ShaderVertexType
  {
    //System types for vertexes that allow them to be re-used across different shaders.
    //Feels like position/normal etc would be the correct usage, but we do need the
    //component count because, say plugging a v2 pos into a v3 pos makes no sense.
    Undefined,
    v2, v3, v4, //position
    c3, c4, //color
    n3, //vertex normal
    x2, //texcoord
    fn, //face normal
    vt, //vertex tangent
    gl_InstanceID, gl_InstanceIndex, //gl stuff

    // The following are not built-in types but still have identifiers
    Float,
    Double,
    Byte,
    UByte,
    Short,
    UShort,
    Int,
    UInt,
    Vec2, Vec3, Vec4,
    iVec2, iVec3, iVec4,
    uVec2, uVec3, uVec4,
    dVec2, dVec3, dVec4,
    Mat2, Mat3, Mat4,
  }

  #endregion
  [DataContract]
  public class DataComponent
  {
    public int SizeBytes { get { return _sizeBytes; } set { _sizeBytes = value; } }
    public int ComponentCount { get { return _componentCount; } set { _componentCount = value; } }
    public VertexAttribPointerType? FloatType { get { return _floatType; } set { _floatType = value; } }
    public VertexAttribIntegerType? IntPointerType { get { return _intType; } set { _intType = value; } }
    public VertexAttribDoubleType? DoublePointerType { get { return _doubleType; } set { _doubleType = value; } }
    public ShaderVertexType UserType { get { return _userType; } set { _userType = value; } }
    public int ByteOffset { get { return _byteOffset; } set { _byteOffset = value; } }
    public int AttribLocation { get { return _attribLocation; } set { _attribLocation = value; } }
    public string Name { get { return _name; } set { _name = value; } }// _v2 _c4 ...

    [DataMember] public int _sizeBytes = 0;
    [DataMember] public int _componentCount = 0;
    [DataMember] public VertexAttribPointerType? _floatType = null;
    [DataMember] public VertexAttribIntegerType? _intType = null;
    [DataMember] public VertexAttribDoubleType? _doubleType = null;
    [DataMember] public ShaderVertexType _userType = ShaderVertexType.Undefined;
    [DataMember] public int _lutHash = 0;
    [DataMember] public int _lutIndex = 0;
    [DataMember] public int _byteOffset = 0;
    [DataMember] public int _attribLocation = 0;
    [DataMember] public string _name = Lib.UnsetName;
  }
  [DataContract]
  public class GPUDataFormat : NamedObject
  {
    //Specifies access information for interleaved data (such as vertexes), and other buffer data for the GPU.
    //@note I figure we should serialize this and count it as scene data considering saved mesh data would require 
    //      a vertex format to parse correctly.

    #region Private: Members

    private static Dictionary<Type, GPUDataFormat> _formats = new Dictionary<Type, GPUDataFormat>();

    #endregion
    #region Public: Members

    public Dictionary<int, Dictionary<int, DataComponent>> ComponentsLUT { get { return _componentsLUT; } private set { _componentsLUT = value; } }
    public int VertexSizeBytes { get { return _vertexSizeBytes; } private set { _vertexSizeBytes = value; } }
    public int MaxLocation { get { return _componentsLUT.Count; } }
    public Type? Type { get { return _type; } }

    //Note:primary key is the shader vertex type OR the hash of the input name if no type is specified.
    private Dictionary<int, Dictionary<int, DataComponent>>? _componentsLUT = null;
    private List<DataComponent>? _componentsOrdered = null;
    private int _vertexSizeBytes = 0; // Size of the vert, including padding
    private Type? _type = null;

    #endregion
    #region Public: Static Methods

    public static GPUDataFormat GetDataFormat<T>()
    {
      return GetDataFormat(typeof(T));
    }
    public static GPUDataFormat GetDataFormat(Type ty)
    {
      GPUDataFormat? ret = null;
      if (!_formats.TryGetValue(ty, out ret))
      {
        ret = new GPUDataFormat(ty);
        _formats.Add(ty, ret);
      }
      return ret;
    }

    #endregion
    #region Public: Methods

    public GPUDataFormat(Type dataType) : base(dataType.Name)
    {
      _type = dataType;
      _vertexSizeBytes = Marshal.SizeOf(dataType);

      _componentsOrdered = _componentsOrdered.ConstructIfNeeded();
      _componentsLUT = _componentsLUT.ConstructIfNeeded();

      Gu.Assert(dataType.IsValueType, $"'{Name}': must be a value type.");

      //Try  add the value type e.g. vec3, short.., if fail, parse components as struct
      if (!TryAddComponent(dataType, 1, null))
      {
        var fieldFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        var vtx_comps = dataType.GetFields(fieldFlags);
        Gu.Assert(vtx_comps != null);
        foreach (var vtx_comp in vtx_comps)
        {
          //check value type
          Gu.Assert(vtx_comp.FieldType.IsValueType, $"'{Name}::{vtx_comp.Name}': Component type {vtx_comp.FieldType.ToString()} must be a value type.");
          var comp_fields = vtx_comp.FieldType.GetFields(fieldFlags);
          foreach (var comp_field in comp_fields)
          {
            Gu.Assert(comp_field.FieldType.IsValueType, $"'{Name}::{vtx_comp.Name}::{comp_field.Name}': Component field must be a value type.");
          }
          Gu.Assert(comp_fields != null);

          if (!TryAddComponent(vtx_comp.FieldType, comp_fields.Length, vtx_comp.Name))
          {
            Gu.Log.Error($"Could not find component '{vtx_comp.ToString()}'");
            Gu.DebugBreak();
          }
        }
      }
    }
    public bool TryGetComponent(ShaderVertexType cmp, int index, out DataComponent? val)
    {
      return TryGetComponent((int)cmp, index, out val);
    }
    public int GetComponentOffset(ShaderVertexType t, int index)
    {
      if (TryGetComponent(t, index, out var c))
      {
        return c.ByteOffset;
      }
      return -1;
    }
    public void BindVertexAttribs(GPUDataFormat previousFormat)
    {
      //Previous format is if we are binding multiple vertex buffers to the vao / shader, we use the last location of the attrib from that buffer
      int previousFormatMaxAttribLocation = 0;
      if (previousFormat != null)
      {
        previousFormatMaxAttribLocation = previousFormat.MaxLocation;
      }
      //Creates attribute arrays for all items in here.
      //Note: we use vec4 size offsets here because of the 16 byte padding required by GPUs.
      foreach (var comp_by_index in ComponentsLUT)
      {
        foreach (var comp_pair in comp_by_index.Value)
        {
          var comp = comp_pair.Value;

          GL.EnableVertexAttribArray(previousFormatMaxAttribLocation + comp.AttribLocation);

          if (comp.IntPointerType != null)
          {
            GL.VertexAttribIPointer(
              previousFormatMaxAttribLocation + comp.AttribLocation,
              comp.ComponentCount,
              comp.IntPointerType.Value,
              VertexSizeBytes,
              (IntPtr)(0 + comp.ByteOffset)
            );
          }
          else if (comp.FloatType != null)
          {
            GL.VertexAttribPointer(
              previousFormatMaxAttribLocation + comp.AttribLocation,
              comp.ComponentCount,
              comp.FloatType.Value,
              false,
              VertexSizeBytes,
              (IntPtr)(0 + comp.ByteOffset)
            );
          }
          else if (comp.DoublePointerType != null)
          {
            GL.VertexAttribLPointer(
              previousFormatMaxAttribLocation + comp.AttribLocation,
              comp.ComponentCount,
              comp.DoublePointerType.Value,
              VertexSizeBytes,
              (IntPtr)(0 + comp.ByteOffset)
            );
          }
        }
      }
    }

    #endregion
    #region Private: Methods

    private bool TryAddComponent(Type comp, int comp_count, string? field_name)
    {
      //Return false if the component is not present.. this may be a struct.
      VertexAttribPointerType? ftype = null;
      VertexAttribIntegerType? itype = null;
      VertexAttribDoubleType? dtype = null;

      ShaderVertexType ctype = ShaderVertexType.Undefined;

      //parse regular data format (vec3,float..) then try to parse the "special" field (vert, color, normal..)

      //float
      if (comp == typeof(Single)) { ftype = VertexAttribPointerType.Float; ctype = ShaderVertexType.Float; }
      else if (comp == typeof(vec2)) { ftype = VertexAttribPointerType.Float; ctype = ShaderVertexType.Vec2; }
      else if (comp == typeof(vec3)) { ftype = VertexAttribPointerType.Float; ctype = ShaderVertexType.Vec3; }
      else if (comp == typeof(vec4)) { ftype = VertexAttribPointerType.Float; ctype = ShaderVertexType.Vec4; }
      else if (comp == typeof(mat2)) { ftype = VertexAttribPointerType.Float; ctype = ShaderVertexType.Mat2; }
      else if (comp == typeof(mat3)) { ftype = VertexAttribPointerType.Float; ctype = ShaderVertexType.Mat3; }
      else if (comp == typeof(mat4)) { ftype = VertexAttribPointerType.Float; ctype = ShaderVertexType.Mat4; }
      //int
      else if (comp == typeof(byte)) { itype = VertexAttribIntegerType.Byte; ctype = ShaderVertexType.Byte; }
      else if (comp == typeof(sbyte)) { itype = VertexAttribIntegerType.UnsignedByte; ctype = ShaderVertexType.UByte; }
      else if (comp == typeof(short)) { itype = VertexAttribIntegerType.Short; ctype = ShaderVertexType.Short; }
      else if (comp == typeof(ushort)) { itype = VertexAttribIntegerType.UnsignedShort; ctype = ShaderVertexType.UShort; }
      else if (comp == typeof(int)) { itype = VertexAttribIntegerType.Int; ctype = ShaderVertexType.Int; }
      else if (comp == typeof(uint)) { itype = VertexAttribIntegerType.UnsignedInt; ctype = ShaderVertexType.UInt; }
      else if (comp == typeof(ivec2)) { itype = VertexAttribIntegerType.Int; ctype = ShaderVertexType.iVec2; }
      else if (comp == typeof(ivec3)) { itype = VertexAttribIntegerType.Int; ctype = ShaderVertexType.iVec3; }
      else if (comp == typeof(ivec4)) { itype = VertexAttribIntegerType.Int; ctype = ShaderVertexType.iVec4; }
      else if (comp == typeof(uvec2)) { itype = VertexAttribIntegerType.UnsignedInt; ctype = ShaderVertexType.uVec2; }
      else if (comp == typeof(uvec3)) { itype = VertexAttribIntegerType.UnsignedInt; ctype = ShaderVertexType.uVec3; }
      else if (comp == typeof(uvec4)) { itype = VertexAttribIntegerType.UnsignedInt; ctype = ShaderVertexType.uVec4; }
      //double
      else if (comp == typeof(Double)) { dtype = VertexAttribDoubleType.Double; ctype = ShaderVertexType.Double; }
      else if (comp == typeof(dvec2)) { dtype = VertexAttribDoubleType.Double; ctype = ShaderVertexType.dVec2; }
      else if (comp == typeof(dvec3)) { dtype = VertexAttribDoubleType.Double; ctype = ShaderVertexType.dVec3; }
      else if (comp == typeof(dvec4)) { dtype = VertexAttribDoubleType.Double; ctype = ShaderVertexType.dVec4; }
      else
      {
        return false;
      }

      int byte_size = Marshal.SizeOf(comp);

      if (field_name != null)
      {
        var specialtyp = TryParseSpecialInputType(field_name, comp_count);
        if (specialtyp != ShaderVertexType.Undefined)
        {
          ctype = specialtyp;
        }
      }
      else
      {
        field_name = comp.Name;
      }

      Gu.Assert(ctype != ShaderVertexType.Undefined);

      AddComponent_Ordered(field_name, comp_count, byte_size, ftype, itype, dtype, ctype);

      return true;
    }
    private ShaderVertexType TryParseSpecialInputType(string name, int comp_count)
    {
      //Parse system user type.
      //Kind of a hacky way to map vertex parameters to shader inputs.

      var res = ShaderVertexType.Undefined;

      name = name.Trim(new char[] { ' ', '\t', '\n', '\r' });

      //Name must start with a _
      if (!name.StartsWith('_'))
      {
        return ShaderVertexType.Undefined;
      }
      else if (name.Length >= 2 && name.Length <= 4)
      {
        int index = -1;
        if (name.Length > 2)
        {
          var index_str = name.Substring(2); //index like _v2, or _v04, etc - 
          if (Int32.TryParse(index_str, out index))
          {
            //TODO: index ,if needed ,probably not
          }
        }

        var type_str = name.Substring(1, 1); //v,f,c, => i2, v3, c4, m4, S2 .. etc
        if (type_str == "v" && comp_count == 2) { res = ShaderVertexType.v2; }
        else if (type_str == "v" && comp_count == 3) { res = ShaderVertexType.v3; }
        else if (type_str == "v" && comp_count == 4) { res = ShaderVertexType.v4; }
        else if (type_str == "c" && comp_count == 3) { res = ShaderVertexType.c3; }
        else if (type_str == "c" && comp_count == 4) { res = ShaderVertexType.c4; }
        else if (type_str == "n" && comp_count == 3) { res = ShaderVertexType.n3; }
        else if (type_str == "x" && comp_count == 2) { res = ShaderVertexType.x2; }
        else if (type_str == "f" && comp_count == 3) { res = ShaderVertexType.fn; }
        else if (type_str == "t" && comp_count == 3) { res = ShaderVertexType.vt; }
        else if (type_str == "y" && comp_count == 3) { res = ShaderVertexType.gl_InstanceID; }//never gonna remember this
        else if (type_str == "Y" && comp_count == 3) { res = ShaderVertexType.gl_InstanceIndex; }

      }

      return res;
    }
    private void AddComponent_Ordered(string name, int componentCount, int size_bytes,
                                     VertexAttribPointerType? ftype, VertexAttribIntegerType? itype, VertexAttribDoubleType? dtype,
                                     ShaderVertexType eUserType)
    {
      //add a new component, the order in which we add them matters.
      Gu.Assert(_componentsLUT != null);
      Gu.Assert(_componentsOrdered != null);

      //There can be only one
      Gu.Assert(!(ftype != null && dtype != null));
      Gu.Assert(!(ftype != null && itype != null));
      Gu.Assert(!(itype != null && dtype != null));

      DataComponent cm = new DataComponent();
      cm._floatType = ftype;
      cm._intType = itype;
      cm._componentCount = componentCount;
      cm._sizeBytes = size_bytes;
      cm._userType = eUserType;
      cm._attribLocation = _componentsOrdered.Count;
      cm._name = name;

      if (eUserType == ShaderVertexType.Undefined)
      {
        cm._lutHash = (int)Proteus.Crc32.Compute(cm._name);
      }
      else
      {
        cm._lutHash = (int)eUserType;
      }

      Dictionary<int, DataComponent>? byindex = null;
      if (!_componentsLUT.TryGetValue(cm._lutHash, out byindex))
      {
        byindex = new Dictionary<int, DataComponent>();
        _componentsLUT.Add(cm._lutHash, byindex);
      }
      var max_idx = 1;
      if (byindex.Keys.Count > 0)
      {
        max_idx = byindex.Keys.Max() + 1;
      }
      byindex.Add(max_idx, cm);
      cm._lutIndex = max_idx;

      cm._byteOffset = 0;
      foreach (var cmpi in _componentsOrdered)
      {
        cm._byteOffset += cmpi._sizeBytes;
      }
      _componentsOrdered.Add(cm);
    }
    private bool TryGetComponent(int cmpHash, int index, out DataComponent? val)
    {
      Gu.Assert(index > 0);//Index start at 1
      Gu.Assert(_componentsLUT != null);
      val = null;
      if (_componentsLUT.TryGetValue(cmpHash, out var by_index))
      {
        if (by_index.TryGetValue(index, out val))
        {
          return val != null;
        }
      }
      return false;
    }


    #endregion
  }//end datafrmt

  public class VertexPointer
  {
    //Generic vertex manipulation class
    //Allows us to pass around any vertex array and manipulate data (c++)
    //ex: we have 2 verts: v_v3x2n3 and v_v3c2t3x2n3c4.. this will let us get , for example, v3 and n3 from both in the same method.
    // The class will throw an exception if you try to access a component that does not exist on the type. 
    // ** Be careful, Intellisense may be wrong!
    #region Public: Classes

    public class VertexPointerOffset
    {
      public VertexPointer Pointer;
      public int ArrayIndex { get; private set; }
      public VertexPointerOffset(VertexPointer v, int arridx)
      {
        Pointer = v;
        ArrayIndex = arridx;
      }
      //The 01 methods are for 01 data.
      public vec3 _v { get { return Pointer.GetValue<vec3>(ShaderVertexType.v3, 1, ArrayIndex); } set { Pointer.SetValue<vec3>(ShaderVertexType.v3, 1, ArrayIndex, value); } }
      public vec3 _n { get { return Pointer.GetValue<vec3>(ShaderVertexType.n3, 1, ArrayIndex); } set { Pointer.SetValue<vec3>(ShaderVertexType.n3, 1, ArrayIndex, value); } }
      public vec3 _t { get { return Pointer.GetValue<vec3>(ShaderVertexType.vt, 1, ArrayIndex); } set { Pointer.SetValue<vec3>(ShaderVertexType.vt, 1, ArrayIndex, value); } }
      public vec2 _x { get { return Pointer.GetValue<vec2>(ShaderVertexType.x2, 1, ArrayIndex); } set { Pointer.SetValue<vec2>(ShaderVertexType.x2, 1, ArrayIndex, value); } }
      public vec2 _x2 { get { return Pointer.GetValue<vec2>(ShaderVertexType.x2, 2, ArrayIndex); } set { Pointer.SetValue<vec2>(ShaderVertexType.x2, 2, ArrayIndex, value); } }
      public uint _u { get { return Pointer.GetValue<uint>(ShaderVertexType.UInt, 1, ArrayIndex); } set { Pointer.SetValue<uint>(ShaderVertexType.UInt, 1, ArrayIndex, value); } }
      //index buffer index
      public uint index { get { return Pointer.GetIndex(ArrayIndex); } set { Pointer.SetIndex(ArrayIndex, value); } }
    }

    #endregion
    #region Public: Members

    public GPUDataFormat Format { get; private set; } = null;
    public object Verts { get; private set; } = null;
    public int Length { get; private set; } = 0;

    #endregion
    #region Public: Methods

    public VertexPointer(object verts)
    {
      var t = verts.GetType();
      if (!t.IsArray)
      {
        Gu.BRThrowException("Input vertex array type was not an array (no List<T>, just use array[]).");
      }
      else if (!t.GetElementType().IsValueType)
      {
        Gu.BRThrowException("Element type of input vertex array was not a struct (value type).");
      }
      Verts = verts;
      Format = GPUDataFormat.GetDataFormat(t.GetElementType());
      Length = (verts as Array).Length;
    }
    public VertexPointer(GPUBuffer b) : this(b.CopyFromGPU(), b.Format)
    {
      FromGpuDataPtr(b.CopyFromGPU(), b.Format);
    }
    public VertexPointer(GpuDataPtr verts, GPUDataFormat format)
    {
      FromGpuDataPtr(verts, format);
    }
    private void FromGpuDataPtr(GpuDataPtr verts, GPUDataFormat format)
    {
      Gu.Assert(verts != null);
      Gu.Assert(format != null);
      Gu.Assert(verts.Data != null);
      Gu.Assert(verts.Data is byte[]);
      Verts = verts.Data;
      Format = format;
      Length = verts.Count;
    }
    public uint GetIndex(int arrindex)
    {
      uint ret = 0;
      if (Format.Type == typeof(short))
      {
        ret = (uint)GetValue<short>(ShaderVertexType.Short, 1, arrindex);
      }
      else if (Format.Type == typeof(ushort))
      {
        ret = (uint)GetValue<ushort>(ShaderVertexType.UShort, 1, arrindex);
      }
      else if (Format.Type == typeof(int))
      {
        ret = (uint)GetValue<int>(ShaderVertexType.Int, 1, arrindex);
      }
      else if (Format.Type == typeof(uint))
      {
        ret = (uint)GetValue<uint>(ShaderVertexType.UInt, 1, arrindex);
      }
      else
      {
        Gu.BRThrowException("Invalid index format.");
      }
      return ret;
    }
    public void SetIndex(int arrindex, uint value)
    {
      if (Format.Type == typeof(short))
      {
        SetValue<short>(ShaderVertexType.Short, 1, arrindex, (short)value);
      }
      else if (Format.Type == typeof(ushort))
      {
        SetValue<ushort>(ShaderVertexType.UShort, 1, arrindex, (ushort)value);
      }
      else if (Format.Type == typeof(int))
      {
        SetValue<int>(ShaderVertexType.Int, 1, arrindex, (int)value);
      }
      else if (Format.Type == typeof(uint))
      {
        SetValue<uint>(ShaderVertexType.UInt, 1, arrindex, (uint)value);
      }
      else
      {
        Gu.BRThrowException("Invalid index format.");
      }
    }
    public VertexPointerOffset this[int i]
    {
      get
      {
        return new VertexPointerOffset(this, i);
      }
    }
    public T GetValue<T>(ShaderVertexType ctype, int comp_index, int off) where T : unmanaged
    {
      Gu.Assert(off < this.Length);
      //We could box this type, however we should technically never need to. C# should check bounds.
      unsafe
      {
        var pinnedHandle = GCHandle.Alloc(Verts, GCHandleType.Pinned);
        void* pt = pinnedHandle.AddrOfPinnedObject().ToPointer();
        byte* b = GetPtr(ctype, comp_index, off, pt);
        Gu.Assert(b != null);
        T ret = *((T*)b);
        pinnedHandle.Free();
        return ret;
      }
    }
    public void SetValue<T>(ShaderVertexType ctype, int comp_index, int off, T val) where T : unmanaged
    {
      Gu.Assert(off < this.Length);
      //We could box this type, however we should technically never need to. C# should check bounds.
      unsafe
      {
        var pinnedHandle = GCHandle.Alloc(Verts, GCHandleType.Pinned);
        void* pt = pinnedHandle.AddrOfPinnedObject().ToPointer();
        byte* b = GetPtr(ctype, comp_index, off, pt);
        Gu.Assert(b != null);
        *((T*)b) = val;
        pinnedHandle.Free();
      }
    }

    #endregion
    #region Private: Methods

    private unsafe byte* GetPtr(ShaderVertexType comp, int comp_index, int offset, void* pt)
    {
      byte* ret = null;
      if (Format.TryGetComponent(comp, comp_index, out var cmp))
      {
        Gu.Assert(cmp != null);
        int vsize = Format.VertexSizeBytes;
        int boff = cmp.ByteOffset;
        ret = (byte*)pt + offset * vsize + boff;
      }
      else
      {
        Gu.BRThrowException($"Component '{comp.ToString()}' not found on vertex format '{Format.Name}' ");
      }

      return ret;
    }

    #endregion
  }

  #region Vertex Formats

  //note we removed std430 padding .. this is erroneous.. we need to fix it
  [DataContract]
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3n3x2t3u1
  {
    //Default object vertex
    [DataMember] public vec3 _v;
    [DataMember] public vec3 _n;
    [DataMember] public vec2 _x;
    [DataMember] public vec3 _t;
    [DataMember] public uint _u;//Face ID, note this is convenient just because we had a pad value.
  }
  [DataContract]
  [StructLayout(LayoutKind.Sequential)]
  public struct v_debug_draw
  {
    //debug vertex
    [DataMember] public vec3 _v;
    [DataMember] public vec4 _c;
    [DataMember] public vec2 _size;
    [DataMember] public vec3 _outl;//outline color
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v4v4v4v2u2v4v4
  {
    //v_GuiVert
    //26 float = 96B
    [DataMember] public vec4 _rect;
    [DataMember] public vec4 _clip;
    [DataMember] public vec4 _tex;
    [DataMember] public vec2 _texsiz;
    [DataMember] public uvec2 _pick_color;
    [DataMember] public vec4 _rtl_rtr; //css corners = tl, tr, br, bl = xyzw
    [DataMember] public vec4 _rbr_rbl;
    [DataMember] public vec3 _quadrant;
    [DataMember] public float _pad;
  };
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3x2
  {
    //Textured Quad
    [DataMember] public vec3 _v;
    [DataMember] public vec2 _x;
  };

  #endregion

}
