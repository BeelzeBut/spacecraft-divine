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
    }
}
