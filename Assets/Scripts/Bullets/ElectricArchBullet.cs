using DigitalRuby.LightningBolt;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricArchBullet : Bullet
{
    public float maxSpacing;
    public float spacingSpeed;
    public float initialSpacing;
    private float spacing;
    public Transform[] components;
    public LightningBoltScript lightning;
    public BoxCollider2D col;

    private void Start()
    {
        spacing = initialSpacing;
        col = GetComponent<BoxCollider2D>();
    }
    void FixedUpdate()
    {

    }

    private void Update()
    {
        transform.position += transform.right * speed * Time.deltaTime;
        col.size = new Vector2(col.size.x, spacing + .1f);
        if(spacing < maxSpacing)
        {
            spacing += Time.deltaTime * spacingSpeed;
            for(int i = 0; i < 2; i++)
            {
                components[i].transform.position += components[i].transform.up * Time.deltaTime * spacingSpeed / 2f;
            }
        }
    }

    public void DeflectBullet(Transform bulletTransform)
    {
        Bullet bullet = bulletTransform.GetComponent<Bullet>();
        bullet.transform.SetParent(null);
        if (bullet.speed == 0)
            bullet.speed = bullet.latentSpeed;
        if (bullet)
        {
            if (!bullet.gameObject.GetComponent<ExplosionBullet>())
            {
                bullet.gameObject.layer = 11;
                Transform target = bullet.whoShotIt;
                if (target)
                {
                    if (bullet.gameObject.GetComponent<TargetedBullet>())
                    {
                        TargetedBullet tBullet = bullet.GetComponent<TargetedBullet>();
                        tBullet.target = target;
                    }
                }
                bullet.transform.rotation = Quaternion.Euler(0, 0, bullet.transform.rotation.eulerAngles.z + 180);

                if (bullet.gameObject.GetComponent<StickyBullet>())
                {
                    bullet.gameObject.GetComponent<StickyBullet>().isEnemy = false;
                }
                if (bullet.GetComponent<BoomerangBullet>())
                {
                    bullet.GetComponent<BoomerangBullet>().targetPos = target.position;
                    bullet.GetComponent<BoomerangBullet>().reachedTarget = false;
                }
                if(bullet.GetComponent<TargetedBullet>())
                {
                    bullet.GetComponent<TargetedBullet>().StopTargeting(0.25f);
                }
                TrailRenderer existentTrail = bullet.gameObject.GetComponent<TrailRenderer>();
                if (existentTrail)
                {
                    existentTrail.startColor = Color.red;
                    existentTrail.endColor = Color.red;
                }
                else
                {
                    Instantiate(this.trail, bullet.transform.position, bullet.transform.rotation, bullet.transform).time /= (.5f * bullet.speed);
                }
            }
            else
            {
                ExplosionBullet expBullet = bullet.gameObject.GetComponent<ExplosionBullet>();
                Transform target = expBullet.whoShotIt;
                expBullet.gameObject.layer = 11;
                expBullet.transform.rotation = Quaternion.Euler(0, 0, bullet.transform.rotation.eulerAngles.z + 180);
                expBullet.locationToExplode = expBullet.transform.position + expBullet.transform.right;
            }
            bullet.damage *= damageMultiplier;
            bullet.speed *= .65f;
            TrailRenderer trail = bullet.gameObject.GetComponent<TrailRenderer>();
            if (trail)
            {
                trail.startColor = Color.red;
                trail.endColor = Color.red;
            }
            else
            {
                Instantiate(this.trail, bullet.transform.position, bullet.transform.rotation, bullet.transform).time /= (.5f * bullet.speed);
            }
        }
    }



    private void OnTriggerEnter2D(Collider2D other)
    {
        if (classicTrigger)
        {
            if ((ignoreCollisionLayer & 1 << other.gameObject.layer) == 1 << other.gameObject.layer)
                return;
            else
            {
                if (other.CompareTag("Deflect"))
                    return;
                if (other.gameObject.layer == 10)
                {
                    Bullet playerBullet = other.GetComponent<Bullet>();
                    if (playerBullet && playerBullet.canBeDeflected)
                    {
                        DeflectBullet(playerBullet.transform);
                    }
                }
                else
                {
                    if (other.gameObject.CompareTag("Enemy"))
                    {
                        Enemy enemy;
                        if (other.GetComponent<Enemy>() != null)
                        {
                            enemy = other.GetComponent<Enemy>();
                        }
                        else
                        {
                            enemy = other.GetComponentInParent<Enemy>();
                        }

                        if (enemy.canTakeDamage)
                        {
                            enemy.TakeDamage((damage * damageMultiplier) * (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f));
                            if (isCrit)
                                Instantiate(GameManager.instance.criticalText, other.transform.position, Quaternion.identity);
                            if (enemy.canBePushedBack && hasPushBack)
                            {
                                Vector2 forceDir = (enemy.transform.position - transform.position).normalized;
                                enemy.rb.AddForce(forceDir * pushBack / 2f, ForceMode2D.Impulse);
                            }

                        }
                        else
                        {
                            Instantiate(GameManager.instance.immuneText, other.transform.position, Quaternion.identity);
                        }
                        Explode();
                        if (canExplode)
                        {
                            Explosion exp = Instantiate(blast, other.transform.position, Quaternion.Euler(0, 0, Random.Range(0, 360)));
                            exp.damage = (damage / 2f * (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f));
                            exp.gameObject.layer = gameObject.layer;
                            exp.GetComponentInChildren<ExplosionDamage>().ignoredTarget = other.transform;
                        }
                        if (!canPierce || pierces <= 0)
                        {
                            Destroy(gameObject);
                            return;
                        }
                        else
                        {
                            if (limitedPiercings)
                            {
                                pierces--;
                                damage *= .66f;
                            }
                        }
                    }
                    else
                    if (other.gameObject.CompareTag("Player"))
                    {
                        if (PlayerController.instance.invincibility <= 0)
                        {
                            //DamagePopup.Create(other.transform.position, (int)damage, isCrit, other.gameObject);
                            if (PlayerController.instance.canTakeDamage && hasPushBack)
                            {
                                Vector2 forceDir;
                                if (PlayerController.instance.reducePushBack)
                                    forceDir = (other.transform.position - transform.position).normalized * .5f;
                                else
                                    forceDir = (other.transform.position - transform.position).normalized;
                                PlayerController.instance.rb.AddForce(forceDir * damage, ForceMode2D.Impulse);
                            }
                        }
                        PlayerController.instance.TakeDamage(damage);
                        Explode();
                        if (!canPierce)
                        {
                            Destroy(gameObject);
                            return;
                        }
                    }
                    else
                    {
                        Explode();
                        if (shouldDestroyOnCollision)
                            Destroy(gameObject);
                        return;
                    }
                }
            }
        }
    }
}
