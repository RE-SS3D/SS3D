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
        GameObject TryToAssignObjectiveOwner();
        GameObject ForceAssignObjectiveOwner();
    }
}