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
using System.Windows.Forms;

namespace PirateCraft
{
    public class Input
    {
        //PINVOKE
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        Vec2f _lastPos;

        public Input(Scene nm, Renderer rm)
        {
            ////input
            //GetRenderManager().GetControl().MouseWheel += _objOpenGl_MouseWheel;
            //GetRenderManager().GetControl().MouseMove += _objOpenGl_MouseMove;
            //GetRenderManager().GetControl().MouseDown += _objOpenGL_MouseDown;
            //GetRenderManager().GetControl().KeyDown += _objOpenGl_KeyDown;
        }
        public void Update()
        {
            var keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Key.Escape))
            {
                Exit();
            }
        }
        private void _objOpenGl_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                GetScene().DeleteSelectedNode();
            else if (e.KeyCode == Keys.W)
                GetRenderManager().GetCamera().Pos.Y += 2.0f;
            else if (e.KeyCode == Keys.S)
                GetRenderManager().GetCamera().Pos.Y -= 2.0f;
            else if (e.KeyCode == Keys.A)
                GetRenderManager().GetCamera().Pos.X -= 2.0f;
            else if (e.KeyCode == Keys.D)
                GetRenderManager().GetCamera().Pos.X += 2.0f;
        }
        private void _objOpenGl_MouseWheel(object sender, MouseEventArgs e)
        {
            //zoom
            if (GetScene().SelectedNode == null)
                return;

            Vec3f scl = GetScene().SelectedNode.GetScale();

            float newscl;
            if (ControlButtonIsPressed())
                newscl = (float)e.Delta / (120 * 500);
            else
                newscl = (float)e.Delta / (120 * 50);

            scl.X += newscl;
            scl.Y += newscl;
            scl.Z += newscl;
            if (scl.X < 0.02) scl.X = 0.02f;
            if (scl.Y < 0.02) scl.Y = 0.02f;
            if (scl.Z < 0.02) scl.Z = 0.02f;
            GetScene().SelectedNode.SetScale(scl);
            //    _objRenderManager.Invalidate();
        }
        private bool ControlButtonIsPressed()
        {
            return (GetAsyncKeyState(Keys.LControlKey) & 0x8000) > 0 || (GetAsyncKeyState(Keys.RControlKey) & 0x8000) > 0;
        }
        private void _objOpenGl_MouseMove(object sender, MouseEventArgs e)
        {
            Vec2f curPos = new Vec2f(e.X, e.Y);
            if ((GetAsyncKeyState(Keys.LButton) & 0x8000) > 0)
            {
                if (GetScene().SelectedNode != null)
                {
                    Vec2f delta = curPos - _lastPos;
                    Vec3f trans = GetScene().SelectedNode.GetPos();
                    trans.X += delta.X;
                    trans.Y -= delta.Y;
                    GetScene().SelectedNode.SetPos(trans);
                }
            }
            _lastPos = curPos;
        }
        private void _objOpenGL_MouseDown(object sender, MouseEventArgs e)
        {
            Vec2f curPos = new Vec2f(e.X, e.Y);
            _lastPos = curPos;

            PickRay pr = GetRenderManager().GetCamera().ProjectPoint(curPos);

            _objNodeManager.SelectedNode = GetScene().PickClosestNode(pr);
        }
    }
}
