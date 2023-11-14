using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBullet : ExplodeBullet
{
    void Start()
    {
        explosion.GetComponent<Explosion>().damage = damage;
        if (gameObject.layer == 10)
            explosion.GetComponent<Explosion>().damage *= 1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f;
    }

    void Update()
    {
        if ((locationToExplode - transform.position).sqrMagnitude <= .025f)
        {
            Explode();
            Destroy(gameObject);
        }
    }
}
