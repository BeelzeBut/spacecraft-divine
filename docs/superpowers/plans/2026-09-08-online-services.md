# Online Services Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add leaderboards, a shared daily challenge, cloud save and remote economy tuning — starting with the one change everything competitive depends on: making level generation reproducible.

**Architecture:** Firebase (Firestore, Anonymous Auth, Cloud Functions, Remote Config). Every client service degrades to a working offline state; none may gate the core loop. Scores are written only by a Cloud Function, never by a client.

**Tech Stack:** Unity 2022.3.14f1, Firebase Unity SDK, Firestore, Cloud Functions (Node), Remote Config.

**Spec:** `docs/superpowers/specs/2026-09-08-online-services-design.md`

## Global Constraints

- **Branch:** `production-release`. **Never push to any remote** until after the diploma defense on 11 Sep 2026.
- **The game must remain fully playable offline.** Every online call is fire-and-forget or has an offline fallback. No await on a network call blocks play.
- **Clients never write leaderboard entries.** Firestore rules deny it; `submitScore` is the only path.
- **Seeds are server-issued.** A client never chooses the daily seed.
- **Depends on the save plan landing** — cloud save builds on `SaveService` and `PlayerProfile`.
- Run the suite with `./run-tests.sh`. **Never modify it.** One Unity batchmode process at a time; check `pgrep -fl "Unity.app/Contents/MacOS/Unity"` first.

## The prerequisite, stated plainly

`Assets/Scripts/Object Managers/LevelGenerator.cs` calls `Random.Range` at lines 52, 55, 70, 76, 188 and 275, and never calls `Random.InitState`. **Generation is unseeded and irreproducible.** Until Task 1 lands, a shared daily challenge is impossible — two players given the same "seed" would get different dungeons, and the leaderboard would be meaningless.

Task 1 is executable today with no credentials. Tasks 2 onward need a Firebase project.

---

### Task 1: Make level generation reproducible

Generation happens in `LevelGenerator.Start()`. The seed must control the **layout only** — enemy AI, drops and combat keep drawing from the global random stream, so a run is not fully deterministic. That is the correct scope: the daily challenge needs everyone to get the same dungeon, not the same fight.

The mechanism matters. `Random.InitState` reseeds Unity's **global** random state. Seeding without restoring would make every subsequent gameplay draw deterministic from that seed too. So the generator saves the prior state, seeds, generates, and restores.

**Files:**
- Create: `Assets/Scripts/Levels/LevelSeed.cs`
- Modify: `Assets/Scripts/Object Managers/LevelGenerator.cs`
- Create: `Assets/Tests/EditMode/LevelSeedTests.cs`

**Interfaces:**
- Produces:
  - `LevelSeed.Pending` — `int?`, the seed the next generated level must use; `null` means "random"
  - `LevelSeed.Set(int seed)` / `LevelSeed.Clear()`
  - `LevelSeed.ForDate(System.DateTime utcDate) -> int` — deterministic fallback seed derived from a UTC date, used only if the server is unreachable
  - `LevelGenerator.LastUsedSeed` — `int`, what actually generated the current level

- [ ] **Step 1: Write the failing tests**

`Assets/Tests/EditMode/LevelSeedTests.cs`:

```csharp
using System;
using NUnit.Framework;

public class LevelSeedTests
{
    [SetUp]
    public void SetUp() => LevelSeed.Clear();

    [TearDown]
    public void TearDown() => LevelSeed.Clear();

    [Test]
    public void PendingIsNullByDefault()
    {
        Assert.IsNull(LevelSeed.Pending);
    }

    [Test]
    public void SetThenClearRoundTrips()
    {
        LevelSeed.Set(12345);
        Assert.AreEqual(12345, LevelSeed.Pending);
        LevelSeed.Clear();
        Assert.IsNull(LevelSeed.Pending);
    }

    [Test]
    public void ForDateIsDeterministicForTheSameUtcDay()
    {
        var morning = new DateTime(2026, 9, 8, 3, 0, 0, DateTimeKind.Utc);
        var evening = new DateTime(2026, 9, 8, 22, 30, 0, DateTimeKind.Utc);
        Assert.AreEqual(LevelSeed.ForDate(morning), LevelSeed.ForDate(evening),
            "the same UTC day must yield the same seed regardless of time of day");
    }

    [Test]
    public void ForDateDiffersAcrossDays()
    {
        int day1 = LevelSeed.ForDate(new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc));
        int day2 = LevelSeed.ForDate(new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc));
        Assert.AreNotEqual(day1, day2);
    }

    [Test]
    public void ForDateIsStableAcrossProcessRuns()
    {
        // Pinned value: if this changes, every previously published daily challenge changes
        // dungeon. Regenerate deliberately, never incidentally.
        Assert.AreEqual(LevelSeed.ForDate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                        LevelSeed.ForDate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void SeedingIsRepeatableAndRestoresGlobalRandomState()
    {
        // Two identical seeded sequences must match...
        UnityEngine.Random.InitState(999);
        int unrelatedBefore = UnityEngine.Random.Range(0, 1000000);

        UnityEngine.Random.State saved = UnityEngine.Random.state;
        UnityEngine.Random.InitState(4242);
        int a1 = UnityEngine.Random.Range(0, 1000000);
        int a2 = UnityEngine.Random.Range(0, 1000000);
        UnityEngine.Random.state = saved;

        // ...and restoring must leave the outer stream exactly where it was.
        int unrelatedAfter = UnityEngine.Random.Range(0, 1000000);

        UnityEngine.Random.InitState(999);
        UnityEngine.Random.Range(0, 1000000);           // consume unrelatedBefore's draw
        int expectedAfter = UnityEngine.Random.Range(0, 1000000);

        UnityEngine.Random.State saved2 = UnityEngine.Random.state;
        UnityEngine.Random.InitState(4242);
        int b1 = UnityEngine.Random.Range(0, 1000000);
        int b2 = UnityEngine.Random.Range(0, 1000000);
        UnityEngine.Random.state = saved2;

        Assert.AreEqual(a1, b1, "same seed must give the same first draw");
        Assert.AreEqual(a2, b2, "same seed must give the same second draw");
        Assert.AreEqual(expectedAfter, unrelatedAfter,
            "restoring state must not disturb the surrounding random stream");
    }
}
```

- [ ] **Step 2: Run to confirm failure**

Run: `./run-tests.sh` — expect a compile error, `LevelSeed` does not exist.

- [ ] **Step 3: Write LevelSeed**

`Assets/Scripts/Levels/LevelSeed.cs`:

```csharp
using System;

/// <summary>
/// Carries the seed for the NEXT level to be generated. Set before loading a level scene;
/// LevelGenerator consumes it and clears it, so an ordinary run after a daily challenge is
/// random again.
/// </summary>
public static class LevelSeed
{
    public static int? Pending { get; private set; }

    public static void Set(int seed) => Pending = seed;
    public static void Clear() => Pending = null;

    /// <summary>
    /// Offline fallback only. The authoritative daily seed comes from the server; this exists
    /// so a player with no connection still gets a coherent, self-consistent challenge rather
    /// than a broken screen. Their score simply is not submitted.
    ///
    /// The formula is FROZEN. Changing it changes the dungeon of every past daily challenge.
    /// </summary>
    public static int ForDate(DateTime utcDate)
    {
        int days = (int)(utcDate.Date - new DateTime(2020, 1, 1)).TotalDays;
        unchecked
        {
            int h = 17;
            h = h * 31 + days;
            h = h * 31 + 0x5F3759DF;
            h ^= h >> 13;
            h *= 0x27D4EB2D;
            h ^= h >> 15;
            return h & 0x7FFFFFFF;   // keep it non-negative
        }
    }
}
```

- [ ] **Step 4: Seed the generator, then restore**

In `LevelGenerator.cs`, add a field and wrap generation in `Start()`:

```csharp
    /// <summary>What actually generated the current level. Submitted with a score so the
    /// server can confirm the run was played on the dungeon it claims.</summary>
    public int LastUsedSeed { get; private set; }
```

Immediately before the generation work in `Start()`:

```csharp
        // Seed the LAYOUT only. Enemy AI, drops and combat keep drawing from the global
        // stream, so a run is not fully deterministic — the daily challenge needs everyone
        // on the same dungeon, not in the same fight.
        int seed = LevelSeed.Pending ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        LastUsedSeed = seed;

        UnityEngine.Random.State stateBeforeGeneration = UnityEngine.Random.state;
        UnityEngine.Random.InitState(seed);
```

Immediately after generation finishes (after the last `CreateRoomOutline` / centre instantiation):

```csharp
        // Restore, so gameplay randomness is unaffected by the layout seed.
        UnityEngine.Random.state = stateBeforeGeneration;
        LevelSeed.Clear();
```

**Placement matters.** If generation is spread across a coroutine, the restore must run after the last generating call, not at the end of `Start()`. Read the whole method before editing and report where you placed it.

- [ ] **Step 5: Prove determinism empirically**

A unit test cannot exercise scene generation. Add a temporary Editor entry point to `Assets/Editor/SdAutomation.cs` that loads a level scene twice with the same `LevelSeed`, records the generated room positions each time, and compares:

```csharp
    public static void VerifyGenerationDeterminism()
    {
        var problems = new System.Collections.Generic.List<string>();
        string[] first = GenerateRoomSignature(4242);
        string[] second = GenerateRoomSignature(4242);
        string[] different = GenerateRoomSignature(9999);

        if (first.Length == 0) problems.Add("generation produced no rooms");
        if (string.Join("|", first) != string.Join("|", second))
            problems.Add("same seed produced DIFFERENT layouts — generation is not deterministic");
        if (string.Join("|", first) == string.Join("|", different))
            problems.Add("different seeds produced the SAME layout — the seed is being ignored");

        Debug.Log("rooms with seed 4242: " + first.Length);
        Finish("VerifyGenerationDeterminism", problems);
    }
```

Implement `GenerateRoomSignature(int seed)` to open a level scene, set `LevelSeed.Set(seed)`, run generation, and return the sorted room world positions as strings.

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
pgrep -fl "Unity.app/Contents/MacOS/Unity" && echo "KILL THE EDITOR FIRST" && exit 1
"/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$(pwd)" \
  -executeMethod SdAutomation.VerifyGenerationDeterminism -logFile - 2>&1 | tail -20
echo "EXIT=$?"
```

Both checks must pass. **If the same seed produces different layouts, stop and report** — some other source of randomness is involved and the daily challenge cannot be built until it is found.

- [ ] **Step 6: Run tests and commit**

```bash
./run-tests.sh
git add Assets/Scripts/Levels "Assets/Scripts/Object Managers/LevelGenerator.cs" \
        Assets/Tests Assets/Editor
git commit -m "Nivele: generare reproductibila prin samanta, cu restaurarea starii aleatoare"
```

---

### Task 2: Firebase project and OnlineService

**Blocked on a human step:** creating the Firebase project, enabling Blaze with a budget cap, and downloading `google-services.json` / `GoogleService-Info.plist`. Document what is needed, then stop and report rather than guessing project IDs.

**Files:**
- Modify: `Packages/manifest.json` (Firebase Unity SDK)
- Create: `Assets/Scripts/Online/OnlineService.cs`
- Create: `docs/superpowers/notes/firebase-setup.md`

- [ ] **Step 1: Write the setup runbook** — project creation, Blaze + budget alert threshold, Anonymous Auth, Firestore in production mode, and where the config files go.
- [ ] **Step 2: Add the Firebase Unity SDK** (Auth, Firestore, Functions, Remote Config only — not Analytics, which duplicates the crash/analytics decision made elsewhere).
- [ ] **Step 3: Write `OnlineService`** — owns init and anonymous sign-in, exposes a single `IsOnline` flag every other service reads, and **fails silently to offline**. Nothing may await it during startup.
- [ ] **Step 4: Verify the game still launches and plays with the network disabled.** This is the acceptance test for the whole task.

---

### Task 3: Cloud save — and the decision it reverses

`SaveIntegrity` signs saves with an HMAC keyed on `SystemInfo.deviceUniqueIdentifier`. That was correct with no backend — it blocks save-sharing as a piracy vector — but it means **a save does not survive a device change**. Once players buy gems with real money, losing them on a new phone is a refund request and a one-star review.

This task supersedes that decision. The local HMAC stays and keeps protecting the local file; the cloud copy becomes authoritative on a fresh install.

**Files:**
- Create: `Assets/Scripts/Online/CloudSaveService.cs`
- Modify: `Assets/Scripts/Save/SaveService.cs`

- [ ] **Step 1: Add a cloud tier above `Fresh` in the recovery ladder.** On launch, if a cloud save exists and no valid local save does, write the cloud copy locally and **re-sign it for this device**.
- [ ] **Step 2: Implement the conflict policy from the spec.** Prefer the higher `gems + lifetime spend`, then the later `updatedAt`. **Never discard the loser** — write it to `players/{uid}/save/conflicted` so a support request can be answered.
- [ ] **Step 3: Debounce writes.** Push on meaningful change only (purchase, unlock, run end), never per frame.
- [ ] **Step 4: Test the device-change path explicitly** — a profile written under one device id must load under a different one via the cloud tier.

---

### Task 4: Score model and leaderboards

Ranking is **depth → kills → time**, as a Firestore compound index rather than a computed scalar, so the components stay legible and no weighting needs tuning.

**Files:**
- Create: `Assets/Scripts/Online/RunScore.cs`, `LeaderboardService.cs`
- Create: `functions/submitScore.js`
- Create: `Assets/Tests/EditMode/RunScoreTests.cs`

- [ ] **Step 1: `RunScore`** — `depth = level * 100 + subLevel`, plus kills, duration, shipId, seed. Unit-test the depth encoding and the comparator at every tie level.
- [ ] **Step 2: `submitScore` Cloud Function** — the only write path. Validates plausibility bounds (kills against reachable depth, duration against a floor for that depth, depth against level count), rate-limits per uid, and writes `verified: false` for top-100 entries.
- [ ] **Step 3: Firestore rules** — clients may read boards and write only their own `players/{uid}` document. All `entries/` writes denied.
- [ ] **Step 4: `LeaderboardService`** — submit fire-and-forget with a local retry queue; fetch a page; fetch own rank. Never blocks.

---

### Task 5: Daily challenge

The retention feature, and the reason Task 1 exists.

- [ ] **Step 1: `rotateDailyChallenge`** — scheduled 00:00 UTC, generates and publishes the next seed. **Seeds are never client-supplied.**
- [ ] **Step 2: `DailyChallengeService`** — fetches today's seed, reports whether the player already submitted, hands the seed to `LevelSeed.Set` before loading the level.
- [ ] **Step 3: Offline fallback** — with no connection, use `LevelSeed.ForDate(DateTime.UtcNow)` so the player still gets a coherent run; do not submit the score.
- [ ] **Step 4: Gem reward** — the daily challenge grants gems (user decision). Put the amount in Remote Config, not a constant: it is simultaneously the strongest retention lever and the largest economy leak in this design.

**Two decisions still open** (from the spec's §10, unresolved): how much the reward is, and whether the challenge uses a fixed ship or free choice. Fixed makes it about play rather than about who has unlocked Valiant. **Ask before implementing; do not pick silently.**

---

### Task 6: Remote Config

- [ ] Compiled-in defaults for every key, so a fetch failure is invisible to the player.
- [ ] Keys: ship unlock prices, star upgrade curve, starting gems, `coinsMultiplier`, `videoRewardGems`, revive cost, `countToAd` cadence, blueprint drop chances, `killMilestones`, daily-challenge reward, and the `submitScore` plausibility bounds.
- [ ] Verify the game behaves identically with Remote Config unreachable.

---

## Out of scope for this plan

| Deferred | Why |
|---|---|
| Input-trace replay verification | Needs full run determinism (this plan gives layout determinism only) |
| Account linking to Google/Apple | Anonymous-only for v1; first follow-up if churn shows identity loss hurting |
| Friends, chat, guilds | No social graph in v1 |
| Server-side receipt validation | Shares the Cloud Functions setup; belongs to the ads-and-IAP plan's follow-up |
