namespace SpaceshipDivine.Save
{
    /// <summary>Exists so the assembly has a type before real code lands. Delete in Task 2.</summary>
    internal static class SaveCoreMarker
    {
        internal const int SchemaVersion = 1;
    }
}

namespace SpaceshipDivine.Save
{
    /// <summary>Test-visible probe for the harness check. Delete in Task 2.</summary>
    public static class SaveCoreMarkerProbe
    {
        public static int SchemaVersion => SaveCoreMarker.SchemaVersion;
    }
}
