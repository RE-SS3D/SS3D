using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Audio;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Selection;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;
using Random = UnityEngine.Random;

namespace SS3D.UI.MachineInterface
{
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(MachinePowerConsumer))]
    public sealed class VendingMachineController : MachineInterfaceBehaviour
    {
        private readonly struct TrayEntry
        {
            public TrayEntry(int productIndex, string name)
            {
                ProductIndex = productIndex;
                Name = name;
            }

            public int ProductIndex { get; }

            public string Name { get; }
        }

        private const int MaxTrayItems = VendingInterfaceSnapshot.MaxTrayItems;

        private readonly List<TrayEntry> _trayItems = new();

        private readonly List<string> _actionLog = new();

        [SerializeField]
        private MachinePowerConsumer _powerConsumer;

        [SerializeField]
        private VendingMachineProductStock[] _productsToDispense;

        [SerializeField]
        private Transform _dispensingTransform;

        [SerializeField]
        private string _title = "GALLEY VENDOMAT";

        [SerializeField]
        private string _subtitle = "Crew Mess — Sundries Dispenser";

        [SerializeField]
        private string _modelLabel = "VND-7 · ration & sundries dispenser";

        [SerializeField]
        private string _connectionStatus = "WIRED · VEND BUS · PORT J1";

        private int _logSequence;

        public override string InterfaceId => MachineInterfaceIds.Vending;

        protected override void SendOpenToViewer(NetworkConnection conn)
        {
            TargetOpenInterface(conn, BuildSnapshot());
        }

        protected override void SendRefreshToViewer(NetworkConnection conn)
        {
            TargetRefreshInterface(conn, BuildSnapshot());
        }

        protected override bool ApplyControl(byte controlId, bool value)
        {
            return false;
        }

        protected override bool ApplyActionControl(byte controlId, int value)
        {
            switch (controlId)
            {
                case MachineInterfaceControlIds.Vending.SelectProduct:
                {
                    return TryVendToTray(value);
                }

                case MachineInterfaceControlIds.Vending.TakeTrayItem:
                {
                    return TryTakeFromTray(value);
                }

                case MachineInterfaceControlIds.Vending.ReadId:
                {
                    PushLog("ID reader unavailable — access system not installed");
                    RefreshAllViewers();
                    return true;
                }

                default:
                {
                    return false;
                }
            }
        }

        private static void SetProduct(ref VendingInterfaceSnapshot snapshot, int index, VendingProductSnapshot product)
        {
            switch (index)
            {
                case 0:
                {
                    snapshot.Product0 = product;
                    break;
                }

                case 1:
                {
                    snapshot.Product1 = product;
                    break;
                }

                case 2:
                {
                    snapshot.Product2 = product;
                    break;
                }

                case 3:
                {
                    snapshot.Product3 = product;
                    break;
                }

                case 4:
                {
                    snapshot.Product4 = product;
                    break;
                }

                case 5:
                {
                    snapshot.Product5 = product;
                    break;
                }

                case 6:
                {
                    snapshot.Product6 = product;
                    break;
                }

                case 7:
                {
                    snapshot.Product7 = product;
                    break;
                }

                case 8:
                {
                    snapshot.Product8 = product;
                    break;
                }

                case 9:
                {
                    snapshot.Product9 = product;
                    break;
                }

                case 10:
                {
                    snapshot.Product10 = product;
                    break;
                }

                case 11:
                {
                    snapshot.Product11 = product;
                    break;
                }
            }
        }

        private static void SetTrayItem(ref VendingInterfaceSnapshot snapshot, int index, VendingTrayItemSnapshot trayItem)
        {
            switch (index)
            {
                case 0:
                {
                    snapshot.Tray0 = trayItem;
                    break;
                }

                case 1:
                {
                    snapshot.Tray1 = trayItem;
                    break;
                }

                case 2:
                {
                    snapshot.Tray2 = trayItem;
                    break;
                }

                case 3:
                {
                    snapshot.Tray3 = trayItem;
                    break;
                }
            }
        }

        private static void SetLogEntry(ref VendingInterfaceSnapshot snapshot, int index, string value)
        {
            switch (index)
            {
                case 0:
                {
                    snapshot.Log0 = value;
                    break;
                }

                case 1:
                {
                    snapshot.Log1 = value;
                    break;
                }

                case 2:
                {
                    snapshot.Log2 = value;
                    break;
                }

                case 3:
                {
                    snapshot.Log3 = value;
                    break;
                }
            }
        }

        [TargetRpc(RunLocally = true)]
        private void TargetOpenInterface(NetworkConnection conn, VendingInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, VendingInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(snapshot);
        }

        private bool TryVendToTray(int productIndex)
        {
            if (!IsPowered() || productIndex < 0 || productIndex >= _productsToDispense.Length)
            {
                return false;
            }

            if (_trayItems.Count >= MaxTrayItems)
            {
                PushLog("Dispense tray full — take items before vending more");
                RefreshAllViewers();
                return false;
            }

            VendingMachineProductStock productStock = _productsToDispense[productIndex];
            if (productStock.Product == null || productStock.Stock <= 0)
            {
                PlayOutOfStockSound();
                return false;
            }

            _powerConsumer.UseMachineOnce();
            productStock.Stock--;
            string productName = productStock.Product.NameString;
            _trayItems.Add(new TrayEntry(productIndex, productName));
            PlayVendSound();
            PushLog($"{productName} dispensed to tray");
            RefreshAllViewers();
            return true;
        }

        private bool TryTakeFromTray(int trayIndex)
        {
            if (!IsPowered() || trayIndex < 0 || trayIndex >= _trayItems.Count)
            {
                return false;
            }

            TrayEntry trayEntry = _trayItems[trayIndex];
            if (trayEntry.ProductIndex < 0 || trayEntry.ProductIndex >= _productsToDispense.Length)
            {
                return false;
            }

            VendingMachineProductStock productStock = _productsToDispense[trayEntry.ProductIndex];
            if (productStock.Product == null)
            {
                return false;
            }

            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            Quaternion quaternion = Quaternion.Euler(
                Random.Range(0, 360),
                Random.Range(0, 360),
                Random.Range(0, 360));
            itemSystem.SpawnItem(
                productStock.Product.PrefabAsset.Id,
                _dispensingTransform.position,
                quaternion);

            _trayItems.RemoveAt(trayIndex);
            PushLog($"{trayEntry.Name} taken from tray");
            RefreshAllViewers();
            return true;
        }

        private VendingInterfaceSnapshot BuildSnapshot()
        {
            int inStockCount = 0;
            for (int i = 0; i < _productsToDispense.Length; i++)
            {
                if (_productsToDispense[i].Stock > 0)
                {
                    inStockCount++;
                }
            }

            VendingInterfaceSnapshot snapshot = new()
            {
                MachineObjectId = NetworkObject.ObjectId,
                InterfaceId = InterfaceId,
                Title = _title,
                Subtitle = _subtitle,
                ModelLabel = _modelLabel,
                ConnectionStatus = _connectionStatus,
                StockedReadout = $"{inStockCount} of {_productsToDispense.Length} items stocked",
                PowerOk = IsPowered(),
                IdScanned = false,
                VendingProductIndex = VendingInterfaceSnapshot.NoVendingProduct,
                ProductCount = (byte)Mathf.Min(_productsToDispense.Length, VendingInterfaceSnapshot.MaxProducts),
                TrayItemCount = (byte)Mathf.Min(_trayItems.Count, MaxTrayItems),
                LogEntryCount = (byte)Mathf.Min(_actionLog.Count, VendingInterfaceSnapshot.MaxLogEntries),
            };

            for (int i = 0; i < snapshot.ProductCount; i++)
            {
                VendingMachineProductStock stock = _productsToDispense[i];
                SetProduct(ref snapshot, i, new VendingProductSnapshot
                {
                    Name = stock.Product != null ? stock.Product.NameString : "Unknown",
                    Stock = stock.Stock,
                    RequiresId = false,
                });
            }

            for (int i = 0; i < snapshot.TrayItemCount; i++)
            {
                TrayEntry trayEntry = _trayItems[i];
                SetTrayItem(ref snapshot, i, new VendingTrayItemSnapshot
                {
                    Name = trayEntry.Name,
                });
            }

            for (int i = 0; i < snapshot.LogEntryCount; i++)
            {
                SetLogEntry(ref snapshot, i, _actionLog[i]);
            }

            return snapshot;
        }

        private bool IsPowered()
        {
            return _powerConsumer != null && _powerConsumer.PowerStatus != PowerStatus.Inactive;
        }

        private void PlayOutOfStockSound()
        {
            SubSystems.Get<AudioSubSystem>().PlayAudioSource(
                AudioType.Sfx,
                Sounds.BikeHorn,
                Position,
                NetworkObject,
                false,
                0.7f,
                1,
                1,
                3);
        }

        private void PlayVendSound()
        {
            SubSystems.Get<AudioSubSystem>().PlayAudioSource(
                AudioType.Sfx,
                Sounds.Can1,
                Position,
                NetworkObject,
                false,
                0.7f,
                1,
                1,
                3);
        }

        private void PushLog(string message)
        {
            _logSequence++;
            int totalSeconds = (_logSequence * 19) + 30;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            string entry = $"[{minutes:00}:{seconds:00}] {message}";
            _actionLog.Insert(0, entry);

            if (_actionLog.Count > VendingInterfaceSnapshot.MaxLogEntries)
            {
                _actionLog.RemoveAt(_actionLog.Count - 1);
            }
        }
    }
}
