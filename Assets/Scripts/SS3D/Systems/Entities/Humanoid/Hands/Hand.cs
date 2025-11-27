using FishNet.Object;
using SS3D.Intents;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Furniture;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// A hand is what an entity uses to grab and hold items, to interact with things in range.
    /// </summary>
    public class Hand : InteractionSource, IInteractionRangeLimit, IInteractionOriginProvider, IInteractionSourceAnimate, IAimingTargetProvider, IIntentProvider, IRootTransformProvider, IContainerProvider, IItemHolder
    {
        public delegate void HandEventHandler(Hand hand);

        public delegate void HandContentsHandler(Hand hand, Item oldItem, Item newItem, ContainerChangeType type);

        public event HandEventHandler OnHandDisabled;

        public event HandContentsHandler OnContentsChanged;

        /// <summary>
        /// the hands script controlling this hand.
        /// </summary>
        [FormerlySerializedAs("HandsController")]
        [SerializeField]
        private Hands _handsController;

        /// <summary>
        /// Container linked to this hand, necessary to hold stuff.
        /// </summary>
        [FormerlySerializedAs("Container")]
        [SerializeField]
        private AttachedContainer _container;

        /// <summary>
        /// Point from where distances for interaction is computed.
        /// </summary>
        [SerializeField]
        private Transform _interactionOrigin;

        [SerializeField]
        private Transform _interactionPoint;

        /// <summary>
        /// Horizontal and vertical max distance to interact with stuff.
        /// </summary>
        [SerializeField]
        private RangeLimit _range = new(0.7f, 2);

        public bool Empty => _container.Empty;

        public bool Full => !_container.Empty;

        /// <summary>
        /// The item held in this hand, if it exists
        /// </summary>
        public Item ItemHeld => _container.Items.FirstOrDefault();

        public Vector3 InteractionOrigin => _interactionOrigin.position;

        [field: SerializeField]
        public HandType HandType { get; private set; }

        [field:SerializeField]
        public Transform HandBone { get; private set; }

        [field:SerializeField]
        public HandHoldIk Hold { get; private set; }

        public Transform AimTarget => GetComponentInParent<HumanoidMovementController>().AimTarget;

        public bool IsAimingToThrow => GetComponentInParent<AimController>().IsAimingToThrow;

        public AttachedContainer Container => _container;

        public Transform InteractionPoint => _interactionPoint;

        public IntentType Intent => GetComponentInParent<IntentController>().Intent;

        public Transform RootTransform => GetComponentInParent<Hands>().gameObject.transform;

        public override void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> entries)
        {
            // todo : hands should not handle sit interactions, ass should, but the interaction controller needs some changes to handle interaction sources other than hands
            base.CreateSourceInteractions(targets, entries);

            #pragma warning disable RCS1077
            IInteractionTarget target = targets.FirstOrDefault(x => x.GameObject.TryGetComponent(out Sittable sittable));

            if (target is not null)
            {
                entries.Add(new InteractionEntry(target, new SitInteraction(1f)));
            }
        }

        public bool IsEmpty()
        {
            return _container.Empty;
        }

        /// <summary>
        /// Get the interaction source from stuff in hand if there's any.
        /// Also sets the source of the IInteraction source to be this hand.
        /// </summary>
        /// <returns></returns>
        public IInteractionSource GetActiveTool()
        {
            Item itemInHand = ItemHeld;
            if (itemInHand == null)
            {
                return null;
            }

            if (itemInHand.TryGetComponent(out IInteractionSource interactionSource))
            {
                interactionSource.Source = this;
            }

            return interactionSource;
        }

        public RangeLimit GetInteractionRange()
        {
            return _range;
        }

        [ServerRpc]
        public void CmdDropHeldItem()
        {
            DropHeldItem();
        }

        [Server]
        public void DropHeldItem()
        {
            _container.Dump();
        }

        /// <summary>
        /// Checks if the creature can interact with an object
        /// </summary>
        /// <param name="otherObject">The game object to interact with</param>
        public bool CanInteract(GameObject otherObject)
        {
            return GetInteractionRange().IsInRange(InteractionOrigin, otherObject.transform.position);
        }

        public void PlaySourceAnimation(InteractionType interactionType, NetworkObject target, Vector3 point, float time)
        {
            GetComponentInParent<ProceduralAnimationController>().PlayAnimation(interactionType, this,  target, point, time);
        }

        public void CancelSourceAnimation(InteractionType interactionType, NetworkObject target, float time)
        {
            GetComponentInParent<ProceduralAnimationController>().CancelAnimation(this);
        }

        protected void Awake()
        {
            // should only be called on server, however it's too late if listening in OnStartServer (again, issue with initialization timing of our systems...)
            _container.OnServerContentsChanged += ContainerOnOnContentsChanged;
        }

        protected override void OnDisabled()
        {
            if (!IsServer)
            {
                return;
            }

            OnHandDisabled?.Invoke(this);
        }

        private void ContainerOnOnContentsChanged(AttachedContainer container, Item olditem, Item newitem, ContainerChangeType type)
        {
            if (type == ContainerChangeType.Remove && olditem != null)
            {
                HumanoidAnimatorController animatorController = GetComponentInParent<HumanoidAnimatorController>();

                if (animatorController)
                {
                    GetComponentInParent<HumanoidAnimatorController>().RemoveHandHolding(this, olditem.Holdable);
                }

                StopHolding(olditem);
            }

            if (type == ContainerChangeType.Add && newitem.Holdable != null)
            {
                HumanoidAnimatorController animatorController = GetComponentInParent<HumanoidAnimatorController>();
                if (animatorController)
                {
                    animatorController.AddHandHolding(this, newitem.Holdable);
                }
            }

            OnContentsChanged?.Invoke(this, olditem, newitem, type);
        }

        private void StopHolding(Item item)
        {
            item.transform.parent = null;

            if (Hold)
            {
                Hold.StopHolding(item);
            }
        }
    }
}
