using UnityEngine;
using Mirror;
using SS3D.Engine.Interactions;
using System.Collections;
<<<<<<< HEAD
using System.Media;
=======
using System.Collections.Generic;
using System.Linq;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Inventory;
>>>>>>> upstream/master

public class EnergySword : Item, IToggleable
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
    private static readonly int OnHash = Animator.StringToHash("On");

    [ClientRpc]
    private void RpcSetBlade(bool on)
    {
        animator.SetBool(OnHash, on);
    }

    public bool GetState()
    {
        return on;
    }

    public void Toggle()
    {
        if (audio.isPlaying && animator.IsInTransition(0))
            return;

        on = !on;
<<<<<<< HEAD

        animator.SetBool("On", on);

        audio.clip = on ? soundOn : soundOff;
        audio.Play();

=======
        animator.SetBool(OnHash, on);
>>>>>>> upstream/master
        RpcSetBlade(on);
    }

    public override IInteraction[] GenerateInteractions(IInteractionTarget[] targets)
    {
<<<<<<< HEAD
        if (audio.isPlaying && animator.IsInTransition(0))
            return;

        on = !on;
        animator.SetBool("On", on);

        audio.clip = on ? soundOn : soundOff;
        audio.Play();
=======
        List<IInteraction> interactions = base.GenerateInteractions(targets).ToList();
        interactions.Insert(0, new ToggleInteraction
        {
            OnName = "Turn off", OffName = "Turn on"
        });
        return interactions.ToArray();
>>>>>>> upstream/master
    }
}
