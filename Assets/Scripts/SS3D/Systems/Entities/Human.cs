using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Stamina;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using Coimbra;
using SS3D.Core;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Base class for all humans
    /// </summary>
    public class Human : Entity
    {
        // prefab for the ghost 
		public GameObject Ghost;
		private GameObject _spawnedGhost;
        private bool _killStarted;

        [SyncVar(OnChange = nameof(SyncAppearance))]
        private CharacterAppearance _appearance;

        /// <summary>
        /// Applies the player's lobby character. Server sets it once after spawn; observers apply
        /// morphs and styles locally. The server applies height too so its bone hit colliders match.
        /// </summary>
        [Server]
        public void SetAppearance(CharacterAppearance appearance)
        {
            _appearance = appearance;
        }

        /// <summary>Re-applies body morphs after a worn mesh swap (clothes share the body's shape keys).</summary>
        public void ReapplyBodyMorphs()
        {
            if (!_appearance.IsSet)
            {
                return;
            }

            HumanoidRigReferences rig = GetComponent<HumanoidRigReferences>();
            HumanoidMorphApplier.Apply(
                gameObject,
                _appearance.Female,
                _appearance.Breasts,
                _appearance.Fat,
                _appearance.Muscle,
                _appearance.Jaw,
                _appearance.Height,
                rig != null ? rig.ArmatureRoot : null);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsServer)
            {
                ApplyAppearance();
            }
        }

        private void SyncAppearance(CharacterAppearance oldAppearance, CharacterAppearance newAppearance, bool asServer)
        {
            // Host: the server pass already applied it to this shared instance.
            if (!asServer && IsServer)
            {
                return;
            }

            ApplyAppearance();
        }

        private void ApplyAppearance()
        {
            if (!_appearance.IsSet)
            {
                return;
            }

            ReapplyBodyMorphs();

            IHumanoidStyleSource styles = HumanoidStyleSource.Current;
            HumanoidStyleApplier.ApplyFace(gameObject, _appearance.SkinColor, _appearance.EyeColor,
                styles?.GetEyebrowStyle(_appearance.EyebrowStyle), _appearance.EyebrowColor);
            if (styles == null) return;

            HumanoidStyleApplier.Apply(
                gameObject,
                styles.GetHairStyle(_appearance.HairStyle),
                styles.GetBeardStyle(_appearance.BeardStyle),
                _appearance.HairColor,
                _appearance.BeardColor);
        }

        /// <summary>
		/// On death, the player should become a ghost.
		/// </summary>
		[Server]
		private void BecomeGhost(GameObject player, GameObject ghost)
		{
			Entity originEntity = player.GetComponent<Entity>();
			Entity ghostEntity = ghost.GetComponent<Entity>();

            // Presentation is already Dead — Kill() applies it before mind-swap / component
            // dispose. Ragdoll owns replicated BodyPresentationState.
            MindSubSystem mindSystem = SubSystems.Get<MindSubSystem>();
            mindSystem.SwapMinds(originEntity, ghostEntity);

            RpcUpdateGhostPosition(originEntity, ghostEntity);
            RpcDestroyComponents(originEntity);
        }

        /// <summary>
        /// Destroys all "human" components, such as Hands and HumanoidController. Also activates ragdoll
        /// </summary>
		[ObserversRpc(RunLocally = true)]
		private void RpcDestroyComponents(Entity originEntity)
		{
            // Instead of destroying components it should deactivate them.
			GameObject originEntityGameObject = originEntity.gameObject;
			originEntityGameObject.GetComponent<Hands>()?.Dispose(true);
			originEntityGameObject.GetComponent<HumanInventory>()?.Dispose(true);
            // CombatInteractionNetwork requires InteractionController, so Unity refuses to remove it.
            if (originEntityGameObject.TryGetComponent(out InteractionController interactions))
                interactions.enabled = false;
			originEntityGameObject.GetComponent<StaminaController>()?.Dispose(true);
			originEntityGameObject.GetComponent<HumanoidController>()?.Dispose(true);
            originEntityGameObject.GetComponent<HumanoidLivingController>()?.Dispose(true);
            // TODO: Optimize these GetComponents, this is a temporary solution.
        }

        /// <summary>
		/// Put Ghost at the same place as the deceased player.
		/// </summary>
		[ObserversRpc(RunLocally = true)]
		private void RpcUpdateGhostPosition(Entity originEntity, Entity ghostEntity)
		{
			ghostEntity.Transform.SetPositionAndRotation(originEntity.Transform.position, originEntity.Transform.rotation);
        }

		/// <summary>
		/// Kill a player, instantiating a ghost.
		/// </summary>
		[Server]
		public override void Kill()
		{
            if (_killStarted)
            {
                return;
            }

            _killStarted = true;

            // Death presentation must not depend on the ghost hand-off. Mindless bodies (combat
            // dummies) are never in EntitySubSystem's spawned-player list, so TryTransferEntity
            // fails for them and the corpse would otherwise stay standing with kinematic bones.
            if (TryGetComponent(out Ragdoll ragdoll))
            {
                ragdoll.ServerDeathRagdoll();
            }

            if (Mind == null || Mind == Mind.Empty)
            {
                // No player to transfer into a ghost — the body stays behind as a corpse.
                return;
            }

            _spawnedGhost = Instantiate(Ghost);
			EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
			if(entitySystem.TryTransferEntity(GetComponentInParent<Entity>(), _spawnedGhost.GetComponent<Entity>()))
            {
                ServerManager.Spawn(_spawnedGhost);
                BecomeGhost(gameObject, _spawnedGhost);
                entitySystem.RecordPlayerDeath(GetComponentInParent<Entity>(), _spawnedGhost.GetComponent<Entity>());
            }
            else
            {
                _spawnedGhost.Dispose(true);
                _spawnedGhost = null;
                _killStarted = false;
            }
		}

        public override void DeactivateComponents()
        {
            RpcDestroyComponents(this);
        }
    }
}
