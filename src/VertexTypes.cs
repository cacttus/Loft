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

        i2_01,
        i2_02,
        i2_03,

        u2_01,
        u2_02,
        u2_03,

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
        public int VertexSizeBytes { get; private set; } = 0;
        public string Name { get; private set; } = "Undefind vtx format";
        public int GetComponentOffset(VertexComponentType t)
        {
            VertexComponent val = null;
            if (Components.TryGetValue(t, out val))
            {
                return val.ByteOffset;
            }
            return -1;
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

        public static VertexFormat DeclareVertexFormat(string name, string format)
        {
            //an even easier way to declare shader input vertexes. 
            // Format will get parsed v_xxyyzzww.. name is arbitrary
            //ex v_v2c4x3
            VertexFormat vft = new VertexFormat();
            Dictionary<VertexComponentType, int> occurances = new Dictionary<VertexComponentType, int>();
            if (!format.StartsWith("v_", StringComparison.InvariantCulture))
            {
                throw new Exception("Vertex format must start with v_, and name attributes ex: v_v3n3x2c4");
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
                            throw new Exception("Attribute count for type was more than 3. Not a bug, just we haven't supported it yet (update the enum).");
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

            return vft;
        }
        private static VertexComponentType ParseUserType(string st)
        {
            switch (st)
            {
                case "v2": return VertexComponentType.v2_01;
                case "v3": return VertexComponentType.v3_01;
                case "n3": return VertexComponentType.n3_01;
                case "c4": return VertexComponentType.c4_01;
                case "x2": return VertexComponentType.x2_01;
            }
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
                    //**Look at the size: vec4 - note this from the opengl wiki
                    //"Implementations sometimes get the std140 layout wrong for vec3 components.
                    //You are advised to manually pad your structures/arrays out and avoid using vec3 at all."
                    eType = VertexAttribPointerType.Float;
                    compCount = 3;
                    size = Marshal.SizeOf(default(vec4));
                    //*******************************************
                    break;
                case VertexComponentType.v4_01:
                case VertexComponentType.v4_02:
                case VertexComponentType.v4_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 4;
                    size = Marshal.SizeOf(default(vec4));
                    break;
                case VertexComponentType.c4_01:
                case VertexComponentType.c4_02:
                case VertexComponentType.c4_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 4;
                    size = Marshal.SizeOf(default(vec4));
                    break;
                case VertexComponentType.c3_01:
                case VertexComponentType.c3_02:
                case VertexComponentType.c3_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 3;
                    size = Marshal.SizeOf(default(vec4));  //**Look at the size: vec4 - OpenGL requires components to be 64 byte aligned.
                    break;
                case VertexComponentType.n3_01:
                case VertexComponentType.n3_02:
                case VertexComponentType.n3_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 3;
                    size = Marshal.SizeOf(default(vec4));  //**Look at the size: vec4  - OpenGL requires components to be 64 byte aligned.
                    break;
                case VertexComponentType.x2_01:
                case VertexComponentType.x2_02:
                case VertexComponentType.x2_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 2;
                    size = Marshal.SizeOf(default(vec4));  //**Look at the size: vec4  - OpenGL requires components to be 64 byte aligned.
                    break;
                case VertexComponentType.i2_01:
                case VertexComponentType.i2_02:
                case VertexComponentType.i2_03:
                    eType = VertexAttribPointerType.Int;
                    compCount = 2;
                    size = Marshal.SizeOf(default(vec2));
                    break;
                //case VertexComponentType.u2_01:
                //case VertexComponentType.u2_02:
                //case VertexComponentType.u2_03:
                //    eType = VertexAttribPointerType.UnsignedInt;
                //    compCount = 2;
                //    size = Marshal.SizeOf(default(uvec2));
                //    break;
                //**std430 padded types.
                //case VertexUserType::v3_01_STD430:
                //    eType = GL_FLOAT;
                //    compCount = 3;
                //    size = sizeof(vec4);
                //    break;
                //case VertexUserType::n3_01_STD430:
                //    eType = GL_FLOAT;
                //    compCount = 3;
                //    size = sizeof(vec4);
                //    break;
                //case VertexUserType::x2_01_STD430:
                //    eType = GL_FLOAT;
                //    compCount = 2;
                //    size = sizeof(vec4);
                //    break;
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
            VertexSizeBytes = 0;
            foreach (KeyValuePair<VertexComponentType, VertexComponent> entry in Components)
            {
                VertexSizeBytes += entry.Value.SizeBytes;
            }

        }
        //std::shared_ptr<VertexComponent> getComponentForUserType(VertexUserType eUserType);
        //const std::map<int, std::shared_ptr<VertexComponent>>& getComponents() { return _vecComponents; }
        //static GLenum computeAttributeType(GLenum type, GLuint count);
        //static string_t getUserTypeName(VertexUserType t);
        //int matchTypeForShaderType(std::shared_ptr<VertexFormat> shaderType);

        //private:

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
                    return ("Color4f");
                case VertexComponentType.v2_01:
                    return ("Position2f");
                case VertexComponentType.v3_01:
                    return ("Position3f");
                case VertexComponentType.n3_01:
                    return ("Normal3f");
                case VertexComponentType.x2_01:
                    return ("Texcoord2f");
                case VertexComponentType.u2_01:
                    return ("Unsigned_Int_2");
                case VertexComponentType.v4_01:
                case VertexComponentType.v4_02:
                case VertexComponentType.v4_03:
                    return ("Position4f");
            };

            Gu.BRThrowNotImplementedException();
            return ("Unknown User Type.");
        }
    }

    public enum IndexFormatType
    {
        None,
        Uint16,
        Uint32
    }
    public class IndexFormat
    {
        public static readonly IndexFormat IFMT_U32 = new IndexFormat(IndexFormatType.Uint32);
        public static readonly IndexFormat IFMT_U16 = new IndexFormat(IndexFormatType.Uint16);
        public int SizeBytes
        {
            get
            {
                if (IndexFormatType == IndexFormatType.Uint16)
                {
                    return 2;
                }
                else if (IndexFormatType == IndexFormatType.Uint32)
                {
                    return 4;
                }
                else
                {
                    Gu.BRThrowNotImplementedException();
                }
                return 0;
            }
        }
        public IndexFormatType IndexFormatType { get; private set; } = IndexFormatType.Uint32;
        public IndexFormat(IndexFormatType t)
        {
            IndexFormatType = t;
        }
        public unsafe dynamic Access(int index, byte[] data)
        {
            Gu.Assert(IndexFormatType == IndexFormatType.Uint32);
            int offset = index * SizeBytes;

            Gu.Assert(offset < data.Length);

            dynamic ret = 0;
            fixed (byte* dat = data)
            {
                if (IndexFormatType == IndexFormatType.Uint16)
                {
                    ret = *((UInt16*)(dat + offset));
                }
                else if (IndexFormatType == IndexFormatType.Uint32)
                {
                    ret = *((UInt32*)(dat + offset));
                }
                else
                {
                    Gu.BRThrowNotImplementedException();
                }
            }
            return ret;
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct v_v3c4
    {
        public vec3 _v;
        public float pad1;
        public vec4 _c;

        public static VertexFormat VertexFormat = VertexFormat.DeclareVertexFormat("v_v3c4", "v_v3c4");
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct v_v3n3x2
    {
        public vec3 _v;
        public float pad1;
        public vec3 _n;
        public float pad2;
        public vec2 _x;
        public float pad3;
        public float pad4;

        public static VertexFormat VertexFormat = VertexFormat.DeclareVertexFormat("v_v3n3x2", "v_v3n3x2");
    }
}
