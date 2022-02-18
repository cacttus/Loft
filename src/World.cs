using System;
using System.Collections.Generic;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;
using Quat = OpenTK.Quaternion;

namespace PirateCraft
{
   public class World
   {
      private Dictionary<string, WorldObject> Objects { get; set; } = new Dictionary<string, WorldObject>();

      //Public <camera, rendertexture>
      //Store rendertexture array to each camera.
      // Update array when camera changes size.

      public World()
      {
      }
      public WorldObject FindObject(string name)
      {
         WorldObject obj = null;
         Objects.TryGetValue(name, out obj);
         return obj;
      }
      public Camera3D CreateCamera(string name, int w, int h, Vec3f pos)
      {
         Camera3D c = new Camera3D(name, w, h);
         c.Position = pos;
         c.Update(0);
         Objects.Add(name, c);
         return c;
      }
      private void AddObject(string name, WorldObject ob)
      {
         //Use a suffix if there is a duplicate object
         int suffix = 0;
         string name_suffix = name;
         while (FindObject(name_suffix) != null)
         {
            suffix++;
            name_suffix = name +"-"+ suffix.ToString();
         }
         ob.Name = name_suffix;
         Objects.Add(name_suffix, ob);
      }
      public WorldObject CreateObject(string name, MeshData mesh, Material material, Vec3f pos = default(Vec3f))
      {
         WorldObject ob = new WorldObject(pos);
         ob.Name = name;
         ob.Mesh = mesh;
         ob.Material = material;
         AddObject(name, ob);
         return ob;
      }
      public void DestroyObject(string name)
      {
         WorldObject wo = null;
         if (Objects.TryGetValue(name, out wo))
         {
            Objects.Remove(name);
         }
         else
         {
            Gu.Log.Error("Object '" + name + "' was not found.");
         }
      }
      public void Update(double dt)
      {
         foreach (var ob in Objects.Values)
         {
            ob.Update(dt);
         }
      }
      public void Render(double Delta, Camera3D camera)
      {
         camera.BeginRender();
         {
            //TODO: of course we're going to use a bucket collection algorithm. This is in the future.
            foreach (var ob in Objects.Values)
            {
               DrawOb(ob,Delta,camera);
            }
         }
         camera.EndRender();
      }
      private void DrawOb(WorldObject ob, double Delta, Camera3D camera)
      {
         if (ob.Mesh != null)
         {
            Material mat = ob.Material;
            if (ob.Material == null)
            {
               mat = Material.Default(ob.Color);
            }
            mat.BeginRender(Delta, camera, ob);
            Renderer.Render(camera, ob, mat);
            mat.EndRender();
         }
         foreach(var c in ob.Children)
         {
            DrawOb(c,Delta,camera);
         }
      }
   }
}
