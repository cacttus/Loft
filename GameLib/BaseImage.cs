using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat4 = OpenTK.Matrix4;
namespace Oc
{
    public class BaseImage : BaseNode
    {
        Texture _objTexture;
        int _intVboId;
        int _intIboId;
        int _intVaoId;

        const int NumVertexes = 4;
        const int NumIndexes = 6;

        Box3f _imageBox = new Box3f();
        public Texture GetTexture() { return _objTexture; }
        public bool UserAdjusted { get; set; }
        private void CreateBuffer()
        {
            Gu.CheckGpuErrorsDbg();

            _intVboId = GL.GenBuffer();
            _intIboId = GL.GenBuffer();
            _intVaoId = GL.GenVertexArray();

            int attr_v = 0;//These are the layout=x locations in glsl
            int attr_c = 1;
            int attr_n = 2;
            int attr_x = 3;

            GL.BindVertexArray(_intVaoId);

            Gu.CheckGpuErrorsDbg();

            //Bind Vertexes
            GL.BindBuffer(BufferTarget.ArrayBuffer, _intVboId);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(NumVertexes * v3c4n3x2.ByteSize()),
                IntPtr.Zero, // Fill data later
                BufferUsageHint.StaticDraw
                );

            //Note: we use vec4 size offsets here because of the 16 byte padding required by GPUs.
            int v4s = System.Runtime.InteropServices.Marshal.SizeOf(default(Vec4f));
            GL.EnableVertexAttribArray(attr_v);
            GL.EnableVertexAttribArray(attr_c);
            GL.EnableVertexAttribArray(attr_n);
            GL.EnableVertexAttribArray(attr_x);
            GL.VertexAttribPointer(attr_v, 3, VertexAttribPointerType.Float, false, v3c4n3x2.ByteSize(), (IntPtr)(0));
            GL.VertexAttribPointer(attr_c, 4, VertexAttribPointerType.Float, false, v3c4n3x2.ByteSize(), (IntPtr)(0 + v4s));
            GL.VertexAttribPointer(attr_n, 3, VertexAttribPointerType.Float, false, v3c4n3x2.ByteSize(), (IntPtr)(0 + v4s + v4s));
            GL.VertexAttribPointer(attr_x, 2, VertexAttribPointerType.Float, false, v3c4n3x2.ByteSize(), (IntPtr)(0 + v4s + v4s + v4s));

            Gu.CheckGpuErrorsDbg();

            //Bind indexes
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _intIboId);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(NumIndexes * sizeof(uint)),
                IntPtr.Zero, //Fill data later
                BufferUsageHint.StaticDraw
                );
            Gu.CheckGpuErrorsDbg();

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);

            Gu.CheckGpuErrorsDbg();
        }
        private void CopyVertexData()
        {
            /* Layout:
             v0     v1
             x-----x
             |    /|
             |  /  |
             x/----x
            v3     v2
             indexes: 321, 310 , CCW
             */
            // Creates vertexes and stuff
            GL.BindBuffer(BufferTarget.ArrayBuffer, _intVboId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _intIboId);
            Gu.CheckGpuErrorsDbg();

            IntPtr pIbo = GL.MapBuffer(BufferTarget.ElementArrayBuffer, BufferAccess.WriteOnly);
            IntPtr pVbo = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);
            Gu.CheckGpuErrorsDbg();
            unsafe
            {
                uint* pIndexes = (uint*)pIbo.ToPointer();

                pIndexes[0] = 3;//T1
                pIndexes[1] = 2;
                pIndexes[2] = 1;
                pIndexes[3] = 3;//T2
                pIndexes[4] = 1;
                pIndexes[5] = 0;

                v3c4n3x2* pVertexes = (v3c4n3x2*)pVbo.ToPointer();

                for (int i = 0; i < 4; ++i)
                {
                    pVertexes[i]._v.Z = 0; // default z
                    //white color
                    pVertexes[i]._c.X = pVertexes[i]._c.Y = pVertexes[i]._c.Z = pVertexes[i]._c.W = 1.0f;
                    //normal up
                    pVertexes[i]._n = new Vec3f(0, 0, 1);
                }

                pVertexes[0]._v.X = _imageBox._vmin.X;
                pVertexes[0]._v.Y = _imageBox._vmax.Y;
                pVertexes[0]._x.X = 0.0020f;
                pVertexes[0]._x.Y = 0.0020f;// shrink texture a bit so we don't show any wrapping seams

                pVertexes[1]._v.X = _imageBox._vmax.X;
                pVertexes[1]._v.Y = _imageBox._vmax.Y;
                pVertexes[1]._x.X = 0.9979f;
                pVertexes[1]._x.Y = 0.0020f;

                pVertexes[2]._v.X = _imageBox._vmax.X;
                pVertexes[2]._v.Y = _imageBox._vmin.Y;
                pVertexes[2]._x.X = 0.9979f;
                pVertexes[2]._x.Y = 0.9979f;

                pVertexes[3]._v.X = _imageBox._vmin.X;
                pVertexes[3]._v.Y = _imageBox._vmin.Y;
                pVertexes[3]._x.X = 0.0020f;
                pVertexes[3]._x.Y = 0.9979f;
            }
            GL.UnmapBuffer(BufferTarget.ElementArrayBuffer);
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);
            Gu.CheckGpuErrorsDbg();

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
        private TextureLoadResult CreateTexture(string path)
        {
            _objTexture = new Texture();
            return _objTexture.Load(path);
        }
        public BaseImage(RenderManager rm, NodeManager om)
            : base(rm, om)
        {
        }
        public TextureLoadResult Load(string path, bool blnScaleImage = true)
        {
            //NOTE: must crfeate texture BEFORE the vertex aray.
            TextureLoadResult res = CreateTexture(path);
            if(res!=TextureLoadResult.Success)
                return res;

            CreateBuffer();
            FitToWindow(GetRenderManager().GetViewport().Width, GetRenderManager().GetViewport().Height);
            CopyVertexData();

            return res;
        }
        public override void Free()
        {
            _objTexture.Free();
            _objTexture = null;

            GL.DeleteBuffer(_intVboId);
            GL.DeleteBuffer(_intIboId);
            GL.DeleteVertexArray(_intVaoId);
        }
        public void FitToWindow(int vpWidth, int vpHeight)
        {
            float w = (float)vpWidth;
            float h = (float)vpHeight;
            float tw = (float)_objTexture.Width;
            float th = (float)_objTexture.Height;
            float tx = 0, ty = 0;

            //Scale image to fit window
            if (tw > w)
            {
                th *= w / tw; // shrink
                tw = w;
            }
            if (th > h)
            {
                tw *= h / th; // shrink
                th = h;
            }

            //Center image.
            tx = (int)(-tw * 0.5);
            ty = (int)(-th * 0.5);

            _imageBox._vmin.X = tx;
            _imageBox._vmin.Y = ty;
            _imageBox._vmax.X = tx + tw;
            _imageBox._vmax.Y = ty + th;

            _imageBox._vmin.Z = -0.5f;
            _imageBox._vmax.Z = 0.5f;
        }
        public override void Update(RenderManager rm)
        {
            CopyVertexData();
        }
        public override void Render(RenderManager rm)
        {
            if (_objTexture != null)
                _objTexture.Bind();

            Gu.CheckGpuErrorsDbg();
            //bool b = GL.IsVertexArray(_intVaoId);
            GL.BindVertexArray(_intVaoId);
            Gu.CheckGpuErrorsDbg();

            GL.BindBuffer(BufferTarget.ArrayBuffer, _intVboId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _intIboId);

            GL.DrawElements(PrimitiveType.Triangles,
                            NumIndexes,
                            DrawElementsType.UnsignedInt,
                            IntPtr.Zero
                            );

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            Gu.CheckGpuErrorsDbg();

            if (_objTexture != null)
                _objTexture.Unbind();
        }
        public override void UpdateBoundBox()
        {
            //Multiply transform by our box to transform it
            Mat4 trans = Mat4.CreateTranslation(GetPos());
            Mat4 rot = Mat4.CreateFromAxisAngle(GetRotationVector(), GetRotation());
            Mat4 scl = Mat4.CreateScale(GetScale());

            Mat4 res = Mat4.Mult(Mat4.Mult(scl, rot), trans);


            Vec3f mint = Vec4f.Transform(new Vec4f(_imageBox._vmin,0), res).Xyz;
            Vec3f maxt = Vec4f.Transform(new Vec4f(_imageBox._vmax,0), res).Xyz;
            
            //Compute max and min of translated image.
            BoundBoxComputed._vmin.X = Math.Min(mint.X, maxt.X);
            BoundBoxComputed._vmin.Y = Math.Min(mint.Y, maxt.Y);
            BoundBoxComputed._vmin.Z = Math.Min(mint.Z, maxt.Z);

            BoundBoxComputed._vmax.X = Math.Max(mint.X, maxt.X);
            BoundBoxComputed._vmax.Y = Math.Max(mint.Y, maxt.Y);
            BoundBoxComputed._vmax.Z = Math.Max(mint.Z, maxt.Z);

            BoundBoxComputed.Validate();

            _iBoundBoxUpdateStamp = _objRenderManager.GetFrameStamp();
        }
        public override void Resize(Viewport vp)
        {
            if (!UserAdjusted)
            {
                FitToWindow(vp.Width, vp.Height);
                Update(_objRenderManager);
            }
        }
        public void SaveToDisk(string path)
        {
            int width;
            int height;
            GL.BindTexture(TextureTarget.Texture2D, GetTexture().GetGlId());

            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out width);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out height);

            byte[] textureData = new byte[width * height * 4];
            GL.GetTexImage(TextureTarget.Texture2D,
                0,
                GetTexture().GlPixelFormat,
                PixelType.UnsignedByte,
                textureData
                );
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // save
            var stride = width * 4; // 4 bytes
            //var newbytes = PadLines(textureData, height, width); // This is if we weren't 32 bit.
            var im = new Bitmap(width, height, stride,
                                System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                                System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(textureData, 0));

            im.Save(path);
        }
        //Many thanks, T Cooper
        //http://stackoverflow.com/questions/5124713/can-bitmap-object-be-save-as-png-or-jpeg-file-format
        //static byte[] PadLines(byte[] bytes, int rows, int columns)
        //{
        //    //The old and new offsets could be passed through parameters,
        //    //but I hardcoded them here as a sample.
        //    var currentStride = columns * 3;
        //    var newStride = columns * 4;
        //    var newBytes = new byte[newStride * rows];
        //    for (var i = 0; i < rows; i++)
        //        Buffer.BlockCopy(bytes, currentStride * i, newBytes, newStride * i, currentStride);
        //    return newBytes;
        //}

    }
}
