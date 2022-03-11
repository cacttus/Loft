﻿using System;
using System.Collections.Generic;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
namespace PirateCraft
{
  public enum InputState
  {
    World, //User is moving inw orld.
    Inventory //User has inventory window open.
  }
  public class FirstPersonMouseRotator
  {
    private double rotX = 0;
    private double rotY = 0;

    private double warp_boundary = 0.1f;//the distance user can move mouse in window  until we warp. Warping every frame absolutely sucks.

    public void DoRotate(Camera3D cam, MainWindow gw)
    {
      if (gw.InputState == InputState.World)
      {
        //Rotate Camera
        float width = cam.Viewport_Width;
        float height = cam.Viewport_Height;
        float rotations_per_width = 3.5f; // How many times we rotate 360 degrees  if the user moves the cursor across the whole window width
        float half_rotations_per_height = 2f; // How many times we rotate 180 degrees  if the user moves the cursor across the whole window heihgt.

        rotX += Math.PI * 2 * (Gu.Mouse.Delta.x / width) * rotations_per_width * Gu.CoordinateSystemMultiplier;
        if (rotX >= Math.PI * 2.0f)
        {
          rotX = (float)(rotX % (Math.PI * 2.0f));
        }
        if (rotX <= 0)
        {
          rotX = (float)(rotX % (Math.PI * 2.0f));
        }

        rotY += Math.PI * 2 * (Gu.Mouse.Delta.y / height) * half_rotations_per_height * Gu.CoordinateSystemMultiplier;
        if (rotY >= Math.PI / 2)
        {
          rotY = Math.PI / 2 - 0.001f;
        }
        if (rotY <= -Math.PI / 2)
        {
          rotY = -Math.PI / 2 + 0.001f;
        }

        quat qx = quat.fromAxisAngle(new vec3(0, 1, 0), (float)rotX).normalized();
        quat qy = quat.fromAxisAngle(new vec3(1, 0, 0), (float)rotY).normalized();

        cam.Rotation = qx * qy;

        if (gw.InputState == InputState.World)
        {
          if ((Gu.Mouse.Pos.x <= width * warp_boundary) || (Gu.Mouse.Pos.x >= width - width * warp_boundary))
          {
            Gu.Mouse.WarpMouse(true, false, true);
          }

          if ((Gu.Mouse.Pos.y <= height * warp_boundary) || (Gu.Mouse.Pos.y >= height - height * warp_boundary))
          {
            Gu.Mouse.WarpMouse(false, true, true);
          }
        }

      }
    }
  }
  public class MainWindow : GameWindow
  {
    bool DELETE_WORLD_START_FRESH = true;
    Camera3D _camera = null;
    WorldObject _boxMeshThing = null;
    int meshIdx = 0;
    const float scale = 0.5f;
    public InputState InputState = InputState.World;
    FirstPersonMouseRotator _FPSRotator = new FirstPersonMouseRotator();
    private NativeWindowSettings _ns = NativeWindowSettings.Default;
    WorldObject Sphere_Rotate_Quat_Test;
    WorldObject Sphere_Rotate_Quat_Test2;
    WorldObject Sphere_Rotate_Quat_Test3;

    WorldObject pick = null;
    WorldObject sword = null;
    WorldObject left_hand = null;
    WorldObject right_hand = null;
    bool _bInitialized = true;
    const float BaseSpeed = World.BlockSizeX * 3.0f;
    const float MaxAirFriction = BaseSpeed*0.95f; 
    /*
     * base((int)(1920 * scale), (int)(1080 * scale),
    GraphicsMode.Default, "Test", GameWindowFlags.Default,
    OpenTK.DisplayDevice.Default, 4, 0, GraphicsContextFlags.ForwardCompatible)
     * */
    public MainWindow() : base(
       new GameWindowSettings()
       {
         IsMultiThreaded = false,
         RenderFrequency = 0,
         UpdateFrequency = 0
       }
    , new NativeWindowSettings()
    {
      Profile = ContextProfile.Compatability,
      Flags = ContextFlags.Default,
      AutoLoadBindings = true,
      APIVersion = new Version(4, 6),
      Title = "PirateCraft",
      StartFocused = true,
      StartVisible = true,
      WindowState = WindowState.Normal,
      WindowBorder = WindowBorder.Resizable,
      Location = new OpenTK.Mathematics.Vector2i(200, 200),
      Size = new OpenTK.Mathematics.Vector2i((int)(1920 * scale), (int)(1080 * scale)),
      NumberOfSamples = 0, //TODO:
      StencilBits = 8,
      DepthBits = 24,
      RedBits = 8,
      GreenBits = 8,
      BlueBits = 8,
      AlphaBits = 8
    }

    )
    {
      Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
    }
    protected override void OnResize(ResizeEventArgs e)
    {
      if (_bInitialized)
      {
        _camera.Viewport_Width = e.Width;
        _camera.Viewport_Height = e.Height;
      }
    }
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
      base.OnMouseMove(e);
    }
    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
      base.OnFocusedChanged(e);
    }
    protected override void OnLoad()
    {
      //The synchronization context isn't present here. Luckily OpenTK gives us this neat callback that gets called after it is initialized.
      //   RenderThreadStarted += () =>
      {
        InitializeEverything();
      };
    }
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
      if (_bInitialized)
      {
        UpdateFrame(e);
      }
    }
    protected override void OnRenderFrame(FrameEventArgs e)
    {
      if (_bInitialized)
      {
        RenderFrame();
      }
    }

    private void InitHandObjects()
    {
      vec3 right_pos = new vec3(3.0f, -2.1f, 4.5f);
      vec3 left_pos = new vec3(-3.0f, -2.1f, 4.5f);

      List<WorldObject> objs;
      objs = Gu.Resources.LoadObjects(new FileLoc("pick.glb", FileStorage.Embedded));
      if (objs?.Count > 0)
      {
        pick = objs[0];
        pick.Position = left_pos;
        pick.Scale *= new vec3(0.7f, 0.7f, 0.7f);
        pick.Rotation *= quat.fromAxisAngle(new vec3(0, 1, 0), MathUtils.M_PI * 0.5f);
        pick.Rotation *= quat.fromAxisAngle(new vec3(1, 0, 1), MathUtils.M_PI_2 * 0.125f);

        var cmp = new AnimationComponent();
        vec3 raxis = new vec3(-1, 1, 0);
        float t_seconds = 0.5f; // swipe time in seconds
        float kf_count = 3;
        cmp.KeyFrames.Add(new Keyframe(t_seconds / kf_count * 0, mat3.getRotation(raxis, 0).toQuat()));
        cmp.KeyFrames.Add(new Keyframe(t_seconds / kf_count * 1, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 * 0.5 - 0.001)).toQuat()));
        cmp.KeyFrames.Add(new Keyframe(t_seconds / kf_count * 2, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 - 0.002)).toQuat())); ;
        pick.Components.Add(cmp);

        _camera.AddChild(pick);
      }
      objs = Gu.Resources.LoadObjects(new FileLoc("sword.glb", FileStorage.Embedded));
      if (objs?.Count > 0)
      {
        sword = objs[0];
        sword.Position = right_pos;
        sword.Scale *= new vec3(0.7f, 0.7f, 0.7f);
        sword.Rotation *= quat.fromAxisAngle(new vec3(1, 0, 1).normalized(), MathUtils.M_PI_2 * 0.125f);

        _camera.AddChild(sword);
      }
    }
    private void Crosshair()
    {
      vec4 ch_c = new vec4(0.31f, 0, 0, .1f);
      float size = 0.08f;
      v_v3c4[] verts = new v_v3c4[] {
           new v_v3c4() { _v = new vec3(-size, 0, 0), _c =  ch_c },
           new v_v3c4() { _v = new vec3(size, 0, 0), _c =   ch_c },
           new v_v3c4() { _v = new vec3(0, -size, 0), _c =  ch_c },
           new v_v3c4() { _v = new vec3(0, size, 0), _c =   ch_c  }
         };
      WorldObject Crosshair = new WorldObject("crosshair");
      Crosshair.Mesh = new MeshData("crosshair_mesh", PrimitiveType.Lines, v_v3c4.VertexFormat, Gpu.GetGpuDataPtr(verts.ToArray()));
      Crosshair.Mesh.DrawOrder = DrawOrder.Last;
      Crosshair.Position = new vec3(0, 0, 3);
      Material crosshair_mat = new Material(Shader.DefaultFlatColorShader());
      crosshair_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      crosshair_mat.GpuRenderState.Blend = true;
      Crosshair.Material = crosshair_mat;
      _camera.AddChild(Crosshair);
    }
    private void InitializeEverything()
    {
      try
      {
        Gu.Init_RenderThread_Only(this);

        //Cameras
        _camera = Gu.World.CreateCamera("Camera-001", this.Size.X, this.Size.Y,
           //Put player exactly in center.
           new vec3(
           World.BlockSizeX * World.GlobBlocksX * .5f,
           World.BlockSizeY * World.GlobBlocksY * .5f,
           World.BlockSizeZ * World.GlobBlocksZ * .5f
           ));

        _camera.Far = 2000.0f;
        _camera.HasPhysics = true;
        _camera.AirFriction = MaxAirFriction; //Movement Damping
        _camera.HasGravity = false;
        //string embedded_fil1e = "route110.ogg";
        //   Gu.Context. Audio.Play(new FileLoc(embedded_fil1e, FileStorage.Embedded));

        ////Test sound
        //while (true)
        //{
        //  int r = Random.NextInt(0, 4);
        //  string embedded_file = "rock";
        //  if (r == 0) { embedded_file += "_1.ogg"; }
        //  if (r == 1) { embedded_file += "_2.ogg"; }
        //  if (r == 2) { embedded_file += "_3.ogg"; }
        //  if (r == 3) { embedded_file += "_4.ogg"; }
        //  if (r == 4) { embedded_file += "_5.ogg"; }

        //  var x = Gu.Context. Audio.Play(new FileLoc(embedded_file, FileStorage.Embedded));
        //  x.Loop = false;
        //  // Gu.Context.Audio.Update();
        //  System.Threading.Thread.Sleep(5000);
        //  int n = 0;
        //  n++;
        //}

        Gu.World.Initialize(_camera, "Boogerton", DELETE_WORLD_START_FRESH, 2);

        Gu.Context.DebugDraw.DrawBoundBoxes = false;

        CreateDebugObjects();
        InitHandObjects();
        Crosshair();

        CursorVisible = true;
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Failed to initialize engine. Errors occured: " + ex.ToString() + ex.InnerException?.ToString());
        System.Environment.Exit(0);
      }
    }
    private void PlayDropSound()
    {
      Gu.Context.Audio.Play(new FileLoc("wood_1.ogg", FileStorage.Embedded));
    }

    private void PlayPickSound(ushort bc)
    {
      string embedded_file = "";
      int num = 0;
      if (bc == BlockItemCode.Brick ||
          bc == BlockItemCode.Brick2 ||
          bc == BlockItemCode.Feldspar ||
          bc == BlockItemCode.Gravel
               )
      {
        embedded_file = "rock";
        num = 5;
      }
      else if (bc == BlockItemCode.Dirt ||
               bc == BlockItemCode.Grass ||
               bc == BlockItemCode.Cedar ||
               bc == BlockItemCode.Cedar_Needles
               )
      {
        embedded_file = "wood";
        num = 4;
      }
      else if (bc == BlockItemCode.Sand
               )
      {
        embedded_file = "glass";
        num = 5;
      }
      else
      {
        embedded_file = "rock";
        num = 5;
      }
      int r = Random.NextInt(1, num);
      embedded_file += "_" + r.ToString() + ".ogg";

      var x = Gu.Context.Audio.Play(new FileLoc(embedded_file, FileStorage.Embedded));
    }
    private void CreateDebugObjects()
    {
      //Textures
      //   Texture2D noise = Noise3D.TestNoise();
      Texture2D peron = Gu.Resources.LoadTexture(new FileLoc("main char.png", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D grass = Gu.Resources.LoadTexture(new FileLoc("grass_base.png", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D mclovin = Gu.Resources.LoadTexture(new FileLoc("mclovin.jpg", FileStorage.Embedded), true, TexFilter.Nearest);
      Texture2D usopp = Gu.Resources.LoadTexture(new FileLoc("usopp.jpg", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D hogback = Gu.Resources.LoadTexture(new FileLoc("hogback.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture2D sky1 = Gu.Resources.LoadTexture(new FileLoc("hdri_sky2.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);

      //Objects
      //Integrity test of GPU memory.
      //for (int i = 0; i < 100; ++i)
      //{
      //  Gu.World.CreateAndAddObject("BoxMesh", MeshData.GenBox(1, 1, 1), new Material(Shader.DefaultDiffuse(), noise));
      //}
      //for (int i = 1; i < 100; ++i)
      //{
      //  Gu.World.RemoveObject("BoxMesh-" + i.ToString());
      //}
      Gu.World.CreateAndAddObject("TextureFront", MeshData.GenTextureFront(_camera, 0, 0, this.Size.X, this.Size.Y), new Material(Shader.DefaultDiffuse(), peron));
      Gu.World.CreateAndAddObject("Plane.", MeshData.GenPlane(10, 10), new Material(Shader.DefaultDiffuse(), grass));
      //  _boxMeshThing = Gu.World.FindObject("BoxMesh");
      //_boxMeshThing.Position = new vec3(0, _boxMeshThing.BoundBoxMeshBind.Height() * 0.5f, 0);

      Sphere_Rotate_Quat_Test = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test", MeshData.GenSphere(1), new Material(Shader.DefaultDiffuse(), mclovin));
      Sphere_Rotate_Quat_Test2 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test2", MeshData.GenEllipsoid(new vec3(1.9f, 1, 1.5f)), new Material(Shader.DefaultDiffuse(), usopp));
      Sphere_Rotate_Quat_Test3 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test3", MeshData.GenEllipsoid(new vec3(1, 4, 4)), new Material(Shader.DefaultDiffuse(), hogback));

      //TODO: sky shader. 
      // Material sky_mat = new Material(Shader.DefaultDiffuse(), sky1);
      // sky_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      // Gu.World.Sky = Gu.World.CreateObject("sky", MeshData.GenSphere(128, 128, 400, true, true), sky_mat);
      // Gu.World.Sky.Mesh.DrawOrder = DrawOrder.First;
      // Gu.World.Sky.Constraints.Add(new FollowConstraint(_camera, FollowConstraint.FollowMode.Snap));

      //Animation test
      var cmp = new AnimationComponent();
      vec3 raxis = new vec3(0, 1, 0);
      cmp.KeyFrames.Add(new Keyframe(0, mat3.getRotation(raxis, 0).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 0, 0), KeyframeInterpolation.Ease));
      cmp.KeyFrames.Add(new Keyframe(1, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 * 0.5 - 0.001)).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 1, 0), KeyframeInterpolation.Ease));
      cmp.KeyFrames.Add(new Keyframe(2, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 - 0.002)).toQuat(), KeyframeInterpolation.Slerp, new vec3(1, 1, 0), KeyframeInterpolation.Ease)); ;
      cmp.KeyFrames.Add(new Keyframe(3, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 + MathUtils.M_PI_2 * 0.5 - 0.004)).toQuat(), KeyframeInterpolation.Slerp, new vec3(1, 0, 0), KeyframeInterpolation.Ease)); ;
      cmp.KeyFrames.Add(new Keyframe(4, mat3.getRotation(raxis, (float)(MathUtils.M_PI * 2 - 0.006)).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 0, 0), KeyframeInterpolation.Ease)); ;
      cmp.Play();
      Sphere_Rotate_Quat_Test.Components.Add(cmp);

      //Some fun parenting stuff.
      //  Sphere_Rotate_Quat_Test.AddChild(Sphere_Rotate_Quat_Test2.AddChild(Sphere_Rotate_Quat_Test3.AddChild(_boxMeshThing)));
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
    private void UpdateFrame(FrameEventArgs e)
    {
      float chary = this._camera.Position.y;
      Title = $"(Cam = {_camera.Position.ToString()}) FPS: {(int)Gu.Context.Fps}  " +
         $"nyugs b: {Box3f.nugs} " +
         $"Visible Glob: {Gu.World.NumVisibleRenderGlobs} " +
         $"Gen Glob: {Gu.World.NumGenGlobs} " +
         $"Gen Drome: {Gu.World.NumGenDromes} " +
         $"DrawElements_Frame:{MeshData.dbg_numDrawElements_Frame} Arrays_Frame: {MeshData.dbg_numDrawArrays_Frame} " +
         $"OBs culled:{Gu.World.Dbg_N_OB_Culled} " +
         $"Mouse:{Gu.Mouse.Pos.x},{Gu.Mouse.Pos.y} " +
         $"Cap Hit:{CapsuleHit} "
         ;

      Gu.Context.DebugDraw.BeginFrame();

      Gu.Context.Update();

      //_boxMeshThing.Rotation = quaternion.FromAxisAngle(new vec3(0, 1, 0), (float)rot);
      //rot += Math.PI * 2.0f * Gu.CurrentWindowContext.Delta * 0.0125f;

      Gu.World.Update(Gu.Context.Delta, _camera);

      //checks out
      if (Sphere_Rotate_Quat_Test != null)
      {

        Sphere_Rotate_Quat_Test.Position = new vec3(0, 3, 0);
        Sphere_Rotate_Quat_Test.Rotation = quat.fromAxisAngle(new vec3(1, 1, 1).normalized(), (float)Gu.RotationPerSecond(10));
      }
      if (Sphere_Rotate_Quat_Test2 != null)
      {
        Sphere_Rotate_Quat_Test2.Position = new vec3(-3, 3, 0);
        Sphere_Rotate_Quat_Test2.Rotation = mat4.getRotation(new vec3(-1, -1, -1).normalized(), (float)Gu.RotationPerSecond(10)).toQuat();
      }
      if (Sphere_Rotate_Quat_Test3 != null)
      {
        Sphere_Rotate_Quat_Test3.Position = new vec3(3, 3, 0);
        Sphere_Rotate_Quat_Test3.Rotation = quat.fromAxisAngle(new vec3(1, 1, 1).normalized(), (float)Gu.RotationPerSecond(3));
      }

      UpdateInput();
    }
    private void UpdateInput()
    {
      if (!IsFocused)
      {
        return;
      }

      if (Gu.Keyboard.PressOrDown(Keys.Escape))
      {
        Close();
      }

      Movement();

      DebugKeyboard();

      EditBlocks();

      TestEllipsoid_Box();

    }
    bool CapsuleHit = false;
    private void Movement()
    {
      float speed = BaseSpeed;

      float speedMul = 1;
      if (Gu.Keyboard.PressOrDown(Keys.LeftControl) || Gu.Keyboard.PressOrDown(Keys.RightControl))
      {
        speedMul = 6;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Q }))
      {
        _camera.Velocity += _camera.BasisY * speed * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.E }))
      {
        _camera.Velocity -= _camera.BasisY * speed * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Up, Keys.W }))
      {
        _camera.Velocity += _camera.BasisZ * speed * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Down, Keys.S }))
      {
        _camera.Velocity -= _camera.BasisZ * speed * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Right, Keys.D }))
      {
        _camera.Velocity += _camera.BasisX * speed * Gu.CoordinateSystemMultiplier * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Left, Keys.A }))
      {
        _camera.Velocity -= _camera.BasisX * speed * Gu.CoordinateSystemMultiplier * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.Press(Keys.Space))
      {
        if (_camera.OnGround)
        {
          _camera.Velocity += new vec3(0, speed, 0);// _camera.BasisX * speed * Gu.CoordinateSystemMultiplier * (float)Gu.Context.Delta * speedMul;
        }
      }
      if (_camera.HasPhysics == false)
      {
        _camera.Position += _camera.Velocity;
        _camera.Velocity = new vec3(0);
      }
      if (Gu.Keyboard.Press(Keys.I))
      {
        if (InputState == InputState.World)
        {
          InputState = InputState.Inventory;
          CursorVisible = true;
        }
        else if (InputState == InputState.Inventory)
        {
          InputState = InputState.World;
          CursorVisible = false;
        }
      }
      if (InputState == InputState.World)
      {
        _FPSRotator.DoRotate(_camera, this);
      }
      else if (InputState == InputState.Inventory)
      {
        //do inventory
      }
      else
      {
        Gu.BRThrowNotImplementedException();
      }
    }
    int mode = 0;
    private void DebugKeyboard()
    {
      if (Gu.Keyboard.Press(Keys.F1))
      {
        Gu.Context.DebugDraw.DrawBoundBoxes = !Gu.Context.DebugDraw.DrawBoundBoxes;
      }
      if (Gu.Keyboard.Press(Keys.F2))
      {
        VSync = (VSync == VSyncMode.Off) ? VSyncMode.On : VSyncMode.Off;
      }
      if (Gu.Keyboard.Press(Keys.F3))
      {
        if (mode == 0)
        {

          GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
          mode = 1;
        }
        else if (mode == 1)
        {
          GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
          mode = 0;
        }

      }
      if (Gu.Keyboard.Press(Keys.F4))
      {
        Material.DefaultDiffuse().Shader.lightingModel = ((Material.DefaultDiffuse().Shader.lightingModel + 1) % 3);
      }
      if (Gu.Keyboard.Press(Keys.F5))
      {
        Box3f.nugs = (Box3f.nugs + 1) % Box3f.maxnugs;
        //Material.DefaultDiffuse().Shader.GGX_X = (Material.DefaultDiffuse().Shader.GGX_X + 0.01f) % 3.0f;
      }
      //if (Gu.Keyboard.PressOrDown(Keys.F6))
      //{
      //  Material.DefaultDiffuse().Shader.GGX_Y = (Material.DefaultDiffuse().Shader.GGX_Y + 0.01f) % 3.0f;
      //}
      if (Gu.Keyboard.Press(Keys.F7))
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
      if (Gu.Keyboard.PressOrDown(Keys.F8))
      {
        _camera.Collides = !_camera.Collides;
        _camera.HasGravity = !_camera.HasGravity;
        _camera.AirFriction = MaxAirFriction - _camera.AirFriction;// ; //Movement Damping

      }
      if (Gu.Keyboard.PressOrDown(Keys.F9))
      {
        Material.DefaultDiffuse().Shader.nmap += 0.01f;
        Material.DefaultDiffuse().Shader.nmap = Material.DefaultDiffuse().Shader.nmap % 1;
      }
      if (Gu.Keyboard.Press(Keys.F11))
      {
        if (this.WindowState == WindowState.Fullscreen)
        {
          WindowState = WindowState.Normal;
        }
        else
        {
          WindowState = WindowState.Fullscreen;
        }
      }

    }
    float PickaxeStrength = 1;//Number of seconds it takes to mine 1 unit of strength
    ivec3? editing_block = null;
    float Curblock_Mine_Time = -1;
    float PickSound_Time = 0.4f;
    float PickSound_Time_Max = 0.4f;
    float MaxEditDistance_Block = World.BlockSizeX * 300.0f;
    void UpdateMineBlock(PickedBlock? b)
    {
      Gu.Assert(b != null);
      if (editing_block == null || (editing_block.Value != b.BlockPosLocal))
      {
        if (Gu.World.BlockTiles.TryGetValue(b.Block, out var tile))
        {
          Curblock_Mine_Time = tile.MineTime_Pickaxe;
          PickSound_Time = PickSound_Time_Max;

          editing_block = b.BlockPosLocal;
        }
      }

      if (editing_block != null)
      {
        Curblock_Mine_Time -= (float)Gu.Context.Delta * PickaxeStrength;
        PickSound_Time -= (float)Gu.Context.Delta;

        if (Curblock_Mine_Time <= 0)
        {
          //Destroy block. We mined it!

          Gu.World.DestroyBlock(b.Block_Center, true, true);
          //b.Drome.SetBlock(b.BlockPosLocal, BlockItemCode.Air, false);
          StopMineBlock();
        }
        else
        {
          if (PickSound_Time <= 0)
          {
            PlayPickSound(b.Block);
            PickSound_Time = PickSound_Time_Max;
          }
        }
      }

    }
    void StopMineBlock()
    {
      editing_block = null;
      Curblock_Mine_Time = -1;
    }
    private vec2 Get_Interaction_Pos()
    {
      //Depending on game mode
      //Returns either world editing projection position, or, the mouse position if we are in inventory
      vec2 projec_pos = new vec2(0, 0);
      if (this.InputState == InputState.Inventory)
      {
        projec_pos = Gu.Mouse.Pos;
      }
      else if (this.InputState == InputState.World)
      {
        projec_pos = new vec2(_camera.Viewport_Width * 0.5f, _camera.Viewport_Height * 0.5f);
      }
      else
      {
        Gu.Log.Error("Invalid projection position for raycast blocks.");
      }
      return projec_pos;
    }
    private Box3f GetPicked_MineBlock_Box(PickedBlock b)
    {
      return World.GetBlockBox(b, 0.01f);
    }
    private Box3f GetPicked_PlaceBlock_Box(PickedBlock b)
    {
      vec3 dir_n = b.GetHitNormal_Block();
      var neighbor = b.HitPos + dir_n * new vec3(World.BlockSizeX * 0.5f, World.BlockSizeY * 0.5f, World.BlockSizeZ * 0.5f);
      return World.GetBlockBoxGlobalR3(neighbor, 0.01f);
    }
    private void EditBlocks()
    {
      //Project ray and hit box.
      //vec2 projec_pos = Gu.Mouse.Pos;
      vec2 projec_pos = Get_Interaction_Pos();

      Line3f proj_pt = _camera.Frustum.ScreenToWorld(projec_pos, TransformSpace.World, 0.001f, MaxEditDistance_Block);
      var pr = new PickRay3D(proj_pt);
      var b = Gu.World.RaycastBlock(pr);

      vec3? block_center = null;
      if (b.IsHit)
      {
        if (Gu.Keyboard.PressOrDown(Keys.LeftControl))
        {
          //get center of neighbor block
          var box = GetPicked_PlaceBlock_Box(b);
          block_center = box.center();
          Gu.Context.DebugDraw.Box(box, new vec4(.1014f, .155f, .0915f, 1));
        }
        else
        {
          var box = GetPicked_MineBlock_Box(b);
          block_center = box.center();
          Gu.Context.DebugDraw.Box(box, new vec4(.1014f, .155f, .0915f, 1));
        }
        //  Gu.Context.DebugDraw.Point(b.HitPos + b.HitNormal * 0.1f, new vec4(1, 0, 0, 1));
      }

      //Play mine animation if we press
      if (Gu.Mouse.PressOrDown(MouseButton.Left))
      {
        if (pick != null)
        {
          pick.GrabFirstAnimation().Play(true);
        }
        if (b != null)
        {
          if (b.IsHit && b.Drome != null)
          {

            if (Gu.Keyboard.PressOrDown(Keys.LeftControl)) //Press only for placement.
            {
              if (Gu.Mouse.Press(MouseButton.Left))
              {
                //Just being lazy.
                if (block_center != null)
                {
                  PlayDropSound();
                  Gu.World.SetBlock(block_center.Value, BlockItemCode.Feldspar);
                }
              }

            }
            else
            {
              UpdateMineBlock(b);
            }
          }
        }
      }
      else
      {
        pick.GrabFirstAnimation().Repeat = false;
      }

      if (b.IsHit && b.Drome != null && Gu.Mouse.Press(MouseButton.Right))
      {
        var box = GetPicked_PlaceBlock_Box(b);
        block_center = box.center();
        Gu.World.SetBlock(block_center.Value, BlockItemCode.Torch);
      }
    }
    private void TestEllipsoid_Box()
    {
      //Testing ellipsoid Ray
      //do a bunch of lines in the world and test for collisions
      var proj_pos = Get_Interaction_Pos();
      List<Line3f> lines = new List<Line3f>()
      {
     //  new Line3f(new vec3(0,20,10),new vec3(10,-100,10)),
     //   new Line3f(new vec3(20,20,10),new vec3(20,100,10)),

        new Line3f(new vec3(50,20,50),new vec3(-40,-50,-60)),
        new Line3f(new vec3(-50,20,-50),new vec3(40,-50,60)),
        new Line3f(new vec3(-50,20,50),new vec3(40,-50,-60)),
        new Line3f(new vec3(50,20,-50),new vec3(-40,-50,60)),

        new Line3f(new vec3(-50,20,0),new vec3(40,-50,0)),
        new Line3f(new vec3(50,20,0),new vec3(-40,-50,0)),
        new Line3f(new vec3(0,20,-50),new vec3(0,-50,40)),
        new Line3f(new vec3(0,20,50),new vec3(0,-50,-40)),

      //  new Line3f(new vec3(40,20,10),new vec3(-40,20,10)),
     //  new Line3f(new vec3(41,19,10),new vec3(81,19,10)),
      };
      vec3 ellipsoid_r = new vec3(5, 10, 5);

      foreach (var projected_point in lines)
      {
        //_camera.Frustum.ProjectPoint(proj_pos, TransformSpace.World, 0.001f, MaxEditDistance_Block );
        var pick_ray = new PickRay3D(projected_point, ellipsoid_r);
        var picked_block = Gu.World.RaycastBlock_2(pick_ray);

        Gu.Context.DebugDraw.Line(projected_point.p0, projected_point.p1, new vec4(1, 0, 1, 1));

        if (picked_block.PickedBlockBoxes_Debug != null)
        {
          foreach (var box in picked_block.PickedBlockBoxes_Debug)
          {
            Gu.Context.DebugDraw.Box(box, new vec4(1, 1, 0, 1));
          }
        }

        CapsuleHit = false;
        if (picked_block.IsHit)
        {
          var blockbox = World.GetBlockBoxGlobalR3(picked_block.Block_Center);
          Gu.Context.DebugDraw.Box(blockbox, new vec4(1, 0, 0, 1));

          vec3 e_pos = pick_ray.Origin + pick_ray.Dir * (float)picked_block._t;
          Gu.Context.DebugDraw.Ellipsoid(32, 32, ellipsoid_r, e_pos, new vec4(0.4f, 0.02f, 0.76f, 1));
          Gu.Context.DebugDraw.Point(e_pos, new vec4(0, .2f, 1, 1));

          //second test . i guess 
          BoxAAHit b = new BoxAAHit();
          if (blockbox.LineOrRayIntersectInclusive_EasyOut(pick_ray, ref b)) // Ellipsoid_Collide_With_Velocity(pick_ray, ref b))
          {
            e_pos = pick_ray.Origin + pick_ray.Dir * (float)b._t;
            Gu.Context.DebugDraw.Ellipsoid(32, 32, ellipsoid_r, e_pos, new vec4(.987f, .79f, .00313f, 1));
            Gu.Context.DebugDraw.Point(e_pos, new vec4(1, 0, 0, 1));

            CapsuleHit = true;
          }
        }
      }
    }

    private void RenderFrame()
    {
      Gu.Context.Renderer.BeginRender(this, new vec4(.3f, .3f, .3f, 1));
      {
        Gu.World.Render(Gu.Context.Delta, _camera);
        Gu.World.RenderDebug(Gu.Context.Delta, _camera);
      }
      Gu.Context.Renderer.EndRender();

      Gu.Context.DebugDraw.EndFrame();

      Gu.Context.Gpu.ExecuteCallbacks_RenderThread(Gu.Context);
    }
  }
  class MainClass
  {
    public static void Main(string[] args)
    {
      var m = new MainWindow();
      m.Run();
    }
  }
}