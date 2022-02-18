using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Quat = OpenTK.Quaternion;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;

namespace PirateCraft
{
   public class DebugDraw
   {
      private static List<v_v3c4> _inlineVerts = null;
      private static List<UInt16> _inlineInds = null;
      public static void Begin()
      {
         _inlineVerts = new List<v_v3c4>();
         _inlineInds = new List<UInt16>();
      }
      public static void v3c4(Vec3f v, Vec4f c)
      {
         _inlineVerts.Add(new v_v3c4() { _v = v, _c = c });
      }
      public static void line(UInt16 i1, UInt16 i2)
      {
         _inlineInds.Add(i1);
         _inlineInds.Add(i2);
      }
      public static void End(MeshData d)
      {
         d.CreateBuffers(Gpu.SerializeGPUData(_inlineVerts.ToArray()), Gpu.SerializeGPUData(_inlineInds.ToArray()));

         _inlineVerts = null;
         _inlineInds= null;
      }
      public static WorldObject CreateBoxLines(Vec3f i, Vec3f a, Vec4f color)
      {
         WorldObject wo = new WorldObject();
         MeshData d = new MeshData("Debug", PrimitiveType.Lines, v_v3c4.VertexFormat, IndexFormatType.Uint16);
         Begin();
         //      6     7 a
         //   2     3
         //      4      5
         // i 0     1
         v3c4(new Vec3f(i.X, i.Y, i.Z), color);
         v3c4(new Vec3f(a.X, i.Y, i.Z), color);
         v3c4(new Vec3f(i.X, a.Y, i.Z), color);
         v3c4(new Vec3f(a.X, a.Y, i.Z), color);
         v3c4(new Vec3f(i.X, i.Y, a.Z), color);
         v3c4(new Vec3f(a.X, i.Y, a.Z), color);
         v3c4(new Vec3f(i.X, a.Y, a.Z), color);
         v3c4(new Vec3f(a.X, a.Y, a.Z), color);

         line(0, 1);
         line(1, 3);
         line(3, 2);
         line(2, 0);

         line(5, 4);
         line(4, 6);
         line(6, 7);
         line(7, 5);

         line(0, 4);
         line(1, 5);
         line(3, 7);
         line(2, 6);

         End(d);

         wo.Mesh = d;
         wo.Material = new Material(null, Shader.DefaultFlatColorShader());

         return wo;

      }
   }
}
