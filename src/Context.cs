using OpenTK;
namespace PirateCraft
{
    public class Context
    {
        public GpuInfo GpuInfo { get; private set; } = null;
        public GameWindow GameWindow { get; set; }

        public Context(GameWindow g)
        {
            GameWindow = g;
            GpuInfo = new GpuInfo();
        }
    }
}
