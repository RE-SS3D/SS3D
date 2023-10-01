﻿using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Health
{
	public class HandBodyPart : BodyPart
	{

		[SerializeField] private Hand _hand;

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
			InvokeOnBodyPartLayerAdded();
		}
	}
}
