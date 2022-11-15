using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.GameModes.Objectives;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives.NukeObjectives
{
    [CreateAssetMenu(menuName = "Gamemode/Objectives/ItemPickedUpObjective", fileName = "ItemPickedUpObjective")]
    public class ItemPickedUpObjective : GamemodeObjective
    {
        public Item Match;

        public override void InitializeObjective()
        {
            ItemPickedUpEvent.AddListener(HandleItemPickedUpEvent);
        }

        [Server]
        public override void CheckCompleted()
        {
            
        }

        private void HandleItemPickedUpEvent(ref EventContext context, in ItemPickedUpEvent e)
        {
            CheckCompleted();
        }
    }
}