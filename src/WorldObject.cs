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
   public class Component
   {
      public Component()
      {
      }
      public virtual void Update(double dt, WorldObject myObj) { }
   }
   public class MeshComponent : Component
   {
      MeshData MeshData { get; set; }//Data blocks can be shared.
      public override void Update(double dt, WorldObject myObj) { }
   }
   public enum KeyframeInterpolation
   {
      Constant,
      Linear,
      Ease,
      Cubic,
      Slerp,
   }
   //This is a Q&D animation system for realtime animations.
   public class Keyframe
   {
      public double Time = 0;
      public Quat Rot { get; set; } = Quat.Identity;
      public Vec3f Pos { get; set; } = new Vec3f(0, 0, 0);
      public Vec3f Scale { get; set; } = new Vec3f(1, 1, 1);
      public KeyframeInterpolation PosInterp { get; private set; } = KeyframeInterpolation.Cubic;
      public KeyframeInterpolation RotInterp { get; private set; } = KeyframeInterpolation.Slerp;
      public KeyframeInterpolation SclInterp { get; private set; } = KeyframeInterpolation.Cubic;
      public Keyframe(double time, Quat quat, KeyframeInterpolation rotInt, Vec3f pos, KeyframeInterpolation posInt)
      {
         Rot = quat;
         Pos = pos;
         Time = time;
         RotInterp = rotInt;
         PosInterp = posInt;
      }
      public Keyframe(double time, Quat quat, Vec3f pos)
      {
         Rot = quat;
         Pos = pos;
         Time = time;
      }
      public Keyframe(double time, Quat quat)
      {
         Rot = quat;
         Time = time;
      }
      public Keyframe(double time, Vec3f pos)
      {
         Pos = pos;
         Time = time;
      }
      public Keyframe() { }
   }
   public enum AnimationState
   {
      Paused, Playing, Stopped
   }
   public class AnimationComponent : Component
   {
      public AnimationState AnimationState { get; private set; } = AnimationState.Stopped;
      public double Time { get; private set; } = 0;//Seconds
      public double MaxTime
      {
         //Note: Keyframes should be sorted.
         get
         {
            if (KeyFrames.Count == 0)
            {
               return 0;
            }
            return KeyFrames[KeyFrames.Count - 1].Time;
         }
      }
      public double MinTime
      {
         get
         {
            if (KeyFrames.Count == 0)
            {
               return 0;
            }
            return KeyFrames[0].Time;
         }
      }
      public Keyframe Current { get; private set; } = new Keyframe(); // Current interpolated Keyframe
      public List<Keyframe> KeyFrames { get; private set; } = new List<Keyframe>(); //This must be sorted by Time

      public override void Update(double dt, WorldObject myObj)
      {
         Time += dt;
         Time = Time % MaxTime;

         if (AnimationState == AnimationState.Playing)
         {
            SlerpFrames();
         }

         myObj.AnimatedPosition = Current.Pos;
         myObj.AnimatedRotation = Current.Rot;
         myObj.AnimatedScale = Current.Scale;

         //Mat4f mScl = Mat4f.CreateScale(Current.Scale);
         //Mat4f mRot = Mat4f.CreateFromQuaternion(Current.Rot);
         //Mat4f mPos = Mat4f.CreateTranslation(Current.Pos);
         //   myObj.Local = myObj.Local * (mScl) * (mRot) * mPos;

         //TODO: put this in the keyframe when modified.
         //NormalizeState();
      }
      private void NormalizeState()
      {
         if (KeyFrames.Count > 0)
         {
            //foreach(var k in KeyFrames)
            {
               //TODO: find equal keyframe times as this could be an error.
               // Gu.Log.Warn("Keyframes had equal times.");
            }
         }
      }
      public void Start()
      {
         KeyFrames.Sort((x, y) => x.Time.CompareTo(y.Time));
         AnimationState = AnimationState.Playing;
      }
      public void Pause()
      {
         AnimationState = AnimationState.Paused;
      }
      private void SlerpFrames()
      {
         if (KeyFrames.Count == 0)
         {
            //No animation.
         }
         else if (KeyFrames.Count == 1)
         {
            //Set to this key frame.
            Current = KeyFrames[0];
         }
         else
         {
            //Grab 0 and 1
            Keyframe f0 = null, f1 = null;
            for (int i = 0; i < KeyFrames.Count - 1; i++)
            {
               var k0 = KeyFrames[i];
               var k1 = KeyFrames[i + 1];
               if (k0.Time <= Time && k1.Time >= Time)
               {
                  f0 = k0;
                  f1 = k1;
                  break;
               }
            }
            if (f0 == null || f1 == null)
            {
               //This should not be possible as the MaxTime must be the last of the keyframes.
               if (Time > KeyFrames[KeyFrames.Count - 1].Time)
               {
                  f0 = KeyFrames[KeyFrames.Count - 1];
                  f1 = f0;
               }
               else //if (Time < KeyFrames[0].Time)
               {
                  f0 = KeyFrames[0];
                  f1 = f0;
               }
            }

            Gu.Assert(f0 != null && f1 != null);
            double denom = f1.Time - f0.Time;
            double slerpTime = 0;
            if (denom == 0)
            {
               slerpTime = 1; //If f0 and f1 are euqal in time it's an error.
            }
            else
            {
               slerpTime = (Time - f0.Time) / denom;
            }

            //Ok, now slerp it up
            Current.Rot = Quat.Slerp(f0.Rot, f1.Rot, (float)slerpTime);
            Current.Pos = InterpolateV3(f1.PosInterp, f0.Pos, f1.Pos, slerpTime);
            Current.Scale = InterpolateV3(f1.SclInterp, f0.Scale, f1.Scale, slerpTime);
         }

      }
      private Vec3f InterpolateV3(KeyframeInterpolation interp, Vec3f f0, Vec3f f1, double slerpTime)
      {
         if (interp == KeyframeInterpolation.Linear)
         {
            return LinearInterpolate(f0, f1, slerpTime);
         }
         else if (interp == KeyframeInterpolation.Cubic)
         {
            return CubicInterpolate(f0, f1, slerpTime);
         }
         else if (interp == KeyframeInterpolation.Ease)
         {
            return EaseInterpolate(f0, f1, slerpTime);
         }
         else if (interp == KeyframeInterpolation.Constant)
         {
            return ConstantInterpolate(f0, f1, slerpTime);
         }
         Gu.BRThrowNotImplementedException();
         return f0;
      }
      private Vec3f LinearInterpolate(Vec3f a, Vec3f b, double time)
      {
         Vec3f ret = a + (b - a) * (float)time;
         return ret;
      }
      private Vec3f EaseInterpolate(Vec3f a, Vec3f b, double time)
      {
         //Sigmoid "ease"
         //Assuming time is normalized [0,1]
         double k = 0.1; //Slope
         double f = 1 / (1 + Math.Exp(-((time - 0.5) / k)));
         return a * (1 - (float)f) + b * (float)f;
      }
      private Vec3f CubicInterpolate(Vec3f a, Vec3f b, double time)
      {
         //This is actually cosine interpolate. We need to update this to be cubic.
         //TODO:
         double ft = time * Math.PI;
         double f = (1.0 - Math.Cos(ft)) * 0.5;
         return a * (1 - (float)f) + b * (float)f;
      }
      private Vec3f ConstantInterpolate(Vec3f a, Vec3f b, double time)
      {
         Vec3f ret = a;
         return ret;
      }
   }
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
      //*This does not work correctly.
      //Essentially it would set the camera object's world matrix, but it doesn't wrok.
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
   /// <summary>
   /// This is the main object that stores matrix for pos/rot/scale, and components for mesh, sound, script .. etc. GameObject in Unity.
   /// </summary>
   public class WorldObject
   {
      // public RotationType RotationType = RotationType.AxisAngle;
      string _name = "<Unnamed>";
      public string Name
      {
         get { return _name; }
         set
         {
            _name = value;
         }
      }

      private Quat _rotation = new Quat(0, 0, 0, 1); //Axis-Angle xyz,ang
      private Vec3f _scale = new Vec3f(1, 1, 1);
      private Vec3f _position = new Vec3f(0, 0, 0);
      private WorldObject _parent = null;
      private Mat4f _world = Mat4f.Identity;

      public Vec4f Color { get; set; } = new Vec4f(1, 1, 1, 1); // Mesh color if no material
      public bool TransformChanged { get; private set; } = false;
      public bool Hidden { get; set; } = false;
      public Box3f BoundBox { get; set; } = new Box3f(new Vec3f(0, 0, 0), new Vec3f(1, 1, 1));
      public WorldObject Parent { get { return _parent; } private set { _parent = value; SetTransformChanged(); } }
      public List<WorldObject> Children { get; private set; } = new List<WorldObject>();

      public void AddChild(WorldObject child)
      {
         if (child.Parent != null)
         {
            child.Parent.RemoveChild(child);
         }
         Children.Add(child);
         child.Parent = this;
      }
      public void RemoveChild(WorldObject child)
      {
         Children.Remove(child);
         child.Parent = null;
      }

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

      //TODO: make this part of animation component / apply animation in the component update.
      public Quat AnimatedRotation { get; set; } = Quat.Identity;
      public Vec3f AnimatedScale { get; set; } = new Vec3f(1, 1, 1);
      public Vec3f AnimatedPosition { get; set; } = new Vec3f(0, 0, 0);

      public Vec3f BasisX { get { return (World * new Vec4f(1, 0, 0, 0)).Xyz.Normalized(); } private set { } }
      public Vec3f BasisY { get { return (World * new Vec4f(0, 1, 0, 0)).Xyz.Normalized(); } private set { } }
      public Vec3f BasisZ { get { return (World * new Vec4f(0, 0, 1, 0)).Xyz.Normalized(); } private set { } }

      public MeshData Mesh = null;
      public Material Material = null;

      //TODO: Clone

      public WorldObject(Vec3f pos) : base()
      {
         Position = pos;
      }
      public WorldObject()
      {
         Color = Random.NextVec4(new Vec4f(0.3f,0.3f,0.3f,1), new Vec4f(1,1,1,1));
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
      public virtual void Update(double dt, Box3f? parentBoundBox = null)
      {
         if (Hidden)
         {
            return;
         }
         UpdateComponents(dt);
         ApplyConstraints();
         CompileLocalMatrix();
         ApplyParentMatrix();
         foreach (var child in this.Children)
         {
            child.Update(dt);
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

         Mat4f mSclA = Mat4f.CreateScale(AnimatedScale);
         Mat4f mRotA = Mat4f.CreateFromQuaternion(AnimatedRotation);
         Mat4f mPosA = Mat4f.CreateTranslation(AnimatedPosition);

         Mat4f mScl = Mat4f.CreateScale(Scale);
         Mat4f mRot = Mat4f.CreateFromQuaternion(Rotation);
         Mat4f mPos = Mat4f.CreateTranslation(Position);
         Local = (mScl * mSclA) * (mRot * mRotA) * (mPos * mPosA);
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
      public void UpdateComponents(double dt)
      {
         // TODO:
         foreach (var cmp in Components)
         {
            cmp.Update(dt, this);
         }
      }

   }
}
