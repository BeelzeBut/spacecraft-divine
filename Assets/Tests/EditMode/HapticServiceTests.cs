using System.Collections.Generic;
using NUnit.Framework;
using SpaceshipDivine.Haptics;

namespace SpaceshipDivine.Haptics.Tests
{
    /// <summary>Records what would have reached the device.</summary>
    public class FakeBackend : IHapticBackend
    {
        public bool Available = true;
        public readonly List<Haptic> Played = new List<Haptic>();

        public bool IsAvailable => Available;
        public void Play(Haptic haptic) => Played.Add(haptic);
    }

    public class HapticServiceTests
    {
        private FakeBackend backend;
        private float now;
        private HapticService service;

        [SetUp]
        public void SetUp()
        {
            backend = new FakeBackend();
            now = 0f;
            service = new HapticService(backend, () => now);
        }

        [Test]
        public void PlayReachesTheBackend()
        {
            Assert.IsTrue(service.Play(Haptic.Selection));
            Assert.AreEqual(1, backend.Played.Count);
            Assert.AreEqual(Haptic.Selection, backend.Played[0]);
        }

        [Test]
        public void MutedServicePlaysNothing()
        {
            service.Muted = true;
            Assert.IsFalse(service.Play(Haptic.Confirm));
            Assert.AreEqual(0, backend.Played.Count);
        }

        [Test]
        public void UnavailableBackendIsNeverCalled()
        {
            backend.Available = false;
            Assert.IsFalse(service.Play(Haptic.Confirm));
            Assert.AreEqual(0, backend.Played.Count);
        }

        [Test]
        public void RapidRepeatsAreRateLimited()
        {
            Assert.IsTrue(service.Play(Haptic.ImpactLight));
            Assert.IsFalse(service.Play(Haptic.ImpactLight), "second pulse in the same instant");
            Assert.IsFalse(service.Play(Haptic.ImpactLight));
            Assert.AreEqual(1, backend.Played.Count);
        }

        [Test]
        public void PulsesResumeOnceTheIntervalHasPassed()
        {
            service.Play(Haptic.ImpactLight);
            now += HapticService.MinIntervalSeconds + 0.001f;
            Assert.IsTrue(service.Play(Haptic.ImpactLight));
            Assert.AreEqual(2, backend.Played.Count);
        }

        [Test]
        public void HighPriorityHapticsBypassTheRateLimit()
        {
            // Death and BossRumble are rare and structurally important; a dropped Death pulse
            // reads as a bug to the player.
            service.Play(Haptic.ImpactLight);
            Assert.IsTrue(service.Play(Haptic.Death));
            Assert.IsTrue(service.Play(Haptic.BossRumble));
            Assert.AreEqual(3, backend.Played.Count);
        }

        [Test]
        public void PlayedCountTracksOnlyDeliveredPulses()
        {
            service.Play(Haptic.Selection);
            service.Play(Haptic.Selection);   // rate limited
            service.Muted = true;
            service.Play(Haptic.Selection);
            Assert.AreEqual(1, service.PlayedCount);
        }

        [Test]
        public void PlayedCountAccumulatesAcrossDeliveredPulses()
        {
            // Guards against a counter that is SET rather than incremented — the existing
            // single-pulse test cannot distinguish those two implementations.
            service.Play(Haptic.Selection);
            now += HapticService.MinIntervalSeconds + 0.001f;
            service.Play(Haptic.Selection);
            now += HapticService.MinIntervalSeconds + 0.001f;
            service.Play(Haptic.Selection);

            Assert.AreEqual(3, service.PlayedCount);
            Assert.AreEqual(3, backend.Played.Count);
        }

        [Test]
        public void NullBackendIsToleratedRatherThanThrowing()
        {
            var s = new HapticService(null, () => 0f);
            Assert.DoesNotThrow(() => s.Play(Haptic.Selection));
            Assert.IsFalse(s.Play(Haptic.Selection));
        }

        [Test]
        public void MinIntervalIsTheValueTheTimingFixturesAssume()
        {
            // The other timing tests advance the clock by MinIntervalSeconds + epsilon, i.e. by
            // the constant under test, which makes them blind to a change in the constant
            // itself: set it to 5f and they all stay green while, on the phone, every button
            // after the first feels dead for five seconds. This is what notices.
            Assert.AreEqual(0.05f, HapticService.MinIntervalSeconds, 0.0001f);
        }

        [Test]
        public void HapticOrdinalsAreTheWireContractWithTheNativePlugins()
        {
            // Assets/Plugins/iOS/SdHaptics.mm switches on these integers, and the Android
            // backend maps them to VibrationEffect constants. Inserting or reordering a member
            // silently gives every effect after it the wrong feel, on a device, with nothing
            // failing anywhere. Appending leaves this passing - which is the intent.
            Assert.AreEqual(0, (int)Haptic.Selection);
            Assert.AreEqual(1, (int)Haptic.Confirm);
            Assert.AreEqual(2, (int)Haptic.Reject);
            Assert.AreEqual(3, (int)Haptic.ImpactLight);
            Assert.AreEqual(4, (int)Haptic.ImpactHeavy);
            Assert.AreEqual(5, (int)Haptic.Ability);
            Assert.AreEqual(6, (int)Haptic.Reward);
            Assert.AreEqual(7, (int)Haptic.Death);
            Assert.AreEqual(8, (int)Haptic.BossRumble);
        }
    }
}
