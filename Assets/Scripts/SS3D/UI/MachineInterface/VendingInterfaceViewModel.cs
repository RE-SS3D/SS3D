using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public class VendingInterfaceViewModel : IMachineInterfaceViewModel
    {
        public string Title { get; set; } = "GALLEY VENDOMAT";

        public string Subtitle { get; set; } = "Crew Mess — Sundries Dispenser";

        public string ModelLabel { get; set; } = "VND-7 · ration & sundries dispenser";

        public string ConnectionStatus { get; set; } = "WIRED · VEND BUS · PORT J1";

        public string StockedReadout { get; set; } = "0 of 0 items stocked";

        public bool PowerOk { get; set; } = true;

        public bool IdScanned { get; set; }

        public bool IdScanning { get; set; }

        public int VendingProductIndex { get; set; } = -1;

        public List<VendingProductViewData> Products { get; set; } = new();

        public List<VendingTrayItemViewData> TrayItems { get; set; } = new();

        public List<string> ActionLog { get; set; } = new();

        public static VendingInterfaceViewModel CreateSample()
        {
            return new VendingInterfaceViewModel
            {
                Title = "GALLEY VENDOMAT 7",
                Subtitle = "Crew Mess — Sundries Dispenser",
                ModelLabel = "VND-7 · ration & sundries dispenser",
                ConnectionStatus = "WIRED · VEND BUS · PORT J1",
                StockedReadout = "7 of 9 items stocked",
                PowerOk = true,
                Products = new List<VendingProductViewData>
                {
                    new() { Index = 0, Name = "Ration Bar", Stock = 14, CanSelect = true },
                    new() { Index = 1, Name = "Water Pouch", Stock = 9, CanSelect = true },
                    new() { Index = 2, Name = "Space Cola", Stock = 6, CanSelect = true },
                    new() { Index = 3, Name = "Cigarettes", Stock = 11, CanSelect = true },
                    new() { Index = 4, Name = "Multitool Battery", Stock = 3, CanSelect = true },
                    new() { Index = 5, Name = "Analgesic Tablets", Stock = 0, CanSelect = false },
                    new() { Index = 6, Name = "Antitox Vial", Stock = 5, RequiresId = true, Locked = true, CanSelect = false },
                    new() { Index = 7, Name = "Sugar Candy", Stock = 20, CanSelect = true },
                    new() { Index = 8, Name = "Coffee Grounds", Stock = 7, CanSelect = true },
                },
                ActionLog = new List<string>
                {
                    "[07:45] Connection established — Port J1",
                },
            };
        }
    }
}
