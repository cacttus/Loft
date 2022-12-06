using System;
using System.Collections.Generic;
namespace Loft
{
    /**
     * The visible set
     * TODO:
     * */    
    public class VisibleSet
    {
        public Dictionary<double, WorldObject> Visible { get; private set; } = new Dictionary<double, WorldObject>();
        public VisibleSet()
        {
        }
    }
}
