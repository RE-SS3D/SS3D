using System;
using UnityEngine;

[Serializable]
public class Item : MonoBehaviour
{
    public string[] CompatibleSlots;
    public Sprite Image;

    private Vector3 defaultScale;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    public void Hide()
    {
        transform.localScale = Vector3.zero;
    }

    public void Reveal()
    {
        transform.localScale = defaultScale;
    }
}