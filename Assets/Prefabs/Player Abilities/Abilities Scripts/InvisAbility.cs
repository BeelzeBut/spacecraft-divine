using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName = "Abilities/Invis Ability")]
public class InvisAbility : Ability
{
    public float duration;
    private float durationTimer;
    private Image charge;
    public Material invisMat;
    public bool leaveInvis;
    float critChance;
    public float critMultiplier = 2f;
    float critmultiplier;
    float initialCritMultiplier;
    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        charge = UIManager.instance.charge;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        cooldown = baseCooldown - .5f * level + .5f;
        critmultiplier = p.critMultiplier + level / 4f;
    }

    public override void ToActivateUpdate()
    {
        p.justShot = false;
        initialCritMultiplier = p.critMultiplier;
        p.critMultiplier = critmultiplier;
        p.abilityUpdateBool = true;
        durationTimer = duration;
        abilityCoroutineManager.GoInvis(invisMat);
        critChance = p.critChance;
        p.critChance = 100;
    }
    public override void UpdateAbility()
    {
        charge.fillAmount = durationTimer / duration;

        if (durationTimer > 0)
        {
            durationTimer -= Time.deltaTime;
            if (durationTimer <= 0)
            {
                CancelUpdateAbility();
            }
        }

        if(p.justShot)
        {
            p.justShot = false;
            CancelUpdateAbility();
        }
    }

    public override void CancelUpdateAbility()
    {
        abilityCoroutineManager.LeaveInvis(invisMat);
        p.critChance = critChance;
        p.abilityUpdateBool = false;
        p.damageTakenWhenShielded = 0;
        abilityManager.GoOnCooldown();
        charge.fillAmount = 0;
        abilityManager.abilityUpdateOn = false;
        p.critMultiplier = initialCritMultiplier;
    }


    public override void TriggerAbility()
    {
        throw new System.NotImplementedException();
    }

}
