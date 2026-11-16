using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor.Commands;

namespace SS3D.Tests.EditMode
{
    public sealed class MapEditorCommandRevertTests
    {
        [Test]
        public void UndoStack_PushUndoRedo_UpdatesDepth()
        {
            var stack = new MapEditorUndoStack();
            var map = TileMap.Create("undo-test");
            var ctx = new MapEditorCommandContext(map, null, new ConstructionService(map, new TileQueryService(map)));

            stack.Push(new CompoundCommand(System.Array.Empty<IMapEditorCommand>()));
            Assert.AreEqual(1, stack.UndoDepth);

            stack.TryUndo(ctx);
            Assert.AreEqual(0, stack.UndoDepth);
            Assert.AreEqual(1, stack.RedoDepth);

            stack.TryRedo(ctx);
            Assert.AreEqual(1, stack.UndoDepth);
            Assert.AreEqual(0, stack.RedoDepth);
        }

        [Test]
        public void SandboxPlayer_CanOnlyUndoTheirOwnNewestEdit()
        {
            MapEditorUndoStack stack = new();
            stack.Push(EmptyCommand(), owner: 1);
            stack.Push(EmptyCommand(), owner: 2);

            Assert.IsFalse(stack.TryUndo(null, actor: 1), "player 1 must not undo player 2's newer edit");
            Assert.AreEqual(2, stack.UndoDepth);
            Assert.IsTrue(stack.TryUndo(null, actor: 2));
            Assert.IsFalse(stack.TryRedo(null, actor: 1));
            Assert.IsTrue(stack.TryUndo(null, actor: 1));
            Assert.IsTrue(stack.TryUndo(null) == false && stack.UndoDepth == 0);
        }

        [Test]
        public void Admin_UndoesAnyEdit()
        {
            MapEditorUndoStack stack = new();
            stack.Push(EmptyCommand(), owner: 7);
            Assert.IsTrue(stack.TryUndo(null, MapEditorUndoStack.AnyOwner));
        }

        [Test]
        public void SavePoint_UndoAndRedoReturnToSavedState()
        {
            MapEditorUndoStack stack = new();
            stack.Push(EmptyCommand());
            stack.MarkSaved();
            stack.Push(EmptyCommand());
            Assert.IsTrue(stack.HasUnsavedChanges);
            Assert.IsTrue(stack.TryUndo(null));
            Assert.IsFalse(stack.HasUnsavedChanges);
            Assert.IsTrue(stack.TryUndo(null));
            Assert.IsTrue(stack.HasUnsavedChanges);
            Assert.IsTrue(stack.TryRedo(null));
            Assert.IsFalse(stack.HasUnsavedChanges);
        }

        [Test]
        public void SavePoint_BranchAtSameDepthDoesNotLookSaved()
        {
            MapEditorUndoStack stack = new();
            stack.Push(EmptyCommand());
            stack.Push(EmptyCommand());
            stack.MarkSaved();
            Assert.IsTrue(stack.TryUndo(null));
            stack.Push(EmptyCommand());
            Assert.AreEqual(2, stack.UndoDepth);
            Assert.IsTrue(stack.HasUnsavedChanges);
            Assert.AreEqual(0, stack.RedoDepth);
        }

        [Test]
        public void SavePoint_SurvivesHistoryLimitAndCannotUndoBeforeRetainedHistory()
        {
            MapEditorUndoStack stack = new();
            for (int i = 0; i < 80; i++) stack.Push(EmptyCommand());
            stack.MarkSaved();
            stack.Push(EmptyCommand());
            Assert.AreEqual(50, stack.UndoDepth);
            Assert.IsTrue(stack.TryUndo(null));
            Assert.IsFalse(stack.HasUnsavedChanges);
            for (int i = 0; i < 49; i++) Assert.IsTrue(stack.TryUndo(null));
            Assert.IsFalse(stack.TryUndo(null));
            Assert.IsTrue(stack.HasUnsavedChanges);
            for (int i = 0; i < 49; i++) Assert.IsTrue(stack.TryRedo(null));
            Assert.IsFalse(stack.HasUnsavedChanges);
        }

        [Test]
        public void NewMapIsUnsavedEvenWithoutCommands_LoadClearsOldHistory()
        {
            MapEditorUndoStack stack = new();
            stack.MarkNewMap();
            Assert.AreEqual(0, stack.UndoDepth);
            Assert.IsTrue(stack.HasUnsavedChanges);
            Assert.IsFalse(stack.TryUndo(null));
            stack.MarkSaved();
            Assert.IsFalse(stack.HasUnsavedChanges);
            stack.Push(EmptyCommand());
            long beforeLoad = stack.Revision;
            stack.Clear();
            Assert.AreNotEqual(beforeLoad, stack.Revision);
            Assert.IsFalse(stack.HasUnsavedChanges);
            Assert.IsFalse(stack.TryUndo(null));
            Assert.IsFalse(stack.TryRedo(null));
        }

        private static IMapEditorCommand EmptyCommand() =>
            new CompoundCommand(System.Array.Empty<IMapEditorCommand>());
    }
}
