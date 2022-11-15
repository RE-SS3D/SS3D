using SS3D.Core;
using SS3D.Tilemaps.Objects;
using SS3D.UI.Buttons;
using UnityEngine;

namespace SS3D.Systems.Tilemaps
{
    public class TilemapCreateTestView : MonoBehaviour
    {
        [SerializeField] private LabelButton _button;

        private void Start()
        {
            _button.OnPressedUp += HandleButtonPressed;
        }

        private void HandleButtonPressed(bool state)
        {
            TileObjectSystem tileObjectSystem = GameSystems.Get<TileObjectSystem>();

            tileObjectSystem.StressTest();
        }
    }
}
