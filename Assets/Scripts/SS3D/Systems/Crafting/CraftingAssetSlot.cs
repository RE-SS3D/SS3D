using SS3D.Systems.Tile.TileMapCreator;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SS3D.Data.AssetDatabases;
using System;
using QuikGraph;
using static SS3D.Systems.Crafting.CraftingRecipe;
using SS3D.Systems.Crafting;
using UnityEngine.AddressableAssets;
using SS3D.Interactions;
using SS3D.Core;
using System.Linq;

public class CraftingAssetSlot : MonoBehaviour
{
    private string resultName ;

    private CraftingInteraction _interaction;

    private InteractionEvent _interactionEvent;

    [SerializeField]
    private GameObject _craftingSlotPartPrefab;


    /// <summary>
    /// Set up the name of the recipe step in a UI slot for the crafting menu. 
    /// </summary>
    public void Setup(CraftingInteraction interaction, InteractionEvent interactionEvent)
    {
        _interaction = interaction;
        _interactionEvent = interactionEvent;

        // Prepare the main result of the recipe if it exist
        if(interaction.ChosenLink.Target.TryGetResult(out WorldObjectAssetReference result))
        {
            resultName = result.Prefab.name;
        }

        // If the link doesn't lead to a new result, show the modified object.
        if (!interaction.ChosenLink.Target.IsTerminal)
        {
            resultName = interaction.ChosenLink.Target.Name;
        }

        GetComponentInChildren<TMP_Text>().text = resultName;
    }

    public void OnClick()
    {
        ViewLocator.Get<CraftingMenu>().First().SetSelectedInteraction(_interaction, _interactionEvent);
    }
}
