using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SpaceshipDivine.Haptics;

/// <summary>
/// Walks every Selectable in each loaded scene and attaches UIFeedback. This exists because
/// buttons bind onClick directly in the scene files — 122 entries in Main Menu alone, ~220
/// across the game — and editing those bindings to add feedback is not maintainable. Only four
/// call sites in the whole project play a UI sound today; every other button is silent.
///
/// Name-based overrides are deliberately coarse: a button whose name contains one of these
/// fragments gets a different haptic. Anything unmatched gets Selection, which is correct for
/// the overwhelming majority (navigation).
/// </summary>
public class UIFeedbackInstaller : MonoBehaviour
{
    private struct Override
    {
        public string fragment;
        public Haptic haptic;
        public bool playSound;
    }

    /// <summary>
    /// Fragments were checked against every m_Name in every scene before being chosen.
    ///
    /// "back" and "close" are absent on purpose: both mapped to Selection, which is already the
    /// default, so they changed nothing — while "back" also matches "Background" and
    /// "Feedback Button". A no-op entry that collides is pure future foot-gun.
    ///
    /// playSound is false where the button's own handler already plays UISounds[0] on success
    /// (MainMenu.UnlockShip for "Buy Ship", MainMenu.UpgradeShip for "Upgrade Ship"); leaving it
    /// true would play the click twice on a successful purchase.
    /// </summary>
    private static readonly Override[] Overrides =
    {
        new Override { fragment = "play",     haptic = Haptic.Confirm, playSound = true  },
        new Override { fragment = "start",    haptic = Haptic.Confirm, playSound = true  },
        new Override { fragment = "buy",      haptic = Haptic.Confirm, playSound = false },
        new Override { fragment = "upgrade",  haptic = Haptic.Confirm, playSound = false },
        new Override { fragment = "unlock",   haptic = Haptic.Confirm, playSound = true  },
        new Override { fragment = "purchase", haptic = Haptic.Confirm, playSound = true  },
        new Override { fragment = "revive",   haptic = Haptic.Confirm, playSound = true  },
    };

    /// <summary>
    /// Creates the installer with no scene involvement at all.
    ///
    /// The alternative — a GameObject authored into Main Menu.unity — was tried and rejected:
    /// saving that 21k-line scene in 2022.3 reserialises it (serializedVersion bumps,
    /// m_ConstrainProportionsScale on every transform, camera physical properties), producing a
    /// 580-line diff to add one object. That is unreviewable, and it would also leave the
    /// installer deletable by anyone tidying the hierarchy.
    ///
    /// AfterSceneLoad rather than BeforeSceneLoad: it runs once the first scene is loaded, so
    /// that scene can be installed explicitly. Subscribing before the first load and relying on
    /// sceneLoaded to fire for scene 0 is not dependable.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("UI Feedback Installer");
        go.AddComponent<UIFeedbackInstaller>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // The scene that was already loaded when Bootstrap ran. Not gameObject.scene:
        // DontDestroyOnLoad has just moved this object into its own scene, which holds
        // nothing but the installer.
        Install(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);

    private static void Install(Scene scene)
    {
        if (!scene.IsValid()) return;

        int attached = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // true = include inactive: most menus are inactive panels until opened.
            foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                if (selectable.GetComponent<UIFeedback>() != null) continue;   // idempotent

                UIFeedback feedback = selectable.gameObject.AddComponent<UIFeedback>();
                Apply(selectable.gameObject.name, feedback);
                attached++;
            }
        }

        if (attached > 0)
            Debug.Log("UIFeedbackInstaller: attached to " + attached + " selectables in " + scene.name);
    }

    private static void Apply(string objectName, UIFeedback feedback)
    {
        string lower = objectName.ToLowerInvariant();
        foreach (Override entry in Overrides)
        {
            if (!lower.Contains(entry.fragment)) continue;
            feedback.haptic = entry.haptic;
            feedback.playSound = entry.playSound;
            return;
        }
        feedback.haptic = Haptic.Selection;
        feedback.playSound = true;
    }
}
