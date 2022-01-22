using System;
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
                GenMesh();
                this._camera.v3pos = new vec3(0, 0, -10);
                _shader.Load();
                _texture.Load("../../main char.png");
                CursorVisible = true;
               var siz= Marshal.SizeOf(default(MeshVert));
                var fmt = VertexFormat.DeclareVertexFormat("MeshFmt", "v_v3n3x2");
                int n=0;
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
        private void GenMesh()
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
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            HandleKeyboard();
        }
        vec2 last = new vec2(0, 0);
        bool lastSet = false;
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
            rot += Math.PI * 2.0f * dt * 0.125f;

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