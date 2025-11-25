using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public sealed class AtmosEnvironmentSubSystem : Core.Behaviours.SubSystem
    {
        public event Action<float> OnAtmosTick;

        private TileSubSystem _tileSubSystem;
        private List<AtmosMap> _atmosMaps;
        private List<AtmosJobPersistentData> _atmosJobs;

        private float _lastStep;
        private bool _initCompleted;

        // This list track job handles to complete jobs after a while.
        private NativeList<JobHandle> _jobHandles;

        // This is purely for debugging purposes. When false, the jobs execute all jobs on the main thread, allowing for easy debugging.
        [SerializeField]
        private bool _usesParallelComputation;

        // True when jobs are scheduled, false after making sure they completed.
        private bool _jobsScheduled;

        [field:SerializeField]
        public float UpdateRate { get; set; }

        [CanBeNull]
        public AtmosContainer GetAtmosContainer(Vector3 worldPosition)
        {
            if (_atmosMaps == null)
            {
                return null;
            }

            foreach (AtmosMap map in _atmosMaps)
            {
                AtmosContainer atmos = map.GetTileAtmosObject(worldPosition);

                if (atmos != null)
                {
                    return atmos;
                }
            }

            return null;
        }

        public List<AtmosJobPersistentData> GetAtmosJobs()
        {
            return _atmosJobs;
        }

        public void ChangeState(Vector3 worldPosition, AtmosState state)
        {
            AtmosContainer tile = GetAtmosContainer(worldPosition);

            if (tile != null)
            {
                _atmosJobs.Find(x => x.Map == tile.Map).ChangeState(tile, state);
            }
        }

        public void AddGasses(Vector3 worldPosition, float4 amount)
        {
            AtmosContainer tile = GetAtmosContainer(worldPosition);

            if (tile != null)
            {
                _atmosJobs.Find(x => x.Map == tile.Map).AddGasses(tile, amount);
            }
        }

        public void RandomizeAllGasses(float maxAmount)
        {
            _atmosJobs.ForEach(job => job.RandomizeAllGasses(maxAmount));
        }

        public void ClearAllGasses()
        {
            _atmosJobs.ForEach(job => job.ClearAllGasses());
        }

        public void RemoveGasses(Vector3 worldPosition, float4 amount)
        {
            AtmosContainer tile = GetAtmosContainer(worldPosition);

            if (tile != null)
            {
                _atmosJobs.Find(x => x.Map == tile.Map).RemoveGasses(tile, amount);
            }
        }

        public void AddHeat(Vector3 worldPosition, float amount)
        {
            AtmosContainer tile = GetAtmosContainer(worldPosition);

            if (tile != null)
            {
                _atmosJobs.Find(x => x.Map == tile.Map).AddHeat(tile, amount);
            }
        }

        public void RemoveHeat(Vector3 worldPosition, float amount)
        {
            AtmosContainer tile = GetAtmosContainer(worldPosition);

            if (tile != null)
            {
                _atmosJobs.Find(x => x.Map == tile.Map).RemoveHeat(tile, amount);
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            if (_atmosMaps == null || _atmosJobs == null)
            {
                return;
            }

            foreach (AtmosMap map in _atmosMaps)
            {
                map.Clear();
            }

            _atmosMaps.Clear();

            foreach (AtmosJobPersistentData job in _atmosJobs)
            {
                job.Destroy();
            }

            _atmosJobs.Clear();
            _jobHandles.Dispose();
        }

        protected override void OnStart()
        {
            base.OnStart();
            _tileSubSystem = Subsystems.Get<TileSubSystem>();

            // Initialization is invoked by the tile manager
            _tileSubSystem.OnTileSystemLoaded += Initialize;
            _jobHandles = new(1, Allocator.Persistent);
        }

        private void Update()
        {
            if (!_initCompleted)
            {
                return;
            }

            if (Time.fixedTime >= _lastStep + UpdateRate)
            {
                // If the time step is smaller than the number of frames waited by the DelayCompleteJob method, then its necessary to ensure jobs are complete before proceeding
                if (_jobsScheduled)
                {
                    CompleteJobs();
                }

                float dt = Time.fixedTime - _lastStep;

                foreach (AtmosJobPersistentData atmosJob in _atmosJobs)
                {
                    atmosJob.Refresh();
                    ScheduleTileObjectJobs(atmosJob, dt);
                }

                _jobsScheduled = true;

                StartCoroutine(DelayCompleteJob());

                _lastStep = Time.fixedTime;

                OnAtmosTick?.Invoke(dt);
            }
        }

        private void CompleteJobs()
        {
            if (_usesParallelComputation)
            {
                JobHandle.CompleteAll(_jobHandles);
                _jobHandles.Clear();
            }

            // Write back the results
            _atmosJobs.ForEach(x => x.WriteResultsToList());
            _jobsScheduled = false;
        }

        private void CreateAtmosMaps()
        {
            int chunkCounter = 0;
            _atmosMaps.Clear();

            // Create identical atmos chunks for each tile chunk
            TileMap map = _tileSubSystem.CurrentMap;

            AtmosMap atmosMap = new(map, map.Name);

            List<TileChunk> tileChunks = map.Chunks;

            foreach (TileChunk chunk in tileChunks)
            {
                atmosMap.CreateChunkFromTileChunk(chunk.Key, chunk.Origin);
                chunkCounter++;
            }

            _atmosMaps.Add(atmosMap);

            Debug.Log("AtmosManager: recreated " + chunkCounter + " chunks from the tilemap");
        }

        private void Initialize()
        {
            _atmosMaps = new();
            _atmosJobs = new();

            if (_tileSubSystem == null || _tileSubSystem.CurrentMap == null)
            {
                Debug.LogError("AtmosManager couldn't find the tilemanager or map.");
                return;
            }

            CreateAtmosMaps();

            foreach (AtmosMap map in _atmosMaps)
            {
                List<AtmosContainer> tiles = new();

                // Add tiles in chunk
                foreach (AtmosChunk chunk in map.GetAtmosChunks())
                {
                    List<AtmosContainer> tileAtmosObjects = chunk.GetAllTileAtmosObjects();
                    tileAtmosObjects.ForEach(tile => tile.Initialize());
                    tiles.AddRange(tileAtmosObjects);
                }

                AtmosJobPersistentData atmosJob = new(map, tiles);
                _atmosJobs.Add(atmosJob);
            }

            _initCompleted = true;
        }

        private void ScheduleTileObjectJobs(AtmosJobPersistentData atmosJob, float deltaTime)
        {
            // compute neighbour indexes of tile atmos objects. TODO :  No need to run this job unless a new chunk was created
            ComputeIndexesJob computeIndexesJob = new(
                atmosJob.NeighbourTileIndexes,
                atmosJob.NativeAtmosTiles,
                atmosJob.ChunkKeyHashMap,
                16);

            SetAtmosStateJob setAtmosStateJob = new(
                atmosJob.NativeAtmosTiles,
                atmosJob.NeighbourTileIndexes,
                atmosJob.ActiveEnvironmentIndexes,
                atmosJob.SemiActiveEnvironmentIndexes);

            ComputeFluxesJob diffusionFluxJob = new(
                atmosJob.NativeAtmosTiles,
                atmosJob.MoleTransferArray,
                atmosJob.NeighbourTileIndexes,
                atmosJob.SemiActiveEnvironmentIndexes.AsDeferredJobArray(),
                deltaTime,
                false);

            TransferFluxesJob transferDiffusionFluxJob = new(
                atmosJob.MoleTransferArray,
                atmosJob.NativeAtmosTiles,
                atmosJob.NeighbourTileIndexes,
                atmosJob.SemiActiveEnvironmentIndexes.AsDeferredJobArray());

            ComputeFluxesJob activeFluxesJob = new(
                atmosJob.NativeAtmosTiles,
                atmosJob.MoleTransferArray,
                atmosJob.NeighbourTileIndexes,
                atmosJob.ActiveEnvironmentIndexes.AsDeferredJobArray(),
                deltaTime,
                true);

            TransferFluxesJob transferActiveFluxesJob = new(
                atmosJob.MoleTransferArray,
                atmosJob.NativeAtmosTiles,
                atmosJob.NeighbourTileIndexes,
                atmosJob.ActiveEnvironmentIndexes.AsDeferredJobArray());

            ComputeVelocityJob velocityJob = new(atmosJob.NativeAtmosTiles, atmosJob.MoleTransferArray, atmosJob.ActiveEnvironmentIndexes.AsDeferredJobArray());

            ComputeHeatTransferJob computeHeatJob = new(
                atmosJob.NativeAtmosTiles,
                atmosJob.HeatTransferArray,
                atmosJob.NeighbourTileIndexes,
                atmosJob.ActiveEnvironmentIndexes.AsDeferredJobArray(),
                deltaTime);

            TransferHeatJob transferHeatJob = new(
                atmosJob.HeatTransferArray,
                atmosJob.NativeAtmosTiles,
                atmosJob.NeighbourTileIndexes,
                atmosJob.ActiveEnvironmentIndexes.AsDeferredJobArray());

            if (_usesParallelComputation)
            {
                JobHandle computeIndexesHandle = computeIndexesJob.Schedule(atmosJob.AtmosTiles.Count, 64);
                JobHandle setActiveHandle = setAtmosStateJob.Schedule(computeIndexesHandle);
                setActiveHandle.Complete();

                JobHandle diffusionFluxHandle = diffusionFluxJob.Schedule(atmosJob.SemiActiveEnvironmentIndexes.Length, 64, setActiveHandle);
                JobHandle transferDiffusionFluxHandle = transferDiffusionFluxJob.Schedule(diffusionFluxHandle);
                JobHandle activeFluxHandle = activeFluxesJob.Schedule(atmosJob.ActiveEnvironmentIndexes.Length, 64, transferDiffusionFluxHandle);
                JobHandle transferActiveFluxHandle = transferActiveFluxesJob.Schedule(activeFluxHandle);
                JobHandle computeVelocityHandle = velocityJob.Schedule(atmosJob.ActiveEnvironmentIndexes.Length, 64, transferActiveFluxHandle);

                JobHandle computeHeatJobHandle = computeHeatJob.Schedule(atmosJob.ActiveEnvironmentIndexes.Length, 64, computeVelocityHandle);
                JobHandle transferHeatJobHandle = transferHeatJob.Schedule(computeHeatJobHandle);

                _jobHandles.Add(computeIndexesHandle);
                _jobHandles.Add(setActiveHandle);
                _jobHandles.Add(diffusionFluxHandle);
                _jobHandles.Add(transferDiffusionFluxHandle);
                _jobHandles.Add(activeFluxHandle);
                _jobHandles.Add(transferActiveFluxHandle);
                _jobHandles.Add(computeVelocityHandle);
            }
            else
            {
                computeIndexesJob.Run(atmosJob.AtmosTiles.Count);
                setAtmosStateJob.Run();
                diffusionFluxJob.Run(atmosJob.SemiActiveEnvironmentIndexes.Length);
                transferDiffusionFluxJob.Run();
                activeFluxesJob.Run(atmosJob.ActiveEnvironmentIndexes.Length);
                transferActiveFluxesJob.Run();
                velocityJob.Run(atmosJob.ActiveEnvironmentIndexes.Length);
                computeHeatJob.Run(atmosJob.ActiveEnvironmentIndexes.Length);
                transferHeatJob.Run();
            }

            // Debug.Log($"Active count : {atmosJob.ActiveEnvironmentIndexes.Length}, SemiActive count : {atmosJob.SemiActiveEnvironmentIndexes.Length}, "
            //    + $"Inactive count : {atmosJob.NativeAtmosTiles.Length - atmosJob.ActiveEnvironmentIndexes.Length - atmosJob.SemiActiveEnvironmentIndexes.Length}");
        }

        /// <summary>
        /// Delay as much as possible the job main thread completion, as the maximum allowed for temp allocation is 4 frame. Delaying allows
        /// spreading the jobs computation as much as possible, as we do not wish to have them all executed on a single frame, that can cause lag spikes.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DelayCompleteJob()
        {
            yield return null;
            yield return null;
            yield return null;

            if (_jobsScheduled)
            {
                CompleteJobs();
            }
        }
    }
}
