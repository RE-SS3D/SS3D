using SS3D.Systems.Tile.TileMapCreator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small script for map's name slots in the loading tab of the tilemap menu.
/// Mostly to access some particular components.
/// </summary>
public class MapNameSlot : MonoBehaviour
{
    [SerializeField]
    private ControlsOffInputField _mapNameField;

    [SerializeField]
    private UnityEngine.UI.Button _deleteButton;

    [SerializeField]
    private UnityEngine.UI.Button _renameButton;

    /// <summary>
    /// The input field for the map name.
    /// </summary>
    public ControlsOffInputField MapNameField => _mapNameField;

    /// <summary>
    /// The button close to the map name to delete the map.
    /// </summary>
    public UnityEngine.UI.Button DeleteButton => _deleteButton;

    /// <summary>
    /// The button close to the map name to rename it.
    /// </summary>
    public UnityEngine.UI.Button RenameButton => _renameButton;
}
