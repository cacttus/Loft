//using System;
//namespace PirateCraft
//{
//    public class Frustum3D
//    {
//        WeakReference<Viewport> Viewport = null;
//        WeakReference<Camera3D> Camera = null;

//        public float Near { get; private set; } = 1.0f;
//        public float Far { get; private set; } = 1000.0f;

//        public Vec3f NearCenter { get; private set; }
//        public Vec3f FarCenter { get; private set; }
//        public Vec3f NearTopLeft { get; private set; }

//        public Frustum3D(Camera3D cam, Viewport v, float near, float far)
//        {
//            Near=near;
//            Far=far;
//            Viewport = new WeakReference<Viewport>(v);
//            Camera = new WeakReference<Camera3D>(cam);

//        }
//        public void Update(Camera3D myCam)
//        {
//            Viewport vp=null;
//            Camera3D cam = null;
//            if(!Viewport.TryGetTarget(out vp))
//            {
//                Gu.Log.Error("Frustum: Viewport was null.");
//            }
//            if (!Camera.TryGetTarget(out cam))
//            {
//                Gu.Log.Error("Frustum: Camera was null.");
//            }

//            float vpw_n = MathUtils.tanf(myCam.FOV / 2.0f) * Near;
//            float vph_n = vpw_n / ((float)vp.Width / (float)vp.Height);
//            float vpw_f = MathUtils.tanf(myCam.FOV / 2.0f) * Far;
//            float vph_f = vpw_f / ((float)vp.Width / (float)vp.Height);

//            Vec3f ncPos = cam.v3pos + cam.v3z * Near;
//            Vec3f left_top = ncPos - cam.v3x - cam.v3y;

//            pt.p0 = left_top + cam.v3x * cam.vpw * 2 * left + cam.v3y * cam.vph * 2 * top;

//        }
//    }
//}
