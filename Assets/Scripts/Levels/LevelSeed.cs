using System;

namespace SpaceshipDivine.Levels
{
    /// <summary>
    /// Carries the seed for the NEXT level to be generated. Set it before loading a level
    /// scene; LevelGenerator consumes it, so an ordinary run after a daily challenge is
    /// random again.
    ///
    /// This lives in its own assembly with no references, so it can be unit tested without a
    /// scene — same arrangement as SpaceshipDivine.Save.
    /// </summary>
    public static class LevelSeed
    {
        public static int? Pending { get; private set; }

        public static void Set(int seed) => Pending = seed;
        public static void Clear() => Pending = null;

        /// <summary>
        /// Takes the pending seed and clears it, or invents one when nothing is pending.
        ///
        /// Clearing here rather than after generation is not a detail. LevelGenerator.Start
        /// retries by calling itself when a layout is rejected as too small; if the seed were
        /// still pending on re-entry, every retry would reseed to the same value, reject the
        /// same layout, and recurse until the stack gave out.
        /// </summary>
        public static int Consume()
        {
            int seed = Pending ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            Pending = null;
            return seed;
        }

        /// <summary>
        /// Offline fallback only. The authoritative daily seed comes from the server; this
        /// exists so a player with no connection still gets a coherent, self-consistent
        /// challenge rather than a broken screen. Their score simply is not submitted.
        ///
        /// The formula is FROZEN. Changing it changes the dungeon of every past daily
        /// challenge. LevelSeedTests pins one value against exactly that.
        /// </summary>
        public static int ForDate(DateTime utcDate)
        {
            int days = (int)(utcDate.Date - new DateTime(2020, 1, 1)).TotalDays;
            unchecked
            {
                int h = 17;
                h = h * 31 + days;
                h = h * 31 + 0x5F3759DF;
                h ^= h >> 13;
                h *= 0x27D4EB2D;
                h ^= h >> 15;
                return h & 0x7FFFFFFF;   // keep it non-negative
            }
        }
    }
}
