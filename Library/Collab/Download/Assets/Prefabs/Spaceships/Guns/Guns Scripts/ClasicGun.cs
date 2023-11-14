using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName =("Guns/Clasic Gun"))]
public class ClasicGun : Gun
{
    public override void Initialize()
    {
        p = PlayerController.instance;
    }

    public override IEnumerator Shoot()
    {
        p.shotCounter = p.fireRate + p.numberOfBursts * p.waitTimeBetweenBursts;
        if (!p.ship.alternateFirePoints)
        {
            for (int j = 0; j < p.numberOfBursts; j++)
            {
                SoundManager.instance.source.PlayOneShot(SoundManager.instance.shootSounds[p.ship.audioIndex]);
                foreach (Transform firePoint in p.firePoints)
                {
                    Instantiate(p.bulletPrefab.muzzleFlash, firePoint.position, firePoint.rotation);
                    for (int i = 0; i < p.bulletsShot; i++)
                    {
                        float bulletSpread = Random.Range(-1f, 1f) * p.spread * 7.5f;
                        Bullet bullet = Instantiate(p.bulletPrefab, firePoint.position + p.transform.up * .05f, Quaternion.Euler(firePoint.rotation.eulerAngles + Vector3.forward * bulletSpread));
                        BulletSetup(bullet);
                    }
                }
                yield return new WaitForSeconds(p.waitTimeBetweenBursts);
            }
        }
        else
        {

            for (int j = 0; j < p.numberOfBursts; j++)
            {
                SoundManager.instance.source.PlayOneShot(SoundManager.instance.shootSounds[p.ship.audioIndex]);
                for (int i = 0; i < p.bulletsShot; i++)
                {
                    if (p.firePointNumber >= p.firePoints.Count)
                        p.firePointNumber = 0;
                    Instantiate(p.bulletPrefab.muzzleFlash, p.firePoints[p.firePointNumber].position, p.firePoints[p.firePointNumber].rotation);

                    float bulletSpread = Random.Range(-1f, 1f) * p.spread * 7.5f;
                    Bullet bullet = Instantiate(p.bulletPrefab, p.firePoints[p.firePointNumber].position + p.transform.up * .05f, Quaternion.Euler(p.firePoints[p.firePointNumber].rotation.eulerAngles + Vector3.forward * bulletSpread));
                    BulletSetup(bullet);
                    p.firePointNumber++;
                }
                yield return new WaitForSeconds(p.waitTimeBetweenBursts);
            }
        }
        yield return new WaitForSeconds(p.fireRate);
        p.isShooting = false;
    }

    public void BulletSetup(Bullet bullet)
    {
        bullet.whoShotIt = p.transform;
        bool isCrit = Random.Range(0, 101) < p.critChance;
        bullet.pushBack *= p.bulletPushBack * (isCrit ? p.critMultiplier : 1);
        if (isCrit)
        {
            bullet.damage = p.damagePerBullet * 2;
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
            //bullet.damage *= .9f;
            //bullet.GetComponent<BeamBullet>().startWidth = bullet.GetComponent<BeamBullet>().maxWidth;
        }
    }
}

