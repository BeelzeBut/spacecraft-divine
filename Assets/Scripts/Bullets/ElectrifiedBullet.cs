using DigitalRuby.LightningBolt;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectrifiedBullet : Bullet
{
    PlayerController p;
    public float lifeTime = 5f;
    public float fireRate = .5f;
    public LightningBoltScript lightningPrefab;
    public float radius = 1.25f;
    private LightningBoltScript[] lightnings = new LightningBoltScript[15];
    private LineRenderer[] lines = new LineRenderer[15];
    public AudioClip shootSound;

    private void Start()
    {
        p = PlayerController.instance;
        InvokeRepeating("LookForTargets", 0, fireRate);
        for(int i = 0; i < 15; i ++)
        {
            lightnings[i] = Instantiate(lightningPrefab, transform.position, transform.rotation, transform);
            lines[i] = lightnings[i].GetComponent<LineRenderer>();
            lines[i].enabled = false;
        }
    }
    private void Update()
    {
        lifeTime -= Time.deltaTime;
        if(lifeTime <= 0)
        {
            Explode();
            Destroy(gameObject);
        }
    }
    private void LookForTargets()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Units"));
        if (hits.Length > 0)
            SoundManager.instance.soundSource.PlayOneShot(shootSound);
        for(int i = 0; i < Mathf.Clamp(hits.Length, 0, 15) ; i++)
        {
            StartCoroutine(LightningDamage(hits[i].GetComponent<Enemy>(), i));
        }
    }
    public IEnumerator LightningDamage(Enemy enemy, int i)
    {
        lines[i].positionCount = 0;
        float elapsed = 0;
        if (enemy.canTakeDamage)
        {
            float damage = this.damage;
            bool isCritical = Random.Range(0, 101) < p.critChance;
            if (isCritical)
            {
                damage *=  p.critMultiplier;
                isCrit = isCritical;
            }
            enemy.TakeDamage((damage * damageMultiplier) * (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f));
            if (isCrit)
                Instantiate(GameManager.instance.criticalText, enemy.transform.position, Quaternion.identity);
            if (enemy.canBePushedBack && hasPushBack)
            {
                Vector2 forceDir = (enemy.transform.position - transform.position).normalized;
                enemy.rb.AddForce(forceDir * pushBack / 2f, ForceMode2D.Impulse);
            }
        }
        else
        {
            Instantiate(GameManager.instance.immuneText, enemy.transform.position, Quaternion.identity);
        }
        while (elapsed < .15f)
        {
            if (enemy)
            {
                lightnings[i].StartPosition = transform.position;
                lightnings[i].EndPosition = enemy.transform.position;
            }
            else break;
            lines[i].enabled = true;
            elapsed += Time.deltaTime;
            yield return null;
        }
        lines[i].enabled = false;
    }
    
}