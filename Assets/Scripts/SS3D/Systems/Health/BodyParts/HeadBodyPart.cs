using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Health
{
	/// <summary>
	/// Body part for a human head.
	/// </summary>
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
			_internalBodyParts.AddItem(brain.gameObject.GetComponent<Item>());
		}

		protected override void AddInitialLayers()
		{
			TryAddBodyLayer(new MuscleLayer(this));
			TryAddBodyLayer(new BoneLayer(this));
			TryAddBodyLayer(new CirculatoryLayer(this));
			TryAddBodyLayer(new NerveLayer(this));
			InvokeOnBodyPartLayerAdded();
		}

		protected override void DetachBodyPart()
		{
			if (_isDetached) return;
			DetachChildBodyParts();
			HideSeveredBodyPart();

			// When detached, spawn a head and set player's mind to be in the head,
			// so that player can still play as a head (death is near though..).
			BodyPart head = SpawnDetachedBodyPart();
			MindSystem mindSystem = Subsystems.Get<MindSystem>();
			mindSystem.SwapMinds(GetComponentInParent<Entity>(), head.GetComponent<Entity>());
			head.GetComponent<NetworkObject>().RemoveOwnership();

			var entitySystem = Subsystems.Get<EntitySystem>();
			entitySystem.TransferEntity(GetComponentInParent<Entity>(),head.GetComponent<Entity>());

			InvokeOnBodyPartDetached();
			_isDetached = true;
			// For now simply set unactive the whole body. In the future, should instead put the body in ragdoll mode
			// and disable a bunch of components.
			DeactivateWholeBody();
			Dispose(false);
		}

		/// <summary>
		/// Deactivate this game object, should run for all observers, and for late joining (hence bufferlast = true).
		/// </summary>
		[ObserversRpc(RunLocally = true, BufferLast = true)]
		protected void DeactivateWholeBody()
		{
			GetComponentInParent<Entity>().gameObject.SetActive(false);
		}
	}
}
