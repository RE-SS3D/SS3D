using SS3D.Systems.Furniture;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using UnityEngine;
using SS3D.Systems.GameModes.Events;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Systems.PlayerControl;
using System;

namespace SS3D.Systems.Items
{
    /// <summary>
    /// Honks a horn. Honking requires the target to be BikeHorn
    /// </summary>
    public class SimpleInteraction : Interaction
    {
        private string _name;
        private Sprite _icon;

        public Func<bool> canInteract;
        public Func<bool> interact;

        public SimpleInteraction(string name, Sprite icon,Func<bool> canInteract,
            Func<bool> interact)
        {
            _name = name;
            _icon = icon;
            this.canInteract = canInteract;
            this.interact = interact;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return _name;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return _icon;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            return canInteract();
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return interact();
        }
    }
}