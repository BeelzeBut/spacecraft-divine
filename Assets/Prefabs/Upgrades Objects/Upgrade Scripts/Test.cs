using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = ("Upgrades/Test"))]
public class Test : Upgrade
{
    public override void UpgradeShip()
    {
        Debug.Log("works");
    }
}
