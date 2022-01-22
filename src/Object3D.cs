using System;
using System.Collections.Generic;

namespace PirateCraft
{
    public class WorldObject
    {
        private vec4 _rotation = new vec4(0, 0, 0, 1);
        private vec3 _scale = new vec3(1, 1, 1);
        private vec3 _position = new vec3(0, 0, 0);
        private WorldObject _parent = null;

        public bool TransformChanged { get; private set; } = false;
        public bool Hidden { get; set; } = false;
        public Box3f BoundBox { get; set; } = new Box3f(new vec3(0, 0, 0), new vec3(1, 1, 1));
        public WorldObject Parent { get { return _parent; } set { _parent = value; SetTransformChanged(); } }
        public List<WorldObject> Children { get; set; } = new List<WorldObject>();
       public vec4 Rotation { get { return _rotation;} set { _rotation = value; SetTransformChanged(); } }//xyz,angle
        public vec3 Scale { get { return _scale; } set { _scale = value; SetTransformChanged(); } }
        public vec3 Position { get { return _position; } set { _position = value; SetTransformChanged(); } }
        public mat4 Bind { get; private set; } = mat4.Identity(); // Skinned Bind matrix
        public mat4 InverseBind { get; private set; } = mat4.Identity(); // Skinned Inverse Bind
        public mat4 Local { get; private set; } = mat4.Identity();
        public mat4 World { get; private set; } = mat4.Identity();
        public Int64 LastUpdate { get; private set; } = 0;
        public List<Component> Components { get; private set; } = new List<Component>();

        //TODO: Clone

        public WorldObject()
        {

        }
        public void SetTransformChanged()
        {
            TransformChanged=true;
        }
        public void Update(Box3f? parentBoundBox=null)
        {
            if (!Hidden)
            {
                return;
            }
            CompileLocalMatrix();
            ApplyParentMatrix();
            UpdateComponents();

            foreach(var child in this.Children)
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

            mat4 mPos = mat4.GetTranslation(Position);
            mat4 mRot = mat4.GetRotation(Rotation.w, Rotation.xyz());
            mat4 mScl = mat4.getScale(Scale);
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
