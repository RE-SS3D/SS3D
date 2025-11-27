using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    public class ThrowAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private readonly AbstractHoldable _holdable;
        private readonly Hand _mainHand;
        private readonly Hand _secondaryHand;
        private readonly Transform _rootTransform;

        public ThrowAnimation(float interactionTime, ProceduralAnimationController controller, NetworkObject holdable, Hand mainHand, Hand secondaryHand)
            : base(interactionTime, controller)
        {
            _holdable = holdable.GetComponent<AbstractHoldable>();
            _mainHand = mainHand;
            _secondaryHand = secondaryHand;
            _rootTransform = controller.transform;
        }

        public override void ClientPlay()
        {
            Controller.AnimatorController.Throw(_mainHand.HandType == HandType.RightHand);

            _holdable.GameObject.transform.parent = _mainHand.HandBone;

            // remove all IK constraint on second hand if needed
            if (_holdable.CanHoldTwoHand && _secondaryHand)
            {
                _secondaryHand.Hold.HoldIkConstraint.weight = 0f;
            }

            if (_holdable.TryGetComponent(out Collider collider))
            {
                // Ignore collision between thrown item and player for a short while
                Physics.IgnoreCollision(collider, _rootTransform.GetComponent<Collider>(), true);
                WaitToRestoreCollision();
            }
        }

        public override void Cancel()
        {
        }

        private async void WaitToRestoreCollision()
        {
            await Task.Delay(300);

            // Allow back collision between thrown item and player
            Physics.IgnoreCollision(_holdable.GetComponent<Collider>(), _rootTransform.GetComponent<Collider>(), false);
        }
    }
}
