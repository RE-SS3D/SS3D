using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FeetController : NetworkActor
{

	/// <summary>
	/// List of feet used by the player
	/// </summary>
	List<FootBodyPart> feet;

	/// <summary>
	/// Number of optimal feet to move at the maximum speed. For a human, more than two feets is useless, and less makes you run slower (or not at all).
	/// </summary>
	private int _optimalFeetNumber = 2;

	/// <summary>
	/// Factor influencing the speed of a player, based upon state of feets. Should be between 0 and 1 in value.
	/// </summary>
	[SyncVar] private float _feetHealthFactor;

	public float FeetHealthFactor => _feetHealthFactor;

	public override void OnStartServer()
	{
		base.OnStartServer();
		feet = GetComponentsInChildren<FootBodyPart>().ToList();
		foreach (FootBodyPart foot in feet)
		{
			foot.OnDamageInflicted += HandleFootHurt;
			foot.OnBodyPartDetached += HandleFootRemoved;
			foot.OnBodyPartDestroyed += HandleFootRemoved;
		}
		_feetHealthFactor = 1;
	}

	/// <summary>
	/// Update feet health factor when a foot takes damages.
	/// </summary>
	private void HandleFootHurt(object sender, EventArgs e)
	{
		UpdateFeetHealthFactor();
	}

	/// <summary>
	/// If a foot is destroyed or detached, remove it from the list of available feet.
	/// </summary>
	private void HandleFootRemoved(object sender, EventArgs e)
	{
		FootBodyPart foot = (FootBodyPart)sender;
		if (foot != null)
		{
			feet.Remove(foot);
		}
		UpdateFeetHealthFactor();
	}

	/// <summary>
	/// Simply update the feet health factor, based on the health of each available feet and their numbers.
	/// </summary>
	private void UpdateFeetHealthFactor()
	{
		_feetHealthFactor = feet.Sum(x => x.GetSpeedContribution()) / _optimalFeetNumber;
	}
}
