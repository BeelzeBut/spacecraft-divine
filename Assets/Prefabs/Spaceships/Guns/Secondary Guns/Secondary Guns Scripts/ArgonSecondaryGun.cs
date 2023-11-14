using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = ("Secondary Guns/The Argon"))]
public class ArgonSecondaryGun : SecondaryGun
{
    public Image charge;
    public Transform[] firePointsPrefabs;
    Transform[] firePoints = new Transform[2];
    public Bullet bulletPrefab;
    public float damage;
    public ParticleSystem chargeEffect;
    private ParticleSystem chargeUp;
    [SerializeField]
    public GameObject trailPrefab;
    private GameObject trail;
    public float dashDuration;
    public float fireRate;
    float shootCounter;
    public AudioClip chargeSound, flySound;
    private AudioSource pSource;

    public override void Initialize()
    {
        p = PlayerController.instance;
        pSource = p.GetComponent<AudioSource>();
        pSource.loop = false;
        pSource.clip = flySound;
        charge = GameObject.Find("SecondaryGunCharge").GetComponent<Image>();
        shootTimer = 0;
        chargeUp = Instantiate(chargeEffect, p.transform.position + chargeEffect.transform.position, chargeEffect.transform.rotation, p.transform);
        trail = Instantiate(trailPrefab, p.transform.position + trailPrefab.transform.position, trailPrefab.transform.rotation, p.transform);
        trail.SetActive(false);
        for (int i = 0; i < 2; i++)
            firePoints[i] = Instantiate(firePointsPrefabs[i], p.transform.position + firePointsPrefabs[i].position, firePointsPrefabs[i].rotation, p.transform);
    }
    public override IEnumerator Shoot()
    {
        if (shootTimer > 0)
            yield break;
        p.StopShootingCoroutine();
        p.canShoot = false;
        shootTimer = cooldown;
        p.isShooting = false;

        float elapsed = 0;
        shootCounter = .05f;
        p.justShot = true;
        int firePointNumber = Random.Range(0, firePoints.Length);
        foreach(Transform thruster in p.thrusters)
        {
            thruster.GetComponent<SpriteRenderer>().enabled = false;
        }
        chargeUp.Play();
        SoundManager.instance.soundSource.PlayOneShot(chargeSound);
        yield return new WaitForSeconds(.225f);
        p.damageReduction += 50;
        trail.SetActive(true);
        pSource.Play();
        while (elapsed < dashDuration && p != null)
        {
            p.slowDuration = 100f;
            p.rb.velocity = Vector2.zero;
            p.moveSpeed = p.maxMoveSpeed * 2f;
            p.curMoveSpeed = p.moveSpeed;
            if (p.moveDirection == Vector2.zero)
            {
                p.rb.MovePosition(p.rb.position + (p.lastMoveDirection).normalized * p.curMoveSpeed * Time.fixedDeltaTime);
            }
            if(shootCounter <= 0)
            {
                if (firePointNumber >= firePoints.Length)
                    firePointNumber = 0;
                shootCounter = fireRate;
                SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);
                Bullet bullet = Instantiate(bulletPrefab, firePoints[firePointNumber].position, Quaternion.Euler(firePoints[firePointNumber].rotation.eulerAngles + Vector3.forward * Random.Range(-30, 30)));
                bool isCrit = Random.Range(0, 101) < p.critChance;
                if (isCrit)
                {
                    bullet.damage = damage * p.critMultiplier;
                    bullet.isCrit = isCrit;
                }
                else
                {
                    bullet.damage = damage;
                    bullet.isCrit = isCrit;
                }
                firePointNumber++;
            }
            shootCounter -= Time.fixedDeltaTime;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        pSource.Stop();
        trail.SetActive(false);
        chargeUp.Stop();
        foreach (Transform thruster in p.thrusters)
        {
            thruster.GetComponent<SpriteRenderer>().enabled = true;
        }
        p.slowDuration = 0;
        p.moveSpeed = p.maxMoveSpeed;
        p.canMove = true;
        p.damageReduction -= 50;
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