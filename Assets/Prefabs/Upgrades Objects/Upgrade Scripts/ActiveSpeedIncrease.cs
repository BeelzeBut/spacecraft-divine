using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Upgrades/Active Speed Increase"))]
public class ActiveSpeedIncrease : Upgrade
{
    public float activeSpeedIncrease;

    public override void UpgradeShip()
    {
        PlayerController.instance.ship.speedMultiplier += activeSpeedIncrease;
        if (PlayerController.instance.speedMultiplier > 0)
            PlayerController.instance.speedMultiplier = PlayerController.instance.ship.speedMultiplier;
    }
}
