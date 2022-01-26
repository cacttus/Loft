using System;
using System.Collections.Generic;
namespace PirateCraft
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
