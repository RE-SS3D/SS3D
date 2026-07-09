using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Examine
{
    public class ExamineDetailedView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;

        public RectTransform Panel => _panel != null ? _panel : _panel = transform as RectTransform;

        public void SetContent(string name, string description)
        {
            bool hasName = !string.IsNullOrEmpty(name);
            bool hasDescription = !string.IsNullOrEmpty(description);

            _nameText.gameObject.SetActive(hasName);
            _descriptionText.gameObject.SetActive(hasDescription);

            if (hasName)
            {
                _nameText.text = name;
            }

            if (hasDescription)
            {
                _descriptionText.text = description;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(Panel);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
