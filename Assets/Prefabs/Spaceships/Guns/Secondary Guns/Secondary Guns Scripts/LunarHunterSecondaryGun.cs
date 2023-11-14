using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu (menuName =("Secondary Guns/Lunar Hunter"))]
public class LunarHunterSecondaryGun : SecondaryGun
{
    public Image charge;
    public Gun playerGun;
    public Material material;
    private Material mat;
    public override void Initialize()
    {
        p = PlayerController.instance;

        charge = GameObject.Find("SecondaryGunCharge").GetComponent<Image>();
        shootTimer = 0;
        playerGun = p.ship.gun;
        mat = new Material(material);
    }
    public override IEnumerator Shoot()
    {
        if (shootTimer > 0)
            yield break;
        p.StopShootingCoroutine();
        p.canShoot = false;
        p.shotCounter = 10f;
        shootTimer = cooldown;
        p.isShooting = true;
        float initialChargeTime = playerGun.chargeTime;
        playerGun.chargeTime = 0.013f;
        p.standardInput = true;
        p.shouldTurn = true;
        SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);

        //go invis
        p.spriteRenderer.material = mat;
        mat.SetFloat("_DissolveAmount", 0);
        float elapsed = 0;
        p.GetComponent<Collider2D>().enabled = false;
        Shadow shadow = p.GetComponentInChildren<Shadow>();
        while (mat.GetFloat("_DissolveAmount") < 1)
        {
            mat.SetFloat("_DissolveAmount", elapsed);
            elapsed += 7f * Time.deltaTime;
            Color blackness = new Color(0, 0, 0, 0.6f - elapsed * .5f);
            shadow.sprite.color = blackness;
            yield return null;
        }
        shadow.sprite.color = new Color(0, 0, 0, .1f);
        mat.SetFloat("_DissolveAmount", 1);
        p.canMove = false;

        foreach (TargetedBullet bullet in FindObjectsOfType<TargetedBullet>())
        {
            if (bullet.target == p.transform)
                bullet.target = null;
        }

        //dash
        elapsed = 0;
        while(elapsed < .1f)
        {
            p.rb.MovePosition(p.rb.position + (Vector2)p.lastMoveDirection * p.maxMoveSpeed * 6f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        //go out of invis
        elapsed = 1;
        while (mat.GetFloat("_DissolveAmount") > 0)
        {
            mat.SetFloat("_DissolveAmount", elapsed);
            elapsed -= 7f * Time.deltaTime;
            Color blackness = new Color(0, 0, 0, 0.6f - elapsed * .5f);
            shadow.sprite.color = blackness;
            yield return null;
        }
        shadow.sprite.color = new Color(0, 0, 0, .6f);

        mat.SetFloat("_DissolveAmount", 0);
        p.spriteRenderer.material = p.material;
        p.GetComponent<Collider2D>().enabled = true;

        Quaternion initialFirePointRotation = p.firePoints[0].localRotation;
        
        Vector3 deviation = Vector3.forward * Random.Range(-1f, 1f) * .6f * 7.5f;
        p.firePoints[0].rotation = Quaternion.Euler(p.firePoints[0].rotation.eulerAngles + deviation);
        playerGun.activeCoroutine = p.StartCoroutine(playerGun.Shoot());

        p.isShooting = true;
        yield return new WaitForSeconds(.075f);
        p.firePoints[0].localRotation = initialFirePointRotation;
        p.StopShootingCoroutine();
        p.isShooting = true;
        yield return new WaitForSeconds(.15f);
        deviation = Vector3.forward * Random.Range(-1f, 1f) * .6f * 7.5f;
        p.firePoints[0].rotation = Quaternion.Euler(p.firePoints[0].rotation.eulerAngles + deviation);
        p.StartCoroutine(playerGun.Shoot());


        playerGun.chargeTime = initialChargeTime;
        p.justShot = true;
        yield return new WaitForSeconds(.1f);
        p.firePoints[0].localRotation = initialFirePointRotation;
        yield return new WaitForSeconds(.1f);
        p.shotCounter = 0;
        p.canMove = true;
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
                p.StartCoroutine(CooldownReady());
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