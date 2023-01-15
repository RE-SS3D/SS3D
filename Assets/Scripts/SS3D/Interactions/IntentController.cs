﻿using SS3D.Core.Behaviours;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Interactions
{
    /// <summary>
    /// Basically a copy of HumanoidBOdyPartTargetSelector.cs
    /// This manages intent and it's done to easily support other intents
    /// </summary>
    public class IntentController : Actor
    {
        private IntentType _selectedIntent;

        private Image _intentImage;

        private Sprite _spriteHelp;
        private Sprite _spriteHarm;

        private Color _colorHarm;
        private Color _colorHelp;

        private Button _intentButton;

        protected override void OnStart()
        {
            base.OnStart();

            _intentButton = GetComponent<Button>();
            _intentButton.onClick.AddListener(HandleIntentButtonPressed);
        }

        public void HandleIntentButtonPressed()
        {
            SelectIntent();
        }

        public void SelectIntent()
        {
            bool harm = _selectedIntent == IntentType.Harm;
            _selectedIntent = harm ? IntentType.Help : IntentType.Harm;
            _intentImage.sprite = harm ? _spriteHelp : _spriteHarm;
            _intentImage.color = harm ? _colorHelp : _colorHarm;
        }
    }
}
