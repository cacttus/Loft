using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
namespace PirateCraft
{
    public class GpuInfo
    {
        private int _maxTextureSize = 1;
        public GpuInfo()
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
    }
}
