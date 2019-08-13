using System;
using Mirror;
using UnityEngine;

[Serializable]
public class Item : NetworkBehaviour
{
    public Sprite Sprite;

    public SlotTypes compatibleSlots = SlotTypes.Hand;

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

        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
    }

    public void MoveVisual(GameObject slotObject)
    {
        ItemSlot slot = slotObject.GetComponent<ItemSlot>();
        visual.transform.SetParent(slot.physicalItemLocation);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
    }
    
    [ClientRpc]
    public void RpcRelease()
    {
        ShowOriginal();
        transform.position = visual.transform.position;
        transform.rotation = visual.transform.rotation;

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