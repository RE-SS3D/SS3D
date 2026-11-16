using System.Collections.Generic;
using System.Reflection;
using SS3D.Core;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Roles;
using UnityEngine;

namespace EditorTests
{
    internal static class IdAccessTestFixtures
    {
        public static IdAccessSubSystem CreateRegisteredIdAccess(out GameObject subsystemObject)
        {
            subsystemObject = new GameObject("IdAccessSubSystem");
            IdAccessSubSystem subsystem = subsystemObject.AddComponent<IdAccessSubSystem>();
            SubSystems.Register(subsystem);
            return subsystem;
        }

        public static void DestroyRegistered(IdAccessSubSystem subsystem, GameObject subsystemObject)
        {
            if (subsystem != null)
            {
                SubSystems.Unregister(subsystem);
            }

            if (subsystemObject != null)
            {
                Object.DestroyImmediate(subsystemObject);
            }
        }

        public static CrewRecord CreateEngineerRecord(IdAccessSubSystem subsystem, string name = "Test User")
        {
            return RegisterRecord(subsystem, new CrewRecord
            {
                Id = AllocateRecordId(subsystem),
                Name = name,
                JobName = "Engineer",
                Department = Department.Engineering,
                Access = AccessPresets.Engineer,
                ConnectionStatus = CrewConnectionStatus.Online,
            });
        }

        public static CrewRecord CreateCivilianRecord(IdAccessSubSystem subsystem, string name = "Assistant")
        {
            return RegisterRecord(subsystem, new CrewRecord
            {
                Id = AllocateRecordId(subsystem),
                Name = name,
                JobName = "Assistant",
                Department = Department.Civilian,
                Access = AccessPresets.StandardCrew,
                ConnectionStatus = CrewConnectionStatus.Online,
            });
        }

        public static IDCard CreateBoundCard(CrewRecord record)
        {
            GameObject cardObject = new("IDCard");
            IDCard card = cardObject.AddComponent<IDCard>();
            card.ServerBind(record);
            return card;
        }

        public static AttachedContainer CreateContainer(ContainerType type, Vector2Int? size = null)
        {
            GameObject containerObject = new($"Container-{type}");
            AttachedContainer container = containerObject.AddComponent<AttachedContainer>();
            container.Init(size ?? new Vector2Int(4, 4), null);
            SetContainerType(container, type);
            return container;
        }

        public static HumanInventory CreateInventory(
            IEnumerable<AttachedContainer> containers,
            IEnumerable<AttachedContainer> handContainers = null)
        {
            GameObject inventoryObject = new("HumanInventory");
            HumanInventory inventory = inventoryObject.AddComponent<HumanInventory>();

            if (handContainers != null)
            {
                GameObject handsObject = new("Hands");
                handsObject.transform.SetParent(inventoryObject.transform);
                Hands hands = handsObject.AddComponent<Hands>();
                hands.Inventory = inventory;
                inventory.Hands = hands;

                var playerHands = new List<Hand>();
                foreach (AttachedContainer handContainer in handContainers)
                {
                    GameObject handObject = new("Hand");
                    handObject.transform.SetParent(handsObject.transform);
                    Hand hand = handObject.AddComponent<Hand>();
                    hand.Container = handContainer;
                    hand.HandsController = hands;
                    playerHands.Add(hand);
                }

                SetPlayerHands(hands, playerHands);
            }

            foreach (AttachedContainer container in containers)
            {
                inventory.TryAddContainer(container);
            }

            return inventory;
        }

        public static PDA CreatePdaWithInsertedCard(IDCard card)
        {
            GameObject pdaObject = new("PDA");
            PDA pda = pdaObject.AddComponent<PDA>();
            AttachedContainer pdaContainer = pdaObject.AddComponent<AttachedContainer>();
            pdaContainer.Init(new Vector2Int(1, 1), null);
            pdaContainer.AddItem(card);
            SetPrivateField(pda, "attachedContainer", pdaContainer);
            return pda;
        }

        public static void DestroyObjects(params Object[] objects)
        {
            foreach (Object obj in objects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        private static void SetContainerType(AttachedContainer container, ContainerType type)
        {
            SetPrivateField(container, "_type", type);
        }

        private static void SetPlayerHands(Hands hands, List<Hand> playerHands)
        {
            hands.PlayerHands = playerHands;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static CrewRecord RegisterRecord(IdAccessSubSystem subsystem, CrewRecord record)
        {
            var records = (Dictionary<CrewRecordId, CrewRecord>)typeof(IdAccessSubSystem)
                .GetField("_records", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(subsystem);
            records[record.Id] = record;
            return record;
        }

        private static CrewRecordId AllocateRecordId(IdAccessSubSystem subsystem)
        {
            FieldInfo nextIdField = typeof(IdAccessSubSystem).GetField("_nextRecordId", BindingFlags.Instance | BindingFlags.NonPublic);
            uint nextId = (uint)nextIdField.GetValue(subsystem);
            nextIdField.SetValue(subsystem, nextId + 1);
            return new CrewRecordId(nextId);
        }
    }
}
