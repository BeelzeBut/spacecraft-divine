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

**Upgrade within the 2022.3 LTS line — do not migrate to Unity 6.**

Unity fixed 16 KB alignment in `2021.3.55f1`, **`2022.3.65f1`**, `6000.0.54f1` and later
(https://discussions.unity.com/t/android-unity-engine-support-for-16-kb-memory-page-sizes-android-15/1589588).
That the fix was backported into 2022.3 is the whole reason this is cheap: 2022.3.14f1 →
2022.3.65f1+ stays on the same LTS stream, so URP 14, the Input System and TextMeshPro keep the
package versions this project already resolves against. The plan's stated fear — "a project
migration that can break URP 14, the Input System and TextMeshPro" — is a Unity 6 risk, and Unity
6 is not required.

**Open question the upgrade must answer, and the gate on it:** whether the chosen 2022.3 patch
can target API 36. Unity's own docs list `AndroidApiLevel36` under the 6000.x reference, and
reports of 2022.3 patches supporting it are mixed. Decide it empirically, in this order:

1. Install the newest 2022.3 patch available; confirm `>= 2022.3.65f1`.
2. Open the project, let it migrate, then `./run-tests.sh`. **The suite passing is the gate.**
   If URP, Input System or TMP break, report the specific failure — that is a decision, not a task.
3. Build an APK and re-run both measurements above. Require `targetSdkVersion:'36'` **and**
   `LOAD align = 0x4000`.
4. Only if step 3 cannot reach 36 does Unity 6 LTS become necessary — and then the URP/Input
   System/TMP migration risk from the plan applies in full.

Do not set `AndroidTargetSdkVersion` away from Automatic as part of this. `ProjectSettings/` has
uncommitted thesis-related changes and is deliberately untouched on this branch.

## Not urgent

None of this blocks the 11 Sep 2026 defense, and none of it blocks testing on a physical device:
an APK built here installs and runs fine. It blocks the **store upload**, which happens after.
