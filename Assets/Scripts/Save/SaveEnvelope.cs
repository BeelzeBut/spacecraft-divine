using System;

namespace SpaceshipDivine.Save
{
    /// <summary>
    /// What actually lands on disk: a schema version, the serialised profile, and a
    /// signature over that payload. Versioning the envelope rather than the payload means
    /// the migrator can read a file it does not yet understand the shape of.
    /// </summary>
    [Serializable]
    public class SaveEnvelope
    {
        public const int CurrentVersion = 1;

        public int version;
        public string payload;
        public string signature;
    }
}
