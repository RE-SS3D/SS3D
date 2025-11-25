using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// The atmos network manager handle sending visible gas data to clients, a few chunks around them.
    /// </summary>
    public class AtmosNetworkSubManager : NetworkSubSystem
    {
        private struct PreviousValuesChunkCentered
        {
            public byte[] PreviousConcentrationValues;
            public Vector2Int ChunkKey;

            public PreviousValuesChunkCentered(byte[] previousConcentrationValues, Vector2Int chunkKey)
            {
                PreviousConcentrationValues = previousConcentrationValues;
                ChunkKey = chunkKey;
            }
        }

        // the granularity of values representing a concentration, the higher the more the change between two different concentrations
        // can be subtle. But keep in mind setting a higher value for this will mean having to send more data to clients.
        private const byte ConcentrationRange = 16;

        // The amount of moles for which the concentration will look at its maximum on client
        private const int MaxMoles = 1000;

        private readonly Dictionary<NetworkConnection, PreviousValuesChunkCentered> _previousValuesChunkCenteredPlayers = new();

        private List<Entity> _playersEntities = new();

        private AtmosEnvironmentSubSystem _atmosEnvironmentSubSystem;

        // only manipulate that on client
        private byte[] _previousConcentrationValues;

        public override void OnStartServer()
        {
            base.OnStartServer();
            AddHandle(SpawnedPlayersUpdated.AddListener(HandleSpawnedPlayersUpdated));
            _atmosEnvironmentSubSystem = Subsystems.Get<AtmosEnvironmentSubSystem>();
            _atmosEnvironmentSubSystem.OnAtmosTick += HandleAtmosTick;
        }

        /// <summary>
        /// For each observing entities, we want to send the data necessary to visualise visible gas.
        /// </summary>
        [Server]
        private void HandleAtmosTick(float dt)
        {
            foreach (Entity player in _playersEntities)
            {
                if (!_previousValuesChunkCenteredPlayers.ContainsKey(player.Owner))
                {
                    return;
                }

                AtmosContainer atmosTile = _atmosEnvironmentSubSystem.GetAtmosContainer(player.Position);

                if (atmosTile == null)
                {
                    return;
                }

                AtmosChunk chunk = atmosTile.Map.GetChunk(player.Position);

                if (chunk == null)
                {
                    return;
                }

                Vector2Int key = chunk.GetKey();

                byte[] data = RetrieveGasData(player.Position);

                // if we had already values for the chunk, use delta compress for better compression
                if (key == _previousValuesChunkCenteredPlayers[player.Owner].ChunkKey)
                {
                    byte[] deltaCompressedData = DeltaCompress(data, _previousValuesChunkCenteredPlayers[player.Owner].PreviousConcentrationValues);
                    RpcDeltaAtmosData(player.Owner, deltaCompressedData);
                }

                // If we don't have previous values, just compress the raw data.
                else
                {
                    PreviousValuesChunkCentered previousValuesChunkCentered = _previousValuesChunkCenteredPlayers[player.Owner];
                    previousValuesChunkCentered.ChunkKey = key;
                    _previousValuesChunkCenteredPlayers[player.Owner] = previousValuesChunkCentered;
                    byte[] compressedData = Compress(data);
                    RpcAtmosData(player.Owner, compressedData);
                }

                PreviousValuesChunkCentered previousValuesChunk = _previousValuesChunkCenteredPlayers[player.Owner];
                previousValuesChunk.PreviousConcentrationValues = data;
                _previousValuesChunkCenteredPlayers[player.Owner] = previousValuesChunk;
            }
        }

        [TargetRpc]
        private void RpcAtmosData(NetworkConnection connection, byte[] compressedData)
        {
            byte[] decompressed = Decompress(compressedData);
            _previousConcentrationValues = decompressed;
        }

        [TargetRpc]
        private void RpcDeltaAtmosData(NetworkConnection connection, byte[] deltaCompressedData)
        {
            byte[] decompressed = DeltaDecompress(deltaCompressedData);
            _previousConcentrationValues = decompressed;
        }

        /// <summary>
        /// Transform gas concentration value (a float) into a simple array byte for a bunch of chunks around a given position.
        /// Each byte represent some concentration which can vary between 0 and {ConcentrationRange}.
        /// </summary>
        /// <param name="position"> The center position of the entity we want to know gas concentration for.</param>
        /// <returns></returns>
        private byte[] RetrieveGasData(Vector3 position)
        {
            AtmosContainer atmosTile = _atmosEnvironmentSubSystem.GetAtmosContainer(position);
            AtmosChunk[] chunks = atmosTile.Map.GetChunkAndEightNeighbours(position);
            List<byte> chunkBytesList = new();

            foreach (AtmosChunk chunk in chunks)
            {
                if (chunk == null)
                {
                    chunkBytesList.AddRange(new byte[AtmosMap.ChunkSize * AtmosMap.ChunkSize]);
                    continue;
                }

                chunkBytesList.AddRange(
                    chunk.GetAllTileAtmosObjects()
                        .Select(x => Convert.ToByte(math.min((x.AtmosObject.CoreGasses[0] / MaxMoles) * ConcentrationRange, ConcentrationRange))));
            }

            return chunkBytesList.ToArray();
        }

        /// <summary>
        /// Instead of sending the raw data, we can send the difference between the current value and the previous one. This usually should increase repetitivity of the data and therefore allow for
        /// better compression. e.g. you have the data {1,2,3,4,5} and previously it was {0,1,2,3,4} now you got {1,1,1,1,1} which is more compressable.
        /// </summary>
        [Server]
        private byte[] DeltaCompress(byte[] toCompress, byte[] previousValues)
        {
            byte[] compressed;
            byte[] deltas = new byte[toCompress.Length];

            for (int i = 0; i < toCompress.Length; i++)
            {
                deltas[i] = (byte)(toCompress[i] - previousValues[i] + ConcentrationRange);
            }

            using (MemoryStream memoryStream = new())
            {
                using (DeflateStream compressionStream = new(memoryStream, CompressionLevel.Optimal))
                {
                    compressionStream.Write(deltas, 0, toCompress.Length);
                }

                compressed = memoryStream.ToArray();
            }

            return compressed;

            // Stats(toCompress, decompressed, compressed, "lossless");
        }

        [Server]
        private byte[] Compress(byte[] toCompress)
        {
            byte[] compressed;

            using (MemoryStream memoryStream = new())
            {
                using (DeflateStream compressionStream = new(memoryStream, CompressionLevel.Optimal))
                {
                    compressionStream.Write(toCompress, 0, toCompress.Length);
                }

                compressed = memoryStream.ToArray();
            }

            return compressed;
        }

        [Client]
        private byte[] Decompress(byte[] compressedData)
        {
            byte[] decompressed;

            using (MemoryStream inputMemoryStream = new(compressedData))
            using (MemoryStream outputMemoryStream = new())
            using (DeflateStream deflateStream = new(inputMemoryStream, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(outputMemoryStream);
                decompressed = outputMemoryStream.ToArray();
            }

            return decompressed;
        }

        [Client]
        private byte[] DeltaDecompress(byte[] compressedData)
        {
            byte[] decompressDeltas;

            using (MemoryStream inputMemoryStream = new(compressedData))
            using (MemoryStream outputMemoryStream = new())
            using (DeflateStream deflateStream = new(inputMemoryStream, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(outputMemoryStream);
                decompressDeltas = outputMemoryStream.ToArray();
            }

            byte[] decompressed = new byte[AtmosMap.ChunkSize * AtmosMap.ChunkSize];

            for (int i = 0; i < decompressed.Length; i++)
            {
                decompressed[i] = (byte)(_previousConcentrationValues[i] + decompressDeltas[i] - ConcentrationRange);
            }

            return decompressed;
        }

        private void Stats(byte[] originalData, byte[] reconstitutedData, byte[] compressedData, string name)
        {
            int maxError = 0;
            float averageError = 0;
            for (int i = 0; i < originalData.Length; i++)
            {
                maxError = math.max(math.abs(originalData[i] - reconstitutedData[i]), maxError);
                averageError += (math.abs(originalData[i] - reconstitutedData[i]) - averageError) / (i + 1);
            }

            Debug.Log($"method {name}, Original byte array size: {originalData.Length}, compressed data {compressedData.Length}");
            Debug.Log($"method {name} max error between original and reconstituted data {maxError}. Average error {averageError}");
        }

        [Server]
        private void HandleSpawnedPlayersUpdated(ref EventContext context, in SpawnedPlayersUpdated e)
        {
            // todo : should track player controlled entity instead of spawned entity as spawned entity can change
            _playersEntities = e.SpawnedPlayers;
            Entity lastSpawned = e.SpawnedPlayers.Last();
            AtmosContainer atmosTile = _atmosEnvironmentSubSystem.GetAtmosContainer(lastSpawned.Position);
            _previousValuesChunkCenteredPlayers[lastSpawned.Owner] =
                new(RetrieveGasData(lastSpawned.Position), atmosTile.Chunk.GetKey());
            RpcSendPreviousValues(lastSpawned.Owner, _previousValuesChunkCenteredPlayers[lastSpawned.Owner].PreviousConcentrationValues);
        }

        [TargetRpc]
        private void RpcSendPreviousValues(NetworkConnection connection, byte[] previousValues)
        {
            _previousConcentrationValues = previousValues;
        }
    }
}
