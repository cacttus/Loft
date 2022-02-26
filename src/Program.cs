using System;
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
    bool DELETE_WORLD_START_FRESH = false;
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
      Profile = ContextProfile.Core,
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
      _camera.Viewport_Width = e.Width;
      _camera.Viewport_Height = e.Height;
    }
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
      base.OnMouseMove(e);
    }
    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
      base.OnFocusedChanged(e);
    }
    private void InitHandObjects()
    {
      vec3 right_pos = new vec3(3.0f, -2.1f, 4.5f);
      vec3 left_pos = new vec3(-3.0f, -2.1f, 4.5f);

      List<WorldObject> objs;
      objs = Gu.ResourceManager.LoadObjects(new FileLoc("pick.glb", FileStorage.Embedded));
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
      objs = Gu.ResourceManager.LoadObjects(new FileLoc("sword.glb", FileStorage.Embedded));
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
    protected override void OnLoad()
    {
      try
      {
        Gu.Init(this);

        //Cameras
        _camera = Gu.World.CreateCamera("Camera-001", this.Size.X, this.Size.Y,
           //Put player exactly in center.
           new vec3(
           World.BlockSizeX * World.GlobBlocksX * .5f,
           World.BlockSizeY * World.GlobBlocksY * .5f,
           World.BlockSizeZ * World.GlobBlocksZ * .5f
           ));

        Gu.World.Initialize(_camera, "Boogerton", DELETE_WORLD_START_FRESH);

        Gu.Context.DebugDraw.DrawBoundBoxes = false;

        CreateDebugObjects();
        InitHandObjects();
        Crosshair();

        CursorVisible = true;
      }
      catch (Exception ex)
      {
        Gu.Log.Error("Failed to initialize engine. Errors occured: " + ex.ToString());
        System.Environment.Exit(0);
      }
    }
    private void CreateDebugObjects()
    {
      //Textures
      Texture2D noise = Noise3D.TestNoise();
      Texture2D peron = Gu.ResourceManager.LoadTexture(new FileLoc("main char.png", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D grass = Gu.ResourceManager.LoadTexture(new FileLoc("grass_base.png", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D mclovin = Gu.ResourceManager.LoadTexture(new FileLoc("mclovin.jpg", FileStorage.Embedded), true, TexFilter.Nearest);
      Texture2D usopp = Gu.ResourceManager.LoadTexture(new FileLoc("usopp.jpg", FileStorage.Embedded), true, TexFilter.Bilinear);
      Texture2D hogback = Gu.ResourceManager.LoadTexture(new FileLoc("hogback.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture2D sky1 = Gu.ResourceManager.LoadTexture(new FileLoc("hdri_sky2.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);

      //Objects
      //Integrity test of GPU memory.
      for (int i = 0; i < 100; ++i)
      {
        Gu.World.CreateAndAddObject("BoxMesh", MeshData.GenBox(1, 1, 1), new Material(Shader.DefaultDiffuse(), noise));
      }
      for (int i = 1; i < 100; ++i)
      {
        Gu.World.RemoveObject("BoxMesh-" + i.ToString());
      }
      Gu.World.CreateAndAddObject("TextureFront", MeshData.GenTextureFront(_camera, 0, 0, this.Size.X, this.Size.Y), new Material(Shader.DefaultDiffuse(), peron));
      Gu.World.CreateAndAddObject("Plane.", MeshData.GenPlane(10, 10), new Material(Shader.DefaultDiffuse(), grass));
      _boxMeshThing = Gu.World.FindObject("BoxMesh");
      _boxMeshThing.Position = new vec3(0, _boxMeshThing.BoundBoxMeshBind.Height() * 0.5f, 0);

      Sphere_Rotate_Quat_Test = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test", MeshData.GenSphere(), new Material(Shader.DefaultDiffuse(), mclovin));
      Sphere_Rotate_Quat_Test2 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test2", MeshData.GenSphere(), new Material(Shader.DefaultDiffuse(), usopp));
      Sphere_Rotate_Quat_Test3 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test3", MeshData.GenSphere(), new Material(Shader.DefaultDiffuse(), hogback));

      //TODO: sky shader. 
      Material sky_mat = new Material(Shader.DefaultDiffuse(), sky1);
      sky_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      Gu.World.Sky = Gu.World.CreateObject("sky", MeshData.GenSphere(128, 128, 400, true, true), sky_mat);
      Gu.World.Sky.Mesh.DrawOrder = DrawOrder.First;
      Gu.World.Sky.Constraints.Add(new FollowConstraint(_camera, FollowConstraint.FollowMode.Snap));

      //Animation test
      var cmp = new AnimationComponent();
      vec3 raxis = new vec3(0, 1, 0);
      cmp.KeyFrames.Add(new Keyframe(0, mat3.getRotation(raxis, 0).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 0, 0), KeyframeInterpolation.Ease));
      cmp.KeyFrames.Add(new Keyframe(1, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 * 0.5 - 0.001)).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 1, 0), KeyframeInterpolation.Ease));
      cmp.KeyFrames.Add(new Keyframe(2, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 - 0.002)).toQuat(), KeyframeInterpolation.Slerp, new vec3(1, 1, 0), KeyframeInterpolation.Ease)); ;
      cmp.KeyFrames.Add(new Keyframe(3, mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 + MathUtils.M_PI_2 * 0.5 - 0.004)).toQuat(), KeyframeInterpolation.Slerp, new vec3(1, 0, 0), KeyframeInterpolation.Ease)); ;
      cmp.KeyFrames.Add(new Keyframe(4, mat3.getRotation(raxis, (float)(MathUtils.M_PI * 2 - 0.006)).toQuat(), KeyframeInterpolation.Slerp, new vec3(0, 0, 0), KeyframeInterpolation.Ease)); ;
      cmp.Play();
      _boxMeshThing.Components.Add(cmp);

      //Some fun parenting stuff.
      Sphere_Rotate_Quat_Test.AddChild(Sphere_Rotate_Quat_Test2.AddChild(Sphere_Rotate_Quat_Test3.AddChild(_boxMeshThing)));
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
      Title = $"(CharY = {chary}) (Vsync: {VSync}) FPS: {1f / e.Time:0} " +
         $"AllGlobs: {Gu.World.NumGlobs} Render: {Gu.World.NumRenderGlobs} Visible: {Gu.World.NumVisibleRenderGlobs} " +
         $"Elements_Frame:{MeshData.dbg_numDrawElements_Frame} Arrays_Frame: {MeshData.dbg_numDrawArrays_Frame} " +
         $"OBs culled:{Gu.World.Dbg_N_OB_Culled} " +
         $"Mouse:{Gu.Mouse.Pos.x},{Gu.Mouse.Pos.y} "
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

      if (Gu.Context.PCKeyboard.Press(Keys.D1))
      {
        // _boxMeshThing.Mesh.BeginEdit(0, 1);
        // MeshVert v= _boxMeshThing.Mesh.EditVert(0);
        // _boxMeshThing.Mesh.EndEdit();
      }
      if (Gu.Keyboard.PressOrDown(Keys.Escape))
      {
        Close();
      }
      float speed = 20.7f;

      float speedMul = 1;
      if (Gu.Keyboard.PressOrDown(Keys.LeftControl) || Gu.Keyboard.PressOrDown(Keys.RightControl))
      {
        speedMul = 3;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Q }))
      {
        _camera.Position += _camera.BasisY * speed * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.E }))
      {
        _camera.Position -= _camera.BasisY * speed * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Up, Keys.W }))
      {
        _camera.Position += _camera.BasisZ * speed * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Down, Keys.S }))
      {
        _camera.Position -= _camera.BasisZ * speed * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Right, Keys.D }))
      {
        _camera.Position += _camera.BasisX * speed * Gu.CoordinateSystemMultiplier * (float)Gu.Context.Delta * speedMul;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Left, Keys.A }))
      {
        _camera.Position -= _camera.BasisX * speed * Gu.CoordinateSystemMultiplier * (float)Gu.Context.Delta * speedMul;
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
      if (Gu.Keyboard.Press(Keys.F1))
      {
        Gu.Context.DebugDraw.DrawBoundBoxes = !Gu.Context.DebugDraw.DrawBoundBoxes;
      }
      if (Gu.Keyboard.Press(Keys.F2))
      {
        VSync = (VSync==VSyncMode.Off) ? VSyncMode.On : VSyncMode.Off;
      }
      if (Gu.Keyboard.Press(Keys.F11))
      {
        if(this.WindowState == WindowState.Fullscreen)
        {
          WindowState = WindowState.Normal;
        }
        else
        {
          WindowState = WindowState.Fullscreen;
        }
      }
      if (Gu.Keyboard.Press(Keys.D1))
      {
        Material.DefaultDiffuse().Shader.lightingModel = ((Material.DefaultDiffuse().Shader.lightingModel + 1) % 3);
      }
      if (Gu.Keyboard.PressOrDown(Keys.D2))
      {
        Material.DefaultDiffuse().Shader.GGX_X = (Material.DefaultDiffuse().Shader.GGX_X + 0.01f) % 3.0f;
      }
      if (Gu.Keyboard.PressOrDown(Keys.D3))
      {
        Material.DefaultDiffuse().Shader.GGX_Y = (Material.DefaultDiffuse().Shader.GGX_Y + 0.01f) % 3.0f;
      }
      if (Gu.Keyboard.Press(Keys.D4))
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
      if (Gu.Keyboard.PressOrDown(Keys.D6))
      {
        Material.DefaultDiffuse().Shader.nmap += 0.01f;
        Material.DefaultDiffuse().Shader.nmap = Material.DefaultDiffuse().Shader.nmap % 1;
      }

      //Project ray and hit box.
      //vec2 projec_pos = Gu.Mouse.Pos;
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
      Line3f proj_pt = _camera.Frustum.ProjectPoint(projec_pos, TransformSpace.World, 0.001f);
      var b = Gu.World.RaycastBlock(new PickRay3D(proj_pt));
      if (b.Hit)
      {
        //  Gu.Context.DebugDraw.Point(b.HitPos + b.HitNormal * 0.1f, new vec4(1, 0, 0, 1));
        Gu.Context.DebugDraw.Box(Gu.World.GetBlockBox(b, 0.01f), new vec4(.1014f, .155f, .0915f, 1));
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
          if (b.Drome != null)
          {
            Gu.World.SetBlock(b.Drome, b.BlockPosLocal, Block.Empty, false);
          }
        }
      }
      else
      {
        pick.GrabFirstAnimation().Repeat = false;
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
    protected override void OnRenderFrame(FrameEventArgs e)
    {
      Gu.Context.Renderer.BeginRender(this, new vec4(1, 1, 1, 1));
      {
        Gu.World.Render(Gu.Context.Delta, _camera);
        Gu.World.RenderDebug(Gu.Context.Delta, _camera);
      }
      Gu.Context.Renderer.EndRender();

      Gu.Context.DebugDraw.EndFrame();

      //GC.Collect();
      Gpu.FreeGPUMemory(Gu.Context);
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