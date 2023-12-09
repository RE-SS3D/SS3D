using SS3D.Core;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace System.Electricity
{
    public class GenericToggleInteractionTarget : InteractionTargetNetworkBehaviour, IToggleable
    {

        private bool _on;

        public Action<bool> OnToggle;

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = new List<IInteraction>(1)
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
