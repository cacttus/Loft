using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
namespace Oc
{
    public class EngineManager
    {
        RenderManager _objRenderManager;
        GLControl _objOpenGl;
        NodeManager _objNodeManager;
        InputManager _objInputManager ;
        System.Windows.Forms.Timer _objUpdateTimer;

        //System.Windows.Threading.DispatcherTimer _objUpdateTimer;

        public RenderManager GetRenderManager(){ return _objRenderManager; }
        public NodeManager GetNodeManager(){ return _objNodeManager; }
        public InputManager GetInputManager() { return _objInputManager; }

        public GLControl GetGLControl(){ return _objOpenGl; }
        public EngineManager(System.Windows.Forms.Form objForm)
        {
            _objOpenGl = new GLControl(new OpenTK.Graphics.GraphicsMode(32, 24, 0, 8));
            objForm.Controls.Add(_objOpenGl);

            _objNodeManager = new NodeManager();
            _objRenderManager = new RenderManager(_objOpenGl, _objNodeManager);
            _objInputManager = new InputManager(_objNodeManager, _objRenderManager);
            _objUpdateTimer = new System.Windows.Forms.Timer();
            _objUpdateTimer.Interval = 1;// new TimeSpan(0, 0, 0, 0, 1);//ASAP
            _objUpdateTimer.Tick += new EventHandler(EngineTick);
            _objUpdateTimer.Start();
        }
        public void Initialize(bool blnOrtho)
        {
            _objRenderManager.Initialize(blnOrtho);
        }
        private void EngineTick(object sender, EventArgs e)
        {
            _objNodeManager.Update();
            _objRenderManager.Redraw();
        }

    }
}
