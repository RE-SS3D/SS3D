using UnityEngine;
using Mirror;
using SS3D.Engine.Interactions;
using System.Collections;
using System.Media;

public class EnergySword : NetworkBehaviour, Interaction
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private Transform blade;

    [SerializeField]
    private AudioSource audio;
    [SerializeField]
    private AudioClip soundOn;
    [SerializeField]
    private AudioClip soundOff;

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
        if (audio.isPlaying && animator.IsInTransition(0))
            return;

        on = !on;

        animator.SetBool("On", on);

        audio.clip = on ? soundOn : soundOff;
        audio.Play();

        RpcSetBlade(on);
    }

    [ClientRpc]
    private void RpcSetBlade(bool on)
    {
        if (audio.isPlaying && animator.IsInTransition(0))
            return;

        on = !on;
        animator.SetBool("On", on);

        audio.clip = on ? soundOn : soundOff;
        audio.Play();
    }
}
