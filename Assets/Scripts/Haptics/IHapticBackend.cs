namespace SpaceshipDivine.Haptics
{
    public interface IHapticBackend
    {
        bool IsAvailable { get; }
        void Play(Haptic haptic);
    }
}
