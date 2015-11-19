using System.Collections.Generic;
using System.IO;

namespace PipBoy.Debugging
{
    public class InitialPacketDumper
    {
        private readonly TextWriter _writer;

        public InitialPacketDumper(TextWriter writer)
        {
            _writer = writer;
        }
        public static void DumpInitialPacket(Dictionary<uint, DataElement> data, TextWriter writer)
        {
            new InitialPacketDumper(writer).DumpInitialPacket(data);
        }

        public void DumpInitialPacket(Dictionary<uint, DataElement> data)
        {
            var dataMap = new DataMap(data);

            uint index;
            if (dataMap.TryGetIndex(DataCategory.Radio, out index))
            {
                var radioStations = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(radioStations);
            }

            if (dataMap.TryGetIndex(DataCategory.Perks, out index))
            {
                var perks = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(perks);
            }

            if (dataMap.TryGetIndex(DataCategory.Stats, out index))
            {
                var stats = ReadListOfAttributes(data, index);
                PrintAttributeMap(stats);
            }

            if (dataMap.TryGetIndex(DataCategory.Special, out index))
            {
                var special = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(special);
            }

            if (dataMap.TryGetIndex(DataCategory.Quests, out index))
            {
                var quests = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(quests);
            }

            if (dataMap.TryGetIndex(DataCategory.Workshop, out index))
            {
                var workshops = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(workshops);
            }

            if (dataMap.TryGetIndex(DataCategory.Log, out index))
            {
                var log = ReadListOfAttributeMaps(data, index);
                PrintAttributeMaps(log);
            }

            if (dataMap.TryGetIndex(DataCategory.Map, out index))
            {
                var map = ReadListOfAttributes(data, index);
                PrintAttributeMap(map);
            }

            if (dataMap.TryGetIndex(DataCategory.PlayerInfo, out index))
            {
                var playerInfo = ReadListOfAttributes(data, index);
                PrintAttributeMap(playerInfo);
            }

            if (dataMap.TryGetIndex(DataCategory.Status, out index))
            {
                var status = ReadListOfAttributes(data, index);
                PrintAttributeMap(status);
            }

            if (dataMap.TryGetIndex(DataCategory.Inventory, out index))
            {
                var inventoryMap = new InventoryMap(data, index);

                if (inventoryMap.TryGetIndex(InventoryCategory.Aid, out index))
                {
                    var aid = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(aid);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Recordings, out index))
                {
                    var recordings = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(recordings);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Weapons, out index))
                {
                    var weapons = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(weapons);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Writings, out index))
                {
                    var books = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(books);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Junk, out index))
                {
                    var junk = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(junk);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Apparel, out index))
                {
                    var apparel = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(apparel);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Keys, out index))
                {
                    var keys = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(keys);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Ammo, out index))
                {
                    var ammo = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(ammo);
                }

                if (inventoryMap.TryGetIndex(InventoryCategory.Components, out index))
                {
                    var components = ReadListOfAttributeMaps(data, index);
                    PrintAttributeMaps(components);
                }
            }
        }

        private Dictionary<string, DataElement> ReadListOfAttributes(Dictionary<uint, DataElement> data, uint itemIndex)
        {
            // reads index -> map<AttributeName, AttributeValueIndex>
            var itemAttributes = new Dictionary<string, DataElement>();
            var attributes = ((MapElement)data[itemIndex]).Value;
            foreach (var attribute in attributes)
            {
                itemAttributes.Add(attribute.Value, data[attribute.Key]);
            }
            return itemAttributes;
        }

        private List<Dictionary<string, DataElement>> ReadListOfAttributeMaps(Dictionary<uint, DataElement> data, uint itemIndex)
        {
            // reads index -> list<Index> -> map<AttributeName, AttributeValueIndex>
            var result = new List<Dictionary<string, DataElement>>();
            var itemList = ((ListElement)data[itemIndex]).Value;
            foreach (var itemAttributeListIndex in itemList)
            {
                result.Add(ReadListOfAttributes(data, itemAttributeListIndex));
            }
            return result;
        }

        private void PrintAttributeMap(Dictionary<string, DataElement> attributeMap)
        {
            foreach (var attribute in attributeMap)
            {
                _writer.WriteLine("{0}: {1}", attribute.Key, attribute.Value);
            }
            _writer.WriteLine();
        }

        private void PrintAttributeMaps(List<Dictionary<string, DataElement>> attributeMaps)
        {
            foreach (var attributeMap in attributeMaps)
            {
                PrintAttributeMap(attributeMap);
            }
        }
    }
}
