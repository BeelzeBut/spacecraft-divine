using UnityEngine;

namespace SpaceshipDivine.Haptics
{
    /// <summary>
    /// Maps the semantic vocabulary onto Android's VibrationEffect. Predefined effects
    /// (API 29+) feel native; below that we fall back to short one-shots (API 26+), and below
    /// THAT to the deprecated vibrate(long). Every call is guarded — a device with no vibrator,
    /// or a manufacturer ROM that misbehaves, must degrade to silence, never to an exception.
    /// </summary>
    public class AndroidHapticBackend : IHapticBackend
    {
        private const int EffectTick = 2;         // VibrationEffect.EFFECT_TICK
        private const int EffectClick = 0;        // VibrationEffect.EFFECT_CLICK
        private const int EffectHeavyClick = 5;   // VibrationEffect.EFFECT_HEAVY_CLICK
        private const int EffectDoubleClick = 1;  // VibrationEffect.EFFECT_DOUBLE_CLICK

        private readonly AndroidJavaObject vibrator;
        private readonly int apiLevel;
        private bool hasVibrator;

        public AndroidHapticBackend()
        {
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    apiLevel = version.GetStatic<int>("SDK_INT");

                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                hasVibrator = vibrator != null && vibrator.Call<bool>("hasVibrator");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Haptics unavailable on this device: " + e.Message);
                hasVibrator = false;
            }
        }

        public bool IsAvailable => hasVibrator;

        /// <summary>
        /// Set the first time a tier throws, so the device stays permanently demoted instead of
        /// re-entering a call that its ROM cannot service. Without these, a throwing tier would
        /// also log on every pulse — up to 20 lines a second at the rate limiter's ceiling.
        /// </summary>
        private bool predefinedBroken;
        private bool oneShotBroken;

        public void Play(Haptic haptic)
        {
            if (!hasVibrator) return;

            // Each tier gets its own try. One try around all three would let an exception in
            // the top tier abort Play entirely rather than fall through — which is the whole
            // point of having tiers. A ROM that throws IllegalArgumentException from
            // createPredefined would otherwise lose every menu tap for the session, even
            // though createOneShot works fine on the same device.
            if (apiLevel >= 29 && !predefinedBroken)
            {
                try
                {
                    if (TryPredefined(haptic)) return;
                }
                catch (System.Exception e)
                {
                    predefinedBroken = true;
                    Debug.LogWarning("Predefined haptics failed, falling back to one-shot: " + e.Message);
                }
            }

            if (apiLevel >= 26 && !oneShotBroken)
            {
                try
                {
                    OneShot(DurationFor(haptic));
                    return;
                }
                catch (System.Exception e)
                {
                    oneShotBroken = true;
                    Debug.LogWarning("One-shot haptics failed, falling back to legacy vibrate: " + e.Message);
                }
            }

            try
            {
                vibrator.Call("vibrate", (long)DurationFor(haptic));
            }
            catch (System.Exception e)
            {
                // Bottom tier: nothing left to fall back to, so go quiet for good.
                hasVibrator = false;
                Debug.LogWarning("Haptics disabled for this session: " + e.Message);
            }
        }

        private bool TryPredefined(Haptic haptic)
        {
            int effect;
            switch (haptic)
            {
                case Haptic.Selection:   effect = EffectTick;        break;
                case Haptic.Confirm:     effect = EffectClick;       break;
                case Haptic.Reject:      effect = EffectDoubleClick; break;
                case Haptic.ImpactLight: effect = EffectTick;        break;
                case Haptic.ImpactHeavy: effect = EffectHeavyClick;  break;
                case Haptic.Ability:     effect = EffectClick;       break;
                case Haptic.Reward:      effect = EffectDoubleClick; break;
                default: return false;   // Death and BossRumble need a longer one-shot
            }

            using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
            using (var vibrationEffect = effectClass.CallStatic<AndroidJavaObject>("createPredefined", effect))
                vibrator.Call("vibrate", vibrationEffect);
            return true;
        }

        private void OneShot(int milliseconds)
        {
            using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
            using (var effect = effectClass.CallStatic<AndroidJavaObject>(
                       "createOneShot", (long)milliseconds, -1 /* DEFAULT_AMPLITUDE */))
                vibrator.Call("vibrate", effect);
        }

        private static int DurationFor(Haptic haptic)
        {
            switch (haptic)
            {
                case Haptic.Selection:   return 10;
                case Haptic.ImpactLight: return 10;
                case Haptic.Confirm:     return 20;
                case Haptic.Ability:     return 20;
                case Haptic.Reject:      return 40;
                case Haptic.Reward:      return 40;
                case Haptic.ImpactHeavy: return 60;
                case Haptic.Death:       return 200;
                case Haptic.BossRumble:  return 300;
                default:                 return 20;
            }
        }
    }
}
