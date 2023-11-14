using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName =("Upgrades/Increase Max Shields"))]
public class IncreaseMaxShields : Upgrade
{
    public float shieldsIncreasePercentage;
    public override void UpgradeShip()
    {
        PlayerController.instance.ship.maxHealth *= (1 + shieldsIncreasePercentage / 100f);
        PlayerController.instance.ship.currentHealth = PlayerController.instance.ship.maxHealth;
        PlayerController.instance.ship.minHealth += PlayerController.instance.ship.maxHealth * shieldsIncreasePercentage / 100f;
        PlayerController.instance.SetupHealthbar();
    }
}
