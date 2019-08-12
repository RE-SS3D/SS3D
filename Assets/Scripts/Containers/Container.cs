using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour
{
    [SerializeField]
    private int size = 1;

//    [SerializeField]
//    private int columns = 4;

    private List<GameObject> items;

    [SerializeField]
    private ContainerUI containerUIPrefab;

    private ContainerUI ui;

    public void Open()
    {
        if (!ui)
        {
            ui = Instantiate(containerUIPrefab);
            ui.Initialize(size, transform);
        }
    }
}