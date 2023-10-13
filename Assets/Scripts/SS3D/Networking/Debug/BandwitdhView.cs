using Coimbra;
using FishNet.Component.Utility;
using SS3D.Networking.Settings;

namespace SS3D.Networking.Debug
{
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