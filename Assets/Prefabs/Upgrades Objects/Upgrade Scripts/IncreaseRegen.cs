using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Upgrades/Increase Regen"))]
public class IncreaseRegen : Upgrade
{
    public float regenIncreasePercentage;
    public override void UpgradeShip()
    {
        PlayerController.instance.ship.regenAmount *= (1 + regenIncreasePercentage / 100f);
    }
}
