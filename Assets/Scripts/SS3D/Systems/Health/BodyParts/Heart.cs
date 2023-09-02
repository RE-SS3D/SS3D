using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UI;

public class Heart : BodyPart
{
	
	// Number of beat per minutes
	private float _beatFrequency;

	public event EventHandler OnPulse;

	public float SecondsBetweenBeats => 60f / _beatFrequency;

	private float _timer = 0f;

    void Update()
    {

		_timer += Time.deltaTime;

		if (_timer > SecondsBetweenBeats)
		{
			_timer= 0f;
			OnPulse?.Invoke(this, EventArgs.Empty);
		}
	}

	protected override void AddInitialLayers()
	{
		throw new NotImplementedException();
	}
}
