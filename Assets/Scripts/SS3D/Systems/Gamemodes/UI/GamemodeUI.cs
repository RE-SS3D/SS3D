using System.Collections;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.GameModes.UI
{
    public class GamemodeUI : MonoBehaviour
    {
        public TextMeshProUGUI MainText;
        public ObjectivesUI ObjectivesUI;

        public void SetMainText(string _text, Color _color)
        {
            MainText.text = _text;
            MainText.color = _color;
        }

        public void FadeOutMainText()
        {
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            yield return new WaitForSeconds(2f);
            for (float _alpha = 1f; _alpha >= 0f; _alpha -= 0.1f)
            {
                MainText.color = new Color(MainText.color.r, MainText.color.g, MainText.color.b, _alpha);
                yield return null;
            }
            MainText.color = new Color(MainText.color.r, MainText.color.g, MainText.color.b, 0f);
        }
    }
}