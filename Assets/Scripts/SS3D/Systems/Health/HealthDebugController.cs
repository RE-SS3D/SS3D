using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Inputs;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Runtime health debug overlay. Toggle with H (or Other/Toggle Health Debug when bound).
    /// </summary>
    public sealed class HealthDebugController : Actor
    {
        private InputSubSystem _inputSystem;
        private InputAction _toggleAction;
        private EntitySubSystem _entitySubSystem;

        private bool _open;
        private Rect _panelRect = new Rect(16f, 16f, 420f, 520f);
        private Vector2 _scroll;
        private HumanHealthController _selected;
        private HumanHealthController[] _targets = System.Array.Empty<HumanHealthController>();
        private int _selectedIndex;

        protected override void OnStart()
        {
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _entitySubSystem = SubSystems.Get<EntitySubSystem>();

            if (_inputSystem != null)
            {
                _toggleAction = _inputSystem.Inputs.FindAction("Other/Toggle Health Debug", throwIfNotFound: false);
                if (_toggleAction != null)
                {
                    _toggleAction.performed += OnToggle;
                }
            }

            AddHandle(LocalPlayerObjectChanged.AddListener(HandleLocalPlayerObjectChanged));
            RefreshTargets();
        }

        protected override void OnDestroyed()
        {
            if (_toggleAction != null)
            {
                _toggleAction.performed -= OnToggle;
            }
        }

        private void Update()
        {
            if (_toggleAction == null && Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            {
                _open = !_open;
                if (_open)
                {
                    RefreshTargets();
                }
            }
        }

        private void OnGUI()
        {
            if (!_open)
            {
                return;
            }

            _panelRect = GUILayout.Window(
                GetInstanceID(),
                _panelRect,
                DrawPanel,
                "Health Debug (H)");
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            _open = !_open;
            if (_open)
            {
                RefreshTargets();
            }
        }

        private void HandleLocalPlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            if (!e.PlayerHasObject)
            {
                return;
            }

            if (e.PlayerObject != null && e.PlayerObject.TryGetComponent(out HumanHealthController health))
            {
                SelectTarget(health);
            }
        }

        private void RefreshTargets()
        {
            _targets = FindObjectsByType<HumanHealthController>(FindObjectsSortMode.None);
            if (_selected != null)
            {
                for (int i = 0; i < _targets.Length; i++)
                {
                    if (_targets[i] == _selected)
                    {
                        _selectedIndex = i;
                        return;
                    }
                }
            }

            if (_targets.Length > 0)
            {
                SelectTarget(_targets[0]);
            }
            else
            {
                _selected = null;
                _selectedIndex = 0;
            }
        }

        private void SelectTarget(HumanHealthController health)
        {
            _selected = health;
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] == health)
                {
                    _selectedIndex = i;
                    return;
                }
            }
        }

        private void DrawPanel(int windowId)
        {
            _scroll = GUILayout.BeginScrollView(_scroll);

            if (_targets.Length == 0)
            {
                RefreshTargets();
            }

            if (_targets.Length == 0)
            {
                GUILayout.Label("No HumanHealthController instances in scene.");
                GUILayout.EndScrollView();
                GUI.DragWindow();
                return;
            }

            DrawTargetSelector();

            if (_selected == null)
            {
                GUILayout.Label("Select a target to inspect.");
                GUILayout.EndScrollView();
                GUI.DragWindow();
                return;
            }

            DrawHealthReport(_selected.Snapshot, _selected.DebugDetail, _selected.gameObject.name);

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawTargetSelector()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh"))
            {
                RefreshTargets();
            }

            if (GUILayout.Button("Local player") && TryGetLocalHealth(out HumanHealthController local))
            {
                SelectTarget(local);
            }

            GUILayout.EndHorizontal();

            string[] labels = BuildTargetLabels(_targets);
            int nextIndex = GUILayout.SelectionGrid(_selectedIndex, labels, 1);
            if (nextIndex != _selectedIndex && nextIndex >= 0 && nextIndex < _targets.Length)
            {
                _selectedIndex = nextIndex;
                _selected = _targets[_selectedIndex];
            }
        }

        private bool TryGetLocalHealth(out HumanHealthController health)
        {
            health = null;
            if (_entitySubSystem == null)
            {
                return false;
            }

            foreach (HumanHealthController candidate in _targets)
            {
                if (candidate.TryGetComponent(out Entity entity)
                    && entity.Mind?.player != null
                    && entity.Mind.player.IsLocalConnection)
                {
                    health = candidate;
                    return true;
                }
            }

            return false;
        }

        private static string[] BuildTargetLabels(HumanHealthController[] targets)
        {
            var labels = new string[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                HumanHealthController target = targets[i];
                string name = target.gameObject.name;
                if (target.TryGetComponent(out Entity entity) && entity.Mind?.player != null)
                {
                    name = $"{entity.Mind.player.Ckey} ({target.gameObject.name})";
                }

                labels[i] = name;
            }

            return labels;
        }

        private static void DrawHealthReport(HealthSnapshot snapshot, HealthDebugDetail detail, string objectName)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Target: {objectName}");
            builder.AppendLine($"State: {snapshot.State}");
            builder.AppendLine($"Conscious: {snapshot.IsConscious}  |  Cardiac arrest: {snapshot.IsCardiacArrest}  |  Bleeding: {snapshot.IsBleeding}");
            builder.AppendLine($"Can defibrillate: {snapshot.CanDefibrillate}  |  Critical flags: {snapshot.CriticalFlags}");
            builder.AppendLine($"Move speed x{snapshot.MovementSpeedMultiplier:F2}  |  Can use arms: {snapshot.CanUseArms}");
            builder.AppendLine();
            builder.AppendLine("Systemic pools");
            builder.AppendLine($"  Blood volume: {snapshot.Pools.BloodVolumeRatio:P0}");
            builder.AppendLine($"  Oxy debt: {snapshot.Pools.OxyDebt:F2}");
            builder.AppendLine($"  Toxin: {snapshot.Pools.ToxinConcentration:F2}");
            builder.AppendLine();
            builder.AppendLine("Worst zone damage (snapshot)");
            builder.AppendLine($"  Brute: {snapshot.WorstZoneBrute:F1}  |  Burn: {snapshot.WorstZoneBurn:F1}");
            builder.AppendLine();
            builder.AppendLine("Organs (stored function %)");
            AppendOrgan(builder, detail.Brain);
            AppendOrgan(builder, detail.Heart);
            AppendOrgan(builder, detail.LeftLung);
            AppendOrgan(builder, detail.RightLung);
            AppendOrgan(builder, detail.Liver);
            builder.AppendLine();
            builder.AppendLine("Body zones");
            AppendZone(builder, BodyZone.Head, detail.Head, snapshot);
            AppendZone(builder, BodyZone.Chest, detail.Chest, snapshot);
            AppendZone(builder, BodyZone.LeftArm, detail.LeftArm, snapshot);
            AppendZone(builder, BodyZone.RightArm, detail.RightArm, snapshot);
            AppendZone(builder, BodyZone.LeftLeg, detail.LeftLeg, snapshot);
            AppendZone(builder, BodyZone.RightLeg, detail.RightLeg, snapshot);
            AppendZone(builder, BodyZone.Groin, detail.Groin, snapshot);

            GUILayout.TextArea(builder.ToString());
        }

        private static void AppendOrgan(StringBuilder builder, OrganState organ)
        {
            string critical = organ.IsCritical ? " [CRIT]" : string.Empty;
            builder.AppendLine($"  {organ.Type,-10} {organ.FunctionPercent,5:F1}%{critical}");
        }

        private static void AppendZone(StringBuilder builder, BodyZone zone, ZoneDamageState state, HealthSnapshot snapshot)
        {
            string bleeding = snapshot.IsZoneBleeding(zone) ? " bleed" : string.Empty;
            string disabled = state.IsDisabled ? " DISABLED" : string.Empty;
            builder.AppendLine(
                $"  {zone,-9} brute {state.Brute,5:F1}  burn {state.Burn,5:F1}  {state.Severity,-8} rate {state.BleedingRate:F2}{bleeding}{disabled}");
        }
    }
}
