using UnityEngine;

namespace SS3D.Systems.IdAccess
{
    /// <summary>
    /// MonoBehaviour adapter so doors, lockers, and consoles can participate in auth logging.
    /// </summary>
    public sealed class AuthLogDeviceBehaviour : MonoBehaviour, IAuthLogDevice
    {
        [SerializeField]
        private string _deviceId;

        [SerializeField]
        private int _logCapacity = 32;

        private DeviceAuthLog _authLog;

        public string DeviceId => string.IsNullOrWhiteSpace(_deviceId) ? name : _deviceId;

        public DeviceAuthLog AuthLog => _authLog ??= new DeviceAuthLog(_logCapacity);

        private void Reset()
        {
            _deviceId = name;
        }
    }
}
