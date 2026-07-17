using System;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Full per-zone and per-organ state synced for the runtime health debug overlay.
    /// </summary>
    [Serializable]
    public struct HealthDebugDetail
    {
        public ZoneDamageState Head;
        public ZoneDamageState Chest;
        public ZoneDamageState LeftArm;
        public ZoneDamageState RightArm;
        public ZoneDamageState LeftLeg;
        public ZoneDamageState RightLeg;
        public ZoneDamageState Groin;

        public OrganState Brain;
        public OrganState Heart;
        public OrganState LeftLung;
        public OrganState RightLung;
        public OrganState Liver;

        public ZoneDamageState GetZone(BodyZone zone)
        {
            return zone switch
            {
                BodyZone.Head => Head,
                BodyZone.Chest => Chest,
                BodyZone.LeftArm => LeftArm,
                BodyZone.RightArm => RightArm,
                BodyZone.LeftLeg => LeftLeg,
                BodyZone.RightLeg => RightLeg,
                BodyZone.Groin => Groin,
                _ => ZoneDamageState.Default,
            };
        }

        public OrganState GetOrgan(OrganType type)
        {
            return type switch
            {
                OrganType.Brain => Brain,
                OrganType.Heart => Heart,
                OrganType.LeftLung => LeftLung,
                OrganType.RightLung => RightLung,
                OrganType.Liver => Liver,
                _ => OrganState.Default(type),
            };
        }

        public static HealthDebugDetail FromStates(ZoneDamageState[] zones, System.Collections.Generic.List<OrganState> organs)
        {
            return new HealthDebugDetail
            {
                Head = zones[(int)BodyZone.Head],
                Chest = zones[(int)BodyZone.Chest],
                LeftArm = zones[(int)BodyZone.LeftArm],
                RightArm = zones[(int)BodyZone.RightArm],
                LeftLeg = zones[(int)BodyZone.LeftLeg],
                RightLeg = zones[(int)BodyZone.RightLeg],
                Groin = zones[(int)BodyZone.Groin],
                Brain = FindOrgan(organs, OrganType.Brain),
                Heart = FindOrgan(organs, OrganType.Heart),
                LeftLung = FindOrgan(organs, OrganType.LeftLung),
                RightLung = FindOrgan(organs, OrganType.RightLung),
                Liver = FindOrgan(organs, OrganType.Liver),
            };
        }

        private static OrganState FindOrgan(System.Collections.Generic.List<OrganState> organs, OrganType type)
        {
            for (int i = 0; i < organs.Count; i++)
            {
                if (organs[i].Type == type)
                {
                    return organs[i];
                }
            }

            return OrganState.Default(type);
        }
    }
}
