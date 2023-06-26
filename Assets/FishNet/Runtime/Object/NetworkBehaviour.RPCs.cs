﻿using FishNet.Connection;
using FishNet.Documenting;
using FishNet.Managing.Logging;
using FishNet.Managing.Transporting;
using FishNet.Object.Delegating;
using FishNet.Serializing;
using FishNet.Serializing.Helping;
using FishNet.Transporting;
using FishNet.Utility.Extension;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FishNet.Object
{


    public abstract partial class NetworkBehaviour : MonoBehaviour
    {
        #region Private.
        /// <summary>
        /// Registered ServerRpc methods.
        /// </summary>
        private readonly Dictionary<uint, ServerRpcDelegate> _serverRpcDelegates = new Dictionary<uint, ServerRpcDelegate>();
        /// <summary>
        /// Registered ObserversRpc methods.
        /// </summary>
        private readonly Dictionary<uint, ClientRpcDelegate> _observersRpcDelegates = new Dictionary<uint, ClientRpcDelegate>();
        /// <summary>
        /// Registered TargetRpc methods.
        /// </summary>
        private readonly Dictionary<uint, ClientRpcDelegate> _targetRpcDelegates = new Dictionary<uint, ClientRpcDelegate>();
        /// <summary>
        /// Number of total RPC methods for scripts in the same inheritance tree for this instance.
        /// </summary>
        private uint _rpcMethodCount;
        /// <summary>
        /// Size of every rpcHash for this networkBehaviour.
        /// </summary>
        private byte _rpcHashSize = 1;
        /// <summary>
        /// RPCs buffered for new clients.
        /// </summary>
        private Dictionary<uint, (PooledWriter, Channel)> _bufferedRpcs = new Dictionary<uint, (PooledWriter, Channel)>();
        /// <summary>
        /// Connections to exclude from RPCs, such as ExcludeOwner or ExcludeServer.
        /// </summary>
        private HashSet<NetworkConnection> _networkConnectionCache = new HashSet<NetworkConnection>();
        #endregion

        /// <summary>
        /// Called when buffered RPCs should be sent.
        /// </summary>
        internal void SendBufferedRpcs(NetworkConnection conn)
        {
            TransportManager tm = _networkObjectCache.NetworkManager.TransportManager;
            foreach ((PooledWriter writer, Channel ch) in _bufferedRpcs.Values)
                tm.SendToClient((byte)ch, writer.GetArraySegment(), conn);
        }

        /// <summary>
        /// Registers a RPC method.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="del"></param>
        [APIExclude]
        [CodegenMakePublic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RegisterServerRpc(uint hash, ServerRpcDelegate del)
        {
            if (_serverRpcDelegates.TryGetValueIL2CPP(hash, out ServerRpcDelegate currentDelegate))
            {
                FishNet.Managing.NetworkManager.StaticLogError($"ServerRpc hash {hash} registered multiple times. First registration by {currentDelegate.Method.DeclaringType.GetType().FullName}. New registration by {GetType().FullName}.");
            }
            else
            {
                _serverRpcDelegates[hash] = del;
                IncreaseRpcMethodCount();
            }
        }
        /// <summary>
        /// Registers a RPC method.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="del"></param>
        [APIExclude]
        [CodegenMakePublic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RegisterObserversRpc(uint hash, ClientRpcDelegate del)
        {
            if (_observersRpcDelegates.TryGetValueIL2CPP(hash, out ClientRpcDelegate currentDelegate))
            {
                FishNet.Managing.NetworkManager.StaticLogError($"ObserverRpc hash {hash} registered multiple times. First registration by {currentDelegate.Method.DeclaringType.GetType().FullName}. New registration by {GetType().FullName}.");
            }
            else
            {
                _observersRpcDelegates[hash] = del;
                IncreaseRpcMethodCount();
            }
        }
        /// <summary>
        /// Registers a RPC method.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="del"></param>
        [APIExclude]
        [CodegenMakePublic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RegisterTargetRpc(uint hash, ClientRpcDelegate del)
        {
            if (_targetRpcDelegates.TryGetValueIL2CPP(hash, out ClientRpcDelegate currentDelegate))
            {
                FishNet.Managing.NetworkManager.StaticLogError($"TargetRpc hash {hash} registered multiple times. First registration by {currentDelegate.Method.DeclaringType.GetType().FullName}. New registration by {GetType().FullName}.");
            }
            else
            {
                _targetRpcDelegates[hash] = del;
                IncreaseRpcMethodCount();
            }
        }

        /// <summary>
        /// Increases rpcMethodCount and rpcHashSize.
        /// </summary>
        private void IncreaseRpcMethodCount()
        {
            _rpcMethodCount++;
            if (_rpcMethodCount <= byte.MaxValue)
                _rpcHashSize = 1;
            else
                _rpcHashSize = 2;
        }

        /// <summary>
        /// Clears all buffered RPCs for this NetworkBehaviour.
        /// </summary>
        public void ClearBuffedRpcs()
        {
            foreach ((PooledWriter writer, Channel _) in _bufferedRpcs.Values)
                writer.Store();
            _bufferedRpcs.Clear();
        }

        /// <summary>
        /// Reads a RPC hash.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private uint ReadRpcHash(PooledReader reader)
        {
            if (_rpcHashSize == 1)
                return reader.ReadByte();
            else
                return reader.ReadUInt16();
        }
        /// <summary>
        /// Called when a ServerRpc is received.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnServerRpc(PooledReader reader, NetworkConnection sendingClient, Channel channel)
        {
            uint methodHash = ReadRpcHash(reader);

            if (sendingClient == null)
            {
                _networkObjectCache.NetworkManager.LogError($"NetworkConnection is null. ServerRpc {methodHash} on object {gameObject.name} [id {ObjectId}] will not complete. Remainder of packet may become corrupt.");
                return;
            }

            if (_serverRpcDelegates.TryGetValueIL2CPP(methodHash, out ServerRpcDelegate data))
                data.Invoke(reader, channel, sendingClient);
            else
                _networkObjectCache.NetworkManager.LogWarning($"ServerRpc not found for hash {methodHash} on object {gameObject.name} [id {ObjectId}]. Remainder of packet may become corrupt.");
        }

        /// <summary>
        /// Called when an ObserversRpc is received.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnObserversRpc(uint? methodHash, PooledReader reader, Channel channel)
        {
            if (methodHash == null)
                methodHash = ReadRpcHash(reader);

            if (_observersRpcDelegates.TryGetValueIL2CPP(methodHash.Value, out ClientRpcDelegate del))
                del.Invoke(reader, channel);
            else
                _networkObjectCache.NetworkManager.LogWarning($"ObserversRpc not found for hash {methodHash.Value} on object {gameObject.name} [id {ObjectId}] . Remainder of packet may become corrupt.");
        }

        /// <summary>
        /// Called when an TargetRpc is received.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnTargetRpc(uint? methodHash, PooledReader reader, Channel channel)
        {
            if (methodHash == null)
                methodHash = ReadRpcHash(reader);

            if (_targetRpcDelegates.TryGetValueIL2CPP(methodHash.Value, out ClientRpcDelegate del))
                del.Invoke(reader, channel);
            else
                _networkObjectCache.NetworkManager.LogWarning($"TargetRpc not found for hash {methodHash.Value} on object {gameObject.name} [id {ObjectId}] . Remainder of packet may become corrupt.");
        }

        /// <summary>
        /// Sends a RPC to server.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="methodWriter"></param>
        /// <param name="channel"></param>
        [CodegenMakePublic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void SendServerRpc_Internal(uint hash, PooledWriter methodWriter, Channel channel)
        {
            if (!IsSpawnedWithWarning())
                return;

            PooledWriter writer = CreateRpc(hash, methodWriter, PacketId.ServerRpc, channel);
            _networkObjectCache.NetworkManager.TransportManager.SendToServer((byte)channel, writer.GetArraySegment());
            writer.StoreLength();
        }

        /// <summary>
        /// Sends a RPC to observers.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="methodWriter"></param>
        /// <param name="channel"></param>
        [APIExclude]
        [CodegenMakePublic] //Make internal.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void SendObserversRpc_Internal(uint hash, PooledWriter methodWriter, Channel channel, bool buffered, bool excludeServer, bool excludeOwner)
        {
            if (!IsSpawnedWithWarning())
                return;

            PooledWriter writer;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (NetworkManager.DebugManager.ObserverRpcLinks && _rpcLinks.TryGetValueIL2CPP(hash, out RpcLinkType link))
#else
            if (_rpcLinks.TryGetValueIL2CPP(hash, out RpcLinkType link))
#endif
                writer = CreateLinkedRpc(link, methodWriter, channel);
            else
                writer = CreateRpc(hash, methodWriter, PacketId.ObserversRpc, channel);

            SetNetworkConnectionCache(excludeServer, excludeOwner);
            _networkObjectCache.NetworkManager.TransportManager.SendToClients((byte)channel, writer.GetArraySegment(), _networkObjectCache.Observers, _networkConnectionCache, true);

            /* If buffered then dispose of any already buffered
             * writers and replace with new one. Writers should
             * automatically dispose when references are lost
             * anyway but better safe than sorry. */
            if (buffered)
            {
                if (_bufferedRpcs.TryGetValueIL2CPP(hash, out (PooledWriter pw, Channel ch) result))
                    result.pw.StoreLength();
                _bufferedRpcs[hash] = (writer, channel);
            }
            //If not buffered then dispose immediately.
            else
            {
                writer.StoreLength();
            }
        }

        /// <summary>
        /// Sends a RPC to target.
        /// </summary>
        [CodegenMakePublic] //Make internal.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void SendTargetRpc_Internal(uint hash, PooledWriter methodWriter, Channel channel, NetworkConnection target, bool excludeServer, bool validateTarget = true)
        {
            if (!IsSpawnedWithWarning())
                return;

            if (validateTarget)
            {
                if (target == null)
                {
                    _networkObjectCache.NetworkManager.LogWarning($"Action cannot be completed as no Target is specified.");
                    return;
                }
                else
                {
                    //If target is not an observer.
                    if (!_networkObjectCache.Observers.Contains(target))
                    {
                        _networkObjectCache.NetworkManager.LogWarning($"Action cannot be completed as Target is not an observer for object {gameObject.name} [id {ObjectId}].");
                        return;
                    }
                }
            }

            //Excluding server.
            if (excludeServer && target.IsLocalClient)
                return;

            PooledWriter writer;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (NetworkManager.DebugManager.TargetRpcLinks && _rpcLinks.TryGetValueIL2CPP(hash, out RpcLinkType link))
#else
            if (_rpcLinks.TryGetValueIL2CPP(hash, out RpcLinkType link))
#endif
                writer = CreateLinkedRpc(link, methodWriter, channel);
            else
                writer = CreateRpc(hash, methodWriter, PacketId.TargetRpc, channel);

            _networkObjectCache.NetworkManager.TransportManager.SendToClient((byte)channel, writer.GetArraySegment(), target);
            writer.Store();
        }

        /// <summary>
        /// Adds excluded connections to ExcludedRpcConnections.
        /// </summary>
        private void SetNetworkConnectionCache(bool addClientHost, bool addOwner)
        {
            _networkConnectionCache.Clear();
            if (addClientHost && IsClient)
                _networkConnectionCache.Add(LocalConnection);
            if (addOwner && Owner.IsValid)
                _networkConnectionCache.Add(Owner);
        }


        /// <summary>
        /// Returns if spawned and throws a warning if not.
        /// </summary>
        /// <returns></returns>
        private bool IsSpawnedWithWarning()
        {
            bool result = this.IsSpawned;
            if (!result)
                _networkObjectCache.NetworkManager.LogWarning($"Action cannot be completed as object {gameObject.name} [Id {ObjectId}] is not spawned.");

            return result;
        }

        /// <summary>
        /// Writes a full RPC and returns the writer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PooledWriter CreateRpc(uint hash, PooledWriter methodWriter, PacketId packetId, Channel channel)
        {
            int rpcHeaderBufferLength = GetEstimatedRpcHeaderLength();
            int methodWriterLength = methodWriter.Length;
            //Writer containing full packet.
            PooledWriter writer = WriterPool.Retrieve(rpcHeaderBufferLength + methodWriterLength);
            writer.WritePacketId(packetId);
            writer.WriteNetworkBehaviour(this);
            //Only write length if reliable.
            if (channel == Channel.Reliable)
                writer.WriteLength(methodWriterLength + _rpcHashSize);
            //Hash and data.
            WriteRpcHash(hash, writer);
            writer.WriteArraySegment(methodWriter.GetArraySegment());

            return writer;
        }

        /// <summary>
        /// Writes rpcHash to writer.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="writer"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteRpcHash(uint hash, PooledWriter writer)
        {
            if (_rpcHashSize == 1)
                writer.WriteByte((byte)hash);
            else
                writer.WriteUInt16((byte)hash);
        }
    }


}