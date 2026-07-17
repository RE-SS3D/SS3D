using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SS3D.Systems.Testing
{
    /// <summary>
    /// One instruction from a multiplayer test harness script: an opcode plus positional args.
    /// See <see cref="AutomationSubSystem"/> for the supported opcodes.
    /// </summary>
    public readonly struct AutomationInstruction
    {
        public readonly string Opcode;
        public readonly string[] Args;

        public AutomationInstruction(string opcode, string[] args)
        {
            Opcode = opcode;
            Args = args;
        }

        public string ArgsJoined => string.Join(" ", Args);
    }

    /// <summary>
    /// Parses a multiplayer test harness script (see Testing/multiplayer/scenarios/*.txt) into a
    /// flat list of instructions. One instruction per line, whitespace-separated, first token is
    /// the opcode. Blank lines and lines starting with '#' are ignored.
    /// </summary>
    public static class AutomationScript
    {
        public static IReadOnlyList<AutomationInstruction> Load(string path)
        {
            List<AutomationInstruction> instructions = new();

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                string[] tokens = line.Split(' ');
                string opcode = tokens[0].ToLowerInvariant();
                string[] args = tokens.Skip(1).ToArray();

                instructions.Add(new AutomationInstruction(opcode, args));
            }

            return instructions;
        }
    }
}
