using SS3D.Systems.GameModes.Objectives;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.GameModes.UI
{
    public class ObjectivesUI : MonoBehaviour
    {
        public GameObject ObjectivePrefab;
        public List<TextMeshProUGUI> Objectives;
        public List<GamemodeObjective> Objectives2;
        public Dictionary<TextMeshProUGUI, GamemodeObjective> Objectives3;
        
        public void AddObjective(string Title, GamemodeObjective gamemodeObjective)
        {
            GameObject _gameObject;
            _gameObject = Instantiate(ObjectivePrefab);
            _gameObject.transform.parent = this.transform;
            _gameObject.GetComponent<RectTransform>().anchoredPosition
                = new Vector2(-110f, -15f + (-15f * Objectives.Count));

            TextMeshProUGUI _text = _gameObject.GetComponent<TextMeshProUGUI>();
            _text.text = Title + " -";

            Objectives.Add(_text);
            Objectives2.Add(gamemodeObjective);
        }
    }
}