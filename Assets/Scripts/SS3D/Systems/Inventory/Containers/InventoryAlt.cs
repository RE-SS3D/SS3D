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

public class InventoryAlt : MonoBehaviour
{

    public List<AttachedContainer> OnPlayerContainers = new();

    private readonly List<AttachedContainer> _openedContainers = new();

    public delegate void InventoryContainerUpdated(AttachedContainer container);

    public event InventoryContainerUpdated OnInventoryContainerAdded;

    public event InventoryContainerUpdated OnInventoryContainerRemoved;

    public InventoryViewAlt InventoryView { get; private set; }

    public int CountHands => OnPlayerContainers.Where(x => x.Type == ContainerType.Hand).Count();

    void Start()
    {
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





    void Update()
    {
        
    }
}
