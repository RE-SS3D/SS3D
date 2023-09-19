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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public class SliceInteraction : CraftingInteraction
    {
        public SliceInteraction(float delay)
        {
            Delay = delay;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if(!base.CanInteract(interactionEvent)) return false;

            bool isInRange = InteractionExtensions.RangeCheck(interactionEvent);
            return isInRange;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Assets.Get(InteractionIcons.Take);
        }

        public override string GetGenericName()
        {
            return "Slice";
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

