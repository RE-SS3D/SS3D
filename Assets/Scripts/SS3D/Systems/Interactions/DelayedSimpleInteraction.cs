using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Interactions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedSimpleInteraction : DelayedInteraction
{
    public DelayedSimpleInteraction(
        Action<InteractionEvent, InteractionReference> doImmediately,
        Action<InteractionEvent, InteractionReference> doDelayed,
        Predicate<InteractionEvent> canInteractCallback,
        string name,
        bool animateSource,
        InteractionType interactionType,
        float time)
    {
        DoImmediately = doImmediately;
        DoDelayed = doDelayed;
        CanInteractCallback = canInteractCallback;
        Name = name;
        AnimateSource = animateSource;
        InteractionType = interactionType;
        Time = time;
    }

    public string Name { get; set; }

    /// <summary>
    /// Checks if the interaction should be possible
    /// </summary>
    public Predicate<InteractionEvent> CanInteractCallback { get; private set; } = _ => true;

    /// <summary>
    /// Executed when the interaction takes place
    /// </summary>
    public Action<InteractionEvent, InteractionReference> DoImmediately { get; private set; }

    /// <summary>
    /// Executed when the interaction takes place
    /// </summary>
    public Action<InteractionEvent, InteractionReference> DoDelayed { get; private set; }

    public bool AnimateSource  { get; private set; }

    /// <summary>
    /// Executed when the interaction takes place
    /// </summary>
    public float Time { get; private set; }

    public override string GetName(InteractionEvent interactionEvent) => throw new System.NotImplementedException();

    public override string GetGenericName() => throw new System.NotImplementedException();

    public override InteractionType InteractionType { get; }
    public override Sprite GetIcon(InteractionEvent interactionEvent) => null;

    public override bool CanInteract(InteractionEvent interactionEvent) => CanInteractCallback(interactionEvent);

    public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
    {
        if (interactionEvent.Source is IInteractionSourceAnimate sourceAnimated)
        {
            sourceAnimated.CancelSourceAnimation(InteractionType, interactionEvent.Target.GetComponent<NetworkObject>(), Time);
        }
    }

    protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference) => DoDelayed(interactionEvent, reference);

    protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
    {
        DoImmediately(interactionEvent, reference);

        if (interactionEvent.Source is IInteractionSourceAnimate sourceAnimated)
        {
            sourceAnimated.PlaySourceAnimation(InteractionType, interactionEvent.Target.GetComponent<NetworkObject>(), interactionEvent.Point, Time);
        }
        return true;
    }
}
