using Coimbra;
using DynamicPanels;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Management;
using SS3D.Systems.Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// the tilemap menu is an in-game editor for placing and deleting items/objects in a tilemap, as well as loading
    /// and saving tilemaps.
    /// This scripts orchestrate a bunch of other scripts related to making the menu work.
    /// </summary>
    public class TileMapMenu : NetworkSubSystem, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// Enum to switch between tabs.
        /// </summary>
        private enum TileMapMenuTab
        {
            Save = 0,
            Load = 1,
            Build = 2,
        }

        /// <summary>
        /// Is the tilemap menu enabled
        /// </summary>
        private bool _enabled;

        private Controls.TileCreatorActions _controls;

        private InputSubSystem _inputSubSystem;

        private PanelTab _tab;

        [SerializeField]
        private GameObject _menuRoot;

        /// <summary>
        /// manager of holograms tile objects.
        /// </summary>
        [SerializeField]
        private ConstructionHologramManager _hologramManager;

        /// <summary>
        /// Tab for handling saving maps.
        /// </summary>
        [SerializeField]
        private TileMapSaveTab _tileMapSaveTab;

        /// <summary>
        /// Tab for handling loading maps.
        /// </summary>
        [SerializeField]
        private TileMapLoadTab _tileMapLoadTab;

        /// <summary>
        /// Tab for handling building the tilemap.
        /// </summary>
        [SerializeField]
        private TileMapBuildTab _tileMapBuildTab;

        /// <summary>
        /// Current selected tab in the menu.
        /// </summary>
        private TileMapMenuTab _currentTab;

        /// <summary>
        /// Is the mouse over the menu UI
        /// </summary>
        public bool MouseOverUI { get; private set; }

        /// <summary>
        /// Are we deleting objects from the tilemap
        /// </summary>
        public bool IsDeleting => _tileMapBuildTab.IsDeleting;

        /// <summary>
        /// Called when pointer enter the UI of the menu.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            MouseOverUI = true;
            _inputSubSystem.ToggleBinding("<Mouse>/scroll/y", false);
            if (!_hologramManager.IsDragging)
            {
                _inputSubSystem.ToggleAction(_controls.Place, false);
            }
        }

        /// <summary>
        /// Called when pointer exit the UI of the menu.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            MouseOverUI = false;
            _inputSubSystem.ToggleBinding("<Mouse>/scroll/y", true);
            if (!_hologramManager.IsDragging)
            {
                _inputSubSystem.ToggleAction(_controls.Place, true);
            }
        }

        /// <summary>
        /// Called when clicking on the build button of the menu.
        /// </summary>
        public void HandleBuildButton()
        {
            ClearCurrentTab();
            _currentTab = TileMapMenuTab.Build;
            _tileMapBuildTab.Display();
        }

        /// <summary>
        /// Method called when the load button is clicked.
        /// </summary>
        public void HandleLoadButton()
        {
            ClearCurrentTab();
            _currentTab = TileMapMenuTab.Load;
            _tileMapLoadTab.Display();
        }

        /// <summary>
        /// Method called when the save tab is clicked.
        /// </summary>
        public void HandleSaveTabButton()
        {
            ClearCurrentTab();
            _currentTab = TileMapMenuTab.Save;
            _tileMapSaveTab.Display();
        }

        /// <summary>
        /// Called when the input field to search for tile objects is selected.
        /// </summary>
        public void HandleInputFieldSelect()
        {
            _inputSubSystem.ToggleAllActions(false);
        }

        /// <summary>
        /// Called when the input field to search for tile objects is selected.
        /// </summary>
        public void HandleInputFieldDeselect()
        {
            _inputSubSystem.ToggleAllActions(true);
        }

        protected override void OnStart()
        {
            base.OnStart();
            _tab = PanelUtils.GetAssociatedTab(GetComponent<RectTransform>());
            ShowUI(false);
            _inputSubSystem = Subsystems.Get<InputSubSystem>();
            _controls = _inputSubSystem.Inputs.TileCreator;
            _inputSubSystem.ToggleAction(_controls.ToggleMenu, true);
            _controls.ToggleMenu.performed += HandleToggleMenu;
        }

        /// <summary>
        /// Method called when the control to open the tilemap menu is performed.
        /// </summary>
        private void HandleToggleMenu(InputAction.CallbackContext context)
        {
            if (_enabled)
            {
                _inputSubSystem.ToggleActionMap(_controls, false, new InputAction[] { _controls.ToggleMenu });
                _inputSubSystem.ToggleCollisions(_controls, true);
            }
            else
            {
                _inputSubSystem.ToggleActionMap(_controls, true, new InputAction[] { _controls.ToggleMenu });
                _inputSubSystem.ToggleCollisions(_controls, false);
            }

            _enabled = !_enabled;
            ShowUI(_enabled);

            _currentTab = TileMapMenuTab.Build;
            ClearAllTab();
            _tileMapBuildTab.Display();
        }

        /// <summary>
        /// Hide or show the tilemap menu.
        /// </summary>
        private void ShowUI(bool isShow)
        {
            if (!isShow)
            {
                // Detach the tab from a movable panel. It's necessary to prevent disabling all tabs in the panel.
                _tab.Detach();
                _hologramManager.DestroyHolograms();
            }

            _tab.Panel.gameObject.SetActive(isShow);
            _menuRoot.SetActive(isShow);
        }

        /// <summary>
        /// Clear the content of the current selected tab.
        /// </summary>
        [ServerOrClient]
        private void ClearCurrentTab()
        {
            switch (_currentTab)
            {
                case TileMapMenuTab.Build:
                {
                    _tileMapBuildTab.Clear();
                    break;
                }

                case TileMapMenuTab.Load:
                {
                    _tileMapLoadTab.Clear();
                    break;
                }

                case TileMapMenuTab.Save:
                {
                    _tileMapSaveTab.Clear();
                    break;
                }
            }
        }

        /// <summary>
        /// Clear the content of all tabs.
        /// </summary>
        private void ClearAllTab()
        {
            _tileMapBuildTab.Clear();
            _tileMapLoadTab.Clear();
            _tileMapSaveTab.Clear();
        }
    }
}
