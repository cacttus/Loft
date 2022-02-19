using System;
using System.Collections.Generic;
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
            Gu.Init(this);

            //Cameras
            _camera = Gu.World.CreateCamera("Camera-001", Width, Height, new Vec3f(0, 10, -10));

            Gu.World.Initialize();
            Gu.World.player = _camera;

            //Textures
            Texture noise = Noise3D.TestNoise();
            Texture peron = new Texture(new FileLoc("main char.png", FileStorage.Embedded));
            Texture grass = new Texture(new FileLoc("grass_base.png", FileStorage.Embedded));

            //Objects
            //Integrity test of GPU memory.
            for (int i = 0; i < 100; ++i)
            {
               Gu.World.CreateObject("BoxMesh", MeshData.GenBox(1, 1, 1), new Material(noise, Shader.DefaultDiffuse()));
            }
            for (int i = 1; i < 100; ++i)
            {
               Gu.World.DestroyObject("BoxMesh-" + i.ToString());
            }
            Gu.World.CreateObject("TextureFront",MeshData.GenTextureFront(_camera, 0, 0, Width, Height), new Material(peron, Shader.DefaultDiffuse()));
            Gu.World.CreateObject("Plane.",MeshData.GenPlane(10, 10), new Material(grass, Shader.DefaultDiffuse()));
            _boxMeshThing = Gu.World.FindObject("BoxMesh");
            _boxMeshThing.Position = new Vec3f(0, _boxMeshThing.BoundBox.Height() * 0.5f, 0);

            //Animation test
            var cmp = new AnimationComponent();
            cmp.KeyFrames.Add(new Keyframe(0, Mat3f.CreateRotationY(0).ExtractRotation(), KeyframeInterpolation.Slerp, new Vec3f(0, 0, 0), KeyframeInterpolation.Ease));
            cmp.KeyFrames.Add(new Keyframe(1, Mat3f.CreateRotationY((float)(MathUtils.M_PI_2 * 0.5 - 0.001)).ExtractRotation(), KeyframeInterpolation.Slerp, new Vec3f(0, 1, 0), KeyframeInterpolation.Ease));
            cmp.KeyFrames.Add(new Keyframe(2, Mat3f.CreateRotationY((float)(MathUtils.M_PI_2 - 0.002)).ExtractRotation(), KeyframeInterpolation.Slerp, new Vec3f(1, 1, 0), KeyframeInterpolation.Ease)); ;
            cmp.KeyFrames.Add(new Keyframe(3, Mat3f.CreateRotationY((float)(MathUtils.M_PI_2 + MathUtils.M_PI_2 * 0.5 - 0.004)).ExtractRotation(), KeyframeInterpolation.Slerp, new Vec3f(1, 0, 0), KeyframeInterpolation.Ease)); ;
            cmp.KeyFrames.Add(new Keyframe(4, Mat3f.CreateRotationY((float)(MathUtils.M_PI * 2 - 0.006)).ExtractRotation(), KeyframeInterpolation.Slerp, new Vec3f(0, 0, 0), KeyframeInterpolation.Ease)); ;
            cmp.Start();
            _boxMeshThing.Components.Add(cmp);

            var db = DebugDraw.CreateBoxLines(new Vec3f(.5f, .5f, .5f), new Vec3f(1, 1, 1), new Vec4f(.2f, .2f, .2f, 1));
            db.Color = new Vec4f(1, 0, 0, 1);
            _boxMeshThing.AddChild(db);

            CursorVisible = true;
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
      protected override void OnUpdateFrame(FrameEventArgs e)
      {
         float chary = this._camera.Position.Y;
         Title = $"(CharY = {chary}) (Vsync: {VSync}) FPS: {1f / e.Time:0} Globs: {Gu.World.NumGlobs} Render: {Gu.World.NumRenderGlobs}";
         
         Gu.CurrentWindowContext.Update();

         //_boxMeshThing.Rotation = Quaternion.FromAxisAngle(new Vec3f(0, 1, 0), (float)rot);
         //rot += Math.PI * 2.0f * Gu.CurrentWindowContext.Delta * 0.0125f;

         Gu.World.Update(Gu.CurrentWindowContext.Delta);

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
         if (Gu.CurrentWindowContext.PCKeyboard.KeyPress(Key.Number1))
         {
            // _boxMeshThing.Mesh.BeginEdit(0, 1);
            // MeshVert v= _boxMeshThing.Mesh.EditVert(0);
            // _boxMeshThing.Mesh.EndEdit();
         }
         float speed = 10.7f;
         if (Gu.CurrentWindowContext.PCKeyboard.AnyKeysPressedOrHeld(new List<Key>() { Key.Up, Key.W }))
         {
            _camera.Position += _camera.BasisZ * speed * (float)Gu.CurrentWindowContext.Delta;
         }
         if (Gu.CurrentWindowContext.PCKeyboard.AnyKeysPressedOrHeld(new List<Key>() { Key.Down, Key.S }))
         {
            _camera.Position -= _camera.BasisZ * speed * (float)Gu.CurrentWindowContext.Delta;
         }
         if (Gu.CurrentWindowContext.PCKeyboard.AnyKeysPressedOrHeld(new List<Key>() { Key.Right, Key.D }))
         {
            _camera.Position -= _camera.BasisX * speed * coordMul * (float)Gu.CurrentWindowContext.Delta;
         }
         if (Gu.CurrentWindowContext.PCKeyboard.AnyKeysPressedOrHeld(new List<Key>() { Key.Left, Key.A }))
         {
            _camera.Position += _camera.BasisX * speed * coordMul * (float)Gu.CurrentWindowContext.Delta;
         }
         if (Gu.CurrentWindowContext.PCKeyboard.KeyPress(Key.Number1))
         {
            _boxMeshThing.Material.Shader.lightingModel = ((_boxMeshThing.Material.Shader.lightingModel + 1) % 5);
         }
         if (Gu.CurrentWindowContext.PCKeyboard.KeyPressOrDown(Key.Number2))
         {
            _boxMeshThing.Material.Shader.GGX_X = (_boxMeshThing.Material.Shader.GGX_X + 0.01f) % 3.0f;
         }
         if (Gu.CurrentWindowContext.PCKeyboard.KeyPressOrDown(Key.Number3))
         {
            _boxMeshThing.Material.Shader.GGX_Y = (_boxMeshThing.Material.Shader.GGX_Y + 0.01f) % 3.0f;
         }
         if (Gu.CurrentWindowContext.PCKeyboard.KeyPress(Key.Number4))
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
         Gu.World.Render(Gu.CurrentWindowContext.Delta, _camera);

         Renderer.EndRender();
         //This is big.
         //GC.Collect();
         Gpu.FreeGPUMemory(Gu.CurrentWindowContext);
      }
   }
   class MainClass
   {
      public static void Main(string[] args)
      {
         var m = new MainWindow();
         m.VSync = OpenTK.VSyncMode.Off;
         m.Run();
      }
   }
}