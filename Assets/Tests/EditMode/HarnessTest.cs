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
