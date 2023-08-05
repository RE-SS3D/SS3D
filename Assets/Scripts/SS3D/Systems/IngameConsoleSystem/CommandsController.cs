﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.IngameConsoleSystem.Commands;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using UnityEngine;
using UnityEngine.InputSystem;

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

		[SerializeField] private ConsolePanelView console;

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

		[Client]
		public void ClientProcessCommand(string command)
		{
			CmdProcessCommand(command);
		}

		/// <summary>
		/// Find and call command
		/// </summary>
		/// <param name="command">Command and it's args separated by spaces</param>
		/// <returns>Command response</returns>
		[ServerRpc(RequireOwnership = false)]
        public void CmdProcessCommand(string command, NetworkConnection conn = null)
        {
			string ckey = Subsystems.Get<PlayerSystem>().GetCkey(conn);
			if (!Subsystems.Get<PermissionSystem>().TryGetUserRole(ckey, out ServerRoleTypes userPermission))
			{
				RpcCommandAnswer(conn, string.Format("No role found for user {0}, can't process command", ckey));
				return;
			}

			string[] splitCommand = command.Split(' ');
            string commandName = splitCommand[0]; 
            if (commandName == "help")
            {
				RpcCommandAnswer(conn, HelpCommand());
				return;
            }
            if (splitCommand.Length > 1)
            {
                if (splitCommand[1] == "help")
                {
					RpcCommandAnswer(conn, LongHelpCommand(command));
					return;
                }
            }
            
            if (!_allCommands.ContainsKey(commandName))
            {
				RpcCommandAnswer(conn, "No such command exists");
				return;
            }

			if (_allCommands[commandName].AccessLevel > userPermission)
			{
				RpcCommandAnswer(conn, "Access level too low, can't perform command");
				return;
			}

			if (_allCommands[commandName].ServerCommand)
			{
				RpcCommandAnswer(conn, _allCommands[commandName].Perform(command.Split().Skip(1).ToArray(), conn));
				return;
			}
			else
			{
				RpcPerformOnClient(conn, command);
				return;
			}
		}

		[TargetRpc]
		public void RpcPerformOnClient(NetworkConnection conn, string command)
		{
			string[] splitCommand = command.Split(' ');
			string commandName = splitCommand[0];
			string answer = _allCommands[commandName].Perform(command.Split().Skip(1).ToArray());
			console.AddText(answer);
		}

		[TargetRpc]
		public void RpcCommandAnswer(NetworkConnection conn, string answer) 
		{ 
			console.AddText(answer);
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