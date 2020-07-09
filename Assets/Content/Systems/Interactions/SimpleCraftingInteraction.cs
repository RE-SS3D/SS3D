using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;

public class SimpleCraftingInteraction : IInteraction
{
    public Sprite icon;

    /// <summary>
    /// The id of the necessary item
    /// </summary>
    public string ItemId { get; set; }

    /// <summary>
    /// Should the source be consumed
    /// </summary>
    public bool Consume { get; set; }

    /// <summary>
    /// The objects which are created
    /// </summary>
    public CraftingResult[] ResultingObjects { get; set; }

    /// <summary>
    /// The name shown in the context menu
    /// </summary>
    public string Name { get; set; }


    public IClientInteraction CreateClient(InteractionEvent interactionEvent)
    {
        return null;
    }

    public string GetName(InteractionEvent interactionEvent)
    {
        return Name;
    }

    public Sprite GetIcon(InteractionEvent interactionEvent)
    {
        return icon;
    }

    public bool CanInteract(InteractionEvent interactionEvent)
    {
        Item item = interactionEvent.Source as Item;
        if (item == null || item.ItemId != ItemId)
        {
            return false;
        }

        if (!InteractionExtensions.RangeCheck(interactionEvent))
        {
            return false;
        }

        return true;
    }

    public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
    {
        Item item = (Item) interactionEvent.Source;
        Item targetItem = interactionEvent.Target.GetComponent<Item>();
        if (Consume)
        {
            item.Destroy();
        }

        GameObject targetGameObject = targetItem.gameObject;


        for (int index = 0; index < ResultingObjects.Length; index++)
        {
            CraftingResult result = ResultingObjects[index];
            Item created = ItemHelpers.CreateItem(result.Prefab);
            GameObject gameObject = created.gameObject;
            gameObject.transform.position = targetGameObject.transform.position + targetGameObject.transform.rotation * result.Position;
            gameObject.transform.eulerAngles = targetGameObject.transform.eulerAngles + result.Rotation;
            
            if (index == 0 && ResultingObjects.Length == 1)
            {
                ItemHelpers.ReplaceItem(targetItem, created);
            }
        }

        if (ResultingObjects.Length > 1)
        {
            targetItem.Destroy();
        }


        return false;
    }

    public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
    {
        throw new System.NotImplementedException();
    }

    public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
    {
        throw new System.NotImplementedException();
    }

    [Serializable]
    public struct CraftingResult
    {
        public GameObject Prefab;
        public Vector3 Position;
        public Vector3 Rotation;
    }
}