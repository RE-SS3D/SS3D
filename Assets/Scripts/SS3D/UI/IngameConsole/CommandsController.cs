using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SS3D.UI.IngameConsole
{
    public class CommandsController
    {
        private struct CommandInfo
        {
            public MethodInfo CommandMethod;
            public string ShortDescription;
            public string LongDescription;
        }

        private Dictionary<string, CommandInfo> _allMethods = new();
        public CommandsController()
        {
            MethodInfo[] methodInfos = typeof(CommandsContainer).GetMethods(BindingFlags.Public|BindingFlags.Static);
            foreach (MethodInfo i in methodInfos)
            {
                CommandInfo method = new CommandInfo();
                method.CommandMethod = i;
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

        public string ProcessCommand(string command)
        {
            string[] splittedCommand = command.Split(' ');
            if (splittedCommand[0] == "help")
            {
                return HelpCommand();
            }

            if (splittedCommand.Length > 1)
            {
                if (splittedCommand[1] == "help")
                {
                    return LongHelpCommand(command);
                }
            }

            if (_allMethods.ContainsKey(splittedCommand[0]))
            {
                object[] args;
                if (splittedCommand.Length > 1)
                {
                    ParameterInfo[] commandParameterInfos = _allMethods[splittedCommand[0]].CommandMethod.GetParameters();
                    if (splittedCommand.Length - 1 != commandParameterInfos.Length)
                    {
                        return "Wrong args. \"Type (command) help\"";
                    }
                    args = splittedCommand.ToList().GetRange(1, splittedCommand.Length - 1).ToArray();
                    args = Array.ConvertAll(args, item => (object)item);
                    try
                    {
                        for (int i = 0; i < commandParameterInfos.Length; i++)
                        {
                            args[i] = Convert.ChangeType(args[i], commandParameterInfos[i].ParameterType);
                        }
                    }
                    catch (FormatException)
                    {
                        return "Wrong args. Type \"(command) help\"";
                    }
                }
                else
                {
                    args = new object[] {};
                }
                return (string)_allMethods[splittedCommand[0]].CommandMethod.Invoke(null, args);
            }
                
            return "nothing";
        }

        public string HelpCommand()
        {
            string ret = "";
            foreach (KeyValuePair<string, CommandInfo> i in _allMethods)
            {
                ret += i.Key + "\t" + i.Value.ShortDescription + "\n";
            }

            return ret;
        }

        public string LongHelpCommand(string command)
        {
            return _allMethods[command.Split(' ')[0]].LongDescription;
        }
    }
}