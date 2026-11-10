using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FishNet.Object;
using NUnit.Framework;
using UnityEngine;

namespace SS3D.Tests.AssetAudit
{
    /// <summary>
    /// Guardrail G1 of the tech-debt paydown (Documents/architecture/2026-11_tech-debt-paydown.md, Phase 0).
    /// Every ServerRpc that any client may call (<c>RequireOwnership = false</c>), and every ServerRpc that takes a
    /// position, rotation or object reference from the client, must be listed here with the check that protects
    /// it. A new RPC of that kind fails this test until its author names its check. The <see cref="AuthorityCheck.Unchecked"/>
    /// entries are known holes (TECH_DEBT §1.19); Phase 1 removes them, and the list may only shrink.
    /// FishNet's weaver keeps <see cref="ServerRpcAttribute"/> on the original method, so reflection sees it.
    /// </summary>
    public sealed class ServerRpcAuthorityAuditTests
    {
        private enum AuthorityCheck
        {
            /// <summary>The server returns unless the caller is an administrator.</summary>
            Admin,
            /// <summary><c>MapEditorPermissions.TryAuthorize</c>: admin, or a non-Restricted player on <c>-sandbox</c>.</summary>
            MapEditorPermission,
            /// <summary>The server checks that the caller owns the object argument (or that it belongs to the caller's body).</summary>
            OwnerOfArgument,
            /// <summary>The server acts only on the caller's own connection, player or body; arguments are intent.</summary>
            ServerDerived,
            /// <summary>Answers the caller and changes no game state.</summary>
            ReadOnly,
            /// <summary>The server re-runs the gameplay rule (range, line of sight, <c>CanInteract</c>) against its own copy of the caller's body.</summary>
            Revalidated,
            /// <summary>The caller must be a registered viewer of the interface, which it became through a range-checked interaction.</summary>
            Viewer,
            /// <summary>Checks the caller's role against the command's access level. Owned by the console rework (plan Decision 12).</summary>
            ConsoleAccessLevel,
            /// <summary>The RPC lives on an object no client owns, so <c>RequireOwnership</c> rejects every client call.</summary>
            NoClientOwner,
            /// <summary>Known hole. Only Phase 1 items may hold this, and they remove it.</summary>
            Unchecked,
        }

        /// <summary>Key: <c>DeclaringType.Name + "." + Method.Name</c>.</summary>
        private static readonly Dictionary<string, AuthorityCheck> Allowlist = new()
        {
            // Combat
            { "CombatInteractionNetwork.CmdSyncMeleeAim", AuthorityCheck.Unchecked }, // P1.1: client ray origin
            { "CombatInteractionNetwork.CmdThrow", AuthorityCheck.Unchecked }, // P1.5: target height and NaN unchecked

            // Entities and body
            { "EntitySubSystem.CmdSpawnLatePlayer", AuthorityCheck.OwnerOfArgument },
            { "EntitySubSystem.CmdRespawn", AuthorityCheck.ServerDerived },
            { "MindSubSystem.CmdSwapMinds", AuthorityCheck.NoClientOwner },
            { "Ragdoll.KnockdownTimeless", AuthorityCheck.Unchecked }, // P1.2
            { "Ragdoll.Knockdown", AuthorityCheck.Unchecked }, // P1.2
            { "Ragdoll.Recover", AuthorityCheck.Unchecked }, // P1.2
            { "HumanoidBodyStateMachine.CmdTrySit", AuthorityCheck.Revalidated },
            { "Draggable.SetDragServer", AuthorityCheck.Unchecked }, // P1.4: client-supplied dragger

            // Interactions and inventory
            { "InteractionController.CmdStartTakeFromCharacter", AuthorityCheck.Revalidated },
            { "InteractionController.CmdRunInteraction", AuthorityCheck.Revalidated },
            { "InteractionController.CmdRunInventoryInteraction", AuthorityCheck.Revalidated },
            { "HumanInventory.CmdDropItem", AuthorityCheck.Revalidated },
            { "HumanInventory.CmdPlaceItemInWorld", AuthorityCheck.Revalidated },
            { "HumanInventory.CmdTransferItem", AuthorityCheck.Revalidated },
            { "Hands.CmdSetActiveHand", AuthorityCheck.OwnerOfArgument },
            { "ContainerViewer.CmdContainerClose", AuthorityCheck.ServerDerived },
            { "ItemSubSystem.CmdSpawnItem", AuthorityCheck.Unchecked }, // P1.3
            { "ItemSubSystem.CmdSpawnItemInContainer", AuthorityCheck.Unchecked }, // P1.3

            // Rounds, gamemodes, console
            { "GamemodeSubSystem.CmdGetCurrentClientObjectives", AuthorityCheck.ReadOnly },
            { "RoundSubSystem.CmdResolveMapStop", AuthorityCheck.Admin },
            { "RoundSubSystem.CmdReadServerSettings", AuthorityCheck.Admin },
            { "RoundSubSystem.CmdSaveServerSettings", AuthorityCheck.Admin },
            { "RoundSubSystem.CmdUpdateServerSetting", AuthorityCheck.Admin },
            { "RoundSubSystem.CmdRoundAdminAction", AuthorityCheck.Admin },
            { "CommandsController.CmdProcessOnlineCommand", AuthorityCheck.ConsoleAccessLevel },

            // Tile and Map Editor
            { "TileSubSystem.RpcPlaceObject", AuthorityCheck.Unchecked }, // P1.7: dead, bypasses sandbox rules
            { "TileSubSystem.RpcClearTileObject", AuthorityCheck.Unchecked }, // P1.7
            { "TileSubSystem.RpcClearItemObject", AuthorityCheck.Unchecked }, // P1.7
            { "TileSubSystem.RpcSetFloorDecal", AuthorityCheck.Unchecked }, // P1.7
            { "TileSubSystem.RpcClearFloorDecal", AuthorityCheck.Unchecked }, // P1.7
            { "ConstructionHologramManager.RpcSendCanBuild", AuthorityCheck.Unchecked }, // P1.7
            { "MapEditorSubSystem.RpcInspectArea", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcInspectSelectedArea", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcApplyArea", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcCreateRoom", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcPaintArea", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcSetAlwaysPowerLights", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcSetInvincible", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcSaveMap", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcExportMap", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcValidateMap", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcLoadMap", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcDeleteMap", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcNewMap", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcExecuteCommands", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcUndo", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcRedo", AuthorityCheck.MapEditorPermission },
            { "MapEditorSubSystem.RpcMoveObject", AuthorityCheck.Unchecked }, // P1.7: authorized but skips the rate limiter
            { "MapEditorSubSystem.RpcRequestUndoState", AuthorityCheck.MapEditorPermission },

            // Machine interfaces
            { "MachineInterfaceBehaviour.CmdSetControl", AuthorityCheck.Viewer },
            { "MachineInterfaceBehaviour.CmdSetNumericControl", AuthorityCheck.Viewer },
            { "MachineInterfaceBehaviour.CmdSetActionControl", AuthorityCheck.Viewer },
            { "MachineInterfaceBehaviour.CmdCloseInterface", AuthorityCheck.Viewer },
        };

        /// <summary>
        /// The only RPCs allowed to be <see cref="AuthorityCheck.Unchecked"/>: the ones Phase 1 fixes. Remove a name
        /// when its fix ships. Adding a name here needs the owner's sign-off (testing philosophy rule 7).
        /// </summary>
        private static readonly HashSet<string> Phase1Unchecked = new()
        {
            "CombatInteractionNetwork.CmdSyncMeleeAim",
            "CombatInteractionNetwork.CmdThrow",
            "Ragdoll.KnockdownTimeless",
            "Ragdoll.Knockdown",
            "Ragdoll.Recover",
            "Draggable.SetDragServer",
            "ItemSubSystem.CmdSpawnItem",
            "ItemSubSystem.CmdSpawnItemInContainer",
            "TileSubSystem.RpcPlaceObject",
            "TileSubSystem.RpcClearTileObject",
            "TileSubSystem.RpcClearItemObject",
            "TileSubSystem.RpcSetFloorDecal",
            "TileSubSystem.RpcClearFloorDecal",
            "ConstructionHologramManager.RpcSendCanBuild",
            "MapEditorSubSystem.RpcMoveObject",
        };

        private static readonly Type[] TrustedArgumentTypes =
        {
            typeof(Vector3), typeof(Quaternion), typeof(GameObject), typeof(NetworkObject),
        };

        [Test]
        public void Reflection_SeesServerRpcAttributes()
        {
            // If the weaver ever strips the attribute, every other test here would pass vacuously.
            Assert.That(AllServerRpcs().Count(), Is.GreaterThan(50),
                "Reflection found almost no [ServerRpc] methods; the audit must scan source instead.");
        }

        [Test]
        public void EveryAuditedServerRpc_NamesItsCheck()
        {
            List<string> missing = AuditedServerRpcs().Select(Key).Where(k => !Allowlist.ContainsKey(k))
                .Distinct().OrderBy(k => k).ToList();
            Assert.That(missing, Is.Empty,
                "These ServerRpcs take a client position, rotation or object, or allow any caller, but name no check. "
                + "Add the server-side check, then list the RPC in ServerRpcAuthorityAuditTests.Allowlist:\n  "
                + string.Join("\n  ", missing));
        }

        [Test]
        public void Allowlist_HasNoStaleEntries()
        {
            HashSet<string> audited = new(AuditedServerRpcs().Select(Key));
            List<string> stale = Allowlist.Keys.Where(k => !audited.Contains(k)).OrderBy(k => k).ToList();
            Assert.That(stale, Is.Empty,
                "These allowlist entries no longer match an audited ServerRpc; remove them:\n  " + string.Join("\n  ", stale));
        }

        [Test]
        public void UncheckedEntries_AreOnlyPhase1Items()
        {
            List<string> unexpected = Allowlist.Where(p => p.Value == AuthorityCheck.Unchecked && !Phase1Unchecked.Contains(p.Key))
                .Select(p => p.Key).OrderBy(k => k).ToList();
            Assert.That(unexpected, Is.Empty,
                "Only the RPCs Phase 1 fixes may be Unchecked; add a server-side check instead:\n  " + string.Join("\n  ", unexpected));

            List<string> fixedButListed = Phase1Unchecked.Where(k => Allowlist.TryGetValue(k, out AuthorityCheck c) && c != AuthorityCheck.Unchecked)
                .OrderBy(k => k).ToList();
            Assert.That(fixedButListed, Is.Empty,
                "These RPCs have a check now; remove them from Phase1Unchecked:\n  " + string.Join("\n  ", fixedButListed));
        }

        private static string Key(MethodInfo method) => method.DeclaringType.Name + "." + method.Name;

        private static IEnumerable<MethodInfo> AuditedServerRpcs()
        {
            foreach (MethodInfo method in AllServerRpcs())
            {
                ServerRpcAttribute attribute = method.GetCustomAttribute<ServerRpcAttribute>(false);
                if (!attribute.RequireOwnership || method.GetParameters().Any(p => TakesTrustedArgument(p.ParameterType)))
                {
                    yield return method;
                }
            }
        }

        private static bool TakesTrustedArgument(Type type)
        {
            if (type.IsArray)
            {
                type = type.GetElementType();
            }

            return TrustedArgumentTypes.Contains(type) || typeof(NetworkBehaviour).IsAssignableFrom(type);
        }

        private static IEnumerable<MethodInfo> AllServerRpcs()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (!name.StartsWith("SS3D", StringComparison.Ordinal) || name.StartsWith("SS3D.Tests", StringComparison.Ordinal))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (Type type in types)
                {
                    foreach (MethodInfo method in type.GetMethods(flags))
                    {
                        if (method.IsDefined(typeof(ServerRpcAttribute), false))
                        {
                            yield return method;
                        }
                    }
                }
            }
        }
    }
}
