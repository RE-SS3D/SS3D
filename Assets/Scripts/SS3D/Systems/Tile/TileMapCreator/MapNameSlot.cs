using SS3D.Systems.Tile.TileMapCreator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapNameSlot : MonoBehaviour
{
    [SerializeField]
    private ControlsOffInputField _mapNameField;

    [SerializeField]
    private UnityEngine.UI.Button _deleteButton;

    [SerializeField]
    private UnityEngine.UI.Button _renameButton;

    public ControlsOffInputField MapNameField => _mapNameField;

    public UnityEngine.UI.Button DeleteButton => _deleteButton;

    public UnityEngine.UI.Button RenameButton => _renameButton;

    public void HandleDeselect()
    {
        Debug.Log("deselect shit");
    }

}
