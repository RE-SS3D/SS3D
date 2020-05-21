using UnityEngine;
using Mirror;
using SS3D.Engine.Interactions;
using System.Collections;

public class EnergySword : NetworkBehaviour, Interaction
{
    [SerializeField]
    private Animator animator;
    private bool on;

    public InteractionEvent Event { get; set; }
    public string Name => "Turn On/Off";

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool CanInteract()
    {
        return Event.tool == gameObject;
    }

    public void Interact()
    {
        on = !on;
        animator.SetBool("On", on);

        RpcSetBlade(on);
    }

    [ClientRpc]
    private void RpcSetBlade(bool on)
    {
        animator.SetBool("On", on);
    }
}
