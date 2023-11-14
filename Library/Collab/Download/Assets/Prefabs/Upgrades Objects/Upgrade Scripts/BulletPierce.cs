using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Upgrades/Bullet Pierce"))]
public class BulletPierce : Upgrade
{
    public override void UpgradeShip()
    {
        PlayerController p = PlayerController.instance;
        p.ship.canPierce = true;
    }
}
