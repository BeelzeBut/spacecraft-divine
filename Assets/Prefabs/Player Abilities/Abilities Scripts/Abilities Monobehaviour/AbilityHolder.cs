using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilityHolder : MonoBehaviour
{
    public static AbilityHolder instance;
    public List<Abilities> abilities = new List<Abilities>();
    public GameObject abilityButton;
    public Joystick abilityJoystick;
    [SerializeField]
    public GameObject abilityRange, abilityTargetRange;
    public Image darkMask;
    PlayerController p;
    [SerializeField] private Abilities ability;
    private Image myButtonImage;
    private float cooldownDuration;
    private float nextReadyTime;
    private float cooldownTimer;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        p = PlayerController.instance;
        abilityRange = GameObject.FindWithTag("RangeIndicator");
        abilityRange.gameObject.SetActive(false);
        abilityTargetRange = GameObject.FindWithTag("RocketTarget");
        abilityTargetRange.gameObject.SetActive(false);
        abilityJoystick.gameObject.SetActive(false);
    }

    public void Initialize(Abilities selectedAbility)
    {
        ability = selectedAbility;
        abilityButton.GetComponent<Image>().sprite = ability.abilitySprite;
        cooldownDuration = ability.baseCooldown;
        ability.GetComponent<Abilities>().enabled = true;
    }

    void Update()
    {
        bool cooldownComplete = (Time.time > nextReadyTime);
        if (cooldownComplete)
        {
            AbilityReady();
        }
        else
        {
            Cooldown();
        }
    }
    public void ButtonPress()
    {
        bool cooldownComplete = (Time.time > nextReadyTime);
        if (cooldownComplete)
        {

            if (ability.isUpdate)
            {
                if (!ability.isUpdateOn)
                {
                    ability.ToActivateUpdate();
                    ability.isUpdateOn = true;
                }
                else
                {
                    ability.CancelUpdateAbility();
                    ability.isUpdateOn = false;
                }
            }
            else
            {
                ButtonTriggered();
                GoOnCooldown();
            }
        }
    }
    void AbilityReady()
    {
        darkMask.enabled = false;
    }

    void Cooldown()
    {
        cooldownTimer -= Time.deltaTime;
        darkMask.fillAmount = cooldownTimer / cooldownDuration;
    }
    public void GoOnCooldown()
    {
        nextReadyTime = cooldownDuration + Time.time;
        cooldownTimer = cooldownDuration;
        darkMask.enabled = true;
    }
    void ButtonTriggered()
    {
        ability.TriggerAbility();
    }
}
