using UnityEngine;
using System.Collections;
using Mirror;

namespace SS3D.Engine.Interactions
{
    /**
     * <summary>Information that initiated an interaction</summary>
     */
    public struct InteractionEvent
    {
        public InteractionEvent(GameObject tool, GameObject target, RaycastHit at)
            : this(tool, target, at, null)
        {
        }
        public InteractionEvent(GameObject tool, GameObject target, RaycastHit at, NetworkConnection connectionToClient)
        {
            this.tool = tool;
            this.target = target;
            this.at = at;
            this.connectionToClient = connectionToClient;
        }

        public readonly GameObject tool;
        public readonly GameObject target;
        public readonly RaycastHit at;

        /// <summary>Only present when the action is active (i.e., null when BeginInteraction)</summary>
        public readonly NetworkConnection connectionToClient;

        // Helpers
        public GameObject Player => tool.transform.root.gameObject;
    }
}