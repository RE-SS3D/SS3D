using SS3D.Core.Behaviours;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Server-side scheduler that drives every entity's circulation on one shared, fixed-cadence tick. It no longer
    /// distinguishes deliverers from consumers: each <see cref="IMetabolicController"/> runs its whole metabolic step
    /// (deliver + consume) in one continuous, dt-scaled pass, which removes the delivery/consume clock crossover behind
    /// #1362.
    /// </summary>
    public class MetabolicSubSystem : NetworkSubSystem
    {
        /// <summary>
        /// How often the metabolic tick runs. The tick is dt-scaled, so this only trades smoothness against cost:
        /// ~10 Hz is smooth without paying the per-frame cost of the LINQ-heavy substance containers.
        /// </summary>
        private const float MetabolicTickInterval = 0.1f;

        private readonly List<IMetabolicController> _controllers = new();
        private float _timer;

        public void RegisterController(IMetabolicController controller)
        {
            if (!_controllers.Contains(controller))
            {
                _controllers.Add(controller);
            }
        }

        public void UnregisterController(IMetabolicController controller)
        {
            _controllers.Remove(controller);
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            _timer += Time.deltaTime;

            if (_timer < MetabolicTickInterval)
            {
                return;
            }

            float elapsed = _timer;
            _timer = 0f;

            // Iterate backwards so a controller that unregisters during its own tick (OnStopServer) is safe.
            for (int i = _controllers.Count - 1; i >= 0; i--)
            {
                _controllers[i].MetabolicTick(elapsed);
            }
        }
    }
}