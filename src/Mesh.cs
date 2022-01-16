using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PirateCraft
{
    //Triangle mesh .. 
    public class Mesh
    {
        //VAO
        //Shader
        int _intVboId;
        int _intIboId;
        public int _intVaoId { get; private set; }
        //List<MeshVert> _verts;
        //List<uint> _indexes = null;
        public int IndexCount { get; private set;}

        //Do we need vertex formats if we don't have interleaved arrays?
        public Mesh(in MeshVert[] verts, in uint[] indexes)
        {
            if (verts == null || indexes == null)
            {
                Gu.Log.Error("Error: vertexes and indexes required.");
            }
            CreateBuffers(verts, indexes);
        }
        private void CreateBuffers(in MeshVert[] verts, in uint[] indexes)
        {
            Gu.CheckGpuErrorsDbg();

            _intVboId = GL.GenBuffer();
            _intIboId = GL.GenBuffer();
            _intVaoId = GL.GenVertexArray();

            int attr_v = 0;//These are the layout=x locations in glsl
            int attr_n = 1;
            int attr_x = 2;

            GL.BindVertexArray(_intVaoId);

            Gu.CheckGpuErrorsDbg();

            //Bind Vertexes
            GL.BindBuffer(BufferTarget.ArrayBuffer, _intVboId);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(verts.Length * MeshVert.SizeBytes),
                verts,
                BufferUsageHint.StaticDraw
                );

            //Note: we use vec4 size offsets here because of the 16 byte padding required by GPUs.
            int v4s = Marshal.SizeOf(default(Vector4));
            GL.EnableVertexAttribArray(attr_v);
            GL.EnableVertexAttribArray(attr_n);
            GL.EnableVertexAttribArray(attr_x);
            GL.VertexAttribPointer(attr_v, 3, VertexAttribPointerType.Float, false, MeshVert.SizeBytes, (IntPtr)(0));
            GL.VertexAttribPointer(attr_n, 3, VertexAttribPointerType.Float, false, MeshVert.SizeBytes, (IntPtr)(0 + v4s));
            GL.VertexAttribPointer(attr_x, 2, VertexAttribPointerType.Float, false, MeshVert.SizeBytes, (IntPtr)(0 + v4s + v4s));

            Gu.CheckGpuErrorsDbg();

            //Bind indexes
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _intIboId);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(indexes.Length * sizeof(uint)),
                indexes,
                BufferUsageHint.StaticDraw
                );
            Gu.CheckGpuErrorsDbg();

            IndexCount = indexes.Length;

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            Gu.CheckGpuErrorsDbg();
        }
        private void BeginEdit(out IntPtr verts, out IntPtr inds)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, _intVboId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _intIboId);
            Gu.CheckGpuErrorsDbg();

            inds = GL.MapBuffer(BufferTarget.ElementArrayBuffer, BufferAccess.WriteOnly);
            verts = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);
            Gu.CheckGpuErrorsDbg();
        }
        private void EndEdit()
        {
            GL.UnmapBuffer(BufferTarget.ElementArrayBuffer);
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);
            Gu.CheckGpuErrorsDbg();

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        //private void CopyVertexData(in List<MeshVert> verts, in List<uint> indexes)
        //{
        //    /* Layout:
        //     v0     v1
        //     x-----x
        //     |    /|
        //     |  /  |
        //     x/----x
        //    v3     v2
        //     indexes: 321, 310 , CCW
        //     */
        //    // Creates vertexes and stuff

        //    unsafe
        //    {
        //        uint* pIndexes = (uint*)pIbo.ToPointer();

        //        pIndexes[0] = 3;//T1
        //        pIndexes[1] = 2;
        //        pIndexes[2] = 1;
        //        pIndexes[3] = 3;//T2
        //        pIndexes[4] = 1;
        //        pIndexes[5] = 0;

        //        MeshVert* pVertexes = (MeshVert*)pVbo.ToPointer();

        //        //for (int i = 0; i < 4; ++i)
        //        //{
        //        //    pVertexes[i]._v.Z = 0; // default z
        //        //    //white color
        //        //    //pVertexes[i]._c.X = pVertexes[i]._c.Y = pVertexes[i]._c.Z = pVertexes[i]._c.W = 1.0f;
        //        //    //normal up
        //        //    pVertexes[i]._n = new Vec3f(0, 0, 1);
        //        //}

        //        pVertexes[0]._v.X = _imageBox._vmin.X;
        //        pVertexes[0]._v.Y = _imageBox._vmax.Y;
        //        pVertexes[0]._x.X = 0.0020f;
        //        pVertexes[0]._x.Y = 0.0020f;// shrink texture a bit so we don't show any wrapping seams

        //        pVertexes[1]._v.X = _imageBox._vmax.X;
        //        pVertexes[1]._v.Y = _imageBox._vmax.Y;
        //        pVertexes[1]._x.X = 0.9979f;
        //        pVertexes[1]._x.Y = 0.0020f;

        //        pVertexes[2]._v.X = _imageBox._vmax.X;
        //        pVertexes[2]._v.Y = _imageBox._vmin.Y;
        //        pVertexes[2]._x.X = 0.9979f;
        //        pVertexes[2]._x.Y = 0.9979f;

        //        pVertexes[3]._v.X = _imageBox._vmin.X;
        //        pVertexes[3]._v.Y = _imageBox._vmin.Y;
        //        pVertexes[3]._x.X = 0.0020f;
        //        pVertexes[3]._x.Y = 0.9979f;
        //    }

        //}

    }
}
