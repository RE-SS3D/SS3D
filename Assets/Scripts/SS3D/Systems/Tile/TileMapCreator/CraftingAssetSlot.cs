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

public class CraftingAssetSlot : MonoBehaviour
{
    private List<Tuple<WorldObjectAssetReference, string>> assetsAndNames;

    private CraftingInteraction _interaction;

    private InteractionEvent _interactionEvent;

    private InteractionReference _interactionReference;

    [SerializeField]
    private GameObject _craftingSlotPartPrefab;


    /// <summary>
    /// Load an UI icon and string for the item/tile.
    /// </summary>
    /// <param name="genericObjectSo"></param>
    public void Setup(CraftingInteraction interaction, InteractionEvent interactionEvent, InteractionReference reference)
    {
        _interaction = interaction;
        _interactionReference = reference;
        _interactionEvent = interactionEvent;

        List<Tuple<WorldObjectAssetReference, string>> assetsAndNames = new();

        foreach(WorldObjectAssetReference assetReference in interaction.ChosenLink.Target.Results)
        {
            assetsAndNames.Add(new Tuple<WorldObjectAssetReference, string>(assetReference, assetReference.Prefab.name));
        }

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
        var reference =  _interactionEvent.Source.Interact(_interactionEvent, _interaction);
        _interactionEvent.Source.ClientInteract(_interactionEvent, _interaction, reference);
    }
}
