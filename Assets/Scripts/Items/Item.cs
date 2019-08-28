using System;
using Mirror;
using UnityEngine;

[Serializable]
public class Item : NetworkBehaviour
{
    public Sprite Sprite;

    public SlotTypes compatibleSlots = SlotTypes.LeftHand | SlotTypes.RightHand | SlotTypes.Storage;

    [SerializeField]
    private VisualObject visualObjectPrefab;

    [SyncVar]
    public bool Held;

    public VisualObject visual;

    public void CreateVisual(Transform target)
    {
        if (!visual) visual = Instantiate(visualObjectPrefab, target);
        visual.name = "visual - " + name;
        visual.Initialize(GetComponentInChildren<MeshFilter>().mesh, GetComponentInChildren<MeshRenderer>().materials);
    }

    public void MoveVisual(GameObject slotObject)
    {
        ItemSlot slot = slotObject.GetComponent<ItemSlot>();
        visual.transform.SetParent(slot.physicalItemLocation);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
    }

    [ClientRpc]
    public void RpcDrop()
    {
        ShowOriginal();

        transform.position = visual.transform.position;
        transform.rotation = visual.transform.rotation;
        visual.transform.SetParent(null);

        Destroy(visual.gameObject);
    }

    public void HideOriginal()
    {
        transform.localScale = Vector3.zero;
    }

    public void ShowOriginal()
    {
        transform.localScale = Vector3.one;
    }
}