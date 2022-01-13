using System;
namespace PirateCraft
{
    public class Frustum3D
    {
        WeakReference<Viewport> Viewport = null;
        public float Near { get; set; } = 1.0f;
        public float Far { get; set; } = 1000.0f;
        public Frustum3D(Viewport v, float near, float far)
        {
            Near=near;
            Far=far;
            Viewport = new WeakReference<Viewport>(v);
        }
    }
}
