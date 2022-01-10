using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;

namespace Oc
{
    public class NodeManager
    {
        List<BaseNode> _objObjectList = new List<BaseNode>();

        public BaseNode SelectedNode;
        public List<BaseNode> GetNodes() { return _objObjectList; }
        public void Resize(Viewport vp)
        {
            foreach (BaseNode bn in _objObjectList)
            {
                bn.Resize(vp);
            }
        }
        public void DeleteNode(BaseNode node)
        {
            node.Free();
            _objObjectList.Remove(node);
        }
        public void DeleteSelectedNode()
        {
            if (SelectedNode == null)
                return;
            DeleteNode(SelectedNode);
            SelectedNode = null;
        }
        //When a node is constructed.
        public void Constructed(BaseNode bn) 
        {
            _objObjectList.Add(bn);
        }
        public void Destructed(BaseNode bn) 
        {
            _objObjectList.Remove(bn);
        }
        //Main update routine.
        public void Update()
        {
            //Bound boxes updated in render manager
        }
        public BaseNode PickClosestNode(PickRay xy)
        {
            List<BaseNode> ret = PickAllNodes(xy);

            if (ret.Count == 0)
                return null;

            ret.Sort((x, y) => x.GetId().CompareTo(y.GetId()));

            return ret[0];
        }
        public List<BaseNode> PickAllNodes(PickRay xy)
        {
            List<BaseNode> nodes = new List<BaseNode>();
            foreach (BaseNode bn in _objObjectList)
            {
                if (bn.SelectEnabled && bn.HitTestRay(xy))
                    nodes.Add(bn);
            }
            return nodes;
        }

    }
}
