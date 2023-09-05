using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Heart : BodyPart
{
	
	// Number of beat per minutes
	private float _beatFrequency = 60f;

	public event EventHandler OnPulse;

	public float SecondsBetweenBeats => _beatFrequency > 0 ? 60f / _beatFrequency : float.MaxValue;

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
        TryAddBodyLayer(new MuscleLayer(this));
        TryAddBodyLayer(new CirculatoryLayer(this, 3f));
        TryAddBodyLayer(new NerveLayer(this));
        TryAddBodyLayer(new OrganLayer(this));
    }
}
