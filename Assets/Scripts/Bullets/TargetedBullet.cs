using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetedBullet : ExplodeBullet
{
    public float turningSpeed = 5f;
    public float turningSpeedIncrement = 0f;
    public Transform target;
    public bool shouldTurn = true;
    void Start()
    {
        if (explodeAtLocation)
            explosion.GetComponent<Explosion>().damage = damage * gameObject.layer == 10 ? (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f) : 1;
    }

    void FixedUpdate()
    {
        GoToEnemy();
        turningSpeed += turningSpeedIncrement * Time.fixedDeltaTime;
    }

    void GoToEnemy()
    {
        if (target)
        {
            if (shouldTurn)
            {
                Vector3 Target = target.transform.position;

                Vector3 lookDirection = Target - transform.position;
                var angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Slerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningSpeed * Time.deltaTime);

                if (Mathf.Abs((transform.position - Target).sqrMagnitude) <= .15f && explodeAtLocation)
                {
                    Explode();
                    Destroy(gameObject);
                }
            }
        }
        else if(locationToExplode != Vector3.zero)
        {
            if (shouldTurn)
            {
                Vector3 lookDirection = locationToExplode - transform.position;
                var angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Slerp(transform.rotation, (Quaternion.AngleAxis(angle, Vector3.forward)), turningSpeed * Time.deltaTime);

                if (Mathf.Abs((transform.position - locationToExplode).sqrMagnitude) <= .15f)
                {
                    if (explodeAtLocation)
                    {
                        Explode();
                        Destroy(gameObject);
                    }
                    else
                        StopTargeting(0);
                }
            }
        }

        rb.MovePosition(rb.position + (Vector2)transform.right * speed * Time.fixedDeltaTime);
    }

    public void StopTargeting(float time)
    {
        StartCoroutine(StopTargetingC(time));
    }
    public IEnumerator StopTargetingC(float time)
    {
        yield return new WaitForSeconds(time);
        shouldTurn = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((ignoreCollisionLayer & 1 << other.gameObject.layer) == 1 << other.gameObject.layer)
            return;
        if (other.CompareTag("Deflect"))
            return;
        if (!classicTrigger)
        {
            if (other.gameObject.layer == 0 || other.gameObject.layer == 16)
            {
                Explode();
                Destroy(gameObject);
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
                        Vector2 forceDir = (other.transform.position - transform.position).normalized;
                        other.GetComponent<Rigidbody2D>().AddForce(forceDir * pushBack / 2f, ForceMode2D.Impulse);
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
                    exp.damage = (damage * damageMultiplier);
                    exp.gameObject.layer = gameObject.layer;
                    exp.GetComponentInChildren<ExplosionDamage>().ignoredTarget = other.transform;
                }
                if (!canPierce)
                {
                    Destroy(gameObject);
                    return;
                }
                else
                {
                    target = null;
                }
            }
            else
            if (other.gameObject.CompareTag("Player"))
            {
                if (PlayerController.instance.invincibility <= 0)
                {
                    //DamagePopup.Create(other.transform.position, (int)damage, isCrit, other.gameObject);
                    if (PlayerController.instance.canTakeDamage)
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
                return;
            }
            else
            {
                Explode();
                if (canBounce && bounces > 0)
                    Bounce();
                else
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
