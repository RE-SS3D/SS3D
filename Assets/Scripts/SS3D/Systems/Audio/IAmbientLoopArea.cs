using SS3D.Systems.Area;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// A machine that knows which Area it serves, for loops whose
    /// <see cref="AmbientLoopSettings.StaysInArea"/> is set. Read on clients, so implementations
    /// must answer from state a pure client has (the floor cache, not the server registry).
    /// </summary>
    public interface IAmbientLoopArea
    {
        bool TryGetLoopArea(out AreaId areaId);
    }
}
