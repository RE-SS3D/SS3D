using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Management;
using SS3D.Logging;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.TileMapCreator;
using TMPro;
using UnityEngine;

/// <summary>
/// Handle the UI and logic related to saving and loading maps in the tilemap menu.
/// </summary>
public class TileMapSaveAndLoad : NetworkActor
{

    [SerializeField]
    private GameObject _mapNameSlotPrefab;

    [SerializeField]
    private GameObject _saveMapContentRoot;

    [SerializeField]
    private GameObject _loadMapContentRoot;

    [SerializeField]
    private TMP_InputField _saveInputField;


    [SerializeField]
    private TileMapMenu _menu;

    /// <summary>
    /// Method called when the load button is clicked.
    /// </summary>
    [Server]
    public void HandleLoadButton()
    {
        DisplayMapLoader();
    }

    /// <summary>
    /// Method called when the save button is clicked.
    /// </summary>
    [Server]
    public void HandleSaveButton()
    {
        DisplayMapSaver();
    }

    public void OnSaveInputFieldEndEdit()
    {
        SaveMap(_saveInputField.text);
    }

    [Server]
    public void DisplayMapSaver()
    {
        _menu.ClearGrid();
        _saveMapContentRoot.SetActive(true);
    }

    [Server]
    public void DisplayMapLoader()
    {
        _menu.ClearGrid();
        var MapNames = SaveSystem.GetAllObjectsNameInFolder(TileSystem.SavePath);

        foreach (string mapName in MapNames)
        {
            string mapNameWithNoExtension = mapName.Substring(0, mapName.IndexOf("."));
            GameObject slot = Instantiate(_mapNameSlotPrefab, _loadMapContentRoot.transform, true);
            slot.GetComponentInChildren<TextMeshProUGUI>().text = mapNameWithNoExtension;
            slot.GetComponentInChildren<UnityEngine.UI.Button>().onClick.AddListener(() => LoadMap(mapNameWithNoExtension));
        }
    }

    [Server]
    private void LoadMap(string mapName)
    {
        if (IsServer)
        {
            Subsystems.Get<TileSystem>().Load(TileSystem.SavePath + "/" + mapName);
        }
        else
        {
            Log.Information(this, "Cannot load the map on a client");
        }
    }

    [Server]
    public void SaveMap(string mapName)
    {
        if (IsServer)
        {
            Subsystems.Get<TileSystem>().Save(mapName);
        }
        else
        {
            Log.Information(this, "Cannot save the map on a client");
        }
    }
}
