using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackExplosion : ExplosionDamage
{
    private void Start()
    {
        if (shouldMakeSound)
        {
            SoundManager.instance.soundSource.PlayOneShot(explosionSound);
        }
        damage /= GetComponentInParent<Explosion>().lifeTime;
        InvokeRepeating("LookForTargets", 0, 0.25f);
    }

    private void LookForTargets()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.25f, LayerMask.GetMask("Units"));
    
        for (int i = 0; i < hits.Length; i++)
        {
            Abduct(hits[i].GetComponent<Enemy>());
        }
    }
    public void Abduct(Enemy enemy)
    {
        if (enemy.canTakeDamage)
        {
            float damage = this.damage / 4f;

            enemy.TakeDamage(damage * (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f));

        }
        else
        {
            Instantiate(GameManager.instance.immuneText, enemy.transform.position, Quaternion.identity);
        }
        if (enemy.canBePushedBack)
        {
            Vector2 forceDir = -(enemy.transform.position - transform.position);
            if (forceDir.sqrMagnitude <= 0.05f)
            {
                forceDir *= 2f;
            }
            enemy.rb.AddForce(forceDir * 4f, ForceMode2D.Impulse);
        }
    }

    IEnumerator SlowEnemy(Enemy enemy)
    {
        float enemyMoveSpeed = enemy.moveSpeed;
        enemy.moveSpeed /= 2f;

        yield return new WaitForSeconds(1.5f);

        enemy.moveSpeed = enemyMoveSpeed;
        enemy.curMoveSpeed = enemy.moveSpeed;
    }

}
