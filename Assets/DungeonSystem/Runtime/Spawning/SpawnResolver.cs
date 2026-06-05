using System.Collections.Generic;
using UnityEngine;

namespace DungeonSystem.Spawning
{
    public static class SpawnResolver
    {
        /// <summary>
        /// Selects the dominant spawn zone from a list of overlapping zones based on priority.
        /// If multiple zones share the highest priority, selects among them using their relative weights.
        /// </summary>
        public static SpawnZoneData ResolveOverlap(List<SpawnZoneData> overlappingZones, System.Random rng)
        {
            if (overlappingZones == null || overlappingZones.Count == 0) return null;
            if (overlappingZones.Count == 1) return overlappingZones[0];

            // Find highest priority
            int maxPriority = int.MinValue;
            int count = overlappingZones.Count;
            for (int i = 0; i < count; i++)
            {
                if (overlappingZones[i].priority > maxPriority)
                {
                    maxPriority = overlappingZones[i].priority;
                }
            }

            // Collect all zones that share the highest priority
            List<SpawnZoneData> topZones = new List<SpawnZoneData>();
            float totalWeight = 0f;
            for (int i = 0; i < count; i++)
            {
                if (overlappingZones[i].priority == maxPriority)
                {
                    topZones.Add(overlappingZones[i]);
                    totalWeight += overlappingZones[i].spawnWeight;
                }
            }

            if (topZones.Count == 1) return topZones[0];
            if (totalWeight <= 0f) return topZones[0];

            // Resolve using weighted selection among same-priority zones
            double roll = rng.NextDouble() * totalWeight;
            float sum = 0f;
            int topCount = topZones.Count;
            for (int i = 0; i < topCount; i++)
            {
                sum += topZones[i].spawnWeight;
                if (roll < sum)
                {
                    return topZones[i];
                }
            }

            return topZones[topCount - 1];
        }

        /// <summary>
        /// Selects the dominant local spawn zone from a list of overlapping zones based on priority.
        /// If multiple zones share the highest priority, selects among them using their relative weights.
        /// </summary>
        public static RoomSpawnZoneAuthoring.LocalSpawnZone ResolveOverlap(List<RoomSpawnZoneAuthoring.LocalSpawnZone> overlappingZones, System.Random rng)
        {
            if (overlappingZones == null || overlappingZones.Count == 0) return null;
            if (overlappingZones.Count == 1) return overlappingZones[0];

            // Find highest priority
            int maxPriority = int.MinValue;
            int count = overlappingZones.Count;
            for (int i = 0; i < count; i++)
            {
                if (overlappingZones[i].priority > maxPriority)
                {
                    maxPriority = overlappingZones[i].priority;
                }
            }

            // Collect all zones that share the highest priority
            List<RoomSpawnZoneAuthoring.LocalSpawnZone> topZones = new List<RoomSpawnZoneAuthoring.LocalSpawnZone>();
            float totalWeight = 0f;
            for (int i = 0; i < count; i++)
            {
                if (overlappingZones[i].priority == maxPriority)
                {
                    topZones.Add(overlappingZones[i]);
                    totalWeight += overlappingZones[i].spawnWeight;
                }
            }

            if (topZones.Count == 1) return topZones[0];
            if (totalWeight <= 0f) return topZones[0];

            // Resolve using weighted selection among same-priority zones
            double roll = rng.NextDouble() * totalWeight;
            float sum = 0f;
            int topCount = topZones.Count;
            for (int i = 0; i < topCount; i++)
            {
                sum += topZones[i].spawnWeight;
                if (roll < sum)
                {
                    return topZones[i];
                }
            }

            return topZones[topCount - 1];
        }

        /// <summary>
        /// Allocation-free method to determine the dominant spawn zone at a given coordinate.
        /// </summary>
        public static SpawnZoneData ResolveDominantZone(List<SpawnZoneData> zones, Vector2 localPt, Vector2 roomRealSize, Vector2 roomNoiseOffset, System.Random rng)
        {
            if (zones == null || zones.Count == 0) return null;
            
            // Pass 1: Find max priority among zones containing the point
            int maxPriority = int.MinValue;
            bool foundAny = false;
            int count = zones.Count;
            for (int i = 0; i < count; i++)
            {
                var zone = zones[i];
                if (zone == null) continue;
                if (SpawnShapeEvaluator.ContainsPoint(zone, localPt, roomRealSize, roomNoiseOffset))
                {
                    foundAny = true;
                    if (zone.priority > maxPriority)
                    {
                        maxPriority = zone.priority;
                    }
                }
            }

            if (!foundAny) return null;

            // Pass 2: Sum weights of zones with max priority
            float totalWeight = 0f;
            int countMatching = 0;
            SpawnZoneData firstMatching = null;
            for (int i = 0; i < count; i++)
            {
                var zone = zones[i];
                if (zone == null) continue;
                if (zone.priority == maxPriority && SpawnShapeEvaluator.ContainsPoint(zone, localPt, roomRealSize, roomNoiseOffset))
                {
                    totalWeight += zone.spawnWeight;
                    countMatching++;
                    if (firstMatching == null)
                    {
                        firstMatching = zone;
                    }
                }
            }

            if (countMatching == 1 || totalWeight <= 0f)
            {
                return firstMatching;
            }

            // Pass 3: Select by weight
            double roll = rng.NextDouble() * totalWeight;
            float sum = 0f;
            SpawnZoneData lastMatching = null;
            for (int i = 0; i < count; i++)
            {
                var zone = zones[i];
                if (zone == null) continue;
                if (zone.priority == maxPriority && SpawnShapeEvaluator.ContainsPoint(zone, localPt, roomRealSize, roomNoiseOffset))
                {
                    sum += zone.spawnWeight;
                    lastMatching = zone;
                    if (roll < sum)
                    {
                        return zone;
                    }
                }
            }

            return lastMatching;
        }

        /// <summary>
        /// Allocation-free method to determine the dominant local spawn zone at a given coordinate.
        /// </summary>
        public static RoomSpawnZoneAuthoring.LocalSpawnZone ResolveDominantZone(List<RoomSpawnZoneAuthoring.LocalSpawnZone> zones, Vector2 localPt, Vector2 roomRealSize, Vector2 roomNoiseOffset, System.Random rng)
        {
            if (zones == null || zones.Count == 0) return null;
            
            // Pass 1: Find max priority among zones containing the point
            int maxPriority = int.MinValue;
            bool foundAny = false;
            int count = zones.Count;
            for (int i = 0; i < count; i++)
            {
                var zone = zones[i];
                if (zone == null) continue;
                if (SpawnShapeEvaluator.ContainsPoint(zone, localPt, roomRealSize, roomNoiseOffset))
                {
                    foundAny = true;
                    if (zone.priority > maxPriority)
                    {
                        maxPriority = zone.priority;
                    }
                }
            }

            if (!foundAny) return null;

            // Pass 2: Sum weights of zones with max priority
            float totalWeight = 0f;
            int countMatching = 0;
            RoomSpawnZoneAuthoring.LocalSpawnZone firstMatching = null;
            for (int i = 0; i < count; i++)
            {
                var zone = zones[i];
                if (zone == null) continue;
                if (zone.priority == maxPriority && SpawnShapeEvaluator.ContainsPoint(zone, localPt, roomRealSize, roomNoiseOffset))
                {
                    totalWeight += zone.spawnWeight;
                    countMatching++;
                    if (firstMatching == null)
                    {
                        firstMatching = zone;
                    }
                }
            }

            if (countMatching == 1 || totalWeight <= 0f)
            {
                return firstMatching;
            }

            // Pass 3: Select by weight
            double roll = rng.NextDouble() * totalWeight;
            float sum = 0f;
            RoomSpawnZoneAuthoring.LocalSpawnZone lastMatching = null;
            for (int i = 0; i < count; i++)
            {
                var zone = zones[i];
                if (zone == null) continue;
                if (zone.priority == maxPriority && SpawnShapeEvaluator.ContainsPoint(zone, localPt, roomRealSize, roomNoiseOffset))
                {
                    sum += zone.spawnWeight;
                    lastMatching = zone;
                    if (roll < sum)
                    {
                        return zone;
                    }
                }
            }

            return lastMatching;
        }
    }
}
