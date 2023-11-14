using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodeBullet : Bullet
{
    public bool explodeAtLocation;
    public Vector3 locationToExplode;

    public bool explodeAfterTime;
    public float explodeTimer;
}
