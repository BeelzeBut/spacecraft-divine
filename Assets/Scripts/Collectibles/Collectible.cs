using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Collectible : MonoBehaviour
{ 
    public bool onlyDropsOnce = false;
    public int dropIndex;

    public abstract void UnlockCollectible();
}
