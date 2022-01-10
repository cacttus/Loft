using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Mat4f = OpenTK.Matrix4;

namespace Oc
{
    public class MathUtils
    {
        public static Mat4f m4f16(float[] mv)
        {
            if (mv.Length != 16)
                throw new Exception("matrix was not 16 elements wide.");

              return new Mat4f(
                    mv[0],
                    mv[1],
                    mv[2],
                    mv[3],
                    mv[4],
                    mv[5],
                    mv[6],
                    mv[7],
                    mv[8],
                    mv[9],
                    mv[10],
                    mv[11],
                    mv[12],
                    mv[13],
                    mv[14],
                    mv[15]
                    );
        }
    }
}
