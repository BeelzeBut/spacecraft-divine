using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Upgrades/Reset CD On Kill"))]
public class KillHasChanceToResetCd : Upgrade
{
    public float resetCdChance = 15;
    public override void UpgradeShip()
    {
        PlayerController p = PlayerController.instance;
        p.ship.resetCdOnKill = true;
        p.ship.resetCdChance = resetCdChance;
    }
}
