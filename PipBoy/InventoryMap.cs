using System.Collections.Generic;
using System.Linq;

namespace PipBoy
{
    public enum InventoryCategory
    {
        Aid,
        Recordings,
        Weapons,
        Writings,
        Junk,
        Apparel,
        Keys,
        Ammo,
        Components
    }

    public class InventoryMap
    {
        // 47 -> list<attributeMapIndex> (e.g. Federal Ration Stockpile Password)
        // HolotapePlaying -> bool
        // 44 -> list<attributeMapIndex> (e.g. .38 Round)
        // UnderwearType -> int32 (e.g. 1)
        // stimpakObjectIDIsValid -> bool
        // InvComponents -> list<attributeMapIndex> (e.g. Silver)
        // 43 -> list<attributeMapIndex> (e.g. Nuka Grenade)
        // 30 -> list<attributeMapIndex> (e.g. Grognak the Barbarian)
        // stimpakObjectID -> sint (e.g. 24601)
        // radawayObjectID -> sint (e.g. 25442)
        // 50 -> list<attributeMapIndex> (e.g. Atomic Command)
        // 48 -> list<attributeMapIndex> (e.g. Stimpack)
        // 35 -> list<attributeMapIndex> (e.g. Bobby Pin)
        // SortMode -> uint32 (e.g. 0)
        // Version -> sint32 (e.g. 16)
        // stimpakObjectIDIsValid -> bool
        // 29 -> list<attributeMapIndex> (e.g. Wedding Ring)
        // sortedIDS -> list<Index> -> sint32 -> attributeMap (e.g. .308 Round)

        private static readonly Dictionary<InventoryCategory, string> InventoryKeys = new Dictionary<InventoryCategory, string>()
        {
            { InventoryCategory.Apparel, "29" },
            { InventoryCategory.Writings, "30" },
            { InventoryCategory.Junk, "35" },
            { InventoryCategory.Weapons, "43" },
            { InventoryCategory.Ammo, "44" },
            { InventoryCategory.Keys, "47" },
            { InventoryCategory.Aid, "48" },
            { InventoryCategory.Recordings, "50" },
            { InventoryCategory.Components, "InvComponents" },
        };

        private readonly Dictionary<string, uint> _inventoryIndexMap;

        public InventoryMap(Dictionary<uint, DataElement> data, uint inventoryIndex)
        {
            _inventoryIndexMap = ((MapElement)data[inventoryIndex]).Value.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        }

        public bool TryGetIndex(InventoryCategory category, out uint index)
        {
            return _inventoryIndexMap.TryGetValue(InventoryKeys[category], out index);
        }
    }
}
