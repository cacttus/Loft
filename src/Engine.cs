//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using OpenTK;
//using OpenTK.Graphics;
//using OpenTK.Graphics.OpenGL;
//using System.Drawing.Imaging;
//using System.Runtime.InteropServices;
//using Vec2f = OpenTK.Vector2;
//using Vec3f = OpenTK.Vector3;
//namespace PirateCraft
//{
//    public class Engine
//    {
//        Renderer _objRenderManager;
//        //GLControl _objOpenGl;
//        Scene _objScene;
//        InputManager _objInputManager ;
//        //System.Windows.Forms.Timer _objUpdateTimer;

//        public Renderer RenderManager { get; set; }
//        public Renderer GetRenderManager(){ return _objRenderManager; }
//        public Scene GetScene(){ return _objScene; }
//        public InputManager GetInputManager() { return _objInputManager; }

//       ///public GLControl GetGLControl(){ return _objOpenGl; }
//        public Engine(OpenTK.GameWindow objForm)
//        {
//            //_objOpenGl = new GLControl(new OpenTK.Graphics.GraphicsMode(32, 24, 0, 8));
//            //objForm.Controls.Add(_objOpenGl);

//            _objScene = new Scene();
//            _objRenderManager = new Renderer( _objScene);
//            _objInputManager = new InputManager(_objScene, _objRenderManager);
//            //_objUpdateTimer = new System.Windows.Forms.Timer();
//            //_objUpdateTimer.Interval = 1;// new TimeSpan(0, 0, 0, 0, 1);//ASAP
//            //_objUpdateTimer.Tick += new EventHandler(EngineTick);
//            //_objUpdateTimer.Start();
//        }
//        public void Initialize(bool blnOrtho)
//        {

//        }
//        private void EngineTick(object sender, EventArgs e)
//        {

//        }

//    }
//}
