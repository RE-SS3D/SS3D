using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Furniture
{
    public class Sittable : NetworkActor, IInteractionTarget
    {
        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return Array.Empty<IInteraction>();
        }

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);
    }
}
