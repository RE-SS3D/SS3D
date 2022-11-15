using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Logging;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.GameModes.Objectives;
using SS3D.Systems.Items;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives.NukeObjectives
{
    [CreateAssetMenu(menuName = "Gamemode/Objectives/ItemPickedUpObjective", fileName = "ItemPickedUpObjective")]
    public class ItemPickedUpObjective : GamemodeObjective
    {
        public Item Match;
        private Item ItemRef;

        public override void InitializeObjective()
        {
            Punpun.Say(this, this.name + " initialized");
            ItemPickedUpEvent.AddListener(HandleItemPickedUpEvent);
        }

        [Server]
        public override void CheckCompleted()
        {
            if (ItemRef && Match)
            {
                Punpun.Say(this, "Checking if " + ItemRef.name + " is " + Match.name);
                if (ItemRef == Match)
                {
                    Status = ObjectiveStatus.Success;
                }
            }
        }

        private void HandleItemPickedUpEvent(ref EventContext context, in ItemPickedUpEvent e)
        {
            ItemRef = e.ItemRef;
            CheckCompleted();
        }
    }
}