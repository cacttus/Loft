using System;
using System.Collections.Generic;

namespace PirateCraft
{
    public class World
    {
        public List<WorldObject> Objects { get; set; } = new List<WorldObject>();

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
            foreach (var ob in Objects)
            {
                if (ob.Mesh != null)
                {
                    if (ob.Material != null)
                    {
                        ob.Material.PreRender(Delta, camera, ob.World);
                        Renderer.Render(camera, ob);
                        ob.Material.PostRender();
                    }
                    else
                    {
                        Gu.Log.Error("Object had no material");
                    }
                }
            }
        }
    }
}
