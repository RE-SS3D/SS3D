using System;
using System.Collections.Generic;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [CreateAssetMenu(fileName = "Group Event", menuName = "Interaction/Group Trigger", order = 0)]
    public class GroupTrigger : SingularInteraction
    {
        [SerializeField] private InteractionKind inputKind = null;
        [SerializeField] private InteractionKind[] senders = new InteractionKind[0];
        [SerializeField] private InteractionKind outputKind = null;
        
        private readonly HashSet<InteractionKind> checkset = new HashSet<InteractionKind>();

        public override void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(inputKind);
        }

        public override void Reset()
        {
            foreach (var sender in senders)
            {
                checkset.Add(sender);
            }
        }

        public override bool Handle(Core.InteractionEvent e)
        {
            if (!checkset.Contains(e.previousKind))
            {
                return false;
            }

            checkset.Remove(e.previousKind);

            if (checkset.Count > 0)
            {
                return false;
            }

            e.kind = outputKind;
            Receiver.Trigger(e);
            return true;
        }
    }
}