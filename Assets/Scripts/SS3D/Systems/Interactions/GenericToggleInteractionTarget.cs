using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Small script to make a game object toggleable. All it does is send an OnToggle event when its state changes.
    /// </summary>
    public class GenericToggleInteractionTarget : NetworkActor, IToggleable, IInteractionTarget
    {
        [SyncVar]
        private bool _on;

        public event Action<bool> OnToggle;
 
        private IInteractionPointProvider _interactionPointProvider;

        protected override void OnAwake()
        {
            _interactionPointProvider = GetComponent<IInteractionPointProvider>();
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new(1)
            {
                new ToggleInteraction()
            };

            return interactions.ToArray();
        }

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => _interactionPointProvider != null ? _interactionPointProvider.TryGetInteractionPoint(source, out point) : this.GetInteractionPoint(source, out point);

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
