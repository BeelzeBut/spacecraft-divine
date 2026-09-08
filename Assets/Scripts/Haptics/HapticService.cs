using System;

namespace SpaceshipDivine.Haptics
{
    /// <summary>
    /// The semantic layer. Owns muting and rate limiting; knows nothing about platforms.
    /// The clock is injected so the rate limiter is testable without waiting in real time.
    /// </summary>
    public class HapticService
    {
        /// <summary>Minimum gap between ordinary pulses. Combat would otherwise be one long buzz.</summary>
        public const float MinIntervalSeconds = 0.05f;

        private readonly IHapticBackend backend;
        private readonly Func<float> nowSeconds;
        private float lastPlayedAt = float.NegativeInfinity;

        public bool Muted { get; set; }
        public int PlayedCount { get; private set; }

        public HapticService(IHapticBackend backend, Func<float> nowSeconds)
        {
            this.backend = backend;
            this.nowSeconds = nowSeconds ?? (() => 0f);
        }

        /// <summary>
        /// Death and BossRumble are rare and structurally important — dropping one reads as a
        /// bug to the player, so they bypass the rate limit.
        /// </summary>
        private static bool BypassesRateLimit(Haptic haptic)
        {
            return haptic == Haptic.Death || haptic == Haptic.BossRumble;
        }

        /// <returns>true if the pulse actually reached the backend.</returns>
        public bool Play(Haptic haptic)
        {
            if (Muted) return false;
            if (backend == null || !backend.IsAvailable) return false;

            float now = nowSeconds();
            if (!BypassesRateLimit(haptic) && now - lastPlayedAt < MinIntervalSeconds)
                return false;

            backend.Play(haptic);
            lastPlayedAt = now;
            PlayedCount++;
            return true;
        }
    }
}
