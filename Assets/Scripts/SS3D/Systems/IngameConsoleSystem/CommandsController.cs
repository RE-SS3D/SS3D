using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Permissions;
using SS3D.Systems.IngameConsoleSystem.Commands;
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
                Command instance = (Command)Activator.CreateInstance(command);
                _allCommands.Add(GetCommandName(command), instance);
            }
        }

        /// <summary>
        /// Treat a command, either going through the server if the client is connected, or
        /// executing it on the disconnected client if the command type allows it.
        /// </summary>
        [Client]
        public void ClientProcessCommand(string command)
        {
	        if (GetCommandName(command) == "help")
	        {
		        CommandAnswer(HelpCommand());
		        return;
	        }
	        
	        if (!FindCommand(GetCommandName(command), out Command commandObject))
	        {
		        CommandAnswer("No such command exists");
		        return;
	        }
	        
	        string[] splitCommand = command.Split(' ');
	        if (splitCommand.Length > 1)
	        {
		        if (splitCommand[1] == "help")
		        {
			        CommandAnswer(LongHelpCommand(commandObject));
                    return;
                }
	        }

	        string[] args = GetCommandArgs(command);
	        if(_allCommands[GetCommandName(command)].Type == CommandType.Offline)
            {
                OfflineProcessCommand(commandObject, args); 
                return;
            }
	        CmdProcessOnlineCommand(GetCommandName(command), args);
        }

        /// <summary>
        /// Get command name out of string with args
        /// </summary>
        private string GetCommandName(string command)
        {
            string[] splitCommand = command.Split(' ');
            return splitCommand[0].ToLower();
        }

        /// <summary>
        /// Get command name out of command type
        /// </summary>
        private string GetCommandName(Type command)
        {
            string name = command.Name.ToLower();
            // Remove "Command" suffix
            return name.Substring(0, name.Length - 7);
        }
        
        private string[] GetCommandArgs(string command)
        {
	        return command.Split().Skip(1).ToArray();
        }
        /// <summary>
        /// Perform command without sending a request to server.
        /// </summary>
        private void OfflineProcessCommand(Command command, string[] args)
        {
	        CommandAnswer(command.Perform(args));
        }

        /// <summary>
        /// Check user permission and either perform command on server or on client
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void CmdProcessOnlineCommand(string commandName, string[] args, NetworkConnection conn = null)
        {
            string ckey = SubSystems.Get<PlayerSubSystem>().GetCkey(conn);
            FindCommand(commandName, out Command command);

            if (!SubSystems.Get<PermissionSubSystem>().TryGetUserRole(ckey, out ServerRoleTypes userPermission))
            {
                CommandAnswer(string.Format("No role found for user {0}, can't process command", ckey), conn);
                return;
            }
            
            if (userPermission < command.AccessLevel)
            {
                CommandAnswer("Your permission level is too low, can't perform the command", conn);
                return;
            }

            // Either execute command on server or call on client.
            // Offline commands go through server if client is connected to server. 
            switch (command.Type)
            {
                case CommandType.Server:
                    CommandAnswer(command.Perform(args, conn), conn);
                    break;

                case CommandType.Client:
                    RpcPerformOnClient(conn, commandName, args);
                    break;
            }
        }
        /// <summary>
        /// Find command by name
        /// </summary>
        /// <returns>Is given command found</returns>
        private bool FindCommand(string commandName, out Command command)
        {
            if (!_allCommands.ContainsKey(commandName))
            {
	            command = null;
                return false;
            }
            command = _allCommands[commandName];
            return true;
        }
		/// <summary>
		/// Perform command on client
		/// </summary>
		[TargetRpc]
        private void RpcPerformOnClient(NetworkConnection conn, string commandName, string[] args)
        {
	        FindCommand(commandName, out Command command);
	        string answer = command.Perform(args);
            _console.AddText(answer);
        }
		/// <summary>
		/// Send or print command's answer
		/// </summary>
        private void CommandAnswer(string answer, NetworkConnection conn = null)
        {
	        if ((IsServer) && (conn != null))
	        {
		        RpcCommandAnswer(conn, answer);
	        }
	        else
	        {
		        _console.AddText(answer);
	        }
        }

        [TargetRpc]
        private void RpcCommandAnswer(NetworkConnection conn, string answer)
        {
            _console.AddText(answer);
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
        
        /// <returns>Long help and usage for given command</returns>
        private string LongHelpCommand(Command command)
        {
            return command.LongDescription + "\nUsage: " + GetCommandName(command.GetType()) + ' ' + command.Usage;
        }
    }
}