using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using SS3D.Systems.IngameConsoleSystem.Commands;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem
{
    public class CommandsController
    {
        /// <summary>
        /// Contains all command infos and their names in lowercase
        /// </summary>
        private readonly Dictionary<string, Command> _allCommands = new();
        public CommandsController()
        {
            IEnumerable<Type> commands = Assembly.GetAssembly(typeof(Command)).GetTypes().Where(iType =>
                iType.IsClass && iType.IsSubclassOf(typeof(Command)) && !iType.IsAbstract);
            foreach (Type command in commands)
            {
                string name = command.Name.ToLower();
                Command instance = (Command)Activator.CreateInstance(command);
                _allCommands.Add(name, instance);
            }
        }
        /// <summary>
        /// Find and call command
        /// </summary>
        /// <param name="command">Command and it's args separated by spaces</param>
        /// <returns>Command response</returns>
        public string ProcessCommand(string command)
        {
            string[] splitCommand = command.Split(' ');
            string commandName = splitCommand[0]; 
            if (commandName == "help")
            {
                return HelpCommand();
            }
            if (splitCommand.Length > 1)
            {
                if (splitCommand[1] == "help")
                {
                    return LongHelpCommand(command);
                }
            }
            // Find proper command body and prepare correct args
            if (_allCommands.ContainsKey(commandName))
            {
                return _allCommands[commandName].Perform(command.Split().Skip(1).ToArray());
            } 
            return "No such command exists";
        }
        /// <returns>All available commands with short descriptions</returns>
        private string HelpCommand()
        {
            string ret = "";
            foreach (KeyValuePair<string, Command> i in _allCommands)
            {
                ret += i.Key + "\t" + i.Value.ShortDescription + "\n";
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