using System;
namespace PirateCraft
{
    public class Component
    {
        public Component()
        {
        }
        public virtual void Update() { }
    }
    public class MeshComponent : Component
    {
        MeshData MeshData { get; set; }//Data blocks can be shared.
        public override void Update() { }
    }
}
