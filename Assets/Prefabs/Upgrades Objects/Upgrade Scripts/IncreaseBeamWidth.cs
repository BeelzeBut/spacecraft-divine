using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Upgrades/Increase Beam Width"))]
public class IncreaseBeamWidth : Upgrade
{
    public float beamWidthIncreasePercentage = 25f;
    public override void UpgradeShip()
    {
        if(PlayerController.instance.ship.gun.isBeam)
        {
            PlayerController.instance.ship.gun.maxWidth *= beamWidthIncreasePercentage;
        }
    }
}
