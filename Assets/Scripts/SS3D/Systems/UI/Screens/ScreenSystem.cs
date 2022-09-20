using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.UI.Screens
{
    public class ScreenSystem : MonoBehaviour
    {
        private List<GameScreen> _gameScreens;

        private void Awake()
        {
            _gameScreens = FindObjectsOfType<GameScreen>().ToList();
        }
    }
}
