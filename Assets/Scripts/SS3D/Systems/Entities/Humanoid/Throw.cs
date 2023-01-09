using System.Linq;
using SS3D.Core.Behaviours;
using SS3D.Systems.Storage.Containers;
using UnityEngine;
using SS3D.Systems.Storage.Items;
using FishNet.Object;

public class Throw : NetworkActor
{
    Hands _hands;
    Animator _humanAnimator;

    void Start()
    {
        _hands = GetComponent<Hands>();
        _humanAnimator = GetComponent<Animator>();

        foreach(AttachedContainer container in _hands.HandContainers)
        {
            container.OnItemDetached += ContainerOnItemDetached;
        }
        
    }

    

    
    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Y))
        {
            
            _humanAnimator.SetTrigger("Throw");
            CmdSetTriggerThrow();
            print("Y key was pressed by " + _humanAnimator.gameObject.name);
        }
    }

    [ObserversRpc(IncludeOwner = false)]
    private void RpcSetTriggerThrow()
    {
        _humanAnimator.SetTrigger("Throw");
    }

    [ServerRpc]
    private void CmdSetTriggerThrow()
    {
        if(!IsHost)
            _humanAnimator.SetTrigger("Throw");
        RpcSetTriggerThrow();
    }

    void ContainerOnItemDetached(object sender, Item item)
    {
        if (_humanAnimator.GetCurrentAnimatorStateInfo(1).IsName("CHR_ThrowItem_HRM") &&
            _humanAnimator.GetCurrentAnimatorStateInfo(1).normalizedTime < 1.0f)
        {
            var sameDirectionAsBody = transform.rotation * Vector3.forward;
            item.gameObject.GetComponent<Rigidbody>().AddForce(sameDirectionAsBody * 200);
        }
        
    }

    /// <summary>
    /// Called when the throwing animation event reach a realistic point where one would
    /// leave the item go when throwing (arm extended).
    /// </summary>
    public void ThrowAnimationEvent()
    {
        var hands = GetComponent<Hands>();
        var item = hands.SelectedHandContainer.Items.First();
        if (item != null)
        {
            var inventory = GetComponent<Inventory>();
            inventory.ClientDropItem(item);

        }
    }

    
}
