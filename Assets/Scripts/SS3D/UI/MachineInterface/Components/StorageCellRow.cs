using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class StorageCellRow : VisualElement
    {
        private const int CellCount = 10;
        private readonly VisualElement[] _cells = new VisualElement[CellCount];
        private float _chargePct;

        public StorageCellRow()
        {
            AddToClassList("storage-cell-row");

            for (int i = 0; i < CellCount; i++)
            {
                VisualElement cell = new();
                cell.AddToClassList("storage-cell-row__cell");
                if (i == CellCount - 1)
                {
                    cell.AddToClassList("storage-cell-row__cell--last");
                }

                _cells[i] = cell;
                Add(cell);
            }
        }

        [UxmlAttribute]
        public float ChargePct
        {
            get => _chargePct;
            set
            {
                _chargePct = Mathf.Clamp01(value);
                UpdateCells();
            }
        }

        public void SetCharge(float chargePct, StatusTone tone)
        {
            _chargePct = Mathf.Clamp01(chargePct);
            UpdateCells(tone);
        }

        private static StatusTone GetToneForCharge(float chargePct)
        {
            if (chargePct <= 0.15f)
            {
                return StatusTone.Danger;
            }

            if (chargePct <= 0.4f)
            {
                return StatusTone.Warning;
            }

            return StatusTone.Success;
        }

        private void UpdateCells(StatusTone? toneOverride = null)
        {
            int filledCells = Mathf.CeilToInt(_chargePct * CellCount);
            StatusTone tone = toneOverride ?? GetToneForCharge(_chargePct);

            for (int i = 0; i < CellCount; i++)
            {
                VisualElement cell = _cells[i];
                bool filled = i < filledCells;
                cell.EnableInClassList("storage-cell-row__cell--filled", filled);
                cell.RemoveFromClassList("tone-success");
                cell.RemoveFromClassList("tone-warning");
                cell.RemoveFromClassList("tone-danger");

                if (filled)
                {
                    string toneClass = tone switch
                    {
                        StatusTone.Danger => "tone-danger",
                        StatusTone.Warning => "tone-warning",
                        _ => "tone-success",
                    };
                    cell.AddToClassList(toneClass);
                }
            }
        }
    }
}
