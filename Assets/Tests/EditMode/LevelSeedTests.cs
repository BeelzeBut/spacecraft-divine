using System;
using NUnit.Framework;
using SpaceshipDivine.Levels;

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
    public void ConsumeReturnsThePendingSeed()
    {
        LevelSeed.Set(4242);
        Assert.AreEqual(4242, LevelSeed.Consume());
    }

    [Test]
    public void ConsumeClearsPendingSoARetryCannotReseed()
    {
        // The guard against LevelGenerator.Start's rejection retry reseeding to the same value
        // and recursing until the stack gives out.
        LevelSeed.Set(4242);
        LevelSeed.Consume();
        Assert.IsNull(LevelSeed.Pending, "the seed must not survive being consumed");
    }

    [Test]
    public void ConsumeWithNothingPendingInventsADifferentSeedEachTime()
    {
        // 4242 is deliberately NOT used here: a fixture value equal to a seed used elsewhere
        // would let a "returns the pending seed regardless" bug pass.
        int first = LevelSeed.Consume();
        int second = LevelSeed.Consume();
        Assert.AreNotEqual(first, second,
            "an unseeded level must not reuse one value for every generation");
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
    public void ForDateIsNeverNegative()
    {
        // A negative seed is legal for Random.InitState, but the value is also sent to the
        // server and used in document ids, where a minus sign is a nuisance.
        for (int dayOffset = 0; dayOffset < 400; dayOffset++)
        {
            int seed = LevelSeed.ForDate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(dayOffset));
            Assert.GreaterOrEqual(seed, 0, "day offset " + dayOffset + " produced a negative seed");
        }
    }

    [Test]
    public void ForDateIsFrozenAgainstPinnedValues()
    {
        // These are the literal outputs of the shipped formula. If a change to ForDate makes
        // this test fail, every daily challenge ever published would move to a different
        // dungeon. Regenerate these numbers deliberately, never to make the suite green.
        Assert.AreEqual(2002182125, LevelSeed.ForDate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        Assert.AreEqual(1941320525, LevelSeed.ForDate(new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc)));
    }
}
