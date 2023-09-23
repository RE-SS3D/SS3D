using System.Collections.Generic;

namespace SS3D.Systems.Health
{
    public class BodyDamageContainer
    {
        private IDictionary<DamageType, BodyDamageInfo> _damageInfos;

        public BodyDamageContainer()
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
                else
                {
                    return new BodyDamageInfo(damageType);
                }
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
    }
}