using DynamicPanels;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// the tilemap menu is an in-game editor for placing and deleting items/objects in a tilemap, as well as loading
    /// and saving tilemaps.
    /// This scripts orchestrate a bunch of other scripts related to making the menu work. 
    /// </summary>
    public class TileMapMenu : NetworkSystem, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private GameObject _menuRoot;

        public GameObject MenuRoot => _menuRoot;

        private bool _mouseOverUI = false;

        /// <summary>
        /// Is the mouse over the menu UI ?
        /// </summary>
        public bool MouseOverUI => _mouseOverUI;

        /// <summary>
        /// Is the tilemap menu enabled ?
        /// </summary>
        private bool _enabled = false;


        private bool _isDeleting;

        /// <summary>
        /// Are we deleting objects from the tilemap ?
        /// </summary>
        public bool IsDeleting => _isDeleting;


        private Controls.TileCreatorActions _controls;
        private InputSystem _inputSystem;
        private PanelTab _tab;

        [SerializeField]
        private ConstructionHologramManager _hologramManager;

        [SerializeField]
        private TileMapMenuBuildingTabs _tileMapMenuBuildingTabs ;


        [SerializeField]
        private TileMapMenuSaveAndLoadTabs _tileMapMenuSaveAndLoadTabs;

        /// <summary>
        /// Called when pointer enter the UI of the menu.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _mouseOverUI = true;
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", false);
            if (!_hologramManager.IsDragging)
            {
                _inputSystem.ToggleAction(_controls.Place, false);
            }
        }

        /// <summary>
        /// Called when pointer exit the UI of the menu.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _mouseOverUI = false;
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
            if (!_hologramManager.IsDragging)
            {
                _inputSystem.ToggleAction(_controls.Place, true);
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            _tab = PanelUtils.GetAssociatedTab(GetComponent<RectTransform>());
            ShowUI(false);
            _inputSystem = Subsystems.Get<InputSystem>();
            _controls = _inputSystem.Inputs.TileCreator;
            _inputSystem.ToggleAction(_controls.ToggleMenu, true);
            _controls.ToggleMenu.performed += HandleToggleMenu;
        }
       

        /// <summary>
        /// Method called when the control to open the tilemap menu is performed.
        /// </summary>
        private void HandleToggleMenu(InputAction.CallbackContext context)
        {
            if (_enabled)
            {
                _inputSystem.ToggleActionMap(_controls, false, new[] { _controls.ToggleMenu });
                _inputSystem.ToggleCollisions(_controls, true);
            }
            else
            {
                _inputSystem.ToggleActionMap(_controls, true, new[] { _controls.ToggleMenu });
                _inputSystem.ToggleCollisions(_controls, false);
            }
            _enabled = !_enabled;
            ShowUI(_enabled);
            
            _tileMapMenuBuildingTabs.Setup();
        }
       
        /// <summary>
        /// TODO : document, what does this do ?
        /// </summary>
        private void ShowUI(bool show)
        {
            if (!show)
            {
                _tab.Detach();
                _hologramManager.DestroyHolograms();
            }
            _tab.Panel.gameObject.SetActive(show);
            _menuRoot.SetActive(show);
        }

        /// <summary>
        /// Called when clicking on the delete button of the menu.
        /// </summary>
        private void HandleDeleteButton()
        {
            _isDeleting = true;
        }

        /// <summary>
        /// Called when clicking on the build button of the menu.
        /// </summary>
        private void HandleBuildButton()
        {
            _isDeleting = false;
        }
    }
}