using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;

namespace Oc
{
    public class Viewport
    {
        public int _intWidth, _intHeight;
        public int _intX, intY;
        public int X { get { return _intX; } private set { } }
        public int Y { get { return _intX; } private set { } }
        public int Width { get { return _intWidth; } private set { } }
        public int Height { get { return _intHeight; } private set { } }

        public Viewport(int w, int h)
        {
            _intWidth = w;
            _intHeight = h;
        }

        public void SetupProjection()
        {
            float _fov = 70.0f;
            float _f_near = 1.0f;
            float _f_far = 1000.0f;
            float _f_tan_fov_2 = (float)Math.Tan((double)_fov)/2.0f;

            float vpWidth_2 = _f_tan_fov_2 * _f_near;

            //Vector3 curPos = getPos();
            // - Update frustum

            //_pMainFrustum->update(_viewVec, curPos, _upVec, _f_tan_fov_2, _f_near, _f_far);

            float arat_1 = 1.0f/((float)Width / (float)Height);
            //if (_viewportAxis == VPA_LOOKZ_XRIGHT)//LHS coordinate system
            //{
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Viewport(X, Y, Width, Height); // Use all of the glControl painting area
            GL.Scissor(X, Y, Width, Height);
            GL.Frustum(
                vpWidth_2, -vpWidth_2,//+- - swaps coord system.
                -vpWidth_2 * arat_1, vpWidth_2 * arat_1,
                _f_near, _f_far
                );
        }

    }
}
