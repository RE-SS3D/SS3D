using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandHit : MonoBehaviour, IInteractionSourceExtension
{
	public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
	{
		var interaction = new HitInteraction();
		
		foreach(IInteractionTarget target in targets)
		{
			if(interaction.CanInteract(new InteractionEvent(gameObject.GetComponent<Hand>(), target)))
			{
				interactions.Add(new InteractionEntry(target, interaction));
			}
		}
	}
}
