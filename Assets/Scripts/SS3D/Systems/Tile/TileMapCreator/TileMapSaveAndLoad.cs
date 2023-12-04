using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Management;
using SS3D.Logging;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.TileMapCreator;
using System.Collections.Generic;
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
    private GameObject _confirmOverWriteButton;

    [SerializeField]
    private TMP_InputField _saveInputField;


    [SerializeField]
    private TileMapMenu _menu;

    public void DisplayMapSaver()
    {
        _saveMapContentRoot.gameObject.SetActive(true);
        _confirmOverWriteButton.gameObject.SetActive(false);
        _saveMapContentRoot.SetActive(true);
    }

    /// <summary>
    /// Method called when the save button is clicked.
    /// </summary>
    public void HandleConfirmOverWriteButton()
    {
        SaveMap(_saveInputField.text);
    }

    /// <summary>
    /// Method called when the save button is clicked.
    /// </summary>
    public void HandleSaveMapButton()
    {
        if (AlreadyContainsName(_saveInputField.text))
        {
            _confirmOverWriteButton.gameObject.SetActive(true);
        }
        else
        {
            SaveMap(_saveInputField.text);
        }
    }

    public void DisplayMapLoader()
    {
        var MapNames = SaveSystem.GetAllObjectsNameInFolder(Subsystems.Get<TileSystem>().SavePath);

        foreach (string mapName in MapNames)
        {
            string mapNameWithNoExtension = mapName.Substring(0, mapName.IndexOf("."));
            GameObject slot = Instantiate(_mapNameSlotPrefab, _loadMapContentRoot.transform, true);
            slot.GetComponentInChildren<TextMeshProUGUI>().text = mapNameWithNoExtension;
            MapNameSlot mapNameSlot = slot.GetComponent<MapNameSlot>();
            mapNameSlot.MapNameButton.onClick.AddListener(() => LoadMap(mapNameWithNoExtension));
            mapNameSlot.DeleteButton.onClick.AddListener(() => DeleteMap(mapNameWithNoExtension));
        }
    }

    private void DeleteMap(string mapName)
    {
        if (IsServer)
        {
            SaveSystem.DeleteFile(Subsystems.Get<TileSystem>().SavePath + "/" + mapName);
        }
        else
        {
            Log.Information(this, "Cannot load the map on a client");
        }
    }

    private void LoadMap(string mapName)
    {
        if (IsServer)
        {
            Subsystems.Get<TileSystem>().Load(Subsystems.Get<TileSystem>().SavePath + "/" + mapName);
        }
        else
        {
            Log.Information(this, "Cannot load the map on a client");
        }
    }

    public void SaveMap(string mapName)
    {
        if (IsServer)
        {
            Subsystems.Get<TileSystem>().Save(mapName, true);
        }
        else
        {
            Log.Information(this, "Cannot save the map on a client");
        }
    }

    public bool AlreadyContainsName(string name)
    {
        string savePath = Subsystems.Get<TileSystem>().SavePath;
        List<string> saves = SaveSystem.GetAllObjectsNameInFolder(savePath);
        return saves.Contains(name+".json");
    }
}
