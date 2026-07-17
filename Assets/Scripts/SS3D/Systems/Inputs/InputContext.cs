namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Named input contexts. At any moment exactly one context is "active": the highest-priority
    /// context currently pushed (ties broken by most-recent push). The active context declares which
    /// action maps and actions are enabled; suppressions then remove specific maps/bindings on top.
    /// </summary>
    /// <remarks>
    /// The integer value is the priority — higher wins. <see cref="Global"/> is pushed once at
    /// startup and never released, so a context is always resolvable.
    /// </remarks>
    public enum InputContext
    {
        /// <summary>Always present. System/menu actions available regardless of gameplay state.</summary>
        Global = 0,

        /// <summary>Normal in-world play: movement, camera, world interactions, hotkeys.</summary>
        Gameplay = 10,

        /// <summary>The construction build menu is open (placement input, no world interactions).</summary>
        TileMenu = 20,

        /// <summary>A machine interface panel is open (movement/camera captured by the panel).</summary>
        MachineUI = 30,

        /// <summary>The in-game debug console is open.</summary>
        Console = 40,

        /// <summary>A generic text input field is focused; all gameplay/system keybinds are off.</summary>
        TextEntry = 50,

        /// <summary>The chat input field is focused; like <see cref="TextEntry"/> but keeps the
        /// send-message action live so Enter submits.</summary>
        ChatEntry = 60,
    }
}
