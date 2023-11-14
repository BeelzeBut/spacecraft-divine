using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName =("Secondary Guns/Valiant"))]
public class ValiantSecondaryGun : SecondaryGun
{
    public Image charge;
    public Bullet bulletPrefab;
    public float damagePerTick;
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
        yield return new WaitForSeconds(.05f);

        SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);
        Instantiate(bulletPrefab.muzzleFlash, (p.firePoints[1].position + p.firePoints[0].position) / 2f - p.transform.up * .1f, p.firePoints[1].rotation);
        Bullet bullet = Instantiate(bulletPrefab, (p.firePoints[1].position + p.firePoints[0].position) / 2f + p.transform.up * .05f, p.firePoints[1].rotation);
        bullet.pushBack = damagePerTick / 3f;
        bullet.damage = damagePerTick / 3f;

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