using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
namespace PirateCraft
{
    public class GpuRenderState
    {
        //State switches to prevent unnecessary gpu context changes.
        private bool _depthTestEnabledLast = false; 
        private bool _depthTestEnabled = true;
        private bool _cullFaceEnabledLast = false;
        private bool _cullFaceEnabled = true;
        private bool _scissorTestEnabledLast = false;
        private bool _scissorTestEnabled = true;
        public bool CullFace { get { return _cullFaceEnabled; } set { _cullFaceEnabledLast = _cullFaceEnabled; _cullFaceEnabled = value; } }
        public bool DepthTest { get { return _depthTestEnabled; } set { _depthTestEnabledLast = _depthTestEnabled; _depthTestEnabled = value; } }
        public bool ScissorTest { get { return _scissorTestEnabled; } set { _scissorTestEnabledLast = _scissorTestEnabled; _scissorTestEnabled = value; } }
        public void SetState()
        {
            if(_depthTestEnabled != _depthTestEnabledLast)
            {
                if (_depthTestEnabled)
                {
                    GL.Enable(EnableCap.DepthTest);
                }
                else
                {
                    GL.Disable(EnableCap.DepthTest);
                }
            }
            if (_scissorTestEnabled != _scissorTestEnabledLast)
            {
                if (_scissorTestEnabled)
                {
                    GL.Enable(EnableCap.ScissorTest);
                }
                else
                {
                    GL.Disable(EnableCap.ScissorTest);
                }
            }
            if (_cullFaceEnabled != _cullFaceEnabledLast)
            {
                if (_cullFaceEnabled)
                {
                    GL.Enable(EnableCap.CullFace);
                }
                else
                {
                    GL.Disable(EnableCap.CullFace);
                }
            }
        }
    }
    //This instance must be per-context.
    public class Gpu
    {
        private int _maxTextureSize = 1;
        public Gpu()
        {
            //Initializes gpu info
            int[] maxTextureSize = new int[2];
            GL.GetInteger(GetPName.MaxTextureSize, maxTextureSize);
            _maxTextureSize = maxTextureSize[0];
        }
        public int GetMaxTextureSize()
        {
            return _maxTextureSize;
        }

        public GpuRenderState GpuRenderState { get; set; } = new GpuRenderState();
    }
}
