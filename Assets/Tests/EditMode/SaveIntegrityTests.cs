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
