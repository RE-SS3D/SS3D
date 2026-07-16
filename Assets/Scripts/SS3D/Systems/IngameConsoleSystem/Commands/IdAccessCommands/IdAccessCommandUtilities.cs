using System.Collections.Generic;
using System.Text;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.IngameConsoleSystem.Commands.IdAccessCommands
{
    public static class IdAccessCommandUtilities
    {
        public static bool TryResolveTarget(
            NetworkConnection conn,
            string optionalCkey,
            out HumanInventory inventory,
            out string error)
        {
            inventory = null;
            error = string.Empty;

            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();

            Player player = string.IsNullOrWhiteSpace(optionalCkey)
                ? playerSystem.GetPlayer(conn)
                : playerSystem.GetPlayer(optionalCkey);

            if (player == null)
            {
                error = string.IsNullOrWhiteSpace(optionalCkey)
                    ? "Could not resolve the calling player."
                    : $"No player found for ckey '{optionalCkey}'.";
                return false;
            }

            Entity entity = entitySystem.GetSpawnedEntity(player);
            if (entity == null)
            {
                error = $"Player '{player.Ckey}' has no spawned entity.";
                return false;
            }

            inventory = entity.GetComponent<HumanInventory>();
            if (inventory == null)
            {
                error = $"Player '{player.Ckey}' has no HumanInventory.";
                return false;
            }

            return true;
        }

        public static bool TryParseAccessLevel(string name, out AccessLevel level, out string error)
        {
            level = AccessLevel.None;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Access level name is required.";
                return false;
            }

            string normalized = name.Trim().Replace(" ", string.Empty).Replace("'", string.Empty);
            foreach (AccessLevelEntry entry in AccessLevelCatalog.AllEditableLevels)
            {
                string display = entry.DisplayName.Replace(" ", string.Empty).Replace("'", string.Empty);
                if (string.Equals(display, normalized, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(entry.Level.ToString(), normalized, System.StringComparison.OrdinalIgnoreCase))
                {
                    level = entry.Level;
                    return true;
                }
            }

            error = $"Unknown access level '{name}'. Try Engineering, Security, ChangeId, etc.";
            return false;
        }

        public static bool TryParsePreset(string name, out AccessMask mask, out string error)
        {
            mask = AccessMask.None;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Preset name is required.";
                return false;
            }

            mask = name.Trim().ToLowerInvariant() switch
            {
                "none" or "clear" => AccessMask.None,
                "civilian" or "crew" or "standard" or "standardcrew" => AccessPresets.StandardCrew,
                "engineer" or "engineering" => AccessPresets.Engineer,
                "security" or "securityofficer" => AccessPresets.SecurityOfficer,
                "hop" or "headofpersonnel" => AccessPresets.HeadOfPersonnel,
                "captain" or "all" => AccessPresets.Captain,
                _ => AccessMask.None,
            };

            if (mask.IsNone && name.Trim().ToLowerInvariant() is not ("none" or "clear"))
            {
                error = "Unknown preset. Try engineer, security, hop, captain, standard, none.";
                return false;
            }

            return true;
        }

        public static string FormatAccessMask(AccessMask mask)
        {
            if (mask.IsNone)
            {
                return "none";
            }

            var levels = new List<string>();
            foreach (AccessLevelEntry entry in AccessLevelCatalog.AllEditableLevels)
            {
                if (mask.HasAll(new AccessMask(entry.Level)))
                {
                    levels.Add(entry.DisplayName);
                }
            }

            return levels.Count == 0
                ? $"0x{mask.Value:X}"
                : string.Join(", ", levels);
        }

        public static string BuildCredentialReport(HumanInventory inventory, AccessLevel? testLevel = null)
        {
            var report = new StringBuilder();
            IdAccessSubSystem idAccess = SubSystems.Get<IdAccessSubSystem>();

            report.AppendLine($"Player: {inventory.GetComponentInParent<Entity>()?.Ckey ?? "unknown"}");

            List<FoundCard> foundCards = FindAllOnPersonCards(inventory);
            if (foundCards.Count == 0)
            {
                report.AppendLine("On-person ID cards: none");
            }
            else
            {
                report.AppendLine("On-person ID cards:");
                foreach (FoundCard found in foundCards)
                {
                    report.AppendLine(
                        $"  - {found.ContainerName}: {found.Card.OwnerName} / {found.Card.RoleName} "
                        + $"(record {found.Card.BoundRecordId}, bound={!found.Card.BoundRecordId.IsNone})");
                }
            }

            if (!AccessCredentialResolver.TryResolveBoundRecord(inventory, out CrewRecordId recordId, out IDCard activeCard))
            {
                report.AppendLine("Active credential: none (resolver found no bound card)");
                return report.ToString().TrimEnd();
            }

            report.AppendLine(
                $"Active credential: {activeCard.OwnerName} / {activeCard.RoleName} (record {recordId})");

            if (!idAccess.TryGetRecord(recordId, out CrewRecord record))
            {
                report.AppendLine("Crew record: missing — card is bound but no server record exists.");
                return report.ToString().TrimEnd();
            }

            report.AppendLine($"Record access: {FormatAccessMask(record.Access)} (0x{record.Access.Value:X})");

            if (testLevel.HasValue)
            {
                AccessMask required = AccessMask.FromLevels(testLevel.Value);
                AccessCheckResult result = idAccess.CheckAccess(inventory, required, device: null);
                report.AppendLine(
                    $"Check {testLevel.Value}: {(result.Passed ? "PASS" : $"FAIL ({result.FailureReason})")}");
            }

            return report.ToString().TrimEnd();
        }

        public static bool TrySetRecordAccess(
            HumanInventory inventory,
            AccessMask newAccess,
            out string error)
        {
            error = string.Empty;

            if (!AccessCredentialResolver.TryResolveBoundRecord(inventory, out CrewRecordId recordId, out IDCard _))
            {
                error = "No bound ID card found on person.";
                return false;
            }

            IdAccessSubSystem idAccess = SubSystems.Get<IdAccessSubSystem>();
            if (!idAccess.TrySetAccess(recordId, newAccess))
            {
                error = $"Failed to update crew record {recordId}.";
                return false;
            }

            return true;
        }

        private static List<FoundCard> FindAllOnPersonCards(HumanInventory inventory)
        {
            var found = new List<FoundCard>();
            var seen = new HashSet<IDCard>();

            void ScanContainer(AttachedContainer container, string containerName)
            {
                if (container == null)
                {
                    return;
                }

                foreach (Item item in container.Items)
                {
                    if (item is IDCard directCard && seen.Add(directCard))
                    {
                        found.Add(new FoundCard(containerName, directCard));
                    }

                    if (item is PDA pda
                        && pda.GetInsertedIdCard() is IDCard insertedCard
                        && seen.Add(insertedCard))
                    {
                        found.Add(new FoundCard($"{containerName} (PDA)", insertedCard));
                    }
                }
            }

            if (inventory.Containers != null)
            {
                foreach (AttachedContainer container in inventory.Containers)
                {
                    ScanContainer(container, container.ContainerType.ToString());
                }
            }

            Hands hands = inventory.Hands;
            if (hands?.HandContainers != null)
            {
                for (int i = 0; i < hands.HandContainers.Count; i++)
                {
                    ScanContainer(hands.HandContainers[i], $"Hand{i}");
                }
            }

            return found;
        }

        private readonly struct FoundCard
        {
            public string ContainerName { get; }

            public IDCard Card { get; }

            public FoundCard(string containerName, IDCard card)
            {
                ContainerName = containerName;
                Card = card;
            }
        }
    }
}
