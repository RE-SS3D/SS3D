using System;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// A live input request handed out by <see cref="InputSubSystem"/> (a pushed context or a
    /// suppression). Dispose it to release exactly this request. Releasing is idempotent and
    /// order-independent, so a caller can never accidentally leave input stuck disabled or enabled.
    /// </summary>
    public interface IInputHandle : IDisposable
    {
    }
}
