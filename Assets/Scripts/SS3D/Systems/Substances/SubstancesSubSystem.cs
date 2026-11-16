using System;
using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Systems.Substances
{
    public sealed class SubstancesSubSystem : NetworkSubSystem
    {
        [SerializeField]
        private ReagentRegistry _registry;

        private ReactionResolver _resolver;
        private readonly List<SubstanceContainer> _temperatureContainers = new();
        private bool _hazardStubSubscribed;

        public ReagentRegistry Registry => _registry;
        public ReactionResolver Resolver => _resolver;
        public IReadOnlyList<RecipeDefinition> Recipes => _registry != null ? _registry.Recipes : Array.Empty<RecipeDefinition>();

        protected override void OnAwake()
        {
            base.OnAwake();
            RebuildResolver();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (!_hazardStubSubscribed)
            {
                SubstanceContainer.HazardOccurred += OnHazardStub;
                _hazardStubSubscribed = true;
            }
        }

        public override void OnStopServer()
        {
            if (_hazardStubSubscribed)
            {
                SubstanceContainer.HazardOccurred -= OnHazardStub;
                _hazardStubSubscribed = false;
            }

            _temperatureContainers.Clear();
            base.OnStopServer();
        }

        private void Update()
        {
            if (!IsServer || _temperatureContainers.Count == 0)
            {
                return;
            }

            float dt = Time.deltaTime;
            for (int i = _temperatureContainers.Count - 1; i >= 0; i--)
            {
                SubstanceContainer container = _temperatureContainers[i];
                if (container == null)
                {
                    _temperatureContainers.RemoveAt(i);
                    continue;
                }

                container.TickTemperature(dt);
            }
        }

        public void RegisterContainer(SubstanceContainer container)
        {
            if (container == null || _temperatureContainers.Contains(container))
            {
                return;
            }

            _temperatureContainers.Add(container);
        }

        public void UnregisterContainer(SubstanceContainer container)
        {
            _temperatureContainers.Remove(container);
        }

        public bool TryGetReagent(string reagentId, out ReagentDefinition definition)
        {
            if (_registry == null)
            {
                definition = null;
                return false;
            }

            return _registry.TryGet(reagentId, out definition);
        }

        public void RebuildResolver()
        {
            _registry?.Initialize();
            _resolver = new ReactionResolver(_registry, Recipes);
        }

        private static void OnHazardStub(SubstanceContainer container, HazardKind hazard, Vector3 position)
        {
            Log.Information(
                nameof(SubstancesSubSystem),
                "Hazard stub: {hazard} from {container} at {pos}",
                Logs.Generic,
                hazard,
                container != null ? container.name : "null",
                position);
        }
    }
}
