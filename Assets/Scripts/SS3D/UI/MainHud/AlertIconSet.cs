using UnityEngine;

namespace SS3D.UI.MainHud
{
    /// <summary>
    /// The imported alert-stack icon sprites (<c>Assets/Art/Icons/Alerts</c>),
    /// one per <see cref="Components.AlertHazard"/>. Assigned on <see cref="MainHudSubSystem"/> and threaded
    /// down to <see cref="Components.AlertIconStack"/>.
    /// </summary>
    [System.Serializable]
    public struct AlertIconSet
    {
        public Sprite Fire;
        public Sprite Hot;
        public Sprite Cold;
        public Sprite LowPressure;
        public Sprite HighPressure;
        public Sprite Radiation;
        public Sprite Hunger;
        public Sprite Thirst;
        public Sprite Pulling;
        public Sprite Restrained;
        public Sprite LowOxygen;
        public Sprite Dying;
        public Sprite Bleeding;
        public Sprite CardiacArrest;
        public Sprite SealBreach;
        public Sprite TankLow;

        /// <summary>The internals chip beside the hands; it is not a hazard.</summary>
        public Sprite Internals;

        /// <summary>The magboots chip beside the hands; it is not a hazard.</summary>
        public Sprite Magboots;

        public Sprite this[Components.AlertHazard hazard] => hazard switch
        {
            Components.AlertHazard.Fire => Fire,
            Components.AlertHazard.Hot => Hot,
            Components.AlertHazard.Cold => Cold,
            Components.AlertHazard.LowPressure => LowPressure,
            Components.AlertHazard.HighPressure => HighPressure,
            Components.AlertHazard.Radiation => Radiation,
            Components.AlertHazard.Hunger => Hunger,
            Components.AlertHazard.Thirst => Thirst,
            Components.AlertHazard.Pulling => Pulling,
            Components.AlertHazard.Restrained => Restrained,
            Components.AlertHazard.LowOxygen => LowOxygen,
            Components.AlertHazard.Dying => Dying,
            Components.AlertHazard.Bleeding => Bleeding,
            Components.AlertHazard.CardiacArrest => CardiacArrest,
            Components.AlertHazard.SealBreach => SealBreach,
            Components.AlertHazard.TankLow => TankLow,
            _ => null,
        };
    }
}
