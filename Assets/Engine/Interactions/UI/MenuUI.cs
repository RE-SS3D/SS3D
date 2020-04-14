using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Engine.Interactions.UI
{
    /**
     * Used for the context menu
     */
    public class MenuUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject buttonPrefab = null;
        [SerializeField]
        private GameObject contentPanel = null;

        public Vector2 Position {
            get => gameObject.transform.GetChild(0).position;
            set => gameObject.transform.GetChild(0).position = value;
        }
        public List<Interaction> Interactions {
            get => interactions;
            set
            {
                interactions = value;
                UpdateInteractions();
            }
        }
        public Action<Interaction> onSelect;

        private void Update()
        {
            if(Input.GetButtonDown("Click") || Input.GetButtonDown("Secondary Click")) {
                // Check for self as parent of click
                bool hasSelfAsParent = false;

                var obj = EventSystem.current.currentSelectedGameObject?.transform;
                while(obj != null) {
                    if (obj == transform) {
                        hasSelfAsParent = true;
                        break;
                    }

                    obj = obj.parent;
                }

                if(!hasSelfAsParent) {
                    // Delete self
                    Destroy(gameObject);
                }
            }
        }

        private void UpdateInteractions()
        {
            for(int i = contentPanel.transform.childCount - 1; i >= 0; --i) {
                Destroy(contentPanel.transform.GetChild(i));
            }

            foreach(var interaction in interactions) {

                var button = Instantiate(buttonPrefab, contentPanel.transform);

                button.GetComponentInChildren<TextMeshProUGUI>().text = interaction.Name;
                button.GetComponent<Button>().onClick.AddListener(() => {
                    Destroy(gameObject);
                    onSelect?.Invoke(interaction);
                });
            }
        }

        private List<Interaction> interactions;
    }
}
