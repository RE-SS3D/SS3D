namespace SS3D.Systems.Atmospherics
{
    public interface IAtmosValve : IAtmosPipe
    {
        public bool IsOpen { get; }
    }
}
