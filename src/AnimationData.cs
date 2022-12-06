using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.Serialization;
namespace Loft
{
  public enum KeyframeInterpolation
  {
    Step,
    Linear,
    Ease,
    Cubic,
    Slerp,
  }

  [DataContract]
  public class KeyframeBase
  {
    [DataMember] private double _time = 0;
    [DataMember] public KeyframeInterpolation _interpolation = KeyframeInterpolation.Cubic;

    public double Time { get { return _time; } set { _time = value; } }
    public KeyframeInterpolation Interpolation { get { return _interpolation; } set { _interpolation = value; } }

    public KeyframeBase() { }
  }
  [DataContract]
  public class Vec3Keyframe : KeyframeBase
  {
    [DataMember] private vec3 _value = new vec3(0, 0, 0);

    public vec3 Value { get { return _value; } set { _value = value; } }

    public Vec3Keyframe() { }
    public Vec3Keyframe(double t, vec3 p, KeyframeInterpolation n) { Time = t; Value = p; Interpolation = n; }
    public Vec3Keyframe Clone()
    {
      return (Vec3Keyframe)this.MemberwiseClone();
    }
  }
  [DataContract]
  public class QuatKeyframe : KeyframeBase
  {
    [DataMember] private quat _value = quat.Identity;

    public quat Value { get { return _value; } set { _value = value; } }

    public QuatKeyframe() { }
    public QuatKeyframe(double t, quat r, KeyframeInterpolation n) { Time = t; Value = r; Interpolation = n; }
    public QuatKeyframe Clone()
    {
      return (QuatKeyframe)this.MemberwiseClone();
    }
  }
  [DataContract]
  public class KeyframeData : DataBlock
  {
    //animation clip data for a node / joint
    public List<Vec3Keyframe>? PosChannel { get { return _posChannel; } private set { _posChannel = value; } }
    public List<QuatKeyframe>? RotChannel { get { return _rotChannel; } private set { _rotChannel = value; } }
    public List<Vec3Keyframe>? ScaleChannel { get { return _scaleChannel; } private set { _scaleChannel = value; } }
    public double MaxTime { get { return _maxTime; } }

    [DataMember] private List<Vec3Keyframe>? _posChannel = null;
    [DataMember] private List<QuatKeyframe>? _rotChannel = null;
    [DataMember] private List<Vec3Keyframe>? _scaleChannel = null;
    [DataMember] private double _maxTime = 0;

    public KeyframeData(string name) : base(name) { }

    public void FillRot(float[] times, quat[] rots, KeyframeInterpolation interp, bool data_is_wxyz, bool flipy_gl)
    {
      Gu.Assert(times != null);
      Gu.Assert(rots != null);
      Gu.Assert(times.Length == rots.Length);
      RotChannel = RotChannel.ConstructIfNeeded();
      for (var i = 0; i < times.Length; ++i)
      {
        quat q = rots[i];
        if (data_is_wxyz) // in GLTF
        {
          var w = q.x;
          q.x = q.y;
          q.y = q.z;
          q.z = q.w;
          q.w = w;
        }
        if (flipy_gl)
        {
          var tmp = q.y;
          q.y = q.z;
          q.z = tmp;
        }

        //Normalize double precision due to some rounding errors (C# or export -- not sure)
        //q = q.toQuatD().normalized().toQuatF();

        RotChannel.Add(new QuatKeyframe((double)times[i], q, interp));
      }
    }
    public void FillPos(float[] times, vec3[] vals, KeyframeInterpolation interp)
    {
      Gu.Assert(times != null);
      Gu.Assert(vals != null);
      Gu.Assert(times.Length == vals.Length);
      PosChannel = PosChannel.ConstructIfNeeded();
      for (var i = 0; i < times.Length; ++i)
      {
        PosChannel.Add(new Vec3Keyframe((double)times[i], vals[i], interp));
      }
    }
    public void FillScale(float[] times, vec3[] vals, KeyframeInterpolation interp)
    {
      Gu.Assert(times != null);
      Gu.Assert(vals != null);
      Gu.Assert(times.Length == vals.Length);
      ScaleChannel = ScaleChannel.ConstructIfNeeded();
      for (var i = 0; i < times.Length; ++i)
      {
        ScaleChannel.Add(new Vec3Keyframe((double)times[i], vals[i], interp));
      }
    }
    public void AddFrame(double time, vec3? pos, quat? rot, vec3? scl)
    {
      KeyframeInterpolation posInterp = KeyframeInterpolation.Ease;
      KeyframeInterpolation rotInterp = KeyframeInterpolation.Slerp;
      KeyframeInterpolation sclInterp = KeyframeInterpolation.Ease;
      if (pos != null)
      {
        PosChannel = PosChannel.ConstructIfNeeded();
        PosChannel.Add(new Vec3Keyframe(time, pos.Value, posInterp));
      }
      if (rot != null)
      {
        RotChannel = RotChannel.ConstructIfNeeded();
        RotChannel.Add(new QuatKeyframe(time, rot.Value, rotInterp));
      }
      if (scl != null)
      {
        ScaleChannel = ScaleChannel.ConstructIfNeeded();
        ScaleChannel.Add(new Vec3Keyframe(time, scl.Value, sclInterp));
      }
    }
    public mat4 Animate(double time)
    {
      KeyframeBase? c = null, n = null;
      double t = 0;
      mat4 m = mat4.Identity;

      if (PosChannel != null && PosChannel.Count > 0)
      {
        NextFrame(time, PosChannel.OfType<KeyframeBase>().ToList(), ref c, ref n, ref t);
        if (c != null && n == null)
        {
          m.SetTranslation((c as Vec3Keyframe).Value);
        }
        else if (c != null && n != null)
        {
          var pos = InterpolateV3((n as Vec3Keyframe).Interpolation, (c as Vec3Keyframe).Value, (n as Vec3Keyframe).Value, t);
          m.SetTranslation(pos);
        }
      }
      if (RotChannel != null && RotChannel.Count > 0)
      {
        NextFrame(time, RotChannel.OfType<KeyframeBase>().ToList(), ref c, ref n, ref t);
        if (c != null && n == null)
        {
          var r = mat4.getRotation((c as QuatKeyframe).Value);
          m = m * r;
        }
        else if (c != null && n != null)
        {
          var r = mat4.getRotation((c as QuatKeyframe).Value.slerpTo((n as QuatKeyframe).Value, (float)t));
          m = m * r;
        }
      }
      if (ScaleChannel != null && ScaleChannel.Count > 0)
      {
        NextFrame(time, ScaleChannel.OfType<KeyframeBase>().ToList(), ref c, ref n, ref t);
        if (c != null && n == null)
        {
          m = m * mat4.getScale((c as Vec3Keyframe).Value);
        }
        else if (c != null && n != null)
        {
          m = m * mat4.getScale(InterpolateV3((n as Vec3Keyframe).Interpolation, (c as Vec3Keyframe).Value, (n as Vec3Keyframe).Value, t));
        }
      }

      return m;
    }
    private void NextFrame(double time, List<KeyframeBase> frames, ref KeyframeBase? current, ref KeyframeBase? next, ref double out_time)
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
          if (k0.Time <= time && k1.Time >= time)
          {
            f0 = k0;
            f1 = k1;
            break;
          }
        }
        if (f0 == null || f1 == null)
        {
          //This should not be possible as the MaxTime must be the last of the keyframes.
          if (time > frames[frames.Count - 1].Time)
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
          out_time = (time - f0.Time) / denom;
        }
        current = f0;
        next = f1;
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
        return StepInterpolate(f0, f1, slerpTime);
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
    private vec3 StepInterpolate(vec3 a, vec3 b, double time)
    {
      vec3 ret = a;
      return ret;
    }
    public void SortAndCalculate()
    {
      //Sort
      PosChannel?.Sort((x, y) => x.Time.CompareTo(y.Time));
      RotChannel?.Sort((x, y) => x.Time.CompareTo(y.Time));
      ScaleChannel?.Sort((x, y) => x.Time.CompareTo(y.Time));

      //Calc max time.
      _maxTime = 0;
      if (PosChannel != null) { _maxTime = Math.Max(PosChannel[PosChannel.Count - 1].Time, _maxTime); }
      if (RotChannel != null) { _maxTime = Math.Max(RotChannel[RotChannel.Count - 1].Time, _maxTime); }
      if (ScaleChannel != null) { _maxTime = Math.Max(ScaleChannel[ScaleChannel.Count - 1].Time, _maxTime); }
    }
  }//cls
  public class AnimationData : DataBlock
  {
    //really we need everything together.
    // we need max time to be the max time for the node, and all arm joints.
    public Dictionary<string, KeyframeData> Animations = new Dictionary<string, KeyframeData>();
  }
  [DataContract]
  public class AnimationClip
  {
    //animation instatnce
    // contains paramteres and sequencer time to play all the keyframe data the node has (joints + node)
    public string Name { get { return _name; } }
    public AnimationPlayMode PlayMode { get { return _playMode; } set { _playMode = value; } }
    public AnimationTransition Transition { get { return _transition; } set { _transition = value; } }
    public float RepeatAt { get { return _repeatAt; } set { _repeatAt = value; } } //time to repeat the animation, in seconds
    public float Speed { get { return _speed; } set { _speed = value; } } //[-inf,inf] negative speed is backwards
    public ActionState State { get { return _state; } } //[-inf,inf] negative speed is backwards
    public double Time { get { return _time; } } //[-inf,inf] negative speed is backwards

    [DataMember] private string _name = "";
    [DataMember] private AnimationPlayMode _playMode = AnimationPlayMode.Linear;
    [DataMember] private AnimationTransition _transition = AnimationTransition.Stop;
    [DataMember] private float _repeatAt = 0; //time to repeat the animation, in seconds
    [DataMember] private float _speed = 1; //[-inf,inf] negative speed is backward
    [DataMember] private ActionState _state = ActionState.Run;
    [DataMember] private double _time = 0;
    [DataMember] private float _fade = 0;
    [DataMember] private bool _ping = false;

    public float End = 10;

    public AnimationClip(string name, AnimationTransition transition = AnimationTransition.Repeat, float speed = 1)
    {
      _name = name;
      _transition = transition;
      _speed = speed;

    }
    public void Update(double dt, double end)
    {
      if (_state == ActionState.Run)
      {
        UpdateSequencer(dt, End);
      }
    }
    private void UpdateSequencer(double dt, double maxTime)
    {
      //Compute play direction pingpong/fw/bw
      float direction = 1;
      if (PlayMode == AnimationPlayMode.Linear)
      {
        direction = 1;
      }
      else if (PlayMode == AnimationPlayMode.PingPong)
      {
        if (_ping)
        {
          direction = 1;
        }
        else
        {
          direction = -1;
        }
      }

      _time += Speed * dt * direction;

      if (_time > maxTime)
      {
        if (Transition == AnimationTransition.Repeat)
        {
          _time = RepeatAt + (_time - RepeatAt) % maxTime;
          _ping = !_ping;
        }
        else
        {
          Stop();
        }
      }
      else if (_time < 0)
      {
        if (Transition == AnimationTransition.Repeat)
        {
          _time = RepeatAt + (_time - RepeatAt) + (_time - RepeatAt) % maxTime;
          _ping = !_ping;
        }
        else
        {
          Stop();
        }
      }
    }
    public void Play()
    {
      _state = ActionState.Run;
    }
    public void Stop()
    {
      _time = 0;
      _state = ActionState.Stop;
    }
    public void Pause()
    {
      _state = ActionState.Pause;
    }
  }//cls
  [DataContract]
  public class ArmatureData : DataBlock
  {
    //there could be 1000's of joints, so we need to minimize joint data to just an inv bind matrix
    [DataMember] private string Name = "";
    [DataMember] public mat4[] InvBinds = new mat4[0];
    [DataMember] public mat4[] Bind = new mat4[0];
    [DataMember] public AnimationData[] JointAnims = new AnimationData[0];
    [DataMember] public int[] JointParents = new int[0];
    // [DataMember] public int[] JointIds = new int[0];
    //joint ids should be in order breadth first.
    public ArmatureData(string name) : base(name) { }
  }
  [DataContract]
  public class Armature : WorldObject
  {
    //we would need some kind of instance data due to the fact that jointdata and armature data are spec datas and matrix palette are instance datas
    [DataMember] public ArmatureData? Data = null;
    [DataMember] public mat4[] Matrices = null;
    private KeyframeData[] _currentKeys = null;

    public GPUBuffer _gpuMats; //makes more sense to copy all this one time for all instances

    public Armature(string name) : base(name)
    {
    }
    public override void Update(double dt, ref Box3f parentBoundBox)
    {
      //animation would be mush faster on gpu
      base.Update(dt, ref parentBoundBox);
      if (_currentClip != null)
      {
        if (_currentKeys != null)
        {
          for (int jid = 0; jid < Data.InvBinds.Length; jid++)
          {
            if (_currentKeys[jid] != null)
            {
              mat4 anim = _currentKeys[jid].Animate(_currentClip.Time);
              Matrices[jid] = Data.InvBinds[jid] * anim;
            }
            else
            {
              Matrices[jid] = Data.InvBinds[jid]; //idk rn
            }
          }
        }
      }
      else
      {
        _currentKeys = null;
      }
    }
    public override void Play(AnimationClip ac)
    {
      base.Play(ac);
      if (_currentClip != null)
      {
        _currentKeys = new KeyframeData[Data.InvBinds.Length];

        for (int jid = 0; jid < Data.InvBinds.Length; jid++)
        {
          if (Data.JointAnims[jid] != null)
          {
            if (Data.JointAnims[jid].Animations.TryGetValue(_currentClip.Name, out var janim))
            {
              Matrices = new mat4[Data.InvBinds.Length];
              if (_gpuMats == null)
              {
                _gpuMats = Gpu.CreateShaderStorageBuffer<mat4>(this.Name+"-bones", Matrices);
              }
              _currentKeys[jid] = janim;
            }
            else
            {
              _currentKeys[jid] = null;
            }

          }
        }
      }
    }


  }//cls



}//ns