using System;
using System.Runtime.InteropServices;
using OpenTK;
namespace PirateCraft
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVert
    {
        public Vector3 _v;
        public float pad1;
        public Vector3 _n;
        public float pad2;
        public Vector2 _x;
        public float pad3;
        public float pad4;
        public static int SizeBytes
        {
            get { return 12 * 4; }
        }
    }
}
