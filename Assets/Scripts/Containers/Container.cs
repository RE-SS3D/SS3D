using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Container : MonoBehaviour
{
    [SerializeField]
    private int size = 1;

    [SerializeField]
    private int columns = 4;

    [SerializeField]
    private List<Transform> visualItemLocations;

//    [SerializeField, ReadOnly]
//    private List<Item> initialItems;

    [SerializeField]
    private ContainerUI containerUIPrefab;

    private ContainerUI ui;

    private bool visible;

    private void Awake()
    {
        ui = Instantiate(containerUIPrefab);
        ui.Initialize(size, columns, transform, visualItemLocations);
    }

    public void ToggleUI()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        ui.ToggleVisible();
    }
}