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
		AddInternalBodyPart(brain);
    }

		protected override void AddInitialLayers()
		{
			TryAddBodyLayer(new MuscleLayer(this));
			TryAddBodyLayer(new BoneLayer(this));
			TryAddBodyLayer(new CirculatoryLayer(this, 5f));
			TryAddBodyLayer(new NerveLayer(this));
			InvokeOnBodyPartLayerAdded();
		}

        /// <summary>
        /// Deactivate this game object, should run for all observers, and for late joining (hence bufferlast = true).
        /// </summary>
        [ObserversRpc(RunLocally = true, BufferLast = true)]
		protected void DeactivateWholeBody()
		{
			GetComponentInParent<Human>().gameObject.SetActive(false);
		}

        protected override void AfterSpawningCopiedBodyPart()
        {

            GetComponentInParent<Human>()?.DeactivateComponents();

            // When detached, spawn a head and set player's mind to be in the head,
            // so that player can still play as a head (death is near though..).
            MindSystem mindSystem = Subsystems.Get<MindSystem>();

            var EntityControllingHead = GetComponentInParent<Entity>();
            if (EntityControllingHead.Mind != null)
            {
                mindSystem.SwapMinds(GetComponentInParent<Entity>(), _spawnedCopy.GetComponent<Entity>());
                _spawnedCopy.GetComponent<NetworkObject>().RemoveOwnership();

                EntitySystem entitySystem = Subsystems.Get<EntitySystem>();
                entitySystem.TryTransferEntity(GetComponentInParent<Entity>(), _spawnedCopy.GetComponent<Entity>());
            }
        }

        protected override void BeforeDestroyingBodyPart()
        {
            GetComponentInParent<Human>()?.DeactivateComponents();
            return;
        }
    }
}
