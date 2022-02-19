using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PirateCraft
{
   public enum TextureChannel
   {
      Channel0,
      Channel1,
      Channel2,
      Channel3,
      Channel4,
      Channel5,
      Channel6,
      Channel7,
      Channel8,
      Channel9,
   }
   public enum TexWrap
   {
      Clamp,
      Repeat
   }
   public enum TexFilter
   {
      Linear,
      Nearest
   }
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
   public enum RenderPipelineState
   {
      None,
      Begin,
      End
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
}
