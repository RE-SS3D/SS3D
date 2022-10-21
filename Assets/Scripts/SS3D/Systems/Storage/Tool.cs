using UnityEngine;

namespace SS3D.Storage
{
    /// <summary>
    /// A tool is something capable of interacting with something else.
    /// </summary>
    public interface ITool
    {
        void Interact(RaycastHit hit, bool secondary = false);
    }
}