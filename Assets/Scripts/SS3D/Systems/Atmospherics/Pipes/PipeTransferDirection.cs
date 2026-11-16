namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Direction of mole transfer between a turf gas cell and a pipe network pool.
    /// </summary>
    public enum PipeTransferDirection
    {
        /// <summary>Move moles from the turf cell into the pipe network.</summary>
        ToNetwork,

        /// <summary>Move moles from the pipe network into the turf cell.</summary>
        ToTurf,
    }
}
