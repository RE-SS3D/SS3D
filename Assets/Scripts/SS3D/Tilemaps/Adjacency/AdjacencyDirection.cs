namespace SS3D.Tilemaps.Adjacency
{
    public enum AdjacencyDirection
    {
        Right = 0b00000001,
        Bottom = 0b00000010,
        Left = 0b00000100,
        Top = 0b00001000,
        BottomRight = 0b00010000,
        BottomLeft = 0b00100000,
        TopLeft = 0b01000000,
        TopRight = 0b10000000

    }
}