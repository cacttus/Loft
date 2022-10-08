namespace PirateCraft
{

  public abstract class Constraint : DataBlock
  {
    public abstract void Apply(WorldObject ob);
  }
  // public class FollowConstraint : Constraint
  // {
  //   public enum FollowMode
  //   {
  //     Snap,
  //     Drift
  //   }
  //   public WeakReference<WorldObject> FollowObj { get; set; } = null;
  //   public float DriftSpeed { get; set; } = 0;//meters per second
  //   public FollowMode Mode { get; set; } = FollowConstraint.FollowMode.Snap;
  //   public FollowConstraint(WorldObject followob, FollowMode mode, float drift = 0)
  //   {
  //     FollowObj = new WeakReference<WorldObject>(followob);
  //     Mode = mode;
  //     DriftSpeed = drift;
  //   }
  //   public override void Apply(WorldObject ob)
  //   {
  //     if (FollowObj != null && FollowObj.TryGetTarget(out WorldObject obj))
  //     {
  //       ob.Position_Local = obj.WorldMatrix.ExtractTranslation();
  //     }
  //     else
  //     {
  //       Gu.Log.Error("'" + ob.Name + "' - Follow constraint - object not found.");
  //     }
  //   }
  //   public override Constraint Clone(bool? shallow = null)
  //   {
  //     FollowConstraint cc = null;
  //     if (FollowObj != null && FollowObj.TryGetTarget(out var wo))
  //     {
  //       cc = new FollowConstraint(wo, Mode, DriftSpeed);
  //     }
  //     else
  //     {
  //       Gu.BRThrowException("Could not get target for cloing follow constraint.");
  //     }
  //     return cc;
  //   }
  // }
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

}