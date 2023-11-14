using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName =("Upgrades/PushBack On Ability"))]
public class PushBackOnAbility : Upgrade
{
    public float pushBackAmount = 6f;

    public override void UpgradeShip()
    {
        PlayerController.instance.ship.pushBackOnAbility = true;
        PlayerController.instance.ship.pushBackStrength = pushBackAmount;
    }
}
