using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = ("Upgrades/Damage Reduction"))]
public class DamageTakenDecrease : Upgrade
{
    public float damageReduction;
    public override void UpgradeShip()
    {
        PlayerController.instance.damageReduction += damageReduction;
    }
}
