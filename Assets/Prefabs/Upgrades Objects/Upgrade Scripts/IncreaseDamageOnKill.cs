using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Upgrades/Increase Damage On Kill"))]
public class IncreaseDamageOnKill : Upgrade
{
    public float damageIncrease = 10f;
    public override void UpgradeShip()
    {
        PlayerController p = PlayerController.instance;
        p.ship.increaseDamageOnKill = true;
        p.ship.damageIncreaseOnKill = damageIncrease;
    }
}
