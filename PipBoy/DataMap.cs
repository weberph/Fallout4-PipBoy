using System.Collections.Generic;
using System.Linq;

namespace PipBoy
{
    public enum DataCategory
    {
        Radio,
        Perks,
        Stats,
        Special,
        Inventory,
        Quests,
        Workshop,
        Log,
        Map,
        PlayerInfo,
        Status
    }

    public class DataMap
    {
        public static readonly Dictionary<DataCategory, string> DataKeys = new Dictionary<DataCategory, string>
        {
            { DataCategory.Radio, "Radio" },
            { DataCategory.Perks, "Perks" },
            { DataCategory.Stats, "Stats" },
            { DataCategory.Special, "Special" },
            { DataCategory.Inventory, "Inventory" },
            { DataCategory.Quests, "Quests" },
            { DataCategory.Workshop, "Workshop" },
            { DataCategory.Log, "Log" },
            { DataCategory.Map, "Map" },
            { DataCategory.PlayerInfo, "PlayerInfo" },
            { DataCategory.Status, "Status" },
        };
        
        private readonly Dictionary<string, uint> _categoryIndexMap;

        public DataMap(Dictionary<uint, DataElement> data)
        {
            _categoryIndexMap = ((MapElement)data[0]).Value.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        }

        public bool TryGetIndex(DataCategory category, out uint index)
        {
            return _categoryIndexMap.TryGetValue(DataKeys[category], out index);
        }
    }
}
