﻿using UnityEngine;
using Mirror;
using SS3D.Engine.Interactions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Inventory;

public class EnergySword : Item, IToggleable
{
    [SerializeField]
    private Animator animator;
    private bool on;
    private static readonly int OnHash = Animator.StringToHash("On");

    [SerializeField]
    private AudioSource audio;
    [SerializeField]
    private AudioClip soundOn;
    [SerializeField]
    private AudioClip soundOff;

    [ClientRpc]
    private void RpcSetBlade(bool on)
    {
        audio.clip = on ? soundOn : soundOff;
        audio.Play();
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

        audio.clip = on ? soundOn : soundOff;
        audio.Play();

        animator.SetBool(OnHash, on);
        RpcSetBlade(on);
    }

    public override IInteraction[] GenerateInteractions(IInteractionTarget[] targets)
    {
        List<IInteraction> interactions = base.GenerateInteractions(targets).ToList();
        interactions.Insert(0, new ToggleInteraction
        {
            OnName = "Turn off", OffName = "Turn on"
        });
        return interactions.ToArray();
    }
}
