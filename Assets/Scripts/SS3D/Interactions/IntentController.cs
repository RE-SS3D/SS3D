using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Interactions
{
    /// <summary>
    /// Basically a copy of HumanoidBodyPartTargetSelector.cs
    /// This manages intent and it's done to easily support other intents
    /// </summary>
    public class IntentController : Actor
    {
        private IIntentProvider _intentProvider;

        private Image _intentImage;

        private Sprite _spriteHelp;
        private Sprite _spriteHarm;

        private Color _colorHarm;
        private Color _colorHelp;

        private Button _intentButton;

        protected override void OnStart()
        {
            base.OnStart();

            _intentProvider = GetComponentInParent<IIntentProvider>();
            _intentButton = GetComponent<Button>();
            _intentButton.onClick.AddListener(HandleIntentButtonPressed);
            RefreshIntentVisual();
        }

        public void HandleIntentButtonPressed()
        {
            SelectIntent();
        }

        /// <summary>
        /// Switches between Help and Harm intent
        /// </summary>
        public void SelectIntent()
        {
            IntentType current = _intentProvider?.CurrentIntent ?? IntentType.Help;
            IntentType next = current == IntentType.Harm ? IntentType.Help : IntentType.Harm;
            _intentProvider?.RequestToggleIntent();
            RefreshIntentVisual(next);
        }

        private void RefreshIntentVisual(IntentType? intentOverride = null)
        {
            if (_intentImage == null)
            {
                return;
            }

            IntentType intent = intentOverride ?? _intentProvider?.CurrentIntent ?? IntentType.Help;
            bool isHelp = intent == IntentType.Help;
            _intentImage.sprite = isHelp ? _spriteHelp : _spriteHarm;
            _intentImage.color = isHelp ? _colorHelp : _colorHarm;
        }
    }
}
