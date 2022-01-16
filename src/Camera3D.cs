using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat4f = OpenTK.Matrix4;

namespace PirateCraft
{

    public class Camera3D : BaseNode
    {
        private ProjectionMode ProjectionMode = ProjectionMode.Perspective;
        public Viewport Viewport { get; private set; } = null;
        public Frustum3D Frustum { get; set; } = null;
        float FOV { get; set; } = ((float)Math.PI / (float)6);

        //public mat4 ProjectionMatrix { get; private set;}
        //public mat4 ViewMatrix { get; private set;}

        public mat4 ProjectionMatrix { get; private set;}
        public mat4 ViewMatrix { get; private set;}

        public Camera3D(int w, int h, float near=1, float far=1000)
        {
            //DO not select camera (at least not active camera) since we wont be able to hit anything else.
            //SelectEnabled = false;
            Viewport = new Viewport(w,h);
            Frustum = new Frustum3D(Viewport,near,far);
        }
        public vec3 v3pos = new vec3(0, 0, -10);
        public vec3 v3x = new vec3(1, 0, 0);
        public vec3 v3y = new vec3(0, 1, 0);
        public vec3 v3z = new vec3(0,0,1);
        public void Update()
        {
            //GL.MatrixMode(MatrixMode.Projection);
            //GL.LoadIdentity();
            //Gu.CheckGpuErrorsDbg();
            //GL.Scissor(Viewport.X, Viewport.Y, Viewport.Width, Viewport.Height);
            //Gu.CheckGpuErrorsDbg();
            //GL.Viewport(Viewport.X, Viewport.Y, Viewport.Width, Viewport.Height); // Use all of the glControl painting area
            //Gu.CheckGpuErrorsDbg();

            //SetupProjectionMatrix();

            //GL.MatrixMode(MatrixMode.Modelview);
            //GL.LoadIdentity();
            //Vec3f lookAtPos = Pos + View;
            //Gu.CheckGpuErrorsDbg();
            ////TODO: track to constraint
            //Mat4 m = Mat4.LookAt(Pos, lookAtPos, new Vec3f(0,1,0));
            //GL.LoadMatrix(ref m);
            //Gu.CheckGpuErrorsDbg();

            ProjectionMatrix = mat4.projection(MathUtils.ToRadians(80.0f), 1920,1080,1,1000);
            ViewMatrix = mat4.getLookAt(this.v3pos, this.v3pos+this.v3z, v3y);
           // ViewMatrix = mat4.getRotation((float)Math.PI / 4.0f, new vec3(0, 1, 0))*
           // mat4.getTranslation(new vec3(10,0,-10));//* mat4.getRotation((float)Math.PI/2.0f,new vec3(0,1,0));

            //ViewMatrix = Mat4f.LookAt(new Vec3f(0,10,-10), new Vec3f(0,0,0), new Vec3f(0, 1, 0));
            //ProjectionMatrix = Mat4f.CreatePerspectiveFieldOfView(MathUtils.ToRadians(70.0f),1920.0f/ 1080.0f, 1.0f, 1000.0f);

        }
        public override void Resize(Viewport vp) { }
        public override void Update(double dt) { base.Update(dt); }
        public override void Render(Renderer rm) { }
        public override void Free() { }
        public override void UpdateBoundBox()
        {
            //BoundBoxComputed._vmax = BoundBoxComputed._vmin = Pos;
            //BoundBoxComputed._vmax.X += _mainVolume._radius;
            //BoundBoxComputed._vmax.Y += _mainVolume._radius;
            //BoundBoxComputed._vmax.Z += _mainVolume._radius;
            //BoundBoxComputed._vmin.X += -_mainVolume._radius;
            //BoundBoxComputed._vmin.Y += -_mainVolume._radius;
            //BoundBoxComputed._vmin.Z += -_mainVolume._radius;
        }
        public  PickRay ProjectPoint(Vec2f mouse)
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
