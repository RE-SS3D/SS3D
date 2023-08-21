using FishNet;
using SS3D.Core;
using SS3D.Systems.Entities;
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
		TryAddBodyLayer(new OrganLayer(this));
	}

	protected override void DetachBodyPart()
	{
		GameObject go = Instantiate(_bodyPartItem, Position, Rotation);
		InstanceFinder.ServerManager.Spawn(go, null);
		MindSystem entitySystem = Subsystems.Get<MindSystem>();
		entitySystem.SwapMinds(GetComponentInParent<Entity>(), go.GetComponent<Entity>());

		RemoveChildAndParent();
		DumpContainers();
		Deactivate();
	}
}
