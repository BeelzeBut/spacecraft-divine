using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Upgrades/Increase FireRate Decrease Damage"))]
public class IncFRDecDMG : Upgrade
{
    public float damageDecrease;
    public float fireRateIncrease;

    public override void UpgradeShip()
    {
        PlayerController p = PlayerController.instance;
        p.ship.damagePerBullet *= (1 + damageDecrease / 100f);
        p.damagePerBullet = p.ship.damagePerBullet;
        if (p.ship.gun.chargeTime > 0)
            p.ship.gun.chargeTime /= (1 + fireRateIncrease / 100f);
        else
        {
            p.ship.fireRate /= (1 + fireRateIncrease / 100f);
            p.fireRate = p.ship.fireRate;
        }
    }
}
