using System;

namespace DungeonSystem.Generation
{
    public class SeedManager
    {
        public int ActiveSeed { get; private set; }
        public System.Random RNG { get; private set; }

        public void Initialize(int configSeed, bool useRandomSeed)
        {
            ActiveSeed = useRandomSeed ? UnityEngine.Random.Range(0, 999999) : configSeed;
            RNG = new System.Random(ActiveSeed);
        }
    }
}
