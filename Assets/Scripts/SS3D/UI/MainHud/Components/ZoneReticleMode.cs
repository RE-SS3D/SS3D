using SS3D.Interactions;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Decides whether the zone reticle shows and whether it carries the body-part label.
    /// Combat (Harm) shows the reticle without the label; medical (Help with a zone-targeted
    /// treatment item in the selected hand) shows both.
    /// </summary>
    public static class ZoneReticleMode
    {
        public static void Resolve(IntentType intent, bool holdsZoneTreatment, out bool visible, out bool showLabel)
        {
            if (intent == IntentType.Harm)
            {
                visible = true;
                showLabel = false;
                return;
            }

            visible = intent == IntentType.Help && holdsZoneTreatment;
            showLabel = visible;
        }
    }
}
