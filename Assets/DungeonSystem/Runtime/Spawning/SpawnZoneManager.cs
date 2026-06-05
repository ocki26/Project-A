using System.Collections.Generic;
using UnityEngine;
using DungeonSystem.Core;
using DungeonSystem.Generation;
using DungeonSystem.Optimization;
using DungeonSystem.Spawning.Spatial;

namespace DungeonSystem.Spawning
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpawnDebugger))]
    public class SpawnZoneManager : MonoBehaviour
    {
        [System.Serializable]
        public struct RoomPresetMapping
        {
            public RoomType roomType;
            public RoomSpawnPreset preset;
        }

        [Header("Preset Configurations")]
        public List<RoomPresetMapping> roomPresets = new List<RoomPresetMapping>();
        public RoomSpawnPreset defaultPreset;

        [Header("Collision & Obstacle Checking")]
        [Tooltip("Layer mask for static physics obstacles to avoid spawning objects inside them.")]
        public LayerMask obstacleLayer;

        private readonly List<GameObject> activeSpawns = new List<GameObject>();
        private readonly List<Vector2> tempDoorPositions = new List<Vector2>();
        private SpatialHashGrid spatialGrid;
        private SpawnDebugger debugger;

        private void Awake()
        {
            debugger = GetComponent<SpawnDebugger>();
        }

        /// <summary>
        /// Clears all previously spawned objects and returns them to the pool.
        /// </summary>
        public void ClearObjects()
        {
            if (VegetationPooler.Instance != null)
            {
                int count = activeSpawns.Count;
                for (int i = 0; i < count; i++)
                {
                    if (activeSpawns[i] != null)
                    {
                        VegetationPooler.Instance.Recycle(activeSpawns[i]);
                    }
                }
            }
            activeSpawns.Clear();

            if (debugger != null)
            {
                debugger.Clear();
            }
        }

        /// <summary>
        /// Generates objects for all rooms in the layout based on spawn presets and zones.
        /// </summary>
        public void GenerateObjects(DungeonLayout layout, System.Random rng, Dictionary<Vector2Int, GameObject> spawnedRoomsMap, int cellSize)
        {
            ClearObjects();

            if (layout == null) return;

            // Initialize or clear spatial grid. Cell size of grid is set to an average min-distance
            float averageMinDistance = 1.5f;
            if (spatialGrid == null)
            {
                spatialGrid = new SpatialHashGrid(averageMinDistance);
            }
            else
            {
                spatialGrid.Reset(averageMinDistance);
            }

            foreach (var node in layout.Rooms)
            {
                // Skip corridors unless configured otherwise
                if (node.type == RoomType.Corridor) continue;

                // 1. Check if the spawned room GameObject has RoomSpawnZoneAuthoring attached
                GameObject roomObj = null;
                if (spawnedRoomsMap != null)
                {
                    spawnedRoomsMap.TryGetValue(node.gridPos, out roomObj);
                }

                if (roomObj != null)
                {
                    RoomSpawnZoneAuthoring authoring = roomObj.GetComponent<RoomSpawnZoneAuthoring>();
                    if (authoring != null && authoring.isActive)
                    {
                        // Spawn using hand-painted local zones on prefab
                        SpawnObjectsViaLocalAuthoring(node, authoring, rng, roomObj.transform, cellSize);
                        continue;
                    }
                }

                // Fallback to room preset ScriptableObject
                RoomSpawnPreset preset = GetPresetForRoom(node, spawnedRoomsMap);
                if (preset == null || !preset.isActive) continue;

                // Spawning in this room
                SpawnObjectsInRoom(node, preset, rng, spawnedRoomsMap, cellSize);
            }
        }

        private RoomSpawnPreset GetPresetForRoom(RoomNode node, Dictionary<Vector2Int, GameObject> spawnedRoomsMap)
        {
            // 1. Check if the spawned room GameObject's RoomPrefabData has an override preset
            if (spawnedRoomsMap != null && spawnedRoomsMap.TryGetValue(node.gridPos, out GameObject roomObj))
            {
                if (roomObj != null)
                {
                    RoomPrefabData prefabData = roomObj.GetComponent<RoomPrefabData>();
                    if (prefabData != null && prefabData.spawnPreset != null)
                    {
                        return prefabData.spawnPreset;
                    }
                }
            }

            // 2. Check room type mapping
            int mappingCount = roomPresets.Count;
            for (int i = 0; i < mappingCount; i++)
            {
                if (roomPresets[i].roomType == node.type)
                {
                    return roomPresets[i].preset;
                }
            }

            // 3. Fallback to default preset
            return defaultPreset;
        }

        private void SpawnObjectsInRoom(RoomNode node, RoomSpawnPreset preset, System.Random rng, Dictionary<Vector2Int, GameObject> spawnedRoomsMap, int cellSize)
        {
            // Position in world space of the room bottom-left
            Vector3 bottomLeftPos = new Vector3(node.gridPos.x * cellSize, node.gridPos.y * cellSize, 0f);

            // Real room size from prefab
            Vector2 roomRealSize = (node.assignedPrefab != null)
                ? new Vector2(node.assignedPrefab.roomSize.x, node.assignedPrefab.roomSize.y)
                : new Vector2(cellSize, cellSize);

            // Parent transform
            Transform roomParent = null;
            if (spawnedRoomsMap != null && spawnedRoomsMap.TryGetValue(node.gridPos, out GameObject roomObj))
            {
                if (roomObj != null) roomParent = roomObj.transform;
            }

            // Get door locations to avoid blocking paths (allocation-free)
            PopulateDoorWorldPositions(node, bottomLeftPos, roomRealSize, tempDoorPositions);

            // Roll spawn chance for each zone and build a list of active zones for this room instance
            List<SpawnZoneData> activeZones = new List<SpawnZoneData>(preset.zones.Count);
            foreach (var zone in preset.zones)
            {
                if (zone == null) continue;
                if (rng.NextDouble() <= zone.zoneSpawnChance)
                {
                    activeZones.Add(zone);
                }
            }

            if (debugger != null)
            {
                foreach (var zone in activeZones)
                {
                    debugger.RegisterZoneOutline(bottomLeftPos, roomRealSize, zone);
                }
            }

            // A noise offset specific to this room to vary perlin masks per instance
            Vector2 roomNoiseOffset = new Vector2(
                (float)(rng.NextDouble() * 10000.0),
                (float)(rng.NextDouble() * 10000.0)
            );

            int totalSpawnedInRoom = 0;

            // Execute spawning zone by zone
            foreach (var zone in activeZones)
            {
                if (zone == null || zone.allowedObjects == null || zone.allowedObjects.Count == 0) continue;
                if (totalSpawnedInRoom >= preset.maxTotalSpawns) break;

                // Run spawning for each allowed object rule in the zone
                foreach (var rule in zone.allowedObjects)
                {
                    if (rule == null || !rule.HasValidPrefab()) continue;
                    if (totalSpawnedInRoom >= preset.maxTotalSpawns) break;

                    // Calculate target count for this specific rule inside this zone
                    int targetCount = rng.Next(rule.minSpawnCount, rule.maxSpawnCount + 1);
                    
                    int attempts = 0;
                    // Up to 15 attempts per target spawn to find a valid location
                    int maxAttempts = targetCount * 15; 
                    int spawnedCount = 0;

                    while (spawnedCount < targetCount && attempts < maxAttempts)
                    {
                        if (totalSpawnedInRoom >= preset.maxTotalSpawns) break;
                        attempts++;

                        // 1. Generate candidate point in local room coordinates
                        float rx = (float)(rng.NextDouble() * roomRealSize.x);
                        float ry = (float)(rng.NextDouble() * roomRealSize.y);
                        Vector2 localPt = new Vector2(rx, ry);
                        Vector3 candidateWorldPos = bottomLeftPos + new Vector3(localPt.x, localPt.y, rule.heightOffset);

                        // 2. Shape Containment Check
                        if (!SpawnShapeEvaluator.ContainsPoint(zone, localPt, roomRealSize, roomNoiseOffset))
                        {
                            continue; // Out of shape bounds
                        }

                        // 3. Overlap & Priority Resolution (allocation-free double pass)
                        SpawnZoneData dominantZone = SpawnResolver.ResolveDominantZone(activeZones, localPt, roomRealSize, roomNoiseOffset, rng);
                        if (dominantZone != zone)
                        {
                            // This point belongs to a higher priority overlapping zone, skip
                            continue;
                        }

                        // 4. Object Rule: Chance Check
                        // Modulate chance based on Perlin Noise Strength at location
                        float noiseStrength = Mathf.PerlinNoise(candidateWorldPos.x * 0.05f, candidateWorldPos.y * 0.05f);
                        float finalChance = rule.spawnChance * preset.globalSpawnChanceMultiplier;
                        if (zone.useNoiseModifier)
                        {
                            finalChance *= noiseStrength;
                        }

                        if (rng.NextDouble() > finalChance)
                        {
                            if (debugger != null) debugger.AddFailure(candidateWorldPos, "Failed Spawn Chance Roll");
                            continue;
                        }

                        // 5. Object Rule: Wall Safety Margins
                        if (!rule.allowWallNearSpawn)
                        {
                            if (localPt.x < rule.wallSafetyMargin || localPt.x > roomRealSize.x - rule.wallSafetyMargin ||
                                localPt.y < rule.wallSafetyMargin || localPt.y > roomRealSize.y - rule.wallSafetyMargin)
                            {
                                if (debugger != null) debugger.AddFailure(candidateWorldPos, "Too close to walls");
                                continue;
                            }
                        }

                        // 6. Object Rule: Edge Spawn
                        if (!rule.allowEdgeSpawn)
                        {
                            // Avoid outer boundaries of the room
                            float edgeMargin = 0.5f;
                            if (localPt.x < edgeMargin || localPt.x > roomRealSize.x - edgeMargin ||
                                localPt.y < edgeMargin || localPt.y > roomRealSize.y - edgeMargin)
                            {
                                if (debugger != null) debugger.AddFailure(candidateWorldPos, "Edge spawn disallowed");
                                continue;
                            }
                        }

                        // 7. Object Rule: Door Safety Radius
                        bool tooCloseToDoor = false;
                        int doorCount = tempDoorPositions.Count;
                        for (int d = 0; d < doorCount; d++)
                        {
                            if (Vector2.Distance(candidateWorldPos, tempDoorPositions[d]) < rule.doorSafetyMargin)
                            {
                                tooCloseToDoor = true;
                                break;
                            }
                        }
                        if (tooCloseToDoor)
                        {
                            if (debugger != null) debugger.AddFailure(candidateWorldPos, "Too close to door entrance");
                            continue;
                        }

                        // 8. Spatial Proximity Check (Other objects)
                        if (!rule.allowOverlap && spatialGrid.CheckProximity(candidateWorldPos, rule.minDistanceBetween))
                        {
                            if (debugger != null) debugger.AddFailure(candidateWorldPos, "Proximity overlap conflict");
                            continue;
                        }

                        // 9. Physical Collision Check (Obstacles in Scene)
                        if (obstacleLayer != 0)
                        {
                            // Overlap radius based on rule minimum distance or standard scale
                            float collisionRadius = Mathf.Min(0.4f, rule.minDistanceBetween * 0.4f);
                            Collider2D hit = Physics2D.OverlapCircle(candidateWorldPos, collisionRadius, obstacleLayer);
                            if (hit != null)
                            {
                                if (debugger != null) debugger.AddFailure(candidateWorldPos, "Physical obstacle collision");
                                continue;
                            }
                        }

                        // Point is fully validated!
                        spatialGrid.Insert(candidateWorldPos);
                        if (debugger != null) debugger.AddSuccess(candidateWorldPos, rule.name);

                        // Spawn from Object Pooler
                        Quaternion rotation = rule.randomRotation
                            ? Quaternion.Euler(0f, 0f, (float)(rng.NextDouble() * 360f))
                            : Quaternion.identity;

                        float scaleVal = (float)(rng.NextDouble() * (rule.scaleRange.y - rule.scaleRange.x)) + rule.scaleRange.x;

                        if (VegetationPooler.Instance != null)
                        {
                            GameObject prefabToSpawn = rule.GetPrefabToSpawn(rng);
                            if (prefabToSpawn != null)
                            {
                                GameObject spawnedObj = VegetationPooler.Instance.Get(prefabToSpawn, candidateWorldPos, rotation, roomParent);
                                if (spawnedObj != null)
                                {
                                    spawnedObj.transform.localScale = new Vector3(scaleVal, scaleVal, 1f);
                                    activeSpawns.Add(spawnedObj);
                                    spawnedCount++;
                                    totalSpawnedInRoom++;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void PopulateDoorWorldPositions(RoomNode node, Vector3 bottomLeft, Vector2 roomSize, List<Vector2> outDoors)
        {
            outDoors.Clear();
            float halfWidth = roomSize.x * 0.5f;
            float halfHeight = roomSize.y * 0.5f;

            if ((node.requiredDoors & DoorMask.North) != 0)
                outDoors.Add(new Vector2(bottomLeft.x + halfWidth, bottomLeft.y + roomSize.y));
            if ((node.requiredDoors & DoorMask.South) != 0)
                outDoors.Add(new Vector2(bottomLeft.x + halfWidth, bottomLeft.y));
            if ((node.requiredDoors & DoorMask.East) != 0)
                outDoors.Add(new Vector2(bottomLeft.x + roomSize.x, bottomLeft.y + halfHeight));
            if ((node.requiredDoors & DoorMask.West) != 0)
                outDoors.Add(new Vector2(bottomLeft.x, bottomLeft.y + halfHeight));
        }

        private void SpawnObjectsViaLocalAuthoring(RoomNode node, RoomSpawnZoneAuthoring authoring, System.Random rng, Transform roomParent, int cellSize)
        {
            Vector3 bottomLeftPos = new Vector3(node.gridPos.x * cellSize, node.gridPos.y * cellSize, 0f);
            Vector2 roomRealSize = (node.assignedPrefab != null)
                ? new Vector2(node.assignedPrefab.roomSize.x, node.assignedPrefab.roomSize.y)
                : new Vector2(cellSize, cellSize);

            PopulateDoorWorldPositions(node, bottomLeftPos, roomRealSize, tempDoorPositions);

            // Roll spawn chance for each local zone
            List<RoomSpawnZoneAuthoring.LocalSpawnZone> activeZones = new List<RoomSpawnZoneAuthoring.LocalSpawnZone>(authoring.localZones.Count);
            foreach (var zone in authoring.localZones)
            {
                if (zone == null) continue;
                if (rng.NextDouble() <= zone.zoneSpawnChance)
                {
                    activeZones.Add(zone);
                }
            }

            if (debugger != null)
            {
                foreach (var zone in activeZones)
                {
                    debugger.RegisterZoneOutline(bottomLeftPos, roomRealSize, zone);
                }
            }

            Vector2 roomNoiseOffset = new Vector2(
                (float)(rng.NextDouble() * 10000.0),
                (float)(rng.NextDouble() * 10000.0)
            );

            int totalSpawnedInRoom = 0;

            foreach (var zone in activeZones)
            {
                if (zone == null || zone.allowedObjects == null || zone.allowedObjects.Count == 0) continue;
                if (totalSpawnedInRoom >= authoring.maxTotalSpawns) break;

                foreach (var rule in zone.allowedObjects)
                {
                    if (rule == null || !rule.HasValidPrefab()) continue;
                    if (totalSpawnedInRoom >= authoring.maxTotalSpawns) break;

                    int targetCount = rng.Next(rule.minSpawnCount, rule.maxSpawnCount + 1);
                    int attempts = 0;
                    int maxAttempts = targetCount * 15;
                    int spawnedCount = 0;

                    while (spawnedCount < targetCount && attempts < maxAttempts)
                    {
                        if (totalSpawnedInRoom >= authoring.maxTotalSpawns) break;
                        attempts++;

                        float rx = (float)(rng.NextDouble() * roomRealSize.x);
                        float ry = (float)(rng.NextDouble() * roomRealSize.y);
                        Vector2 localPt = new Vector2(rx, ry);
                        Vector3 candidateWorldPos = bottomLeftPos + new Vector3(localPt.x, localPt.y, rule.heightOffset);

                        // 1. Shape Containment Check
                        if (!SpawnShapeEvaluator.ContainsPoint(zone, localPt, roomRealSize, roomNoiseOffset))
                        {
                            continue;
                        }

                        // 2. Overlap & Priority Resolution (allocation-free double pass)
                        RoomSpawnZoneAuthoring.LocalSpawnZone dominantZone = SpawnResolver.ResolveDominantZone(activeZones, localPt, roomRealSize, roomNoiseOffset, rng);
                        if (dominantZone != zone) continue;

                        // 3. Object Rule: Chance Check
                        float noiseStrength = Mathf.PerlinNoise(candidateWorldPos.x * 0.05f, candidateWorldPos.y * 0.05f);
                        float finalChance = rule.spawnChance * authoring.globalSpawnChanceMultiplier;
                        if (zone.useNoiseModifier)
                        {
                            finalChance *= noiseStrength;
                        }

                        if (rng.NextDouble() > finalChance)
                        {
                            if (debugger != null) debugger.AddFailure(candidateWorldPos, "Failed Spawn Chance Roll");
                            continue;
                        }

                        // 4. Object Rule: Wall Safety Margins
                        if (!rule.allowWallNearSpawn)
                        {
                            if (localPt.x < rule.wallSafetyMargin || localPt.x > roomRealSize.x - rule.wallSafetyMargin ||
                                localPt.y < rule.wallSafetyMargin || localPt.y > roomRealSize.y - rule.wallSafetyMargin)
                            {
                                if (debugger != null) debugger.AddFailure(candidateWorldPos, "Too close to walls");
                                continue;
                            }
                        }

                        // 5. Object Rule: Edge Spawn
                        if (!rule.allowEdgeSpawn)
                        {
                            float edgeMargin = 0.5f;
                            if (localPt.x < edgeMargin || localPt.x > roomRealSize.x - edgeMargin ||
                                localPt.y < edgeMargin || localPt.y > roomRealSize.y - edgeMargin)
                            {
                                if (debugger != null) debugger.AddFailure(candidateWorldPos, "Edge spawn disallowed");
                                continue;
                            }
                        }

                        // 6. Object Rule: Door Safety Radius
                        bool tooCloseToDoor = false;
                        int doorCount = tempDoorPositions.Count;
                        for (int d = 0; d < doorCount; d++)
                        {
                            if (Vector2.Distance(candidateWorldPos, tempDoorPositions[d]) < rule.doorSafetyMargin)
                            {
                                tooCloseToDoor = true;
                                break;
                            }
                        }
                        if (tooCloseToDoor)
                        {
                            if (debugger != null) debugger.AddFailure(candidateWorldPos, "Too close to door entrance");
                            continue;
                        }

                        // 7. Spatial Proximity Check
                        if (!rule.allowOverlap && spatialGrid.CheckProximity(candidateWorldPos, rule.minDistanceBetween))
                        {
                            if (debugger != null) debugger.AddFailure(candidateWorldPos, "Proximity overlap conflict");
                            continue;
                        }

                        // 8. Physical Collision Check
                        if (obstacleLayer != 0)
                        {
                            float collisionRadius = Mathf.Min(0.4f, rule.minDistanceBetween * 0.4f);
                            Collider2D hit = Physics2D.OverlapCircle(candidateWorldPos, collisionRadius, obstacleLayer);
                            if (hit != null)
                            {
                                if (debugger != null) debugger.AddFailure(candidateWorldPos, "Physical obstacle collision");
                                continue;
                            }
                        }

                        // Point is validated!
                        spatialGrid.Insert(candidateWorldPos);
                        if (debugger != null) debugger.AddSuccess(candidateWorldPos, rule.name);

                        // Spawn from Object Pooler
                        Quaternion rotation = rule.randomRotation
                            ? Quaternion.Euler(0f, 0f, (float)(rng.NextDouble() * 360f))
                            : Quaternion.identity;

                        float scaleVal = (float)(rng.NextDouble() * (rule.scaleRange.y - rule.scaleRange.x)) + rule.scaleRange.x;

                        if (VegetationPooler.Instance != null)
                        {
                            GameObject prefabToSpawn = rule.GetPrefabToSpawn(rng);
                            if (prefabToSpawn != null)
                            {
                                GameObject spawnedObj = VegetationPooler.Instance.Get(prefabToSpawn, candidateWorldPos, rotation, roomParent);
                                if (spawnedObj != null)
                                {
                                    spawnedObj.transform.localScale = new Vector3(scaleVal, scaleVal, 1f);
                                    activeSpawns.Add(spawnedObj);
                                    spawnedCount++;
                                    totalSpawnedInRoom++;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
