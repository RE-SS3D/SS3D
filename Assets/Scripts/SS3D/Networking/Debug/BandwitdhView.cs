using Coimbra;
using FishNet.Component.Utility;
using SS3D.Networking.Settings;

namespace SS3D.Networking.Debug
{
    /// <summary>
    /// Overrides FishNets BandwidthDisplay class to implement a way to disable it using ScriptableSettings.
    /// </summary>
	public class BandwitdhView : BandwidthDisplay
	{
		private void Awake()
		{
			bool enableNetworkBandwidthUsageStats = ScriptableSettings.GetOrFind<NetworkSettings>().EnableNetworkBandwidthUsageStats;

			if (!enableNetworkBandwidthUsageStats)
			{
				gameObject.Dispose(true);
			}
		}
	}
}