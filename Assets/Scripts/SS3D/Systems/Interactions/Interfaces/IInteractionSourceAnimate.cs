using FishNet.Object;
using SS3D.Interactions;
using SS3D.Systems.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractionSourceAnimate
{
    public void PlaySourceAnimation(InteractionType interactionType, NetworkObject target, Vector3 point, float time);

    public void CancelSourceAnimation(InteractionType interactionType, NetworkObject target, float time);
}
