using UnityEngine;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Items.Functional.Generic.Crayons
{
    [CreateAssetMenu(fileName = "Painter Properties", menuName = "Painting/Painter Properties", order = 0)]
    public class PainterProperties : ScriptableObject
    {
        [SerializeField] private GameObject decalPrefab = null;
        [SerializeField] private ItemSupply itemSupply = default;
        
        public GameObject DecalPrefab => decalPrefab;

        public ItemSupply ItemSupply
        {
            get => itemSupply;
            set => itemSupply = value;
        }
    }
}