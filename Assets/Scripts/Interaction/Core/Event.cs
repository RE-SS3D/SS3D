using System;
using UnityEngine;

namespace Interaction.Core
{
    [Serializable]
    public struct Event
    {
        public string kind;
        public GameObject sender;
        public Vector3 worldPosition;
        public Vector3 worldNormal;
        public InteractionReceiver forwardTo;
        public InteractionReceiver waitFor;

        public Event(string kind, GameObject sender)
        {
            this.kind = kind;
            this.sender = sender;
            this.worldPosition = Vector3.zero;
            this.worldNormal = Vector3.zero;
            this.forwardTo = null;
            this.waitFor = null;
        }

        public Event Forward(string place, GameObject transform)
        {
            return new Event
            {
                kind = place,
                sender = transform,
                worldNormal = worldNormal,
                worldPosition = worldPosition,
                forwardTo = null,
            };
        }

        public Event WorldPosition(Vector3 value)
        {
            worldPosition = value;
            return this;
        }
        public Event WorldNormal(Vector3 value)
        {
            worldNormal = value;
            return this;
        }
        public Event ForwardTo(InteractionReceiver value)
        {
            forwardTo = value;
            return this;
        }

        public Event WaitFor(InteractionReceiver interactionReceiver)
        {
            waitFor = interactionReceiver;
            return this;
        }

        public override string ToString()
        {
            return $"{kind}: {nameof(sender)}: {sender}";
        }
    }
}