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
    /// Single source of truth for ship data, keyed by ASSET FILE NAME. The order of this table
    /// is arbitrary and behaviourally irrelevant — lookups are by name, never by index.
    ///
    /// It deliberately does NOT match LegacyShipIdMap, and must never be reconciled to it.
    /// LegacyShipIdMap is 13 entries in MainMenu.shipPrefabs scene order and maps a 2021 save's
    /// isUnlocked[] indices to ships; this table is 14 entries (it also covers the tutorial ship,
    /// which is absent from shipPrefabs) and carries authoring values. Reordering LegacyShipIdMap
    /// to match this table would silently hand returning players the wrong ships.
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

    private const string ShipsFolder = "Assets/Prefabs/Spaceships/";

    /// <summary>Loads every Spaceship asset, keyed by asset file name.</summary>
    ///
    /// Only assets directly inside ShipsFolder are template assets. The folder also contains
    /// a "Spaceships Instances" subfolder of runtime-mutated clones (e.g. "Apollo Instance")
    /// used at play time; those are not part of the shipId source of truth and are excluded
    /// by requiring no further "/" after the folder prefix.
    public static Dictionary<string, Spaceship> LoadAllShips()
    {
        var byName = new Dictionary<string, Spaceship>();
        foreach (string guid in AssetDatabase.FindAssets("t:Spaceship"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith(ShipsFolder)) continue;
            if (path.Substring(ShipsFolder.Length).Contains("/")) continue; // skip subfolders

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

        int expectedUnlocked = 0;
        foreach (ShipSpec s in Ships) if (s.unlocked) expectedUnlocked++;
        if (unlockedCount != expectedUnlocked)
            problems.Add("expected exactly " + expectedUnlocked +
                         " unlocked ships on a fresh install, found " + unlockedCount);
    }
}
