using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyBullet : Bullet
{
    public bool goToPlayer;
    bool hasGoneToPlayer;
    public float rotationDuration = 1f;
    public Animator anim;
    float startSpeed;
    bool hasHitWall;
    public bool isEnemy = true;
    void Start()
    {
        startSpeed = speed;
    }

    private void Update()
    {
        transform.position += transform.right * Time.deltaTime * speed;
        if (goToPlayer && hasHitWall && isEnemy)
        {
            hasHitWall = true;
            goToPlayer = false;
            StartCoroutine(GoToPlayer(PlayerController.instance.transform));
        }
    }

    IEnumerator GoToPlayer(Transform player)
    {
        float elapsed = 0;
        Vector2 lookDir = player.position - transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg + Random.Range(-1f, 1f) * 1.5f * 7.5f;
        Quaternion initialRotation = transform.rotation;
        while(elapsed < rotationDuration)
        {
            transform.rotation = Quaternion.Lerp(initialRotation, Quaternion.Euler(0, 0, angle), elapsed / rotationDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0, 0, angle);

        yield return new WaitForSeconds(.1f);
        hasGoneToPlayer = true;
        trail.enabled = true;
        GetComponent<Collider2D>().enabled = true;
        speed = startSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
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
                enemy.TakeDamage(damage * (1 + PlayerController.instance.attackMultiplier / 100f));
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
        }
        if (other.CompareTag("Player"))
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
            Destroy(gameObject);
            return;
        }
        hasHitWall = true;
        if (!hasGoneToPlayer)
        {
            speed = 0;
            GetComponent<Collider2D>().enabled = false;
        }
        else
        {
            Explode();
            Destroy(gameObject);
        }
    }
}
