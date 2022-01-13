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
        public Vector2 _t;
        public Vector2 pad3;
        public static int SizeBytes
        {
            get { return 12; }
        }
    }
}
