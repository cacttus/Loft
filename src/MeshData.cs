using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PirateCraft
{
    //Triangle mesh .. 
    public class MeshData
    {
        int _intVboId;
        int _intIboId;
        public int _intVaoId { get; private set; }
        public int IndexCount { get; private set; }
        // public VertexFormat VertexFormat { get; private set; }

        //Mesh data is just a byte buffer with float3 float2 float16 accessor

        //Do we need vertex formats if we don't have interleaved arrays?
        public MeshData(in MeshVert[] verts, in uint[] indexes)
        {
            if (verts == null || indexes == null)
            {
                Gu.Log.Error("Mesh(): Error: vertexes and indexes null.");
            }
            CreateBuffers(verts, indexes);
        }
        public MeshData() { }
        protected void CreateBuffers(in MeshVert[] verts, in uint[] indexes)
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
                (IntPtr)(verts.Length * MeshVert.SizeBytes/* VertexFormat.VertexSizeBytes*/),
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


        public static MeshData GenPlane(float w, float h)
        {
            //Left Righ, Botom top, back front
            vec3[] box = new vec3[8];
            box[0] = new vec3(-w * 0.5f, 0, -h * 0.5f);
            box[1] = new vec3(w * 0.5f, 0, -h * 0.5f);
            box[2] = new vec3(-w * 0.5f, 0, h * 0.5f);
            box[3] = new vec3(w * 0.5f, 0, h * 0.5f);

            vec3[] norms = new vec3[1];//lrbtaf
            norms[0] = new vec3(0, 1, 0);

            vec2[] texs = new vec2[4];
            texs[0] = new vec2(0, 0);
            texs[1] = new vec2(1, 0);
            texs[2] = new vec2(0, 1);
            texs[3] = new vec2(1, 1);

            MeshVert[] verts = new MeshVert[4];
            verts[0 * 4 + 0] = new MeshVert() { _v = box[0], _n = norms[0], _x = texs[0] };
            verts[0 * 4 + 1] = new MeshVert() { _v = box[1], _n = norms[0], _x = texs[1] };
            verts[0 * 4 + 2] = new MeshVert() { _v = box[2], _n = norms[0], _x = texs[2] };
            verts[0 * 4 + 3] = new MeshVert() { _v = box[3], _n = norms[0], _x = texs[3] };

            var inds = GenerateQuadIndices(6);
            return new MeshData(verts, inds);
        }
        public static MeshData GenBox()
        {
            //Left Righ, Botom top, back front
            vec3[] box = new vec3[8];
            box[0] = new vec3(-1, -1, -1);
            box[1] = new vec3(1, -1, -1);
            box[2] = new vec3(-1, 1, -1);
            box[3] = new vec3(1, 1, -1);
            box[4] = new vec3(-1, -1, 1);
            box[5] = new vec3(1, -1, 1);
            box[6] = new vec3(-1, 1, 1);
            box[7] = new vec3(1, 1, 1);

            vec3[] norms = new vec3[6];//lrbtaf
            norms[0] = new vec3(-1, 0, 0);
            norms[1] = new vec3(1, 0, 0);
            norms[2] = new vec3(0, -1, 0);
            norms[3] = new vec3(0, 1, 0);
            norms[4] = new vec3(0, 0, -1);
            norms[5] = new vec3(0, 0, 1);

            vec2[] texs = new vec2[4];
            texs[0] = new vec2(0, 1);
            texs[1] = new vec2(1, 1);
            texs[2] = new vec2(0, 0);
            texs[3] = new vec2(1, 0);

            //     6       7
            // 2      3
            //     4       5
            // 0      1
            MeshVert[] verts = new MeshVert[6 * 4];//lrbtaf
            verts[0 * 4 + 0] = new MeshVert() { _v = box[4], _n = norms[0], _x = texs[0] };
            verts[0 * 4 + 1] = new MeshVert() { _v = box[0], _n = norms[0], _x = texs[1] };
            verts[0 * 4 + 2] = new MeshVert() { _v = box[6], _n = norms[0], _x = texs[2] };
            verts[0 * 4 + 3] = new MeshVert() { _v = box[2], _n = norms[0], _x = texs[3] };

            verts[1 * 4 + 0] = new MeshVert() { _v = box[1], _n = norms[1], _x = texs[0] };
            verts[1 * 4 + 1] = new MeshVert() { _v = box[5], _n = norms[1], _x = texs[1] };
            verts[1 * 4 + 2] = new MeshVert() { _v = box[3], _n = norms[1], _x = texs[2] };
            verts[1 * 4 + 3] = new MeshVert() { _v = box[7], _n = norms[1], _x = texs[3] };

            verts[2 * 4 + 0] = new MeshVert() { _v = box[4], _n = norms[2], _x = texs[0] };
            verts[2 * 4 + 1] = new MeshVert() { _v = box[5], _n = norms[2], _x = texs[1] };
            verts[2 * 4 + 2] = new MeshVert() { _v = box[0], _n = norms[2], _x = texs[2] };
            verts[2 * 4 + 3] = new MeshVert() { _v = box[1], _n = norms[2], _x = texs[3] };

            verts[3 * 4 + 0] = new MeshVert() { _v = box[2], _n = norms[3], _x = texs[0] };
            verts[3 * 4 + 1] = new MeshVert() { _v = box[3], _n = norms[3], _x = texs[1] };
            verts[3 * 4 + 2] = new MeshVert() { _v = box[6], _n = norms[3], _x = texs[2] };
            verts[3 * 4 + 3] = new MeshVert() { _v = box[7], _n = norms[3], _x = texs[3] };

            verts[4 * 4 + 0] = new MeshVert() { _v = box[0], _n = norms[4], _x = texs[0] };
            verts[4 * 4 + 1] = new MeshVert() { _v = box[1], _n = norms[4], _x = texs[1] };
            verts[4 * 4 + 2] = new MeshVert() { _v = box[2], _n = norms[4], _x = texs[2] };
            verts[4 * 4 + 3] = new MeshVert() { _v = box[3], _n = norms[4], _x = texs[3] };

            verts[5 * 4 + 0] = new MeshVert() { _v = box[5], _n = norms[5], _x = texs[0] };
            verts[5 * 4 + 1] = new MeshVert() { _v = box[4], _n = norms[5], _x = texs[1] };
            verts[5 * 4 + 2] = new MeshVert() { _v = box[7], _n = norms[5], _x = texs[2] };
            verts[5 * 4 + 3] = new MeshVert() { _v = box[6], _n = norms[5], _x = texs[3] };

            var inds = GenerateQuadIndices(6);
            return new MeshData(verts, inds);
        }
        public static uint[] GenerateQuadIndices(int numQuads)
        {
            uint idx = 0;
            uint[] inds = new uint[numQuads * 6];
            for (int face = 0; face < numQuads; ++face)
            {
                inds[face * 6 + 0] = idx + 0;
                inds[face * 6 + 1] = idx + 3;
                inds[face * 6 + 2] = idx + 2;
                inds[face * 6 + 3] = idx + 0;
                inds[face * 6 + 4] = idx + 1;
                inds[face * 6 + 5] = idx + 3;
                idx += 4;
            }
            return inds;
        }
        public static MeshData GenTextureFront(Camera3D c, float x, float y, float w, float h)
        {
            //Let's do the UI from bottom left like OpenGL
            MeshVert[] verts = new MeshVert[4];
            verts[0]._v = c.ProjectPoint(new vec2(x, y), TransformSpace.Local, 0.01f).p0;
            verts[1]._v = c.ProjectPoint(new vec2(x + w, y), TransformSpace.Local, 0.01f).p0;
            verts[2]._v = c.ProjectPoint(new vec2(x, y + h), TransformSpace.Local, 0.01f).p0;
            verts[3]._v = c.ProjectPoint(new vec2(x + w, y + h), TransformSpace.Local, 0.01f).p0;

            verts[0]._x.construct(1, 0);
            verts[1]._x.construct(1, 1);
            verts[2]._x.construct(1, 0);
            verts[3]._x.construct(0, 0);

            verts[0]._n.construct(0, 0, -1);
            verts[1]._n.construct(0, 0, -1);
            verts[2]._n.construct(0, 0, -1);
            verts[3]._n.construct(0, 0, -1);

            var inds = GenerateQuadIndices(verts.Length / 4);

            return new MeshData(verts, inds);
        }
        public static MeshData GenSphere(int slices = 128, int stacks = 128, float radius = 1, bool smooth = false)
        {
            int vcount = slices * stacks * 4;
            MeshVert[] verts = new MeshVert[vcount];

            //Use a 2D grid as a sphere. This is less optimal but doesn't mess up the tex coords.
            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    float[] phi = new float[2];
                    float[] theta = new float[2];
                    phi[0] = MathUtils.M_PI * ((float)stack / (float)stacks);
                    phi[1] = MathUtils.M_PI * ((float)(stack + 1) / (float)stacks); //0<phi<pi
                    theta[0] = MathUtils.M_2PI * ((float)slice / (float)slices);
                    theta[1] = MathUtils.M_2PI * ((float)(slice + 1) / (float)slices);//0<theta<2pi

                    int vind = (stack * slices + slice) * 4;
                    for (int p = 0; p < 2; ++p)
                    {
                        for (int t = 0; t < 2; ++t)
                        {
                            // 2 3
                            // 0 1  
                            // >x ^y
                            verts[vind + p * 2 + t]._v.construct(
                                radius * MathUtils.sinf(phi[p]) * MathUtils.cosf(theta[t]),
                                radius * MathUtils.cosf(phi[p]),
                                radius * MathUtils.sinf(phi[p]) * MathUtils.sinf(theta[t])
                            );
                        }
                    }

                    if (smooth)
                    {
                        verts[vind + 0]._n = verts[vind + 0]._v.normalized();
                        verts[vind + 1]._n = verts[vind + 1]._v.normalized();
                        verts[vind + 2]._n = verts[vind + 2]._v.normalized();
                        verts[vind + 3]._n = verts[vind + 3]._v.normalized();
                    }
                    else
                    {
                        vec3 n = (verts[vind + 1]._v - verts[vind + 0]._v).cross(verts[vind + 2]._v - verts[vind + 0]._v).normalized();
                        verts[vind + 0]._n = n;
                        verts[vind + 1]._n = n;
                        verts[vind + 2]._n = n;
                        verts[vind + 3]._n = n;
                    }

                    float tx0 = (float)slice / (float)slices;
                    float ty0 = (float)stack / (float)stacks;
                    float tx1 = (float)(slice + 1) / (float)slices;
                    float ty1 = (float)(stack + 1) / (float)stacks;
                    verts[vind + 0]._x.construct(tx0, ty0);
                    verts[vind + 1]._x.construct(tx1, ty0);
                    verts[vind + 2]._x.construct(tx0, ty1);
                    verts[vind + 3]._x.construct(tx1, ty1);

                }
            }

            //uint idx = 0;
            //int icount = verts.Length / 4 * 6;
            //uint[] inds = new uint[icount];
            //for (int face = 0; face < icount / 6; ++face)
            //{
            //    inds[face * 6 + 0] = idx + 0;
            //    inds[face * 6 + 1] = idx + 2;
            //    inds[face * 6 + 2] = idx + 3;
            //    inds[face * 6 + 3] = idx + 0;
            //    inds[face * 6 + 4] = idx + 3;
            //    inds[face * 6 + 5] = idx + 1;
            //    idx += 4;
            //}
            var inds = GenerateQuadIndices(verts.Length / 4);

            return new MeshData(verts, inds);
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
