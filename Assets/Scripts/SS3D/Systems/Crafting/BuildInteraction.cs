using Cysharp.Threading.Tasks.Triggers;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Logging;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace SS3D.Systems.Crafting
{
    public class BuildInteraction : CraftingInteraction
    {
        private Transform _characterTransform;
        private Vector3 _startPosition;
        private bool _replace;

        public bool Replace => _replace;

        public BuildInteraction(float delay, Transform characterTransform)
        {
            _characterTransform = characterTransform;
            _startPosition = characterTransform.position;
            Delay = delay;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!interactionEvent.Target.GetGameObject().TryGetComponent<PlacedTileObject>(out var target)) return false;

            // Should only check for movement once the interaction started.
            if (HasStarted && !InteractionExtensions.CharacterMoveCheck(_startPosition, _characterTransform.position)) return false;

            if (!base.CanInteract(interactionEvent)) return false;

            GameObject recipeResult = Assets.Get<GameObject>((int)AssetDatabases.Items, (int)_recipe.Result[0]);

            if (!recipeResult.TryGetComponent<PlacedTileObject>(out var result)) return false;

            _replace = false;

            if(result.Layer == target.Layer)
            {
                _replace = true;
            }


            if (!Subsystems.Get<TileSystem>().CanBuild(result.tileObjectSO, target.transform.position, Direction.North, _replace)) return false;

            return true;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Assets.Get(InteractionIcons.Take);
        }

        public override string GetGenericName()
        {
            return "Build";
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            base.Start(interactionEvent, reference);
            _startPosition = _characterTransform.position;
            return true;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return GetGenericName() + " " + interactionEvent.Target.GetGameObject().name.Split("(")[0];
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {

        }
    }
}

