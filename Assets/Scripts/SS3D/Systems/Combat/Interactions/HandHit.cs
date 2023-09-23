using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Combat.Interactions
{
	/// <summary>
	/// Little script to add next to Hand script, allowing to add a HitInteraction from hand sources.
	/// </summary>
	public class HandHit : MonoBehaviour, IInteractionSourceExtension
	{
		public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
		{
			HitInteraction interaction = new HitInteraction();

			foreach (IInteractionTarget target in targets)
			{
                // Todo : unnecessary check, the interaction controller takes care of that.
				if (interaction.CanInteract(new InteractionEvent(gameObject.GetComponent<Hand>(), target)))
				{
					interactions.Add(new InteractionEntry(target, interaction));
				}
			}
		}
	}
}
