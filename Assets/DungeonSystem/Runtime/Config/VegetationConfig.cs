using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonSystem.Core;

namespace DungeonSystem.Config
{
    [Serializable]
    public class RoomBiomeMapping
    {
        public RoomType roomType;
        public BiomeConfig biome;
    }

    [CreateAssetMenu(fileName = "NewVegetationConfig", menuName = "Dungeon/Vegetation Config")]
    public class VegetationConfig : ScriptableObject
    {
        [Header("Biome Allocation")]
        [Tooltip("Chỉ định Biome cụ thể cho từng loại phòng")]
        public List<RoomBiomeMapping> biomeMappings = new List<RoomBiomeMapping>();

        [Tooltip("Biome mặc định khi loại phòng không được chỉ định trong danh sách mapping")]
        public BiomeConfig defaultBiome;

        public BiomeConfig GetBiomeForRoom(RoomType roomType)
        {
            foreach (var mapping in biomeMappings)
            {
                if (mapping.roomType == roomType)
                    return mapping.biome;
            }
            return defaultBiome;
        }
    }
}
