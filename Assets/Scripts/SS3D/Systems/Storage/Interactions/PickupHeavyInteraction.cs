using SS3D.Data;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Storage.Containers;
using SS3D.Systems.Storage.Items;
using SS3D.Systems.GameModes.Events;
using UnityEngine;
using SS3D.Core;
using SS3D.Systems.PlayerControl;
using FishNet.Object;
using SS3D.Systems.Health;

namespace SS3D.Systems.Storage.Interactions
{
    /// <summary>
    /// This is simply a pickup interaction which consumes stamina. Testing purposes only.
    /// </summary>
    public class PickupHeavyInteraction : StaminaRequirement
    {
        public PickupHeavyInteraction(float weight): base(new PickupInteraction())
        {
            if (weight < 5f)
            {
                exertion = Exertion.None;
            }
            else if (weight < 10f)
            {
                exertion = Exertion.Light;
            }
            else if (weight < 15f)
            {
                exertion = Exertion.Moderate;
            }
            else if (weight < 20f)
            {
                exertion = Exertion.Heavy;
            }
            else
            {
                exertion = Exertion.Extreme;
            }

        }

        public Sprite Icon
        {
            get => (Interaction as PickupInteraction).Icon;
            set => (Interaction as PickupInteraction).Icon = value;
        }
    }



}
