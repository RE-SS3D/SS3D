using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public sealed class IdConsoleInterfaceViewModel : IMachineInterfaceViewModel
    {
        public string Title { get; set; } = "ID CONSOLE";

        public string Subtitle { get; set; } = "Personnel Access Management";

        public string ModelLabel { get; set; } = "IDC-1 · crew credential editor";

        public string PromptText { get; set; } = "Insert your ID to unlock editing.";

        public bool EditorUnlocked { get; set; }

        public bool HasTargetCard { get; set; }

        public string TargetName { get; set; } = string.Empty;

        public string TargetJob { get; set; } = string.Empty;

        public List<IdConsoleAccessLevelViewData> Levels { get; } = new();

        public List<string> EditLog { get; } = new();
    }
}
