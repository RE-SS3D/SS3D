using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Entities.Humanoid
{
	public class RagdollNetwork : NetworkSystem
	{
		public struct RagdollBody : IBroadcast
		{
			public Ragdoll RagdollInstance;
			public string MethodName;
			public object[] Parameters;

			public RagdollBody(Ragdoll ragdollInstance, string methodName, object[] parameters)
			{
				RagdollInstance = ragdollInstance;
				MethodName = methodName;
				Parameters = parameters;
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

		public void SendBroadcast(Ragdoll ragdoll, string methodName, object[] parameters)
		{
			RagdollBody ragdollBody = new (ragdoll, methodName, parameters);
			if (InstanceFinder.IsServer)
			{
				InstanceFinder.ServerManager.Broadcast(ragdollBody);
			}
			else if (InstanceFinder.IsClient)
			{
				InstanceFinder.ClientManager.Broadcast(ragdollBody);
			}
		}
		private void OnRagdollBroadcast(RagdollBody ragdollBody)
		{
			typeof(Ragdoll).GetMethod(ragdollBody.MethodName).Invoke(ragdollBody.RagdollInstance, ragdollBody.Parameters);
		}
		private void OnClientRagdollBroadcast(NetworkConnection networkConnection, RagdollBody ragdollBody)
		{
			InstanceFinder.ServerManager.Broadcast(ragdollBody);
		}
	}
}