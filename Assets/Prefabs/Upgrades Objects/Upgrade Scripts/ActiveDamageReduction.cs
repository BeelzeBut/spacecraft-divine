using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName =("Upgrades/Active Damage Reduction"))]
public class ActiveDamageReduction : Upgrade
{
    public float activeDamageReduction;
    public override void UpgradeShip()
    {
        PlayerController.instance.ship.defenseMultiplier += activeDamageReduction;
        if(PlayerController.instance.defenseMultiplier > 0)
        {
            PlayerController.instance.defenseMultiplier = PlayerController.instance.ship.defenseMultiplier;
        }
    }
}
