using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Upgrades/Active Damage Increase"))]
public class ActiveDamageIncrease : Upgrade
{
    public float activeDamageIncrease;

    public override void UpgradeShip()
    {
        PlayerController.instance.ship.attackMultiplier += activeDamageIncrease;
        if(PlayerController.instance.attackMultiplier > 0)
        {
            PlayerController.instance.attackMultiplier = PlayerController.instance.ship.attackMultiplier;
        }
    }
}
