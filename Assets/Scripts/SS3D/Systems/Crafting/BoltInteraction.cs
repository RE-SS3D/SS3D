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
        private Transform _characterTransform;
        private Vector3 _startPosition;
        private bool _replace;

        public bool Replace => _replace;

        public BoltInteraction(float delay, Transform characterTransform)
        {
            _characterTransform = characterTransform;
            _startPosition = characterTransform.position;
            Delay = delay;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // Should only check for movement once the interaction started.
            if (HasStarted && !InteractionExtensions.CharacterMoveCheck(_startPosition, _characterTransform.position)) return false;

            if (!base.CanInteract(interactionEvent)) return false;

            GameObject recipeResult = _recipe.Result[0];

            if (!recipeResult.TryGetComponent<PlacedTileObject>(out var result)) return false;

            if (!Subsystems.Get<TileSystem>().CanBuild(result.tileObjectSO, interactionEvent.Target.GetGameObject().transform.position, Direction.North, false)) return false;

            return true;
        }

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
            _startPosition = _characterTransform.position;
            Subsystems.Get<AudioSystem>().PlayAudioSource(AudioType.Sfx, Sounds.Screwdriver,
                interactionEvent.Target.GetGameObject().GetComponent<NetworkObject>());
            return true;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return GetGenericName() + " " + interactionEvent.Target.GetGameObject().name.Split("(")[0];
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {

        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            base.StartDelayed(interactionEvent);

            Subsystems.Get<AudioSystem>().StopAudioSource(interactionEvent.Target.GetGameObject().GetComponent<NetworkObject>());
        }
    }
}

