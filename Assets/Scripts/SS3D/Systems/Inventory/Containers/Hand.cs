using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Systems.Inventory.Items;
using System.Linq;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using FishNet.Object;
using static SS3D.Systems.Inventory.Containers.AttachedContainer;

namespace SS3D.Systems.Inventory.Containers
{
	public class Hand : InteractionSource, IInteractionRangeLimit, IInteractionOriginProvider
	{
		public AttachedContainer Container;
		[SerializeField] private float handRange;

		public RangeLimit range = new(1.5f, 2);

		// pickup icon that this hand uses when there's a pickup interaction
		// TODO: When AssetData is on, we should update this to not use this
		public Sprite pickupIcon;

		/// <summary>
		/// The item held in the active hand
		/// </summary>
		public Item ItemInHand => Container.Items.FirstOrDefault();

		public Transform interactionOrigin;

		public Hands handsController;

		public Vector3 InteractionOrigin => interactionOrigin.position;

		public delegate void HandHandler(Hand hand);
		public event HandHandler OnHandDisabled;

		protected override void OnDisabled()
		{
			if (!IsServer)
			{
				return;
			}

			OnHandDisabled?.Invoke(this);
		}

		public bool IsEmpty()
		{
			return Container.Container.Empty;
		}

		public IInteractionSource GetActiveTool()
		{
			Item itemInHand = ItemInHand;
			if (itemInHand == null)
			{
				return null;
			}

			IInteractionSource interactionSource = itemInHand.Prefab.GetComponent<IInteractionSource>();
			if (interactionSource != null)
			{
				interactionSource.Source = (IInteractionSource) this;
			}
			return interactionSource;
		}

		public RangeLimit GetInteractionRange()
		{
			return range;
		}

		[Server]
		public void Pickup(Item item)
		{
			if (!IsEmpty())
			{
				return;
			}

			if (item.Container != null && item.Container.AttachedTo != Container)
			{
				item.Container.RemoveItem(item);
			}

			Container.Container.AddItem(item);
		}

		[ServerRpc]
		public void CmdDropHeldItem()
		{
			if (IsEmpty())
			{
				return;
			}

			Container.Container.Dump();
		}

		[Server]
		public void PlaceHeldItemOutOfHand(Vector3 position, Quaternion rotation)
		{
			if (IsEmpty())
			{
				return;
			}

			Item item = ItemInHand;
			item.SetContainer(null);
			ItemUtility.Place(item, position, rotation, transform);
		}

		/// <summary>
		/// Checks if the creature can interact with an object
		/// </summary>
		/// <param name="otherObject">The game object to interact with</param>
		public bool CanInteract(GameObject otherObject)
		{
			return GetInteractionRange().IsInRange(InteractionOrigin, otherObject.transform.position);
		}
	}
}
