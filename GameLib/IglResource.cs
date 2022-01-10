using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oc
{
    public abstract class IglResource
    {
        protected int _glId;
        public int GetGlId() { return _glId; }
        public abstract void Free();
    }
}
