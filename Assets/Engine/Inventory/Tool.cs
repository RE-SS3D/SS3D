using UnityEngine;

namespace SS3D.Engine.Inventory
{

    /**
     * A tool is something capable of interacting with something else.
     */
    public interface Tool
    {
        void Interact(RaycastHit hit, bool secondary = false);
    }
}