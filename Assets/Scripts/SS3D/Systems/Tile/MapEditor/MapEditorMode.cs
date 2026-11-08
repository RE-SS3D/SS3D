namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// The palette's four tabs (creative-mode §2). Labels live in <see cref="MapEditorSubTabs.ModeLabel"/>.
    /// </summary>
    public enum MapEditorMode
    {
        Upper,
        Lower,
        Items,
        Scripting,
    }

    /// <summary>
    /// A palette sub-tab ID. Every fact about a sub-tab is a row in <see cref="MapEditorSubTabs"/>.
    /// Values are explicit and serialized into <c>MapEditorCatalog.asset</c>: never renumber or reuse one.
    /// The 0–20 range belonged to the retired taxonomy and stays unused.
    /// </summary>
    public enum MapEditorSubcategory
    {
        // Upper
        Floors = 100,
        WallsAndWindows = 101,
        Doors = 102,
        WallMounts = 103,
        Furniture = 104,
        Machines = 105,
        Decals = 106,

        // Lower
        Subfloor = 200,
        Cables = 201,
        GasPipes = 202,
        DisposalPipes = 203,

        // Items
        Tools = 300,
        MaterialsAndParts = 301,
        Clothing = 302,
        Medical = 303,
        FoodAndDrink = 304,
        Devices = 305,
        Weapons = 306,
        Misc = 307,

        // Scripts
        SpawnPoints = 400,
        Rooms = 401,
        StartingAir = 402,
    }
}
