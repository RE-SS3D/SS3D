using System;
using System.Collections;
using System.Collections.Generic;
using SS3D.Core;
using SS3D.Tilemaps.Objects;
using SS3D.UI.Buttons;
using UnityEngine;

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
