using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName =("Secondary Guns/Bat-Oh-No"))]
public class BatSecondaryGun : SecondaryGun
{
    public float chargeTime = .5f;
    public Image charge;
    public ParticleSystem chargeEffect;
    private ParticleSystem[] effects = new ParticleSystem[3];
    public override void Initialize()
    {
        p = PlayerController.instance;
        for(int i = 0; i < 3;i++)
        {
            effects[i] = Instantiate(chargeEffect, p.firePoints[i].position, p.firePoints[i].rotation, p.transform);
        }
        charge = GameObject.Find("SecondaryGunCharge").GetComponent<Image>();
        shootTimer = 0;
    }
    public override IEnumerator Shoot()
    {
        if (shootTimer > 0)
            yield break;
        p.canShoot = false;
        shootTimer = cooldown;
        SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);

        for (int i = 0; i < 3; i++)
        {
            effects[i].Play();
        }
        float elapsed = 0;
        while(elapsed < chargeTime)
        {
            p.isShooting = true;
            elapsed += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < 3;i ++)
        {
            Instantiate(p.bulletPrefab.muzzleFlash, p.firePoints[i].position, p.firePoints[i].rotation);
            Bullet bullet = Instantiate(p.bulletPrefab, p.firePoints[i].position, p.firePoints[i].rotation);
            bullet.whoShotIt = p.transform;
            bullet.transform.localScale *= 1.25f;
            bullet.pushBack = p.damagePerBullet * 1.375f;
            bullet.speed *= 1.5f;
            bullet.canPierce = true;
            bullet.limitedPiercings = false;
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
            bullet.damage *= 1.375f;
        }
        for (int i = 0; i < 3; i++)
        {
            effects[i].Stop();
        }

        p.justShot = true;

        yield return new WaitForSeconds(.2f);
        p.canShoot = true;

        yield return new WaitForSeconds(.1f);
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
        while(elapsed < .125f)
        {
            newColor = new Color(1, 1, 1, elapsed * .75f / .125f);
            charge.color = newColor;
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = .125f;
        while(elapsed > 0)
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
