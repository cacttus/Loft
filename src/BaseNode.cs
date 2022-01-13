using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat4f = OpenTK.Matrix4;

namespace PirateCraft
{
    public abstract class BaseNode
    {
        private static int _idGenerator = 0;
        protected Int64 _iBoundBoxUpdateStamp = 0;
        protected Int64 _transformUpdateStamp = 0;

        Vec3f _pos = new Vec3f(0, 0, 0);
        Vec4f _rot = new Vec4f(0, 1, 0, 0);
        Vec3f _scale = new Vec3f(0, 0, 0);

        Vec3f _cachedView = new Vec3f(0,0,-1);

        public int Id { get; private set; } = ++_idGenerator;    // node id
        public Vec3f Pos { get { return _pos; } set { _pos = value; UpdateTransform(); } }
        public Vec4f Rotation { get { return _rot; } set { _rot = value; UpdateTransform(); } }
        public Vec3f Scale { get { return _scale; } set { _scale = value; UpdateTransform(); } }

        public OpenTK.Matrix4 Transform { get; private set; } = OpenTK.Matrix4.Identity;
        public Box3f BoundBoxComputed { get; private set; } = new Box3f();//Absolute coordinates of bound box.  This is after applying transforms.
        private bool SelectEnabled { get; set; } = true;

        public OpenTK.Matrix4 MatWorld { get; private set; }
        public Vec3f ViewNormal { get { 
               // if(_transformUpdateStamp!=)
        return (MatWorld * new Vec4f(0,0,-1,0)).Xyz.Normalized(); 
        } }

        public BaseNode()
        {
        }
        public virtual void Update(double dt)
        {
            UpdateTransform();
            UpdateBoundBox();
        }
        public virtual void UpdateBoundBox()
        {
            //  BoundBoxComputed.
        }
        public virtual void Render(Renderer rm) { }
        public virtual void Free() { }    // free alld ata;
        public virtual void Resize(Viewport vp) { }

        private void UpdateTransform()
        {
            var xx = new vec4(Pos.X, Pos.Y, Pos.Z, 1);
            Transform = OpenTK.Matrix4.Identity;
            
            //mat4 m = mat4.identity();
            //mat4
            //mat4 m2 = xx*m;
            //Transform = xx*Transform;
           // _transformUpdateStamp = Context;
        }
        public bool HitTestRay(PickRay xy)
        {
            //this isn't working probably because the input is async.
            //if(_iBoundBoxUpdateStamp!=_objRenderManager.GetFrameStamp())
            //    throw new Exception("Bound box was not updated this frame.");
            BoxAAHit baa = new BoxAAHit();

            bool b = BoundBoxComputed.LineOrRayIntersectInclusive_EasyOut(xy, ref baa);

            return b;
        }

        //~BaseNode()
        //{
        //}

    }
}
