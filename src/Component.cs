using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
namespace Loft
{
  public enum LightType
  {
    Point, Direction
  }
  public enum ComponentState
  {
    Added,
    Initialized,
    Destroyed
  }
  public enum AnimationTransition
  {
    Repeat, Stop, PlayNext
  }
  public enum AnimationPlayMode
  {
    Linear, //linear play - for backwards set _speed = -speed
    PingPong
  }
  [DataContract]
  public abstract class Component /*Components are unique to objects, they are not shared the *data* a component references may be shared though */
  {
    public bool Enabled { get { return _enabled; } set { _enabled = value; } }
    public ComponentState ComponentState { get { return _componentState; } set { _componentState = value; } }

    [DataMember] private bool _enabled = true;
    private ComponentState _componentState = ComponentState.Added;

    public Component() { }

    #region Abstract methods

    public virtual void OnCreate(WorldObject myObj) { }
    public virtual void OnUpdate(double dt, WorldObject myObj) { }
    public virtual void OnPick() { }
    public virtual void OnDestroy(WorldObject myObj) { }
    public abstract Component Clone();

    #endregion

    public override string ToString()
    {
      string json = SerializeTools.SerializeJSON(this);
      return json;
    }
  }
  [DataContract]
  public class EventComponent : Component
  {
    //Executes an action on an object for a given interval
    [DataMember] public DeltaTimer Timer { get; private set; } = null;
    public Action<WorldObject>? Action { get; set; } = null;

    public EventComponent() { }
    public EventComponent(Action<WorldObject>? action, double tick_seconds, ActionRepeat repeat, ActionState start)
    {
      Action = action;
      Timer = new DeltaTimer((long)(tick_seconds * 1000.0), repeat, start, null);
    }
    public override void OnUpdate(double dt, WorldObject myObj)
    {
      int fires = Timer.Update(dt);
      for (int x = 0; x < fires; x++)
      {
        Action?.Invoke(myObj);
      }
    }
    public void Start()
    {
      Timer.Start();
    }
    public void Stop()
    {
      Timer.Stop();
    }
    public override Component Clone()
    {
      return (EventComponent)this.MemberwiseClone();
    }

  }
  [DataContract]
  public class FPSInputComponent : Component
  {
    public enum FPSCamMode
    {
      Playing, Flying
    }
    private const float Base_Speed = 10.0f * 0.10f;
    private const float Run_Mul = 6;
    private const float Creep_Mul = 0.1f;
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

    private RenderView _rv;

    public FPSInputComponent(RenderView cam) { _rv = cam; }

    public override void OnCreate(WorldObject myObj)
    {
      if (myObj.PhysicsData != null)
      {
        myObj.PhysicsData.HasGravity = false;
        myObj.PhysicsData.Enabled = true; // camera moves with velocity vector
      }
    }
    public override void OnUpdate(double dt, WorldObject myObj)
    {
      if (!Gu.Context.GameWindow.IsFocused)
      {
        return;
      }
      if (!Gu.TryGetSelectedView(out var rv))
      {
        return;
      }
      if (_rv != rv)
      {
        return;
      }

      if (myObj.PhysicsData != null)
      {
        myObj.PhysicsData.AirFriction = MaxAirFriction; //Movement Damping
      }

      vec3basis basis = new vec3basis();
      //removing cammode for now
      // if (CamMode == FPSCamMode.Flying)
      // {
      basis.x = myObj.BasisX_World;
      basis.y = myObj.BasisY_World;
      basis.z = myObj.BasisZ_World;
      // }
      // else if (CamMode == FPSCamMode.Playing)
      // {
      //   basis.x = cam.BasisX;
      //   basis.y = vec3.Zero; //no cheating
      //   basis.z = cam.BasisZ;
      // }
      DoMouse(myObj, _rv, basis);
    }
    private void DoMouse(WorldObject obj, RenderView view, vec3basis basis)
    {
      //Rotate Camera
      float width = view.Viewport.Width;
      float height = view.Viewport.Height;
      float mpx_rel = Gu.Context.PCMouse.Pos.x - view.Viewport.X;
      float mpy_rel = Gu.Context.PCMouse.Pos.y - view.Viewport.Y;
      vec2 mouse_delta_wh = new vec2(Gu.Context.PCMouse.PosDelta.x / view.Viewport.Width, Gu.Context.PCMouse.PosDelta.y / view.Viewport.Height);

      bool ms_move_editing_must_warp = false;

      //** Mimicking Blender Defaults ** 
      if (Gu.Context.PCMouse.ScrollDelta.y != 0)
      {
        obj.Position_Local += basis.z * Gu.Context.PCMouse.ScrollDelta.y * _scroll_zoom_meters_per_pixel;
        ms_move_editing_must_warp = true;
      }
      if (Gu.Context.PCMouse.PressOrDown(MouseButton.Middle))
      {
        //Allow shift or control to affect speed instead, if WSAD is down.
        bool bMoving = Gu.Context.PCKeyboard.PressOrDown(Keys.W) || Gu.Context.PCKeyboard.PressOrDown(Keys.S) || Gu.Context.PCKeyboard.PressOrDown(Keys.A) || Gu.Context.PCKeyboard.PressOrDown(Keys.D);

        if (!bMoving && (Gu.Context.PCKeyboard.PressOrDown(Keys.LeftShift) || Gu.Context.PCKeyboard.PressOrDown(Keys.RightShift)))
        {
          //Pan
          obj.Position_Local += basis.x * -mouse_delta_wh.x * _pan_meters_per_pixel;
          obj.Position_Local += basis.y * mouse_delta_wh.y * _pan_meters_per_pixel;
          ms_move_editing_must_warp = true;
        }
        else if (!bMoving && (Gu.Context.PCKeyboard.PressOrDown(Keys.LeftControl) || Gu.Context.PCKeyboard.PressOrDown(Keys.RightControl)))
        {
          //Zoom
          obj.Position_Local += basis.z * -mouse_delta_wh.y * _zoom_meters_per_pixel;
          ms_move_editing_must_warp = true;
        }
        else
        {
          DoMoveWSAD(obj, basis);

          //Rotate
          rotX -= Math.PI * 2 * mouse_delta_wh.x * _rotations_per_width * Gu.CoordinateSystemMultiplier;
          if (rotX >= Math.PI * 2.0f)
          {
            rotX = (float)(rotX % (Math.PI * 2.0f));
          }
          if (rotX <= 0)
          {
            rotX = (float)(rotX % (Math.PI * 2.0f));
          }

          rotY -= Math.PI * 2 * mouse_delta_wh.y * _half_rotations_per_height * Gu.CoordinateSystemMultiplier;
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

          obj.Rotation_Local = qx * qy;
          obj.SanitizeTransform();
          //cam.Rotation_Local = qx;
          //cam.SanitizeTransform();

          ms_move_editing_must_warp = true;
        }
        if (ms_move_editing_must_warp)
        {
          Gu.Context.PCMouse.WarpMouse(view, WarpMode.Wrap, 0.001f);
        }
      }

    }
    private void DoMoveWSAD(WorldObject myObj, vec3basis basis)
    {

      //Modify speed multiplier based on state
      float speedMul = 1; //normal speed
      if (Gu.Context.PCKeyboard.PressOrDown(Keys.LeftControl) || Gu.Context.PCKeyboard.PressOrDown(Keys.RightControl))
      {
        speedMul = Run_Mul; // run speed
      }
      if (Gu.Context.PCKeyboard.PressOrDown(Keys.LeftShift) || Gu.Context.PCKeyboard.PressOrDown(Keys.LeftShift))
      {
        speedMul = Creep_Mul; // run speed
      }
      // if (!myObj.OnGround && this.CamMode != FPSCamMode.Flying)
      // {
      //   speedMul = 0.1f; // "in the air" movement. 
      // }

      float final_run_speed = Base_Speed * speedMul;
      if (Gu.Context.PCKeyboard.PressOrDown(new List<Keys>() { Keys.Q }))
      {
        myObj.PhysicsData.Velocity += basis.y * final_run_speed;
      }
      if (Gu.Context.PCKeyboard.PressOrDown(new List<Keys>() { Keys.E }))
      {
        myObj.PhysicsData.Velocity -= basis.y * final_run_speed;
      }
      if (Gu.Context.PCKeyboard.PressOrDown(new List<Keys>() { Keys.Up, Keys.W }))
      {
        myObj.PhysicsData.Velocity += basis.z * final_run_speed;
      }
      if (Gu.Context.PCKeyboard.PressOrDown(new List<Keys>() { Keys.Down, Keys.S }))
      {
        myObj.PhysicsData.Velocity -= basis.z * final_run_speed;
      }
      if (Gu.Context.PCKeyboard.PressOrDown(new List<Keys>() { Keys.Right, Keys.D }))
      {
        myObj.PhysicsData.Velocity += basis.x * final_run_speed;
      }
      if (Gu.Context.PCKeyboard.PressOrDown(new List<Keys>() { Keys.Left, Keys.A }))
      {
        myObj.PhysicsData.Velocity -= basis.x * final_run_speed;
      }

      // if (myObj.OnGround && this.CamMode != FPSCamMode.Flying)
      // {
      //   if (Gu.Context.PCKeyboard.PressOrDown(Keys.Space))
      //   {
      //     myObj.Velocity += new vec3(0, Base_Jump_Speed, 0);
      //   }
      // }

    }
    public override Component Clone()
    {
      return (FPSInputComponent)this.MemberwiseClone();
    }

  }//FpsInputComponent
  [DataContract]
  public class ScriptComponent : Component
  {
    public override void OnUpdate(double dt, WorldObject myObj)
    {
    }
    public override Component Clone()
    {
      return (ScriptComponent)this.MemberwiseClone();
    }
  }

  [DataContract]
  public class SpringPhysics : Modifier
  {
    //do boba physics
    public virtual void Dispatch(MeshData myMesh) { }
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct wd_in_st //GpuVertJointOffset
  {
    //vertidx -> [count, jw offset, ]
    //std430
    public int wc;	//joints count
    public int wo;	//joints offset
    public float pad0;
    public float pad1;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct jw_in_st //GpuVertWeight
  {
    //[jid, weight]
    //std430
    public int joff; //offset into matrix palette, i.e. joint id
    public float wt;

    public float pad0;
    public float pad1;
  }
  [DataContract]
  public class PhysicsData : DataBlock
  {
    public bool Enabled { get { return _enabled; } set { _enabled = value; } }
    public vec3 Velocity { get { return _velocity; } set { _velocity = value; } }
    public bool OnGround { get { return _resting; } set { _resting = value; } }
    public bool HasGravity { get { return _hasGravity; } set { _hasGravity = value; } }
    public bool Collides { get { return _collides; } set { _collides = value; } }
    public float AirFriction { get { return _airFriction; } set { _airFriction = value; } }
    [DataMember] private vec3 _velocity = new vec3(0, 0, 0);
    [DataMember] private bool _resting = false;
    [DataMember] private bool _hasGravity = true;
    [DataMember] private bool _collides = false;
    [DataMember] private float _airFriction = 0.0f;//friction with the air i.e. movement damping in m/s    
    [DataMember] private bool _enabled = true;

    public PhysicsData(string name = "PhysicsData") : base(name) { }
  }
  [DataContract]
  public class Modifier
  {
    //mesh modifiers, dispatched and applied to meshes
    public enum ModifierState
    {
      None,
      Error,
      Dispatched,
      Success
    }
    public bool Enabled { get { return _enabled; } set { _enabled = value; } }
    [DataMember] private bool _enabled = true;

    protected ModifierState _state = ModifierState.None;

    protected void SetError(string e)
    {
      Gu.Log.Error(e);
      _state = ModifierState.Error;
    }

    public virtual void Dispatch(MeshData mesh) { }
  }
  [DataContract]
  public class ArmatureModifier : Modifier
  {
    //"skin" modifier, "skeleton" etc
    public GPUBuffer Influences;
    public GPUBuffer VertexWeights;
    public GPUBuffer MatrixPalette;

    public WeakReference<Armature> ArmatureRef = null;
    public ArmatureModifier(Armature arm)
    {
      Gu.Assert(arm != null);
      ArmatureRef = new WeakReference<Armature>(arm);
      //check armature data exists when we update, we may not set the data until later.
    }
    public override void Dispatch(MeshData mesh)
    {
      //compile vertex groups from mesh into a single buffer if modified.
      //dispatch compute.
    }
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct vertex_adjacency
  {
    //adjacency per vertex
    // gives us the count (_adj0.x) then vertex offset's (adj0.y-adj1.w)
    //  max 6 adjacent verts
    //    [(count0), (o00, o01, o02, o03, o04, o05, o06)],[(count1),(o10,o11,o12,o13,o14,o15,o16)],...,[(countn),(o0n,...)]
    public Svec4 _adj0;
    public Svec4 _adj1;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct physics_vert
  {
    //hair / cloth / booba vertexes
    //one phy vert for every vertex in the mesh.
    public vec3 _velocity;
    public float _mass;
    // other stuf

  }

  [DataContract]
  public class SoftbodyModifier : Modifier
  {
    //for boba - but not used right now
    //buffer of ushort 
    public GPUBuffer? _adjacency = null;
    public GPUBuffer? _physicsVerts = null;

    public float _tension;
    public float _compression;
    public float _restitution;
    public float _friction;

    public override void Dispatch(MeshData mesh)
    {
      //compile vertex groups from mesh into a single buffer if modified.
      //dispatch compute.
      if (_adjacency == null)
      {
        CreateVertexAdjacency(mesh);
      }

      if (_state != ModifierState.Error)
      {

      }
    }
    private void CreateVertexAdjacency(MeshData mesh)
    {
      List<ushort> adjacency = new List<ushort>();

      if (mesh.IndexBuffer == null)
      {
        SetError("Mesh had no index buffer. Array meshes are not supported");
        return;
      }


      var vp = new VertexPointer(mesh.IndexBuffer);
      for (int ii = 0; ii < vp.Length; ii += 3)
      {
        //iterate tris.

      }


    }
  }


}//ns 







// private Dictionary<string, AnimationTrack>;

//sequencer should hold animation data via the strng name -> to all objects
// string anim
//   object, sampler
//  Gu.Sequencer.play()
//  WorldObject girl

//AnimatedParameter { 
// float .. animat
//}
// so an aobject can have a sequence in addition to its animation
// we can cancel, or xfade the sequence which will leave the current params as-is
// girl is worldobject, but the data would be like AnimationData
//    Current Armature (skin) Animation
//    List of skin animations..
//    Object PRS Animation
//  girl.play("walk", repeat = true, speed = 1);
//  if(ctrl pressed)
//    girl.Do(
//      new Sequence(
//        girl.Delay(1s, 1s),//start, len
//        girl.Speed.Fade(2s, 1s, 2)  //whatever animation is playing on girl, speed it up by 2 at 2s, for 1s
//        girl.Play(4s, -1, "run", repeat=true//speed=current speed
//        girl.Speed.Fade(7s, 1s, 1)
//      )
//    );
//  cancel current sequence and fade to another one
//  girl.Fade(
//    new Sequence(
//     girl.move(0s, 10s, new vec3(10,10,10))
//     girl.play(10s, "run", loop=true);
//   )
//  )
//  add this new animation and blend it
//  girl.Add(
//    new Sequence(
//     girl.move(0s, 10s, new vec3(10,10,10))
//     girl.play(10s, "run", loop=true);
//   )
//  )    
// if world obj has armature data theen it has skin animation so we can sequence it
// 
//[DataMember] private double _time = 0;
//Sequencer
//move character here a
//play character animation at t
//play animation after t
//animate mesh
//animate PRS of object
