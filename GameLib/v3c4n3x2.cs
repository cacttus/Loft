using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;

namespace Oc
{
    //Vertexes
    public struct v3c4n3x2
    {

        public Vec3f _v;
        public float _pad0;
        public Vec4f _c;
        public Vec3f _n;
        public float _pad1;
        public Vec2f _x;
        public float _pad2;
        public float _pad3;

        public static int ByteSize()
        {
            int siz= System.Runtime.InteropServices.Marshal.SizeOf(default(v3c4n3x2));
            return siz;
        }
    }
}
