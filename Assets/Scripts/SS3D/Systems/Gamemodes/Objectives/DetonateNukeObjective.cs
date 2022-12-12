using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.GameModes.Events;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives
{
    /// <summary>
    /// Objective which the goal is to detonate the nuke. 
    /// </summary>
    [CreateAssetMenu(menuName = "Gamemode/Objectives/DetonateNuke", fileName = "DetonateNuke")]
    public class DetonateNukeObjective : GamemodeObjective
    {
        /// <inheritdoc />
        public override void InitializeObjective()
        {
            SetStatus(ObjectiveStatus.InProgress);

            NukeDetonateEvent.AddListener(HandleNukeDetonateEvent);

            SetTitle("Kaboom*: Activate the Nuclear Fission Explosive");
        }

        /// <inheritdoc />
        public override void FinalizeObjective()
        {
            Succeed();
        }

        public override void CheckCompletion()
        {
            
        }

        private void HandleNukeDetonateEvent(ref EventContext context, in NukeDetonateEvent e)
        {
            FinalizeObjective();
        }
    }
}