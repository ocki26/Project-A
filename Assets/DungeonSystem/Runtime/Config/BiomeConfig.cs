using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSystem.Config
{
    [Serializable]
    public class VegetationItem
    {
        public string name;
        public GameObject prefab;
        [Range(1, 100)] public int weight = 50;
        
        [Tooltip("Khoảng cách tối thiểu tới các thực vật khác để tránh overlap")]
        public float minDistance = 1.2f;
        
        public float scaleMin = 0.8f;
        public float scaleMax = 1.3f;
    }

    [CreateAssetMenu(fileName = "NewBiomeConfig", menuName = "Dungeon/Biome Config")]
    public class BiomeConfig : ScriptableObject
    {
        [Header("General Settings")]
        public string biomeName = "Forest";

        [Header("Density Settings (Per 20x20 Room)")]
        public int minTreeCount = 5;
        public int maxTreeCount = 15;

        [Header("Safety Boundaries (Meters)")]
        [Tooltip("Khoảng cách an toàn tránh xa tường bao quanh phòng")]
        public float wallSafetyMargin = 2.0f;
        
        [Tooltip("Khoảng cách an toàn tránh cản trở lối đi cửa ra vào")]
        public float doorSafetyRadius = 3.0f;

        [Header("Vegetation Prefabs")]
        public List<VegetationItem> vegetationItems = new List<VegetationItem>();
    }
}
