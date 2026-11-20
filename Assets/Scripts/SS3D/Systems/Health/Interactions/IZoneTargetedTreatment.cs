namespace SS3D.Systems.Health.Interactions
{
    /// <summary>
    /// Marker for a held medical item whose treatment lands on one body zone (bandage, splint, defib, ...).
    /// While the selected hand holds one in Help intent, the Main HUD shows the zone reticle with its
    /// body-part label so the medic can see which zone the click will treat.
    /// </summary>
    public interface IZoneTargetedTreatment
    {
    }
}
