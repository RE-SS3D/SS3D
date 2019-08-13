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

    [SyncVar]
    private bool syncing;

    public VisualObject visual;

    private void Update()
    {
        if (Held && !syncing) InvokeRepeating(nameof(SyncPos), uint.MaxValue, .2f);
        else if (!Held && syncing)
        {
            CancelInvoke(nameof(SyncPos));
            syncing = false;
        }
    }

    private void SyncPos()
    {
        syncing = true;
        transform.position = visual.transform.position;
    }


    public void CreateVisual(Transform target)
    {
        if (!visual) visual = Instantiate(visualObjectPrefab, target);
        visual.name = "visual - " + name;
        visual.Initialize(GetComponentInChildren<MeshFilter>().mesh, GetComponentInChildren<MeshRenderer>().materials);

        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
    }

    [ClientRpc]
    public void RpcMoveVisual(GameObject slotObject)
    {
        ItemSlot slot = slotObject.GetComponent<ItemSlot>();
        visual.transform.SetParent(slot.physicalItemLocation);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
    }

//    public void Release()
//    {
//        RpcRelease();
//    }

    [Command]
    public void CmdRelease()
    {
        RpcRelease();
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