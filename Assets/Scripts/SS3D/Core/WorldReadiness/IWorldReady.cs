using System;

namespace SS3D.Core.WorldReadiness
{
    /// <summary>
    /// Per-domain readiness: registered ≠ ready. Consumers use <see cref="IsReady"/> /
    /// <see cref="WhenReady"/> — never <c>SubSystems.Get</c> in Update assuming the target is functional.
    /// </summary>
    public interface IWorldReady
    {
        bool IsReady { get; }

        event Action WhenReady;
    }
}
