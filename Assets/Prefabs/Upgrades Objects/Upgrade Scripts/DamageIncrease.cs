using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Upgrades/DamageIncrease"))]
public class DamageIncrease : Upgrade
{
    public float damageIncrease;

    public override void UpgradeShip()
    {
        PlayerController.instance.ship.damageMultiplier += damageIncrease;
        PlayerController.instance.damageMultiplier = PlayerController.instance.ship.damageMultiplier;
    }
}
