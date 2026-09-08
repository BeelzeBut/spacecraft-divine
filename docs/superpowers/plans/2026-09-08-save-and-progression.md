# Save Rearchitecture & Progression Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the save system so ship design data no longer round-trips through player saves, then activate the ship progression gate with reduced prices — without taking any unlock away from an existing player.

**Architecture:** A new `SpaceshipDivine.Save` assembly holds a pure, engine-independent save core (data models, JSON envelope, HMAC integrity, atomic file store, versioned migration). It is `autoReferenced`, so the existing `Assembly-CSharp` code can call into it, while it deliberately cannot call back — which is what keeps it unit-testable. `Spaceship` ScriptableObjects become read-only design data; per-run mutation moves to a plain `ShipRuntime` class.

**Tech Stack:** Unity 2022.3.14f1, C# (.NET Standard 2.1, `apiCompatibilityLevel: 6`), Unity Test Framework 1.1.33 (NUnit, EditMode), `JsonUtility`, `System.Security.Cryptography.HMACSHA256`.

**Spec:** `docs/superpowers/specs/2026-09-08-production-release-design.md`

## Global Constraints

- **Branch:** all work lands on `production-release`. **Never push to any remote** — the diploma defense is 11 Sep 2026 and `main` must stay presentable. Local commits only.
- **Never change `applicationIdentifier`.** It stays `com.KodaGames.SpaceshipDivine`, mixed case included.
- **Grandfathering is a hard requirement.** Any ship marked unlocked in a legacy save stays unlocked after migration. This is tested explicitly in Task 5; a failure here is a release blocker, not a bug.
- **Ship identity is a stable string ID, never an array index.** Reordering `shipPrefabs` must never reassign an unlock.
- **`Spaceship` ScriptableObjects are read-only at runtime** after Task 8. No code path may assign to a `Spaceship` field during play.
- **The save core assembly must not reference `Assembly-CSharp`.** Unity forbids asmdef → predefined-assembly references, and this constraint is what makes the core testable. Keep `UnityEngine` usage inside it limited to `JsonUtility`, `Application.persistentDataPath` and `Debug`.
- **Starter ships** are exactly: `grey_byrd`, `apollo`, `the_argon`.
- **Unity editor path:** `/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity`
- No Unity editor instance may be open while tests run — batchmode needs the project lock.

---

### Task 1: EditMode test harness

Nothing in this project is currently testable: there are no `.asmdef` files and no test assemblies. This task proves the loop works before any real code depends on it.

**Files:**
- Create: `Assets/Scripts/Save/SpaceshipDivine.Save.asmdef`
- Create: `Assets/Scripts/Save/SaveCoreMarker.cs`
- Create: `Assets/Tests/EditMode/SpaceshipDivine.Save.Tests.asmdef`
- Create: `Assets/Tests/EditMode/HarnessTest.cs`
- Create: `run-tests.sh`

**Interfaces:**
- Consumes: nothing.
- Produces: the `SpaceshipDivine.Save` assembly and namespace that every later task adds to; `./run-tests.sh` as the single test command used by every later task.

- [ ] **Step 1: Create the save core assembly definition**

`Assets/Scripts/Save/SpaceshipDivine.Save.asmdef`:

```json
{
    "name": "SpaceshipDivine.Save",
    "rootNamespace": "SpaceshipDivine.Save",
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

`autoReferenced: true` is what lets the existing `Assembly-CSharp` code (`DataHolder`, `MainMenu`) call into this assembly in later tasks.

- [ ] **Step 2: Add a marker type so the assembly compiles**

`Assets/Scripts/Save/SaveCoreMarker.cs`:

```csharp
namespace SpaceshipDivine.Save
{
    /// <summary>Exists so the assembly has a type before real code lands. Delete in Task 2.</summary>
    internal static class SaveCoreMarker
    {
        internal const int SchemaVersion = 1;
    }
}
```

- [ ] **Step 3: Create the test assembly definition**

`Assets/Tests/EditMode/SpaceshipDivine.Save.Tests.asmdef`:

```json
{
    "name": "SpaceshipDivine.Save.Tests",
    "rootNamespace": "SpaceshipDivine.Save.Tests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "SpaceshipDivine.Save"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4: Write the harness test**

`Assets/Tests/EditMode/HarnessTest.cs`:

```csharp
using NUnit.Framework;

namespace SpaceshipDivine.Save.Tests
{
    public class HarnessTest
    {
        [Test]
        public void TestHarnessRuns()
        {
            Assert.AreEqual(1, SaveCoreMarkerProbe.SchemaVersion);
        }
    }
}
```

`SaveCoreMarker` is `internal`, so add a public probe in the save assembly rather than widening it. Append to `Assets/Scripts/Save/SaveCoreMarker.cs`:

```csharp
namespace SpaceshipDivine.Save
{
    /// <summary>Test-visible probe for the harness check. Delete in Task 2.</summary>
    public static class SaveCoreMarkerProbe
    {
        public static int SchemaVersion => SaveCoreMarker.SchemaVersion;
    }
}
```

- [ ] **Step 5: Write the test runner script**

`run-tests.sh`:

```bash
#!/usr/bin/env bash
# Runs the EditMode test suite in Unity batchmode.
# No Unity editor instance may be open — batchmode needs the project lock.
set -uo pipefail

UNITY="/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESULTS="$PROJECT/Temp/editmode-results.xml"

"$UNITY" \
  -runTests \
  -batchmode \
  -projectPath "$PROJECT" \
  -testPlatform EditMode \
  -testResults "$RESULTS" \
  -logFile - 2>&1 | tail -40

if [ ! -f "$RESULTS" ]; then
  echo "NO RESULTS FILE — Unity failed to start or compile. See log above."
  exit 1
fi

python3 - "$RESULTS" <<'PY'
import sys, xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
total  = r.get('total', '0')
passed = r.get('passed', '0')
failed = r.get('failed', '0')
print(f"\n=== total={total} passed={passed} failed={failed} ===")
for tc in r.iter('test-case'):
    if tc.get('result') != 'Passed':
        print(f"FAIL: {tc.get('fullname')}")
        f = tc.find('failure/message')
        if f is not None and f.text:
            print(f"      {f.text.strip()[:400]}")
sys.exit(1 if failed != '0' or total == '0' else 0)
PY
```

Then: `chmod +x run-tests.sh`

- [ ] **Step 6: Run the tests and confirm the harness works**

Run: `./run-tests.sh`

Expected: `=== total=1 passed=1 failed=0 ===` and exit 0.

First run reimports assets and may take several minutes. If it reports `NO RESULTS FILE`, read the log tail: the usual causes are an open Unity editor holding the lock, or an asmdef JSON syntax error.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Save Assets/Tests run-tests.sh
git commit -m "Test: infrastructura de testare EditMode pentru nucleul de salvare"
```

---

### Task 2: Save envelope and HMAC integrity

**Files:**
- Create: `Assets/Scripts/Save/SaveEnvelope.cs`
- Create: `Assets/Scripts/Save/SaveIntegrity.cs`
- Delete: `Assets/Scripts/Save/SaveCoreMarker.cs`
- Create: `Assets/Tests/EditMode/SaveIntegrityTests.cs`
- Delete: `Assets/Tests/EditMode/HarnessTest.cs`

**Interfaces:**
- Consumes: the `SpaceshipDivine.Save` assembly from Task 1.
- Produces:
  - `SaveEnvelope` — `public int version; public string payload; public string signature;`
  - `SaveIntegrity.Sign(string payload) -> string`
  - `SaveIntegrity.Verify(string payload, string signature) -> bool`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/SaveIntegrityTests.cs`:

```csharp
using NUnit.Framework;

namespace SpaceshipDivine.Save.Tests
{
    public class SaveIntegrityTests
    {
        [Test]
        public void SignedPayloadVerifies()
        {
            string payload = "{\"gems\":350}";
            string sig = SaveIntegrity.Sign(payload);
            Assert.IsTrue(SaveIntegrity.Verify(payload, sig));
        }

        [Test]
        public void TamperedPayloadFailsVerification()
        {
            string sig = SaveIntegrity.Sign("{\"gems\":350}");
            Assert.IsFalse(SaveIntegrity.Verify("{\"gems\":999999}", sig));
        }

        [Test]
        public void EmptySignatureFailsVerification()
        {
            Assert.IsFalse(SaveIntegrity.Verify("{\"gems\":350}", ""));
            Assert.IsFalse(SaveIntegrity.Verify("{\"gems\":350}", null));
        }

        [Test]
        public void SignatureIsStableAcrossCalls()
        {
            string payload = "{\"gems\":350}";
            Assert.AreEqual(SaveIntegrity.Sign(payload), SaveIntegrity.Sign(payload));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

First delete the Task 1 scaffolding so it does not mask the result:

```bash
rm Assets/Tests/EditMode/HarnessTest.cs Assets/Tests/EditMode/HarnessTest.cs.meta
rm Assets/Scripts/Save/SaveCoreMarker.cs Assets/Scripts/Save/SaveCoreMarker.cs.meta
```

Run: `./run-tests.sh`
Expected: FAIL — compile error, `SaveIntegrity` does not exist.

- [ ] **Step 3: Write the envelope**

`Assets/Scripts/Save/SaveEnvelope.cs`:

```csharp
using System;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// What actually lands on disk: a schema version, the serialised profile, and a
    /// signature over that payload. Versioning the envelope rather than the payload means
    /// the migrator can read a file it does not yet understand the shape of.
    /// </summary>
    [Serializable]
    public class SaveEnvelope
    {
        public const int CurrentVersion = 1;

        public int version;
        public string payload;
        public string signature;
    }
}
```

- [ ] **Step 4: Write the integrity implementation**

`Assets/Scripts/Save/SaveIntegrity.cs`:

```csharp
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// Signs save payloads so a hand-edited gem balance is detectable. This raises the cost
    /// of casual tampering; it is not a defence against a determined attacker with the
    /// binary, which would require server-side authority we deliberately do not have.
    /// </summary>
    public static class SaveIntegrity
    {
        // Split so the literal string does not appear contiguously in the binary.
        private const string SecretA = "sd-4f21a9c7";
        private const string SecretB = "e30b-koda";

        private static byte[] Key()
        {
            string deviceSalt = SystemInfo.deviceUniqueIdentifier ?? "nodevice";
            return Encoding.UTF8.GetBytes(SecretA + deviceSalt + SecretB);
        }

        public static string Sign(string payload)
        {
            if (payload == null) payload = string.Empty;
            using (var hmac = new HMACSHA256(Key()))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return Convert.ToBase64String(hash);
            }
        }

        public static bool Verify(string payload, string signature)
        {
            if (string.IsNullOrEmpty(signature)) return false;

            string expected = Sign(payload);
            if (expected.Length != signature.Length) return false;

            // Constant-time compare, so failure timing does not leak the expected value.
            int diff = 0;
            for (int i = 0; i < expected.Length; i++)
                diff |= expected[i] ^ signature[i];
            return diff == 0;
        }
    }
}
```

Note: keying on `deviceUniqueIdentifier` means a save copied between devices fails verification and falls back to a fresh profile. That is the intended trade-off for a game with no cloud save — it blocks save-sharing as a piracy vector. Revisit if cloud save is ever added.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `./run-tests.sh`
Expected: `=== total=4 passed=4 failed=0 ===`

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Save Assets/Tests
git commit -m "Salvare: plic versionat si semnatura HMAC pentru integritate"
```

---

### Task 3: PlayerProfile and RunState data models

**Files:**
- Create: `Assets/Scripts/Save/PlayerProfile.cs`
- Create: `Assets/Scripts/Save/RunState.cs`
- Create: `Assets/Tests/EditMode/PlayerProfileTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces:
  - `PlayerProfile` with `int gems`, `int goldCoins`, `List<string> unlockedShipIds`, `List<ShipLevelEntry> shipLevels`, `List<int> collectedBlueprintIndices`, `List<string> blueprintRedeemableShipIds`, `bool hasCompletedTutorial`, `bool hasCompletedButtonsTutorial`, `bool removeAdsOwned`, `SettingsData settings`, `LifetimeStats stats`
  - `PlayerProfile.IsUnlocked(string shipId) -> bool`
  - `PlayerProfile.Unlock(string shipId) -> void`
  - `PlayerProfile.GetShipLevel(string shipId) -> int`
  - `PlayerProfile.SetShipLevel(string shipId, int level) -> void`
  - `PlayerProfile.HasCollectedBlueprint(int dropIndex) -> bool`
  - `PlayerProfile.CollectBlueprint(int dropIndex) -> void`
  - `PlayerProfile.IsBlueprintRedeemable(string shipId) -> bool`
  - `PlayerProfile.MarkBlueprintRedeemable(string shipId) -> void`
  - `PlayerProfile.CreateDefault() -> PlayerProfile`

**Background — the existing blueprint system.** Three ships (`vickers`, `warspite`, `bubu`)
are unlocked in game, and that system already works: `Enemy.cs:384` flags a boss blueprint
drop, `Enemy.cs:396` drops blueprints at `DataHolder.killMilestones`, `RoomChest` handles
rare drops, and `Blueprint.UnlockCollectible()` records the pickup. The legacy save tracked
this in two places — `hasBeenUnlocked[100]` (which blueprints were picked up) and, as an
overloaded flag, `priceToUnlock[i] == 1` (which ship is now claimable in the menu). Both are
real player progress and both must survive migration. The profile models them explicitly so
the overloading can be retired later.
  - `RunState` with `string selectedShipId`, `int level`, `int subLevel`, `int respawnsRemaining`, `bool runInProgress`, `List<int> upgradeIndices`, `string abilityName`, `int abilityLevel`, `int enemiesKilled`

`JsonUtility` cannot serialise dictionaries, so ship levels use a list of entries.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/PlayerProfileTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

namespace SpaceshipDivine.Save.Tests
{
    public class PlayerProfileTests
    {
        [Test]
        public void DefaultProfileUnlocksExactlyTheThreeStarters()
        {
            var p = PlayerProfile.CreateDefault();
            Assert.IsTrue(p.IsUnlocked("grey_byrd"));
            Assert.IsTrue(p.IsUnlocked("apollo"));
            Assert.IsTrue(p.IsUnlocked("the_argon"));
            Assert.IsFalse(p.IsUnlocked("valiant"));
            Assert.IsFalse(p.IsUnlocked("lunar_hunter"));
            Assert.AreEqual(3, p.unlockedShipIds.Count);
        }

        [Test]
        public void DefaultProfileStartsWith350Gems()
        {
            Assert.AreEqual(350, PlayerProfile.CreateDefault().gems);
        }

        [Test]
        public void UnlockIsIdempotent()
        {
            var p = PlayerProfile.CreateDefault();
            p.Unlock("valiant");
            p.Unlock("valiant");
            Assert.IsTrue(p.IsUnlocked("valiant"));
            Assert.AreEqual(4, p.unlockedShipIds.Count);
        }

        [Test]
        public void ShipLevelDefaultsToOneAndRoundTrips()
        {
            var p = PlayerProfile.CreateDefault();
            Assert.AreEqual(1, p.GetShipLevel("apollo"));
            p.SetShipLevel("apollo", 4);
            Assert.AreEqual(4, p.GetShipLevel("apollo"));
        }

        [Test]
        public void ShipLevelIsClampedToOneThroughFive()
        {
            var p = PlayerProfile.CreateDefault();
            p.SetShipLevel("apollo", 99);
            Assert.AreEqual(5, p.GetShipLevel("apollo"));
            p.SetShipLevel("apollo", -3);
            Assert.AreEqual(1, p.GetShipLevel("apollo"));
        }

        [Test]
        public void BlueprintCollectionIsRecordedAndIdempotent()
        {
            var p = PlayerProfile.CreateDefault();
            Assert.IsFalse(p.HasCollectedBlueprint(7));
            p.CollectBlueprint(7);
            p.CollectBlueprint(7);
            Assert.IsTrue(p.HasCollectedBlueprint(7));
            Assert.AreEqual(1, p.collectedBlueprintIndices.Count);
        }

        [Test]
        public void BlueprintRedeemableIsRecordedAndIdempotent()
        {
            var p = PlayerProfile.CreateDefault();
            Assert.IsFalse(p.IsBlueprintRedeemable("vickers"));
            p.MarkBlueprintRedeemable("vickers");
            p.MarkBlueprintRedeemable("vickers");
            Assert.IsTrue(p.IsBlueprintRedeemable("vickers"));
            Assert.AreEqual(1, p.blueprintRedeemableShipIds.Count);
        }

        [Test]
        public void RedeemableIsNotTheSameAsUnlocked()
        {
            // A collected blueprint makes a ship claimable; the player still has to claim it.
            var p = PlayerProfile.CreateDefault();
            p.MarkBlueprintRedeemable("warspite");
            Assert.IsTrue(p.IsBlueprintRedeemable("warspite"));
            Assert.IsFalse(p.IsUnlocked("warspite"));
        }

        [Test]
        public void ProfileSurvivesJsonRoundTrip()
        {
            var p = PlayerProfile.CreateDefault();
            p.gems = 4200;
            p.Unlock("razor");
            p.SetShipLevel("razor", 3);
            p.removeAdsOwned = true;
            p.stats.totalEnemiesKilled = 812;

            var back = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(p));

            Assert.AreEqual(4200, back.gems);
            Assert.IsTrue(back.IsUnlocked("razor"));
            Assert.AreEqual(3, back.GetShipLevel("razor"));
            Assert.IsTrue(back.removeAdsOwned);
            Assert.AreEqual(812, back.stats.totalEnemiesKilled);
        }

        [Test]
        public void RunStateSurvivesJsonRoundTrip()
        {
            var r = new RunState
            {
                selectedShipId = "hot_talon",
                level = 2,
                subLevel = 3,
                respawnsRemaining = 1,
                runInProgress = true,
                abilityName = "Shield",
                abilityLevel = 2,
                enemiesKilled = 44
            };
            r.upgradeIndices.Add(7);
            r.upgradeIndices.Add(12);

            var back = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(r));

            Assert.AreEqual("hot_talon", back.selectedShipId);
            Assert.AreEqual(2, back.level);
            Assert.AreEqual(3, back.subLevel);
            Assert.IsTrue(back.runInProgress);
            Assert.AreEqual(2, back.upgradeIndices.Count);
            Assert.AreEqual(12, back.upgradeIndices[1]);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `./run-tests.sh`
Expected: FAIL — compile error, `PlayerProfile` does not exist.

- [ ] **Step 3: Write PlayerProfile**

`Assets/Scripts/Save/PlayerProfile.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace SpaceshipDivine.Save
{
    [Serializable]
    public class ShipLevelEntry
    {
        public string shipId;
        public int level = 1;
    }

    [Serializable]
    public class SettingsData
    {
        public int qualityIndex = 3;
        public int targetFps = 60;
        public bool isMusicMuted;
        public bool isSoundMuted;
        public bool isHapticsMuted;
        public bool controllerOn;
        public int cameraSpeedLevel = 3;
    }

    [Serializable]
    public class LifetimeStats
    {
        public int totalEnemiesKilled;
        public int totalBossesDefeated;
        public int totalLevelsCleared;
        public int totalRunsCompleted;
    }

    /// <summary>
    /// Everything that persists across runs. Contains no ship design data — ship stats live
    /// on the Spaceship ScriptableObjects and are read fresh every launch, which is what
    /// makes post-launch rebalancing possible.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public const int StartingGems = 350;

        // The three ships a brand-new player can select. "grey_byrd_tutorial" is NOT here:
        // it is forced by the tutorial rather than chosen, and is handled separately in
        // DataHolder so it can never be the reason a new player is soft-locked.
        public static readonly string[] StarterShipIds = { "grey_byrd", "apollo", "the_argon" };

        public int gems;
        public int goldCoins;
        public bool hasCompletedTutorial;
        public bool hasCompletedButtonsTutorial;
        public bool removeAdsOwned;
        public string levelsPlayed = "";

        public List<string> unlockedShipIds = new List<string>();
        public List<ShipLevelEntry> shipLevels = new List<ShipLevelEntry>();

        // Blueprint progress. collectedBlueprintIndices mirrors the legacy hasBeenUnlocked[]
        // (which blueprint pickups have happened, so they do not drop twice).
        // blueprintRedeemableShipIds replaces the legacy overloading of priceToUnlock == 1
        // (which ships the player may now claim for free in the menu).
        public List<int> collectedBlueprintIndices = new List<int>();
        public List<string> blueprintRedeemableShipIds = new List<string>();

        public SettingsData settings = new SettingsData();
        public LifetimeStats stats = new LifetimeStats();

        public static PlayerProfile CreateDefault()
        {
            var p = new PlayerProfile { gems = StartingGems };
            foreach (string id in StarterShipIds)
                p.Unlock(id);
            return p;
        }

        public bool IsUnlocked(string shipId)
        {
            return !string.IsNullOrEmpty(shipId) && unlockedShipIds.Contains(shipId);
        }

        public void Unlock(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) return;
            if (!unlockedShipIds.Contains(shipId))
                unlockedShipIds.Add(shipId);
        }

        public int GetShipLevel(string shipId)
        {
            ShipLevelEntry e = FindLevelEntry(shipId);
            return e == null ? 1 : e.level;
        }

        public void SetShipLevel(string shipId, int level)
        {
            if (string.IsNullOrEmpty(shipId)) return;

            if (level < 1) level = 1;
            if (level > 5) level = 5;

            ShipLevelEntry e = FindLevelEntry(shipId);
            if (e == null)
            {
                e = new ShipLevelEntry { shipId = shipId };
                shipLevels.Add(e);
            }
            e.level = level;
        }

        public bool HasCollectedBlueprint(int dropIndex)
        {
            return collectedBlueprintIndices.Contains(dropIndex);
        }

        public void CollectBlueprint(int dropIndex)
        {
            if (!collectedBlueprintIndices.Contains(dropIndex))
                collectedBlueprintIndices.Add(dropIndex);
        }

        public bool IsBlueprintRedeemable(string shipId)
        {
            return !string.IsNullOrEmpty(shipId) && blueprintRedeemableShipIds.Contains(shipId);
        }

        public void MarkBlueprintRedeemable(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) return;
            if (!blueprintRedeemableShipIds.Contains(shipId))
                blueprintRedeemableShipIds.Add(shipId);
        }

        private ShipLevelEntry FindLevelEntry(string shipId)
        {
            for (int i = 0; i < shipLevels.Count; i++)
                if (shipLevels[i].shipId == shipId)
                    return shipLevels[i];
            return null;
        }
    }
}
```

- [ ] **Step 4: Write RunState**

`Assets/Scripts/Save/RunState.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// The current run only. Cleared when a run ends. Ship stat mutations during a run live
    /// on ShipRuntime (Assembly-CSharp), never here and never on the ScriptableObject.
    /// </summary>
    [Serializable]
    public class RunState
    {
        public string selectedShipId = "";
        public int level = 1;
        public int subLevel = 1;
        public int respawnsRemaining = 1;
        public bool runInProgress;
        public int enemiesKilled;
        public float timeSinceGameStarted;

        public string abilityName = "";
        public int abilityLevel;

        public List<int> upgradeIndices = new List<int>();

        public void Clear()
        {
            selectedShipId = "";
            level = 1;
            subLevel = 1;
            respawnsRemaining = 1;
            runInProgress = false;
            enemiesKilled = 0;
            timeSinceGameStarted = 0f;
            abilityName = "";
            abilityLevel = 0;
            upgradeIndices.Clear();
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `./run-tests.sh`
Expected: `=== total=14 passed=14 failed=0 ===`

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Save Assets/Tests
git commit -m "Salvare: modele PlayerProfile si RunState, separate de datele de proiectare"
```

---

### Task 4: Atomic file store with recovery ladder

**Files:**
- Create: `Assets/Scripts/Save/ISaveStore.cs`
- Create: `Assets/Scripts/Save/FileSaveStore.cs`
- Create: `Assets/Tests/EditMode/FileSaveStoreTests.cs`

**Interfaces:**
- Consumes: `SaveEnvelope`, `SaveIntegrity` (Task 2); `PlayerProfile` (Task 3).
- Produces:
  - `ISaveStore` with `void Write(PlayerProfile profile)`, `PlayerProfile Read()`, `bool Exists()`, `void Delete()`
  - `FileSaveStore(string directory)` — constructor takes the directory so tests use a temp path instead of `Application.persistentDataPath`
  - `FileSaveStore.LastReadOutcome` → `ReadOutcome` enum: `Fresh`, `Loaded`, `RecoveredFromBackup`, `RejectedAndReset`

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/FileSaveStoreTests.cs`:

```csharp
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SpaceshipDivine.Save.Tests
{
    public class FileSaveStoreTests
    {
        private string dir;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "sd_save_tests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        [Test]
        public void ReadWithNoFileReturnsDefaultProfile()
        {
            var store = new FileSaveStore(dir);
            PlayerProfile p = store.Read();

            Assert.AreEqual(ReadOutcome.Fresh, store.LastReadOutcome);
            Assert.AreEqual(350, p.gems);
            Assert.IsTrue(p.IsUnlocked("apollo"));
        }

        [Test]
        public void WriteThenReadRoundTrips()
        {
            var store = new FileSaveStore(dir);
            var p = PlayerProfile.CreateDefault();
            p.gems = 9001;
            p.Unlock("valiant");
            store.Write(p);

            var reader = new FileSaveStore(dir);
            PlayerProfile back = reader.Read();

            Assert.AreEqual(ReadOutcome.Loaded, reader.LastReadOutcome);
            Assert.AreEqual(9001, back.gems);
            Assert.IsTrue(back.IsUnlocked("valiant"));
        }

        [Test]
        public void TamperedSaveFallsBackToBackup()
        {
            var store = new FileSaveStore(dir);

            var first = PlayerProfile.CreateDefault();
            first.gems = 100;
            store.Write(first);

            var second = PlayerProfile.CreateDefault();
            second.gems = 200;
            store.Write(second);   // first write becomes the backup

            // Hand-edit the live file the way a cheat tool would.
            string live = Path.Combine(dir, FileSaveStore.SaveFileName);
            File.WriteAllText(live, File.ReadAllText(live).Replace("\"gems\":200", "\"gems\":999999"));

            var reader = new FileSaveStore(dir);
            PlayerProfile p = reader.Read();

            Assert.AreEqual(ReadOutcome.RecoveredFromBackup, reader.LastReadOutcome);
            Assert.AreEqual(100, p.gems);
        }

        [Test]
        public void CorruptSaveWithNoBackupResetsToDefault()
        {
            File.WriteAllText(Path.Combine(dir, FileSaveStore.SaveFileName), "this is not json");

            var store = new FileSaveStore(dir);
            PlayerProfile p = store.Read();

            Assert.AreEqual(ReadOutcome.RejectedAndReset, store.LastReadOutcome);
            Assert.AreEqual(350, p.gems);
        }

        [Test]
        public void PartialTempFileIsIgnoredAndLiveSaveSurvives()
        {
            var store = new FileSaveStore(dir);
            var p = PlayerProfile.CreateDefault();
            p.gems = 777;
            store.Write(p);

            // Simulate a write killed midway: a temp file left behind.
            File.WriteAllText(Path.Combine(dir, FileSaveStore.TempFileName), "{\"version\":1,\"pay");

            PlayerProfile back = new FileSaveStore(dir).Read();
            Assert.AreEqual(777, back.gems);
        }

        [Test]
        public void ExistsReflectsWhetherASaveIsPresent()
        {
            var store = new FileSaveStore(dir);
            Assert.IsFalse(store.Exists());
            store.Write(PlayerProfile.CreateDefault());
            Assert.IsTrue(store.Exists());
            store.Delete();
            Assert.IsFalse(store.Exists());
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `./run-tests.sh`
Expected: FAIL — compile error, `FileSaveStore` does not exist.

- [ ] **Step 3: Write the store interface**

`Assets/Scripts/Save/ISaveStore.cs`:

```csharp
namespace SpaceshipDivine.Save
{
    public enum ReadOutcome
    {
        Fresh,                 // no save existed; a default profile was created
        Loaded,                // the live save verified and loaded
        RecoveredFromBackup,   // the live save was bad; the backup verified and loaded
        RejectedAndReset       // both were bad; a default profile was created
    }

    public interface ISaveStore
    {
        void Write(PlayerProfile profile);
        PlayerProfile Read();
        bool Exists();
        void Delete();
        ReadOutcome LastReadOutcome { get; }
    }
}
```

- [ ] **Step 4: Write the file store**

`Assets/Scripts/Save/FileSaveStore.cs`:

```csharp
using System;
using System.IO;
using UnityEngine;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// Writes the profile atomically: serialise to a temp file, flush it, then move it over
    /// the live file, promoting the previous live file to a backup first. A process killed
    /// mid-write therefore leaves either the old save or the new one, never a half-written
    /// one. Reads walk a recovery ladder rather than ever throwing at the caller.
    /// </summary>
    public class FileSaveStore : ISaveStore
    {
        public const string SaveFileName = "profile.json";
        public const string BackupFileName = "profile.backup.json";
        public const string TempFileName = "profile.tmp.json";

        private readonly string directory;

        public ReadOutcome LastReadOutcome { get; private set; } = ReadOutcome.Fresh;

        public FileSaveStore(string directory)
        {
            this.directory = directory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        public FileSaveStore() : this(Application.persistentDataPath) { }

        private string LivePath => Path.Combine(directory, SaveFileName);
        private string BackupPath => Path.Combine(directory, BackupFileName);
        private string TempPath => Path.Combine(directory, TempFileName);

        public bool Exists() => File.Exists(LivePath);

        public void Delete()
        {
            SafeDelete(LivePath);
            SafeDelete(BackupPath);
            SafeDelete(TempPath);
        }

        public void Write(PlayerProfile profile)
        {
            if (profile == null) return;

            string payload = JsonUtility.ToJson(profile);
            var envelope = new SaveEnvelope
            {
                version = SaveEnvelope.CurrentVersion,
                payload = payload,
                signature = SaveIntegrity.Sign(payload)
            };

            try
            {
                using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(JsonUtility.ToJson(envelope));
                    writer.Flush();
                    stream.Flush(true);
                }

                // Demote the current live file to backup before replacing it.
                if (File.Exists(LivePath))
                {
                    SafeDelete(BackupPath);
                    File.Move(LivePath, BackupPath);
                }

                File.Move(TempPath, LivePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Save write failed: " + e.Message);
                SafeDelete(TempPath);
            }
        }

        public PlayerProfile Read()
        {
            if (!File.Exists(LivePath) && !File.Exists(BackupPath))
            {
                LastReadOutcome = ReadOutcome.Fresh;
                return PlayerProfile.CreateDefault();
            }

            PlayerProfile fromLive = TryLoad(LivePath);
            if (fromLive != null)
            {
                LastReadOutcome = ReadOutcome.Loaded;
                return fromLive;
            }

            PlayerProfile fromBackup = TryLoad(BackupPath);
            if (fromBackup != null)
            {
                Debug.LogWarning("Live save was unreadable; recovered from backup.");
                LastReadOutcome = ReadOutcome.RecoveredFromBackup;
                return fromBackup;
            }

            Debug.LogWarning("Live save and backup both unreadable; starting a new profile.");
            LastReadOutcome = ReadOutcome.RejectedAndReset;
            return PlayerProfile.CreateDefault();
        }

        private PlayerProfile TryLoad(string path)
        {
            if (!File.Exists(path)) return null;

            try
            {
                var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
                if (envelope == null || string.IsNullOrEmpty(envelope.payload)) return null;
                if (!SaveIntegrity.Verify(envelope.payload, envelope.signature)) return null;

                return JsonUtility.FromJson<PlayerProfile>(envelope.payload);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception) { /* a locked file is not worth crashing a save over */ }
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `./run-tests.sh`
Expected: `=== total=20 passed=20 failed=0 ===`

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Save Assets/Tests
git commit -m "Salvare: scriere atomica in fisier cu rezerva si recuperare la coruptie"
```

---

### Task 5: Legacy migration with grandfathering

This is the release-blocking task. If it drops an unlock, returning players lose ships they earned or paid for.

**Files:**
- Create: `Assets/Scripts/Save/LegacySaveDataV0.cs`
- Create: `Assets/Scripts/Save/LegacyShipIdMap.cs`
- Create: `Assets/Scripts/Save/MigrationV0ToV1.cs`
- Create: `Assets/Tests/EditMode/MigrationV0ToV1Tests.cs`

**Interfaces:**
- Consumes: `PlayerProfile` (Task 3).
- Produces:
  - `LegacySaveDataV0` — an XML-deserialisable mirror of the old `SaveData`
  - `LegacyShipIdMap.IndexToId(int index) -> string` and `LegacyShipIdMap.Count`
  - `MigrationV0ToV1.FromXml(string xml) -> PlayerProfile` (returns `null` if the XML is unusable)

The legacy save stored unlocks as `bool[50]` indexed by position in `MainMenu.shipPrefabs`. That ordering is the ground truth for migration and must be transcribed exactly from the Main Menu scene's list order.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/MigrationV0ToV1Tests.cs`:

```csharp
using NUnit.Framework;

namespace SpaceshipDivine.Save.Tests
{
    public class MigrationV0ToV1Tests
    {
        /// <summary>Builds legacy XML with the given unlock flags and gem count.</summary>
        private static string LegacyXml(bool[] unlocked, int gems, int shipLevelAtIndex1 = 1)
        {
            var data = new LegacySaveDataV0 { gems = gems, hasCompletedTutorial = true };
            for (int i = 0; i < unlocked.Length && i < data.isUnlocked.Length; i++)
                data.isUnlocked[i] = unlocked[i];
            data.shipLevel[1] = shipLevelAtIndex1;

            // Ship design values that must NOT survive migration.
            data.maxHealths[0] = 12345f;
            data.priceToUnlock[0] = 99999f;

            return LegacyXmlSerializer.ToXml(data);
        }

        [Test]
        public void EveryLegacyUnlockIsPreserved()
        {
            var flags = new bool[LegacyShipIdMap.Count];
            for (int i = 0; i < flags.Length; i++) flags[i] = true;   // the old "everything unlocked" save

            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXml(flags, 1200));

            Assert.IsNotNull(p);
            for (int i = 0; i < LegacyShipIdMap.Count; i++)
                Assert.IsTrue(p.IsUnlocked(LegacyShipIdMap.IndexToId(i)),
                    "lost unlock for " + LegacyShipIdMap.IndexToId(i));
        }

        [Test]
        public void PartialUnlocksMigrateAndStartersAreAddedAsAFloor()
        {
            var flags = new bool[LegacyShipIdMap.Count];
            flags[0] = true;   // grey_byrd_tutorial
            flags[3] = true;   // the_argon (also a starter)

            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXml(flags, 500));

            // What the legacy save had.
            Assert.IsTrue(p.IsUnlocked("grey_byrd_tutorial"));
            Assert.IsTrue(p.IsUnlocked("the_argon"));
            // Plus the starter floor, so a migrated player is never worse off than a new one.
            Assert.IsTrue(p.IsUnlocked("grey_byrd"));
            Assert.IsTrue(p.IsUnlocked("apollo"));
            // And nothing beyond that.
            Assert.AreEqual(4, p.unlockedShipIds.Count);
            Assert.IsFalse(p.IsUnlocked("valiant"));
        }

        [Test]
        public void GemsAndTutorialFlagsCarryForward()
        {
            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXml(new bool[LegacyShipIdMap.Count], 4321));
            Assert.AreEqual(4321, p.gems);
            Assert.IsTrue(p.hasCompletedTutorial);
        }

        [Test]
        public void ShipLevelsCarryForward()
        {
            PlayerProfile p = MigrationV0ToV1.FromXml(
                LegacyXml(new bool[LegacyShipIdMap.Count], 0, shipLevelAtIndex1: 4));
            Assert.AreEqual(4, p.GetShipLevel(LegacyShipIdMap.IndexToId(1)));
        }

        [Test]
        public void ShipDesignDataIsDiscarded()
        {
            // PlayerProfile has no field capable of holding ship stats. This test documents
            // that intent: if someone adds one, it fails to compile here first.
            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXml(new bool[LegacyShipIdMap.Count], 0));
            Assert.IsNotNull(p);
            Assert.IsNull(typeof(PlayerProfile).GetField("maxHealths"));
            Assert.IsNull(typeof(PlayerProfile).GetField("priceToUnlock"));
        }

        [Test]
        public void CollectedBlueprintsArePreserved()
        {
            // hasBeenUnlocked[] records which blueprint pickups already happened. Losing it
            // would make already-collected blueprints drop again.
            var d = new LegacySaveDataV0 { gems = 0 };
            d.hasBeenUnlocked[4] = true;
            d.hasBeenUnlocked[11] = true;

            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXmlSerializer.ToXml(d));

            Assert.IsTrue(p.HasCollectedBlueprint(4));
            Assert.IsTrue(p.HasCollectedBlueprint(11));
            Assert.IsFalse(p.HasCollectedBlueprint(5));
        }

        [Test]
        public void RedeemableBlueprintsArePreserved()
        {
            // The legacy build overloaded priceToUnlock[i] == 1 to mean "blueprint collected,
            // ship now claimable". Discarding it would revoke a ship the player earned.
            var d = new LegacySaveDataV0 { gems = 0 };
            int vickers = -1;
            for (int i = 0; i < LegacyShipIdMap.Count; i++)
                if (LegacyShipIdMap.IndexToId(i) == "vickers") vickers = i;
            Assert.AreNotEqual(-1, vickers, "vickers missing from LegacyShipIdMap");
            d.priceToUnlock[vickers] = 1f;

            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXmlSerializer.ToXml(d));

            Assert.IsTrue(p.IsBlueprintRedeemable("vickers"));
            Assert.IsFalse(p.IsBlueprintRedeemable("warspite"));
        }

        [Test]
        public void GarbageXmlReturnsNullRatherThanThrowing()
        {
            Assert.IsNull(MigrationV0ToV1.FromXml("not xml at all"));
            Assert.IsNull(MigrationV0ToV1.FromXml(""));
            Assert.IsNull(MigrationV0ToV1.FromXml(null));
        }

        [Test]
        public void MigratedProfileNeverHasFewerUnlocksThanTheLegacySave()
        {
            var flags = new bool[LegacyShipIdMap.Count];
            flags[2] = true;
            flags[5] = true;
            flags[9] = true;

            PlayerProfile p = MigrationV0ToV1.FromXml(LegacyXml(flags, 0));

            int legacyCount = 0;
            foreach (bool f in flags) if (f) legacyCount++;
            Assert.GreaterOrEqual(p.unlockedShipIds.Count, legacyCount);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `./run-tests.sh`
Expected: FAIL — compile error, `LegacySaveDataV0` does not exist.

- [ ] **Step 3: Write the legacy model and XML helper**

`Assets/Scripts/Save/LegacySaveDataV0.cs`:

```csharp
using System;
using System.IO;
using System.Xml.Serialization;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// Mirror of the pre-v1 SaveData class, kept only so migration can read saves written by
    /// the shipped 2021 build. Field names and array sizes must match the original exactly or
    /// XmlSerializer silently drops values. Do not add to this class.
    /// </summary>
    [Serializable]
    public class LegacySaveDataV0
    {
        public bool hasCompletedTutorial = false;
        public bool hasCompletedButtonsTutorial = false;

        public int[] level = new int[2];
        public int gems, goldCoins;
        public int targetFps;
        public int qualityIndex;
        public bool controllerOn;
        public bool isMusicMuted;
        public bool isSoundMuted;

        public bool[] isUnlocked = new bool[50];
        public int totalEnemiesKilled;
        public int respawnsRemaining = 1;
        public string levelsPlayed = "";

        public int orderNumber;
        public float maxHealth;
        public float currentHealth;
        public float minHealth;
        public float fireRate;
        public int bulletsShot;
        public float spread;
        public float maxMoveSpeed;
        public float damagePerbullet;
        public int numberOfBursts;
        public float waitTimeBetweenBursts;
        public float critChance;
        public float critMultiplier;
        public bool gameHasEnded = true;
        public float timeSinceGameStarted;
        public int enemiesKilled;
        public float attackMultiplier, defenseMultiplier, speedMultiplier;
        public float damageReduction, damageMultiplier;
        public int abilityLevel;
        public string abilityName;
        public bool canBounce, canPierce, canExplode, moveSpeedOnAbility,
                    pushBackOnAbility, moveSpeedOnEnemyKill, resetCdOnKill;
        public float chanceToBlockAttack, moveSpeedIncrease, pushBackStrength,
                     moveSpeedAmount, resetCdChance, regenPerSecond, constantRegenPerSecond;
        public float gunMaxWidth;

        public int[] shipLevel = new int[50];
        public float[] priceToUnlock = new float[50];
        public float[] damageMultipliers = new float[50];
        public float[] maxHealths = new float[50];
        public float[] spreads = new float[50];
        public float[] maxMoveSpeeds = new float[50];
        public float[] critChances = new float[50];
        public float[] attackMultipliers = new float[50];
        public float[] defenseMultipliers = new float[50];
        public float[] speedMultipliers = new float[50];
        public float[] damageReductions = new float[50];
        public int numberOfShips;

        public bool[] hasBeenUnlocked = new bool[100];

        public int[] upgradesIndex = new int[5];
        public int numberOfUpgrades = 0;
    }

    public static class LegacyXmlSerializer
    {
        public static string ToXml(LegacySaveDataV0 data)
        {
            var xml = new XmlSerializer(typeof(LegacySaveDataV0));
            var writer = new StringWriter();
            xml.Serialize(writer, data);
            return writer.ToString();
        }

        public static LegacySaveDataV0 FromXml(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            try
            {
                var serializer = new XmlSerializer(typeof(LegacySaveDataV0));
                return (LegacySaveDataV0)serializer.Deserialize(new StringReader(xml));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
```

The legacy class was named `SaveData`, and `XmlSerializer` writes the type name as the root element. Renaming it to `LegacySaveDataV0` would change that root element and break deserialisation of real saves — so the migration reads with an explicit root override. That is handled in Step 5.

- [ ] **Step 4: Write the ship ID map**

`Assets/Scripts/Save/LegacyShipIdMap.cs`:

```csharp
namespace SpaceshipDivine.Save
{
    /// <summary>
    /// The legacy save stored unlocks as bool[50] indexed by position in
    /// MainMenu.shipPrefabs. This transcribes that ordering so migration can turn indices
    /// into stable IDs. THE ORDER HERE IS GROUND TRUTH AND MUST NOT BE CHANGED — it is the
    /// only thing tying a 2021 save to a ship.
    /// </summary>
    public static class LegacyShipIdMap
    {
        private static readonly string[] Ids =
        {
            "grey_byrd_tutorial",  // 0
            "grey_byrd",           // 1
            "apollo",              // 2
            "the_argon",           // 3
            "razor",               // 4
            "hot_talon",           // 5
            "white_ripper",        // 6
            "the_reaper",          // 7
            "lunar_hunter",        // 8
            "valiant",             // 9
            "bat_oh_no",           // 10
            "vickers",             // 11
            "warspite",            // 12
            "bubu"                 // 13
        };

        public static int Count => Ids.Length;

        public static string IndexToId(int index)
        {
            if (index < 0 || index >= Ids.Length) return null;
            return Ids[index];
        }
    }
}
```

**Verification required before this task is complete:** open `Assets/Scenes/Main Menu.unity`, find the `MainMenu` component's `shipPrefabs` list, and confirm this ordering matches element-for-element. If it does not, correct the array above — the tests will still pass either way, because they test the mapping's self-consistency, not its correspondence to the scene. Getting this wrong silently gives returning players the wrong ships.

- [ ] **Step 5: Write the migration**

`Assets/Scripts/Save/MigrationV0ToV1.cs`:

```csharp
using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// Turns a legacy XML save into a v1 PlayerProfile. Unlocks are only ever added, never
    /// removed: a returning player must not lose a ship because the gate turned on.
    /// </summary>
    public static class MigrationV0ToV1
    {
        public const string LegacyPlayerPrefsKey = "save";

        public static PlayerProfile FromXml(string xml)
        {
            LegacySaveDataV0 legacy = Deserialize(xml);
            if (legacy == null) return null;

            // Start from an empty profile, not CreateDefault(), so the result reflects the
            // legacy save exactly. Starters are added afterwards as a floor.
            var p = new PlayerProfile
            {
                gems = legacy.gems,
                goldCoins = legacy.goldCoins,
                hasCompletedTutorial = legacy.hasCompletedTutorial,
                hasCompletedButtonsTutorial = legacy.hasCompletedButtonsTutorial,
                levelsPlayed = legacy.levelsPlayed ?? ""
            };

            p.settings.qualityIndex = legacy.qualityIndex;
            p.settings.targetFps = legacy.targetFps == 0 ? 60 : legacy.targetFps;
            p.settings.isMusicMuted = legacy.isMusicMuted;
            p.settings.isSoundMuted = legacy.isSoundMuted;
            p.settings.controllerOn = legacy.controllerOn;

            p.stats.totalEnemiesKilled = legacy.totalEnemiesKilled;

            for (int i = 0; i < LegacyShipIdMap.Count && i < legacy.isUnlocked.Length; i++)
            {
                if (!legacy.isUnlocked[i]) continue;
                p.Unlock(LegacyShipIdMap.IndexToId(i));
            }

            for (int i = 0; i < LegacyShipIdMap.Count && i < legacy.shipLevel.Length; i++)
            {
                if (legacy.shipLevel[i] <= 1) continue;
                p.SetShipLevel(LegacyShipIdMap.IndexToId(i), legacy.shipLevel[i]);
            }

            // Blueprint pickups, so already-collected blueprints do not drop again.
            for (int i = 0; i < legacy.hasBeenUnlocked.Length; i++)
                if (legacy.hasBeenUnlocked[i])
                    p.CollectBlueprint(i);

            // The legacy build overloaded priceToUnlock[i] == 1 to mean "blueprint collected,
            // this ship is now claimable for free in the menu". That is earned progress, so
            // it migrates to an explicit flag. Only in-game-unlockable ships used this; gem
            // ships carry a real price here, which is why the comparison is exact.
            for (int i = 0; i < LegacyShipIdMap.Count && i < legacy.priceToUnlock.Length; i++)
            {
                if (Math.Abs(legacy.priceToUnlock[i] - 1f) > 0.0001f) continue;
                p.MarkBlueprintRedeemable(LegacyShipIdMap.IndexToId(i));
            }

            // Floor: never end up with fewer ships than a brand-new player gets.
            foreach (string id in PlayerProfile.StarterShipIds)
                p.Unlock(id);

            // Ship design arrays (maxHealths, spreads, damageMultipliers, ...) are
            // deliberately not read. They now live on the Spaceship ScriptableObjects, which
            // is what makes post-launch rebalancing reach existing players.

            return p;
        }

        private static LegacySaveDataV0 Deserialize(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            try
            {
                // Real saves have <SaveData> as the root element, because that was the class
                // name when they were written. Override the root so the renamed mirror class
                // still deserialises them.
                var root = new XmlRootAttribute("SaveData");
                var serializer = new XmlSerializer(typeof(LegacySaveDataV0), root);
                return (LegacySaveDataV0)serializer.Deserialize(new StringReader(xml));
            }
            catch (Exception)
            {
                // Fall back to the natural root, which is what LegacyXmlSerializer.ToXml writes.
                try
                {
                    var serializer = new XmlSerializer(typeof(LegacySaveDataV0));
                    return (LegacySaveDataV0)serializer.Deserialize(new StringReader(xml));
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Legacy save could not be read: " + e.Message);
                    return null;
                }
            }
        }
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `./run-tests.sh`
Expected: `=== total=29 passed=29 failed=0 ===`

- [ ] **Step 7: Verify the ship order against the scene**

```bash
grep -n -A 20 "shipPrefabs:" "Assets/Scenes/Main Menu.unity" | head -30
```

Cross-reference each GUID against `Assets/Prefabs/Spaceships/*.asset.meta` and confirm the order in `LegacyShipIdMap`. Correct the array if it differs, then re-run `./run-tests.sh`.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Save Assets/Tests
git commit -m "Salvare: migrare v0->v1 cu pastrarea navelor deja deblocate"
```

---

### Task 6: SaveService facade

**Files:**
- Create: `Assets/Scripts/Save/SaveService.cs`
- Create: `Assets/Tests/EditMode/SaveServiceTests.cs`

**Interfaces:**
- Consumes: `ISaveStore`, `FileSaveStore` (Task 4); `MigrationV0ToV1` (Task 5).
- Produces:
  - `SaveService(ISaveStore store, Func<string> legacyXmlProvider, Action<string> legacyCleared)`
  - `SaveService.Profile` → `PlayerProfile`
  - `SaveService.Run` → `RunState`
  - `SaveService.Load()`, `SaveService.Save()`
  - `SaveService.MigratedThisLoad` → `bool`

`legacyXmlProvider` is injected so tests do not touch `PlayerPrefs`.

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/SaveServiceTests.cs`:

```csharp
using System.IO;
using NUnit.Framework;

namespace SpaceshipDivine.Save.Tests
{
    public class SaveServiceTests
    {
        private string dir;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "sd_svc_tests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        private static string LegacyXmlWithEverythingUnlocked()
        {
            var d = new LegacySaveDataV0 { gems = 2500, hasCompletedTutorial = true };
            for (int i = 0; i < LegacyShipIdMap.Count; i++) d.isUnlocked[i] = true;
            return LegacyXmlSerializer.ToXml(d);
        }

        [Test]
        public void FirstLaunchWithNoSaveAndNoLegacyCreatesDefault()
        {
            var svc = new SaveService(new FileSaveStore(dir), () => null, _ => { });
            svc.Load();

            Assert.IsFalse(svc.MigratedThisLoad);
            Assert.AreEqual(350, svc.Profile.gems);
            Assert.AreEqual(3, svc.Profile.unlockedShipIds.Count);
        }

        [Test]
        public void LegacySaveIsMigratedOnFirstLoad()
        {
            var svc = new SaveService(new FileSaveStore(dir), LegacyXmlWithEverythingUnlocked, _ => { });
            svc.Load();

            Assert.IsTrue(svc.MigratedThisLoad);
            Assert.AreEqual(2500, svc.Profile.gems);
            Assert.AreEqual(LegacyShipIdMap.Count, svc.Profile.unlockedShipIds.Count);
        }

        [Test]
        public void MigrationHappensOnceThenReadsTheV1File()
        {
            int legacyReads = 0;
            string legacy = LegacyXmlWithEverythingUnlocked();

            var first = new SaveService(new FileSaveStore(dir),
                () => { legacyReads++; return legacy; }, _ => { });
            first.Load();
            Assert.IsTrue(first.MigratedThisLoad);

            // Second launch: a v1 file now exists, so the legacy provider must not be consulted.
            var second = new SaveService(new FileSaveStore(dir),
                () => { legacyReads++; return legacy; }, _ => { });
            second.Load();

            Assert.IsFalse(second.MigratedThisLoad);
            Assert.AreEqual(1, legacyReads);
            Assert.AreEqual(LegacyShipIdMap.Count, second.Profile.unlockedShipIds.Count);
        }

        [Test]
        public void SaveThenLoadPreservesProfileChanges()
        {
            var svc = new SaveService(new FileSaveStore(dir), () => null, _ => { });
            svc.Load();
            svc.Profile.gems = 8888;
            svc.Profile.Unlock("valiant");
            svc.Save();

            var again = new SaveService(new FileSaveStore(dir), () => null, _ => { });
            again.Load();

            Assert.AreEqual(8888, again.Profile.gems);
            Assert.IsTrue(again.Profile.IsUnlocked("valiant"));
        }

        [Test]
        public void RunStateIsAvailableAndClearable()
        {
            var svc = new SaveService(new FileSaveStore(dir), () => null, _ => { });
            svc.Load();
            svc.Run.selectedShipId = "razor";
            svc.Run.runInProgress = true;
            svc.Run.Clear();

            Assert.IsFalse(svc.Run.runInProgress);
            Assert.AreEqual("", svc.Run.selectedShipId);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `./run-tests.sh`
Expected: FAIL — compile error, `SaveService` does not exist.

- [ ] **Step 3: Write the service**

`Assets/Scripts/Save/SaveService.cs`:

```csharp
using System;
using UnityEngine;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// The single entry point the game uses for persistence. Owns the profile and the run
    /// state, and performs the one-time legacy migration on first load.
    /// </summary>
    public class SaveService
    {
        private readonly ISaveStore store;
        private readonly Func<string> legacyXmlProvider;
        private readonly Action<string> legacyCleared;

        public PlayerProfile Profile { get; private set; }
        public RunState Run { get; private set; } = new RunState();
        public bool MigratedThisLoad { get; private set; }

        public SaveService(ISaveStore store, Func<string> legacyXmlProvider, Action<string> legacyCleared)
        {
            this.store = store;
            this.legacyXmlProvider = legacyXmlProvider;
            this.legacyCleared = legacyCleared;
        }

        public void Load()
        {
            MigratedThisLoad = false;

            if (store.Exists())
            {
                Profile = store.Read();
                return;
            }

            string legacyXml = legacyXmlProvider != null ? legacyXmlProvider() : null;
            if (!string.IsNullOrEmpty(legacyXml))
            {
                PlayerProfile migrated = MigrationV0ToV1.FromXml(legacyXml);
                if (migrated != null)
                {
                    Profile = migrated;
                    MigratedThisLoad = true;
                    store.Write(Profile);
                    // The legacy PlayerPrefs blob is deliberately left in place for one
                    // release, as an escape hatch if migration turns out to be wrong.
                    legacyCleared?.Invoke(MigrationV0ToV1.LegacyPlayerPrefsKey);
                    Debug.Log("Migrated legacy save to v1: " +
                              Profile.unlockedShipIds.Count + " ships preserved.");
                    return;
                }
                Debug.LogWarning("Legacy save present but unreadable; starting fresh.");
            }

            Profile = store.Read();   // no file and no legacy -> default profile
        }

        public void Save()
        {
            if (Profile == null) return;
            store.Write(Profile);
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `./run-tests.sh`
Expected: `=== total=34 passed=34 failed=0 ===`

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Save Assets/Tests
git commit -m "Salvare: fatada SaveService cu migrare unica la prima pornire"
```

---

### Task 7: Headless asset automation harness + stable ship IDs

Every ship-data change in this plan goes through a Unity Editor script run headless, not through
`sed` on the `.asset` YAML. `SerializedObject` cannot emit malformed YAML, each entry point
verifies its own result, and a mismatch exits non-zero so the step can be trusted from a script.

**Files:**
- Create: `Assets/Editor/SdAutomation.cs`
- Modify: `Assets/Scripts/Spaceships/Spaceship.cs`
- Modify (via the tool, not by hand): all 14 files in `Assets/Prefabs/Spaceships/*.asset`

**Interfaces:**
- Consumes: the ID vocabulary from `LegacyShipIdMap` (Task 5).
- Produces:
  - `Spaceship.shipId` — `public string`
  - `SdAutomation.Ships` — the single source of truth table (id, price, unlocked) for Tasks 8 and 9
  - `SdAutomation.AssignShipIds()` — batchmode entry point
  - `SdAutomation.VerifyShipIds()` — batchmode entry point, exits 1 on any mismatch
  - `SdAutomation.LoadAllShips()` — helper reused by later entry points

- [ ] **Step 1: Add the shipId field**

In `Assets/Scripts/Spaceships/Spaceship.cs`, directly under the `[Header("General")]` line, add:

```csharp
    [Tooltip("Stable identifier used by the save system. NEVER change this for a shipped " +
             "ship — the save file references it, and changing it revokes the unlock.")]
    public string shipId;
```

- [ ] **Step 2: Write the automation harness**

`Assets/Editor/SdAutomation.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Headless asset surgery for ship ScriptableObjects.
///
/// Run:
///   Unity -batchmode -quit -projectPath . -executeMethod SdAutomation.AssignShipIds -logFile -
///
/// Every entry point verifies its own result and calls EditorApplication.Exit with a non-zero
/// code on mismatch, so a caller can trust the exit status. Editing the YAML directly with sed
/// is forbidden: it cannot validate, and a bad replacement silently corrupts an asset.
/// </summary>
public static class SdAutomation
{
    public class ShipSpec
    {
        public string assetName;
        public string shipId;
        public float price;     // priceToUnlock
        public bool unlocked;   // isUnlocked on a fresh install
    }

    /// <summary>
    /// Single source of truth for ship data. Order matches MainMenu.shipPrefabs, which is also
    /// the order LegacyShipIdMap depends on for save migration — do not reorder.
    ///
    /// price for grey_byrd_tutorial / grey_byrd is a real price of 0 (free starters).
    /// price for vickers / warspite / bubu is NOT a price: priceToUnlock is overloaded as a
    /// "blueprint collected" boolean for in-game unlock ships, so 0 means "not yet collected",
    /// which is the only correct value for a shipped asset.
    /// price for bat_oh_no is a real-money price in EUR, not gems.
    /// </summary>
    public static readonly ShipSpec[] Ships =
    {
        new ShipSpec { assetName = "Grey Byrd Tutorial", shipId = "grey_byrd_tutorial", price = 0f,    unlocked = true  },
        new ShipSpec { assetName = "Grey Byrd",          shipId = "grey_byrd",          price = 0f,    unlocked = true  },
        new ShipSpec { assetName = "Apollo",             shipId = "apollo",             price = 500f,  unlocked = true  },
        new ShipSpec { assetName = "The Argon",          shipId = "the_argon",          price = 650f,  unlocked = true  },
        new ShipSpec { assetName = "Razor",              shipId = "razor",              price = 1000f, unlocked = false },
        new ShipSpec { assetName = "Hot Talon",          shipId = "hot_talon",          price = 2000f, unlocked = false },
        new ShipSpec { assetName = "White Ripper",       shipId = "white_ripper",       price = 2250f, unlocked = false },
        new ShipSpec { assetName = "The Reaper",         shipId = "the_reaper",         price = 2600f, unlocked = false },
        new ShipSpec { assetName = "Lunar Hunter",       shipId = "lunar_hunter",       price = 3900f, unlocked = false },
        new ShipSpec { assetName = "Valiant",            shipId = "valiant",            price = 4500f, unlocked = false },
        new ShipSpec { assetName = "Bat-Oh-No",          shipId = "bat_oh_no",          price = 9.99f, unlocked = false },
        new ShipSpec { assetName = "Vickers",            shipId = "vickers",            price = 0f,    unlocked = false },
        new ShipSpec { assetName = "Warspite",           shipId = "warspite",           price = 0f,    unlocked = false },
        new ShipSpec { assetName = "Bubu",               shipId = "bubu",               price = 0f,    unlocked = false },
    };

    /// <summary>Loads every Spaceship asset, keyed by asset file name.</summary>
    public static Dictionary<string, Spaceship> LoadAllShips()
    {
        var byName = new Dictionary<string, Spaceship>();
        foreach (string guid in AssetDatabase.FindAssets("t:Spaceship"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/Prefabs/Spaceships/")) continue;

            var ship = AssetDatabase.LoadAssetAtPath<Spaceship>(path);
            if (ship == null) continue;

            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            byName[name] = ship;
        }
        return byName;
    }

    /// <summary>Resolves the spec table against the assets on disk, reporting anything missing.</summary>
    private static bool Resolve(out Dictionary<string, Spaceship> ships, out List<string> problems)
    {
        ships = LoadAllShips();
        problems = new List<string>();

        foreach (ShipSpec spec in Ships)
            if (!ships.ContainsKey(spec.assetName))
                problems.Add("MISSING ASSET: " + spec.assetName);

        var known = new HashSet<string>(Ships.Select(s => s.assetName));
        foreach (string name in ships.Keys)
            if (!known.Contains(name))
                problems.Add("UNKNOWN ASSET not in spec table: " + name);

        return problems.Count == 0;
    }

    private static void Finish(string label, List<string> problems)
    {
        if (problems.Count == 0)
        {
            Debug.Log(label + ": OK");
            EditorApplication.Exit(0);
            return;
        }
        foreach (string p in problems) Debug.LogError(label + ": " + p);
        EditorApplication.Exit(1);
    }

    // ---- Task 7 -----------------------------------------------------------------

    public static void AssignShipIds()
    {
        if (!Resolve(out var ships, out var problems)) { Finish("AssignShipIds", problems); return; }

        foreach (ShipSpec spec in Ships)
        {
            var so = new SerializedObject(ships[spec.assetName]);
            so.FindProperty("shipId").stringValue = spec.shipId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();

        VerifyIdsInto(problems, LoadAllShips());
        Finish("AssignShipIds", problems);
    }

    public static void VerifyShipIds()
    {
        if (!Resolve(out var ships, out var problems)) { Finish("VerifyShipIds", problems); return; }
        VerifyIdsInto(problems, ships);
        Finish("VerifyShipIds", problems);
    }

    private static void VerifyIdsInto(List<string> problems, Dictionary<string, Spaceship> ships)
    {
        var seen = new HashSet<string>();
        foreach (ShipSpec spec in Ships)
        {
            if (!ships.TryGetValue(spec.assetName, out Spaceship ship)) continue;

            if (ship.shipId != spec.shipId)
                problems.Add(spec.assetName + ": shipId is '" + ship.shipId + "', expected '" + spec.shipId + "'");
            else if (!seen.Add(ship.shipId))
                problems.Add(spec.assetName + ": duplicate shipId '" + ship.shipId + "'");

            Debug.Log(spec.assetName.PadRight(22) + " shipId=" + ship.shipId);
        }
    }
}
```

- [ ] **Step 3: Assign the IDs headlessly**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
pgrep -fl "Unity.app/Contents/MacOS/Unity" && echo "KILL THE EDITOR FIRST" && exit 1
"/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$(pwd)" \
  -executeMethod SdAutomation.AssignShipIds -logFile - 2>&1 | tail -30
echo "EXIT=$?"
```

Expected: 14 lines each showing a non-empty `shipId`, then `AssignShipIds: OK`, `EXIT=0`.

- [ ] **Step 4: Verify independently**

```bash
"/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$(pwd)" \
  -executeMethod SdAutomation.VerifyShipIds -logFile - 2>&1 | tail -20
echo "EXIT=$?"
```

Expected: `VerifyShipIds: OK`, `EXIT=0`.

Also confirm the YAML is well-formed — this is what the old `sed` approach could silently break:

```bash
cd "Assets/Prefabs/Spaceships"
for f in *.asset; do printf "%-22s" "${f%.asset}"; grep -m1 "^  shipId:" "$f" || echo "*** MISSING ***"; done
grep -l '\\n' *.asset && echo "*** LITERAL BACKSLASH-N FOUND — CORRUPT ***" || echo "no literal \\n — YAML clean"
cd -
```

- [ ] **Step 5: Confirm the test suite still passes**

Run: `./run-tests.sh`
Expected: `=== total=34 passed=34 failed=0 ===`

- [ ] **Step 6: Commit**

```bash
git add Assets/Editor/SdAutomation.cs Assets/Editor/SdAutomation.cs.meta \
        Assets/Scripts/Spaceships/Spaceship.cs "Assets/Prefabs/Spaceships"
git commit -m "Nave: unealta de automatizare in editor si identificator stabil shipId"
```

---

### Task 8: Ship pricing — 35% reduction

Applied before the gate is switched on, so the gate is never live at the old prices.

**Files:**
- Modify: `Assets/Editor/SdAutomation.cs` (add one entry point)
- Modify (via the tool): 9 files in `Assets/Prefabs/Spaceships/*.asset`

**Interfaces:**
- Consumes: `SdAutomation.Ships`, `SdAutomation.LoadAllShips()`, `Resolve`, `Finish` (Task 7).
- Produces: `SdAutomation.ApplyShipPrices()`, `SdAutomation.VerifyShipPrices()`.

The prices are already in the `Ships` table from Task 7. This task applies them.

| Ship | Old | New | Cut |
|---|---:|---:|---:|
| Apollo | 750 | **500** | 33.3% |
| The Argon | 1000 | **650** | 35.0% |
| Razor | 1500 | **1000** | 33.3% |
| Hot Talon | 3000 | **2000** | 33.3% |
| White Ripper | 3500 | **2250** | 35.7% |
| The Reaper | 4000 | **2600** | 35.0% |
| Lunar Hunter | 6000 | **3900** | 35.0% |
| Valiant | 7000 | **4500** | 35.7% |

Mean reduction 34.6%. `Bat-Oh-No` stays at 9.99 (real money, not gems).

**Bubu also changes here, and it is not a price change.** `Bubu.asset` ships with
`priceToUnlock: 1`, which in this codebase means "blueprint already collected" — so Bubu is
claimable free from a brand-new save without ever finding its rare chest drop. The table sets it
to 0, matching Vickers and Warspite.

- [ ] **Step 1: Add the price entry points**

Append inside the `SdAutomation` class, after `VerifyIdsInto`:

```csharp
    // ---- Task 8 -----------------------------------------------------------------

    public static void ApplyShipPrices()
    {
        if (!Resolve(out var ships, out var problems)) { Finish("ApplyShipPrices", problems); return; }

        foreach (ShipSpec spec in Ships)
        {
            var so = new SerializedObject(ships[spec.assetName]);
            so.FindProperty("priceToUnlock").floatValue = spec.price;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();

        VerifyPricesInto(problems, LoadAllShips());
        Finish("ApplyShipPrices", problems);
    }

    public static void VerifyShipPrices()
    {
        if (!Resolve(out var ships, out var problems)) { Finish("VerifyShipPrices", problems); return; }
        VerifyPricesInto(problems, ships);
        Finish("VerifyShipPrices", problems);
    }

    private static void VerifyPricesInto(List<string> problems, Dictionary<string, Spaceship> ships)
    {
        foreach (ShipSpec spec in Ships)
        {
            if (!ships.TryGetValue(spec.assetName, out Spaceship ship)) continue;

            if (Mathf.Abs(ship.priceToUnlock - spec.price) > 0.001f)
                problems.Add(spec.assetName + ": priceToUnlock is " + ship.priceToUnlock +
                             ", expected " + spec.price);

            Debug.Log(spec.assetName.PadRight(22) + " priceToUnlock=" + ship.priceToUnlock);
        }
    }
```

- [ ] **Step 2: Apply and verify**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
U="/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity"
"$U" -batchmode -quit -projectPath "$(pwd)" -executeMethod SdAutomation.ApplyShipPrices -logFile - 2>&1 | tail -25
echo "EXIT=$?"
"$U" -batchmode -quit -projectPath "$(pwd)" -executeMethod SdAutomation.VerifyShipPrices -logFile - 2>&1 | tail -20
echo "EXIT=$?"
```

Expected: both `OK` with `EXIT=0`, and the logged table matching the table above with Bubu at 0.

- [ ] **Step 3: Confirm the test suite still passes**

Run: `./run-tests.sh`
Expected: `=== total=34 passed=34 failed=0 ===`

- [ ] **Step 4: Commit**

```bash
git add Assets/Editor/SdAutomation.cs "Assets/Prefabs/Spaceships"
git commit -m "Economie: preturi de deblocare reduse cu aproximativ 35%; Bubu fara plan precolectat"
```

---

### Task 9: Activate the progression gate

**Files:**
- Modify: `Assets/Editor/SdAutomation.cs` (add one entry point)
- Modify (via the tool): 10 files in `Assets/Prefabs/Spaceships/*.asset`
- Modify: `Assets/Scripts/Object Managers/DataHolder.cs` (the `Load()` blanket-unlock loop)

**Interfaces:**
- Consumes: `SdAutomation.Ships` (Task 7); `Spaceship.shipId` (Task 7); `PlayerProfile.StarterShipIds` (Task 3).
- Produces: `SdAutomation.ApplyUnlockGate()`, `SdAutomation.VerifyUnlockGate()`.

Exactly four assets stay unlocked: `Grey Byrd`, `Grey Byrd Tutorial`, `Apollo`, `The Argon`.
The tutorial ship is included because the tutorial forces it; a new player who cannot select it
is soft-locked. It is deliberately not in `PlayerProfile.StarterShipIds`, which means "ships a
player may choose".

- [ ] **Step 1: Add the gate entry points**

Append inside the `SdAutomation` class:

```csharp
    // ---- Task 9 -----------------------------------------------------------------

    public static void ApplyUnlockGate()
    {
        if (!Resolve(out var ships, out var problems)) { Finish("ApplyUnlockGate", problems); return; }

        foreach (ShipSpec spec in Ships)
        {
            var so = new SerializedObject(ships[spec.assetName]);
            so.FindProperty("isUnlocked").boolValue = spec.unlocked;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();

        VerifyGateInto(problems, LoadAllShips());
        Finish("ApplyUnlockGate", problems);
    }

    public static void VerifyUnlockGate()
    {
        if (!Resolve(out var ships, out var problems)) { Finish("VerifyUnlockGate", problems); return; }
        VerifyGateInto(problems, ships);
        Finish("VerifyUnlockGate", problems);
    }

    private static void VerifyGateInto(List<string> problems, Dictionary<string, Spaceship> ships)
    {
        int unlockedCount = 0;
        foreach (ShipSpec spec in Ships)
        {
            if (!ships.TryGetValue(spec.assetName, out Spaceship ship)) continue;

            if (ship.isUnlocked != spec.unlocked)
                problems.Add(spec.assetName + ": isUnlocked is " + ship.isUnlocked +
                             ", expected " + spec.unlocked);
            if (ship.isUnlocked) unlockedCount++;

            Debug.Log(spec.assetName.PadRight(22) + " isUnlocked=" + ship.isUnlocked);
        }

        if (unlockedCount != 4)
            problems.Add("expected exactly 4 unlocked ships on a fresh install, found " + unlockedCount);
    }
```

- [ ] **Step 2: Apply and verify**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
U="/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity"
"$U" -batchmode -quit -projectPath "$(pwd)" -executeMethod SdAutomation.ApplyUnlockGate -logFile - 2>&1 | tail -25
echo "EXIT=$?"
"$U" -batchmode -quit -projectPath "$(pwd)" -executeMethod SdAutomation.VerifyUnlockGate -logFile - 2>&1 | tail -20
echo "EXIT=$?"
```

Expected: both `OK` with `EXIT=0`; `isUnlocked=True` on exactly Grey Byrd, Grey Byrd Tutorial,
Apollo and The Argon.

- [ ] **Step 3: Remove the blanket unlock loop**

In `Assets/Scripts/Object Managers/DataHolder.cs`, inside `Load()`, replace:

```csharp
                dataSaved = new SaveData();
                for(int i = 0; i < mainMenu.shipPrefabs.Count; i++)
                    dataSaved.isUnlocked[i] = true;
```

with:

```csharp
                dataSaved = new SaveData();
                // Only the starter ships begin unlocked. Everything else is earned or bought.
                // Existing players keep what they had — see MigrationV0ToV1.
                // grey_byrd_tutorial is included because the tutorial forces that ship; a new
                // player who cannot select it is soft-locked.
                for (int i = 0; i < mainMenu.shipPrefabs.Count; i++)
                {
                    string id = mainMenu.shipPrefabs[i].shipId;
                    dataSaved.isUnlocked[i] = System.Array.IndexOf(
                        SpaceshipDivine.Save.PlayerProfile.StarterShipIds, id) >= 0
                        || id == "grey_byrd_tutorial";
                }
```

- [ ] **Step 4: Confirm the test suite still passes**

Run: `./run-tests.sh`
Expected: `=== total=34 passed=34 failed=0 ===`

- [ ] **Step 5: Commit**

```bash
git add Assets/Editor/SdAutomation.cs "Assets/Prefabs/Spaceships" "Assets/Scripts/Object Managers/DataHolder.cs"
git commit -m "Progresie: activarea blocarii navelor, doar cele initiale deblocate"
```

---

### Task 10: Close the real-money free-ship loophole

`MainMenu.UnlockShip()`'s `canBeBoughtWithCurrency` branch sets `isUnlocked = true` and saves,
with no purchase call anywhere. `Bat-Oh-No` is a €9.99 product handed out on tap.

**Do not touch the `canBeUnlockedInGame` branch.** It looks like the same bug and is not:
`priceToUnlock != 0` there means *"the player already picked this ship's blueprint up in
game"*, written by `Blueprint.UnlockCollectible()`. That branch is blueprint redemption
working as designed. Removing it would strand every blueprint a player has earned.

**Files:**
- Modify: `Assets/Scripts/Main Menu/MainMenu.cs:594-600` (the `canBeBoughtWithCurrency` branch only)
- Modify: `Assets/Scripts/Object Managers/IAPManager.cs`

**Interfaces:**
- Consumes: `Spaceship.shipId` (Task 7).
- Produces:
  - `MainMenu.GrantShipAfterPurchase(string shipId)` — the callback the IAP layer will invoke in a later plan
  - `IAPManager.BuyShip(string shipId)` — seam, fails closed until the IAP plan wires it

- [ ] **Step 1: Replace only the real-money branch**

In `Assets/Scripts/Main Menu/MainMenu.cs`, replace exactly this:

```csharp
        } else if(shipPrefabs[infoMenuTrackedShip].canBeBoughtWithCurrency)
        {
            SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.UISounds[0]);
            shipPrefabs[infoMenuTrackedShip].isUnlocked = true;
            data.dataSaved.isUnlocked[infoMenuTrackedShip] = true;
            data.Save();
            data.Load();
        } else if(shipPrefabs[infoMenuTrackedShip].canBeUnlockedInGame)
```

with:

```csharp
        } else if(shipPrefabs[infoMenuTrackedShip].canBeBoughtWithCurrency)
        {
            // Real-money ship. The grant happens only in GrantShipAfterPurchase, called from
            // IAPManager.ProcessPurchase once the store confirms. Never grant here.
            IAPManager.instance.BuyShip(shipPrefabs[infoMenuTrackedShip].shipId);
        } else if(shipPrefabs[infoMenuTrackedShip].canBeUnlockedInGame)
```

Everything from `} else if(shipPrefabs[infoMenuTrackedShip].canBeUnlockedInGame)` onward stays
byte-for-byte as it is.

- [ ] **Step 2: Add the purchase-completion callback**

Add to `MainMenu`, immediately after `UnlockShip()`:

```csharp
    /// <summary>
    /// Called by IAPManager once a store purchase is confirmed. This is the ONLY path that
    /// may grant a real-money ship.
    /// </summary>
    public void GrantShipAfterPurchase(string shipId)
    {
        for (int i = 0; i < shipPrefabs.Count; i++)
        {
            if (shipPrefabs[i].shipId != shipId) continue;

            SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.UISounds[0]);
            data.dataSaved.isUnlocked[i] = true;
            data.Save();
            data.Load();
            RefreshData();
            ShipsInformation(i);
            return;
        }
        Debug.LogWarning("GrantShipAfterPurchase: unknown shipId " + shipId);
    }
```

- [ ] **Step 3: Add the IAP stub this calls**

The full IAP implementation belongs to a later plan; this task only needs the seam. Add to `Assets/Scripts/Object Managers/IAPManager.cs`, after `LargeGems()`:

```csharp
    /// <summary>
    /// Starts a real-money ship purchase. Wired to store products in the IAP hardening plan;
    /// until then it fails closed and grants nothing.
    /// </summary>
    public void BuyShip(string shipId)
    {
        Debug.LogWarning("BuyShip not yet wired to a store product: " + shipId);
    }
```

- [ ] **Step 4: Confirm compilation and that tests still pass**

Run: `./run-tests.sh`
Expected: `=== total=34 passed=34 failed=0 ===`

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scripts/Main Menu/MainMenu.cs" "Assets/Scripts/Object Managers/IAPManager.cs"
git commit -m "Achizitii: navele contra cost nu se mai acorda gratuit la apasare"
```

---

### Task 11: Verify the blueprint path and document the overloading

The in-game unlock system already works end to end. Bubu's pre-collected blueprint flag is
fixed in Task 8 (it lives in the `SdAutomation.Ships` table). This task confirms the three
unlock conditions are actually reachable, and leaves a note so the `priceToUnlock` overloading
is retired deliberately rather than discovered again.

**Vickers** (`This blueprint is obtained by destroying the first boss`) — `Enemy.cs:384`
sets `GameManager.shouldDropBossBlueprint`, `RoomChest` spawns `bossBlueprint`. Works.
**Warspite** (`...destroying 1500 enemy spaceships`) — `Enemy.cs:396` drops blueprints at
`DataHolder.killMilestones`. Works.
**Bubu** (`...a rare drop from a room chest`) — `RoomChest.randomDrops` / `dropChances`. Works.

No tracker is needed and none should be written.

**Files:**
- Modify: `Assets/Scripts/Collectibles/Blueprint.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Verify the three unlock conditions are actually reachable**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
grep -n "shouldDropBossBlueprint" Assets/Scripts/Enemies/Enemy.cs Assets/Scripts/Object\ Managers/RoomChest.cs
grep -n "killMilestones" Assets/Scripts/Enemies/Enemy.cs
grep -n "randomDrops\|dropChances" Assets/Scripts/Object\ Managers/RoomChest.cs | head -4
```

Expected: boss-blueprint flag set in `Enemy.cs` and consumed in `RoomChest.cs`; kill-milestone
drop loop present; random drop lists present. If any is missing, stop and report — the
instruction text would then be promising something the game cannot deliver.

- [ ] **Step 2: Document the overloading at its source**

In `Assets/Scripts/Collectibles/Blueprint.cs`, replace the body of `UnlockCollectible()`:

```csharp
    public override void UnlockCollectible()
    {
        DataHolder.instance.dataSaved.priceToUnlock[unlockableShip.orderNumber] = 1;
        unlockableShip.priceToUnlock = 1;
        DataHolder.instance.dataSaved.hasBeenUnlocked[dropIndex] = true;
        gameObject.SetActive(false);
    }
```

with:

```csharp
    // NOTE: priceToUnlock is overloaded here as a boolean. Setting it to 1 does not mean the
    // ship costs 1 gem — it means "blueprint collected, this ship is now claimable for free
    // in the menu", which MainMenu.UnlockShip() reads in its canBeUnlockedInGame branch.
    // PlayerProfile.MarkBlueprintRedeemable() replaces this once DataHolder is cut over to
    // SaveService; until then the two must agree, so do not change one without the other.
    public override void UnlockCollectible()
    {
        DataHolder.instance.dataSaved.priceToUnlock[unlockableShip.orderNumber] = 1;
        unlockableShip.priceToUnlock = 1;
        DataHolder.instance.dataSaved.hasBeenUnlocked[dropIndex] = true;
        gameObject.SetActive(false);
    }
```

- [ ] **Step 3: Confirm nothing broke**

Run: `./run-tests.sh`
Expected: `=== total=34 passed=34 failed=0 ===`

- [ ] **Step 4: Verify Bubu's flag was already corrected in Task 8**

```bash
cd "Assets/Prefabs/Spaceships"
for f in Bubu Vickers Warspite; do printf "%-10s" "$f"; grep -m1 "^  priceToUnlock:" "$f.asset"; done
cd -
```

Expected: all three report `priceToUnlock: 0`. If Bubu is not 0, Task 8 did not apply — stop
and report rather than fixing it here.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Collectibles/Blueprint.cs
git commit -m "Documentare: priceToUnlock folosit ca indicator de plan colectat"
```

---

### Task 12: Remove dead code

**Files:**
- Delete: `Assets/Scripts/Object Managers/Purchaser.cs` (+ `.meta`)
- Delete: `Assets/Scripts/Object Managers/LocalisedPrices.cs` (+ `.meta`)
- Delete: `Assets/Scripts/Object Managers/SaveManager.cs` (+ `.meta`)
- Delete: `Assets/Scripts/Object Managers/RestorePurchases.cs` (+ `.meta`)
- Delete: `Assets/Scenes/Main Menu Before Demo.unity` (+ `.meta`)
- Create: `Assets/Editor/BuildAndroid.cs.meta`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing.

- [ ] **Step 1: Confirm nothing references them**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
for s in Purchaser LocalisedPrices SaveManager RestorePurchases; do
  g=$(grep -m1 "guid:" "Assets/Scripts/Object Managers/$s.cs.meta" | awk '{print $2}')
  echo -n "$s: "
  grep -rl "$g" Assets/Scenes/ Assets/Prefabs/ 2>/dev/null | tr '\n' ' ' || true
  echo "(empty = unreferenced)"
done
grep -n "Main Menu Before Demo" ProjectSettings/EditorBuildSettings.asset || echo "scene not in build settings"
```

Expected: all four unreferenced; the demo scene absent from build settings. **If any is referenced, stop and report rather than deleting.**

- [ ] **Step 2: Delete them**

```bash
git rm "Assets/Scripts/Object Managers/Purchaser.cs" "Assets/Scripts/Object Managers/Purchaser.cs.meta"
git rm "Assets/Scripts/Object Managers/LocalisedPrices.cs" "Assets/Scripts/Object Managers/LocalisedPrices.cs.meta"
git rm "Assets/Scripts/Object Managers/SaveManager.cs" "Assets/Scripts/Object Managers/SaveManager.cs.meta"
git rm "Assets/Scripts/Object Managers/RestorePurchases.cs" "Assets/Scripts/Object Managers/RestorePurchases.cs.meta"
git rm "Assets/Scenes/Main Menu Before Demo.unity" "Assets/Scenes/Main Menu Before Demo.unity.meta"
```

`RestorePurchases.cs` is fully commented out; the IAP hardening plan rewrites it from scratch.

- [ ] **Step 3: Add the missing meta file**

Open the project in Unity once and let it generate `Assets/Editor/BuildAndroid.cs.meta`, or run:

```bash
"/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$(pwd)" -logFile - 2>&1 | tail -5
ls Assets/Editor/BuildAndroid.cs.meta
```

- [ ] **Step 4: Confirm tests still pass**

Run: `./run-tests.sh`
Expected: `=== total=34 passed=34 failed=0 ===`

- [ ] **Step 5: Commit**

```bash
git add -A Assets/Editor
git commit -m "Curatare: eliminarea codului si a scenei nefolosite"
```

---

## Out of scope for this plan

These are named so no one assumes they were forgotten. Each gets its own plan.

| Deferred | Why | Blocked on |
|---|---|---|
| Unity editor upgrade, targetSdk, 16 KB pages | Requires a Unity Hub download and a human decision on the patch version | Human |
| `DataHolder` full cutover to `SaveService` | Large, touches every scene; safer once the core is proven in isolation. Tasks 9–11 deliberately keep writing through the existing `dataSaved` shim | This plan landing |
| `ShipRuntime` (spec §4.1) | Depends on the `DataHolder` cutover above | The task above |
| Ads, UMP consent, ATT | Needs an AdMob account and app IDs | Human |
| IAP hardening, receipt validation, `removeads` | Needs Play Console and App Store Connect products | Human |
| Haptics and `UIFeedbackInstaller` | Independent; needs device testing to tune | Own plan |
| Analytics, crash reporting | Needs a Firebase project | Human |
| Compliance, store metadata | Needs ads and IAP final | Later plans |

**Note on Task 9's shim:** the gate is switched on inside the existing `DataHolder`/`SaveData` code rather than on top of `SaveService`, because the full cutover is too large to land safely in the same plan as the save core. The consequence is that `SaveService` and `MigrationV0ToV1` are complete and tested but not yet driving the game. Wiring them in is the first task of the follow-up plan, and until it lands the 35% price change still round-trips through the legacy save for existing players.
