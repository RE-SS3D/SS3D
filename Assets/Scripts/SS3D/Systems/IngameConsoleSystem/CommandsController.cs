using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SS3D.Systems.IngameConsoleSystem
{
    public class CommandsController
    {
        private struct CommandInfo
        {
            public MethodInfo CommandMethod;
            public string ShortDescription;
            public string LongDescription;
        }
        /// <summary>
        /// Contains all command infos and their names in lowercase
        /// </summary>
        private readonly Dictionary<string, CommandInfo> _allMethods = new();
        public CommandsController()
        {
            MethodInfo[] methodInfos = typeof(CommandsContainer).GetMethods(BindingFlags.Public|BindingFlags.Static);
            foreach (MethodInfo i in methodInfos)
            {
                CommandInfo method = new CommandInfo
                {
                    CommandMethod = i
                };
                foreach (Attribute j in i.GetCustomAttributes())
                {
                    if (j is ShortDescriptionAttribute shortDescription)
                        method.ShortDescription = shortDescription.Descrition;
                    else if (j is LongDescriptionAttribute longDescription)
                        method.LongDescription = longDescription.Description;
                }
                _allMethods.Add(i.Name.ToLower(), method);
            }
        }
        /// <summary>
        /// Find and call command
        /// </summary>
        /// <param name="command">Command and its args separated by spaces</param>
        /// <returns>Command response</returns>
        public string ProcessCommand(string command)
        {
            string[] splitCommand = command.Split(' ');
            if (splitCommand[0] == "help")
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
            // Find proper command body ant prepare correct args
            if (_allMethods.ContainsKey(commandName))
            {
                MethodInfo commandMethod = _allMethods[commandName].CommandMethod;
                int paramsLength = commandMethod.GetParameters().Length;
                List<object> args = new List<object>();

                if (splitCommand.Length > 1)
                {
                    // Check if the command takes an array
                    if (commandMethod.GetParameters().Any(x => x.ParameterType.IsArray))
                    {
                        args.AddRange(splitCommand.Skip(1).Take(paramsLength - 1));
                        args.Add(splitCommand.Skip(paramsLength).ToArray());
                    }
                    else
                    {
                        args.AddRange(splitCommand.Skip(1));
                    }
                }

                try
                {
                    Console.WriteLine((string)commandMethod.Invoke(null, args.ToArray()));
                }
                catch (TargetParameterCountException)
                {
                    return "Wrong args. Type \"(command) help\"";
                }
            }
            return "nothing";
        }
        /// <returns>All available commands with short descriptions as string</returns>
        private string HelpCommand()
        {
            string ret = "";
            foreach (KeyValuePair<string, CommandInfo> i in _allMethods)
            {
                ret += i.Key + "\t" + i.Value.ShortDescription + "\n";
            }

            return ret;
        }
        /// <returns>Exact command with long descriptiption as string</returns>
        private string LongHelpCommand(string command)
        {
            return _allMethods[command.Split(' ')[0]].LongDescription;
        }
    }
}