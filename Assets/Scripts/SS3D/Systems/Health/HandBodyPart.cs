using Coimbra;
using FishNet;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandBodyPart : HumanBodypart
{

	[SerializeField] private Hand _hand;
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
		_hand.DisableHand();
	}

	protected override void DetachBodyPart()
	{
		GameObject go = Instantiate(_bodyPartItem);
		Hand hand = go.GetComponent<Hand>();
		Destroy(hand);
		InstanceFinder.ServerManager.Spawn(go, null);
	}
}
