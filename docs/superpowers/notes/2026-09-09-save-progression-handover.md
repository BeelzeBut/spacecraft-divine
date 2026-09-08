# Save & Progression — Handover

**Branch:** `production-release` (33 commits over `main` at `750a0ae`)
**Completed:** 9 Sep 2026 · 44 EditMode tests green
**Plan:** `docs/superpowers/plans/2026-09-08-save-and-progression.md`
**Spec:** `docs/superpowers/specs/2026-09-08-production-release-design.md`

This note exists so the decisions and known gaps from that run survive the scratch workspace.
Read it before starting the `DataHolder` → `SaveService` cutover.

---

## What shipped

| | |
|---|---|
| Save core | Versioned JSON envelope, HMAC integrity, atomic write with backup ladder, legacy v0→v1 migration, `SaveService` facade |
| Progression | Three starter ships (Grey Byrd, Apollo, The Argon) + the tutorial ship; ten ships locked |
| Economy | Gem unlock prices cut ~35% |
| Security | €9.99 real-money ship no longer granted free; grants only via a confirmed store callback |
| Bug fix | `Bubu.asset` no longer ships with its blueprint pre-collected |
| Cleanup | 4 dead scripts + 1 stale scene removed; `.gitignore` now covers ~1.6 GB of build output |

**The structural win:** ship design data no longer round-trips through player saves. Before this,
`SavePlayerData()` copied ship stats out of the ScriptableObjects and `LoadPlayerData()` wrote them
back, freezing v1.0 balance onto every device permanently. `PlayerProfile` now has no field capable
of holding a ship stat, and a reflection test enforces that.

---

## MUST DO FIRST in the cutover plan

### 1. `DataHolder.Load()`'s `HasKey("save")` branch is the ONLY thing grandfathering 2021 players

`MigrationV0ToV1` and `SaveService` are fully built and tested but are called from **no live code
path**. The single production consumer of the entire `SpaceshipDivine.Save` assembly is
`PlayerProfile.IsStarterShip` at `DataHolder.cs:141`.

A returning player is protected *incidentally*: the 2021 build wrote `PlayerPrefs["save"]`, so
`Load()` takes the deserialize branch and never reaches the starter-only gate. **Re-verify that
branch before restructuring it**, and add a test — it currently has none, because `DataHolder` is a
MonoBehaviour bound to `PlayerPrefs` and scene state.

### 2. Ship instances have no `shipId`

The 13 assets in `Assets/Prefabs/Spaceships/Spaceships Instances/` carry no `shipId`.
`MainMenu.RefreshData()` copies `isUnlocked` and `priceToUnlock` to instances but not `shipId`, and
`DataHolder.LoadPlayerData()` sets `selectedShip` from `shipInstances`. So `selectedShip.shipId` is
empty, and `RunState.selectedShipId` would silently persist `""`.

**The obvious fix is wrong.** The copy block is inside `if (shipInstances[i].selected == false)`.
A `shipId` copy placed there skips **the selected ship** — the only one whose id is needed. Put it
below the guard, alongside `secondaryGun` (`MainMenu.cs:155`).

Do not give the instance assets their own `shipId`: they are runtime clones whose purpose is to
avoid dirtying the source assets, and duplicating canonical data means keeping it in sync forever.

### 3. `SaveEnvelope.version` has no downgrade policy

`version` is written (`FileSaveStore.cs:52`) and never read (`TryLoad`). A future-version envelope
loads on signature match alone. Deliberately left open: rejecting it would reset the profile and
lose data on an app downgrade. **The migrator must decide this explicitly.**

### 4. `SaveService` never falls back to the legacy escape hatch

`Load()` returns immediately when `store.Exists()`, even on `RejectedAndReset`. The HMAC is salted
with `SystemInfo.deviceUniqueIdentifier`, and Android Auto Backup restores both `persistentDataPath`
and `PlayerPrefs` to a new device — so post-cutover that player gets a reset profile while an intact
legacy blob sits unread. `ISaveStore.LastReadOutcome` already exposes what is needed.

### 5. IL2CPP stripping has never been exercised

`MigrationV0ToV1` uses `XmlSerializer` in a **new** assembly under `managedStrippingLevel: Android: 1`,
and there is no `link.xml`. EditMode tests run on Mono in the Editor and prove nothing here. Silent
field-stripping means every 2021 unlock reads `false`. Add a `link.xml` preserving
`SpaceshipDivine.Save` and smoke-test one real 2021 save blob on a device before the cutover ships.

---

## Blocks the store release (not the merge)

**The Bat-Oh-No purchase button is dead.** `MainMenu.cs:604` → `IAPManager.BuyShip()` → a
`Debug.LogWarning` and nothing else. Fail-closed is correct and deliberate — but combined with the
ship now defaulting to locked, a new player sees a locked ship with a button that does nothing.
Hide it, disable it, or show "coming soon" until the ads-and-IAP plan wires a real product.
Bat-Oh-No also has **no published SKU** in either store; that is a pricing decision, not a task.

---

## Known-weak tests

Four tests in this plan had assertions that looked right but could not catch a regression. Three
were fixed during the run; one remains:

- `RunFromXmlCarriesForwardLevelSubLevelAndRespawns` asserts `respawnsRemaining == 1`, which is both
  the legacy default and the `RunState` default — it would pass even if the field were never copied.
  One-line fix: set it to `2` in the fixture. The production code is confirmed correct by reading.

Also unfixed, by decision:
- No test pins `SaveIntegrity.Verify()` against a wrong-length signature.
- No test exercises `FileSaveStore.Write()`'s failure path — the one untested branch of "never
  silently lose progress".
- `SdAutomation`'s three verifiers are wired into nothing. `run-tests.sh` runs EditMode only, and
  playing in the Editor permanently dirties the ship assets, so the gate can be silently broken
  between now and release. **Add them to CI.**

---

## Two comments that must not be "cleaned up"

1. **`Blueprint.UnlockCollectible()`** — `priceToUnlock = 1` is a boolean meaning "blueprint
   collected, ship claimable", not a price. `MainMenu.UnlockShip()`'s `canBeUnlockedInGame` branch
   redeems it. An earlier draft of this plan mistook it for a bug and would have stranded every
   blueprint players had earned.

2. **`SdAutomation.Ships`** — its order is arbitrary and must **never** be reconciled to
   `LegacyShipIdMap`. The two differ in 9 of 13 positions by design. `LegacyShipIdMap` is the
   verified scene order and is the only thing tying a 2021 save's `isUnlocked[]` index to a ship;
   `LegacyShipIdMapTests` pins it. Reordering it to match `SdAutomation` would hand a player who
   paid €9.99 for Bat-Oh-No a different ship.

---

## Repo hygiene

`Assets/Editor/BuildAndroid.cs`, `BuildIOS.cs` and `Assets/Editor.meta` are **untracked** — they
exist on disk in no commit on any branch, so a fresh clone lacks them, and the release-automation
plan's Task 2 modifies them. `run-tests.sh` hardcodes the Unity path and needs a `$UNITY` override
before CI.
