using FishNet.Object.Synchronizing;
using SS3D.Interactions.Interfaces;
using System;
using System.Collections.Generic;

namespace SS3D.Interactions
{
    /// <summary>
    /// Small script to make a game object toggleable. All it does is send an OnToggle event when its state changes.
    /// </summary>
    public class GenericToggleInteractionTarget : InteractionTargetNetworkBehaviour, IToggleable
    {
        [SyncVar]
        private bool _on;
        public Action<bool> OnToggle;

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new(1)
            {
                new ToggleInteraction()
            };

            return interactions.ToArray();
        }

        public bool GetState()
        {
            return _on;
        }

        public void Toggle()
        {
            _on = !_on;

            OnToggle?.Invoke(_on);
        }
    }
}
