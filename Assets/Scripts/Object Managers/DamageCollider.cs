using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageCollider : MonoBehaviour
{
    public float damage;
    public float pushBack = 15f;
    public GameObject explosion;
    public GameObject impactExplosion;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy;
            if ((enemy = other.GetComponent<Enemy>()) != null)
            {
                enemy = other.GetComponentInParent<Enemy>();
            }

            if (enemy.canTakeDamage)
            {
                if (!enemy.GetComponent<DamageIfHitsWall>())
                {
                    enemy.StopAllCoroutines();
                    StartCoroutine(enemy.StopAttack());
                    enemy.TakeDamage((damage) * (1 + (PlayerController.instance.attackMultiplier + PlayerController.instance.damageMultiplier) / 100f));
                    Instantiate(impactExplosion, enemy.transform.position, Quaternion.Euler(transform.rotation.eulerAngles + Vector3.forward * 90));

                    foreach(Collider2D col in enemy.GetComponentsInChildren<Collider2D>())
                    {
                        col.enabled = false;
                    }
                    Vector2 forceDir = transform.up;
                    enemy.rb.AddForce(forceDir * pushBack * enemy.rb.mass, ForceMode2D.Impulse);
                    enemy.rb.drag *= 3f;
                    DamageIfHitsWall dmg = enemy.gameObject.AddComponent<DamageIfHitsWall>();
                    dmg.damage = 1.5f * damage;
                    dmg.explosion = explosion;
                    dmg.freezeTime = 1.5f;
                    foreach (Collider2D col in enemy.GetComponentsInChildren<Collider2D>())
                    {
                        col.enabled = true;
                    }
                }

            }
        }
    }
}
