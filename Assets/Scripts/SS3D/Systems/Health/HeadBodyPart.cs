using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadBodyPart : BodyPart
{ 
	public override void Init(BodyPart parent)
	{
		base.Init(parent);
	}

	protected override void AddInitialLayers()
	{
		TryAddBodyLayer(new MuscleLayer(this));
		TryAddBodyLayer(new BoneLayer(this));
		TryAddBodyLayer(new CirculatoryLayer(this));
		TryAddBodyLayer(new NerveLayer(this));
	}
}
