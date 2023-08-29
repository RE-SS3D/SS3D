using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface to implement for stuff that provide some ability to walk, like a foot or a wooden leg.
/// </summary>
public interface IWalkEnabler
{
	/// <summary>
	/// Should send a positive float, 0 meaning it doesn't help to walk at all,
	/// 1 it contributes to the normal player speed, more it helps the player to walk
	/// faster than usual.
	/// for instance, a healthy foot should send 1, hurt, it should get close to 0,
	/// a wooden leg should send a value around 0.5 probably.
	/// </summary>
	public abstract float GetSpeedContribution();
}
