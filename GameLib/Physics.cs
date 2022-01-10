using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oc
{
    //TODO: duh
    public class CollisionVolume
    {
    }
    public class CollisionSphere : CollisionVolume
    {
        public float _radius = 1.0f;
        public CollisionSphere(float r)
        {
            _radius = r;
        }

    }

    class Physics
    {

    }
}
