using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName =("Secondary Guns/Grey Byrd"))]
public class GreyByrdSecondaryGun : SecondaryGun
{
    public Image charge;
    public float shootAngle = 60;
    public int numberOfBullets;
    public float fireRate;
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
        p.canShoot = false;
        shootTimer = cooldown;
        p.isShooting = true;
        p.StopShootingCoroutine();

        yield return new WaitForSeconds(.05f);
        for (int j = 0; j < 2; j++)
        {
            Vector3 deviation = Vector3.forward * -shootAngle / 2;
            SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);
            for (int i = 0; i < numberOfBullets; i++)
            {
                Instantiate(p.bulletPrefab.muzzleFlash, p.firePoints[0].position, Quaternion.Euler(p.firePoints[0].rotation.eulerAngles + deviation));// p.firePoints[p.firePointNumber].rotation);
                Bullet bullet = Instantiate(p.bulletPrefab, p.firePoints[0].position + p.transform.up * .025f, Quaternion.Euler(p.firePoints[0].rotation.eulerAngles + deviation));
                deviation += Vector3.forward * (shootAngle / (numberOfBullets - 1));
                bullet.pushBack = p.damagePerBullet;
                bool isCrit = Random.Range(0, 101) < p.critChance;
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
                bullet.damage = p.damagePerBullet * .8f;
                bullet.whoShotIt = p.transform;
            }
            yield return new WaitForSeconds(fireRate);
        }
        p.isShooting = false;
        p.justShot = true;

        yield return new WaitForSeconds(.2f);
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