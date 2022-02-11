using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;
using Quat = OpenTK.Quaternion;
using System.Security.Cryptography;

namespace PirateCraft
{
    public class MainWindow : OpenTK.GameWindow
    {
        Camera3D _camera = null;
        WorldObject _boxMeshThing = null;

        double rot = 0;

        Vec2f last = new Vec2f(0, 0);
        bool lastSet = false;
        int meshIdx = 0;
        const float scale = 0.5f;

        public MainWindow() : base((int)(1920 * scale), (int)(1080 * scale), 
        GraphicsMode.Default, "Test", GameWindowFlags.Default, DisplayDevice.Default, 4, 0, GraphicsContextFlags.ForwardCompatible)
        {
            Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
        }
        protected override void OnResize(EventArgs e)
        {
            _camera.Viewport_Width = Width;
            _camera.Viewport_Height = Height;
        }
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                Gu.Log.Info("Base Dir=" + System.IO.Directory.GetCurrentDirectory());
                Gu.Init(this);

                _camera = new Camera3D(Width, Height);
                _camera.Position = new Vec3f(0, 0, -10);
                _camera.Update();
                Gu.World.Objects.Add(_camera);

                //string frag = Gu.ReadTextFile(Gu.EmbeddedDataPath + "BasicShader_frag.glsl",true);
                //string vert = Gu.ReadTextFile(Gu.EmbeddedDataPath + "BasicShader_vert.glsl",true);
                //S_shader = new Shader(vert, frag);

                Texture noise = Noise3D.TestNoise();
                Texture peron = new Texture(Gu.EmbeddedDataPath + "main char.png", true);
                Texture grass = new Texture(Gu.EmbeddedDataPath + "grass_base.png", true);

                _boxMeshThing = CreateObject("BoxMesh",MeshData.GenBox(1, 1, 1), new Material(noise, Shader.DefaultDiffuse()));
                CreateObject("TextureFront",MeshData.GenTextureFront(_camera, 0, 0, Width, Height), new Material(peron, Shader.DefaultDiffuse()));
                CreateObject("Plane.",MeshData.GenPlane(10, 10), new Material(grass, Shader.DefaultDiffuse()));

                _boxMeshThing.Position = new Vec3f(0, _boxMeshThing.BoundBox.Height() * 0.5f, 0);

                CursorVisible = true;
                //var siz = Mvec3rshal.SizeOf(default(MeshVert));
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
        private WorldObject CreateObject(string name, MeshData mesh, Material material, Vec3f pos = default(Vec3f))
        {
            WorldObject ob = new WorldObject(pos);
            ob.Name=name;
            ob.Mesh = mesh;
            ob.Material = material;
            Gu.World.Objects.Add(ob);
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
            Title = $"(Vsync: {VSync}) FPS: {1f / e.Time:0} Size: {Width}x{Height}";
            Gu.Window.Update();

            _boxMeshThing.Rotation = Quaternion.FromAxisAngle(new Vec3f(0, 1, 0), (float)rot);
            rot += Math.PI * 2.0f * Gu.Window.Delta * 0.0125f;

            Gu.World.Update();

            UpdateInput();
        }
        private void UpdateInput()
        {
            float coordMul = (Gu.CoordinateSystem == CoordinateSystem.Lhs ? -1 : 1);
            var keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Key.Escape))
            {
                Exit();
            }
            if (Gu.Window.PCKeyboard.AnyKeysPressedOrHeld(new List<Key>() { Key.Up, Key.W }))
            {
                _camera.Position += _camera.BasisZ * 0.1f;
            }
            if (Gu.Window.PCKeyboard.AnyKeysPressedOrHeld(new List<Key>() { Key.Down, Key.S }))
            {
                _camera.Position -= _camera.BasisZ * 0.1f;
            }
            if (Gu.Window.PCKeyboard.AnyKeysPressedOrHeld(new List<Key>() { Key.Right, Key.D }))
            {
                _camera.Position -= _camera.BasisX * 0.1f * coordMul;
            }
            if (Gu.Window.PCKeyboard.AnyKeysPressedOrHeld(new List<Key>() { Key.Left, Key.A }))
            {
                _camera.Position += _camera.BasisX * 0.1f * coordMul;
            }
            if (Gu.Window.PCKeyboard.KeyPress(Key.Number1))
            {
                _boxMeshThing.Material.Shader.lightingModel = ((_boxMeshThing.Material.Shader.lightingModel + 1) % 4);
            }
            if (Gu.Window.PCKeyboard.KeyPressOrDown(Key.Number2))
            {
                _boxMeshThing.Material.Shader.GGX_X = (_boxMeshThing.Material.Shader.GGX_X + 0.01f) % 3.0f;
            }
            if (Gu.Window.PCKeyboard.KeyPressOrDown(Key.Number3))
            {
                _boxMeshThing.Material.Shader.GGX_Y = (_boxMeshThing.Material.Shader.GGX_Y + 0.01f) % 3.0f;
            }
            if (Gu.Window.PCKeyboard.KeyPress(Key.Number4))
            {
                meshIdx = (meshIdx + 1) % 3;
                if (meshIdx == 0)
                {
                    _boxMeshThing.Mesh = MeshData.GenBox(1, 1, 1);
                }
                if (meshIdx == 1)
                {
                    _boxMeshThing.Mesh = MeshData.GenSphere(32, 32, 1, true);
                }
                if (meshIdx == 2)
                {
                    _boxMeshThing.Mesh = MeshData.GenSphere(32, 32, 1, false);
                }
            }

            var mouseState = Mouse.GetState();
            if (lastSet == false)
            {
                last.X = (float)mouseState.X;
                last.Y = (float)mouseState.Y;
                lastSet = true;
            }
            if (mouseState.IsButtonDown(MouseButton.Left))
            {
                float mx = (float)mouseState.X - last.X;
                float my = (float)mouseState.Y - last.Y;

                float rot_speed = 0.001f;

                rotX += Math.PI * mx * rot_speed * coordMul;
                if (rotX > Math.PI * 2.0f)
                {
                    rotX = (float)(rotX % (Math.PI * 2.0f));
                }
                if (rotX < Math.PI * 2.0f)
                {
                    rotX = (float)(rotX % (Math.PI * 2.0f));
                }
                rotY += Math.PI * my * -rot_speed * coordMul;
                if (rotY > Math.PI * 2.0f)
                {
                    rotY = (float)(rotY % (Math.PI * 2.0f));
                }
                if (rotY < Math.PI * 2.0f)
                {
                    rotY = (float)(rotY % (Math.PI * 2.0f));
                }
                //_camera.Rotation *= Quat.FromAxisAngle(new Vec3f(0, 1, 0), rotX) *
                //Quat.FromAxisAngle(_camera.BasisX, rotY);
                _camera.Rotation = Quat.FromAxisAngle(new Vec3f(0, 1, 0), (float)rotX) *
                Quat.FromAxisAngle(_camera.BasisX, (float)rotY);
                Console.WriteLine("x=" + _camera.BasisX.X + " " + _camera.BasisX.Y + " " + _camera.BasisX.Z);
                Console.WriteLine("y=" + _camera.BasisY.X + " " + _camera.BasisY.Y + " " + _camera.BasisY.Z);
                Console.WriteLine("ry=" + rotY);
                Console.WriteLine("rx=" + rotX);
            }
            last.X = (float)mouseState.X;
            last.Y = (float)mouseState.Y;
        }
        private double rotX = 0;
        private double rotY = 0;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            Renderer.BeginRender(this, new Vec4f(1, 1, 1, 1));
            Gu.World.Render(Gu.Window.Delta, _camera);
            Renderer.EndRender();
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