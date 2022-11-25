using System.Collections.Generic;
using SS3D.Systems.Gamemodes;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.GameModes.UI
{
    public class ObjectivesView : MonoBehaviour
    {
        [SerializeField] private GameObject ObjectivePrefab;
        private Dictionary<TextMeshProUGUI, GamemodeObjective> Objectives =
            new Dictionary<TextMeshProUGUI, GamemodeObjective>();
        
        public void AddObjective(string Title, GamemodeObjective gamemodeObjective)
        {
            foreach (KeyValuePair<TextMeshProUGUI, GamemodeObjective> entry in Objectives)
            {
                if (entry.Value == gamemodeObjective)
                    return;
            }

            TextMeshProUGUI _text = CreateObjective(Title);
            Objectives.Add(_text, gamemodeObjective);
        }

        private TextMeshProUGUI CreateObjective(string title)
        {
            GameObject _gameObject;
            _gameObject = Instantiate(ObjectivePrefab);
            _gameObject.transform.parent = this.transform;
            _gameObject.GetComponent<RectTransform>().anchoredPosition
                = new Vector2(-110f, -15f + (-15f * Objectives.Count));

            TextMeshProUGUI _text = _gameObject.GetComponent<TextMeshProUGUI>();
            _text.text = title + " -";

            return _text;
        }

        public void UpdateObjective(GamemodeObjective gamemodeObjective)
        {
            foreach (KeyValuePair<TextMeshProUGUI, GamemodeObjective> entry in Objectives) {
                if (entry.Value == gamemodeObjective)
                {
                    switch (gamemodeObjective.Status)
                    {
                        case ObjectiveStatus.Success:
                            entry.Key.color = Color.green;
                            break;
                        case ObjectiveStatus.Failed:
                            entry.Key.color = Color.red;
                            break;
                        default:
                            entry.Key.color = Color.white;
                            break;
                    }
                }
            }
        }
    }
}