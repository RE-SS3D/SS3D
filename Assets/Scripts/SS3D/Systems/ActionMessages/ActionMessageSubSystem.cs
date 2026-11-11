using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Vision;
using UnityEngine;

namespace SS3D.Systems.ActionMessages
{
    /// <summary>
    /// Narrated action lines (action-messages.md): the server picks recipients by visibility level
    /// and sends a key plus object references; each client formats names with
    /// <see cref="PresentedIdentity"/> and raises <see cref="ClientLineReceived"/> for the chip
    /// controller. Lives on the NetworkSystemsHub (added by <c>ActionMessageHubRecipe</c>).
    /// </summary>
    public sealed class ActionMessageSubSystem : NetworkSubSystem
    {
        /// <summary>Raised on a client for every line this viewer perceives.</summary>
        public event Action<ActionLine> ClientLineReceived;

        private readonly ExamineCueLimiter _cueLimiter = new();
        private readonly List<(NetworkConnection Listener, Vector3 Position)> _listeners = new();
        private readonly List<NetworkConnection> _recipients = new();

        /// <summary>
        /// Server: narrate an action. <paramref name="actor"/> and <paramref name="target"/> are the
        /// characters (or objects) involved, <paramref name="item"/> what was used; any may be null.
        /// </summary>
        [Server]
        public void Narrate(string key, NetworkObject actor, NetworkObject target, NetworkObject item, ActionVisibility level)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            Vector3 origin = actor != null ? actor.transform.position : target != null ? target.transform.position : Vector3.zero;
            CollectListeners();
            ActionMessageRecipients.Select(level, origin, actor != null ? actor.Owner : null,
                target != null ? target.Owner : null, _listeners, _recipients);

            foreach (NetworkConnection recipient in _recipients)
            {
                if (recipient != null && recipient.IsActive)
                {
                    TargetLine(recipient, key, actor, target, item, (byte)level);
                }
            }
        }

        /// <summary>
        /// Client: the local player began holding examine on <paramref name="target"/> (examine.md §4).
        /// The server decides whether they were close enough to be noticed.
        /// </summary>
        [Client]
        public void RequestExamineCue(NetworkObject target)
        {
            if (target != null)
            {
                CmdExamineCue(target);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void CmdExamineCue(NetworkObject target, NetworkConnection sender = null)
        {
            if (target == null || sender == null
                || !SubSystems.TryGet(out EntitySubSystem entities)
                || !entities.TryGetOwnedEntity(sender, out Entity examiner)
                || examiner == null
                || examiner.NetworkObject == target
                || target.GetComponent<HumanInventory>() == null)
            {
                return;
            }

            float distance = Vector3.Distance(examiner.Position, target.transform.position);
            if (distance > ActionMessageConfig.CueRange)
            {
                return;
            }

            if (!_cueLimiter.TryAllow(sender.ClientId, target.ObjectId, Time.unscaledTime))
            {
                return;
            }

            Narrate(ActionMessageKeys.ExamineCue, examiner.NetworkObject, target, null, ActionVisibility.TargetOnly);
        }

        [TargetRpc]
        private void TargetLine(NetworkConnection connection, string key, NetworkObject actor, NetworkObject target, NetworkObject item, byte level)
        {
            ActionVisibility visibility = (ActionVisibility)level;
            Entity actorEntity = actor != null ? actor.GetComponent<Entity>() : null;

            // Public lines follow the screen: what the FOV hides, you didn't see (examine map Pitfall).
            if (visibility == ActionVisibility.Public && actorEntity != null && !IsVisibleToViewer(actorEntity))
            {
                return;
            }

            bool aimedAtViewer = target != null && target.IsOwner;
            string text = Format(key, Name(actor), Name(target), ItemName(item));
            ClientLineReceived?.Invoke(new ActionLine(text, actorEntity, visibility, aimedAtViewer, ActionMessageKeys.IsHostile(key)));
        }

        /// <summary>Fills the line's template with names already formatted for this viewer.</summary>
        public static string Format(string key, string actorName, string targetName, string itemName) =>
            ExamineText.Get(key, ActionMessageKeys.EnglishFor(key) ?? key, actorName ?? string.Empty,
                targetName ?? string.Empty, itemName ?? string.Empty);

        private void CollectListeners()
        {
            _listeners.Clear();
            if (!SubSystems.TryGet(out EntitySubSystem entities))
            {
                return;
            }

            foreach (Entity entity in entities.SpawnedPlayers)
            {
                if (entity != null && entity.Owner != null && entity.Owner.IsValid)
                {
                    _listeners.Add((entity.Owner, entity.Position));
                }
            }
        }

        private static bool IsVisibleToViewer(Entity actor)
        {
            if (!SubSystems.TryGet(out VisionSubSystem vision))
            {
                return true;
            }

            Vector3 point = actor.ViewPoint != null ? actor.ViewPoint.transform.position : actor.Position;
            return vision.IsVisible(point);
        }

        private static string Name(NetworkObject subject)
        {
            if (subject == null)
            {
                return null;
            }

            HumanInventory inventory = subject.GetComponent<HumanInventory>();
            if (inventory != null)
            {
                return PresentedIdentity.DisplayName(inventory);
            }

            return subject.TryGetComponent(out Item item) ? item.Name : subject.name;
        }

        private static string ItemName(NetworkObject item) =>
            item != null && item.TryGetComponent(out Item component) ? component.Name.ToLowerInvariant() : null;
    }
}
