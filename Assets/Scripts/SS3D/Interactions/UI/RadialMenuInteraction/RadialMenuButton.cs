using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Interactions.UI.RadialMenuInteraction
{
    public class RadialMenuButton : Button
    {
        public string ObjectName;
        public string Interaction;
        [HideInInspector] public float Angle;

        public RadialInteractionMenuView Menu;

        // Sets menu's selected petal and the interaction name
        private void PetalSelect()
        {
            Menu.selectedPetal = transform.GetComponent<RectTransform>();
            Menu.InteractionName.text = Interaction;
            Menu.ObjectName.text = ObjectName;
        }

        private void Update()
        {
            if (Menu == null)
            {
                return;
            }

            if (!(Menu.MouseAngle >= Angle - Menu.ButtonAngle) || !(Menu.MouseAngle < Angle + Menu.ButtonAngle))
            {
                return;
            }

            if (Menu.MouseDistance > Menu.ButtonMaxDistance)
            {
                PetalSelect();
            }
        }
    }
}
