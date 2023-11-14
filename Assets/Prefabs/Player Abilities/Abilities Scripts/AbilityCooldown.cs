using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilityCooldown : MonoBehaviour
{
    public static AbilityCooldown instance;
    [SerializeField]
    public GameObject abilityButton;
    public Joystick abilityJoystick;
    [SerializeField]
    public GameObject abilityRange, abilityTargetRange;
    public Image darkMask;
    PlayerController p;
    [SerializeField] private Ability ability;
    private Image myButtonImage;
    private float cooldownDuration;
    private float nextReadyTime;
    private float cooldownTimer;
    public bool abilityUpdateOn = false;
    private WaitForSeconds wait = new WaitForSeconds(.25f);
    public bool isPressed = false;
    public Image whiteImage;
    public bool blinkOnAbilityReady;

    private void Awake()
    {
        instance = this;
        p = PlayerController.instance;
        if (!whiteImage)
            whiteImage = GameObject.Find("Ability White Image").GetComponent<Image>();
    }
    private void Start()
    {      
        abilityRange.gameObject.SetActive(false);
        abilityTargetRange.gameObject.SetActive(false);
        abilityJoystick.gameObject.SetActive(false);
    }

    public void Initialize(Ability selectedAbility)
    {
        if (p == null)
            p = PlayerController.instance;
        if (ability && ability.isUpdate)
            ability.CancelUpdateAbility();
        abilityUpdateOn = false;
        blinkOnAbilityReady = false;
        ability = selectedAbility;
        abilityButton.GetComponentsInChildren<Image>()[1].sprite = ability.abilitySprite;
        foreach (Transform obj in p.spaceshipObjects)
        {
            Destroy(obj.gameObject);
        }
        p.spaceshipObjects.Clear();

        switch(ability.level)
        {
            case 1:
                p.abilityManager.abilityButton.GetComponent<Image>().color = Color.white;
                    break;
            case 2:
                p.abilityManager.abilityButton.GetComponent<Image>().color = Color.green;
                break;
            case 3:
                p.abilityManager.abilityButton.GetComponent<Image>().color = new Color(0, .5f, 1);
                break;
            case 4:
                p.abilityManager.abilityButton.GetComponent<Image>().color = new Color(.75f, 0, 1);
                break;
            case 5:
                p.abilityManager.abilityButton.GetComponent<Image>().color = new Color(1, .5f, 0);
                break;
            case 6:
                p.abilityManager.abilityButton.GetComponent<Image>().color = Color.red;
                break;
        }
        ability.Initialize();
        cooldownDuration = ability.cooldown;
        cooldownTimer = 0;
        nextReadyTime = 0;
        GoOnCooldown();
    }

    void Update()
    {
        bool cooldownComplete = cooldownTimer <= 0;//(Time.time > nextReadyTime);
        if(cooldownComplete)
        {
            AbilityReadyUpdate();
        }
        else
        {
            Cooldown();
        }
    }

    public void ButtonPress()
    {
        if (!isPressed)
        {
            isPressed = true;
            StartCoroutine(PreventDoubleTouch());
            if ((!p.canMove && !ability.canBeUsedWhileMoving) || (!p.canShoot && !ability.canBeUsedWhileShooting))
                return;
            bool cooldownComplete = (cooldownTimer <= 0);
            if (cooldownComplete)
            {
                
                if (ability.isUpdate)
                {
                    if (!abilityUpdateOn)
                    {
                        p.ship.ApplyUpgrades();
                        ability.ToActivateUpdate();
                        abilityUpdateOn = true;
                    }
                    else
                    {
                        ability.CancelUpdateAbility();
                        abilityUpdateOn = false;
                    }
                }
                else
                {
                    p.ship.ApplyUpgrades();
                    ButtonTriggered();
                    GoOnCooldown();
                }
            }
        }
    }
    IEnumerator PreventDoubleTouch()
    {
        yield return wait;
        isPressed = false;
    }

    public void AbilityReadyUpdate()
    {
        if (!blinkOnAbilityReady)
            AbilityReady();
    }

    public void AbilityReady()
    {
        cooldownTimer = -0.1f;
        darkMask.enabled = false;
        blinkOnAbilityReady = true;
        StartCoroutine(CooldownReady(whiteImage));
    }
    public IEnumerator CooldownReady(Image charge)
    {
        Color initialColor = charge.color;
        charge.color = new Color(1, 1, 1, 0);
        Color newColor = charge.color;
        float elapsed = 0;
        charge.enabled = true;
        while (elapsed < .125f)
        {
            newColor = new Color(1, 1, 1, elapsed * .75f / .125f);
            charge.color = newColor;
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = .125f;
        while (elapsed > 0)
        {
            newColor = new Color(1, 1, 1, elapsed * .75f / .125f);
            charge.color = newColor;
            elapsed -= Time.deltaTime;
            yield return null;
        }
        charge.fillAmount = 0;
        charge.enabled = false;
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
        blinkOnAbilityReady = false;
        darkMask.enabled = true;
    }
    void ButtonTriggered()
    {
        ability.TriggerAbility();
    }
}
