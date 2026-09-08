# Haptics & UI Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every button and every significant gameplay moment a haptic response appropriate to that action, without editing ~220 scene bindings — and fix the ~216 currently-silent buttons as a side effect.

**Architecture:** A `HapticService` singleton exposes a semantic vocabulary (`Haptics.Play(Haptic.Selection)`) over two thin native backends — `UIImpactFeedbackGenerator` / `UINotificationFeedbackGenerator` on iOS, `VibrationEffect` predefined effects on Android — and a no-op in the editor. A `UIFeedbackInstaller` walks every `Selectable` on scene load and attaches feedback at runtime, so no scene file is edited. A global rate limiter keeps combat from degrading into continuous buzz.

**Tech Stack:** Unity 2022.3.14f1, C#, Objective-C++ (`.mm`) iOS plugin, `AndroidJavaObject` JNI on Android, uGUI `Selectable`.

**Spec:** `docs/superpowers/specs/2026-09-08-production-release-design.md` §4.6

## Global Constraints

- **Branch:** `production-release`. **Never push to any remote** until after the diploma defense on 11 Sep 2026. Local commits only.
- **No scene or prefab file may be edited to add feedback.** Buttons bind `onClick` directly in the scenes — 122 entries in `Main Menu.unity`, roughly 220 across the game. Editing them is not an acceptable approach; the installer exists precisely to avoid it.
- **Haptics must be user-disableable**, persisted alongside the existing music and sound mutes. `PlayerProfile.SettingsData` already has an `isHapticsMuted` field, added in the save plan's Task 3.
- **The OS-level haptic setting is respected.** If the device has haptics off, the service is inert.
- **Rate-limited globally.** A bullet-hell game firing a pulse per bullet hit drains battery and feels like mush. Minimum interval between pulses plus a per-frame cap.
- **Nothing may throw on a device without a vibrator.** Every native call is wrapped; failure degrades to silence, never to an exception.
- Unity editor path: `/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity`. Only one batchmode process at a time; check `pgrep -fl "Unity.app/Contents/MacOS/Unity"` first.
- Run the suite with `./run-tests.sh`. **Never modify it.**

## Environment facts established before planning

| Fact | Consequence |
|---|---|
| `Assets/Plugins/` does not exist | Both native plugins and the Android manifest are created from scratch in Task 2 and 3 |
| No `AndroidManifest.xml` in the project | Unity uses its default; a custom one is needed for `android.permission.VIBRATE` |
| `SoundManager` has `UISounds` (a `List<AudioClip>`), `soundSource`, `musicSource` | The installer plays `UISounds[0]` — the same clip the four existing call sites use |
| `UISounds[...]` is referenced in only 4 places, all in `MainMenu` ship unlock/upgrade | ~216 buttons are currently silent; the installer fixes this |
| `PlayerController.TakeDamage(float)` at line 447 | Gameplay hook for `ImpactHeavy` |
| `Collectible.UnlockCollectible()` is abstract, overridden by `Blueprint` and others | Single hook for `Reward` |
| `Enemy.cs:384` sets `GameManager.shouldDropBossBlueprint` on boss death | Hook for `BossRumble` |
| `OptionsMenu.cs` is minimal (quality + back button) | The haptics toggle is added to `MainMenu`, which already owns `MusicOnOff` / `SoundOnOff` |

---

### Task 1: HapticService core with a testable backend seam

Built test-first against a fake backend, so the semantic layer, the mute logic and the rate limiter are all verified without a device.

**Files:**
- Create: `Assets/Scripts/Haptics/SpaceshipDivine.Haptics.asmdef`
- Create: `Assets/Scripts/Haptics/Haptic.cs`
- Create: `Assets/Scripts/Haptics/IHapticBackend.cs`
- Create: `Assets/Scripts/Haptics/HapticService.cs`
- Create: `Assets/Tests/EditMode/HapticServiceTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `enum Haptic { Selection, Confirm, Reject, ImpactLight, ImpactHeavy, Ability, Reward, Death, BossRumble }`
  - `interface IHapticBackend { bool IsAvailable { get; } void Play(Haptic haptic); }`
  - `HapticService(IHapticBackend backend, Func<float> nowSeconds)`
  - `HapticService.Muted { get; set; }`
  - `HapticService.MinIntervalSeconds` — default `0.05f`
  - `HapticService.Play(Haptic)` → `bool` (true if it actually reached the backend)
  - `HapticService.PlayedCount` — for tests

The assembly is separate and `autoReferenced: true`, so `Assembly-CSharp` can call it while the core stays unit-testable.

- [ ] **Step 1: Create the assembly definition**

`Assets/Scripts/Haptics/SpaceshipDivine.Haptics.asmdef`:

```json
{
    "name": "SpaceshipDivine.Haptics",
    "rootNamespace": "SpaceshipDivine.Haptics",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the failing tests**

`Assets/Tests/EditMode/HapticServiceTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using SpaceshipDivine.Haptics;

namespace SpaceshipDivine.Haptics.Tests
{
    /// <summary>Records what would have reached the device.</summary>
    public class FakeBackend : IHapticBackend
    {
        public bool Available = true;
        public readonly List<Haptic> Played = new List<Haptic>();

        public bool IsAvailable => Available;
        public void Play(Haptic haptic) => Played.Add(haptic);
    }

    public class HapticServiceTests
    {
        private FakeBackend backend;
        private float now;
        private HapticService service;

        [SetUp]
        public void SetUp()
        {
            backend = new FakeBackend();
            now = 0f;
            service = new HapticService(backend, () => now);
        }

        [Test]
        public void PlayReachesTheBackend()
        {
            Assert.IsTrue(service.Play(Haptic.Selection));
            Assert.AreEqual(1, backend.Played.Count);
            Assert.AreEqual(Haptic.Selection, backend.Played[0]);
        }

        [Test]
        public void MutedServicePlaysNothing()
        {
            service.Muted = true;
            Assert.IsFalse(service.Play(Haptic.Confirm));
            Assert.AreEqual(0, backend.Played.Count);
        }

        [Test]
        public void UnavailableBackendIsNeverCalled()
        {
            backend.Available = false;
            Assert.IsFalse(service.Play(Haptic.Confirm));
            Assert.AreEqual(0, backend.Played.Count);
        }

        [Test]
        public void RapidRepeatsAreRateLimited()
        {
            Assert.IsTrue(service.Play(Haptic.ImpactLight));
            Assert.IsFalse(service.Play(Haptic.ImpactLight), "second pulse in the same instant");
            Assert.IsFalse(service.Play(Haptic.ImpactLight));
            Assert.AreEqual(1, backend.Played.Count);
        }

        [Test]
        public void PulsesResumeOnceTheIntervalHasPassed()
        {
            service.Play(Haptic.ImpactLight);
            now += HapticService.MinIntervalSeconds + 0.001f;
            Assert.IsTrue(service.Play(Haptic.ImpactLight));
            Assert.AreEqual(2, backend.Played.Count);
        }

        [Test]
        public void HighPriorityHapticsBypassTheRateLimit()
        {
            // Death and BossRumble are rare and structurally important; a dropped Death pulse
            // reads as a bug to the player.
            service.Play(Haptic.ImpactLight);
            Assert.IsTrue(service.Play(Haptic.Death));
            Assert.IsTrue(service.Play(Haptic.BossRumble));
            Assert.AreEqual(3, backend.Played.Count);
        }

        [Test]
        public void PlayedCountTracksOnlyDeliveredPulses()
        {
            service.Play(Haptic.Selection);
            service.Play(Haptic.Selection);   // rate limited
            service.Muted = true;
            service.Play(Haptic.Selection);
            Assert.AreEqual(1, service.PlayedCount);
        }

        [Test]
        public void NullBackendIsToleratedRatherThanThrowing()
        {
            var s = new HapticService(null, () => 0f);
            Assert.DoesNotThrow(() => s.Play(Haptic.Selection));
            Assert.IsFalse(s.Play(Haptic.Selection));
        }
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `./run-tests.sh`
Expected: FAIL — compile error, `HapticService` does not exist.

- [ ] **Step 4: Write the vocabulary and the backend seam**

`Assets/Scripts/Haptics/Haptic.cs`:

```csharp
namespace SpaceshipDivine.Haptics
{
    /// <summary>
    /// What a haptic MEANS, not how long it buzzes. Call sites choose meaning; the platform
    /// backend chooses the physical effect. Adding a duration parameter here would be a
    /// mistake — it puts platform detail at every call site.
    /// </summary>
    public enum Haptic
    {
        Selection,    // menu nav, tab change, toggle, slider notch
        Confirm,      // start run, purchase success, ship unlock
        Reject,       // insufficient gems, tapping a locked ship
        ImpactLight,  // player bullet hits an enemy — rate limited, default off
        ImpactHeavy,  // player takes damage
        Ability,      // ability cast
        Reward,       // chest open, upgrade pickup, level clear
        Death,        // player death
        BossRumble    // boss spawn and boss defeat
    }
}
```

`Assets/Scripts/Haptics/IHapticBackend.cs`:

```csharp
namespace SpaceshipDivine.Haptics
{
    public interface IHapticBackend
    {
        bool IsAvailable { get; }
        void Play(Haptic haptic);
    }
}
```

- [ ] **Step 5: Write the service**

`Assets/Scripts/Haptics/HapticService.cs`:

```csharp
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
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `./run-tests.sh`
Expected: 8 new tests passing. Report the actual total.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Haptics Assets/Tests
git commit -m "Haptica: serviciu semantic cu limitare de rata si comutator de dezactivare"
```

---

### Task 2: Android backend

**Files:**
- Create: `Assets/Plugins/Android/AndroidManifest.xml`
- Create: `Assets/Scripts/Haptics/AndroidHapticBackend.cs`

**Interfaces:**
- Consumes: `IHapticBackend`, `Haptic` (Task 1).
- Produces: `AndroidHapticBackend : IHapticBackend`.

`VibrationEffect.createPredefined` needs API 29; `createOneShot` needs API 26. `AndroidMinSdkVersion` is 22, so both paths must degrade rather than throw.

- [ ] **Step 1: Add the VIBRATE permission**

`Assets/Plugins/Android/AndroidManifest.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <uses-permission android:name="android.permission.VIBRATE" />
    <application android:extractNativeLibs="false">
        <activity android:name="com.unity3d.player.UnityPlayerActivity"
                  android:theme="@style/UnityThemeSelector"
                  android:exported="true">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
            <meta-data android:name="unityplayer.UnityActivity" android:value="true" />
        </activity>
    </application>
</manifest>
```

- [ ] **Step 2: Write the backend**

`Assets/Scripts/Haptics/AndroidHapticBackend.cs`:

```csharp
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
        private readonly bool hasVibrator;

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

        public void Play(Haptic haptic)
        {
            if (!hasVibrator) return;

            try
            {
                if (apiLevel >= 29 && TryPredefined(haptic)) return;
                if (apiLevel >= 26) { OneShot(DurationFor(haptic)); return; }
                vibrator.Call("vibrate", (long)DurationFor(haptic));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Haptic play failed: " + e.Message);
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
                default: return false;   // Death and BossRumble use waveforms below
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
```

- [ ] **Step 3: Confirm the project still compiles and the suite is unchanged**

Run: `./run-tests.sh`
Expected: same total as after Task 1, all passing. `AndroidHapticBackend` is not unit-tested — it is a thin JNI adapter, verified on device in Task 6.

- [ ] **Step 4: Commit**

```bash
git add Assets/Plugins Assets/Scripts/Haptics
git commit -m "Haptica: implementare Android prin VibrationEffect, cu degradare pe API vechi"
```

---

### Task 3: iOS backend

**Files:**
- Create: `Assets/Plugins/iOS/SdHaptics.mm`
- Create: `Assets/Scripts/Haptics/IosHapticBackend.cs`

**Interfaces:**
- Consumes: `IHapticBackend`, `Haptic` (Task 1).
- Produces: `IosHapticBackend : IHapticBackend`.

- [ ] **Step 1: Write the native plugin**

`Assets/Plugins/iOS/SdHaptics.mm`:

```objectivec
#import <UIKit/UIKit.h>

// Generators are cached and prepared: creating one per call adds latency, and UIKit warns
// that an unprepared generator may drop the first pulse.
static UIImpactFeedbackGenerator *lightGen  = nil;
static UIImpactFeedbackGenerator *mediumGen = nil;
static UIImpactFeedbackGenerator *heavyGen  = nil;
static UINotificationFeedbackGenerator *noticeGen = nil;
static UISelectionFeedbackGenerator *selectGen = nil;

extern "C" {

bool _SdHapticsAvailable() {
    // Haptics need an iPhone 7 or newer. UIFeedbackGenerator exists from iOS 10.
    if (@available(iOS 10.0, *)) { return true; }
    return false;
}

void _SdHapticsPrepare() {
    if (@available(iOS 10.0, *)) {
        if (lightGen  == nil) lightGen  = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        if (mediumGen == nil) mediumGen = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
        if (heavyGen  == nil) heavyGen  = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
        if (noticeGen == nil) noticeGen = [[UINotificationFeedbackGenerator alloc] init];
        if (selectGen == nil) selectGen = [[UISelectionFeedbackGenerator alloc] init];
        [lightGen prepare]; [mediumGen prepare]; [heavyGen prepare];
        [noticeGen prepare]; [selectGen prepare];
    }
}

// kind mirrors the C# Haptic enum ordinal.
void _SdHapticsPlay(int kind) {
    if (@available(iOS 10.0, *)) {
        _SdHapticsPrepare();
        switch (kind) {
            case 0: [selectGen selectionChanged]; break;                                        // Selection
            case 1: [noticeGen notificationOccurred:UINotificationFeedbackTypeSuccess]; break;  // Confirm
            case 2: [noticeGen notificationOccurred:UINotificationFeedbackTypeError];   break;  // Reject
            case 3: [lightGen impactOccurred];  break;                                          // ImpactLight
            case 4: [heavyGen impactOccurred];  break;                                          // ImpactHeavy
            case 5: [mediumGen impactOccurred]; break;                                          // Ability
            case 6: [noticeGen notificationOccurred:UINotificationFeedbackTypeSuccess]; break;  // Reward
            case 7: [noticeGen notificationOccurred:UINotificationFeedbackTypeError];   break;  // Death
            case 8: [heavyGen impactOccurred];  break;                                          // BossRumble
            default: break;
        }
    }
}

}
```

- [ ] **Step 2: Write the managed wrapper**

`Assets/Scripts/Haptics/IosHapticBackend.cs`:

```csharp
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpaceshipDivine.Haptics
{
    /// <summary>
    /// Thin bridge to SdHaptics.mm. The enum ordinal is passed straight through, so the order
    /// of the Haptic enum is part of this contract — reordering it silently remaps every
    /// effect. Add new values at the END only.
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
```

- [ ] **Step 3: Confirm compilation**

Run: `./run-tests.sh`
Expected: unchanged total, all passing.

- [ ] **Step 4: Commit**

```bash
git add Assets/Plugins/iOS Assets/Scripts/Haptics
git commit -m "Haptica: implementare iOS prin UIFeedbackGenerator"
```

---

### Task 4: Runtime installer — feedback on every button without touching a scene

This is the task that avoids editing ~220 onClick bindings, and it fixes the ~216 silent buttons.

**Files:**
- Create: `Assets/Scripts/Haptics/Haptics.cs` (the MonoBehaviour facade + backend selection)
- Create: `Assets/Scripts/Haptics/UIFeedback.cs`
- Create: `Assets/Scripts/Haptics/UIFeedbackInstaller.cs`

**Interfaces:**
- Consumes: `HapticService`, `IHapticBackend`, `Haptic` (Tasks 1-3).
- Produces:
  - `Haptics.Play(Haptic)` — static, safe to call before initialisation
  - `Haptics.Muted { get; set; }`
  - `UIFeedback` — per-`Selectable` component with an optional `Haptic` override
  - `UIFeedbackInstaller` — walks each loaded scene

These live in `Assembly-CSharp`, not the Haptics assembly, because they touch uGUI and `SoundManager`.

- [ ] **Step 1: Write the facade**

`Assets/Scripts/Haptics/Haptics.cs`:

```csharp
using UnityEngine;
using SpaceshipDivine.Haptics;

/// <summary>
/// Static entry point for all call sites. Chooses the platform backend once and survives
/// scene loads. Every method is safe to call before initialisation.
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
```

- [ ] **Step 2: Write the per-button component**

`Assets/Scripts/Haptics/UIFeedback.cs`:

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using SpaceshipDivine.Haptics;

/// <summary>
/// Attached at runtime by UIFeedbackInstaller — never authored into a scene. Fires on pointer
/// down rather than on click, because that is when the press is felt.
/// </summary>
public class UIFeedback : MonoBehaviour, IPointerDownHandler
{
    /// <summary>Overridden by the installer for buttons that mean something other than navigation.</summary>
    public Haptic haptic = Haptic.Selection;

    /// <summary>Set false for buttons that already play their own sound, to avoid doubling.</summary>
    public bool playSound = true;

    public void OnPointerDown(PointerEventData eventData)
    {
        Haptics.Play(haptic);

        if (!playSound) return;
        SoundManager sound = SoundManager.instance;
        if (sound == null || sound.soundSource == null) return;
        if (sound.UISounds == null || sound.UISounds.Count == 0) return;
        sound.soundSource.PlayOneShot(sound.UISounds[0]);
    }
}
```

- [ ] **Step 3: Write the installer**

`Assets/Scripts/Haptics/UIFeedbackInstaller.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SpaceshipDivine.Haptics;

/// <summary>
/// Walks every Selectable in each loaded scene and attaches UIFeedback. This exists because
/// buttons bind onClick directly in the scene files — 122 entries in Main Menu alone, ~220
/// across the game — and editing those bindings to add feedback is not maintainable.
///
/// Name-based overrides are deliberately coarse: a button whose name contains one of these
/// fragments gets a different haptic. Anything unmatched gets Selection, which is correct for
/// the overwhelming majority (navigation).
/// </summary>
public class UIFeedbackInstaller : MonoBehaviour
{
    private static readonly (string fragment, Haptic haptic)[] Overrides =
    {
        ("start",    Haptic.Confirm),
        ("play",     Haptic.Confirm),
        ("unlock",   Haptic.Confirm),
        ("upgrade",  Haptic.Confirm),
        ("buy",      Haptic.Confirm),
        ("purchase", Haptic.Confirm),
        ("revive",   Haptic.Confirm),
        ("locked",   Haptic.Reject),
        ("back",     Haptic.Selection),
        ("close",    Haptic.Selection),
    };

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Install(gameObject.scene);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);

    private static void Install(Scene scene)
    {
        if (!scene.IsValid()) return;

        int attached = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                if (selectable.GetComponent<UIFeedback>() != null) continue;   // idempotent

                var feedback = selectable.gameObject.AddComponent<UIFeedback>();
                feedback.haptic = HapticFor(selectable.gameObject.name);
                attached++;
            }
        }

        if (attached > 0)
            Debug.Log("UIFeedbackInstaller: attached to " + attached + " selectables in " + scene.name);
    }

    private static Haptic HapticFor(string objectName)
    {
        string lower = objectName.ToLowerInvariant();
        foreach ((string fragment, Haptic haptic) in Overrides)
            if (lower.Contains(fragment))
                return haptic;
        return Haptic.Selection;
    }
}
```

- [ ] **Step 4: Place the installer in the first scene**

The installer must exist from the first loaded scene and survive scene changes. Add it headlessly rather than by hand-editing the scene YAML — extend `Assets/Editor/SdAutomation.cs` (created in the save plan's Task 7) with:

```csharp
    // ---- Haptics ------------------------------------------------------------------

    public static void InstallUIFeedbackBootstrap()
    {
        var problems = new System.Collections.Generic.List<string>();
        const string scenePath = "Assets/Scenes/Main Menu.unity";

        UnityEngine.SceneManagement.Scene scene =
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

        bool exists = false;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponent<UIFeedbackInstaller>() != null) exists = true;

        if (!exists)
        {
            var go = new GameObject("UI Feedback Installer");
            go.AddComponent<UIFeedbackInstaller>();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("Added UI Feedback Installer to " + scenePath);
        }
        else Debug.Log("UI Feedback Installer already present in " + scenePath);

        Finish("InstallUIFeedbackBootstrap", problems);
    }
```

Run it:

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
pgrep -fl "Unity.app/Contents/MacOS/Unity" && echo "KILL THE EDITOR FIRST" && exit 1
"/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$(pwd)" \
  -executeMethod SdAutomation.InstallUIFeedbackBootstrap -logFile - 2>&1 | tail -20
echo "EXIT=$?"
```

- [ ] **Step 5: Confirm the suite still passes**

Run: `./run-tests.sh`

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Haptics Assets/Editor/SdAutomation.cs "Assets/Scenes/Main Menu.unity"
git commit -m "Haptica: instalator in timp de rulare pentru feedback pe toate butoanele"
```

---

### Task 5: Gameplay hooks and the settings toggle

**Files:**
- Modify: `Assets/Scripts/Object Managers/PlayerController.cs` (line ~447, `TakeDamage`)
- Modify: `Assets/Scripts/Object Managers/GameManager.cs` (death, revive)
- Modify: `Assets/Scripts/Collectibles/Collectible.cs` or `Blueprint.cs` (pickup)
- Modify: `Assets/Scripts/Enemies/Enemy.cs` (line ~384, boss death)
- Modify: `Assets/Scripts/Main Menu/MainMenu.cs` (settings toggle, alongside `MusicOnOff`/`SoundOnOff`)

**Interfaces:**
- Consumes: `Haptics.Play`, `Haptics.Muted` (Task 4); `PlayerProfile.SettingsData.isHapticsMuted` (save plan Task 3).

- [ ] **Step 1: Add the gameplay hooks**

One line at each site. In `PlayerController.TakeDamage(float damage)`, at the point damage is actually applied (inside the `canTakeDamage` branch, not before it):

```csharp
            Haptics.Play(Haptic.ImpactHeavy);
```

In `Enemy.cs` where `GameManager.instance.shouldDropBossBlueprint = true` is set (boss defeated):

```csharp
                        Haptics.Play(Haptic.BossRumble);
```

In `GameManager.GoToRespawnMenuC()`, immediately before `respawnMenu.SetActive(true)`:

```csharp
            Haptics.Play(Haptic.Death);
```

In `Blueprint.UnlockCollectible()`, before `gameObject.SetActive(false)`:

```csharp
        Haptics.Play(Haptic.Reward);
```

**Do not** add `ImpactLight` on bullet hits in this task. It is default-off per the spec and needs the device tuning pass in Task 6 before it is wired at all.

- [ ] **Step 2: Add the settings toggle**

In `MainMenu.cs`, next to `MusicOnOff()` and `SoundOnOff()`, following the same shape:

```csharp
    public Image hapticsImage;
    public Sprite hapticsSprite, hapticsMuteSprite;

    public void HapticsOnOff()
    {
        bool nowMuted = !Haptics.Muted;
        Haptics.Muted = nowMuted;
        data.dataSaved.isHapticsMuted = nowMuted;
        hapticsImage.sprite = nowMuted ? hapticsMuteSprite : hapticsSprite;
        if (!nowMuted) Haptics.Play(Haptic.Selection);   // confirm it works when re-enabled
        data.Save();
    }
```

`SaveData` needs a matching `public bool isHapticsMuted;` field, and `DataHolder` must apply it on load:

```csharp
        Haptics.Muted = dataSaved.isHapticsMuted;
```

- [ ] **Step 3: Confirm the suite still passes**

Run: `./run-tests.sh`

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts
git commit -m "Haptica: legaturi in joc si comutator in meniul de optiuni"
```

---

### Task 6: On-device verification

**This task cannot be completed by an agent alone.** An agent can prove the code path fires; whether a pulse *feels* right is a human judgement, and iOS simulators have no haptics at all.

**Files:**
- Create: `docs/superpowers/notes/haptics-device-verification.md`

- [ ] **Step 1: Build and install on a physical Android device**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
make android-apk          # from the release-automation plan
adb devices               # confirm a device is attached
adb install -r "Build Android/SpaceshipDivine.apk"
```

- [ ] **Step 2: Prove the code path fires**

```bash
adb logcat -c
adb logcat -s Unity:V | grep -iE "UIFeedbackInstaller|Haptic"
```

Expected: an `attached to N selectables` line on scene load. If N is far below ~120 for the main menu, the installer is not finding the buttons — investigate before tuning anything.

- [ ] **Step 3: Human tuning pass**

Record answers in the notes file:

- Does `Selection` on menu navigation feel crisp, or mushy and late?
- Is `Confirm` distinguishable from `Selection` by feel alone?
- Does `Reject` read as "no" rather than as a second confirm?
- Is `ImpactHeavy` on taking damage informative, or annoying during a boss fight?
- Does `Death` land at the right moment relative to the animation?
- Is `MinIntervalSeconds = 0.05f` right, or does combat still feel buzzy?
- Should `ImpactLight` on bullet hits be enabled at all?

- [ ] **Step 4: Apply the tuning and re-verify**

Adjust `MinIntervalSeconds`, the `DurationFor` table and the iOS effect mapping from the answers. Rebuild, reinstall, confirm the feel.

- [ ] **Step 5: iOS device check**

TestFlight build to a physical iPhone 7 or newer. Simulators cannot verify this.

- [ ] **Step 6: Commit the notes**

```bash
git add docs/superpowers/notes/haptics-device-verification.md
git commit -m "Nota: verificarea hapticii pe dispozitiv fizic"
```

---

## Out of scope for this plan

| Deferred | Why |
|---|---|
| Core Haptics amplitude envelopes on iOS | Preset effects are sufficient for a 2D shooter; revisit only if the tuning pass says otherwise |
| `ImpactLight` on bullet hits | Default off per spec; enabled only if Task 6's tuning pass concludes it improves feel |
| Controller rumble | Different subsystem (`Gamepad.SetMotorSpeeds`); no design decision has been made |
| Per-button haptic authoring in the Inspector | The name-fragment overrides cover the meaningful cases; revisit if they prove too coarse |
