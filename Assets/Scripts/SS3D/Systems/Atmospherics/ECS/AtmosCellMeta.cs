namespace SS3D.Systems.Atmospherics.ECS
{
    public struct AtmosCellMeta
    {
        public float Temperature;
        public float Volume;
        public AtmosCellState State;

        public bool IsSimulated => State is AtmosCellState.Active or AtmosCellState.Semiactive;
    }
}
