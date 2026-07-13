using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface
{
    public sealed class MachineInterfaceUiRegistration
    {
        public string InterfaceId { get; init; }

        public VisualTreeAsset Template { get; init; }

        public StyleSheet TemplateStyle { get; init; }

        public StyleSheet[] ComponentStyles { get; init; } = Array.Empty<StyleSheet>();

        public MachineInterfaceShellKind ShellKind { get; init; } = MachineInterfaceShellKind.ModalWindow;

        public bool Wide { get; init; }

        public Func<VisualElement, IMachineInterfaceBinder> CreateBinder { get; init; }
    }
}
