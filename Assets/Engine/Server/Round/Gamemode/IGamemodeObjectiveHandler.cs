using SS3D.Content;
using UnityEngine;

namespace SS3D.Engine.Server.Round
{
    /// <summary>
    /// This is the class that administrates the objectives given in the current round.
    /// 
    /// </summary>
    public interface IGamemodeObjectiveHandler
    {
        bool ValidateObjectiveCompletion();
        Entity TryToAssignObjectiveOwner(Entity owner);
        Entity ForceAssignObjectiveOwner(Entity owner);
        void UpdateCompletionStatus(Entity entity, bool state);
    }
}