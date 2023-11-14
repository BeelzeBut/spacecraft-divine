using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName = ("Abilities/Shield Ability"))]
public class ShieldAbility : Ability
{
    public float duration;
    private float durationTimer;
    public float shieldHealth;
    private float health;
    [SerializeField]
    public GameObject shieldPrefab;
    private GameObject activeShield;
    private Image charge;
    float damageBlocked = 0;

    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        charge = UIManager.instance.charge;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        cooldown = baseCooldown - .5f * level + .5f;
        health = shieldHealth + (level - 1) * .4f;
    }

    public override void TriggerAbility()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateAbility()
    {
        activeShield.transform.position = p.transform.position;
        charge.fillAmount = durationTimer / duration;

        if (durationTimer > 0)
        {
            durationTimer -= Time.deltaTime;
            if(durationTimer <= 0)
            {
                CancelUpdateAbility();
            }
        }
        if (damageBlocked != p.damageTakenWhenShielded)
            activeShield.GetComponent<SpriteRenderer>().color = new Color(activeShield.GetComponent<SpriteRenderer>().color.r, activeShield.GetComponent<SpriteRenderer>().color.g, activeShield.GetComponent<SpriteRenderer>().color.b, 1 - p.damageTakenWhenShielded / health);
        damageBlocked = p.damageTakenWhenShielded;

        if (p.damageTakenWhenShielded >= health)
        {
            CancelUpdateAbility();
        }
    }

    public override void ToActivateUpdate()
    {
        p.abilityUpdateBool = true;
        p.canTakeDamage = false;
        activeShield = Instantiate(shieldPrefab, p.transform.position, p.transform.rotation);
        abilityCoroutineManager.SpawnShield(activeShield, .375f);
        durationTimer = duration;
    }


    public override void CancelUpdateAbility()
    {
        p.abilityUpdateBool = false;
        p.canTakeDamage = true;
        p.damageTakenWhenShielded = 0;
        abilityManager.GoOnCooldown();
        charge.fillAmount = 0;
        abilityManager.abilityUpdateOn = false;
        if(activeShield)
        {
            abilityCoroutineManager.DespawnShield(activeShield, .375f);
        }
    }

}
