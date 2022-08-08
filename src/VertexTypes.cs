using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace PirateCraft
{
  public enum VertexComponentType
  {
    v2_01,
    v2_02,
    v2_03,

    v3_01,
    v3_02,
    v3_03,

    v4_01,
    v4_02,
    v4_03,

    c4_01,
    c4_02,
    c4_03,

    c3_01,
    c3_02,
    c3_03,

    n3_01,
    n3_02,
    n3_03,

    x2_01,
    x2_02,
    x2_03,

    i1_01, //int (4 bytes)
    i1_02,
    i1_03,
    i2_01,
    i2_02,
    i2_03,
    i3_01,
    i3_02,
    i3_03,

    u1_01,//unsigned int
    u1_02,
    u1_03,
    u2_01,
    u2_02,
    u2_03,
    u3_01,
    u3_02,
    u3_03,

    s1_01, // short (2 bytes)
    s1_02,
    s1_03,
    s2_01,
    s2_02,
    s2_03,
    s3_01,
    s3_02,
    s3_03,

    S1_01,//unsigned short
    S1_02,
    S1_03,
    S2_01,
    S2_02,
    S2_03,
    S3_01,
    S3_02,
    S3_03,

    b1_01,//byte
    b1_02,
    b1_03,
    b2_01,
    b2_02,
    b2_03,
    b3_01,
    b3_02,
    b3_03,

    B1_01,//ubyte
    B1_02,
    B1_03,
    B2_01,
    B2_02,
    B2_03,
    B3_01,
    B3_02,
    B3_03,

    gl_InstanceID,
    gl_InstanceIndex,
    NoVertexType
  };

  public class VertexComponent
  {

    public int SizeBytes { get; set; }
    public int ComponentCount { get; set; }
    public VertexAttribPointerType DataType { get; set; }
    public int AttributeType { get { return VertexFormat.ComputeAttributeType((int)DataType, ComponentCount); } }
    public VertexComponentType UserType { get; set; }
    public int ByteOffset { get; set; }
    public int AttribLocation { get; set; } //Attrib Location
    public string Name { get; set; }// _v2 _c4 ...

    //int32_t getSizeBytes() { return _iSizeBytes; }
    //  int32_t getComponentCount() { return _iComponentCount; }
    //  GLenum getDataType() { return _eDataType; }
    //  GLenum getAttributeType();
    //  VertexUserType getUserType() { return _eUserType; }
    //  int32_t getByteOffset() { return _iByteOffset; }
    //  int32_t getLocation() { return _iLocation; }
    //  string_t getUserTypeName();

    //  private:
    //friend class VertexFormat;
    //int32_t _iSizeBytes;
    //int32_t _iComponentCount;
    //GLenum _eDataType;
    //VertexUserType _eUserType = VertexUserType::NoVertexType;
    //int32_t _iLocation;
    //int32_t _iByteOffset;
  };
  //Format for interleaved vertex.
  public class VertexFormat
  {
    public Dictionary<VertexComponentType, VertexComponent> Components { get; private set; } = new Dictionary<VertexComponentType, VertexComponent>();
    public int VertexSizeBytes_WithoutPadding { get; private set; } = 0; //Size of all added components - neglecting padding. (pretty much useless)
    public int ItemSizeBytes { get; private set; } = 0; // Size of the vert, including padding
    public string Name { get; private set; } = "Undefind vtx format";
    public int MaxLocation { get { return Components.Count; } }
    public int GetComponentOffset(VertexComponentType t)
    {
      VertexComponent val = null;
      if (Components.TryGetValue(t, out val))
      {
        return val.ByteOffset;
      }
      return -1;
    }
    public void BindAttribs(VertexFormat previousFormat)
    {
      //Previous format is if we are binding multiple vertex buffers to the vao / shader, we use the last location of the attrib from that buffer
      int previousFormatMaxAttribLocation = 0;
      if (previousFormat != null)
      {
        previousFormatMaxAttribLocation = previousFormat.MaxLocation;
      }
      //Creates attribute arrays for all items in here.
      //Note: we use vec4 size offsets here because of the 16 byte padding required by GPUs.
      foreach (var comp in Components)
      {
        GL.EnableVertexAttribArray(previousFormatMaxAttribLocation + comp.Value.AttribLocation);
        if (comp.Value.DataType == VertexAttribPointerType.Byte ||
            comp.Value.DataType == VertexAttribPointerType.UnsignedByte ||
            comp.Value.DataType == VertexAttribPointerType.Short ||
            comp.Value.DataType == VertexAttribPointerType.UnsignedShort ||
            comp.Value.DataType == VertexAttribPointerType.Int ||
            comp.Value.DataType == VertexAttribPointerType.UnsignedInt)
        {
          //OpenTK is so weird. Same enum, but in a separate enum?
          VertexAttribIntegerType intType = VertexAttribIntegerType.Int;

          if (comp.Value.DataType == VertexAttribPointerType.Byte)
          {
            intType = VertexAttribIntegerType.Byte;
          }
          else if (comp.Value.DataType == VertexAttribPointerType.UnsignedByte)
          {
            intType = VertexAttribIntegerType.UnsignedByte;
          }
          else if (comp.Value.DataType == VertexAttribPointerType.Short)
          {
            intType = VertexAttribIntegerType.Short;
          }
          else if (comp.Value.DataType == VertexAttribPointerType.UnsignedShort)
          {
            intType = VertexAttribIntegerType.UnsignedShort;
          }
          else if (comp.Value.DataType == VertexAttribPointerType.Int)
          {
            intType = VertexAttribIntegerType.Int;
          }
          else if (comp.Value.DataType == VertexAttribPointerType.UnsignedInt)
          {
            intType = VertexAttribIntegerType.UnsignedInt;
          }
          else
          {
            Gu.BRThrowNotImplementedException();
          }

          GL.VertexAttribIPointer(
            previousFormatMaxAttribLocation + comp.Value.AttribLocation,
            comp.Value.ComponentCount,
            intType,
            ItemSizeBytes,
            (IntPtr)(0 + comp.Value.ByteOffset)
          );
        }
        else if (comp.Value.DataType == VertexAttribPointerType.Float ||
                 comp.Value.DataType == VertexAttribPointerType.Double)
        {
          GL.VertexAttribPointer(
            previousFormatMaxAttribLocation + comp.Value.AttribLocation,
            comp.Value.ComponentCount,
            comp.Value.DataType,
            false,
            ItemSizeBytes,
            (IntPtr)(0 + comp.Value.ByteOffset)
          );
        }



      }

    }
    public unsafe dynamic Access(int vertex_index, VertexComponentType type, byte[] data)
    {
      //This is fun. Ok.
      //index is the vertex index
      //comp_index is which ccomponent, e.g. in v_v3x2v3v3 comp_index is 0, 1, 2 for v3
      VertexComponent comp = null;
      if (!Components.TryGetValue(type, out comp))
      {
        Gu.BRThrowException("Could not get component value for " + type);
      }

      int comp_off = comp.ByteOffset;
      int total_offset = vertex_index * ItemSizeBytes + comp_off;
      if (total_offset >= data.Length)
      {
        Gu.BRThrowException("Access to vertex data byte buffer is out of range (" + total_offset + ").");
      }

      dynamic ret = null;
      fixed (byte* dat = data)
      {
        //Declare more types here as needed.
        if (comp.DataType == VertexAttribPointerType.Float)
        {
          if (comp.ComponentCount == 2)
          {
            ret = *((vec2*)(dat + total_offset));
          }
          else if (comp.ComponentCount == 3)
          {
            ret = *((vec3*)(dat + total_offset));
          }
          else if (comp.ComponentCount == 4)
          {
            ret = *((vec4*)(dat + total_offset));
          }
          else
          {
            Gu.BRThrowNotImplementedException();
          }
        }
        else if (comp.DataType == VertexAttribPointerType.Int)
        {
          if (comp.ComponentCount == 2)
          {
            ret = *((ivec2*)(dat + total_offset));
          }
          else if (comp.ComponentCount == 3)
          {
            ret = *((ivec3*)(dat + total_offset));
          }
          else if (comp.ComponentCount == 4)
          {
            ret = *((ivec3*)(dat + total_offset));
          }
          else
          {
            Gu.BRThrowNotImplementedException();
          }
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
      }
      return ret;
    }
    private static Dictionary<string, VertexFormat> _formats = new Dictionary<string, VertexFormat>();
    public static VertexFormat GetVertexFormat<T>()
    {
      string szfmt = typeof(T).Name;

      VertexFormat ret = null;
      if (!_formats.TryGetValue(szfmt, out ret))
      {
        ret = DeclareFormat<T>();
        _formats.Add(szfmt, ret);
      }
      return ret;
    }
    private static VertexFormat DeclareFormat<T>()
    {
      string format = typeof(T).Name;
      //an even easier way to declare shader input vertexes. 
      // Format will get parsed v_xxyyzzww.. name is arbitrary
      //ex v_v2c4x3
      if (format == "byte")
      {
        format = "v_b1";
      }
      else if (format == "SByte")
      {
        format = "v_B1";
      }
      else if (format == "short" || format == "Int16")
      {
        format = "v_s1";
      }
      else if (format == "ushort" || format == "UInt16")
      {
        format = "v_S1";
      }
      else if (format == "int" || format == "Int32")
      {
        format = "v_i1";
      }
      else if (format == "uint" || format == "UInt32")
      {
        format = "v_u1";
      }
      else if (format == "vec2")
      {
        format = "v_v2";
      }
      else if (format == "vec3")
      {
        format = "v_v3";
      }
      else if (format == "vec4")
      {
        format = "v_v4";
      }

      //I realy think this should just cycle through the properties using reflection
      VertexFormat vft = new VertexFormat();
      Dictionary<VertexComponentType, int> occurances = new Dictionary<VertexComponentType, int>();
      if (!format.StartsWith("v_", StringComparison.InvariantCulture))
      {
        throw new Exception("Vertex format class named '" + format + "' must start with v_, and name attributes ex: v_v3n3x2c4");
      }
      string c = "";
      for (int i = 2; i < format.Length; ++i) //start at v_
      {
        c += format[i];
        if (c.Length == 2)
        {
          VertexComponentType outType = ParseUserType(c);
          int count = 0;
          if (occurances.TryGetValue(outType, out count))
          {
            if (count >= 3)
            {
              Gu.BRThrowException("Attribute count for type was more than 3. Not a bug, just we haven't supported it yet (update the enum).");
            }
            occurances[outType] += 1;
            outType += count;
          }
          else
          {
            occurances.Add(outType, 1);
          }
          vft.AddComponent(outType, c);

          c = "";
        }
      }

      //Different from size without padding.
      vft.ItemSizeBytes = Marshal.SizeOf(default(T));

      return vft;
    }
    private static VertexComponentType ParseUserType(string st)
    {
      switch (st)
      {
        case "v2": return VertexComponentType.v2_01;
        case "v3": return VertexComponentType.v3_01;
        case "v4": return VertexComponentType.v4_01;
        case "n3": return VertexComponentType.n3_01;
        case "c3": return VertexComponentType.c3_01;
        case "c4": return VertexComponentType.c4_01;
        case "x2": return VertexComponentType.x2_01;
        case "i1": return VertexComponentType.i1_01;
        case "i2": return VertexComponentType.i2_01;
        case "i3": return VertexComponentType.i3_01;
        case "u1": return VertexComponentType.u1_01;
        case "u2": return VertexComponentType.u2_01;
        case "u3": return VertexComponentType.u3_01;
        case "s1": return VertexComponentType.s1_01;//short
        case "s2": return VertexComponentType.s2_01;
        case "s3": return VertexComponentType.s3_01;
        case "S1": return VertexComponentType.S1_01;//unsigned short .. 
        case "S2": return VertexComponentType.S2_01;
        case "S3": return VertexComponentType.S3_01;
        case "b1": return VertexComponentType.b1_01;//short
        case "b2": return VertexComponentType.b2_01;
        case "b3": return VertexComponentType.b3_01;
        case "B1": return VertexComponentType.B1_01;//unsigned short .. 
        case "B2": return VertexComponentType.B2_01;
        case "B3": return VertexComponentType.B3_01;
      }
      Gu.BRThrowException("Component type '" + st + "' was not recognized. ");
      Gu.BRThrowNotImplementedException();
      return VertexComponentType.NoVertexType;
    }
    public void AddComponent(VertexComponentType eUserType, string name)
    {

      VertexAttribPointerType eType; //GLenum
      int compCount;
      int size;
      switch (eUserType)
      {
        case VertexComponentType.v2_01:
        case VertexComponentType.v2_02:
        case VertexComponentType.v2_03:
          eType = VertexAttribPointerType.Float;
          compCount = 2;
          size = Marshal.SizeOf(default(vec2));
          break;
        case VertexComponentType.v3_01:
        case VertexComponentType.v3_02:
        case VertexComponentType.v3_03:
          //*******************************************
          //**note this from the opengl wiki
          //"Implementations sometimes get the std140 layout wrong for vec3 components.
          //You are advised to manually pad your structures/arrays out and avoid using vec3 at all."
          eType = VertexAttribPointerType.Float;
          compCount = 3;
          size = Marshal.SizeOf(default(vec3));
          //*******************************************
          break;
        case VertexComponentType.v4_01:
        case VertexComponentType.v4_02:
        case VertexComponentType.v4_03:
          eType = VertexAttribPointerType.Float;
          compCount = 4;
          size = Marshal.SizeOf(default(vec4));
          break;
        case VertexComponentType.c3_01:
        case VertexComponentType.c3_02:
        case VertexComponentType.c3_03:
          eType = VertexAttribPointerType.Float;
          compCount = 3;
          size = Marshal.SizeOf(default(vec3));  //**Look at the size: vec4 - OpenGL requires components to be 64 byte aligned.
          break;
        case VertexComponentType.c4_01:
        case VertexComponentType.c4_02:
        case VertexComponentType.c4_03:
          eType = VertexAttribPointerType.Float;
          compCount = 4;
          size = Marshal.SizeOf(default(vec4));
          break;
        case VertexComponentType.n3_01:
        case VertexComponentType.n3_02:
        case VertexComponentType.n3_03:
          eType = VertexAttribPointerType.Float;
          compCount = 3;
          size = Marshal.SizeOf(default(vec3));  //**Look at the size: vec4  - OpenGL requires components to be 64 byte aligned.
          break;
        case VertexComponentType.x2_01:
        case VertexComponentType.x2_02:
        case VertexComponentType.x2_03:
          eType = VertexAttribPointerType.Float;
          compCount = 2;
          size = Marshal.SizeOf(default(vec2));  //**Look at the size: vec4  - OpenGL requires components to be 64 byte aligned.
          break;
        case VertexComponentType.i1_01:
        case VertexComponentType.i1_02:
        case VertexComponentType.i1_03:
          eType = VertexAttribPointerType.Int;
          compCount = 1;
          size = Marshal.SizeOf(default(Int32)) * 1;
          break;
        case VertexComponentType.i2_01:
        case VertexComponentType.i2_02:
        case VertexComponentType.i2_03:
          eType = VertexAttribPointerType.Int;
          compCount = 2;
          size = Marshal.SizeOf(default(Int32)) * 2;
          break;
        case VertexComponentType.i3_01:
        case VertexComponentType.i3_02:
        case VertexComponentType.i3_03:
          eType = VertexAttribPointerType.Int;
          compCount = 3;
          size = Marshal.SizeOf(default(Int32)) * 3;
          break;

        case VertexComponentType.u1_01:
        case VertexComponentType.u1_02:
        case VertexComponentType.u1_03:
          eType = VertexAttribPointerType.UnsignedInt;
          compCount = 1;
          size = Marshal.SizeOf(default(UInt32)) * 1;
          break;
        case VertexComponentType.u2_01:
        case VertexComponentType.u2_02:
        case VertexComponentType.u2_03:
          eType = VertexAttribPointerType.UnsignedInt;
          compCount = 2;
          size = Marshal.SizeOf(default(UInt32)) * 2;
          break;
        case VertexComponentType.u3_01:
        case VertexComponentType.u3_02:
        case VertexComponentType.u3_03:
          eType = VertexAttribPointerType.UnsignedInt;
          compCount = 3;
          size = Marshal.SizeOf(default(UInt32)) * 3;
          break;

        case VertexComponentType.s1_01:
        case VertexComponentType.s1_02:
        case VertexComponentType.s1_03:
          eType = VertexAttribPointerType.Short;
          compCount = 1;
          size = Marshal.SizeOf(default(Int16)) * 1;
          break;
        case VertexComponentType.s2_01:
        case VertexComponentType.s2_02:
        case VertexComponentType.s2_03:
          eType = VertexAttribPointerType.Short;
          compCount = 2;
          size = Marshal.SizeOf(default(Int16)) * 2;
          break;
        case VertexComponentType.s3_01:
        case VertexComponentType.s3_02:
        case VertexComponentType.s3_03:
          eType = VertexAttribPointerType.Short;
          compCount = 3;
          size = Marshal.SizeOf(default(Int16)) * 3;
          break;

        case VertexComponentType.S1_01:
        case VertexComponentType.S1_02:
        case VertexComponentType.S1_03:
          eType = VertexAttribPointerType.UnsignedShort;
          compCount = 1;
          size = Marshal.SizeOf(default(UInt16)) * 1;
          break;
        case VertexComponentType.S2_01:
        case VertexComponentType.S2_02:
        case VertexComponentType.S2_03:
          eType = VertexAttribPointerType.UnsignedShort;
          compCount = 2;
          size = Marshal.SizeOf(default(UInt16)) * 2;
          break;
        case VertexComponentType.S3_01:
        case VertexComponentType.S3_02:
        case VertexComponentType.S3_03:
          eType = VertexAttribPointerType.UnsignedShort;
          compCount = 3;
          size = Marshal.SizeOf(default(UInt16)) * 3;
          break;

        case VertexComponentType.b1_01:
        case VertexComponentType.b1_02:
        case VertexComponentType.b1_03:
          eType = VertexAttribPointerType.Byte;
          compCount = 1;
          size = Marshal.SizeOf(default(SByte)) * 1;
          break;
        case VertexComponentType.b2_01:
        case VertexComponentType.b2_02:
        case VertexComponentType.b2_03:
          eType = VertexAttribPointerType.Byte;
          compCount = 2;
          size = Marshal.SizeOf(default(SByte)) * 2;
          break;
        case VertexComponentType.b3_01:
        case VertexComponentType.b3_02:
        case VertexComponentType.b3_03:
          eType = VertexAttribPointerType.Byte;
          compCount = 3;
          size = Marshal.SizeOf(default(SByte)) * 3;
          break;

        case VertexComponentType.B1_01:
        case VertexComponentType.B1_02:
        case VertexComponentType.B1_03:
          eType = VertexAttribPointerType.UnsignedByte;
          compCount = 1;
          size = Marshal.SizeOf(default(byte)) * 1;
          break;
        case VertexComponentType.B2_01:
        case VertexComponentType.B2_02:
        case VertexComponentType.B2_03:
          eType = VertexAttribPointerType.UnsignedByte;
          compCount = 2;
          size = Marshal.SizeOf(default(byte)) * 2;
          break;
        case VertexComponentType.B3_01:
        case VertexComponentType.B3_02:
        case VertexComponentType.B3_03:
          eType = VertexAttribPointerType.UnsignedByte;
          compCount = 3;
          size = Marshal.SizeOf(default(byte)) * 3;
          break;


        default:
          throw new Exception("Vertex user type not impelmented.");
      }

      AddComponent(eType, compCount, size, eUserType, name);
    }
    public void AddComponent(VertexAttribPointerType type, int componentCount,
        int size, VertexComponentType eUserType = VertexComponentType.NoVertexType, string name = "")
    {
      foreach (KeyValuePair<VertexComponentType, VertexComponent> entry in Components)
      {
        if (entry.Value.UserType == eUserType)
        {
          throw new Exception("Duplicate Vertex component '" + GetUserTypeName(eUserType) + "' for Vertex Type '" + Name + "'.");
        }
      }

      VertexComponent cmp = new VertexComponent();
      cmp.DataType = type;
      cmp.ComponentCount = componentCount;
      cmp.SizeBytes = size;
      cmp.UserType = eUserType;
      cmp.AttribLocation = Components.Count;
      cmp.Name = name;

      cmp.ByteOffset = 0;
      foreach (KeyValuePair<VertexComponentType, VertexComponent> entry in Components)
      {
        cmp.ByteOffset += entry.Value.SizeBytes;
      }

      Components.Add(eUserType, cmp);

      //Re-calculate size of vertex
      VertexSizeBytes_WithoutPadding = 0;
      foreach (KeyValuePair<VertexComponentType, VertexComponent> entry in Components)
      {
        VertexSizeBytes_WithoutPadding += entry.Value.SizeBytes;
      }

    }
    public static int ComputeAttributeType(int /*GLenum*/ type, int count)
    {
      //We bh
      if (type == (int)VertexAttribPointerType.Float)
      {
        if (count == 1)
        {
          return (int)VertexAttribPointerType.Float;
        }
        else if (count == 2)
        {
          return (int)AttributeType.FloatVec2;
        }
        else if (count == 3)
        {
          return (int)AttributeType.FloatVec3;
        }
        else if (count == 4)
        {
          return (int)AttributeType.FloatVec4;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
      }
      else if (type == (int)VertexAttribPointerType.Int)
      {
        if (count == 1)
        {
          return (int)VertexAttribPointerType.Int;
        }
        else if (count == 2)
        {
          return (int)AttributeType.IntVec2;
        }
        else if (count == 3)
        {
          return (int)AttributeType.IntVec3;
        }
        else if (count == 4)
        {
          return (int)AttributeType.IntVec4;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
      }
      else if (type == (int)VertexAttribPointerType.UnsignedInt)
      {
        if (count == 1)
        {
          return (int)VertexAttribPointerType.UnsignedInt;
        }
        else if (count == 2)
        {
          return (int)AttributeType.IntVec2;
        }
        else if (count == 3)
        {
          return (int)AttributeType.IntVec3;
        }
        else if (count == 4)
        {
          return (int)AttributeType.IntVec4;
        }
        else
        {
          Gu.BRThrowNotImplementedException();
        }
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
      return 0;
    }
    public static string GetUserTypeName(VertexComponentType eUserType)
    {
      switch (eUserType)
      {
        case VertexComponentType.c4_01:
        case VertexComponentType.c4_02:
        case VertexComponentType.c4_03:
          return ("Color4f");
        case VertexComponentType.v2_01:
        case VertexComponentType.v2_02:
        case VertexComponentType.v2_03:
          return ("Position2f");
        case VertexComponentType.v3_01:
        case VertexComponentType.v3_02:
        case VertexComponentType.v3_03:
          return ("Position3f");
        case VertexComponentType.n3_01:
        case VertexComponentType.n3_02:
        case VertexComponentType.n3_03:
          return ("Normal3f");
        case VertexComponentType.x2_01:
        case VertexComponentType.x2_02:
        case VertexComponentType.x2_03:
          return ("Texcoord2f");
        case VertexComponentType.i1_01:
        case VertexComponentType.i1_02:
        case VertexComponentType.i1_03:
          return ("Int_1");
        case VertexComponentType.i2_01:
        case VertexComponentType.i2_02:
        case VertexComponentType.i2_03:
          return ("Int_2");
        case VertexComponentType.i3_01:
        case VertexComponentType.i3_02:
        case VertexComponentType.i3_03:
          return ("Int_3");
        case VertexComponentType.u1_01:
        case VertexComponentType.u1_02:
        case VertexComponentType.u1_03:
          return ("Unsigned_Int_1");
        case VertexComponentType.u2_01:
        case VertexComponentType.u2_02:
        case VertexComponentType.u2_03:
          return ("Unsigned_Int_2");
        case VertexComponentType.u3_01:
        case VertexComponentType.u3_02:
        case VertexComponentType.u3_03:
          return ("Unsigned_Int_3");
        case VertexComponentType.v4_01:
        case VertexComponentType.v4_02:
        case VertexComponentType.v4_03:
          return ("Position4f");
      };

      Gu.BRThrowNotImplementedException();
      return ("Unknown User Type name.");
    }
  }
  //public enum IndexFormatType
  //{
  //  None,
  //  Uint16,
  //  Uint32
  //}
  //public class IndexFormat
  //{
  //  public static readonly IndexFormat IFMT_U32 = new IndexFormat(IndexFormatType.Uint32);
  //  public static readonly IndexFormat IFMT_U16 = new IndexFormat(IndexFormatType.Uint16);
  //  public int SizeBytes
  //  {
  //    get
  //    {
  //      if (IndexFormatType == IndexFormatType.Uint16)
  //      {
  //        return 2;
  //      }
  //      else if (IndexFormatType == IndexFormatType.Uint32)
  //      {
  //        return 4;
  //      }
  //      else
  //      {
  //        Gu.BRThrowNotImplementedException();
  //      }
  //      return 0;
  //    }
  //  }
  //  public IndexFormatType IndexFormatType { get; private set; } = IndexFormatType.Uint32;
  //  public IndexFormat(IndexFormatType t)
  //  {
  //    IndexFormatType = t;
  //  }
  //  public unsafe dynamic Access(int index, byte[] data)
  //  {
  //    Gu.Assert(IndexFormatType == IndexFormatType.Uint32);
  //    int offset = index * SizeBytes;

  //    Gu.Assert(offset < data.Length);

  //    dynamic ret = 0;
  //    fixed (byte* dat = data)
  //    {
  //      if (IndexFormatType == IndexFormatType.Uint16)
  //      {
  //        ret = *((UInt16*)(dat + offset));
  //      }
  //      else if (IndexFormatType == IndexFormatType.Uint32)
  //      {
  //        ret = *((UInt32*)(dat + offset));
  //      }
  //      else
  //      {
  //        Gu.BRThrowNotImplementedException();
  //      }
  //    }
  //    return ret;
  //  }
  //}
  //note i removed std430 padding .. this is erroneous.. we need to fix it
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3c4
  {
    public vec3 _v;
    public vec4 _c;
  }
  [Serializable]
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3n3x2
  {
    public vec3 _v;
    public vec3 _n;
    public vec2 _x;
  }
  //GlobVert
  [Serializable]
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3n3x2u1
  {
    public vec3 _v; //3   = 3
    public vec3 _n; //3   = 9
    public vec2 _x; //2   = 11
    public uint _u; // 1  = 12  => 12%4=0
  }
  //v_GuiVert
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v4v4v4v2u2
  {
    public vec4 _rect;
    public vec4 _clip;
    public vec4 _tex;
    public vec2 _texsiz;
    public uvec2 _pick_color;
  };
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3x2
  {
    public vec3 _v;
    public vec2 _x;
  };

}
