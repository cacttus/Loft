using System;
using System.Collections.Generic;

namespace PirateCraft
{
    public abstract class Constraint
    {
        public abstract void Apply(WorldObject ob);
    }
    public class TrackToConstraint : Constraint
    {
        public bool Relative = false;    
        public WorldObject LookAt = null;
        public vec3 Up = new vec3(0, 1, 0);
        public TrackToConstraint(WorldObject ob, bool relative)
        {
            LookAt = ob;
            Relative=relative;
        }
        public override void Apply(WorldObject self)
        {
            //Technically we should apply constraints right?
            //empty is a child of camera
            //compile world matrix children
            //compile world matrix parents
            //apply xforms to children
            //apply xforms to children
            //apply constraints to parents
            //apply constraitns to children
            vec3 eye;   
            if (!Relative)
            {
                eye = LookAt.Position - self.Position;
            }
            else
            {
                eye = LookAt.Position;
            }

            vec3 zaxis = (eye).normalize();
            vec3 xaxis = Up.cross(zaxis).normalize();
            vec3 yaxis = zaxis.cross(xaxis);
            //vec3 zaxis = (LookAt - eye).normalize();
            //vec3 xaxis = zaxis.cross(Up).normalize();
            //vec3 yaxis = xaxis.cross(zaxis);
            //zaxis*=-1;

            mat4 mm = mat4.Identity();
            mm._m11 = xaxis.x; mm._m12 = yaxis.x; mm._m13 = zaxis.x;
            mm._m21 = xaxis.y; mm._m22 = yaxis.y; mm._m23 = zaxis.y;
            mm._m31 = xaxis.z; mm._m32 = yaxis.z; mm._m33 = zaxis.z;

            self.Rotation = mm.GetQuaternion().toAxisAngle();
        }
    }

    public class WorldObject
    {
        private vec4 _rotation = new vec4(0, 1, 0, 0); //Axis-Angle xyz,ang
        private vec3 _scale = new vec3(1, 1, 1);
        private vec3 _position = new vec3(0, 0, 0);
        private WorldObject _parent = null;
        private mat4 _world = mat4.Identity();

        public bool TransformChanged { get; private set; } = false;
        public bool Hidden { get; set; } = false;
        public Box3f BoundBox { get; set; } = new Box3f(new vec3(0, 0, 0), new vec3(1, 1, 1));
        public WorldObject Parent { get { return _parent; } set { _parent = value; SetTransformChanged(); } }
        public List<WorldObject> Children { get; set; } = new List<WorldObject>();
        public vec4 Rotation { get { return _rotation; } set { _rotation = value; SetTransformChanged(); } }//xyz,angle
        public vec3 Scale { get { return _scale; } set { _scale = value; SetTransformChanged(); } }
        public vec3 Position { get { return _position; } set { _position = value; SetTransformChanged(); } }
        public mat4 Bind { get; private set; } = mat4.Identity(); // Skinned Bind matrix
        public mat4 InverseBind { get; private set; } = mat4.Identity(); // Skinned Inverse Bind
        public mat4 Local { get; private set; } = mat4.Identity();
        public mat4 World { get { return _world; } set { _world = value; } }
        public Int64 LastUpdate { get; private set; } = 0;
        public List<Component> Components { get; private set; } = new List<Component>();
        public List<Constraint> Constraints { get; private set; } = new List<Constraint>();// *This is an ordered list they come in order

        public vec3 BasisX { get; private set; } = new vec3(1, 0, 0);
        public vec3 BasisY { get; private set; } = new vec3(0, 1, 0);
        public vec3 BasisZ { get; private set; } = new vec3(0, 0, 1);

        public MeshData Mesh = null;
        public Material Material = null;

        //TODO: Clone

        public WorldObject(vec3 pos)
        {
            Position=pos;
        }
        public WorldObject()
        {

        }
        public void SetTransformChanged()
        {
            TransformChanged = true;
        }
        public void ApplyConstraints()
        {
            foreach (var c in Constraints)
            {
                c.Apply(this);
            }
        }
        public void ConstructBasis()
        {
            BasisX = (World * new vec4(1, 0, 0, 0)).xyz().normalize();
            BasisY = (World * new vec4(0, 1, 0, 0)).xyz().normalize();
            BasisZ = (World * new vec4(0, 0, 1, 0)).xyz().normalize();
        }
        public virtual void Update(Box3f parentBoundBox = null)
        {
            if (Hidden)
            {
                return;
            }
            ApplyConstraints();
            CompileLocalMatrix();
            ApplyParentMatrix();
            ConstructBasis();
            UpdateComponents();
            foreach (var child in this.Children)
            {
                child.Update();
            }

            // TODO: Calc bound box

            LastUpdate = Gu.Microseconds();
        }
        public void CompileLocalMatrix()
        {
            if (TransformChanged == false)
            {
                return;
            }

            mat4 mScl = mat4.getScale(Scale);
            mat4 mRot = mat4.GetRotation(Rotation.w, Rotation.xyz());
            mat4 mPos = mat4.GetTranslation(Position);
            Local = mScl * mRot * mPos;
        }
        public void ApplyParentMatrix()
        {
            //TODO: Parent types
            //if isBoneNode
            if (Parent != null)
            {
                World = Local * Parent.World;
            }
            else
            {
                World = Local;
            }
        }
        public void UpdateComponents()
        {
            // TODO:
        }

    }
}
