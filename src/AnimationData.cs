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
    [DataMember] public double _time = 0;
    [DataMember] public int _index = 0;
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
    public class CComparer : IComparer<Vec3Keyframe>
    {
      public int Compare(Vec3Keyframe? a, Vec3Keyframe? b)
      {
        return a._time < b._time ? -1 : 1;
      }

      public int GetHashCode(Vec3Keyframe a)
      {
        return a.GetHashCode();
      }
    }
    public static CComparer Comparer = new CComparer();
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
    public class CComparer : IComparer<QuatKeyframe>
    {
      public int Compare(QuatKeyframe? a, QuatKeyframe? b)
      {
        return a._time < b._time ? -1 : 1;
      }

      public int GetHashCode(QuatKeyframe a)
      {
        return a.GetHashCode();
      }
    }
    public static CComparer Comparer = new CComparer();
  }
  [DataContract]
  public class KeyframeData : DataBlock
  {
    //animation clip data for a node / joint
    public List<Vec3Keyframe>? PosChannel { get { return _posChannel; } private set { _posChannel = value; } }
    public List<float>? PosChannelT { get { return _posChannelT; } private set { _posChannelT = value; } }
    public List<QuatKeyframe>? RotChannel { get { return _rotChannel; } private set { _rotChannel = value; } }
    public List<float>? RotChannelT { get { return _rotChannelT; } private set { _rotChannelT = value; } }
    public List<Vec3Keyframe>? ScaleChannel { get { return _scaleChannel; } private set { _scaleChannel = value; } }
    public List<float>? ScaleChannelT { get { return _scaleChannelT; } private set { _scaleChannelT = value; } }
    public double MaxTime { get { return _maxTime; } }

    [DataMember] private List<Vec3Keyframe>? _posChannel = null;
    [DataMember] private List<QuatKeyframe>? _rotChannel = null;
    [DataMember] private List<Vec3Keyframe>? _scaleChannel = null;
    [DataMember] private List<float>? _posChannelT = null;
    [DataMember] private List<float>? _rotChannelT = null;
    [DataMember] private List<float>? _scaleChannelT = null;
    [DataMember] private double _maxTime = 0;

    public KeyframeData(string name) : base(name) { }

    public void FillRot(float[] times, quat[] rots, KeyframeInterpolation interp, bool data_is_wxyz, bool flipy_gl)
    {
      Gu.Assert(times != null);
      Gu.Assert(rots != null);
      Gu.Assert(times.Length == rots.Length);
      RotChannel = RotChannel.ConstructIfNeeded();
      RotChannelT = RotChannelT.ConstructIfNeeded();
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
        RotChannel[RotChannel.Count - 1]._index = i;
        RotChannelT.Add(times[i]);
      }
    }
    public void FillPos(float[] times, vec3[] vals, KeyframeInterpolation interp)
    {
      Gu.Assert(times != null);
      Gu.Assert(vals != null);
      Gu.Assert(times.Length == vals.Length);
      PosChannel = PosChannel.ConstructIfNeeded();
      PosChannelT = PosChannelT.ConstructIfNeeded();
      for (var i = 0; i < times.Length; ++i)
      {
        PosChannel.Add(new Vec3Keyframe((double)times[i], vals[i], interp));
        PosChannel[PosChannel.Count - 1]._index = i;
        PosChannelT.Add(times[i]);
      }
    }
    public void FillScale(float[] times, vec3[] vals, KeyframeInterpolation interp)
    {
      Gu.Assert(times != null);
      Gu.Assert(vals != null);
      Gu.Assert(times.Length == vals.Length);
      ScaleChannel = ScaleChannel.ConstructIfNeeded();
      ScaleChannelT = ScaleChannelT.ConstructIfNeeded();
      for (var i = 0; i < times.Length; ++i)
      {
        ScaleChannel.Add(new Vec3Keyframe((double)times[i], vals[i], interp));
        ScaleChannel[ScaleChannel.Count - 1]._index = i;
        ScaleChannelT.Add(times[i]);
      }
    }
    // public void AddFrame(double time, vec3? pos, quat? rot, vec3? scl)
    // {
    //   KeyframeInterpolation posInterp = KeyframeInterpolation.Ease;
    //   KeyframeInterpolation rotInterp = KeyframeInterpolation.Slerp;
    //   KeyframeInterpolation sclInterp = KeyframeInterpolation.Ease;
    //   if (pos != null)
    //   {
    //     PosChannel = PosChannel.ConstructIfNeeded();
    //     PosChannel.Add(new Vec3Keyframe(time, pos.Value, posInterp));
    //   }
    //   if (rot != null)
    //   {
    //     RotChannel = RotChannel.ConstructIfNeeded();
    //     RotChannel.Add(new QuatKeyframe(time, rot.Value, rotInterp));
    //   }
    //   if (scl != null)
    //   {
    //     ScaleChannel = ScaleChannel.ConstructIfNeeded();
    //     ScaleChannel.Add(new Vec3Keyframe(time, scl.Value, sclInterp));
    //   }
    // }
    public mat4 Animate(float time)
    {
      mat4 m = mat4.Identity;
      float t = 0;
      int a = -1, b = -1;

      if (PosChannel != null && PosChannel.Count > 0)
      {
        // Vec3Keyframe? c = null, n = null;
        NextFrame(time, PosChannelT, ref a, ref b, ref t);
        if (a != -1 && b == -1)
        {
          m.SetTranslation(PosChannel[a].Value);
        }
        else if (a != -1 && b != -1)
        {
          var pos = InterpolateV3(PosChannel[b].Interpolation, PosChannel[a].Value, PosChannel[b].Value, t);
          m.SetTranslation(pos);
        }
      }
      if (RotChannel != null && RotChannel.Count > 0)
      {
        //QuatKeyframe? c = null, n = null;
        NextFrame(time, RotChannelT, ref a, ref b, ref t);
        if (a != -1 && b == -1)
        {
          var r = mat4.getRotation(RotChannel[a].Value);
          m = m * r;
        }
        else if (a != -1 && b != -1)
        {
          var r = mat4.getRotation(RotChannel[a].Value.slerpTo(RotChannel[b].Value, t));
          m = m * r;
        }
      }
      if (ScaleChannel != null && ScaleChannel.Count > 0)
      {
        // Vec3Keyframe? c = null, n = null;
        NextFrame(time, ScaleChannelT, ref a, ref b, ref t);
        if (a != -1 && b == -1)
        {
          m = m * mat4.getScale(ScaleChannel[a].Value);
        }
        else if (a != -1 && b != -1)
        {
          m = m * mat4.getScale(InterpolateV3(ScaleChannel[b].Interpolation, ScaleChannel[a].Value, ScaleChannel[b].Value, t));
        }
      }
      return m;
    }
    public class ListUtils
    {
      public static int First_LE(List<float> list, float value)
      {
        //Find index of element <= value
        // return the end of list if last element is <= value
        //List must be sorted
        int count = list.Count;

        if (count == 0) { return -1; }
        if (count == 1) { return 0; }
        if (value > list[list.Count - 1]) { return list.Count - 1; }
        if (value <= list[0]) { return 0; }

        int pos = 0;//count / 2;
        int len = count;
        float left = 0;
        float right = 0;

        for (int i = 0; i < 32; i++)
        {
#if DEBUG
          if (i >= 32 - 1)
          {
            Gu.DebugBreak();
          }
#endif
          //outlier vs bounds 
          //This is more bounds checking
          if (pos >= 0 && pos < count)
          {
            if (pos + 1 >= 0 && pos + 1 < count)
            {
              left = list[pos];
              right = list[pos + 1];
            }
            else
            {
              //outside right
              left = list[pos];
              right = value + 1;//this is kind of hacky and fp math may be a performance problem. wish to fix it. just remove it and add the logic below
            }
          }
          else
          {
            if (pos + 1 >= 0 && pos + 1 < count)
            {
              //outside left
              left = value - 1;
              right = list[pos + 1];
            }
            else
            {
              //also outside left
              return 0;
            }
          }

          if (left <= value && right > value)
          {
            //found
            return pos;
          }

          if (len == 1)
          {
            break;//we should be at the beginning of the list if this works correctly.
          }
          if (len % 2 == 0)
          {
            len /= 2;
          }
          else
          {
            len = len / 2 + 1;
          }

          if (left == value && right == value)
          {
            //go left since we want "first"
            pos -= len;
          }
          else if (left > value)
          {
            //go left
            pos -= len;
          }
          else
          {
            //go right
            pos += len;
          }

        }

        return -1;
      }
      public static void Test_FirstLE()
      {
        //test the above function
        int testcount = 4;
        List<float>[] test0 = new List<float>[]
        {
        new List<float>{ 0.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f }
        ,new List<float>{ 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f }
        ,new List<float>{ 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f }
        ,new List<float>{ 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f }
        };
        List<float>[] test1 = new List<float>[]
        {
        new List<float>{ 0.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f }
        ,new List<float>{ 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f }
        ,new List<float>{ 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f }
        ,new List<float>{ 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f }
        };

        //TODO: expected results.
        float[] data = new float[]
        {
        2.5f
        ,1.0f
        ,4.5f
        ,0.0f
        ,5.0f
        ,6.0f
        ,-1.0f
        ,-1988205.04223252f
        ,198205.04234252f
        ,float.NaN
        };

        List<List<int>[]> results = new List<List<int>[]>();
        for (int ti = 0; ti < testcount; ti++)
        {
          var output = new List<int>[2];
          results.Add(output);
          output[0] = new List<int>();
          output[1] = new List<int>();

          Console.Write($"t0[{ti}]=");
          foreach (int x in data)
          {
            int val = ListUtils.First_LE(test0[ti], x);
            output[0].Add(val);
            Console.Write(val + $"({x}),");
          }
          Console.WriteLine();
          Console.Write($"t1[{ti}]=");

          foreach (int x in data)
          {
            int val = ListUtils.First_LE(test1[ti], x);
            output[1].Add(val);
            Console.Write(val + $"({x}),");
          }
          Console.WriteLine();
        }
      }
    }
    private void NextFrame(float time, List<float> times, ref int current, ref int next, ref float out_time)
    {

      current = -1;
      next = -1;
      out_time = 0;
      if (times == null)
      {
        //empty channel
      }
      else if (times.Count == 0)
      {
        //No animation.
      }
      else if (times.Count == 1)
      {
        //Set to this key frame.
        current = 0;// frames[0];
      }
      else
      {
        int f0 = -1, f1 = -1;

        //Grab 0 and 1
        //unfortunately this only saved a small amount of time, perhaps when we have thousands of keyframes it will.
        var idx = ListUtils.First_LE(times, time);
        // //.net where is slower than for loop on linx
        // // var xx = frames.Where(x => x._time < time).LastOrDefault();
        // // if (xx == null)
        // // {
        // //   xx = frames[0];
        // // }
        // if (idx + 1 < times.Count)
        // {
        //   f0 = idx;
        //   f1 = idx + 1;//frames[xx._index + 1];
        // }
        // for (int i = 0; i < times.Count - 1; i++)
        // {
        //   if (times[i] <= time && times[i+1]>= time)
        //   {
        //     f0 = i;
        //     f1 = i+1;
        //     break;
        //   }
        // }
        if (f0 == -1 || f1 == -1)
        {
          //This should not be possible as the MaxTime must be the last of the keyframes.
          if (time > times[times.Count - 1])
          {
            f0 = times.Count - 1;//times[times.Count - 1];
            f1 = f0;
          }
          else //if (Time < KeyFrames[0].Time)
          {
            f0 = 0;//times[0];
            f1 = f0;
          }
        }

        Gu.Assert(f0 != -1 && f1 != -1);
        var denom = times[f1] - times[f0];
        if (denom == 0)
        {
          out_time = 1; //If f0 and f1 are euqal in time it's an error.
        }
        else
        {
          out_time = (time - times[f0]) / denom;
        }
        current = f0;
        next = f1;
      }
    }
    private vec3 InterpolateV3(KeyframeInterpolation interp, vec3 f0, vec3 f1, float slerpTime)
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
    private vec3 LinearInterpolate(vec3 a, vec3 b, float time)
    {
      vec3 ret = a + (b - a) * (float)time;
      return ret;
    }
    private vec3 EaseInterpolate(vec3 a, vec3 b, float time)
    {
      //Sigmoid "ease"
      //Assuming time is normalized [0,1]
      float k = 0.1f; //Slope
      float f = 1.0f / (1.0f + (float)Math.Exp(-((time - 0.5f) / k)));
      return a * (1.0f - (float)f) + b * (float)f;
    }
    private vec3 CubicInterpolate(vec3 a, vec3 b, float time)
    {
      //This is actually cosine interpolate. We need to update this to be cubic.
      //TODO:
      float ft = time * MathUtils.M_PI;
      float f = (1.0f - (float)Math.Cos(ft)) * 0.5f;
      return a * (1.0f - (float)f) + b * (float)f;
    }
    private vec3 StepInterpolate(vec3 a, vec3 b, float time)
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

      PosChannelT?.Sort((x, y) => x.CompareTo(y));
      RotChannelT?.Sort((x, y) => x.CompareTo(y));
      ScaleChannelT?.Sort((x, y) => x.CompareTo(y));

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
              Matrices[jid] = Data.InvBinds[jid] * _currentKeys[jid].Animate((float)_currentClip.Time);
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
                _gpuMats = Gpu.CreateShaderStorageBuffer<mat4>(this.Name + "-bones", Matrices);
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