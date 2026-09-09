using UnityEngine;
using SpaceshipDivine.Haptics;

/// <summary>
/// Static entry point for all call sites. Chooses the platform backend once and survives
/// scene loads. Every method is safe to call before initialisation.
///
/// This lives in Assembly-CSharp rather than the SpaceshipDivine.Haptics assembly because
/// game code calls it from everywhere; the assembly itself stays free of game types so it
/// remains unit-testable without a scene.
/// </summary>
public static class Haptics
{
    private static HapticService service;

    private static HapticService Service
    {
        get
        {
            if (service == null)
            {
                IHapticBackend backend;
#if UNITY_ANDROID && !UNITY_EDITOR
                backend = new AndroidHapticBackend();
#elif UNITY_IOS && !UNITY_EDITOR
                backend = new IosHapticBackend();
#else
                backend = null;   // editor and desktop: no-op
#endif
                service = new HapticService(backend, () => Time.unscaledTime);
            }
            return service;
        }
    }

    public static bool Muted
    {
        get => Service.Muted;
        set => Service.Muted = value;
    }

    public static bool Play(Haptic haptic) => Service.Play(haptic);
}
