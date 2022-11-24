using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.GameModes.Objectives;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives.NukeObjectives
{
    [CreateAssetMenu(menuName = "Gamemodes/Objectives/DetonateNuke", fileName = "DetonateNuke")]
    public class DetonateNukeObjective : GamemodeObjective
    {
        public override void InitializeObjective()
        {
            NukeDetonateEvent.AddListener(HandleNukeDetonateEvent);
            Title = "Use it to activate the Nuclear Fission Explosive";
            Status = ObjectiveStatus.InProgress;
        }

        [Server]
        public override void FinalizeObjective()
        {
            Succeed();
        }

        private void HandleNukeDetonateEvent(ref EventContext context, in NukeDetonateEvent e)
        {
            FinalizeObjective();
        }

        public DetonateNukeObjective(int id, string title, ObjectiveStatus status, NetworkConnection author) : base(id, title, status, author) { }
    }
}