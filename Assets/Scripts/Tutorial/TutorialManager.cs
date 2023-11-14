using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;
    PlayerController p;
    GameObject secondaryAttackButton;
    public Transform indicationTextBg;
    public TextMeshProUGUI indicationText;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        p = PlayerController.instance;
        p.ability = null;
        secondaryAttackButton = GameObject.Find("Secondary Attack Button");
        secondaryAttackButton.SetActive(false);
        indicationTextBg.gameObject.SetActive(false);
    }
}
