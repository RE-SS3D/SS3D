using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor.Commands
{
    /// <summary>
    /// Server-side undo/redo stacks for map editor mutations.
    /// </summary>
    public sealed class MapEditorUndoStack
    {
        private const int MaxDepth = 50;

        private readonly Stack<Entry> _undo = new();
        private readonly Stack<Entry> _redo = new();

        private sealed class Entry
        {
            public IMapEditorCommand Command;
            public long Before;
            public long After;
            public int Owner;
        }

        /// <summary>Owner value for edits that anyone may undo (admins, host, single-player).</summary>
        public const int AnyOwner = -1;

        private long _nextRevision;
        private long _revision;
        private long _savedRevision;
        public long Revision => _revision;
        public bool HasUnsavedChanges => _revision != _savedRevision;
        public void MarkSaved() => _savedRevision = _revision;
        public void MarkNewMap()
        {
            Clear();
            _revision = ++_nextRevision;
        }

        public int UndoDepth => _undo.Count;
        public int RedoDepth => _redo.Count;

        public void Push(IMapEditorCommand command, int owner = AnyOwner)
        {
            _undo.Push(new Entry { Command = command, Before = _revision, After = ++_nextRevision, Owner = owner });
            _revision = _nextRevision;
            _redo.Clear();

            while (_undo.Count > MaxDepth)
            {
                Stack<Entry> trimmed = new();
                int keep = MaxDepth;
                foreach (Entry item in _undo)
                {
                    if (keep-- <= 0)
                        break;
                    trimmed.Push(item);
                }

                _undo.Clear();
                foreach (Entry item in trimmed)
                    _undo.Push(item);
            }
        }

        /// <summary>
        /// Undoes the newest edit. With an <paramref name="actor"/> other than <see cref="AnyOwner"/>,
        /// only when that newest edit is the actor's own, so players on a shared server cannot undo
        /// each other's work. Only the top entry is ever reverted, which keeps the history consistent.
        /// </summary>
        public bool TryUndo(MapEditorCommandContext ctx, int actor = AnyOwner)
        {
            if (_undo.Count == 0)
                return false;

            Entry entry = _undo.Peek();
            if (!MayTouch(entry, actor))
                return false;

            entry.Command.Revert(ctx);
            _undo.Pop();
            _revision = entry.Before;
            _redo.Push(entry);
            return true;
        }

        public bool TryRedo(MapEditorCommandContext ctx, int actor = AnyOwner)
        {
            if (_redo.Count == 0)
                return false;

            Entry entry = _redo.Peek();
            if (!MayTouch(entry, actor))
                return false;

            entry.Command.Apply(ctx);
            _redo.Pop();
            _revision = entry.After;
            _undo.Push(entry);
            return true;
        }

        private static bool MayTouch(Entry entry, int actor) => actor == AnyOwner || entry.Owner == actor;

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
            _revision = ++_nextRevision;
            _savedRevision = _revision;
        }
    }
}
