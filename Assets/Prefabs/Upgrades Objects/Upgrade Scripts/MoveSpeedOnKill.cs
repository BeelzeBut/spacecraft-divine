using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Upgrades/Move Speed On Kill"))]
public class MoveSpeedOnKill : Upgrade
{
    public float moveSpeedAmount = 20f;
    public override void UpgradeShip()
    {
        PlayerController p = PlayerController.instance;
        p.ship.moveSpeedOnEnemyKill = true;
        p.ship.moveSpeedAmount = moveSpeedAmount;
    }
}

