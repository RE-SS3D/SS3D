using UnityEngine;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Items.Functional.Generic.PowerCells
{
    [CreateAssetMenu(fileName = "Power Cell Properties", menuName = "Power/Power Cell Properties", order = 0)]
    public class PowerCellProperties : ScriptableObject
    {
        [SerializeField] private PowerSupply powerSupply = default;

        public PowerSupply PowerSupply
        {
            get => powerSupply;
            set => powerSupply = value;
        }
    }
}