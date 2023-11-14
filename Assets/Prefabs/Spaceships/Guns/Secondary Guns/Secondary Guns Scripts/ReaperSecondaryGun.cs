using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[CreateAssetMenu(menuName = ("Secondary Guns/The Reaper"))]

public class ReaperSecondaryGun : SecondaryGun
{
    public Image charge;
    public Harpoon bulletPrefab;
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
        p.canShoot = false;
        shootTimer = cooldown;
        p.isShooting = true;
        yield return new WaitForSeconds(.05f);

        //Instantiate(bulletPrefab.muzzleFlash, p.transform.position, p.transform.rotation);
        SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);
        Harpoon bullet = Instantiate(bulletPrefab, p.transform.position, p.firePoints[0].rotation);
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