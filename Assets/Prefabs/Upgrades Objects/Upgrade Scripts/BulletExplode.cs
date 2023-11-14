using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Upgrades/Bullet Explode"))]
public class BulletExplode : Upgrade
{
    public override void UpgradeShip()
    {
        PlayerController p = PlayerController.instance;
        p.ship.canExplode = true;
    }
}
