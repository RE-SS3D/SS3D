using SS3D.Systems.Tile.Connections;
using System;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// very basic power generator, producing constant infinite power.
    /// </summary>
    public class BasicPowerGenerator : BasicElectricDevice, IPowerProducer
    {
        [SerializeField]
        private float _powerProduction = 10f;

        public float PowerProduction 
        { 
          get => _powerProduction;
          set => _powerProduction = MathF.Max(value, 0f);
        }
    }
}
