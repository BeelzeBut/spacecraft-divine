using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Upgrades/Move Speed On Ability"))]
public class MoveSpeedIncreaseOnAbility : Upgrade
{
    public float speedIncrease;
    public override void UpgradeShip()
    {
        PlayerController.instance.ship.moveSpeedOnAbility = true;
        PlayerController.instance.ship.moveSpeedIncrease = speedIncrease;
    }
}
