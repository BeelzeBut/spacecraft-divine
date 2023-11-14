using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName =("Guns/Bat Gun"))]
public class BatGun : Gun
{
    int bigGuns = 0;
    public ArchBulletTest bigBullet;
    public AudioClip bombSound;
    public override void Initialize()
    {
        p = PlayerController.instance;
    }
    public override IEnumerator Shoot()
    {
        p.shotCounter = 100;
        yield return new WaitForSeconds(.05f);

        if (bigGuns != 2)
        {
            for (int i = 0; i < 2; i++)
            {
                SoundManager.instance.soundSource.PlayOneShot(p.ship.primarySound);
                float bulletSpread = Random.Range(-1f, 1f) * p.spread * 7.5f;
                Instantiate(p.bulletPrefab.muzzleFlash, p.firePoints[i].position, Quaternion.Euler(p.firePoints[i].rotation.eulerAngles + Vector3.forward * bulletSpread));// p.firePoints[p.firePointNumber].rotation);

                Bullet bullet = Instantiate(p.bulletPrefab, p.firePoints[i].position + p.transform.up * .05f, Quaternion.Euler(p.firePoints[i].rotation.eulerAngles + Vector3.forward * bulletSpread));
                BulletSetup(bullet);
                yield return new WaitForSeconds(p.ship.fireRate / 2f);
            }
            p.justShot = true;
            bigGuns++;
            if(bigGuns == 1)
                p.shotCounter = 0;
            else
                p.shotCounter = p.fireRate * .5f;
        }
        else
        {
            SoundManager.instance.soundSource.PlayOneShot(bombSound);
            Instantiate(bigBullet.muzzleFlash, p.firePoints[2].position, p.firePoints[2].rotation);
            ArchBulletTest bullet = Instantiate(bigBullet, p.firePoints[2].position, p.firePoints[2].rotation);
            if (p.targetEnemy)
            {
                Enemy pEnemy = p.targetEnemy.GetComponent<Enemy>();
                if (pEnemy.isMoving)
                    bullet.targetPos = pEnemy.transform.position + (Vector3)pEnemy.moveDirection / 2.25f * pEnemy.curMoveSpeed ;
                else
                    bullet.targetPos = pEnemy.transform.position;
            }
            else
                bullet.targetPos = p.transform.position + p.transform.up * 2.5f;
            bullet.damage = p.damagePerBullet * 2.5f;
            p.justShot = true;
            p.shotCounter = p.fireRate;
            bigGuns = 0;
        }
        yield return new WaitForSeconds(p.fireRate);
        p.isShooting = false;
    }
    public void BulletSetup(Bullet bullet)
    {
        bullet.whoShotIt = p.transform;
        bool isCrit = Random.Range(0, 101) < p.critChance;
        bullet.pushBack *= p.bulletPushBack * (isCrit ? p.critMultiplier * .75f : 1);
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
        }
        if (bullet.GetComponent<TargetedBullet>())
        {
            if (p.targetEnemy)
                bullet.GetComponent<TargetedBullet>().target = p.targetEnemy.transform;
        }
        if (bullet.GetComponent<TrailBullet>())
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
