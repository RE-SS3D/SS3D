using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// A bar split into one coloured length per gas. The track paints and the inner clip cuts,
    /// never both on one element: UI Toolkit draws that as a flat block.
    /// </summary>
    [UxmlElement]
    public partial class GasMixBar : VisualElement
    {
        private readonly VisualElement _clip;

        public GasMixBar()
        {
            AddToClassList("gas-mix-bar");

            _clip = new VisualElement();
            _clip.AddToClassList("gas-mix-bar__clip");
            Add(_clip);
        }

        public void SetSegments(IReadOnlyList<GasMixSegment> segments)
        {
            while (_clip.childCount > segments.Count)
            {
                _clip.RemoveAt(_clip.childCount - 1);
            }

            while (_clip.childCount < segments.Count)
            {
                VisualElement segment = new();
                segment.AddToClassList("gas-mix-bar__segment");
                _clip.Add(segment);
            }

            for (int i = 0; i < segments.Count; i++)
            {
                VisualElement segment = _clip[i];
                segment.style.flexGrow = segments[i].Percent;
                segment.style.backgroundColor = segments[i].Color;
            }
        }
    }
}
