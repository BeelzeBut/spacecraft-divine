using NUnit.Framework;

namespace SpaceshipDivine.Save.Tests
{
    public class LegacyShipIdMapTests
    {
        [Test]
        public void LegacyShipIdMapMatchesTheVerifiedSceneOrder()
        {
            // Verified against MainMenu.shipPrefabs in Assets/Scenes/Main Menu.unity by
            // resolving each GUID against Assets/Prefabs/Spaceships/*.asset.meta.
            // This ordering is the ONLY thing tying a 2021 save's isUnlocked[] index to a ship.
            // If it changes, returning players get the wrong ships. Do not "fix" this test to
            // match the code — re-verify against the scene.
            string[] expected =
            {
                "grey_byrd", "bubu", "apollo", "the_argon", "razor", "the_reaper",
                "white_ripper", "hot_talon", "bat_oh_no", "vickers", "lunar_hunter",
                "warspite", "valiant"
            };

            Assert.AreEqual(expected.Length, LegacyShipIdMap.Count);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], LegacyShipIdMap.IndexToId(i), "index " + i);
        }
    }
}
