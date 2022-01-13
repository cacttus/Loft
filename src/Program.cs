using System;
using System.Diagnostics;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;

namespace PirateCraft
{
    public class MainWindow : OpenTK.GameWindow
    {
        Camera3D _camera = new Camera3D(860, 540, 1,1000);
        Shader _shader = new Shader();

        public MainWindow() : base(860, // initial width
        540, // initial height
        GraphicsMode.Default,
        "dreamstatecoding",  // initial title
        GameWindowFlags.Default,
        DisplayDevice.Default,
        4, // OpenGL major version
        0, // OpenGL minor version
        GraphicsContextFlags.ForwardCompatible)
        {
            Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
        }
        // private void StartMonoDebugger(){
        //   Process proc = new Process {
        //     StartInfo = new ProcessStartInfo {
        //         FileName = "../../mono_debugger_daemon.sh",
        //         Arguments = "",
        //         UseShellExecute = true,
        //         RedirectStandardOutput = false,
        //         CreateNoWindow = false
        //       }
        //   };
        // }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }
        protected override void OnLoad(EventArgs e)
        {
            Gu.Init(this);

            CursorVisible = true;
           // TestFonts();
            _shader.Load();
        }
        private void TestFonts()
        {
            //TODO: we might need to use STB here. This is just .. ugh
            try
            {
                System.Console.WriteLine("Testing fonts.");
                string ttf_loc = "../../testdata/fonts/Lato-Regular.ttf";
                if (!System.IO.File.Exists(ttf_loc))
                {
                    System.Console.WriteLine("We aren't in the app directory for some reason");
                    ttf_loc = "./testdata/fonts/Lato-Regular.ttf";
                }
                Font f = new Font(ttf_loc); 
                System.Drawing.Bitmap b = f.RenderString("Hello World");
                b.Save("./test.bmp");
                var fff = b.RawFormat;
                var ffff = b.PixelFormat;


                //System.Console.WriteLine("whate");
                //System.Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
                //NRasterizer.OpenTypeReader r = new NRasterizer.OpenTypeReader();
                //NRasterizer.Typeface face;

                //using (var fs = File.Open(ttf_loc, FileMode.Open, FileAccess.Read, FileShare.None))
                //{
                //    face = r.Read(fs);
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            HandleKeyboard();
        }
        private void HandleKeyboard()
        {
            var keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Key.Escape))
            {
                Exit();
            }
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            Title = $"(Vsync: {VSync}) FPS: {1f / e.Time:0}";

            _camera.Setup();

            _shader.UpdateAndBind();

            Color4 backColor;
            backColor.A = 1.0f;
            backColor.R = 0.1f;
            backColor.G = 0.1f;
            backColor.B = 0.3f;
            GL.ClearColor(backColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            SwapBuffers();
        }
    }
    class MainClass
    {
        public static void Main(string[] args)
        {
            var m = new MainWindow();
            m.VSync = OpenTK.VSyncMode.Off;
            m.Run();
        }
    }
}