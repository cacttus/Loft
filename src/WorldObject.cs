using System;
using System.Collections.Generic;

namespace PirateCraft
{
  public abstract class Component : Cloneable<Component>
  {
    public Component()
    {
    }
    public abstract void Update(double dt, WorldObject myObj);
  }
  //public class MeshComponent : Component
  //{
  //  MeshData MeshData { get; set; }//Data blocks can be shared.
  //  public override void Update(double dt, WorldObject myObj) { }
  //}
  public class EventComponent : Component
  {
    public ActionState State { get; private set; } = ActionState.Stop;
    public double Frequency { get; private set; } = 0;
    public double Time { get; private set; } = 0;
    public Action<WorldObject>? Action { get; set; } = null;
    public bool Repeat { get; set; } = false;
    private EventComponent() { }
    public EventComponent(Action<WorldObject>? action, double tick_seconds, bool repeat)
    {
      Frequency = tick_seconds;
      Action = action;
      Repeat = repeat;
    }
    public override void Update(double dt, WorldObject myObj)
    {
      if (State != ActionState.Stop)
      {
        Time += dt;
        while (Time > Frequency)
        {
          Time -= Frequency;
          if (Action != null)
          {
            Action.Invoke(myObj);
          }
        }
      }
    }
    public override Component Clone(bool shallow = true)
    {
      EventComponent other = new EventComponent();
      other.State = this.State;
      other.Frequency = this.Frequency;
      other.Time = this.Time;
      other.Action = this.Action;
      other.Repeat = this.Repeat;
      return other;
    }
    public void Start()
    {
      State = ActionState.Run;
    }
    public void Stop()
    {
      State = ActionState.Stop;
    }
  }
  public class PhysicsComponent : Component
  {
    public vec3 Velocity = new vec3(0, 0, 0);
    public bool HasGravity = false;
    public bool Collides = false;
    public override void Update(double dt, WorldObject myObj)
    {
      myObj.Position_Local += Velocity;
    }
    public override Component Clone(bool shallow = true)
    {
      PhysicsComponent c = new PhysicsComponent();

      c.Velocity = this.Velocity;
      c.HasGravity = this.HasGravity;
      c.Collides = this.Collides;

      return c;
    }
  }
  public enum KeyframeInterpolation
  {
    Constant,
    Linear,
    Ease,
    Cubic,
    Slerp,
  }
  //This is a Q&D animation sys em for realtime animations.
  public class Keyframe : Cloneable<Keyframe>
  {
    public double Time = 0;
    public quat Rot { get; set; } = quat.identity();
    public vec3 Pos { get; set; } = new vec3(0, 0, 0);
    public vec3 Scale { get; set; } = new vec3(1, 1, 1);
    public KeyframeInterpolation PosInterp { get; private set; } = KeyframeInterpolation.Cubic;
    public KeyframeInterpolation RotInterp { get; private set; } = KeyframeInterpolation.Slerp;
    public KeyframeInterpolation SclInterp { get; private set; } = KeyframeInterpolation.Cubic;
    public Keyframe(double time, quat quat, KeyframeInterpolation rotInt, vec3 pos, KeyframeInterpolation posInt)
    {
      Rot = quat;
      Pos = pos;
      Time = time;
      RotInterp = rotInt;
      PosInterp = posInt;
    }
    public Keyframe(double time, quat quat, vec3 pos)
    {
      Rot = quat;
      Pos = pos;
      Time = time;
    }
    public Keyframe(double time, quat quat)
    {
      Rot = quat;
      Time = time;
    }
    public Keyframe(double time, vec3 pos)
    {
      Pos = pos;
      Time = time;
    }
    public Keyframe() { }
    public override Keyframe Clone(bool shallow = true)
    {
      Keyframe other = new Keyframe();
      other.Time = this.Time;
      other.Rot = this.Rot;
      other.Pos = this.Pos;
      other.Scale = this.Scale;
      other.PosInterp = this.PosInterp;
      other.RotInterp = this.RotInterp;
      other.SclInterp = this.SclInterp;
      return other;
    }
  }
  public enum ActionState
  {
    Pause, Run, Stop
  }
  public class AnimationComponent : Component
  {
    public ActionState AnimationState { get; private set; } = ActionState.Stop;
    public double Time { get; private set; } = 0;//Seconds
    public bool Repeat { get; set; } = false;
    public Keyframe Current { get; private set; } = new Keyframe(); // Current interpolated Keyframe
    public List<Keyframe> KeyFrames { get; private set; } = new List<Keyframe>(); //This must be sorted by Time

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
    public AnimationComponent() { }
    public AnimationComponent(List<Keyframe> keyframes, bool repeat = false) { KeyFrames = keyframes; Repeat = repeat; }
    public override void Update(double dt, WorldObject myObj)
    {
      if (AnimationState == ActionState.Run)
      {
        Time += dt;
        if (Time > MaxTime)
        {
          if (Repeat)
          {
            Time = Time % MaxTime;
          }
          else
          {
            Stop();
          }
        }


        SlerpFrames();

        myObj.AnimatedPosition = Current.Pos;
        myObj.AnimatedRotation = Current.Rot;
        myObj.AnimatedScale = Current.Scale;
      }

      //mat4 mScl = mat4.CreateScale(Current.Scale);
      //mat4 mRot = mat4.CreateFromquaternion(Current.Rot);
      //mat4 mPos = mat4.CreateTranslation(Current.Pos);
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
    public void Play(bool? repeat = null)
    {
      if (KeyFrames.Count == 0)
      {
        Gu.Log.Error("Animation had no keyframes.");
        Gu.DebugBreak();
      }
      KeyFrames.Sort((x, y) => x.Time.CompareTo(y.Time));
      AnimationState = ActionState.Run;
      if (repeat != null)
      {
        Repeat = repeat.Value;
      }
    }
    public void Stop()
    {
      Time = 0;
      //Call slerpFrames once more to make sure we reset the current state back to frame zero
      SlerpFrames();
      AnimationState = ActionState.Stop;
    }
    public void Pause()
    {
      AnimationState = ActionState.Pause;
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
        Current.Rot = f0.Rot.slerpTo(f1.Rot, (float)slerpTime);
        Current.Pos = InterpolateV3(f1.PosInterp, f0.Pos, f1.Pos, slerpTime);
        Current.Scale = InterpolateV3(f1.SclInterp, f0.Scale, f1.Scale, slerpTime);
      }

    }
    private vec3 InterpolateV3(KeyframeInterpolation interp, vec3 f0, vec3 f1, double slerpTime)
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
    private vec3 LinearInterpolate(vec3 a, vec3 b, double time)
    {
      vec3 ret = a + (b - a) * (float)time;
      return ret;
    }
    private vec3 EaseInterpolate(vec3 a, vec3 b, double time)
    {
      //Sigmoid "ease"
      //Assuming time is normalized [0,1]
      double k = 0.1; //Slope
      double f = 1 / (1 + Math.Exp(-((time - 0.5) / k)));
      return a * (1 - (float)f) + b * (float)f;
    }
    private vec3 CubicInterpolate(vec3 a, vec3 b, double time)
    {
      //This is actually cosine interpolate. We need to update this to be cubic.
      //TODO:
      double ft = time * Math.PI;
      double f = (1.0 - Math.Cos(ft)) * 0.5;
      return a * (1 - (float)f) + b * (float)f;
    }
    private vec3 ConstantInterpolate(vec3 a, vec3 b, double time)
    {
      vec3 ret = a;
      return ret;
    }
    public override Component Clone(bool shallow = true)
    {
      AnimationComponent other = new AnimationComponent();
      other.AnimationState = this.AnimationState;
      other.Time = this.Time;
      other.Repeat = this.Repeat;
      other.Current = this.Current;
      other.KeyFrames = this.KeyFrames.Clone();
      return other;
    }
  }
  public abstract class Constraint : Cloneable<Constraint>
  {
    public abstract void Apply(WorldObject ob);
  }
  public class InputConstraint
  {
    //Stuff drive input.
  }
  public class FollowConstraint : Constraint
  {
    public enum FollowMode
    {
      Snap,
      Drift
    }
    public WeakReference<WorldObject> FollowObj { get; set; } = null;
    public float DriftSpeed { get; set; } = 0;//meters per second
    public FollowMode Mode { get; set; } = FollowConstraint.FollowMode.Snap;
    public FollowConstraint(WorldObject followob, FollowMode mode, float drift = 0)
    {
      FollowObj = new WeakReference<WorldObject>(followob);
      Mode = mode;
      DriftSpeed = drift;
    }
    public override void Apply(WorldObject ob)
    {
      if (FollowObj != null && FollowObj.TryGetTarget(out WorldObject obj))
      {
        ob.Position_Local = obj.World.extractTranslation();
      }
      else
      {
        Gu.Log.Error("'" + ob.Name + "' - Follow constraint - object not found.");
      }
    }
    public override Constraint Clone(bool shallow = true)
    {
      FollowConstraint cc = null;
      if (FollowObj.TryGetTarget(out var wo))
      {
        cc = new FollowConstraint(wo, Mode, DriftSpeed);
      }
      else
      {
        Gu.BRThrowException("Could not get target for cloing follow constraint.");
      }
      return cc;
    }
  }
  //public class TrackToConstraint : Constraint
  //{
  //  //*This does not work correctly.
  //  //Essentially it would set the camera object's world matrix, but it doesn't wrok.
  //  public bool Relative = false;
  //  public WorldObject LookAt = null;
  //  public vec3 Up = new vec3(0, 1, 0);
  //  public TrackToConstraint(WorldObject ob, bool relative)
  //  {
  //    LookAt = ob;
  //    Relative = relative;
  //  }
  //  public override void Apply(WorldObject self)
  //  {
  //    //Technically we should apply constraints right?
  //    //empty is a child of camera
  //    //compile world matrix children
  //    //compile world matrix parents
  //    //apply xforms to children
  //    //apply xforms to children
  //    //apply constraints to parents
  //    //apply constraitns to children
  //    vec3 eye;
  //    if (!Relative)
  //    {
  //      eye = LookAt.Position - self.Position;
  //    }
  //    else
  //    {
  //      eye = LookAt.Position;
  //    }

  //    //vec3 zaxis = (eye).Normalized();
  //    //vec3 xaxis = vec3.Cross(Up, zaxis).Normalized();
  //    //vec3 yaxis = vec3.Cross(zaxis, xaxis);
  //    ////vec3 zaxis = (LookAt - eye).normalize();
  //    ////vec3 xaxis = zaxis.cross(Up).normalize();
  //    ////vec3 yaxis = xaxis.cross(zaxis);
  //    ////zaxis*=-1;

  //    //mat4 mm = mat4.Identity;
  //    //mm.M11 = xaxis.x; mm.M12 = yaxis.x; mm.M13 = zaxis.x;
  //    //mm.M21 = xaxis.y; mm.M22 = yaxis.y; mm.M23 = zaxis.y;
  //    //mm.M31 = xaxis.z; mm.M32 = yaxis.z; mm.M33 = zaxis.z;
  //    //// mm = mm.Inverted();

  //    // self.Rotation = mm.ExtractRotation().ToAxisAngle();
  //  }
  //}
  public enum WorldObjectState
  {
    Created, Active, Destroyed
  }
  /// <summary>
  /// This is the main object that stores matrix for pos/rot/scale, and components for mesh, sound, script .. etc. GameObject in Unity.
  /// </summary>
  public class WorldObject : Cloneable<WorldObject>
  {
    //private mat4 _worldLast;
    //private quat _rotationLast = new quat(0, 0, 0, 1); //Axis-Angle xyz,ang
    //private vec3 _scaleLast = new vec3(1, 1, 1);
    //private vec3 _positionLast = new vec3(0, 0, 0);
    public object LoaderTempData = null;

    public vec3 dbg_last_n = vec3.Zero;

    private WorldObjectState _state = WorldObjectState.Created;
    static int _idGen=1;
    private int _uniqueId = 0; //Never duplicated, unique for all objs
    private int _typeId = 1; // When Clone() is called this gets duplicated
    private string _name = "<Unnamed>";
    private quat _rotation = new quat(0, 0, 0, 1); //Axis-Angle xyz,ang
    private vec3 _scale = new vec3(1, 1, 1);
    private vec3 _position = new vec3(0, 0, 0);
    private quat _animatedRotation = quat.identity();
    private vec3 _animatedScale = new vec3(1, 1, 1);
    private vec3 _animatedPosition = new vec3(0, 0, 0);
    private WorldObject _parent = null;
    private mat4 _world = mat4.identity();
    private mat4 _local = mat4.identity();
    private mat4 _bind = mat4.identity();
    private mat4 _inverse_bind = mat4.identity();
    private vec3 _basisX = new vec3(1, 0, 0);
    private vec3 _basisY = new vec3(0, 1, 0);
    private vec3 _basisZ = new vec3(0, 0, 1);
    private OOBox3f _boundBoxTransform = new OOBox3f(new vec3(0, 0, 0), new vec3(1, 1, 1));
    private Box3f _boundBox = new Box3f(new vec3(0, 0, 0), new vec3(1, 1, 1));
    private MeshData _meshData = null;
    private Material _material = null;
    private vec4 _color = new vec4(1, 1, 1, 1);
    private List<Component> _components = new List<Component>();
    private List<Constraint> _constraints = new List<Constraint>();
    private HashSet<WorldObject> _children = new HashSet<WorldObject>();
    private bool _transformChanged = false;
    private bool _hidden = false;
    private int _treeDepth = 0; //used to check for DAG cycles
    private vec3 _velocity = new vec3(0, 0, 0);
    private bool _resting = false;
    private bool _hasGravity = true;
    private bool _collides = false;
    private float _airFriction = 0.0f;//friction with the air i.e. movement damping in m/s
    private bool _hasPhysics = false;
    private vec3 _positionWorld = vec3.Zero;
    private quat _rotationWorld = quat.Identity;
    private vec3 _scaleWorld = vec3.Zero;


    public string Name { get { return _name; } set { _name = value; } }
    public int UniqueID { get { return _uniqueId; } private set { _uniqueId = value; } }
    public int TypeID { get { return _typeId; } private set { _typeId = value; } }
    public WorldObjectState State { get { return _state; } set { _state = value; } }
    public vec4 Color { get { return _color; } set { _color = value; } }// Mesh color if no material
    public bool TransformChanged { get { return _transformChanged; } private set { _transformChanged = value; } }
    public bool Hidden { get { return _hidden; } private set { _hidden = value; } }
    public Box3f BoundBoxMeshBind
    {
      get
      {
        if (Mesh != null)
        {
          //TODO: - apply animation bind matrix
          return Mesh.BoundBox_Extent;
        }
        else
        {
          return Box3f.Default; // No mesh, return 1,1,1
        }
      }
    }
    public OOBox3f BoundBoxMeshTransform { get { return _boundBoxTransform; } } //Transformed bound box
    public Box3f BoundBox { get { return _boundBox; } } //Entire AABB with all meshes and children inside
    public WorldObject Parent { get { return _parent; } private set { _parent = value; SetTransformChanged(); } }
    public HashSet<WorldObject> Children { get { return _children; } private set { _children = value; } }

    public vec3 Position_Local { get { return _position; } set { _position = value; SetTransformChanged(); } }
    public quat Rotation_Local { get { return _rotation; } set { _rotation = value; SetTransformChanged(); } }//xyz,angle
    public vec3 Scale_Local { get { return _scale; } set { _scale = value; SetTransformChanged(); } }
    
    public vec3 Position_World { get { return _positionWorld; } private set { _positionWorld = value; } }
    public quat Rotation_World { get { return _rotationWorld; } private set { _rotationWorld = value; } }
    public vec3 Scale_World { get { return _scaleWorld; } private set { _scaleWorld = value; } }

    public vec3 AnimatedPosition { get { return _animatedPosition; } set { _animatedPosition = value; SetTransformChanged(); } }
    public quat AnimatedRotation { get { return _animatedRotation; } set { _animatedRotation = value; SetTransformChanged(); } }
    public vec3 AnimatedScale { get { return _animatedScale; } set { _animatedScale = value; SetTransformChanged(); } }

    public mat4 Bind { get { return _bind; } } // Skinned Bind matrix
    public mat4 InverseBind { get { return _inverse_bind; } } // Skinned Inverse Bind
    public mat4 Local { get { return _local; } }
    public mat4 World { get { return _world; } }
    // public Int64 LastUpdate { get { }; private set; } = 0;
    public List<Component> Components { get { return _components; } private set { _components = value; } }
    public List<Constraint> Constraints { get { return _constraints; } private set { _constraints = value; } }// *This is an ordered list they come in order

    public vec3 BasisX { get { return _basisX; } }
    public vec3 BasisY { get { return _basisY; } }
    public vec3 BasisZ { get { return _basisZ; } }
    public vec3 ForwardNormalVector { get { return _basisZ; } }

    public MeshData Mesh { get { return _meshData; } set { _meshData = value; } }
    public Material Material { get { return _material; } set { _material = value; } }

    public Action<WorldObject>? OnAddedToScene { get; set; } = null;
    public Action<WorldObject>? OnDestroyed { get; set; } = null;

    public bool HasPhysics { get { return _hasPhysics; } set { _hasPhysics = value; } }
    public vec3 Velocity { get { return _velocity; } set { _velocity = value; } }
    public bool OnGround { get { return _resting; } set { _resting = value; } }
    public bool HasGravity { get { return _hasGravity; } set { _hasGravity = value; } }
    public bool Collides { get { return _collides; } set { _collides = value; } }
    public float AirFriction { get { return _airFriction; } set { _airFriction = value; } }

    private WorldObject() { }//Clone ctor
    public WorldObject(string name)
    {
      Name = name;
      //For optimization, nothing shoudl be here. WorldObject is new'd a lot each frame.
      _color = Random.NextVec4(new vec4(0.2f, 0.2f, 0.2f, 1), new vec4(1, 1, 1, 1));
      _uniqueId = _idGen++;
      _typeId = _typeId++;
    }
    public virtual void Update(double dt, ref Box3f parentBoundBox)
    {
      if (Hidden)
      {
        return;
      }

      UpdateComponents(dt);
      ApplyConstraints();
      CompileLocalMatrix();
      ApplyParentMatrix();

      //Basis calculuation must come after the world is computed
      _basisX = (World * new vec4(1, 0, 0, 0)).xyz().normalized();
      _basisY = (World * new vec4(0, 1, 0, 0)).xyz().normalized();
      _basisZ = (World * new vec4(0, 0, 1, 0)).xyz().normalized();

      // bleh. We should just compute these if we need them. _bComputedWorldDecompose
      mat4 tmprot;
      vec4 pw;
      vec4 sw;
      World.decompose(out pw, out tmprot, out sw);
      _positionWorld = pw.xyz();
      _scaleWorld = sw.xyz();
      _rotationWorld = tmprot.toQuat();

      _boundBox.genResetLimits();
      foreach (var child in this.Children)
      {
        child.Update(dt, ref _boundBox);
      }
      CalcBoundBox(ref parentBoundBox);
    }
    public override WorldObject Clone(bool shallow = true)
    {
      //Create new wo WITHOUT THE PARENT but WITH ALL CLONED CHILDREN
      //This really does need to be its own method
      WorldObject other = new WorldObject();
      other._name = this._name;
      other._uniqueId = _idGen++;
      other._rotation = this._rotation;
      other._scale = this._scale;
      other._position = this._position;
      other._animatedRotation = this._animatedRotation;
      other._animatedScale = this._animatedScale;
      other._animatedPosition = this._animatedPosition;
      //other._parent = _parent; //Do not clone
      other._world = this._world;
      other._local = this._local;
      other._bind = this._bind;
      other._inverse_bind = this._inverse_bind;
      other._basisX = this._basisX;
      other._basisY = this._basisY;
      other._basisZ = this._basisZ;
      other._boundBoxTransform = this._boundBoxTransform;
      other._boundBox = this._boundBox;
      other._color = this._color;
      other._transformChanged = this._transformChanged;
      other._hidden = this._hidden;
      other._typeId = this._typeId;
      other._state = this._state;
      //other._treeDepth = this._treeDepth; //Do not clone
      other.OnAddedToScene = this.OnAddedToScene;
      other.OnDestroyed = this.OnDestroyed;
      other._velocity = this._velocity;
      other._resting = this._resting;
      other._hasGravity = this._hasGravity;
      other._collides = this._collides;
      other._airFriction = this._airFriction;
      other._hasPhysics = this._hasPhysics;
    Gu.Assert(shallow == true);//Not supported

      if (shallow == false)
      {
        other._meshData = this._meshData.Clone();
        other._material = this._material.Clone(shallow);
      }
      else
      {
        //Create an instance copy of the data blocks.
        other._meshData = this._meshData;
        other._material = this._material;
      }

      other._components = this._components.Clone(shallow);
      other._constraints = this._constraints.Clone(shallow);

      foreach (var ch in this._children)
      {
        WorldObject cc = ch.Clone(shallow);
        other.AddChild(cc);
      }

      return other;
    }
    public void Destroy()
    {
      _state = WorldObjectState.Destroyed;//Picked up and destroyed by the world.
    }
    public AnimationComponent GrabFirstAnimation()
    {
      //Test - assume tool has just one component
      foreach (var c in Components)
      {
        if (c is AnimationComponent)
        {
          return c as AnimationComponent;
        }
      }
      return null;
    }
    public void SetTransformChanged()
    {
      if (GetType() == typeof(Camera3D))
      {
        int n = 0;
        n++;
      }
      TransformChanged = true;
    }
    public void ApplyConstraints()
    {
      foreach (var c in Constraints)
      {
        c.Apply(this);
      }
    }
    private void UpdateTreeDepth()
    {
      if (_parent == null)
      {
        _treeDepth = 0;
      }
      else
      {
        _treeDepth = _parent._treeDepth + 1;
        foreach (var cc in Children)
        {
          cc.UpdateTreeDepth();
        }
      }
    }
    public WorldObject AddChild(WorldObject child)
    {
      Gu.Assert(child != Gu.World.SceneRoot);
      Gu.Assert(child != this);

      if (child.Parent != null)
      {
        child.Parent.RemoveChild(child);
      }

      Children.Add(child);
      child.Parent = this;

      child.UpdateTreeDepth();

      return this;
    }
    public void RemoveChild(WorldObject child)
    {
      //Note this will keep the object in memory, to delete an object call World.DestroyObject
      if (!Children.Remove(child))
      {
        Gu.Log.Error("Child '" + child.Name + "' not found in '" + Name + "'");
        Gu.DebugBreak();
      }
      child._parent = null;
      child.UpdateTreeDepth();
    }
    public void CalcBoundBox(ref Box3f parent)
    {
      if (Mesh != null)
      {
        _boundBoxTransform = new OOBox3f(BoundBoxMeshBind._min, BoundBoxMeshBind._max);
        for (int vi = 0; vi < OOBox3f.VertexCount; ++vi)
        {
          vec4 v = World * _boundBoxTransform.Verts[vi].toVec4(1);
          _boundBoxTransform.Verts[vi] = v.xyz();
          _boundBox.genExpandByPoint(_boundBoxTransform.Verts[vi]);
        }
      }

      parent.genExpandByPoint(_boundBox._min);
      parent.genExpandByPoint(_boundBox._max);
    }
    public void CompileLocalMatrix()
    {
      if (TransformChanged == false)
      {
        return;
      }

      mat4 mSclA = mat4.getScale(AnimatedScale);
      mat4 mRotA = mat4.getRotation(AnimatedRotation);
      mat4 mPosA = mat4.getTranslation(AnimatedPosition);

      mat4 mScl = mat4.getScale(Scale_Local);
      mat4 mRot = mat4.getRotation(Rotation_Local);
      mat4 mPos = mat4.getTranslation(Position_Local);
      _local = (mScl * mSclA) * (mRot * mRotA) * (mPos * mPosA);
    }
    public void ApplyParentMatrix()
    {
      //TODO: Parent types
      //if isBoneNode
      if (Parent != null)
      {
        _world = _local * Parent.World;
      }
      else
      {
        _world = _local;
      }
    }
    public void UpdateComponents(double dt)
    {
      // TODO:
      for (int c = Components.Count - 1; c >= 0; c--)
      {
        var cmp = Components[c];
        cmp.Update(dt, this);
      }
    }
    public T Component<T>() where T : class
    {
      foreach (var c in Components)
      {
        if (c is T)
        {
          return c as T;
        }
      }
      return null;
    }
    public void Unlink()
    {
      //Unlink object for destroy
      HashSet<WorldObject> cpy = new HashSet<WorldObject>(Children);
      foreach (var c in cpy)
      {
        c.Unlink();
      }
      Children.Clear();
      cpy.Clear();
      _parent?.RemoveChild(this);
    }
  }
}
