using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.ActionMessages;
using SS3D.Systems.Area;
using SS3D.Systems.Audio;
using SS3D.Systems.Comms;
using SS3D.Systems.Entities;
using SS3D.Systems.Examine;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Tile;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// The station's nuclear device (<c>antagonist-content.md</c> §6). Load the authentication disk, arm it, and
    /// it detonates when the countdown runs out unless someone opens its panel with a screwdriver and cuts the
    /// wires. Arming is announced station-wide; examine shows the disk, the clock and the panel. The rules live
    /// in <see cref="NukeDeviceState"/>; this is the server adapter and the synced readout.
    /// </summary>
    /// <remarks>
    /// The loaded disk is not stored in a container: it sits in a slot on top of the device, kinematic and
    /// still selectable. Picking it up (or "Eject disk") takes it out; the server notices any move away from
    /// the slot, including an admin <c>disk</c> teleport, and clears the slot.
    /// </remarks>
    public class Nuke : InteractionSource, IInteractionTarget, IExamineContentProvider
    {
        public const float LoadDiskSeconds = 2f;
        public const float ArmSeconds = 3f;
        public const float OpenPanelSeconds = 5f;
        public const float CutWiresSeconds = 10f;

        /// <summary>A seated disk further than this from its slot has been taken out.</summary>
        private const float SlotTolerance = 0.3f;

        private static int? _countdownOverride;

        [SyncVar] private bool _diskLoaded;
        [SyncVar] private bool _armed;
        [SyncVar] private bool _panelOpen;
        [SyncVar] private bool _spent;
        [SyncVar] private double _deadline;

        // Server only.
        private NukeCard _disk;
        private Vector3 _slotPosition;
        private Quaternion _slotRotation;
        private string _armedByCkey;
        private bool _alarmSent;

        // Client only: server time of the next countdown beep.
        private double _nextTickAt;

        /// <summary>Seconds from arming to detonation: the test override, else <c>-nukeops-nuke-seconds=</c>, else 3 minutes.</summary>
        public static float CountdownSeconds => _countdownOverride ?? NukeOpsProfile.NukeSecondsFrom(Environment.GetCommandLineArgs());

        /// <summary>Tests: shortens the countdown; null restores the default.</summary>
        public static void SetCountdownSecondsForTesting(int? seconds) => _countdownOverride = seconds;

        public NukeDeviceState State => new(_diskLoaded, _armed, _panelOpen, _spent, _deadline);

        /// <summary>Server: the disk sitting in the slot, if any.</summary>
        public NukeCard LoadedDisk => _disk;

        public float Remaining => State.Remaining(Now);

        private double Now
        {
            get
            {
                FishNet.Managing.Timing.TimeManager clock = NetworkObject != null ? NetworkObject.TimeManager : null;
                return clock != null ? clock.TicksToTime(clock.Tick) : 0d;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            SceneRegistry<Nuke>.Register(this);
        }

        public override void OnStopServer()
        {
            SceneRegistry<Nuke>.Unregister(this);
            base.OnStopServer();
        }

        public override void Update()
        {
            base.Update();
            if (!IsSpawned)
            {
                return;
            }

            if (IsClient)
            {
                TickCountdownBeep();
            }

            if (!IsServer)
            {
                return;
            }

            KeepDiskSeated();
            if (State.AlarmDue(Now, _alarmSent))
            {
                _alarmSent = true;
                if (SubSystems.TryGet(out CommsSubSystem comms))
                {
                    comms.SendStationCue(NukeAudioTrackIds.Alarm);
                }
            }

            if (State.ShouldDetonate(Now))
            {
                Detonate();
            }
        }

        /// <summary>Client: the device beeps while armed, faster toward zero, derived from the synced deadline.</summary>
        private void TickCountdownBeep()
        {
            NukeDeviceState state = State;
            double now = Now;
            if (!state.Armed || state.Spent)
            {
                _nextTickAt = 0d;
                return;
            }

            if (now < _nextTickAt)
            {
                return;
            }

            _nextTickAt = now + NukeDeviceState.TickInterval(state.Remaining(now));
            if (SubSystems.TryGet(out AudioSubSystem audio))
            {
                audio.PlayAudioSourceLocal(AudioType.Sfx, NukeAudioTrackIds.Tick, transform.position, NetworkObject,
                    volume: 0.5f, minRange: 1f, maxRange: 12f);
            }
        }

        /// <summary>Server: a clip at the device for everyone within <paramref name="maxRange"/>.</summary>
        private void PlayAtDevice(string clipId, float maxRange, float volume = 0.7f)
        {
            if (IsServer && SubSystems.TryGet(out AudioSubSystem audio))
            {
                audio.PlayAudioSource(AudioType.Sfx, clipId, transform.position, NetworkObject,
                    volume: volume, minRange: 1f, maxRange: maxRange);
            }
        }

        /// <summary>
        /// Server entry. Deliberately not a <c>ServerRpc</c>: interaction code runs on the server, and FishNet
        /// drops a ServerRpc invoked from server code on a dedicated server (the generated writer returns when
        /// <c>!IsClient</c>), so detonation silently did nothing there. The countdown calls it on expiry.
        /// </summary>
        [Server]
        public void Detonate()
        {
            if (_spent)
            {
                return;
            }

            Apply(State.Detonated());
            // Ends the round, regardless of how many objectives were completed. The rules resolve inside
            // EndRound, so the event below is only for listeners that don't decide the outcome.
            SubSystems.Get<GamemodeSubSystem>().EndRound(RoundEndRequest.Detonation);
            new NukeDetonateEvent(this, _armedByCkey).Invoke(this);
        }

        /// <summary>Server: seats <paramref name="disk"/> in the slot, out of whatever hand or bag holds it.</summary>
        [Server]
        public bool ServerLoadDisk(NukeCard disk)
        {
            if (disk == null || !State.CanLoadDisk)
            {
                return false;
            }

            if (disk.Container != null)
            {
                disk.Container.RemoveItem(disk);
                if (disk.Container != null)
                {
                    return false;
                }
            }

            disk.GiveOwnership(null);
            Bounds bounds = DeviceBounds();
            ItemUtility.Place(disk, new Vector3(bounds.center.x, bounds.max.y, bounds.center.z), transform.rotation);
            _slotPosition = disk.transform.position;
            _slotRotation = disk.transform.rotation;
            _disk = disk;
            SetDiskKinematic(true);
            Apply(State.WithDisk(true));
            PlayAtDevice(NukeAudioTrackIds.DiskInsert, 8f);
            return true;
        }

        /// <summary>Server: hands the seated disk to <paramref name="hand"/> and clears the slot at once.</summary>
        [Server]
        public bool ServerEjectDisk(Hand hand)
        {
            NukeCard disk = _disk;
            if (disk == null || hand == null || !State.CanEjectDisk)
            {
                return false;
            }

            hand.Pickup(disk);
            if (disk.Container == null)
            {
                return false;
            }

            _disk = null;
            Apply(State.WithDisk(false));
            return true;
        }

        /// <summary>Server: arms the device. The countdown starts and the whole station hears where it is.</summary>
        [Server]
        public bool ServerArm(string ckey)
        {
            if (!State.CanArm)
            {
                return false;
            }

            float seconds = CountdownSeconds;
            Apply(State.ArmedAt(Now, seconds));
            _armedByCkey = ckey;
            _alarmSent = false;
            PlayAtDevice(NukeAudioTrackIds.ArmBeep, 10f);
            if (SubSystems.TryGet(out CommsSubSystem comms))
            {
                comms.SendAnnouncement(ArmAnnouncement(LocationName(), seconds), NukeAudioTrackIds.Armed);
            }

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
            _armedByCkey = null;
            PlayAtDevice(NukeAudioTrackIds.WireCut, 8f);
            PlayAtDevice(NukeAudioTrackIds.Disarmed, 20f);
            return true;
        }

        public static string ArmAnnouncement(string location, float seconds) =>
            $"A nuclear device has been armed in {location}. Detonation in {NukeDeviceState.FormatTime(seconds)}.";

        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent) =>
            new IInteraction[]
            {
                new LoadDiskInteraction(this), new EjectDiskInteraction(this), new ArmInteraction(this),
                new OpenPanelInteraction(this), new CutWiresInteraction(this),
            };

        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            NukeDeviceState state = State;
            if (state.Spent)
            {
                return;
            }

            sections.Add(new ExamineSection(state.DiskLoaded
                ? ExamineText.Get(ExamineNukeKeys.DiskLoaded, ExamineNukeKeys.DiskLoadedFallback)
                : ExamineText.Get(ExamineNukeKeys.DiskEmpty, ExamineNukeKeys.DiskEmptyFallback)));
            sections.Add(new ExamineSection(state.Armed
                ? ExamineText.Get(ExamineNukeKeys.Armed, ExamineNukeKeys.ArmedFallback, NukeDeviceState.FormatTime(state.Remaining(Now)))
                : ExamineText.Get(ExamineNukeKeys.Safe, ExamineNukeKeys.SafeFallback)));
            if (state.Armed && state.PanelOpen)
            {
                sections.Add(new ExamineSection(ExamineText.Get(ExamineNukeKeys.PanelOpen, ExamineNukeKeys.PanelOpenFallback)));
            }
        }

        private void Apply(NukeDeviceState state)
        {
            _diskLoaded = state.DiskLoaded;
            _armed = state.Armed;
            _panelOpen = state.PanelOpen;
            _spent = state.Spent;
            _deadline = state.Deadline;
        }

        /// <summary>Server: clears the slot once the disk leaves it (picked up, teleported, despawned).</summary>
        private void KeepDiskSeated()
        {
            if (_disk == null)
            {
                if (_diskLoaded)
                {
                    Apply(State.WithDisk(false));
                }

                return;
            }

            bool seated = _disk.IsSpawned && _disk.Container == null
                && Vector3.Distance(_disk.transform.position, _slotPosition) <= SlotTolerance;
            if (!seated)
            {
                if (_disk.IsSpawned && _disk.Container == null)
                {
                    SetDiskKinematic(false);
                }

                _disk = null;
                Apply(State.WithDisk(false));
                return;
            }

            // Leaving the hand unfreezes the item; hold it in the slot.
            Rigidbody body = _disk.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                SetDiskKinematic(true);
                ItemUtility.SetWorldPose(_disk.transform, _slotPosition, _slotRotation);
            }
        }

        private void SetDiskKinematic(bool kinematic)
        {
            if (_disk == null)
            {
                return;
            }

            if (kinematic)
            {
                Rigidbody body = _disk.GetComponent<Rigidbody>();
                if (body != null)
                {
                    if (!body.isKinematic)
                    {
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                    }

                    body.isKinematic = true;
                }
            }
            else
            {
                _disk.Unfreeze();
            }
        }

        private Bounds DeviceBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private string LocationName()
        {
            if (SubSystems.TryGet(out TileSubSystem tiles) && tiles.CurrentMap != null && tiles.QueryService != null
                && SubSystems.TryGet(out AreaSubSystem areas)
                && areas.TryGetAreaForTile(tiles.QueryService.WorldToTile(transform.position, tiles.CurrentMap.MapId), out AreaRecord area)
                && !string.IsNullOrWhiteSpace(area.DisplayName))
            {
                return area.DisplayName.Trim();
            }

            return "the station";
        }

        private static Sprite NukeIcon => Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Nuke);

        private static bool IsTool(InteractionEvent e, string itemId)
        {
            Item tool = e.Source as Item;
            return tool != null && tool.Asset != null && tool.Asset.Id == itemId;
        }

        private static string CkeyOf(InteractionEvent e)
        {
            Entity entity = e.Source is Component source ? source.GetComponentInParent<Entity>() : null;
            return entity != null && SubSystems.TryGet(out PlayerSubSystem players) ? players.GetCkey(entity.Owner) : null;
        }

        // Arming and cutting are seen by anyone in sight; loading and unscrewing only up close.
        private static InteractionNarration Narration(string key, InteractionEvent e, ActionVisibility level = ActionVisibility.Close) =>
            new(key, (byte)level, InteractionNarration.ActorOf(e), InteractionNarration.TargetOf(e), null);

        private sealed class LoadDiskInteraction : DelayedInteraction, IInteraction, INarratedInteraction
        {
            private readonly Nuke _nuke;
            public LoadDiskInteraction(Nuke nuke) { _nuke = nuke; Delay = LoadDiskSeconds; CheckInterval = 0.1f; }
            public int Priority => 40;
            public override string GetName(InteractionEvent e) => "Load disk";
            public override string GetGenericName() => "NukeLoadDisk";
            public override Sprite GetIcon(InteractionEvent e) => NukeIcon;
            public override bool CanInteract(InteractionEvent e) => _nuke != null && e.Source is NukeCard
                && _nuke.State.CanLoadDisk && InteractionExtensions.RangeCheck(e);
            protected override void StartDelayed(InteractionEvent e, InteractionReference reference) =>
                _nuke.ServerLoadDisk(e.Source as NukeCard);
            public override void Cancel(InteractionEvent e, InteractionReference reference) { }
            public bool TryGetNarration(InteractionEvent e, out InteractionNarration narration)
            {
                narration = Narration(ActionMessageKeys.NukeLoadDisk, e);
                return narration.Actor != null;
            }
        }

        private sealed class EjectDiskInteraction : IInteraction
        {
            private readonly Nuke _nuke;
            public EjectDiskInteraction(Nuke nuke) => _nuke = nuke;
            public int Priority => 20;
            public string GetName(InteractionEvent e) => "Eject disk";
            public string GetGenericName() => "NukeEjectDisk";
            public Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Take;
            public bool CanInteract(InteractionEvent e) => _nuke != null && e.Source is Hand hand && hand.IsEmpty()
                && _nuke.State.CanEjectDisk && InteractionExtensions.RangeCheck(e);
            public bool Start(InteractionEvent e, InteractionReference reference)
            {
                if (CanInteract(e))
                {
                    _nuke.ServerEjectDisk((Hand)e.Source);
                }

                return false;
            }
        }

        private sealed class ArmInteraction : DelayedInteraction, IInteraction, INarratedInteraction
        {
            private readonly Nuke _nuke;
            public ArmInteraction(Nuke nuke) { _nuke = nuke; Delay = ArmSeconds; CheckInterval = 0.1f; }
            public int Priority => 40;
            public override string GetName(InteractionEvent e) => $"Arm nuke ({NukeDeviceState.FormatTime(CountdownSeconds)})";
            public override string GetGenericName() => "NukeArm";
            public override Sprite GetIcon(InteractionEvent e) => NukeIcon;
            public override bool CanInteract(InteractionEvent e) => _nuke != null && e.Source is Hand
                && _nuke.State.CanArm && InteractionExtensions.RangeCheck(e);
            protected override void StartDelayed(InteractionEvent e, InteractionReference reference) =>
                _nuke.ServerArm(CkeyOf(e));
            public override void Cancel(InteractionEvent e, InteractionReference reference) { }
            public bool TryGetNarration(InteractionEvent e, out InteractionNarration narration)
            {
                narration = Narration(ActionMessageKeys.NukeArm, e, ActionVisibility.Public);
                return narration.Actor != null;
            }
        }

        private sealed class OpenPanelInteraction : DelayedInteraction, IInteraction, INarratedInteraction
        {
            private readonly Nuke _nuke;
            public OpenPanelInteraction(Nuke nuke) { _nuke = nuke; Delay = OpenPanelSeconds; CheckInterval = 0.1f; }
            public int Priority => 40;
            public override string GetName(InteractionEvent e) => "Unscrew wiring panel";
            public override string GetGenericName() => "NukeOpenPanel";
            public override Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Open;
            public override bool CanInteract(InteractionEvent e) => _nuke != null && IsTool(e, Items.ScrewdriverBlue)
                && _nuke.State.CanOpenPanel(_nuke.Now) && InteractionExtensions.RangeCheck(e);
            protected override void StartDelayed(InteractionEvent e, InteractionReference reference) => _nuke.ServerOpenPanel();
            public override bool Start(InteractionEvent e, InteractionReference reference)
            {
                // The screws come out over the whole step, so the tool sounds as it begins.
                bool running = base.Start(e, reference);
                if (running)
                {
                    _nuke.PlayAtDevice(NukeAudioTrackIds.Panel, 8f);
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
            private readonly Nuke _nuke;
            public CutWiresInteraction(Nuke nuke) { _nuke = nuke; Delay = CutWiresSeconds; CheckInterval = 0.1f; }
            public int Priority => 40;
            public override string GetName(InteractionEvent e) => "Cut the wires (defuse)";
            public override string GetGenericName() => "NukeCutWires";
            public override Sprite GetIcon(InteractionEvent e) => InteractionIconLookup.Power;
            public override bool CanInteract(InteractionEvent e) => _nuke != null && IsTool(e, Items.Wirecutters)
                && _nuke.State.CanCutWires(_nuke.Now) && InteractionExtensions.RangeCheck(e);
            protected override void StartDelayed(InteractionEvent e, InteractionReference reference) => _nuke.ServerCutWires();
            public override void Cancel(InteractionEvent e, InteractionReference reference) { }
            public bool TryGetNarration(InteractionEvent e, out InteractionNarration narration)
            {
                narration = Narration(ActionMessageKeys.NukeCutWires, e, ActionVisibility.Public);
                return narration.Actor != null;
            }
        }
    }
}
