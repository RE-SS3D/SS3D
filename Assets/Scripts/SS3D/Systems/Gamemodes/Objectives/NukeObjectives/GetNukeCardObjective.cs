using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.GameModes.Objectives;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives.NukeObjectives
{
    [CreateAssetMenu(menuName = "Gamemode/Objectives/GetNukeCard", fileName = "GetNukeCard")]
    public class GetNukeCardObjective : GamemodeObjective
    {
        public override void InitializeObjective()
        {
            ItemPickedUpEvent.AddListener(HandleItemPickedUpEvent);
        }

        [Server]
        public override void CheckCompleted()
        {
            
        }

        private void HandleItemPickedUpEvent(ref EventContext context, in ItemPickedUpEvent itemPickedUpEvent)
        {
            string ownerName = itemPickedUpEvent.OwnerName;
            string itemName = itemPickedUpEvent.ItemName;

            Status = ObjectiveStatus.Success;

            new ObjectiveStatusChangedEvent(this).Invoke(this);

            Debug.Log("HandleItemPickedUpEvent with values: " + ownerName + "/" + itemName);
        }
    }
}
