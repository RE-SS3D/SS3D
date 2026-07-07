using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Examine
{
    public class ExamineImageDetailedView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image _image;
        [SerializeField] private TMP_Text _captionText;

        public RectTransform Panel => _panel != null ? _panel : _panel = transform as RectTransform;

        public void SetContent(Sprite image, string caption, Vector2 imageSize)
        {
            bool hasImage = image != null;
            bool hasCaption = !string.IsNullOrEmpty(caption);

            _image.gameObject.SetActive(hasImage);
            _captionText.gameObject.SetActive(hasCaption);

            if (hasImage)
            {
                _image.sprite = image;
                _image.preserveAspect = true;

                if (imageSize != Vector2.zero)
                {
                    _image.rectTransform.sizeDelta = imageSize;
                }
            }

            if (hasCaption)
            {
                _captionText.text = caption;
            }

            if (imageSize != Vector2.zero)
            {
                Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, imageSize.x);
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
