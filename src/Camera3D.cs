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

    public class Camera3D : WorldObject
    {
        float _fov = MathUtils.ToRadians(80.0f);
        float _near = 1;
        float _far = 1000;
        bool _updating = false;
        private float _widthNear = 1;
        private float _heightNear = 1;
        private float _widthFar = 1;
        private float _heightFar = 1;

        //TODO: turn these into the object data.

        //  vec3 _v3pos = new vec3(0, 0, -10);
        //  vec3 _v3x = new vec3(1, 0, 0); //These are the basis vectors, please turn into object data later
        // vec3 _v3y = new vec3(0, 1, 0);
        //  vec3 _v3z = new vec3(0, 0, 1);

        // public vec3 v3pos { get { return _v3pos; } set { _v3pos = value; SetDirty(); } }
        // public vec3 v3x { get { return _v3x; } private set { _v3x = value; SetDirty(); } }  //These are the basis vectors, please turn into object data later
        // public vec3 v3y { get { return _v3y; } private set { _v3y = value; SetDirty(); } }
        // public vec3 v3z { get { return _v3z; } private set { _v3z = value; SetDirty(); } }
        bool _dirty = true;
        public void SetDirty()
        {
            _dirty = true;
        }
        public bool Dirty()
        {
            return _dirty;
        }

        Viewport _viewport = null;
        vec3 _nearCenter = new vec3(0, 0, 0);
        vec3 _farCenter = new vec3(0, 0, 0);
        vec3 _nearTopLeft = new vec3(0, 0, 0);
        vec3 _farTopLeft = new vec3(0, 0, 0);
        mat4 _projectionMatrix = mat4.Identity();
        mat4 _viewMatrix = mat4.Identity();
        ProjectionMode ProjectionMode = ProjectionMode.Perspective;

        public float FOV { get { return _fov; } set { _fov = value; SetDirty(); } }
        public float Near { get { return _near; } private set { _near = value; SetDirty(); } }
        public float Far { get { return _far; } private set { _far = value; SetDirty(); } }
        public Viewport Viewport { get { return _viewport; } private set { _viewport = value; SetDirty(); } }
        public vec3 NearCenter { get { return _nearCenter; } private set { _nearCenter = value; SetDirty(); } }
        public vec3 FarCenter { get { return _farCenter; } private set { _farCenter = value; SetDirty(); } }
        public vec3 NearTopLeft { get { return _nearTopLeft; } private set { _nearTopLeft = value; SetDirty(); } }
        public vec3 FarTopLeft { get { return _farTopLeft; } private set { _farTopLeft = value; SetDirty(); } }
        public mat4 ProjectionMatrix { get { return _projectionMatrix; } private set { _projectionMatrix = value; SetDirty(); } }
        public mat4 ViewMatrix { get { return _viewMatrix; } private set { _viewMatrix = value; SetDirty(); } }

        public Camera3D(int w, int h, float near = 1, float far = 1000)
        {
            //Do not select camera (at least not active camera) since we wont be able to hit anything else.
            //SelectEnabled = false;
            Viewport = new Viewport(w, h, this);
        }
        public override void Update(Box3f parentBoundBox = null)
        {
            base.Update(parentBoundBox);
            //if (_dirty && !_updating)
            //{
            //_updating = true;
            //{

            ProjectionMatrix = mat4.projection(FOV, Viewport.Width, Viewport.Height, Near, Far);
            ViewMatrix = World; //mat4.getLookAt(Position, LookAt, up);

            //Frustum
            float tanfov2 = MathUtils.tanf(FOV / 2.0f);
            float ar = ((float)Viewport.Width / (float)Viewport.Height);

            //tan(fov2) = w2/near
            //tan(fov2) * near = w2
            //w/h = w2/h2
            //(w/h)*h2 = w2
            //w2/(w/h) = h2
            _widthNear = tanfov2 * Near * 2;
            _heightNear = _widthNear / ar;
            _widthFar = tanfov2 * Far * 2;
            _heightFar = _widthFar / ar;

            NearCenter = Position + BasisZ * Near;
            FarCenter = Position + BasisZ * Far;
            NearTopLeft = NearCenter - BasisX * _widthNear + BasisY * _heightNear;
            FarTopLeft = FarCenter - BasisX * _widthFar + BasisY * _heightFar;

            //    }
            //    _updating = false;
            //}
            //_dirty = false;
        }
        //public override void Resize(Viewport vp) { }
        //public override void Update(double dt) { base.Update(dt); }
        //public override void Render(Renderer rm) { }
        //public override void Free() { }
        //public override void UpdateBoundBox()
        //{
        //    //BoundBoxComputed._vmax = BoundBoxComputed._vmin = Pos;
        //    //BoundBoxComputed._vmax.X += _mainVolume._radius;
        //    //BoundBoxComputed._vmax.Y += _mainVolume._radius;
        //    //BoundBoxComputed._vmax.Z += _mainVolume._radius;
        //    //BoundBoxComputed._vmin.X += -_mainVolume._radius;
        //    //BoundBoxComputed._vmin.Y += -_mainVolume._radius;
        //    //BoundBoxComputed._vmin.Z += -_mainVolume._radius;
        //}

        public Line3f ProjectPoint(vec2 point_on_screen, TransformSpace space = TransformSpace.World, float additionalZDepthNear = 0)
        {
            //Note: we were using PickRay before because that's used to pick OOBs. We don't need that right now but we will in the future.
            Line3f pt = new Line3f();

            float left_pct = point_on_screen.x / (float)Viewport.Width;
            float top_pct = (point_on_screen.y) / (float)Viewport.Height;

            if (space == TransformSpace.Local)
            {
                //Transform in local coordinates.
                vec3 localX = new vec3(1, 0, 0);
                vec3 localY = new vec3(0, 1, 0);
                vec3 localZ = new vec3(0, 0, 1);
                vec3 near_center_local = localZ * Near;
                vec3 far_center_local = localZ * Far;
                vec3 ntl = near_center_local - localX * _widthNear + localY * _heightNear;
                vec3 ftl = far_center_local - localX * _widthFar + localY * _heightFar;
                pt.p0 = ntl + localX * _widthNear * left_pct + localY * _heightNear * top_pct;
                pt.p1 = ftl + localX * _widthFar * left_pct + localY * _heightFar * top_pct;
                pt.p0 += localZ * additionalZDepthNear;
            }
            else
            {
                pt.p0 = NearTopLeft + BasisX * _widthNear * left_pct + BasisY * _heightNear * top_pct;
                pt.p1 = FarTopLeft + BasisX * _widthFar * left_pct + BasisY * _heightFar * top_pct;
                pt.p0 += BasisZ * additionalZDepthNear;
            }

            return pt;
        }

    }
}
