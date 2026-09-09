using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SpaceshipDivine.Haptics;

/// <summary>
/// Attached at runtime by UIFeedbackInstaller — never authored into a scene. Fires on pointer
/// down rather than on click, because that is when the press is felt.
/// </summary>
public class UIFeedback : MonoBehaviour, IPointerDownHandler
{
    /// <summary>Overridden by the installer for buttons that mean something other than navigation.</summary>
    public Haptic haptic = Haptic.Selection;

    /// <summary>Set false for buttons whose own handler already plays a sound, to avoid doubling.</summary>
    public bool playSound = true;

    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;

        // A disabled Selectable still receives pointer events: Selectable guards interactability
        // *inside* its own OnPointerDown, which proves the handler is dispatched either way.
        // Without this check, a locked ship's buy button would feel and sound exactly like a
        // live one. Reject is the honest answer, and this is the only path that reaches it —
        // no GameObject in any scene is named "locked".
        if (selectable != null && !selectable.IsInteractable())
        {
            Haptics.Play(Haptic.Reject);
            return;
        }

        Haptics.Play(haptic);

        if (!playSound) return;
        SoundManager sound = SoundManager.instance;
        if (sound == null || sound.soundSource == null) return;
        if (sound.UISounds == null || sound.UISounds.Count == 0) return;

        // soundSource.mute is what SoundOnOff() toggles, and PlayOneShot honours it, so a
        // player who muted sound stays muted without any check here.
        sound.soundSource.PlayOneShot(sound.UISounds[0]);
    }
}
