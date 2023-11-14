using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = ("Secondary Guns/Apollo"))]
public class ApolloSecondaryGun : SecondaryGun
{
    public Image charge;
    public float speedToTarget = 35f;
    public TrailRenderer trailPrefab;
    private TrailRenderer trail;
    public float rangeAroundEnemy = .75f;
    public AudioClip dashSound;
    public override void Initialize()
    {
        p = PlayerController.instance;
        trail = Instantiate(trailPrefab, p.transform.position, p.transform.rotation, p.transform);
        trail.enabled = false;
        trail.material.SetTexture("_MainTex", p.ship.shipSprite.texture);
        charge = GameObject.Find("SecondaryGunCharge").GetComponent<Image>();
        shootTimer = 0;
    }


    public override IEnumerator Shoot()
    {
        if (shootTimer > 0)
            yield break;
        p.canShoot = false;
        p.StopShootingCoroutine();
        p.shotCounter = 5f;
        shootTimer = cooldown;
        p.isShooting = true;
        if (p.targetEnemy)
        {
            Vector3 backDir = p.targetEnemy.transform.up * Random.Range(0, 1f) + p.targetEnemy.transform.right * Random.Range(-1f, 1f);//new Vector2(Random.Range(-1f, 0), Random.Range(-1f, 1f));
            float distance = Random.Range(rangeAroundEnemy / 1.5f, rangeAroundEnemy);
            while (Physics2D.Raycast(p.targetEnemy.transform.position, backDir, distance, LayerMask.GetMask("Obstacles") | LayerMask.GetMask("Default")))
            {
                backDir = backDir = p.targetEnemy.transform.up * Random.Range(0, 1f) + p.targetEnemy.transform.right * Random.Range(-1f, 1f);//new Vector2(Random.Range(-1f, 0f), Random.Range(-1f, 1f));
                distance = Random.Range(rangeAroundEnemy / 1.5f, rangeAroundEnemy);
            }
            Vector3 enemyBackPos = p.targetEnemy.transform.position + backDir.normalized * distance;
            p.canTakeDamage = false;
            trail.enabled = true;
            p.GetComponentsInChildren<Collider2D>()[1].enabled = false;
            float elapsed = 0;
            SoundManager.instance.soundSource.PlayOneShot(dashSound);
            while ((p.transform.position - enemyBackPos).sqrMagnitude > .05f && elapsed < .75f)
            {
                p.transform.position = Vector3.MoveTowards(p.transform.position, enemyBackPos, 25f * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            p.GetComponentsInChildren<Collider2D>()[1].enabled = true;
            p.canTakeDamage = true;
            trail.enabled = false;
            p.canMove = false;
            for (int i = 0; i < 7; i++)
            {
                SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);

                foreach (Transform firePoint in p.firePoints)
                {
                    //SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);
                    float bulletSpread = Random.Range(-1f, 1f) * p.spread * 7.5f;
                    Instantiate(p.bulletPrefab.muzzleFlash, firePoint.position, Quaternion.Euler(firePoint.rotation.eulerAngles + Vector3.forward * bulletSpread));// p.firePoints[p.firePointNumber].rotation);

                    Bullet bullet = Instantiate(p.bulletPrefab, firePoint.position + p.transform.up * .05f, Quaternion.Euler(firePoint.rotation.eulerAngles + Vector3.forward * bulletSpread));
                    bullet.pushBack = p.damagePerBullet;
                    bool isCrit = Random.Range(0, 101) < p.critChance;
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
                    bullet.whoShotIt = p.transform;
                    bullet.pushBack *= p.bulletPushBack;
                    yield return new WaitForSeconds(p.ship.fireRate * .25f);
                }
            }
            p.canMove = true;
        }
        else
            shootTimer /= 3;
        p.shotCounter = p.fireRate;
        yield return new WaitForSeconds(.2f);

        p.isShooting = false;
        p.canShoot = true;
    }

    public override void SecondaryGunUpdate()
    {
        if (shootTimer > 0)
        {
            charge.enabled = true;
            shootTimer -= Time.deltaTime;
            charge.fillAmount = shootTimer / cooldown;
            if (shootTimer <= 0)
            {
                //charge.enabled = false;
                GameManager.instance.StartCoroutine(CooldownReady());
            }
        }
    }

    public IEnumerator CooldownReady()
    {
        charge.raycastTarget = false;
        charge.fillAmount = 1;
        Color initialColor = charge.color;
        charge.color = new Color(1, 1, 1, 0);
        Color newColor = charge.color;
        float elapsed = 0;
        while (elapsed < .125f)
        {
            newColor = new Color(1, 1, 1, elapsed * .75f / .125f);
            charge.color = newColor;
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = .125f;
        while (elapsed > 0)
        {
            newColor = new Color(1, 1, 1, elapsed * .75f / .125f);
            charge.color = newColor;
            elapsed -= Time.deltaTime;
            yield return null;
        }
        charge.fillAmount = 0;
        charge.color = initialColor;
        charge.raycastTarget = true;
        charge.enabled = false;
    }
}