# Spaceship Divine — Online Services Design

**Date:** 2026-09-08
**Author:** Marco Buga (with Claude)
**Status:** Approved for planning
**Branch:** `production-release`
**Companion spec:** `2026-09-08-production-release-design.md` (monetisation, progression, haptics)

---

## 1. Purpose

Spaceship Divine is entirely offline. This spec adds the online layer: leaderboards, a shared
daily challenge, cloud save, server-side validation, and remote economy tuning.

The motivating insight is specific to this game. It is a roguelite with procedural generation,
which means **every player can be given the same dungeon from the same seed**. That turns a
single-player score into a fair competition, and it is the strongest retention mechanic
available to this genre.

### Constraints

- **Nothing is pushed to GitHub until after the diploma defense on 11 Sep 2026.**
- Backend must serve both Android and iOS from one dataset — a player's rank is global, not
  per-platform.
- The game must remain fully playable offline. Every online feature degrades to a working
  offline state; none of them may gate the core loop.

---

## 2. Blocking finding: generation is not seeded

`Assets/Scripts/Object Managers/LevelGenerator.cs` calls `Random.Range` throughout (lines 52,
55, 70, 76, 188, 275) and never calls `Random.InitState`. Generation is therefore unseeded and
irreproducible.

Every competitive feature in this spec depends on fixing that. The change is small — seed
`UnityEngine.Random` at the start of generation and thread the seed through room population —
but it is a hard prerequisite, and it must be verified by generating the same seed twice and
comparing the resulting room graph.

**Determinism scope.** Seeding the generator makes the *dungeon layout* reproducible. It does
not make the whole run deterministic: enemy AI, drops and combat also draw from `Random`. Full
run determinism is required only if input-replay verification is added later (§7); the daily
challenge needs layout determinism alone.

---

## 3. Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Backend | **Firebase** | Firestore, Anonymous Auth, Cloud Functions, Remote Config and Crashlytics in one SDK; same Google account as Play and AdMob; `firebase` CLI already installed |
| Billing tier | **Blaze, with a hard budget alert at a low threshold** | Cloud Functions require it. Server-side validation of both scores and IAP receipts is not possible without them, and at this DAU the real cost is near zero |
| Score formula | **Depth first, then kills, time as tiebreak** | Rewards what a roguelite is about; depth is the component most verifiable against the seed, so it is also the hardest to forge |
| Identity | **Anonymous auth + player-chosen display name** | No signup wall on first launch. Display names are moderated |
| Achievements | Play Games Services + Game Center | Native, discoverable, free, and complementary to the Firestore leaderboards rather than a replacement |

### Non-goals

- Friends lists, chat, guilds, or any social graph.
- Real-time multiplayer or co-op.
- Input-trace replay verification (see §7 — designed for, not built now).
- Account linking to Google/Apple. Deliberately deferred; see the risk in §8.

---

## 4. Architecture

### 4.1 Data model (Firestore)

```
players/{uid}
  displayName        string, moderated, unique-ish
  createdAt          timestamp
  lastSeenAt         timestamp
  platform           "android" | "ios"

players/{uid}/save/current          <- cloud save, one document
  profileJson        string   (the PlayerProfile payload from the save core)
  schemaVersion      int
  updatedAt          timestamp
  deviceId           string   (last writer, for conflict reporting)

dailyChallenges/{yyyy-MM-dd}
  seed               int      (server-generated, never client-supplied)
  publishedAt        timestamp
  expiresAt          timestamp

dailyChallenges/{yyyy-MM-dd}/entries/{uid}
  displayName        string
  depth              int      (level * 100 + subLevel)
  kills              int
  durationSeconds    float
  shipId             string
  submittedAt        timestamp
  verified           bool

leaderboards/{boardId}/entries/{uid}
  boardId in: "global_alltime", "ship_<shipId>"
  same shape as daily entries
```

Ranking order is `depth DESC, kills DESC, durationSeconds ASC` — the agreed formula, applied
as a compound Firestore index rather than a computed scalar, so the components stay legible to
players and no weighting needs tuning.

### 4.2 Client services

Each is a small, independently testable class. None may block startup or the core loop.

- **`OnlineService`** — owns Firebase init, anonymous sign-in, and a single `IsOnline` state
  every other service reads. Fails silently to offline.
- **`LeaderboardService`** — submits a completed run, fetches a page of a board, fetches the
  local player's rank. All calls are fire-and-forget with a local queue for retry.
- **`DailyChallengeService`** — fetches today's seed, reports whether the player has already
  submitted, and hands the seed to `LevelGenerator`.
- **`CloudSaveService`** — pushes on meaningful change (purchase, unlock, run end), pulls on
  launch. Conflict policy in §5.
- **`RemoteConfigService`** — typed accessors with compiled-in defaults, so a config fetch
  failure is invisible to the player.

### 4.3 Cloud Functions

| Function | Trigger | Job |
|---|---|---|
| `rotateDailyChallenge` | Scheduled, daily 00:00 UTC | Generate and publish the next seed. Seeds are never client-supplied |
| `submitScore` | Callable | Validate and write a leaderboard entry. The only write path — clients cannot write entries directly |
| `validateReceipt` | Callable | Verify a Play or Apple purchase receipt, then grant entitlement server-side |
| `moderateDisplayName` | Callable | Profanity and impersonation filter on name set/change |

Firestore security rules deny all client writes to `entries/` and `dailyChallenges/`. A client
may read boards and write only its own `players/{uid}` document and its own save.

### 4.4 Server-side receipt validation

The companion spec deferred this with the reason "no backend exists". That reason is now gone.
`validateReceipt` verifies against the Play Developer API and Apple's verifyReceipt endpoint,
then writes the entitlement (`removeAdsOwned`, gem grants) into the player's cloud save. This
closes the IAP fraud hole properly, rather than relying on the client-side local validation the
companion spec settles for.

---

## 5. Cloud save and the device-binding problem

The save core built in the companion plan signs saves with an HMAC keyed on
`SystemInfo.deviceUniqueIdentifier`. That was correct with no backend — it blocks save-sharing
as a piracy vector — but it means **a save does not survive a device change**. Once players buy
gems with real money, losing them on a new phone is a refund request and a one-star review.

This spec supersedes that decision:

- The local HMAC stays, keyed on device, and continues to protect the local file.
- The **cloud save is authoritative** on a fresh install. On launch, if a cloud save exists and
  no valid local save does, the cloud copy is written locally and re-signed for this device.
- **Conflict policy:** if both exist and disagree, prefer the one with the higher
  `gems + lifetime spend` total, then the later `updatedAt`. Never silently discard the loser —
  write it to a `players/{uid}/save/conflicted` document so a support request can be answered.
- Cloud save writes are debounced and only on meaningful change, to stay inside quota.

This is a change to already-implemented Task 2 work in the companion plan. The local integrity
code does not change; the recovery ladder in `FileSaveStore` gains a cloud tier above `Fresh`.

---

## 6. Remote Config

Compiled-in defaults, overridden remotely. Directly serves the companion spec's admission that
every price in the game is an unvalidated 2021 guess.

Tunable without a store release: ship unlock prices, star upgrade cost curve, starting gems,
`coinsMultiplier`, `videoRewardGems`, revive gem cost, interstitial cadence (`countToAd`),
blueprint drop chances, and `killMilestones`.

---

## 7. Anti-cheat

A client-submitted score is trivially forged, and a single impossible entry permanently kills a
leaderboard's credibility. Defence in depth, cheapest first:

1. **Server-issued seeds.** The client never chooses the daily seed. A submission carries the
   date; the function looks up the authoritative seed.
2. **Plausibility bounds** in `submitScore`: reject entries where kills exceed what the reached
   depth can spawn, where duration is below a floor for that depth, or where depth exceeds the
   level count. Bounds live in Remote Config so they can be tightened without a deploy.
3. **Rate limiting** per uid — one daily-challenge entry per day, and a submission ceiling per
   hour on other boards.
4. **Top-N flagging.** Entries in the top 100 are written with `verified: false` and surfaced
   for review before appearing on the public board.
5. **Replay verification** — designed for, not built. Would require full run determinism (§2)
   plus an input trace. Deferred to its own plan.

Accepted residual risk: a determined attacker with the binary can still forge a plausible
score. The goal is to make casual cheating fail and blatant cheating visible, not to make
forgery impossible without server-authoritative simulation.

---

## 8. Risks

| Risk | Mitigation |
|---|---|
| Generation seeding turns out to be non-deterministic across platforms or Unity versions | Verified by a test generating the same seed twice and comparing the room graph, run on both platforms, before any competitive feature is built |
| Anonymous identity lost on device wipe or reinstall | Accepted for v1 and stated plainly in-game before a player invests in a name. Account linking is the deferred fix, and is the first follow-up if churn shows up |
| Firestore or Functions cost spike from abuse | Hard budget alert; rate limiting in `submitScore`; security rules deny direct entry writes |
| An online outage blocks play | Every service degrades to offline; the core loop never awaits a network call |
| Cloud/local save conflict destroys paid currency | Conflict policy in §5 never discards; the losing copy is retained server-side for support |
| Leaderboard sits empty at launch and reads as dead | Seed with the daily challenge first, which concentrates a small player base on one board per day rather than splitting it across many |
| Display names used for abuse | `moderateDisplayName` filter, and a report path in a later plan |

---

## 9. Phases

| # | Phase | Depends on | Notes |
|---|---|---|---|
| 0 | Seed `LevelGenerator`, prove determinism | — | Hard prerequisite for everything competitive |
| 1 | Firebase project, Blaze + budget cap, Anonymous Auth, `OnlineService` | — | Credentials are a one-time human step |
| 2 | Cloud save + conflict policy | Companion plan's save core | Supersedes the device-binding decision in §5 |
| 3 | Score model, `submitScore`, global and per-ship boards | 0, 1 | |
| 4 | Daily challenge: rotation function, seed delivery, daily board | 0, 3 | The retention feature |
| 5 | Remote Config with compiled-in defaults | 1 | Should land before economy tuning |
| 6 | Server-side receipt validation | 1, companion IAP plan | Replaces client-side local validation |
| 7 | Play Games Services + Game Center achievements | 1 | Native, cheap, discoverable |
| 8 | Anti-cheat hardening: bounds, rate limits, top-N flagging | 3, 4 | |

---

## 10. Open items

- Confirm a Firebase project does not already exist for `com.KodaGames.SpaceshipDivine`.
- Choose the budget alert threshold.
- Decide whether the daily challenge grants gems, and how much — it is a strong lever and an
  equally strong economy leak if set wrong.
- Decide whether the daily challenge uses a fixed ship or free ship choice. Free choice is
  simpler; a fixed ship makes the competition purely about play rather than about who has
  unlocked Valiant.
