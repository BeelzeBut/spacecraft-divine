using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName = ("Secondary Guns/Rocket"))]
public class RocketSecondaryGun : SecondaryGun
{
    public Image charge;
    public ExplosionBullet bulletPrefab;
    public float damage;
    public override void Initialize()
    {
        p = PlayerController.instance;

        charge = GameObject.Find("SecondaryGunCharge").GetComponent<Image>();
        if(charge == null)
            charge = GameObject.Find("UI Canvas").GetComponentInChildren<SpriteMask>(true).gameObject.GetComponent<Image>();
        shootTimer = 0;
    }
    public override IEnumerator Shoot()
    {
        if (shootTimer > 0)
            yield break;
        p.StopShootingCoroutine();
        p.canShoot = false;
        shootTimer = cooldown;
        p.isShooting = true;
        yield return new WaitForSeconds(.05f);

        SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);
        Instantiate(bulletPrefab.muzzleFlash, p.firePoints[0].position, p.firePoints[0].rotation);
        ExplosionBullet bullet = Instantiate(bulletPrefab, p.firePoints[0].position + p.transform.up * .075f, p.firePoints[0].rotation);
        bool isCrit = Random.Range(0, 101) < p.critChance;
        if (isCrit)
        {
            bullet.damage = damage * p.critMultiplier * .8f;
            bullet.isCrit = isCrit;
        }
        else
        {
            bullet.damage = damage;
            bullet.isCrit = isCrit;
        }
        if (p.targetEnemy)
            bullet.locationToExplode = p.targetEnemy.transform.position;
        else
            bullet.locationToExplode = p.transform.position + p.transform.up * 2.5f;
        Vector3 shootDir = bullet.locationToExplode - bullet.transform.position;
        float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        p.justShot = true;
        yield return new WaitForSeconds(.2f);
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