using System;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.IngameConsoleSystem.Commands;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem
{
    /// <summary>
    /// Network shell around <see cref="CommandRegistry"/>: routes a typed line to where its command runs,
    /// checks the caller's role on the server, writes the admin action log, and shows answers.
    /// </summary>
    public class CommandsController : NetworkActor
    {
        [SerializeField] private ConsolePanelView _console;

        private readonly CommandResolver _resolver = new();
        private CommandRegistry _registry;

        /// <summary>Every answer this client shows in its console, local or from the server. Scenarios await it.</summary>
        public event Action<string> AnswerShown;

        public CommandRegistry Registry => _registry ??= new CommandRegistry(CommandRegistry.DiscoverCommands());

        protected override void OnStart()
        {
            _ = Registry;
        }

        /// <summary>
        /// Run a typed line. <c>help</c>, unknown commands and Local commands answer immediately and return their
        /// result; Server and Client commands go through the server and return null (their answer arrives
        /// through <see cref="AnswerShown"/>). The harness's <c>wait_console</c> retries while this returns a
        /// result that isn't <see cref="CommandResult.Ok"/>.
        /// </summary>
        public CommandResult? ClientProcessCommand(string commandLine)
        {
            if (!CommandRegistry.TrySplitLine(commandLine, out string name, out string[] args))
            {
                return null;
            }

            ServerRoleTypes role = LocalRole();
            if (name == CommandRegistry.HelpCommandName)
            {
                return Show(CommandResult.Success(Registry.HelpAnswer(args, role)));
            }

            if (!Registry.TryGet(name, out Command command))
            {
                return Show(CommandResult.Failure(Registry.UnknownCommandText(name, role)));
            }

            if (command.Type == CommandType.Local)
            {
                CommandContext context = new(null, LocalPlayer.Ckey, role, LocalEntity(), commandLine);
                return Show(Registry.Execute(command, args, context, _resolver, out _));
            }

            if (!IsClient)
            {
                return Show(CommandResult.Error($"{name} needs a connection to a server"));
            }

            CmdProcessOnlineCommand(commandLine);
            return null;
        }

        /// <summary>Check the caller's role, then run the command here or on the caller's client.</summary>
        [ServerRpc(RequireOwnership = false)]
        private void CmdProcessOnlineCommand(string commandLine, NetworkConnection conn = null)
        {
            if (!CommandRegistry.TrySplitLine(commandLine, out string name, out string[] args))
            {
                return;
            }

            string ckey = SubSystems.Get<PlayerSubSystem>().GetCkey(conn);
            if (!SubSystems.Get<PermissionSubSystem>().TryGetUserRole(ckey, out ServerRoleTypes role))
            {
                CommandAnswer(CommandResult.Error($"no role found for {ckey}, can't run commands").Message, conn);
                return;
            }

            if (!Registry.TryGet(name, out Command command))
            {
                CommandAnswer(Registry.UnknownCommandText(name, role), conn);
                return;
            }

            if (!CommandRegistry.CanRun(command, role))
            {
                CommandAnswer(CommandResult.Error($"{name} needs {command.AccessLevel}; you are {role}").Message, conn);
                return;
            }

            switch (command.Type)
            {
                case CommandType.Server:
                    CommandContext context = new(conn, ckey, role, CallerEntity(conn), commandLine);
                    CommandResult result = Registry.Execute(command, args, context, _resolver, out CommandArgs bound);
                    if (result.Ok && command.AccessLevel >= ServerRoleTypes.Administrator)
                    {
                        LogAdminAction(ckey, commandLine, bound);
                    }

                    CommandAnswer(result.Message, conn);
                    break;

                case CommandType.Client:
                    RpcPerformOnClient(conn, commandLine, role);
                    break;

                case CommandType.Local:
                    // Local commands never reach the server; run them in place rather than drop the line.
                    CommandAnswer(Registry.Execute(command, args, new CommandContext(conn, ckey, role, CallerEntity(conn), commandLine), _resolver, out _).Message, conn);
                    break;
            }
        }

        /// <summary>
        /// admin-tools.md §1: every admin action writes a line naming who, what and to whom. Permission checks are not logged.
        /// </summary>
        private void LogAdminAction(string ckey, string commandLine, CommandArgs bound)
        {
            string targets = bound != null && bound.Targets.Count > 0
                ? string.Join(", ", bound.Targets.Select(t => t.DisplayName))
                : "-";
            Log.Information(this, "AdminAction {admin} ran \"{line}\" targeting {targets}", Logs.ServerOnly, ckey, commandLine, targets);
        }

        /// <summary>Run a Client command on the caller's client once the server has checked the role.</summary>
        [TargetRpc]
        private void RpcPerformOnClient(NetworkConnection conn, string commandLine, ServerRoleTypes role)
        {
            if (!CommandRegistry.TrySplitLine(commandLine, out string name, out string[] args)
                || !Registry.TryGet(name, out Command command))
            {
                return;
            }

            CommandContext context = new(null, LocalPlayer.Ckey, role, LocalEntity(), commandLine);
            Show(Registry.Execute(command, args, context, _resolver, out _));
        }

        private static Entity CallerEntity(NetworkConnection conn)
        {
            if (conn == null || !SubSystems.TryGet(out EntitySubSystem entities))
            {
                return null;
            }

            if (entities.TryGetSpawnedEntity(conn, out Entity entity) && entity != null)
            {
                return entity;
            }

            return entities.TryGetOwnedEntity(conn, out entity) ? entity : null;
        }

        private Entity LocalEntity()
        {
            if (!IsClient)
            {
                return null;
            }

            return CallerEntity(LocalConnection);
        }

        /// <summary>The local player's role for filtering help; User when there is no session yet.</summary>
        private static ServerRoleTypes LocalRole()
        {
            if (!string.IsNullOrEmpty(LocalPlayer.Ckey)
                && SubSystems.TryGet(out PermissionSubSystem permissions)
                && permissions.TryGetUserRole(LocalPlayer.Ckey, out ServerRoleTypes role))
            {
                return role;
            }

            return ServerRoleTypes.User;
        }

        private CommandResult Show(CommandResult result)
        {
            ShowAnswer(result.Message);
            return result;
        }

        /// <summary>Send an answer to the caller, or show it here when there is no remote caller.</summary>
        private void CommandAnswer(string answer, NetworkConnection conn = null)
        {
            if (IsServer && conn != null)
            {
                RpcCommandAnswer(conn, answer);
            }
            else
            {
                ShowAnswer(answer);
            }
        }

        [TargetRpc]
        private void RpcCommandAnswer(NetworkConnection conn, string answer)
        {
            ShowAnswer(answer);
        }

        private void ShowAnswer(string answer)
        {
            if (_console != null)
            {
                _console.AddText(answer);
            }

            AnswerShown?.Invoke(answer);
        }
    }
}
