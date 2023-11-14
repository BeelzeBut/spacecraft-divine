using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Upgrade : ScriptableObject
{
    public string description;
    public bool isAttack, isSpeed, isDefense;
    public bool hasBeenChosen;
    public abstract void UpgradeShip();
}
