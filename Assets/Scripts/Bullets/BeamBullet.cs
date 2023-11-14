using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeamBullet : Bullet
{
    public LayerMask layermask;
    public LineRenderer beam;
    public float startWidth;
    public float maxWidth;
    bool hasActivated = false;
    public float explosionScale = 1f;
    void Start()
    {
        if (!canPierce)
            if (gameObject.layer == 10)
                layermask = layermask | LayerMask.GetMask("Units");
            else if (gameObject.layer == 11)
                layermask |= LayerMask.GetMask("Player");
        startWidth = charged * maxWidth;
        beam.startWidth = 0;
        StartCoroutine(Beam());
    }
    private void FixedUpdate()
    {
       
    }
    IEnumerator Beam()
    {
        beam.SetPosition(0, transform.position);
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, Mathf.Clamp(startWidth / 2f, 0.01f, 100), transform.right, gameObject.layer == 10 ? PlayerController.instance.ship.range : 30f, layermask);
        if (hit.collider)
        {
            beam.SetPosition(1, hit.point);
        }
        else
        {
            beam.SetPosition(1, transform.position + transform.right * 10);
        }
        float elapsed = 0;
        BoxCollider2D beamCollider = beam.GetComponent<BoxCollider2D>();
        beamCollider.size = new Vector2((beam.GetPosition(1) - beam.GetPosition(0)).magnitude + (gameObject.layer == 10 ? .15f : .75f), startWidth * .5f);
        beamCollider.offset = new Vector2((beam.GetPosition(1) - beam.GetPosition(0)).magnitude / 2f + .075f, 0);
        
        while (elapsed < startWidth)
        {
            elapsed += 20 * startWidth * Time.deltaTime;
            beam.startWidth = elapsed;
            if (elapsed >= startWidth / 2f && !hasActivated)
            {
                beamCollider.enabled = true;
                GameObject exp = Instantiate(explosion, beam.GetPosition(1), Quaternion.identity);
                exp.transform.localScale *= explosionScale;
                hasActivated = true;
                exp.transform.localScale *= charged;
            }
            yield return null;
        }
        beam.startWidth = startWidth;

        yield return new WaitForSeconds(.225f);

        beamCollider.enabled = false;
        while(elapsed > 0)
        {
            elapsed -= 20 * startWidth * Time.deltaTime;
            beam.startWidth = elapsed;
            yield return null;
        }
        Destroy(gameObject);

    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
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
                enemy.TakeDamage(damage * (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f));
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
            if (canPierce)
            {
                GameObject exp = Instantiate(explosion, other.transform.position, other.transform.rotation);
                exp.transform.localScale *= charged * explosionScale;
            }
            if (canExplode)
            {
                Explosion explosion = Instantiate(blast, other.transform.position, Quaternion.Euler(0, 0, Random.Range(0, 360)));
                explosion.damage = (damage * damageMultiplier);
                explosion.gameObject.layer = gameObject.layer;
                explosion.GetComponentInChildren<ExplosionDamage>().ignoredTarget = other.transform;
            }
        }
        else if(other.CompareTag("Player"))
        {
            if (PlayerController.instance.invincibility <= 0)
            {
                //DamagePopup.Create(other.transform.position, (int)damage, isCrit, other.gameObject);
                if (PlayerController.instance.canTakeDamage && hasPushBack)
                {
                    Vector2 forceDir;
                    if (PlayerController.instance.reducePushBack)
                        forceDir = (other.transform.position - whoShotIt.position).normalized * .5f;
                    else
                        forceDir = (other.transform.position - whoShotIt.position).normalized;
                    PlayerController.instance.rb.AddForce(forceDir * damage, ForceMode2D.Impulse);
                }
            }
            PlayerController.instance.TakeDamage(damage);
            if (canPierce)
            {
                GameObject exp = Instantiate(explosion, other.transform.position, other.transform.rotation);
                exp.transform.localScale *= charged * explosionScale;
            }
        }
    }
}
