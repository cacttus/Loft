using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;


namespace PirateCraft
{
    //Triangle mesh .. 
    public class MeshData
    {
        int _intVboId;
        int _intIboId;
        public int _intVaoId { get; private set; }
        public int IndexCount { get; private set; }

        public Box3f BoundBox { get; set; } =new Box3f();
       // public Box3f BoundBox { get { return _boundBox; } private set { _boundBox = value; } } 

        //Mesh data is just a byte buffer with float3 float2 float16 accessor

        private void ComputeBoundBox(in MeshVert[] verts, in uint[] indexes)
        {
            BoundBox.genResetLimits();
            foreach(uint ind in indexes)
            {
                BoundBox.genExpandByPoint(verts[indexes[ind]]._v);
            }
            float boxSize = BoundBox.Width() * BoundBox.Height() * BoundBox.Depth() ;
            int n=0;
        }
        //Do we need vertex formats if we don't have interleaved arrays?
        public MeshData(in MeshVert[] verts, in uint[] indexes)
        {
            if (verts == null || indexes == null)
            {
                Gu.Log.Error("Mesh(): Error: vertexes and indexes null.");
            }
            CreateBuffers(verts, indexes);

            ComputeBoundBox(verts,indexes);
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

            //Note: we use Vec4f size offsets here because of the 16 byte padding required by GPUs.
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
        public static uint[] GenerateQuadIndices(int numQuads, bool flip = false)
        {
            //0  1
            //2  3
            uint idx = 0;
            uint[] inds = new uint[numQuads * 6];
            for (int face = 0; face < numQuads; ++face)
            {
                inds[face * 6 + 0] = idx + 0;
                inds[face * 6 + 1] = idx + (uint)(flip ? 2 : 3);
                inds[face * 6 + 2] = idx + (uint)(flip ? 3 : 2);
                inds[face * 6 + 3] = idx + 0;
                inds[face * 6 + 4] = idx + (uint)(flip ? 3 : 1);
                inds[face * 6 + 5] = idx + (uint)(flip ? 1 : 3);
                idx += 4;
            }
            return inds;
        }
        public static MeshData GenPlane(float w, float h)
        {
            //Left Righ, Botom top, back front
            Vec3f[] box = new Vec3f[4];
            float w2 = w * 0.5f;
            float h2 = h * 0.5f;
            box[0] = new Vec3f(-w2, 0, -h2);
            box[1] = new Vec3f(w2, 0, -h2);
            box[2] = new Vec3f(-w2, 0, h2);
            box[3] = new Vec3f(w2, 0, h2);

            Vec3f[] norms = new Vec3f[1];//lrbtaf
            norms[0] = new Vec3f(0, 1, 0);

            Vec2f[] texs = new Vec2f[4];
            texs[0] = new Vec2f(0, 0);
            texs[1] = new Vec2f(1, 0);
            texs[2] = new Vec2f(0, 1);
            texs[3] = new Vec2f(1, 1);

            MeshVert[] verts = new MeshVert[4];
            verts[0 * 4 + 0] = new MeshVert() { _v = box[0], _n = norms[0], _x = texs[0] };
            verts[0 * 4 + 1] = new MeshVert() { _v = box[1], _n = norms[0], _x = texs[1] };
            verts[0 * 4 + 2] = new MeshVert() { _v = box[2], _n = norms[0], _x = texs[2] };
            verts[0 * 4 + 3] = new MeshVert() { _v = box[3], _n = norms[0], _x = texs[3] };

            var inds = GenerateQuadIndices(verts.Length / 4);
            return new MeshData(verts, inds);
        }
        public static MeshData GenBox(float w, float h, float d)
        {
            //Left Righ, Botom top, back front
            Vec3f[] box = new Vec3f[8];
            float w2=w*0.5f,h2=h*0.5f,d2=d*0.5f;
            box[0] = new Vec3f(-w2, -h2, -d2);
            box[1] = new Vec3f(w2, -h2, -d2);
            box[2] = new Vec3f(-w2, h2, -d2);
            box[3] = new Vec3f(w2, h2, -d2);
            box[4] = new Vec3f(-w2, -h2, d2);
            box[5] = new Vec3f(w2, -h2, d2);
            box[6] = new Vec3f(-w2, h2, d2);
            box[7] = new Vec3f(w2, h2, d2);

            Vec3f[] norms = new Vec3f[6];//lrbtaf
            norms[0] = new Vec3f(-1, 0, 0);
            norms[1] = new Vec3f(1, 0, 0);
            norms[2] = new Vec3f(0, -1, 0);
            norms[3] = new Vec3f(0, 1, 0);
            norms[4] = new Vec3f(0, 0, -1);
            norms[5] = new Vec3f(0, 0, 1);

            Vec2f[] texs = new Vec2f[4];
            texs[0] = new Vec2f(0, 1);
            texs[1] = new Vec2f(1, 1);
            texs[2] = new Vec2f(0, 0);
            texs[3] = new Vec2f(1, 0);

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

            var inds = GenerateQuadIndices(verts.Length/4);
            return new MeshData(verts, inds);
        }
        public static MeshData GenTextureFront(Camera3D c, float x, float y, float w, float h)
        {
            //Let's do the UI from bottom left like OpenGL
            MeshVert[] verts = new MeshVert[4];
            verts[0]._v = c.ProjectPoint(new Vec2f(x, y), TransformSpace.Local, 0.01f).p0;
            verts[1]._v = c.ProjectPoint(new Vec2f(x + w, y), TransformSpace.Local, 0.01f).p0;
            verts[2]._v = c.ProjectPoint(new Vec2f(x, y + h), TransformSpace.Local, 0.01f).p0;
            verts[3]._v = c.ProjectPoint(new Vec2f(x + w, y + h), TransformSpace.Local, 0.01f).p0;

            verts[0]._x = new Vec2f(1, 0);
            verts[1]._x = new Vec2f(1, 1);
            verts[2]._x = new Vec2f(1, 0);
            verts[3]._x = new Vec2f(0, 0);

            verts[0]._n = new Vec3f(0, 0, -1);
            verts[1]._n = new Vec3f(0, 0, -1);
            verts[2]._n = new Vec3f(0, 0, -1);
            verts[3]._n = new Vec3f(0, 0, -1);

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
                            verts[vind + p * 2 + t]._v = new Vec3f(
                                radius * MathUtils.sinf(phi[p]) * MathUtils.cosf(theta[t]),
                                radius * MathUtils.cosf(phi[p]),
                                radius * MathUtils.sinf(phi[p]) * MathUtils.sinf(theta[t])
                            );
                        }
                    }

                    if (smooth)
                    {
                        verts[vind + 0]._n = verts[vind + 0]._v.Normalized();
                        verts[vind + 1]._n = verts[vind + 1]._v.Normalized();
                        verts[vind + 2]._n = verts[vind + 2]._v.Normalized();
                        verts[vind + 3]._n = verts[vind + 3]._v.Normalized();
                    }
                    else
                    {
                        Vec3f n = Vec3f.Cross((verts[vind + 1]._v - verts[vind + 0]._v) , (verts[vind + 2]._v - verts[vind + 0]._v)).Normalized();
                        verts[vind + 0]._n = n;
                        verts[vind + 1]._n = n;
                        verts[vind + 2]._n = n;
                        verts[vind + 3]._n = n;
                    }

                    float tx0 = (float)slice / (float)slices;
                    float ty0 = (float)stack / (float)stacks;
                    float tx1 = (float)(slice + 1) / (float)slices;
                    float ty1 = (float)(stack + 1) / (float)stacks;
                    verts[vind + 0]._x = new Vec2f(tx0, ty0);
                    verts[vind + 1]._x = new Vec2f(tx1, ty0);
                    verts[vind + 2]._x = new Vec2f(tx0, ty1);
                    verts[vind + 3]._x = new Vec2f(tx1, ty1);

                }
            }

            var inds = GenerateQuadIndices(verts.Length / 4, true);

            return new MeshData(verts, inds);
        }


    }
}
