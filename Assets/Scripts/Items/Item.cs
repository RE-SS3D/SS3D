using System;
using Mirror;
using UnityEngine;

[Serializable]
public class Item : MonoBehaviour
{
    public Sprite Sprite;

    public SlotTypes compatibleSlots = SlotTypes.Hand;

    private Vector3 defaultScale;

    public Rigidbody rigidBody;

    public bool Held;

    private void Awake()
    {
        defaultScale = transform.localScale;

        if (!rigidBody) rigidBody = GetComponent<Rigidbody>();
    }

    public void Hold(Transform holder)
    {
        transform.SetParent(holder);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

//        rigidBody.isKinematic = true;
        transform.localScale = Vector3.one * 0.01f;
        Held = true;
    }

    public void CmdRelease()
    {
        transform.SetParent(null);
//        rigidBody.isKinematic = false;
        transform.localScale = defaultScale;
        Held = false;
    }

    public void Store()
    {
        transform.localScale = Vector3.zero;
    }

    public void Retrieve()
    {
        transform.localScale = Vector3.one * 0.01f;
    }
}