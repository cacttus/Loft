using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;

namespace PirateCraft
{
    public enum VertexUserType
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
        public VertexUserType UserType { get; set; }
        public int ByteOffset { get; set; }
        public int AttribLocation { get; set; } //Attrib Location
        public int Location { get; set; }

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
        private Dictionary<VertexUserType, VertexComponent> Components = new Dictionary<VertexUserType, VertexComponent>();
        public int VertexSizeBytes { get; private set; } = 0;
        public string Name { get; private set; } = "Undefind vtx format";
        public int SizeBytes { get; private set; }

        public static VertexFormat DeclareVertexFormat(string name, string format)
        {
            //an even easier way to declare shader input vertexes. 
            // Format will get parsed v_xxyyzzww.. name is arbitrary
            //ex v_v2c4x3
            VertexFormat vft = new VertexFormat();
            Dictionary<VertexUserType, int> occurances = new Dictionary<VertexUserType, int>();
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
                    VertexUserType outType = ParseUserType(c);
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
                    vft.AddComponent(outType);

                    c = "";
                }
            }

            return vft;
        }
        private static VertexUserType ParseUserType(string st)
        {
            switch (st)
            {
                case "v2": return VertexUserType.v2_01;
                case "v3": return VertexUserType.v3_01;
                case "n3": return VertexUserType.n3_01;
                case "c4": return VertexUserType.c4_01;
                case "x2": return VertexUserType.x2_01;
            }
            Gu.BRThrowNotImplementedException();
            return VertexUserType.NoVertexType;
        }
        public void AddComponent(VertexUserType eUserType)
        {
            VertexAttribPointerType eType; //GLenum
            int compCount;
            int size;
            switch (eUserType)
            {
                case VertexUserType.v2_01:
                case VertexUserType.v2_02:
                case VertexUserType.v2_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 2;
                    size = Marshal.SizeOf(default(Vec2f));
                    break;
                case VertexUserType.v3_01:
                case VertexUserType.v3_02:
                case VertexUserType.v3_03:
                    //*******************************************
                    //**Look at the size: Vec4f - note this from the opengl wiki
                    //"Implementations sometimes get the std140 layout wrong for Vec3f components.
                    //You are advised to manually pad your structures/arrays out and avoid using Vec3f at all."
                    eType = VertexAttribPointerType.Float;
                    compCount = 3;
                    size = Marshal.SizeOf(default(Vec4f));
                    //*******************************************
                    break;
                case VertexUserType.v4_01:
                case VertexUserType.v4_02:
                case VertexUserType.v4_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 4;
                    size = Marshal.SizeOf(default(Vec4f));
                    break;
                case VertexUserType.c4_01:
                case VertexUserType.c4_02:
                case VertexUserType.c4_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 4;
                    size = Marshal.SizeOf(default(Vec4f));
                    break;
                case VertexUserType.c3_01:
                case VertexUserType.c3_02:
                case VertexUserType.c3_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 3;
                    size = Marshal.SizeOf(default(Vec4f));  //**Look at the size: Vec4f - OpenGL requires components to be 64 byte aligned.
                    break;
                case VertexUserType.n3_01:
                case VertexUserType.n3_02:
                case VertexUserType.n3_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 3;
                    size = Marshal.SizeOf(default(Vec4f));  //**Look at the size: Vec4f  - OpenGL requires components to be 64 byte aligned.
                    break;
                case VertexUserType.x2_01:
                case VertexUserType.x2_02:
                case VertexUserType.x2_03:
                    eType = VertexAttribPointerType.Float;
                    compCount = 2;
                    size = Marshal.SizeOf(default(Vec4f));  //**Look at the size: Vec4f  - OpenGL requires components to be 64 byte aligned.
                    break;
                case VertexUserType.i2_01:
                case VertexUserType.i2_02:
                case VertexUserType.i2_03:
                    eType = VertexAttribPointerType.Int;
                    compCount = 2;
                    size = Marshal.SizeOf(default(Vec2f));
                    break;
                case VertexUserType.u2_01:
                case VertexUserType.u2_02:
                case VertexUserType.u2_03:
                    eType = VertexAttribPointerType.UnsignedInt;
                    compCount = 2;
                    size = Marshal.SizeOf(default(uVec2f));
                    break;
                //**std430 padded types.
                //case VertexUserType::v3_01_STD430:
                //    eType = GL_FLOAT;
                //    compCount = 3;
                //    size = sizeof(Vec4f);
                //    break;
                //case VertexUserType::n3_01_STD430:
                //    eType = GL_FLOAT;
                //    compCount = 3;
                //    size = sizeof(Vec4f);
                //    break;
                //case VertexUserType::x2_01_STD430:
                //    eType = GL_FLOAT;
                //    compCount = 2;
                //    size = sizeof(Vec4f);
                //    break;
                default:
                    throw new Exception("Vertex user type not impelmented.");
            }

            AddComponent(eType, compCount, size, eUserType);
        }
        public void AddComponent(VertexAttribPointerType type, int componentCount,
            int size, VertexUserType eUserType = VertexUserType.NoVertexType)
        {
            foreach (KeyValuePair<VertexUserType, VertexComponent> entry in Components)
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

            cmp.ByteOffset = 0;
            foreach (KeyValuePair<VertexUserType, VertexComponent> entry in Components)
            {
                cmp.ByteOffset += entry.Value.SizeBytes;
            }

            Components.Add(eUserType, cmp);

            //Re-calculate size of vertex
            VertexSizeBytes = 0;
            foreach (KeyValuePair<VertexUserType, VertexComponent> entry in Components)
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
        public static string GetUserTypeName(VertexUserType eUserType)
        {
            switch (eUserType)
            {
                case VertexUserType.c4_01:
                    return ("Color4f");
                case VertexUserType.v2_01:
                    return ("Position2f");
                case VertexUserType.v3_01:
                    return ("Position3f");
                case VertexUserType.n3_01:
                    return ("Normal3f");
                case VertexUserType.x2_01:
                    return ("Texcoord2f");
                case VertexUserType.u2_01:
                    return ("Unsigned_Int_2");
                case VertexUserType.v4_01:
                case VertexUserType.v4_02:
                case VertexUserType.v4_03:
                    return ("Position4f");
            };

            Gu.BRThrowNotImplementedException();
            return ("Unknown User Type.");
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct UtilMeshVert
    {
        public Vec3f _v;
        public float pad1;
        public Vec4f _c;
        public static int SizeBytes
        {
            get { return 12 * 2; }
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVert
    {
        public Vec3f _v;
        public float pad1;
        public Vec3f _n;
        public float pad2;
        public Vec2f _x;
        public float pad3;
        public float pad4;
        public static int SizeBytes
        {
            get { return 12 * 4; }
        }

        public static VertexFormat VertexFormat = null;
    }
}
