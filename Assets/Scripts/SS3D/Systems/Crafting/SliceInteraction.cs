using Cysharp.Threading.Tasks.Triggers;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Logging;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace SS3D.Systems.Crafting
{
    public class SliceInteraction : CraftingInteraction
    {
        private Transform _characterTransform;
        private Vector3 _startPosition;
        
        public SliceInteraction(float delay, Transform characterTransform)
        {
            _characterTransform = characterTransform;
            _startPosition = characterTransform.position;
            Delay = delay;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is not Item) return false;

            // Should only check for movement once the interaction started.
            if (HasStarted && !InteractionExtensions.CharacterMoveCheck(_startPosition, _characterTransform.position)) return false;

            Item target = interactionEvent.Target as Item;

            // Can slice only things out of containers.
            if (target.Container != null) return false;

            if (!base.CanInteract(interactionEvent)) return false;

            return true;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Take;
        }

        public override string GetGenericName()
        {
            return "Slice";
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

