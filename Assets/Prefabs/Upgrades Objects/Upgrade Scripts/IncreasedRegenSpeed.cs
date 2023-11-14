using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Increased Regen Speed")]
public class IncreasedRegenSpeed : Upgrade
{
    public override void UpgradeShip()
    {
        PlayerController.instance.ship.regenPerSecond *= 1.5f;
    }
}
