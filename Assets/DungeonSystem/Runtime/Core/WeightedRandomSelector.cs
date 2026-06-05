using System;
using System.Collections.Generic;

namespace DungeonSystem.Core
{
    public static class WeightedRandomSelector
    {
        public static T Select<T>(IList<T> items, Func<T, float> weightFunc, System.Random random)
        {
            if (items == null || items.Count == 0) return default;
            if (items.Count == 1) return items[0];

            float totalWeight = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                totalWeight += weightFunc(items[i]);
            }

            if (totalWeight <= 0f) return items[0];

            double target = random.NextDouble() * totalWeight;
            float currentSum = 0f;

            for (int i = 0; i < items.Count; i++)
            {
                currentSum += weightFunc(items[i]);
                if (target <= currentSum)
                {
                    return items[i];
                }
            }

            return items[items.Count - 1];
        }
    }
}
