
using System;
using System.Collections.Generic;
using Loft;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.Serialization;


namespace Loft
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
      var sun = new Light("sunlight")
      {
        Position_Local = new vec3(10, 100, 10),
        LightType = LightType.Direction,
        LightPower = 300.0f,
        LightColor = new vec3(1, 1, 1)
      };
      sun.LookAtConstraint(new vec3(0, 0, 0));
      Gu.World.AddObject(sun);

      Gu.World.AddObject(new Light("pt")
      {
        Position_Local = new vec3(0, 10, 0),
        LightType = LightType.Point,
        LightRadius = 60,
        LightPower = 0.75f,
        LightColor = new vec3(1, 1, 1)
      });

      Gu.World.AddObject(new Light("pt2")
      {
        Position_Local = new vec3(-10, 10, -10),
        LightType = LightType.Point,
        LightRadius = 50,
        LightPower = 0.75f,
        LightColor = new vec3(.1f, .85f, .58f)
      });

      Gu.World.AddObject(new Light("pt3")
      {
        Position_Local = new vec3(10, 10, 10),
        LightType = LightType.Point,
        LightRadius = 50,
        LightPower = 0.75f,
        LightColor = new vec3(1, 1, 1)
      });

      Gu.World.AddObject(new Light("pt4")
      {
        Position_Local = new vec3(20, 10, 20),
        LightType = LightType.Point,
        LightRadius = 50,
        LightPower = 0.75f,
        LightColor = new vec3(1, 0, 1)
      });
    }
    private void CreateSky()
    {
      var that = this;

      Texture? tx_sky = new Texture("tx_sky", Gu.Lib.GetOrLoadImage(new FileLoc("hdri_sky2.jpg", EmbeddedFolder.Image)), true, TexFilter.Trilinear);
      Texture? tx_sky_stars = new Texture("tx_sky_stars", Gu.Lib.GetOrLoadImage(new FileLoc("hdri_stars.jpg", EmbeddedFolder.Image)), true, TexFilter.Trilinear);
      Texture? tx_sun = new Texture("tx_sun", Gu.Lib.GetOrLoadImage(new FileLoc("tx64_sun.png", EmbeddedFolder.Image)), true, TexFilter.Trilinear);
      Texture? tx_moon = new Texture("tx_moon", Gu.Lib.GetOrLoadImage(new FileLoc("tx64_moon.png", EmbeddedFolder.Image)), true, TexFilter.Trilinear);
      Texture? tx_bloom = new Texture("tx_bloom", Gu.Lib.GetOrLoadImage(new FileLoc("bloom.png", EmbeddedFolder.Image)), true, TexFilter.Trilinear);

      //Sky 
      Material sky_mat = new Material("sky",  tx_sky);
      sky_mat.Flat = true;
      sky_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      var sky = Gu.World.CreateAndAddObject("sky", MeshGen.GenSphere("sky", DayNightCycle.SkyRadius, 128, 128, true, true), sky_mat);
      sky.Selectable = false;
      sky.Pickable = false;
      sky_mat.DrawOrder = DrawOrder.First;
      sky_mat.DrawMode = DrawMode.Deferred;

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

        // if (Gu.TryGetSelectedViewCamera(out var cm))
        // {
        //   var vp = cm.RootParent;
        //   obj.Position_Local = vp.WorldMatrix.ExtractTranslation();
        // }
        //TODO:
        //sky_mat.SetUniform("_ufSkyBlend")
      };

      //Empty that rotates the sun / moon
      var sun_moon_empty = Gu.World.CreateObject("sun_moon_empty", null, null);
      sun_moon_empty.OnUpdate = (obj) =>
      {
        double ang = Gu.World.WorldProps.DayNightCycle.DayTime_Seconds / Gu.World.WorldProps.DayNightCycle.DayLength_Seconds * Math.PI * 2.0;
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), (float)ang);

        // if (Gu.TryGetSelectedViewCamera(out var cm))
        // {
        //   vec3 pe = cm.Position_World;//.WorldMatrix.ExtractTranslation();
        //   obj.Position_Local = pe;
        // }
      };
      sun_moon_empty.Persistence = DataPersistence.Temporary;
      Gu.World.AddObject(sun_moon_empty);
      /*
      view update action
      for all ob
      ob.OnUpdateForView(rv)
      ob.OnBeforeRender()
      */
      Material sun_moon_mat = new Material("sunmoon", new Shader("Shader_SunMoonShader", "v_sun_moon"));
      sun_moon_mat.GpuRenderState.DepthTest = false;//Disable depth test.
      sun_moon_mat.GpuRenderState.CullFace = false;//Disable depth test.
      sun_moon_mat.GpuRenderState.Blend = false;
      sun_moon_mat.DrawMode = DrawMode.Deferred;
      sun_moon_mat.DrawOrder = DrawOrder.First;

      float sun_size = 13;
      float moon_size = 23;

      //Sun
      var sun_mat = sun_moon_mat.Clone() as Material;
      sun_mat.AlbedoSlot.Texture = tx_sun;
      var sun = Gu.World.CreateObject("sun", MeshGen.GenPlane("sun", sun_size, sun_size), sun_mat);
      sun.OnUpdate = (obj) =>
      {
        //All this stuff can be script.
        sun_mat.BaseColor = new vec4(.994f, .990f, .8f, 1);
        obj.Position_Local = new vec3(DayNightCycle.SkyRadius, 0, 0);
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), (float)Math.PI / 2);
      };

      sun_moon_empty.AddChild(sun);

      // sun.LookAtConstraint(new vec3(0, 0, 0));


      var bloom_mat = sun_moon_mat.Clone() as Material;
      bloom_mat.AlbedoSlot.Texture = tx_bloom;
      var sun_bloom = Gu.World.CreateObject("sun_bloom", MeshGen.GenPlane("sun_bloom", sun_size, sun_size), bloom_mat);
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
        //  obj.Scale_Local = new vec3(1.1f + bloom_dp * 30.0f, 0, 1.1f + bloom_dp * 30.0f);
        }

      };
      sun.AddChild(sun_bloom);

      //Moon
      var moon_mat = sun_moon_mat.Clone() as Material;
      moon_mat.AlbedoSlot.Texture = tx_moon;
      var moon = Gu.World.CreateObject("moon", MeshGen.GenPlane("moon", moon_size, moon_size), moon_mat);
      moon.OnUpdate = (obj) =>
      {
        //All this stuff can be script.
        moon_mat.BaseColor = new vec4(.78f, .78f, .92f, 1);
        obj.Position_Local = new vec3(-DayNightCycle.SkyRadius, 0, 0);
        obj.Rotation_Local = quat.fromAxisAngle(new vec3(0, 0, 1), -(float)Math.PI / 2);
      };
      sun_moon_empty.AddChild(moon);

      var moon_bloom = Gu.World.CreateObject("moon_bloom", MeshGen.GenPlane("moon_bloom", moon_size, moon_size), bloom_mat);
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
        //  obj.Scale_Local = new vec3(1.1f + bloom_dp * 4.0f, 0, 1.1f + bloom_dp * 4.0f);
        }
      };
      moon.AddChild(moon_bloom);
    }
    private void TestCreateDebugObjects()
    {
      //Textures
      Texture tx_c = new Texture("tx_c", Gu.Lib.GetOrLoadImage(new FileLoc("mario.jpg", EmbeddedFolder.Image)), true, TexFilter.Nearest);
      Texture tx_d = new Texture("tx_d", Gu.Lib.GetOrLoadImage(new FileLoc("ganon.jpg", EmbeddedFolder.Image)), true, TexFilter.Bilinear);
      Texture tx_e = new Texture("tx_e", Gu.Lib.GetOrLoadImage(new FileLoc("peach.jpg", EmbeddedFolder.Image)), true, TexFilter.Trilinear);
 
      //normal map test (slow)
      //new Texture2D(ResourceManager.LoadImage().CreateNormalMap(false), true, TexFilter.Linear)
      //Gu.Debug_IntegrityTestGPUMemory();

      testobjs[0] = new Material("sphere_rot",tx_c);
      testobjs[1] = new Material("sphere_rot2",  tx_d);
      testobjs[1].Flat = true;
      testobjs[2] = new Material("sphere_rot3", tx_e);

      Sphere_Rotate_Quat_Test = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test", MeshGen.GenSphere("Sphere_Rotate_Quat_Test", 1, 12, 12, true), testobjs[0]);
      Sphere_Rotate_Quat_Test2 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test2", MeshGen.GenEllipsoid("Sphere_Rotate_Quat_Test2", new vec3(1f, 1, 1f), 32, 32, true), testobjs[1]);
      Sphere_Rotate_Quat_Test3 = Gu.World.CreateAndAddObject("Sphere_Rotate_Quat_Test3", MeshGen.GenEllipsoid("Sphere_Rotate_Quat_Test3", new vec3(1, 1, 1), 32, 32, true), testobjs[2]);
      Sphere_Rotate_Quat_Test.Position_Local = new vec3(0, 3, 0);
      Sphere_Rotate_Quat_Test2.Position_Local = new vec3(-3, 3, 0);
      Sphere_Rotate_Quat_Test3.Position_Local = new vec3(3, 3, 0);

      //Test STB laoding EXR images.
      Texture tx_exr = new Texture("tx_exr", Gu.Lib.GetOrLoadImage(new FileLoc("hilly_terrain_01_2k.hdr", EmbeddedFolder.Image)), true, TexFilter.Bilinear);
      var exr_test = MeshGen.GenPlane("tx_exr", 10, 10);
      var exr_test_mat = new Material("plane", tx_exr);
      var exr_test_ob = Gu.World.CreateAndAddObject("EXR test", exr_test, exr_test_mat);
      exr_test_ob.Position_Local = new vec3(10, 10, 5);

      //Animation test
      // vec3 raxis = new vec3(0, 1, 0);
      // var testTrack = new AnimationClip("test");
      // testTrack.AddFrame(0, new vec3(0, 0, 0), mat3.getRotation(raxis, 0).toQuat(), new vec3(1, 1, 1));
      // testTrack.AddFrame(1, new vec3(0, 1, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 * 0.5 - 0.001)).toQuat(), new vec3(.5f, .5f, 3));
      // testTrack.AddFrame(2, new vec3(1, 1, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 - 0.002)).toQuat(), new vec3(2, 2, 2));
      // testTrack.AddFrame(3, new vec3(1, 0, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI_2 + MathUtils.M_PI_2 * 0.5 - 0.004)).toQuat(), new vec3(2, 3, 1));
      // testTrack.AddFrame(4, new vec3(0, 0, 0), mat3.getRotation(raxis, (float)(MathUtils.M_PI * 2 - 0.006)).toQuat(), new vec3(1, 1, 1));
      // var adata = new AnimationData("", testTrack);

      // var cmp = new AnimationComponent(new List<AnimationData>() { adata });
      // cmp.Repeat = true;
      // cmp.Play();
      //Sphere_Rotate_Quat_Test.AddComponent(cmp);

      //Check to see if this uses the resource and not the real thing
      ModelFile mod;
      // var mod = Gu.Lib.GetOrLoadModel(Rs.Model.Gear);
      // mod.CreateObjectInstances(new vec3(2,0,2));
      // mod = Gu.Lib.GetOrLoadModel(Rs.Model.Barrel);
      // mod.CreateObjectInstances(new vec3(-2,0,-2));      
      // mod = Gu.Lib.GetOrLoadModel(Rs.Model.Camera);
      // mod.CreateObjectInstances(new vec3(10,0,10));      
      // mod = new ModelFile("guyonlytest", new FileLoc("guy_only_test.glb", FileStorage.Embedded));
      // mod.CreateObjectInstances(new vec3(3,3,3));
      mod = new ModelFile("angelina", new FileLoc("angelina.glb", EmbeddedFolder.Model));
      var obb = mod.CreateObject("Armature", new vec3(-2, 3, 10), quat.fromAxisAngle(new vec3(0, 1, 0), MathUtils.M_PI), new vec3(9, 9, 9));

      obb.Play(new AnimationClip("Walk"));

      //).CreateObjects(new vec3(-2, 3, 10), quat.fromAxisAngle(new vec3(0, 1, 0), MathUtils.M_PI));
      mod = new ModelFile("elecro", new FileLoc("guy_only_test.glb", EmbeddedFolder.Model));
      mod.CreateObjects(new vec3(4, 3, 10), quat.fromAxisAngle(new vec3(0, 1, 0), MathUtils.M_PI));

    }

  }
}
