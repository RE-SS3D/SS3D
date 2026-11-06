using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// A bar per pressure sample, oldest first; the newest takes the reading's tone.
    /// </summary>
    [UxmlElement]
    public partial class GasHistoryBars : VisualElement
    {
        public GasHistoryBars()
        {
            AddToClassList("gas-history-bars");
        }

        /// <param name="heights">Each bar's height, 0 to 1.</param>
        public void SetBars(float[] heights, StatusTone newestTone)
        {
            while (childCount < heights.Length)
            {
                VisualElement bar = new();
                bar.AddToClassList("gas-history-bars__bar");
                Add(bar);
            }

            for (int i = 0; i < heights.Length; i++)
            {
                VisualElement bar = this[i];
                bar.style.height = new Length(heights[i] * 100f, LengthUnit.Percent);
                bar.EnableInClassList("gas-history-bars__bar--newest", i == heights.Length - 1);
                if (i == heights.Length - 1)
                {
                    StatusToneUtility.ApplyTone(bar, newestTone);
                }
            }
        }
    }
}
