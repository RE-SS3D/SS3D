using UnityEngine;
using UnityEngine.Assertions;
using Coimbra;
using SS3D.Networking.Debug;
using SS3D.Networking.Settings;

namespace SS3D.UI.Settings
{
    public class UserInterfaceSettings : MonoBehaviour
    {
        [SerializeField]
        private GameObject _staminaBar;
        private BandwidthView _bandwidthView;

        private bool _enableNetworkBandwidthUsageStats;

        private void Awake()
        {
            Assert.IsNotNull(_staminaBar);
            _bandwidthView = FindObjectOfType<BandwidthView>();
            _enableNetworkBandwidthUsageStats =
                ScriptableSettings.GetOrFind<NetworkSettings>().EnableNetworkBandwidthUsageStats;
        }

        public void ToogleStaminaBar(bool isActive)
        {
            _staminaBar.SetActive(isActive);
        }

        public void ToogleNetworkTrafficInfo(bool isActive)
        {
            if (_enableNetworkBandwidthUsageStats)
            {
                _bandwidthView.gameObject.SetActive(isActive);
            }
        }
    }
}