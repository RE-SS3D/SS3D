using System;
using UnityEngine;

namespace Interaction.Core
{
    [Serializable]
    public struct InteractionEvent
    {
        public string kind;
        public Transform sender;
        public Vector3 worldPosition;
        public Vector3 worldNormal;
        public Interactable forwardTo;
        public Interactable waitFor;

        public InteractionEvent(string kind, Transform sender, Transform parent = null)
        {
            this.kind = kind;
            this.sender = sender;
            this.worldPosition = Vector3.zero;
            this.worldNormal = Vector3.zero;
            this.forwardTo = null;
            this.waitFor = null;
        }

        public InteractionEvent Forward(string place, Transform transform)
        {
            return new InteractionEvent
            {
                kind = place,
                sender = transform,
                worldNormal = worldNormal,
                worldPosition = worldPosition,
                forwardTo = null,
            };
        }

        public InteractionEvent WorldPosition(Vector3 value)
        {
            worldPosition = value;
            return this;
        }
        public InteractionEvent WorldNormal(Vector3 value)
        {
            worldNormal = value;
            return this;
        }
        public InteractionEvent ForwardTo(Interactable value)
        {
            forwardTo = value;
            return this;
        }

        public InteractionEvent WaitFor(Interactable interactable)
        {
            waitFor = interactable;
            return this;
        }

        public override string ToString()
        {
            return $"{kind}: {nameof(sender)}: {sender}";
        }
    }
}