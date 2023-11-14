using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Gun : ScriptableObject
{
    public PlayerController p;
    public float chargeTime;
    public bool isBeam = false;
    [HideInInspector]
    public float maxWidth = 0;
    public Coroutine activeCoroutine;
    [HideInInspector]
    public bool isCharging;
    public abstract IEnumerator Shoot();
    public abstract void Initialize();
    public abstract void Reset();

    public abstract void CancelShooting();
}
