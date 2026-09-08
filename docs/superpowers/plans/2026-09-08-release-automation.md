# Release Automation & Store Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every build, store-metadata and release action executable headlessly by an agent, and determine — cheaply, before committing to a 10–20 GB download — whether the Unity editor actually needs upgrading.

**Architecture:** Three layers. Unity Editor scripts invoked with `-executeMethod` handle everything inside the project. `fastlane` lanes handle everything at the stores. A thin `Makefile` gives both a single entry point so an agent never has to remember flag soup. Credentials live outside the repo and are never committed.

**Tech Stack:** Unity 2022.3.14f1 batchmode, fastlane 2.229.1, Google Play Developer API (`androidpublisher`) via service account, App Store Connect API via `.p8` key, Xcode 26.6, `adb`.

**Spec:** `docs/superpowers/specs/2026-09-08-production-release-design.md` (§2.5 build and store readiness, §4.8 compliance, phases 0 and 8)

## Global Constraints

- **Branch:** `production-release`. **Never push to any remote** until after the diploma defense on 11 Sep 2026. Local commits only.
- **`applicationIdentifier` is `com.KodaGames.SpaceshipDivine` and must never change** — mixed case included. The Play listing exists and is updated in place; changing the identifier creates a new app and forfeits the reviews, install base and published IAP SKUs.
- **Credentials are never committed.** Service account JSON and the ASC `.p8` live outside the repo; only their paths are referenced, via environment variables. Add every credential filename pattern to `.gitignore` before any credential exists on disk.
- Production rollout is authorised, but **staged**: a production release starts at a low rollout percentage and is widened only after crash-free rate holds. Never ship a first release at 100%.
- Unity editor path: `/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity`
- No Unity editor instance may be open while any batchmode command runs — it needs the project lock. Check `pgrep -fl "Unity.app/Contents/MacOS/Unity"` first.
- Only one Unity batchmode process at a time, project-wide. Never run a build and the test suite concurrently.

## Environment facts established before planning

| Fact | Value | Consequence |
|---|---|---|
| Installed Unity editors | only `2022.3.14f1` | An upgrade means a Hub download; Task 1 decides whether that is necessary |
| Android SDK platforms available | up to **android-36** (user SDK at `~/Library/Android/sdk`) | targetSdk 36 may be reachable without an editor upgrade — see Task 1 |
| Unity's bundled Android SDK | up to **android-32** only | Unity must be pointed at the external SDK |
| fastlane | 2.229.1, installed | No install step needed |
| Xcode | 26.6 (build 17F113) | Current; iOS builds and ASC uploads viable |
| Existing build scripts | `Assets/Editor/BuildAndroid.cs`, `BuildIOS.cs` | Foundation exists; Task 2 hardens rather than replaces them |
| fastlane config | none | Created in Task 4 |

---

### Task 1: Spike — can 2022.3.14f1 satisfy Play's requirements without an upgrade?

**This is a spike. Its output is an answer and a recommendation, not code you keep.** It exists
because an editor upgrade costs a 10–20 GB download plus a project migration that can break URP
14, the Input System and TextMeshPro — and it may be avoidable.

The cheap path is plausible: `PlayerSettings.Android.targetSdkVersion` is an int-backed enum, so
a value the 2022.3.14f1 enum does not name can still be assigned by casting, and the external SDK
already has android-36. The expensive path is likely unavoidable for the **16 KB page size**
requirement, which depends on the NDK and toolchain baked into the editor version, not on a
setting.

**Files:**
- Create (throwaway): `Assets/Editor/SdProbe.cs`
- Create: `docs/superpowers/notes/2026-09-08-editor-upgrade-decision.md`

- [ ] **Step 1: Establish what Play currently requires**

Do not rely on recalled values — they change annually. Determine, from Google's current
documentation, the target API level required for app updates today, and the current status of
the 16 KB page size requirement. Record both with the URL and the date checked.

- [ ] **Step 2: Point Unity at the external Android SDK**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
ls ~/Library/Android/sdk/platforms   # expect android-36 present
```

Set the SDK path in `ProjectSettings/EditorBuildSettings` is not where this lives — it is an
editor preference. Set it headlessly via the probe script in Step 3 using
`EditorPrefs.SetString("AndroidSdkRoot", "<path>")`, or confirm Unity already resolves it.

- [ ] **Step 3: Write the probe**

`Assets/Editor/SdProbe.cs`:

```csharp
using UnityEditor;
using UnityEngine;

/// <summary>
/// THROWAWAY probe for the editor-upgrade decision. Delete once the decision is recorded.
/// Reports what this editor can actually be told to target.
/// </summary>
public static class SdProbe
{
    public static void ReportAndroidCapabilities()
    {
        Debug.Log("Unity version: " + Application.unityVersion);
        Debug.Log("AndroidSdkRoot: " + EditorPrefs.GetString("AndroidSdkRoot"));
        Debug.Log("current targetSdkVersion: " + PlayerSettings.Android.targetSdkVersion +
                  " (int " + (int)PlayerSettings.Android.targetSdkVersion + ")");
        Debug.Log("current minSdkVersion: " + PlayerSettings.Android.minSdkVersion);
        Debug.Log("scripting backend: " + PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android));
        Debug.Log("architectures: " + PlayerSettings.Android.targetArchitectures);

        Debug.Log("named values in this editor's AndroidSdkVersions enum:");
        foreach (object v in System.Enum.GetValues(typeof(AndroidSdkVersions)))
            Debug.Log("  " + v + " = " + (int)v);

        // The actual question: does assigning an unnamed value stick?
        AndroidSdkVersions before = PlayerSettings.Android.targetSdkVersion;
        try
        {
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            Debug.Log("assigned 36 -> reads back as int " +
                      (int)PlayerSettings.Android.targetSdkVersion);
        }
        catch (System.Exception e)
        {
            Debug.LogError("assigning 36 threw: " + e.Message);
        }
        finally
        {
            PlayerSettings.Android.targetSdkVersion = before;   // leave settings untouched
            AssetDatabase.SaveAssets();
        }

        EditorApplication.Exit(0);
    }
}
```

- [ ] **Step 4: Run the probe**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
pgrep -fl "Unity.app/Contents/MacOS/Unity" && echo "KILL THE EDITOR FIRST" && exit 1
"/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$(pwd)" \
  -executeMethod SdProbe.ReportAndroidCapabilities -logFile - 2>&1 | grep -Ev "^\s*$" | tail -60
```

Confirm afterwards that `ProjectSettings/ProjectSettings.asset` is unchanged:
`git diff --stat ProjectSettings/ProjectSettings.asset` must be empty relative to where it
started. **Note:** this file has pre-existing uncommitted thesis-related changes — do not stage,
commit or revert it. Compare against the working-tree state you found, not against HEAD.

- [ ] **Step 5: Test the 16 KB question empirically**

This is the part a setting cannot answer. Produce an AAB with the current editor and inspect the
alignment of its native libraries:

```bash
# after a build exists (Task 2 produces one; if running Task 1 standalone, build manually first)
unzip -o -q "Build Android/SpaceshipDivine.aab" -d /tmp/aabcheck 2>/dev/null || \
  echo "no AAB yet — run Task 2 first, then return to this step"
find /tmp/aabcheck -name "*.so" | head -5 | while read -r so; do
  echo "--- $so"
  objdump -p "$so" 2>/dev/null | grep -i "LOAD" | head -3
done
```

A 16 KB-compatible library shows load segments aligned to 0x4000. If they are aligned to 0x1000,
this editor cannot satisfy the requirement and the upgrade is mandatory.

- [ ] **Step 6: Record the decision and delete the probe**

Write `docs/superpowers/notes/2026-09-08-editor-upgrade-decision.md` covering: what Play requires
today (with URL and date), what the probe reported, the `.so` alignment result, and a clear
recommendation — **upgrade required** or **not required**, with the reason.

```bash
rm Assets/Editor/SdProbe.cs Assets/Editor/SdProbe.cs.meta
git add docs/superpowers/notes/
git commit -m "Nota: decizia privind actualizarea editorului Unity pentru cerintele Play"
```

- [ ] **Step 7: If the upgrade IS required, perform it**

Only if Step 6 concluded it is necessary:

```bash
"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless editors --installed
"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless install-modules \
  --version 2022.3.14f1 --module android ios --childModules
```

To install a newer patch you need its exact version and changeset from Unity's release archive:

```bash
"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless install \
  --version <VERSION> --changeset <CHANGESET> --module android ios --childModules
```

Then migrate and verify, in this order, stopping at the first failure:

```bash
NEW="/Applications/Unity/Hub/Editor/<VERSION>/Unity.app/Contents/MacOS/Unity"
"$NEW" -batchmode -quit -projectPath "$(pwd)" -logFile - 2>&1 | tail -40   # migration
./run-tests.sh                                                             # suite still green
```

The suite passing after migration is the gate. If URP 14, the Input System or TextMeshPro break,
report the specific failure rather than attempting a blind fix — that is a decision, not a task.

---

### Task 2: Harden the headless build entry points

`Assets/Editor/BuildAndroid.cs` and `BuildIOS.cs` already exist. This task makes them
agent-drivable: deterministic output paths, version handling, explicit exit codes.

**Files:**
- Modify: `Assets/Editor/BuildAndroid.cs`
- Modify: `Assets/Editor/BuildIOS.cs`
- Create: `Makefile`

**Interfaces:**
- Produces:
  - `BuildAndroid.BuildAab()` — AAB to `Build Android/SpaceshipDivine.aab`, exits non-zero on failure
  - `BuildAndroid.BuildApk()` — APK for `adb` device testing
  - `BuildIOS.BuildXcodeProject()` — Xcode project to `Build IOS/`
  - `make android-aab`, `make android-apk`, `make ios`, `make test`

- [ ] **Step 1: Read the existing build scripts before changing them**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
cat Assets/Editor/BuildAndroid.cs
cat Assets/Editor/BuildIOS.cs
```

Preserve whatever already works. The required additions are below; do not rewrite wholesale.

- [ ] **Step 2: Ensure each entry point reports failure**

Every build method must end by inspecting the `BuildReport` and exiting non-zero when the build
did not succeed. A batchmode build that fails silently and exits 0 is the same class of defect as
a test harness that reports false success:

```csharp
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("Build FAILED: " + report.summary.result +
                           ", errors: " + report.summary.totalErrors);
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("Build OK: " + report.summary.outputPath +
                  " (" + report.summary.totalSize + " bytes)");
        EditorApplication.Exit(0);
```

- [ ] **Step 3: Add version handling driven by the environment**

So a release lane can set the version without editing the project:

```csharp
    private static void ApplyVersionFromEnvironment()
    {
        string version = System.Environment.GetEnvironmentVariable("SD_VERSION");
        if (!string.IsNullOrEmpty(version))
            PlayerSettings.bundleVersion = version;

        string code = System.Environment.GetEnvironmentVariable("SD_BUILD_NUMBER");
        if (!string.IsNullOrEmpty(code) && int.TryParse(code, out int parsed))
        {
            PlayerSettings.Android.bundleVersionCode = parsed;
            PlayerSettings.iOS.buildNumber = code;
        }

        Debug.Log("Building version " + PlayerSettings.bundleVersion +
                  " code " + PlayerSettings.Android.bundleVersionCode);
    }
```

Call it first in each build method. **Never change `applicationIdentifier` here or anywhere.**

- [ ] **Step 4: Write the Makefile**

```makefile
UNITY := /Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity
PROJECT := $(shell pwd)

.PHONY: guard test android-aab android-apk ios

# Every Unity target depends on this: batchmode needs the project lock.
guard:
	@pgrep -fl "Unity.app/Contents/MacOS/Unity" >/dev/null 2>&1 && \
	  { echo "A Unity editor is open. Close it first."; exit 1; } || true

test: guard
	./run-tests.sh

android-aab: guard
	"$(UNITY)" -batchmode -quit -projectPath "$(PROJECT)" \
	  -executeMethod BuildAndroid.BuildAab -logFile - 2>&1 | tail -30

android-apk: guard
	"$(UNITY)" -batchmode -quit -projectPath "$(PROJECT)" \
	  -executeMethod BuildAndroid.BuildApk -logFile - 2>&1 | tail -30

ios: guard
	"$(UNITY)" -batchmode -quit -projectPath "$(PROJECT)" \
	  -executeMethod BuildIOS.BuildXcodeProject -logFile - 2>&1 | tail -30
```

- [ ] **Step 5: Verify both targets actually build**

```bash
make android-aab && echo "AAB EXIT=$?"
ls -la "Build Android/"*.aab
```

- [ ] **Step 6: Commit**

```bash
git add Assets/Editor Makefile
git commit -m "Compilare: puncte de intrare headless cu coduri de iesire si versionare din mediu"
```

---

### Task 3: Credential scaffolding — gitignore first, secrets second

Done before any credential exists on disk, so a secret can never be committed by accident.

**Files:**
- Modify: `.gitignore`
- Create: `fastlane/.env.example`
- Create: `docs/superpowers/notes/credential-setup.md`

- [ ] **Step 1: Extend .gitignore before creating any secret**

Append to `.gitignore`:

```
# ---------------------------------------------------------------------------
# Store credentials — never commit. Real files live outside the repo.
# ---------------------------------------------------------------------------
*.p8
*.p12
play-service-account*.json
fastlane/.env
fastlane/report.xml
fastlane/Preview.html
fastlane/screenshots/**/*.png
```

Verify it works before proceeding:

```bash
touch /tmp/x.p8 && cp /tmp/x.p8 ./probe.p8
git check-ignore -v probe.p8 || echo "*** NOT IGNORED — STOP ***"
rm probe.p8 /tmp/x.p8
```

- [ ] **Step 2: Document what a human must generate**

Write `docs/superpowers/notes/credential-setup.md` listing, with exact console navigation:

1. **Google Play service account** — created in Google Cloud Console, granted access in Play
   Console under Users and permissions. Scope it to **Release manager**, not Admin. Download the
   JSON to `~/.spaceship-divine/play-service-account.json`.
2. **App Store Connect API key** — App Store Connect → Users and Access → Integrations → keys.
   Role **Developer**, not Account Holder. Download the `.p8` (downloadable once only) to
   `~/.spaceship-divine/`. Record the Key ID and Issuer ID.
3. **AdMob** — has no API for creating ad units; app and ad-unit IDs are read from the console UI.

- [ ] **Step 3: Write the env template**

`fastlane/.env.example`:

```bash
# Copy to fastlane/.env (git-ignored) and fill in. Never commit the real file.
SD_PLAY_JSON_KEY=$HOME/.spaceship-divine/play-service-account.json
SD_ASC_KEY_ID=
SD_ASC_ISSUER_ID=
SD_ASC_KEY_PATH=$HOME/.spaceship-divine/AuthKey_XXXXXXXX.p8
SD_APP_IDENTIFIER=com.KodaGames.SpaceshipDivine
SD_ANDROID_PACKAGE=com.KodaGames.SpaceshipDivine
```

- [ ] **Step 4: Commit**

```bash
git add .gitignore fastlane/.env.example docs/superpowers/notes/credential-setup.md
git commit -m "Securitate: ignorarea acreditarilor magazinelor si sablon de configurare"
```

---

### Task 4: fastlane lanes

**Files:**
- Create: `fastlane/Fastfile`
- Create: `fastlane/Appfile`

**Interfaces:**
- Produces lanes: `android_internal`, `android_production`, `ios_testflight`, `play_products`, `verify_credentials`

- [ ] **Step 1: Write the Appfile**

```ruby
app_identifier ENV["SD_APP_IDENTIFIER"]
package_name   ENV["SD_ANDROID_PACKAGE"]
```

- [ ] **Step 2: Write the Fastfile**

```ruby
default_platform(:android)

# Verifies credentials resolve before any lane tries to use them.
lane :verify_credentials do
  UI.user_error!("SD_PLAY_JSON_KEY not set") if ENV["SD_PLAY_JSON_KEY"].to_s.empty?
  UI.user_error!("Play key file missing") unless File.exist?(ENV["SD_PLAY_JSON_KEY"])
  UI.success("Play credentials present")
end

platform :android do
  desc "Upload the AAB to the internal test track"
  lane :android_internal do
    upload_to_play_store(
      track: "internal",
      aab: "Build Android/SpaceshipDivine.aab",
      json_key: ENV["SD_PLAY_JSON_KEY"],
      skip_upload_metadata: true,
      skip_upload_images: true,
      skip_upload_screenshots: true
    )
  end

  desc "Promote to production as a STAGED rollout. Never ships at 100% on a first release."
  lane :android_production do |options|
    fraction = (options[:rollout] || "0.05").to_f
    UI.user_error!("rollout must be < 1.0 for a first production release") if fraction >= 1.0
    upload_to_play_store(
      track: "production",
      aab: "Build Android/SpaceshipDivine.aab",
      json_key: ENV["SD_PLAY_JSON_KEY"],
      rollout: fraction.to_s,
      skip_upload_metadata: true,
      skip_upload_images: true,
      skip_upload_screenshots: true
    )
  end
end

platform :ios do
  desc "Build the Xcode project output and ship it to TestFlight"
  lane :ios_testflight do
    app_store_connect_api_key(
      key_id: ENV["SD_ASC_KEY_ID"],
      issuer_id: ENV["SD_ASC_ISSUER_ID"],
      key_filepath: ENV["SD_ASC_KEY_PATH"]
    )
    build_app(
      project: "Build IOS/Unity-iPhone.xcodeproj",
      scheme: "Unity-iPhone",
      export_method: "app-store"
    )
    upload_to_testflight(skip_waiting_for_build_processing: true)
  end
end
```

- [ ] **Step 3: Verify the lanes parse without credentials present**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
fastlane lanes
```

Expected: all lanes listed, no syntax error. This works without credentials.

- [ ] **Step 4: Commit**

```bash
git add fastlane/Fastfile fastlane/Appfile
git commit -m "Livrare: benzi fastlane pentru Play si TestFlight, cu lansare esalonata"
```

---

## Out of scope for this plan

| Deferred | Why |
|---|---|
| Actually running store lanes | Requires the credentials a human generates in Task 3 Step 2 |
| AdMob ad-unit creation | No create API; driven through the console UI with Playwright when app IDs are needed |
| Play Data Safety form, ATT strings, content ratings | Legal attestations. Marco is the declarant; an agent must not answer them on his behalf |
| iOS privacy manifest content | Depends on which SDKs ship, which the ads and IAP plans decide |
| Firebase project creation | Belongs to the online-services plan |
| Device haptic verification | `adb` can prove the API fired; whether a pulse *feels* right is not automatable |
