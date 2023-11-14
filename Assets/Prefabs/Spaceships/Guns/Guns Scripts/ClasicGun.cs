using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName =("Guns/Clasic Gun"))]
public class ClasicGun : Gun
{
    WaitForSeconds delay;
    public override void Initialize()
    {
        p = PlayerController.instance;
        delay = new WaitForSeconds(p.ship.waitTimeBetweenBursts);
    }

    public override IEnumerator Shoot()
    {
        p.isShooting = true;
        p.shotCounter = p.fireRate + p.numberOfBursts * p.waitTimeBetweenBursts;
        yield return new WaitForSeconds(.05f);

        if (!p.ship.alternateFirePoints)
        {
            for (int j = 0; j < p.numberOfBursts; j++)
            {
                SoundManager.instance.soundSource.PlayOneShot(p.ship.primarySound);
                foreach (Transform firePoint in p.firePoints)
                {            
                    for (int i = 0; i < p.bulletsShot; i++)
                    {
                        float bulletSpread = Random.Range(-1f, 1f) * p.spread * 7.5f;
                        Instantiate(p.bulletPrefab.muzzleFlash, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles + Vector3.forward * bulletSpread));// p.firePoints[p.firePointNumber].rotation);
                        Bullet bullet = Instantiate(p.bulletPrefab, firePoint.position + p.transform.up * .05f, Quaternion.Euler(firePoint.rotation.eulerAngles + Vector3.forward * bulletSpread));
                        BulletSetup(bullet);
                    }
                }
                yield return delay;
            }
        }
        else
        {
            for (int j = 0; j < p.numberOfBursts; j++)
            {
                SoundManager.instance.soundSource.PlayOneShot(p.ship.primarySound);
                for (int i = 0; i < p.bulletsShot; i++)
                {
                    if (p.firePointNumber >= p.firePoints.Count)
                        p.firePointNumber = 0;

                    float bulletSpread = Random.Range(-1f, 1f) * p.spread * 7.5f;
                    Instantiate(p.bulletPrefab.muzzleFlash, p.firePoints[p.firePointNumber].position, Quaternion.Euler(p.firePoints[p.firePointNumber].rotation.eulerAngles + Vector3.forward * bulletSpread));// p.firePoints[p.firePointNumber].rotation);
                    Bullet bullet = Instantiate(p.bulletPrefab, p.firePoints[p.firePointNumber].position + p.transform.up * .05f, Quaternion.Euler(p.firePoints[p.firePointNumber].rotation.eulerAngles + Vector3.forward * bulletSpread));
                    BulletSetup(bullet);
                    p.firePointNumber++;
                }
                yield return delay;
            }
        }
        p.justShot = true;
        yield return new WaitForSeconds(p.fireRate);
        p.isShooting = false;
    }

    public void BulletSetup(Bullet bullet)
    {
        bullet.canExplode = p.ship.canExplode;
        bullet.canPierce = p.ship.canPierce;
        bullet.canBounce = p.ship.canBounce;

        bullet.whoShotIt = p.transform;
        bool isCrit = Random.Range(0, 101) < p.critChance;
        bullet.pushBack = p.ship.bulletPushBack * (isCrit ? p.critMultiplier * .75f : 1);
        if (isCrit)
        {
            bullet.damage = p.damagePerBullet * p.critMultiplier;
            bullet.isCrit = isCrit;
        }
        else
        {
            bullet.damage = p.damagePerBullet;
            bullet.isCrit = isCrit;
        }
        if (bullet.hasCharge)
        {
            bullet.charged = 1;
        }
        if(bullet.GetComponent<TargetedBullet>())
        {
            if(p.targetEnemy)
                bullet.GetComponent<TargetedBullet>().target = p.targetEnemy.transform;
        }
        if(bullet.GetComponent<TrailBullet>())
        {
            bullet.GetComponent<TrailBullet>().smallBulletDamage = bullet.damage / 4f;
        }
    }

    public override void Reset()
    {
        
    }

    public override void CancelShooting()
    {
    }
}

