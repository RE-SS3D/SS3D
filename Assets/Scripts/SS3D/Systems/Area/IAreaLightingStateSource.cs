namespace SS3D.Systems.Area
{
    public interface IAreaLightingStateSource
    {
        bool TryGetLightingState(AreaId areaId, out AreaLightingState state);
    }
}
