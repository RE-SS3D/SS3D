using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class ContainerUI : MonoBehaviour
{
    public GameObject   itemPrefab;
    public Container    container;

    public void ToggleShow()
    {
        if (show) // hide
        {

        }
        else // show
        {

        }
    }

    private void Start()
    {
        if(container == null)
            return;

        // Create all the objects for each tile
        for (int i = 0; i < container.items.Length; ++i)
        {
            int x = (i + 1) % 4;
            int y = (i + 1) / 4;
            GameObject itemTile = GameObject.Instantiate(itemPrefab, new Vector3(x * 50f, y * 50f, 0f), new Quaternion());
            itemTile.transform.SetParent(transform, false);
        }
    }

    private bool show = false;
    private List<GameObject> objects;
}