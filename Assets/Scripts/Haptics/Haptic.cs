namespace SpaceshipDivine.Haptics
{
    /// <summary>
    /// What a haptic MEANS, not how long it buzzes. Call sites choose meaning; the platform
    /// backend chooses the physical effect. Adding a duration parameter here would be a
    /// mistake — it puts platform detail at every call site.
    /// </summary>
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
