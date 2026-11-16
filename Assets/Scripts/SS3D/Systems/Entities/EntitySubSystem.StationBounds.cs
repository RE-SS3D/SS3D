using SS3D.Systems.Entities.Humanoid;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    public partial class EntitySubSystem
    {
        private const float StationBoundsCheckInterval = 0.25f;

        private float _nextStationBoundsCheck;

        /// <summary>Server: bodies above the ceiling or below the deck go back into the station (items check themselves).</summary>
        private void Update()
        {
            if (!IsServer || Time.time < _nextStationBoundsCheck)
            {
                return;
            }

            _nextStationBoundsCheck = Time.time + StationBoundsCheckInterval;
            foreach (Ragdoll body in Ragdoll.All)
            {
                if (body != null && body.IsServer)
                {
                    body.ServerKeepInStation();
                }
            }
        }
    }
}
