using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;


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
      public static void v3c4(vec3 v, vec4 c)
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
      public static WorldObject CreateBoxLines(vec3 i, vec3 a, vec4 color)
      {
         WorldObject wo = new WorldObject();
         MeshData d = new MeshData("Debug", PrimitiveType.Lines, v_v3c4.VertexFormat, IndexFormatType.Uint16);
         Begin();
         //      6     7 a
         //   2     3
         //      4      5
         // i 0     1
         v3c4(new vec3(i.x, i.y, i.z), color);
         v3c4(new vec3(a.x, i.y, i.z), color);
         v3c4(new vec3(i.x, a.y, i.z), color);
         v3c4(new vec3(a.x, a.y, i.z), color);
         v3c4(new vec3(i.x, i.y, a.z), color);
         v3c4(new vec3(a.x, i.y, a.z), color);
         v3c4(new vec3(i.x, a.y, a.z), color);
         v3c4(new vec3(a.x, a.y, a.z), color);

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
