using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.UI;
using SS3D.Systems.Roles;
using UnityEngine;
using System.Collections;

public class InventoryAlt : NetworkActor
{

    public List<AttachedContainer> OnPlayerContainers = new();

    private readonly List<AttachedContainer> _openedContainers = new();

    public delegate void InventoryContainerUpdated(AttachedContainer container);

    public event InventoryContainerUpdated OnInventoryContainerAdded;

    public event InventoryContainerUpdated OnInventoryContainerRemoved;

    public delegate void ContainerEventHandler(AttachedContainer container);

    public event ContainerEventHandler OnContainerOpened;

    public event ContainerEventHandler OnContainerClosed;

    public InventoryViewAlt InventoryView { get; private set; }

    private float _nextAccessCheck;

    /// <summary>
    /// The controllable body of the owning player
    /// </summary>
    public Entity Body;

    /// <summary>
    /// The hands used by this inventory
    /// </summary>
    public HandsAlt Hands;

    public int CountHands => OnPlayerContainers.Where(x => x.Type == ContainerType.Hand).Count();

    protected override void OnAwake()
    {
        base.OnAwake();



        Hands.Inventory = this;

        AddHandle(UpdateEvent.AddListener(HandleUpdate));
    }

    private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
    {
        float time = Time.time;
        if (!(time > _nextAccessCheck))
        {
            return;
        }

        // Remove all containers from the inventory that can't be interacted with anymore.
        HandsAlt hands = GetComponent<HandsAlt>();
        for (int i = 0; i < _openedContainers.Count; i++)
        {
            AttachedContainer attachedContainer = _openedContainers[i];
            if (hands.CanInteract(attachedContainer.gameObject))
            {
                continue;
            }

            CloseContainer(attachedContainer);
            i--;
        }

        _nextAccessCheck = time + 0.5f;
    }

    protected override void OnStart()
    {
        base.OnStart();
        // Start by adding all containers on the human in the inventory
        SetupView();
        var attachedContainers =  GetComponentsInChildren<AttachedContainer>();
        foreach(var container in attachedContainers)
        {
            AddContainer(container);
        }
    }

    private void SetupView()
    {
        InventoryView = ViewLocator.Get<InventoryViewAlt>().First();
        InventoryView.Setup(this);
    }

    public void AddContainer(AttachedContainer container)
    {
        OnPlayerContainers.Add(container);
        OnInventoryContainerAdded?.Invoke(container);
    }

    public void RemoveContainer(AttachedContainer container)
    {
        OnPlayerContainers.Remove(container);
        OnInventoryContainerRemoved?.Invoke(container);
    }

    /// <summary>
    /// Requests the server to drop an item out of a container
    /// </summary>
    /// <param name="item">The item to drop</param>
    public void ClientDropItem(Item item)
    {
        CmdDropItem(item.gameObject);
    }

    /// <summary>
    /// Use it to switch between active hands.
    /// </summary>
    /// <param name="container">This AttachedContainer should be the hand to activate.</param>
    public void ActivateHand(AttachedContainer container)
    {
        Hands.SetActiveHand(container);
    }

    [ServerRpc]
    private void CmdDropItem(GameObject gameObject)
    {
        Item item = gameObject.GetComponent<Item>();
        if (item == null)
        {
            return;
        }

        AttachedContainer attachedTo = item.Container?.AttachedTo;
        if (attachedTo == null)
        {
            return;
        }

        if (!CanModifyContainer(attachedTo))
        {
            return;
        }

        item.SetContainer(null);
    }
    public bool CanModifyContainer(AttachedContainer container)
    {
        // TODO: This root transform check might allow you to take out your own organs down the road O_O
        return _openedContainers.Contains(container) || container.transform.root == transform;
    }

    /// <summary>
    /// Requests the server to transfer an item from one container to another, at the given slot position.
    /// </summary>
    /// <param name="item">The item to transfer</param>
    /// <param name="targetContainer">Into which container to move the item</param>
    public void ClientTransferItem(Item item, Vector2Int position, AttachedContainer targetContainer)
    {
        CmdTransferItem(item.gameObject, position, targetContainer);
    }

    /// <summary>
    /// Does this inventory have a specific container ?
    /// </summary>
    public bool HasContainer(AttachedContainer container)
    {
        return _openedContainers.Contains(container);
    }

    /// <summary>
    /// Removes a container from this inventory.
    /// </summary>
    public void CloseContainer(AttachedContainer container)
    {
        container.Container.RemoveObserver(GetComponent<Entity>());
        if (_openedContainers.Remove(container))
        {
            Debug.Log("client call remove");
            SetOpenState(container.gameObject, false);
            NetworkConnection client = Owner;
            if (client != null)
            {
                TargetCloseContainer(client, container);
            }
        }
    }

    [ServerRpc]
    public void CmdContainerClose(AttachedContainer container)
    {
        CloseContainer(container);
    }


    [TargetRpc]
    private void TargetCloseContainer(NetworkConnection target, AttachedContainer container)
    {
        InvokeContainerClosed(container);
    }

    private void InvokeContainerClosed(AttachedContainer container)
    {
        OnContainerClosed?.Invoke(container);
    }


    [ServerRpc]
    private void CmdTransferItem(GameObject itemObject, Vector2Int position, AttachedContainer container)
    {
        Item item = itemObject.GetComponent<Item>();
        if (item == null)
        {
            return;
        }

        Container itemContainer = item.Container;
        if (itemContainer == null)
        {
            return;
        }

        AttachedContainer attachedTo = itemContainer.AttachedTo;
        if (attachedTo == null)
        {
            return;
        }

        if (container == null)
        {
            Debug.LogError($"Client sent invalid container reference: NetId {container.ObjectId}");
            return;
        }

        if (!CanModifyContainer(attachedTo) || !CanModifyContainer(container))
        {
            return;
        }

        HandsAlt hands = GetComponent<HandsAlt>();
        if (hands == null || !hands.CanInteract(container.gameObject))
        {
            return;
        }

        itemContainer.RemoveItem(item);
        container.Container.AddItemPosition(item, position);
    }

    /// <summary>
    /// On containers having OpenWhenContainerViewed set true in AttachedContainer, this set the containers state appropriately.
    /// If the container belongs to another Inventory, it's already opened, and therefore it does nothing.
    /// If this Inventory is the first to have it, it triggers the open animation of the object.
    /// If this Inventory is the last to have it, it closes the container.
    /// </summary>
    /// <param name="containerObject"> The container's game object belonging to this inventory.</param>
    /// <param name="state"> The state to set in the container, true is opened and false is closed.</param>
    [Server]
    private void SetOpenState(GameObject containerObject, bool state)
    {
        var container = containerObject.GetComponent<AttachedContainer>();

        if (!container.OpenWhenContainerViewed)
        {
            return;
        }

        Hands hands = GetComponent<Hands>();
        foreach (Entity observer in container.Container.ObservingPlayers)
        {
            // checks if the container is already viewed by another entity
            if (hands.Inventory.HasContainer(container) && observer != hands)
            {
                return;
            }
        }

        container.ContainerInteractive.SetOpenState(state);
    }

    [TargetRpc]
    private void TargetOpenContainer(NetworkConnection target, AttachedContainer container)
    {
        InvokeContainerOpened(container);
    }

    private void InvokeContainerOpened(AttachedContainer container)
    {
        OnContainerOpened?.Invoke(container);
    }


    /// <summary>
    /// Make this inventory open an container.
    /// </summary>
    public void OpenContainer(AttachedContainer attachedContainer)
    {
        attachedContainer.Container.AddObserver(GetComponent<Entity>());
        _openedContainers.Add(attachedContainer);
        SetOpenState(attachedContainer.gameObject, true);
        NetworkConnection client = Owner;
        if (client != null)
        {
            TargetOpenContainer(client, attachedContainer);
        }
    }

    /// <summary>
    /// Interact with a container at a certain position. Transfer items from selected hand to container, or from container to selected hand.
    /// </summary>
    /// <param name="container">The container being interacted with.</param>
    /// <param name="position">Position of the slot where the interaction happened.</param>
    public void ClientInteractWithContainerSlot(AttachedContainer container, Vector2Int position)
    {
        if (Hands == null)
        {
            return;
        }

        Item item = container.Container.ItemAt(position);
        // If selected hand is empty and an item is present on the slot position in the container, transfer it to hand.
        if (Hands.SelectedHandEmpty)
        {
            if (item != null)
            {
                ClientTransferItem(item, Vector2Int.zero, Hands.SelectedHand);
            }
        }
        // If selected hand has an item and there's no item on the slot in the container, transfer it to container slot.
        else
        {
            if (item == null)
            {
                ClientTransferItem(Hands.ItemInHand, position, container);
            }
        }
    }





    void Update()
    {
        
    }
}
