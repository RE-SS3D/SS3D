using Cysharp.Threading.Tasks.Triggers;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Enums;
using SS3D.Interactions;
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
        public override string GetGenericName()
        {
            return "Slice";
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return GetGenericName() + " " + interactionEvent.Target.ToString();
        } 
    }
}

