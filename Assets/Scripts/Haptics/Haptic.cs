namespace SpaceshipDivine.Haptics
{
    /// <summary>
    /// What a haptic MEANS, not how long it buzzes. Call sites choose meaning; the platform
    /// backend chooses the physical effect. Adding a duration parameter here would be a
    /// mistake — it puts platform detail at every call site.
    /// </summary>
    /// <remarks>
    /// APPEND-ONLY. The ordinal of each value is a wire contract: IosHapticBackend passes
    /// (int)haptic straight to SdHaptics.mm, which switches on that integer. Reordering,
    /// inserting, or removing a value silently remaps every effect on iOS — it still compiles,
    /// still runs, and no test can detect it. Only a human holding the phone would notice.
    /// Add new values at the END, and update the switch in SdHaptics.mm in the same commit.
    /// </remarks>
    public enum Haptic
    {
        Selection,    // menu nav, tab change, toggle, slider notch
        Confirm,      // start run, purchase success, ship unlock
        Reject,       // insufficient gems, tapping a locked ship
        ImpactLight,  // player bullet hits an enemy — rate limited, default off
        ImpactHeavy,  // player takes damage
        Ability,      // ability cast
        Reward,       // chest open, upgrade pickup, level clear
        Death,        // player death
        BossRumble    // boss spawn and boss defeat
    }
}
