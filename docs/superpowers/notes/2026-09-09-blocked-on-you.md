# What is blocked, and what unblocks it

**As of:** 9 Sep 2026 · branch `production-release`, 54 commits over `main` (`750a0ae`, untouched)
**Suite:** 86 EditMode tests green · **Nothing pushed to any remote.**

Every remaining task needs an account, a device, or a download. This is the list.

---

## 1. Test the build on your phone — 10 minutes, unblocks the most

```bash
cd /Users/bugamarco/Documents/GitHub/spacecraft-divine
adb install -r "Build Android/SpaceshipDivine.apk"
```

Debug-signed: fine for your phone, rejected by Play. Rebuild any time with `make android-apk`.

**If you still have a device with the 2021 build on it, capture its save BEFORE installing.**
It is the only copy of that player's history, and it is the only way to prove the migration:

```bash
adb shell "run-as com.KodaGames.SpaceshipDivine cat shared_prefs/com.KodaGames.SpaceshipDivine.v2.playerprefs.xml" > ~/legacy-save-backup.xml
```

Then install **over** the old build — do not uninstall — and watch for the migration line:

```bash
adb logcat -c && adb logcat -s Unity:V | grep -iE "Migrated|ships preserved|UIFeedbackInstaller|Haptic"
```

Expect exactly one `Migrated legacy save to v1: N ships preserved`, and
`UIFeedbackInstaller: attached to 72 selectables in Main Menu`.

**What to check, in order of how much it would hurt to get wrong:**

| Check | Why it matters |
|---|---|
| Every ship you owned is still owned | Migration is add-only, but only a real save proves it |
| Ship levels unchanged | Carried by migration; never exercised on real data |
| Gem count unchanged | — |
| The app launches at all | The Android manifest was wrong until today; the fix is verified in the APK's manifest but not on hardware |
| Haptics feel right | Nobody has felt a single pulse yet — see the tuning questions in the haptics plan, Task 6 |
| A locked ship's buy button gives a "no" buzz, not a normal click | New behaviour; `Reject` is otherwise unreachable |

If `N` is lower than what that player owned, **stop and tell me** — do not play further, because
the first `Save()` writes the migrated profile over the good one.

---

## 2. Decide the `removeads` product id — blocks the whole ads plan

`removeads` **has no published SKU in either store.** Three places disagreed about its id:

- `IAPManager` registered the bare `removeads` — a string in no catalog, so it could never resolve
- `Assets/Resources/IAPProductCatalog.json` declares `com.kodagames.spaceshipdivine.removeads`
- `GooglePlayProductCatalog.csv` does not mention it at all

I standardised on **`com.kodagames.spaceshipdivine.removeads`**, because all four gem SKUs carry
that prefix and nothing has ever been purchased against either spelling. **Create the SKU in Play
Console with exactly that string.** If you would rather use a different id, say so — it is one
constant in `Assets/Scripts/Store/ProductIds.cs`, and changing it now is free. After a single
purchase exists, it never is again.

Also: `gems10000` was published in Play but never registered in code, so the store advertised a
product the app could not sell. It is registered now.

---

## 3. Accounts I cannot create

| Need | Blocks | Notes |
|---|---|---|
| **AdMob account + ad unit ids** | Ads plan Tasks 4–5 | App ids and unit ids go in the manifest and `AdService`. Also needs the UMP consent SDK. |
| **Firebase project (Blaze, with a budget cap)** | Online services Tasks 2–6 | Leaderboards, daily challenge, cloud save. Task 1 (seeded levels) is already done and needs nothing. |
| **Google Play license public key** | Receipt validation (Ads Task 3) | Play Console → Monetisation setup. Feeds Unity's IAP Tangle obfuscator. |

---

## 4. Unity editor upgrade — required before any store upload

Measured, not recalled: this editor builds `targetSdkVersion: 32`, and every native library is
4 KB-aligned (`LOAD align = 0x1000`). Play has required API 36 for new submissions since
**31 Aug 2026 — nine days ago** — and anything targeting 35+ must support 16 KB pages.

`libunity.so` ships prebuilt inside the editor, so no project setting can fix the alignment.

**Install `2022.3.62f3` — the newest and last release in the 2022.3 LTS line. Not Unity 6.**
(An earlier version of this note said `2022.3.65f1`; that version does not exist. See the
correction in the editor-upgrade note.) Staying in the 2022.3 line keeps URP 14, the Input System and
TextMeshPro on the versions this project already resolves. Full reasoning and the verification
sequence: `docs/superpowers/notes/2026-09-08-editor-upgrade-decision.md`.

After installing, the gate is `./run-tests.sh` passing, then a build showing
`targetSdkVersion:'36'` **and** `LOAD align = 0x4000`.

---

## 5. One thing only Play Mode can answer

`LevelSeed` is wired into `LevelGenerator` and unit tested, but **nothing has confirmed that the
same seed produces the same dungeon**, because generation runs in `Start()` and depends on
`DataHolder.instance` and `Physics2D`. Step 5 of the online-services plan describes the check.
Until it passes, the daily challenge cannot be trusted to give everyone the same level.

---

## Where things stand

| Done | |
|---|---|
| Save & progression | Complete, plus the cutover: migration now actually runs |
| Haptics | Tasks 1–5; only device tuning left |
| Level seeding | Complete except the Play Mode determinism check |
| Build automation | `make android-apk` / `android-aab` / `ios` / `test`, exit codes verified |
| Product registry | Complete; three catalog disagreements closed |
| Editor upgrade decision | Answered with measurements |

| Not started | Blocked by |
|---|---|
| Ads Tasks 2–5 | AdMob account, `removeads` SKU |
| Receipt validation | Play license key |
| Online services Tasks 2–6 | Firebase project |
