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

namespace Oc
{
    public class Camera3d : BaseCamera
    {
        float _f_hfov;//horiz fov
        float _f_tan_fov_2;

        public float getFOV() 
        { 
            return _f_hfov; 
        }
        public void setFOV( float fov ) 
        { 
            _f_hfov = fov; 
            _f_tan_fov_2 = (float)Math.Tan((double)_f_hfov/2.0f); 
        }	

        public Camera3d(RenderManager rm, NodeManager nm)
            : base(rm, nm)
        {
            setFOV((float)Math.PI / (float)6);	// - Horizontal Field of view
        }
        public override void SetupProjectionMatrix()
        {
            // ** Create a centered Orthogonal viewport - center of screen is xy=0
            Gu.CheckGpuErrorsDbg();
            int w2 = GetViewport().Width / 2;
            Gu.CheckGpuErrorsDbg();
            int h2 = GetViewport().Height / 2;

            float arat = (float)w2 / (float)h2;
            float vpWidth_2 = _f_tan_fov_2 * GetNear();

            GL.Frustum(-vpWidth_2, vpWidth_2, -vpWidth_2*arat, vpWidth_2*arat, GetNear(), GetFar());
            Gu.CheckGpuErrorsDbg();
            
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
