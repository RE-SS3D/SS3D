using System;
using System.Collections.Generic;

namespace SS3D.Core.Debugging
{
    /// <summary>
    /// A named developer tool the F1 debug window can run. Owners subscribe to <see cref="Invoked"/> (per-instance
    /// owners such as every container all subscribe to the same tool) and set <see cref="IsOn"/> when the tool is a
    /// toggle. Replaces the scattered per-tool debug keys.
    /// </summary>
    public sealed class DebugTool
    {
        private Action _invoked;

        internal DebugTool(string name) => Name = name;

        public string Name { get; }

        /// <summary>Current state for a toggle tool (shown as On/Off); null for a one-shot action.</summary>
        public Func<bool> IsOn { get; set; }

        public bool IsToggle => IsOn != null;

        /// <summary>False while nothing in the current scene listens (e.g. no body to knock down).</summary>
        public bool Available => _invoked != null;

        public event Action Invoked
        {
            add => _invoked += value;
            remove => _invoked -= value;
        }

        public void Invoke() => _invoked?.Invoke();
    }

    /// <summary>Process-wide registry of <see cref="DebugTool"/>s, in first-registration order.</summary>
    public static class DebugTools
    {
        private static readonly List<DebugTool> Tools = new();

        public static IReadOnlyList<DebugTool> All => Tools;

        /// <summary>Gets or creates the tool with this name.</summary>
        public static DebugTool Get(string name)
        {
            foreach (DebugTool tool in Tools)
                if (tool.Name == name) return tool;
            DebugTool created = new(name);
            Tools.Add(created);
            return created;
        }

        /// <summary>Forgets every tool; for tests and play-mode resets.</summary>
        public static void Clear() => Tools.Clear();

        // Enter Play Mode skips the domain reload, so drop the previous session's subscribers.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode() => Clear();
    }
}
