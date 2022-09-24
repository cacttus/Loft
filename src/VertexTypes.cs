using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace PirateCraft
{
  #region Enums

  public enum VertexComponentType
  {
    v2_01,
    v2_02,
    v2_03,
    v2_04,
    v2_05,

    v3_01,
    v3_02,
    v3_03,
    v3_04,
    v3_05,

    v4_01,
    v4_02,
    v4_03,
    v4_04,
    v4_05,

    c4_01,
    c4_02,
    c4_03,
    c4_04,
    c4_05,

    c3_01,
    c3_02,
    c3_03,
    c3_04,
    c3_05,

    n3_01,
    n3_02,
    n3_03,
    n3_04,
    n3_05,

    f3_01,//face
    f3_02,
    f3_03,
    f3_04,
    f3_05,

    t3_01, //tangent
    t3_02,
    t3_03,
    t3_04,
    t3_05,

    x2_01,
    x2_02,
    x2_03,
    x2_04,
    x2_05,

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

  #endregion

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
  public class VertexFormat
  {
    //Format for interleaved vertexes
    #region Private:Members

    private static Dictionary<string, VertexFormat> _formats = new Dictionary<string, VertexFormat>();

    #endregion
    #region Public:Members

    public Dictionary<VertexComponentType, VertexComponent> Components { get; private set; } = new Dictionary<VertexComponentType, VertexComponent>();
    public int VertexSizeBytes_WithoutPadding { get; private set; } = 0; //Size of all added components - neglecting padding. (pretty much useless)
    public int VertexSizeBytes { get; private set; } = 0; // Size of the vert, including padding
    public string Name { get; private set; } = "Undefind vtx format";
    public int MaxLocation { get { return Components.Count; } }
    public Type Type { get; private set; } = null;

    #endregion
    #region Public:Static Methods

    public static VertexFormat GetVertexFormat<T>()
    {
      return GetVertexFormat(typeof(T));
    }
    public static VertexFormat GetVertexFormat(Type vertexType)
    {
      //Automatically compute a vertex format for a given vertex type.
      //Cached
      string szfmt = vertexType.Name;

      VertexFormat ret = null;
      if (!_formats.TryGetValue(szfmt, out ret))
      {
        ret = DeclareFormat(vertexType);
        _formats.Add(szfmt, ret);
      }
      return ret;
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
        case VertexComponentType.v4_04:
        case VertexComponentType.v4_05:
          return ("Position4f");
      };

      Gu.BRThrowNotImplementedException();
      return ("Unknown User Type name.");
    }
    public static void ComputeNormalAndTangent(vec3 p0, vec3 p1, vec3 p2, vec2 x0, vec2 x1, vec2 x2, out vec3 normal, out vec3 tangent)
    {
      //https://learnopengl.com/Advanced-Lighting/Normal-Mapping
      vec3 dp0 = p1 - p0;
      vec3 dp1 = p2 - p0;
      vec2 dx0 = x1 - x0;
      vec2 dx1 = x2 - x0;

      normal = dp1.cross(dp0).normalize();

      float f = 1.0f / (dx0.x * dx1.y - dx1.x * dx0.y);
      tangent = new vec3();
      tangent.x = f * (dx1.y * dp0.x - dx0.y * dp1.x);
      tangent.y = f * (dx1.y * dp0.y - dx0.y * dp1.y);
      tangent.z = f * (dx1.y * dp0.z - dx0.y * dp1.z);
      tangent.normalize();
    }

    #endregion
    #region Public:Methods

    public VertexFormat(string name, Type t)
    {
      Name = name;
      Type = t;
    }
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
            VertexSizeBytes,
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
            VertexSizeBytes,
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
      int total_offset = vertex_index * VertexSizeBytes + comp_off;
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
        case VertexComponentType.v2_04:
        case VertexComponentType.v2_05:
        case VertexComponentType.x2_01:
        case VertexComponentType.x2_02:
        case VertexComponentType.x2_03:
        case VertexComponentType.x2_04:
        case VertexComponentType.x2_05:
          eType = VertexAttribPointerType.Float;
          compCount = 2;
          size = Marshal.SizeOf(default(vec2));
          break;
        case VertexComponentType.v3_01:
        case VertexComponentType.v3_02:
        case VertexComponentType.v3_03:
        case VertexComponentType.v3_04:
        case VertexComponentType.v3_05:
        case VertexComponentType.c3_01:
        case VertexComponentType.c3_02:
        case VertexComponentType.c3_03:
        case VertexComponentType.c3_04:
        case VertexComponentType.c3_05:
        case VertexComponentType.n3_01:
        case VertexComponentType.n3_02:
        case VertexComponentType.n3_03:
        case VertexComponentType.n3_04:
        case VertexComponentType.n3_05:
        case VertexComponentType.f3_01:
        case VertexComponentType.f3_02:
        case VertexComponentType.f3_03:
        case VertexComponentType.f3_04:
        case VertexComponentType.f3_05:
        case VertexComponentType.t3_01:
        case VertexComponentType.t3_02:
        case VertexComponentType.t3_03:
        case VertexComponentType.t3_04:
        case VertexComponentType.t3_05:
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
        case VertexComponentType.v4_04:
        case VertexComponentType.v4_05:
        case VertexComponentType.c4_01:
        case VertexComponentType.c4_02:
        case VertexComponentType.c4_03:
        case VertexComponentType.c4_04:
        case VertexComponentType.c4_05:
          eType = VertexAttribPointerType.Float;
          compCount = 4;
          size = Marshal.SizeOf(default(vec4));
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
    public void AddComponent(VertexAttribPointerType type, int componentCount, int size, VertexComponentType eUserType = VertexComponentType.NoVertexType, string name = "")
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

    #endregion
    #region Private: Methods

    private static VertexFormat DeclareFormat(Type vertexType)
    {
      string strFormat = vertexType.Name;

//*** THIS SUCKS
//*** THIS SUCKS
// TODO: Reflection and get teh field infos, and deduce them by their field names.
//Don't use the class name itself.....

      //an even easier way to declare shader input vertexes. 
      // Format will get parsed v_xxyyzzww.. name is arbitrary
      //ex v_v2c4x3
      if (strFormat == "byte")
      {
        strFormat = "v_b1";
      }
      else if (strFormat == "SByte")
      {
        strFormat = "v_B1";
      }
      else if (strFormat == "short" || strFormat == "Int16")
      {
        strFormat = "v_s1";
      }
      else if (strFormat == "ushort" || strFormat == "UInt16")
      {
        strFormat = "v_S1";
      }
      else if (strFormat == "int" || strFormat == "Int32")
      {
        strFormat = "v_i1";
      }
      else if (strFormat == "uint" || strFormat == "UInt32")
      {
        strFormat = "v_u1";
      }
      else if (strFormat == "vec2")
      {
        strFormat = "v_v2";
      }
      else if (strFormat == "vec3")
      {
        strFormat = "v_v3";
      }
      else if (strFormat == "vec4")
      {
        strFormat = "v_v4";
      }

      //I realy think this should just cycle through the properties using reflection
      VertexFormat vft = new VertexFormat(strFormat, vertexType);
      Dictionary<VertexComponentType, int> occurances = new Dictionary<VertexComponentType, int>();
      if (!strFormat.StartsWith("v_", StringComparison.InvariantCulture))
      {
        throw new Exception("Vertex format class named '" + strFormat + "' must start with v_, and name attributes ex: v_v3n3x2c4");
      }
      string c = "";
      for (int i = 2; i < strFormat.Length; ++i) //start at v_
      {
        c += strFormat[i];
        if (c.Length == 2)
        {
          VertexComponentType outType = ParseUserType(c);
          int count = 0;
          if (occurances.TryGetValue(outType, out count))
          {
            if (count >= 5)
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

      //Different from size without padding. Make sure this new one works
      vft.VertexSizeBytes = Marshal.SizeOf(vertexType);

      return vft;
    }
    private static VertexComponentType ParseUserType(string st)
    {
      switch (st)
      {
        case "v2": return VertexComponentType.v2_01;//vertex
        case "v3": return VertexComponentType.v3_01;
        case "v4": return VertexComponentType.v4_01;
        case "n3": return VertexComponentType.n3_01;//normal
        case "f3": return VertexComponentType.f3_01;//face normal
        case "t3": return VertexComponentType.t3_01;//tangent
        case "c3": return VertexComponentType.c3_01;//color
        case "c4": return VertexComponentType.c4_01;
        case "x2": return VertexComponentType.x2_01;//texcoord
        case "i1": return VertexComponentType.i1_01;//signed int
        case "i2": return VertexComponentType.i2_01;
        case "i3": return VertexComponentType.i3_01;
        case "u1": return VertexComponentType.u1_01;//unsigned int
        case "u2": return VertexComponentType.u2_01;
        case "u3": return VertexComponentType.u3_01;
        case "s1": return VertexComponentType.s1_01;//signed short
        case "s2": return VertexComponentType.s2_01;
        case "s3": return VertexComponentType.s3_01;
        case "S1": return VertexComponentType.S1_01;//unsigned short 
        case "S2": return VertexComponentType.S2_01;
        case "S3": return VertexComponentType.S3_01;
        case "b1": return VertexComponentType.b1_01;//signed byte
        case "b2": return VertexComponentType.b2_01;
        case "b3": return VertexComponentType.b3_01;
        case "B1": return VertexComponentType.B1_01;//unsigned byte
        case "B2": return VertexComponentType.B2_01;
        case "B3": return VertexComponentType.B3_01;
      }
      Gu.BRThrowException("Component type '" + st + "' was not recognized. ");
      Gu.BRThrowNotImplementedException();
      return VertexComponentType.NoVertexType;
    }

    #endregion
  }
  public class VertexPointer
  {
    //Generic vertex class
    //Allows us to pass around any vertex array.
    //ex: we have 2 verts: v_v3x2n3 and v_v3c2t3x2n3c4.. this will let us get , for example, v3 and n3 from both in the same method.
    // The class will throw an exception if you try to access a component that does not exist on the type. 
    // ** Be careful, Intellisense may be wrong!
    #region Public: Classes

    public class VertexPointerOffset
    {
      public VertexPointer Pointer;
      public int Index { get; private set; }
      public VertexPointerOffset(VertexPointer v, int index)
      {
        Pointer = v;
        Index = index;
      }
      //The 01 methods are for 01 data.
      public vec3 _v { get { return Pointer.GetValue<vec3>(VertexComponentType.v3_01, Index); } set { Pointer.SetValue<vec3>(VertexComponentType.v3_01, Index, value); } }
      public vec3 _n { get { return Pointer.GetValue<vec3>(VertexComponentType.n3_01, Index); } set { Pointer.SetValue<vec3>(VertexComponentType.n3_01, Index, value); } }
      public vec3 _t { get { return Pointer.GetValue<vec3>(VertexComponentType.t3_01, Index); } set { Pointer.SetValue<vec3>(VertexComponentType.t3_01, Index, value); } }
      public vec2 _x { get { return Pointer.GetValue<vec2>(VertexComponentType.x2_01, Index); } set { Pointer.SetValue<vec2>(VertexComponentType.x2_01, Index, value); } }
      public vec2 _x2 { get { return Pointer.GetValue<vec2>(VertexComponentType.x2_02, Index); } set { Pointer.SetValue<vec2>(VertexComponentType.x2_02, Index, value); } }
      public uint _u { get { return Pointer.GetValue<uint>(VertexComponentType.u1_01, Index); } set { Pointer.SetValue<uint>(VertexComponentType.u1_01, Index, value); } }
    }

    #endregion
    #region Public: Members

    public VertexFormat Format { get; private set; } = null;
    public object Verts { get; private set; } = null;
    public int BufferSizeBytes { get; private set; } = 0;
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
      Format = VertexFormat.GetVertexFormat(t.GetElementType());
      Length = (verts as Array).Length;
      BufferSizeBytes = Format.VertexSizeBytes * Length;
    }
    public VertexPointerOffset this[int i]
    {
      get
      {
        return new VertexPointerOffset(this, i);
      }
    }
    public T GetValue<T>(VertexComponentType ctype, int off) where T : unmanaged
    {
      Gu.Assert(off < this.Length);
      //We could box this type, however we should technically never need to. C# should check bounds.
      unsafe
      {
        var pinnedHandle = GCHandle.Alloc(Verts, GCHandleType.Pinned);
        void* pt = pinnedHandle.AddrOfPinnedObject().ToPointer();
        byte* b = GetPtr(ctype, off, pt);
        Gu.Assert(b != null);
        T ret = *((T*)b);
        pinnedHandle.Free();
        return ret;
      }
    }
    public void SetValue<T>(VertexComponentType ctype, int off, T val) where T : unmanaged
    {
      Gu.Assert(off < this.Length);
      //We could box this type, however we should technically never need to. C# should check bounds.
      unsafe
      {
        var pinnedHandle = GCHandle.Alloc(Verts, GCHandleType.Pinned);
        void* pt = pinnedHandle.AddrOfPinnedObject().ToPointer();
        byte* b = GetPtr(ctype, off, pt);
        Gu.Assert(b != null);
        *((T*)b) = val;
        pinnedHandle.Free();
      }
    }

    #endregion
    #region Private: Methods

    private unsafe byte* GetPtr(VertexComponentType comp, int offset, void* pt)
    {
      byte* ret = null;
      if (Format.Components.TryGetValue(comp, out var cmp))
      {
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

  //note i removed std430 padding .. this is erroneous.. we need to fix it
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3c4
  {
    public vec3 _v;
    public vec4 _c;
  }
  //Base object vertex, with picking<
  [Serializable]
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3n3x2t3u1
  {
    public vec3 _v;
    public vec3 _n;
    public vec2 _x;
    public vec3 _t;
    public uint _u;//Face ID, note this is convenient just because we had a pad value.
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
  public struct v_v4v4v4v2u2v4v4
  {
    public vec4 _rect;
    public vec4 _clip;
    public vec4 _tex;
    public vec2 _texsiz;
    public uvec2 _pick_color;
    public vec4 _rtl_rtr; //css corners = tl, tr, br, bl = xyzw
    public vec4 _rbr_rbl;
  };
  [StructLayout(LayoutKind.Sequential)]
  public struct v_v3x2
  {
    public vec3 _v;
    public vec2 _x;
  };
  //Billboard Quad Vert
  // [StructLayout(LayoutKind.Sequential)]
  // public struct v_v4v2c4x4u2
  // {
  //   public vec4 _v401;//pos
  //   public vec2 _v201;//size
  //   public vec4 _x401;//uv0, uv1
  //   public uvec2 _u201;//pick_color
  // };

  #endregion

}
