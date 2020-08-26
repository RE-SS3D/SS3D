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
        public Transform parent;
        [Range(1.5f, 3f)]
        public float menuScale = 1;

        public Animator menuAnimator;
        
        public RectTransform indicator;
        public RectTransform selectedPetal;

        public PetalsManager petalsManager;
        private PetalFolder folder;
        private Canvas parentCanvas;

        public TextMeshProUGUI objectName;
        public TextMeshProUGUI interactionName;
		public TextMeshProUGUI interactionNameAltPosition;
		

        [HideInInspector]
        public float mouseAngle;
        public float buttonAngle = 22.5f;
        public float buttonMaxDistance = 40f;
        [HideInInspector]
        public float mouseDistance;
        public Sprite missingIcon;

        private Image indicatorImage;
        private Color indicatorBaseColor;
        private Color indicatorOffColor;

        // Current selected object
        GameObject obj = null;

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

            indicatorImage = indicator.GetComponent<Image>();
            indicatorOffColor = indicatorImage.color;
            indicatorBaseColor = new Color(indicatorOffColor.r, indicatorOffColor.g, indicatorOffColor.b, .69f);
        }
        private void Update()
        {
            //parent.GetComponent<CanvasScaler>().scaleFactor = scale;
            parent.localScale = new Vector2(menuScale, menuScale);


            float currentAngle = indicator.eulerAngles.z;

            if (selectedPetal != null)
                indicator.eulerAngles = new Vector3(0, 0, Mathf.LerpAngle(currentAngle, selectedPetal.eulerAngles.z, Time.deltaTime * 15));
            
            bool hasSelfAsParent = false;
            obj = EventSystem.current?.currentSelectedGameObject;

            if (Input.GetButtonDown("Secondary Click"))
            {
                // Check for self as parent of click
                while (obj != null)
                {
                    if (obj == transform)
                    {
                        hasSelfAsParent = true;
                        break;
                    }

                    obj = obj.transform.parent.gameObject;
                }

                if (!hasSelfAsParent)
                {
                    // Delete self
                    Destroy(gameObject);
                }
            }
            // Deletes the object and calls the interaction if it exists when mouse 1 is up
            if (Input.GetMouseButtonUp(1))
            {
                if (selectedPetal != null)
                    selectedPetal.GetComponentInChildren<Button>().onClick.Invoke();
                Destroy(gameObject);
            }
            Vector3 mouse = Input.mousePosition;
            Vector3 dir = (mouse - indicator.position).normalized;

            float atan = Mathf.Atan2(dir.y, dir.x);
            mouseAngle = (atan > 0 ? atan : (2 * Mathf.PI + atan)) * 360 / (2 * Mathf.PI);
            mouseDistance = (mouse - indicator.position).magnitude;
            if (mouseDistance < buttonMaxDistance)
            {
                selectedPetal = null;
                interactionName.text = null;
                objectName.text = null;
            }

            indicatorImage.color = (selectedPetal == null) ? indicatorOffColor : indicatorBaseColor;
        }

        private void UpdateInteractions()
        {
            if (Event == null || interactions == null || interactions.Count < 1)
            {
                return;
            }

            folder = new PetalFolder(GetPetalPrefab());
            Appear(new Vector2(Input.mousePosition.x, Input.mousePosition.y), 1, folder);
            foreach (IInteraction interaction in Interactions)
            {
                Sprite icon = interaction.GetIcon(Event) ? interaction.GetIcon(Event) : missingIcon;
                string name = interaction.GetName(Event);
                string objectName = Event.Target.ToString();

                //Debug.LogError(name);

                Petal newPetal = petalsManager.AddPetalToFolder(icon, name);
                RadialMenuButton petalButton = newPetal.GetComponentInChildren<RadialMenuButton>();

                petalButton.menu = GetComponent<RadialInteractionMenuUI>();
                petalButton.interaction = name;
                petalButton.objectName = objectName;

                petalButton.onClick.AddListener(() =>
                {
                    Destroy(gameObject);
                    onSelect?.Invoke(interaction);
                });
            }
        }

        public bool Appear(Vector2 screenPos, float scale, PetalFolder spawnFolder)
        {
            //Debug.Log("appear called");
            //this.transform.position
            if (menuAnimator.GetBool("Visible") == true)
                return (false);
            this.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 1));
            folder = spawnFolder;
            if (spawnFolder != null)
            {
                petalsManager.SetFolder(spawnFolder, true);
            }
			
			// Set location of text display to above the RadialInteractionMenuIU if in bottom half of screen
			if (screenPos.y * 2 < Screen.height){
				interactionName = interactionNameAltPosition;
			}
			
            menuAnimator.SetBool("Visible", true);
			
            return (true);
        }

        public bool Disappear()
        {
            //Debug.Log("disappear called");
            menuAnimator.SetBool("Visible", false);
            menuAnimator.SetBool("ReturnButtonVisible", false);
            return (true);
        }

        public void Return()
        {
            //Debug.Log("return called");
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
