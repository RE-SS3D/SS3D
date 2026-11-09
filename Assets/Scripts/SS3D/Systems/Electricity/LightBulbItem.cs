using System.Collections.Generic;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// A loose light bulb or tube. A whole one offers Insert / Replace on a fixture of its kind; a broken one is
    /// only rubbish.
    /// </summary>
    public sealed class LightBulbItem : MonoBehaviour, IInteractionSourceExtension
    {
        private static readonly InsertLightBulbInteraction Interaction = new();

        [SerializeField]
        private LightBulbKind _kind;
        [SerializeField]
        private bool _broken;

        public LightBulbKind Kind => _kind;

        public bool IsBroken => _broken;

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context)
        {
            if (_broken)
            {
                return;
            }

            foreach (IInteractionTarget target in targets)
            {
                if (target is LightFixtureBulb && Interaction.CanInteract(context.WithTarget(target)))
                {
                    interactions.Add(new InteractionEntry(target, Interaction));
                }
            }
        }
    }
}
