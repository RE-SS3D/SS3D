using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using Coimbra.Services.Events;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Logging;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Roles.Messages;
using SS3D.Systems.Rounds;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using System;
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// Job slots, lobby job/antagonist preferences and job assignment (lobby.md §3–5). Round start
    /// resolves every spawning player in one pass; latejoiners take their highest ranked open job.
    /// </summary>
    public class RoleSubSystem : NetworkSubSystem
    {
        [SerializeField] private RolesAvailable _rolesAvailable;
        [SerializeField] private string _fallbackRole = "Assistant";

        [SyncObject] private readonly SyncList<JobSlotInfo> _jobs = new();

        private readonly Dictionary<string, RoleData> _roleData = new();
        private readonly Dictionary<string, Dictionary<string, JobPriority>> _preferences = new();
        private readonly Dictionary<string, HashSet<string>> _antagonistOptIns = new();
        private readonly Dictionary<Player, RoleData> _rolePlayers = new();
        private readonly Dictionary<string, RoleData> _antagonistRoles = new();
        private readonly List<Player> _operatives = new();
        private readonly System.Random _random = new();

        /// <summary>Replicated job list in authoring order.</summary>
        public IReadOnlyList<JobSlotInfo> Jobs => _jobs;

        /// <summary>Server: this round's drawn Nuke Ops squad, in draw order. Empty outside a Nuke Ops round.</summary>
        public IReadOnlyList<Player> Operatives => _operatives;

        /// <summary>Server: the player was drawn into this round's squad.</summary>
        public bool IsOperative(Player player) => player != null && _operatives.Contains(player);

        #region Setup
        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerManager.RegisterBroadcast<JobPreferencesMessage>(HandleJobPreferences);
            AddHandle(OnlinePlayersChanged.AddListener(HandleOnlinePlayersChanged));
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));

            GetAvailableRoles();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            ServerManager.UnregisterBroadcast<JobPreferencesMessage>(HandleJobPreferences);
        }

        /// <summary>
        /// Builds the replicated job list from the available roles.
        /// </summary>
        [Server]
        private void GetAvailableRoles()
        {
            if (_rolesAvailable == null)
            {
                Log.Error(this, "Initial Available Roles not set!");
                return;
            }

            _jobs.Clear();
            _roleData.Clear();
            _antagonistRoles.Clear();
            foreach (RolesData role in _rolesAvailable.Roles)
            {
                if (role.Data == null || _roleData.ContainsKey(role.Data.Name))
                {
                    continue;
                }

                _roleData[role.Data.Name] = role.Data;
                // Antagonist roles are assigned by a gamemode, never chosen in the lobby.
                if (role.Data.IsAntagonist)
                {
                    _antagonistRoles[role.Data.AntagonistCategory] = role.Data;
                    continue;
                }

                _jobs.Add(new JobSlotInfo
                {
                    Name = role.Data.Name,
                    Department = (byte)IdAccessSubSystem.DepartmentForRole(role.Data.Name),
                    Slots = role.AvailableRoles,
                });
            }

            if (!_roleData.ContainsKey(_fallbackRole))
            {
                Log.Error(this, "Fallback role {role} is not in the available roles", Logs.ServerOnly, _fallbackRole);
            }
        }
        #endregion

        #region Preferences
        [Server]
        private void HandleJobPreferences(NetworkConnection sender, JobPreferencesMessage message)
        {
            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(sender);
            if (player == null)
            {
                return;
            }

            Dictionary<string, JobPriority> priorities = new();
            int count = Mathf.Min(message.Jobs?.Length ?? 0, message.Priorities?.Length ?? 0);
            for (int i = 0; i < count; i++)
            {
                string job = message.Jobs[i];
                byte priority = message.Priorities[i];
                if (job != null && _roleData.TryGetValue(job, out RoleData data) && !data.IsAntagonist
                    && priority <= (byte)JobPriority.High)
                {
                    priorities[job] = (JobPriority)priority;
                }
            }

            HashSet<string> antagonists = new();
            foreach (string key in message.AntagonistOptIns ?? Array.Empty<string>())
            {
                if (AntagonistCategories.IsKnown(key))
                {
                    antagonists.Add(key);
                }
            }

            _preferences[player.Ckey] = priorities;
            _antagonistOptIns[player.Ckey] = antagonists;
            RefreshJobCounts();
        }

        /// <summary>
        /// Whether the player opted into consideration for an antagonist category. Gamemodes
        /// choose among opted-in players; opting in guarantees nothing. A player who never sent
        /// preferences is opted into every category (round-end loop Decision 22), as the lobby list starts ticked.
        /// </summary>
        public bool IsAntagonistOptedIn(string ckey, string category) =>
            ckey != null && AntagonistCategories.IsOptedIn(_antagonistOptIns.GetValueOrDefault(ckey), category);

        /// <summary>Automation: opts a player into an antagonist category without a lobby preferences message.</summary>
        [Server]
        public void OptIntoAntagonistForTesting(string ckey, string category)
        {
            if (!_antagonistOptIns.TryGetValue(ckey, out HashSet<string> set))
            {
                _antagonistOptIns[ckey] = set = new HashSet<string>();
            }

            set.Add(category);
        }

        public bool TryGetRole(Player player, out RoleData role)
        {
            role = null;
            return player != null && _rolePlayers.TryGetValue(player, out role);
        }
        #endregion

        #region Event Handlers
        [Server]
        private void HandleOnlinePlayersChanged(ref EventContext context, in OnlinePlayersChanged e)
        {
            if (!e.AsServer)
            {
                return;
            }

            RefreshJobCounts();
        }

        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            if (e.RoundState != RoundState.Stopped)
            {
                return;
            }

            _rolePlayers.Clear();
            _operatives.Clear();
            RefreshJobCounts();
        }
        #endregion

        #region Assignment
        /// <summary>
        /// One resolution pass over the players spawning at round start. Replaces earlier assignments.
        /// A Nuke Ops round draws its squad first; drawn players skip job resolution (Decision 11).
        /// </summary>
        [Server]
        public void ResolveRoundStartRoles(IReadOnlyList<Player> players)
        {
            _rolePlayers.Clear();
            _operatives.Clear();
            RefreshJobCounts();

            Dictionary<string, Player> byCkey = new();
            foreach (Player player in players)
            {
                if (player != null)
                {
                    byCkey[player.Ckey] = player;
                }
            }

            foreach (string ckey in DrawSquad(byCkey.Keys.ToList()))
            {
                Player operative = byCkey[ckey];
                _operatives.Add(operative);
                Assign(operative, _antagonistRoles[AntagonistCategories.NuclearOperative].Name);
                byCkey.Remove(ckey);
            }

            Dictionary<string, IReadOnlyDictionary<string, JobPriority>> preferences = new();
            foreach (string ckey in byCkey.Keys)
            {
                if (_preferences.TryGetValue(ckey, out Dictionary<string, JobPriority> priorities))
                {
                    preferences[ckey] = priorities;
                }
            }

            Dictionary<string, string> assignments =
                JobResolver.Resolve(byCkey.Keys.ToList(), preferences, _jobs, _fallbackRole, _random);
            foreach (KeyValuePair<string, string> assignment in assignments)
            {
                Assign(byCkey[assignment.Key], assignment.Value);
            }

            RefreshJobCounts();
        }

        /// <summary>
        /// Draws the squad when the configured mode runs Nuke Ops; tells the round to run as Extended when
        /// too few players opted in. Latejoiners never reach this.
        /// </summary>
        [Server]
        private IReadOnlyList<string> DrawSquad(IReadOnlyList<string> ckeys)
        {
            RoundSubSystem rounds = SubSystems.Get<RoundSubSystem>();
            if (!rounds.ModeDrawsOperatives)
            {
                return Array.Empty<string>();
            }

            if (!_antagonistRoles.ContainsKey(AntagonistCategories.NuclearOperative))
            {
                Log.Error(this, "No antagonist role for {category} in the available roles; running Extended",
                    Logs.ServerOnly, AntagonistCategories.NuclearOperative);
                rounds.FallBackToExtended("No Nuclear Operative role is configured. This round runs as Extended.");
                return Array.Empty<string>();
            }

            ServerSettings settings = rounds.SettingsForServer;
            if (!SquadDraw.HasEnoughReady(settings.Mode, ckeys.Count, settings.MinimumReadyForNukeOps))
            {
                rounds.FallBackToExtended(SquadDraw.TooFewReadyNotice(ckeys.Count, settings.MinimumReadyForNukeOps));
                return Array.Empty<string>();
            }

            SquadDrawResult result = SquadDraw.Draw(ckeys,
                ckey => IsAntagonistOptedIn(ckey, AntagonistCategories.NuclearOperative),
                settings.MinimumOperatives, _random);
            if (result.FellBack)
            {
                rounds.FallBackToExtended(SquadDraw.FallbackNotice(settings.Mode, result));
                return Array.Empty<string>();
            }

            Log.Information(this, "Drew {count} operatives from {optedIn} opt-ins", Logs.ServerOnly,
                result.Squad.Count, result.OptedIn);
            return result.Squad;
        }

        [Server]
        private void AssignLateJoinRole(Player player)
        {
            _preferences.TryGetValue(player.Ckey, out Dictionary<string, JobPriority> priorities);
            Assign(player, JobResolver.ResolveLateJoin(priorities, _jobs, _fallbackRole, _random));
            RefreshJobCounts();
        }

        [Server]
        private void Assign(Player player, string job)
        {
            if (_roleData.TryGetValue(job, out RoleData role))
            {
                _rolePlayers[player] = role;
                Log.Information(this, "{ckey} assigned job {job}", Logs.ServerOnly, player.Ckey, job);
            }
        }

        /// <summary>
        /// Recomputes replicated assigned/interested counts; only changed rows are resent.
        /// </summary>
        [Server]
        private void RefreshJobCounts()
        {
            HashSet<string> online = new(SubSystems.Get<PlayerSubSystem>().OnlinePlayers
                .Where(player => player != null)
                .Select(player => player.Ckey));

            for (int i = 0; i < _jobs.Count; i++)
            {
                JobSlotInfo job = _jobs[i];
                int assigned = _rolePlayers.Values.Count(role => role.Name == job.Name);
                int interested = _preferences.Count(entry => online.Contains(entry.Key)
                    && entry.Value.TryGetValue(job.Name, out JobPriority priority)
                    && priority != JobPriority.Never);

                if (assigned == job.Assigned && interested == job.Interested)
                {
                    continue;
                }

                job.Assigned = assigned;
                job.Interested = interested;
                _jobs[i] = job;
            }
        }
        #endregion

        /// <summary>
        /// Gives the spawned player the items of their job, assigning one first for latejoiners.
        /// </summary>
        /// <param name="entity">The player that will receive the items</param>
        [Server]
        public void GiveRoleLoadoutToPlayer(Entity entity)
        {
            Player player = entity.Mind?.player;
            if (player == null)
            {
                return;
            }

            if (!_rolePlayers.ContainsKey(player))
            {
                AssignLateJoinRole(player);
            }

            if (!_rolePlayers.TryGetValue(player, out RoleData roleData))
            {
                return;
            }

            Log.Information(this, entity.Ckey + " embarked with role " + roleData.Name);
            SpawnIdentificationItems(entity, roleData);

            if (roleData.Loadout != null)
            {
                SpawnLoadoutItems(entity, roleData.Loadout);
            }
        }

        /// <summary>
        /// Spawn the player's PDA and IDCard with the proper permissions
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="role"></param>
        private void SpawnIdentificationItems(Entity entity, RoleData role)
        {
            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            HumanInventory inventory = entity.GetComponent<HumanInventory>();
            IdAccessSubSystem idAccess = SubSystems.Get<IdAccessSubSystem>();

            if (!inventory.TryGetTypeContainer(ContainerType.Identification, 0, out AttachedContainer container)) return;

            Item idCardItem = itemSystem.SpawnItem(role.IDCardAsset.Id, Vector3.zero, Quaternion.identity);
            IDCard idCard = (IDCard)idCardItem;

            CrewRecord crewRecord = idAccess.CreateCrewRecord(
                entity.Mind.DisplayName,
                role.Name,
                IdAccessSubSystem.DepartmentForRole(role.Name),
                role.StartingAccess);

            idAccess.BindIdCard(idCard, crewRecord.Id);

            Item pdaItem = SpawnItemInSlot(role.PDAAsset, true, container);
            PDA pda = (PDA)pdaItem;
            pda.StartingIDCard = idCardItem;
            pda.EnsureStartingIdCardInserted();

            Log.Information(this, "Bound crew record " + crewRecord.Id + " to ID card of " + entity.Ckey);
        }

        /// <summary>
        /// Spawn all the role items for the player
        /// </summary>
        /// <param name="entity">The player that will receive the items</param>
        /// <param name="loadout">The loadout of items he will receive</param>
        private void SpawnLoadoutItems(Entity entity, RoleLoadout loadout)
        {
            Hands hands = entity.GetComponent<Hands>();
            HumanInventory inventory = entity.GetComponent<HumanInventory>();

            Dictionary<ContainerType, AttachedContainer> containers = new Dictionary<ContainerType, AttachedContainer>();

            foreach (AttachedContainer inventoryContainer in inventory.Containers)
            {
                if (inventoryContainer.ContainerType == ContainerType.Hand)
                {
                    continue;
                }

                containers.Add(inventoryContainer.ContainerType, inventoryContainer);
            }

            // Suit storage only accepts a tank once the suit is on.
            foreach (KeyValuePair<ContainerType, ObjectAssetReference> itemToEquip in loadout.EquipmentAssets
                .OrderBy(equipment => equipment.Key == ContainerType.SuitStorage))
            {
                if (!itemToEquip.Value)
                {
                    continue;
                }

                if (containers.TryGetValue(itemToEquip.Key, out AttachedContainer container))
                {
                    SpawnItemInSlot(itemToEquip.Value, true, container);
                }
            }

            if (containers.TryGetValue(ContainerType.Pocket, out AttachedContainer pocket))
            {
                foreach (ObjectAssetReference pocketAsset in loadout.PocketAssets)
                {
                    SpawnItemInSlot(pocketAsset, true, pocket);
                }
            }

            // By side, not list order: a two-handed rifle may only sit in its required hand.
            AttachedContainer HandContainer(HandSide side) =>
                hands.PlayerHands.FirstOrDefault(hand => hand != null && hand.Side == side)?.Container;

            if (loadout.HandLeftAsset && HandContainer(HandSide.Left) is { } left)
            {
                SpawnItemInSlot(loadout.HandLeftAsset, true, left);
            }

            if (loadout.HandRightAsset && HandContainer(HandSide.Right) is { } right)
            {
                SpawnItemInSlot(loadout.HandRightAsset, true, right);
            }

            inventory.TriggerInventorySetup();
        }

        /// <summary>
        /// Spawns an item inside a container slot after checking for boolean
        /// </summary>
        /// <param name="assetReference"></param>
        /// <param name="shouldSpawn">Condition indicating if the item should be spawned</param>
        /// <param name="container">Container the item will be spawned in</param>
        private Item SpawnItemInSlot(ObjectAssetReference assetReference, bool shouldSpawn, AttachedContainer container)
        {
            if (!shouldSpawn)
            {
                return null;
            }

            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            GameObject gameObjectToSpawn = Assets.Get<GameObject>(assetReference);
            return itemSystem.SpawnItemInContainer(gameObjectToSpawn, container);
        }
    }
}