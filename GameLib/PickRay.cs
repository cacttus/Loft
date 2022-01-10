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
    public class BoxAAHit {
	    public bool _bHit;	// Whether the ray intersected the box.
	    public bool _p1Contained;
	    public bool _p2Contained;
	    public float _t; // - Time to hit [0,1]
    };
    public class PickRay
    {
        public Vec3f Origin;
        public Vec3f Dir;
        public float _length;
        public bool _isOpt;
        public Vec3f InvDir;
	    public int[] Sign = new int[3];
	
        public void Opt()
	    {
		    //**New - optimization
		    //http://people.csail.mit.edu/amy/papers/box-jgt.pdf
           // if (Dir.X != 0.0f)
                InvDir.X = 1.0f / Dir.X;
         //   else
         //       InvDir.X = 0.0f;
         //   if (Dir.Y != 0.0f)
                InvDir.Y = 1.0f / Dir.Y;
      //      else
       //         InvDir.Y = 0.0f;
       //     if (Dir.Z != 0.0f)
                InvDir.Z = 1.0f / Dir.Z;
       //     else
       //         InvDir.Z = 0.0f;

            Sign[0] = Convert.ToInt32(InvDir.X < 0);
            Sign[1] = Convert.ToInt32(InvDir.Y < 0);
            Sign[2] = Convert.ToInt32(InvDir.Z < 0);

		    _isOpt=true;
	    }
    }
}
