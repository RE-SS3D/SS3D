using Coimbra;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandBodyPart : HumanBodypart
{

	[SerializeField] private Hand hand;
	public override void Init(string name = "")
	{
		base.Init(name);
	}

	public override void Init(BodyPart parent, string name = "")
	{
		base.Init(parent, name);
	}

	protected override void RemoveSingleBodyPart()
	{
		base.RemoveSingleBodyPart();
		hand.DisableHand();
	}
}
