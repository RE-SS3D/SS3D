using UnityEngine;
using Mirror;
using SS3D.Engine.Interactions;
using System.Collections;
<<<<<<< HEAD
using System.Media;
=======
>>>>>>> upstream/master

public class EnergySword : NetworkBehaviour, Interaction
{
    [SerializeField]
    private Animator animator;
<<<<<<< HEAD

    [SerializeField]
    private AudioSource audio;
    [SerializeField]
    private AudioClip soundOn;
    [SerializeField]
    private AudioClip soundOff;

=======
>>>>>>> upstream/master
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
<<<<<<< HEAD
        if (audio.isPlaying && animator.IsInTransition(0))
            return;

        on = !on;
        animator.SetBool("On", on);

        audio.clip = on ? soundOn : soundOff;
        audio.Play();

=======
        on = !on;
        animator.SetBool("On", on);

>>>>>>> upstream/master
        RpcSetBlade(on);
    }

    [ClientRpc]
    private void RpcSetBlade(bool on)
    {
<<<<<<< HEAD
        if (audio.isPlaying && animator.IsInTransition(0))
            return;

        on = !on;
        animator.SetBool("On", on);

        audio.clip = on ? soundOn : soundOff;
        audio.Play();
=======
        animator.SetBool("On", on);
>>>>>>> upstream/master
    }
}
