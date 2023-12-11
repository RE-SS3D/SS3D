using Coimbra;
using UnityEngine.UI;
namespace SS3D.Systems.Inventory.UI
{
    public class ItemGridItem : ItemDisplay
    {
        public override void OnDropAccepted()
        {
            base.OnDropAccepted();
			MakeVisible(false);
        }
    }
}