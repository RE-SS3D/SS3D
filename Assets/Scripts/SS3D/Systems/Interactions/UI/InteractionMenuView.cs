using System;
using System.Collections.Generic;
using Coimbra;
using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Interactions.UI
{
    /// <summary>
    /// The radial interaction menu
    /// </summary>
    public class InteractionMenuView : Actor
    {
        public event Action<IInteraction> OnSelect;

        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private GameObject _contentPanel;

        private List<IInteraction> _interactions;
        private InteractionEvent _interactionEvent;

        public List<IInteraction> Interactions
        {
            get => _interactions;
            set
            {
                _interactions = value;
                UpdateInteractions();
            }
        }

        private InteractionEvent Event
        {
            get => _interactionEvent;
            set
            {
                _interactionEvent = value;
                UpdateInteractions();
            }
        }

        private void Update()
        {
            if (!Input.GetButtonDown("Primary Click") && !Input.GetButtonDown("Secondary Click"))
            {
                return;
            }

            // Check for self as parent of click
            bool hasSelfAsParent = false;

            Transform selected = EventSystem.current.currentSelectedGameObject.transform;
            while (selected != null)
            {
                if (selected == transform)
                {
                    hasSelfAsParent = true;
                    break;
                }

                selected = selected.parent;
            }

            if (!hasSelfAsParent)
            {
                gameObject.Dispose(true);
            }
        }

        private void UpdateInteractions()
        {
            for (int i = _contentPanel.transform.childCount - 1; i >= 0; --i)
            {
                Destroy(_contentPanel.transform.GetChild(i));
            }

            if (Event == null || _interactions == null)
            {
                return;
            }

            foreach (IInteraction interaction in _interactions)
            {

                GameObject button = Instantiate(_buttonPrefab, _contentPanel.transform);

                button.GetComponentInChildren<TextMeshProUGUI>().text = interaction.GetName(Event);
                button.GetComponent<Button>().onClick.AddListener(() =>
                {
                    GameObject.Dispose(true);
                    OnSelect?.Invoke(interaction);
                });
            }
        }
    }
}
