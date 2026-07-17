using SS3D.Core;
using SS3D.Systems.Inputs;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// TMP input field, with the added functionnality of toggling controls on and off when the field is selected or deselected.
    /// </summary>
    public class ControlsOffInputField : TMP_InputField
    {
        private readonly InputTextEntryScope _textEntry = new();

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            _textEntry.Enter();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            _textEntry.Exit();
        }
    }
}
