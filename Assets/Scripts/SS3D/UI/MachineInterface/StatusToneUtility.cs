using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface
{
    public static class StatusToneUtility
    {
        public static void ApplyTone(VisualElement element, StatusTone tone, bool background = false)
        {
            element.RemoveFromClassList("tone-info");
            element.RemoveFromClassList("tone-success");
            element.RemoveFromClassList("tone-warning");
            element.RemoveFromClassList("tone-danger");
            element.RemoveFromClassList("tone-neutral");

            string className = tone switch
            {
                StatusTone.Success => "tone-success",
                StatusTone.Warning => "tone-warning",
                StatusTone.Danger => "tone-danger",
                StatusTone.Neutral => "tone-neutral",
                _ => "tone-info",
            };

            element.AddToClassList(className);

            if (background)
            {
                element.RemoveFromClassList("tone-bg-info");
                element.RemoveFromClassList("tone-bg-success");
                element.RemoveFromClassList("tone-bg-warning");
                element.RemoveFromClassList("tone-bg-danger");
                element.AddToClassList(className.Replace("tone-", "tone-bg-"));
            }
        }
    }
}
