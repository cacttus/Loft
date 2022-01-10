using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
namespace Oc
{
    // Global Utils. static lcass
    public class Gu
    {
        public static long _lngFrameStamp = 0;
        public static CoordinateSystem _enmCoordinateSystem = CoordinateSystem.Rhs;
        public static void Update()
        {
            _lngFrameStamp++;
        }
        public static long GetFrameStamp()
        {
            return _lngFrameStamp;
        }
        public static void CheckGpuErrorsRt()
        {
            ErrorCode c = GL.GetError();
            if (c != ErrorCode.NoError)
                throw new Exception("OpenGL Error " + c.ToString());
        }
        public static void CheckGpuErrorsDbg()
        {
#if DEBUG
            CheckGpuErrorsRt();
#endif
        }
    }
}
