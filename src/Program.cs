using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;

namespace PirateCraft
{
    public class MainWindow : OpenTK.GameWindow
    {
        Camera3D _camera = new Camera3D(860, 540, 1, 1000);
        Shader _shader = new Shader();
        Texture _texture = new Texture();
        MeshData _mesh;

        public MainWindow() : base(1920 / 2, // initial width
        1080 / 2, // initial height
        GraphicsMode.Default,
        "dreamstatecoding",  // initial title
        GameWindowFlags.Default,
        DisplayDevice.Default,
        4, // OpenGL major version
        0, // OpenGL minor version
        GraphicsContextFlags.ForwardCompatible)
        {
            Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
        }
        // private void StartMonoDebugger(){
        //   Process proc = new Process {
        //     StartInfo = new ProcessStartInfo {
        //         FileName = "../../mono_debugger_daemon.sh",
        //         Arguments = "",
        //         UseShellExecute = true,
        //         RedirectStandardOutput = false,
        //         CreateNoWindow = false
        //       }
        //   };
        // }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                Gu.Log.Info("Base Dir=" + System.IO.Directory.GetCurrentDirectory());
                Gu.Init(this);
                //GenBox();
                GenSphere();
                this._camera.v3pos = new vec3(0, 0, -10);
                _shader.Load();


                //_texture.Load("../../main char.png");
                _texture = Noise3D.TestNoise();

                CursorVisible = true;
                var siz = Marshal.SizeOf(default(MeshVert));
                var fmt = VertexFormat.DeclareVertexFormat("MeshFmt", "v_v3n3x2");
                int n = 0;
                n++;
            }
            catch (Exception ex)
            {
                Gu.Log.Error("Failed to initialize engine. Errors occured: " + ex.ToString());
                System.Environment.Exit(0);
            }
        }
        private void TestFonts()
        {
            //TODO: we might need to use STB here. This is just .. ugh
            try
            {

                //Font f = new Font(ttf_loc);
                //System.Drawing.Bitmap b = f.RenderString("Hello World");
                //b.Save("./test.bmp");
                //var fff = b.RawFormat;
                //var ffff = b.PixelFormat;


                //System.Console.WriteLine("whate");
                //System.Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
                //NRasterizer.OpenTypeReader r = new NRasterizer.OpenTypeReader();
                //NRasterizer.Typeface face;

                //using (var fs = File.Open(ttf_loc, FileMode.Open, FileAccess.Read, FileShare.None))
                //{
                //    face = r.Read(fsgit stat);
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private void GenBox()
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

            uint idx = 0;
            uint[] inds = new uint[6 * 6];
            for (int face = 0; face < 6; ++face)
            {
                inds[face * 6 + 0] = idx + 0;
                inds[face * 6 + 1] = idx + 3;
                inds[face * 6 + 2] = idx + 2;
                inds[face * 6 + 3] = idx + 0;
                inds[face * 6 + 4] = idx + 1;
                inds[face * 6 + 5] = idx + 3;
                idx += 4;
            }

            _mesh = new MeshData(verts, inds);
        }
        private void GenSphere(int slices = 32, int stacks = 32, float radius = 1, bool smooth = false)
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
                                radius * MathUtils.Sinf(phi[p]) * MathUtils.Cosf(theta[t]),
                                radius * MathUtils.Cosf(phi[p]),
                                radius * MathUtils.Sinf(phi[p]) * MathUtils.Sinf(theta[t])
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
                    float tx1 = (float)(slice+1) / (float)slices;
                    float ty1 = (float)(stack+1) / (float)stacks;
                    verts[vind + 0]._x.construct(tx0,ty0);
                    verts[vind + 1]._x.construct(tx1,ty0);
                    verts[vind + 2]._x.construct(tx0,ty1);
                    verts[vind + 3]._x.construct(tx1,ty1);

                }
            }

            uint idx = 0;
            int icount = verts.Length / 4 * 6;
            uint[] inds = new uint[icount];
            for (int face = 0; face < icount / 6; ++face)
            {
                inds[face * 6 + 0] = idx + 0;
                inds[face * 6 + 1] = idx + 2;
                inds[face * 6 + 2] = idx + 3;
                inds[face * 6 + 3] = idx + 0;
                inds[face * 6 + 4] = idx + 3;
                inds[face * 6 + 5] = idx + 1;
                idx += 4;
            }

            _mesh = new MeshData(verts, inds);
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            HandleKeyboard();
        }
        vec2 last = new vec2(0, 0);
        bool lastSet = false;
        bool oneDown = false;
        bool twoDown = false;
        bool threeDown = false;
        bool fourDown = false;
        int meshIdx = 0;
        private void HandleKeyboard()
        {
            float coordMul = (Gu.CoordinateSystem == CoordinateSystem.Lhs ? -1 : 1);

            var keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Key.Escape))
            {
                Exit();
            }
            if (keyState.IsKeyDown(Key.Up) || keyState.IsKeyDown(Key.W))
            {
                _camera.v3pos += _camera.v3z * 0.1f;
            }
            if (keyState.IsKeyDown(Key.Down) || keyState.IsKeyDown(Key.S))
            {
                _camera.v3pos -= _camera.v3z * 0.1f;
            }
            if (keyState.IsKeyDown(Key.Right) || keyState.IsKeyDown(Key.D))
            {
                _camera.v3pos -= _camera.v3x * 0.1f * coordMul;
            }
            if (keyState.IsKeyDown(Key.Left) || keyState.IsKeyDown(Key.A))
            {
                _camera.v3pos += _camera.v3x * 0.1f * coordMul;
            }
            if (keyState.IsKeyDown(Key.Number1))
            {
                if (oneDown == false)
                {
                    _shader.lightingModel = ((_shader.lightingModel + 1) % 4);
                }
                oneDown = true;
            }
            else
            {
                oneDown = false;
            }

            if (keyState.IsKeyDown(Key.Number2))
            {
                //if (twoDown == false)
                {
                    _shader.GGX_X = (_shader.GGX_X + 0.01f) % 3.0f;
                }
                twoDown = true;
            }
            else
            {
                twoDown = false;
            }

            if (keyState.IsKeyDown(Key.Number3))
            {
                //  if (threeDown == false)
                {
                    _shader.GGX_Y = (_shader.GGX_Y + 0.01f) % 3.0f;

                }
                threeDown = true;
            }
            else
            {
                threeDown = false;
            }
            if (keyState.IsKeyDown(Key.Number4))
            {
                if (fourDown == false)
                {
                    meshIdx = (meshIdx + 1) % 3;
                    if (meshIdx == 0)
                    {
                        GenBox();
                    }
                    if (meshIdx == 1)
                    {
                        GenSphere(32, 32, 1, true);
                    }
                    if (meshIdx == 2)
                    {
                        GenSphere(32, 32, 1, false);
                    }
                }
                fourDown = true;
            }
            else
            {
                fourDown = false;
            }
            var mouseState = Mouse.GetState();
            if (lastSet == false)
            {
                last.x = (float)mouseState.X;
                last.y = (float)mouseState.Y;
                lastSet = true;
            }
            if (mouseState.IsButtonDown(MouseButton.Left))
            {
                float mx = (float)mouseState.X - last.x;
                float my = (float)mouseState.Y - last.y;


                mat4 rx = mat4.GetRotation((float)Math.PI * mx * -0.001f * coordMul, new vec3(0, 1, 0));
                mat4 ry = mat4.GetRotation((float)Math.PI * my * -0.001f, _camera.v3x);

                _camera.v3z = (rx * ry * new vec4(_camera.v3z, 1)).xyz().normalize();
                _camera.v3x = new vec3(0, 1, 0).cross(_camera.v3z).normalize();
                _camera.v3y = _camera.v3z.cross(_camera.v3x);
            }
            last.x = (float)mouseState.X;
            last.y = (float)mouseState.Y;
        }

        long _frameStamp = 0;
        long _lastTime = Gu.Nanoseconds();
        double rot = 0;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            // Title = $"(Vsync: {VSync}) FPS: {1f / e.Time:0}";

            //For first frame run at a smooth time.
            double dt = 1 / 60;
            long curTime = Gu.Nanoseconds();
            if (_frameStamp > 0)
            {
                dt = (double)((decimal)(curTime - _lastTime) / (decimal)(1000000000));
            }
            _lastTime = curTime;

            mat4 model = mat4.GetRotation((float)rot, new vec3(0, 1, 0));
            rot += Math.PI * 2.0f * dt * 0.0125f;

            _camera.Update();
            _shader.UpdateAndBind(dt, _camera, model);

            Color4 backColor;
            backColor.A = 1.0f;
            backColor.R = 0.1f;
            backColor.G = 0.1f;
            backColor.B = 0.3f;
            GL.ClearColor(backColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gu.CheckGpuErrorsRt();

            Renderer.Render(_camera, _mesh, _shader, _texture);

            Gu.CheckGpuErrorsRt();
            SwapBuffers();

            _frameStamp++;
        }
    }
    class MainClass
    {
        public static void Main(string[] args)
        {
            var m = new MainWindow();
            m.VSync = OpenTK.VSyncMode.On;
            m.Run();
        }
    }
}