using SS3D.Core.Behaviours;
using SS3D.Interactions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Intents
{
    public class IntentView : View
    {
        [SerializeField]
        private Button _intentButton;

        [SerializeField]
        private Sprite _harmIcon;

        [SerializeField]
        private Sprite _helpIcon;

        [SerializeField]
        private Image _intentIcon;

        private IntentType _intentType;

        protected void Awake()
        {
            _intentButton.onClick.AddListener(OnIntentChanged);
        }

        private void OnIntentChanged()
        {
            _intentType = _intentType == IntentType.Harm ? IntentType.Help : IntentType.Harm;
            _intentIcon.sprite = _intentType == IntentType.Harm ? _harmIcon : _helpIcon;
            _intentIcon.color = _intentType == IntentType.Harm ? Color.red : Color.green;

            IntentChanged intentChanged = new(_intentType);
            intentChanged.Invoke(this);
        }
    }
}
