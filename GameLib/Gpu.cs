using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
namespace Oc
{
    public class Gpu
    {
        public static int GetMaxTextureSize()
        {
            int[] maxTextureSize = new int[2];
            GL.GetInteger(GetPName.MaxTextureSize, maxTextureSize);
            return maxTextureSize[0];
        }
    }
}
