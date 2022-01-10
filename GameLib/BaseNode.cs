using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;

namespace Oc
{
    public abstract class BaseNode
    {
        private static int _idGenerator = 0;

        private int _id;    // node id
        public Vec3f Pos;
        private Vec3f _vScale;
        private Vec3f _vRotation;
        private float _fRotation;
        private Box3f _pBoundBoxComputed = new Box3f();//Absolute coordinates of bound box.  This is after applying transforms.
        protected Box3f BoundBoxComputed
        {
            get
            {
                return _pBoundBoxComputed;
            }
            set
            {
                _pBoundBoxComputed = value;
            }
        }
        public List<CollisionVolume> CollisionVolumes = new List<CollisionVolume>();

        protected RenderManager _objRenderManager;
        protected NodeManager _objNodeManager;
        protected Int64 _iBoundBoxUpdateStamp = 0;

        private bool _blnSelectEnabled = true;
        public bool SelectEnabled
        {
            get { return _blnSelectEnabled; }
            set { _blnSelectEnabled = value; }
        }

        public int GetId() { return _id; }
        public Box3f GetBoundBox() { return _pBoundBoxComputed; }
        public Vec3f GetPos() { return Pos; }
        public void SetPos(Vec3f pos) { Pos = pos; }

        public Vec3f GetRotationVector() { return _vRotation; }
        public void SetRotationVector(Vec3f rot) { _vRotation = rot; }
        public Vec3f GetScale() { return _vScale; }
        public void SetScale(Vec3f scale) { _vScale = scale; }
        public float GetRotation() { return _fRotation; }
        public void SetRotation(float rot) { _fRotation = rot; }

        public abstract void Update(RenderManager rm);
        public abstract void Render(RenderManager rm);
        public abstract void UpdateBoundBox();
        public abstract void Free();    // free alld ata;
        public abstract void Resize(Viewport vp);

        public bool HitTestRay(PickRay xy)
        {
            //this isn't working probably because the input is async.
            //if(_iBoundBoxUpdateStamp!=_objRenderManager.GetFrameStamp())
            //    throw new Exception("Bound box was not updated this frame.");
            BoxAAHit baa = new BoxAAHit();

            bool b = _pBoundBoxComputed.LineOrRayIntersectInclusive_EasyOut(xy, ref baa);

            return b;
        }
        public RenderManager GetRenderManager() { return _objRenderManager; }

        public BaseNode(RenderManager rm, NodeManager om)
        {
            _id = ++_idGenerator;
            _objRenderManager = rm;
            _objNodeManager = om;
            _vScale = new Vec3f(1, 1, 1);
            _vRotation = new Vec3f(0, 1, 0);
            _objNodeManager.Constructed(this);
        }
        ~BaseNode()
        {
            _objNodeManager.Destructed(this);
        }

    }
}
