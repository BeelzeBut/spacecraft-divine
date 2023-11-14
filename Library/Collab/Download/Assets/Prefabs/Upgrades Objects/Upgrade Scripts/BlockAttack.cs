using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Upgrades/Block Attack"))]
public class BlockAttack : Upgrade
{
    public float chanceToBlock;
    public override void UpgradeShip()
    {
        PlayerController.instance.ship.chanceToBlockAttack = chanceToBlock;
    }
}
