using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Audio;
using SS3D.Systems.Tile;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Interaction to turn something into a placed tile object
    /// </summary>
    public class BoltInteraction : CraftingInteraction
    {
        private bool _replace;

        public bool Replace => _replace;

        public BoltInteraction(float delay, Transform characterTransform) : base(delay, characterTransform) { }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Take;
        }

        public override string GetGenericName()
        {
            return "Bolt";
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            base.Start(interactionEvent, reference);
            Subsystems.Get<AudioSystem>().PlayAudioSource(AudioType.Sfx, Sounds.Screwdriver,
                interactionEvent.Target.GetGameObject().GetComponent<NetworkObject>());
            return true;
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            base.StartDelayed(interactionEvent);

            Subsystems.Get<AudioSystem>().StopAudioSource(interactionEvent.Target.GetGameObject().GetComponent<NetworkObject>());
        }
    }
}

