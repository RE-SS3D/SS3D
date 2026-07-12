using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public static class VendingInterfaceSnapshotMapper
    {
        public static VendingInterfaceViewModel ToViewModel(VendingInterfaceSnapshot snapshot)
        {
            VendingInterfaceViewModel model = new()
            {
                Title = snapshot.Title,
                Subtitle = snapshot.Subtitle,
                ModelLabel = snapshot.ModelLabel,
                ConnectionStatus = snapshot.ConnectionStatus,
                StockedReadout = snapshot.StockedReadout,
                PowerOk = snapshot.PowerOk,
                IdScanned = snapshot.IdScanned,
                VendingProductIndex = snapshot.VendingProductIndex == VendingInterfaceSnapshot.NoVendingProduct
                    ? -1
                    : snapshot.VendingProductIndex,
            };

            int productCount = snapshot.ProductCount;
            if (productCount > VendingInterfaceSnapshot.MaxProducts)
            {
                productCount = VendingInterfaceSnapshot.MaxProducts;
            }

            bool anyVending = model.VendingProductIndex >= 0;
            for (int i = 0; i < productCount; i++)
            {
                VendingProductSnapshot product = GetProduct(snapshot, i);
                bool locked = product.RequiresId && !snapshot.IdScanned;
                bool canSelect = snapshot.PowerOk
                    && product.Stock > 0
                    && !locked
                    && (!anyVending || model.VendingProductIndex == i);

                model.Products.Add(new VendingProductViewData
                {
                    Index = i,
                    Name = product.Name,
                    Stock = product.Stock,
                    RequiresId = product.RequiresId,
                    Locked = locked,
                    CanSelect = canSelect,
                });
            }

            int trayCount = snapshot.TrayItemCount;
            if (trayCount > VendingInterfaceSnapshot.MaxTrayItems)
            {
                trayCount = VendingInterfaceSnapshot.MaxTrayItems;
            }

            for (int i = 0; i < trayCount; i++)
            {
                VendingTrayItemSnapshot trayItem = GetTrayItem(snapshot, i);
                model.TrayItems.Add(new VendingTrayItemViewData
                {
                    TrayIndex = i,
                    Name = trayItem.Name,
                });
            }

            int logCount = snapshot.LogEntryCount;
            if (logCount > VendingInterfaceSnapshot.MaxLogEntries)
            {
                logCount = VendingInterfaceSnapshot.MaxLogEntries;
            }

            for (int i = 0; i < logCount; i++)
            {
                model.ActionLog.Add(GetLogEntry(snapshot, i));
            }

            return model;
        }

        private static VendingProductSnapshot GetProduct(VendingInterfaceSnapshot snapshot, int index)
        {
            return index switch
            {
                0 => snapshot.Product0,
                1 => snapshot.Product1,
                2 => snapshot.Product2,
                3 => snapshot.Product3,
                4 => snapshot.Product4,
                5 => snapshot.Product5,
                6 => snapshot.Product6,
                7 => snapshot.Product7,
                8 => snapshot.Product8,
                9 => snapshot.Product9,
                10 => snapshot.Product10,
                11 => snapshot.Product11,
                _ => default,
            };
        }

        private static VendingTrayItemSnapshot GetTrayItem(VendingInterfaceSnapshot snapshot, int index)
        {
            return index switch
            {
                0 => snapshot.Tray0,
                1 => snapshot.Tray1,
                2 => snapshot.Tray2,
                3 => snapshot.Tray3,
                _ => default,
            };
        }

        private static string GetLogEntry(VendingInterfaceSnapshot snapshot, int index)
        {
            return index switch
            {
                0 => snapshot.Log0,
                1 => snapshot.Log1,
                2 => snapshot.Log2,
                3 => snapshot.Log3,
                _ => string.Empty,
            };
        }
    }
}
