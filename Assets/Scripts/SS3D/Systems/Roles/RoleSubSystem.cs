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
    /// Job slots, lobby job/antagonist preferences and job assignment (lobby.md §3–5). The preferences are the
    /// selected character's, sent by the client. Round start resolves every spawning player in one pass;
    /// latejoiners take their highest ranked open job. A player with no open job becomes the fallback job or
    /// stays in the lobby, as their character says.
    /// </summary>
    public class RoleSubSystem : NetworkSubSystem
    {
        [SerializeField] private RolesAvailable _rolesAvailable;
        [SerializeField] private string _fallbackRole = "Assistant";

        [SyncObject] private readonly SyncList<JobSlotInfo> _jobs = new();

        /// <summary>The round's crew manifest, sent to every client: names and crew jobs only (Decision 10).</summary>
        [SyncObject] private readonly SyncList<CrewManifestEntry> _manifest = new();
        private readonly CrewManifest _manifestBook = new();

        private readonly Dictionary<string, RoleData> _roleData = new();
        private readonly Dictionary<string, Dictionary<string, JobPriority>> _preferences = new();
        private readonly Dictionary<string, HashSet<string>> _antagonistOptIns = new();
        private readonly Dictionary<string, HashSet<string>> _antagonistOptOuts = new();
        private readonly Dictionary<string, JobFallback> _fallbacks = new();
        private readonly Dictionary<Player, RoleData> _rolePlayers = new();

        /// <summary>Loadouts whose <see cref="RoleLoadout.FirstHolderContents"/> went out this round.</summary>
        private readonly HashSet<RoleLoadout> _firstHolderContentsGiven = new();
        private readonly Dictionary<string, RoleData> _antagonistRoles = new();
        private readonly List<Player> _operatives = new();
        private readonly System.Random _random = new();

        /// <summary>Replicated job list in authoring order.</summary>
        public IReadOnlyList<JobSlotInfo> Jobs => _jobs;

        /// <summary>
        /// Replicated crew manifest: everyone given a crew job this round, dead or alive, in spawn order. Leaves out
        /// operatives and ghosts. Empty between rounds.
        /// </summary>
        public IReadOnlyList<CrewManifestEntry> Manifest => _manifest;

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

            HashSet<string> optIns = KnownCategories(message.AntagonistOptIns);
            HashSet<string> optOuts = KnownCategories(message.AntagonistOptOuts);
            optOuts.ExceptWith(optIns);

            _preferences[player.Ckey] = priorities;
            _antagonistOptIns[player.Ckey] = optIns;
            _antagonistOptOuts[player.Ckey] = optOuts;
            _fallbacks[player.Ckey] = message.Fallback == (byte)JobFallback.ReturnToLobby
                ? JobFallback.ReturnToLobby
                : JobFallback.Assistant;
            RefreshJobCounts();
        }

        private static HashSet<string> KnownCategories(string[] keys)
        {
            HashSet<string> result = new();
            foreach (string key in keys ?? Array.Empty<string>())
            {
                if (AntagonistCategories.IsKnown(key))
                {
                    result.Add(key);
                }
            }

            return result;
        }

        /// <summary>
        /// Whether the player opted into consideration for an antagonist category. Gamemodes choose among
        /// opted-in players; opting in guarantees nothing. A category the selected character never set takes
        /// <see cref="ServerSettings.AntagonistOptInDefault"/>: off, except on the <c>-nukeops</c> profile.
        /// </summary>
        public bool IsAntagonistOptedIn(string ckey, string category) =>
            ckey != null && AntagonistCategories.IsOptedIn(_antagonistOptIns.GetValueOrDefault(ckey),
                _antagonistOptOuts.GetValueOrDefault(ckey), category, AntagonistOptInDefault);

        private static bool AntagonistOptInDefault =>
            SubSystems.TryGet(out ServerSettingsService settings) && settings.Current.AntagonistOptInDefault;

        /// <summary>Server: what the player's selected character does when none of its jobs is open.</summary>
        public JobFallback FallbackFor(string ckey) =>
            ckey != null && _fallbacks.TryGetValue(ckey, out JobFallback fallback) ? fallback : JobFallback.Assistant;

        /// <summary>Automation: opts a player into an antagonist category without a lobby preferences message.</summary>
        [Server]
        public void OptIntoAntagonistForTesting(string ckey, string category)
        {
            if (!_antagonistOptIns.TryGetValue(ckey, out HashSet<string> set))
            {
                _antagonistOptIns[ckey] = set = new HashSet<string>();
            }

            set.Add(category);
            _antagonistOptOuts.GetValueOrDefault(ckey)?.Remove(category);
        }

        /// <summary>
        /// The job's description (lobby.md §2), read from the local role assets so it needs no replication; empty
        /// when the job is unknown or has none.
        /// </summary>
        public string DescriptionOf(string job) => RoleDataOf(job)?.Description ?? string.Empty;

        /// <summary>
        /// The prefabs of the job's loadout equipment (worn slots, bag, ID, PDA), from the local role assets; the
        /// character window dresses its preview in the High job's clothing (lobby effort Decision 14). An unknown job
        /// gives the fallback job's; empty without role assets.
        /// </summary>
        public List<GameObject> LoadoutEquipmentOf(string job)
        {
            List<GameObject> prefabs = new();
            RoleData role = RoleDataOf(job);
            if (role == null)
            {
                role = RoleDataOf(_fallbackRole);
            }

            RoleLoadout loadout = role != null ? role.Loadout : null;
            if (loadout == null || loadout.EquipmentAssets == null)
            {
                return prefabs;
            }

            foreach (KeyValuePair<ContainerType, ObjectAssetReference> equipment in loadout.EquipmentAssets)
            {
                GameObject prefab = equipment.Value ? Assets.Get<GameObject>(equipment.Value) : null;
                if (prefab)
                {
                    prefabs.Add(prefab);
                }
            }

            return prefabs;
        }

        /// <summary>The job a player without a fit gets, uncapped (Assistant).</summary>
        public string FallbackJob => _fallbackRole;

        private RoleData RoleDataOf(string job)
        {
            if (_rolesAvailable == null || string.IsNullOrEmpty(job))
            {
                return null;
            }

            foreach (RolesData role in _rolesAvailable.Roles)
            {
                if (role.Data != null && role.Data.Name == job)
                {
                    return role.Data;
                }
            }

            return null;
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
            _firstHolderContentsGiven.Clear();
            _manifestBook.Clear();
            _manifest.Clear();
            RefreshJobCounts();
        }
        #endregion

        #region Assignment
        /// <summary>
        /// One resolution pass over the players spawning at round start. Replaces earlier assignments.
        /// A Nuke Ops round draws its squad first; drawn players skip job resolution (Decision 11).
        /// </summary>
        /// <returns>
        /// The players who stay in the lobby: no job of theirs was open and their character returns to the lobby
        /// (<see cref="JobFallback.ReturnToLobby"/>). They get no role and must not be spawned.
        /// </returns>
        [Server]
        public IReadOnlyList<Player> ResolveRoundStartRoles(IReadOnlyList<Player> players)
        {
            _rolePlayers.Clear();
            _operatives.Clear();
            _firstHolderContentsGiven.Clear();
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

            HashSet<string> returnsToLobby = new(byCkey.Keys.Where(ckey => FallbackFor(ckey) == JobFallback.ReturnToLobby));
            Dictionary<string, string> assignments =
                JobResolver.Resolve(byCkey.Keys.ToList(), preferences, _jobs, _fallbackRole, _random, returnsToLobby);
            List<Player> stayInLobby = new();
            foreach (KeyValuePair<string, Player> entry in byCkey)
            {
                if (assignments.TryGetValue(entry.Key, out string job))
                {
                    Assign(entry.Value, job);
                }
                else
                {
                    stayInLobby.Add(entry.Value);
                    Log.Information(this, "{ckey} has no open job and returns to the lobby", Logs.ServerOnly, entry.Key);
                }
            }

            RefreshJobCounts();
            return stayInLobby;
        }

        /// <summary>The notice a player sent back to the lobby reads (round start or a late join).</summary>
        public const string ReturnedToLobbyNotice =
            "None of your character's jobs was open, so you stayed in the lobby. Your character is set to return to the lobby "
            + "rather than join as Assistant.";

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

            ServerSettings settings = SubSystems.Get<ServerSettingsService>().Current;
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

        /// <summary>
        /// Server: gives a latejoiner their highest ranked open job before they spawn. False, with no role assigned,
        /// when none is open and their character returns to the lobby; the caller must not spawn them.
        /// </summary>
        [Server]
        public bool TryAssignLateJoinRole(Player player)
        {
            if (player == null)
            {
                return false;
            }

            if (_rolePlayers.ContainsKey(player))
            {
                return true;
            }

            _preferences.TryGetValue(player.Ckey, out Dictionary<string, JobPriority> priorities);
            string job = JobResolver.ResolveLateJoin(priorities, _jobs, _fallbackRole, _random, FallbackFor(player.Ckey));
            if (job == null)
            {
                return false;
            }

            Assign(player, job);
            RefreshJobCounts();
            return true;
        }

        /// <summary>
        /// Server: gives a latejoiner the job they picked, if it is still open (Decision 11). False, with
        /// <paramref name="refusal"/> set and no role assigned, when it is full or unknown.
        /// </summary>
        [Server]
        public bool TryAssignLateJoinPick(Player player, string job, out string refusal)
        {
            refusal = null;
            if (player == null)
            {
                refusal = "Unknown player.";
                return false;
            }

            if (_rolePlayers.ContainsKey(player))
            {
                return true;
            }

            refusal = JobResolver.LateJoinPickRefusal(job, _jobs, _fallbackRole);
            if (refusal != null)
            {
                Log.Information(this, "Late-join pick {job} refused for {ckey}: {reason}", Logs.ServerOnly, job, player.Ckey, refusal);
                return false;
            }

            Assign(player, job);
            RefreshJobCounts();
            return true;
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
                // Spawns outside round start and late join (a respawn, a test) always get a job.
                Assign(player, JobResolver.ResolveLateJoin(_preferences.GetValueOrDefault(player.Ckey), _jobs, _fallbackRole, _random));
                RefreshJobCounts();
            }

            if (!_rolePlayers.TryGetValue(player, out RoleData roleData))
            {
                return;
            }

            Log.Information(this, entity.Ckey + " embarked with role " + roleData.Name);
            RecordInManifest(player, entity, roleData);
            SpawnIdentificationItems(entity, roleData);

            if (roleData.Loadout != null)
            {
                SpawnLoadoutItems(entity, roleData.Loadout);
            }
        }

        [Server]
        private void RecordInManifest(Player player, Entity entity, RoleData role)
        {
            int row = _manifestBook.Record(player.Ckey, entity.Mind.DisplayName, role.Name, role.IsAntagonist);
            if (row < 0)
            {
                return;
            }

            CrewManifestEntry entry = _manifestBook.Entries[row];
            if (row < _manifest.Count)
            {
                _manifest[row] = entry;
            }
            else
            {
                _manifest.Add(entry);
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

            Item idCardItem = itemSystem.SpawnItemAtRest(role.IDCardAsset.Id, Vector3.zero);
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

            Dictionary<ContainerType, Item> equipped = new();

            // The belt slot only takes a belt over a jumpsuit, and suit storage only accepts a tank once the suit is on.
            foreach (KeyValuePair<ContainerType, ObjectAssetReference> itemToEquip in loadout.EquipmentAssets
                .OrderBy(equipment => equipment.Key switch
                {
                    ContainerType.Jumpsuit => 0,
                    ContainerType.SuitStorage => 2,
                    _ => 1,
                }))
            {
                if (!itemToEquip.Value)
                {
                    continue;
                }

                if (containers.TryGetValue(itemToEquip.Key, out AttachedContainer container)
                    && SpawnItemInSlot(itemToEquip.Value, true, container) is { } item)
                {
                    equipped[itemToEquip.Key] = item;
                }
            }

            foreach (LoadoutContents contents in loadout.EquipmentContents)
            {
                if (contents != null)
                {
                    SpawnContents(equipped.GetValueOrDefault(contents.Slot), contents.Items);
                }
            }

            if (loadout.FirstHolderContents is { Count: > 0 } && _firstHolderContentsGiven.Add(loadout))
            {
                foreach (LoadoutContents contents in loadout.FirstHolderContents)
                {
                    if (contents != null)
                    {
                        SpawnContents(equipped.GetValueOrDefault(contents.Slot), contents.Items);
                    }
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
                SpawnContents(SpawnItemInSlot(loadout.HandLeftAsset, true, left), loadout.HandLeftContents);
            }

            if (loadout.HandRightAsset && HandContainer(HandSide.Right) is { } right)
            {
                SpawnContents(SpawnItemInSlot(loadout.HandRightAsset, true, right), loadout.HandRightContents);
            }

            inventory.TriggerInventorySetup();
        }

        /// <summary>
        /// Fills the storage of a spawned loadout item (a toolbelt's tools, a medkit's patches). Contents that
        /// don't fit are refused and despawned by <see cref="ItemSubSystem.SpawnItemInContainer(GameObject, AttachedContainer)"/>.
        /// </summary>
        /// <param name="holder">The loadout item that holds the contents; nothing spawns when it is null.</param>
        /// <param name="contents">The items to spawn inside its storage.</param>
        private void SpawnContents(Item holder, List<ObjectAssetReference> contents)
        {
            if (holder == null || contents == null || contents.Count == 0)
            {
                return;
            }

            AttachedContainer storage = RoleLoadout.StorageOf(holder.gameObject);
            if (storage == null)
            {
                Log.Warning(this, "Loadout item {item} has no storage for its contents", Logs.ServerOnly, holder.name);
                return;
            }

            foreach (ObjectAssetReference content in contents)
            {
                if (content)
                {
                    SpawnItemInSlot(content, true, storage);
                }
            }
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