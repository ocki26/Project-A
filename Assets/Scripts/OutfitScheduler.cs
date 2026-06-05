using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Outfit Schedule", menuName = "LPC/Outfit Schedule")]
public class OutfitSchedule : ScriptableObject
{
    [System.Serializable]
    public class TimeSlot
    {
        [Range(0f, 24f)]
        [Tooltip("The time of day in hours (0-24) when this outfit should be active.")]
        public float hour;
        public CharacterLoadout loadout;
    }

    public List<TimeSlot> slots = new List<TimeSlot>();

    public CharacterLoadout GetLoadoutForHour(float hour)
    {
        if (slots == null || slots.Count == 0) return null;

        // Find the time slot that is closest to the given hour
        TimeSlot bestSlot = null;
        float minDiff = float.MaxValue;

        foreach (var slot in slots)
        {
            if (slot.loadout == null) continue;
            float diff = Mathf.Abs(slot.hour - hour);
            // Handle 24-hour wrap-around
            if (diff > 12f) diff = 24f - diff;

            if (diff < minDiff)
            {
                minDiff = diff;
                bestSlot = slot;
            }
        }

        return bestSlot?.loadout;
    }
}
