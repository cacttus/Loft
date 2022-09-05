using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace PirateCraft
{
  #region Enums

  public enum ComponentState
  {
    Added,
    Initialized,
    Destroyed
  }
  public enum KeyframeInterpolation
  {
    Constant,
    Linear,
    Ease,
    Cubic,
    Slerp,
  }
  public enum WorldObjectState
  {
    Created, Active, Destroyed
  }

  #endregion
  #region Components

  public abstract class Component : Cloneable<Component>
  {
    public bool Enabled { get; set; } = true;

    public Component()
    {
    }
    public ComponentState ComponentState { get; set; } = ComponentState.Added;
    public abstract void OnCreate(WorldObject myObj); //called after the object is created
    public abstract void OnUpdate(double dt, WorldObject myObj); //update
    public abstract void OnDestroy(WorldObject myObj); //called before the object is destroyed.
    public virtual void OnPick() { }
    public virtual void OnView(WorldObject ob, RenderView rv) { }
  }
  public class EventComponent : Component
  {
    //Executes an action on an object for a given interval
    public DeltaTimer Timer { get; private set; } = null;
    public Action<WorldObject>? Action { get; set; } = null;
    private EventComponent() { }
    public EventComponent(Action<WorldObject>? action, double tick_seconds, bool repeat)
    {
      Action = action;
      Timer = new DeltaTimer(tick_seconds, repeat);
    }
    public override void OnCreate(WorldObject myObj)
    {
    }
    public override void OnUpdate(double dt, WorldObject myObj)
    {
      int fires = Timer.Update(dt);
      for (int x = 0; x < fires; x++)
      {
        Action?.Invoke(myObj);
      }
    }
    public override void OnDestroy(WorldObject myObj)
    {
    }
    public override Component Clone(bool shallow = true)
    {
      EventComponent other = new EventComponent();
      other.Timer = this.Timer.Clone();
      other.Action = this.Action;
      return other;
    }
    public void Start()
    {
      Timer.Start();
    }
    public void Stop()
    {
      Timer.Stop();
    }
  }
  public class PhysicsComponent : Component
  {
    //Yup
    public vec3 Velocity = new vec3(0, 0, 0);
    public bool HasGravity = false;
    public bool Collides = false;
    public override void OnCreate(WorldObject myObj)
    {
    }
    public override void OnUpdate(double dt, WorldObject myObj)
    {
      myObj.Position_Local += Velocity;
    }
    public override void OnDestroy(WorldObject myObj)
    {
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
  public class Keyframe : Cloneable<Keyframe>
  {
    //This is a Q&D animation sys em for realtime matrix animations

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
    public override void OnCreate(WorldObject myObj)
    {
    }
    public override void OnUpdate(double dt, WorldObject myObj)
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
    public override void OnDestroy(WorldObject myObj)
    {
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
  public class InputComponent : Component
  {
    protected RenderView View { get; set; } = null;
    public InputComponent(RenderView win)
    {
      View = win;
    }
    public override void OnCreate(WorldObject myObj)
    {
    }
    public override void OnUpdate(double dt, WorldObject myObj)
    {
    }
    public override void OnDestroy(WorldObject myObj)
    {
    }
    public override Component Clone(bool shallow = true)
    {
      throw new NotImplementedException();
    }
  }
  public class FPSInputComponent : InputComponent
  {
    public enum FPSCamMode
    {
      Playing, Flying
    }
    public FPSCamMode CamMode { get; set; } = FPSCamMode.Flying;
    private const float Base_Speed = World.BlockSizeX * 0.10f;
    private const float Run_Mul = 6;
    private const float Base_Jump_Speed = World.BlockSizeY * 0.75f;
    private const float MaxAirFriction = 10.0f;//friction percentage in velocity Units per second (1.0 means the velocity will reach 0 in one second) [0,1]. lower values result in less friction
                                               //    private FirstPersonMouseRotator _FPSRotator = new FirstPersonMouseRotator();

    //Rotate camera via mouse on screen. Warp mouse.
    private double rotX = 0;
    private double rotY = 0;
    private double warp_boundary = 0.001f;//the distance user can move mouse in window  until we warp. Warping every frame absolutely sucks.
    private float _rotations_per_width = 2.5f; // How many times we rotate 360 degrees width/Pixel.  if the user moves the cursor across the whole window width
    private float _half_rotations_per_height = 1f; // How many times we rotate 180 degrees height/Pixel  if the user moves the cursor across the whole window heihgt.
    private float _pan_meters_per_pixel = 14f; //shift+mmb
    private float _zoom_meters_per_pixel = 22f; //ctrl+mmb
    private float _scroll_zoom_meters_per_pixel = 4f;//Zoom with mouse wheel

    private bool _isActiveView = false; //user pressed MMB, RMB, LMB in view

    public FPSInputComponent(RenderView view) : base(view)
    {
    }
    public override void OnCreate(WorldObject myObj)
    {
    }
    public override void OnUpdate(double dt, WorldObject myObj)
    {
      if (!Gu.Context.GameWindow.IsFocused)
      {
        return;
      }
      if (View != Gu.Context.GameWindow.ActiveView)
      {
        return;
      }
      Camera3D cam = null;
      if (!View.Camera.TryGetTarget(out cam))
      {
        return;
      }
      base.OnUpdate(dt, myObj);

      myObj.AirFriction = MaxAirFriction; //Movement Damping

      vec3basis basis = new vec3basis();
      if (CamMode == FPSCamMode.Flying)
      {
        basis.x = cam.BasisX;
        basis.y = cam.BasisY;
        basis.z = cam.BasisZ;
      }
      else if (CamMode == FPSCamMode.Playing)
      {
        basis.x = cam.BasisX;
        basis.y = vec3.Zero; //no cheating
        basis.z = cam.BasisZ;
      }
      DoMouse(myObj, cam, basis);
    }
    public override void OnDestroy(WorldObject myObj)
    {
    }
    private void DoMouse(WorldObject obj, Camera3D cam, vec3basis basis)
    {
      if (cam.View != null && cam.View.TryGetTarget(out var view))
      {
        //Rotate Camera
        float width = view.Viewport.Width;
        float height = view.Viewport.Height;
        float mpx_rel = Gu.Mouse.Pos.x - view.Viewport.X;
        float mpy_rel = Gu.Mouse.Pos.y - view.Viewport.Y;
        vec2 mouse_delta_wh = new vec2(Gu.Mouse.PosDelta.x / view.Viewport.Width, Gu.Mouse.PosDelta.y / view.Viewport.Height);

        bool ms_move_editing_must_warp = false;

        //** Mimicking Blender Defaults ** 
        if (Gu.Mouse.ScrollDelta.y != 0)
        {
          obj.Position_Local += basis.z * Gu.Mouse.ScrollDelta.y * _scroll_zoom_meters_per_pixel;
          ms_move_editing_must_warp = true;
        }
        if (Gu.Mouse.GetDeviceButtonDown(MouseButton.Middle))
        {
          //Allow shift or control to affect speed instead, if WSAD is down.
          bool bMoving = Gu.Keyboard.PressOrDown(Keys.W) || Gu.Keyboard.PressOrDown(Keys.S) || Gu.Keyboard.PressOrDown(Keys.A) || Gu.Keyboard.PressOrDown(Keys.D);
          
          if (!bMoving && (Gu.Keyboard.PressOrDown(Keys.LeftShift) || Gu.Keyboard.PressOrDown(Keys.RightShift)))
          {
            //Pan
            obj.Position_Local += basis.x * -mouse_delta_wh.x * _pan_meters_per_pixel;
            obj.Position_Local += basis.y * mouse_delta_wh.y * _pan_meters_per_pixel;
            ms_move_editing_must_warp = true;
          }
          else if (!bMoving && (Gu.Keyboard.PressOrDown(Keys.LeftControl) || Gu.Keyboard.PressOrDown(Keys.RightControl)))
          {
            //Zoom
            obj.Position_Local += basis.z * -mouse_delta_wh.y * _zoom_meters_per_pixel;
            ms_move_editing_must_warp = true;
          }
          else
          {
            DoMoveWSAD(obj, basis);

            //Rotate
            rotX += Math.PI * 2 * mouse_delta_wh.x * _rotations_per_width * Gu.CoordinateSystemMultiplier;
            if (rotX >= Math.PI * 2.0f)
            {
              rotX = (float)(rotX % (Math.PI * 2.0f));
            }
            if (rotX <= 0)
            {
              rotX = (float)(rotX % (Math.PI * 2.0f));
            }

            rotY += Math.PI * 2 * mouse_delta_wh.y * _half_rotations_per_height * Gu.CoordinateSystemMultiplier;
            if (rotY >= Math.PI / 2)
            {
              rotY = Math.PI / 2 - 0.001f;
            }
            if (rotY <= -Math.PI / 2)
            {
              rotY = -Math.PI / 2 + 0.001f;
            }

            quat qy = quat.fromAxisAngle(new vec3(0, 1, 0), (float)rotX).normalized();
            quat qx = quat.fromAxisAngle(new vec3(1, 0, 0), (float)rotY).normalized();

            obj.Rotation_Local = qy;
            cam.Rotation_Local = qx;

            ms_move_editing_must_warp = true;
          }
          if (ms_move_editing_must_warp)
          {
            Gu.Mouse.WarpMouse(View, WarpMode.Wrap, 0.001f);
          }
        }
      }

    }
    private void DoMoveWSAD(WorldObject myObj, vec3basis basis)
    {

      //Modify speed multiplier based on state
      float speedMul = 1; //normal speed
      if (Gu.Keyboard.PressOrDown(Keys.LeftControl) || Gu.Keyboard.PressOrDown(Keys.RightControl))
      {
        speedMul = Run_Mul; // run speed
      }
      if (!myObj.OnGround && this.CamMode != FPSCamMode.Flying)
      {
        speedMul = 0.1f; // "in the air" movement. 
      }

      float final_run_speed = Base_Speed * speedMul;
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Q }))
      {
        myObj.Velocity += basis.y * final_run_speed;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.E }))
      {
        myObj.Velocity -= basis.y * final_run_speed;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Up, Keys.W }))
      {
        myObj.Velocity += basis.z * final_run_speed;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Down, Keys.S }))
      {
        myObj.Velocity -= basis.z * final_run_speed;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Right, Keys.D }))
      {
        myObj.Velocity += basis.x * final_run_speed;
      }
      if (Gu.Keyboard.PressOrDown(new List<Keys>() { Keys.Left, Keys.A }))
      {
        myObj.Velocity -= basis.x * final_run_speed;
      }

      if (myObj.OnGround && this.CamMode != FPSCamMode.Flying)
      {
        if (Gu.Keyboard.PressOrDown(Keys.Space))
        {
          myObj.Velocity += new vec3(0, Base_Jump_Speed, 0);
        }
      }

    }
  }//FpsInputComponent





  #endregion
  #region Constraints

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
        ob.Position_Local = obj.WorldMatrix.ExtractTranslation();
      }
      else
      {
        Gu.Log.Error("'" + ob.Name + "' - Follow constraint - object not found.");
      }
    }
    public override Constraint Clone(bool shallow = true)
    {
      FollowConstraint cc = null;
      if (FollowObj != null && FollowObj.TryGetTarget(out var wo))
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

  #endregion

  public class WorldObject : DataBlock
  {
    // main object that stores matrix for pos/rot/scale, and components for mesh, sound, script .. GameObject ..
    #region Public:Members

    public object LoaderTempData = null;
    public bool DebugBreakRender = false;
    public uint PickId { get { return _pickId; } }
    public WorldObjectState State { get { return _state; } set { _state = value; } }
    public bool TransformChanged { get { return _transformChanged; } private set { _transformChanged = value; } }
    public bool Hidden { get { return _hidden; } private set { _hidden = value; } }

    public OOBox3f BoundBoxMeshTransform { get { return _boundBoxTransform; } } //Transformed bound box
    public Box3f BoundBox { get { return _boundBox; } } //Entire AABB with all meshes and children inside

    public List<WorldObject> Children { get { return _children; } private set { _children = value; } }

    public vec3 Position_Local { get { return _position; } set { _position = value; SetTransformChanged(); } }
    public quat Rotation_Local { get { return _rotation; } set { _rotation = value; SetTransformChanged(); } }//xyz,angle
    public vec3 Scale_Local { get { return _scale; } set { _scale = value; SetTransformChanged(); } }

    public vec3 Position_World { get { return _positionWorld; } private set { _positionWorld = value; } }
    public quat Rotation_World { get { return _rotationWorld; } private set { _rotationWorld = value; } }
    public vec3 Scale_World { get { return _scaleWorld; } private set { _scaleWorld = value; } }

    public vec3 AnimatedPosition { get { return _animatedPosition; } set { _animatedPosition = value; SetTransformChanged(); } }
    public quat AnimatedRotation { get { return _animatedRotation; } set { _animatedRotation = value; SetTransformChanged(); } }
    public vec3 AnimatedScale { get { return _animatedScale; } set { _animatedScale = value; SetTransformChanged(); } }

    public mat4 BindMatrix { get { return _bind; } } // Skinned Bind matrix
    public mat4 InverseBindMatrix { get { return _inverse_bind; } } // Skinned Inverse Bind
    public mat4 LocalMatrix { get { return _local; } }
    public mat4 WorldMatrix { get { return _world; } }
    public List<Component> Components { get { return _components; } private set { _components = value; } }
    public List<Constraint> Constraints { get { return _constraints; } private set { _constraints = value; } }// *This is an ordered list they come in order

    public vec3 BasisX { get { return _basisX; } }
    public vec3 BasisY { get { return _basisY; } }
    public vec3 BasisZ { get { return _basisZ; } }
    public vec3 ForwardNormalVector { get { return _basisZ; } }
    public vec3 Heading { get { return _basisZ; } }

    public MeshData Mesh { get { return _meshData; } set { _meshData = value; } }
    public Material Material { get { return _material; } set { _material = value; } }

    public Action<WorldObject>? OnUpdate { get; set; } = null;
    public Action<WorldObject, RenderView>? OnView { get; set; } = null;
    public Action<WorldObject>? OnAddedToScene { get; set; } = null;
    public Action<WorldObject>? OnDestroyed { get; set; } = null;

    public bool HasPhysics { get { return _hasPhysics; } set { _hasPhysics = value; } }
    public vec3 Velocity { get { return _velocity; } set { _velocity = value; } }
    public bool OnGround { get { return _resting; } set { _resting = value; } }
    public bool HasGravity { get { return _hasGravity; } set { _hasGravity = value; } }
    public bool Collides { get { return _collides; } set { _collides = value; } }
    public float AirFriction { get { return _airFriction; } set { _airFriction = value; } }

    //public WindowContext ExclusiveRenderContext { get; set; } = null; //ONLY render this object in THIS context, regardless of whether it is visible. This is for multiple-windows. If null: render in any context.
    //public WindowContext ExcludeFromRenderContext { get; set; } = null; //DO NOT render in THIS context. Used for an FPS seeing other characters.
    public WeakReference<RenderView> ExcludeFromRenderView { get; set; } = null; //DO NOT render in THIS context. Used for an FPS seeing other characters.

    public List<WorldObject> Instances = null;// To make an Instance's object data  unique call MakeUnique

    #endregion
    #region Public:Propfuncs

    public static WorldObject Default
    {
      get
      {
        return new WorldObject();
      }
    }
    public WorldObject RootParent
    {
      //Return root object parent that is NOT the scene. E.g., the root of the given object
      get
      {
        var thep = this;
        for (int ip = 0; ip < Gu.c_intMaxWhileTrueLoop; ip++)
        {
          if (thep._parent != null && thep._parent.TryGetTarget(out var p))
          {
            if (p != Gu.World.SceneRoot)
            {
              thep = p;
            }
          }
          else
          {
            break;
          }
        }
        return thep;
      }
    }
    public bool Pickable
    {
      get
      {
        return _pickable;
      }
      set
      {
        if ((_pickable == false && value == true) || _pickId == 0)
        {
          //NOTE: the pick IDs are from the context.. and they should be basd on eadch context.
          // This is INVALID
          _pickId = Gu.Context.Renderer.Picker.GenPickId();
        }
        else if (value == false)
        {
          _pickId = 0;
        }
        _pickable = value;
      }
    }
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
    public WorldObject Parent
    {
      get
      {
        if (_parent != null && _parent.TryGetTarget(out var p))
        {
          return p;
        }
        return null;
      }
      private set
      {
        _parent = new WeakReference<WorldObject>(value);
        SetTransformChanged();
      }
    }

    #endregion
    #region Private:Members

    private WorldObjectState _state = WorldObjectState.Created;
    private quat _rotation = new quat(0, 0, 0, 1); //Axis-Angle xyz,ang
    private vec3 _scale = new vec3(1, 1, 1);
    private vec3 _position = new vec3(0, 0, 0);
    private quat _animatedRotation = quat.identity();
    private vec3 _animatedScale = new vec3(1, 1, 1);
    private vec3 _animatedPosition = new vec3(0, 0, 0);
    private WeakReference<WorldObject> _parent = null;
    private mat4 _world = mat4.Identity;
    private mat4 _local = mat4.Identity;
    private mat4 _bind = mat4.Identity;
    private mat4 _inverse_bind = mat4.Identity;
    private vec3 _basisX = new vec3(1, 0, 0);
    private vec3 _basisY = new vec3(0, 1, 0);
    private vec3 _basisZ = new vec3(0, 0, 1);
    private OOBox3f _boundBoxTransform = new OOBox3f(new vec3(0, 0, 0), new vec3(1, 1, 1));
    protected Box3f _boundBox = new Box3f(new vec3(0, 0, 0), new vec3(1, 1, 1));
    private MeshData _meshData = null;
    private Material _material = null;
    private vec4 _color = new vec4(1, 1, 1, 1);
    private List<Component> _components = new List<Component>();
    private List<Constraint> _constraints = new List<Constraint>();
    private List<WorldObject> _children = new List<WorldObject>();
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
    private uint _pickId = 0;
    private bool _pickable = true;

    #endregion
    #region Public:Methods

    protected WorldObject() { }//Clone ctor
    public WorldObject(string name) : base(name + "-obj")
    {
      Gu.Assert(Gu.Context != null);
      Gu.Assert(Gu.Context.Renderer != null);
      //For now, everything gets a pick color. Debug reasons.
      //NOTE: the pick IDs are from the context.. and they should be basd on eadch context.
      // This is INVALID
      if (_pickable)
      {
        _pickId = Gu.Context.Renderer.Picker.GenPickId();
      }
      //For optimization, nothing shoudl be here. WorldObject is new'd a lot each frame.
      _color = Random.NextVec4(new vec4(0.2f, 0.2f, 0.2f, 1), new vec4(1, 1, 1, 1));
      _meshData = MeshData.DefaultBox;
      _material = Material.DefaultObjectMaterial;
    }
    public virtual void Update(World world, double dt, ref Box3f parentBoundBox)
    {
      if (Hidden)
      {
        return;
      }
      OnUpdate?.Invoke(this);

      UpdateComponents(dt);
      ApplyConstraints();
      CompileLocalMatrix();
      ApplyParentMatrix();

      //Basis calculuation must come after the world is computed
      _basisX = (WorldMatrix * new vec4(1, 0, 0, 0)).xyz().normalized();
      _basisY = (WorldMatrix * new vec4(0, 1, 0, 0)).xyz().normalized();
      _basisZ = (WorldMatrix * new vec4(0, 0, 1, 0)).xyz().normalized();

      // bleh. We should just compute these if we need them. _bComputedWorldDecompose
      mat4 tmprot;
      vec4 pw;
      vec4 sw;
      WorldMatrix.decompose(out pw, out tmprot, out sw);
      _positionWorld = pw.xyz();
      _scaleWorld = sw.xyz();
      _rotationWorld = tmprot.toQuat();

      _boundBox.genResetLimits();
      foreach (var child in this.Children)
      {
        child.Update(world, dt, ref _boundBox);
      }
      CalcBoundBox(ref parentBoundBox);
    }
    private void UpdateComponents(double dt)
    {
      IterateComponentsSafe((cmp) =>
      {
        if (cmp.ComponentState == ComponentState.Added)
        {
          cmp.OnCreate(this);
          cmp.ComponentState = ComponentState.Initialized;
        }
        if (cmp.Enabled)
        {
          cmp.OnUpdate(dt, this);
        }
        return LambdaBool.Continue;
      });
    }
    public void Pick()
    {
      if (!Pickable)
      {
        return;
      }
      if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
      {
        //Picking is pixel perfect, so the first picked object is the exact object.
        //However objects may have children, and components which can also be picked, and may not be in the global list.
        //Obviously, a list of pickid->obj would be the fastest.
        return;
      }
      if (_pickId != Picker.c_iInvalidPickId)
      {
        var pixid = Gu.Context.Renderer.Picker.GetSelectedPixelId();
        if (pixid != 0)
        {
          if (pixid == this._pickId)
          {
            Gu.Context.Renderer.Picker.PickedObjectFrame = this;
          }
        }
      }
      IterateComponentsSafe((cmp) =>
      {
        if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
        {
          return LambdaBool.Break;
        }
        cmp.OnPick();
        if (Gu.Context.Renderer.Picker.PickedObjectFrame != null)
        {
          //The component (gui) picked something that it owns. Set the worldobject to this.
          Gu.Context.Renderer.Picker.PickedObjectFrame = this;
          return LambdaBool.Break;
        }
        return LambdaBool.Continue;
      });
      IterateChildrenSafe((child) =>
      {
        if (Gu.Context.Renderer.Picker.PickedObjectFrame == null)
        {
          child.Pick();
        }
        else
        {
          return LambdaBool.Break;
        }
        return LambdaBool.Continue;
      });

    }
    public WorldObject Clone()
    {
      //Creates a shallow clone.
      //MakeUnique will deep clone the resulting shallow object
      WorldObject other = new WorldObject();

      Copy(other);

      Instances.Add(other);

      return other;
    }
    public override void MakeUnique()
    {
      base.MakeUnique();
      _meshData = _meshData.Clone();
      _material = _material.Clone();
      _components = this._components.Clone(false);
      _constraints = this._constraints.Clone(false);
    }
    protected void Copy(WorldObject other)
    {
      base.Copy(other);

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
      other._state = this._state;
      //other._treeDepth = this._treeDepth; //Do not clone
      other.OnUpdate = this.OnUpdate;
      other.OnAddedToScene = this.OnAddedToScene;
      other.OnDestroyed = this.OnDestroyed;
      other._velocity = this._velocity;
      other._resting = this._resting;
      other._hasGravity = this._hasGravity;
      other._collides = this._collides;
      other._airFriction = this._airFriction;
      other._hasPhysics = this._hasPhysics;

      //Create an instance copy of the data blocks.
      other._meshData = this._meshData;
      other._material = this._material;

      other._components = this._components.Clone(true);
      other._constraints = this._constraints.Clone(true);

      IterateChildrenSafe((ch) =>
      {
        other.AddChild(ch.Clone());
        return LambdaBool.Continue;
      });
    }
    public void View(RenderView rv)
    {
      OnView?.Invoke(this, rv);
      var that = this;
      IterateComponentsSafe((cmp) =>
      {
        cmp.OnView(that, rv);
        return LambdaBool.Continue;
      });

      IterateChildrenSafe((child) =>
      {
        child.View(rv);
        return LambdaBool.Continue;
      });
    }
    public void Destroy()
    {
      _state = WorldObjectState.Destroyed;//Picked up and destroyed by the world.
    }
    public AnimationComponent GrabFirstAnimation()
    {
      AnimationComponent found = null;
      //Test - assume tool has just one component
      IterateComponentsSafe((c) =>
      {
        if (c is AnimationComponent)
        {
          found = c as AnimationComponent;
          return LambdaBool.Break;
        }
        return LambdaBool.Continue;
      });
      return found;
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
          vec4 v = WorldMatrix * _boundBoxTransform.Verts[vi].toVec4(1);
          _boundBoxTransform.Verts[vi] = v.xyz();
          _boundBox.genExpandByPoint(_boundBoxTransform.Verts[vi]);
        }
      }
      else
      {
        _boundBox.genExpandByPoint(this.Position_World);
      }

      //So for now, I'm saying every object has a mesh of some kind. This makes things simpler.
      //If you don't want to draw it set Visible=false.
      if (!_boundBox.Validate())
      {
        Gu.Log.ErrorCycle(this.Name + " BoundBox was invalid.");
        Gu.DebugBreak();
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
        _world = _local * Parent.WorldMatrix;
      }
      else
      {
        _world = _local;
      }
    }
    public T Component<T>() where T : class
    {
      T found = null;
      //Gets the first component of the given template type
      IterateComponentsSafe((c) =>
      {
        if (c is T)
        {
          found = c as T;
          return LambdaBool.Break;
        }
        return LambdaBool.Continue;
      });
      return found;
    }
    public void IterateComponentsSafe(Func<Component, LambdaBool> act)
    {
      //If we remove components while iterating components..
      for (int c = Components.Count - 1; c >= 0; c--)
      {
        if (c < Components.Count)
        {
          if (act(Components[c]) == LambdaBool.Break)
          {
            break;
          }
        }
      }
    }
    public void IterateChildrenSafe(Func<WorldObject, LambdaBool> act)
    {
      //If we remove components while iterating components..
      for (int c = _children.Count - 1; c >= 0; c--)
      {
        if (c < _children.Count)
        {
          if (act(_children[c]) == LambdaBool.Break)
          {
            break;
          }
        }
      }
    }
    public void Unlink()
    {
      //Unlink object for destroy
      IterateChildrenSafe((c) =>
      {
        c.Unlink();
        return LambdaBool.Continue;
      });
      Children.Clear();
      TryGetParent()?.RemoveChild(this);
    }
    public WorldObject TryGetParent()
    {
      if (_parent != null && _parent.TryGetTarget(out var p))
      {
        return p;
      }
      return null;
    }
    #endregion
    #region Private:Methods

    private void UpdateTreeDepth()
    {
      if (Parent == null)
      {
        _treeDepth = 0;
      }
      else
      {
        _treeDepth = Parent._treeDepth + 1;
        IterateChildrenSafe((cc) =>
        {
          cc.UpdateTreeDepth();
          return LambdaBool.Continue;
        });
      }
    }

    #endregion
  }

  //*Note this is a test of billboarded quads.
  //for optimiz, We need to use
  // 1 single model matrix
  // 2 instancing
  // 3 megatex
  public enum LightType
  {
    Point, Direction
  }
  public class Light : WorldObject
  {
    public Light(string name) : base(name + "-light")
    {
      Texture2D light = Gu.Resources.LoadTexture(new FileLoc("bulb.png", FileStorage.Embedded), true, TexFilter.Bilinear);
      Pickable = true;
    }
    public float Radius { get; set; } = 100;//Distance in the case of directional light
    public vec3 Color { get; set; } = vec3.One;
    public float Power { get; set; } = 1;
    public LightType Type { get; set; } = LightType.Point;

    public override void Update(World world, double dt, ref Box3f parentBoundBox)
    {
      base.Update(world, dt, ref parentBoundBox);
      this._boundBox.genExpandByPoint(Position_Local - Radius);
      this._boundBox.genExpandByPoint(Position_Local + Radius);
    }
  }


}//ns 
