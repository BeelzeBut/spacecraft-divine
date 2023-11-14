using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName = ("Abilities/Deflect Ability"))]
public class DeflectAbility : Ability
{
    public LaserTurret turret;
    public Transform turretExplosion;
    public TrailRenderer trail;
    LaserTurret activeTurret;
    public float damageMultiplier;
    float damage;
    private Image charge;
    public float duration;
    public float fireRate;
    float firerate;

    public override void Initialize()
    {
        abilityManager = AbilityCooldown.instance;
        abilityCoroutineManager = AbilityCoroutineManager.instance;
        p = PlayerController.instance;
        cooldown = baseCooldown;
        damage = damageMultiplier + .2f * level - .2f;
        firerate = fireRate - .05f * level + .05f;
        charge = UIManager.instance.charge;
    }

    public override void TriggerAbility()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateAbility()
    {
        
    }
    public override void CancelUpdateAbility()
    {
        if (activeTurret)
        {
            Instantiate(turretExplosion, activeTurret.transform.position, Quaternion.identity);
            Destroy(activeTurret.gameObject);
        }
        abilityManager.GoOnCooldown();
        p.abilityUpdateBool = false;
        charge.fillAmount = 0;
        abilityManager.abilityUpdateOn = false;
    }
    public override void ToActivateUpdate()
    {
        p.abilityUpdateBool = true;
        activeTurret = Instantiate(turret, p.transform.position, Quaternion.identity);
        activeTurret.ability = this;
        activeTurret.duration = duration;
        activeTurret.damageMultiplier = damage;
        activeTurret.charge = charge;
        activeTurret.trail = trail;
    }
}
