namespace SS3D.Systems.Atmospherics.ECS
{
    /// <summary>
    /// Signed bulk flow in moles per second across the two edges a cell owns. Positive flow leaves
    /// this cell toward its east or north neighbour, so every open edge is stored exactly once.
    /// </summary>
    public struct AtmosEdgeFlow
    {
        public const int NorthAxis = 0;
        public const int EastAxis = 1;

        public float North;
        public float East;

        public float Get(int axis) => axis == NorthAxis ? North : East;

        public void Set(int axis, float flow)
        {
            if (axis == NorthAxis)
                North = flow;
            else
                East = flow;
        }
    }

    /// <summary>Per-substep working values for edge flow, rebuilt for the working set each run.</summary>
    public struct AtmosEdgeCellScratch
    {
        public float Pressure;
        public float TotalMoles;
        public float Outflow;
        public float OutflowScale;
        public bool Awake;
    }
}
