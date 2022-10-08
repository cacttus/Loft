using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.Serialization;
namespace PirateCraft
{
  public enum ComponentState
  {
    Added,
    Initialized,
    Destroyed
  }
  [DataContract]
  [Serializable]
  public abstract class Component : DataBlock, IClone, ICopy<Component>, ISerializeBinary
  {
    private ComponentState _componentState = ComponentState.Added;
    [DataMember] private bool _enabled = true;

    public bool Enabled { get { return _enabled; } set { _enabled = value; } }
    public ComponentState ComponentState { get { return _componentState; } set { _componentState = value; } }

    public Component() { }
    public abstract void OnCreate(WorldObject myObj); //called after the object is created
    public abstract void OnUpdate(double dt, WorldObject myObj); //update
    public abstract void OnDestroy(WorldObject myObj); //called before the object is destroyed.
    public virtual void OnPick() { }
    public virtual void OnView(WorldObject ob, RenderView rv) { }
    public abstract object? Clone(bool? shallow = null);
    public virtual void CopyFrom(Component? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      base.CopyFrom(other, shallow);
      this._enabled = other._enabled;
      this._componentState = other._componentState;
    }
    public override void Serialize(BinaryWriter bw)
    {
      bw.Write((System.Boolean)Enabled);
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      Enabled = br.ReadBoolean();
    }
  }
  [DataContract]
  [Serializable]
  public class EventComponent : Component, IClone, ICopy<EventComponent>
  {
    //Executes an action on an object for a given interval
    [DataMember] public DeltaTimer Timer { get; private set; } = null;
    public Action<WorldObject>? Action { get; set; } = null;

    public EventComponent() { }
    public EventComponent(Action<WorldObject>? action, double tick_seconds, ActionRepeat repeat, ActionState start)
    {
      Action = action;
      Timer = new DeltaTimer(tick_seconds, repeat, start);
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

    public void Start()
    {
      Timer.Start();
    }
    public void Stop()
    {
      Timer.Stop();
    }
    public override object? Clone(bool? shallow = null)
    {
      return Gu.Clone<EventComponent>(this);
    }
    public virtual void CopyFrom(EventComponent? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      base.CopyFrom(other, shallow);
      this.Timer = Gu.Clone<DeltaTimer>(other.Timer);
      this.Action = other.Action;
    }
    public override void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
      //Ok so Action() can't be serialized, this is a problem. Maybe we add a quick LUA script hting..ughhh
      Gu.BRThrowNotImplementedException();
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      base.Deserialize(br, version);
      Gu.BRThrowNotImplementedException();
    }
  }
  // public class PhysicsComponent : Component
  // {
  //   //Yup
  //   public vec3 Velocity = new vec3(0, 0, 0);
  //   public bool HasGravity = false;
  //   public bool Collides = false;
  //   public override void OnCreate(WorldObject myObj)
  //   {
  //   }
  //   public override void OnUpdate(double dt, WorldObject myObj)
  //   {
  //     myObj.Position_Local += Velocity;
  //   }
  //   public override void OnDestroy(WorldObject myObj)
  //   {
  //   }
  //   public override Component Clone(bool? shallow = null)
  //   {
  //     PhysicsComponent c = new PhysicsComponent();

  //     c.Velocity = this.Velocity;
  //     c.HasGravity = this.HasGravity;
  //     c.Collides = this.Collides;

  //     return c;
  //   }
  // }
  [DataContract]
  [Serializable]
  public class AnimationComponent : Component, IClone, ICopy<AnimationComponent>
  {
    [DataMember] public double Time { get; private set; } = 0;
    [DataMember] public ActionState AnimationState { get; private set; } = ActionState.Stop;
    [DataMember] public bool Repeat { get; set; } = false;
    [DataMember] public AnimationData AnimationData { get; set; } = null;

    [DataMember] private vec3? _currentPos = null;
    [DataMember] private quat? _currentRot = null;
    [DataMember] private vec3? _currentScl = null;
    [DataMember] private double _maxTime = 0;

    public AnimationComponent() { }
    public AnimationComponent(AnimationData dat) { this.AnimationData = dat; }

    public override void OnCreate(WorldObject myObj)
    {
    }
    public override void OnUpdate(double dt, WorldObject myObj)
    {
      if (AnimationState == ActionState.Run)
      {
        Time += dt;
        if (Time > _maxTime)
        {
          if (Repeat)
          {
            Time = Time % _maxTime;
          }
          else
          {
            Stop();
          }
        }

        SlerpFrames();

        if (_currentPos != null)
        {
          myObj.AnimatedPosition = _currentPos.Value;
        }
        if (_currentRot != null)
        {
          myObj.AnimatedRotation = _currentRot.Value;
        }
        if (_currentScl != null)
        {
          myObj.AnimatedScale = _currentScl.Value;
        }
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
    // private void NormalizeState()
    // {
    //   if (KeyFrames.Count > 0)
    //   {
    //     //foreach(var k in KeyFrames)
    //     {
    //       //TODO: find equal keyframe times as this could be an error.
    //       // Gu.Log.Warn("Keyframes had equal times.");
    //     }
    //   }
    // }
    public void Play(bool? repeat = null)
    {
      //Sort.
      if (AnimationData == null)
      {
        Gu.Log.Error("Animation had no data.");
        Gu.DebugBreak();
      }
      else if (
        (AnimationData.PosChannel == null || AnimationData.PosChannel.Count == 0) &&
        (AnimationData.RotChannel == null || AnimationData.RotChannel.Count == 0) &&
        (AnimationData.ScaleChannel == null || AnimationData.ScaleChannel.Count == 0)
        )
      {
        Gu.Log.Error("Animation had no keyframes.");
        Gu.DebugBreak();
      }
      AnimationData.PosChannel?.Sort((x, y) => x.Time.CompareTo(y.Time));
      AnimationData.RotChannel?.Sort((x, y) => x.Time.CompareTo(y.Time));
      AnimationData.ScaleChannel?.Sort((x, y) => x.Time.CompareTo(y.Time));

      //Calc max time.
      _maxTime = 0;
      if (AnimationData.PosChannel != null) { _maxTime = Math.Max(AnimationData.PosChannel[AnimationData.PosChannel.Count - 1].Time, _maxTime); }
      if (AnimationData.RotChannel != null) { _maxTime = Math.Max(AnimationData.RotChannel[AnimationData.RotChannel.Count - 1].Time, _maxTime); }
      if (AnimationData.ScaleChannel != null) { _maxTime = Math.Max(AnimationData.ScaleChannel[AnimationData.ScaleChannel.Count - 1].Time, _maxTime); }

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
    private void AdvanceFrame(List<KeyframeBase> frames, ref KeyframeBase? current, ref KeyframeBase? next, ref double out_time)
    {
      current = null;
      next = null;
      out_time = 0;
      if (frames == null)
      {
        //empty channel
      }
      else if (frames.Count == 0)
      {
        //No animation.
      }
      else if (frames.Count == 1)
      {
        //Set to this key frame.
        current = frames[0];
      }
      else
      {
        //Grab 0 and 1
        KeyframeBase? f0 = null, f1 = null;
        for (int i = 0; i < frames.Count - 1; i++)
        {
          var k0 = frames[i];
          var k1 = frames[i + 1];
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
          if (Time > frames[frames.Count - 1].Time)
          {
            f0 = frames[frames.Count - 1];
            f1 = f0;
          }
          else //if (Time < KeyFrames[0].Time)
          {
            f0 = frames[0];
            f1 = f0;
          }
        }

        Gu.Assert(f0 != null && f1 != null);
        double denom = f1.Time - f0.Time;
        if (denom == 0)
        {
          out_time = 1; //If f0 and f1 are euqal in time it's an error.
        }
        else
        {
          out_time = (Time - f0.Time) / denom;
        }
        current = f0;
        next = f1;
      }
    }
    private void SlerpFrames()
    {
      KeyframeBase? c = null, n = null;
      double t = 0;

      if (AnimationData.PosChannel != null && AnimationData.PosChannel.Count > 0)
      {
        AdvanceFrame(AnimationData.PosChannel.OfType<KeyframeBase>().ToList(), ref c, ref n, ref t);
        if (c != null && n == null)
        {
          _currentPos = (c as Vec3Keyframe).Value;
        }
        else if (c != null && n != null)
        {
          _currentPos = InterpolateV3((n as Vec3Keyframe).Interpolation, (c as Vec3Keyframe).Value, (n as Vec3Keyframe).Value, t);
        }
        else
        {
          _currentPos = null;
        }
      }
      if (AnimationData.RotChannel != null && AnimationData.RotChannel.Count > 0)
      {
        AdvanceFrame(AnimationData.RotChannel.OfType<KeyframeBase>().ToList(), ref c, ref n, ref t);
        if (c != null && n == null)
        {
          _currentRot = (c as QuatKeyframe).Value;
        }
        else if (c != null && n != null)
        {
          _currentRot = (c as QuatKeyframe).Value.slerpTo((n as QuatKeyframe).Value, (float)t);
        }
        else
        {
          _currentRot = null;
        }
      }
      if (AnimationData.ScaleChannel != null && AnimationData.ScaleChannel.Count > 0)
      {
        AdvanceFrame(AnimationData.ScaleChannel.OfType<KeyframeBase>().ToList(), ref c, ref n, ref t);
        if (c != null && n == null)
        {
          _currentScl = (c as Vec3Keyframe).Value;
        }
        else if (c != null && n != null)
        {
          _currentScl = InterpolateV3((n as Vec3Keyframe).Interpolation, (c as Vec3Keyframe).Value, (n as Vec3Keyframe).Value, t);
        }
        else
        {
          _currentScl = null;
        }
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
      else if (interp == KeyframeInterpolation.Step)
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
    public override object? Clone(bool? shallow = null)
    {
      return Gu.Clone<AnimationComponent>(this);
    }
    public void CopyFrom(AnimationComponent? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      base.CopyFrom(other, shallow);
      this.AnimationState = other.AnimationState;
      this.Time = other.Time;
      this.Repeat = other.Repeat;
      this._currentPos = other._currentPos;
      this._currentScl = other._currentScl;
      this._currentRot = other._currentRot;
    }
    public override void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
      bw.Write(Time);
      bw.Write((Int32)AnimationState);
      bw.Write(Repeat);
      // SerializeTools.SerializeRef(bw, this.Data);
      SerializeTools.SerializeNullable(bw, _currentPos, () => bw.Write(_currentPos.Value));
      SerializeTools.SerializeNullable(bw, _currentRot, () => bw.Write(_currentRot.Value));
      SerializeTools.SerializeNullable(bw, _currentScl, () => bw.Write(_currentScl.Value));
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      base.Deserialize(br, version);

      Gu.BRThrowNotImplementedException();
    }
  }
  public abstract class InputComponent : Component
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
  }
  [DataContract]
  [Serializable]
  public class FPSInputComponent : InputComponent, ICopy<FPSInputComponent>, ISerializeBinary
  {
    public enum FPSCamMode
    {
      Playing, Flying
    }
    private const float Base_Speed = 10.0f * 0.10f;
    private const float Run_Mul = 6;
    private const float Base_Jump_Speed = 10.0f * 0.75f;
    private const float MaxAirFriction = 10.0f;//friction percentage in velocity Units per second (1.0 means the velocity will reach 0 in one second) [0,1]. lower values result in less friction
                                               //    private FirstPersonMouseRotator _FPSRotator = new FirstPersonMouseRotator();
                                               //Rotate camera via mouse on screen. Warp mouse.
    private const double warp_boundary = 0.001f;//the distance user can move mouse in window  until we warp. Warping every frame absolutely sucks.
    private const float _rotations_per_width = 2.5f; // How many times we rotate 360 degrees width/Pixel.  if the user moves the cursor across the whole window width
    private const float _half_rotations_per_height = 1f; // How many times we rotate 180 degrees height/Pixel  if the user moves the cursor across the whole window heihgt.
    private const float _pan_meters_per_pixel = 14f; //shift+mmb
    private const float _zoom_meters_per_pixel = 22f; //ctrl+mmb
    private const float _scroll_zoom_meters_per_pixel = 4f;//Zoom with mouse wheel

    [DataMember] private double rotX = 0;
    [DataMember] private double rotY = 0;

    private bool _isActiveView = false; //user pressed MMB, RMB, LMB in view


    public FPSInputComponent() : base(null) { }
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
      if (View != Gu.Context.GameWindow.SelectedView)
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
      //removing cammode for now
      // if (CamMode == FPSCamMode.Flying)
      // {
      basis.x = cam.BasisX;
      basis.y = cam.BasisY;
      basis.z = cam.BasisZ;
      // }
      // else if (CamMode == FPSCamMode.Playing)
      // {
      //   basis.x = cam.BasisX;
      //   basis.y = vec3.Zero; //no cheating
      //   basis.z = cam.BasisZ;
      // }
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
        if (Gu.Mouse.PressOrDown(MouseButton.Middle))
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
            obj.SanitizeTransform();
            cam.Rotation_Local = qx;
            cam.SanitizeTransform();

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
      // if (!myObj.OnGround && this.CamMode != FPSCamMode.Flying)
      // {
      //   speedMul = 0.1f; // "in the air" movement. 
      // }

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

      // if (myObj.OnGround && this.CamMode != FPSCamMode.Flying)
      // {
      //   if (Gu.Keyboard.PressOrDown(Keys.Space))
      //   {
      //     myObj.Velocity += new vec3(0, Base_Jump_Speed, 0);
      //   }
      // }

    }
    public override object? Clone(bool? shallow = null)
    {
      return Gu.Clone<FPSInputComponent>(this);
    }
    public void CopyFrom(FPSInputComponent? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      this.rotX = other.rotX;
      this.rotY = other.rotY;
      this.View = other.View;
    }
    public override void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
      bw.Write(rotX);
      bw.Write(rotY);
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      base.Deserialize(br, version);
      Gu.BRThrowNotImplementedException();
    }

  }//FpsInputComponent


}