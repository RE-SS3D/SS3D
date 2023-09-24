using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Entities.Humanoid
{
	public class RagdollNetwork : NetworkSystem
	{
		private struct RagdollBody : IBroadcast
		{
			public Ragdoll Ragdoll;

			public RagdollBody(Ragdoll ragdoll)
			{
				Ragdoll =  ragdoll;
			}
		}
		private void OnEnable()
		{
			InstanceFinder.ClientManager.RegisterBroadcast<RagdollBody>(OnRagdollBroadcast);
			InstanceFinder.ServerManager.RegisterBroadcast<RagdollBody>(OnClientRagdollBroadcast);
		}

		private void OnDisable()
		{
			InstanceFinder.ClientManager.UnregisterBroadcast<RagdollBody>(OnRagdollBroadcast);
			InstanceFinder.ServerManager.UnregisterBroadcast<RagdollBody>(OnClientRagdollBroadcast);
		}

		public void SendBroadcast(Ragdoll ragdoll)
		{
			if (InstanceFinder.IsServer)
			{
				InstanceFinder.ServerManager.Broadcast(new RagdollBody(ragdoll));
			}
			else if (InstanceFinder.IsClient)
			{
				InstanceFinder.ClientManager.Broadcast(new RagdollBody(ragdoll));
			}
		}
		private void OnRagdollBroadcast(RagdollBody ragdollBody)
		{
			ragdollBody.Ragdoll.Knockdown(1f);
		}
		private void OnClientRagdollBroadcast(NetworkConnection networkConnection, RagdollBody ragdollBody)
		{
			InstanceFinder.ServerManager.Broadcast(ragdollBody);
		}
	}
}