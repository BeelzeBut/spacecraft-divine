using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Upgrades/Speed Increase"))]
public class SpeedIncrease : Upgrade
{
    public float speedIncrease;

    public override void UpgradeShip()
    {
        PlayerController.instance.ship.maxMoveSpeed *= (1 + speedIncrease / 100f);
        PlayerController.instance.maxMoveSpeed = PlayerController.instance.ship.maxMoveSpeed;
    }
}
