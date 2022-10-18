using System;
using System.Collections.Generic;
using Coimbra;
using SS3D.Core.Behaviours;
using SS3D.Engine.Interactions;
using SS3D.Interactions.UI.RadialMenuInteraction;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Interactions.UI
{
    public class RadialInteractionMenuView : SpessBehaviour
    {
        public Action<IInteraction> OnSelect;

        [Range(1.5f, 3f)]
        public float MenuScale = 1;

        public Animator Animator;
        
        public RectTransform Indicator;
        public RectTransform selectedPetal;

        public PetalsManager PetalsManager;

        private PetalFolder _folder;
        private Canvas _parentCanvas;

        public TextMeshProUGUI ObjectName;
        public TextMeshProUGUI InteractionName;
		public TextMeshProUGUI InteractionNameAltPosition;

        private Camera _camera;
        
        [HideInInspector] public float MouseAngle;
        [HideInInspector] public float MouseDistance;

        public float ButtonAngle = 22.5f;
        public float ButtonMaxDistance = 40f;
        public Sprite MissingIcon;

        private Image _indicatorImage;
        private Color _indicatorBaseColor;
        private Color _indicatorOffColor;

        private GameObject _selectedObject = null;
        private List<IInteraction> _interactions;
        private InteractionEvent _interactionEvent;

        private static readonly int AnimationVisible = Animator.StringToHash("Visible");
        private static readonly int AnimationReturnButtonVisible = Animator.StringToHash("ReturnButtonVisible");

        public List<IInteraction> Interactions
        {
            get => _interactions;
            set
            {
                _interactions = value;
                UpdateInteractions();
            }
        }

        public InteractionEvent Event
        {
            get => _interactionEvent;
            set
            {
                _interactionEvent = value;
                UpdateInteractions();
            }
        }

        private void Start()
        {
            //camera = CameraManager.singleton.playerCamera;
            
            PetalsManager = GetComponent<PetalsManager>();
            PetalsManager.ContextMenu = this;
            _parentCanvas = GetComponentInParent<Canvas>();

            _indicatorImage = Indicator.GetComponent<Image>();
            _indicatorOffColor = _indicatorImage.color;
            _indicatorBaseColor = new Color(_indicatorOffColor.r, _indicatorOffColor.g, _indicatorOffColor.b, .69f);
        }
        private void Update()
        {
            //parent.GetComponent<CanvasScaler>().scaleFactor = scale;
            Parent.localScale = new Vector2(MenuScale, MenuScale);

            float currentAngle = Indicator.eulerAngles.z;

            if (selectedPetal != null)
            {
                Indicator.eulerAngles = new Vector3(0, 0, Mathf.LerpAngle(currentAngle, selectedPetal.eulerAngles.z, Time.deltaTime * 15));
            }

            _selectedObject = EventSystem.current.currentSelectedGameObject;

            if (Input.GetButtonDown("Secondary Click"))
            {
                ProcessSecondaryClickDown();
            }

            // Deletes the object and calls the interaction if it exists when mouse 1 is up
            if (Input.GetMouseButtonUp(1))
            {
                ProcessSecondaryClickUp();
            }

            Vector3 mouse = Input.mousePosition;
            Vector3 dir = (mouse - Indicator.position).normalized;

            float atan = Mathf.Atan2(dir.y, dir.x);
            MouseAngle = (atan > 0 ? atan : (2 * Mathf.PI + atan)) * 360 / (2 * Mathf.PI);
            MouseDistance = (mouse - Indicator.position).magnitude;
            if (MouseDistance < ButtonMaxDistance)
            {
                selectedPetal = null;
                InteractionName.text = null;
                ObjectName.text = null;
            }

            _indicatorImage.color = (selectedPetal == null) ? _indicatorOffColor : _indicatorBaseColor;
        }

        private void ProcessSecondaryClickUp()
        {
            if (selectedPetal != null)
            {
                selectedPetal.GetComponentInChildren<Button>().onClick.Invoke();
            }

            gameObject.Destroy();
        }

        private void ProcessSecondaryClickDown()
        {
            bool hasSelfAsParent = false;

            // Check for self as parent of click
            while (_selectedObject != null)
            {
                if (_selectedObject == GameObjectCache)
                {
                    hasSelfAsParent = true;
                    break;
                }

                _selectedObject = _selectedObject.transform.parent.gameObject;
            }

            if (!hasSelfAsParent)
            {
                // Delete self
                gameObject.Destroy();
            }
        }

        private void UpdateInteractions()
        {
            if (Event == null || _interactions == null || _interactions.Count < 1)
            {
                return;
            }

            _folder = new PetalFolder(GetPetalPrefab());
            Appear(new Vector2(Input.mousePosition.x, Input.mousePosition.y), 1, _folder);

            foreach (IInteraction interaction in Interactions)
            {
                Sprite icon = interaction.GetIcon(Event) ? interaction.GetIcon(Event) : MissingIcon;
                string interactionName = interaction.GetName(Event);
                string objectName = Event.Target.ToString();

                Petal newPetal = PetalsManager.AddPetalToFolder(icon, interactionName);
                RadialMenuButton petalButton = newPetal.GetComponentInChildren<RadialMenuButton>();

                petalButton.Menu = GetComponent<RadialInteractionMenuView>();
                petalButton.Interaction = interactionName;
                petalButton.ObjectName = objectName;

                petalButton.onClick.AddListener(() =>
                {
                    Disappear();
                    OnSelect?.Invoke(interaction);
                });
            }
        }

        public bool Appear(Vector2 screenPos, float scale, PetalFolder spawnFolder)
        {
            if (Animator.GetBool(AnimationVisible))
            {
                return (false);
            }

            Position = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 1));
            _folder = spawnFolder;
            if (spawnFolder != null)
            {
                PetalsManager.SetFolder(spawnFolder, true);
            }
			
			// Set location of text display to above the RadialInteractionMenuIU if in bottom half of screen
			if (screenPos.y * 2 < Screen.height){
				InteractionName = InteractionNameAltPosition;
			}
			
            Animator.SetBool(AnimationVisible, true);
			
            return (true);
        }

        public bool Disappear()
        {
            Animator.SetBool(AnimationVisible, false);
            Animator.SetBool(AnimationReturnButtonVisible, false);
            return (true);
        }

        public void Return()
        {
            PetalsManager.Return();
        }

        public PetalsManager GetPetalsManager()
        {
            return (PetalsManager);
        }

        public GameObject GetPetalPrefab()
        {
            return (PetalsManager.PetalPrefab);
        }
    }

}
