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

    private TaggedEdge<RecipeStep, RecipeStepLink> _link;

    [SerializeField]
    private GameObject _craftingSlotPartPrefab;


    /// <summary>
    /// Load an UI icon and string for the item/tile.
    /// </summary>
    /// <param name="genericObjectSo"></param>
    public void Setup(TaggedEdge<RecipeStep, RecipeStepLink> link, CraftingInteraction interaction, InteractionEvent interactionEvent)
    {

        _link= link;

        _interaction = interaction;

        List<Tuple<WorldObjectAssetReference, string>> assetsAndNames = new();

        _interactionEvent = interactionEvent;

        foreach(WorldObjectAssetReference assetReference in link.Target.Results)
        {
            assetsAndNames.Add(new Tuple<WorldObjectAssetReference, string>(assetReference, assetReference.Prefab.name));
        }

        if (!link.Target.IsTerminal)
        {
            assetsAndNames.Add(new Tuple<WorldObjectAssetReference, string>(link.Target.Recipe.Target, link.Target.Name));
        }

        foreach(Tuple<WorldObjectAssetReference, string> assetAndName in assetsAndNames)
        {
            GameObject craftingSlotPart = Instantiate(_craftingSlotPartPrefab, gameObject.transform);
            craftingSlotPart.GetComponent<TMP_Text>().text = assetAndName.Item2;
        }

    }

    public void OnClick()
    {
        _interaction.StartCrafting(_interactionEvent, _link);
    }
}
