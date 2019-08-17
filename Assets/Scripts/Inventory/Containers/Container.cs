using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Container : Inventory
{
    [SerializeField]
    private int size = 1;

    [SerializeField]
    private int columns = 4;

    [SerializeField]
    private List<Transform> visualItemLocations;

    [SerializeField]
    private ContainerUI containerUIPrefab;

    private ContainerUI ui;

    private bool visible;

    private void Start()
    {
        ui = Instantiate(containerUIPrefab);
        ui.Initialize(size, columns, transform, visualItemLocations, this);
    }

    public void ToggleUI()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            ui.ToggleVisible();
    }
}