using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.GameModes.Objectives;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives.NukeObjectives
{
    [CreateAssetMenu(menuName = "Gamemode/Objectives/DetonateNuke", fileName = "DetonateNuke")]
    public class DetonateNukeObjective : GamemodeObjective
    {
        public override void InitializeObjective()
        {
            NukeDetonateEvent.AddListener(HandleNukeDetonateEvent);
            Title = "Use it to activate the Nuclear Fission Explosive";
            Status = ObjectiveStatus.InProgress;
        }

        [Server]
        public override void CheckCompleted()
        {
            Success();
        }

        private void HandleNukeDetonateEvent(ref EventContext context, in NukeDetonateEvent e)
        {
            CheckCompleted();
        }
    }
}
