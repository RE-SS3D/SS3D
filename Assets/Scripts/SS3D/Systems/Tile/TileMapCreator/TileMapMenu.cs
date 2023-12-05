﻿using Coimbra;
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
        /// <summary>
        /// Is the mouse over the menu UI
        /// </summary>
        public bool MouseOverUI => _mouseOverUI;
        private bool _mouseOverUI = false;
        /// <summary>
        /// Is the tilemap menu enabled
        /// </summary>
        private bool _enabled = false;
        /// <summary>
        /// Are we deleting objects from the tilemap
        /// </summary>
        public bool IsDeleting => _tileMapBuildTab.IsDeleting;

        private Controls.TileCreatorActions _controls;

        private InputSystem _inputSystem;

        private PanelTab _tab;
        
        [SerializeField]
        private GameObject _menuRoot;

        [SerializeField]
        private ConstructionHologramManager _hologramManager;

        [SerializeField]
        private TileMapSaveTab _tileMapSaveTab;

        [SerializeField]
        private TileMapLoadTab _tileMapLoadTab;

        [SerializeField]
        private TileMapBuildTab _tileMapBuildTab;

        private enum TileMapMenuTab
        {
            Save,
            Load,
            Build,
        }

        private TileMapMenuTab _currentTab;


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
            _inputSystem.ToggleAllActions(false);
        }

        /// <summary>
        /// Called when the input field to search for tile objects is selected.
        /// </summary>
        public void HandleInputFieldDeselect()
        {
            _inputSystem.ToggleAllActions(true);
        }

        [ServerOrClient]
        public void ClearCurrentTab()
        {
            switch (_currentTab)
            {
                case TileMapMenuTab.Build:
                    _tileMapBuildTab.Clear();
                    break;
                case TileMapMenuTab.Load:
                    _tileMapLoadTab.Clear();
                    break;
                case TileMapMenuTab.Save:
                    _tileMapSaveTab.Clear();
                    break;
            }
        }
    }
}