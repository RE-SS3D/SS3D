using SS3D.Core;
using UnityEngine;

namespace SS3D.Systems.UI.Screens
{
    /// <summary>
    /// Controls a screen that can be activated and deactivated globally using events
    /// </summary>
    public class GameScreen : SpessBehaviour
    {
        [SerializeField] private ScreenType _screenType;

        public ScreenType ScreenType => _screenType;

        protected override void OnStart()
        {
            base.OnStart();

            
        }
    }
}