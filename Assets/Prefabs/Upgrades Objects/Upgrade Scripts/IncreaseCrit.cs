using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName =("Upgrades/Increase Crit"))]
public class IncreaseCrit : Upgrade
{
    public float critIncrease;

    public override void UpgradeShip()
    {
        PlayerController.instance.ship.critChance += critIncrease;
    }
}
