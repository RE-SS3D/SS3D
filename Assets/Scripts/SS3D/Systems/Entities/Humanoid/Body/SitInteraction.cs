using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Interaction to buckle/unbuckle from a seat anchor.
    /// </summary>
    public class SitInteraction : IInteraction
    {
        public string GetGenericName() => "Sit";

        public string GetName(InteractionEvent interactionEvent) => "Sit";

        public Sprite GetIcon(InteractionEvent interactionEvent) => null;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            SeatAnchor anchor = interactionEvent.Target?.GetComponent<SeatAnchor>();
            HumanoidBodyStateMachine body = interactionEvent.Source?.GetComponentInParent<HumanoidBodyStateMachine>();
            return anchor != null && body != null && body.Snapshot.State == BodyState.Locomotion;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            HumanoidBodyStateMachine body = interactionEvent.Source.GetComponentInParent<HumanoidBodyStateMachine>();
            SeatAnchor anchor = interactionEvent.Target.GetComponent<SeatAnchor>();
            if (body == null || anchor == null)
            {
                return false;
            }

            body.CmdTrySit(anchor.NetworkObject);
            return false;
        }
    }
}
