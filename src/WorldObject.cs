using System;
using System.Collections.Generic;

using Quat = OpenTK.Quaternion;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;

namespace PirateCraft
{
    public abstract class Constraint
    {
        public abstract void Apply(WorldObject ob);
    }
    public class InputConstraint
    {
        //Stuff drive input.
    }
    public class TrackToConstraint : Constraint
    {
        public bool Relative = false;
        public WorldObject LookAt = null;
        public Vec3f Up = new Vec3f(0, 1, 0);
        public TrackToConstraint(WorldObject ob, bool relative)
        {
            LookAt = ob;
            Relative = relative;
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
            Vec3f eye;
            if (!Relative)
            {
                eye = LookAt.Position - self.Position;
            }
            else
            {
                eye = LookAt.Position;
            }

            Vec3f zaxis = (eye).Normalized();
            Vec3f xaxis = Vec3f.Cross(Up, zaxis).Normalized();
            Vec3f yaxis = Vec3f.Cross(zaxis, xaxis);
            //Vec3f zaxis = (LookAt - eye).normalize();
            //Vec3f xaxis = zaxis.cross(Up).normalize();
            //Vec3f yaxis = xaxis.cross(zaxis);
            //zaxis*=-1;

            Mat4f mm = Mat4f.Identity;
            mm.M11 = xaxis.X; mm.M12 = yaxis.X; mm.M13 = zaxis.X;
            mm.M21 = xaxis.Y; mm.M22 = yaxis.Y; mm.M23 = zaxis.Y;
            mm.M31 = xaxis.Z; mm.M32 = yaxis.Z; mm.M33 = zaxis.Z;
            // mm = mm.Inverted();

            // self.Rotation = mm.ExtractRotation().ToAxisAngle();
        }
    }
    public class WorldObject
    {
        // public RotationType RotationType = RotationType.AxisAngle;

        private Quat _rotation = new Quat(0, 0, 0, 1); //Axis-Angle xyz,ang
        private Vec3f _scale = new Vec3f(1, 1, 1);
        private Vec3f _position = new Vec3f(0, 0, 0);
        private WorldObject _parent = null;
        private Mat4f _world = Mat4f.Identity;

        public bool TransformChanged { get; private set; } = false;
        public bool Hidden { get; set; } = false;
        public Box3f BoundBox { get; set; } = new Box3f(new Vec3f(0, 0, 0), new Vec3f(1, 1, 1));
        public WorldObject Parent { get { return _parent; } set { _parent = value; SetTransformChanged(); } }
        public List<WorldObject> Children { get; set; } = new List<WorldObject>();
        public Quat Rotation { get { return _rotation; } set { _rotation = value; SetTransformChanged(); } }//xyz,angle
        public Vec3f Scale { get { return _scale; } set { _scale = value; SetTransformChanged(); } }
        public Vec3f Position { get { return _position; } set { _position = value; SetTransformChanged(); } }
        public Mat4f Bind { get; private set; } = Mat4f.Identity; // Skinned Bind matrix
        public Mat4f InverseBind { get; private set; } = Mat4f.Identity; // Skinned Inverse Bind
        public Mat4f Local { get; private set; } = Mat4f.Identity;
        public Mat4f World { get { return _world; } set { _world = value; } }
        public Int64 LastUpdate { get; private set; } = 0;
        public List<Component> Components { get; private set; } = new List<Component>();
        public List<Constraint> Constraints { get; private set; } = new List<Constraint>();// *This is an ordered list they come in order

        public Vec3f BasisX { get { return World.Column0.Xyz; } private set { } }
        public Vec3f BasisY { get { return World.Column1.Xyz; } private set { } }
        public Vec3f BasisZ { get { return World.Column2.Xyz; } private set { } }

        public MeshData Mesh = null;
        public Material Material = null;

        //TODO: Clone

        public WorldObject(Vec3f pos)
        {
            Position = pos;
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
        public virtual void Update(Box3f parentBoundBox = null)
        {
            if (Hidden)
            {
                return;
            }
            ApplyConstraints();
            CompileLocalMatrix();
            ApplyParentMatrix();
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

            Mat4f mScl = Mat4f.CreateScale(Scale);
            Mat4f mRot = Mat4f.CreateFromQuaternion(Rotation);
            Mat4f mPos = Mat4f.CreateTranslation(Position);
            Local = (mScl) * (mRot) * mPos;
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
