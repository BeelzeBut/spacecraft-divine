using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Transform muzzleFlash;
    public float speed = 10f;
    public Rigidbody2D rb;
    public float damage = 0;
    public float damageMultiplier = 1;
    public bool hasPushBack = true;
    public float pushBack = 1f;
    public bool isCrit = false;
    public bool hasCharge = false;
    [HideInInspector]
    public float charged;
    [SerializeField]
    public GameObject explosion;
    public Explosion blast;
    public TrailRenderer trail;
    public bool classicTrigger = true;
    [SerializeField]
    public Transform whoShotIt;
    public LayerMask layerMask;
    RaycastHit2D hit;
    public float latentSpeed;
    public Transform mainBullet;
    public bool canBeDeflected = true;
    public bool shouldAccelerate;
    public float accelerationConstant;
    public float acceleration;

    [Header("Upgrade")]
    public bool canBounce;
    public bool canPierce;
    public bool limitedPiercings = true;
    public bool canSplit;
    public bool canExplode;
    public bool canFreeze;

    public LayerMask ignoreCollisionLayer;
    public int bounces = 1;
    public int pierces = 2;
    public int splitAmount;
    public float freezeTime = 1.25f;
    public Transform iceExplosion;
    public bool isComposed;
    public bool shouldDestroyOnCollision = true;

    private void Awake()
    {
        if(gameObject.layer == 11)
            damageMultiplier = GameManager.instance.damageScale;
    }

    void FixedUpdate()
    {
        if (speed > 0)
        {
            rb.MovePosition(rb.position + (Vector2)transform.right * speed * Time.fixedDeltaTime);
        }
        if (shouldAccelerate)
        {
            Mathf.Clamp(speed += acceleration * Time.fixedDeltaTime, 0, 50);
            acceleration += accelerationConstant * Time.fixedDeltaTime;
            
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (classicTrigger)
        {
            if ((ignoreCollisionLayer & (1 << other.gameObject.layer)) == (1 << other.gameObject.layer))
                return;
            else
            {
                if (other.CompareTag("Deflect"))
                    return;
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
                    if (canFreeze && enemy.canTakeDamage)
                    {
                        enemy.Freeze(Random.Range(freezeTime * 3 / 4f, freezeTime * 5 / 4f), Color.blue);
                        enemy.iceExplosion = iceExplosion;
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
                    if (canBounce && bounces > 0)
                        Bounce();
                    else
                    {
                        if(shouldDestroyOnCollision)
                            Destroy(gameObject);
                        return;
                    }
                }
            }
        }
    }
    public void Explode()
    {
        if (explosion)
        {
            GameObject explosion = Instantiate(this.explosion, transform.position, Quaternion.Euler(0,0, Random.Range(0,360)));
            explosion.GetComponent<Explosion>().isCrit = isCrit;
            explosion.layer = gameObject.layer;
            explosion.gameObject.SetActive(true);
            explosion.transform.localScale = Vector3.one * transform.localScale.x;
        }
    }

    public void Bounce()
    {
        if (mainBullet)
        {
            transform.SetParent(null);
            speed = latentSpeed;
            transform.rotation = mainBullet.rotation;
        }
        
        Vector2 pos = transform.position - transform.right * .25f;
        hit = Physics2D.Raycast(transform.position, transform.right, 10f, layerMask);
        Vector2 dir = transform.right;
        transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z - ((-90 + Vector2.SignedAngle(hit.normal, dir)) * 2));
        bounces--;
        StartCoroutine(ResetCollider());
    }

    IEnumerator ResetCollider()
    {
        GetComponent<Collider2D>().enabled = false;
        yield return new WaitForSeconds(.05f);
        GetComponent<Collider2D>().enabled = true;
    }
}
