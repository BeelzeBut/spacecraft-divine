using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName =("Secondary Guns/Warspite"))]
public class WarspiteSecondaryGun : SecondaryGun
{
    public Image charge;
    public OrbitingBullet bulletPrefab;
    public float damage;
    public override void Initialize()
    {
        p = PlayerController.instance;

        charge = GameObject.Find("SecondaryGunCharge").GetComponent<Image>();
        shootTimer = 0;
    }
    public override IEnumerator Shoot()
    {
        if (shootTimer > 0)
            yield break;
        p.StopShootingCoroutine();
        p.standardInput = true;
        p.shouldTurn = true;
        p.canMove = true;
        p.canShoot = false;
        shootTimer = cooldown;
        p.isShooting = true;

        Vector3 initialScale = bulletPrefab.transform.localScale;
        float initialSpeed = bulletPrefab.speed;
        SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);
        OrbitingBullet bullet = Instantiate(bulletPrefab, p.firePoints[0].position + p.transform.up * .06f, p.firePoints[0].rotation, p.transform);
        bullet.transform.localScale = Vector3.zero;
        bullet.speed = 0;
        if (p.targetEnemy)
            bullet.target = p.targetEnemy.transform;
        float elapsed = 0;
        while(elapsed < .25f)
        {
            bullet.transform.localScale += Vector3.one * Time.deltaTime;
            bullet.transform.position = p.firePoints[0].position + p.transform.up * .06f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        while(elapsed < .5f)
        {
            bullet.transform.position = p.firePoints[0].position + p.transform.up * .06f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Instantiate(bullet.muzzleFlash, p.firePoints[0].position + p.transform.up * .03f, p.firePoints[0].rotation);
        bullet.smallBulletDamage = damage;
        bullet.damage = damage * 1.75f;
        bullet.transform.SetParent(null);
        bullet.speed = initialSpeed;
        bullet.SpawnSmallBullets();
        bullet.StopTargeting(1.5f);
        while (bullet.transform.localScale.x < initialScale.x)
        {
            bullet.transform.localScale += Vector3.one * 1.5f * Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(.2f);

        p.justShot = true;
        p.canShoot = true;
        p.isShooting = false;
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