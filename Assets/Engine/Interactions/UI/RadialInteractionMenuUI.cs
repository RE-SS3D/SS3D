using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Engine.Interactions.UI
{
    public class RadialInteractionMenuUI : MonoBehaviour
    {
        public Animator menuAnimator;
        public PetalsManager petalsManager;
        private Canvas parentCanvas;
        public Button close;

        public Sprite missingIcon;

        private static RadialInteractionMenuUI contextMenuManagerInstance = null;

        public static RadialInteractionMenuUI ContextMenuManagerSingleton()
        {
            return (RadialInteractionMenuUI.contextMenuManagerInstance);
        }

        public Vector2 Position
        {
            get => gameObject.transform.GetChild(0).position;
            set => gameObject.transform.GetChild(0).position = value;
        }
        public List<IInteraction> Interactions
        {
            get => interactions;
            set
            {
                interactions = value;
                UpdateInteractions();
            }
        }

        public InteractionEvent Event
        {
            get => interactionEvent;
            set
            {
                interactionEvent = value;
                UpdateInteractions();
            }
        }

        public Action<IInteraction> onSelect;

        private void Start()
        {
            petalsManager = GetComponent<PetalsManager>();
            petalsManager.contextMenu = this;
            if (contextMenuManagerInstance != null)
            {
                Destroy(this.gameObject);
                return;
            }
            else
                contextMenuManagerInstance = this;
            parentCanvas = GetComponentInParent<Canvas>();
        }
        private void Update()
        {
            if (Input.GetButtonDown("Click") || Input.GetButtonDown("Secondary Click"))
            {
                // Check for self as parent of click
                bool hasSelfAsParent = false;

                var obj = EventSystem.current?.currentSelectedGameObject?.transform;
                while (obj != null)
                {
                    if (obj == transform)
                    {
                        hasSelfAsParent = true;
                        break;
                    }

                    obj = obj.parent;
                }

                if (!hasSelfAsParent)
                {
                    // Delete self
                    Destroy(gameObject);
                }
            }
        }

        private void UpdateInteractions()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (Event == null || interactions == null)
                {
                    return;
                }

                PetalFolder folder = new PetalFolder(GetPetalPrefab());
                Appear(new Vector2(Input.mousePosition.x, Input.mousePosition.y), 1, folder);
                foreach (IInteraction interaction in Interactions)
                {
                    Sprite icon = interaction.GetIcon(Event) ? interaction.GetIcon(Event) : missingIcon;
                    string name = interaction.GetName(Event);

                    Petal newPetal = petalsManager.AddPetalToFolder(icon, name);

                    close.onClick.AddListener(() =>
                    {
                        Destroy(gameObject);
                    });
                        newPetal.GetComponentInChildren<Button>().onClick.AddListener(() =>
                    {
                        Destroy(gameObject);
                        onSelect?.Invoke(interaction);
                    });
                }
                
            }
            else if (Input.GetMouseButtonUp(1))
            {
                Disappear();
            }
            int val = Input.GetAxis("Camera Zoom") > 0 ? -1 : Input.GetAxis("Camera Zoom") < 0 ? 1 : 0;
            GetPetalsManager().MoveIndex(val);
        }
      
        public bool Appear(Vector2 screenPos, float scale, PetalFolder spawnFolder)
        {
            Debug.Log("appear called");
            //this.transform.position
            if (menuAnimator.GetBool("Visible") == true)
                return (false);
            this.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 1));
            if (spawnFolder != null)
            {
                petalsManager.SetFolder(spawnFolder, true);
            }
            menuAnimator.SetBool("Visible", true);
            return (true);
        }

        public bool Disappear()
        {
            Debug.Log("disappear called");
            menuAnimator.SetBool("Visible", false);
            menuAnimator.SetBool("ReturnButtonVisible", false);
            return (true);
        }

        public void Return()
        {
            Debug.Log("return called");
            petalsManager.Return();
        }

        public PetalsManager GetPetalsManager()
        {
            return (petalsManager);
        }

        public GameObject GetPetalPrefab()
        {
            return (petalsManager.petalPrefab);
        }
        private List<IInteraction> interactions;
        private InteractionEvent interactionEvent;
    }

}
