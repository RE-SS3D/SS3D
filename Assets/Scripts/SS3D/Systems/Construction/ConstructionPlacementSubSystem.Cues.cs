using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Audio;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Work cues that carry a timed step alongside the ring (construction.md §11): a welder throws sparks
    /// and ticks, a crowbar scrapes. Unbuffered and one-shot: a late joiner misses work already under way.
    /// There is no weld hiss clip in the project yet; the metal contact flecks and quiet metal hits stand in.
    /// </summary>
    public sealed partial class ConstructionPlacementSubSystem
    {
        private const float WeldSparkInterval = 0.25f;
        private const float ScrapeInterval = 1.4f;

        private sealed class WorkCue
        {
            public int Id;
            public Vector3 Point;
            public Vector3 Normal;
            public ConstructionInput Input;
            public float EndTime;
            public float NextTime;
        }

        private readonly List<WorkCue> _workCues = new();
        private int _nextCueId = 1;

        /// <summary>
        /// Starts the cue for a step at <paramref name="point"/>, facing the worker at <paramref name="worker"/>.
        /// Returns an id for <see cref="ServerEndWorkCue"/>, or 0 when the input has no cue.
        /// </summary>
        [Server]
        public int ServerBeginWorkCue(Vector3 point, Vector3 worker, ConstructionInput input, float seconds)
        {
            if (input != ConstructionInput.Welder && input != ConstructionInput.Crowbar)
            {
                return 0;
            }

            Vector3 normal = worker - point;
            normal.y = 0f;
            normal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            int id = _nextCueId++;
            RpcBeginWorkCue(id, point, normal, (byte)input, seconds);
            return id;
        }

        [Server]
        public void ServerEndWorkCue(int id)
        {
            if (id > 0)
            {
                RpcEndWorkCue(id);
            }
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcBeginWorkCue(int id, Vector3 point, Vector3 normal, byte input, float seconds)
        {
            if (!IsClient)
            {
                return;
            }

            _workCues.Add(new WorkCue
            {
                Id = id,
                Point = point,
                Normal = normal,
                Input = (ConstructionInput)input,
                EndTime = Time.time + seconds,
                NextTime = Time.time,
            });
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcEndWorkCue(int id)
        {
            for (int i = _workCues.Count - 1; i >= 0; i--)
            {
                if (_workCues[i].Id == id)
                {
                    _workCues.RemoveAt(i);
                }
            }
        }

        private void UpdateWorkCues()
        {
            if (_workCues.Count == 0)
            {
                return;
            }

            float now = Time.time;
            for (int i = _workCues.Count - 1; i >= 0; i--)
            {
                WorkCue cue = _workCues[i];
                if (now >= cue.EndTime)
                {
                    _workCues.RemoveAt(i);
                    continue;
                }

                if (now < cue.NextTime)
                {
                    continue;
                }

                if (cue.Input == ConstructionInput.Welder)
                {
                    cue.NextTime = now + WeldSparkInterval;
                    Vector3 jitter = Random.insideUnitSphere * 0.08f;
                    SteelWallDamageVfx.PlayImpact(cue.Point + jitter, cue.Normal, (byte)StructuralMaterial.Metal);
                    PlayCueSound(cue.Point, StructuralAudioTrackIds.MetalHit, volume: 0.12f, pitch: 1.6f);
                }
                else
                {
                    cue.NextTime = now + ScrapeInterval;
                    PlayCueSound(cue.Point, StructuralAudioTrackIds.MetalStrain, volume: 0.45f, pitch: 1.1f);
                }
            }
        }

        private static void PlayCueSound(Vector3 point, string[] clips, float volume, float pitch)
        {
            if (clips == null || clips.Length == 0 || !SubSystems.TryGet(out AudioSubSystem audio))
            {
                return;
            }

            string clip = clips[Random.Range(0, clips.Length)];
            audio.PlayAudioSourceLocal(AudioType.Sfx, clip, point, null, volume: volume, pitch: pitch, minRange: 1f, maxRange: 14f);
        }
    }
}
