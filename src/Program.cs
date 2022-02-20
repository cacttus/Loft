using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;


namespace PirateCraft
{
   public class MainWindow : OpenTK.GameWindow
   {
      Camera3D _camera = null;
      WorldObject _boxMeshThing = null;

      vec2 last = new vec2(0, 0);
      bool lastSet = false;
      int meshIdx = 0;
      const float scale = 0.5f;

      WorldObject _sky;

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
      WorldObject Sphere_Rotate_Quat_Test = null;
      WorldObject Sphere_Rotate_Quat_Test2 = null;
      WorldObject Sphere_Rotate_Quat_Test3 = null;
      protected override void OnLoad(EventArgs e)
      {
         try
         {
            Gu.Init(this);

            //Cameras
            _camera = Gu.World.CreateCamera("Camera-001", Width, Height, new vec3(0, 20, -10));

            Gu.World.Initialize(_camera);

            //Textures
            Texture2D noise = Noise3D.TestNoise();
            Texture2D peron = new Texture2D(new FileLoc("main char.png", FileStorage.Embedded), true, TexFilter.Bilinear);
            Texture2D grass = new Texture2D(new FileLoc("grass_base.png", FileStorage.Embedded), true, TexFilter.Bilinear);
            Texture2D mclovin = new Texture2D(new FileLoc("mclovin.jpg", FileStorage.Embedded), true, TexFilter.Nearest);
            Texture2D usopp = new Texture2D(new FileLoc("usopp.jpg", FileStorage.Embedded), true, TexFilter.Bilinear);
            Texture2D hogback = new Texture2D(new FileLoc("hogback.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
            Texture2D sky1 = new Texture2D(new FileLoc("hdri_sky2.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);

            //Objects
            //Integrity test of GPU memory.
            for (int i = 0; i < 100; ++i)
            {
               Gu.World.CreateObject("BoxMesh", MeshData.GenBox(1, 1, 1), new Material(Shader.DefaultDiffuse(), noise));
            }
            for (int i = 1; i < 100; ++i)
            {
               Gu.World.DestroyObject("BoxMesh-" + i.ToString());
            }
            Gu.World.CreateObject("TextureFront", MeshData.GenTextureFront(_camera, 0, 0, Width, Height), new Material(Shader.DefaultDiffuse(), peron));
            Gu.World.CreateObject("Plane.", MeshData.GenPlane(10, 10), new Material(Shader.DefaultDiffuse(), grass));
            _boxMeshThing = Gu.World.FindObject("BoxMesh");
            _boxMeshThing.Position = new vec3(0, _boxMeshThing.BoundBox.Height() * 0.5f, 0);

            Sphere_Rotate_Quat_Test = Gu.World.CreateObject("Sphere_Rotate_Quat_Test", MeshData.GenSphere(), new Material(Shader.DefaultDiffuse(), mclovin));
            Sphere_Rotate_Quat_Test2 = Gu.World.CreateObject("Sphere_Rotate_Quat_Test2", MeshData.GenSphere(), new Material(Shader.DefaultDiffuse(), usopp));
            Sphere_Rotate_Quat_Test3 = Gu.World.CreateObject("Sphere_Rotate_Quat_Test3", MeshData.GenSphere(), new Material(Shader.DefaultDiffuse(), hogback));

            //TODO: sky shader. 
            Material sky_mat = new Material(Shader.DefaultDiffuse(), sky1);
            sky_mat.GpuRenderState.DepthTest = false;//Disable depth test.
            _sky = Gu.World.CreateObject("sky", MeshData.GenSphere(128,128,400,true,true), sky_mat);
            _sky.Constraints.Add(new FollowConstraint(_camera, FollowConstraint.FollowMode.Snap));

            //Animation test
            var cmp = new AnimationComponent();
            vec3 raxis = new vec3(0, 1, 0);
            cmp.KeyFrames.Add(new Keyframe(0, mat3.getRotation(raxis, 0).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 0, 0), KeyframeInterpolation.Ease));
            cmp.KeyFrames.Add(new Keyframe(1, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 * 0.5 - 0.001)).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 1, 0), KeyframeInterpolation.Ease));
            cmp.KeyFrames.Add(new Keyframe(2, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 - 0.002)).toQuat(), KeyframeInterpolation.Slerp, new vec3(1, 1, 0), KeyframeInterpolation.Ease)); ;
            cmp.KeyFrames.Add(new Keyframe(3, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 + MathUtils.M_PI_2 * 0.5 - 0.004)).toQuat(), KeyframeInterpolation.Slerp, new vec3(1, 0, 0), KeyframeInterpolation.Ease)); ;
            cmp.KeyFrames.Add(new Keyframe(4, mat3.getRotation(raxis, (float)(MathUtils.M_PI * 2 - 0.006)).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 0, 0), KeyframeInterpolation.Ease)); ;
            cmp.Start();
            _boxMeshThing.Components.Add(cmp);

            var db = DebugDraw.CreateBoxLines(new vec3(.5f, .5f, .5f), new vec3(1, 1, 1), new vec4(.2f, .2f, .2f, 1));
            db.Color = new vec4(1, 0, 0, 1);
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
         float chary = this._camera.Position.y;
         Title = $"(CharY = {chary}) (Vsync: {VSync}) FPS: {1f / e.Time:0} AllGlobs: {Gu.World.NumGlobs} Render: {Gu.World.NumRenderGlobs} Visible: {Gu.World.NumVisibleRenderGlobs}";

         Gu.CurrentWindowContext.Update();

         //_boxMeshThing.Rotation = quaternion.FromAxisAngle(new vec3(0, 1, 0), (float)rot);
         //rot += Math.PI * 2.0f * Gu.CurrentWindowContext.Delta * 0.0125f;

         Gu.World.Update(Gu.CurrentWindowContext.Delta, _camera);

         //checks out
         Sphere_Rotate_Quat_Test.Position = new vec3(0, 3, 0);
         Sphere_Rotate_Quat_Test.Rotation = quat.fromAxisAngle(new vec3(1, 1, 1).normalized(), (float)Gu.RotationPerSecond(10));

         Sphere_Rotate_Quat_Test2.Position = new vec3(-3, 3, 0);
         Sphere_Rotate_Quat_Test2.Rotation = mat4.getRotation(new vec3(-1, -1, -1).normalized(), (float)Gu.RotationPerSecond(10)).toQuat();

         Sphere_Rotate_Quat_Test3.Position = new vec3(3, 3, 0);
         Sphere_Rotate_Quat_Test3.Rotation = quat.fromAxisAngle(new vec3(1, 1, 1).normalized(), (float)Gu.RotationPerSecond(3));

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
         float speed = 20.7f;
         var kb = Gu.CurrentWindowContext.PCKeyboard;
         if (kb.KeyPressOrDown(Key.Number6))
         {
            _boxMeshThing.Material.Shader.nmap += 0.01f;
            _boxMeshThing.Material.Shader.nmap = _boxMeshThing.Material.Shader.nmap % 1;
         }
         float speedMul = 1;
         if(kb.KeyPressOrDown(Key.ControlLeft)|| kb.KeyPressOrDown(Key.ControlRight ))
         {
            speedMul = 3;
         }
         if (kb.AnyKeysPressedOrHeld(new List<Key>() { Key.Q }))
         {
            _camera.Position += _camera.BasisY * speed * (float)Gu.CurrentWindowContext.Delta * speedMul;
         }
         if (kb.AnyKeysPressedOrHeld(new List<Key>() { Key.E }))
         {
            _camera.Position -= _camera.BasisY * speed * (float)Gu.CurrentWindowContext.Delta * speedMul;
         }
         if (kb.AnyKeysPressedOrHeld(new List<Key>() { Key.Up, Key.W }))
         {
            _camera.Position += _camera.BasisZ * speed * (float)Gu.CurrentWindowContext.Delta * speedMul;
         }
         if (kb.AnyKeysPressedOrHeld(new List<Key>() { Key.Down, Key.S }))
         {
            _camera.Position -= _camera.BasisZ * speed * (float)Gu.CurrentWindowContext.Delta * speedMul;
         }
         if (kb.AnyKeysPressedOrHeld(new List<Key>() { Key.Right, Key.D }))
         {
            _camera.Position += _camera.BasisX * speed * coordMul * (float)Gu.CurrentWindowContext.Delta * speedMul;
         }
         if (kb.AnyKeysPressedOrHeld(new List<Key>() { Key.Left, Key.A }))
         {
            _camera.Position -= _camera.BasisX * speed * coordMul * (float)Gu.CurrentWindowContext.Delta * speedMul;
         }
         if (kb.KeyPress(Key.Number1))
         {
            _boxMeshThing.Material.Shader.lightingModel = ((_boxMeshThing.Material.Shader.lightingModel + 1) % 3);
         }
         if (kb.KeyPressOrDown(Key.Number2))
         {
            _boxMeshThing.Material.Shader.GGX_X = (_boxMeshThing.Material.Shader.GGX_X + 0.01f) % 3.0f;
         }
         if (kb.KeyPressOrDown(Key.Number3))
         {
            _boxMeshThing.Material.Shader.GGX_Y = (_boxMeshThing.Material.Shader.GGX_Y + 0.01f) % 3.0f;
         }
         if (kb.KeyPress(Key.Number4))
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
            last.x = (float)mouseState.X;
            last.y = (float)mouseState.Y;
            lastSet = true;
         }
         if (mouseState.IsButtonDown(MouseButton.Left))
         {
            float mx = (float)mouseState.X - last.x;
            float my = (float)mouseState.Y - last.y;

            float rot_speed = 0.001f;

            rotX += Math.PI * mx * rot_speed * coordMul;
            if (rotX > Math.PI * 2.0f)
            {
               rotX = (float)(rotX % (Math.PI * 2.0f));
            }
            if (rotX < 0)
            {
               rotX = (float)(rotX % (Math.PI * 2.0f));
            }
            rotY += Math.PI * my * rot_speed * coordMul;
            if (rotY > Math.PI * 2.0f)
            {
               rotY = (float)(rotY % (Math.PI * 2.0f));
            }
            if (rotY < 0)
            {
               rotY = (float)(rotY % (Math.PI * 2.0f));
            }
            quat qx = quat.fromAxisAngle(new vec3(0, 1, 0), (float)rotX).normalized();
            quat qy = quat.fromAxisAngle(new vec3(1, 0, 0), (float)rotY).normalized();

            _camera.Rotation = qx * qy;
         }
         last.x = (float)mouseState.X;
         last.y = (float)mouseState.Y;
      }
      private double rotX = 0;
      private double rotY = 0;
      protected override void OnRenderFrame(FrameEventArgs e)
      {
         Renderer.BeginRender(this, new vec4(1, 1, 1, 1));
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