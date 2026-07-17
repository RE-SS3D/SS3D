namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// Physical size tier of an item, used as a container's flat fit-check ceiling.
    /// Ordered smallest to largest; an item fits a container if its SizeClass is at or
    /// below the container's MaxSizeClass. See Documents/design/inventory-storage.md §4.
    /// </summary>
    public enum SizeClass
    {
        Tiny = 0,
        Small = 1,
        Normal = 2,
        Bulky = 3,
        Huge = 4,
    }
}
