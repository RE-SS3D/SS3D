using System.Collections;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.GameModes.UI
{
    public class GamemodeUI : MonoBehaviour
    {
        public TextMeshProUGUI MainText;
        public GamemodeObjectivesView _gamemodeObjectivesView;

        public void SetMainText(string _text, Color _color)
        {
            MainText.text = _text;
            MainText.color = _color;
        }

        public void FadeOutMainText(float time)
        {
            StartCoroutine(FadeOut(time));
        }

        IEnumerator FadeOut(float time)
        {
            yield return new WaitForSeconds(time);
            for (float _alpha = 1f; _alpha >= 0f; _alpha -= 0.1f)
            {
                MainText.alpha = _alpha;
                yield return null;
            }
            MainText.alpha = 0f;
        }
    }
}