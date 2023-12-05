using SS3D.Core;
using SS3D.Systems.Inputs;
using TMPro;
using UnityEngine.EventSystems;

namespace SS3D.Systems.Tile.TileMapCreator
{
    public class ControlsOffInputField : TMP_InputField
    {
        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            Subsystems.Get<InputSystem>().ToggleAllActions(false);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            Subsystems.Get<InputSystem>().ToggleAllActions(true);
        }
    }
}
