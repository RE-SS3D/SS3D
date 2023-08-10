using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.IngameConsoleSystem.Commands;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem
{
    public class CommandsController : NetworkActor
    {
        /// <summary>
        /// Contains all command infos and their names in lowercase
        /// </summary>
        private readonly Dictionary<string, Command> _allCommands = new();
        /// <summary>
        /// Length of space between columns in help command
        /// </summary>
        private const int TabLength = 20;

        [SerializeField] private ConsolePanelView _console;

        protected override void OnStart()
        {
            IEnumerable<Type> commands = Assembly.GetAssembly(typeof(Command)).GetTypes().Where(iType =>
                iType.IsClass && iType.IsSubclassOf(typeof(Command)) && !iType.IsAbstract);
            foreach (Type command in commands)
            {
                string name = command.Name.ToLower();
                // Remove Command suffix
                name = name.Substring(0, name.Length - 7);
                Command instance = (Command)Activator.CreateInstance(command);
                _allCommands.Add(name, instance);
            }
        }

        /// <summary>
        /// Treat a command, either going through the server if the client is connected, or
        /// executing it on the disconnected client if the command type allows it.
        /// </summary>
        [Client]
        public void ClientProcessCommand(string command)
        {
            if(_allCommands[CommandName(command)].Type == CommandType.Offline)
            {
                OfflineProcessCommand(command); 
                return;
            }
            CmdProcessOnlineCommand(command);
        }

        [ServerOrClient]
        private string CommandName(string command)
        {
            string[] splitCommand = command.Split(' ');
            return splitCommand[0];
        }

        [ServerOrClient]
        private void OfflineProcessCommand(string command)
        {
            string[] splitCommand = command.Split(' ');
            string commandName = CommandName(command);

            if (TreatUnknownCommand(null, commandName, out Command commandObject))
            {
                return;
            }

            if (TreatHelpCommand(null, command, commandName, splitCommand))
            {
                return;
            }

            commandObject.Perform(command.Split().Skip(1).ToArray());
        }

        /// <summary>
        /// Find and call command
        /// </summary>
        /// <param name="command">Command and it's args separated by spaces</param>
        /// <returns>Command response</returns>
        [ServerRpc(RequireOwnership = false)]
        private void CmdProcessOnlineCommand(string command, NetworkConnection conn = null)
        {
            string ckey = Subsystems.Get<PlayerSystem>().GetCkey(conn);

            if (!Subsystems.Get<PermissionSystem>().TryGetUserRole(ckey, out ServerRoleTypes userPermission))
            {
                CommandAnswer(conn, string.Format("No role found for user {0}, can't process command", ckey));
                return;
            }

            string[] splitCommand = command.Split(' ');
            string commandName = CommandName(command);

            if (TreatUnknownCommand(conn, commandName, out Command commandObject))
            {
                return;
            }

            if (TreatHelpCommand(conn, command, commandName, splitCommand))
            {
                return;
            }

            if (commandObject.AccessLevel > userPermission)
            {
                CommandAnswer(conn, "Access level too low, can't perform command");
                return;
            }

            // Either execute command on server or call on client.
            // Offline commands go through server if client is connected to server. 
            switch (commandObject.Type)
            {
                case CommandType.Server:
                    CommandAnswer(conn, commandObject.Perform(command.Split().Skip(1).ToArray(), conn));
                    break;

                case CommandType.Client:
                    RpcPerformOnClient(conn, command);
                    break;

                case CommandType.Offline:
                    RpcPerformOnClient(conn, command);
                    break;
            }
        }

        [ServerOrClient]
        private bool TreatHelpCommand(NetworkConnection conn, string command, string commandName, string[] splitCommand)
        {
            if (commandName == "help")
            {
                CommandAnswer(conn, HelpCommand());
                return true;
            }
            if (splitCommand.Length > 1)
            {
                if (splitCommand[1] == "help")
                {
                    CommandAnswer(conn, LongHelpCommand(command));
                    return true;
                }
            }
            return false;
        }

        [ServerOrClient]
        private bool TreatUnknownCommand(NetworkConnection conn, string commandName, out Command command)
        {
            if (!_allCommands.ContainsKey(commandName))
            {
                CommandAnswer(conn, "No such command exists");
                command = null;
                return true;
            }
            command = _allCommands[commandName];
            return false;
        }


        [TargetRpc]
        private void RpcPerformOnClient(NetworkConnection conn, string command)
        {
            string[] splitCommand = command.Split(' ');
            string commandName = splitCommand[0];

            if (TreatUnknownCommand(conn, commandName, out Command commandObject))
            {
                return;
            }

            if (TreatHelpCommand(conn, command, commandName, splitCommand))
            {
                return;
            }

            string answer = commandObject.Perform(command.Split().Skip(1).ToArray());
            _console.AddText(answer);
        }

        [TargetRpc]
        private void RpcCommandAnswer(NetworkConnection conn, string answer)
        {
            _console.AddText(answer);
        }

        [ServerOrClient]
        private void CommandAnswer(NetworkConnection conn, string answer)
        {
            if (IsServer)
            {
                RpcCommandAnswer(conn, answer);
            }
            else
            {
                _console.AddText(answer);
            }	
        }

        /// <returns>All available commands with short descriptions</returns>
        private string HelpCommand()
        {
            string ret = "";
            foreach (KeyValuePair<string, Command> i in _allCommands)
            {
                string tabulation = new(' ', TabLength - i.Key.Length);
                ret += i.Key + tabulation + i.Value.ShortDescription + "\n";
            }

            return ret;
        }
        /// <returns>Long help for given command</returns>
        private string LongHelpCommand(string command)
        {
            return _allCommands[command.Split(' ')[0]].LongDescription;
        }
    }
}