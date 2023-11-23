using Coimbra;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handle the UI and logic related to saving and loading maps in the tilemap menu.
/// </summary>
public class TileMapMenuSaveAndLoadTabs : NetworkActor
{
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
