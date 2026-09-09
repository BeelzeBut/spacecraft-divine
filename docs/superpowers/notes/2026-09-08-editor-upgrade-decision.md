# Editor upgrade decision — Unity 2022.3.14f1 vs Google Play

**Decided:** 9 Sep 2026
**Question:** can 2022.3.14f1 satisfy Play's current requirements without an editor upgrade?
**Answer: no. The upgrade is mandatory, and it is a patch upgrade, not a migration.**

The plan's Step 3 probe (`Assets/Editor/SdProbe.cs`) was never written. It asked whether
`(AndroidSdkVersions)36` can be assigned by casting past an enum that does not name it. That
question turned out not to matter — see "Why the probe is moot" below — so the throwaway code
was not created and there is nothing to delete.

---

## What Play requires today

| Requirement | Value | Since |
|---|---|---|
| Target API level, new apps **and updates** | **36** (Android 16) | 31 Aug 2026 — *nine days ago* |
| Target API level, existing apps to stay visible to new users | 35 | 31 Aug 2026 |
| 16 KB memory page support, for anything targeting API 35+ | required | 1 Nov 2025 |

Extensions to 1 Nov 2026 could be requested through Play Console.

Sources, checked 9 Sep 2026:
- https://developer.android.com/google/play/requirements/target-sdk
- https://android-developers.googleblog.com/2025/05/prepare-play-apps-for-devices-with-16kb-page-size.html
- https://developer.android.com/guide/practices/page-sizes

**The listing is unpublished**, so the first upload is a new-app submission and lands on the
strictest row: target 36.

## What this editor actually produces

Measured, not recalled — from a real Android build made on 9 Sep 2026
(`Build Android/SpaceshipDivine-haptics.apk`, `BuildAndroid.Build`, result Succeeded):

```
aapt2 dump badging  →  targetSdkVersion:'32'      (compileSdkVersion 32)
llvm-readelf -l lib/arm64-v8a/libunity.so  →  LOAD align = 0x1000  ×4
```

Same 0x1000 for `libil2cpp.so`, `libmain.so` and `lib_burst_generated.so`, on both
`arm64-v8a` and `armeabi-v7a`. 16 KB compatibility needs `0x4000`.

`ProjectSettings.asset` has `AndroidTargetSdkVersion: 0` — "Automatic (highest installed)".
Automatic resolves to **32** here, so the gap to 36 is four levels, and it is the editor that
caps it, not the SDK: `~/Library/Android/sdk/platforms` has android-36 installed already.

## Why the probe is moot

Even if `(AndroidSdkVersions)36` could be forced by a cast, targeting 35 or above is exactly
what activates the 16 KB requirement. And 16 KB alignment cannot be reached from this editor at
any setting:

**`libunity.so` ships prebuilt inside the editor installation.** It is not compiled from project
sources, so no NDK, Gradle, or Player Setting in the project can change its segment alignment.
The alignment is a property of the Unity version.

So the two requirements are jointly unsatisfiable here: staying below API 35 avoids the 16 KB
rule but fails the target-level rule, and meeting the target-level rule triggers a rule this
editor cannot meet. Forcing the cast would produce an upload Play rejects, one build later.

## Recommendation

**Upgrade to `2022.3.62f3` — the newest release in the 2022.3 LTS line. Do not migrate to Unity 6
unless the measurement in step 4 below forces it.**

```
version    2022.3.62f3
changeset  96770f904ca7
released   28 Oct 2025
```

Enumerated from Unity's release API (`services.api.unity.com/unity/editor/release/v1/releases`,
`stream=LTS`, checked 9 Sep 2026): **65 releases exist in 2022.3, and 62f3 is the last of them.**
The line has had nothing new for nearly a year and should be treated as closed.

### Correcting an earlier version of this note

This note previously recommended "at least 2022.3.65f1", citing a forum summary that listed the
16 KB fix as landing in `2021.3.55f1, 2022.3.65f1, 6000.0.54f1`. **`2022.3.65f1` does not exist.**
The 2022.3 line stops at 62f3. That number was wrong and the recommendation built on it was
unsafe — it would have sent someone hunting for an editor Unity never shipped.

What this means for 16 KB is now genuinely **open**, not settled:

- `2022.3.62f3`'s release notes cover exactly two fixes (an inspector serialization issue,
  UUM-103578, and a WebGL 2D physics memory leak, UUM-108093). No page-size work.
- `62f2` and `62f3` both shipped in Oct 2025, five months after `62f1` and right around the
  16 KB deadline, which is suggestive but is not evidence.

So the upgrade is worth doing on its own merits — 48 patch releases of fixes, and the user asked
for it — but **it may not be sufficient for the store.** Measure, do not assume.

### Migration sequence, and the gate at each step

1. Install (the Hub CLI cannot be driven from the agent sandbox; run this yourself):

```bash
"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless install \
  --version 2022.3.62f3 --changeset 96770f904ca7 --module android ios --childModules
```

2. Back up uncommitted work first. `ProjectSettings/ProjectSettings.asset`,
   `UnityConnectSettings.asset`, `README.md` and `Assets/2D Renderer.asset` carry uncommitted
   thesis changes that a migration will rewrite. A copy plus a patch is kept at
   `~/spacecraft-divine-thesis-wip-2026-09-09/`.

3. Migrate by opening the project once in batchmode:

```bash
NEW="/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity"
"$NEW" -batchmode -quit -projectPath "$(pwd)" -logFile - 2>&1 | tail -40
```

4. **The gate is the suite, then the measurements:**

```bash
UNITY="$NEW" ./run-tests.sh                 # must stay at 86/86
make android-apk UNITY="$NEW"
aapt2 dump badging "Build Android/SpaceshipDivine.apk" | grep targetSdkVersion
llvm-readelf -l lib/arm64-v8a/libunity.so   # after unzipping the apk
```

Store readiness needs `targetSdkVersion:'36'` **and** `LOAD align = 0x4000`. Device testing needs
neither — an APK from any of these editors installs and runs.

5. If URP 14, the Input System or TextMeshPro break, report the specific failure rather than
   attempting a blind fix. That is a decision, not a task.

6. If step 4 cannot reach both numbers, Unity 6 LTS becomes necessary for the store upload, and
   the URP / Input System / TextMeshPro migration risk applies in full.

## Not urgent

None of this blocks the 11 Sep 2026 defense, and none of it blocks testing on a physical device:
an APK built here installs and runs fine. It blocks the **store upload**, which happens after.
