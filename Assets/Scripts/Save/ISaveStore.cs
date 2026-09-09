namespace SpaceshipDivine.Save
{
    public enum ReadOutcome
    {
        Fresh,                 // no save existed; a default profile was created
        Loaded,                // the live save verified and loaded
        RecoveredFromBackup,   // the live save was bad; the backup verified and loaded
        RejectedAndReset       // both were bad; a default profile was created
    }

    public interface ISaveStore
    {
        void Write(PlayerProfile profile);
        PlayerProfile Read();
        bool Exists();
        void Delete();
        ReadOutcome LastReadOutcome { get; }

        /// <summary>
        /// True when the file on disk was written by a NEWER build. Write() becomes a no-op:
        /// this build cannot see fields it does not know about, so anything it wrote back would
        /// silently drop them. Refusing to write loses this session's progress; writing loses
        /// the save. The first is recoverable, the second is not.
        /// </summary>
        bool IsReadOnlyBecauseNewer { get; }
    }
}
