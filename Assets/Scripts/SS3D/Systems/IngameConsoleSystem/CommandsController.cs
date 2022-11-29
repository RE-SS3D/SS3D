using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem
{
    public class CommandsController
    {
        // TODO: implement permissions control through attribute
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
                MethodInfo commandMethod = _allMethods[splittedCommand[0]].CommandMethod;
                object[] args;
                if (splittedCommand.Length > 1)
                {
                    List<object> temp = splittedCommand.ToList().GetRange(1, splittedCommand.Length - 1)
                        .Select(x => (object)x).ToList();
                    int paramsLength = commandMethod.GetParameters().Length;
                    if (!commandMethod.GetParameters().Any(x => x.ParameterType.IsArray))
                    {
                        args = temp.ToArray();
                    }
                    else
                    {
                        List<object> temp2 = temp.GetRange(paramsLength - 1, temp.Count - paramsLength + 1).ToList();
                        temp.RemoveRange(paramsLength - 1, temp.Count - paramsLength + 1);
                        temp.Add(temp2.ToArray());
                        args = temp.ToArray();
                    }
                }
                else
                {
                    args = new object[] {};
                }

                try
                {
                    return (string)commandMethod.Invoke(null, args);
                }
                catch (Exception)
                {
                    return "Wrong args. Type \"(command) help\"";
                }
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