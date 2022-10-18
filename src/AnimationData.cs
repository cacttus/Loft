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
  public class KeyframeBase : ISerializeBinary, IClone, ICopy<KeyframeBase>
  {
    [DataMember] private double _time = 0;
    [DataMember] public KeyframeInterpolation _interpolation = KeyframeInterpolation.Cubic;

    public double Time { get { return _time; } set { _time = value; } }
    public KeyframeInterpolation Interpolation { get { return _interpolation; } set { _interpolation = value; } }

    public KeyframeBase() { }
    public virtual object? Clone(bool? shallow = null)
    {
      KeyframeBase k = new KeyframeBase();
      k.CopyFrom(this, shallow);
      return k;
    }
    public void CopyFrom(KeyframeBase? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      this._time = other._time;
      this._interpolation = other._interpolation;
    }
    public virtual void Serialize(BinaryWriter bw)
    {
      bw.Write(Time);
      bw.Write((Int32)Interpolation);
    }
    public virtual void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      Time = br.ReadDouble();
      Interpolation = (KeyframeInterpolation)br.ReadInt32();
    }
  }
  [DataContract]
  public class Vec3Keyframe : KeyframeBase, ISerializeBinary, IClone, ICopy<Vec3Keyframe>
  {
    [DataMember] private vec3 _value = new vec3(0, 0, 0);

    public vec3 Value { get { return _value; } set { _value = value; } }

    public Vec3Keyframe() { }
    public Vec3Keyframe(double t, vec3 p, KeyframeInterpolation n) { Time = t; Value = p; Interpolation = n; }
    public override object? Clone(bool? shallow = null)
    {
      Vec3Keyframe k = new Vec3Keyframe();
      k.CopyFrom(this, shallow);
      return k;
    }
    public void CopyFrom(Vec3Keyframe? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      base.CopyFrom(other, shallow);
      this._value = other._value;
    }
    public override void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
      bw.Write(Value);
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      base.Deserialize(br, version);
      Value = br.ReadVec3();
    }

  }
  [DataContract]
  public class QuatKeyframe : KeyframeBase, ISerializeBinary, IClone, ICopy<QuatKeyframe>
  {
    [DataMember] private quat _value = quat.identity();

    public quat Value { get { return _value; } set { _value = value; } }

    public QuatKeyframe() { }
    public QuatKeyframe(double t, quat r, KeyframeInterpolation n) { Time = t; Value = r; Interpolation = n; }
    public override object? Clone(bool? shallow = null)
    {
      QuatKeyframe k = new QuatKeyframe();
      k.CopyFrom(this, shallow);
      return k;
    }
    public void CopyFrom(QuatKeyframe? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      base.CopyFrom(other, shallow);
      this._value = other._value;
    }
    public override void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
      bw.Write(Value);
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      base.Deserialize(br, version);
      Value = br.ReadQuat();
    }
  }

  [DataContract]
  public class AnimationData : DataBlock, ISerializeBinary, IClone, ICopy<AnimationData> /*technically not a datablock, because this is generated from the .glb, however if we discard glb in hte future it would be*/
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
    public object? Clone(bool? shallow = null)
    {
      AnimationData? k = new AnimationData();
      k.CopyFrom(this, shallow);
      return k;
    }
    public void CopyFrom(AnimationData? other, bool? shallow = null)
    {
      Gu.Assert(other != null);
      base.CopyFrom(other, shallow);
      if (other._posChannel != null) { this._posChannel = new List<Vec3Keyframe>(other._posChannel); }
      if (other._rotChannel != null) { this._rotChannel = new List<QuatKeyframe>(other._rotChannel); }
      if (other._scaleChannel != null) { this._scaleChannel = new List<Vec3Keyframe>(other._scaleChannel); }
    }
    public override void Serialize(BinaryWriter bw)
    {
      base.Serialize(bw);
      SerializeTools.SerializeList<Vec3Keyframe>(bw, _posChannel);
      SerializeTools.SerializeList<QuatKeyframe>(bw, _rotChannel);
      SerializeTools.SerializeList<Vec3Keyframe>(bw, _scaleChannel);
    }
    public override void Deserialize(BinaryReader br, SerializedFileVersion version)
    {
      base.Deserialize(br, version);
      _posChannel = SerializeTools.DeserializeList<Vec3Keyframe>(br, version);
      _rotChannel = SerializeTools.DeserializeList<QuatKeyframe>(br, version);
      _scaleChannel = SerializeTools.DeserializeList<Vec3Keyframe>(br, version);
    }


  }

}