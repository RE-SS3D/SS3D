using Coimbra;
using DynamicPanels;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Tile.TileMapCreator
{
	/// <summary>
	/// In-game editor for placing and deleting items/objects in a tilemap.
	/// </summary>
	public class TileMapCreator : NetworkSystem, IPointerEnterHandler, IPointerExitHandler
	{
		public GameObject _menuRoot;
		public GameObject _contentRoot;
		public GameObject _slotPrefab;
		public TMP_Dropdown _layerPlacementDropdown;
        [SerializeField]
        private TMP_InputField _inputField;

		private bool _enabled = false;
		private bool _initalized = false;
		private bool _isDeleting = false;
		private bool _itemPlacement = false;
		private bool _mouseOverUI = false;

		private Vector3 _lastSnappedPosition;
		private GenericObjectSo _selectedObject;

		private TileSystem _tileSystem;
		private GhostManager _ghostManager;
		private Plane _plane;

		private List<GenericObjectSo> _objectDatabase;
		private Controls.TileCreatorActions _controls;
		private InputSystem _inputSystem;
        private PanelTab _tab;

		public bool IsDeleting
		{
			get => _isDeleting;
			set
			{
				if (_selectedObject == null)
				{
					return;
				}

				_isDeleting = value;
				RefreshGhost();
			}
		}

		[ServerOrClient]
		protected override void OnStart()
		{
			base.OnStart();
            _tab = PanelUtils.GetAssociatedTab(GetComponent<RectTransform>());
            ShowUI(false);
            _inputSystem = Subsystems.Get<InputSystem>();
			_controls = _inputSystem.Inputs.TileCreator;
			_inputSystem.ToggleAction(_controls.ToggleMenu, true);
            
            _controls.ToggleMenu.performed += HandleToggleMenu;
			_controls.Replace.performed += HandleReplace;
			_controls.Replace.canceled += HandleReplace;
			_controls.Place.performed += HandlePlace;
			_controls.Rotate.performed += HandleRotate;
            
		}

		private void HandleToggleMenu(InputAction.CallbackContext context)
		{
			if (_enabled)
			{
				_inputSystem.ToggleActionMap(_controls, false, new[]
				{
					_controls.ToggleMenu
				});

				_inputSystem.ToggleCollisions(_controls, true);
			}
			else
			{
				_inputSystem.ToggleActionMap(_controls, true, new[]
				{
					_controls.ToggleMenu
				});

				_inputSystem.ToggleCollisions(_controls, false);
			}

			_enabled = !_enabled;
			ShowUI(_enabled);
			Initialize();
		}

		private void HandleRotate(InputAction.CallbackContext context)
		{
			_ghostManager.SetNextRotation();
			RefreshGhost();
		}

		private void HandleReplace(InputAction.CallbackContext context)
		{
			RefreshGhost();
		}

		private void HandlePlace(InputAction.CallbackContext context)
		{
			if (_mouseOverUI)
			{
				return;
			}

			HandleMouseClick(_lastSnappedPosition, _controls.Replace.phase == InputActionPhase.Performed);
		}

		[ServerOrClient]
		private void Initialize()
		{
			if (_initalized)
			{
				return;
			}

			_tileSystem = Subsystems.Get<TileSystem>();
			_ghostManager = GetComponent<GhostManager>();
			_plane = new Plane(Vector3.up, 0);

			LoadObjectGrid(new[]
			{
				TileLayer.Plenum
			}, false);

			_initalized = true;
		}

		[ServerOrClient]
		private void Update()
		{
			if (!_initalized)
			{
				return;
			}
			// Clean-up if we are not building
			if (_selectedObject == null)
			{
                _ghostManager.DestroyGhost();
                return;
			}

			_ghostManager.CreateGhost(_selectedObject.prefab);

			// Check if mouse moved
			Vector3 snappedPosition = TileHelper.GetClosestPosition(GetMousePosition());

			if (snappedPosition != _lastSnappedPosition && !_itemPlacement)
			{
				_ghostManager.SetTargetPosition(snappedPosition);
				_lastSnappedPosition = snappedPosition;
				RefreshGhost();
			}

			if (_itemPlacement)
			{
				Vector3 newPosition = GetMousePosition();
				_ghostManager.SetTargetPosition(newPosition);
				_lastSnappedPosition = newPosition;
			}

			_ghostManager.MoveGhost();
		}

		[ServerOrClient]
		private void HandleMouseClick(Vector3 snappedPosition, bool replaceExisting)
		{
			if (_isDeleting)
			{
				if (_itemPlacement)
				{
					FindAndDeleteItem(snappedPosition);
				}
				else
				{
					_tileSystem.RpcClearTileObject(_selectedObject.nameString, snappedPosition, _ghostManager.Dir);
				}
			}
			else
			{
				if (_selectedObject == null)
				{
					return;
				}

				_tileSystem.RpcPlaceObject(_selectedObject.nameString, snappedPosition, _ghostManager.Dir, replaceExisting);
				RefreshGhost();
			}
		}

		[ServerOrClient]
		private void FindAndDeleteItem(Vector3 worldPosition)
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			if (Physics.Raycast(ray, out RaycastHit hitInfo))
			{
				PlacedItemObject placedItem = hitInfo.collider.gameObject.GetComponent<PlacedItemObject>();

				if (placedItem != null)
				{
					_tileSystem.RpcClearItemObject(placedItem.NameString, worldPosition);
				}
			}
		}

		[ServerOrClient]
		private Vector3 GetMousePosition()
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			if (_plane.Raycast(ray, out float distance))
			{
				return ray.GetPoint(distance);
			}

			return Vector3.zero;
		}

		[ServerOrClient]
		private void CheckBuildValidity(Vector3 placePosition, bool replaceExisting)
		{
			RpcSendCanBuild(_selectedObject.nameString, placePosition, _ghostManager.Dir, replaceExisting, LocalConnection);
		}

		// Ownership not required since a client can request whether it is possible to build
		[Client]
		[ServerRpc(RequireOwnership = false)]
		public void RpcSendCanBuild(string tileObjectSoName, Vector3 placePosition, Direction dir, bool replaceExisting, NetworkConnection conn)
		{
			if (_tileSystem == null)
			{
				_tileSystem = Subsystems.Get<TileSystem>();
			}

			TileObjectSo tileObjectSo = (TileObjectSo)_tileSystem.GetAsset(tileObjectSoName);

			if (tileObjectSo == null)
			{
				return;
			}

			bool canBuild = _tileSystem.CanBuild(tileObjectSo, placePosition, dir, replaceExisting);
			RpcReceiveCanBuild(conn, canBuild);
		}

		[Client]
		[TargetRpc]
		private void RpcReceiveCanBuild(NetworkConnection conn, bool canBuild)
		{
			_ghostManager.ChangeGhostColor(canBuild ? GhostManager.BuildMatMode.Valid : GhostManager.BuildMatMode.Invalid);
		}

		[ServerOrClient]
		private void ShowUI(bool show)
		{
            if (!show)
            {
                if (_ghostManager)
                {
                    _ghostManager.DestroyGhost();
                }
                _tab.Detach();
            }
            _tab.Panel.gameObject.SetActive(show);
			_menuRoot.SetActive(show);
		}

		/// <summary>
		/// Loads a list of tile objects and places them in the UI box grid.
		/// </summary>
		/// <param name="allowedLayers"></param>
		/// <param name="isItems"></param>
		[ServerOrClient]
		private void LoadObjectGrid(TileLayer[] allowedLayers, bool isItems)
		{
			ClearGrid();

			_objectDatabase = _tileSystem.Loader.Assets;

			foreach (GenericObjectSo asset in _objectDatabase)
			{
				switch (isItems)
				{
					case true when asset is not ItemObjectSo:
					case false when asset is ItemObjectSo:
					case false when asset is TileObjectSo so && !allowedLayers.Contains(so.layer):
						continue;
				}

				GameObject slot = Instantiate(_slotPrefab, _contentRoot.transform, true);

				TileMapCreatorTab tab = slot.AddComponent<TileMapCreatorTab>();

				tab.Setup(asset);
			}
		}

		[ServerOrClient]
		private void ClearGrid()
		{
			for (int i = 0; i < _contentRoot.transform.childCount; i++)
			{
				_contentRoot.transform.GetChild(i).gameObject.Dispose(true);
			}
		}

		[ServerOrClient]
		private void RefreshGhost()
		{
			if (_selectedObject == null)
			{
				return;
			}

			if (_isDeleting)
			{
				_ghostManager.ChangeGhostColor(GhostManager.BuildMatMode.Deleting);
			}
			else
			{
				switch (_itemPlacement)
				{
					case true:
						_ghostManager.ChangeGhostColor(GhostManager.BuildMatMode.Valid);

						break;
					case false when _controls.Replace.phase == InputActionPhase.Performed:
						CheckBuildValidity(_lastSnappedPosition, true);

						break;
					case false:
						CheckBuildValidity(_lastSnappedPosition, false);

						break;
				}
			}
		}

		[ServerOrClient]
		public void SetSelectedObject(GenericObjectSo genericObjectSo)
		{
			_itemPlacement = genericObjectSo switch
			{
				TileObjectSo => false,
				ItemObjectSo => true,
				_ => _itemPlacement,
			};

			_selectedObject = genericObjectSo;
			_ghostManager.DestroyGhost();
			_ghostManager.CreateGhost(genericObjectSo.prefab);

			RefreshGhost();
		}

		[Server]
		public void LoadMap()
		{
			if (IsServer)
			{
				_tileSystem.Load();
			}
			else
			{
				Log.Information(this, "Cannot load the map on a client");
			}
		}

		[Server]
		public void SaveMap()
		{
			if (IsServer)
			{
				_tileSystem.Save();
			}
			else
			{
				Log.Information(this, "Cannot save the map on a client");
			}
		}


		/// <summary>
		/// Change the currently displayed tiles/items when a new layer is selected in the drop down menu.
		/// </summary>
		[ServerOrClient]
		public void OnDropDownChange()
		{
			int index = _layerPlacementDropdown.value;

			switch (index)
			{
				case 0:
					LoadObjectGrid(new[]
					{
						TileLayer.Plenum
					}, false);

					break;
				case 1:
					LoadObjectGrid(new[]
					{
						TileLayer.Turf
					}, false);

					break;
				case 2:
					LoadObjectGrid(new[]
					{
						TileLayer.FurnitureBase,
						TileLayer.FurnitureTop
					}, false);

					break;
				case 3:
					LoadObjectGrid(new[]
					{
						TileLayer.WallMountHigh,
						TileLayer.WallMountLow
					}, false);

					break;
				case 4:
					LoadObjectGrid(new[]
					{
						TileLayer.Wire,
						TileLayer.Disposal,
						TileLayer.PipeLeft,
                        TileLayer.PipeRight,
                        TileLayer.PipeSurface,
                        TileLayer.PipeMiddle
					}, false);

					break;
				case 5:
					LoadObjectGrid(new[]
					{
						TileLayer.Overlays
					}, false);

					break;
				case 6:
					LoadObjectGrid(null, true);

					break;
				default:
					ClearGrid();

					break;
			}
		}

        public void OnInputFieldChanged()
        {
            ClearGrid();

            if (_inputField.text.Contains(' '))
            {
                // Replace spaces with underscores, since all asset names contain underscores
                _inputField.text = _inputField.text.Replace(' ', '_');
                // Prevent executing the same code twice
                return;
            }
            foreach (GenericObjectSo asset in _objectDatabase)
            {
                if (!asset.nameString.Contains(_inputField.text)) continue;
                GameObject slot = Instantiate(_slotPrefab, _contentRoot.transform, true);
                TileMapCreatorTab tab = slot.AddComponent<TileMapCreatorTab>();
                tab.Setup(asset);
            }
        }

        public void OnInputFieldSelect()
        {
            _inputSystem.ToggleAllActions(false);
        }

        public void OnInputFieldDeselect()
        {
            _inputSystem.ToggleAllActions(true);
        }

        [ServerOrClient]
		public void OnPointerEnter(PointerEventData eventData)
		{
			_mouseOverUI = true;
			Subsystems.Get<InputSystem>().ToggleBinding("<Mouse>/scroll/y", false);
		}

		[ServerOrClient]
		public void OnPointerExit(PointerEventData eventData)
		{
			_mouseOverUI = false;
			Subsystems.Get<InputSystem>().ToggleBinding("<Mouse>/scroll/y", true);
		}
	}
}