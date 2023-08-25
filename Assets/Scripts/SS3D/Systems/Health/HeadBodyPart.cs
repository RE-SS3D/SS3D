using FishNet;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Systems.Inventory.Items;

public class HeadBodyPart : BodyPart
{
	public Brain brain;

	public override void Init(BodyPart parent)
	{
		base.Init(parent);
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		_internalBodyParts.Container.AddItem(brain.gameObject.GetComponent<Item>());
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
		var bodyPart = go.GetComponent<BodyPart>();
		CopyValuesToBodyPart(bodyPart);

		MindSystem entitySystem = Subsystems.Get<MindSystem>();
		entitySystem.SwapMinds(GetComponentInParent<Entity>(), go.GetComponent<Entity>());
		go.GetComponent<NetworkObject>().RemoveOwnership();

		RemoveChildAndParent();
		DumpContainers();
		Deactivate();
	}
}
