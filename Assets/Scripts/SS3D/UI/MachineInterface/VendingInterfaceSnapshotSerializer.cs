using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class VendingInterfaceSnapshotSerializer
    {
        public static void WriteVendingInterfaceSnapshot(this Writer writer, VendingInterfaceSnapshot snapshot)
        {
            writer.WriteInt32(snapshot.MachineObjectId);
            writer.WriteString(snapshot.InterfaceId);
            writer.WriteString(snapshot.Title);
            writer.WriteString(snapshot.Subtitle);
            writer.WriteString(snapshot.ModelLabel);
            writer.WriteString(snapshot.ConnectionStatus);
            writer.WriteString(snapshot.StockedReadout);
            writer.WriteBoolean(snapshot.PowerOk);
            writer.WriteBoolean(snapshot.IdScanned);
            writer.WriteByte(snapshot.VendingProductIndex);

            int productCount = snapshot.ProductCount;
            if (productCount > VendingInterfaceSnapshot.MaxProducts)
            {
                productCount = VendingInterfaceSnapshot.MaxProducts;
            }

            writer.WriteByte((byte)productCount);
            for (int i = 0; i < productCount; i++)
            {
                WriteProduct(writer, GetProduct(snapshot, i));
            }

            int trayCount = snapshot.TrayItemCount;
            if (trayCount > VendingInterfaceSnapshot.MaxTrayItems)
            {
                trayCount = VendingInterfaceSnapshot.MaxTrayItems;
            }

            writer.WriteByte((byte)trayCount);
            for (int i = 0; i < trayCount; i++)
            {
                WriteTrayItem(writer, GetTrayItem(snapshot, i));
            }

            int logCount = snapshot.LogEntryCount;
            if (logCount > VendingInterfaceSnapshot.MaxLogEntries)
            {
                logCount = VendingInterfaceSnapshot.MaxLogEntries;
            }

            writer.WriteByte((byte)logCount);
            for (int i = 0; i < logCount; i++)
            {
                writer.WriteString(GetLogEntry(snapshot, i));
            }
        }

        public static VendingInterfaceSnapshot ReadVendingInterfaceSnapshot(this Reader reader)
        {
            VendingInterfaceSnapshot snapshot = new()
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
                Title = reader.ReadString(),
                Subtitle = reader.ReadString(),
                ModelLabel = reader.ReadString(),
                ConnectionStatus = reader.ReadString(),
                StockedReadout = reader.ReadString(),
                PowerOk = reader.ReadBoolean(),
                IdScanned = reader.ReadBoolean(),
                VendingProductIndex = reader.ReadByte(),
                ProductCount = reader.ReadByte(),
            };

            if (snapshot.ProductCount > VendingInterfaceSnapshot.MaxProducts)
            {
                snapshot.ProductCount = VendingInterfaceSnapshot.MaxProducts;
            }

            for (int i = 0; i < snapshot.ProductCount; i++)
            {
                VendingProductSnapshot product = ReadProduct(reader);
                SetProduct(ref snapshot, i, product);
            }

            snapshot.TrayItemCount = reader.ReadByte();
            if (snapshot.TrayItemCount > VendingInterfaceSnapshot.MaxTrayItems)
            {
                snapshot.TrayItemCount = VendingInterfaceSnapshot.MaxTrayItems;
            }

            for (int i = 0; i < snapshot.TrayItemCount; i++)
            {
                VendingTrayItemSnapshot trayItem = ReadTrayItem(reader);
                SetTrayItem(ref snapshot, i, trayItem);
            }

            snapshot.LogEntryCount = reader.ReadByte();
            if (snapshot.LogEntryCount > VendingInterfaceSnapshot.MaxLogEntries)
            {
                snapshot.LogEntryCount = VendingInterfaceSnapshot.MaxLogEntries;
            }

            for (int i = 0; i < snapshot.LogEntryCount; i++)
            {
                string logEntry = reader.ReadString();
                SetLogEntry(ref snapshot, i, logEntry);
            }

            return snapshot;
        }

        private static void WriteProduct(Writer writer, VendingProductSnapshot product)
        {
            writer.WriteString(product.Name);
            writer.WriteInt32(product.Stock);
            writer.WriteBoolean(product.RequiresId);
        }

        private static VendingProductSnapshot ReadProduct(Reader reader)
        {
            return new VendingProductSnapshot
            {
                Name = reader.ReadString(),
                Stock = reader.ReadInt32(),
                RequiresId = reader.ReadBoolean(),
            };
        }

        private static void WriteTrayItem(Writer writer, VendingTrayItemSnapshot trayItem)
        {
            writer.WriteString(trayItem.Name);
        }

        private static VendingTrayItemSnapshot ReadTrayItem(Reader reader)
        {
            return new VendingTrayItemSnapshot
            {
                Name = reader.ReadString(),
            };
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

        private static void SetProduct(ref VendingInterfaceSnapshot snapshot, int index, VendingProductSnapshot product)
        {
            switch (index)
            {
                case 0: snapshot.Product0 = product; break;
                case 1: snapshot.Product1 = product; break;
                case 2: snapshot.Product2 = product; break;
                case 3: snapshot.Product3 = product; break;
                case 4: snapshot.Product4 = product; break;
                case 5: snapshot.Product5 = product; break;
                case 6: snapshot.Product6 = product; break;
                case 7: snapshot.Product7 = product; break;
                case 8: snapshot.Product8 = product; break;
                case 9: snapshot.Product9 = product; break;
                case 10: snapshot.Product10 = product; break;
                case 11: snapshot.Product11 = product; break;
            }
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

        private static void SetTrayItem(ref VendingInterfaceSnapshot snapshot, int index, VendingTrayItemSnapshot trayItem)
        {
            switch (index)
            {
                case 0: snapshot.Tray0 = trayItem; break;
                case 1: snapshot.Tray1 = trayItem; break;
                case 2: snapshot.Tray2 = trayItem; break;
                case 3: snapshot.Tray3 = trayItem; break;
            }
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

        private static void SetLogEntry(ref VendingInterfaceSnapshot snapshot, int index, string value)
        {
            switch (index)
            {
                case 0: snapshot.Log0 = value; break;
                case 1: snapshot.Log1 = value; break;
                case 2: snapshot.Log2 = value; break;
                case 3: snapshot.Log3 = value; break;
            }
        }
    }
}
