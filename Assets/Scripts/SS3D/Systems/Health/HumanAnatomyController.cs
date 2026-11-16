using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Generated;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Maps gameplay zones to nested body-part prefabs and executes severance visuals and world drops.
    /// </summary>
    public class HumanAnatomyController : MonoBehaviour
    {
        private static readonly Dictionary<BodyZone, string> DefaultRootObjectNames = new()
        {
            { BodyZone.Head, "HumanHead" },
            { BodyZone.LeftArm, "HumanArmLeft" },
            { BodyZone.RightArm, "HumanArmRight" },
            { BodyZone.LeftLeg, "HumanLegLeft" },
            { BodyZone.RightLeg, "HumanLegRight" },
        };

        private static readonly Dictionary<BodyZone, string> DefaultSeveredItemIds = new()
        {
            { BodyZone.Head, Items.HumanHead },
            { BodyZone.LeftArm, Items.HumanArmLeft },
            { BodyZone.RightArm, Items.HumanArmRight },
            { BodyZone.LeftLeg, Items.HumanLegLeft },
            { BodyZone.RightLeg, Items.HumanLegRight },
        };

        private HumanHealthController _health;
        private Entity _entity;
        private readonly Dictionary<BodyZone, AnatomyNode> _zoneRoots = new();
        private readonly HashSet<BodyZone> _visualSeverApplied = new();

        public void Initialize(HumanHealthController health)
        {
            _health = health;
            _entity = health.GetComponent<Entity>();
            BuildZoneRootMap();
        }

        public static bool IsSeverableZone(BodyZone zone)
        {
            return zone is BodyZone.Head
                or BodyZone.LeftArm
                or BodyZone.RightArm
                or BodyZone.LeftLeg
                or BodyZone.RightLeg;
        }

        public void ApplyVisualSeverance(BodyZone zone)
        {
            if (!IsSeverableZone(zone) || _visualSeverApplied.Contains(zone))
            {
                return;
            }

            _visualSeverApplied.Add(zone);

            if (_zoneRoots.TryGetValue(zone, out AnatomyNode root))
            {
                root.ApplySeveredVisuals();
            }

            DisableZoneColliders(zone);
        }

        public void ExecuteServerSeverance(BodyZone zone)
        {
            ApplyVisualSeverance(zone);

            if (zone == BodyZone.Head)
            {
                HandleHeadMindSwap();
                return;
            }

            SpawnSeveredDrop(zone);
        }

        private void BuildZoneRootMap()
        {
            _zoneRoots.Clear();
            AnatomyNode[] nodes = _health.GetComponentsInChildren<AnatomyNode>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                AnatomyNode node = nodes[i];
                if (!node.IsDetachable || !IsSeverableZone(node.PrimaryZone) || IsChildOfSameZone(node))
                {
                    continue;
                }

                _zoneRoots.TryAdd(node.PrimaryZone, node);
            }

            foreach (KeyValuePair<BodyZone, string> entry in DefaultRootObjectNames)
            {
                if (_zoneRoots.ContainsKey(entry.Key))
                {
                    continue;
                }

                Transform child = FindChildTransform(_health.transform, entry.Value);
                if (child != null && child.TryGetComponent(out AnatomyNode node))
                {
                    _zoneRoots[entry.Key] = node;
                }
            }
        }

        private bool IsChildOfSameZone(AnatomyNode node)
        {
            Transform parent = node.transform.parent;
            while (parent != null && parent != _health.transform)
            {
                if (parent.TryGetComponent(out AnatomyNode parentNode)
                    && parentNode.IsDetachable
                    && parentNode.PrimaryZone == node.PrimaryZone)
                {
                    return true;
                }

                parent = parent.parent;
            }

            return false;
        }

        private static Transform FindChildTransform(Transform root, string objectName)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private void DisableZoneColliders(BodyZone zone)
        {
            ZoneTargetCollider[] colliders = _health.GetComponentsInChildren<ZoneTargetCollider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].Zone != zone)
                {
                    continue;
                }

                Collider physicsCollider = colliders[i].GetComponent<Collider>();
                if (physicsCollider != null)
                {
                    physicsCollider.enabled = false;
                }
            }
        }

        private void SpawnSeveredDrop(BodyZone zone)
        {
            if (!_zoneRoots.TryGetValue(zone, out AnatomyNode root))
            {
                return;
            }

            Transform anchor = root.SeverAnchor;
            Vector3 position = anchor.position;
            Quaternion rotation = anchor.rotation;

            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            Item dropPrefab = root.ResolveSeveredDropPrefab();
            if (dropPrefab != null && dropPrefab.Asset)
            {
                itemSystem.SpawnItem(dropPrefab.Asset.Id, position, rotation);
                return;
            }

            if (DefaultSeveredItemIds.TryGetValue(zone, out string itemId))
            {
                itemSystem.SpawnItem(itemId, position, rotation);
            }
        }

        private void HandleHeadMindSwap()
        {
            if (_entity == null || _entity.Mind == null || _entity.Mind == Mind.Empty)
            {
                return;
            }

            if (!_zoneRoots.TryGetValue(BodyZone.Head, out AnatomyNode headRoot))
            {
                return;
            }

            Item headPrefab = Assets.Get<Item>(AssetDatabases.Items, Items.HumanHead);
            if (headPrefab == null)
            {
                return;
            }

            Transform anchor = headRoot.SeverAnchor;
            Item spawnedHead = Object.Instantiate(headPrefab, anchor.position, anchor.rotation);
            if (!spawnedHead.TryGetComponent(out Entity headEntity))
            {
                headEntity = spawnedHead.gameObject.AddComponent<Entity>();
            }

            _health.ServerManager.Spawn(spawnedHead.GameObject);

            MindSubSystem mindSystem = SubSystems.Get<MindSubSystem>();
            mindSystem.SwapMinds(_entity, headEntity);

            if (_entity.TryGetComponent(out Human human))
            {
                human.DeactivateComponents();
            }
        }
    }
}
