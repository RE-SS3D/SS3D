using FishNet.Connection;
using SS3D.Core;
using SS3D.Data.Persistence;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile.MapEditor.Commands;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Server: turns an in-world setup change made by someone with the map editor open (a valve turned, a locker filled)
    /// into an undo step owned by them, so it also marks the map unsaved
    /// (Documents/architecture/2026-11_object-setup-persistence.md Q4, Q14). The server entry points wrap their work in
    /// <see cref="Track"/>; for anyone without the editor open it just runs the work, so normal play pays one lookup.
    /// </summary>
    public sealed class MapEditorSetupTracker
    {
        private const int MaxContainerDepth = 16;

        private readonly MapEditorCommandService _commands;

        /// <summary>Clients whose editor is open, as each reported it.</summary>
        private readonly HashSet<int> _editingClients = new();

        public MapEditorSetupTracker(MapEditorCommandService commands) => _commands = commands;

        public bool IsEditing(int clientId) => _editingClients.Contains(clientId);

        /// <summary>Someone has the editor open, so in-world changes need tracking at all.</summary>
        public bool AnyEditing => _editingClients.Count > 0;

        /// <summary>A client opened or closed the editor; a disconnect closes it (FishNet reuses ids).</summary>
        public void SetEditing(int clientId, bool open)
        {
            if (open)
                _editingClients.Add(clientId);
            else
                _editingClients.Remove(clientId);
        }

        /// <summary>Raised with the owner's client id after an undo step was recorded.</summary>
        public event Action<int> Recorded;

        /// <summary>
        /// Runs <paramref name="action"/>; when <paramref name="conn"/> has the editor open, snapshots the setup of the
        /// placed objects behind <paramref name="involved"/> before and after and records what changed.
        /// </summary>
        public static void Track(NetworkConnection conn, Action action, params GameObject[] involved)
        {
            MapEditorSetupTracker tracker = conn != null && conn.IsValid && SubSystems.TryGet(out MapEditorSubSystem editor)
                ? editor.SetupTracker
                : null;

            if (tracker == null)
            {
                action();
                return;
            }

            tracker.Run(conn.ClientId, involved, action);
        }

        public void Run(int clientId, IReadOnlyList<GameObject> involved, Action action) =>
            RunStep(clientId, involved, () =>
            {
                action();
                return true;
            });

        /// <summary>As <see cref="Run"/>, for a step that returns a result (an interaction's Start or Update).</summary>
        public bool RunStep(int clientId, IReadOnlyList<GameObject> involved, Func<bool> step)
        {
            if (!IsEditing(clientId))
                return step();

            List<GameObject> roots = new();
            if (involved != null)
            {
                foreach (GameObject candidate in involved)
                    AddRoot(FindPlacedRoot(candidate), roots, expandScope: true);
            }

            var before = new SavedSetupEntry[roots.Count][];
            for (int i = 0; i < roots.Count; i++)
                before[i] = ObjectSetup.Capture(roots[i]);

            try
            {
                return step();
            }
            finally
            {
                Record(clientId, roots, before);
            }
        }

        private void Record(int clientId, List<GameObject> roots, SavedSetupEntry[][] before)
        {
            List<IMapEditorCommand> changes = null;
            for (int i = 0; i < roots.Count; i++)
            {
                // Despawned by the action (a deconstruction): not a setup change.
                if (roots[i] == null)
                    continue;

                if (SetObjectSetupCommand.TryCreate(roots[i], before[i], ObjectSetup.Capture(roots[i]), out SetObjectSetupCommand command))
                    (changes ??= new List<IMapEditorCommand>()).Add(command);
            }

            if (changes == null)
                return;

            IMapEditorCommand step = changes.Count == 1 ? changes[0] : new CompoundCommand(changes.ToArray());
            if (_commands.Record(step, clientId))
                Recorded?.Invoke(clientId);
        }

        private static void AddRoot(GameObject root, List<GameObject> roots, bool expandScope)
        {
            if (root == null || roots.Contains(root))
                return;

            roots.Add(root);
            if (!expandScope)
                return;

            List<GameObject> scope = null;
            foreach (ISetupScope scoped in root.GetComponentsInChildren<ISetupScope>(true))
                scoped.CollectSetupScope(scope ??= new List<GameObject>());

            if (scope == null)
                return;

            foreach (GameObject other in scope)
                AddRoot(FindPlacedRoot(other), roots, expandScope: false);
        }

        /// <summary>
        /// The placed tile object or placed item whose saved setup covers <paramref name="target"/>: itself, or for an
        /// item stored in a container, the placed object holding that container, however deep. Null for anything else,
        /// such as an item in someone's hands.
        /// </summary>
        public static GameObject FindPlacedRoot(GameObject target)
        {
            GameObject current = target;
            for (int depth = 0; current != null && depth < MaxContainerDepth; depth++)
            {
                PlacedTileObject tile = current.GetComponentInParent<PlacedTileObject>(true);
                if (tile != null)
                    return tile.gameObject;

                PlacedItemObject placedItem = current.GetComponentInParent<PlacedItemObject>(true);
                if (placedItem != null)
                    return placedItem.gameObject;

                Item item = current.GetComponentInParent<Item>(true);
                if (item == null || item.Container == null)
                    return null;

                current = item.Container.gameObject;
            }

            return null;
        }
    }
}
