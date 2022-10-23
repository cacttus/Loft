
using System;
using System.Collections.Generic;
using PirateCraft;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.Serialization;


namespace PirateCraft
{
  public class MyWorldScript : IWorldScript
  {
    private WorldObject Sphere_Rotate_Quat_Test;
    private WorldObject Sphere_Rotate_Quat_Test2;
    private WorldObject Sphere_Rotate_Quat_Test3;
    private Material[] testobjs = new Material[3];

    public void OnLoad(World w)
    {
      Gu.Log.Info("Init script");
      Gu.Log.Debug("Debug:Creatingf flat area");
      Gu.WorldLoader.CreateHillsArea();

      TestCreateDebugObjects();
      CreateSky();
      CreateLight();
    }
    public void OnUpdate(World w, double delta)
    {
      if (Gu.Context.FrameStamp % 100 == 0)
      {
      }
    }
    public void OnExit(World w)
    {
    }

    private void CreateLight()
    {
      var sun = new WorldObject("sunlight");
      sun.LookAtConstraint(new vec3(0,0,0));
      sun.HasLight = true;
      sun.LightType = LightType.Direction;//Direction is the object heading
      sun.Position_Local = new vec3(10, 100, 10);
      sun.LightRadius = 10000;
      sun.LightPower = 100.0f;
      sun.LightColor = new vec3(1,1,1);//Gu.World.WorldProps.DayNightCycle.SkyColor.ToVec3();//. new vec3(.9f, .8f, .1f);
      Gu.World.AddObject(sun);

      var l = new WorldObject("pt");
      l.LightType = LightType.Point;
      l.Position_Local = new vec3(0, 10, 0);
      l.HasLight = true;
      l.LightRadius = 50;
      l.LightPower = 0.75f;
      l.LightColor = new vec3(1, 1, 1);
      Gu.World.AddObject(l);

      l = new WorldObject("pt2");
      l.LightType = LightType.Point;
      l.Position_Local = new vec3(-10, 10, -10);
      l.HasLight = true;
      l.LightRadius = 50;
      l.LightPower = 0.75f;
      l.LightColor = new vec3(.1f, .85f, .58f);
      Gu.World.AddObject(l);

      l = new WorldObject("pt3");
      l.LightType = LightType.Point;
      l.Position_Local = new vec3(10, 10, 10);
      l.HasLight = true;
      l.LightRadius = 50;
      l.LightPower = 0.75f;
      l.LightColor = new vec3(1, 1, 1);
      Gu.World.AddObject(l);

      l = new WorldObject("pt4");
      l.LightType = LightType.Point;
      l.Position_Local = new vec3(20, 10, 20);
      l.HasLight = true;
      l.LightRadius = 50;
      l.LightPower = 0.75f;
      l.LightColor = new vec3(1, 0, 1);
      Gu.World.AddObject(l);
    }
    private void CreateCrosshair(Camera3D c)
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
      Crosshair.Mesh = new MeshData("crosshair_mesh", PrimitiveType.Lines, Gpu.CreateVertexBuffer("crosshair", verts));
      Crosshair.Mesh.DrawOrder = DrawOrder.Last;
      Crosshair.Position_Local = new vec3(0, 0, 3);
      Material crosshair_mat = new Material("crosshair", Shader.DefaultFlatColorShader());
      crosshair_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      crosshair_mat.GpuRenderState.Blend = true;
      Crosshair.Material = crosshair_mat;
      c.AddChild(Crosshair);
    }
    private void CreateSky()
    {
      var that = this;

      Texture? tx_sky = Gu.Lib.LoadTexture("tx_sky", new FileLoc("hdri_sky2.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture? tx_sky_stars = Gu.Lib.LoadTexture("tx_sky_stars", new FileLoc("hdri_stars.jpg", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture? tx_sun = Gu.Lib.LoadTexture("tx_sun", new FileLoc("tx64_sun.png", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture? tx_moon = Gu.Lib.LoadTexture("tx_moon", new FileLoc("tx64_moon.png", FileStorage.Embedded), true, TexFilter.Trilinear);
      Texture? tx_bloom = Gu.Lib.LoadTexture("tx_bloom", new FileLoc("bloom.png", FileStorage.Embedded), true, TexFilter.Trilinear);

      //Sky
      Material sky_mat = new Material("sky", Shader.DefaultObjectShader(), tx_sky);
      sky_mat.Flat = true;
      sky_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      var sky = Gu.World.CreateAndAddObject("sky", MeshGen.GenSphereResource("sky", DayNightCycle.SkyRadius, 128, 128, true, true), sky_mat);
      sky.Selectable = false;
      sky.Pickable = false;
      sky.Mesh.DrawOrder = DrawOrder.First;
      sky.Mesh.DrawMode = DrawMode.Deferred;
      //sky.Constraints.Add(new FollowConstraint(Player, FollowConstraint.FollowMode.Snap)); ;
      sky.OnUpdate = (obj) =>
      {
        //Kind of sloppy way to do this whole thing. 
        sky_mat.BaseColor =
        new vec4(
          (float)Gu.World.WorldProps.DayNightCycle.SkyColor.x,
          (float)Gu.World.WorldProps.DayNightCycle.SkyColor.y,
          (float)Gu.World.WorldProps.DayNightCycle.SkyColor.z,
          1
          );

        if (Gu.TryGetSelectedViewCamera(out var cm))
        {
          var vp = cm.RootParent;
          obj.Position_Local = vp.WorldMatrix.ExtractTranslation();
        }
        //TODO:
        //sky_mat.SetUniform("_ufSkyBlend")
      };

      //Empty that rotates the sun / moon
      var sun_moon_empty = Gu.World.CreateObject("sun_moon_empty", null, null);
      sun_moon_empty.OnUpdate = (obj) =>
      {
        double ang = Gu.World.WorldProps.DayNightCycle.DayTime_Seconds / Gu.World.WorldProps.DayNightCycle.DayLength_Seconds * Math.PI * 2.0;
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), (float)ang);

        if (Gu.TryGetSelectedViewCamera(out var cm))
        {
          vec3 pe = cm.Position_World;//.WorldMatrix.ExtractTranslation();
          obj.Position_Local = pe;
        }
      };
      sun_moon_empty.Persistence = DataPersistence.Temporary;
      Gu.World.AddObject(sun_moon_empty);
      /*
      view update action
      for all ob
      ob.OnUpdateForView(rv)
      ob.OnBeforeRender()
      */
      Material sun_moon_mat = new Material("sunmoon", new Shader("Shader_SunMoonShader", "v_sun_moon", FileStorage.Embedded));
      sun_moon_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      sun_moon_mat.GpuRenderState.CullFace = false;//Disable depth test.
      sun_moon_mat.GpuRenderState.Blend = false;

      float sun_size = 13;
      float moon_size = 23;

      //Sun
      var sun_mat = sun_moon_mat.Clone() as Material;
      sun_mat.AlbedoSlot.Texture = tx_sun;
      var sun = Gu.World.CreateObject("sun", MeshGen.GenPlaneResource("sun", sun_size, sun_size), sun_mat);
      sun.Mesh.DrawOrder = DrawOrder.First;
      sun.OnUpdate = (obj) =>
      {
        //All this stuff can be script.
        sun_mat.BaseColor = new vec4(.994f, .990f, .8f, 1);
        obj.Position_Local = new vec3(DayNightCycle.SkyRadius, 0, 0);
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), (float)Math.PI / 2);
        sun.LightColor = new vec3(1,1,1);// Gu.World.WorldProps.DayNightCycle.LightColor.ToVec3() ;//. new vec3(.9f, .8f, .1f);
      };

      sun_moon_empty.AddChild(sun);

      // sun.LookAtConstraint(new vec3(0, 0, 0));


      var bloom_mat = sun_moon_mat.Clone() as Material;
      bloom_mat.AlbedoSlot.Texture = tx_bloom;
      var sun_bloom = Gu.World.CreateObject("sun_bloom", MeshGen.GenPlaneResource("sun_bloom", sun_size, sun_size), bloom_mat);
      sun_bloom.Mesh.DrawOrder = DrawOrder.First;
      sun_bloom.OnUpdate = (obj) =>
      {
        if (Gu.TryGetSelectedViewCamera(out var cm))
        {
          vec3 dir = Gu.World.WorldProps.DayNightCycle.MoonDir.ToVec3();
          //ease multiplier so that the glare does not show on the horizon.
          float horizon_mul = (float)MathUtils.Ease(0, 1, (double)dir.dot(new vec3(0, 1, 0)));
          float bloom_dp = dir.dot(cm.BasisZ_World);
          float bloom_dp_pw = (float)Math.Pow(bloom_dp, 64);
          bloom_mat.BaseColor = new vec4(sun_mat.BaseColor.x, sun_mat.BaseColor.y, sun_mat.BaseColor.z, bloom_dp_pw * horizon_mul * 0.9413f);
          obj.Scale_Local = new vec3(1.1f + bloom_dp * 30.0f, 0, 1.1f + bloom_dp * 30.0f);
        }

      };
      sun.AddChild(sun_bloom);

      //Moon
      var moon_mat = sun_moon_mat.Clone() as Material;
      moon_mat.AlbedoSlot.Texture = tx_moon;
      var moon = Gu.World.CreateObject("moon", MeshGen.GenPlaneResource("moon", moon_size, moon_size), moon_mat);
      moon.Mesh.DrawOrder = DrawOrder.First;
      moon.OnUpdate = (obj) =>
      {
        //All this stuff can be script.
        moon_mat.BaseColor = new vec4(.78f, .78f, .92f, 1);
        obj.Position_Local = new vec3(-DayNightCycle.SkyRadius, 0, 0);
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), -(float)Math.PI / 2);
      };
      sun_moon_empty.AddChild(moon);


      var moon_bloom = Gu.World.CreateObject("moon_bloom", MeshGen.GenPlaneResource("moon_bloom", moon_size, moon_size), bloom_mat);
      moon_bloom.Mesh.DrawOrder = DrawOrder.First;
      moon_bloom.OnUpdate = (obj) =>
      {
        //All this stuff can be script.
        if (Gu.TryGetSelectedViewCamera(out var cm))
        {
          vec3 dir = Gu.World.WorldProps.DayNightCycle.MoonDir.ToVec3() * -1.0f;
          //ease multiplier so that the glare does not show on the horizon.
          float horizon_mul = (float)MathUtils.Ease(0, 1, (double)dir.dot(new vec3(0, 1, 0)));
          float bloom_dp = dir.dot(cm.BasisZ_World);
          float bloom_dp_pw = (float)Math.Pow(bloom_dp, 64);
          obj.Material.BaseColor = new vec4(moon_mat.BaseColor.x, moon_mat.BaseColor.y, moon_mat.BaseColor.z, bloom_dp_pw * horizon_mul * 0.3f);
          obj.Scale_Local = new vec3(1.1f + bloom_dp * 4.0f, 0, 1.1f + bloom_dp * 4.0f);
        }
      };
      moon.AddChild(moon_bloom);
    }
    private void TestCreateDebugObjects()
    {
      //Textures
      var grass = new FileLoc("grass_base.png", FileStorage.Embedded);
      var gates = new FileLoc("gates.jpg", FileStorage.Embedded);
      var brady = new FileLoc("brady.jpg", FileStorage.Embedded);
      var zuck = new FileLoc("zuck.jpg", FileStorage.Embedded);
      var mainch = new FileLoc("main char.png", FileStorage.Embedded);
      Texture tx_peron = new Texture("tx_peron", Gu.Lib.LoadImage("tx_peron", mainch), true, TexFilter.Bilinear);
      Texture tx_grass = new Texture("tx_grass", Gu.Lib.LoadImage("tx_grass", grass), true, TexFilter.Bilinear);
      Texture tx_gates = new Texture("tx_gates", Gu.Lib.LoadImage("tx_gates", gates), true, TexFilter.Nearest);
      Texture tx_zuck = new Texture("tx_zuck", Gu.Lib.LoadImage("tx_zuck", zuck), true, TexFilter.Bilinear);
      Texture tx_brady = new Texture("tx_brady", Gu.Lib.LoadImage("tx_brady", brady), true, TexFilter.Trilinear);

      //Objects
      Gu.World.CreateAndAddObject("Grass-Plane.", MeshGen.GenPlaneResource("Grass-Plane", 10, 10), new Material("grass-plane", Shader.DefaultObjectShader(), tx_grass, null));

      //normal map test (slow)
      //new Texture2D(ResourceManager.LoadImage(brady).CreateNormalMap(false), true, TexFilter.Linear)

      //Gu.Debug_IntegrityTestGPUMemory();

      testobjs[0] = new Material("sphere_rot", Shader.DefaultObjectShader(), tx_gates);
      testobjs[1] = new Material("sphere_rot2", Shader.DefaultObjectShader(), tx_zuck);
      testobjs[1].Flat = true;
      testobjs[2] = new Material("sphere_rot3", Shader.DefaultObjectShader(), tx_brady, null);

      Sphere_Rotate_Quat_Test = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test", MeshGen.GenSphereResource("Sphere_Rotate_Quat_Test", 1, 12, 12, true), testobjs[0]);
      Sphere_Rotate_Quat_Test2 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test2", MeshGen.GenEllipsoid("Sphere_Rotate_Quat_Test2", new vec3(1f, 1, 1f), 32, 32, true), testobjs[1]);
      Sphere_Rotate_Quat_Test3 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test3", MeshGen.GenEllipsoid("Sphere_Rotate_Quat_Test3", new vec3(1, 1, 1), 32, 32, true), testobjs[2]);
      Sphere_Rotate_Quat_Test.Position_Local = new vec3(0, 3, 0);
      Sphere_Rotate_Quat_Test2.Position_Local = new vec3(-3, 3, 0);
      Sphere_Rotate_Quat_Test3.Position_Local = new vec3(3, 3, 0);

      //Test STB laoding EXR images.
      Texture tx_exr = new Texture("tx_exr", Gu.Lib.LoadImage("tx_exr", new FileLoc("hilly_terrain_01_2k.hdr", FileStorage.Embedded)), true, TexFilter.Bilinear);
      var exr_test = MeshGen.GenPlaneResource("tx_exr", 10, 10);
      var exr_test_mat = new Material("plane", Shader.DefaultObjectShader(), tx_exr);
      var exr_test_ob = Gu.World.CreateAndAddObject("EXR test", exr_test, exr_test_mat);
      exr_test_ob.Position_Local = new vec3(10, 10, 5);

      //Animation test
      vec3 raxis = new vec3(0, 1, 0);
      var adata = new AnimationData("test");
      adata.AddFrame(0, new vec3(0, 0, 0), mat3.getRotation(raxis, 0).toQuat(), new vec3(1, 1, 1));
      adata.AddFrame(1, new vec3(0, 1, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 * 0.5 - 0.001)).toQuat(), new vec3(.5f, .5f, 3));
      adata.AddFrame(2, new vec3(1, 1, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 - 0.002)).toQuat(), new vec3(2, 2, 2));
      adata.AddFrame(3, new vec3(1, 0, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 + MathUtils.M_PI_2 * 0.5 - 0.004)).toQuat(), new vec3(2, 3, 1));
      adata.AddFrame(4, new vec3(0, 0, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI * 2 - 0.006)).toQuat(), new vec3(1, 1, 1));
      var cmp = new AnimationComponent(adata);
      cmp.Repeat = true;
      cmp.Play();
      Sphere_Rotate_Quat_Test.AddComponent(cmp);

      //Check to see if this uses the resource and not the real thing
      var gearob = Gu.Lib.LoadModel(RName.WorldObject_Gear);
      Gu.World.AddObject(gearob);
      gearob.Position_Local = new vec3(4, 8, -4);
      if (gearob.Component<AnimationComponent>(out var x))
      {
        x.Repeat = true;
        x.Play();
      }
      var bare = Gu.Lib.LoadModel(RName.WorldObject_Barrel);
      Gu.World.AddObject(bare);
      bare.Position_Local = new vec3(-4, 8, -7);
    }

  }
}
