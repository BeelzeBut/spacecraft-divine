using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName =("Guns/Basic Gun"))]
public class BasicGun : Gun
{
    public float fireRate;
    public int bulletsShot;
    public float spread;
    public float damagePerBullet;
    public float bulletPushBack;
    public Bullet bulletPrefab;
    public int numberOfBursts;
    public float waitTimeBetweenBursts;
    public bool alternateFirePoints;
    public float critChance;

    public override void Initialize()
    {
        p = PlayerController.instance;
    }

    public override IEnumerator Shoot()
    {
        p.shotCounter = fireRate + numberOfBursts * waitTimeBetweenBursts;
        if (!alternateFirePoints)
        {
            for (int j = 0; j < p.numberOfBursts; j++)
            {
                SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.shootSounds[p.ship.audioIndex]);
                foreach (Transform firePoint in p.firePoints)
                {
                    Instantiate(bulletPrefab.muzzleFlash, firePoint.position, firePoint.rotation);
                    for (int i = 0; i < bulletsShot; i++)
                    {
                        float bulletSpread = Random.Range(-1f, 1f) * spread * 7.5f;
                        Bullet bullet = Instantiate(bulletPrefab, firePoint.position + p.transform.up * .1f, Quaternion.Euler(firePoint.rotation.eulerAngles + Vector3.forward * bulletSpread));
                        BulletSetup(bullet);
                    }
                }
                yield return new WaitForSeconds(waitTimeBetweenBursts);
            }
        }
        else
        {

            for (int j = 0; j < numberOfBursts; j++)
            {
                SoundManager.instance.soundSource.PlayOneShot(SoundManager.instance.shootSounds[p.ship.audioIndex]);
                for (int i = 0; i < bulletsShot; i++)
                {
                    if (p.firePointNumber >= p.firePoints.Count)
                        p.firePointNumber = 0;
                    Instantiate(bulletPrefab.muzzleFlash, p.firePoints[p.firePointNumber].position, p.firePoints[p.firePointNumber].rotation);

                    float bulletSpread = Random.Range(-1f, 1f) * spread * 7.5f;
                    Bullet bullet = Instantiate(bulletPrefab, p.firePoints[p.firePointNumber].position + p.transform.up * .1f, Quaternion.Euler(p.firePoints[p.firePointNumber].rotation.eulerAngles + Vector3.forward * bulletSpread));
                    BulletSetup(bullet);
                    p.firePointNumber++;
                }
                yield return new WaitForSeconds(waitTimeBetweenBursts);
            }
        }
        yield return new WaitForSeconds(fireRate);
        p.isShooting = false;
    }

    public void BulletSetup(Bullet bullet)
    {
        bullet.whoShotIt = p.transform;
        bool isCrit = Random.Range(0, 101) < critChance;
        bullet.pushBack *= bulletPushBack * (isCrit ? p.critMultiplier : 1);
        if (isCrit)
        {
            bullet.damage = damagePerBullet / p.firePoints.Count * 2;
            bullet.isCrit = isCrit;
        }
        else
        {
            bullet.damage = damagePerBullet / p.firePoints.Count;
            bullet.isCrit = isCrit;
        }
        if (bullet.hasCharge)
        {
            bullet.charged = 1;
        }
    }

    public override void Reset()
    {

    }

    public override void CancelShooting()
    {

    }
}
