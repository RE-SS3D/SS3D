using SS3D.Systems.GameModes.Objectives;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.GameModes.UI
{
    public class ObjectivesUI : MonoBehaviour
    {
        public GameObject ObjectivePrefab;
        public Dictionary<TextMeshProUGUI, GamemodeObjective> Objectives =
            new Dictionary<TextMeshProUGUI, GamemodeObjective>();
        
        public void AddObjective(string Title, GamemodeObjective gamemodeObjective)
        {
            foreach (KeyValuePair<TextMeshProUGUI, GamemodeObjective> entry in Objectives)
            {
                if (entry.Value == gamemodeObjective)
                    return;
            }

                GameObject _gameObject;
            _gameObject = Instantiate(ObjectivePrefab);
            _gameObject.transform.parent = this.transform;
            _gameObject.GetComponent<RectTransform>().anchoredPosition
                = new Vector2(-110f, -15f + (-15f * Objectives.Count));

            TextMeshProUGUI _text = _gameObject.GetComponent<TextMeshProUGUI>();
            _text.text = Title + " -";

            Objectives.Add(_text, gamemodeObjective);
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