using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Tile;

/// <summary>
/// Handle the UI and logic related to saving and loading maps in the tilemap menu.
/// </summary>
public class TileMapSaveAndLoad : NetworkActor
{
    /// <summary>
    /// Set up the UI for loading saved maps. TODO : actually implement the logic here. Coming soon.
    /// </summary>
    public void SetUpLoad()
    {
        if (IsServer)
        {
            Subsystems.Get<TileSystem>().Load();
        }
        else
        {
            Log.Information(this, "Cannot load the map on a client");
        }
    }

    /// <summary>
    /// Set up the UI for saving maps. TODO : actually implement the logic here. Coming soon.
    /// </summary>
    public void SetUpSave()
    {
        if (IsServer)
        {
            Subsystems.Get<TileSystem>().Save();
        }
        else
        {
            Log.Information(this, "Cannot save the map on a client");
        }
    }
}
