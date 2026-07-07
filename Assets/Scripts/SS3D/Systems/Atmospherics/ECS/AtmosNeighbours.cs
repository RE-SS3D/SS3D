namespace SS3D.Systems.Atmospherics.ECS
{
    public struct AtmosNeighbours
    {
        public int North;
        public int East;
        public int South;
        public int West;

        public int Get(int directionIndex)
        {
            return directionIndex switch
            {
                0 => North,
                1 => East,
                2 => South,
                3 => West,
                _ => -1,
            };
        }

        public void Set(int directionIndex, int neighbourIndex)
        {
            switch (directionIndex)
            {
                case 0: North = neighbourIndex; break;
                case 1: East = neighbourIndex; break;
                case 2: South = neighbourIndex; break;
                case 3: West = neighbourIndex; break;
            }
        }
    }
}
