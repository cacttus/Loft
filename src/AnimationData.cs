using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.Serialization;
namespace PirateCraft
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
    [DataMember] private quat _value = quat.identity();

    public quat Value { get { return _value; } set { _value = value; } }

    public QuatKeyframe() { }
    public QuatKeyframe(double t, quat r, KeyframeInterpolation n) { Time = t; Value = r; Interpolation = n; }
    public QuatKeyframe Clone()
    {
      return (QuatKeyframe)this.MemberwiseClone();
    }
  }
  [DataContract]
  //technically not a datablock, because this is generated from the .glb, however if we discard glb in hte future it would be
  public class AnimationData : DataBlock
  {
    //We may end up compressing the data blocks here.
    public AnimationData(string name) : base(name)
    {
    }
    public List<Vec3Keyframe>? PosChannel { get { return _posChannel; } private set { _posChannel = value; } }
    public List<QuatKeyframe>? RotChannel { get { return _rotChannel; } private set { _rotChannel = value; } }
    public List<Vec3Keyframe>? ScaleChannel { get { return _scaleChannel; } private set { _scaleChannel = value; } }

    [DataMember] private List<Vec3Keyframe>? _posChannel = null;
    [DataMember] private List<QuatKeyframe>? _rotChannel = null;
    [DataMember] private List<Vec3Keyframe>? _scaleChannel = null;

    public AnimationData() { }
    public void FillRot(float[] times, quat[] rots, KeyframeInterpolation interp, bool data_is_wxyz, bool flipy_gl)
    {
      Gu.Assert(times != null);
      Gu.Assert(rots != null);
      Gu.Assert(times.Length == rots.Length);
      RotChannel = RotChannel.ConstructIfNeeded();
      for (var i = 0; i < times.Length; ++i)
      {
        quat q = rots[i];
        if (data_is_wxyz)
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
    public AnimationData Clone()
    {
      AnimationData k = (AnimationData)this.MemberwiseClone();

      //Deep copy refs.
      if (_posChannel != null) { k._posChannel = new List<Vec3Keyframe>(_posChannel); }
      if (_rotChannel != null) { k._rotChannel = new List<QuatKeyframe>(_rotChannel); }
      if (_scaleChannel != null) { k._scaleChannel = new List<Vec3Keyframe>(_scaleChannel); }

      return k;
    }
  }

}