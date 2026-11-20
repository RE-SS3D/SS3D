using SS3D.Systems.Tile.MapEditor;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// What a map editor drag shows besides its ghosts: its size for the cursor badge, and in the hint bar how much of it
    /// can't be placed and why. A rectangle over <see cref="MapEditorPlacementRules.MaxDragCells"/> previews and places
    /// nothing.
    /// </summary>
    internal sealed class ConstructionDragFeedback
    {
        public const string TooBigMessage = "That's more than 4096 tiles — drag a smaller area";

        // The drag's hint replaced the tool's own; put that back when the drag ends.
        private bool _hintShown;

        /// <summary>"7 × 5 · 35 tiles"; null when not dragging.</summary>
        public string Readout { get; private set; }

        public bool TooBig { get; set; }

        /// <summary>Shows <paramref name="readout"/> and <paramref name="hint"/>; a null hint restores the tool's own.</summary>
        public void Show(MapEditorSubSystem editor, string readout, string hint)
        {
            Readout = readout;
            if (editor == null)
                return;

            if (!string.IsNullOrEmpty(hint))
            {
                editor.SetPlacementHint(hint);
                _hintShown = true;
            }
            else if (_hintShown)
            {
                _hintShown = false;
                editor.RestoreDefaultPlacementHint();
            }
        }

        // The joined badge changes only when the drag does, so it is rebuilt only then, not every frame.
        private string _labelBase;
        private string _labelReadout;
        private string _label;

        /// <summary>The cursor badge with the drag's size after it: "Steel Floor · 7 × 5 · 35 tiles".</summary>
        public string LabelWith(string label)
        {
            if (string.IsNullOrEmpty(Readout))
                return label;

            if (!ReferenceEquals(label, _labelBase) || !ReferenceEquals(Readout, _labelReadout))
            {
                _labelBase = label;
                _labelReadout = Readout;
                _label = string.IsNullOrEmpty(label) ? Readout : $"{label} · {Readout}";
            }

            return _label;
        }

        public void ShowTooBig(MapEditorSubSystem editor)
        {
            TooBig = true;
            Show(editor, "Too big", TooBigMessage);
        }

        /// <summary>The drag ended or was cancelled.</summary>
        public void Clear(MapEditorSubSystem editor)
        {
            TooBig = false;
            Show(editor, null, null);
        }
    }
}
