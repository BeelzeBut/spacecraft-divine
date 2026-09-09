# DataHolder → SaveService Cutover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `SaveService` the live persistence path so the v0→v1 migration actually runs, and paid entitlements stop depending on an array index.

**Architecture:** `PlayerProfile` becomes authoritative for **meta-progression** (gems, unlocks, ship levels, blueprints, settings, lifetime stats). The legacy `PlayerPrefs["save"]` blob stays authoritative for the **current run only** (level, respawns, the selected ship's live stats). `DataHolder` projects one onto the other at two well-defined points — `Load()` and `Save()` — so there is never a moment where two stores disagree about a field either of them owns.

**Tech Stack:** Unity 2022.3.14f1, IL2CPP, `XmlSerializer` (legacy blob), `JsonUtility` (profile envelope), NUnit EditMode tests.

**Spec:** `docs/superpowers/specs/2026-09-08-production-release-design.md`
**Predecessor handover (read first):** `docs/superpowers/notes/2026-09-09-save-progression-handover.md`

## Global Constraints

- **Branch:** `production-release`. **Never push to any remote.** Local commits only.
- **`applicationIdentifier` stays `com.KodaGames.SpaceshipDivine`**, mixed case included.
- **Never touch `README.md`, `ProjectSettings/`, or `Assets/2D Renderer.asset`.** They carry uncommitted thesis work. Do not stage, commit, or revert them.
- **`main` stays at `750a0ae`.**
- **No unlock is ever removed from an existing player.** Migration and projection are add-only.
- **`SdAutomation.Ships` order must never be reconciled to `LegacyShipIdMap`.** They differ in 9 of 13 positions by design.
- **`Blueprint.UnlockCollectible`'s `priceToUnlock = 1` is a boolean, not a price.** Do not "fix" it.
- Run the suite with `./run-tests.sh`. **Never modify it.** One Unity batchmode process at a time, and **run it once per task, at the end** — each run costs 4–5 minutes.

## Why the order of these tasks matters

Tasks 1–4 are independent hardening that must land **before** the cutover, because each one closes a way the cutover could silently destroy data. Task 5 is the testable translation layer. Task 6 is the switch. Task 7 is the only thing that can prove it on a real save.

---

### Task 1: `link.xml` — stop IL2CPP from stripping the save assemblies

`MigrationV0ToV1` uses `XmlSerializer` inside a **new assembly**, under `managedStrippingLevel: Android: 1`, and the project has no `link.xml`. EditMode tests run on Mono in the Editor and prove exactly nothing about this. Silent field-stripping means every 2021 unlock reads `false` — the single worst outcome this plan can produce.

**Files:**
- Create: `Assets/link.xml`

**Interfaces:**
- Produces: nothing in code. This is a build-time contract.

- [ ] **Step 1: Write the file**

`Assets/link.xml`:

```xml
<!--
  IL2CPP managed stripping is on (managedStrippingLevel Android: 1). Reflection-driven
  serialization is invisible to the static analyser: XmlSerializer discovers SaveData's fields
  at runtime, so the stripper sees no caller and is free to remove them.

  A stripped field does not throw. It deserializes as default(T) — every unlock false, every
  gem count zero — which is why this file matters more than its size suggests.

  Assembly names here are the asmdef "name" values, not file names.
-->
<linker>
  <assembly fullname="SpaceshipDivine.Save" preserve="all"/>
  <assembly fullname="SpaceshipDivine.Levels" preserve="all"/>
  <assembly fullname="SpaceshipDivine.Haptics" preserve="all"/>
  <assembly fullname="Assembly-CSharp">
    <type fullname="SaveData" preserve="all"/>
    <type fullname="Helper" preserve="all"/>
  </assembly>
  <assembly fullname="System.Xml">
    <type fullname="System.Xml.Serialization.XmlSerializer" preserve="all"/>
  </assembly>
</linker>
```

- [ ] **Step 2: Prove the build still succeeds and the file was consumed**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
make android-apk
grep -ci "link.xml" build-logs/android-apk.log
```

Expected: exit 0. A non-zero grep count confirms the linker saw it. If the count is 0, the build
did not consume the file — check the path is exactly `Assets/link.xml`.

- [ ] **Step 3: Commit**

```bash
git add Assets/link.xml Assets/link.xml.meta
git commit -m "Compilare: link.xml pentru a impiedica eliminarea ansamblurilor de salvare"
```

---

### Task 2: `shipId` reaches ship instances, and prefab order becomes a checked assumption

Two defects, one commit, because the second is what makes the first safe.

`MainMenu.RefreshData()` copies stats to `shipInstances` but never `shipId`, so `DataHolder.LoadPlayerData()` sets `selectedShip` from an instance whose `shipId` is `""`. `RunState.selectedShipId` would silently persist empty.

**The obvious fix is wrong.** The copy block is inside `if (shipInstances[i].selected == false)`. A `shipId` copy placed there skips **the selected ship** — the only one whose id is needed.

Separately: the whole cutover maps `isUnlocked[i]` ↔ `shipId` through `LegacyShipIdMap`, which is only correct while `MainMenu.shipPrefabs` stays in that exact order. Today that is an unwritten assumption. Make it a test.

**Files:**
- Modify: `Assets/Scripts/Main Menu/MainMenu.cs` (the `secondaryGun` line, ~155)
- Modify: `Assets/Editor/SdAutomation.cs`

**Interfaces:**
- Consumes: `SpaceshipDivine.Save.LegacyShipIdMap.OrderedShipIds`
- Produces: `SdAutomation.VerifyShipPrefabOrder()` — exits non-zero if scene order drifts

- [ ] **Step 1: Copy `shipId` below the guard**

In `MainMenu.RefreshData()`, immediately after the existing `secondaryGun` line — which is already
outside the `selected == false` block, and is there precisely because it must apply to every ship:

```csharp
            shipInstances[i].secondaryGun = shipPrefabs[i].secondaryGun;
            // Below the guard, deliberately. Inside it, the copy would skip the SELECTED ship —
            // the one whose id RunState.selectedShipId actually needs.
            shipInstances[i].shipId = shipPrefabs[i].shipId;
```

Do **not** author `shipId` into the instance assets themselves. They are runtime clones that exist
to avoid dirtying the source assets; duplicating canonical data means keeping it in sync forever.

- [ ] **Step 2: Add the order verifier**

Append to `Assets/Editor/SdAutomation.cs`, before the closing brace:

```csharp
    /// <summary>
    /// LegacyShipIdMap is the bridge between a save's isUnlocked[] index and a ship id. It is
    /// only correct while MainMenu.shipPrefabs keeps that exact order. Nothing enforced that,
    /// so a drag in the Inspector could silently hand a player who paid for Bat-Oh-No a
    /// different ship. This turns the assumption into a build-time check.
    /// </summary>
    public static void VerifyShipPrefabOrder()
    {
        var problems = new List<string>();

        UnityEngine.SceneManagement.Scene scene =
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity");

        MainMenu menu = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            menu = root.GetComponentInChildren<MainMenu>(true);
            if (menu != null) break;
        }

        if (menu == null)
        {
            problems.Add("no MainMenu component found in the scene");
            Finish("VerifyShipPrefabOrder", problems);
            return;
        }

        var expected = SpaceshipDivine.Save.LegacyShipIdMap.OrderedShipIds;
        if (menu.shipPrefabs.Count != expected.Count)
            problems.Add("shipPrefabs has " + menu.shipPrefabs.Count +
                         " entries, LegacyShipIdMap has " + expected.Count);

        int shared = Mathf.Min(menu.shipPrefabs.Count, expected.Count);
        for (int i = 0; i < shared; i++)
        {
            string actual = menu.shipPrefabs[i] == null ? "<null>" : menu.shipPrefabs[i].shipId;
            if (actual != expected[i])
                problems.Add("index " + i + ": scene has '" + actual +
                             "', LegacyShipIdMap has '" + expected[i] + "'");
        }

        Debug.Log("VerifyShipPrefabOrder: checked " + shared + " ships");
        Finish("VerifyShipPrefabOrder", problems);
    }
```

**`LegacyShipIdMap` does not expose this yet.** It has `Count` and `IndexToId(int)` only. Add both
accessors in this same commit — the projection in Task 5 needs the reverse lookup too:

```csharp
        // Array.AsReadOnly wraps rather than clones, so this stays allocation-cheap while
        // returning something callers cannot mutate. Same arrangement as PlayerProfile.
        public static IReadOnlyList<string> OrderedShipIds => System.Array.AsReadOnly(Ids);

        /// <summary>-1 when the id is not a ship this map knows. Callers must skip, not throw:
        /// an unknown id in a save is bad data, and bad data must never crash the game.</summary>
        public static int IdToIndex(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) return -1;
            for (int i = 0; i < Ids.Length; i++)
                if (Ids[i] == shipId) return i;
            return -1;
        }
```

`using System.Collections.Generic;` is needed at the top of the file for `IReadOnlyList`.

- [ ] **Step 3: Run the verifier**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
pgrep -fl "Unity.app/Contents/MacOS/Unity" && echo "KILL THE EDITOR FIRST" && exit 1
"/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$(pwd)" \
  -executeMethod SdAutomation.VerifyShipPrefabOrder -logFile - 2>&1 | tail -20
echo "EXIT=$?"
```

Expected: `VerifyShipPrefabOrder: OK` and exit 0. **If it reports a mismatch, stop and report it** —
that means `LegacyShipIdMap` and the scene already disagree, and every assumption downstream of it
in the save plan needs re-checking before anything else proceeds.

`OpenScene` in batchmode does not save, so the scene must remain unmodified. Confirm with
`git status --short -- "Assets/Scenes/Main Menu.unity"` — it must print nothing. If the scene comes
back dirty, discard it (`git checkout --`); saving it reserialises 580 lines of unrelated format
churn.

- [ ] **Step 4: Run the suite once and commit**

```bash
./run-tests.sh
git add "Assets/Scripts/Main Menu/MainMenu.cs" Assets/Editor/SdAutomation.cs
git commit -m "Salvare: shipId ajunge la instantele navelor; ordinea din scena devine verificata"
```

---

### Task 3: `SaveService` falls back to the legacy blob when the store rejects a save

`Load()` returns as soon as `store.Exists()` is true, even when the read outcome was
`RejectedAndReset`. The HMAC is salted with `SystemInfo.deviceUniqueIdentifier`, and Android Auto
Backup restores **both** `persistentDataPath` and `PlayerPrefs` onto a new device. So the restored
profile fails its signature on the new hardware, gets reset to default — while an intact legacy
blob sits unread in `PlayerPrefs`. The player loses everything, on a brand-new phone, silently.

**Files:**
- Modify: `Assets/Scripts/Save/SaveService.cs`
- Test: `Assets/Tests/EditMode/SaveServiceTests.cs`

**Interfaces:**
- Consumes: `ISaveStore.LastReadOutcome`, `ReadOutcome.RejectedAndReset`
- Produces: `SaveService.RecoveredFromLegacyAfterRejection` — `bool`, true when this path fired

- [ ] **Step 1: Write the failing test**

Add to `Assets/Tests/EditMode/SaveServiceTests.cs`:

```csharp
        [Test]
        public void RejectedSaveFallsBackToLegacyInsteadOfResetting()
        {
            // Android Auto Backup restores persistentDataPath AND PlayerPrefs to a new device.
            // The profile's HMAC is salted with the device id, so it fails there — but the
            // legacy blob is plain XML and still readable.
            var store = new FileSaveStore(dir);
            store.Write(PlayerProfile.CreateDefault());
            CorruptBothCopies();

            var svc = new SaveService(store, LegacyXmlWithEverythingUnlocked, _ => { });
            svc.Load();

            Assert.AreEqual(ReadOutcome.RejectedAndReset, store.LastReadOutcome,
                "the fixture must actually produce a rejection, or this test proves nothing");
            Assert.IsTrue(svc.RecoveredFromLegacyAfterRejection);
            Assert.AreEqual(LegacyShipIdMap.Count, svc.Profile.unlockedShipIds.Count);
        }

        [Test]
        public void RejectedSaveWithNoLegacyKeepsTheResetProfile()
        {
            var store = new FileSaveStore(dir);
            store.Write(PlayerProfile.CreateDefault());
            CorruptBothCopies();

            var svc = new SaveService(store, () => null, _ => { });
            svc.Load();

            Assert.IsFalse(svc.RecoveredFromLegacyAfterRejection);
            Assert.IsNotNull(svc.Profile, "a reset profile is still better than none");
        }

        [Test]
        public void HealthyLoadNeverConsultsTheLegacyBlob()
        {
            // Discriminating on purpose: the legacy provider unlocks every ship, so a
            // wrongly-ordered guard shows up immediately as an inflated unlock count.
            var store = new FileSaveStore(dir);
            PlayerProfile saved = PlayerProfile.CreateDefault();   // 3 starters only
            store.Write(saved);

            bool legacyConsulted = false;
            var svc = new SaveService(store,
                () => { legacyConsulted = true; return LegacyXmlWithEverythingUnlocked(); },
                _ => { });
            svc.Load();

            Assert.IsFalse(legacyConsulted, "a healthy save must not read the legacy blob");
            Assert.AreEqual(3, svc.Profile.unlockedShipIds.Count);
        }
```

Add this helper alongside the existing `LegacyXmlWithEverythingUnlocked()`:

```csharp
        /// <summary>
        /// Makes both the live file and its backup fail verification, which is the only way to
        /// reach ReadOutcome.RejectedAndReset. Writing garbage rather than deleting matters:
        /// a deleted file gives Exists() == false and takes a completely different branch.
        /// </summary>
        private void CorruptBothCopies()
        {
            File.WriteAllText(Path.Combine(dir, FileSaveStore.SaveFileName), "not json");
            File.WriteAllText(Path.Combine(dir, FileSaveStore.BackupFileName), "not json either");
        }
```

The existing `dir` field and its `[SetUp]`/`[TearDown]` already provide a fresh temp directory per
test — reuse them rather than introducing a fake store; these tests are more convincing against the
real `FileSaveStore`.

- [ ] **Step 2: Run to confirm failure**

Run: `./run-tests.sh`
Expected: compile error — `RecoveredFromLegacyAfterRejection` does not exist.

- [ ] **Step 3: Implement**

Replace the early return in `SaveService.Load()`:

```csharp
        public bool RecoveredFromLegacyAfterRejection { get; private set; }

        public void Load()
        {
            MigratedThisLoad = false;
            RecoveredFromLegacyAfterRejection = false;

            if (store.Exists())
            {
                Profile = store.Read();

                // A rejected save is not the same as no save. Both files failed their signature
                // — most often because Android Auto Backup moved them to a device whose id no
                // longer salts the HMAC. Returning here would hand the player a default profile
                // while a readable legacy blob sits untouched in PlayerPrefs.
                if (store.LastReadOutcome != ReadOutcome.RejectedAndReset)
                    return;

                PlayerProfile fromLegacy = TryLegacy();
                if (fromLegacy == null) return;      // keep the reset profile; nothing better exists

                Profile = fromLegacy;
                RecoveredFromLegacyAfterRejection = true;
                store.Write(Profile);
                Debug.LogWarning("Profile failed verification; recovered " +
                                 Profile.unlockedShipIds.Count + " ships from the legacy save.");
                return;
            }

            PlayerProfile migrated = TryLegacy();
            if (migrated != null)
            {
                Profile = migrated;
                MigratedThisLoad = true;
                store.Write(Profile);
                legacyCleared?.Invoke(MigrationV0ToV1.LegacyPlayerPrefsKey);
                Debug.Log("Migrated legacy save to v1: " +
                          Profile.unlockedShipIds.Count + " ships preserved.");
                return;
            }

            Profile = store.Read();   // no file and no legacy -> default profile
        }

        private PlayerProfile TryLegacy()
        {
            string xml = legacyXmlProvider != null ? legacyXmlProvider() : null;
            if (string.IsNullOrEmpty(xml)) return null;

            PlayerProfile parsed = MigrationV0ToV1.FromXml(xml);
            if (parsed == null)
                Debug.LogWarning("Legacy save present but unreadable.");
            return parsed;
        }
```

- [ ] **Step 4: Run the suite and commit**

```bash
./run-tests.sh
git add Assets/Scripts/Save/SaveService.cs Assets/Tests/EditMode/SaveServiceTests.cs
git commit -m "Salvare: revenire la blocul vechi cand profilul este respins la verificare"
```

---

### Task 4: Version policy — refuse to downgrade, never destroy

`SaveEnvelope.version` is written and never read. A future-version envelope loads on signature
match alone, so a player who installs a newer build, plays, then rolls back would have the old
build silently reinterpret newer data. The opposite reflex — rejecting it — resets the profile and
loses everything.

**Neither is acceptable. The rule is: refuse to write over what you cannot read.**

**Files:**
- Modify: `Assets/Scripts/Save/FileSaveStore.cs`
- Test: `Assets/Tests/EditMode/FileSaveStoreTests.cs`

**Interfaces:**
- Produces: `ISaveStore.IsReadOnlyBecauseNewer` — `bool`; `Write` becomes a no-op while true

- [ ] **Step 1: Write the failing test**

```csharp
        [Test]
        public void AFutureVersionSaveIsNotOverwritten()
        {
            // Forward compatibility is impossible; data loss is avoidable. An older build must
            // leave a newer save alone rather than reset it or write stale fields over it.
            var writer = new FileSaveStore(dir);   // `dir` is the per-test temp dir from [SetUp]
            PlayerProfile p = PlayerProfile.CreateDefault();
            p.gems = 4242;
            writer.Write(p);

            RewriteEnvelopeVersion(dir, SaveEnvelope.CurrentVersion + 1);
            string before = File.ReadAllText(Path.Combine(dir, FileSaveStore.SaveFileName));

            var older = new FileSaveStore(dir);
            older.Read();
            Assert.IsTrue(older.IsReadOnlyBecauseNewer);

            PlayerProfile overwrite = PlayerProfile.CreateDefault();
            overwrite.gems = 1;
            older.Write(overwrite);

            Assert.AreEqual(before, File.ReadAllText(Path.Combine(dir, FileSaveStore.SaveFileName)),
                "an older build must not overwrite a newer save");
        }

        [Test]
        public void ACurrentVersionSaveIsStillWritable()
        {
            // Guards the obvious over-correction: locking every save read-only.
            var store = new FileSaveStore(dir);   // `dir` is the per-test temp dir from [SetUp]
            store.Write(PlayerProfile.CreateDefault());
            store.Read();
            Assert.IsFalse(store.IsReadOnlyBecauseNewer);

            PlayerProfile p = PlayerProfile.CreateDefault();
            p.gems = 777;
            store.Write(p);

            Assert.AreEqual(777, new FileSaveStore(dir).Read().gems);
        }
```

`RewriteEnvelopeVersion(string dir, int version)` reads the live file, replaces the envelope's
`version` field, **re-signs it with the same key** so the test exercises version handling rather
than signature failure, and writes it back. If re-signing is not reachable from the test assembly,
add an `internal` helper on `SaveIntegrity` rather than duplicating the HMAC in the test.

- [ ] **Step 2: Run to confirm failure**

Run: `./run-tests.sh` — expect a compile error on `IsReadOnlyBecauseNewer`.

- [ ] **Step 3: Implement**

Add to `ISaveStore`:

```csharp
        /// <summary>
        /// True when the file on disk was written by a NEWER build. Write() becomes a no-op:
        /// this build cannot read those fields, so anything it wrote would silently drop them.
        /// Refusing to write loses this session; writing loses the save.
        /// </summary>
        bool IsReadOnlyBecauseNewer { get; }
```

In `FileSaveStore.TryLoad`, after the signature verifies and the envelope parses:

```csharp
                if (envelope.version > SaveEnvelope.CurrentVersion)
                {
                    IsReadOnlyBecauseNewer = true;
                    Debug.LogWarning("Save was written by a newer build (v" + envelope.version +
                                     " > v" + SaveEnvelope.CurrentVersion +
                                     "); loading read-only and refusing to overwrite it.");
                }
```

and at the top of `Write`:

```csharp
            if (IsReadOnlyBecauseNewer)
            {
                Debug.LogWarning("Refusing to overwrite a newer save.");
                return;
            }
```

Implement `IsReadOnlyBecauseNewer` on any other `ISaveStore` implementations, including test fakes.

- [ ] **Step 4: Run the suite and commit**

```bash
./run-tests.sh
git add Assets/Scripts/Save Assets/Tests/EditMode/FileSaveStoreTests.cs
git commit -m "Salvare: o salvare scrisa de o versiune mai noua se citeste, dar nu se suprascrie"
```

---

### Task 5: `ProfileProjection` — the translation layer, unit tested

This is the task that makes the cutover reviewable. `DataHolder` is a MonoBehaviour bound to
`PlayerPrefs` and scene state and cannot be tested; the *translation* can be, if it lives away from
the MonoBehaviour.

`SaveData` currently sits in `Assembly-CSharp`, which the test assembly cannot reference. Move it
into the save assembly. This is a file move with no code change: `XmlSerializer` names the root
element after the **type**, not the assembly or namespace, so every existing 2021 blob keeps
deserializing byte-for-byte. Keep the class in the global namespace — do not add one.

**Files:**
- Move: `Assets/Scripts/Object Managers/SaveData.cs` → `Assets/Scripts/Save/SaveData.cs`
- Create: `Assets/Scripts/Save/ProfileProjection.cs`
- Test: `Assets/Tests/EditMode/ProfileProjectionTests.cs`

**Interfaces:**
- Consumes: `PlayerProfile`, `SaveData`, `LegacyShipIdMap.OrderedShipIds`
- Produces:
  - `ProfileProjection.ApplyToSaveData(PlayerProfile profile, SaveData target)`
  - `ProfileProjection.CaptureFromSaveData(SaveData source, PlayerProfile target)`

**Ownership, and this is the whole design:**

| Field group | Owner | Why |
|---|---|---|
| gems, goldCoins, unlocks, ship levels, blueprints, `removeAdsOwned` | `PlayerProfile` | Paid content. Must survive a `PlayerPrefs` wipe and must not be index-addressed. |
| settings, tutorial flags, `levelsPlayed`, lifetime stats | `PlayerProfile` | Player-visible preferences; cheap to carry and wanted for cloud save later. |
| `level[]`, `respawnsRemaining`, the selected ship's live stats, `numberOfShips`, upgrades, `gameHasEnded` | `SaveData` | Current run only. Worthless after the run ends; not worth a schema. |

- [ ] **Step 1: Move `SaveData.cs`**

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
git mv "Assets/Scripts/Object Managers/SaveData.cs" "Assets/Scripts/Save/SaveData.cs"
git mv "Assets/Scripts/Object Managers/SaveData.cs.meta" "Assets/Scripts/Save/SaveData.cs.meta"
```

Moving the `.meta` with it preserves the GUID, so nothing that references the script by GUID breaks.

- [ ] **Step 2: Write the failing tests**

`Assets/Tests/EditMode/ProfileProjectionTests.cs`:

```csharp
using NUnit.Framework;
using SpaceshipDivine.Save;

public class ProfileProjectionTests
{
    [Test]
    public void UnlocksProjectOntoTheCorrectIndices()
    {
        var profile = PlayerProfile.CreateDefault();
        profile.Unlock("valiant");
        var data = new SaveData();

        ProfileProjection.ApplyToSaveData(profile, data);

        int valiantIndex = LegacyShipIdMap.OrderedShipIds.IndexOf("valiant");
        int razorIndex = LegacyShipIdMap.OrderedShipIds.IndexOf("razor");
        Assert.IsTrue(data.isUnlocked[valiantIndex]);
        Assert.IsFalse(data.isUnlocked[razorIndex], "an unowned ship must stay locked");
    }

    [Test]
    public void CaptureThenApplyRoundTripsUnlocks()
    {
        var profile = PlayerProfile.CreateDefault();
        var data = new SaveData();
        data.isUnlocked[LegacyShipIdMap.OrderedShipIds.IndexOf("hot_talon")] = true;

        ProfileProjection.CaptureFromSaveData(data, profile);

        Assert.IsTrue(profile.IsUnlocked("hot_talon"));
    }

    [Test]
    public void CaptureNeverRemovesAnUnlockTheProfileAlreadyHad()
    {
        // The rule the whole plan rests on: projection is add-only. A gameplay scene builds a
        // SaveData with fewer unlocks than the profile; capturing it must not revoke anything.
        var profile = PlayerProfile.CreateDefault();
        profile.Unlock("bat_oh_no");          // the EUR 9.99 ship
        var data = new SaveData();            // every isUnlocked false

        ProfileProjection.CaptureFromSaveData(data, profile);

        Assert.IsTrue(profile.IsUnlocked("bat_oh_no"),
            "a paid unlock must never be revoked by projection");
    }

    [Test]
    public void ShipLevelsSurviveBothDirections()
    {
        // 3, not 1: 1 is both the SaveData default and PlayerProfile's default, so a fixture of
        // 1 would pass even if nothing were copied.
        var profile = PlayerProfile.CreateDefault();
        profile.SetShipLevel("apollo", 3);
        var data = new SaveData();

        ProfileProjection.ApplyToSaveData(profile, data);
        Assert.AreEqual(3, data.shipLevel[LegacyShipIdMap.OrderedShipIds.IndexOf("apollo")]);

        var roundTripped = PlayerProfile.CreateDefault();
        ProfileProjection.CaptureFromSaveData(data, roundTripped);
        Assert.AreEqual(3, roundTripped.GetShipLevel("apollo"));
    }

    [Test]
    public void GemsAndSettingsProjectBothWays()
    {
        var profile = PlayerProfile.CreateDefault();
        profile.gems = 1234;
        profile.settings.isHapticsMuted = true;
        profile.settings.qualityIndex = 1;
        var data = new SaveData();

        ProfileProjection.ApplyToSaveData(profile, data);
        Assert.AreEqual(1234, data.gems);
        Assert.IsTrue(data.isHapticsMuted);
        Assert.AreEqual(1, data.qualityIndex);

        data.gems = 99;
        data.isHapticsMuted = false;
        var back = PlayerProfile.CreateDefault();
        ProfileProjection.CaptureFromSaveData(data, back);
        Assert.AreEqual(99, back.gems);
        Assert.IsFalse(back.settings.isHapticsMuted);
    }

    [Test]
    public void BlueprintRedeemableShipsBecomeThePriceSentinel()
    {
        // priceToUnlock == 1 is the legacy boolean "blueprint collected, ship claimable".
        // MainMenu.UnlockShip's canBeUnlockedInGame branch reads exactly this.
        var profile = PlayerProfile.CreateDefault();
        profile.MarkBlueprintRedeemable("vickers");
        var data = new SaveData();

        ProfileProjection.ApplyToSaveData(profile, data);

        Assert.AreEqual(1f, data.priceToUnlock[LegacyShipIdMap.OrderedShipIds.IndexOf("vickers")]);
    }

    [Test]
    public void ProjectionToleratesAnIndexBeyondTheSaveDataArrays()
    {
        // SaveData's arrays are fixed at 50/100. A profile naming a ship outside the map must
        // be skipped, not throw — a crash here is a player who cannot launch the game.
        var profile = PlayerProfile.CreateDefault();
        profile.Unlock("a_ship_that_does_not_exist");
        var data = new SaveData();

        Assert.DoesNotThrow(() => ProfileProjection.ApplyToSaveData(profile, data));
    }
}
```

- [ ] **Step 3: Run to confirm failure**

Run: `./run-tests.sh` — expect a compile error, `ProfileProjection` does not exist.

- [ ] **Step 4: Implement**

`Assets/Scripts/Save/ProfileProjection.cs`:

```csharp
using System.Collections.Generic;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// Translates between the profile (authoritative for meta-progression) and the legacy
    /// SaveData blob (authoritative for the current run only).
    ///
    /// Both directions are ADD-ONLY for unlocks and blueprints. A gameplay scene can build a
    /// SaveData with fewer unlocks than the profile holds — MainMenu is not loaded there — and
    /// a symmetric copy would revoke ships the player paid for.
    ///
    /// Indices come from LegacyShipIdMap, which SdAutomation.VerifyShipPrefabOrder pins against
    /// the scene. Any id not in the map is skipped rather than throwing: a crash in the save
    /// path is a player who cannot launch the game at all.
    /// </summary>
    public static class ProfileProjection
    {
        public static void ApplyToSaveData(PlayerProfile profile, SaveData target)
        {
            if (profile == null || target == null) return;

            IReadOnlyList<string> order = LegacyShipIdMap.OrderedShipIds;
            for (int i = 0; i < order.Count; i++)
            {
                if (i >= target.isUnlocked.Length) break;

                string id = order[i];
                if (profile.IsUnlocked(id)) target.isUnlocked[i] = true;

                if (i < target.shipLevel.Length)
                    target.shipLevel[i] = profile.GetShipLevel(id);

                // 1 is the legacy sentinel for "blueprint collected, ship claimable" — a
                // boolean wearing a float's clothes, read by MainMenu.UnlockShip.
                if (i < target.priceToUnlock.Length && profile.IsBlueprintRedeemable(id))
                    target.priceToUnlock[i] = 1f;
            }

            foreach (int dropIndex in profile.collectedBlueprintIndices)
                if (dropIndex >= 0 && dropIndex < target.hasBeenUnlocked.Length)
                    target.hasBeenUnlocked[dropIndex] = true;

            target.gems = profile.gems;
            target.goldCoins = profile.goldCoins;
            target.hasCompletedTutorial = profile.hasCompletedTutorial;
            target.hasCompletedButtonsTutorial = profile.hasCompletedButtonsTutorial;
            target.levelsPlayed = profile.levelsPlayed;
            target.totalEnemiesKilled = profile.stats.totalEnemiesKilled;

            target.qualityIndex = profile.settings.qualityIndex;
            target.targetFps = profile.settings.targetFps;
            target.isMusicMuted = profile.settings.isMusicMuted;
            target.isSoundMuted = profile.settings.isSoundMuted;
            target.isHapticsMuted = profile.settings.isHapticsMuted;
            target.controllerOn = profile.settings.controllerOn;
        }

        public static void CaptureFromSaveData(SaveData source, PlayerProfile target)
        {
            if (source == null || target == null) return;

            IReadOnlyList<string> order = LegacyShipIdMap.OrderedShipIds;
            for (int i = 0; i < order.Count; i++)
            {
                if (i >= source.isUnlocked.Length) break;

                string id = order[i];
                if (source.isUnlocked[i]) target.Unlock(id);   // add-only, never Remove

                if (i < source.shipLevel.Length && source.shipLevel[i] > 0)
                    target.SetShipLevel(id, source.shipLevel[i]);

                if (i < source.priceToUnlock.Length && source.priceToUnlock[i] == 1f)
                    target.MarkBlueprintRedeemable(id);
            }

            for (int d = 0; d < source.hasBeenUnlocked.Length; d++)
                if (source.hasBeenUnlocked[d])
                    target.CollectBlueprint(d);

            target.gems = source.gems;
            target.goldCoins = source.goldCoins;
            target.hasCompletedTutorial = source.hasCompletedTutorial;
            target.hasCompletedButtonsTutorial = source.hasCompletedButtonsTutorial;
            target.levelsPlayed = source.levelsPlayed;
            target.stats.totalEnemiesKilled = source.totalEnemiesKilled;

            target.settings.qualityIndex = source.qualityIndex;
            target.settings.targetFps = source.targetFps;
            target.settings.isMusicMuted = source.isMusicMuted;
            target.settings.isSoundMuted = source.isSoundMuted;
            target.settings.isHapticsMuted = source.isHapticsMuted;
            target.settings.controllerOn = source.controllerOn;
        }
    }
}
```

If `LegacyShipIdMap.OrderedShipIds` is not an `IReadOnlyList<string>` with `IndexOf`, adapt the
tests to whatever it exposes rather than changing the map's verified contents.

- [ ] **Step 5: Run the suite and commit**

```bash
./run-tests.sh
git add Assets/Scripts/Save Assets/Tests/EditMode/ProfileProjectionTests.cs
git commit -m "Salvare: ProfileProjection intre profil si blocul vechi, doar prin adaugare"
```

---

### Task 6: Wire `DataHolder` to `SaveService`

The switch. Everything before this was making it safe.

**Files:**
- Modify: `Assets/Scripts/Object Managers/DataHolder.cs`

**Interfaces:**
- Consumes: `SaveService`, `FileSaveStore`, `ProfileProjection`
- Produces: `DataHolder.Profile` — `PlayerProfile`, for the ads/IAP plan's `removeAdsOwned`

- [ ] **Step 1: Add the service, constructed once**

```csharp
    private SaveService saveService;

    /// <summary>Authoritative for meta-progression. The ads and IAP plan writes
    /// removeAdsOwned through this, never through dataSaved.</summary>
    public SpaceshipDivine.Save.PlayerProfile Profile => saveService?.Profile;

    private SaveService Service
    {
        get
        {
            if (saveService == null)
            {
                saveService = new SaveService(
                    new FileSaveStore(),
                    () => PlayerPrefs.HasKey("save") ? PlayerPrefs.GetString("save") : null,
                    // The legacy blob is deliberately NOT deleted for one release: it is the
                    // only escape hatch if migration turns out to be wrong on real data.
                    key => Debug.Log("Legacy save retained as a fallback (key: " + key + ")"));
            }
            return saveService;
        }
    }
```

- [ ] **Step 2: Rewrite `Load()`**

```csharp
    public void Load()
    {
        if (!enableSaving) return;

        Service.Load();

        // The run blob still comes from PlayerPrefs: it describes the CURRENT run, which the
        // profile deliberately does not model.
        if (PlayerPrefs.HasKey("save"))
            dataSaved = Helper.Deserialize<SaveData>(PlayerPrefs.GetString("save"));
        else
            dataSaved = new SaveData();

        // Profile wins for everything it owns. This is what makes a returning player's unlocks
        // survive even if the PlayerPrefs blob is missing — the case the old code could not
        // handle, because the starter-only branch fired whenever the key was absent.
        ProfileProjection.ApplyToSaveData(Service.Profile, dataSaved);

        if (Service.MigratedThisLoad)
            Debug.Log("Legacy save migrated on this launch.");

        gems = dataSaved.gems;
        qualityIndex = dataSaved.qualityIndex;
        QualitySettings.SetQualityLevel(qualityIndex, true);
        Haptics.Muted = dataSaved.isHapticsMuted;

        LoadPlayerData();
    }
```

**Delete the `else` branch's starter-ship loop.** `PlayerProfile.CreateDefault()` already unlocks
exactly the three starters, and `ApplyToSaveData` projects them onto `isUnlocked[]`. Keeping both
would mean two places decide who starts unlocked.

- [ ] **Step 3: Rewrite `Save()`**

```csharp
    public void Save()
    {
        if (!enableSaving) return;

        SavePlayerData(PlayerController.instance);

        // Capture BEFORE writing either store, so both see the same state.
        ProfileProjection.CaptureFromSaveData(dataSaved, Service.Profile);
        Service.Save();

        PlayerPrefs.SetString("save", Helper.Serialize<SaveData>(dataSaved));
    }
```

- [ ] **Step 4: Verify the returning-player path by hand**

An EditMode test cannot reach `DataHolder`. Prove it with the store instead:

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
ls -la ~/Library/Application\ Support/*/Spaceship*/ 2>/dev/null
```

Then in the Editor: Play, confirm the log line `Migrated legacy save to v1: N ships preserved`,
quit, Play again, and confirm it does **not** appear the second time (the profile now exists).
Record N. **If N is lower than the number of ships that player owned, stop and report it.**

- [ ] **Step 5: Run the suite and commit**

```bash
./run-tests.sh
git add "Assets/Scripts/Object Managers/DataHolder.cs"
git commit -m "Salvare: DataHolder trece pe SaveService; migrarea ruleaza la prima pornire"
```

---

### Task 7: Prove it on a real 2021 save, on a device

**This task cannot be completed by an agent.** It needs a real legacy blob and a physical phone.

**Files:**
- Create: `docs/superpowers/notes/save-cutover-device-verification.md`

- [ ] **Step 1: Capture a real legacy save before touching anything**

On a device with the 2021 build installed:

```bash
adb shell "run-as com.KodaGames.SpaceshipDivine cat shared_prefs/com.KodaGames.SpaceshipDivine.v2.playerprefs.xml" > legacy-save-backup.xml
wc -c legacy-save-backup.xml
```

Keep this file outside the repo. It is the only copy of that player's history.

- [ ] **Step 2: Install the new build over the old one — do not uninstall**

```bash
make android-apk
adb install -r "Build Android/SpaceshipDivine.apk"
adb logcat -c && adb logcat -s Unity:V | grep -iE "Migrated|legacy|profile|ships preserved"
```

Expected: `Migrated legacy save to v1: N ships preserved` exactly once.

- [ ] **Step 3: Confirm what the player kept**

Check in the menu, against what they owned before: every unlocked ship, every ship level, gem
count, and blueprint progress. **Any regression here stops the release.**

- [ ] **Step 4: Confirm the stripping question is actually settled**

This is the step `link.xml` exists for, and only a device build answers it. If migration reports
0 ships on device but works in the Editor, IL2CPP stripped the fields and `link.xml` is not
covering the right assembly.

- [ ] **Step 5: Record and commit**

```bash
git add docs/superpowers/notes/save-cutover-device-verification.md
git commit -m "Nota: verificarea trecerii la SaveService pe o salvare reala"
```

---

## Out of scope for this plan

| Deferred | Why |
|---|---|
| Deleting the legacy `PlayerPrefs` blob | Kept one release as the escape hatch if migration is wrong on real data |
| Moving run state into `RunState` | `SaveData` already carries it and nothing is broken; the profile split is the part that pays |
| Cloud save | Needs Firebase; the online-services plan owns it, and it depends on this cutover |
| Bat-Oh-No's dead purchase button | Store-release blocker, owned by the ads and IAP plan |
