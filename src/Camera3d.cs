using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;

namespace PirateCraft
{
    public class Camera3d : BaseCamera
    {

        public Camera3d(int w, int h, float n, float f) : base(w,h,n,f)
        {
        }
        public override void SetupProjectionMatrix()
        {

            // ** Create a centered Orthogonal viewport - center of screen is xy=0
            //Gu.CheckGpuErrorsDbg();
            //int w2 = Viewport.Width / 2;
            //Gu.CheckGpuErrorsDbg();
            //int h2 = Viewport.Height / 2;

            //float arat = (float)w2 / (float)h2;
            //float vpWidth_2 = (float)Math.Tan((double)(FOV/2.0f)) * Near;

            //GL.Frustum(-vpWidth_2, vpWidth_2, -vpWidth_2*arat, vpWidth_2*arat, Near, Far);
            //Gu.CheckGpuErrorsDbg();
        }
        public override PickRay ProjectPoint(Vec2f mouse)
        {
            PickRay pt = new PickRay();
            //Vec2f screen = screenPoint;

            ////translate to center
            //screen.X -= (float)GetViewport().Width / 2.0f;
            //screen.Y = (float)GetViewport().Height / 2.0f - screen.Y;

            ////Get up /rioght
            //Vec3f right = Vec3f.Cross(GetViewVec(), GetUpVec());
            //Vec3f up = GetUpVec();

            //// Get screen world center position
            //Vec3f ncPos = GetPos() + GetViewVec() * GetNear();

            //pt.Origin = ncPos + right * screen.X + up * screen.Y;
            //pt._length = GetFar();

            //pt.Dir = GetViewVec();
            //pt.Opt();

            //return pt;
            ///*
            //    //update();
	//_p_pMainFrustum->update(getView(),getPos(),getUp());
	//ProjectedRay pr;
            //float wratio = (float)((float)mouse.X/(float)GetViewport().Width());
            //float hratio = (float)((float)mouse.Y/(float)GetViewport().Height());

            //Vec3f dp1 = _pMainFrustum->PointAt(fpt_ntr) - _pMainFrustum->PointAt(fpt_ntl);
            //Vec3f dp2 = _pMainFrustum->PointAt(fpt_nbl) - _pMainFrustum->PointAt(fpt_ntl);

            //dp1*=wratio;
            //dp2*=hratio;

            //pr.Origin = _pMainFrustum->PointAt(fpt_ntl)+dp1+dp2;

            //dp1 = _pMainFrustum->PointAt(fpt_ftr) - _pMainFrustum->PointAt(fpt_ftl);
            //dp2 = _pMainFrustum->PointAt(fpt_fbl) - _pMainFrustum->PointAt(fpt_ftl);

            //dp1*=wratio;
            //dp2*=hratio;

            //pr.Dir = _pMainFrustum->PointAt(fpt_ftl) + dp1 + dp2 - pr.Origin;

            //pr.opt();

            return pt;

        }
    }
}
