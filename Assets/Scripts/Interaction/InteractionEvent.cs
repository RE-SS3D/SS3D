using System;
using UnityEngine;

namespace Interaction
{
    [Serializable]
    public struct InteractionEvent
    {
        public InteractionKind kind;
        public Transform sender;
        public Transform parent;
        public Vector3 worldPosition;
        public Vector3 worldNormal;

        public InteractionEvent(InteractionKind kind, Transform sender, Transform parent = null)
        {
            this.kind = kind;
            this.sender = sender;
            this.parent = parent;
            this.worldPosition = Vector3.zero;
            this.worldNormal = Vector3.zero;
        }

        public override string ToString()
        {
            return $"{kind}: {nameof(sender)}: {sender} ({parent})";
        }
    }
}