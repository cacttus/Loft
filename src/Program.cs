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
        Camera3D _camera = null;
        Shader _shader;
        List<WorldObject> _objects = new List<WorldObject>();
        WorldObject boxMeshThing = null;

        double Delta = 0;
        long _frameStamp = 0;
        long _lastTime = Gu.Nanoseconds();
        double rot = 0;

        vec2 last = new vec2(0, 0);
        bool lastSet = false;
        bool oneDown = false; // TODO: use a state class for PressOrDown
        bool twoDown = false;
        bool threeDown = false;
        bool fourDown = false;
        int meshIdx = 0;

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

                _camera = new Camera3D(Width, Height);
                _camera.v3pos = new vec3(0, 0, -10);
                _camera.Update();
                _camera.lookAt(new vec3(0, 0, 0));
                _camera.Update();

                string frag = Gu.ReadTextFile(Gu.EmbeddedDataPath + "BasicShader_frag.glsl",true);
                string vert = Gu.ReadTextFile(Gu.EmbeddedDataPath + "BasicShader_vert.glsl",true);
                _shader = new Shader(vert, frag);
                Texture noise = Noise3D.TestNoise();
                Texture peron = new Texture(Gu.EmbeddedDataPath + "main char.png", true);
                Texture grass = new Texture(Gu.EmbeddedDataPath + "grass_base.png", true);

                boxMeshThing = CreateObject(mat4.Identity(), MeshData.GenBox(), new Material(noise, _shader));
                CreateObject(mat4.Identity(), MeshData.GenTextureFront(_camera, 0, 0, Width, Height), new Material(peron, _shader));
                CreateObject(mat4.Identity(), MeshData.GenPlane(10, 10), new Material(grass, _shader));

                CursorVisible = true;
                //var siz = Marshal.SizeOf(default(MeshVert));
                //var fmt = VertexFormat.DeclareVertexFormat("MeshFmt", "v_v3n3x2");
                //int n = 0;
                //n++;
            }
            catch (Exception ex)
            {
                Gu.Log.Error("Failed to initialize engine. Errors occured: " + ex.ToString());
                System.Environment.Exit(0);
            }
        }
        private WorldObject CreateObject(mat4 transform, MeshData mesh, Material material)
        {
            WorldObject ob = new WorldObject();
            ob.Mesh = mesh;
            ob.Material = material;
            _objects.Add(ob);
            return ob;
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
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            UpdateInput();

            Title = $"(Vsync: {VSync}) FPS: {1f / e.Time:0} Size: {Width}x{Height}";

            //For first frame run at a smooth time.
            Delta = 1 / 60;
            long curTime = Gu.Nanoseconds();
            if (_frameStamp > 0)
            {
                Delta = (double)((decimal)(curTime - _lastTime) / (decimal)(1000000000));
            }
            _lastTime = curTime;

            //mat4 model = mat4.GetRotation((float)rot, new vec3(0, 1, 0));
            boxMeshThing.Rotation = new vec4(0, 1, 0, (float)rot);
            rot += Math.PI * 2.0f * Delta * 0.0125f;

            _camera.Update();

            foreach (var ob in _objects)
            {
                ob.Update();
            }
        }
        private void UpdateInput()
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
                        boxMeshThing.Mesh = MeshData.GenBox();
                    }
                    if (meshIdx == 1)
                    {
                        boxMeshThing.Mesh = MeshData.GenSphere(128, 128, 1, true);
                    }
                    if (meshIdx == 2)
                    {
                        boxMeshThing.Mesh = MeshData.GenSphere(128, 128, 1, false);
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

                float rot_speed = 0.001f;

                _camera.Update();
                mat4 ry = mat4.GetRotation((float)Math.PI * mx * -rot_speed * coordMul, new vec3(0, 1, 0));
                mat4 rx = mat4.GetRotation((float)Math.PI * my * -rot_speed, _camera.v3x);
                vec3 vz = (ry * rx * new vec4(_camera.v3z, 1)).xyz().normalize();
                _camera.lookAt(_camera.v3pos + vz);
            }
            last.x = (float)mouseState.X;
            last.y = (float)mouseState.Y;
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {

            Renderer.BeginRender(this, new vec4(1, 1, 1, 1));
            foreach (var ob in _objects)
            {
                ob.Material.PreRender(Delta, _camera, ob.World);
                Renderer.Render(_camera, ob);
                ob.Material.PostRender();
            }
            Renderer.EndRender();

            //  _shader.UpdateAndBind(dt, _camera, model);
            // Renderer.Render(_camera, _mesh, _shader, _texture);

            //  GL.Disable(EnableCap.CullFace);
            //   GL.Disable(EnableCap.DepthTest);


            // _shader.UpdateAndBind(dt,_camera,  _camera.ViewMatrix.inverseOf());
            //   Renderer.Render(_camera, _textureFront, _shader, _texture);

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