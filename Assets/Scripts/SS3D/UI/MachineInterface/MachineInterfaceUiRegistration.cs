using SS3D.UI.MachineInterface.Components;
using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface
{
    public sealed class MachineInterfaceUiRegistration
    {
        public string InterfaceId { get; init; }

        public VisualTreeAsset Template { get; init; }

        public StyleSheet TemplateStyle { get; init; }

        public bool Wide { get; init; }

        public Func<MachineWindow, IMachineInterfaceBinder> CreateBinder { get; init; }
    }
}
