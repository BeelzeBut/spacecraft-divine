using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LaserTurret : MonoBehaviour
{
    public Transform explosion;
    public Transform barrel;
    public Transform firePoint;
    public TrailRenderer trail;
    public LineRenderer laser;
    public float fireRate;
    float shootTimer;

    [Header("Ability")]
    public DeflectAbility ability;
    public float damageMultiplier;
    public Image charge;
    public float duration;
    float totalDuration;
    float radius = 2.05f;
    public LayerMask layermask = 11;
    Collider2D[] hits;
    void Start()
    {
        totalDuration = duration;
    }

    void Update()
    {
        duration -= Time.deltaTime;
        charge.fillAmount = duration / totalDuration;
        if(duration <= 0)
        {
            GoOnCooldown();
        }

        shootTimer -= Time.deltaTime;
        if(shootTimer <= 0)
        {
            hits = Physics2D.OverlapCircleAll(transform.position, radius, layermask);
            for (int i = 0; i < hits.Length; i++)
                if (hits[i] != null)
                {
                    if (hits[i].GetComponent<Bullet>() && hits[i].GetComponent<Bullet>().canBeDeflected)
                    {
                        Vector3 lookDir = hits[i].transform.position - transform.position;
                        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90;
                        barrel.transform.rotation = Quaternion.Euler(0, 0, angle);
                        LineRenderer line = Instantiate(laser);
                        line.SetPosition(0, firePoint.position);
                        line.SetPosition(1, hits[i].transform.position);
                        Instantiate(explosion, hits[i].transform.position, Quaternion.identity);
                        DeflectBullet(hits[i].transform);
                        Destroy(line.gameObject, .2f);
                    }
                    else continue;
                    shootTimer = fireRate;
                    break;
                }
                else break;
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
                bullet.gameObject.layer = 10;
                Transform target = bullet.whoShotIt;
                if (target)
                {
                    if (bullet.gameObject.GetComponent<TargetedBullet>())
                    {
                        TargetedBullet tBullet = bullet.GetComponent<TargetedBullet>();
                        tBullet.target = tBullet.whoShotIt;
                    }
                    Vector2 lookDir = target.position - bullet.transform.position;
                    float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                    bullet.transform.rotation = Quaternion.Euler(0, 0, angle);//bullet.transform.rotation.eulerAngles.z + 180);
                }
                else
                {
                    bullet.transform.rotation = Quaternion.Euler(0, 0, bullet.transform.rotation.eulerAngles.z + 180);
                }

                if(bullet.gameObject.GetComponent<StickyBullet>())
                {
                    bullet.gameObject.GetComponent<StickyBullet>().isEnemy = false;
                }
                if (bullet.GetComponent<BoomerangBullet>())
                {
                    bullet.GetComponent<BoomerangBullet>().targetPos = target.position;
                    bullet.GetComponent<BoomerangBullet>().reachedTarget = false;
                }
            }
            else
            {
                ExplosionBullet expBullet = bullet.gameObject.GetComponent<ExplosionBullet>();
                Transform target = expBullet.whoShotIt;
                expBullet.gameObject.layer = 10;
                
                if (target)
                {
                   
                    Vector2 lookDir = target.position - expBullet.transform.position;
                    float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                    expBullet.transform.rotation = Quaternion.Euler(0, 0, angle);//bullet.transform.rotation.eulerAngles.z + 180);
                    expBullet.locationToExplode = target.position;
                }
                else
                {
                    expBullet.transform.rotation = Quaternion.Euler(0, 0, bullet.transform.rotation.eulerAngles.z + 180);
                    expBullet.locationToExplode = expBullet.transform.position + expBullet.transform.right;
                }
                
            }
            bullet.damage *= damageMultiplier;
            Destroy(bullet.gameObject.GetComponent<TrailRenderer>());
            Instantiate(trail, bullet.transform.position, bullet.transform.rotation, bullet.transform).time /= (.5f * bullet.speed);
        }
    }

    void GoOnCooldown()
    {
        ability.CancelUpdateAbility();
    }
}
