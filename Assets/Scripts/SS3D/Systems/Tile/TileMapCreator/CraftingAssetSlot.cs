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
    private List<Tuple<WorldObjectAssetReference, string>> assetsAndNames;

    private CraftingInteraction _interaction;

    private InteractionEvent _interactionEvent;

    [SerializeField]
    private GameObject _craftingSlotPartPrefab;


    /// <summary>
    /// Load an UI icon and string for the item/tile.
    /// </summary>
    /// <param name="genericObjectSo"></param>
    public void Setup(CraftingInteraction interaction, InteractionEvent interactionEvent)
    {
        _interaction = interaction;
        _interactionEvent = interactionEvent;

        List<Tuple<WorldObjectAssetReference, string>> assetsAndNames = new();

        // Prepare every results to be shown
        foreach(WorldObjectAssetReference assetReference in interaction.ChosenLink.Target.Results)
        {
            assetsAndNames.Add(new Tuple<WorldObjectAssetReference, string>(assetReference, assetReference.Prefab.name));
        }

        // If the link doesn't lead to a new result, show the modified object too.
        if (!interaction.ChosenLink.Target.IsTerminal)
        {
            assetsAndNames.Add(new Tuple<WorldObjectAssetReference, string>(interaction.ChosenLink.Target.Recipe.Target, interaction.ChosenLink.Target.Name));
        }

        foreach(Tuple<WorldObjectAssetReference, string> assetAndName in assetsAndNames)
        {
            GameObject craftingSlotPart = Instantiate(_craftingSlotPartPrefab, gameObject.transform);
            craftingSlotPart.GetComponent<TMP_Text>().text = assetAndName.Item2;
        }

    }

    public void OnClick()
    {
        ViewLocator.Get<CraftingMenu>().First().SetSelectedInteraction(_interaction, _interactionEvent);
    }
}
