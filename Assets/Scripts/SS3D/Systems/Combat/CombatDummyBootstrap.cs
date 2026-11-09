using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Screens;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Freezes player-control behaviours on a mindless Human used as a combat/interaction test dummy.
    /// Added at spawn time — do not put on <c>Human.prefab</c>.
    /// </summary>
    public sealed class CombatDummyBootstrap : MonoBehaviour
    {
        private Ragdoll _ragdoll;
        private HumanoidLivingController[] _livingControllers;

        public void ConfigureAsDummy()
        {
            _ragdoll = GetComponentInChildren<Ragdoll>(true);
            _livingControllers = GetComponentsInChildren<HumanoidLivingController>(true);
            DisableBehavioursInChildren<HumanoidPredictedMovement>();
            DisableBehavioursInChildren<HumanoidLivingController>();
            DisableBehavioursInChildren<InteractionController>();
            DisableBehavioursInChildren<HumanoidCombatController>();
            DisableBehavioursInChildren<CameraFollow>();

            // Keep upright: stop physics from knocking the empty body over.
            foreach (Rigidbody body in GetComponentsInChildren<Rigidbody>(true))
            {
                if (body == null)
                {
                    continue;
                }

                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            gameObject.name = "CombatDummy";
        }

        /// <summary>
        /// Equips the Phase 5 armor test piece so absorption can be verified without manual give/equip.
        /// </summary>
        public void EquipArmorTestLoadout()
        {
            if (!TryGetComponent(out HumanInventory inventory))
            {
                inventory = GetComponentInChildren<HumanInventory>();
            }

            if (inventory == null)
            {
                return;
            }

            if (!inventory.TryGetTypeContainer(ContainerType.Jumpsuit, 0, out AttachedContainer jumpsuitSlot))
            {
                return;
            }

            if (jumpsuitSlot.ItemCount > 0)
            {
                return;
            }

            ItemSubSystem items = SubSystems.Get<ItemSubSystem>();
            items.SpawnItemInContainer(Items.JumpsuitSecurity, jumpsuitSlot);
        }

        private void LateUpdate()
        {
            // Standing up from a knockdown (taser, shove, blast) re-enables the living controller.
            // Freeze it again so the dummy stays a dummy once it is upright.
            if (_ragdoll == null || _ragdoll.IsKnockedDown || _ragdoll.IsGettingUp)
            {
                return;
            }

            foreach (HumanoidLivingController controller in _livingControllers)
            {
                if (controller != null && controller.enabled)
                {
                    controller.enabled = false;
                }
            }
        }

        private void DisableBehavioursInChildren<T>() where T : Behaviour
        {
            T[] behaviours = GetComponentsInChildren<T>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                {
                    behaviours[i].enabled = false;
                }
            }
        }
    }
}
