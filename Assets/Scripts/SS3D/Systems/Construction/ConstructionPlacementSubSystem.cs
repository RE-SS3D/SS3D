using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Screens;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.TileMapCreator;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Held-item cell actions (D3 of 2026-11_construction-mvp1, revised D14): steel sheets on a hull hole, the
    /// one cell with no object to hover. The owning client shows a ghost of the plating under the cursor
    /// through <see cref="ConstructionHologram"/>, with the fail reason and an "n / m" cost tag; a click sends
    /// one ServerRpc with the cell and step. Girders, floor tiles and wreckage are interactions on the
    /// hovered floor or plating (<see cref="ConstructionRule.ActsOnCell"/>). The server checks rule, reach,
    /// materials and <see cref="BuildChecker"/>, runs the step on a timer with the same cancel rules as
    /// <see cref="DelayedInteraction"/>, and spends materials only at completion. Steps on existing objects
    /// (weld, plate, pry) are ordinary interactions, see <see cref="ConstructionInteractions"/>.
    /// Lives on the NetworkSystemsHub (added by <c>ConstructionHubRecipe</c>).
    /// </summary>
    public sealed partial class ConstructionPlacementSubSystem : NetworkSubSystem
    {
        private const float CheckInterval = 0.2f;
        private const float GhostLift = 0.02f;
        private const float RejectShowSeconds = 1.5f;

        private sealed class PendingCellAction
        {
            public int Id;
            public NetworkConnection Connection;
            public ConstructionRule Rule;
            public Vector3 Grid;
            public Hands Hands;
            public Hand Hand;
            public Item Item;
            public float StartTime;
            public float LastCheck;
            public Vector3 StartBody;
            public IDamageNotifier Damage;
            public bool Damaged;
            public int CueId;
            public InteractionController Controller;

            public void HandleDamaged() => Damaged = true;
        }

        // Server
        private readonly Dictionary<NetworkConnection, PendingCellAction> _pending = new();
        private readonly List<NetworkConnection> _pendingScratch = new();
        private int _nextActionId = 1;

        // Client
        private readonly List<ConstructionRule> _cellRules = new();
        private ConstructionRule _ghostRule;
        private Vector3 _ghostGrid;
        private bool _ghostValid;
        private ConstructionHologram _hologram;
        private TileObjectSo _hologramSource;
        private Label _ghostLabel;
        private Camera _camera;
        private int _clientActionId = -1;
        private int _clientRingId = DelayedProgressFeedback.NoRing;
        private int _clientRingKey;
        private string _rejectMessage;
        private Vector3 _rejectGrid;
        private float _rejectUntil;

        /// <summary>True while the owning client has a cell action on the timer.</summary>
        public bool HasLocalAction => _clientActionId >= 0;

        /// <summary>True while a ghost is shown under the cursor, valid or not.</summary>
        public bool HasGhost => _ghostRule != null;

        public override void OnStopServer()
        {
            foreach (PendingCellAction action in _pending.Values)
            {
                ReleaseDamage(action);
            }

            _pending.Clear();
            base.OnStopServer();
        }

        public override void OnStopClient()
        {
            HideGhost();
            base.OnStopClient();
        }

        protected override void OnDestroyed()
        {
            HideGhost();
            base.OnDestroyed();
        }

        private void Update()
        {
            if (IsServer)
            {
                UpdateServerActions();
            }

            if (IsClient)
            {
                UpdateGhost();
                UpdateWorkCues();
            }
        }

        #region Client

        /// <summary>
        /// Primary click with a ghost up. <see cref="InteractionController"/> asks first, before any target
        /// interaction; the ghost is hidden while the hovered target has one. Returns true when the click is used.
        /// </summary>
        [Client]
        public bool TryHandlePrimaryClick()
        {
            if (_ghostRule == null)
            {
                return false;
            }

            if (!_ghostValid || HasLocalAction)
            {
                return true;
            }

            if (!TryGetLocalHand(out Hands _, out Hand hand, out Item _))
            {
                return true;
            }

            // Wall work reaches out at once; the kneel for floor work is the server's replicated body state.
            int clip = InteractionAnimation.ChooseClip(_ghostRule.GenericName);
            InteractionController owner = InteractionController.Local;
            if (clip != InteractionAnimation.Kneel && owner != null)
            {
                owner.BeginOwnerHandAction(clip, _ghostGrid);
            }

            _clientRingKey = DelayedProgressFeedback.KeyFor(hand.GameObject.transform);
            _clientRingId = DelayedProgressFeedback.Begin(_clientRingKey, null, _ghostGrid, _ghostRule.Seconds);
            _clientActionId = 0;
            CmdRequestCellAction((byte)_ghostRule.Step, _ghostGrid);
            return true;
        }

        /// <summary>The Cancel key. Returns true when a cell action was running.</summary>
        [Client]
        public bool TryCancelLocal()
        {
            if (!HasLocalAction)
            {
                return false;
            }

            DelayedProgressFeedback.Cancel(_clientRingId, DelayedCancelReason.Manual);
            ClearClientAction();
            CmdCancelCellAction();
            return true;
        }

        [Client]
        private void UpdateGhost()
        {
            if (!TryResolveGhost(out ConstructionRule rule, out Vector3 grid, out TileObjectSo result, out string message, out bool valid))
            {
                HideGhost();
                return;
            }

            // A server-only refusal (outside the station) stays on the cell for a moment.
            if (_rejectMessage != null && Time.time < _rejectUntil && grid == _rejectGrid)
            {
                message = _rejectMessage;
                valid = false;
            }

            _ghostRule = rule;
            _ghostGrid = grid;
            _ghostValid = valid;
            ShowHologram(result, grid, valid);
            ShowLabel(grid, message, valid);
        }

        [Client]
        private bool TryResolveGhost(out ConstructionRule rule, out Vector3 grid, out TileObjectSo result, out string message, out bool valid)
        {
            rule = null;
            grid = default;
            result = null;
            message = null;
            valid = false;

            InteractionController controller = InteractionController.Local;
            if (controller == null || controller.CurrentIntent != IntentType.Help || HasLocalAction
                || controller.HasActiveDelayedInteraction || controller.HoveredTargetHasInteraction
                || InputInterface.IsPointerOverInterface())
            {
                return false;
            }

            if (SubSystems.TryGet(out MapEditorSubSystem mapEditor) && mapEditor.IsActive)
            {
                return false;
            }

            if (SubSystems.TryGet(out ArmedInteractionSubSystem armed) && armed.IsArmed)
            {
                return false;
            }

            if (!TryGetLocalHand(out Hands hands, out Hand _, out Item item)
                || !ConstructionInputs.TryGet(item, out ConstructionInput input))
            {
                return false;
            }

            ConstructionRecipes.ForInput(input, cellActions: true, _cellRules);
            if (_cellRules.Count == 0 || !SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null)
            {
                return false;
            }

            Camera camera = ResolveCamera();
            if (camera == null)
            {
                return false;
            }

            grid = TileHelper.GetPointedPosition(true, camera);
            grid.y = 0f;
            TileMap map = tiles.CurrentMap;

            foreach (ConstructionRule candidate in _cellRules)
            {
                if (!ConstructionCellRules.Matches(map, candidate, grid, tiles.HasWreckage))
                {
                    continue;
                }

                rule = candidate;
                result = ResolveResult(tiles, candidate);
                // Area ids are server-only; the server repeats the station check.
                message = ConstructionCellRules.Validate(map, candidate, grid, result, checkStation: IsServer, tiles.HasWreckage)
                    ?? CheckHolder(candidate, hands, item, grid);
                valid = message == null;
                message = FormatGhostText(candidate, item, message);
                return true;
            }

            return false;
        }

        [Client]
        private void ShowHologram(TileObjectSo result, Vector3 grid, bool valid)
        {
            if (result == null)
            {
                DestroyHologram();
                return;
            }

            if (_hologram == null || _hologramSource != result)
            {
                DestroyHologram();
                GameObject prefab = Assets.Get<GameObject>(result.PrefabAsset);
                if (prefab == null)
                {
                    return;
                }

                GameObject ghost = Instantiate(prefab);
                // PlacedTileObject.Create places tiles with yaw only; match it (tile map Pitfall).
                ghost.transform.localRotation = Quaternion.identity;
                _hologram = new ConstructionHologram(ghost, grid, Direction.North, result.placementYOffset + GhostLift);
                ghost.transform.position = grid + Vector3.up * (result.placementYOffset + GhostLift);
                ghost.transform.rotation = _hologram.TargetWorldRotation;
                _hologramSource = result;
            }

            _hologram.TargetPosition = grid;
            _hologram.ChangeHologramColor(valid ? ConstructionMode.Valid : ConstructionMode.Invalid);
            _hologram.UpdateRotationAndPosition();
        }

        [Client]
        private void ShowLabel(Vector3 grid, string text, bool valid)
        {
            if (!EnsureLabel())
            {
                return;
            }

            Camera camera = ResolveCamera();
            IPanel panel = _ghostLabel.panel;
            Vector3 screen = camera != null ? camera.WorldToScreenPoint(grid + Vector3.up * 0.6f) : Vector3.back;
            if (panel == null || screen.z <= 0f || string.IsNullOrEmpty(text))
            {
                _ghostLabel.style.display = DisplayStyle.None;
                return;
            }

            Vector2 point = InputInterface.ScreenToPanel(panel, new Vector2(screen.x, screen.y));
            _ghostLabel.text = text;
            _ghostLabel.style.color = valid ? new Color(0.75f, 1f, 0.75f) : new Color(1f, 0.6f, 0.55f);
            _ghostLabel.style.left = point.x - 150f;
            _ghostLabel.style.top = point.y - 24f;
            _ghostLabel.style.display = DisplayStyle.Flex;
        }

        [Client]
        private bool EnsureLabel()
        {
            if (_ghostLabel != null && _ghostLabel.panel != null)
            {
                return true;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem shell) || !shell.TryGetLayer(UiLayer.Overlay, out VisualElement layer))
            {
                return false;
            }

            _ghostLabel = new Label { name = "construction-ghost-label", pickingMode = PickingMode.Ignore };
            _ghostLabel.style.position = UnityEngine.UIElements.Position.Absolute;
            _ghostLabel.style.width = 300f;
            _ghostLabel.style.unityTextAlign = TextAnchor.LowerCenter;
            _ghostLabel.style.fontSize = 13f;
            _ghostLabel.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 2f, color = new Color(0f, 0f, 0f, 0.85f) };
            layer.Add(_ghostLabel);
            return true;
        }

        [Client]
        private void HideGhost()
        {
            _ghostRule = null;
            _ghostValid = false;
            DestroyHologram();
            if (_ghostLabel != null)
            {
                _ghostLabel.style.display = DisplayStyle.None;
            }
        }

        private void DestroyHologram()
        {
            if (_hologram != null && _hologram.Hologram != null)
            {
                _hologram.Destroy();
            }

            _hologram = null;
            _hologramSource = null;
        }

        private Camera ResolveCamera()
        {
            if (_camera != null && _camera.isActiveAndEnabled)
            {
                return _camera;
            }

            Actor playerCamera = SubSystems.TryGet(out CameraSubSystem cameras) ? cameras.PlayerCamera : null;
            _camera = playerCamera != null ? playerCamera.GetComponent<Camera>() : Camera.main;
            return _camera;
        }

        [Client]
        private static bool TryGetLocalHand(out Hands hands, out Hand hand, out Item item)
        {
            hands = null;
            hand = null;
            item = null;
            InteractionController controller = InteractionController.Local;
            if (controller == null)
            {
                return false;
            }

            hands = controller.GetComponentInChildren<Hands>();
            hand = hands != null ? hands.SelectedHand : null;
            item = hand != null ? hand.ItemInHand : null;
            return item != null;
        }

        [TargetRpc]
        private void TargetCellActionStarted(NetworkConnection connection, int actionId)
        {
            _clientActionId = actionId;
        }

        [TargetRpc]
        private void TargetCellActionRejected(NetworkConnection connection, string message)
        {
            if (_clientRingId != DelayedProgressFeedback.NoRing)
            {
                DelayedProgressFeedback.Clear(_clientRingKey);
            }

            ClearClientAction();
            _rejectMessage = message;
            _rejectGrid = _ghostGrid;
            _rejectUntil = Time.time + RejectShowSeconds;
        }

        [TargetRpc]
        private void TargetCellActionEnded(NetworkConnection connection, int actionId, bool completed, byte reason)
        {
            if (completed)
            {
                DelayedProgressFeedback.Complete(_clientRingId);
            }
            else
            {
                DelayedProgressFeedback.Cancel(_clientRingId, (DelayedCancelReason)reason);
            }

            ClearClientAction();
        }

        private void ClearClientAction()
        {
            _clientActionId = -1;
            _clientRingId = DelayedProgressFeedback.NoRing;
        }

        #endregion

        #region Server

        [ServerRpc(RequireOwnership = false)]
        private void CmdRequestCellAction(byte step, Vector3 grid, NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            ConstructionRule rule = ConstructionRecipes.Get((ConstructionStep)step);
            if (rule == null || !rule.IsCellAction)
            {
                TargetCellActionRejected(conn, "Unknown step");
                return;
            }

            grid = TileHelper.GetClosestPosition(grid);
            grid.y = 0f;

            if (_pending.TryGetValue(conn, out PendingCellAction previous))
            {
                EndAction(previous, completed: false, DelayedCancelReason.Manual);
            }

            if (!TryResolveConnectionHand(conn, out Hands hands, out Hand hand, out Item item))
            {
                TargetCellActionRejected(conn, "Nothing in hand");
                return;
            }

            string failure = ServerValidate(rule, grid, hands, item, out _);
            if (failure != null)
            {
                TargetCellActionRejected(conn, failure);
                return;
            }

            PendingCellAction action = new()
            {
                Id = _nextActionId++,
                Connection = conn,
                Rule = rule,
                Grid = grid,
                Hands = hands,
                Hand = hand,
                Item = item,
                StartTime = Time.time,
                LastCheck = Time.time,
                StartBody = hands.transform.position,
                Damage = hands.GetComponentInParent<IDamageNotifier>(),
            };

            if (action.Damage != null)
            {
                action.Damage.ServerDamaged += action.HandleDamaged;
            }

            _pending[conn] = action;
            action.Controller = hands.GetComponentInParent<InteractionController>();
            if (action.Controller != null)
            {
                action.Controller.ServerBeginStepAnimation(rule.GenericName, grid);
            }
            action.CueId = ServerBeginWorkCue(grid + Vector3.up * 0.1f, hands.transform.position, rule.Input, rule.Seconds);
            TargetCellActionStarted(conn, action.Id);
        }

        [ServerRpc(RequireOwnership = false)]
        private void CmdCancelCellAction(NetworkConnection conn = null)
        {
            if (conn != null && _pending.TryGetValue(conn, out PendingCellAction action))
            {
                EndAction(action, completed: false, DelayedCancelReason.Manual);
            }
        }

        [Server]
        private void UpdateServerActions()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            _pendingScratch.Clear();
            _pendingScratch.AddRange(_pending.Keys);
            float now = Time.time;

            foreach (NetworkConnection conn in _pendingScratch)
            {
                if (!_pending.TryGetValue(conn, out PendingCellAction action))
                {
                    continue;
                }

                if (action.Hands == null || action.Hand == null || !conn.IsActive)
                {
                    EndAction(action, completed: false, DelayedCancelReason.Interrupted);
                    continue;
                }

                if (action.Damaged)
                {
                    EndAction(action, completed: false, DelayedCancelReason.Hurt);
                    continue;
                }

                if (!InteractionExtensions.CharacterMoveCheck(action.StartBody, action.Hands.transform.position))
                {
                    EndAction(action, completed: false, DelayedCancelReason.Moved);
                    continue;
                }

                bool due = now - action.StartTime >= action.Rule.Seconds;
                if (!due && now - action.LastCheck < CheckInterval)
                {
                    continue;
                }

                action.LastCheck = now;

                if (!action.Hand.CanContinueInteraction())
                {
                    EndAction(action, completed: false, DelayedCancelReason.KnockedDown);
                    continue;
                }

                string failure = ServerValidate(action.Rule, action.Grid, action.Hands, action.Hand.ItemInHand, out TileObjectSo result);
                if (failure != null || action.Hand.ItemInHand != action.Item)
                {
                    EndAction(action, completed: false, ReasonFor(action, failure));
                    continue;
                }

                if (!due)
                {
                    continue;
                }

                bool applied = ConstructionApply.TryApplyCell(action.Rule, action.Grid, result, action.Item, action.Hand);
                EndAction(action, applied, DelayedCancelReason.Interrupted);
            }
        }

        private DelayedCancelReason ReasonFor(PendingCellAction action, string failure)
        {
            if (action.Hand.ItemInHand != action.Item)
            {
                return DelayedCancelReason.MissingMaterials;
            }

            return failure == NotEnoughMessage(action.Rule) ? DelayedCancelReason.MissingMaterials : DelayedCancelReason.Interrupted;
        }

        [Server]
        private void EndAction(PendingCellAction action, bool completed, DelayedCancelReason reason)
        {
            ReleaseDamage(action);
            if (action.Controller != null)
            {
                action.Controller.ServerEndStepAnimation();
            }

            ServerEndWorkCue(action.CueId);
            _pending.Remove(action.Connection);
            if (action.Connection != null && action.Connection.IsActive)
            {
                TargetCellActionEnded(action.Connection, action.Id, completed, (byte)reason);
            }
        }

        private static void ReleaseDamage(PendingCellAction action)
        {
            if (action.Damage != null)
            {
                action.Damage.ServerDamaged -= action.HandleDamaged;
                action.Damage = null;
            }
        }

        [Server]
        private string ServerValidate(ConstructionRule rule, Vector3 grid, Hands hands, Item item, out TileObjectSo result)
        {
            result = null;
            if (!SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null)
            {
                return "No map";
            }

            if (!ConstructionInputs.TryGet(item, out ConstructionInput input) || input != rule.Input)
            {
                return $"Needs {ConstructionInputs.Label(rule.Input)}";
            }

            result = ResolveResult(tiles, rule);
            return ConstructionCellRules.Validate(tiles.CurrentMap, rule, grid, result, checkStation: true, tiles.HasWreckage)
                ?? CheckHolder(rule, hands, item, grid);
        }

        [Server]
        private static bool TryResolveConnectionHand(NetworkConnection conn, out Hands hands, out Hand hand, out Item item)
        {
            hands = conn.FirstObject != null ? conn.FirstObject.GetComponentInChildren<Hands>() : null;
            hand = hands != null ? hands.SelectedHand : null;
            item = hand != null ? hand.ItemInHand : null;
            return item != null;
        }

        #endregion

        #region Shared

        private static TileObjectSo ResolveResult(TileSubSystem tiles, ConstructionRule rule) =>
            string.IsNullOrEmpty(rule.ResultName) ? null : tiles.GetAsset(rule.ResultName) as TileObjectSo;

        /// <summary>Reach and materials, from the holder's side. Null when fine.</summary>
        private static string CheckHolder(ConstructionRule rule, Hands hands, Item item, Vector3 grid)
        {
            if (!ConstructionCellRules.InReach(hands.transform.position, grid))
            {
                return "Out of reach";
            }

            if (ConstructionInputs.IsMaterial(rule.Input) && item.StackCount < rule.Cost)
            {
                return NotEnoughMessage(rule);
            }

            return null;
        }

        private static string NotEnoughMessage(ConstructionRule rule) => $"Not enough {ConstructionInputs.Label(rule.Input)}";

        /// <summary>"Patch hull · 1 / 20 steel sheets", or the fail reason in place of the verb.</summary>
        private static string FormatGhostText(ConstructionRule rule, Item item, string failure)
        {
            string head = failure ?? rule.Verb;
            if (!ConstructionInputs.IsMaterial(rule.Input) || rule.Cost <= 0)
            {
                return head;
            }

            return $"{head} · {rule.Cost} / {item.StackCount} {ConstructionInputs.Label(rule.Input)}";
        }

        #endregion
    }
}
