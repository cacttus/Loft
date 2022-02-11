using System;
using System.Collections.Generic;

namespace PirateCraft
{
  public class World
  {
    public List<WorldObject> Objects { get; set; } = new List<WorldObject>();

    //Public <camera, rendertexture>
    //Store rendertexture array to each camera.
    // Update array when camera changes size.

    public World()
    {
    }
    public void Update()
    {
      foreach (var ob in Objects)
      {
        ob.Update();
      }
    }
    public void Render(double Delta, Camera3D camera)
    {
      camera.BeginRender();
      {
        //TODO: of course we're going to use a bucket collection algorithm. This is in the future.
        foreach (var ob in Objects)
        {
          if (ob.Mesh != null)
          {
            if (ob.Material != null)
            {
              ob.Material.BeginRender(Delta, camera, ob.World);
              Renderer.Render(camera, ob);
              ob.Material.EndRender();
            }
            else
            {
              Gu.Log.Error("Object had no material");
            }
          }
        }
      }
      camera.EndRender();
    }
  }
}
