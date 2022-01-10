using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat4 = OpenTK.Matrix4;
namespace Oc
{

    public abstract class BaseCamera : BaseNode
    {
        private Vec3f _upVec;
        private Vec3f _viewVec;
        private CollisionSphere _mainVolume;

        //**Characteristics of Avatar - we should make the avatar class later.
        public Vec3f GetUpVec(){ return _upVec; }
        public Vec3f GetViewVec() { return _viewVec; }
        protected Viewport GetViewport(){ return GetRenderManager().GetViewport(); }
        public float GetNear() { return _near; }
        public float GetFar() { return _far; }
        public void SetView(Vec3f view) { _viewVec = view; }
        float _near = 1.0f;
        float _far = 1000.0f;

        public BaseCamera(RenderManager rm, NodeManager nm) : base(rm, nm)
        {
            _upVec = new Vec3f(0, 1, 0);
            _viewVec = new Vec3f(0, 0, -1);

             _mainVolume= new CollisionSphere(3.0f);
             CollisionVolumes.Add(_mainVolume);

            //DO not select camera (at least not active camera) since we wont be able to hit anything else.
             SelectEnabled = false;
        }
        public void Setup()
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            Gu.CheckGpuErrorsDbg();
            GL.Scissor(GetViewport().X, GetViewport().Y, GetViewport().Width, GetViewport().Height);
            Gu.CheckGpuErrorsDbg();
            GL.Viewport(GetViewport().X, GetViewport().Y, GetViewport().Width, GetViewport().Height); // Use all of the glControl painting area
            Gu.CheckGpuErrorsDbg();


            SetupProjectionMatrix();

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            Vec3f lookAtPos = GetPos() + _viewVec;
            Gu.CheckGpuErrorsDbg();

            Mat4 m = Mat4.LookAt(GetPos(), lookAtPos, _upVec);
            GL.LoadMatrix(ref m);
            Gu.CheckGpuErrorsDbg();
        }

        public abstract void SetupProjectionMatrix();
        public abstract PickRay ProjectPoint(Vec2f screenPoint);
        public override void Resize(Viewport vp) { }
        public override void Update(RenderManager rm) { }
        public override void Render(RenderManager rm) { }
        public override void Free() { }
        public override void UpdateBoundBox()
        {
            BoundBoxComputed._vmax = BoundBoxComputed._vmin = GetPos();
            BoundBoxComputed._vmax.X += _mainVolume._radius;
            BoundBoxComputed._vmax.Y += _mainVolume._radius;
            BoundBoxComputed._vmax.Z += _mainVolume._radius;
            BoundBoxComputed._vmin.X += -_mainVolume._radius;
            BoundBoxComputed._vmin.Y += -_mainVolume._radius;
            BoundBoxComputed._vmin.Z += -_mainVolume._radius;
        }


    }
}
