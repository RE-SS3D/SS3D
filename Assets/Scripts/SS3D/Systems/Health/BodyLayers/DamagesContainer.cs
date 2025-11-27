using SS3D.Logging;
using System.Collections.Generic;

namespace SS3D.Systems.Health
{
    public class DamagesContainer
    {
        private readonly IDictionary<DamageType, BodyDamageInfo> _damageInfos;

        public DamagesContainer()
        {
            _damageInfos = new Dictionary<DamageType, BodyDamageInfo>();
        }

        public BodyDamageInfo this[DamageType damageType]
        {
            get
            {
                if (_damageInfos.ContainsKey(damageType))
                {
                    return _damageInfos[damageType];
                }

                Log.Warning(this, $"no damages of type {damageType} defined for this damage container");
                return new BodyDamageInfo(damageType);
            }

            set
            {
                if (_damageInfos.ContainsKey(damageType))
                {
                    _damageInfos[damageType] = value;
                }
                else
                {
                    _damageInfos.Add(damageType, value);
                }
            }
        }

        public IDictionary<DamageType, BodyDamageInfo> DamagesInfo => _damageInfos;
    }
}
