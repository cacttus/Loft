using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;

namespace Oc
{
    public class CameraOrtho2d : BaseCamera
    {
        public CameraOrtho2d(RenderManager rm, NodeManager nm)
            : base(rm, nm)
        {
        }
        public override void SetupProjectionMatrix()
        {
            // ** Create a centered Orthogonal viewport - center of screen is xy=0
            Gu.CheckGpuErrorsDbg();
            int w2 = GetViewport().Width / 2;
            Gu.CheckGpuErrorsDbg();
            int h2 = GetViewport().Height / 2;
            GL.Ortho(-w2, w2, -h2, h2, GetNear(), GetFar()); // Bottom-left corner pixel has coordinate (0, 0)
            Gu.CheckGpuErrorsDbg();
        }
        public override PickRay ProjectPoint(Vec2f screenPoint)
        {
            PickRay pt = new PickRay();
            Vec2f screen = screenPoint;

            //translate to center
            screen.X -= (float)GetViewport().Width / 2.0f;
            screen.Y = (float)GetViewport().Height / 2.0f - screen.Y;

            //Get up /rioght
            Vec3f right = Vec3f.Cross(GetViewVec(), GetUpVec());
            Vec3f up = GetUpVec();

            // Get screen world center position
            Vec3f ncPos = GetPos() + GetViewVec() * GetNear();

            pt.Origin = ncPos + right * screen.X + up * screen.Y;
            pt._length = GetFar();

            pt.Dir = GetViewVec();
            pt.Opt();

            return pt;
        }

    }
}
