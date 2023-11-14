using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Constant Shield Regen")]

public class ConstantShieldRegen : Upgrade
{
    public float constantRegenSpeed = 0.15f;
    public override void UpgradeShip()
    {
        PlayerController.instance.ship.constantRegenPerSecond = constantRegenSpeed;
    }
}
