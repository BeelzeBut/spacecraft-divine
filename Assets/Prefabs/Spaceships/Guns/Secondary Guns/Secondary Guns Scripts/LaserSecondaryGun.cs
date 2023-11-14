using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = ("Secondary Guns/Laser"))]
public class LaserSecondaryGun : SecondaryGun
{
    public Image charge;
    public float damagePerTick;
    public float laserDuration;
    public float fireRate;
    private float shootCounter;
    private LineRenderer laser;
    public LineRenderer laserPrefab;
    [SerializeField]
    public GameObject explosionEffect, laserMuzzle;
    private GameObject muzzle, endExplosion;
    public ParticleSystem chargeEffect;
    private ParticleSystem chargeUp;
    private AudioSource pSource;

    public override void Initialize()
    {
        p = PlayerController.instance;
        pSource = p.GetComponent<AudioSource>();
        pSource.clip = p.ship.secondarySound;
        pSource.loop = false;
        charge = GameObject.Find("SecondaryGunCharge").GetComponent<Image>();
        shootTimer = 0;
        laser = Instantiate(laserPrefab, p.transform.position, p.transform.rotation, p.transform);
        laser.enabled = false;
        muzzle = Instantiate(laserMuzzle, p.transform.position, p.transform.rotation, p.transform);
        endExplosion = Instantiate(explosionEffect, p.transform.position, p.transform.rotation, p.transform);
        muzzle.SetActive(false);
        endExplosion.SetActive(false);
        chargeUp = Instantiate(chargeEffect, p.firePoints[0].position - p.transform.up * .05f, p.firePoints[0].rotation, p.transform);
    }
    public override IEnumerator Shoot()
    {
        if (shootTimer > 0)
            yield break;
        p.StopShootingCoroutine();
        p.canShoot = false;
        shootTimer = cooldown;
        p.isShooting = true;
        //SoundManager.instance.soundSource.PlayOneShot(p.ship.secondarySound);
        pSource.Play();
        chargeUp.Play();
        yield return new WaitForSeconds(.35f);

        float elapsed = 0;
        laser.enabled = true;
        endExplosion.SetActive(true);
        muzzle.SetActive(true);
        shootCounter = .05f;
        while(elapsed < laserDuration && p != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(p.firePoints[0].position, p.transform.up, 20, LayerMask.GetMask("Default") | LayerMask.GetMask("Obstacles"));
            laser.SetPosition(0, p.firePoints[0].position - p.transform.up * .05f);
            if (hit.collider != null)
                laser.SetPosition(1, hit.point);
            else
                laser.SetPosition(1, p.transform.position + p.transform.up * 20);
            endExplosion.transform.position = laser.GetPosition(1);
            muzzle.transform.position = laser.GetPosition(0);
            if(shootCounter <= 0)
            {
                shootCounter = fireRate;
                RaycastHit2D[] enemyHits = Physics2D.CircleCastAll(p.firePoints[0].position, .075f, p.transform.up, 20, LayerMask.GetMask("Default") | LayerMask.GetMask("Obstacles") | LayerMask.GetMask("Units"));
                for(int i = 0; i < enemyHits.Length; i++)
                {
                    Enemy enemy = enemyHits[i].collider.GetComponent<Enemy>();
                    if (enemy)
                    {
                        bool isCrit = Random.Range(0, 101) < p.critChance;
                        if (enemy.canTakeDamage)
                        {
                            if (isCrit)
                            {
                                Instantiate(GameManager.instance.criticalText, enemy.transform.position, Quaternion.identity);
                                enemy.TakeDamage(damagePerTick * p.critMultiplier * (1 + (p.attackMultiplier + p.damageMultiplier) / 100f));
                            }
                            else
                            {
                                enemy.TakeDamage(damagePerTick * (1 + (p.attackMultiplier + p.damageMultiplier) / 100f));
                            }
                            if (enemy.canBePushedBack)
                            {
                                Vector2 forceDir = (enemy.transform.position - p.transform.position).normalized;
                                enemy.rb.AddForce(forceDir / 2f, ForceMode2D.Impulse);
                            }
                        }
                    }
                }
            }
            shootCounter -= Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        laser.enabled = false;
        endExplosion.SetActive(false);
        muzzle.SetActive(false);
        chargeUp.Stop();
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