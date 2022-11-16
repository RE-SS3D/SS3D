using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.GameModes.Objectives;
using SS3D.Systems.Items;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives.NukeObjectives
{
    [CreateAssetMenu(menuName = "Gamemode/Objectives/GetNukeCard", fileName = "GetNukeCard")]
    public class GetNukeCardObjective : GamemodeObjective
    {
        Item ItemRef;

        public override void InitializeObjective()
        {
            ItemPickedUpEvent.AddListener(HandleItemPickedUpEvent);
            Title = "Retrieve the Nuclear Authentication Disk";
            Status = ObjectiveStatus.InProgress;
        }

        [Server]
        public override void CheckCompleted()
        {
            if (ItemRef is NukeCard)
            {
                Success();
            }
        }

        private void HandleItemPickedUpEvent(ref EventContext context, in ItemPickedUpEvent e)
        {
            ItemRef = e.ItemRef;

            CheckCompleted();
        }
    }
}
