using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PirateCraft
{
  public enum CameraType
  {
    Follow//follow cam
  }
  public enum CoordinateSystem
  {
    Rhs,
    Lhs
  }
  public enum ProjectionMode
  {
    Orthographic,
    Perspective
  }
  public enum TransformSpace
  {
    World,
    Local
  }
  public enum ConstraintType
  {
    LookAt
  }
  public enum RotationType
  {
    AxisAngle,
    Quaternion
  }
  public enum FileStorage
  {
    Disk,
    Embedded,
    Web,
    Generated,
  }
}
