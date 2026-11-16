using System;
using Coimbra;
using SS3D.Logging;
using SS3D.Systems.Entities.Character;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Worn body-mesh map for cloth-slot Items. Pair with <see cref="ClothingItemPresentation"/>
    /// for the folded world/hand child; <see cref="ClothesDisplayer"/> applies these meshes to
    /// <see cref="ClothedBodyPart"/> while the Item is hidden in a worn slot.
    /// </summary>
    public class Cloth : MonoBehaviour
    {
        [SerializeField]
        private SerializableDictionary<ClothType, Mesh> _clothMeshes;

        [SerializeField]
        private Material[] _wornMaterials;

        [SerializeField]
        private string[] _occludedBodyRenderers;

        /// <summary>Opaque equipment can conceal underlying body/clothing surfaces to prevent clipping.</summary>
        public string[] OccludedBodyRenderers => _occludedBodyRenderers;

        /// <summary>Anything worn on the head (hats, helmets, hoods) hides the head hair, which would poke through it.</summary>
        public bool HidesHair => TryGetClothMesh(ClothType.Head, out _);

        /// <summary>
        /// Whether wearing this item hides <paramref name="renderer"/>: a body renderer it names, or the head hair
        /// when <see cref="HidesHair"/>. Beards and eyebrows stay visible.
        /// </summary>
        public bool Occludes(Renderer renderer)
        {
            if (renderer == null) return false;
            if (_occludedBodyRenderers != null && Array.IndexOf(_occludedBodyRenderers, renderer.name) >= 0) return true;
            return HidesHair && HumanoidStyleApplier.IsHeadHair(renderer.transform);
        }

        /// <summary>Optional per-item materials; empty preserves the body's palette material.</summary>
        public Material[] WornMaterials => _wornMaterials;

        /// <summary>
        /// Get the mesh associated to a cloth type.
        /// </summary>
        /// <param name="clothType">type of cloth for which mesh is required</param>
        /// <returns>Mesh of the required cloth type</returns>
        public Mesh GetClothMesh(ClothType clothType)
        {
            if (_clothMeshes.TryGetValue(clothType, out Mesh mesh))
            {
                return mesh;
            }

            Log.Error(this, "Cloth Mesh not found");

            return null;
        }

        /// <summary>The mesh this item shows on <paramref name="clothType"/>, if it covers that body part.</summary>
        public bool TryGetClothMesh(ClothType clothType, out Mesh mesh)
        {
            mesh = null;
            return _clothMeshes != null && _clothMeshes.TryGetValue(clothType, out mesh) && mesh != null;
        }
    }
}