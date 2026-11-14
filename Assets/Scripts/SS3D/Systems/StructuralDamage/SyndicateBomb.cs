using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.ActionMessages;
using SS3D.Systems.Audio;
using SS3D.Systems.Examine;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// The Syndicate bomb (explosives-destruction.md §6): a floor device too heavy to lift. It is dragged into
    /// place, its timer set (90 seconds or more), armed, and bolted down with a wrench so nobody can drag it off
    /// or let the wind take it. While armed it beeps loudly enough to hear from the next room, and it ends in
    /// the largest blast in the game. Screwdriver then wirecutters defuse it, as on the nuke. Rules live in
    /// <see cref="SyndicateBombState"/>; this is the server adapter and the client readout.
    /// </summary>
    /// <remarks>
    /// The bomb is an <see cref="Item"/> (map editor, admin <c>spawn</c>, save) flagged too heavy to lift, with a
    /// <see cref="Draggable"/> beside it. Operatives get it from <see cref="SyndicateBombBeacon"/>.
    /// </remarks>
    public sealed class SyndicateBomb : InteractionTargetNetworkBehaviour, IExamineContentProvider, IDragLock
    {
        public const float ArmSeconds = 2f;
        public const float WrenchSeconds = 3f;
        public const float OpenPanelSeconds = 5f;
        public const float CutWiresSeconds = 10f;

        /// <summary>Provisional tuning: the biggest blast in the game, holing the hull for several tiles around.</summary>
        public const float BlastYield = 500f;
        public const float BlastFalloff = 40f;

        /// <summary>Loud enough to carry into the rooms next door.</summary>
        public const float BeepRange = 25f;

        [SyncVar] private bool _armed;
        [SyncVar] private bool _panelOpen;
        [SyncVar] private bool _anchored;
        [SyncVar] private bool _spent;
        [SyncVar] private double _deadline;
        [SyncVar] private float _timerSeconds = SyndicateBombState.MinTimerSeconds;

        [SerializeField] private string _wrenchSoundId;
        [SerializeField] private Light _lamp;

        // Client only.
        private double _nextBeepAt;
        private float _lampOffAt;

        public SyndicateBombState State => new(_armed, _panelOpen, _anchored, _spent, _deadline, _timerSeconds);

        public float Remaining => State.Remaining(Now);

        public bool DragLocked => !State.CanMove;

        private double Now
        {
            get
            {
                NetworkObject network = GetComponent<NetworkObject>();
                FishNet.Managing.Timing.TimeManager clock = network != null ? network.TimeManager : null;
                return clock != null ? clock.TicksToTime(clock.Tick) : 0d;
            }
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent e) =>
            new IInteraction[]
            {
                new TimerInteraction(this, +1), new TimerInteraction(this, -1), new ArmInteraction(this),
                new WrenchInteraction(this), new OpenPanelInteraction(this), new CutWiresInteraction(this),
            };

        private void Update()
        {
            if (!GetComponent<NetworkObject>().IsSpawned)
            {
                return;
            }

            if (IsServer)
            {
                KeepBolted();
                if (State.ShouldDetonate(Now))
                {
                    Detonate();
                    return;
                }
            }

            if (IsClient)
            {
                TickBeep();
            }
        }

        /// <summary>Server: bolted down, the body ignores wind and shoves; loose, it is an ordinary heavy item.</summary>
        private void KeepBolted()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null || !_anchored || body.isKinematic)
            {
                return;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        /// <summary>Client: beep and flash the lamp while armed, faster toward zero, from the synced deadline.</summary>
        private void TickBeep()
        {
            SyndicateBombState state = State;
            double now = Now;
            if (_lamp != null)
            {
                _lamp.enabled = state.Armed && Time.time < _lampOffAt;
            }

            if (!state.Armed || state.Spent)
            {
                _nextBeepAt = 0d;
                return;
            }

            if (now < _nextBeepAt)
            {
                return;
            }

            _nextBeepAt = now + SyndicateBombState.BeepInterval(state.Remaining(now));
            _lampOffAt = Time.time + 0.12f;
            if (SubSystems.TryGet(out AudioSubSystem audio))
            {
                audio.PlayAudioSourceLocal(AudioType.Sfx, NukeAudioTrackIds.Tick, transform.position, NetworkObject,
                    volume: 0.9f, minRange: 2f, maxRange: BeepRange);
            }
        }

        private void PlayAtBomb(string clipId, float maxRange, float volume = 0.7f)
        {
            if (IsServer && !string.IsNullOrEmpty(clipId) && SubSystems.TryGet(out AudioSubSystem audio))
            {
                audio.PlayAudioSource(AudioType.Sfx, clipId, transform.position, NetworkObject,
                    volume: volume, minRange: 1f, maxRange: maxRange);
            }
        }

        [Server]
        public bool ServerChangeTimer(int steps)
        {
            SyndicateBombState state = State;
            if (!state.CanSetTimer || steps == 0)
            {
                return false;
            }

            float seconds = SyndicateBombState.ClampTimer(state.TimerSeconds + steps * SyndicateBombState.TimerStepSeconds);
            if (Mathf.Approximately(seconds, state.TimerSeconds))
            {
                return false;
            }

            Apply(state.WithTimer(seconds));
            return true;
        }

        [Server]
        public bool ServerArm()
        {
            if (!State.CanArm)
            {
                return false;
            }

            Apply(State.ArmedAt(Now));
            PlayAtBomb(NukeAudioTrackIds.ArmBeep, 12f);
            return true;
        }

        [Server]
        public bool ServerSetAnchored(bool anchored)
        {
            SyndicateBombState state = State;
            if (anchored ? !state.CanAnchor : !state.CanUnanchor)
            {
                return false;
            }

            Draggable draggable = GetComponent<Draggable>();
            if (anchored && draggable != null && draggable.Dragged)
            {
                return false;
            }

            Apply(state.WithAnchored(anchored));
            if (anchored)
            {
                KeepBolted();
            }
            else
            {
                GetComponent<Item>().Unfreeze();
            }

            PlayAtBomb(_wrenchSoundId, 8f);
            return true;
        }

        [Server]
        public bool ServerOpenPanel()
        {
            if (!State.CanOpenPanel(Now))
            {
                return false;
            }

            Apply(State.WithPanelOpen());
            return true;
        }

        [Server]
        public bool ServerCutWires()
        {
            if (!State.CanCutWires(Now))
            {
                return false;
            }

            Apply(State.Defused());
            PlayAtBomb(NukeAudioTrackIds.WireCut, 8f);
            PlayAtBomb(NukeAudioTrackIds.Disarmed, 20f);
            return true;
        }

        /// <summary>Server entry, not a ServerRpc: see <c>Nuke.Detonate</c> for why.</summary>
        [Server]
        public void Detonate()
        {
            if (_spent)
            {
                return;
            }

            Apply(State.Detonated());
            if (SubSystems.TryGet(out TileSubSystem tiles) && tiles.CurrentMap != null && tiles.QueryService != null
                && SubSystems.TryGet(out StructuralDamageSubSystem damage))
            {
                TileCoord tile = tiles.QueryService.WorldToTile(transform.position, tiles.CurrentMap.MapId);
                damage.ResolveBlast(tile, BlastYield, BlastFalloff);
            }

            GetComponent<Item>().Delete();
        }

        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            SyndicateBombState state = State;
            if (state.Spent)
            {
                return;
            }

            sections.Add(new ExamineSection(state.Armed
                ? ExamineText.Get(ExamineSyndicateBombKeys.Armed, ExamineSyndicateBombKeys.ArmedFallback,
                    NukeDeviceState.FormatTime(state.Remaining(Now)))
                : ExamineText.Get(ExamineSyndicateBombKeys.Safe, ExamineSyndicateBombKeys.SafeFallback,
                    NukeDeviceState.FormatTime(state.TimerSeconds))));
            sections.Add(new ExamineSection(state.Anchored
                ? ExamineText.Get(ExamineSyndicateBombKeys.Bolted, ExamineSyndicateBombKeys.BoltedFallback)
                : ExamineText.Get(ExamineSyndicateBombKeys.Loose, ExamineSyndicateBombKeys.LooseFallback)));
            if (state.Armed && state.PanelOpen)
            {
                sections.Add(new ExamineSection(ExamineText.Get(ExamineSyndicateBombKeys.PanelOpen,
                    ExamineSyndicateBombKeys.PanelOpenFallback)));
            }
        }

        private void Apply(SyndicateBombState state)
        {
            _armed = state.Armed;
            _panelOpen = state.PanelOpen;
            _anchored = state.Anchored;
            _spent = state.Spent;
            _deadline = state.Deadline;
            _timerSeconds = state.TimerSeconds;
        }

        private static bool IsTool(InteractionEvent e, string itemId)
        {
            Item tool = e.Source as Item;
            return tool != null && tool.Asset != null && tool.Asset.Id == itemId;
        }

        private static InteractionNarration Narration(string key, InteractionEvent e, ActionVisibility level = ActionVisibility.Close) =>
            new(key, (byte)level, InteractionNarration.ActorOf(e), InteractionNarration.TargetOf(e), null);

        private static Sprite BombIcon => InteractionIconLookup.Power;

        private sealed class TimerInteraction : IInteraction
        {
            private readonly SyndicateBomb _bomb;
            private readonly int _steps;
            public TimerInteraction(SyndicateBomb bomb, int steps) { _bomb = bomb; _steps = steps; }
            public int Priority => 10;
            public string GetName(InteractionEvent e)
            {
                float next = SyndicateBombState.ClampTimer(_bomb.State.TimerSeconds + _steps * SyndicateBombState.TimerStepSeconds);
                return $"Set timer to {NukeDeviceState.FormatTime(next)}";
            }
            public string GetGenericName() => _steps > 0 ? "SyndicateBombTimerUp" : "SyndicateBombTimerDown";
            public Sprite GetIcon(InteractionEvent e) => BombIcon;
            public bool CanInteract(InteractionEvent e) => _bomb != null && e.Source is Hand
                && (_steps > 0 ? _bomb.State.CanRaiseTimer : _bomb.State.CanLowerTimer) && InteractionExtensions.RangeCheck(e);
            public bool Start(InteractionEvent e, InteractionReference reference)
            {
                if (CanInteract(e))
                {
                    _bomb.ServerChangeTimer(_steps);
                }

                return false;
            }
        }

        private sealed class ArmInteraction : DelayedInteraction, IInteraction, INarratedInteraction
        {
            private readonly SyndicateBomb _bomb;
            public ArmInteraction(SyndicateBomb bomb) { _bomb = bomb; Delay = ArmSeconds; CheckInterval = 0.1f; }
            public int Priority => 40;
            public override string GetName(InteractionEvent e) => $"Arm bomb ({NukeDeviceState.FormatTime(_bomb.State.TimerSeconds)})";
            public override string GetGenericName() => "SyndicateBombArm";
            public override Sprite GetIcon(InteractionEvent e) => BombIcon;
            public override bool CanInteract(InteractionEvent e) => _bomb != null && e.Source is Hand
                && _bomb.State.CanArm && InteractionExtensions.RangeCheck(e);
            protected override void StartDelayed(InteractionEvent e, InteractionReference reference) => _bomb.ServerArm();
            public override void Cancel(InteractionEvent e, InteractionReference reference) { }
            public bool TryGetNarration(InteractionEvent e, out InteractionNarration narration)
            {
                narration = Narration(ActionMessageKeys.NukeArm, e, ActionVisibility.Public);
                return narration.Actor != null;
            }
        }

        private sealed class WrenchInteraction : DelayedInteraction, IInteraction, INarratedInteraction
        {
            private readonly SyndicateBomb _bomb;
            public WrenchInteraction(SyndicateBomb bomb) { _bomb = bomb; Delay = WrenchSeconds; CheckInterval = 0.1f; }
            public int Priority => 40;
            public override string GetName(InteractionEvent e) => _bomb.State.Anchored ? "Unbolt from floor" : "Bolt to floor";
            public override string GetGenericName() => "SyndicateBombWrench";
            public override Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Power;
            public override bool CanInteract(InteractionEvent e)
            {
                if (_bomb == null || !IsTool(e, Items.Wrench) || !InteractionExtensions.RangeCheck(e))
                {
                    return false;
                }

                SyndicateBombState state = _bomb.State;
                Draggable draggable = _bomb.GetComponent<Draggable>();
                return state.Anchored ? state.CanUnanchor : state.CanAnchor && (draggable == null || !draggable.Dragged);
            }
            protected override void StartDelayed(InteractionEvent e, InteractionReference reference) =>
                _bomb.ServerSetAnchored(!_bomb.State.Anchored);
            public override void Cancel(InteractionEvent e, InteractionReference reference) { }
            public bool TryGetNarration(InteractionEvent e, out InteractionNarration narration)
            {
                narration = Narration(_bomb.State.Anchored ? ActionMessageKeys.Unbolt : ActionMessageKeys.BoltDown, e);
                return narration.Actor != null;
            }
        }

        private sealed class OpenPanelInteraction : DelayedInteraction, IInteraction, INarratedInteraction
        {
            private readonly SyndicateBomb _bomb;
            public OpenPanelInteraction(SyndicateBomb bomb) { _bomb = bomb; Delay = OpenPanelSeconds; CheckInterval = 0.1f; }
            public int Priority => 40;
            public override string GetName(InteractionEvent e) => "Unscrew wiring panel";
            public override string GetGenericName() => "SyndicateBombOpenPanel";
            public override Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Open;
            public override bool CanInteract(InteractionEvent e) => _bomb != null && IsTool(e, Items.ScrewdriverBlue)
                && _bomb.State.CanOpenPanel(_bomb.Now) && InteractionExtensions.RangeCheck(e);
            protected override void StartDelayed(InteractionEvent e, InteractionReference reference) => _bomb.ServerOpenPanel();
            public override bool Start(InteractionEvent e, InteractionReference reference)
            {
                bool running = base.Start(e, reference);
                if (running)
                {
                    _bomb.PlayAtBomb(NukeAudioTrackIds.Panel, 8f);
                }

                return running;
            }
            public override void Cancel(InteractionEvent e, InteractionReference reference) { }
            public bool TryGetNarration(InteractionEvent e, out InteractionNarration narration)
            {
                narration = Narration(ActionMessageKeys.NukeOpenPanel, e);
                return narration.Actor != null;
            }
        }

        private sealed class CutWiresInteraction : DelayedInteraction, IInteraction, INarratedInteraction
        {
            private readonly SyndicateBomb _bomb;
            public CutWiresInteraction(SyndicateBomb bomb) { _bomb = bomb; Delay = CutWiresSeconds; CheckInterval = 0.1f; }
            public int Priority => 40;
            public override string GetName(InteractionEvent e) => "Cut the wires (defuse)";
            public override string GetGenericName() => "SyndicateBombCutWires";
            public override Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Power;
            public override bool CanInteract(InteractionEvent e) => _bomb != null && IsTool(e, Items.Wirecutters)
                && _bomb.State.CanCutWires(_bomb.Now) && InteractionExtensions.RangeCheck(e);
            protected override void StartDelayed(InteractionEvent e, InteractionReference reference) => _bomb.ServerCutWires();
            public override void Cancel(InteractionEvent e, InteractionReference reference) { }
            public bool TryGetNarration(InteractionEvent e, out InteractionNarration narration)
            {
                narration = Narration(ActionMessageKeys.NukeCutWires, e, ActionVisibility.Public);
                return narration.Actor != null;
            }
        }
    }
}
