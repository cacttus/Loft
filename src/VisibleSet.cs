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
        public Dictionary<double, BaseNode> Visible { get; private set; } = new Dictionary<double, BaseNode>();
        public VisibleSet()
        {
        }
    }
}
