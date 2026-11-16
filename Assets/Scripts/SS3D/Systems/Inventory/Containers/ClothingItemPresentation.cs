using UnityEngine;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// World/hand visual form for clothing Items: folded mesh child on, worn-shaped child off.
    /// Worn look is <see cref="ClothesDisplayer"/> + <see cref="Cloth"/> body mesh while the Item
    /// is hidden in a cloth slot — same NetworkObject, no dual-prefab swap.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Cloth))]
    public class ClothingItemPresentation : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Folded / piled mesh shown in the world and when held.")]
        private GameObject _worldRoot;

        [SerializeField]
        [Tooltip("Body-shaped mesh used only as a Cloth source historically; keep inactive for world/hand.")]
        private GameObject _wornShapedRoot;

        private Sprite _wornHudSprite;

        private void Awake()
        {
            ApplyWorldForm();
        }

        /// <summary>
        /// Activate the folded world mesh and deactivate the worn-shaped child.
        /// Safe to call when becoming visible again after a hide container.
        /// </summary>
        public void ApplyWorldForm()
        {
            if (_worldRoot != null)
            {
                _worldRoot.SetActive(true);
            }

            if (_wornShapedRoot != null)
            {
                _wornShapedRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Activate the body-shaped child for HUD icon generation (worn equipment doll).
        /// Does not affect live world/hand presentation — call only on preview clones.
        /// </summary>
        public void ApplyWornShapedForm()
        {
            if (_worldRoot != null)
            {
                _worldRoot.SetActive(false);
            }

            if (_wornShapedRoot != null)
            {
                _wornShapedRoot.SetActive(true);
            }
        }

        /// <summary>Only call on disposable icon clones: retain exactly one visual shape.</summary>
        public void PrepareIconPreview(bool worn)
        {
            GameObject excluded = worn ? _worldRoot : _wornShapedRoot;
            if (excluded != null)
            {
                DestroyImmediate(excluded);
            }
            if (worn) ApplyWornShapedForm();
            else ApplyWorldForm();
        }

        /// <summary>
        /// Worn slots hide via <see cref="Item.SetVisibility"/>; body mesh is
        /// <see cref="ClothesDisplayer"/>. Child activity can stay on world form.
        /// </summary>
        public void ApplyHiddenForm()
        {
            // Intentionally empty — HideItems disables renderers; no child swap required.
        }

        /// <summary>
        /// Cached HUD sprite of the worn-shaped mesh for equipment-doll slots.
        /// Hands / world keep using <see cref="Item.ItemSprite"/> (folded).
        /// </summary>
        public Sprite GetWornHudSprite(Item item)
        {
            if (_wornHudSprite != null)
            {
                return _wornHudSprite;
            }

            if (item == null)
            {
                return null;
            }

            _wornHudSprite = item.GenerateIcon(useWornShapedForm: true);
            return _wornHudSprite;
        }
    }
}
