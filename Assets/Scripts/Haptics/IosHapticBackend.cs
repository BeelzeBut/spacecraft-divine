using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceshipDivine.Haptics
{
    /// <summary>
    /// Thin bridge to Assets/Plugins/iOS/SdHaptics.mm.
    ///
    /// The Haptic enum ordinal is passed straight through as an int, so the ORDER of that enum is
    /// part of this contract — see the append-only warning on Haptic itself. Reordering it silently
    /// remaps every effect; add new values at the end and update the .mm switch in the same commit.
    ///
    /// Every native call is guarded. A device with no Taptic Engine, or a simulator, must degrade
    /// to silence rather than throw.
    /// </summary>
    public class IosHapticBackend : IHapticBackend
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern bool _SdHapticsAvailable();
        [DllImport("__Internal")] private static extern void _SdHapticsPrepare();
        [DllImport("__Internal")] private static extern void _SdHapticsPlay(int kind);
#endif

        private readonly bool available;

        public IosHapticBackend()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                available = _SdHapticsAvailable();
                if (available) _SdHapticsPrepare();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("iOS haptics unavailable: " + e.Message);
                available = false;
            }
#else
            available = false;
#endif
        }

        public bool IsAvailable => available;

        public void Play(Haptic haptic)
        {
            if (!available) return;
#if UNITY_IOS && !UNITY_EDITOR
            try { _SdHapticsPlay((int)haptic); }
            catch (System.Exception e) { Debug.LogWarning("iOS haptic failed: " + e.Message); }
#endif
        }
    }
}
