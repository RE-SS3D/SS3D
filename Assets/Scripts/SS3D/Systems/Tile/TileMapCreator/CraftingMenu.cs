using Coimbra;
using DynamicPanels;
using QuikGraph;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Systems.Crafting;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.TileMapCreator;
using SS3D.Systems.Tile.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static SS3D.Systems.Crafting.CraftingRecipe;
using InputSystem = SS3D.Systems.Inputs.InputSystem;
using NetworkView = SS3D.Core.Behaviours.NetworkView;

public class CraftingMenu : NetworkView, IPointerEnterHandler, IPointerExitHandler
{

    private Controls.TileCreatorActions _controls;

    private InputSystem _inputSystem;

    /// <summary>
    ///  The model for a single slot, to display tile objects in the menu.
    /// </summary>
    [SerializeField]
    private GameObject _textSlotPrefab;

    [SerializeField]
    private GameObject _pictureSlotPrefab;

    /// <summary>
    /// Game object parent of the area in the tile map menu where the tile object slots will display.
    /// </summary>
    [SerializeField]
    private GameObject _textSlotArea;

    [SerializeField]
    private GameObject _pictureSlotArea;


    private CraftingInteraction _interaction;

    private InteractionEvent _interactionEvent;

    [SerializeField]
    private TextMeshProUGUI _objectTitle;

    

    protected override void OnStart()
    {
        base.OnStart();
        ShowUI(false);
        _inputSystem = Subsystems.Get<InputSystem>();
        _controls = _inputSystem.Inputs.TileCreator;
    }

    /// <summary>
    /// Called when pointer enter the UI of the menu.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _inputSystem.ToggleBinding("<Mouse>/scroll/y", false);
    }

    /// <summary>
    /// Called when pointer exit the UI of the menu.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
    }

    private void ShowUI(bool isShow)
    {        
        gameObject.SetActive(isShow);
    }

    /// <summary>
    /// Method called when the control to open the tilemap menu is performed.
    /// </summary>
    public void DisplayMenu(List<CraftingInteraction> interactions, InteractionEvent interactionEvent, InteractionReference reference)
    {
        ClearGrid();

        foreach (CraftingInteraction interaction in interactions)
        {
            Instantiate(_textSlotPrefab, _textSlotArea.transform, true).GetComponent<CraftingAssetSlot>().Setup(interaction, interactionEvent);
        }
      
        ShowUI(true);
    }

    public void HideMenu()
    {
        ShowUI(false);
    }


    /// <summary>
    /// Clear all tile slots in the content area of the tilemap menu.
    /// </summary>
    private void ClearGrid()
    {
        for (int i = 0; i < _textSlotArea.transform.childCount; i++)
        {
            _textSlotArea.transform.GetChild(i).gameObject.Dispose(true);
        }

        ClearPictures();
    }

    private void ClearPictures()
    {
        for (int i = 0; i < _pictureSlotArea.transform.childCount; i++)
        {
            _pictureSlotArea.transform.GetChild(i).gameObject.Dispose(true);
        }
    }

    public void SetSelectedInteraction(CraftingInteraction interaction, InteractionEvent interactionEvent)
    {

        ClearPictures();

        _interaction = interaction;
        _interactionEvent = interactionEvent;

        _objectTitle.text =  _interaction.ChosenLink.Target.Name;

        GenericObjectSo asset;

        if (_interaction.ChosenLink.Target.IsTerminal)
        {
            asset = Subsystems.Get<TileSystem>().GetAsset(_interaction.ChosenLink.Target.Results.First().Id);
        }
        else
        {
            asset = Subsystems.Get<TileSystem>().GetAsset(_interaction.ChosenLink.Target.Recipe.Target.Id);
        }

        GameObject _pictureSlot = Instantiate(_pictureSlotPrefab, _pictureSlotArea.transform, true);
        _pictureSlot.GetComponent<AssetSlot>().Setup(asset);
    }

    public void OnBuildClick()
    {
        if(_interaction == null || _interactionEvent == null) return;
        InteractionReference reference = _interactionEvent.Source.Interact(_interactionEvent, _interaction);
        _interactionEvent.Source.ClientInteract(_interactionEvent, _interaction, reference);
    }
}
