using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SecondaryGun : ScriptableObject
{
    public PlayerController p;
    [HideInInspector]
    public float shootTimer;
    public float cooldown;
    public abstract void Initialize();
    public abstract IEnumerator Shoot();
    public abstract void SecondaryGunUpdate();
}
