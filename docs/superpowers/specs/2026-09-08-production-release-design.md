# Spaceship Divine — Production Release Design

**Date:** 2026-09-08
**Author:** Marco Buga (with Claude)
**Status:** Approved for planning
**Branch:** `production-release`

---

## 1. Purpose

Take Spaceship Divine from its current state — a diploma-project build with monetisation
stubbed out and all content unlocked — to a shippable free-to-play mobile game on Google
Play and the App Store.

Four capabilities do not currently exist and must be built:

1. Working ads (rewarded video and interstitials).
2. Hardened in-app purchases that cannot be bypassed.
3. An active ship progression gate, so content is earned rather than granted.
4. Haptic feedback across the UI and gameplay.

Two structural problems block all of the above and must be fixed first:

5. The save system writes ship *design* data into player saves, which makes post-launch
   rebalancing impossible.
6. The editor version cannot produce a build that either store will currently accept.

### Non-goals for this release

- Cloud save and user accounts.
- Localisation beyond English.
- New gameplay content (ships, enemies, levels, abilities).
- Server-side receipt validation (no backend exists; local validation is the correct
  level for this scale).
- Mediation / ad waterfall optimisation.

### Constraints

- **Nothing is pushed to GitHub until after the diploma defense on 11 Sep 2026.** All work
  lands on the `production-release` branch. `main` stays presentable for the demo.
- The Google Play listing for `com.KodaGames.SpaceshipDivine` still exists in the Play
  Console (unpublished/inactive). The app is updated **in place**. The application
  identifier — including its mixed-case spelling — must not change; changing it creates a
  new app and forfeits the existing reviews, install base and published IAP SKUs.
- Android and iOS ship together. iOS is built and pushed to TestFlight at the end of every
  phase, never deferred to the end of the project.

---

## 2. Current state (audit findings)

Findings that the design responds to. Line references are to the state of `main` at
commit `750a0ae`.

### 2.1 Ads: nothing is live

`Assets/Scripts/Object Managers/Monetization.cs` is entirely commented out. It was written
against the Unity Ads **3.x** `IUnityAdsListener` API, which does not compile against the
installed `com.unity.ads@4.4.2` — that is why it is commented rather than merely disabled.

The component *is* attached in 8 places (`Main Menu.unity`, `Main Menu Before Demo.unity`,
`MainMenuCanvas.prefab`, the four level `Manager.prefab`s, and `Ability Manager.prefab`),
so scene wiring for an ad service already exists.

Three call sites are stubbed and dead:

| Call site | Intended behaviour |
|---|---|
| `MainMenu.PlayRewardedAd()` (line 688) | Rewarded video granting `videoRewardGems = 200` |
| `GameManager.ReviveWithAd()` (line 429) | Rewarded video granting a revive |
| `LevelLoader.LoadLevel()` (line 72) | Interstitial every 3rd level load via `countToAd` |

Two landmines in the commented code must not be reproduced:

- `DisplayRewardedAd()` contained `while (!Advertisement.IsReady()) continue;` — a busy-wait
  on the main thread that would hard-freeze the application.
- `LevelLoader`'s `countToAd == 0` branch bypasses the loading screen entirely and, because
  `shouldDisplayAd` is false, never resets the counter.

`com.unity.ads` (legacy Unity Ads) is a dead end. This is a rebuild, not a re-enable.

### 2.2 IAP: half-wired, and gives content away

- `IAPManager.BuyRemoveAds()` has an **empty method body**. `MainMenu.ClickPurchaseButton(0)`
  calls it and logs `"Removed Ads"`. Nothing happens. `removeads` also has no branch in
  `ProcessPurchase`, and is absent from `GooglePlayProductCatalog.csv`.
- `MainMenu.UnlockShip()`, `canBeBoughtWithCurrency` branch (line 594): sets
  `isUnlocked = true` and saves, **with no purchase call at all**. `Bat-Oh-No` is priced at
  €9.99 and is granted on tap.
- Same function, `canBeUnlockedInGame` branch (line 601): grants the ship free when
  `priceToUnlock != 0`. This reads as the same bug but **is correct** — see §2.3. It is
  blueprint redemption, and `priceToUnlock` is being used as a boolean there.
- `IAPManager.InitPrices()` loops `for (i = 1; ...)` over `products.all` and hard-indexes
  `shop.priceTexts[i]`. It skips product 0 and depends on store return order.
- `GooglePlayProductCatalog.csv` publishes a `gems10000` SKU that `ProcessPurchase` does not
  handle.
- No receipt validation. `Assets/Scripts/UnityPurchasing/` does not exist, so there is no
  Tangle obfuscation.
- `Purchaser.cs` and `LocalisedPrices.cs` are Unity sample boilerplate, referenced by no
  scene or prefab. `Purchaser` would call `UnityPurchasing.Initialize` a second time if it
  were ever placed in a scene.

### 2.3 Progression: the gate is two lines

All 14 ship `.asset` files ship with `isUnlocked: 1`. In addition, `DataHolder.Load()`
unconditionally unlocks everything when creating a fresh save:

```csharp
dataSaved = new SaveData();
for (int i = 0; i < mainMenu.shipPrefabs.Count; i++)
    dataSaved.isUnlocked[i] = true;
```

The economy underneath is designed and intact: gem price tiers, star upgrades at
`500 * 2^(level-1)` capped at level 5, three in-game unlock ships with instruction text,
coins converted to gems at `coinsMultiplier = 3`, and 350 starting gems.

The three in-game unlock ships (`Vickers`, `Warspite`, `Bubu`) are served by a blueprint
system that already works end to end: `Enemy.cs:384` flags a boss blueprint drop (Vickers),
`Enemy.cs:396` drops blueprints at `DataHolder.killMilestones` (Warspite, at 1500 kills),
and `RoomChest.randomDrops` handles rare drops (Bubu). `Blueprint.UnlockCollectible()`
records the pickup in `hasBeenUnlocked[dropIndex]` and marks the ship claimable by setting
`priceToUnlock = 1` — an overloading of a price field as a boolean, which the menu's
`canBeUnlockedInGame` branch then redeems.

Two defects sit on top of a working system: `Bubu.asset` ships with `priceToUnlock: 1`, so
its blueprint counts as pre-collected on a brand-new save, and the overloading itself is
undocumented at every site that touches it.

### 2.4 Save system: the largest structural problem

`Spaceship` is a `ScriptableObject`. `DataHolder.SavePlayerData()` copies ship *design*
values — `priceToUnlock`, `maxHealth`, `spread`, `damageMultiplier`, `maxMoveSpeed`,
`critChance`, `attackMultiplier`, `defenseMultiplier`, `speedMultiplier`, `damageReduction`
— out of the SO assets into the save file. `LoadPlayerData()` writes them back onto the
assets.

Two consequences:

1. **Ships can never be rebalanced after launch.** Every existing player's save overwrites
   new design values on load. Version 1.0 stats are frozen onto their device permanently.
   Shipping a tunable gem economy on top of this is shipping an economy that cannot be tuned.
2. `MainMenu.UnlockShip()` mutates the SO asset directly. In the Editor this permanently
   dirties the `.asset` file on disk. The parallel `shipInstances` list exists as a
   workaround for exactly this aliasing problem.

Additionally: `SaveData` uses fixed-size parallel arrays (`bool[50]`, `float[50]`,
`bool[100]`) with no schema version field, serialised as XML into `PlayerPrefs` as a single
string. The first structural change breaks every existing save. There is no integrity check,
so gem balances are trivially editable.

### 2.5 Build and store readiness

| Setting | Current value | Problem |
|---|---|---|
| Unity editor | `2022.3.14f1` | Almost certainly cannot emit the targetSdk Play now requires; likely predates 16 KB page-size support |
| `AndroidTargetSdkVersion` | `0` (Auto) | Must be pinned explicitly |
| `applicationIdentifier` | `com.KodaGames.SpaceshipDivine` | Correct — must be preserved exactly, mixed case included |
| `AndroidTargetArchitectures` | `3` (ARMv7 + ARM64) | Correct |
| `scriptingBackend` Android | `1` (IL2CPP) | Correct |
| `bundleVersion` / `AndroidBundleVersionCode` | `7` / `7` | Needs bump |
| `CrashReportingSettings.m_Enabled` | `0` | No crash visibility |
| Consent / privacy | Absent entirely | No CMP, no ATT, no privacy policy, no Data Safety form, no iOS privacy manifest |

Correctly handled already: the signing keystore is git-ignored and was never committed, and
SMTP credentials for the feedback form are externalised to a git-ignored config with a safe
absent-config path.

`Assets/Editor/BuildAndroid.cs` is missing its `.meta` file.
`Assets/Scenes/Main Menu Before Demo.unity` is a stale duplicate with 118 button bindings,
not present in build settings.

### 2.6 UI feedback: no central hook

Buttons bind `onClick` directly in the scenes — 122 entries in `Main Menu.unity`, roughly
220 across the game — to methods on `MainMenu` and `GameManager`. UI click sound is played
in exactly 4 places, all within the ship unlock/upgrade path.

There is therefore no single place to add feedback, and most buttons are currently silent.
Editing ~220 scene bindings is not an acceptable approach.

---

## 3. Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Ad network | **Google AdMob** (Google Mobile Ads Unity plugin) | Best global fill without UA spend; same account as Play Console; its UMP SDK is a Google-certified CMP, which solves the GDPR/DMA consent requirement inside the ad dependency rather than as a separate integration |
| Ad load | **Player-friendly** | Rewarded video only for gems and revives (fully opt-in), plus one interstitial every 3rd level load. Protects retention, which matters more than ARPDAU with organic-only discovery |
| Save architecture | **Rearchitect, not patch** | Once gems cost money the save is a financial record; and the SO round-trip must be broken before an economy is layered on it |
| Cloud save | **Out of scope** | Deferred to a later release |
| Haptics | **Own thin wrapper** | ~1 day, no dependency, full control over the semantic vocabulary and rate limiting; matches how the rest of the project is built. Platform preset effects are sufficient for a 2D shooter |
| Starter ships | **Three** — Grey Byrd, Apollo, The Argon | Immediate meaningful choice plus a visible next goal, without a first-hour wall |
| Existing saves | **Grandfather** | Every ship already marked unlocked is preserved. Only new saves get the gate |
| Application identifier | **Unchanged** | Preserves reviews, install base and published SKUs |
| Platforms | **Android + iOS together** | With iOS built and TestFlight'd at the end of every phase, to surface App Store rejections early rather than at the end |

---

## 4. Architecture

### 4.1 Save system

Three types replace the single 60-field `SaveData` blob:

**`PlayerProfile`** — persistent across runs. Gems, unlocked ship IDs, ship levels, tutorial
completion flags, settings (quality, target FPS, music/sound/haptics mutes, controller,
camera speed), owned purchases (`removeAds`), and lifetime stats (total enemies killed,
bosses defeated, levels cleared, runs completed) used by the unlock condition tracker.

**`RunState`** — the current run only. Selected ship ID, run-scoped ship stat modifications,
acquired upgrade indices, current ability and level, level/sublevel, respawns remaining,
run start time, run enemies killed.

**`ShipRuntime`** — a plain (non-`ScriptableObject`) class instantiated per run from a
`Spaceship` SO. All mutation during play happens here. The SO becomes **read-only design
data** and is never written to at runtime.

This last change is what makes post-launch rebalancing possible and eliminates the editor
asset-dirtying bug.

**Persistence.** `ISaveStore` with a `FileSaveStore` implementation writing versioned JSON to
`Application.persistentDataPath`:

- Atomic write: serialise to a temporary file, flush, then rename over the target.
- Integrity: HMAC-SHA256 over the payload, keyed by an application secret combined with a
  device-derived value.
- Recovery ladder: on a failed HMAC or a parse error, fall back to the last-good backup
  file; if that also fails, create a fresh profile. Never crash, never silently zero a
  player's progress without first exhausting the backup.
- A `version` integer is present from v1 onward.

**Migration.** `SaveMigrator` holds an ordered list of migration steps. The `v0 -> v1` step:

1. Reads the legacy XML string from `PlayerPrefs["save"]`.
2. Maps gems, gold coins, tutorial flags, settings, level/sublevel and respawns forward.
3. **Preserves every `isUnlocked[i] == true`** — the grandfather requirement.
4. Maps `shipLevel[]` forward.
5. **Discards all ship design arrays** (`priceToUnlock`, `maxHealths`, `spreads`,
   `damageMultipliers`, `maxMoveSpeeds`, `critChances`, `attackMultipliers`,
   `defenseMultipliers`, `speedMultipliers`, `damageReductions`) — these now come from the
   SOs.
6. Writes v1 and leaves the `PlayerPrefs` blob in place, unread, for one release as an
   escape hatch.

Ship identity moves from **array index to a stable string ID**, so that reordering
`shipPrefabs` can never again silently reassign unlocks.

**Testing.** This subsystem gets real unit tests: migration correctness (including the
grandfather path), tamper rejection, backup fallback, and atomic-write-under-process-kill.

### 4.2 Ad service

`Monetization.cs` is rewritten as `AdService` on the same GameObject wiring, preserving the
8 existing scene references.

- **Initialisation is gated on consent.** UMP consent form runs on first launch; on iOS the
  ATT prompt is sequenced correctly relative to it. No ad SDK initialisation and no ad
  request occurs before a consent decision exists.
- **Preload, then show.** Rewarded and interstitial units are requested ahead of time and
  cached. Show calls check availability and take an explicit unavailable path. No busy-wait.
- **Rewarded placements:** gems (`videoRewardGems = 200`) and revive. The reward is granted
  only on a genuine completion callback, and is written through `PlayerProfile`, never
  directly to a UI field.
- **Interstitial placement:** every 3rd level load, driven by the existing `countToAd`. The
  `LevelLoader` branch that skips the loading screen is fixed so both paths present the
  loading screen and the counter always resets.
- **`removeAds` entitlement suppresses all interstitials.** Rewarded video remains available,
  because it is opt-in and grants the player something.

### 4.3 IAP

- `removeads` added to the Play catalog and App Store Connect, with a real `ProcessPurchase`
  branch setting `PlayerProfile.removeAdsOwned`, suppressing interstitials, and granting the
  additional revive the original comment describes.
- `gems10000` branch implemented — the SKU is already published.
- `InitPrices()` rewritten to look products up **by product ID**, tolerate products the store
  did not return, and stop depending on array ordering.
- Restore Purchases implemented and surfaced as a button. Required by App Store review for
  non-consumables.
- Local receipt validation for both stores, with Tangle obfuscation generated into
  `Assets/Scripts/UnityPurchasing/generated`.
- `PurchaseProcessingResult.Pending` handled so a purchase interrupted mid-flight is
  reprocessed on next launch rather than lost.
- `Purchaser.cs` and `LocalisedPrices.cs` deleted.

### 4.4 Progression gate

- All ship assets set to `isUnlocked: 0` except Grey Byrd, Apollo and The Argon.
- The blanket unlock loop in `DataHolder.Load()` is removed and replaced by a
  `DefaultProfile` factory that marks exactly the three starters unlocked.
- `UnlockShip()`'s `canBeBoughtWithCurrency` branch calls IAP and grants the ship only on a
  successful purchase callback.
- `UnlockShip()`'s `canBeUnlockedInGame` branch is **left exactly as it is**. It reads as the
  same free-grant bug and is not one: it is blueprint redemption, and removing it would
  strand every blueprint a player has earned.
- `Bubu.asset`'s `priceToUnlock` corrected from `1` to `0`, so its blueprint must actually be
  found.
- The `priceToUnlock`-as-boolean overloading is documented at `Blueprint.UnlockCollectible()`.
  `PlayerProfile.MarkBlueprintRedeemable()` replaces it when `DataHolder` is cut over to
  `SaveService`; until then both representations must be kept in agreement.
- Blueprint progress — both `hasBeenUnlocked[]` and the `priceToUnlock == 1` redemption flags
  — is preserved by the v0→v1 migration. Discarding either would revoke earned content.

### 4.5 Ship pricing

Gem unlock prices reduced by approximately 35%, rounded to readable values:

| Ship | Old | New | Cut |
|---|---:|---:|---:|
| Grey Byrd | 0 | 0 | starter |
| Apollo | 750 | **500** | 33.3% |
| The Argon | 1000 | **650** | 35.0% |
| Razor | 1500 | **1000** | 33.3% |
| Hot Talon | 3000 | **2000** | 33.3% |
| White Ripper | 3500 | **2250** | 35.7% |
| The Reaper | 4000 | **2600** | 35.0% |
| Lunar Hunter | 6000 | **3900** | 35.0% |
| Valiant | 7000 | **4500** | 35.7% |

Mean reduction: 34.6%.

Not changed by this pass, and flagged for a separate decision:

- **Star upgrade costs** remain `500 * 2^(level-1)`. The instruction was scoped to ships.
- **`Bat-Oh-No`** remains €9.99. It is a real-money product, not a gem product; discounting
  it is a store pricing decision rather than an economy-balance one.

Because `priceToUnlock` currently round-trips through the save file, these values would not
reach existing players at all under the present architecture. The Phase 1 rearchitecture is
what makes this price change — and every future one — actually deliverable.

### 4.6 Haptics and UI feedback

**`HapticService`** — a singleton with a semantic API, `Haptics.Play(Haptic.Selection)`.
Platform backends:

- iOS: `UIImpactFeedbackGenerator`, `UINotificationFeedbackGenerator`,
  `UISelectionFeedbackGenerator` via a small Objective-C plugin.
- Android: `VibrationEffect` predefined effects (`EFFECT_TICK`, `EFFECT_CLICK`,
  `EFFECT_HEAVY_CLICK`) and waveforms via `AndroidJavaObject`, with the `VIBRATE` permission.
- Editor and desktop: no-op.

Semantic vocabulary:

| Haptic | Trigger |
|---|---|
| `Selection` | Menu navigation, tab change, toggle, slider notch |
| `Confirm` | Start run, purchase success, ship unlock |
| `Reject` | Insufficient gems, tapping a locked ship |
| `ImpactLight` | Player bullet hits an enemy — rate-limited, **default off** |
| `ImpactHeavy` | Player takes damage |
| `Ability` | Ability cast |
| `Reward` | Chest open, upgrade pickup, level clear |
| `Death` | Player death |
| `BossRumble` | Boss spawn and boss defeat |

Required behaviour:

- A haptics toggle in the options menu, persisted in `PlayerProfile` alongside the existing
  music and sound mutes.
- The OS-level haptic setting is respected; the service is inert when the device has haptics
  disabled.
- A global rate limiter — minimum interval between pulses and a per-frame cap — so combat
  cannot degrade into continuous buzz or drain battery.

**`UIFeedbackInstaller`** — on scene load, walks every `Selectable` in the scene and attaches
a `UIFeedback` component providing a default `Selection` haptic plus the UI click sound. A
per-button serialised override selects a different haptic where appropriate (`Confirm` on
Start Run, `Reject` on a locked ship).

This is the mechanism that avoids editing ~220 scene bindings, and it incidentally gives the
roughly 216 currently-silent buttons their click sound.

### 4.7 Observability

- **Crash reporting** enabled (currently `m_Enabled: 0`). Firebase Crashlytics, which pairs
  naturally with AdMob.
- **Analytics events** covering the funnel — install, tutorial complete, first run complete,
  first ship unlock — plus D1/D7 retention, gems earned and spent per session, ad
  request/fill/show/complete rates, and IAP conversion.

This lands **before** the economy tuning pass, because every price in the game is currently
an unvalidated guess from 2021, including the new ones in §4.5.

### 4.8 Compliance

Hosted privacy policy URL; Play Data Safety form; App Store privacy nutrition labels; iOS
`PrivacyInfo.xcprivacy` privacy manifest with required-reason API declarations (and
verification that AdMob and Unity IAP ship their own); both content rating questionnaires
re-run, since ads and IAP change the answers; and a Families policy check.

---

## 5. Phases

Ordered by dependency. Phases 3–6 are largely independent of one another once Phase 1 lands.

| # | Phase | Depends on | Notes |
|---|---|---|---|
| 0 | Editor upgrade and dead-code removal | — | Blocks everything. Highest schedule risk; done first to surface surprises early |
| 1 | Save rearchitecture and migration | 0 | Blocks the economy |
| 2 | Analytics and crash reporting | 0 | Cheap; must precede tuning |
| 3 | Progression gate and pricing | 1 | Blueprint system already works; only the gate, prices and Bubu's flag change |
| 4 | IAP hardening | 1 | |
| 5 | Ads and consent | 0 | UMP gates SDK init |
| 6 | Haptics and UI feedback | 0 | Fully independent — good work to interleave |
| 7 | Compliance and store metadata | 4, 5 | Forms cannot be answered honestly until ads and IAP are final |
| 8 | Economy tuning, then staged release | 2, 3, 7 | Needs live data |

Estimated effort: roughly 7–10 weeks of focused solo work, plus calendar time for testing
tracks and data collection.

---

## 6. Risks

| Risk | Mitigation |
|---|---|
| Editor upgrade breaks URP 14 / Input System / TMP | Phase 0 is first, and is timeboxed; failure surfaces before any other work is invested |
| App Store rejects the ad or IAP implementation late | iOS is built and TestFlight'd at the end of every phase, not at the end of the project |
| Save migration corrupts or drops existing player progress | Unit-tested migration; legacy `PlayerPrefs` blob retained unread for one release; backup fallback ladder |
| Progression gate angers returning players | Grandfathering is a hard requirement of the migration, tested explicitly |
| New gem prices are still wrong | Phase 2 precedes Phase 8; prices are tuned against live data, not intuition |
| Haptics feel noisy in combat | `ImpactLight` defaults off; global rate limiter; user toggle |
| Work leaks onto `main` before the defense | All work on `production-release`; nothing pushed until after 11 Sep 2026 |

---

## 7. Open items

- Confirm which 2022.3.x patch release first supports the required targetSdk and 16 KB page
  size. To be resolved against the current Play policy page in Phase 0.
- Decide whether star upgrade costs follow the ship price reduction (§4.5).
- Decide whether `Bat-Oh-No` stays at €9.99 (§4.5).
- Confirm whether the Apple Developer account is active, since iOS ships in this wave.
- Localisation is deliberately out of scope, but it is the largest available
  organic-discovery lever and deserves a deliberate decision rather than an omission.
