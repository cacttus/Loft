using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PirateCraft
{
    public abstract class OpenGLResource
    {
        protected int _glId;
        public int GetGlId() { return _glId; }
        public abstract void Free();
    }
}
