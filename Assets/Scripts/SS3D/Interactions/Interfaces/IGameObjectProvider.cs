using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// This is necessary to get the Game Object of some objects passed as interfaces in argument.
    /// Objects passed as IInteractionSource for example, can't give an access to the GameObject property.
    /// However, if the IInteractionSource object also implement IGameObjectProvider, we can get the Game object.
    /// </summary>
    public interface IGameObjectProvider
    {
        GameObject GameObject { get; }
    }
}