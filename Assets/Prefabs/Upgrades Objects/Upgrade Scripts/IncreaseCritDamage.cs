using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Upgrades/Increase Crit Damage"))]
public class IncreaseCritDamage : Upgrade
{
    public float critDamageIncrease = 20f;
    public override void UpgradeShip()
    {
        PlayerController.instance.ship.critMultiplier += critDamageIncrease / 100f;
    }
}
