using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Inventory
{
    public class ClothingContainers : MonoBehaviour
    {
        public AttachedContainer Gloves => Containers["Gloves"];
        public AttachedContainer Ears => Containers["Ears"];
        public AttachedContainer Jumpsuit => Containers["Jumpsuit"];
        public AttachedContainer Exosuit => Containers["Exosuit"];
        public AttachedContainer Glasses => Containers["Glasses"];
        public AttachedContainer Mask => Containers["Mask"];
        public AttachedContainer Head => Containers["Head"];
        public AttachedContainer Shoes => Containers["Shoes"];
        public AttachedContainer Accessory => Containers["Accessory"];
        // TODO: Replace with actual clothing storage
        public AttachedContainer SuitStorage => Containers["Suit Storage"];

        [NonSerialized]
        public static readonly string[] ClothingSlotNames = {"Ears", "Jumpsuit", "Exosuit", "Glasses", "Mask", "Gloves", "Head", "Shoes", "Accessory", "Suit Storage"};

        [NonSerialized]
        public Dictionary<string, AttachedContainer> Containers = new Dictionary<string, AttachedContainer>();

        public void Awake()
        {
            Filter[] filters = new Filter[1];
            foreach (string slotName in ClothingSlotNames)
            {
                var trait = ScriptableObject.CreateInstance<Trait>();
                trait.Hash = Animator.StringToHash($"Clothing{slotName}".ToUpper());
                var filter = ScriptableObject.CreateInstance<Filter>();
                filter.acceptedTraits = new List<Trait> {trait};
                filter.deniedTraits = new List<Trait>();
                filters[0] = filter;
                Containers.Add(slotName, AttachedContainer.CreateEmpty(gameObject, Vector2Int.one, filters));
            }
        }
    }
}