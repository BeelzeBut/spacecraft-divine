using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Upgrades/Bullet Bounce"))]
public class BulletBounce : Upgrade
{
    public override void UpgradeShip()
    {
        PlayerController p = PlayerController.instance;
        p.ship.canBounce = true;
    }
}
