using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName = ("Abilities/Lil Ship"))]
public class LilShipAbility : Ability
{
    public LilShip ship;
    LilShip activeShip;
    public float duration;
    private float durationTimer;
    public float shipDamage;
    private float damage;
    public Image charge;
    public float shootCooldown;
    private float shootCd;

    public override void Initialize()
    {
        p = PlayerController.instance;
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        charge = UIManager.instance.charge;
        cooldown = baseCooldown;
        damage = shipDamage + .33f * level - .33f;
        shootCd = shootCooldown - .2f * level + .2f;
        durationTimer = duration + level - 1;
    }


    public override void UpdateAbility()
    {
        durationTimer -= Time.deltaTime;
        if (durationTimer <= 0)
        {
            CancelUpdateAbility();

        }

        charge.fillAmount = durationTimer / duration;
    }

    public override void ToActivateUpdate()
    {
        abilityManager.abilityUpdateOn = true;
        p.abilityUpdateBool = true;
        durationTimer = duration;
        activeShip = Instantiate(ship, p.transform.position, p.transform.rotation);
        activeShip.damage = damage;
        activeShip.shootCooldown = shootCd;
    }
    public override void CancelUpdateAbility()
    {
        abilityManager.abilityUpdateOn = false;
        p.abilityUpdateBool = false;
        charge.fillAmount = 0;
        if (activeShip)
        {
            Instantiate(activeShip.explosion, activeShip.transform.position, activeShip.transform.rotation);
            Destroy(activeShip.gameObject);
        }
        abilityManager.GoOnCooldown();

    }
    public override void TriggerAbility()
    {
        throw new System.NotImplementedException();
    }

}
